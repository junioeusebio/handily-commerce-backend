#!/usr/bin/env bash
# Tiny unit-less checks for scripts/parse-conn-string.py (no secrets logged).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PY="$ROOT/scripts/parse-conn-string.py"
fail=0

assert_ok() {
  local name="$1" raw="$2" expect_prefix="$3"
  local out
  out="$(RAW_CONN="$raw" python3 "$PY")" || {
    echo "FAIL $name: expected success"
    fail=1
    return
  }
  case "$out" in
    "$expect_prefix"*) echo "OK   $name" ;;
    *)
      echo "FAIL $name: unexpected normalized prefix"
      fail=1
      ;;
  esac
}

assert_fail() {
  local name="$1" raw="$2" needle="$3"
  local err
  if err="$(RAW_CONN="$raw" python3 "$PY" 2>&1)"; then
    echo "FAIL $name: expected failure"
    fail=1
    return
  fi
  case "$err" in
    *"$needle"*) echo "OK   $name" ;;
    *)
      echo "FAIL $name: error missing expected hint"
      fail=1
      ;;
  esac
}

assert_ok "host-passthrough" \
  'Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=u;Password=s3cret;' \
  'Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;'

assert_ok "uri-encoded-at" \
  'postgresql://user:p%40ss@db.example.com:6543/postgres' \
  'Host=db.example.com;Port=6543;Database=postgres;Username=user;Password=p@ss;'

assert_fail "uri-raw-at-in-password" \
  'postgresql://user:p@ss@db.example.com:6543/postgres' \
  "unescaped '@'"

assert_fail "uri-placeholder" \
  'postgresql://user:YOUR_PASSWORD@db.example.com:6543/postgres' \
  'placeholder'

if [[ "$fail" -ne 0 ]]; then
  echo "check-conn-string-parse: FAILED"
  exit 1
fi
echo "check-conn-string-parse: all OK"
