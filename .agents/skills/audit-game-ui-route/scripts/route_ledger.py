#!/usr/bin/env python3
"""Create and validate a compact UI route ledger.

The script is intentionally dependency-free so a route audit can keep its tree
machine-checkable without spending model context on repeated table rebuilding.
"""

from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path


STATUSES = {
    "not-run",
    "baseline-only",
    "defect",
    "fixing",
    "needs-runtime-verify",
    "blocked",
    "done",
}

LEAF_GATES = (
    "click",
    "result",
    "protocol",
    "immediate",
    "reopen",
    "return_chain",
    "timing",
    "visual_version",
    "visual_match",
    "target_identity",
    "layout_structure",
    "scroll_interaction",
    "page_space_geometry",
    "runtime_state",
    "model_presentation",
    "render_completion",
    "effect_match",
    "resource_stable",
    "restore",
)

VISUAL_EVIDENCE_FIELDS = ("old", "unity", "diff")
MODEL_EVIDENCE_FIELDS = ("old", "unity")
RESOURCE_EVIDENCE_FIELDS = ("preflight_first", "preflight_second", "runtime_delta")
ARRAY_EVIDENCE_GATES = {
    "target_identity": "identity_evidence",
    "layout_structure": "layout_evidence",
    "scroll_interaction": "interaction_evidence",
    "page_space_geometry": "geometry_evidence",
    "render_completion": "render_evidence",
}


def read_json(path: Path):
    with path.open("r", encoding="utf-8") as stream:
        return json.load(stream)


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2)
        stream.write("\n")


def init_ledger(manifest: Path, output: Path) -> int:
    source = read_json(manifest)
    nodes = source.get("nodes", source)
    if not isinstance(nodes, list):
        raise ValueError("manifest must be a node array or contain nodes[]")

    ledger = {
        "schema": 4,
        "route": source.get("route", output.stem) if isinstance(source, dict) else output.stem,
        "baseline": source.get("baseline", {}) if isinstance(source, dict) else {},
        "nodes": [],
    }
    for item in nodes:
        node = dict(item)
        node.setdefault("parent", None)
        node.setdefault("type", "read")
        node.setdefault("status", "not-run")
        node.setdefault("risk", "read-only")
        node.setdefault("gates", {})
        node.setdefault("evidence", [])
        ledger["nodes"].append(node)
    write_json(output, ledger)
    return 0


def apply_results(ledger_path: Path, results_path: Path) -> int:
    """Merge compact leaf results and derive parent status bottom-up."""
    ledger = read_json(ledger_path)
    source = read_json(results_path)
    results = source.get("nodes", source) if isinstance(source, dict) else source
    if not isinstance(results, list):
        raise ValueError("results must be a node array or contain nodes[]")

    nodes = ledger.get("nodes")
    if not isinstance(nodes, list):
        raise ValueError("ledger nodes[] is required")
    by_id = {node.get("id"): node for node in nodes if node.get("id")}
    for result in results:
        node_id = result.get("id")
        if node_id not in by_id:
            raise ValueError(f"result references unknown id: {node_id}")
        node = by_id[node_id]
        for key in (
            "status",
            "risk",
            "applicable_gates",
            "gates",
            "timing",
            "note",
            "visual_evidence",
            "identity_evidence",
            "layout_evidence",
            "interaction_evidence",
            "geometry_evidence",
            "render_evidence",
            "state_evidence",
            "model_evidence",
            "effect_evidence",
            "resource_evidence",
        ):
            if key in result:
                node[key] = result[key]
        if "evidence" in result:
            current = node.setdefault("evidence", [])
            for value in result["evidence"]:
                if value not in current:
                    current.append(value)

    children: dict[str, list[dict]] = {}
    for node in nodes:
        parent = node.get("parent")
        if parent:
            children.setdefault(parent, []).append(node)
    priority = ("blocked", "defect", "fixing", "needs-runtime-verify", "baseline-only", "not-run")
    changed = True
    while changed:
        changed = False
        for parent_id, direct_children in children.items():
            parent = by_id[parent_id]
            if all(child.get("status") == "done" for child in direct_children):
                derived = "done"
            else:
                derived = next(
                    (status for status in priority if any(child.get("status") == status for child in direct_children)),
                    "not-run",
                )
            if parent.get("status") != derived:
                parent["status"] = derived
                changed = True

    write_json(ledger_path, ledger)
    return validate_ledger(ledger_path)


