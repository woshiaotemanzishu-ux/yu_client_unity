#!/usr/bin/env python3
"""Compare an isolated Unity candidate with its independent old-runtime evidence."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def percentile(values: list[float], value: float) -> float | None:
    return round(float(np.percentile(np.asarray(values), value)), 4) if values else None


def flatten_runtime(node: dict, out: list[dict]) -> None:
    runtime = node.get("runtime") or {}
    out.append({
        "name": node.get("name", ""),
        "type": node.get("type", ""),
        "x": float(runtime.get("gx", node.get("gx", 0)) or 0),
        "y": float(runtime.get("gy", node.get("gy", 0)) or 0),
        "width": float(runtime.get("width", node.get("runtimeWidth", node.get("width", 0))) or 0),
        "height": float(runtime.get("height", node.get("runtimeHeight", node.get("height", 0))) or 0),
        "visible": bool(runtime.get("visible", node.get("visible", True))) and bool(runtime.get("displayed", True)),
        "text": str(runtime.get("text", node.get("text", "")) or ""),
        "skin": str(runtime.get("skin", node.get("skin", "")) or ""),
        "runtimePath": runtime.get("path", ""),
    })
    for child in node.get("children") or []:
        flatten_runtime(child, out)


def geometry_delta(old: dict, new: dict) -> dict:
    fields = ("x", "y", "width", "height")
    deltas = {field: round(float(new[field]) - float(old[field]), 4) for field in fields}
    deltas["maxAbs"] = round(max(abs(value) for value in deltas.values()), 4)
    return deltas


def match_geometry(runtime_nodes: list[dict], candidate_nodes: list[dict]) -> tuple[list[dict], list[dict]]:
    by_name: dict[str, list[tuple[int, dict]]] = defaultdict(list)
    for index, node in enumerate(candidate_nodes):
        if node.get("active", False):
            by_name[str(node.get("name", ""))].append((index, node))
    used: set[int] = set()
    matches: list[dict] = []
    missing: list[dict] = []
    for position, old in enumerate(runtime_nodes):
        if not old["visible"] or old["width"] <= 0 or old["height"] <= 0:
            continue
        choices = by_name.get(old["name"], [])
        if position == 0 and not choices and candidate_nodes:
            choices = [(0, candidate_nodes[0])]
        choices = [(index, node) for index, node in choices if index not in used]
        if not choices:
            missing.append(old)
            continue
        index, node = min(choices, key=lambda item: geometry_delta(old, item[1])["maxAbs"])
        used.add(index)
        delta = geometry_delta(old, node)
        matches.append({
            "name": old["name"],
            "runtimePath": old["runtimePath"],
            "candidatePath": node.get("path", ""),
            "old": {field: old[field] for field in ("x", "y", "width", "height")},
            "candidate": {field: round(float(node[field]), 4) for field in ("x", "y", "width", "height")},
            "delta": delta,
            "skin": old["skin"],
            "spriteResolved": bool(node.get("spriteResolved", False)),
        })
    return matches, missing


def write_pixel_diff(old_path: Path, candidate_path: Path, out_dir: Path) -> dict:
    old_image = Image.open(old_path).convert("RGBA")
    candidate_image = Image.open(candidate_path).convert("RGBA")
    if old_image.size != candidate_image.size:
        raise ValueError(f"image size mismatch: old={old_image.size} candidate={candidate_image.size}")
    # RGBA multiplication reaches 65,025, so int16 would overflow before division.
    old = np.asarray(old_image, dtype=np.int32)
    candidate = np.asarray(candidate_image, dtype=np.int32)
    alpha = candidate[:, :, 3]
    mask = alpha > 8
    composite_rgb = (candidate[:, :, :3] * alpha[:, :, None] + old[:, :, :3] * (255 - alpha[:, :, None])) // 255
    rgb_delta = np.abs(composite_rgb - old[:, :, :3])
    per_pixel = rgb_delta.mean(axis=2)
    visible_values = per_pixel[mask]

    composite_rgba = np.dstack((composite_rgb.astype(np.uint8), np.full(alpha.shape, 255, dtype=np.uint8)))
    Image.fromarray(composite_rgba, "RGBA").save(out_dir / "candidate-on-old.png")
    overlay = ((old[:, :, :3] + composite_rgb) // 2).astype(np.uint8)
    Image.fromarray(np.dstack((overlay, np.full(alpha.shape, 255, dtype=np.uint8))), "RGBA").save(out_dir / "pixel-overlay.png")
    heat = np.zeros_like(old, dtype=np.uint8)
    heat[:, :, 0] = np.clip(per_pixel * 4, 0, 255).astype(np.uint8)
    heat[:, :, 1] = np.where(mask, np.clip(255 - per_pixel * 2, 0, 255), 0).astype(np.uint8)
    heat[:, :, 3] = np.where(mask, 255, 0).astype(np.uint8)
    Image.fromarray(heat, "RGBA").save(out_dir / "pixel-diff.png")

    return {
        "candidateAlphaPixels": int(mask.sum()),
        "candidateAlphaCoverage": round(float(mask.mean()), 6),
        "meanAbsRgbOnCandidateFootprint": round(float(visible_values.mean()), 4) if visible_values.size else None,
        "p95AbsRgbOnCandidateFootprint": round(float(np.percentile(visible_values, 95)), 4) if visible_values.size else None,
        "changedPixelRatioGt32OnCandidateFootprint": round(float((visible_values > 32).mean()), 6) if visible_values.size else None,
        "comparisonMode": "candidate RGBA composited over the same old-runtime 720x1280 screenshot; metrics use candidate alpha>8 footprint",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime-snapshot", required=True, type=Path)
    parser.add_argument("--candidate-report", required=True, type=Path)
    parser.add_argument("--old-image", required=True, type=Path)
    parser.add_argument("--candidate-image", required=True, type=Path)
    parser.add_argument("--out-dir", required=True, type=Path)
    args = parser.parse_args()
    args.out_dir.mkdir(parents=True, exist_ok=False)

    snapshot = json.loads(args.runtime_snapshot.read_text(encoding="utf-8"))
    candidate = json.loads(args.candidate_report.read_text(encoding="utf-8"))
    runtime_nodes: list[dict] = []
    flatten_runtime(snapshot["views"][0]["nodeTree"], runtime_nodes)
    candidate_nodes = candidate.get("nodes") or []
    matches, missing = match_geometry(runtime_nodes, candidate_nodes)
    errors = [match["delta"]["maxAbs"] for match in matches]
    visible_runtime = [node for node in runtime_nodes if node["visible"]]
    visible_geometry = [node for node in visible_runtime if node["width"] > 0 and node["height"] > 0]
    visible_skins = [node for node in visible_runtime if node["skin"]]
    visible_texts = [node for node in visible_runtime if node["text"]]
    resolved_skin_matches = [match for match in matches if match["skin"] and match["spriteResolved"]]
    pixel = write_pixel_diff(args.old_image, args.candidate_image, args.out_dir)

    report = {
        "schema": 1,
        "authority": "old-runtime-snapshot-vs-isolated-unity-candidate",
        "inputs": {
            "runtimeSnapshot": str(args.runtime_snapshot),
            "runtimeSnapshotSha256": sha256(args.runtime_snapshot),
            "candidateReport": str(args.candidate_report),
            "candidateReportSha256": sha256(args.candidate_report),
            "oldImage": str(args.old_image),
            "oldImageSha256": sha256(args.old_image),
            "candidateImage": str(args.candidate_image),
            "candidateImageSha256": sha256(args.candidate_image),
        },
        "capture": {
            "capturedNodes": len(runtime_nodes),
            "visibleNodes": len(visible_runtime),
            "visibleGeometryNodes": len(visible_geometry),
            "visibleSkinNodes": len(visible_skins),
            "visibleTextNodes": len(visible_texts),
        },
        "generation": {
            **(candidate.get("metrics") or {}),
            "matchedVisibleGeometryNodes": len(matches),
            "visibleGeometryGenerationRate": round(len(matches) / len(visible_geometry), 6) if visible_geometry else None,
            "missingVisibleGeometryNodes": len(missing),
            "resolvedVisibleSkinNodes": len(resolved_skin_matches),
            "visibleSkinResolutionRate": round(len(resolved_skin_matches) / len(visible_skins), 6) if visible_skins else None,
            "visibleTextCoverageRate": round((candidate.get("metrics", {}).get("renderedTexts", 0)) / len(visible_texts), 6) if visible_texts else None,
        },
        "geometry": {
            "matched": len(matches),
            "medianMaxAbsErrorPx": percentile(errors, 50),
            "p95MaxAbsErrorPx": percentile(errors, 95),
            "maxAbsErrorPx": round(max(errors), 4) if errors else None,
            "exactWithin1PxRate": round(sum(value <= 1 for value in errors) / len(errors), 6) if errors else None,
        },
        "pixel": pixel,
        "missing": missing,
        "matches": matches,
        "limitations": [
            "Snapshot text capture was empty, so text fidelity cannot be recovered by conversion.",
            "The managed view subtree excludes the independently composed 3D wing model and page chrome.",
            "Runtime particle/effect frames, list clone provenance, filters and material state are not represented by this snapshot schema.",
        ],
    }
    (args.out_dir / "diff-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"report": str(args.out_dir / "diff-report.json"), "generation": report["generation"], "geometry": report["geometry"], "pixel": pixel}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
