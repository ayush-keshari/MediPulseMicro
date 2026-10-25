# Evidence-led improvement plan

The October 25 assessment scored the repository 56.5/100 as data engineering,
although its primary product is a .NET/Angular healthcare supply-chain system.
No repository change can guarantee an external score or a cash offer. Treat
reviewer recommendations as hypotheses and verify them against HEAD and CI.

## Baseline

- CI at `4ab6d91` passed backend/frontend build and tests, dbt parse, live SQL
  Server quality tests, and integration tests. The review's statement that CI
  does not run tests/lint/type checks is contradicted by this run and `ci.yml`.
- The backend coverage artifact reports 38.9% raw line coverage (3313/8500).
  Generated EF migrations, model snapshots, and startup code remain in that
  denominator. Existing controller coverage is mostly 85-100%; do not add
  redundant tests merely to increase the number of spec files.
- Pipeline & Data Quality is the weakest scored area (40/100). The repo already
  has five dbt models, source contracts, journal-backed incremental consumption,
  CI checks, and a full-rebuild comparison. Remaining gaps are operational
  recovery, bounded historical repair, and production scheduling/publication.

## Ordered work

1. Add a read-only reconciliation command and transactional, date-bounded
   backfill for the incremental consumption mart. Require a drained journal,
   serialize with the normal consumer, preserve other dates, and prove failure
   rollback with live SQL Server tests. Do not claim continuous CDC or recovery
   of missing source history. Status: in progress.
2. Address incomplete fixture loads separately. Either implement keyed,
   per-row repair with real SQL Server interruption tests or make the loader
   reject partial states clearly before writing. Never silently skip missing
   rows because the first row already exists.
3. Improve tests for uncovered handwritten business and cross-service paths,
   starting with the lowest-covered consequential branches, not generated
   migrations. Publish raw coverage and a separately labelled handwritten-code
   measure. Raise the enforced raw threshold only after a passing measurement.
4. Verify a fresh clone using only documented commands, retain the exact green
   CI run and quality artifacts, then request a rescore. Compare dimension
   evidence rather than the overall number alone; ask the reviewer to correct
   the backend/data-engineering classification if it remains mismatched.

Completion means reproducible tests, an honest recovery contract, and a green
HEAD. The target score of 70+ is an external outcome, not an engineering test.
