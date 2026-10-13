# Testing Guide

MediPulseMicro has backend unit tests, frontend unit tests, and Docker-backed integration tests.

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
