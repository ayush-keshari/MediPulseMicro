# Consumption change capture

## Scope

The LogisticsService migration `AddConsumptionChangeJournal` creates
`MedipulseMain.dbo.ConsumptionChange` and an AFTER trigger on `ConsumptionRecord`.
This is the capture foundation for incremental analytics, not yet an incremental
consumer. The daily mart continues to rebuild from the full source.

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

## Consumer contract for the next step

- Use `ChangeId` as a unique event identity, **not as commit order**. Identity
  allocation can precede commit and rollback leaves gaps. A simple
  `ChangeId > last_seen_id` consumer can permanently miss a late commit.
- Persist per-consumer event acknowledgements in the same transaction as target
  changes; query committed unacknowledged events. Never advance a timestamp-only
  cursor. Bounded batches, concurrency ownership, retries, and acknowledgements
  are the next implementation step, not existing capabilities.
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
`consumption-journal-tests.txt`. EF mapping/migration unit tests verify the
trigger-compatible mapping, table contract, baseline operation, and rollback
ordering. A populated production upgrade and concurrent consumers are not
claimed as tested by these checks.
