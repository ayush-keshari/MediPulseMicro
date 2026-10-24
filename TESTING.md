# Testing Guide

MediPulseMicro has backend unit tests, frontend unit tests, and Docker-backed integration tests.

Use the .NET 10 SDK for backend tests and Node.js 22.23.1 for frontend tests.
Initial package restores require network access. Tests described as offline
need no running application database, not necessarily an offline package cache.

## Disposable Database Quality Pipeline

With Python 3.10+ and Docker Compose 2.24.4+ (Linux containers), run from the root:

```text
python pipelines/dbt/run_pipeline.py
```

This is the same entry point used by the CI data-quality stage and daily
workflow. It provisions SQL Server, migrates, loads fixtures, verifies atomic
rollback and replay, runs dbt checks, retains reports, and removes its isolated
stack. It requires no host dbt/ODBC installation or configured SQL credentials.
It does not replace backend, frontend, or API integration tests below.
See [pipeline usage](pipelines/dbt/README.md) for storage and recovery details.

## Complete Fresh-Clone Validation

From the repository root, run the complete validation flow:

```powershell
./scripts/test-all.ps1
```

Linux/macOS with PowerShell installed:

```bash
./scripts/test-all.sh
```

This runs offline backend/frontend validation first, then the isolated Docker
integration suite. It stops at the first failed stage and returns a non-zero
exit code. Use `-SkipIntegration`, `-SkipFrontend`, `-SkipBackend`, or
`-Rebuild` when isolating a local failure.

## Backend Unit Tests

For the complete offline validation suite, run this from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Run-QualityChecks.ps1
```

The script returns a non-zero exit code at the first failed check, making it suitable for local validation and automation.

Run the complete backend test suite from the repository root:

```powershell
dotnet test Backend.slnx
```

The backend test projects use in-memory providers where possible, so this command does not require Docker or SQL Server.

## Frontend Tests

Run the frontend checks from `Frontend`:

```powershell
npm ci --legacy-peer-deps
npm test -- --watch=false
npm run typecheck
npm run lint
npm run build
```

## Integration Tests

Integration tests require Docker Desktop:

```powershell
powershell -ExecutionPolicy Bypass -File .\Run-IntegrationTests.ps1
```

The integration suite starts the Compose test stack, applies migrations, loads test data, exercises the service APIs, and returns the test process exit code.

## Continuous Integration

GitHub Actions runs these checks through `.github/workflows/ci.yml`:

- backend build and unit tests
- frontend build, tests, type checking, linting, and production dependency audit
- Docker-backed integration tests and dbt data-quality tests

The backend CI job now enforces at least 37% line coverage. The earlier run at commit
`348c15d` measured 36.5%, with 200 backend tests and 3 API integration tests
passing. These numbers describe that commit, not a permanent baseline promise.
See [validation evidence](docs/validation-evidence.md) for the run and artifacts.
