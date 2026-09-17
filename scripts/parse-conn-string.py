#!/usr/bin/env python3
"""Normalize CONNECTIONSTRINGS_DEFAULT / RAW_CONN for Npgsql.

- Host=… key=value strings are returned as-is.
- postgresql:// URIs are converted to Host=… form via urllib.parse.
- Unescaped '@' in a URI password fails with a clear message (use Host=
  format or percent-encode '@' as '%40').
Never prints the raw secret beyond writing the normalized string to stdout
(callers must not log stdout in CI).
"""
from __future__ import annotations

import os
import re
import sys
from urllib.parse import unquote, urlparse


def reject_placeholder(text: str) -> None:
    if "[YOUR-PASSWORD]" in text or "YOUR_PASSWORD" in text:
        raise SystemExit(
            "Secret still contains a password placeholder — set the real DB password in CONNECTIONSTRINGS_DEFAULT"
        )


def _reject_unescaped_at_in_uri(raw: str) -> None:
    after_scheme = raw.split("://", 1)[1]
    authority = after_scheme.split("/", 1)[0].split("?", 1)[0]
    # Unescaped '@' in password → more than one '@' in authority.
    # Percent-encoded '%40' is fine.
    if authority.count("@") > 1:
        raise SystemExit(
            "URI password appears to contain an unescaped '@'. "
            "Use Npgsql Host=…;Port=…;Database=…;Username=…;Password=…;SSL Mode=Require;… "
            "for CONNECTIONSTRINGS_DEFAULT (preferred for GitHub secret and Render), "
            "or percent-encode the password ('@' → '%40')."
        )


def _uri_to_npgsql(raw: str) -> str:
    _reject_unescaped_at_in_uri(raw)
    parsed = urlparse(raw)
    user = unquote(parsed.username or "")
    password = unquote(parsed.password or "")
    host = parsed.hostname
    port = parsed.port
    db = unquote((parsed.path or "").lstrip("/").split("/")[0]) if parsed.path else ""
    if not host or not port or not user or not password or not db:
        raise SystemExit(
            "URI connection string could not be parsed. "
            "Prefer Npgsql Host=… format, or a full postgresql://user:password@host:port/db URI "
            "with a percent-encoded password if it contains '@'."
        )
    reject_placeholder(password)
    if password.startswith("[") and "PASSWORD" in password.upper():
        raise SystemExit(
            "Secret still contains a password placeholder — set the real DB password in CONNECTIONSTRINGS_DEFAULT"
        )
    return (
        f"Host={host};Port={port};Database={db};Username={user};Password={password};"
        "SSL Mode=Require;Trust Server Certificate=true"
    )


def normalize(raw: str) -> str:
    raw = raw.strip()
    if not raw:
        raise SystemExit("Missing env RAW_CONN (from secret CONNECTIONSTRINGS_DEFAULT)")

    # Prefer Npgsql key=value (Host=…) as-is — no URI conversion.
    if re.match(r"(?i)^host\s*=", raw):
        reject_placeholder(raw)
        return raw

    if re.match(r"(?i)^postgres(ql)?://", raw):
        return _uri_to_npgsql(raw)

    reject_placeholder(raw)
    return raw


def main() -> None:
    raw = os.environ.get("RAW_CONN", "")
    sys.stdout.write(normalize(raw))


if __name__ == "__main__":
    main()
