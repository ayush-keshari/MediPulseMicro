# MediPulseMicro - Healthcare Supply Chain Microservices

A backend-focused healthcare supply-chain application built with .NET 10, Entity
Framework Core, SQL Server, and an Angular frontend. Eight domain services sit
behind an API gateway. The dbt project is a supporting analytics and data-quality suite,
not the primary product or a standalone data warehouse.

## Build and test from a fresh clone

Backend unit tests require the .NET 10 SDK. They do not require Docker or SQL
Server; the first run restores NuGet packages.

```text
dotnet test Backend.slnx --configuration Release
```

Frontend tests require Node.js 22.23.1 and npm:

```text
npm --prefix Frontend ci --legacy-peer-deps
npm --prefix Frontend test -- --watch=false
```

The separate database-quality suite requires Python 3.10+ and Docker Compose
2.24.4+ with Linux containers. It provisions its own disposable databases:

```text
python pipelines/dbt/run_pipeline.py
```

This command does not run the application API integration tests. See
[TESTING.md](TESTING.md) for those commands and [verified CI evidence](docs/validation-evidence.md)
for measured results. Coverage is currently 36.5% with a 35% CI minimum, not a
claim of comprehensive business-logic coverage.

## 🏗️ Architecture

MediPulseMicro consists of the following services:

- **AuthService** - Authentication and authorization (JWT-based)
- **FacilityService** - Manage healthcare facilities and storage zones
- **InventoryService** - Track medical inventory across facilities
- **ProcurementService** - Handle purchase orders and supplier management
- **LogisticsService** - Manage transfer orders and consumption tracking
- **TelemetryService** - Collect and analyze device/sensor data
- **NotificationService** - Send alerts and notifications
- **AuditService** - Comprehensive audit logging
- **Gateway** - Ocelot-based API Gateway
- **Frontend** - Angular application (served via nginx)

All backend services follow a clean layered architecture:
- Controllers (API endpoints)
- Services (business logic)
- DTOs (data transfer objects)
- Data Access (Entity Framework Core)
- Shared libraries for common functionality

## 🚀 Quick Start

## 🗄️ Database Initialization

The database initialization follows a two-step process:
1. **Schema Creation**: EF Core migrations are applied to create database tables
2. **Seed Data Population**: Idempotent SQL scripts insert mock data

This approach ensures:
- Schema changes are managed through EF Core migrations (versioned, reversible)
- Complete fixture replay is tested for duplicate and value preservation; partial-row repair is not supported
- Clear separation of concerns between schema and data
- Integration with existing Docker Compose orchestration

The initialization flow in docker-compose.yml:
1. The `migrator` service waits for SQL Server to be ready
2. Applies EF Core migrations for all services using `dotnet ef database update`
3. Mock data is loaded explicitly with the scripts in `Scripts/mockdata/`
4. Services start only after migrator completes successfully

## 📋 Data Quality Checks

Run `python pipelines/dbt/run_pipeline.py` from the repository root.
The same command runs in CI and on the daily schedule. It builds the tools in
containers, starts an isolated SQL Server, applies migrations, loads fixtures
atomically, verifies rollback and replay behavior, and runs dbt model, test,
and freshness checks.

The pipeline builds a daily consumption mart by facility and item, reconciles
each aggregate with source events, tests late-arrival and correction behavior
on SQL Server, and checks that rebuilding unchanged data preserves every row.
See the [analytics lineage and rebuild contract](pipelines/dbt/docs/lineage.md)
for its grain, stored-date semantics, failure recovery, and production limits.

No local dbt installation, ODBC driver, profile, database password, or manual
SQL commands are required for this disposable run. Reports are retained under
`pipelines/dbt/target/pipeline/<run-id>/`; containers and volumes are removed
after success or failure. Docker images and build cache are retained for reuse.
Allow sufficient Docker disk space before the first run.

Fixture replay is verified for a complete load. Existing first-row guards do
not repair partially populated tables; a failed isolated run is retried against
new databases. See [pipeline operations](pipelines/dbt/OPERATIONS.md) for limits
and [existing-database checks](pipelines/dbt/README.md) for explicit profile setup.
The SQL files under `Scripts/DQ/` are optional service-owner diagnostics.

### Prerequisites
- Docker Engine
- Docker Compose
- Git

### One-Command Setup
```bash
# Clone the repository
git clone https://github.com/ayush-keshari/MediPulseMicro.git
cd MediPulseMicro

# Copy environment template
cp .env.example .env

# Review and adjust environment variables if needed (optional)
# nano .env

# Start all services
docker compose up -d

# Wait for services to initialize (~2-3 minutes)
# Check status with: docker compose ps

  # Run backend unit tests without Docker or SQL Server
  dotnet test Backend.slnx
```

