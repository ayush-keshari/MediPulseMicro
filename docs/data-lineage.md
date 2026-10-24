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

## Fixture and quality ownership

| Database/table group | Fixture source | Canonical dbt coverage | Legacy/diagnostic coverage |
| --- | --- | --- | --- |
| `MedipulseMain.Facility`, `StorageZone` | `MockData_InsertOnly.sql` | `schema.yml`: not-null, unique, relationship, minimum rows | `Scripts/DQ/FacilityService.sql` |
| `MedipulseMain.Items`, `InventoryPositions`, `ExceptionEvent`, `RecallAction`, `Forecast`, `ReplenishmentPlan` | `MockData_InsertOnly.sql` | `schema.yml`: not-null, unique, relationship, minimum rows | `Scripts/DQ/InventoryService.sql` |
| `MedipulseMain.Supplier`, `PurchaseOrder`, `Receipt` | `MockData_InsertOnly.sql` | `schema.yml`: not-null, unique, relationship, accepted values, minimum rows | `Scripts/DQ/ProcurementService.sql` |
| `MedipulseMain.TransferOrder`, `TransferOrderItem`, `ConsumptionRecord` | `MockData_InsertOnly.sql` | `schema.yml`: not-null, unique, relationship, accepted values, minimum rows | `Scripts/DQ/LogisticsService.sql` |
| `MedipulseMain.SensorDevice`, `TelemetryRecord` | `MockData_InsertOnly.sql` | `schema.yml`: not-null, unique, relationship, minimum rows | `Scripts/DQ/TelemetryService.sql` |
| `MedipulseMain.User` | `Scripts/mockdata/AuthService.sql` | `schema.yml`: not-null, unique, minimum rows | `Scripts/DQ/AuthService.sql` |
| `MedipulseMain.Notification` | `Scripts/mockdata/NotificationService.sql` | `schema.yml`: not-null, unique, minimum rows | `Scripts/DQ/NotificationService.sql` |
| `MedipulseAudit.AuditLog` | `Scripts/mockdata/AuditService.sql` | `schema.yml`: not-null, unique, minimum rows | `Scripts/DQ/AuditService.sql` |

`pipelines/dbt/models/schema.yml` is the declarative quality contract. The service-specific SQL files remain available for focused diagnostics; they are not the source of the CI assertions.

## Validation flow

1. The `migrator` container applies EF Core migrations to `MedipulseMain` and `MedipulseAudit`.
2. `MockData_InsertOnly.sql` loads the core deterministic fixtures; the service-specific scripts load `User`, `Notification`, and `AuditLog` fixtures.
3. The isolated runner verifies atomic rollback and exact fixture replay before building dbt models.
4. dbt source and model tests validate both databases, including reconciliation of the daily consumption mart with source events.
5. Main CI and scheduled validation use the same runner, including transformation edge cases and an unchanged-source mart rebuild check. Legacy SQL diagnostics remain optional.
6. dbt resolves `mediapulse_main` to `MedipulseMain.dbo` and `mediapulse_audit` to `MedipulseAudit.dbo`, so audit checks execute against the audit database rather than an accidental main-database alias.

The [analytics lineage and rebuild contract](../pipelines/dbt/docs/lineage.md) describes the source-to-mart path, grain, reconciliation, and historical correction behavior.

The scheduled dbt workflow provides a daily/manual rerun, while the main CI integration job runs the dbt gate on every push and pull request path that reaches integration validation.

## Test modes

- `dotnet test Backend.slnx` runs backend unit tests offline using in-memory providers.
- `npm test` and the frontend build/lint/type checks run without SQL Server.
- `Run-IntegrationTests.ps1` validates migrations, seeded data, and HTTP service wiring with Docker and SQL Server.
- `python pipelines/dbt/run_pipeline.py` separately validates database fixtures, transformations, dbt contracts, freshness, and rebuild stability using disposable containers.
