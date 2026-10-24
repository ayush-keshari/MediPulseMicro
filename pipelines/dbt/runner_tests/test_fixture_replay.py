import importlib.util
from collections import Counter
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

SPEC = importlib.util.spec_from_file_location(
    "replay", Path(__file__).resolve().parents[1] / "verify_fixture_replay.py")
replay = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(replay)


class ReplayTests(unittest.TestCase):
    def test_reordered_rows_pass(self):
        self.assertEqual(replay.changes({"t": Counter([(1, 2), (2, 3)])},
                                        {"t": Counter([(2, 3), (1, 2)])}), [])

    def test_duplicate_or_changed_values_fail(self):
        before = {"t": Counter([(1, 2)])}
        for rows in [[(1, 2), (1, 2)], [(1, 3)], []]:
            self.assertEqual(replay.changes(before, {"t": Counter(rows)}), ["t"])

    def test_new_empty_table_is_detected(self):
        self.assertEqual(replay.changes({}, {"t": Counter()}), ["t"])

    def test_rollback_on_success_and_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            for fail in [False, True]:
                connection = Mock()
                if fail:
                    connection.cursor.return_value.execute.side_effect = RuntimeError("failure")
                with patch.object(replay, "FIXTURES", ()), patch.object(
                        replay, "snapshot", return_value={"t": Counter([(1, 2)])}):
                    if fail:
                        with self.assertRaises(RuntimeError):
                            replay.verify(connection, Path(directory))
                    else:
                        self.assertEqual(replay.verify(connection, Path(directory))["status"], "passed")
                connection.rollback.assert_called_once()
                connection.commit.assert_not_called()
