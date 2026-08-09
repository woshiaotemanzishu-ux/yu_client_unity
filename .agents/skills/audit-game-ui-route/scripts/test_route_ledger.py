#!/usr/bin/env python3
"""Fault-injection checks for the UI route ledger completion gates."""

from __future__ import annotations

import contextlib
import copy
import hashlib
import io
import json
import tempfile
from pathlib import Path

from route_ledger import CURRENT_SCHEMA, _ledger_write_lock, apply_results, init_ledger, validate_ledger


def write_case(folder: Path, name: str, node: dict, schema: int = 5) -> Path:
    path = folder / f"{name}.json"
    path.write_text(
        json.dumps({"schema": schema, "route": name, "nodes": [node]}, ensure_ascii=False),
        encoding="utf-8",
    )
    return path


def complete_node() -> dict:
    gates = [
        "click",
        "result",
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
        "shared_component_identity",
        "component_state_matrix",
    ]
    return {
        "id": "page.state",
        "status": "done",
        "applicable_gates": gates,
        "gates": {gate: True for gate in gates},
        "visual_evidence": {"old": "old.png", "unity": "unity.png", "diff": "diff.png"},
        "identity_evidence": ["view-type-and-background.json"],
        "layout_evidence": ["scrollrect-tree.json"],
        "interaction_evidence": ["real-drag.log"],
        "geometry_evidence": ["page-space-rects.json"],
        "state_evidence": ["state.json"],
        "model_evidence": {"old": "old.png", "unity": "unity.png"},
        "render_evidence": ["rt-pixel-probe.json"],
        "effect_evidence": ["effect-diff.png"],
        "component_evidence": ["shared-prefab-guid.json"],
        "component_state_evidence": ["component-state-matrix.json"],
        "resource_evidence": {
            "preflight_first": "preflight-first.log",
            "preflight_second": "preflight-second.log",
            "runtime_delta": "runtime-delta.json",
        },
        "timing": {"cold_ms": 1200, "warm_ms": 80},
    }


