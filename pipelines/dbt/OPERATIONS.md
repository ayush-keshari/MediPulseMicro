# dbt Operations Contract

This document defines how the MediPulse data-quality project is operated in CI
and how an operator can safely re-run checks against a database.

## Lineage and ownership

`MedipulseMain` is the source of truth for operational domains: identity,
facilities, inventory, procurement, logistics, telemetry, notifications, and
replenishment. `MedipulseAudit` is the source of truth for audit events.

The source-to-domain mapping is declared in `models/schema.yml`. The executable
lineage layer currently includes `InventoryPositions`, `Receipt`, and
`TransferOrder`, materialized as the canonical staging views
`stg_inventory_positions`, `stg_receipts`, and `stg_transfer_orders`. Each
source and model documents its purpose, grain, ownership, key columns,
nullability, uniqueness, foreign key relationships, and non-negative quantity
rules. Inventory position staging also enforces the idempotency invariant that
one item/lot/location combination has at most one position row. When a new table or
column is introduced, update the schema contract in the same change as the
migration and fixture data.

Inventory freshness is measured from `InventoryPositions.IngestedAt`, a UTC
load timestamp populated by the database default and checked by CI. It is not
derived from `ExpiryDate` or other business-event dates, so historical fixtures
and legitimate backfills remain valid.

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
   python pipelines/dbt/quality_gate.py --profiles-dir /path/to/profile-directory
   ```

6. Preserve `target/manifest.json`, `target/run_results.json`, and the CI logs
   as the evidence for the run. These artifacts identify the exact tests,
   relationships, and row counts evaluated.

For a repeatable local recovery check, use the repository runner after placing
the target connection in `~/.dbt/profiles.yml` (or setting `DBT_PROFILES_DIR`):

```powershell
./pipelines/dbt/Invoke-QualityGate.ps1
```

To validate only one source/model after a targeted backfill, pass a selector:

```powershell
./pipelines/dbt/Invoke-QualityGate.ps1 -Selector inventory_positions
```

The runner parses metadata, materializes the selected staging models, runs the
selected tests, and checks inventory freshness. It never writes to source
tables; repeatable writes belong to the service migration or controlled
backfill process that produced the data.

### Recovery evidence and limits

The shared Python runner retains independent parse/build/test/freshness artifacts
under `target/quality-gate/<run-id>/`. A failed stage stops the run and writes a
failed summary. Review that summary and the per-stage results before declaring
recovery complete. Re-running refreshes derived views; it does not reload source
rows or change their ingestion timestamps. No automatic retry is performed:
diagnose the failed stage before initiating another run.

Reconciliation tests for inventory, receipts, and transfer orders compare source
and staging projections in both directions. Missing or changed records fail;
key uniqueness tests cover duplication separately. Run against a stable source
snapshot or quiescent maintenance window to avoid concurrent-write differences.
Freshness measures recent insertion into inventory positions, not every stock
update. A quiet inventory can legitimately age past the configured threshold;
operators must assess this instead of rewriting timestamps to silence the check.

## Change checklist

### Complete fixture replay verification

After migrations and the first fixture load, CI and the scheduled workflow run
`python pipelines/dbt/verify_fixture_replay.py`. It uses the existing ODBC driver
and `SA_PASSWORD` environment variable against local disposable SQL Server.
The check captures every application-table row in both databases (excluding EF
migration history), replays the same four fixture scripts, then compares row
values and multiplicities. It detects duplicates, deletions, modified quantities,
keys, and timestamps. It rolls back the replay transaction on both success and
failure and reports only table names/counts, never row contents.

`target/fixture-replay.json` records the result and is uploaded with dbt artifacts.
Run only against quiescent disposable databases: serializable reads can block
other writers. A pass proves replay safety for a complete initial fixture load;
it does not prove repair of a partially loaded table. Existing first-row guards
can skip missing rows, so partial-load recovery remains a separate milestone.

### Contract changes

- Add or update the source/table description and column contract.
- Add relationship, uniqueness, not-null, accepted-value, or volume tests as
  appropriate.
- Add deterministic fixture rows when the contract needs a new scenario.
- Verify migration, dbt parse, dbt test, and integration checks together.
- Record any intentional exception in the pull request description.
