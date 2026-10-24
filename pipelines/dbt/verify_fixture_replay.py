"""Replay CI fixtures in a transaction and verify all application rows are unchanged.

Use only against disposable, quiescent test databases. The replay is rolled back
on success as well as failure. Row values (including credentials) are never logged.
"""
import json
import os
from collections import Counter
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[2]
DATABASES = ("MedipulseMain", "MedipulseAudit")
FIXTURES = ((DATABASES[0], "MockData_InsertOnly.sql"),
            (DATABASES[0], "Scripts/mockdata/AuthService.sql"),
            (DATABASES[0], "Scripts/mockdata/NotificationService.sql"),
            (DATABASES[1], "Scripts/mockdata/AuditService.sql"))


def quote(name):
    return "[" + name.replace("]", "]]") + "]"


def snapshot(cursor):
    result = {}
    for database in DATABASES:
        tables = cursor.execute(
            f"SELECT s.name, t.name FROM {quote(database)}.sys.tables t "
            f"JOIN {quote(database)}.sys.schemas s ON s.schema_id=t.schema_id "
            "WHERE t.is_ms_shipped=0 AND t.name NOT LIKE '__EFMigrationsHistory%'").fetchall()
        for schema, table in tables:
            relation = ".".join(map(quote, (database, schema, table)))
            result[relation] = Counter(tuple(row) for row in
                                       cursor.execute(f"SELECT * FROM {relation}").fetchall())
    if not result or not any(result.values()):
        raise ValueError("Replay requires migrated, populated fixture databases")
    return result


def changes(before, after):
    return sorted(name for name in before.keys() | after.keys()
                  if before.get(name) != after.get(name))


def verify(connection, root=ROOT):
    cursor = connection.cursor()
    try:
        cursor.execute("SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;")
        before = snapshot(cursor)
        for database, path in FIXTURES:
            sql = (root / path).read_text(encoding="utf-8-sig")
            if re.search(r"^\s*GO\s*(?:--.*)?$", sql, re.MULTILINE | re.IGNORECASE):
                raise ValueError(f"Unsupported GO batch separator in fixture: {path}")
            cursor.execute(f"USE {quote(database)}")
            cursor.execute(sql)
            while cursor.nextset():
                pass
        after = snapshot(cursor)
        changed = changes(before, after)
        summary = {"status": "failed" if changed else "passed",
                   "tables_checked": len(before),
                   "rows_checked": sum(sum(rows.values()) for rows in before.values()),
                   "changed_tables": changed,
                   "scope": "complete fixture replay; not partial-load recovery"}
        return summary
    finally:
        connection.rollback()
        cursor.close()


if __name__ == "__main__":
    import pyodbc

    password = os.environ["SA_PASSWORD"].replace("}", "}}")
    connection = pyodbc.connect(
        "DRIVER={ODBC Driver 18 for SQL Server};SERVER=localhost,1433;"
        f"UID=sa;PWD={{{password}}};Encrypt=yes;TrustServerCertificate=yes;",
        autocommit=False, timeout=30)
    try:
        report = verify(connection)
    finally:
        connection.close()
    output = Path(__file__).resolve().parent / "target" / "fixture-replay.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    if report["status"] != "passed":
        raise SystemExit(1)
