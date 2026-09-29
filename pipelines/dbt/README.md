# MediPulse Data Quality Pipeline

This dbt project is the authoritative data-quality gate for the two SQL Server
databases used by MediPulse.

## Lineage

- `MedipulseMain` is the operational source for identity, facilities, inventory,
  procurement, logistics, telemetry, notifications, and replenishment data.
- `MedipulseAudit` is the operational source for the audit trail.
- `models/schema.yml` declares the source tables and their quality contracts.
- dbt generic tests enforce uniqueness, required fields, accepted values,
  foreign-key relationships, and minimum fixture volumes.
- CI runs `dbt test --no-partial-parse` after migrations and fixture loading.

## Local quality gate

```text
python -m pip install -r pipelines/dbt/requirements.txt
dbt deps --project-dir pipelines/dbt
dbt test --project-dir pipelines/dbt --profiles-dir pipelines/dbt --no-partial-parse
```

The profile is intentionally external to source control. Copy
`profiles.yml.example` to a local `profiles.yml` and provide the SQL Server
connection details for the target environment.
