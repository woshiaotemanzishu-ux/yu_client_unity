#!/usr/bin/env python3
"""Fault-injection checks for the UI route ledger completion gates."""

from __future__ import annotations

import json
import tempfile
from pathlib import Path

from route_ledger import validate_ledger


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


def main() -> int:
    with tempfile.TemporaryDirectory() as temp:
        folder = Path(temp)

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

    print("route_ledger self-test: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
