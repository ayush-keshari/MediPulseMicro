"""Exercise the incremental consumer against the disposable migrated SQL Server."""
from datetime import timedelta
import os
from pathlib import Path
import sys
import unittest
from uuid import uuid4

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from incremental_consumption import ConsumerBusy, apply_batch, ensure_tables, relations, run
from verify_fixture_replay import connect_database, quote


@unittest.skipUnless(os.environ.get("MEDIPULSE_SQL_TESTS") == "1", "Requires disposable SQL Server")
class IncrementalConsumptionTests(unittest.TestCase):
    def setUp(self):
        self.schema = "qa_" + uuid4().hex[:12]
        self.connection = connect_database()
        self.addCleanup(self.connection.close)
        cursor = self.connection.cursor()
        try:
            cursor.execute("USE MedipulseMain")
            cursor.execute(f"CREATE SCHEMA {quote(self.schema)}")
            self.connection.commit()
        finally:
            cursor.close()
        self.addCleanup(self.drop_schema)
        ensure_tables(self.connection, self.schema)
        self.target, self.ack = relations(self.schema)

    def drop_schema(self):
        cursor = self.connection.cursor()
        try:
            self.connection.rollback()
            cursor.execute("USE MedipulseMain")
            cursor.execute(f"DROP TABLE IF EXISTS {self.ack}")
            cursor.execute(f"DROP TABLE IF EXISTS {self.target}")
            cursor.execute(f"DROP SCHEMA {quote(self.schema)}")
            self.connection.commit()
        finally:
            cursor.close()

    def counts(self):
        cursor = self.connection.cursor()
        try:
            cursor.execute("USE MedipulseMain")
            return cursor.execute(f"SELECT COUNT(*) FROM {self.ack}").fetchone()[0], \
                cursor.execute(f"SELECT COUNT(*) FROM {self.target}").fetchone()[0]
        finally:
            cursor.close()

    def assert_matches_source(self):
        cursor = self.connection.cursor()
        try:
            cursor.execute("USE MedipulseMain")
            expected = cursor.execute("""
                SELECT CAST(ConsumedDate AS date), FacilityId, ItemId,
                    SUM(CAST(QuantityConsumed AS bigint)), COUNT_BIG(*)
                FROM dbo.ConsumptionRecord
                GROUP BY CAST(ConsumedDate AS date), FacilityId, ItemId
            """).fetchall()
            actual = cursor.execute(f"""
                SELECT ConsumptionDate, FacilityId, ItemId,
                    TotalQuantityConsumed, ConsumptionEventCount FROM {self.target}
            """).fetchall()
            self.assertEqual({tuple(row) for row in expected}, {tuple(row) for row in actual})
        finally:
            cursor.close()

    def test_bounded_backfill_and_repeat_run(self):
        first = run(self.connection, batch_size=2, max_batches=1, schema=self.schema)
        self.assertEqual(first["events"], 2)
        self.assertFalse(first["drained"])
        rest = run(self.connection, batch_size=2, max_batches=100, schema=self.schema)
        self.assertTrue(rest["drained"])
        self.assert_matches_source()
        self.assertEqual(run(self.connection, batch_size=2, max_batches=1, schema=self.schema)["events"], 0)

    def test_failed_batch_keeps_previous_output_and_retries(self):
        with self.assertRaisesRegex(RuntimeError, "Injected failure"):
            apply_batch(self.connection, batch_size=2, schema=self.schema, fail_before_ack=True)
        self.assertEqual(self.counts(), (0, 0))
        self.assertTrue(run(self.connection, batch_size=2, max_batches=100, schema=self.schema)["drained"])
        self.assert_matches_source()

    def test_date_correction_repairs_old_and_new_groups(self):
        self.assertTrue(run(self.connection, batch_size=100, max_batches=10, schema=self.schema)["drained"])
        cursor = self.connection.cursor()
        try:
            cursor.execute("USE MedipulseMain")
            identity, old_date, old_quantity = cursor.execute(
                "SELECT TOP 1 ConsumptionId, ConsumedDate, QuantityConsumed FROM dbo.ConsumptionRecord "
                "ORDER BY ConsumptionId").fetchone()
            last_id = cursor.execute("SELECT MAX(ChangeId) FROM dbo.ConsumptionChange").fetchone()[0]
            try:
                cursor.execute("UPDATE dbo.ConsumptionRecord SET ConsumedDate=?, QuantityConsumed=? "
                               "WHERE ConsumptionId=?", old_date + timedelta(days=1), old_quantity + 3, identity)
                self.connection.commit()
                self.assertTrue(run(self.connection, batch_size=2, max_batches=10, schema=self.schema)["drained"])
                self.assert_matches_source()
            finally:
                self.connection.rollback()
                cursor.execute("USE MedipulseMain")
                cursor.execute("UPDATE dbo.ConsumptionRecord SET ConsumedDate=?, QuantityConsumed=? "
                               "WHERE ConsumptionId=?", old_date, old_quantity, identity)
                cursor.execute("DELETE dbo.ConsumptionChange WHERE ConsumptionId=? AND ChangeId>?",
                               identity, last_id)
                self.connection.commit()
        finally:
            cursor.close()

    def test_competing_processor_cannot_enter_same_target(self):
        holder = connect_database()
        self.addCleanup(holder.close)
        cursor = holder.cursor()
        try:
            cursor.execute("USE MedipulseMain; SET NOCOUNT ON;")
            result = cursor.execute("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=?, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=0;
                SELECT @result;
            """, f"MediPulse:consumption:{self.schema}").fetchone()[0]
            self.assertGreaterEqual(result, 0)
            with self.assertRaises(ConsumerBusy):
                apply_batch(self.connection, batch_size=2, schema=self.schema)
            self.assertEqual(self.counts(), (0, 0))
        finally:
            holder.rollback()
            cursor.close()
        self.assertEqual(apply_batch(self.connection, batch_size=2, schema=self.schema)["events"], 2)
