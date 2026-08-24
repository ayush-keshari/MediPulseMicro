# MediPulseMicro - Healthcare Supply Chain Microservices

A comprehensive microservices architecture for healthcare supply chain management, built with .NET 10 and Angular.

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
- Data insertion is idempotent (safe to run multiple times without duplication)
- Clear separation of concerns between schema and data
- Integration with existing Docker Compose orchestration

The initialization flow in docker-compose.yml:
1. The `migrator` service waits for SQL Server to be ready
2. Applies EF Core migrations for all services using `dotnet ef database update`
3. Loads mock data using `sqlcmd` with `Scripts/MockData_InsertOnly.sql`
4. Services start only after migrator completes successfully

## 📋 Data Quality Checks

You can run the data quality checks locally to validate the database schema and data integrity.

```bash
# Ensure SQL Server is running (via docker compose) and the MediPulseMicro database is populated.
# If you have not yet started the stack, do:
docker compose up -d sqlserver migrator
docker compose up migrator --abort-on-container-exit --exit-code-from migrator

# Set the SA_PASSWORD environment variable (matches the one in .env or docker-compose.yml)
export SA_PASSWORD=MediPulse@2024!Dev   # Linux/macOS
# For Windows PowerShell:
# $env:SA_PASSWORD = "MediPulse@2024!Dev"

# Run the data quality check script
powershell -ExecutionPolicy Bypass -File Scripts\Run-DqChecks.ps1
```


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

# Run tests
dotnet test
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

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_SERVER` | SQL Server hostname | `sqlserver` |
| `DB_NAME_MAIN` | Main database name | `MedipulseMain` |
| `DB_NAME_AUDIT` | Audit database name | `MedipulseAudit` |
| `SA_PASSWORD` | SQL Server sa password | `MediPulse@2024!Dev` |
| `JWT_KEY` | Secret key for JWT tokens | `MediPulse@SuperSecretJWT_Key_2024!ForDev` |
| `JWT_ISSUER` | JWT token issuer | `MediPulseAuthService` |
| `JWT_AUDIENCE` | JWT token audience | `MediPulseAPI` |

> **Note**: For production, always override these values with strong secrets.

## 🧪 Running Tests

You can run many tests offline without any external dependencies (no Docker or SQL Server required).

### Backend Tests (Offline)
```bash
# Run all backend tests (uses InMemoryDatabase, no SQL Server needed)
dotnet test

# Run tests for a specific service
dotnet test AuthService/AuthService.csproj
```

### Frontend Tests (Offline)
```bash
cd Frontend
npm test
```

### Linting and Formatting (Offline)
```bash
# Backend formatting check
dotnet format --verify-no-changes

# Frontend linting
cd Frontend
npm run lint
```

### Integration Tests (Requires Docker)
Integration tests require Docker and SQL Server. They are run in the CI pipeline but can be executed locally:
```bash
# Start dependencies (SQL Server)
docker compose up -d sqlserver

# Run migrations and load test data
docker compose up migrator --abort-on-container-exit --exit-code-from migrator

# Run integration tests (example: using a test project or script)
# Note: Integration tests are primarily run in CI via the integration-validation job.
```

## 🐳 Docker Compose Services

The `docker-compose.yml` defines the following services:

- **sqlserver** - Microsoft SQL Server 2022
- **migrator** - Temporary service that runs EF Core migrations and loads mock data
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

# Run a one-off command in a service (e.g., run migrations)
docker compose run --rm migrator
```

## 📊 CI/CD Pipeline

The project uses GitHub Actions for continuous integration:

- Runs on every push to `main`, `dev2`, and feature branches
- Builds and tests all services
- Validates Docker Compose setup
- Runs backend unit tests with coverage gate (minimum 40%)
- Performs frontend TypeScript checking and linting
- Validates mock data referential integrity
- Checks .NET formatting consistency

### Scheduled DBT Tests Orchestrator

In addition to the CI pipeline, a lightweight orchestrator runs dbt tests on a regular schedule to ensure ongoing data quality:

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