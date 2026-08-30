# MediPulse Data Lineage

This document records the ownership and validation path for the shared SQL Server databases.

## Ownership

| Service | Database | Owned tables |
| --- | --- | --- |
| FacilityService | `MedipulseMain` | `Facility`, `StorageZone` |
| InventoryService | `MedipulseMain` | `Items`, `InventoryPositions`, `ExceptionEvent`, `RecallAction`, `Forecast`, `ReplenishmentPlan` |
| ProcurementService | `MedipulseMain` | `Supplier`, `PurchaseOrder`, `Receipt` |
| LogisticsService | `MedipulseMain` | `TransferOrder`, `TransferOrderItem`, `ConsumptionRecord` |
| TelemetryService | `MedipulseMain` | `SensorDevice`, `TelemetryRecord` |
| NotificationService | `MedipulseMain` | `Notification` |
| AuthService | `MedipulseMain` | `User` |
| AuditService | `MedipulseAudit` | `AuditLog` |

Cross-service reads are explicit in the EF contexts. For example, LogisticsService reads and updates `InventoryPositions`, while InventoryService remains the migration owner for that table.

## Validation flow

1. The `migrator` container applies EF Core migrations to `MedipulseMain` and `MedipulseAudit`.
2. `MockData_InsertOnly.sql` loads deterministic fixtures after migrations complete.
3. `Scripts/validate_mock_data.sql` checks fixture-level referential integrity.
4. `Scripts/DQ/*.sql` runs structured SQL Server checks for row counts and mandatory fields.
5. `pipelines/dbt/models/schema.yml` declares source contracts, minimum row counts, uniqueness, nullability, accepted values, and cross-table relationships.
6. `dbt test` runs those declarations against the same migrated and seeded `MedipulseMain` database in CI.

The scheduled dbt workflow provides a daily/manual rerun, while the main CI integration job runs the same dbt gate on every push and pull request path that reaches integration validation.

## Test modes

- `dotnet test Backend.slnx` runs backend unit tests offline using in-memory providers.
- `npm test` and the frontend build/lint/type checks run without SQL Server.
- `Run-IntegrationTests.ps1` and the integration CI job require Docker and SQL Server because they validate migrations, seeded data, HTTP service wiring, and dbt queries.
