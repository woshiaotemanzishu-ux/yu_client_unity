#!/usr/bin/env python3
"""Create, update, and validate a compact UI route ledger.

Schema 6 makes completion evidence transactional and replayable:

* ``init`` and ``apply`` validate an in-memory candidate before atomically
  replacing the official ledger;
* ``init`` refuses to overwrite an existing ledger and both writers use a
  non-blocking cross-process lock so concurrent updates cannot be lost;
* every completed leaf binds each applicable gate to a named verification run
  and one or more immutable (hashed) artifacts or attributed assertions;
* dynamic effects, scrolling, resources, models, and shared components have
  structured evidence contracts instead of accepting an arbitrary path string;
* a completed root page must reference a real-Web run whose Git/dirty/Player/
  catalog fingerprints and sequential-session report belong to the same batch.

Schemas 2-5 remain readable with their historical validation rules.  New
ledgers are always created as schema 6; changing an old ``schema`` number does
not manufacture the missing evidence.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import os
import re
import sys
import tempfile
from collections import Counter
from contextlib import contextmanager
from datetime import datetime
from pathlib import Path


CURRENT_SCHEMA = 6
SUPPORTED_SCHEMAS = {2, 3, 4, 5, 6}

STATUSES = {
    "not-run",
    "baseline-only",
    "defect",
    "fixing",
    "needs-runtime-verify",
    "blocked",
    "done",
}

NODE_TYPES = {
    "page",
    "tab",
    "navigation",
    "read",
    "reversible-write",
    "destructive-write",
    "transaction",
    "return",
}

RISKS = {"read-only", "reversible-write", "destructive-write"}
PARENT_STATUS_PRIORITY = (
    "blocked",
    "defect",
    "fixing",
    "needs-runtime-verify",
    "baseline-only",
    "not-run",
)

LEGACY_LEAF_GATES = (
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

LEAF_GATES = LEGACY_LEAF_GATES + (
    "shared_component_identity",
    "component_state_matrix",
    "authorization",
)

VISUAL_EVIDENCE_FIELDS = ("old", "unity", "diff")
MODEL_EVIDENCE_FIELDS = ("old", "unity")
RESOURCE_EVIDENCE_FIELDS = ("preflight_first", "preflight_second", "runtime_delta")
LEGACY_ARRAY_EVIDENCE_GATES = {
    "target_identity": "identity_evidence",
    "layout_structure": "layout_evidence",
    "scroll_interaction": "interaction_evidence",
    "page_space_geometry": "geometry_evidence",
    "render_completion": "render_evidence",
    "shared_component_identity": "component_evidence",
    "component_state_matrix": "component_state_evidence",
}

SCHEMA6_MINIMUM_GATES = {
    "tab": {"click", "result", "runtime_state"},
    "navigation": {"click", "result", "target_identity", "timing"},
    "read": {"runtime_state"},
    "reversible-write": {"click", "result", "immediate", "reopen", "restore"},
    "destructive-write": {
        "click",
        "result",
        "protocol",
        "immediate",
        "reopen",
        "authorization",
    },
    "transaction": {"click", "result", "protocol", "immediate", "reopen"},
    "return": {"click", "return_chain"},
}

RUNTIME_GATE_ENVIRONMENTS = {
    "click": {"unity-editor", "real-web", "user-runtime"},
    "result": {"unity-editor", "real-web", "user-runtime"},
    "immediate": {"unity-editor", "real-web", "user-runtime"},
    "reopen": {"unity-editor", "real-web", "user-runtime"},
    "return_chain": {"unity-editor", "real-web", "user-runtime"},
    "timing": {"unity-editor", "real-web"},
    "visual_match": {"real-web"},
    "target_identity": {"unity-editor", "real-web", "user-runtime"},
    "scroll_interaction": {"unity-editor", "real-web", "user-runtime"},
    "page_space_geometry": {"unity-editor", "real-web", "user-runtime"},
    "runtime_state": {"unity-editor", "real-web", "user-runtime"},
    "model_presentation": {"unity-editor", "real-web", "user-runtime"},
    "render_completion": {"unity-editor", "real-web", "user-runtime"},
    "effect_match": {"unity-editor", "real-web", "user-runtime"},
    "component_state_matrix": {"unity-editor", "real-web", "user-runtime"},
    "restore": {"unity-editor", "real-web", "user-runtime"},
}

GATE_DETAIL_FIELDS = {
    "timing": ("timing",),
    "visual_version": ("version_evidence",),
    "visual_match": ("visual_evidence",),
    "target_identity": ("identity_evidence",),
    "layout_structure": ("layout_evidence",),
    "scroll_interaction": ("interaction_evidence",),
    "page_space_geometry": ("geometry_evidence",),
    "runtime_state": ("state_evidence",),
    "model_presentation": ("model_evidence",),
    "render_completion": ("render_evidence",),
    "effect_match": ("effect_evidence",),
    "resource_stable": ("resource_evidence",),
    "shared_component_identity": ("component_evidence",),
    "component_state_matrix": ("component_state_evidence",),
    "authorization": ("authorization_evidence",),
}

RESULT_FIELDS = {
    "id",
    "status",
    "risk",
    "applicable_gates",
    "gates",
    "gate_runs",
    "gate_evidence",
    "timing",
    "note",
    "blocked_reason",
    "runtime_gap",
    "inventory_evidence",
    "visual_evidence",
    "version_evidence",
    "identity_evidence",
    "layout_evidence",
    "interaction_evidence",
    "geometry_evidence",
    "render_evidence",
    "state_evidence",
    "model_evidence",
    "effect_evidence",
    "resource_evidence",
    "component_evidence",
    "component_state_evidence",
    "authorization_evidence",
    "evidence",
    "invalidate_gates",
    "invalidation_reason",
    "observed_at",
}

SHA256_RE = re.compile(r"^[0-9a-fA-F]{64}$")
GIT_HASH_RE = re.compile(r"^(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})$")
UNITY_GUID_RE = re.compile(r"^[0-9a-fA-F]{32}$")


def read_json(path: Path):
    with path.open("r", encoding="utf-8") as stream:
        return json.load(stream)


def _write_json_file(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2)
        stream.write("\n")


def write_json_atomic(path: Path, value) -> None:
    """Replace *path* only after a complete sibling file has been flushed."""
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    try:
        _write_json_file(temporary, value)
        os.replace(temporary, path)
    finally:
        if temporary.exists():
            temporary.unlink()


@contextmanager
def _ledger_write_lock(path: Path):
    """Hold a non-blocking, cross-process lock for one official ledger path."""
    lock_root = Path(tempfile.gettempdir()) / "codex-ui-route-ledger-locks"
    lock_root.mkdir(parents=True, exist_ok=True)
    resolved = path.resolve()
    if resolved.exists():
        stat = resolved.stat()
        identity = f"file:{stat.st_dev}:{stat.st_ino}" if stat.st_ino else f"path:{str(resolved).casefold()}"
    else:
        identity = f"path:{str(resolved).casefold()}"
    lock_key = hashlib.sha256(identity.encode("utf-8")).hexdigest()
    lock_path = lock_root / f"{lock_key}.lock"
    stream = lock_path.open("a+b")
    acquired = False
    try:
        stream.seek(0, os.SEEK_END)
        if stream.tell() == 0:
            stream.write(b"\0")
            stream.flush()
        stream.seek(0)
        try:
            if os.name == "nt":
                import msvcrt

                msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
            else:
                import fcntl

                fcntl.flock(stream.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError as error:
            raise OSError(f"another process is writing route ledger: {path}") from error
        acquired = True
        yield
    finally:
        if acquired:
            try:
                stream.seek(0)
                if os.name == "nt":
                    import msvcrt

                    msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
                else:
                    import fcntl

                    fcntl.flock(stream.fileno(), fcntl.LOCK_UN)
            except OSError:
                pass
        stream.close()


def _sha256(path: Path, cache: dict[Path, str] | None = None) -> str:
    resolved = path.resolve()
    if cache is not None and resolved in cache:
        return cache[resolved]
    digest = hashlib.sha256()
    with resolved.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    value = digest.hexdigest()
    if cache is not None:
        cache[resolved] = value
    return value


def _repo_root(path: Path) -> Path:
    current = path.resolve().parent
    for candidate in (current, *current.parents):
        if (candidate / ".git").exists():
            return candidate
    return Path.cwd().resolve()


def _is_number(value) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool)


def _non_empty_string(value) -> bool:
    return isinstance(value, str) and bool(value.strip())


def _is_timezone_iso8601(value) -> bool:
    if not _non_empty_string(value):
        return False
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        return False
    return parsed.tzinfo is not None


def _parse_timezone_iso8601(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def _artifact_path(raw_path: str, repo_root: Path) -> Path:
    candidate = Path(raw_path)
    return candidate if candidate.is_absolute() else repo_root / candidate


def _validate_evidence_ref(
    value,
    label: str,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    """Validate a schema-6 immutable artifact or attributed assertion."""
    if not isinstance(value, dict):
        errors.append(f"{label}: evidence must be an object, not a bare path string")
        return

    has_path = "path" in value
    has_assertion = "assertion" in value
    if has_path == has_assertion:
        errors.append(f"{label}: evidence requires exactly one of path/assertion")
        return

    if has_path:
        raw_path = value.get("path")
        digest = value.get("sha256")
        if not _non_empty_string(raw_path):
            errors.append(f"{label}.path is required")
            return
        if not isinstance(digest, str) or not SHA256_RE.fullmatch(digest):
            errors.append(f"{label}.sha256 must be a 64-character SHA-256")
            return
        resolved = _artifact_path(raw_path, repo_root)
        if not resolved.is_file():
            errors.append(f"{label}.path does not exist: {raw_path}")
            return
        actual = _sha256(resolved, hash_cache)
        if actual.lower() != digest.lower():
            errors.append(f"{label}.sha256 mismatch for {raw_path}: expected {digest}, actual {actual}")
        return

    for field in ("assertion", "source", "scope"):
        if not _non_empty_string(value.get(field)):
            errors.append(f"{label}.{field} is required for attributed assertion evidence")
    if not _is_timezone_iso8601(value.get("observed_at")):
        errors.append(f"{label}.observed_at must be timezone-aware ISO-8601")


def _validate_ref_list(
    values,
    label: str,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    if not isinstance(values, list) or not values:
        errors.append(f"{label} must be a non-empty array")
        return
    for index, value in enumerate(values):
        _validate_evidence_ref(value, f"{label}[{index}]", errors, repo_root, hash_cache)


def _validate_manifest_binding(
    ledger: dict,
    by_id: dict[str, dict],
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    reference = ledger.get("manifest_source")
    if not isinstance(reference, dict) or not _non_empty_string(reference.get("path")):
        errors.append("schema 6 ledger requires manifest_source.path and sha256")
        return
    _validate_evidence_ref(reference, "manifest_source", errors, repo_root, hash_cache)
    manifest_path = _artifact_path(reference["path"], repo_root)
    if not manifest_path.is_file():
        return
    try:
        source = read_json(manifest_path)
    except (OSError, ValueError, TypeError, json.JSONDecodeError) as error:
        errors.append(f"manifest_source cannot be read as JSON: {error}")
        return
    if isinstance(source, dict):
        manifest_nodes = source.get("nodes")
        manifest_route = source.get("route", manifest_path.stem)
    elif isinstance(source, list):
        manifest_nodes = source
        manifest_route = manifest_path.stem
    else:
        errors.append("manifest_source must be a node array or contain nodes[]")
        return
    if not isinstance(manifest_nodes, list):
        errors.append("manifest_source must contain nodes[]")
        return
    if ledger.get("route") != manifest_route:
        errors.append(f"route differs from manifest_source: {ledger.get('route')!r} != {manifest_route!r}")

    manifest_by_id: dict[str, dict] = {}
    for index, item in enumerate(manifest_nodes):
        if not isinstance(item, dict) or not _non_empty_string(item.get("id")):
            errors.append(f"manifest_source.nodes[{index}] requires a string id")
            continue
        node_id = item["id"]
        if node_id in manifest_by_id:
            errors.append(f"manifest_source contains duplicate id: {node_id}")
            continue
        manifest_by_id[node_id] = item

    missing = sorted(set(manifest_by_id) - set(by_id))
    extra = sorted(set(by_id) - set(manifest_by_id))
    if missing:
        errors.append(f"ledger omits manifest nodes: {', '.join(missing)}")
    if extra:
        errors.append(f"ledger has nodes absent from manifest: {', '.join(extra)}")
    for node_id in sorted(set(manifest_by_id) & set(by_id)):
        manifest_node = manifest_by_id[node_id]
        ledger_node = by_id[node_id]
        expected = {
            "parent": manifest_node.get("parent"),
            "type": manifest_node.get("type", "read"),
            "risk": manifest_node.get("risk", "read-only"),
            "control_inventory": manifest_node.get("control_inventory"),
        }
        for field, expected_value in expected.items():
            if ledger_node.get(field) != expected_value:
                errors.append(f"{node_id}: {field} differs from manifest_source")


def _validate_verification_runs(
    ledger: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> dict[str, dict]:
    runs = ledger.get("verification_runs", {})
    if not isinstance(runs, dict):
        errors.append("verification_runs must be an object")
        return {}

    for run_id, run in runs.items():
        prefix = f"verification_runs.{run_id}"
        if not _non_empty_string(run_id):
            errors.append("verification_runs keys must be non-empty strings")
            continue
        if not isinstance(run, dict):
            errors.append(f"{prefix} must be an object")
            continue
        if not _is_timezone_iso8601(run.get("recorded_at")):
            errors.append(f"{prefix}.recorded_at must be timezone-aware ISO-8601")
        environment = run.get("environment")
        if not isinstance(environment, str) or environment not in {
            "static",
            "unity-editor",
            "real-web",
            "user-runtime",
        }:
            errors.append(f"{prefix}.environment is invalid: {environment!r}")
        git_commit = run.get("git_commit")
        dirty = run.get("dirty_fingerprint")
        if not isinstance(git_commit, str) or not GIT_HASH_RE.fullmatch(git_commit):
            errors.append(f"{prefix}.git_commit must be a 40/64-character Git hash")
        if not isinstance(dirty, str) or not SHA256_RE.fullmatch(dirty):
            errors.append(f"{prefix}.dirty_fingerprint must be a SHA-256")

        if environment == "unity-editor" and not _non_empty_string(run.get("unity_version")):
            errors.append(f"{prefix}.unity_version is required for unity-editor runs")

        if environment == "real-web":
            for field in ("player_sha256", "catalog_sha256"):
                value = run.get(field)
                if not isinstance(value, str) or not SHA256_RE.fullmatch(value):
                    errors.append(f"{prefix}.{field} must be a SHA-256")
            viewports = run.get("viewports")
            if not isinstance(viewports, list) or "720x1280" not in viewports or "1920x1080" not in viewports:
                errors.append(f"{prefix}.viewports must contain 720x1280 and 1920x1080")
            if run.get("old_session_disconnected") is not True:
                errors.append(f"{prefix}.old_session_disconnected must be true")
            if run.get("unity_session_valid") is not True:
                errors.append(f"{prefix}.unity_session_valid must be true")
            _validate_evidence_ref(run.get("report"), f"{prefix}.report", errors, repo_root, hash_cache)
    return runs


def _validate_legacy_done_leaf(node_id: str, node: dict, schema: int, errors: list[str]) -> None:
    gates = node.get("gates") or {}
    if not isinstance(gates, dict):
        errors.append(f"{node_id}: gates must be an object")
        gates = {}
    default_gates = LEAF_GATES[:-1] if schema >= 5 else LEGACY_LEAF_GATES
    raw_applicable = node.get("applicable_gates", list(default_gates))
    if not isinstance(raw_applicable, list):
        errors.append(f"{node_id}: applicable_gates must be an array")
        raw_applicable = []
    for index, gate in enumerate(raw_applicable):
        if not isinstance(gate, str):
            errors.append(f"{node_id}: applicable_gates[{index}] must be a string")
    applicable = [gate for gate in raw_applicable if isinstance(gate, str)]
    if schema >= 5 and not applicable:
        errors.append(f"{node_id}: schema {schema} done leaf requires non-empty applicable_gates")
    if len(applicable) != len(set(applicable)):
        errors.append(f"{node_id}: applicable_gates contains duplicates")
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
                if not _is_number(value) or value < 0:
                    errors.append(f"{node_id}: timing.{field} must be a non-negative number")

    if "visual_match" in applicable:
        visual = node.get("visual_evidence")
        if not isinstance(visual, dict):
            errors.append(f"{node_id}: visual_match requires visual_evidence")
        else:
            for field in VISUAL_EVIDENCE_FIELDS:
                if not _non_empty_string(visual.get(field)):
                    errors.append(f"{node_id}: visual_evidence.{field} is required")

    if "runtime_state" in applicable:
        state = node.get("state_evidence")
        if not isinstance(state, list) or not state or any(not _non_empty_string(value) for value in state):
            errors.append(f"{node_id}: runtime_state requires non-empty state_evidence[]")

    for gate, field in LEGACY_ARRAY_EVIDENCE_GATES.items():
        if gate not in applicable:
            continue
        values = node.get(field)
        if not isinstance(values, list) or not values or any(not _non_empty_string(value) for value in values):
            errors.append(f"{node_id}: {gate} requires non-empty {field}[]")

    if "model_presentation" in applicable:
        model = node.get("model_evidence")
        if not isinstance(model, dict):
            errors.append(f"{node_id}: model_presentation requires model_evidence")
        else:
            for field in MODEL_EVIDENCE_FIELDS:
                if not _non_empty_string(model.get(field)):
                    errors.append(f"{node_id}: model_evidence.{field} is required")

    if "effect_match" in applicable:
        effect = node.get("effect_evidence")
        if not isinstance(effect, list) or not effect or any(not _non_empty_string(value) for value in effect):
            errors.append(f"{node_id}: effect_match requires non-empty effect_evidence[]")

    if "resource_stable" in applicable:
        resource = node.get("resource_evidence")
        if not isinstance(resource, dict):
            errors.append(f"{node_id}: resource_stable requires resource_evidence")
        else:
            for field in RESOURCE_EVIDENCE_FIELDS:
                if not _non_empty_string(resource.get(field)):
                    errors.append(f"{node_id}: resource_evidence.{field} is required")


def _validate_schema6_timing(node_id: str, node: dict, errors: list[str]) -> None:
    timing = node.get("timing")
    if not isinstance(timing, dict):
        errors.append(f"{node_id}: timing requires timing object")
        return
    for phase in ("cold", "warm"):
        values = timing.get(phase)
        if not isinstance(values, dict):
            errors.append(f"{node_id}: timing.{phase} must be an object")
            continue
        first = values.get("first_visible_ms")
        ready = values.get("interactive_ready_ms")
        if not _is_number(first) or first < 0:
            errors.append(f"{node_id}: timing.{phase}.first_visible_ms must be non-negative")
        if not _is_number(ready) or ready < 0:
            errors.append(f"{node_id}: timing.{phase}.interactive_ready_ms must be non-negative")
        if _is_number(first) and _is_number(ready) and ready < first:
            errors.append(f"{node_id}: timing.{phase}.interactive_ready_ms cannot precede first_visible_ms")


def _validate_schema6_visual(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    visual = node.get("visual_evidence")
    if not isinstance(visual, dict):
        errors.append(f"{node_id}: visual_match requires visual_evidence object")
        return
    for field in ("old", "unity", "overlay", "diff"):
        _validate_evidence_ref(visual.get(field), f"{node_id}.visual_evidence.{field}", errors, repo_root, hash_cache)
    viewport = visual.get("viewport")
    if not isinstance(viewport, dict) or not all(
        _is_number(viewport.get(field)) and viewport[field] > 0 for field in ("width", "height")
    ):
        errors.append(f"{node_id}: visual_evidence.viewport requires positive width/height")


def _validate_semantic_artifact_items(
    node_id: str,
    values,
    field: str,
    required_checks: tuple[str, ...],
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    if not isinstance(values, list) or not values:
        errors.append(f"{node_id}: {field} must be a non-empty array")
        return
    for index, item in enumerate(values):
        prefix = f"{node_id}.{field}[{index}]"
        if not isinstance(item, dict):
            errors.append(f"{prefix} must be an object")
            continue
        _validate_evidence_ref(item.get("artifact"), f"{prefix}.artifact", errors, repo_root, hash_cache)
        checks = item.get("checks")
        if not isinstance(checks, dict):
            errors.append(f"{prefix}.checks must be an object")
            continue
        for check in required_checks:
            if checks.get(check) is not True:
                errors.append(f"{prefix}.checks.{check} must be true")


def _validate_schema6_interaction(
    node_id: str,
    values,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    if not isinstance(values, list) or not values:
        errors.append(f"{node_id}: interaction_evidence must be a non-empty array")
        return
    for index, item in enumerate(values):
        prefix = f"{node_id}.interaction_evidence[{index}]"
        if not isinstance(item, dict):
            errors.append(f"{prefix} must be an object")
            continue
        _validate_evidence_ref(item.get("artifact"), f"{prefix}.artifact", errors, repo_root, hash_cache)
        if item.get("raycast_drag") is not True:
            errors.append(f"{prefix}.raycast_drag must be true")
        delta = item.get("content_delta")
        if not _is_number(delta) or abs(delta) <= 0:
            errors.append(f"{prefix}.content_delta must be non-zero")
        if item.get("last_item_reached") is not True:
            errors.append(f"{prefix}.last_item_reached must be true")


def _validate_schema6_geometry(
    node_id: str,
    values,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    if not isinstance(values, list) or not values:
        errors.append(f"{node_id}: geometry_evidence must be a non-empty array")
        return
    for index, item in enumerate(values):
        prefix = f"{node_id}.geometry_evidence[{index}]"
        if not isinstance(item, dict):
            errors.append(f"{prefix} must be an object")
            continue
        _validate_evidence_ref(item.get("artifact"), f"{prefix}.artifact", errors, repo_root, hash_cache)
        expected = item.get("expected_rect")
        actual = item.get("actual_rect")
        tolerance = item.get("tolerance")
        if not isinstance(expected, list) or len(expected) != 4 or not all(_is_number(v) for v in expected):
            errors.append(f"{prefix}.expected_rect must contain four numbers")
            continue
        if not isinstance(actual, list) or len(actual) != 4 or not all(_is_number(v) for v in actual):
            errors.append(f"{prefix}.actual_rect must contain four numbers")
            continue
        if not _is_number(tolerance) or tolerance < 0:
            errors.append(f"{prefix}.tolerance must be non-negative")
            continue
        if any(abs(left - right) > tolerance for left, right in zip(expected, actual)):
            errors.append(f"{prefix}: actual_rect exceeds tolerance")


def _validate_schema6_model(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    model = node.get("model_evidence")
    if not isinstance(model, dict):
        errors.append(f"{node_id}: model_presentation requires model_evidence object")
        return
    for field in ("old", "unity"):
        _validate_evidence_ref(model.get(field), f"{node_id}.model_evidence.{field}", errors, repo_root, hash_cache)
    checks = model.get("checks")
    required = (
        "exists",
        "resource_correct",
        "parts_correct",
        "not_mirrored",
        "not_flipped",
        "angle_correct",
        "position_scale_correct",
        "effects_correct",
    )
    if not isinstance(checks, dict):
        errors.append(f"{node_id}: model_evidence.checks must be an object")
    else:
        for check in required:
            if checks.get(check) is not True:
                errors.append(f"{node_id}: model_evidence.checks.{check} must be true")


def _validate_schema6_render(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    values = node.get("render_evidence")
    if not isinstance(values, list) or not values:
        errors.append(f"{node_id}: render_evidence must be a non-empty array")
        return
    for index, item in enumerate(values):
        prefix = f"{node_id}.render_evidence[{index}]"
        if not isinstance(item, dict):
            errors.append(f"{prefix} must be an object")
            continue
        _validate_evidence_ref(item.get("artifact"), f"{prefix}.artifact", errors, repo_root, hash_cache)
        if item.get("render_completed") is not True:
            errors.append(f"{prefix}.render_completed must be true")
        pixels = item.get("nontransparent_pixels")
        if not isinstance(pixels, int) or isinstance(pixels, bool) or pixels <= 0:
            errors.append(f"{prefix}.nontransparent_pixels must be a positive integer")


def _validate_schema6_effect(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    values = node.get("effect_evidence")
    if not isinstance(values, list) or not values:
        errors.append(f"{node_id}: effect_evidence must be a non-empty array")
        return
    for index, item in enumerate(values):
        prefix = f"{node_id}.effect_evidence[{index}]"
        if not isinstance(item, dict):
            errors.append(f"{prefix} must be an object")
            continue
        if not _non_empty_string(item.get("owner")):
            errors.append(f"{prefix}.owner is required")

        legacy = item.get("legacy_call")
        if not isinstance(legacy, dict):
            errors.append(f"{prefix}.legacy_call must be an object")
        else:
            for field in ("effect_name", "parent", "position", "scale", "rotation", "loop", "render_size"):
                if field not in legacy or legacy[field] is None:
                    errors.append(f"{prefix}.legacy_call.{field} is required")

        driver = item.get("driver")
        if not isinstance(driver, dict):
            errors.append(f"{prefix}.driver must be an object")
        else:
            for field in ("animated_property", "material_property", "shader_branch"):
                if not _non_empty_string(driver.get(field)):
                    errors.append(f"{prefix}.driver.{field} is required")
            if driver.get("consumed") is not True:
                errors.append(f"{prefix}.driver.consumed must be true")

        render = item.get("render")
        if not isinstance(render, dict):
            errors.append(f"{prefix}.render must be an object")
        else:
            if not _non_empty_string(render.get("handle")):
                errors.append(f"{prefix}.render.handle is required")
            _validate_evidence_ref(render.get("frame_a"), f"{prefix}.render.frame_a", errors, repo_root, hash_cache)
            _validate_evidence_ref(render.get("frame_b"), f"{prefix}.render.frame_b", errors, repo_root, hash_cache)
            time_a = render.get("time_a")
            time_b = render.get("time_b")
            if not _is_number(time_a) or not _is_number(time_b) or time_b <= time_a:
                errors.append(f"{prefix}.render requires time_b > time_a")
            if not _is_number(render.get("pixel_diff")) or render["pixel_diff"] <= 0:
                errors.append(f"{prefix}.render.pixel_diff must be positive")
            pixels = render.get("nontransparent_pixels")
            if not isinstance(pixels, int) or isinstance(pixels, bool) or pixels <= 0:
                errors.append(f"{prefix}.render.nontransparent_pixels must be positive")
            bbox = render.get("alpha_bbox")
            if not isinstance(bbox, dict) or not all(
                _is_number(bbox.get(field)) and bbox[field] > 0 for field in ("width", "height")
            ):
                errors.append(f"{prefix}.render.alpha_bbox requires positive width/height")

        lifecycle = item.get("lifecycle")
        if not isinstance(lifecycle, dict) or lifecycle.get("hide") is not True or lifecycle.get("reopen") is not True:
            errors.append(f"{prefix}.lifecycle requires hide=true and reopen=true")

        if item.get("scroll_viewport") is True:
            mask = item.get("mask_states")
            if not isinstance(mask, dict):
                errors.append(f"{prefix}.mask_states is required for scroll_viewport effects")
            else:
                for state in ("full", "partial", "hidden"):
                    entry = mask.get(state)
                    if not isinstance(entry, dict):
                        errors.append(f"{prefix}.mask_states.{state} must be an object")
                        continue
                    _validate_evidence_ref(
                        entry.get("artifact"),
                        f"{prefix}.mask_states.{state}.artifact",
                        errors,
                        repo_root,
                        hash_cache,
                    )
                    alpha = entry.get("alpha_pixels")
                    if not isinstance(alpha, int) or isinstance(alpha, bool) or alpha < 0:
                        errors.append(f"{prefix}.mask_states.{state}.alpha_pixels must be non-negative")
                hidden = mask.get("hidden")
                if isinstance(hidden, dict) and hidden.get("alpha_pixels") != 0:
                    errors.append(f"{prefix}.mask_states.hidden.alpha_pixels must be zero")
                for state in ("full", "partial"):
                    entry = mask.get(state)
                    if (
                        isinstance(entry, dict)
                        and isinstance(entry.get("alpha_pixels"), int)
                        and not isinstance(entry.get("alpha_pixels"), bool)
                        and entry["alpha_pixels"] <= 0
                    ):
                        errors.append(f"{prefix}.mask_states.{state}.alpha_pixels must be positive")


def _validate_schema6_resource(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    resource = node.get("resource_evidence")
    if not isinstance(resource, dict):
        errors.append(f"{node_id}: resource_stable requires resource_evidence object")
        return
    for field in RESOURCE_EVIDENCE_FIELDS:
        _validate_evidence_ref(resource.get(field), f"{node_id}.resource_evidence.{field}", errors, repo_root, hash_cache)
    second = resource.get("second")
    if not isinstance(second, dict) or second.get("imported") != 0 or second.get("configured") != 0:
        errors.append(f"{node_id}: resource_evidence.second requires imported=0 and configured=0")
    runtime = resource.get("runtime")
    if not isinstance(runtime, dict) or runtime.get("added") != 0:
        errors.append(f"{node_id}: resource_evidence.runtime.added must be zero")


def _validate_schema6_components(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    values = node.get("component_evidence")
    if not isinstance(values, list) or not values:
        errors.append(f"{node_id}: component_evidence must be a non-empty array")
        return
    for index, item in enumerate(values):
        prefix = f"{node_id}.component_evidence[{index}]"
        if not isinstance(item, dict):
            errors.append(f"{prefix} must be an object")
            continue
        if not _non_empty_string(item.get("shared_asset")):
            errors.append(f"{prefix}.shared_asset is required")
        guid = item.get("guid")
        if not isinstance(guid, str) or not UNITY_GUID_RE.fullmatch(guid):
            errors.append(f"{prefix}.guid must be a 32-character Unity GUID")
        instance_chain = item.get("instance_chain")
        if not isinstance(instance_chain, list) or not instance_chain or any(
            not _non_empty_string(value) for value in instance_chain
        ):
            errors.append(f"{prefix}.instance_chain must be a non-empty string array")
        consumers = item.get("consumers")
        if not isinstance(consumers, list) or not consumers or any(not _non_empty_string(v) for v in consumers):
            errors.append(f"{prefix}.consumers must be a non-empty string array")
            consumer_set: set[str] = set()
        else:
            consumer_set = set(consumers)
            if len(consumer_set) != len(consumers):
                errors.append(f"{prefix}.consumers contains duplicates")
        groups = item.get("groups")
        group_ids: set[str] = set()
        grouped_consumers: list[str] = []
        if not isinstance(groups, list) or not groups:
            errors.append(f"{prefix}.groups must be a non-empty array")
        else:
            for group_index, group in enumerate(groups):
                if not isinstance(group, dict) or not _non_empty_string(group.get("id")):
                    errors.append(f"{prefix}.groups[{group_index}].id is required")
                    continue
                group_id = group["id"]
                if group_id in group_ids:
                    errors.append(f"{prefix}: duplicate group id {group_id}")
                group_ids.add(group_id)
                members = group.get("members")
                if not isinstance(members, list) or not members or any(not _non_empty_string(v) for v in members):
                    errors.append(f"{prefix}.groups[{group_index}].members must be non-empty")
                else:
                    grouped_consumers.extend(members)
        duplicate_members = sorted(
            member for member in set(grouped_consumers) if grouped_consumers.count(member) > 1
        )
        if duplicate_members:
            errors.append(f"{prefix}: consumers appear in multiple groups: {', '.join(duplicate_members)}")
        unknown_members = sorted(set(grouped_consumers) - consumer_set)
        if unknown_members:
            errors.append(f"{prefix}: group members not declared as consumers: {', '.join(unknown_members)}")
        ungrouped_consumers = sorted(consumer_set - set(grouped_consumers))
        if ungrouped_consumers:
            errors.append(f"{prefix}: consumers missing from groups: {', '.join(ungrouped_consumers)}")
        samples = item.get("samples")
        sampled: set[str] = set()
        if not isinstance(samples, list) or not samples:
            errors.append(f"{prefix}.samples must be a non-empty array")
        else:
            for sample_index, sample in enumerate(samples):
                sample_prefix = f"{prefix}.samples[{sample_index}]"
                if not isinstance(sample, dict):
                    errors.append(f"{sample_prefix} must be an object")
                    continue
                group_id = sample.get("group")
                if group_id not in group_ids:
                    errors.append(f"{sample_prefix}.group is not declared: {group_id!r}")
                else:
                    sampled.add(group_id)
                if not _non_empty_string(sample.get("host")):
                    errors.append(f"{sample_prefix}.host is required")
                if sample.get("result") != "pass":
                    errors.append(f"{sample_prefix}.result must be 'pass'")
                _validate_evidence_ref(
                    sample.get("artifact"), f"{sample_prefix}.artifact", errors, repo_root, hash_cache
                )
        missing_groups = sorted(group_ids - sampled)
        if missing_groups:
            errors.append(f"{prefix}: unsampled consumer groups: {', '.join(missing_groups)}")
        if item.get("root_or_lifecycle_changed") is True and item.get("high_frequency_included") is not True:
            errors.append(f"{prefix}.high_frequency_included must be true for root/lifecycle changes")


def _validate_schema6_component_matrix(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    matrices = node.get("component_state_evidence")
    if not isinstance(matrices, list) or not matrices:
        errors.append(f"{node_id}: component_state_evidence must be a non-empty array")
        return
    for matrix_index, matrix in enumerate(matrices):
        prefix = f"{node_id}.component_state_evidence[{matrix_index}]"
        if not isinstance(matrix, dict):
            errors.append(f"{prefix} must be an object")
            continue
        if not _non_empty_string(matrix.get("component")) or not _non_empty_string(matrix.get("viewport")):
            errors.append(f"{prefix}.component and viewport are required")
        states = matrix.get("states")
        if not isinstance(states, list) or not states:
            errors.append(f"{prefix}.states must be a non-empty array")
            continue
        applicable_count = 0
        for state_index, state in enumerate(states):
            state_prefix = f"{prefix}.states[{state_index}]"
            if not isinstance(state, dict) or not _non_empty_string(state.get("id")):
                errors.append(f"{state_prefix}.id is required")
                continue
            if state.get("applicable") is True:
                applicable_count += 1
                if state.get("result") != "pass":
                    errors.append(f"{state_prefix}.result must be 'pass' when applicable")
                _validate_evidence_ref(
                    state.get("artifact"), f"{state_prefix}.artifact", errors, repo_root, hash_cache
                )
            elif state.get("applicable") is False:
                if not _non_empty_string(state.get("reason")):
                    errors.append(f"{state_prefix}.reason is required when not applicable")
            else:
                errors.append(f"{state_prefix}.applicable must be boolean")
        if applicable_count == 0:
            errors.append(f"{prefix} must contain at least one applicable state")


def _validate_schema6_done_leaf(
    node_id: str,
    node: dict,
    ledger: dict,
    runs: dict[str, dict],
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    raw_applicable = node.get("applicable_gates")
    if not isinstance(raw_applicable, list) or not raw_applicable:
        errors.append(f"{node_id}: schema 6 done leaf requires non-empty applicable_gates")
        raw_applicable = []
    for index, gate in enumerate(raw_applicable):
        if not isinstance(gate, str):
            errors.append(f"{node_id}: applicable_gates[{index}] must be a string")
    applicable = [gate for gate in raw_applicable if isinstance(gate, str)]
    if len(applicable) != len(set(applicable)):
        errors.append(f"{node_id}: applicable_gates contains duplicates")
    unknown = [gate for gate in applicable if gate not in LEAF_GATES]
    for gate in unknown:
        errors.append(f"{node_id}: unknown gate {gate!r}")

    node_type = node.get("type")
    required = set(SCHEMA6_MINIMUM_GATES.get(node_type, ()))
    if node.get("risk") == "destructive-write":
        required.add("authorization")
    if node.get("risk") == "reversible-write":
        required.add("restore")
    missing_required = sorted(required - set(applicable))
    if missing_required:
        errors.append(f"{node_id}: type/risk requires gates: {', '.join(missing_required)}")

    gates = node.get("gates")
    gate_runs = node.get("gate_runs")
    gate_evidence = node.get("gate_evidence")
    if not isinstance(gates, dict):
        errors.append(f"{node_id}: gates must be an object")
        gates = {}
    if not isinstance(gate_runs, dict):
        errors.append(f"{node_id}: gate_runs must be an object")
        gate_runs = {}
    if not isinstance(gate_evidence, dict):
        errors.append(f"{node_id}: gate_evidence must be an object")
        gate_evidence = {}

    expected_gate_keys = set(applicable)
    for field_name, field_value in (
        ("gates", gates),
        ("gate_runs", gate_runs),
        ("gate_evidence", gate_evidence),
    ):
        extra = sorted(set(field_value) - expected_gate_keys)
        if extra:
            errors.append(f"{node_id}: {field_name} has non-applicable gates: {', '.join(extra)}")

    for gate in applicable:
        if gates.get(gate) is not True:
            errors.append(f"{node_id}: done leaf gate {gate!r} is not true")
        run_id = gate_runs.get(gate)
        if not isinstance(run_id, str):
            errors.append(f"{node_id}: gate_runs.{gate} must be a verification run id string")
        elif run_id not in runs:
            errors.append(f"{node_id}: gate_runs.{gate} references unknown run {run_id!r}")
        else:
            allowed = RUNTIME_GATE_ENVIRONMENTS.get(gate)
            environment = runs[run_id].get("environment")
            if allowed is not None and environment not in allowed:
                errors.append(
                    f"{node_id}: gate {gate!r} cannot use {environment!r} run {run_id!r}"
                )
        _validate_ref_list(
            gate_evidence.get(gate),
            f"{node_id}.gate_evidence.{gate}",
            errors,
            repo_root,
            hash_cache,
        )

    if "timing" in applicable:
        _validate_schema6_timing(node_id, node, errors)
    if "visual_version" in applicable:
        _validate_ref_list(
            node.get("version_evidence"),
            f"{node_id}.version_evidence",
            errors,
            repo_root,
            hash_cache,
        )
    if "visual_match" in applicable:
        _validate_schema6_visual(node_id, node, errors, repo_root, hash_cache)
    if "runtime_state" in applicable:
        _validate_ref_list(node.get("state_evidence"), f"{node_id}.state_evidence", errors, repo_root, hash_cache)
    if "target_identity" in applicable:
        _validate_semantic_artifact_items(
            node_id,
            node.get("identity_evidence"),
            "identity_evidence",
            ("view_type", "root_size", "background_rendered", "close_chain"),
            errors,
            repo_root,
            hash_cache,
        )
    if "layout_structure" in applicable:
        _validate_semantic_artifact_items(
            node_id,
            node.get("layout_evidence"),
            "layout_evidence",
            ("scroll_rect", "viewport_mask", "content_layout", "content_fitter"),
            errors,
            repo_root,
            hash_cache,
        )
    if "scroll_interaction" in applicable:
        _validate_schema6_interaction(node_id, node.get("interaction_evidence"), errors, repo_root, hash_cache)
    if "page_space_geometry" in applicable:
        _validate_schema6_geometry(node_id, node.get("geometry_evidence"), errors, repo_root, hash_cache)
    if "model_presentation" in applicable:
        _validate_schema6_model(node_id, node, errors, repo_root, hash_cache)
    if "render_completion" in applicable:
        _validate_schema6_render(node_id, node, errors, repo_root, hash_cache)
    if "effect_match" in applicable:
        _validate_schema6_effect(node_id, node, errors, repo_root, hash_cache)
    if "resource_stable" in applicable:
        _validate_schema6_resource(node_id, node, errors, repo_root, hash_cache)
    if "shared_component_identity" in applicable:
        _validate_schema6_components(node_id, node, errors, repo_root, hash_cache)
    if "component_state_matrix" in applicable:
        _validate_schema6_component_matrix(node_id, node, errors, repo_root, hash_cache)
    if "authorization" in applicable:
        _validate_ref_list(
            node.get("authorization_evidence"),
            f"{node_id}.authorization_evidence",
            errors,
            repo_root,
            hash_cache,
        )


def _validate_inventory(
    node_id: str,
    node: dict,
    direct_children: set[str],
    errors: list[str],
    *,
    exact: bool,
) -> None:
    inventory = node.get("control_inventory")
    if not isinstance(inventory, list) or not inventory:
        errors.append(f"{node_id}: page requires non-empty control_inventory[]")
        return
    control_ids: set[str] = set()
    referenced_children: list[str] = []
    for index, control in enumerate(inventory):
        if not isinstance(control, dict):
            errors.append(f"{node_id}: control_inventory[{index}] must be an object")
            continue
        control_id = control.get("id")
        kind = control.get("kind")
        child = control.get("child")
        if not _non_empty_string(control_id):
            errors.append(f"{node_id}: control_inventory[{index}].id is required")
        elif control_id in control_ids:
            errors.append(f"{node_id}: duplicate control id {control_id}")
        else:
            control_ids.add(control_id)
        if not _non_empty_string(kind):
            errors.append(f"{node_id}: control_inventory[{index}].kind is required")
        if not isinstance(child, str):
            errors.append(f"{node_id}: control_inventory[{index}].child must be a string")
        elif child not in direct_children:
            errors.append(f"{node_id}: control_inventory[{index}].child {child!r} is not a direct child")
        else:
            referenced_children.append(child)
    if exact:
        duplicates = sorted(child for child, count in Counter(referenced_children).items() if count > 1)
        if duplicates:
            errors.append(f"{node_id}: multiple controls map the same child: {', '.join(duplicates)}")
        missing = sorted(direct_children - set(referenced_children))
        if missing:
            errors.append(f"{node_id}: control_inventory omits direct children: {', '.join(missing)}")


def _validate_inventory_evidence(
    node_id: str,
    node: dict,
    errors: list[str],
    repo_root: Path,
    hash_cache: dict[Path, str],
) -> None:
    value = node.get("inventory_evidence")
    if not isinstance(value, dict):
        errors.append(f"{node_id}: done schema 6 page requires inventory_evidence")
        return
    for field in ("legacy_runtime", "legacy_source", "unity_source"):
        _validate_evidence_ref(value.get(field), f"{node_id}.inventory_evidence.{field}", errors, repo_root, hash_cache)
    if value.get("reconciled") is not True:
        errors.append(f"{node_id}: inventory_evidence.reconciled must be true")


def _detect_cycles(by_id: dict[str, dict], errors: list[str]) -> None:
    reported: set[frozenset[str]] = set()
    for node_id in by_id:
        order: list[str] = []
        indexes: dict[str, int] = {}
        current = node_id
        while current in by_id:
            if current in indexes:
                cycle = frozenset(order[indexes[current] :])
                if cycle not in reported:
                    errors.append(f"parent cycle detected: {' -> '.join(order[indexes[current]:] + [current])}")
                    reported.add(cycle)
                break
            indexes[current] = len(order)
            order.append(current)
            parent = by_id[current].get("parent")
            if not isinstance(parent, str) or not parent:
                break
            current = parent


def _status_counts(nodes: list[dict]) -> dict[str, int]:
    values = []
    for node in nodes:
        if not isinstance(node, dict):
            values.append("invalid-node")
            continue
        status = node.get("status", "missing")
        values.append(status if isinstance(status, str) else "invalid-status")
    return dict(sorted(Counter(values).items()))


def _derived_parent_status(direct_children: list[dict]) -> str:
    if all(child.get("status") == "done" for child in direct_children):
        return "done"
    return next(
        (
            status
            for status in PARENT_STATUS_PRIORITY
            if any(child.get("status") == status for child in direct_children)
        ),
        "not-run",
    )


def _refresh_summary(ledger: dict) -> None:
    nodes = ledger.get("nodes") or []
    summary = ledger.get("summary")
    if not isinstance(summary, dict):
        summary = {}
    summary["total"] = len(nodes)
    summary["status_counts"] = _status_counts(nodes)
    ledger["summary"] = summary


def validate_ledger_data(ledger, path: Path, quiet: bool = False) -> int:
    errors: list[str] = []
    if not isinstance(ledger, dict):
        errors.append("ledger root must be an object")
        ledger = {}

    schema = ledger.get("schema", 4)
    if not isinstance(schema, int) or isinstance(schema, bool) or schema not in SUPPORTED_SCHEMAS:
        errors.append(f"unsupported schema: {schema!r}")
        schema = 4

    nodes = ledger.get("nodes")
    if not isinstance(nodes, list) or not nodes:
        errors.append("nodes must be a non-empty array")
        nodes = []

    by_id: dict[str, dict] = {}
    children_by_parent: dict[str, list[dict]] = {}
    for index, node in enumerate(nodes):
        if not isinstance(node, dict):
            errors.append(f"nodes[{index}] must be an object")
            continue
        node_id = node.get("id")
        if not _non_empty_string(node_id):
            errors.append(f"nodes[{index}].id is required")
            continue
        if node_id in by_id:
            errors.append(f"duplicate id: {node_id}")
        by_id[node_id] = node
        status = node.get("status")
        if not isinstance(status, str) or status not in STATUSES:
            errors.append(f"{node_id}: invalid status {status!r}")
        parent = node.get("parent")
        if parent is not None and parent != "" and not isinstance(parent, str):
            errors.append(f"{node_id}: parent must be a string or null")
        elif parent:
            children_by_parent.setdefault(parent, []).append(node)
        if schema >= 6:
            if not isinstance(node.get("type"), str) or node.get("type") not in NODE_TYPES:
                errors.append(f"{node_id}: invalid schema 6 type {node.get('type')!r}")
            if not isinstance(node.get("risk"), str) or node.get("risk") not in RISKS:
                errors.append(f"{node_id}: invalid schema 6 risk {node.get('risk')!r}")
            if status == "blocked" and not _non_empty_string(node.get("blocked_reason")):
                errors.append(f"{node_id}: blocked status requires blocked_reason")
            if status == "needs-runtime-verify" and not _non_empty_string(node.get("runtime_gap")):
                errors.append(f"{node_id}: needs-runtime-verify requires runtime_gap")

    for node_id, node in by_id.items():
        parent = node.get("parent")
        if isinstance(parent, str) and parent and parent not in by_id:
            errors.append(f"{node_id}: missing parent {parent}")
    _detect_cycles(by_id, errors)

    roots = [node for node in by_id.values() if not node.get("parent")]
    if schema >= 6:
        if len(roots) != 1:
            errors.append(f"schema 6 ledger requires exactly one root, found {len(roots)}")
        elif roots[0].get("type") != "page":
            errors.append("schema 6 root must be type=page")

    repo_root = _repo_root(path)
    hash_cache: dict[Path, str] = {}
    if schema >= 6:
        _validate_manifest_binding(ledger, by_id, errors, repo_root, hash_cache)
        runs = _validate_verification_runs(ledger, errors, repo_root, hash_cache)
    else:
        runs = {}

    for node_id, node in by_id.items():
        direct_children_list = children_by_parent.get(node_id, [])
        direct_children = {child.get("id") for child in direct_children_list if child.get("id")}
        status = node.get("status")

        if not direct_children_list and status == "done":
            if schema >= 6:
                if node.get("type") == "page":
                    errors.append(f"{node_id}: schema 6 page cannot be a leaf")
                _validate_schema6_done_leaf(
                    node_id, node, ledger, runs, errors, repo_root, hash_cache
                )
            else:
                _validate_legacy_done_leaf(node_id, node, schema, errors)

        if direct_children_list:
            if schema >= 6:
                expected_parent_status = _derived_parent_status(direct_children_list)
                if status != expected_parent_status:
                    errors.append(
                        f"{node_id}: parent status {status!r} must be derived as {expected_parent_status!r}"
                    )
            if status == "done":
                unfinished = [child["id"] for child in direct_children_list if child.get("status") != "done"]
                if unfinished:
                    errors.append(f"{node_id}: done parent has unfinished children: {', '.join(unfinished)}")
            if node.get("type") == "page" and (schema >= 6 or status == "done"):
                _validate_inventory(node_id, node, direct_children, errors, exact=schema >= 6)
            if schema >= 6 and status == "done" and node.get("type") == "page":
                _validate_inventory_evidence(node_id, node, errors, repo_root, hash_cache)
        elif schema >= 6 and node.get("type") == "page":
            errors.append(f"{node_id}: schema 6 page must enumerate at least one child")

    if schema >= 6:
        summary = ledger.get("summary")
        expected_counts = _status_counts(nodes)
        if not isinstance(summary, dict):
            errors.append("schema 6 ledger requires summary")
        else:
            if summary.get("total") != len(nodes):
                errors.append("summary.total does not match nodes[]")
            if summary.get("status_counts") != expected_counts:
                errors.append("summary.status_counts does not match nodes[]")

        if len(roots) == 1 and roots[0].get("status") == "done":
            route_run_id = ledger.get("route_run_id")
            route_run = runs.get(route_run_id) if isinstance(route_run_id, str) else None
            if not isinstance(route_run_id, str) or route_run is None:
                errors.append(f"done root requires route_run_id referencing a verification run: {route_run_id!r}")
            elif route_run.get("environment") != "real-web":
                errors.append("done root route_run_id must reference environment=real-web")
            elif _is_timezone_iso8601(route_run.get("recorded_at")):
                route_recorded_at = _parse_timezone_iso8601(route_run["recorded_at"])
                for leaf_id, leaf in by_id.items():
                    if children_by_parent.get(leaf_id) or leaf.get("status") != "done":
                        continue
                    gate_runs = leaf.get("gate_runs") if isinstance(leaf.get("gate_runs"), dict) else {}
                    for gate, run_id in gate_runs.items():
                        gate_run = runs.get(run_id) if isinstance(run_id, str) else None
                        if not isinstance(gate_run, dict):
                            continue
                        for fingerprint in ("git_commit", "dirty_fingerprint"):
                            gate_value = gate_run.get(fingerprint)
                            route_value = route_run.get(fingerprint)
                            if (
                                isinstance(gate_value, str)
                                and isinstance(route_value, str)
                                and gate_value.lower() != route_value.lower()
                            ):
                                errors.append(
                                    f"{leaf_id}: gate {gate!r} run {run_id!r} {fingerprint} differs from route run"
                                )
                        recorded_at = gate_run.get("recorded_at")
                        if _is_timezone_iso8601(recorded_at) and _parse_timezone_iso8601(recorded_at) > route_recorded_at:
                            errors.append(
                                f"{leaf_id}: gate {gate!r} run {run_id!r} is newer than route run {route_run_id!r}"
                            )
        elif len(roots) == 1 and ledger.get("route_run_id") is not None:
            errors.append("non-done root must not retain route_run_id")

    if not quiet:
        print(f"route={ledger.get('route', '')} schema={schema} nodes={len(nodes)} status={_status_counts(nodes)}")
        for error in errors:
            print(f"ERROR: {error}")
    return 1 if errors else 0


def validate_ledger(path: Path, quiet: bool = False) -> int:
    return validate_ledger_data(read_json(path), path, quiet)


def init_ledger(manifest: Path, output: Path) -> int:
    if output.exists():
        print(f"ERROR: refusing to overwrite existing ledger with init: {output}", file=sys.stderr)
        return 1
    source = read_json(manifest)
    if isinstance(source, dict):
        nodes = source.get("nodes")
        route = source.get("route", output.stem)
        baseline = source.get("baseline", {})
    elif isinstance(source, list):
        nodes = source
        route = output.stem
        baseline = {}
    else:
        raise ValueError("manifest must be a node array or contain nodes[]")
    if not isinstance(nodes, list):
        raise ValueError("manifest must be a node array or contain nodes[]")

    resolved_manifest = manifest.resolve()
    try:
        manifest_path_value = resolved_manifest.relative_to(_repo_root(output)).as_posix()
    except ValueError:
        manifest_path_value = str(resolved_manifest)

    ledger = {
        "schema": CURRENT_SCHEMA,
        "route": route,
        "baseline": baseline,
        "manifest_source": {
            "path": manifest_path_value,
            "sha256": _sha256(resolved_manifest),
        },
        "verification_runs": {},
        "nodes": [],
    }
    for index, item in enumerate(nodes):
        if not isinstance(item, dict):
            raise ValueError(f"manifest nodes[{index}] must be an object")
        node = dict(item)
        node.setdefault("parent", None)
        node.setdefault("type", "read")
        node.setdefault("status", "not-run")
        node.setdefault("risk", "read-only")
        node.setdefault("gates", {})
        node.setdefault("gate_runs", {})
        node.setdefault("gate_evidence", {})
        node.setdefault("evidence", [])
        ledger["nodes"].append(node)
    _refresh_summary(ledger)
    if validate_ledger_data(ledger, output, quiet=False) != 0:
        return 1
    with _ledger_write_lock(output):
        if output.exists():
            print(f"ERROR: refusing to overwrite existing ledger with init: {output}", file=sys.stderr)
            return 1
        write_json_atomic(output, ledger)
    return 0


def _archive_and_invalidate(node: dict, gates: list[str], result: dict) -> None:
    archived_gate_evidence = {}
    archived_gate_runs = {}
    current_gate_evidence = node.get("gate_evidence") if isinstance(node.get("gate_evidence"), dict) else {}
    current_gate_runs = node.get("gate_runs") if isinstance(node.get("gate_runs"), dict) else {}
    current_gates = node.get("gates") if isinstance(node.get("gates"), dict) else {}
    for gate in gates:
        if gate in current_gate_evidence:
            archived_gate_evidence[gate] = current_gate_evidence[gate]
        if gate in current_gate_runs:
            archived_gate_runs[gate] = current_gate_runs[gate]
        current_gate_evidence.pop(gate, None)
        current_gate_runs.pop(gate, None)
        current_gates[gate] = False
        for field in GATE_DETAIL_FIELDS.get(gate, ()):
            node.pop(field, None)
    if archived_gate_evidence or archived_gate_runs:
        node.setdefault("evidence_history", []).append(
            {
                "invalidated_gates": gates,
                "reason": result.get("invalidation_reason"),
                "observed_at": result.get("observed_at"),
                "gate_runs": archived_gate_runs,
                "gate_evidence": archived_gate_evidence,
            }
        )
    node["gates"] = current_gates
    node["gate_runs"] = current_gate_runs
    node["gate_evidence"] = current_gate_evidence


def _merge_schema6_result(node: dict, result: dict) -> None:
    new_status = result.get("status")
    if new_status == "done":
        if node.get("type") == "page":
            raise ValueError(f"{node.get('id')}: page status is derived from children and cannot be set to done")
        applicable = result.get("applicable_gates")
        gates = result.get("gates")
        gate_runs = result.get("gate_runs")
        gate_evidence = result.get("gate_evidence")
        if not isinstance(applicable, list) or not applicable:
            raise ValueError(f"{node.get('id')}: done result must explicitly provide applicable_gates")
        for field_name, value in (
            ("gates", gates),
            ("gate_runs", gate_runs),
            ("gate_evidence", gate_evidence),
        ):
            if not isinstance(value, dict):
                raise ValueError(f"{node.get('id')}: done result must explicitly provide {field_name}")
        for gate in applicable:
            if gates.get(gate) is not True or gate not in gate_runs or gate not in gate_evidence:
                raise ValueError(
                    f"{node.get('id')}: done result must explicitly refresh gate/run/evidence for {gate!r}"
                )
            for detail_field in GATE_DETAIL_FIELDS.get(gate, ()):
                if detail_field not in result:
                    raise ValueError(
                        f"{node.get('id')}: done result must explicitly provide {detail_field} for {gate!r}"
                    )
        for gate, detail_fields in GATE_DETAIL_FIELDS.items():
            if gate not in applicable:
                for detail_field in detail_fields:
                    node.pop(detail_field, None)
        node["gates"] = {}
        node["gate_runs"] = {}
        node["gate_evidence"] = {}
    invalidates = result.get("invalidate_gates")
    if invalidates is None and node.get("status") == "done" and new_status and new_status != "done":
        invalidates = list(node.get("applicable_gates") or [])
    if invalidates:
        if not isinstance(invalidates, list) or any(
            not isinstance(gate, str) or gate not in LEAF_GATES for gate in invalidates
        ):
            raise ValueError(f"{node.get('id')}: invalidate_gates contains unknown gates")
        if not _non_empty_string(result.get("invalidation_reason")):
            raise ValueError(f"{node.get('id')}: invalidation_reason is required")
        if not _is_timezone_iso8601(result.get("observed_at")):
            raise ValueError(f"{node.get('id')}: observed_at must be timezone-aware ISO-8601")
        _archive_and_invalidate(node, list(dict.fromkeys(invalidates)), result)

    merge_maps = {"gates", "gate_runs", "gate_evidence"}
    replace_fields = RESULT_FIELDS - {
        "id",
        "evidence",
        "invalidate_gates",
        "invalidation_reason",
        "observed_at",
        *merge_maps,
    }
    for key in replace_fields:
        if key in result:
            node[key] = result[key]
    for key in merge_maps:
        if key in result:
            value = result[key]
            if not isinstance(value, dict):
                raise ValueError(f"{node.get('id')}: {key} must be an object")
            current = node.setdefault(key, {})
            if not isinstance(current, dict):
                current = {}
                node[key] = current
            current.update(value)
    if "evidence" in result:
        current = node.setdefault("evidence", [])
        for value in result["evidence"]:
            if value not in current:
                current.append(value)


def _merge_legacy_result(node: dict, result: dict) -> None:
    for key in (
        "status",
        "risk",
        "applicable_gates",
        "gates",
        "timing",
        "note",
        "blocked_reason",
        "runtime_gap",
        "inventory_evidence",
        "visual_evidence",
        "version_evidence",
        "identity_evidence",
        "layout_evidence",
        "interaction_evidence",
        "geometry_evidence",
        "render_evidence",
        "state_evidence",
        "model_evidence",
        "effect_evidence",
        "resource_evidence",
        "component_evidence",
        "component_state_evidence",
        "authorization_evidence",
    ):
        if key in result:
            node[key] = result[key]
    if "evidence" in result:
        current = node.setdefault("evidence", [])
        for value in result["evidence"]:
            if value not in current:
                current.append(value)


def _merge_top_level_schema6(candidate: dict, source: dict) -> None:
    incoming_runs = source.get("verification_runs", {})
    if incoming_runs:
        if not isinstance(incoming_runs, dict):
            raise ValueError("verification_runs update must be an object")
        runs = candidate.setdefault("verification_runs", {})
        for run_id, value in incoming_runs.items():
            if run_id in runs and runs[run_id] != value:
                raise ValueError(f"verification run {run_id!r} already exists with different content")
            runs[run_id] = value
    for field in ("route_run_id", "baseline", "updated_at"):
        if field in source:
            candidate[field] = source[field]
    if "summary_details" in source:
        details = source["summary_details"]
        if not isinstance(details, dict):
            raise ValueError("summary_details must be an object")
        candidate.setdefault("summary", {})["details"] = details


def _derive_parent_statuses(nodes: list[dict]) -> None:
    by_id = {node["id"]: node for node in nodes}
    children: dict[str, list[dict]] = {}
    for node in nodes:
        parent = node.get("parent")
        if parent:
            children.setdefault(parent, []).append(node)
    changed = True
    while changed:
        changed = False
        for parent_id, direct_children in children.items():
            parent = by_id[parent_id]
            derived = _derived_parent_status(direct_children)
            if parent.get("status") != derived:
                parent["status"] = derived
                changed = True
            if derived == "blocked":
                blocked_children = [child["id"] for child in direct_children if child.get("status") == "blocked"]
                parent["blocked_reason"] = f"unfinished blocked children: {', '.join(blocked_children)}"
                parent.pop("runtime_gap", None)
            elif derived == "needs-runtime-verify":
                runtime_children = [
                    child["id"] for child in direct_children if child.get("status") == "needs-runtime-verify"
                ]
                parent["runtime_gap"] = f"unfinished runtime verification children: {', '.join(runtime_children)}"
                parent.pop("blocked_reason", None)
            else:
                parent.pop("blocked_reason", None)
                parent.pop("runtime_gap", None)


def _apply_results_locked(ledger_path: Path, source) -> int:
    ledger = read_json(ledger_path)
    if validate_ledger_data(ledger, ledger_path, quiet=True) != 0:
        print("ERROR: refusing to apply results to an invalid ledger", file=sys.stderr)
        validate_ledger_data(ledger, ledger_path, quiet=False)
        return 1

    if isinstance(source, dict):
        results = source.get("nodes")
    else:
        results = source
    if not isinstance(results, list):
        raise ValueError("results must be a node array or contain nodes[]")

    candidate = copy.deepcopy(ledger)
    schema = candidate.get("schema", 4)
    nodes = candidate.get("nodes")
    by_id = {node.get("id"): node for node in nodes if node.get("id")}
    seen_results: set[str] = set()
    for index, result in enumerate(results):
        if not isinstance(result, dict):
            raise ValueError(f"results[{index}] must be an object")
        node_id = result.get("id")
        if node_id not in by_id:
            raise ValueError(f"result references unknown id: {node_id}")
        if node_id in seen_results:
            raise ValueError(f"duplicate result id: {node_id}")
        seen_results.add(node_id)
        if schema >= 6:
            unknown_fields = sorted(set(result) - RESULT_FIELDS)
            if unknown_fields:
                raise ValueError(f"{node_id}: unknown result fields: {', '.join(unknown_fields)}")
            _merge_schema6_result(by_id[node_id], result)
        else:
            _merge_legacy_result(by_id[node_id], result)

    if schema >= 6 and isinstance(source, dict):
        _merge_top_level_schema6(candidate, source)
    _derive_parent_statuses(nodes)
    if schema >= 6:
        roots = [node for node in nodes if not node.get("parent")]
        if len(roots) == 1 and roots[0].get("status") != "done":
            candidate.pop("route_run_id", None)
    _refresh_summary(candidate)

    if validate_ledger_data(candidate, ledger_path, quiet=True) != 0:
        print("ERROR: candidate ledger failed validation; official ledger was not changed", file=sys.stderr)
        validate_ledger_data(candidate, ledger_path, quiet=False)
        return 1
    write_json_atomic(ledger_path, candidate)
    return 0


def apply_results(ledger_path: Path, results_path: Path) -> int:
    """Lock, validate, merge, and atomically replace one official ledger."""
    source = read_json(results_path)
    with _ledger_write_lock(ledger_path):
        return _apply_results_locked(ledger_path, source)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    init_parser = sub.add_parser("init", help="create a schema-6 ledger from a compact manifest")
    init_parser.add_argument("manifest", type=Path)
    init_parser.add_argument("output", type=Path)

    validate_parser = sub.add_parser("validate", help="validate statuses, evidence, and completion gates")
    validate_parser.add_argument("ledger", type=Path)
    validate_parser.add_argument("--quiet", action="store_true")

    apply_parser = sub.add_parser("apply", help="atomically merge results and roll up parent statuses")
    apply_parser.add_argument("ledger", type=Path)
    apply_parser.add_argument("results", type=Path)

    args = parser.parse_args()
    try:
        if args.command == "init":
            return init_ledger(args.manifest, args.output)
        if args.command == "apply":
            return apply_results(args.ledger, args.results)
        return validate_ledger(args.ledger, args.quiet)
    except (OSError, ValueError, TypeError, json.JSONDecodeError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
