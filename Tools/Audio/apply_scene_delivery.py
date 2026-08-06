#!/usr/bin/env python3
"""Apply or verify the approved scene-music delivery without touching .meta/Addressables."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from pathlib import Path


MANIFEST = Path("Schemas/Audio/scene_delivery_overrides.json")
SCENE_ROOT = Path("Assets/GameRes/resource/sound/scene")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def ensure_inside(path: Path, root: Path) -> Path:
    resolved = path.resolve()
    try:
        resolved.relative_to(root.resolve())
    except ValueError as exc:
        raise RuntimeError(f"target escapes scene root: {resolved}") from exc
    return resolved


def verify_content(path: Path, expected_bytes: int, expected_sha: str, label: str) -> None:
    if not path.is_file():
        raise RuntimeError(f"{label} missing: {path}")
    actual_bytes = path.stat().st_size
    actual_sha = sha256(path)
    if actual_bytes != expected_bytes or actual_sha != expected_sha:
        raise RuntimeError(
            f"{label} content mismatch: {path} "
            f"expected={expected_bytes}/{expected_sha} actual={actual_bytes}/{actual_sha}"
        )


def run(args: argparse.Namespace) -> int:
    unity_root = (
        Path(args.unity_root).resolve()
        if args.unity_root
        else Path(__file__).resolve().parents[2]
    )
    manifest_path = unity_root / MANIFEST
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("schemaVersion") != 1:
        raise RuntimeError("unsupported scene delivery override schema")

    delivery_root = Path(args.delivery_root or manifest["deliveryRoot"]).resolve()
    scene_root = (unity_root / SCENE_ROOT).resolve()
    replacements = manifest.get("replacements", [])
    if not replacements:
        raise RuntimeError("scene delivery replacement list is empty")

    seen_logical: set[str] = set()
    meta_before: dict[Path, str] = {}
    for row in replacements:
        logical = str(row.get("logicalName") or "").strip()
        if not logical or logical in seen_logical:
            raise RuntimeError(f"invalid/duplicate logicalName: {logical!r}")
        seen_logical.add(logical)

        source = (delivery_root / str(row["deliveryRelative"])).resolve()
        target = ensure_inside(unity_root / str(row["target"]), scene_root)
        expected_bytes = int(row["bytes"])
        expected_sha = str(row["sha256"]).lower()
        verify_content(source, expected_bytes, expected_sha, "delivery source")
        if not target.is_file():
            raise RuntimeError(f"tracked Unity target missing: {target}")
        meta = Path(str(target) + ".meta")
        if not meta.is_file():
            raise RuntimeError(f"target .meta missing: {meta}")

        if args.check:
            verify_content(target, expected_bytes, expected_sha, "Unity target")
            continue

        meta_before[meta] = sha256(meta)
        shutil.copyfile(source, target)
        verify_content(target, expected_bytes, expected_sha, "Unity target after copy")
        if sha256(meta) != meta_before[meta]:
            raise RuntimeError(f"target .meta changed unexpectedly: {meta}")

    for row in manifest.get("unmapped", []):
        source = (delivery_root / str(row["deliveryRelative"])).resolve()
        verify_content(source, int(row["bytes"]), str(row["sha256"]).lower(), "unmapped delivery")

    action = "verified" if args.check else "applied"
    print(
        f"scene delivery {action}: replacements={len(replacements)} "
        f"unmapped={len(manifest.get('unmapped', []))} root={delivery_root}"
    )
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity-root", help="Unity repository root; defaults from this script")
    parser.add_argument("--delivery-root", help="override the delivery root recorded in the manifest")
    parser.add_argument("--check", action="store_true", help="verify source/target hashes without writing")
    try:
        return run(parser.parse_args())
    except Exception as exc:
        print(f"scene delivery failed: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