### Access the Application
- Frontend: http://localhost
- API Gateway: http://localhost:5000
- Swagger UI (per service): http://localhost:{port}/swagger
  - AuthService: http://localhost:5001/swagger
  - FacilityService: http://localhost:5002/swagger
  - InventoryService: http://localhost:5003/swagger
  - ProcurementService: http://localhost:5004/swagger
  - LogisticsService: http://localhost:5005/swagger
  - TelemetryService: http://localhost:5006/swagger
  - NotificationService: http://localhost:5007/swagger
  - AuditService: http://localhost:5008/swagger

## 🔧 Environment Variables

Copy `.env.example` to `.env` and adjust as needed:

| Variable | Description | Default (Example) |
|----------|-------------|-------------------|
| `DB_SERVER` | SQL Server hostname | `sqlserver` |
| `DB_NAME_MAIN` | Main database name | `MedipulseMain` |
| `DB_NAME_AUDIT` | Audit database name | `MedipulseAudit` |
| `SA_PASSWORD` | SQL Server sa password | `YOUR_STRONG_PASSWORD_HERE` |
| `JWT_KEY` | Secret key for JWT tokens | `YOUR_STRONG_KEY_HERE_MIN_32_CHARS` |
| `JWT_ISSUER` | JWT token issuer | `MediPulseAuthService` |
| `JWT_AUDIENCE` | JWT token audience | `MediPulseAPI` |
| `SENTRY_DSN` | Optional Sentry exception-tracking DSN | empty (disabled) |

> **Warning**: The example values above are for local development only. In production, you MUST override these with strong, unique secrets. Never use the example values in a production environment.

Sentry is disabled when `SENTRY_DSN` is empty. When configured, all services capture handled business-rule and unhandled exceptions with correlation IDs while keeping personally identifiable data disabled.

## 🧪 Running Tests

For the complete command reference, see [TESTING.md](TESTING.md).

### One-command quality check

From the repository root, run the complete offline validation suite:

```powershell
powershell -ExecutionPolicy Bypass -File .\Run-QualityChecks.ps1
```

This runs backend tests, frontend tests, TypeScript checking, linting, and the production frontend build. It installs frontend dependencies from the committed lockfile when `node_modules` is not present. Use `-SkipFrontend` or `-SkipBackend` when isolating a failing area.

To validate a fresh checkout end to end, including the Docker-backed API
integration suite, run `./scripts/test-all.ps1`. On Linux/macOS with
PowerShell installed, use `./scripts/test-all.sh`.

The logistics domain includes a documented FEFO multi-lot stock allocation transform; see [docs/fefo-stock-allocation.md](docs/fefo-stock-allocation.md).

## Domain Differentiation

MediPulseMicro is designed around healthcare supply-chain decisions rather than generic CRUD alone:

- **FEFO stock movement:** transfer completion allocates across multiple lots by earliest expiry, preserves lot metadata, and reports partial allocation.
- **Safety-stock replenishment:** inventory positions are evaluated against safety thresholds and recent consumption to produce prioritized replenishment plans.
- **Exception detection:** stockout and near-expiry scans create deduplicated operational exceptions for follow-up.
- **Data-quality contracts:** dbt tests validate healthcare facility, inventory, procurement, logistics, telemetry, and audit sources in CI.

You can run many tests offline without any external dependencies (no Docker or SQL Server required).

### Backend Tests (Offline)
```bash
# Run all backend tests (uses InMemoryDatabase, no SQL Server needed)
dotnet test Backend.slnx

# Run tests for a specific service
dotnet test AuthService.Tests/AuthService.Tests.csproj
```

### Frontend Tests (Offline)
```bash
cd Frontend
npm test -- --watch=false
```

### Linting and Formatting (Offline)
```bash
# Backend formatting check
dotnet format --verify-no-changes

  # Frontend linting
  cd Frontend
  npm run typecheck
  npm run lint
```

### Integration Tests (Requires Docker)
Integration tests require Docker and SQL Server to test the full microservices stack. They validate service-to-service communication, API endpoints, and database interactions.

#### Local Execution
You can run integration tests locally using the provided script:
```bash
# PowerShell (Windows/Linux/macOS)
powershell -ExecutionPolicy Bypass -File Run-IntegrationTests.ps1

# Optional parameters:
#   -Rebuild    : Force rebuild of Docker images
#   -NoBuild    : Use existing Docker images (skip build)
#   -TestFilter : Filter tests (e.g., "-TestFilter 'AuthService'")
```

#### How It Works
The integration testing mechanism:
1. Uses `docker-compose.test.yml` (optimized for testing - lighter weight, fixed versions)
2. Starts SQL Server and all backend services
3. Runs EF Core migrations and loads mock data via the migrator service
4. Executes integration tests that make HTTP requests to the running services
5. Preserves test output and returns proper exit codes
6. Cleans up all containers and volumes after completion

