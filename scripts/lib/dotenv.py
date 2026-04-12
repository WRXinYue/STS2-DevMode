"""Minimal .env loader (KEY=VALUE into os.environ)."""

from __future__ import annotations

import os
from pathlib import Path


def load_dotenv(path: Path | None) -> None:
    if not path or not path.is_file():
        return
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in raw:
            continue
        key, _, rest = raw.partition("=")
        key = key.strip()
        if not key:
            continue
        value = rest.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        os.environ[key] = value