def artifact(path: Path) -> dict:
    return {
        "path": str(path.resolve()),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def assertion(text: str = "runtime state matched") -> dict:
    return {
        "assertion": text,
        "source": "fault-injection-test",
        "scope": "single synthetic route leaf",
        "observed_at": "2026-08-08T12:00:00+08:00",
    }


def schema6_ledger(folder: Path, *, effect: bool = False) -> dict:
    evidence_file = folder / "evidence.log"
    evidence_file.write_text("immutable evidence\n", encoding="utf-8")
    ref = artifact(evidence_file)
    real_web = {
        "recorded_at": "2026-08-08T12:00:00+08:00",
        "environment": "real-web",
        "git_commit": "a" * 40,
        "dirty_fingerprint": "b" * 64,
        "player_sha256": "c" * 64,
        "catalog_sha256": "d" * 64,
        "viewports": ["720x1280", "1920x1080"],
        "old_session_disconnected": True,
        "unity_session_valid": True,
        "report": ref,
    }
    editor = {
        "recorded_at": "2026-08-08T11:30:00+08:00",
        "environment": "unity-editor",
        "git_commit": "a" * 40,
        "dirty_fingerprint": "b" * 64,
        "unity_version": "6000.3.17f1",
    }
    root = {
        "id": "route",
        "parent": None,
        "type": "page",
        "risk": "read-only",
        "status": "done",
        "control_inventory": [{"id": "leaf", "kind": "state", "child": "route.leaf"}],
        "inventory_evidence": {
            "legacy_runtime": ref,
            "legacy_source": ref,
            "unity_source": ref,
            "reconciled": True,
        },
        "gates": {},
        "gate_runs": {},
        "gate_evidence": {},
        "evidence": [],
    }
    applicable = ["runtime_state"]
    gate_run = "web"
    leaf = {
        "id": "route.leaf",
        "parent": "route",
        "type": "read",
        "risk": "read-only",
        "status": "done",
        "applicable_gates": applicable,
        "gates": {"runtime_state": True},
        "gate_runs": {"runtime_state": gate_run},
        "gate_evidence": {"runtime_state": [ref]},
        "state_evidence": [ref],
        "evidence": [],
    }
    if effect:
        applicable.extend(["render_completion", "effect_match"])
        leaf["gates"].update({"render_completion": True, "effect_match": True})
        leaf["gate_runs"].update({"runtime_state": "editor", "render_completion": "editor", "effect_match": "editor"})
        leaf["gate_evidence"].update({"render_completion": [ref], "effect_match": [ref]})
        leaf["render_evidence"] = [
            {"artifact": ref, "render_completed": True, "nontransparent_pixels": 100}
        ]
        leaf["effect_evidence"] = [
            {
                "owner": "shared-slot",
                "legacy_call": {
                    "effect_name": "UI_test",
                    "parent": "effect_con",
                    "position": [0, 0],
                    "scale": [1, 1, 1],
                    "rotation": [0, 0, 0],
                    "loop": True,
                    "render_size": [140, 140],
                },
                "driver": {
                    "animated_property": "_BaseMap_ST",
                    "material_property": "_BaseMap_ST",
                    "shader_branch": "_UseBaseMapST=1",
                    "consumed": True,
                },
                "render": {
                    "handle": "slot-1",
                    "frame_a": ref,
                    "frame_b": ref,
                    "time_a": 0.0,
                    "time_b": 0.5,
                    "pixel_diff": 25,
                    "nontransparent_pixels": 100,
                    "alpha_bbox": {"width": 40, "height": 30},
                },
                "lifecycle": {"hide": True, "reopen": True},
                "scroll_viewport": True,
                "mask_states": {
                    "full": {"artifact": ref, "alpha_pixels": 100},
                    "partial": {"artifact": ref, "alpha_pixels": 40},
                    "hidden": {"artifact": ref, "alpha_pixels": 0},
                },
            }
        ]
    manifest_file = folder / "schema6-test-manifest.json"
    manifest_file.write_text(
        json.dumps(
            {
                "route": "schema6-test",
                "nodes": [
                    {
                        "id": "route",
                        "parent": None,
                        "type": "page",
                        "risk": "read-only",
                        "control_inventory": root["control_inventory"],
                    },
                    {
                        "id": "route.leaf",
                        "parent": "route",
                        "type": "read",
                        "risk": "read-only",
                    },
                ],
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    return {
        "schema": CURRENT_SCHEMA,
        "route": "schema6-test",
        "manifest_source": artifact(manifest_file),
        "verification_runs": {"web": real_web, "editor": editor},
        "route_run_id": "web",
        "summary": {"total": 2, "status_counts": {"done": 2}},
        "nodes": [root, leaf],
    }


def write_ledger(folder: Path, name: str, ledger: dict) -> Path:
    path = folder / f"{name}.json"
    path.write_text(json.dumps(ledger, ensure_ascii=False, indent=2), encoding="utf-8")
    return path


def main() -> int:
    with tempfile.TemporaryDirectory() as temp:
        folder = Path(temp)

        lock_target = folder / "locked-ledger.json"
        with _ledger_write_lock(lock_target):
            try:
                with _ledger_write_lock(lock_target):
                    raise AssertionError("second writer unexpectedly acquired the same ledger lock")
            except OSError:
                pass

        good = complete_node()
        assert validate_ledger(write_case(folder, "good", good), quiet=True) == 0

        missing_diff = complete_node()
        missing_diff["visual_evidence"].pop("diff")
        assert validate_ledger(write_case(folder, "missing-diff", missing_diff), quiet=True) == 1

        missing_state = complete_node()
        missing_state["state_evidence"] = []
        assert validate_ledger(write_case(folder, "missing-state", missing_state), quiet=True) == 1

        missing_identity = complete_node()
        missing_identity["identity_evidence"] = []
        assert validate_ledger(write_case(folder, "missing-identity", missing_identity), quiet=True) == 1

        missing_drag = complete_node()
        missing_drag.pop("interaction_evidence")
        assert validate_ledger(write_case(folder, "missing-drag", missing_drag), quiet=True) == 1

        missing_model = complete_node()
        missing_model.pop("model_evidence")
        assert validate_ledger(write_case(folder, "missing-model", missing_model), quiet=True) == 1

        missing_render = complete_node()
        missing_render["render_evidence"] = []
        assert validate_ledger(write_case(folder, "missing-render", missing_render), quiet=True) == 1

        missing_timing = complete_node()
        missing_timing.pop("timing")
        assert validate_ledger(write_case(folder, "missing-timing", missing_timing), quiet=True) == 1

        missing_resource = complete_node()
        missing_resource["resource_evidence"].pop("preflight_second")
        assert validate_ledger(write_case(folder, "missing-resource", missing_resource), quiet=True) == 1

        missing_component = complete_node()
        missing_component["component_evidence"] = []
        assert validate_ledger(write_case(folder, "missing-component", missing_component), quiet=True) == 1

        missing_component_state = complete_node()
        missing_component_state.pop("component_state_evidence")
        assert validate_ledger(
            write_case(folder, "missing-component-state", missing_component_state), quiet=True
        ) == 1

        legacy_default = {
            "id": "legacy.read",
            "status": "done",
            "gates": {gate: True for gate in (
                "click", "result", "protocol", "immediate", "reopen", "return_chain", "timing",
                "visual_version", "visual_match", "target_identity", "layout_structure",
                "scroll_interaction", "page_space_geometry", "runtime_state", "model_presentation",
                "render_completion", "effect_match", "resource_stable", "restore",
            )},
            "timing": {"cold_ms": 1, "warm_ms": 1},
            "visual_evidence": {"old": "old", "unity": "unity", "diff": "diff"},
            "identity_evidence": ["identity"],
            "layout_evidence": ["layout"],
            "interaction_evidence": ["drag"],
            "geometry_evidence": ["geometry"],
            "state_evidence": ["state"],
            "model_evidence": {"old": "old", "unity": "unity"},
            "render_evidence": ["render"],
            "effect_evidence": ["effect"],
            "resource_evidence": {
                "preflight_first": "first", "preflight_second": "second", "runtime_delta": "delta"
            },
        }
        assert validate_ledger(write_case(folder, "legacy-default", legacy_default, schema=4), quiet=True) == 0

        no_model_page = complete_node()
        no_model_page["applicable_gates"].remove("model_presentation")
        no_model_page["gates"].pop("model_presentation")
        no_model_page.pop("model_evidence")
        no_model_page["applicable_gates"].remove("render_completion")
        no_model_page["gates"].pop("render_completion")
        no_model_page.pop("render_evidence")
        assert validate_ledger(write_case(folder, "no-model-page", no_model_page), quiet=True) == 0

        unknown_gate = complete_node()
        unknown_gate["applicable_gates"].append("looks_fine")
        unknown_gate["gates"]["looks_fine"] = True
        assert validate_ledger(write_case(folder, "unknown-gate", unknown_gate), quiet=True) == 1

        page = {
            "id": "page",
            "type": "page",
            "status": "done",
            "control_inventory": [{"id": "change", "kind": "button", "child": "page.change"}],
        }
        child = complete_node()
        child["id"] = "page.change"
        child["parent"] = "page"
        page_path = folder / "page-good.json"
        page_path.write_text(
            json.dumps({"schema": 5, "route": "page-good", "nodes": [page, child]}), encoding="utf-8"
        )
        assert validate_ledger(page_path, quiet=True) == 0

        bad_page = dict(page)
        bad_page.pop("control_inventory")
        bad_page_path = folder / "page-missing-inventory.json"
        bad_page_path.write_text(
            json.dumps({"schema": 5, "route": "page-missing-inventory", "nodes": [bad_page, child]}),
            encoding="utf-8",
        )
        assert validate_ledger(bad_page_path, quiet=True) == 1

        empty_schema5 = complete_node()
        empty_schema5["applicable_gates"] = []
        empty_schema5["gates"] = {}
        assert validate_ledger(write_case(folder, "schema5-empty-gates", empty_schema5), quiet=True) == 1

        schema6_good = schema6_ledger(folder)
        schema6_good_path = write_ledger(folder, "schema6-good", schema6_good)
        assert validate_ledger(schema6_good_path, quiet=True) == 0

        schema6_missing_manifest = copy.deepcopy(schema6_good)
        schema6_missing_manifest.pop("manifest_source")
        assert validate_ledger(
            write_ledger(folder, "schema6-missing-manifest", schema6_missing_manifest), quiet=True
        ) == 1

        drift_manifest = folder / "schema6-drift-manifest.json"
        base_manifest = Path(schema6_good["manifest_source"]["path"])
        drift_manifest.write_bytes(base_manifest.read_bytes())
        schema6_manifest_hash_drift = copy.deepcopy(schema6_good)
        schema6_manifest_hash_drift["manifest_source"] = artifact(drift_manifest)
        drift_manifest.write_text('{"route":"changed","nodes":[]}', encoding="utf-8")
        assert validate_ledger(
            write_ledger(folder, "schema6-manifest-hash-drift", schema6_manifest_hash_drift), quiet=True
        ) == 1

        topology_manifest = folder / "schema6-topology-manifest.json"
        topology_data = json.loads(base_manifest.read_text(encoding="utf-8"))
        topology_data["nodes"][1]["type"] = "navigation"
        topology_manifest.write_text(json.dumps(topology_data), encoding="utf-8")
        schema6_manifest_topology_drift = copy.deepcopy(schema6_good)
        schema6_manifest_topology_drift["manifest_source"] = artifact(topology_manifest)
        assert validate_ledger(
            write_ledger(folder, "schema6-manifest-topology-drift", schema6_manifest_topology_drift), quiet=True
        ) == 1

        schema6_non_string_gate = copy.deepcopy(schema6_good)
        schema6_non_string_gate["nodes"][1]["applicable_gates"] = [{"bad": "gate"}]
        assert validate_ledger(
            write_ledger(folder, "schema6-non-string-gate", schema6_non_string_gate), quiet=True
        ) == 1

        schema6_non_string_run = copy.deepcopy(schema6_good)
        schema6_non_string_run["nodes"][1]["gate_runs"]["runtime_state"] = {"bad": "run"}
        assert validate_ledger(
            write_ledger(folder, "schema6-non-string-run", schema6_non_string_run), quiet=True
        ) == 1

        schema6_non_string_parent = copy.deepcopy(schema6_good)
        schema6_non_string_parent["nodes"][1]["parent"] = {"bad": "parent"}
        assert validate_ledger(
            write_ledger(folder, "schema6-non-string-parent", schema6_non_string_parent), quiet=True
        ) == 1

        schema6_non_string_status = copy.deepcopy(schema6_good)
        schema6_non_string_status["nodes"][1]["status"] = ["done"]
        assert validate_ledger(
            write_ledger(folder, "schema6-non-string-status", schema6_non_string_status), quiet=True
        ) == 1

        schema6_extra_gate_state = copy.deepcopy(schema6_good)
        extra_leaf = schema6_extra_gate_state["nodes"][1]
        extra_leaf["gates"]["click"] = True
        extra_leaf["gate_runs"]["click"] = "web"
        extra_leaf["gate_evidence"]["click"] = extra_leaf["state_evidence"]
        assert validate_ledger(
            write_ledger(folder, "schema6-extra-gate-state", schema6_extra_gate_state), quiet=True
        ) == 1

        schema6_version = copy.deepcopy(schema6_good)
        version_leaf = schema6_version["nodes"][1]
        version_ref = version_leaf["state_evidence"][0]
        version_leaf["applicable_gates"].append("visual_version")
        version_leaf["gates"]["visual_version"] = True
        version_leaf["gate_runs"]["visual_version"] = "web"
        version_leaf["gate_evidence"]["visual_version"] = [version_ref]
        version_leaf["version_evidence"] = [version_ref]
        assert validate_ledger(write_ledger(folder, "schema6-version", schema6_version), quiet=True) == 0

        schema6_missing_version = copy.deepcopy(schema6_version)
        schema6_missing_version["nodes"][1].pop("version_evidence")
        assert validate_ledger(
            write_ledger(folder, "schema6-missing-version", schema6_missing_version), quiet=True
        ) == 1

        schema6_component = copy.deepcopy(schema6_good)
        component_leaf = schema6_component["nodes"][1]
        component_ref = component_leaf["state_evidence"][0]
        component_leaf["applicable_gates"].append("shared_component_identity")
        component_leaf["gates"]["shared_component_identity"] = True
        component_leaf["gate_runs"]["shared_component_identity"] = "web"
        component_leaf["gate_evidence"]["shared_component_identity"] = [component_ref]
        component_leaf["component_evidence"] = [
            {
                "shared_asset": "Assets/UI/Shared.prefab",
                "guid": "e" * 32,
                "instance_chain": ["Root", "Shared"],
                "consumers": ["host-a", "host-b"],
                "groups": [
                    {"id": "direct", "members": ["host-a"]},
                    {"id": "nested", "members": ["host-b"]},
                ],
                "samples": [
                    {"group": "direct", "host": "host-a", "result": "pass", "artifact": component_ref},
                    {"group": "nested", "host": "host-b", "result": "pass", "artifact": component_ref},
                ],
                "root_or_lifecycle_changed": False,
            }
        ]
        assert validate_ledger(
            write_ledger(folder, "schema6-component", schema6_component), quiet=True
        ) == 0

        schema6_ungrouped_component = copy.deepcopy(schema6_component)
        schema6_ungrouped_component["nodes"][1]["component_evidence"][0]["groups"].pop()
        schema6_ungrouped_component["nodes"][1]["component_evidence"][0]["samples"].pop()
        assert validate_ledger(
            write_ledger(folder, "schema6-ungrouped-component", schema6_ungrouped_component),
            quiet=True,
        ) == 1

        schema6_empty = copy.deepcopy(schema6_good)
        schema6_empty["nodes"][1]["applicable_gates"] = []
        schema6_empty["nodes"][1]["gates"] = {}
        assert validate_ledger(write_ledger(folder, "schema6-empty-gates", schema6_empty), quiet=True) == 1

        schema6_missing_gate_evidence = copy.deepcopy(schema6_good)
        schema6_missing_gate_evidence["nodes"][1]["gate_evidence"] = {}
        assert validate_ledger(
            write_ledger(folder, "schema6-missing-gate-evidence", schema6_missing_gate_evidence),
            quiet=True,
        ) == 1

        schema6_unknown_run = copy.deepcopy(schema6_good)
        schema6_unknown_run["nodes"][1]["gate_runs"]["runtime_state"] = "stale-run"
        assert validate_ledger(
            write_ledger(folder, "schema6-unknown-run", schema6_unknown_run), quiet=True
        ) == 1

        schema6_bad_hash = copy.deepcopy(schema6_good)
        schema6_bad_hash["nodes"][1]["gate_evidence"]["runtime_state"][0]["sha256"] = "0" * 64
        assert validate_ledger(write_ledger(folder, "schema6-bad-hash", schema6_bad_hash), quiet=True) == 1

        schema6_no_web = copy.deepcopy(schema6_good)
        schema6_no_web["route_run_id"] = "editor"
        assert validate_ledger(write_ledger(folder, "schema6-no-web", schema6_no_web), quiet=True) == 1

        schema6_stale_web = schema6_ledger(folder, effect=True)
        schema6_stale_web["verification_runs"]["editor"]["recorded_at"] = "2026-08-08T12:30:00+08:00"
        assert validate_ledger(
            write_ledger(folder, "schema6-stale-web", schema6_stale_web), quiet=True
        ) == 1

        schema6_source_mismatch = schema6_ledger(folder, effect=True)
        schema6_source_mismatch["verification_runs"]["editor"]["dirty_fingerprint"] = "f" * 64
        assert validate_ledger(
            write_ledger(folder, "schema6-source-mismatch", schema6_source_mismatch), quiet=True
        ) == 1

        schema6_bad_inventory = copy.deepcopy(schema6_good)
        schema6_bad_inventory["nodes"][0]["control_inventory"] = []
        assert validate_ledger(
            write_ledger(folder, "schema6-bad-inventory", schema6_bad_inventory), quiet=True
        ) == 1

        schema6_bad_summary = copy.deepcopy(schema6_good)
        schema6_bad_summary["summary"]["status_counts"] = {"done": 1}
        assert validate_ledger(
            write_ledger(folder, "schema6-bad-summary", schema6_bad_summary), quiet=True
        ) == 1

        schema6_bad_parent_rollup = copy.deepcopy(schema6_good)
        schema6_bad_parent_rollup["nodes"][0]["status"] = "needs-runtime-verify"
        schema6_bad_parent_rollup["nodes"][0]["runtime_gap"] = "synthetic stale parent"
        schema6_bad_parent_rollup["summary"]["status_counts"] = {
            "done": 1,
            "needs-runtime-verify": 1,
        }
        assert validate_ledger(
            write_ledger(folder, "schema6-bad-parent-rollup", schema6_bad_parent_rollup), quiet=True
        ) == 1

        schema6_cycle = copy.deepcopy(schema6_good)
        schema6_cycle["nodes"][0]["parent"] = "route.leaf"
        assert validate_ledger(write_ledger(folder, "schema6-cycle", schema6_cycle), quiet=True) == 1

        schema6_effect = schema6_ledger(folder, effect=True)
        schema6_effect_path = write_ledger(folder, "schema6-effect", schema6_effect)
        assert validate_ledger(schema6_effect_path, quiet=True) == 0

        schema6_static_effect = copy.deepcopy(schema6_effect)
        schema6_static_effect["nodes"][1]["effect_evidence"][0]["render"]["pixel_diff"] = 0
        assert validate_ledger(
            write_ledger(folder, "schema6-static-effect", schema6_static_effect), quiet=True
        ) == 1

        schema6_mask_leak = copy.deepcopy(schema6_effect)
        schema6_mask_leak["nodes"][1]["effect_evidence"][0]["mask_states"]["hidden"][
            "alpha_pixels"
        ] = 1
        assert validate_ledger(
            write_ledger(folder, "schema6-mask-leak", schema6_mask_leak), quiet=True
        ) == 1

        valid_manifest = {
            "route": "init-schema6",
            "nodes": [
                {
                    "id": "init",
                    "type": "page",
                    "risk": "read-only",
                    "control_inventory": [{"id": "leaf", "kind": "state", "child": "init.leaf"}],
                },
                {"id": "init.leaf", "parent": "init", "type": "read", "risk": "read-only"},
            ],
        }
        valid_manifest_path = folder / "valid-manifest.json"
        valid_manifest_path.write_text(json.dumps(valid_manifest), encoding="utf-8")
        initialized_path = folder / "initialized.json"
        with contextlib.redirect_stdout(io.StringIO()):
            assert init_ledger(valid_manifest_path, initialized_path) == 0
        assert json.loads(initialized_path.read_text(encoding="utf-8"))["schema"] == 6
        initialized_before = initialized_path.read_bytes()
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            assert init_ledger(valid_manifest_path, initialized_path) == 1
        assert initialized_path.read_bytes() == initialized_before

        invalid_manifest = copy.deepcopy(valid_manifest)
        invalid_manifest["nodes"][0]["control_inventory"] = []
        invalid_manifest_path = folder / "invalid-manifest.json"
        invalid_manifest_path.write_text(json.dumps(invalid_manifest), encoding="utf-8")
        invalid_initialized_path = folder / "invalid-initialized.json"
        with contextlib.redirect_stdout(io.StringIO()):
            assert init_ledger(invalid_manifest_path, invalid_initialized_path) == 1
        assert not invalid_initialized_path.exists()

        atomic_ledger = schema6_ledger(folder)
        atomic_ledger["nodes"][0]["status"] = "not-run"
        atomic_ledger["nodes"][1]["status"] = "not-run"
        atomic_ledger.pop("route_run_id")
        atomic_ledger["summary"] = {"total": 2, "status_counts": {"not-run": 2}}
        atomic_path = write_ledger(folder, "atomic-ledger", atomic_ledger)
        invalid_results_path = folder / "invalid-results.json"
        invalid_results_path.write_text(
            json.dumps(
                [
                    {
                        "id": "route.leaf",
                        "status": "done",
                        "applicable_gates": ["runtime_state"],
                        "gates": {"runtime_state": True},
                    }
                ]
            ),
            encoding="utf-8",
        )
        before = atomic_path.read_bytes()
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            try:
                apply_results(atomic_path, invalid_results_path)
            except ValueError:
                pass
            else:
                raise AssertionError("invalid done result unexpectedly applied")
        assert atomic_path.read_bytes() == before

        page_done_results_path = folder / "page-done-results.json"
        page_done_results_path.write_text(
            json.dumps(
                [
                    {
                        "id": "route",
                        "status": "done",
                        "applicable_gates": ["runtime_state"],
                        "gates": {"runtime_state": True},
                        "gate_runs": {"runtime_state": "web"},
                        "gate_evidence": {"runtime_state": [artifact(folder / "evidence.log")]},
                        "state_evidence": [artifact(folder / "evidence.log")],
                    }
                ]
            ),
            encoding="utf-8",
        )
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            try:
                apply_results(atomic_path, page_done_results_path)
            except ValueError:
                pass
            else:
                raise AssertionError("page done status unexpectedly accepted as a direct result")
        assert atomic_path.read_bytes() == before

        valid_apply_source = schema6_ledger(folder)
        valid_apply_root = valid_apply_source["nodes"][0]
        valid_apply_leaf = valid_apply_source["nodes"][1]
        clean_apply = copy.deepcopy(valid_apply_source)
        clean_apply["nodes"][0].pop("inventory_evidence")
        clean_apply["nodes"][0]["status"] = "not-run"
        clean_apply.pop("route_run_id")
        clean_apply["nodes"][1] = {
            "id": "route.leaf",
            "parent": "route",
            "type": "read",
            "risk": "read-only",
            "status": "not-run",
            "gates": {},
            "gate_runs": {},
            "gate_evidence": {},
            "evidence": [],
        }
        clean_apply["summary"] = {"total": 2, "status_counts": {"not-run": 2}}
        valid_apply_path = write_ledger(folder, "valid-apply-ledger", clean_apply)
        valid_apply_results_path = folder / "valid-apply-results.json"
        valid_apply_results_path.write_text(
            json.dumps(
                {
                    "route_run_id": "web",
                    "nodes": [
                        {"id": "route", "inventory_evidence": valid_apply_root["inventory_evidence"]},
                        {
                            "id": "route.leaf",
                            "status": "done",
                            "applicable_gates": valid_apply_leaf["applicable_gates"],
                            "gates": valid_apply_leaf["gates"],
                            "gate_runs": valid_apply_leaf["gate_runs"],
                            "gate_evidence": valid_apply_leaf["gate_evidence"],
                            "state_evidence": valid_apply_leaf["state_evidence"],
                        },
                    ]
                }
            ),
            encoding="utf-8",
        )
        assert apply_results(valid_apply_path, valid_apply_results_path) == 0
        valid_applied = json.loads(valid_apply_path.read_text(encoding="utf-8"))
        assert {node["status"] for node in valid_applied["nodes"]} == {"done"}

        refreshed_path = write_ledger(folder, "refreshed-ledger", schema6_ledger(folder, effect=True))
        refreshed_source = schema6_ledger(folder)
        refreshed_leaf = refreshed_source["nodes"][1]
        refreshed_results_path = folder / "refreshed-results.json"
        refreshed_results_path.write_text(
            json.dumps(
                [
                    {
                        "id": "route.leaf",
                        "status": "done",
                        "applicable_gates": refreshed_leaf["applicable_gates"],
                        "gates": refreshed_leaf["gates"],
                        "gate_runs": refreshed_leaf["gate_runs"],
                        "gate_evidence": refreshed_leaf["gate_evidence"],
                        "state_evidence": refreshed_leaf["state_evidence"],
                    }
                ]
            ),
            encoding="utf-8",
        )
        assert apply_results(refreshed_path, refreshed_results_path) == 0
        refreshed = json.loads(refreshed_path.read_text(encoding="utf-8"))
        refreshed_leaf = next(node for node in refreshed["nodes"] if node["id"] == "route.leaf")
        assert set(refreshed_leaf["gates"]) == {"runtime_state"}
        assert "effect_evidence" not in refreshed_leaf
        assert "render_evidence" not in refreshed_leaf

        invalidation_path = write_ledger(folder, "invalidation-ledger", schema6_ledger(folder))
        invalidation_results_path = folder / "invalidation-results.json"
        invalidation_results_path.write_text(
            json.dumps(
                [
                    {
                        "id": "route.leaf",
                        "status": "defect",
                        "invalidate_gates": ["runtime_state"],
                        "invalidation_reason": "new runtime evidence contradicts the old state",
                        "observed_at": "2026-08-08T13:00:00+08:00",
                    }
                ]
            ),
            encoding="utf-8",
        )
        assert apply_results(invalidation_path, invalidation_results_path) == 0
        invalidated = json.loads(invalidation_path.read_text(encoding="utf-8"))
        invalidated_leaf = next(node for node in invalidated["nodes"] if node["id"] == "route.leaf")
        assert invalidated_leaf["status"] == "defect"
        assert invalidated_leaf["gates"]["runtime_state"] is False
        assert "runtime_state" not in invalidated_leaf["gate_runs"]
        assert "runtime_state" not in invalidated_leaf["gate_evidence"]
        assert invalidated_leaf["evidence_history"]
        assert "route_run_id" not in invalidated

    print("route_ledger self-test: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
