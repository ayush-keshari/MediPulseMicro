# dbt Operations Contract

This document defines how the MediPulse data-quality project is operated in CI
and how an operator can safely re-run checks against a database.

## Lineage and ownership

`MedipulseMain` is the source of truth for operational domains: identity,
facilities, inventory, procurement, logistics, telemetry, notifications, and
replenishment. `MedipulseAudit` is the source of truth for audit events.

The source-to-domain mapping is declared in `models/schema.yml`. The first
executable lineage node is `models/staging/inventory_positions.sql`, which
materializes the `InventoryPositions` source as the canonical staging view
`stg_inventory_positions`. Each source and model documents its purpose, key
columns, nullability, uniqueness, and foreign key relationships. When a new
table or column is introduced, update the schema contract in the same change as
the migration and fixture data.

## Idempotency

- `dbt parse` is safe to repeat and does not connect to SQL Server.
- `dbt test` is read-only; it never inserts, updates, or deletes source data.
- CI starts from a clean Compose database, applies migrations once, and loads
  deterministic fixtures before running dbt.
- Do not use `dbt seed`, ad-hoc SQL writes, or destructive commands as part of
  the quality gate unless the command is explicitly part of environment setup.
- Re-running the CI job is safe because the Compose teardown removes volumes
  before the database is recreated.

## Backfill and recovery runbook

1. Identify the database and table affected by the change, then confirm the
   corresponding source and relationship declarations in `models/schema.yml`.
2. Restore or refresh the target environment using the normal migration and
   fixture process. Do not repair a failed quality check by editing production
   rows.
3. Run `dbt parse --no-partial-parse` first to validate project metadata.
4. Run the smallest relevant selector, for example:

   ```text
   dbt test --project-dir pipelines/dbt --profiles-dir pipelines/dbt \
     --select source:mediapulse_main.InventoryPositions --no-partial-parse
   ```

5. Run the complete gate before declaring recovery complete:

   ```text
   dbt test --project-dir pipelines/dbt --profiles-dir pipelines/dbt --no-partial-parse
   ```

6. Preserve `target/manifest.json`, `target/run_results.json`, and the CI logs
   as the evidence for the run. These artifacts identify the exact tests,
   relationships, and row counts evaluated.

## Change checklist

- Add or update the source/table description and column contract.
- Add relationship, uniqueness, not-null, accepted-value, or volume tests as
  appropriate.
- Add deterministic fixture rows when the contract needs a new scenario.
- Verify migration, dbt parse, dbt test, and integration checks together.
- Record any intentional exception in the pull request description.
