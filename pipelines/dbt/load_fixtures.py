"""Atomically load the canonical fixtures into disposable Compose databases."""
import argparse
import os
from pathlib import Path
import re
import subprocess

from verify_fixture_replay import FIXTURES, ROOT, quote


def fixture_batch(root=ROOT):
    statements = ["SET XACT_ABORT ON;", "BEGIN TRY", "BEGIN TRANSACTION;"]
    for database, path in FIXTURES:
        sql = (root / path).read_text(encoding="utf-8-sig")
        if re.search(r"^\s*(GO\s*(?:--.*)?$|:|!!)", sql, re.MULTILINE | re.IGNORECASE):
            raise ValueError(f"Batch separators and sqlcmd commands are unsupported: {path}")
        # Compile each file in a nested scope so TRY/CATCH also catches SQL errors.
        nested = f"USE {quote(database)};\n{sql}".replace("'", "''")
        statements.append(f"EXEC sys.sp_executesql N'{nested}';")
    statements.extend(["COMMIT TRANSACTION;", "END TRY", "BEGIN CATCH",
                       "IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;",
                       "THROW;", "END CATCH;"])
    return "\n".join(statements)


def load(root=ROOT, project=None):
    password = os.environ.get("SA_PASSWORD")
    if not password:
        raise ValueError("Set SA_PASSWORD for the disposable quality database")
    batch = fixture_batch(root)
    command = ["docker", "compose", "-f", str(root / "docker-compose.ci.yml")]
    if project:
        command.extend(["--project-name", project])
    command.extend(["exec", "-T", "-e", "SQLCMDPASSWORD", "sqlserver",
                    "/opt/mssql-tools18/bin/sqlcmd", "-C", "-b", "-x",
                    "-S", "localhost", "-U", "sa", "-i", "/dev/stdin"])
    subprocess.run(command, input=batch, text=True, encoding="utf-8", check=True,
                   cwd=root, env={**os.environ, "SQLCMDPASSWORD": password})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-name", help="Compose project owning the disposable SQL Server")
    args = parser.parse_args()
    load(project=args.project_name)
