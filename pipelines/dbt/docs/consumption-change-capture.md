# Consumption change capture

## Scope

The LogisticsService migration `AddConsumptionChangeJournal` creates
`MedipulseMain.dbo.ConsumptionChange` and an AFTER trigger on `ConsumptionRecord`.
The journal feeds a bounded incremental consumer. The dbt mart remains the
independent full-source rebuild used for reconciliation.

Each committed insert, analytics-field update, or deletion records the source ID,
operation (`I`, `U`, `D`), and before/after facility, item, consumed timestamp, and
quantity. Existing records receive a baseline (`B`) when the migration is applied.
Baseline rows describe current state; they do not reconstruct prior history.

The trigger executes in the source transaction. Rolling back the source also
rolls back its journal rows; inability to write the journal fails the source
statement rather than silently losing analytics changes. It handles set-based
multi-row writes, including direct SQL. Staff, ward, and item names are omitted.
Changes only to those non-analytics fields, and no-op updates, emit no event.

The journal has no source foreign key so tombstones survive source deletion.
`RecordedAt` is database UTC capture time; `BeforeConsumedDate` and
`AfterConsumedDate` retain the source's stored-date semantics. EF's SQL OUTPUT
optimization is disabled for the triggered source table.

## Incremental consumer

`python pipelines/dbt/incremental_consumption.py --batch-size 100 --max-batches 10`
processes committed, unacknowledged changes in bounded batches. It uses the
same `SA_PASSWORD`, `SQLSERVER_HOST`, and ODBC 18 connection contract as the
other database validation scripts. The first run creates
`dbo.fct_daily_consumption_incremental` and `dbo.ConsumptionChangeAck`.
The target is separate from dbt's full rebuild table. Re-run the command while
changes remain; it exits nonzero if the configured batch limit is reached.

Each batch takes a SQL Server transaction-scoped application lock, chooses up
to 1,000 changes, deduplicates their old/new daily groups, recomputes those
groups directly from `ConsumptionRecord`, and acknowledges the selected
`ChangeId` values in the same transaction. A failed batch rolls back both target
rows and acknowledgements; a repeat run can resume. Only one processor updates
a target schema at a time. Separate batches commit independently, so consumers
may see a partially caught-up target until `drained` is true.

This is a manually invoked processor, not a production scheduler or an atomic
cross-system publication process. Retain journal rows until a reviewed purge
policy exists; deletion from the journal before acknowledgement loses work.

## Ordering and corrections

- Use `ChangeId` as a unique event identity, **not as commit order**. Identity
  allocation can precede commit and rollback leaves gaps. A simple
  `ChangeId > last_seen_id` consumer can permanently miss a late commit.
- `ConsumptionChangeAck` records each processed identity, with no high-water
  cursor. Reads skip uncommitted rows; if a lower identity commits later, it
  remains unacknowledged and is picked up on a subsequent batch.
- Recompute both old and new `(date, facility, item)` groups on a correction.
  A delete invalidates the old group; a baseline/insert invalidates the new one.
- Backfill by recomputing source aggregates, not by summing the whole journal:
  baseline and update images are not independent consumption events.

## Deployment and limitations

Normal EF migration deployment installs the table, trigger, and baseline in its
transaction. The baseline scan can block writes; schedule an upgrade window and
estimate its cost before deployment on a large existing database. Do not disable
the trigger, truncate the source, or use bulk loading without trigger firing:
those paths bypass capture and require a full reconciliation/rebaseline.

No pruning job is installed. Keep the journal until a consumer retention policy
and acknowledgement handling exist; monitor table growth. Restrict journal DML
and trigger administration through deployment-specific database roles. The
current development credentials do not make this a tamper-proof audit ledger.
Rolling this migration down drops capture history and requires a fresh baseline
before restarting a future consumer.

This journal tracks consumption rows, not inventory transactions. The existing
service saves a new consumption record before its stock deduction; this work
does not make those two operations a single business transaction.

## Verification

`python pipelines/dbt/run_pipeline.py` applies the actual SQL Server migration
and requires eight live journal tests: insert, old/new images, delete, multi-row
update, no-op/privacy-only update, source rollback, journal-write failure, and
column privacy. Tests roll back their changes and publish
`consumption-journal-tests.txt`. Four additional live SQL Server tests cover
bounded processing, rollback/retry, historical date correction, and competing
processors in disposable schemas. The runner compares the incremental result
with the independently rebuilt dbt mart and checks that a second pass has no
work; evidence is in `incremental-consumption.json` and
`incremental-consumption-tests.txt`. EF mapping/migration unit tests verify the
trigger-compatible mapping, table contract, baseline operation, and rollback
ordering. A populated production upgrade and concurrent consumers are not
claimed as tested by these checks.
