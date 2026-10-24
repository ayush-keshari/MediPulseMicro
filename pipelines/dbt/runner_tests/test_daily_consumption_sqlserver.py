"""Run the real mart SQL against adversarial events in a SQL Server temp table."""
from datetime import date
import os
from pathlib import Path
import sys
import unittest

from jinja2 import Environment, StrictUndefined

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_fixture_replay import connect_database


@unittest.skipUnless(os.environ.get("MEDIPULSE_SQL_TESTS") == "1",
                     "Requires disposable SQL Server and explicit opt-in")
class DailyConsumptionTests(unittest.TestCase):
    def setUp(self):
        self.connection = connect_database(autocommit=True)
        self.addCleanup(self.connection.close)
        self.cursor = self.connection.cursor()
        self.addCleanup(self.cursor.close)
        self.cursor.execute("CREATE TABLE #events (ConsumptionId int PRIMARY KEY, FacilityId int, "
                            "ItemId int, QuantityConsumed int, ConsumedDate datetime2)")
        sql = (Path(__file__).resolve().parents[1] / "models/marts/daily_consumption.sql").read_text()
        self.sql = Environment(undefined=StrictUndefined).from_string(sql).render(
            config=lambda **kwargs: "", ref=lambda name: "#events")

    def insert(self, identity, quantity, timestamp="2026-01-01T12:00:00", facility=1, item=1):
        self.cursor.execute("INSERT INTO #events VALUES (?, ?, ?, ?, ?)",
                            identity, facility, item, quantity, timestamp)

    def rows(self):
        return sorted(tuple(row) for row in self.cursor.execute(self.sql).fetchall())

    def test_multiple_events_aggregate_without_losing_counts(self):
        self.insert(1, 7)
        self.insert(2, 13)
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 20, 2)])

    def test_stored_midnight_has_separate_days(self):
        self.insert(1, 7, "2026-01-01T23:59:59.9999999")
        self.insert(2, 13, "2026-01-02T00:00:00")
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 7, 1),
                                       (date(2026, 1, 2), 1, 1, 13, 1)])

    def test_facilities_and_items_are_not_combined(self):
        self.insert(1, 7)
        self.insert(2, 13, facility=2)
        self.insert(3, 5, item=2)
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 7, 1),
                                       (date(2026, 1, 1), 1, 2, 5, 1),
                                       (date(2026, 1, 1), 2, 1, 13, 1)])

    def test_late_arrival_updates_its_historical_day(self):
        self.insert(1, 7, "2026-01-02T00:00:00")
        before = self.rows()
        self.insert(2, 13)
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 13, 1), *before])

    def test_correction_and_deletion_recompute_totals(self):
        self.insert(1, 7)
        self.insert(2, 13)
        self.cursor.execute("UPDATE #events SET QuantityConsumed = 2 WHERE ConsumptionId = 1")
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 15, 2)])
        self.cursor.execute("DELETE FROM #events WHERE ConsumptionId = 2")
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 2, 1)])
        self.cursor.execute("DELETE FROM #events")
        self.assertEqual(self.rows(), [])

    def test_quantity_sum_does_not_overflow_int32(self):
        self.insert(1, 2147483647)
        self.insert(2, 2147483647)
        self.assertEqual(self.rows(), [(date(2026, 1, 1), 1, 1, 4294967294, 2)])

    def test_empty_input_has_no_fabricated_zero_rows(self):
        self.assertEqual(self.rows(), [])
