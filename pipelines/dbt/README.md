# MediPulse Data Quality Pipeline

This dbt project is the authoritative data-quality gate for the two SQL Server
databases used by MediPulse.

## CI validation levels

The pull-request pipeline runs `python pipelines/dbt/quality_gate.py --parse-only`
as a database-independent validation. It parses the project and verifies that
every generic test resolves to a discovered macro, then compiles all nodes with
database introspection and relation-cache population disabled. Parsing alone does not catch
all undefined test macros. The runner regression tests also execute in this job.

The authoritative `dbt test` gate runs after SQL Server migrations and
repository mock data have been loaded. CI, scheduled runs, and local runs share
`quality_gate.py`: parse and validate contracts, materialize views, test, then
check inventory freshness. Every failed stage stops subsequent stages.

## Lineage

- `MedipulseMain` is the operational source for identity, facilities, inventory,
  procurement, logistics, telemetry, notifications, and replenishment data.
- `MedipulseAudit` is the operational source for the audit trail.
- `models/schema.yml` declares the source tables and their quality contracts.
- dbt generic tests enforce uniqueness, required fields, accepted values,
  foreign-key relationships, non-negative movement quantities, and minimum
  fixture volumes. Inventory staging also rejects duplicate item/lot/location
  business keys.
- Bidirectional reconciliation checks compare every selected staging column
  with its source, detecting missing, unexpected, and changed rows. Existing
  uniqueness tests additionally detect duplicate rows (EXCEPT uses set semantics).

## Local quality gate

```text
python -m pip install -r pipelines/dbt/requirements.txt
python pipelines/dbt/quality_gate.py --profiles-dir /path/to/profile-directory
```

The profile is intentionally external to source control. Copy
`profiles.yml.example` to a local `profiles.yml` and provide the SQL Server
connection details for the target environment.

The default profile directory is `DBT_PROFILES_DIR` or `~/.dbt`. For a targeted
recovery check, add `--select inventory_positions` (the model name, not its SQL
alias `stg_inventory_positions`). An empty selection fails before materialization.
Inventory freshness is still checked unless `--skip-freshness` is explicit.

Each invocation writes a separate `target/quality-gate/<run-id>/` containing
the manifest, model results, test results, freshness results, and a summary.
The summary records failure even if execution stops midway. CI uploads this
directory with the other dbt artifacts. Missing or empty result artifacts fail
the gate rather than reusing a previous run's success.

Offline regression tests (after installing requirements):

```text
python -m unittest discover -s pipelines/dbt/runner_tests -v
```

These tests cover stage failures, missing macros, namespace resolution, empty
selections, missing evidence, and reconciliation mutations. They complement
SQL Server integration tests; they do not replace them. This runner rebuilds
derived views and validates operational data. It does not ingest historical
records or implement a transactional source-data backfill.
