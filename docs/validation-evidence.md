# Verified validation evidence

MediPulseMicro is primarily a .NET/Angular healthcare supply-chain application.
Its dbt suite validates the operational SQL Server data; it is not a dedicated
data-engineering platform. Use executed test results, rather than file counts
alone, when assessing its current reliability.

## Verified commit

- Commit: `348c15d6e5763611f25a7892bb45c0fd52cf6bc2`.
- Date: 2026-10-24.
- [Full passing GitHub Actions run](https://github.com/ayush-keshari/MediPulseMicro/actions/runs/40076563780).
- Backend: 200 tests passed across 9 test projects; reported line coverage 36.5%.
- CI coverage minimum: 35%, enforced in `.github/workflows/ci.yml`.
- API integration: 3 tests passed against the running Compose stack.
- Database quality: 19 sources, 3 staging models, 212 passing dbt tests, and
  inventory freshness verification.
- Atomic fixture loading: 3 SQL Server tests passed, including late failure
  and nested compilation-error rollback across both databases.
- Fixture replay: passed exact row/multiplicity comparison.
- Isolated pipeline: all stages passed; automatic cleanup passed; 138.16 seconds
  on this runner (not a performance guarantee).
- Frontend build, tests, dependency audit, type checking and lint job passed.
  Lint warnings still exist; a green job is not a claim of zero warnings.

Artifacts on that run include `dbt-quality-artifacts` and
`backend-coverage-report`. The quality artifact contains per-stage logs,
`pipeline-summary.json`, `validation-summary.json`, atomicity test output,
replay results, and dbt results. Download them before the configured retention
expires (quality reports: 7 days). Later runs publish fresh evidence.

## Reproduce

From the root of a fresh checkout:

```text
dotnet test Backend.slnx --configuration Release
python pipelines/dbt/run_pipeline.py
```

The first command requires .NET 10. The second requires Python 3.10+ and Docker
Compose 2.24.4+ with Linux containers; all other quality-pipeline tooling runs
inside containers. See `TESTING.md` for frontend and API integration commands.

## Remaining gaps

36.5% backend coverage is limited. Raising the minimum to 50% without first
adding and measuring meaningful behavior tests would only break the gate.
Prioritize business-rule branches, rejected state transitions, multi-lot FEFO
allocation, and persistence/transaction failures. The 3 API integration tests
are smoke-level coverage, not exhaustive service-to-service validation.

The fixture pipeline provides disposable setup and repeat-load checks, not
production incremental ingestion, historical backfills, or partial-row repair.
No external assessment score is guaranteed by these results.
