"""Apply bounded consumption journal batches to a separate daily aggregate."""
import argparse
from datetime import datetime
import json
import re

from verify_fixture_replay import connect_database, quote


class ConsumerBusy(RuntimeError):
    pass


def relations(schema):
    if not re.fullmatch(r"[A-Za-z][A-Za-z0-9_]{0,63}", schema):
        raise ValueError("Invalid SQL schema name")
    prefix = f"{quote(schema)}."
    return prefix + "fct_daily_consumption_incremental", prefix + "ConsumptionChangeAck"


def acquire_lock(cursor, schema):
    result = cursor.execute("""
        DECLARE @result int;
        EXEC @result = sys.sp_getapplock @Resource=?, @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=0;
        SELECT @result;
    """, f"MediPulse:consumption:{schema}").fetchone()[0]
    if result < 0:
        raise ConsumerBusy("Another consumption processor owns this target")


def ensure_tables(connection, schema="dbo"):
    target, ack = relations(schema)
    cursor = connection.cursor()
    try:
        cursor.execute("USE MedipulseMain")
        cursor.execute("SET NOCOUNT ON; SET XACT_ABORT ON;")
        acquire_lock(cursor, schema)
        cursor.execute(f"""
            IF OBJECT_ID(N'{schema}.fct_daily_consumption_incremental', N'U') IS NULL
            CREATE TABLE {target} (
                ConsumptionDate date NOT NULL,
                FacilityId int NOT NULL,
                ItemId int NOT NULL,
                TotalQuantityConsumed bigint NOT NULL,
                ConsumptionEventCount bigint NOT NULL,
                CONSTRAINT {quote('PK_' + schema + '_DailyConsumptionIncremental')}
                    PRIMARY KEY (ConsumptionDate, FacilityId, ItemId));
        """)
        cursor.execute(f"""
            IF OBJECT_ID(N'{schema}.ConsumptionChangeAck', N'U') IS NULL
            CREATE TABLE {ack} (
                ChangeId bigint NOT NULL PRIMARY KEY,
                AppliedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
        """)
        connection.commit()
    except BaseException:
        connection.rollback()
        raise
    finally:
        cursor.close()


def apply_batch(connection, batch_size=100, schema="dbo", fail_before_ack=False):
    if not 1 <= batch_size <= 1000:
        raise ValueError("Batch size must be between 1 and 1000")
    target, ack = relations(schema)
    cursor = connection.cursor()
    try:
        cursor.execute("USE MedipulseMain")
        cursor.execute("SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 30000;")
        acquire_lock(cursor, schema)

        rows = cursor.execute(f"""
            SELECT TOP (?) j.ChangeId, j.BeforeConsumedDate, j.BeforeFacilityId,
                j.BeforeItemId, j.AfterConsumedDate, j.AfterFacilityId, j.AfterItemId
            FROM dbo.ConsumptionChange j WITH (READPAST, READCOMMITTEDLOCK)
            LEFT JOIN {ack} a ON a.ChangeId = j.ChangeId
            WHERE a.ChangeId IS NULL ORDER BY j.ChangeId;
        """, batch_size).fetchall()
        if not rows:
            connection.commit()
            return {"events": 0, "groups": 0}

        affected = set()
        for _, before_date, before_facility, before_item, after_date, after_facility, after_item in rows:
            if before_date is not None:
                affected.add((before_date.date() if isinstance(before_date, datetime) else before_date,
                              before_facility, before_item))
            if after_date is not None:
                affected.add((after_date.date() if isinstance(after_date, datetime) else after_date,
                              after_facility, after_item))

        cursor.execute("CREATE TABLE #AffectedGroups (ConsumptionDate date NOT NULL, "
                       "FacilityId int NOT NULL, ItemId int NOT NULL, "
                       "PRIMARY KEY (ConsumptionDate, FacilityId, ItemId))")
        cursor.executemany("INSERT #AffectedGroups VALUES (?, ?, ?)", sorted(affected))
        cursor.execute(f"""
            DELETE target FROM {target} target JOIN #AffectedGroups g
                ON g.ConsumptionDate=target.ConsumptionDate AND g.FacilityId=target.FacilityId
                AND g.ItemId=target.ItemId;
            INSERT {target} (ConsumptionDate, FacilityId, ItemId,
                TotalQuantityConsumed, ConsumptionEventCount)
            SELECT g.ConsumptionDate, g.FacilityId, g.ItemId,
                SUM(CAST(s.QuantityConsumed AS bigint)), COUNT_BIG(*)
            FROM #AffectedGroups g
            JOIN dbo.ConsumptionRecord s ON CAST(s.ConsumedDate AS date)=g.ConsumptionDate
                AND s.FacilityId=g.FacilityId AND s.ItemId=g.ItemId
            GROUP BY g.ConsumptionDate, g.FacilityId, g.ItemId;
        """)
        if fail_before_ack:
            raise RuntimeError("Injected failure before acknowledgement")
        cursor.executemany(f"INSERT {ack} (ChangeId) VALUES (?)", [(row[0],) for row in rows])
        cursor.execute("DROP TABLE #AffectedGroups")
        connection.commit()
        return {"events": len(rows), "groups": len(affected)}
    except BaseException:
        connection.rollback()
        raise
    finally:
        cursor.close()


def run(connection, batch_size=100, max_batches=10, schema="dbo"):
    if not 1 <= max_batches <= 1000:
        raise ValueError("Max batches must be between 1 and 1000")
    ensure_tables(connection, schema)
    report = {"batches": 0, "events": 0, "groups_recomputed": 0}
    for _ in range(max_batches):
        result = apply_batch(connection, batch_size, schema)
        if result["events"] == 0:
            break
        report["batches"] += 1
        report["events"] += result["events"]
        report["groups_recomputed"] += result["groups"]
    _, ack = relations(schema)
    cursor = connection.cursor()
    try:
        cursor.execute("USE MedipulseMain")
        report["drained"] = cursor.execute(f"""
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.ConsumptionChange j
                LEFT JOIN {ack} a ON a.ChangeId=j.ChangeId WHERE a.ChangeId IS NULL
            ) THEN 0 ELSE 1 END
        """).fetchone()[0] == 1
        connection.commit()
    finally:
        cursor.close()
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--batch-size", type=int, default=100)
    parser.add_argument("--max-batches", type=int, default=10)
    args = parser.parse_args()
    connection = connect_database()
    try:
        result = run(connection, args.batch_size, args.max_batches)
        print(json.dumps(result))
        if not result["drained"]:
            raise SystemExit("Unacknowledged consumption changes remain; rerun the processor")
    finally:
        connection.close()
