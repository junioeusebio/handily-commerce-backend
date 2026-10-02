#!/usr/bin/env python3
"""Normalize CONNECTIONSTRINGS_DEFAULT / RAW_CONN for Npgsql.

- Host=… key=value strings are returned as-is.
- postgresql:// URIs are converted to Host=… form. The user-info / host boundary is the
  LAST '@' (host names never contain '@'), so passwords with raw '@', ':', '/' or '?' work.
  Percent-encoded values (e.g. '%40') are still decoded.
- Supabase Transaction pooler (port 6543): `No Reset On Close=true` is appended when missing.
  Npgsql's pool reset (DISCARD ALL) makes reused connections hang behind a transaction-mode pooler.
- `--describe` prints a non-secret summary (host, port, pooler mode, SSL mode) for CI logs.
- `--emit-mask` prints a GitHub Actions `::add-mask::` line for the password instead of the
  connection string, so fragments never show up in logs (e.g. inside Npgsql error messages).
Never prints the raw secret beyond writing the normalized string to stdout
(callers must not log stdout in CI).
"""
from __future__ import annotations

import os
import re
import sys
from urllib.parse import unquote, urlsplit

_PLACEHOLDER_HINT = (
    "Secret still contains a password placeholder — set the real DB password in CONNECTIONSTRINGS_DEFAULT"
)
TRANSACTION_POOLER_PORT = 6543
_NPGSQL_PORT = re.compile(r"(?i)(?:^|;)\s*port\s*=\s*\"?(\d+)")
_NPGSQL_HOST = re.compile(r"(?i)(?:^|;)\s*(?:host|server)\s*=\s*\"?([^;\"]*)")
_NPGSQL_SSL = re.compile(r"(?i)(?:^|;)\s*ssl\s*mode\s*=\s*\"?([^;\"]*)")
_NO_RESET_ON_CLOSE = re.compile(r"(?i)(?:^|;)\s*no\s*reset\s*on\s*close\s*=")
_NPGSQL_PASSWORD = re.compile(r"(?i)(?:^|;)\s*(?:password|pwd)\s*=\s*(\"(?:[^\"]|\"\")*\"|[^;]*)")


def reject_placeholder(text: str) -> None:
    if "[YOUR-PASSWORD]" in text or "YOUR_PASSWORD" in text:
        raise SystemExit(_PLACEHOLDER_HINT)


def _npgsql_value(value: str) -> str:
    """Quote a key=value connection-string value when it contains separators or quotes."""
    if value != value.strip() or any(ch in value for ch in ';="\''):
        return '"' + value.replace('"', '""') + '"'
    return value


def _parse_uri(raw: str) -> tuple[str, str, str, int | None, str]:
    rest = raw.split("://", 1)[1]
    if "@" not in rest:
        raise SystemExit(
            "URI connection string has no user:password@ part. "
            "Prefer Npgsql Host=… format or postgresql://user:password@host:port/db."
        )
    userinfo, hostpart = rest.rsplit("@", 1)
    user, _, password = userinfo.partition(":")
    location = urlsplit("//" + hostpart)
    try:
        port = location.port
    except ValueError:
        port = None
    db = unquote(location.path.lstrip("/").split("/")[0]) if location.path else ""
    return unquote(user), unquote(password), location.hostname or "", port, db


def _uri_to_npgsql(raw: str) -> str:
    user, password, host, port, db = _parse_uri(raw)
    if not host or not port or not user or not password or not db:
        raise SystemExit(
            "URI connection string could not be parsed. "
            "Prefer Npgsql Host=… format, or a full postgresql://user:password@host:port/db URI."
        )
    reject_placeholder(password)
    if password.startswith("[") and "PASSWORD" in password.upper():
        raise SystemExit(_PLACEHOLDER_HINT)
    return (
        f"Host={host};Port={port};Database={_npgsql_value(db)};Username={_npgsql_value(user)};"
        f"Password={_npgsql_value(password)};SSL Mode=Require;Trust Server Certificate=true"
    )


def _is_uri(raw: str) -> bool:
    return re.match(r"(?i)^postgres(ql)?://", raw) is not None


def _port_of(conn: str) -> int | None:
    match = _NPGSQL_PORT.search(conn)
    return int(match.group(1)) if match else None


def for_pooler(conn: str) -> str:
    """Append No Reset On Close=true for the Supabase Transaction pooler (6543) when missing."""
    if _port_of(conn) != TRANSACTION_POOLER_PORT or _NO_RESET_ON_CLOSE.search(conn):
        return conn
    return conn.rstrip().rstrip(";") + ";No Reset On Close=true"


def describe(conn: str) -> str:
    """Non-secret summary of the normalized connection string (no user, no password)."""
    host_match = _NPGSQL_HOST.search(conn)
    ssl_match = _NPGSQL_SSL.search(conn)
    host = host_match.group(1).strip() if host_match else "?"
    port = _port_of(conn) or 5432
    if port == TRANSACTION_POOLER_PORT:
        mode = "Supavisor transaction pooler"
    elif host.endswith(".pooler.supabase.com"):
        mode = "Supavisor session pooler"
    else:
        mode = "direct/other"
    ssl = ssl_match.group(1).strip() if ssl_match else "default"
    no_reset = "yes" if _NO_RESET_ON_CLOSE.search(conn) else "no"
    return f"Target: host={host} port={port} ({mode}); SSL Mode={ssl}; No Reset On Close={no_reset}"


def normalize(raw: str) -> str:
    return for_pooler(_normalize(raw))


def _normalize(raw: str) -> str:
    raw = raw.strip()
    if not raw:
        raise SystemExit("Missing env RAW_CONN (from secret CONNECTIONSTRINGS_DEFAULT)")

    # Prefer Npgsql key=value (Host=…) as-is — no URI conversion.
    if re.match(r"(?i)^host\s*=", raw):
        reject_placeholder(raw)
        return raw

    if _is_uri(raw):
        return _uri_to_npgsql(raw)

    reject_placeholder(raw)
    return raw


def password_of(raw: str) -> str:
    """Best-effort password extraction (URI or key=value) for log masking only."""
    raw = raw.strip()
    if _is_uri(raw) and "@" in raw.split("://", 1)[1]:
        return _parse_uri(raw)[1]
    match = _NPGSQL_PASSWORD.search(raw)
    if not match:
        return ""
    value = match.group(1).strip()
    if len(value) >= 2 and value.startswith('"') and value.endswith('"'):
        value = value[1:-1].replace('""', '"')
    return value


def main() -> None:
    raw = os.environ.get("RAW_CONN", "")
    if "--emit-mask" in sys.argv[1:]:
        password = password_of(raw)
        if password:
            sys.stdout.write(f"::add-mask::{password}\n")
        return
    if "--describe" in sys.argv[1:]:
        sys.stdout.write(describe(normalize(raw)) + "\n")
        return
    sys.stdout.write(normalize(raw))


if __name__ == "__main__":
    main()
