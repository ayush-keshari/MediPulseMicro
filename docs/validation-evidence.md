# Verified validation evidence

MediPulseMicro is primarily a .NET/Angular healthcare supply-chain application.
Its dbt suite validates operational SQL Server data and builds daily consumption
analytics; it is not a dedicated
data-engineering platform. Use executed test results, rather than file counts
alone, when assessing its current reliability.

## Verified commit

- Commit: `851f630120e9981f7f5ce76252e5e4c74c837799`.
- Date: 2026-10-24.
- [Full passing GitHub Actions run](https://github.com/ayush-keshari/MediPulseMicro/actions/runs/40096498570).
- Backend: 200 tests passed across 9 test projects; reported line coverage 36.5%.
- CI coverage minimum: 35%, enforced in `.github/workflows/ci.yml`.
- API integration: 3 tests passed against the running Compose stack.
- Database quality: 19 sources, 4 staging models, 1 aggregate mart, 233 passing dbt tests, and
  inventory freshness verification.
- Atomic fixture loading: 3 SQL Server tests passed, including late failure
  and nested compilation-error rollback across both databases.
- Fixture replay: passed exact row/multiplicity comparison.
- Transformation semantics: all 7 SQL Server cases passed, including late arrivals,
  corrections/deletions, date boundaries, and overflow-safe sums.
- Mart rebuild: 6 fixture-derived rows compared exactly after a second build;
  all rows and multiplicities unchanged, with model tests rerun successfully.
- Isolated pipeline: all stages passed; automatic cleanup passed; 157.2 seconds
  on this runner (not a performance guarantee).
- Frontend build, tests, dependency audit, type checking and lint job passed.
  Lint warnings still exist; a green job is not a claim of zero warnings.

Artifacts on that run include `dbt-quality-artifacts` and
`backend-coverage-report`. The quality artifact contains per-stage logs,
`pipeline-summary.json`, `validation-summary.json`, atomicity test output,
replay results, and dbt results. Download them before the configured retention
expires (quality reports: 7 days). Later runs publish fresh evidence.

The artifact also includes `transformation-tests.txt`, `mart-rebuild.json`, and
`mart-rebuild-{run,test}.json`. These distinguish actual SQL execution from
offline compile checks. Security checks and CodeQL also passed for this commit.

## Reproduce

### Lifecycle and safe-error regression checks (2026-10-25)

The follow-up backend suite passed 268 tests locally with 37.2% line coverage
and 68.2% branch coverage, using the same unfiltered collector as CI. The CI
minimum is raised from 35% to 37%; the verified commit above records the prior
baseline rather than claiming this follow-up has already passed remote CI.

New assertions cover all 49 known purchase-order state pairs, unknown statuses,
receipt acceptance across all seven states, deletion isolation, safe error
payloads, logging severity, and preservation of already-handled responses.
They exposed permissive cancellation from unknown/already-cancelled states.
The exception filter now preserves business-rule 409 responses and keeps
unexpected exception details out of client responses. Database assertions use
fresh EF reads after clearing tracking; these unit tests do not prove SQL Server
foreign-key enforcement or concurrent transaction behavior.

Generated migrations remain in the coverage denominator. No coverage exclusion
was added to inflate the result. Overall coverage is still below the 50% target.

### Commands

From the root of a fresh checkout:

```text
dotnet test Backend.slnx --configuration Release
python pipelines/dbt/run_pipeline.py
```

The first command requires .NET 10. The second requires Python 3.10+ and Docker
Compose 2.24.4+ with Linux containers; all other quality-pipeline tooling runs
inside containers. See `TESTING.md` for frontend and API integration commands.

## Remaining gaps

37.2% backend coverage is limited. Raising the minimum to 50% without first
adding and measuring meaningful behavior tests would only break the gate.
Prioritize business-rule branches, rejected state transitions, multi-lot FEFO
allocation, and persistence/transaction failures. The 3 API integration tests
are smoke-level coverage, not exhaustive service-to-service validation.

The fixture pipeline provides disposable setup and repeat-load checks. The
consumption mart supports full historical recomputation from retained source
events, not production incremental ingestion, recovery of deleted history, or
partial-row fixture repair. See the [lineage contract](../pipelines/dbt/docs/lineage.md).
No external assessment score is guaranteed by these results.
