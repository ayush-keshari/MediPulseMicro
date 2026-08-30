# Changelog

All notable changes to MediPulseMicro will be documented in this file.

## [0.2.0] - 2026-08-30
### Added
- Structured JSON request logging with correlation IDs and optional Application Insights traces
- Prometheus-compatible request, error, and duration metrics in Gateway and AuthService
- Service-to-table-to-fixture data-lineage documentation
- Declarative dbt quality contracts with deterministic scheduled execution
- Split Facility, Procurement, and Telemetry backend test suites for maintainability

## [0.1.0] - 2026-08-30
### Added
- Reproducible .NET and frontend dependency lockfiles across the repository
- CI data-quality gates with Docker-backed migrations and machine-readable checks
- Offline test documentation and a dedicated Docker integration-test profile
- Structured request metrics at Gateway and AuthService via `/metrics`

### Fixed
- Corrected SQL fixture and data-quality table names to match the EF Core schema
- Stabilized frontend linting, backend test execution, and integration migration startup

## [Unreleased]
### Added
- Data quality check script (`Scripts/dq_checks.sql`) with row-count and null constraints per table
- Per-service schema and mock data files under `Scripts/schema/` and `Scripts/mockdata/`
- Referential integrity validation script (`Scripts/validate_mock_data.sql`)
- Health check endpoints in all services via `Microsoft.Extensions.Diagnostics.HealthChecks`
- Optional Application Insights logging integration (requires `APPINSIGHTS_INSTRUMENTATIONKEY`)
- Comprehensive unit tests for AuditService and LogisticsService with behavior-based testing
- Repository-wide `.editorconfig` for consistent code formatting
- CI pipeline enhancements:
  - Lockfile validation before restore
  - Coverage gate increased to 60% with reliable collection and reporting
  - Data quality checks run post-migration against both MedipulseMain and MedipulseAudit databases
  - .NET formatting verification via `dotnet format --verify-no-changes`
  - Modular CI jobs separating backend build, frontend build, backend test, frontend test, and integration/data quality
  - Dependency caching for NuGet and NPM packages to improve build performance
- CONTRIBUTING.md with setup, testing, coding standards, and commit convention guidelines

### Changed
- Split monolithic `SQL_Schema_and_MockData.sql` into separate schema and mock data files
- Split monolithic LogisticsService test file into logical test classes: TransferOrderCreationTests.cs, TransferOrderStatusTransitionTests.cs, TransferOrderUpdateTests.cs, TransferOrderDeletionTests.cs, and StockMovementTests.cs
- Made mock data loader idempotent using `IF NOT EXISTS` patterns
- Updated Docker Compose to use new schema/mock data structure
- Updated all service `.csproj` files with required package references and lockfile settings
- Improved test coverage and reliability with InMemoryDatabase verification
- Enhanced observability with structured logging using Serilog including correlation IDs, service name, and environment enrichment
- Standardized error handling using BusinessRuleException for business logic violations (409) vs system exceptions (500)
- Improved JWT authentication with proper issuer and audience validation using "Jwt:Key" configuration
- Updated environment variable documentation with clear placeholders and production usage warnings
- Enhanced Angular frontend test coverage with meaningful tests for authentication workflows, API services, and business components
- Replaced manual SQL schema creation with proper EF Core migrations in LogisticsService and ProcurementService
- Integrated service-specific SQL data quality checks into CI pipeline replacing legacy monolithic checks
- Centralized logging configuration, standardized middleware, and enhanced CORS policies for improved architecture

### Fixed
- Resolved duplicate `</ItemGroup>` tags in service project files when adding package references
- Fixed AuditService test assertions (UserRole "Administrator" → "Admin")
- Corrected GetByIdAsync to use actual ID from database
- Fixed CI/CD failures related to test issues and coverage reporting
- Corrected Serilog configuration to prevent duplicate use calls in Gateway service
- Fixed missing IHttpClientFactory dependency for ActivityLogFilter middleware by adding services.AddHttpClient()
- Updated JWT configuration key from "JWT_KEY" to "Jwt:Key" with proper validation
- Added missing Shared.Exceptions usings and replaced generic exception throws with BusinessRuleException
- Fixed Angular test configuration schema validation error in angular.json
- Removed invalid call to private updateLayout() method in frontend tests
- Fixed dotnet test coverage command failure by using --property:CollectCoverage=true syntax
- Corrected reportgenerator usage to properly generate text summary reports
