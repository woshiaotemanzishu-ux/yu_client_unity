from __future__ import annotations

import copy
import hashlib
import json
import re
from collections import Counter
from pathlib import Path


OUT = Path(__file__).resolve().parent
REPO = OUT.parents[2]
V1 = OUT.parent / "2026-08-09_marriage"
V2 = OUT.parent / "2026-08-09_marriage_v2"
LEGACY = Path(r"E:\GitProject\yu_client\h5\src\marriage")
LEGACY_CONTROLLER = Path(r"E:\GitProject\yu_client\h5\src\commonController\MarriageController.ts")


def load_json(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


manifest = copy.deepcopy(load_json(V2 / "route-manifest.json"))
results: list[dict] = copy.deepcopy(load_json(V2 / "results.json"))
ts_route_data: dict = copy.deepcopy(load_json(V2 / "ts-route-map.json"))
nodes: list[dict] = manifest["nodes"]
manifest["route"] = "marriage"
manifest["baseline"]["supersedes"] = "../2026-08-09_marriage_v2/route-ledger.json"
manifest["baseline"]["original_route"] = "marriage"
manifest["baseline"]["topology_revision"] = 3

node_ids = [item["id"] for item in nodes]
node_id_set = set(node_ids)
children: dict[str, set[str]] = {node_id: set() for node_id in node_ids}
roots: list[str] = []
for item in nodes:
    parent = item.get("parent")
    if parent is None:
        roots.append(item["id"])
    else:
        if parent not in node_id_set:
            raise ValueError(f"missing parent {parent}")
        children[parent].add(item["id"])

page_inventory_ok = True
for item in nodes:
    if item["type"] != "page":
        continue
    inventory_children = [control["child"] for control in item["control_inventory"]]
    page_inventory_ok = page_inventory_ok and len(inventory_children) == len(set(inventory_children))
    page_inventory_ok = page_inventory_ok and set(inventory_children) == children[item["id"]]

leaf_ids = {node_id for node_id, child_ids in children.items() if not child_ids}
result_ids = [item["id"] for item in results]
result_map = {item["id"]: item for item in results}
legacy_ts = sorted(path.stem for path in LEGACY.glob("*.ts"))
ts_route_map: dict[str, str] = ts_route_data["mapping"]

ring_ts = (LEGACY / "MarriageRingView.ts").read_text(encoding="utf-8-sig")
issue_ts = (LEGACY / "MarriageIssueView.ts").read_text(encoding="utf-8-sig")
old_controller_ts = LEGACY_CONTROLLER.read_text(encoding="utf-8-sig")
unity_controller = (REPO / "Assets/Scripts/Module/Core/Marriage/MarriageController.cs").read_text(encoding="utf-8-sig")

active_legacy_dead_import = re.search(
    r"(?m)^\s*import\s+[\"']\.\./marriage/Marriage(?:MatchView|MatchTipsView|TagView)[\"']",
    old_controller_ts,
) is not None
unity_has_match_send_method = re.search(
    r"\b(?:public|private|internal|protected)\s+\w+\s+RequestDunMatch\s*\(",
    unity_controller,
) is not None

query_leaf_ids = {
    "marriage.main.lobby.query-17200",
    "marriage.main.ring.query-17210",
    "marriage.main.mate.query-17232",
    "marriage.main.mate.query-17238",
    "marriage.main.gift.query-17232",
    "marriage.main.gift.query-17238",
    "marriage.ask.query-17232",
}
background_parents = [
    "marriage.ask-list",
    "marriage.ask",
    "marriage.break",
    "marriage.com",
    "marriage.dsgt",
    "marriage.flower",
    "marriage.flow",
    "marriage.gift-tips",
    "marriage.honour",
    "marriage.issue",
    "marriage.record-com",
    "marriage.record-flower",
]
background_leaf_ids = {f"{parent}.background-close" for parent in background_parents}
boundary_leaf_ids = {
    "marriage.legacy-boundaries.match-view",
    "marriage.legacy-boundaries.match-tips-view",
    "marriage.legacy-boundaries.tag-view",
    "marriage.legacy-boundaries.ring-change-view",
}

checks = {
    "stable_logical_route_identity": manifest["route"] == "marriage",
    "v1_is_marked_superseded": (V1 / "superseded.json").is_file(),
    "v2_is_marked_superseded": (V2 / "superseded.json").is_file(),
    "unique_node_ids": len(node_ids) == len(node_id_set),
    "single_root": roots == ["marriage"],
    "page_control_inventory_matches_direct_children": page_inventory_ok,
    "results_exactly_cover_leaves": len(result_ids) == len(set(result_ids)) and set(result_ids) == leaf_ids,
    "only_blocked_or_needs_runtime_verify": all(item["status"] in {"blocked", "needs-runtime-verify"} for item in results),
    "no_done_or_not_run_leaf": all(item["status"] not in {"done", "not-run"} for item in results),
    "all_leaf_reasons_present": all(
        bool(item.get("blocked_reason" if item["status"] == "blocked" else "runtime_gap")) for item in results
    ),
    "query_leaf_set_complete": query_leaf_ids <= leaf_ids,
    "background_close_leaf_set_complete": background_leaf_ids <= leaf_ids,
    "dun_tips_gray_leaf_present": "marriage.dun-tips.gray-disabled" in leaf_ids,
    "help_placeholders_are_blocked": all(
        result_map[help_id]["status"] == "blocked"
        for help_id in ["marriage.main.dungeon.help", "marriage.ask.help", "marriage.flower.help"]
    ),
    "legacy_boundaries_complete": boundary_leaf_ids <= leaf_ids,
    "legacy_ts_exactly_mapped": set(legacy_ts) == set(ts_route_map),
    "mapped_route_nodes_exist": all(route_id in node_id_set for route_id in ts_route_map.values()),
    "dead_views_map_to_boundary_nodes": all(
        ts_route_map[name] in boundary_leaf_ids
        for name in ["MarriageMatchView", "MarriageMatchTipsView", "MarriageTagView", "MarriageRingChangeView"]
    ),
    "ring_uses_outward_changed": 'Fire(OutWardBaseModel.OPEN_VIEW, "OutwardChangedView", data)' in ring_ts,
    "ring_change_open_is_commented": bool(
        re.search(r"(?m)^\s*//\s*local_GlobalEventSystem\.Fire\([^\n]*MarriageRingChangeView", ring_ts)
    ),
    "tag_string_open_exists_but_import_is_commented": (
        'GlobalEventSystem.Fire(EventName.OPEN_VIEW, "MarriageTagView")' in issue_ts
        and '// import "../marriage/MarriageTagView";' in old_controller_ts
        and not active_legacy_dead_import
    ),
    "unity_match_send_is_absent": not unity_has_match_send_method,
    "unity_match_receivers_are_defensive_only": "private void On17245" in unity_controller and "private void On17246" in unity_controller,
}
if not all(checks.values()):
    raise ValueError({name: value for name, value in checks.items() if not value})

static_validation = {
    "schema_target": 6,
    "route": "marriage",
    "topology_revision": 3,
    "supersedes": "../2026-08-09_marriage_v2/route-ledger.json",
    "checks": checks,
    "metrics": {
        "nodes": len(nodes),
        "pages": sum(item["type"] == "page" for item in nodes),
        "leaves": len(results),
        "legacy_ts_count": len(legacy_ts),
        "mapped_ts_count": len(ts_route_map),
        "query_leaf_count": len(query_leaf_ids),
        "background_close_leaf_count": len(background_leaf_ids),
        "dead_or_replaced_boundary_count": len(boundary_leaf_ids),
    },
    "source_sha256": {
        "legacy_marriage_ring_view": sha256(LEGACY / "MarriageRingView.ts"),
        "legacy_marriage_issue_view": sha256(LEGACY / "MarriageIssueView.ts"),
        "legacy_marriage_controller": sha256(LEGACY_CONTROLLER),
        "unity_marriage_controller": sha256(REPO / "Assets/Scripts/Module/Core/Marriage/MarriageController.cs"),
    },
}

OUT.mkdir(parents=True, exist_ok=True)
(OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "static-validation.json").write_text(json.dumps(static_validation, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "ts-route-map.json").write_text(json.dumps(ts_route_data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

statuses = Counter(item["status"] for item in results)
page_count = sum(item["type"] == "page" for item in nodes)
matrix = [
    "# Marriage UI 静态路由矩阵 v3",
    "",
    "> 最终修正版：保持逻辑路线身份 `marriage`；v1/v2 schema 6 台账均原样保留并标记 superseded。",
    "",
    f"- 节点：{len(nodes)}（页面 {page_count}，叶 {len(results)}）",
    f"- 叶状态：blocked={statuses['blocked']}，needs-runtime-verify={statuses['needs-runtime-verify']}，done=0",
    f"- 老端 TS 映射：{len(ts_route_map)}/{len(legacy_ts)}",
    "",
    "| 叶 ID | 状态 | 原因/运行缺口 |",
    "|---|---|---|",
]
for item in results:
    reason = item.get("blocked_reason") or item.get("runtime_gap") or ""
    matrix.append(f"| `{item['id']}` | {item['status']} | {reason.replace('|', '/')} |")
(OUT / "route-matrix.md").write_text("\n".join(matrix) + "\n", encoding="utf-8")

print(
    json.dumps(
        {
            "route": manifest["route"],
            "nodes": len(nodes),
            "pages": page_count,
            "leaves": len(results),
            "blocked": statuses["blocked"],
            "needs_runtime_verify": statuses["needs-runtime-verify"],
            "checks": len(checks),
        },
        ensure_ascii=False,
    )
)
