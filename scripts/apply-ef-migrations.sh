#!/usr/bin/env bash
# Apply EF migrations using CONNECTIONSTRINGS_DEFAULT (via RAW_CONN).
# Never prints the connection string or password.
# Parse/normalize: scripts/parse-conn-string.py
# Manual checks: bash scripts/check-conn-string-parse.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -z "${RAW_CONN:-}" ]]; then
  echo "Missing env RAW_CONN (from secret CONNECTIONSTRINGS_DEFAULT)"
  exit 1
fi

export ConnectionStrings__Default
ConnectionStrings__Default="$(RAW_CONN="$RAW_CONN" python3 "$ROOT/scripts/parse-conn-string.py")"

echo "Connection string loaded (value not printed)."
if [[ "${ConnectionStrings__Default,,}" == host=* ]]; then
  echo "Format: Host=…"
else
  echo "Format: converted/other"
fi

dotnet ef database update \
  --project src/HandilyCommerce.Infrastructure \
  --startup-project src/HandilyCommerce.Api \
  --connection "$ConnectionStrings__Default"

echo "Migration finished."
