# Consumption analytics: lineage and rebuild contract

## Purpose and grain

`MedipulseMain.dbo.fct_daily_consumption` supports demand review by facility and
medical item. One row represents `(ConsumptionDate, FacilityId, ItemId)`.
`TotalQuantityConsumed` sums quantities as bigint; `ConsumptionEventCount`
counts events as bigint. Days without events have no row, not an invented zero.
The mart contains no patient, user, ward, or staff identifiers.

```text
LogisticsService -> MedipulseMain.dbo.ConsumptionRecord
                    -> stg_consumption_records (dbt: consumption_records)
                    -> fct_daily_consumption (dbt: daily_consumption)
Facility + Items -> source/model relationship checks
ConsumptionRecord -> independent grouped reconciliation -> daily_consumption
```

`ConsumedDate` is supplied by API clients and stored without timezone metadata.
The model groups by its stored calendar date, with no timezone conversion.
Mixed timezone inputs must be corrected upstream before using this for global
daily comparisons. This is not a UTC-normalization pipeline.

## Contracts and failure behavior

Source contracts cover all 19 declared tables across eight service domains; see
[database ownership](../../../docs/data-lineage.md). Inventory, receipts, and
transfer orders also have staging views. The consumption mart is the first
aggregate output; other domains are validated sources, not fabricated marts.

The consumption staging projection is compared to its source. Its keys, dates,
quantities, and facility/item references are tested. The mart has non-null,
non-negative, relationship, and composite-grain uniqueness checks. A separate
SQL test compares grouped source totals and event counts with the mart in both
directions, detecting missing, extra, or altered groups.

Seven SQL Server tests execute the actual model query against temporary events:
aggregation, midnight boundaries, group isolation, historical late arrivals,
corrections/deletions, 32-bit sum overflow, and empty input. The runner also
materializes the mart a second time, reruns its dbt tests, and compares every
output row including multiplicity. Unchanged-source replay and changed-source
query semantics are distinct checks; neither is described as CDC validation.

CI publishes logs, manifest lineage, stage results, and rebuild evidence. Any
failed required check fails the pipeline. dbt materialization happens before
validation; this is not an atomic production publish/promotion mechanism.

## Historical rebuild and recovery

The model is a table rebuilt from the entire retained source on every run.
Late events, quantity corrections, and deleted events therefore recompute their
historical groups. There is no watermark, incremental cursor, or date-window
filter that could silently exclude an older correction.

For the disposable repository fixtures, run from the repository root:

```text
python pipelines/dbt/run_pipeline.py
```

This creates fresh databases, runs migrations and fixtures, validates, and
removes its own containers and volumes. It is not a production data loader.

For an existing authorized analytics database, after approved upstream repairs
and with a configured profile, run the full contract:

```text
python pipelines/dbt/quality_gate.py --profiles-dir <profile-directory>
```

For a targeted diagnostic rebuild (not a substitute for the full release gate):

```text
dbt run --project-dir pipelines/dbt --profiles-dir <profile-directory> --select +daily_consumption --full-refresh
dbt test --project-dir pipelines/dbt --profiles-dir <profile-directory> --select +daily_consumption
```

Keep the source quiescent during rebuild/reconciliation: concurrent source
writes can otherwise make separately executed queries disagree. Serialize runs
against a shared target schema. After failure, retain artifacts, repair the
source or model, and rerun the full gate before consumers trust the output.
Never run repository fixture loaders against production to repair history.

Recovery can reconstruct only retained source events. Deleted source history
cannot be recovered without an external backup/event archive. Full rebuilds
trade simplicity for source scanning cost and are intended for current project
scale; incremental ingestion, CDC, coordinated snapshots, production scheduling,
and atomic publication remain future work.
