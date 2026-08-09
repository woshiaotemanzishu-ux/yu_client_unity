import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ROUTE = "mainui.halo"

nodes = []


def add(node_id, node_type, risk="read-only", parent=None, controls=None):
    node = {"id": node_id, "type": node_type, "risk": risk}
    if parent:
        node["parent"] = parent
    if controls is not None:
        node["control_inventory"] = controls
    nodes.append(node)


def control(control_id, kind, child):
    return {"id": control_id, "kind": kind, "child": child}


add(ROUTE, "page", controls=[
    control("mainui-halo-entry", "button-and-red-point", f"{ROUTE}.entry"),
    control("halo-window", "navigation-result", f"{ROUTE}.window"),
])
add(f"{ROUTE}.entry", "navigation", parent=ROUTE)
add(f"{ROUTE}.window", "page", parent=ROUTE, controls=[
    control("background-and-ready", "state", f"{ROUTE}.window.ready"),
    control("close", "button", f"{ROUTE}.window.close"),
    control("halo-effect", "dynamic-effect", f"{ROUTE}.window.effect"),
    control("privilege-list", "scroll-list", f"{ROUTE}.window.list"),
    control("purchase-banner", "conditional-region", f"{ROUTE}.window.purchase"),
    control("countdown", "conditional-state", f"{ROUTE}.window.countdown"),
    control("shared-dependencies", "dependency-matrix", f"{ROUTE}.window.shared"),
])
add(f"{ROUTE}.window.ready", "read", parent=f"{ROUTE}.window")
add(f"{ROUTE}.window.close", "navigation", parent=f"{ROUTE}.window")

add(f"{ROUTE}.window.effect", "page", parent=f"{ROUTE}.window", controls=[
    control("ui-305414", "dynamic-effect", f"{ROUTE}.window.effect.visual"),
    control("illusion-detail", "popup-trigger", f"{ROUTE}.window.effect.illusion-tips"),
])
add(f"{ROUTE}.window.effect.visual", "read", parent=f"{ROUTE}.window.effect")
add(f"{ROUTE}.window.effect.illusion-tips", "read", parent=f"{ROUTE}.window.effect")

add(f"{ROUTE}.window.list", "page", parent=f"{ROUTE}.window", controls=[
    control("scroll-structure", "scroll-structure", f"{ROUTE}.window.list.structure"),
    control("drag-and-last-item", "scroll-action", f"{ROUTE}.window.list.scroll"),
] + [control(f"halo-row-{i}", "list-item", f"{ROUTE}.window.list.row-{i}") for i in range(1, 10)])
add(f"{ROUTE}.window.list.structure", "read", parent=f"{ROUTE}.window.list")
add(f"{ROUTE}.window.list.scroll", "read", parent=f"{ROUTE}.window.list")

desc_rows = {2, 3, 5, 6, 7, 9}
for row_id in range(1, 10):
    row = f"{ROUTE}.window.list.row-{row_id}"
    add(row, "page", parent=f"{ROUTE}.window.list", controls=[
        control(f"row-{row_id}-background", "image", f"{row}.background"),
        control(f"row-{row_id}-three-rewards", "shared-component-list", f"{row}.rewards"),
        control(f"row-{row_id}-claim", "conditional-transaction", f"{row}.claim"),
        control(f"row-{row_id}-lock-mask", "conditional-button", f"{row}.mask"),
        control(f"row-{row_id}-new-flag", "conditional-state", f"{row}.new-flag"),
        control(f"row-{row_id}-description", "conditional-popup" if row_id in desc_rows else "hidden-condition", f"{row}.description"),
    ])
    add(f"{row}.background", "read", parent=row)
    add(f"{row}.rewards", "read", parent=row)
    add(f"{row}.claim", "transaction", risk="destructive-write", parent=row)
    add(f"{row}.mask", "read", parent=row)
    add(f"{row}.new-flag", "read", parent=row)
    add(f"{row}.description", "read", parent=row)

