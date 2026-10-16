#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
if ! command -v pwsh >/dev/null 2>&1; then
  echo "Required command 'pwsh' was not found on PATH." >&2
  exit 127
fi
exec pwsh -NoProfile -File "$SCRIPT_DIR/test-all.ps1" "$@"
