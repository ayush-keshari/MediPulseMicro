# Changelog

All notable changes to MediPulseMicro will be documented in this file.

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
  - Coverage gate increased to 60%
  - Data quality checks run post-migration against both MedipulseMain and MedipulseAudit databases
  - .NET formatting verification via `dotnet format --verify-no-changes`
- CONTRIBUTING.md with setup, testing, and coding standards guidelines

### Changed
- Split monolithic `SQL_Schema_and_MockData.sql` into separate schema and mock data files
- Made mock data loader idempotent using `IF NOT EXISTS` patterns
- Updated Docker Compose to use new schema/mock data structure
- Updated all service `.csproj` files with required package references and lockfile settings
- Improved test coverage and reliability with InMemoryDatabase verification

### Fixed
- Resolved duplicate `</ItemGroup>` tags in service project files when adding package references
- Fixed AuditService test assertions (UserRole "Administrator" → "Admin")
- Corrected GetByIdAsync to use actual ID from database
- Fixed CI/CD failures related to test issues and coverage reporting