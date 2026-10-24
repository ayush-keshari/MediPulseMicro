import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import container_pipeline as container


class ContainerPipelineTests(unittest.TestCase):
    def exercise(self, failure=None):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        evidence = Path(temporary.name) / "evidence"
        work = Path(temporary.name) / "project"
        connection = Mock()
        connection.cursor.return_value.nextset.return_value = False
        atomicity = Mock(testsRun=3, skipped=[], wasSuccessful=Mock(return_value=True))
        if failure == "atomicity":
            atomicity.skipped = [("test", "disabled")]

        def gate(*args):
            target = work / "target"
            target.mkdir()
            (target / "run_results.json").write_text('{"evidence":"preserved"}')
            if failure == "dbt":
                raise RuntimeError("dbt failure")

        if failure == "fixtures":
            connection.cursor.return_value.execute.side_effect = RuntimeError("fixture failure")
        with patch.object(container, "connect_database", return_value=connection), patch.object(
                container, "fixture_batch", return_value="SELECT 1"), patch.object(
                container.unittest.defaultTestLoader, "discover", return_value=Mock()), patch.object(
                container.unittest, "TextTestRunner") as runner, patch.object(
                container, "verify", return_value={"status": "failed" if failure == "replay" else "passed"}), patch.object(
                container.subprocess, "run"), patch.object(container, "run_gate", side_effect=gate):
            runner.return_value.run.return_value = atomicity
            if failure:
                with self.assertRaises(RuntimeError):
                    container.run(evidence, work)
            else:
                container.run(evidence, work)
        report = json.loads((evidence / "validation-summary.json").read_text())
        connection.close.assert_called()
        return report, evidence

    def test_success_runs_all_required_stages(self):
        report, _ = self.exercise()
        self.assertEqual(report["status"], "passed")
        self.assertEqual(report["stages"], ["load-fixtures", "verify-atomicity", "verify-replay",
                                           "dbt-dependencies", "dbt-gate"])

    def test_failures_stop_pipeline_and_record_stage(self):
        for failure, stage in [("fixtures", "load-fixtures"), ("atomicity", "verify-atomicity"),
                               ("replay", "verify-replay"), ("dbt", "dbt-gate")]:
            with self.subTest(failure=failure):
                report, evidence = self.exercise(failure)
                self.assertEqual(report["status"], "failed")
                self.assertEqual(report["failed_stage"], stage)
                if failure == "dbt":
                    self.assertTrue((evidence / "target/run_results.json").is_file())
