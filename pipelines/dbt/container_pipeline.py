"""Container-side fixture, rollback, replay, and dbt validation stages."""
import json
from pathlib import Path
import shutil
import subprocess
import unittest

from load_fixtures import fixture_batch
from quality_gate import run_gate
from verify_fixture_replay import connect_database, verify


PROFILE = """mediapulse_dq:
  target: isolated
  outputs:
    isolated:
      type: sqlserver
      driver: ODBC Driver 18 for SQL Server
      server: sqlserver
      database: MedipulseMain
      schema: dbo
      username: sa
      password: "{{ env_var('DBT_ENV_SECRET_SA_PASSWORD') }}"
      port: 1433
      encrypt: true
      trust_cert: true
"""


def run(evidence=Path("/evidence"), work=Path("/tmp/quality-project")):
    source = Path(__file__).resolve().parent
    summary = {"status": "running", "stages": []}
    evidence.mkdir(parents=True, exist_ok=True)
    stage = "prepare-project"
    try:
        shutil.copytree(source, work, ignore=shutil.ignore_patterns(
            "target", "logs", "__pycache__", "dbt_packages", "dbt_modules"))
        profiles = work / "isolated-profile"
        profiles.mkdir()
        (profiles / "profiles.yml").write_text(PROFILE, encoding="utf-8")
        stage = "load-fixtures"
        connection = connect_database(autocommit=True)
        try:
            cursor = connection.cursor()
            try:
                cursor.execute(fixture_batch())
                while cursor.nextset():
                    pass
            finally:
                cursor.close()
        finally:
            connection.close()
        summary["stages"].append(stage)

        stage = "verify-atomicity"
        suite = unittest.defaultTestLoader.discover(
            str(source / "runner_tests"), pattern="test_atomic_sqlserver.py")
        with (evidence / "atomicity-tests.txt").open("w", encoding="utf-8") as stream:
            result = unittest.TextTestRunner(stream=stream, verbosity=2).run(suite)
        if result.testsRun != 3 or result.skipped or not result.wasSuccessful():
            raise RuntimeError("Atomicity tests failed or did not execute; see atomicity-tests.txt")
        summary["stages"].append(stage)

        stage = "verify-replay"
        connection = connect_database()
        try:
            replay = verify(connection)
        finally:
            connection.close()
        (evidence / "fixture-replay.json").write_text(json.dumps(replay, indent=2), encoding="utf-8")
        if replay["status"] != "passed":
            raise RuntimeError("Fixture replay changed rows")
        summary["stages"].append(stage)

        stage = "dbt-dependencies"
        subprocess.run(["dbt", "deps", "--project-dir", str(work),
                        "--profiles-dir", str(profiles)], check=True)
        summary["stages"].append(stage)
        stage = "dbt-gate"
        run_gate(work, profiles)
        summary["stages"].append(stage)
        summary["status"] = "passed"
    except BaseException:
        summary.update(status="failed", failed_stage=stage)
        raise
    finally:
        for folder in ("target", "logs"):
            if (work / folder).exists():
                shutil.copytree(work / folder, evidence / folder, dirs_exist_ok=True)
        (evidence / "validation-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")


if __name__ == "__main__":
    run()