add(f"{ROUTE}.window.purchase", "page", parent=f"{ROUTE}.window", controls=[
    control("banner-visibility", "conditional-state", f"{ROUTE}.window.purchase.visibility"),
    control("buy-renew-label", "conditional-state", f"{ROUTE}.window.purchase.label"),
    control("original-price", "text", f"{ROUTE}.window.purchase.original-price"),
    control("current-price", "text", f"{ROUTE}.window.purchase.current-price"),
    control("buy-or-renew", "transaction", f"{ROUTE}.window.purchase.submit"),
])
for leaf in ("visibility", "label", "original-price", "current-price"):
    add(f"{ROUTE}.window.purchase.{leaf}", "read", parent=f"{ROUTE}.window.purchase")
add(f"{ROUTE}.window.purchase.submit", "transaction", risk="destructive-write", parent=f"{ROUTE}.window.purchase")
add(f"{ROUTE}.window.countdown", "read", parent=f"{ROUTE}.window")

shared = f"{ROUTE}.window.shared"
add(shared, "page", parent=f"{ROUTE}.window", controls=[
    control("common-base-award-item", "shared-component", f"{shared}.base-award-item"),
    control("common-illusion-tips", "shared-popup", f"{shared}.illusion-tips"),
    control("common-team-small-desc", "shared-popup", f"{shared}.team-small-desc"),
    control("arena-sweep-setting", "external-transaction", f"{shared}.setting-arena"),
    control("equip-dungeon-setting", "external-transaction", f"{shared}.setting-dungeon-equip"),
    control("dragon-dungeon-setting", "external-transaction", f"{shared}.setting-dungeon-dragon"),
    control("godbeast-composite-setting", "external-transaction", f"{shared}.setting-godbeast"),
])
for leaf in ("base-award-item", "illusion-tips", "team-small-desc"):
    add(f"{shared}.{leaf}", "read", parent=shared)
for leaf in ("setting-arena", "setting-dungeon-equip", "setting-dungeon-dragon", "setting-godbeast"):
    add(f"{shared}.{leaf}", "transaction", risk="destructive-write", parent=shared)

manifest = {
    "route": ROUTE,
    "baseline": {
        "account": "not-used",
        "viewport": "720x1280-static-only",
        "legacy_source": "E:/GitProject/yu_client/h5/src/halo/HaloMainView.ts + HaloItem.ts",
        "write_policy": "enumerate-only; purchase, renewal, 51401 and 51402 are never sent",
    },
    "nodes": nodes,
}

children = {node.get("parent") for node in nodes if node.get("parent")}
leaves = [node for node in nodes if node["id"] not in children]
blocked_ids = {f"{ROUTE}.entry", f"{ROUTE}.window.purchase.submit"}
blocked_ids.update(f"{ROUTE}.window.list.row-{i}.claim" for i in range(1, 10))
blocked_ids.update(f"{ROUTE}.window.list.row-{i}.description" for i in desc_rows)
blocked_ids.update(f"{shared}.{name}" for name in (
    "setting-arena", "setting-dungeon-equip", "setting-dungeon-dragon", "setting-godbeast"))

result_nodes = []
for leaf in leaves:
    node_id = leaf["id"]
    if node_id in blocked_ids:
        result_nodes.append({
            "id": node_id,
            "status": "blocked",
            "blocked_reason": "本轮禁止账号写事务、前台运行和跨文件岛实现；未发送协议、未点击真实事务。",
            "runtime_gap": None,
            "note": "静态实现或枚举已完成，但不能用静态结果替代真实事务回包、即时刷新和重开一致性。",
        })
    else:
        result_nodes.append({
            "id": node_id,
            "status": "needs-runtime-verify",
            "blocked_reason": None,
            "runtime_gap": "未启动 Unity/浏览器，尚缺同账号老 H5 与当前 Unity Web 的真实点击、像素、滚动、弹窗身份、状态切换和重开证据。",
            "note": "静态检查不冒充真实 Web/Unity 通过。",
        })

results = {
    "updated_at": "2026-08-09T00:00:00+08:00",
    "summary_details": {
        "scope": "Halo-only static audit and incremental fix-view",
        "runtime": "Unity, browser and account transactions intentionally not used",
    },
    "nodes": result_nodes,
}

(ROOT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(ROOT / "route-results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"nodes": len(nodes), "leaves": len(leaves), "blocked": len(blocked_ids), "needs_runtime": len(leaves) - len(blocked_ids)}))
