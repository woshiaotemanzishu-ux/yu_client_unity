from __future__ import annotations

import copy
import hashlib
import json
from collections import Counter
from pathlib import Path


OUT = Path(__file__).resolve().parent
REPO = OUT.parents[2]
V1 = OUT.parent / "2026-08-09_marriage"
LEGACY = Path(r"E:\GitProject\yu_client\h5\src\marriage")


def load_json(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


manifest = copy.deepcopy(load_json(V1 / "route-manifest.json"))
results: list[dict] = copy.deepcopy(load_json(V1 / "results.json"))
nodes: list[dict] = manifest["nodes"]
manifest["route"] = "marriage_v2"
manifest["baseline"]["supersedes"] = "../2026-08-09_marriage/route-ledger.json"
manifest["baseline"]["topology_revision"] = 2


def node(node_id: str) -> dict:
    matches = [item for item in nodes if item["id"] == node_id]
    if len(matches) != 1:
        raise ValueError(f"expected one node {node_id}, got {len(matches)}")
    return matches[0]


def add_control(parent: str, control_id: str, kind: str, child: str) -> None:
    parent_node = node(parent)
    if parent_node["type"] != "page":
        raise ValueError(f"control parent is not a page: {parent}")
    if any(item["id"] == control_id or item["child"] == child for item in parent_node["control_inventory"]):
        raise ValueError(f"duplicate control/child in {parent}: {control_id}/{child}")
    parent_node["control_inventory"].append({"id": control_id, "kind": kind, "child": child})


def add_page(node_id: str, parent: str, title: str, control_id: str, kind: str = "boundary") -> None:
    add_control(parent, control_id, kind, node_id)
    nodes.append(
        {
            "id": node_id,
            "parent": parent,
            "type": "page",
            "risk": "read-only",
            "control_inventory": [],
            "note": title,
        }
    )


def add_leaf(
    node_id: str,
    parent: str,
    control_id: str,
    kind: str,
    node_type: str,
    status: str,
    legacy: str,
    unity: str,
    reason: str,
) -> None:
    add_control(parent, control_id, kind, node_id)
    nodes.append(
        {
            "id": node_id,
            "parent": parent,
            "type": node_type,
            "risk": "read-only",
            "note": f"老端：{legacy}；Unity：{unity}",
        }
    )
    result = {"id": node_id, "status": status, "note": f"老端：{legacy}；Unity：{unity}"}
    if status == "blocked":
        result["blocked_reason"] = reason
    elif status == "needs-runtime-verify":
        result["runtime_gap"] = reason
    else:
        raise ValueError(f"forbidden status {status}")
    results.append(result)


def replace_result(node_id: str, status: str, reason: str) -> None:
    matches = [item for item in results if item["id"] == node_id]
    if len(matches) != 1:
        raise ValueError(f"expected one result {node_id}, got {len(matches)}")
    item = matches[0]
    item["status"] = status
    item.pop("runtime_gap", None)
    item.pop("blocked_reason", None)
    item["blocked_reason" if status == "blocked" else "runtime_gap"] = reason


# Every read-only OnShow query added in this audit is a separate leaf.
for args in [
    ("marriage.main.lobby.query-17200", "marriage.main.lobby", "query-17200", "17200 page=1", "MarriageFriendView.OnShow RequestPersonalsList(1)"),
    ("marriage.main.ring.query-17210", "marriage.main.ring", "query-17210", "17210", "MarriageRingView.OnShow RequestRingInfo"),
    ("marriage.main.mate.query-17232", "marriage.main.mate", "query-17232", "17232", "MarriageMainView.OnShow RequestMyMate"),
    ("marriage.main.mate.query-17238", "marriage.main.mate", "query-17238", "17238", "MarriageMainView.OnShow RequestGiftInfo"),
    ("marriage.main.gift.query-17232", "marriage.main.gift", "query-17232", "17232", "MarriageGiftView.OnShow RequestMyMate"),
    ("marriage.main.gift.query-17238", "marriage.main.gift", "query-17238", "17238", "MarriageGiftView.OnShow RequestGiftInfo"),
    ("marriage.ask.query-17232", "marriage.ask", "query-17232", "17232", "MarriageAskView.OnShow RequestMyMate"),
]:
    leaf_id, parent, control_id, legacy_query, unity_call = args
    add_leaf(
        leaf_id,
        parent,
        control_id,
        "read-query",
        "read",
        "needs-runtime-verify",
        f"页面打开主动请求 {legacy_query} 权威快照",
        f"已静态接入 {unity_call}",
        "未启动 Unity/Web，缺真实发包、回包、父页即时消费与热重开证据",
    )


# Old click_bg_toClose surfaces are independent controls, not aliases of close buttons.
background_pages = [
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
for parent in background_pages:
    add_leaf(
        f"{parent}.background-close",
        parent,
        "background-close",
        "background",
        "return",
        "blocked",
        "click_bg_toClose=true，点击背景关闭当前窗且不得穿透",
        "未发现等价页面专属背景点击接线",
        "需在现有 Prefab 上增量接入背景点击面；本轮禁止用静态日志冒充点击关闭",
    )


add_leaf(
    "marriage.dun-tips.gray-disabled",
    "marriage.dun-tips",
    "gray-disabled",
    "conditional-click-area",
    "read",
    "blocked",
    "主动发起方显示 _btn_gray 禁用态，不能重复接受/拒绝/取消",
    "仅 Generated Bind，未接主动方条件禁用点击面",
    "缺 DunTips 业务 View、主动方身份参数及禁用态射线拦截",
)


for help_id in ["marriage.main.dungeon.help", "marriage.ask.help", "marriage.flower.help"]:
    replace_result(help_id, "blocked", "当前 Unity 仅日志占位，未打开对应说明页；缺真实目标路由、关闭链与运行态证据")


# Explicitly freeze legacy files which are present but unreachable from the current old-client route.
add_page("marriage.legacy-boundaries", "marriage", "老端死路由/替代路由边界", "legacy-boundaries")
for leaf_id, control_id, legacy, unity, reason in [
    ("marriage.legacy-boundaries.match-view", "match-view", "MarriageMatchView.ts 存在但当前 MarriageModule/调用链不可达", "MarriageModule.prefab 无对应窗口", "死路由；禁止仅因源码文件存在而补 Unity 入口"),
    ("marriage.legacy-boundaries.match-tips-view", "match-tips-view", "MarriageMatchTipsView.ts 存在但当前调用链不可达", "MarriageModule.prefab 无对应窗口", "死路由；禁止臆造匹配提示入口"),
    ("marriage.legacy-boundaries.tag-view", "tag-view", "MarriageTagView.ts 存在但当前调用链不可达", "MarriageModule.prefab 无独立 TagView", "标签编辑由 IssueView 当前路径解释；旧独立窗保持不可达边界"),
    ("marriage.legacy-boundaries.ring-change-view", "ring-change-view", "MarriageRingChangeView 打开点已注释，当前改走 OutwardChangedView", "MarriageModule.prefab 无对应窗口", "替代路由属于 Common/Outward 且不在当前文件岛；禁止恢复旧 RingChange 写事务"),
]:
    add_leaf(leaf_id, "marriage.legacy-boundaries", control_id, "legacy-boundary", "read", "blocked", legacy, unity, reason)


# Every old TS file must map to a current route node or an explicit dead/replaced boundary.
ts_route_map = {
    "MarriageAskItem": "marriage.ask.ring-list",
    "MarriageAskListItem": "marriage.ask-list.row-ask",
    "MarriageAskListView": "marriage.ask-list",
    "MarriageAskTipsView": "marriage.ask-tips",
    "MarriageAskView": "marriage.ask",
    "MarriageBaseBtn": "marriage.main",
    "MarriageBaseView": "marriage.main",
    "MarriageBreakTipsView": "marriage.break-tips",
    "MarriageBreakView": "marriage.break",
    "MarriageComView": "marriage.com",
    "MarriageDropBtn": "marriage.shared.drop-btn",
    "MarriageDropItem": "marriage.shared.drop-btn.options",
    "MarriageDropList": "marriage.shared.drop-btn.options",
    "MarriageDsgtItem": "marriage.dsgt.list",
    "MarriageDsgtView": "marriage.dsgt",
    "MarriageDunLuckBtn": "marriage.dun-luck",
    "MarriageDunLuckView": "marriage.dun-luck",
    "MarriageDunTipsView": "marriage.dun-tips",
    "MarriageDunView": "marriage.main.dungeon",
    "MarriageFlowerItem": "marriage.flower.list",
    "MarriageFlowerTipsView": "marriage.flower-tips",
    "MarriageFlowerView": "marriage.flower",
    "MarriageFlowItem": "marriage.flow.list",
    "MarriageFlowView": "marriage.flow",
    "MarriageForeShowView": "marriage.fore-show",
    "MarriageFriendItem": "marriage.shared.friend-item",
    "MarriageFriendView": "marriage.main.lobby",
    "MarriageGiftTipsView": "marriage.gift-tips",
    "MarriageGiftView": "marriage.main.gift",
    "MarriageHonourItem": "marriage.honour.list",
    "MarriageHonourView": "marriage.honour",
    "MarriageIssueView": "marriage.issue",
    "MarriageMainView": "marriage.main.mate",
    "MarriageMatchTipsView": "marriage.legacy-boundaries.match-tips-view",
    "MarriageMatchView": "marriage.legacy-boundaries.match-view",
    "MarriageRecordComItem": "marriage.record-com.self",
    "MarriageRecordComView": "marriage.record-com",
    "MarriageRecordFlowerItem": "marriage.record-flower.list",
    "MarriageRecordFlowerView": "marriage.record-flower",
    "MarriageRingChangeView": "marriage.legacy-boundaries.ring-change-view",
    "MarriageRingView": "marriage.main.ring",
    "MarriageRoleMenuView": "marriage.role-menu",
    "MarriageSuccessView": "marriage.success",
    "MarriageTagItem": "marriage.issue.tag",
    "MarriageTagLayout": "marriage.issue.tag",
    "MarriageTagShowItem": "marriage.issue.tag",
    "MarriageTagSubItem": "marriage.issue.tag",
    "MarriageTagView": "marriage.legacy-boundaries.tag-view",
}


node_ids = [item["id"] for item in nodes]
node_id_set = set(node_ids)
legacy_ts = sorted(path.stem for path in LEGACY.glob("*.ts"))
mapping_checks = {
    "legacy_ts_exactly_mapped": set(legacy_ts) == set(ts_route_map),
    "mapped_route_nodes_exist": all(route_id in node_id_set for route_id in ts_route_map.values()),
    "dead_views_map_to_explicit_boundaries": all(
        ts_route_map[name].startswith("marriage.legacy-boundaries.")
        for name in ["MarriageMatchView", "MarriageMatchTipsView", "MarriageTagView", "MarriageRingChangeView"]
    ),
}
if not all(mapping_checks.values()):
    raise ValueError(
        {
            "mapping_checks": mapping_checks,
            "unmapped_ts": sorted(set(legacy_ts) - set(ts_route_map)),
            "unknown_map_keys": sorted(set(ts_route_map) - set(legacy_ts)),
            "missing_route_ids": sorted(set(ts_route_map.values()) - node_id_set),
        }
    )


if len(node_ids) != len(node_id_set):
    raise ValueError("duplicate node id")
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
if roots != ["marriage"]:
    raise ValueError(f"unexpected roots {roots}")
for item in nodes:
    if item["type"] != "page":
        continue
    inventory_children = [control["child"] for control in item["control_inventory"]]
    if len(inventory_children) != len(set(inventory_children)) or set(inventory_children) != children[item["id"]]:
        raise ValueError(f"control inventory mismatch: {item['id']}")
leaf_ids = {node_id for node_id, child_ids in children.items() if not child_ids}
result_ids = [item["id"] for item in results]
if len(result_ids) != len(set(result_ids)) or set(result_ids) != leaf_ids:
    raise ValueError("results do not exactly cover leaves")
if any(item["status"] not in {"blocked", "needs-runtime-verify"} for item in results):
    raise ValueError("forbidden result status")
for item in results:
    field = "blocked_reason" if item["status"] == "blocked" else "runtime_gap"
    if not item.get(field):
        raise ValueError(f"missing {field}: {item['id']}")


query_leaf_ids = {
    "marriage.main.lobby.query-17200",
    "marriage.main.ring.query-17210",
    "marriage.main.mate.query-17232",
    "marriage.main.mate.query-17238",
    "marriage.main.gift.query-17232",
    "marriage.main.gift.query-17238",
    "marriage.ask.query-17232",
}
background_leaf_ids = {f"{parent}.background-close" for parent in background_pages}
static_validation = {
    "schema_target": 6,
    "route": "marriage_v2",
    "supersedes": "../2026-08-09_marriage/route-ledger.json",
    "checks": {
        "v1_manifest_ledger_preserved": (V1 / "superseded.json").is_file(),
        "unique_node_ids": len(node_ids) == len(node_id_set),
        "single_root": roots == ["marriage"],
        "page_control_inventory_matches_direct_children": True,
        "results_exactly_cover_leaves": set(result_ids) == leaf_ids,
        "only_blocked_or_needs_runtime_verify": True,
        "query_leaf_set_complete": query_leaf_ids <= leaf_ids,
        "background_close_leaf_set_complete": background_leaf_ids <= leaf_ids,
        "dun_tips_gray_leaf_present": "marriage.dun-tips.gray-disabled" in leaf_ids,
        "help_placeholders_blocked": all(
            next(item for item in results if item["id"] == help_id)["status"] == "blocked"
            for help_id in ["marriage.main.dungeon.help", "marriage.ask.help", "marriage.flower.help"]
        ),
        **mapping_checks,
    },
    "metrics": {
        "legacy_ts_count": len(legacy_ts),
        "mapped_ts_count": len(ts_route_map),
        "query_leaf_count": len(query_leaf_ids),
        "background_close_leaf_count": len(background_leaf_ids),
        "dead_or_replaced_boundary_count": 4,
    },
}
if not all(static_validation["checks"].values()):
    raise ValueError(static_validation)


OUT.mkdir(parents=True, exist_ok=True)
(OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "static-validation.json").write_text(json.dumps(static_validation, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "ts-route-map.json").write_text(
    json.dumps(
        {
            "legacy_root": str(LEGACY),
            "legacy_ts_sha256": {path.name: sha256(path) for path in sorted(LEGACY.glob("*.ts"))},
            "mapping": ts_route_map,
        },
        ensure_ascii=False,
        indent=2,
    )
    + "\n",
    encoding="utf-8",
)

statuses = Counter(item["status"] for item in results)
page_count = sum(item["type"] == "page" for item in nodes)
matrix = [
    "# Marriage UI 静态路由矩阵 v2",
    "",
    "> v1 schema 6 台账保持不可变；本账修复 QA 发现的拓扑漏项。未启动 Unity/Web，未发送协议或执行账号事务。",
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
            "nodes": len(nodes),
            "pages": page_count,
            "leaves": len(results),
            "blocked": statuses["blocked"],
            "needs_runtime_verify": statuses["needs-runtime-verify"],
            "legacy_ts_mapped": len(ts_route_map),
        },
        ensure_ascii=False,
    )
)
