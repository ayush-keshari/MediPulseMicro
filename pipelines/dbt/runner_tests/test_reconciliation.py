"""Exercise the reconciliation query with adversarial rows without SQL Server.

SQLite supports the same EXCEPT/UNION semantics used here; SQL Server execution
is additionally covered by the database-backed dbt CI job.
"""
from pathlib import Path
import sqlite3
import unittest

from jinja2 import Environment


class ReconciliationTests(unittest.TestCase):
    def setUp(self):
        self.db = sqlite3.connect(":memory:")
        self.addCleanup(self.db.close)
        self.db.execute("create table source_rows (id integer, quantity integer)")
        self.db.execute("create table staging_rows (id integer, quantity integer)")
        self.db.executemany("insert into source_rows values (?, ?)", [(1, 10), (2, 20)])
        self.db.execute("insert into staging_rows select * from source_rows")
        macro = (Path(__file__).resolve().parents[1] / "macros" / "matches_source.sql").read_text()
        # Render the actual SQL body, excluding dbt's custom test block tags.
        body = macro.split("%}", 1)[1].rsplit("{% endtest %}", 1)[0]
        self.sql = Environment().from_string(body).render(
            model="staging_rows", source_relation="source_rows", columns=["id", "quantity"])

    def test_identical_rows_pass(self):
        self.assertEqual(self.db.execute(self.sql).fetchall(), [])

    def test_missing_row_fails(self):
        self.db.execute("delete from staging_rows where id = 1")
        self.assertEqual(self.db.execute(self.sql).fetchall(), [(1, 10)])

    def test_extra_row_fails(self):
        self.db.execute("insert into staging_rows values (3, 30)")
        self.assertEqual(self.db.execute(self.sql).fetchall(), [(3, 30)])

    def test_changed_quantity_reports_both_versions(self):
        self.db.execute("update staging_rows set quantity = 99 where id = 1")
        self.assertEqual(set(self.db.execute(self.sql)), {(1, 10), (1, 99)})

    def test_null_mismatch_fails(self):
        self.db.execute("update staging_rows set quantity = null where id = 1")
        self.assertEqual(set(self.db.execute(self.sql)), {(1, 10), (1, None)})