#### CI Pipeline Integration
The same mechanism is used in the CI pipeline via the `integration_and_data_quality` job in `.github/workflows/ci.yml`, which:
- Uses the same docker-compose.test.yml equivalent configuration
- Runs API integration tests against the Compose stack with `dotnet test IntegrationTests/IntegrationTests.csproj`
- Runs the separate `backend_test` job with a 35% minimum line-coverage gate; the verified run at `348c15d` measured 36.5%.
- Preserves test results as artifacts

#### Test Coverage
Current integration tests cover:
- Gateway accessibility and routing to backend services
- Basic endpoint availability for all microservices
- Service-to-service communication through the API gateway

## 🐳 Docker Compose Services

The `docker-compose.yml` defines the following services:

- **sqlserver** - Microsoft SQL Server 2022
  - **migrator** - Temporary service that runs EF Core migrations
- **authservice, facilityservice, inventoryservice, procurementservice, logisticsservice, telemetryservice, notificationservice, auditservice** - Backend microservices
- **gateway** - Ocelot API Gateway
- **frontend** - Angular application served via nginx
- **sonarqube** - Code quality analysis (optional)

### Common Docker Compose Commands

```bash
# Start all services in detached mode
docker compose up -d

# Stop and remove all containers, networks, and volumes
docker compose down -v

# View logs for all services
docker compose logs -f

# View logs for a specific service
docker compose logs -f authservice

# Rebuild and restart a service after code changes
docker compose up -d --build <service-name>

  # Run migrations, then load mock data through SQL Server
  docker compose run --rm migrator
  docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -b -S localhost -U SA -P "$SA_PASSWORD" -i /dev/stdin -d MedipulseMain < MockData_InsertOnly.sql
```

## 📊 CI/CD Pipeline

The project uses GitHub Actions for continuous integration:

- Runs on every push to `main`, `dev2`, and feature branches
- Builds and tests all services
- Validates Docker Compose setup
- Runs backend unit tests with an enforced 35% minimum line-coverage gate
- Performs frontend TypeScript checking and linting
- Validates mock data referential integrity
- Checks .NET formatting consistency

### DBT Data Quality Tests

The main integration job runs dbt tests after migrations and mock-data loading. A lightweight orchestrator also reruns the same checks on a regular schedule:

- **Scheduled Workflow**: `.github/workflows/scheduled-dbt-tests.yml`
- **Schedule**: Runs daily at 2:00 AM UTC (configurable via cron syntax)
- **Manual Trigger**: Can be triggered manually via the GitHub UI ("Run workflow")
- **Process**:
  1. Checks out code
  2. Sets up SQL Server (using docker-compose)
  3. Runs EF Core migrations and loads mock data
  4. Validates mock data referential integrity
  5. Installs dbt dependencies
  6. Runs dbt tests against the migrated database
  7. Publishes test results as an artifact

See [`docs/data-lineage.md`](docs/data-lineage.md) for service ownership, table lineage, and the complete validation flow.

#### Modifying the Schedule

To change the schedule:
1. Edit `.github/workflows/scheduled-dbt-tests.yml`
2. Modify the `cron` value in the `on.schedule` section
3. Use standard cron syntax: `minute hour day month day-of-week`
4. Examples:
   - `0 2 * * *` = Daily at 2:00 AM UTC (current)
   - `0 */6 * * *` = Every 6 hours
   - `0 0 * * 0` = Weekly on Sunday at midnight
   - `0 0 1 * *` = Monthly on the 1st at midnight

For testing, you can also manually trigger the workflow from the GitHub Actions tab.

## 📚 API Documentation

Each service provides Swagger/OpenAPI documentation:
- Access via `http://localhost:{service-port}/swagger`
- Includes JWT authorization for protected endpoints
- Comprehensive DTO descriptions and examples

## 🔐 Security

- JWT-based authentication for all backend services
- Role-based access control (RBAC)
- Secure password hashing with BCrypt
- Environment-based configuration (no hardcoded secrets)
- Regular dependency vulnerability scanning

## 🛠️ Development

### Adding a New Service
1. Create new directory under `src/` (e.g., `NewService`)
2. Add `NewService.csproj` referencing `Shared.csproj`
3. Implement standard layers: Controllers, Services, DTOs, Data
4. Add Dockerfile based on existing templates
5. Register service in `docker-compose.yml`
6. Add corresponding test project

### Database Migrations
```bash
# Add a new migration
dotnet ef migrations add MigrationName --project ServiceDir/ServiceDir.csproj

# Apply migrations
docker compose up migrator --abort-on-container-exit --exit-code-from migrator
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure your code follows:
- Existing code style and patterns
- Includes appropriate unit tests
- Passes `dotnet format --verify-no-changes`
- Adds or updates documentation as needed

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Microsoft .NET team for the excellent platform
- Angular team for the frontend framework
- Docker team for containerization excellence
- All open-source packages used in this project
