# Pipeline and data-quality improvement plan

The latest assessment gives Pipeline & Data Quality 42/100. Focus on executable
contracts and reproducible evidence before adding another orchestration product.
The external overall score cannot be predicted from these changes alone.

## Delivered in this change

1. Repair macro discovery (`macros`, not `macro`), validate generic test resolution,
   and compile the entire dbt graph without a database in the fast CI job.
2. Use the same parse/build/test/freshness runner locally, on pushes/PRs, and on
   the scheduled workflow. Reject empty model selections and missing results.
3. Reconcile all projected columns in the three staging views against sources.
   Validate missing/extra/changed/null rows with mutation tests and keep key
   uniqueness contracts for duplicate detection.
4. Preserve per-stage artifacts and failure summaries for operator review.
   Document profile setup, selectors, recovery steps, and freshness limitations.

## Acceptance evidence

- Offline runner and reconciliation regression tests must pass.
- All dbt models and tests must compile using the SQL Server adapter.
- The database-backed CI job must execute every data test and source freshness.
- A syntax-only pass or a successful parse is not a successful database test run.

## Next milestone: fixture replay and self-contained execution

Complete-load replay verification is now implemented in
`verify_fixture_replay.py` and wired into both database-backed workflows. It
compares exact rows with duplicate multiplicities and always rolls back. Unit
tests validate change detection and rollback. SQL Server execution, all 212 dbt
tests, and the full integration stack passed in CI run 40072804861 at commit
`7405e05`.

The canonical fixture loader now wraps all four files in one cross-database
transaction. Database-backed fault-injection tests exercise successful commit,
late failure, and nested compilation failure using disposable probe tables.
The offline suite checks batch construction, command failure propagation,
password handling, and rejection of unsupported client commands.

Remaining work:

1. Create a single isolated quality-database runner using the existing migrator,
   with unique Compose projects, cleanup on failure, and preserved evidence.
2. Recover by rebuilding a failed disposable fixture database before retrying;
   rollback does not restore SQL Server identity counters.
3. If partial-row repair is required, redesign fixture keys and per-row guards
   and test those scenarios explicitly. Current first-row guards cannot repair
   an already incomplete fixture table. Do not claim idempotent backfills yet.

This requires a running Docker engine and sufficient disk for SQL Server and
the migrator. The current runner assumes an already provisioned database.

## Reassessment

Submit the exact commit with green CI and its quality artifacts. Recheck feedback
that claims tests/lint/type checks are absent against the actual workflow jobs.
Only after measuring this change should a second area (service coverage) be
expanded. Do not raise coverage thresholds beyond measured coverage or add code
solely to increase file counts.
