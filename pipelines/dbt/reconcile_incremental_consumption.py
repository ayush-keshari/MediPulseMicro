"""Check or repair a bounded date range of the incremental consumption mart."""
import argparse
from datetime import date, datetime, timedelta
import json

from incremental_consumption import acquire_lock, relations
from verify_fixture_replay import connect_database


def validate_window(start, end):
    if (not isinstance(start, date) or isinstance(start, datetime)
            or not isinstance(end, date) or isinstance(end, datetime)
            or end < start or (end - start).days >= 31):
        raise ValueError("Choose an inclusive date range of 1 to 31 days")
    return end + timedelta(days=1)


def _open(connection, schema):
    target, ack = relations(schema)
    cursor = connection.cursor()
    try:
        cursor.execute("USE MedipulseMain")
        cursor.execute("SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 30000; "
                       "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;")
        acquire_lock(cursor, schema)
        return cursor, target, ack
    except BaseException:
        connection.rollback()
        cursor.execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED")
        connection.commit()
        cursor.close()
        raise


def _close(connection, cursor, success):
    try:
        if success:
            connection.commit()
        else:
            connection.rollback()
    finally:
        try:
            cursor.execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED")
            connection.commit()
        finally:
            cursor.close()


def _require_drained(cursor, ack):
    pending = cursor.execute(f"""
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM dbo.ConsumptionChange j
            LEFT JOIN {ack} a ON a.ChangeId=j.ChangeId WHERE a.ChangeId IS NULL
        ) THEN 1 ELSE 0 END
    """).fetchone()[0]
    if pending:
        raise ValueError("Drain the consumption journal before reconciliation or backfill")


def reconcile_window(connection, start, end, schema="dbo"):
    next_day = validate_window(start, end)
    cursor, target, ack = _open(connection, schema)
    success = False
    try:
        _require_drained(cursor, ack)
        differences = cursor.execute(f"""
            WITH source_groups AS (
                SELECT CAST(ConsumedDate AS date) AS ConsumptionDate, FacilityId, ItemId,
                    SUM(CAST(QuantityConsumed AS bigint)) AS TotalQuantityConsumed,
                    COUNT_BIG(*) AS ConsumptionEventCount
                FROM dbo.ConsumptionRecord
                WHERE ConsumedDate >= ? AND ConsumedDate < ?
                GROUP BY CAST(ConsumedDate AS date), FacilityId, ItemId
            )
            SELECT COUNT_BIG(*) FROM source_groups s
            FULL OUTER JOIN (
                SELECT ConsumptionDate, FacilityId, ItemId, TotalQuantityConsumed,
                    ConsumptionEventCount FROM {target}
                WHERE ConsumptionDate >= ? AND ConsumptionDate < ?
            ) t ON s.ConsumptionDate=t.ConsumptionDate AND s.FacilityId=t.FacilityId
                AND s.ItemId=t.ItemId
            WHERE s.ConsumptionDate IS NULL OR t.ConsumptionDate IS NULL
                OR s.TotalQuantityConsumed<>t.TotalQuantityConsumed
                OR s.ConsumptionEventCount<>t.ConsumptionEventCount;
        """, start, next_day, start, next_day).fetchone()[0]
        success = True
        return {"start": start.isoformat(), "end": end.isoformat(),
                "differing_groups": differences, "status": "passed" if differences == 0 else "drift"}
    finally:
        _close(connection, cursor, success)


def backfill_window(connection, start, end, schema="dbo", fail_before_insert=False):
    next_day = validate_window(start, end)
    cursor, target, ack = _open(connection, schema)
    success = False
    try:
        _require_drained(cursor, ack)
        cursor.execute(f"DELETE FROM {target} WHERE ConsumptionDate>=? AND ConsumptionDate<?",
                       start, next_day)
        if fail_before_insert:
            raise RuntimeError("Injected failure before backfill insert")
        rows = cursor.execute(f"""
            INSERT INTO {target} (ConsumptionDate, FacilityId, ItemId,
                TotalQuantityConsumed, ConsumptionEventCount)
            SELECT CAST(ConsumedDate AS date), FacilityId, ItemId,
                SUM(CAST(QuantityConsumed AS bigint)), COUNT_BIG(*)
            FROM dbo.ConsumptionRecord WHERE ConsumedDate>=? AND ConsumedDate<?
            GROUP BY CAST(ConsumedDate AS date), FacilityId, ItemId;
            SELECT @@ROWCOUNT;
        """, start, next_day).fetchone()[0]
        success = True
        return {"start": start.isoformat(), "end": end.isoformat(), "groups_rebuilt": rows}
    finally:
        _close(connection, cursor, success)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("check", "backfill"))
    parser.add_argument("--start", type=date.fromisoformat, required=True)
    parser.add_argument("--end", type=date.fromisoformat, required=True)
    args = parser.parse_args()
    connection = connect_database()
    try:
        result = (reconcile_window if args.operation == "check" else backfill_window)(
            connection, args.start, args.end)
        print(json.dumps(result))
        if result.get("status") == "drift":
            raise SystemExit(1)
    finally:
        connection.close()
