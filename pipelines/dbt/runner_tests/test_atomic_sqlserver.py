"""Opt-in SQL Server transaction tests; never modify application tables."""
import os
from pathlib import Path
import sys
import tempfile
import unittest
import uuid

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from load_fixtures import fixture_batch
from verify_fixture_replay import DATABASES, quote


@unittest.skipUnless(os.environ.get("MEDIPULSE_SQL_TESTS") == "1",
                     "Requires disposable migrated SQL Server and explicit opt-in")
class AtomicSqlServerTests(unittest.TestCase):
    def setUp(self):
        import pyodbc

        self.sql_error = pyodbc.Error
        password = os.environ["SA_PASSWORD"].replace("}", "}}")
        self.connection = pyodbc.connect(
            "DRIVER={ODBC Driver 18 for SQL Server};SERVER=localhost,1433;"
            f"UID=sa;PWD={{{password}}};Encrypt=yes;TrustServerCertificate=yes;",
            autocommit=True, timeout=30)
        self.addCleanup(self.connection.close)
        self.cursor = self.connection.cursor()
        self.addCleanup(self.cursor.close)
        self.table = "__FixtureAtomicity_" + uuid.uuid4().hex
        self.relations = [f"{quote(db)}.dbo.{quote(self.table)}" for db in DATABASES]
        self.addCleanup(self.remove_probes)
        for relation in self.relations:
            self.cursor.execute(f"CREATE TABLE {relation} (Id int NOT NULL PRIMARY KEY)")

    def remove_probes(self):
        self.cursor.execute("IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;")
        for relation in self.relations:
            self.cursor.execute(f"DROP TABLE IF EXISTS {relation}")

    def execute_batch(self, failure=""):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fixtures = []
            for index, database in enumerate(DATABASES):
                name = f"{index}.sql"
                sql = f"INSERT INTO dbo.{quote(self.table)} (Id) VALUES (1);\n"
                if index == len(DATABASES) - 1:
                    sql += failure
                (root / name).write_text(sql, encoding="utf-8")
                fixtures.append((database, name))
            self.cursor.execute(fixture_batch(root, fixtures))
            while self.cursor.nextset():
                pass

    def assert_counts(self, expected):
        for relation in self.relations:
            self.assertEqual(self.cursor.execute(f"SELECT COUNT(*) FROM {relation}").fetchone()[0],
                             expected)
        self.assertEqual(self.cursor.execute("SELECT @@TRANCOUNT").fetchone()[0], 0)

    def test_success_commits_both_databases(self):
        self.execute_batch()
        self.assert_counts(1)

    def test_late_failure_rolls_back_both_databases(self):
        with self.assertRaises(self.sql_error):
            self.execute_batch("THROW 51000, 'Intentional fixture rollback test', 1;")
        self.assert_counts(0)
        self.execute_batch()
        self.assert_counts(1)

    def test_nested_compilation_failure_rolls_back_earlier_file(self):
        with self.assertRaises(self.sql_error):
            self.execute_batch(f"SELECT * FROM dbo.{quote(self.table + '_missing')};")
        self.assert_counts(0)
