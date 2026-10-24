import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location(
    "quality_gate", Path(__file__).resolve().parents[1] / "quality_gate.py")
gate = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)


def manifest():
    return {"macros": {"macro.test": {"name": "test_non_negative",
                                     "package_name": "mediapulse_dq"}},
            "nodes": {"test.quantity": {"name": "quantity", "resource_type": "test",
                                        "test_metadata": {"name": "non_negative"}},
                      "model.inventory": {"resource_type": "model"}},
            "sources": {"source.inventory": {}}}


class GateTests(unittest.TestCase):
    def test_missing_macro_fails_even_when_parse_succeeds(self):
        data = manifest()
        data["macros"] = {}
        with self.assertRaisesRegex(ValueError, "test_non_negative"):
            gate.validate_manifest(data)

    def test_namespace_cannot_resolve_to_wrong_package(self):
        data = manifest()
        data["nodes"]["test.quantity"]["test_metadata"]["namespace"] = "other"
        with self.assertRaisesRegex(ValueError, "Missing generic"):
            gate.validate_manifest(data)

    def test_empty_suite_is_not_success(self):
        with self.assertRaisesRegex(ValueError, "No data tests"):
            gate.validate_manifest({})

    def exercise(self, failure=None, selector="", empty_results=False, omit_results=False,
                 parse_only=False):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = root / "target"
            calls = []

            def dbt(command, **kwargs):
                stage = command[1]
                calls.append(stage)
                if stage == failure:
                    raise subprocess.CalledProcessError(1, command)
                if stage == "--quiet":
                    return subprocess.CompletedProcess(command, 0, stdout="")
                if stage == "parse":
                    (target / "manifest.json").write_text(json.dumps(manifest()))
                elif not omit_results:
                    filename = "sources.json" if stage == "source" else "run_results.json"
                    status = "success" if stage == "run" else "pass"
                    data = {"results": [] if empty_results else [{"status": status}]}
                    (target / filename).write_text(json.dumps(data))
                return subprocess.CompletedProcess(command, 0)

            with patch.object(gate.subprocess, "run", side_effect=dbt):
                if failure or selector or empty_results or omit_results:
                    with self.assertRaises((subprocess.CalledProcessError, ValueError,
                                            FileNotFoundError)):
                        gate.run_gate(root, root, selector, parse_only=parse_only)
                else:
                    gate.run_gate(root, root, parse_only=parse_only)
            evidence = next((target / "quality-gate").iterdir())
            summary = json.loads((evidence / "summary.json").read_text())
            return calls, summary, {f.name for f in evidence.iterdir()}

    def test_success_preserves_each_stage_artifact(self):
        calls, summary, files = self.exercise()
        self.assertEqual(calls, ["parse", "run", "test", "source"])
        self.assertEqual(summary["status"], "passed")
        self.assertTrue({"parse.json", "build-models.json", "test.json",
                         "freshness.json", "summary.json"}.issubset(files))

    def test_failed_build_never_runs_tests(self):
        calls, summary, _ = self.exercise(failure="run")
        self.assertEqual(calls, ["parse", "run"])
        self.assertEqual(summary["status"], "failed")

    def test_failed_tests_never_run_freshness(self):
        calls, summary, _ = self.exercise(failure="test")
        self.assertEqual(calls, ["parse", "run", "test"])
        self.assertEqual(summary["status"], "failed")

    def test_empty_selector_fails_before_writes(self):
        calls, summary, _ = self.exercise(selector="misspelled_model")
        self.assertEqual(calls, ["parse", "--quiet"])
        self.assertIn("matches no models", summary["error"])

    def test_no_results_cannot_pass(self):
        _, summary, _ = self.exercise(empty_results=True)
        self.assertEqual(summary["status"], "failed")

    def test_missing_result_artifact_cannot_pass(self):
        _, summary, _ = self.exercise(omit_results=True)
        self.assertEqual(summary["status"], "failed")

    def test_offline_mode_compiles_without_running_sql(self):
        calls, summary, files = self.exercise(parse_only=True)
        self.assertEqual(calls, ["parse", "compile"])
        self.assertEqual(summary["status"], "passed")
        self.assertIn("compile.json", files)

    def test_compilation_failure_fails_offline_gate(self):
        _, summary, _ = self.exercise(failure="compile", parse_only=True)
        self.assertEqual(summary["status"], "failed")


if __name__ == "__main__":
    unittest.main()
