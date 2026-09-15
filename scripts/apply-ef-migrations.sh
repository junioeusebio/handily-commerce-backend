#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${RAW_CONN:-}" ]]; then
  echo "Missing env RAW_CONN (from secret CONNECTIONSTRINGS_DEFAULT)"
  exit 1
fi

export ConnectionStrings__Default
ConnectionStrings__Default="$(python3 - <<'PY'
import os, re
raw = os.environ["RAW_CONN"].strip()
if re.match(r"(?i)^postgres(ql)?://", raw):
    m = re.match(r"(?i)^postgres(?:ql)?://([^:/]+):([^@]+)@([^:/]+):(\d+)/([^?\s]+)", raw)
    if not m:
        raise SystemExit("URI connection string could not be parsed")
    user, password, host, port, db = m.groups()
    placeholders = {"[YOUR-PASSWORD]", "YOUR_PASSWORD", "YOUR-PASSWORD", "SUA_SENHA"}
    if password in placeholders or (password.startswith("[") and "PASSWORD" in password.upper()):
        raise SystemExit(
            "Secret still contains a password placeholder — set the real DB password in CONNECTIONSTRINGS_DEFAULT"
        )
    print(
        f"Host={host};Port={port};Database={db};Username={user};Password={password};"
        "SSL Mode=Require;Trust Server Certificate=true",
        end="",
    )
else:
    if "[YOUR-PASSWORD]" in raw or "YOUR_PASSWORD" in raw:
        raise SystemExit(
            "Secret still contains a password placeholder — set the real DB password in CONNECTIONSTRINGS_DEFAULT"
        )
    print(raw, end="")
PY
)"

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
