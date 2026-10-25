# MediPulse Data Quality Pipeline

This dbt project is the authoritative data-quality gate for the two SQL Server
databases used by MediPulse.

It also builds `dbo.fct_daily_consumption`, a daily facility/item demand dataset
from logistics consumption events. It retains event counts and uses 64-bit sums.
See [lineage and historical rebuilds](docs/lineage.md) for its grain, timezone
limitation, source reconciliation, and recovery procedure.
The [bounded journal processor](docs/consumption-change-capture.md) maintains a
separate incremental table with per-event acknowledgements and retry support.
Its [reconciliation and date-window repair](docs/consumption-change-capture.md#date-window-reconciliation-and-repair)
can detect and rebuild drift against retained source rows during a quiescent window.

## One-command disposable pipeline

From the repository root:

```text
python pipelines/dbt/run_pipeline.py
```

Requirements: Python 3.10+, Docker Engine using Linux containers, Compose
2.24.4+, internet access for the first image/package downloads, and sufficient
Docker disk space. No host .NET SDK, dbt, ODBC driver, SQL Server, password,
or profile is required. The images contain the migration and validation tools.

Each run gets a unique Compose project, a generated temporary password, and
fresh databases with no host ports. The checkout is read-only inside containers;
migration build outputs and dbt working files remain container-local. After
migrations, the runner loads fixtures atomically, tests cross-database rollback,
verifies replay preserves rows, then builds and tests dbt models and freshness.

CI and the scheduled workflow execute this exact command. It returns nonzero
if any stage, required report, or cleanup fails. Reports and redacted stage logs
remain under `target/pipeline/<run-id>/`:

- `pipeline-summary.json`: stage status, duration, project name, cleanup result.
- `validation-summary.json`: fixture/atomicity/replay/dbt stage status.
- `atomicity-tests.txt` and `fixture-replay.json`: database-backed evidence.
- `consumption-journal-tests.txt`: eight SQL Server change-capture checks;
  see [the journal contract](docs/consumption-change-capture.md).
- `incremental-consumption.json` and `incremental-consumption-tests.txt`:
  full-rebuild comparison, repeat-run evidence, and seven live recovery cases.
- `transformation-tests.txt`: seven real-SQL tests for aggregation, date boundaries,
  late arrivals, corrections/deletions, group isolation, overflow, and empty input.
- `mart-rebuild.json` and `mart-rebuild-{run,test}.json`: exact output comparison
  and dbt results after a second materialization with unchanged sources.
- `target/quality-gate/<run-id>/`: separate dbt model/test/freshness results.

`--output-dir` changes the parent report directory; each run still uses a new
subdirectory. `--timeout-seconds` controls each build/run stage (default 1200).
Logs are saved per stage, so a long image build can be inspected in `build.log`
after completion or timeout. No unrelated Docker resources are pruned. Images
and build cache are retained; database containers, networks, and volumes are
removed in a `finally` block, including after a handled keyboard interrupt.
Forced process termination or host shutdown cannot guarantee cleanup. For an
interrupted run, use the exact project name from its report directory/logs:

```text
docker compose --project-name <recorded-project-name> -f docker-compose.ci.yml down --volumes --remove-orphans
```

Never substitute the project name of another development stack. Re-run the
pipeline to recover with new databases; it does not repair existing partial
fixtures or perform production backfills. This quality pipeline is distinct
from the full application's API integration suite.

## CI validation levels

The pull-request pipeline runs `python pipelines/dbt/quality_gate.py --parse-only`
as a database-independent validation. It parses the project and verifies that
every generic test resolves to a discovered macro, then compiles all nodes with
database introspection and relation-cache population disabled. Parsing alone does not catch
all undefined test macros. The runner regression tests also execute in this job.

The authoritative `dbt test` gate runs after SQL Server migrations and
repository mock data have been loaded. CI, scheduled runs, and local runs share
`quality_gate.py`: parse and validate contracts, materialize models, test, then
check inventory freshness. Every failed stage stops subsequent stages.

## Lineage

- `MedipulseMain` is the operational source for identity, facilities, inventory,
  procurement, logistics, telemetry, notifications, and replenishment data.
- `MedipulseAudit` is the operational source for the audit trail.
- `models/schema.yml` declares the source tables and their quality contracts.
- dbt generic tests enforce uniqueness, required fields, accepted values,
  foreign-key relationships, non-negative movement quantities, and minimum
  fixture volumes. Inventory staging also rejects duplicate item/lot/location
  business keys.
- Bidirectional reconciliation checks compare every selected staging column
  with its source, detecting missing, unexpected, and changed rows. Existing
  uniqueness tests additionally detect duplicate rows (EXCEPT uses set semantics).

## Local quality gate

```text
python -m pip install -r pipelines/dbt/requirements.txt
python pipelines/dbt/quality_gate.py --profiles-dir /path/to/profile-directory
```

The profile is intentionally external to source control. Copy
`profiles.yml.example` to a local `profiles.yml` and provide the SQL Server
connection details for the target environment.

The default profile directory is `DBT_PROFILES_DIR` or `~/.dbt`. For a targeted
recovery check, add `--select inventory_positions` (the model name, not its SQL
alias `stg_inventory_positions`). An empty selection fails before materialization.
Inventory freshness is still checked unless `--skip-freshness` is explicit.

Each invocation writes a separate `target/quality-gate/<run-id>/` containing
the manifest, model results, test results, freshness results, and a summary.
The summary records failure even if execution stops midway. CI uploads this
directory with the other dbt artifacts. Missing or empty result artifacts fail
the gate rather than reusing a previous run's success.

Offline regression tests (after installing requirements):

```text
python -m unittest discover -s pipelines/dbt/runner_tests -v
```

These tests cover stage failures, missing macros, namespace resolution, empty
selections, missing evidence, and reconciliation mutations. They complement
SQL Server integration tests; they do not replace them. This runner rebuilds
derived views and validates operational data. It does not ingest historical
records or implement a transactional source-data backfill.
