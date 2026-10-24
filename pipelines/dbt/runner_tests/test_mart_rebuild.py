import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import verify_mart_rebuild as rebuild


class MartRebuildTests(unittest.TestCase):
    def exercise(self, changed=False, empty_results=False):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = root / "target"
            target.mkdir()
            model = {"resource_type": "model", "name": "daily_consumption",
                     "database": "db", "schema": "dbo", "alias": "fct_daily_consumption"}
            (target / "manifest.json").write_text(json.dumps({"nodes": {"mart": model}}))
            before, after = Mock(), Mock()
            rows = [("2026-01-01", 1, 1, 10, 2)]
            before.cursor.return_value.execute.return_value.fetchall.return_value = rows
            after.cursor.return_value.execute.return_value.fetchall.return_value = rows * (2 if changed else 1)

            def run(*args, **kwargs):
                (target / "run_results.json").write_text(json.dumps({
                    "results": [] if empty_results else [{"status": "success"}]}))

            with patch.object(rebuild, "connect_database", side_effect=[before, after]), patch.object(
                    rebuild.subprocess, "run", side_effect=run):
                if changed or empty_results:
                    with self.assertRaises(ValueError):
                        rebuild.verify_rebuild(root, root, root)
                else:
                    rebuild.verify_rebuild(root, root, root)
            self.assertEqual(json.loads((root / "mart-rebuild.json").read_text())["status"],
                             "failed" if changed or empty_results else "passed")
            before.close.assert_called_once()

    def test_repeat_build_preserves_rows(self):
        self.exercise()

    def test_duplicate_rows_fail(self):
        self.exercise(changed=True)

    def test_empty_dbt_results_fail(self):
        self.exercise(empty_results=True)
