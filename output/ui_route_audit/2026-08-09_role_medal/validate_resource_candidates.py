#!/usr/bin/env python3
"""Read-only validator for the medal resource candidate closure."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import sys


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    manifest = json.loads((HERE / "resource-candidates.json").read_text(encoding="utf-8"))
    source_root = Path(manifest["source_root"])
    unity_root = REPO / manifest["unity_root"]
    failures: list[str] = []
    destination_missing = 0
    for item in manifest["candidates"]:
        rel = Path(item["path"])
        source = source_root / rel
        destination = unity_root / rel
        if not source.is_file():
            failures.append(f"source-missing {rel.as_posix()}")
            continue
        actual = sha256(source)
        if actual != item["sha256"]:
            failures.append(f"source-hash {rel.as_posix()} expected={item['sha256']} actual={actual}")
        if not destination.is_file():
            destination_missing += 1
        elif sha256(destination) != item["sha256"]:
            failures.append(f"destination-diverged {rel.as_posix()}")
    print(json.dumps({
        "candidate_count": len(manifest["candidates"]),
        "source_failures": failures,
        "destination_missing": destination_missing,
        "mutation_performed": False,
    }, ensure_ascii=False, indent=2))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
