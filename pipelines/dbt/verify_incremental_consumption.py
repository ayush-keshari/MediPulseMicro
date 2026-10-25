"""Compare the journal processor with the independently rebuilt dbt mart."""
from collections import Counter
import json

from incremental_consumption import relations, run
from reconcile_incremental_consumption import reconcile_window
from verify_fixture_replay import connect_database


def verify(evidence):
    report = {"status": "failed"}
    try:
        connection = connect_database()
        try:
            result = run(connection, batch_size=2, max_batches=100)
            if not result["drained"]:
                raise ValueError("Incremental consumer did not drain the fixture journal")
            target, _ = relations("dbo")
            cursor = connection.cursor()
            try:
                cursor.execute("USE MedipulseMain")
                columns = "ConsumptionDate, FacilityId, ItemId, TotalQuantityConsumed, ConsumptionEventCount"
                incremental = Counter(tuple(row) for row in cursor.execute(
                    f"SELECT {columns} FROM {target}").fetchall())
                full = Counter(tuple(row) for row in cursor.execute(
                    f"SELECT {columns} FROM dbo.fct_daily_consumption").fetchall())
                if incremental != full:
                    raise ValueError("Incremental result differs from the full source rebuild")
            finally:
                cursor.close()
            replay = run(connection, batch_size=2, max_batches=1)
            if replay["events"] != 0 or not replay["drained"]:
                raise ValueError("Repeat processing was not idempotent")
            days = [row[0] for row in full]
            if not days:
                raise ValueError("Fixture mart unexpectedly has no consumption groups")
            reconciliation = reconcile_window(connection, min(days), max(days))
            if reconciliation["differing_groups"]:
                raise ValueError("Date-window reconciliation found consumption drift")
            report.update(status="passed", **result, rows_compared=sum(full.values()),
                          repeat_events=replay["events"], reconciliation=reconciliation)
        finally:
            connection.close()
    finally:
        (evidence / "incremental-consumption.json").write_text(
            json.dumps(report, indent=2), encoding="utf-8")
    return report
