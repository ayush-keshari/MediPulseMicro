import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import run_pipeline as pipeline


class PipelineTests(unittest.TestCase):
    def exercise(self, failure=None, logs_fail=False, cleanup_fail=False, missing_summary=False,
                 unsafe_config=False):
        calls = []
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)

        def execute(args, **kwargs):
            calls.append(args)
            checkpoint = Path(kwargs["env"]["QUALITY_OUTPUT"]) / "pipeline-summary.json"
            self.assertTrue(checkpoint.is_file())
            self.assertIn("project", json.loads(checkpoint.read_text()))
            output = "normal output " + kwargs["env"]["SA_PASSWORD"]
            code = 0
            if "config" in args:
                config = {"services": {"sqlserver": {}, "migrator": {
                    "volumes": [{"target": "/workspace", "read_only": True}]},
                    "quality": {"volumes": [{"target": "/workspace", "read_only": True}]}}}
                if unsafe_config:
                    config["services"]["sqlserver"]["ports"] = ["1433:1433"]
                output = json.dumps(config)
            if args[-1] == "quality" and "run" in args and not missing_summary:
                target = Path(kwargs["env"]["QUALITY_OUTPUT"])
                (target / "validation-summary.json").write_text('{"status":"passed"}')
            should_fail = (failure in ("build", "up") and failure in args or
                           failure in ("migrator", "quality") and "run" in args and args[-1] == failure)
            if should_fail:
                if failure == "build":
                    raise subprocess.TimeoutExpired(args, 1, output=b"partial build log")
                code = 1
            if (logs_fail and "logs" in args) or (cleanup_fail and "down" in args):
                code = 1
            return subprocess.CompletedProcess(args, code, output)

        with patch.object(pipeline.subprocess, "run", side_effect=execute):
            result = pipeline.run_pipeline(root, root / "reports", timeout=1)
        evidence = Path(result["evidence"])
        self.assertEqual(json.loads((evidence / "pipeline-summary.json").read_text()), result)
        for log in evidence.glob("*.log"):
            self.assertNotIn("Mp!", log.read_text())
        return result, calls

    def test_success_uses_unique_project_and_cleans_only_its_stack(self):
        result, calls = self.exercise()
        self.assertEqual(result["status"], "passed")
        self.assertEqual(result["cleanup"], "passed")
        self.assertEqual(result["stages"], ["build", "database", "migrations", "validation"])
        for command in calls[1:]:
            self.assertEqual(command[command.index("--project-name") + 1], result["project"])
            self.assertNotIn("prune", command)
        self.assertIn("down", calls[-1])
        other, _ = self.exercise()
        self.assertNotEqual(result["project"], other["project"])

    def test_failures_and_timeouts_still_cleanup(self):
        for failure in ("build", "up", "migrator", "quality"):
            with self.subTest(failure=failure):
                result, calls = self.exercise(failure=failure)
                self.assertEqual(result["status"], "failed")
                self.assertEqual(result["cleanup"], "passed")
                self.assertIn("down", calls[-1])

    def test_log_failure_does_not_skip_cleanup(self):
        result, calls = self.exercise(logs_fail=True)
        self.assertEqual(result["log_collection"], "failed")
        self.assertIn("down", calls[-1])

    def test_cleanup_failure_fails_pipeline(self):
        result, _ = self.exercise(cleanup_fail=True)
        self.assertEqual(result["status"], "failed")
        self.assertEqual(result["cleanup"], "failed")

    def test_missing_validation_evidence_fails(self):
        result, _ = self.exercise(missing_summary=True)
        self.assertEqual(result["status"], "failed")

    def test_unsafe_config_fails_before_build(self):
        result, calls = self.exercise(unsafe_config=True)
        self.assertEqual(result["status"], "failed")
        self.assertFalse(any("build" in c for c in calls))
        self.assertEqual(result["cleanup"], "not-needed")
