# Releases

MediPulseMicro uses semantic version tags for repository milestones. Each release should include:

1. A focused conventional-commit history with passing build and test checks.
2. A dated entry in `CHANGELOG.md` describing user-visible and operational changes.
3. An annotated Git tag such as `v0.2.0` pointing at the verified commit.

## Current release

`v0.2.0` includes the reproducible CI/data-quality gate, service-owned lineage documentation, structured JSON logging, Prometheus-compatible request metrics, and split backend test suites.

## Verification

Before creating a release tag, run:

```text
dotnet test Backend.slnx --no-restore
dotnet format Backend.slnx --verify-no-changes --no-restore
docker compose config --quiet
```
