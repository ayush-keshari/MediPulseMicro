"""Exercise the migrated trigger on SQL Server; every test rolls back its writes."""
from datetime import datetime
import os
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_fixture_replay import connect_database


@unittest.skipUnless(os.environ.get("MEDIPULSE_SQL_TESTS") == "1", "Requires disposable SQL Server")
class ConsumptionJournalTests(unittest.TestCase):
    def setUp(self):
        self.connection = connect_database()
        self.addCleanup(self.connection.close)
        self.addCleanup(self.connection.rollback)
        self.cursor = self.connection.cursor()
        self.addCleanup(self.cursor.close)
        self.cursor.execute("USE MedipulseMain; SET NOCOUNT ON;")

    def insert(self):
        return self.cursor.execute("""
            INSERT dbo.ConsumptionRecord
                (FacilityId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy)
            VALUES (1, 1, 'Journal test', 7, '2026-01-01', 'private-staff');
            SELECT CAST(SCOPE_IDENTITY() AS int);
        """).fetchone()[0]

    def events(self, identity):
        return self.cursor.execute("""
            SELECT Operation, BeforeConsumedDate, BeforeQuantity, AfterConsumedDate, AfterQuantity
            FROM dbo.ConsumptionChange WHERE ConsumptionId=? ORDER BY ChangeId
        """, identity).fetchall()

    def test_insert_records_analytics_values(self):
        row = self.events(self.insert())[0]
        self.assertEqual(tuple(row), ('I', None, None, datetime(2026, 1, 1), 7))

    def test_update_retains_both_historical_dates_and_quantities(self):
        identity = self.insert()
        self.cursor.execute("UPDATE dbo.ConsumptionRecord SET ConsumedDate='2026-01-02', "
                            "QuantityConsumed=9 WHERE ConsumptionId=?", identity)
        self.assertEqual(tuple(self.events(identity)[1]),
                         ('U', datetime(2026, 1, 1), 7, datetime(2026, 1, 2), 9))

    def test_delete_keeps_tombstone_without_source_foreign_key(self):
        identity = self.insert()
        self.cursor.execute("DELETE dbo.ConsumptionRecord WHERE ConsumptionId=?", identity)
        self.assertEqual(tuple(self.events(identity)[1]), ('D', datetime(2026, 1, 1), 7, None, None))
        self.assertEqual(self.cursor.execute("SELECT COUNT(*) FROM dbo.ConsumptionRecord WHERE ConsumptionId=?",
                                             identity).fetchone()[0], 0)

    def test_multirow_updates_capture_every_changed_row(self):
        first, second = self.insert(), self.insert()
        self.cursor.execute("UPDATE dbo.ConsumptionRecord SET QuantityConsumed=11 WHERE ConsumptionId IN (?, ?)",
                            first, second)
        for identity in (first, second):
            self.assertEqual([row[0] for row in self.events(identity)], ['I', 'U'])
            self.assertEqual(self.events(identity)[1][4], 11)

    def test_noop_and_staff_changes_do_not_emit_analytics_events(self):
        identity = self.insert()
        self.cursor.execute("UPDATE dbo.ConsumptionRecord SET QuantityConsumed=QuantityConsumed, "
                            "ConsumedBy='another-private-staff' WHERE ConsumptionId=?", identity)
        self.assertEqual(len(self.events(identity)), 1)

    def test_source_rollback_also_removes_journal_event(self):
        identity = self.insert()
        self.assertEqual(len(self.events(identity)), 1)
        self.connection.rollback()
        self.cursor.execute("USE MedipulseMain;")
        self.assertEqual(len(self.events(identity)), 0)
        self.assertEqual(self.cursor.execute("SELECT COUNT(*) FROM dbo.ConsumptionRecord WHERE ConsumptionId=?",
                                             identity).fetchone()[0], 0)

    def test_journal_failure_prevents_source_insert(self):
        import pyodbc
        before = self.cursor.execute("SELECT COUNT(*) FROM dbo.ConsumptionRecord").fetchone()[0]
        self.cursor.execute("ALTER TABLE dbo.ConsumptionChange WITH NOCHECK ADD CONSTRAINT "
                            "CK_JournalTest_RejectInsert CHECK (Operation <> 'I')")
        with self.assertRaises(pyodbc.Error):
            self.insert()
        self.connection.rollback()
        self.cursor.execute("USE MedipulseMain;")
        self.assertEqual(self.cursor.execute("SELECT COUNT(*) FROM dbo.ConsumptionRecord").fetchone()[0], before)

    def test_journal_contains_no_staff_or_ward_columns(self):
        columns = {row[0] for row in self.cursor.execute(
            "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.ConsumptionChange')").fetchall()}
        self.assertTrue({'ChangeId', 'ConsumptionId', 'BeforeFacilityId', 'AfterFacilityId'} <= columns)
        self.assertFalse({'ConsumedBy', 'WardId', 'ItemName'} & columns)
