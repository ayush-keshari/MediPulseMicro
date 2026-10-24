"""Prove that rebuilding the daily mart preserves rows for unchanged source data."""
from collections import Counter
import json
import shutil
import subprocess

from verify_fixture_replay import connect_database, quote


def verify_rebuild(project, profiles, evidence):
    report = {"status": "failed", "model": "daily_consumption"}
    try:
        manifest = json.loads((project / "target/manifest.json").read_text(encoding="utf-8"))
        model = next(node for node in manifest["nodes"].values()
                     if node.get("resource_type") == "model" and node.get("name") == "daily_consumption")
        relation = ".".join(quote(model[key]) for key in ("database", "schema", "alias"))

        def snapshot():
            connection = connect_database(autocommit=True)
            try:
                cursor = connection.cursor()
                try:
                    return Counter(tuple(row) for row in cursor.execute(
                        "SELECT ConsumptionDate, FacilityId, ItemId, TotalQuantityConsumed, "
                        f"ConsumptionEventCount FROM {relation}").fetchall())
                finally:
                    cursor.close()
            finally:
                connection.close()

        before = snapshot()
        for command in ("run", "test"):
            results_path = project / "target/run_results.json"
            results_path.unlink(missing_ok=True)
            subprocess.run(["dbt", command, "--project-dir", str(project),
                            "--profiles-dir", str(profiles), "--target-path", str(project / "target"),
                            "--select", "daily_consumption", "--no-partial-parse"], check=True)
            results = json.loads(results_path.read_text(encoding="utf-8"))["results"]
            if not results or any(row.get("status") not in {"pass", "success", "warn"} for row in results):
                raise ValueError(f"Mart rebuild {command} produced no passing results")
            shutil.copy2(results_path, evidence / f"mart-rebuild-{command}.json")
        after = snapshot()
        report.update(rows_compared=sum(before.values()), unchanged=before == after)
        if before != after:
            raise ValueError("Rebuilding the daily mart changed its rows")
        report["status"] = "passed"
    finally:
        (evidence / "mart-rebuild.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report