def validate_ledger(path: Path, quiet: bool = False) -> int:
    ledger = read_json(path)
    nodes = ledger.get("nodes")
    errors: list[str] = []
    if not isinstance(nodes, list) or not nodes:
        errors.append("nodes must be a non-empty array")
        nodes = []

    by_id = {}
    children = Counter()
    for index, node in enumerate(nodes):
        node_id = node.get("id")
        if not isinstance(node_id, str) or not node_id:
            errors.append(f"nodes[{index}].id is required")
            continue
        if node_id in by_id:
            errors.append(f"duplicate id: {node_id}")
        by_id[node_id] = node
        status = node.get("status")
        if status not in STATUSES:
            errors.append(f"{node_id}: invalid status {status!r}")
        parent = node.get("parent")
        if parent:
            children[parent] += 1

    for node_id, node in by_id.items():
        parent = node.get("parent")
        if parent and parent not in by_id:
            errors.append(f"{node_id}: missing parent {parent}")

        status = node.get("status")
        if children[node_id] == 0 and status == "done":
            gates = node.get("gates") or {}
            applicable = node.get("applicable_gates", LEAF_GATES)
            if not isinstance(applicable, list):
                errors.append(f"{node_id}: applicable_gates must be an array")
                applicable = []
            for gate in applicable:
                if gate not in LEAF_GATES:
                    errors.append(f"{node_id}: unknown gate {gate!r}")
                if gates.get(gate) is not True:
                    errors.append(f"{node_id}: done leaf gate {gate!r} is not true")

            if "timing" in applicable:
                timing = node.get("timing")
                if not isinstance(timing, dict):
                    errors.append(f"{node_id}: timing requires timing object")
                else:
                    for field in ("cold_ms", "warm_ms"):
                        value = timing.get(field)
                        if not isinstance(value, (int, float)) or isinstance(value, bool) or value < 0:
                            errors.append(f"{node_id}: timing.{field} must be a non-negative number")

            if "visual_match" in applicable:
                visual = node.get("visual_evidence")
                if not isinstance(visual, dict):
                    errors.append(f"{node_id}: visual_match requires visual_evidence")
                else:
                    for field in VISUAL_EVIDENCE_FIELDS:
                        if not isinstance(visual.get(field), str) or not visual[field].strip():
                            errors.append(f"{node_id}: visual_evidence.{field} is required")

            if "runtime_state" in applicable:
                state = node.get("state_evidence")
                if not isinstance(state, list) or not state or any(
                    not isinstance(value, str) or not value.strip() for value in state
                ):
                    errors.append(f"{node_id}: runtime_state requires non-empty state_evidence[]")

            for gate, field in ARRAY_EVIDENCE_GATES.items():
                if gate not in applicable:
                    continue
                values = node.get(field)
                if not isinstance(values, list) or not values or any(
                    not isinstance(value, str) or not value.strip() for value in values
                ):
                    errors.append(f"{node_id}: {gate} requires non-empty {field}[]")

            if "model_presentation" in applicable:
                model = node.get("model_evidence")
                if not isinstance(model, dict):
                    errors.append(f"{node_id}: model_presentation requires model_evidence")
                else:
                    for field in MODEL_EVIDENCE_FIELDS:
                        if not isinstance(model.get(field), str) or not model[field].strip():
                            errors.append(f"{node_id}: model_evidence.{field} is required")

            if "effect_match" in applicable:
                effect = node.get("effect_evidence")
                if not isinstance(effect, list) or not effect or any(
                    not isinstance(value, str) or not value.strip() for value in effect
                ):
                    errors.append(f"{node_id}: effect_match requires non-empty effect_evidence[]")

            if "resource_stable" in applicable:
                resource = node.get("resource_evidence")
                if not isinstance(resource, dict):
                    errors.append(f"{node_id}: resource_stable requires resource_evidence")
                else:
                    for field in RESOURCE_EVIDENCE_FIELDS:
                        if not isinstance(resource.get(field), str) or not resource[field].strip():
                            errors.append(f"{node_id}: resource_evidence.{field} is required")

        if children[node_id] > 0 and status == "done":
            unfinished = [
                child_id
                for child_id, child in by_id.items()
                if child.get("parent") == node_id and child.get("status") != "done"
            ]
            if unfinished:
                errors.append(f"{node_id}: done parent has unfinished children: {', '.join(unfinished)}")

            if node.get("type") == "page":
                inventory = node.get("control_inventory")
                if not isinstance(inventory, list) or not inventory:
                    errors.append(f"{node_id}: done page requires non-empty control_inventory[]")
                else:
                    control_ids: set[str] = set()
                    direct_children = {
                        child_id for child_id, child in by_id.items() if child.get("parent") == node_id
                    }
                    for index, control in enumerate(inventory):
                        if not isinstance(control, dict):
                            errors.append(f"{node_id}: control_inventory[{index}] must be an object")
                            continue
                        control_id = control.get("id")
                        kind = control.get("kind")
                        child = control.get("child")
                        if not isinstance(control_id, str) or not control_id.strip():
                            errors.append(f"{node_id}: control_inventory[{index}].id is required")
                        elif control_id in control_ids:
                            errors.append(f"{node_id}: duplicate control id {control_id}")
                        else:
                            control_ids.add(control_id)
                        if not isinstance(kind, str) or not kind.strip():
                            errors.append(f"{node_id}: control_inventory[{index}].kind is required")
                        if child not in direct_children:
                            errors.append(
                                f"{node_id}: control_inventory[{index}].child {child!r} is not a direct child"
                            )

    if not quiet:
        counts = Counter(node.get("status", "missing") for node in nodes)
        print(f"route={ledger.get('route', '')} nodes={len(nodes)} status={dict(sorted(counts.items()))}")
        for error in errors:
            print(f"ERROR: {error}")
    return 1 if errors else 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    init_parser = sub.add_parser("init", help="create a ledger from a compact manifest")
    init_parser.add_argument("manifest", type=Path)
    init_parser.add_argument("output", type=Path)

    validate_parser = sub.add_parser("validate", help="validate statuses and done gates")
    validate_parser.add_argument("ledger", type=Path)
    validate_parser.add_argument("--quiet", action="store_true")

    apply_parser = sub.add_parser("apply", help="merge compact results and roll up parent statuses")
    apply_parser.add_argument("ledger", type=Path)
    apply_parser.add_argument("results", type=Path)

    args = parser.parse_args()
    try:
        if args.command == "init":
            code = init_ledger(args.manifest, args.output)
            return validate_ledger(args.output) or code
        if args.command == "apply":
            return apply_results(args.ledger, args.results)
        return validate_ledger(args.ledger, args.quiet)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
