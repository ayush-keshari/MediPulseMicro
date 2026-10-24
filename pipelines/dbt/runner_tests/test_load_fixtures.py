import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import load_fixtures as loader


class FixtureLoadTests(unittest.TestCase):
    def test_all_canonical_files_share_one_transaction(self):
        sql = loader.fixture_batch()
        self.assertEqual(sql.count("BEGIN TRANSACTION;"), 1)
        self.assertEqual(sql.count("COMMIT TRANSACTION;"), 1)
        self.assertEqual(sql.count("EXEC sys.sp_executesql"), 4)
        self.assertIn("USE [MedipulseAudit]", sql)
        self.assertIn("IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;", sql)
        self.assertIn("THROW;", sql)
        self.assertLess(sql.index("USE [MedipulseAudit]"), sql.index("COMMIT TRANSACTION;"))

    def test_literals_are_escaped_and_client_commands_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fixture = root / "fixture.sql"
            with patch.object(loader, "FIXTURES", (("db", "fixture.sql"),)):
                fixture.write_text("SELECT N'O''Brien';", encoding="utf-8")
                self.assertIn("N''O''''Brien''", loader.fixture_batch(root))
                for text in ["GO\n", ":r other.sql", "!! echo unsafe"]:
                    fixture.write_text(text, encoding="utf-8")
                    with self.assertRaises(ValueError):
                        loader.fixture_batch(root)

    def test_password_not_in_arguments_and_failures_propagate(self):
        with patch.dict(os.environ, {"SA_PASSWORD": "test-only"}), patch.object(
                loader.subprocess, "run") as run:
            loader.load(project="quality-test")
            args = run.call_args.args[0]
            self.assertNotIn("test-only", args)
            self.assertIn("quality-test", args)
            self.assertEqual(run.call_args.kwargs["env"]["SQLCMDPASSWORD"], "test-only")
            self.assertTrue(run.call_args.kwargs["check"])
            run.side_effect = subprocess.CalledProcessError(1, "sqlcmd")
            with self.assertRaises(subprocess.CalledProcessError):
                loader.load()

    def test_missing_password_fails_before_docker(self):
        with patch.dict(os.environ, {}, clear=True), patch.object(loader.subprocess, "run") as run:
            with self.assertRaises(ValueError):
                loader.load()
            run.assert_not_called()
