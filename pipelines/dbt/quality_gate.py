"""Shared local/CI dbt gate; never modifies operational source rows."""

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import uuid


def validate_manifest(manifest):
    macros = list(manifest.get("macros", {}).values())
    nodes = list(manifest.get("nodes", {}).values())
    tests = [node for node in nodes if node.get("resource_type") == "test"]
    if not tests:
        raise ValueError("No data tests found in the parsed manifest")
    missing = []
    for test in tests:
        metadata = test.get("test_metadata")
        if not metadata:
            continue  # Singular tests have SQL bodies instead of generic macros.
        name = "test_" + metadata["name"]
        namespace = metadata.get("namespace")
        if not any(m.get("name") == name and
                   (not namespace or m.get("package_name") == namespace)
                   for m in macros):
            missing.append(f"{test['name']}: {name}")
    if missing:
        raise ValueError("Missing generic test macros: " + "; ".join(missing))
    return {"models": sum(n.get("resource_type") == "model" for n in nodes),
            "tests": len(tests), "sources": len(manifest.get("sources", {}))}


def run_gate(project, profiles, selector="", parse_only=False,
             skip_run=False, skip_freshness=False):
    target = project / "target"
    evidence = target / "quality-gate" / uuid.uuid4().hex
    evidence.mkdir(parents=True)
    summary = {"status": "running", "selector": selector, "stages": []}
    common = ["--project-dir", str(project), "--profiles-dir", str(profiles),
              "--target-path", str(target), "--no-partial-parse"]

    def invoke(stage, command, artifact=None):
        print(f"==> {stage}", flush=True)
        # Remove stale results so a successful command cannot reuse old evidence.
        if artifact:
            (target / artifact).unlink(missing_ok=True)
        subprocess.run(["dbt", *command, *common], check=True)
        if artifact:
            data = json.loads((target / artifact).read_text(encoding="utf-8"))
            if artifact != "manifest.json":
                results = data.get("results", [])
                if not results or any(r.get("status") not in
                                      {"success", "pass", "warn"} for r in results):
                    raise ValueError(f"{stage}: missing or unsuccessful results")
            shutil.copy2(target / artifact, evidence / f"{stage}.json")
        summary["stages"].append(stage)

    try:
        invoke("parse", ["parse"], "manifest.json")
        manifest = json.loads((target / "manifest.json").read_text(encoding="utf-8"))
        summary["inventory"] = validate_manifest(manifest)
        if parse_only:
            invoke("compile", ["compile", "--no-introspect", "--no-populate-cache"],
                   "run_results.json")
        else:
            selected = ["--select", selector] if selector else []
            if selector:
                listing = subprocess.run(
                    ["dbt", "--quiet", "ls", *common, *selected,
                     "--resource-type", "model", "--output", "json"],
                    check=True, capture_output=True, text=True)
                models = [json.loads(line) for line in listing.stdout.splitlines()
                          if line.strip()]
                if not models:
                    raise ValueError(f"Selector matches no models: {selector}")
            if not skip_run:
                invoke("build-models", ["run", *selected], "run_results.json")
            invoke("test", ["test", *selected], "run_results.json")
            if not skip_freshness:
                invoke("freshness", ["source", "freshness", "--select",
                                     "source:mediapulse_main.InventoryPositions"],
                       "sources.json")
        summary["status"] = "passed"
    except Exception as exc:
        summary["status"] = "failed"
        summary["error"] = str(exc)
        raise
    finally:
        (evidence / "summary.json").write_text(
            json.dumps(summary, indent=2), encoding="utf-8")
        print(f"Quality gate evidence: {evidence}", flush=True)
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-dir", type=Path, default=Path(__file__).resolve().parent)
    parser.add_argument("--profiles-dir", type=Path,
                        default=Path(os.environ.get("DBT_PROFILES_DIR", Path.home() / ".dbt")))
    parser.add_argument("--select", default="")
    parser.add_argument("--parse-only", action="store_true")
    parser.add_argument("--skip-run", action="store_true")
    parser.add_argument("--skip-freshness", action="store_true")
    args = parser.parse_args()
    run_gate(args.project_dir.resolve(), args.profiles_dir.resolve(), args.select,
             args.parse_only, args.skip_run, args.skip_freshness)
