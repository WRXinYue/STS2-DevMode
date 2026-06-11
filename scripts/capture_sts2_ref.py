#!/usr/bin/env python3
"""Copy sts2.dll (+ Harmony) from current Steam install into eng/sts2-refs/."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_SCRIPT_DIR = Path(__file__).resolve().parent
if str(_SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(_SCRIPT_DIR))

from lib.dotenv import load_dotenv  # noqa: E402
from lib.steam import resolve_sts2_dir  # noqa: E402
from lib.sts2_profiles import (  # noqa: E402
    capture_profile_ref,
    pinned_version,
    resolve_sts2_dll,
)


def main() -> int:
    ap = argparse.ArgumentParser(
        description="Capture STS2 compile refs into eng/sts2-refs/<profile>/<version>/."
    )
    ap.add_argument("profile", choices=("stable", "beta"))
    ap.add_argument(
        "--repo-root",
        type=Path,
        default=_SCRIPT_DIR.parent,
    )
    ap.add_argument(
        "--source",
        type=Path,
        default=None,
        help="Steam install root (default: STS2_DIR / auto-detect)",
    )
    args = ap.parse_args()

    load_dotenv(args.repo_root / ".env")
    source = args.source
    if source is None:
        source = resolve_sts2_dir()
    if source is None:
        print("STS2 install not found. Set STS2_DIR in .env.", file=sys.stderr)
        return 1

    try:
        dest = capture_profile_ref(
            args.profile,
            repo_root=args.repo_root,
            source_root=source,
        )
    except RuntimeError as ex:
        print(str(ex), file=sys.stderr)
        return 1

    dll = resolve_sts2_dll(dest)
    print(f"Captured {args.profile} ref (pinned {pinned_version(args.profile)})")
    print(f"  source={source.resolve()}")
    print(f"  dest={dest}")
    print(f"  dll={dll}")
    print("")
    print("Next: git add eng/sts2-refs && git commit (Git LFS tracks *.dll under refs).")
    print(f"Switch Steam branch and run again for the other profile if needed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
