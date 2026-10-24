"""Run the complete disposable quality pipeline with Python and Docker only."""
import argparse
import json
import os
from pathlib import Path
import secrets
import subprocess
import time
import uuid


ROOT = Path(__file__).resolve().parents[2]


def run_pipeline(root=ROOT, output_root=None, timeout=1200):
    run_id = uuid.uuid4().hex
    project = "medipulse-quality-" + run_id
    evidence = ((output_root or root / "pipelines/dbt/target/pipeline") / run_id).resolve()
    evidence.mkdir(parents=True)
    password = "Mp!" + secrets.token_hex(24) + "9aA"
    env = {**os.environ, "SA_PASSWORD": password, "QUALITY_OUTPUT": str(evidence),
           "COMPOSE_PARALLEL_LIMIT": "1"}
    compose = ["docker", "compose", "--project-name", project,
               "-f", str(root / "docker-compose.ci.yml"),
               "-f", str(root / "docker-compose.quality.yml")]
    summary = {"status": "running", "project": project, "stages": [],
               "evidence": str(evidence), "cleanup": "not-needed"}
    started = time.monotonic()
    stage = "preflight"
    cleanup_needed = False

    def save_summary():
        summary["duration_seconds"] = round(time.monotonic() - started, 2)
        pending = evidence / "pipeline-summary.tmp"
        pending.write_text(json.dumps(summary, indent=2), encoding="utf-8")
        pending.replace(evidence / "pipeline-summary.json")

    def command(name, args, limit=timeout):
        summary["active_stage"] = name
        save_summary()
        print(f"==> {name}", flush=True)
        try:
            result = subprocess.run(args, cwd=root, env=env, text=True,
                                    encoding="utf-8", errors="replace",
                                    stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                    timeout=limit, check=False)
            output = result.stdout
        except subprocess.TimeoutExpired as exc:
            output = exc.stdout or ""
            if isinstance(output, bytes):
                output = output.decode("utf-8", errors="replace")
            (evidence / f"{name}.log").write_text(output.replace(password, "[REDACTED]"), encoding="utf-8")
            raise
        (evidence / f"{name}.log").write_text(output.replace(password, "[REDACTED]"), encoding="utf-8")
        if result.returncode:
            raise RuntimeError(f"{name} failed with exit code {result.returncode}")
        return output

    try:
        command(stage, ["docker", "info", "--format", "{{.ServerVersion}}"], 30)
        stage = "configuration"
        config = json.loads(command(stage, [*compose, "config", "--format", "json"], 30))
        if any(service.get("ports") for service in config["services"].values()):
            raise ValueError("Isolated quality services must not publish host ports")
        for service in ("migrator", "quality"):
            mounts = config["services"][service]["volumes"]
            if not any(m.get("target") == "/workspace" and m.get("read_only") for m in mounts):
                raise ValueError(f"{service}: source checkout must be read-only")
        cleanup_needed = True
        for stage, args in (
            ("build", ["build", "migrator", "quality"]),
            ("database", ["up", "-d", "--wait", "--wait-timeout", "180", "sqlserver"]),
            ("migrations", ["run", "--rm", "--no-deps", "migrator"]),
            ("validation", ["run", "--rm", "--no-deps", "quality"]),
        ):
            command(stage, [*compose, *args])
            summary["stages"].append(stage)
        validation = json.loads((evidence / "validation-summary.json").read_text(encoding="utf-8"))
        if validation.get("status") != "passed":
            raise ValueError("Validation did not produce a passing summary")
        summary["status"] = "passed"
    except (Exception, KeyboardInterrupt) as exc:
        summary.update(status="failed", failed_stage=stage, error=type(exc).__name__)
    finally:
        if cleanup_needed:
            try:
                command("containers", [*compose, "logs", "--no-color", "--tail", "200"], 60)
            except Exception:
                summary["log_collection"] = "failed"
            try:
                command("cleanup", [*compose, "down", "--volumes", "--remove-orphans"], 120)
                summary["cleanup"] = "passed"
            except Exception:
                summary.update(status="failed", cleanup="failed")
        save_summary()
        print(f"Pipeline {summary['status']}. Reports: {evidence}", flush=True)
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, help="Parent directory for unique run reports")
    parser.add_argument("--timeout-seconds", type=int, default=1200, help="Timeout per build/run stage")
    args = parser.parse_args()
    if args.timeout_seconds < 1:
        parser.error("--timeout-seconds must be positive")
    result = run_pipeline(output_root=args.output_dir, timeout=args.timeout_seconds)
    raise SystemExit(0 if result["status"] == "passed" else 1)
