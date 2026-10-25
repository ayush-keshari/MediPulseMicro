"""Validate recovery boundaries without requiring a database."""
from datetime import date, datetime, timedelta
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from reconcile_incremental_consumption import validate_window


class ConsumptionRecoveryTests(unittest.TestCase):
    def test_single_day_and_maximum_window(self):
        start = date(2026, 1, 1)
        self.assertEqual(validate_window(start, start), date(2026, 1, 2))
        self.assertEqual(validate_window(start, start + timedelta(days=30)), date(2026, 2, 1))

    def test_rejects_invalid_or_unbounded_windows(self):
        start = date(2026, 1, 1)
        for end in (start - timedelta(days=1), start + timedelta(days=31),
                    datetime(2026, 1, 1)):
            with self.subTest(end=end), self.assertRaises(ValueError):
                validate_window(start, end)
