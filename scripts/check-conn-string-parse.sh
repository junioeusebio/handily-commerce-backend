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
    echo "FAIL $name: expected success" >&2
    fail=1
    return
  }
  case "$out" in
    "$expect_prefix"*) echo "OK   $name" ;;
    *)
      echo "FAIL $name: unexpected normalized prefix" >&2
      fail=1
      ;;
  esac
}

assert_fail() {
  local name="$1" raw="$2" needle="$3"
  local err
  if err="$(RAW_CONN="$raw" python3 "$PY" 2>&1)"; then
    echo "FAIL $name: expected failure" >&2
    fail=1
    return
  fi
  case "$err" in
    *"$needle"*) echo "OK   $name" ;;
    *)
      echo "FAIL $name: error missing expected hint" >&2
      fail=1
      ;;
  esac
}

assert_ok "host-passthrough" \
  'Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=u;Password=PASSWORD;' \
  'Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;'

assert_ok "uri-encoded-at" \
  'postgresql://user:test%40pass@db.example.com:6543/postgres' \
  'Host=db.example.com;Port=6543;Database=postgres;Username=user;Password=test@pass;'

assert_ok "uri-raw-at-in-password" \
  'postgresql://user:test@pass@db.example.com:6543/postgres' \
  'Host=db.example.com;Port=6543;Database=postgres;Username=user;Password=test@pass;'

assert_ok "uri-raw-specials-in-password" \
  'postgresql://postgres.ref:a@b:c/d?e&f.g@aws-0-sa-east-1.pooler.supabase.com:6543/postgres?sslmode=require' \
  'Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ref;Password=a@b:c/d?e&f.g;'

assert_ok "uri-semicolon-password-quoted" \
  'postgresql://user:p;w"d@db.example.com:5432/postgres' \
  'Host=db.example.com;Port=5432;Database=postgres;Username=user;Password="p;w""d";'

assert_ok "pooler-6543-appends-no-reset" \
  'Host=h.pooler.supabase.com;Port=6543;Database=postgres;Username=u;Password=p;' \
  'Host=h.pooler.supabase.com;Port=6543;Database=postgres;Username=u;Password=p;No Reset On Close=true'

assert_ok "uri-6543-appends-no-reset" \
  'postgresql://u:p@h.pooler.supabase.com:6543/postgres' \
  'Host=h.pooler.supabase.com;Port=6543;Database=postgres;Username=u;Password=p;SSL Mode=Require;Trust Server Certificate=true;No Reset On Close=true'

assert_exact() {
  local name="$1" raw="$2" expected="$3" flag="${4:-}"
  local out
  out="$(RAW_CONN="$raw" python3 "$PY" $flag)"
  if [[ "$out" == "$expected" ]]; then
    echo "OK   $name"
  else
    echo "FAIL $name: unexpected output" >&2
    fail=1
  fi
}

assert_exact "session-5432-unchanged" \
  'Host=h.pooler.supabase.com;Port=5432;Database=postgres;Username=u;Password=p' \
  'Host=h.pooler.supabase.com;Port=5432;Database=postgres;Username=u;Password=p'

assert_exact "6543-already-set-unchanged" \
  'Host=h;Port=6543;Password=p;No Reset On Close=false' \
  'Host=h;Port=6543;Password=p;No Reset On Close=false'

assert_exact "describe-no-secrets" \
  'Host=h.pooler.supabase.com;Port=6543;Database=postgres;Username=secretuser;Password=s3cr3t;SSL Mode=Require' \
  'Target: host=h.pooler.supabase.com port=6543 (Supavisor transaction pooler); SSL Mode=Require; No Reset On Close=yes' \
  --describe

assert_fail "uri-missing-port" \
  'postgresql://user:pw@db.example.com/postgres' \
  'could not be parsed'

assert_fail "uri-placeholder" \
  'postgresql://user:YOUR_PASSWORD@db.example.com:6543/postgres' \
  'placeholder'

assert_mask() {
  local name="$1" raw="$2" expected="$3"
  local out
  out="$(RAW_CONN="$raw" python3 "$PY" --emit-mask)"
  if [[ "$out" == "::add-mask::$expected" ]]; then
    echo "OK   $name"
  else
    echo "FAIL $name: unexpected mask output" >&2
    fail=1
  fi
}

assert_mask "mask-uri" 'postgresql://user:te@st@db.example.com:6543/postgres' 'te@st'
assert_mask "mask-host-format" 'Host=h;Port=1;Database=d;Username=u;Password=s3cr@t;SSL Mode=Require' 's3cr@t'
assert_mask "mask-host-quoted" 'Host=h;Password="a;b""c";Database=d' 'a;b"c'

if [[ "$fail" -ne 0 ]]; then
  echo "check-conn-string-parse: FAILED" >&2
  exit 1
fi
echo "check-conn-string-parse: all OK"
