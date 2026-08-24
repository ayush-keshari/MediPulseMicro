CI Integration Update for Pipeline & Data Quality:

Updated .github/workflows/ci.yml to integrate service-specific SQL data quality checks:

1. Replaced legacy monolithic dq_checks.sql usage in integration-validation job:
   - MedipulseMain checks: Loop through all .sql files in Scripts/DQ/ except AuditService.sql
   - MedipulseAudit check: Run only AuditService.sql against MedipulseAudit database
   - Each service check runs independently, providing granular feedback
   - Overall job fails if any service check fails (non-zero exit count)

2. Preserved existing data-quality job:
   - Continues to run dbt tests against MedipulseMain database
   - Provides complementary validation layer using dbt framework

Benefits:
- Uses our newly created service-specific SQL checks (Inventory, Procurement, Logistics, Telemetry, Notification, Auth, Audit)
- Eliminates ~150 lines of duplicated SQL logic from the old monolithic approach
- Provides more detailed failure reporting (per-service vs aggregate error count)
- Maintains compatibility with existing dbt-based data quality validation
- Aligns CI execution with local PowerShell runner functionality

This completes the Pipeline & Data Quality improvement by:
1. Creating local-executable JSON-based SQL checks (commit 2625f25)
2. Adding documentation for local usage (README update)
3. Integrating into CI pipeline for automated validation (this commit)