import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
ROUTE = "mainui.equip.armor"
nodes = []
results = []


def page(node_id, parent, controls, note=""):
    node = {
        "id": node_id,
        "type": "page",
        "risk": "read-only",
        "control_inventory": [
            {"id": control_id, "kind": kind, "child": child}
            for control_id, kind, child in controls
        ],
    }
    if parent:
        node["parent"] = parent
    if note:
        node["note"] = note
    nodes.append(node)


def leaf(node_id, parent, node_type, risk, status, reason):
    nodes.append({"id": node_id, "parent": parent, "type": node_type, "risk": risk})
    item = {"id": node_id, "status": status}
    if status == "blocked":
        item["blocked_reason"] = reason
    else:
        item["runtime_gap"] = reason
    results.append(item)


runtime = "本轮未启动 Unity/真实 Web；仅有老端源码、Unity Prefab/Bind/业务代码静态证据，需同账号真实 GraphicRaycaster 点击、状态、视觉与生命周期复验。"
cross_equip = "确定修复点位于禁止写入的 Equip 业务 View/Flow；Armor 文件岛只能记录 blocker，不能越界修改。"
transaction = "14402 会真实消耗材料、写入圣骸/角色属性状态；本轮未获账号资产写授权，禁止发送，成功/失败、即时刷新与重开不得静态冒充。"

page(ROUTE, None, [
    ("entry-equip-tab5", "navigation", f"{ROUTE}.entry"),
    ("window-close", "return", f"{ROUTE}.close"),
    ("snapshot-and-config", "state", f"{ROUTE}.data"),
    ("stage-list", "scroll-list", f"{ROUTE}.stages"),
    ("armor-type-tabs", "tab-group", f"{ROUTE}.types"),
    ("suit-summary", "state", f"{ROUTE}.suit"),
    ("position-items", "item-list", f"{ROUTE}.positions"),
    ("selected-equipment", "detail", f"{ROUTE}.selected"),
    ("material-slots", "item-list", f"{ROUTE}.materials"),
    ("make-flow", "transaction-flow", f"{ROUTE}.make"),
    ("all-attributes-popup", "popup", f"{ROUTE}.total-attrs"),
    ("root-red-dot", "conditional-state", f"{ROUTE}.root-red"),
    ("hide-reopen-lifecycle", "lifecycle", f"{ROUTE}.lifecycle"),
    ("visual-ready", "visual", f"{ROUTE}.visual"),
    ("cold-warm-performance", "performance", f"{ROUTE}.performance"),
], "老端 EquipView 第6页签“不朽圣骸”；无页面专属声音调用，通用点击声不替代成功特效。")

leaf(f"{ROUTE}.entry", ROUTE, "navigation", "read-only", "blocked",
     "入口与第6页签属于禁止写入的 EquipFlow/BaseWindow；TabSpec.Label 当前为空，页面身份和页签文案需跨岛修复及真实 Web 核对。")
leaf(f"{ROUTE}.close", ROUTE, "return", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.data", ROUTE, [
    ("request-14401", "read-request", f"{ROUTE}.data.request"),
    ("config-loading", "conditional-state", f"{ROUTE}.data.config-loading"),
    ("snapshot-loading", "conditional-state", f"{ROUTE}.data.snapshot-loading"),
    ("config-closure", "resource", f"{ROUTE}.data.config-closure"),
], "14401 是只读全量树；三张配置为 equipment/suit/kv。")
leaf(f"{ROUTE}.data.request", f"{ROUTE}.data", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.data.config-loading", f"{ROUTE}.data", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.data.snapshot-loading", f"{ROUTE}.data", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.data.config-closure", f"{ROUTE}.data", "read", "read-only", "needs-runtime-verify",
     "三张配置静态存在且行数可核；未运行 ResManager/Addressables，因此资源可达与第二次幂等仍需运行态验证。")

page(f"{ROUTE}.stages", ROUTE, [
    ("scroll-structure", "scroll-structure", f"{ROUTE}.stages.structure"),
    ("scroll-drag-last", "drag", f"{ROUTE}.stages.drag"),
    ("row-select", "list-item", f"{ROUTE}.stages.select"),
    ("locked-row", "conditional-list-item", f"{ROUTE}.stages.locked"),
    ("complete-mark", "conditional-state", f"{ROUTE}.stages.complete"),
    ("red-dot", "conditional-state", f"{ROUTE}.stages.red"),
], "9 阶数据驱动纵向列表；老端锁定行在 open_lv>370 时显示“神创N”。")
leaf(f"{ROUTE}.stages.structure", f"{ROUTE}.stages", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.stages.drag", f"{ROUTE}.stages", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.stages.select", f"{ROUTE}.stages", "tab", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.stages.locked", f"{ROUTE}.stages", "navigation", "read-only", "blocked",
     cross_equip + " Unity 当前总用“open_lv级”，未复刻老端 open_lv>370 的“神创N”文案。")
leaf(f"{ROUTE}.stages.complete", f"{ROUTE}.stages", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.stages.red", f"{ROUTE}.stages", "read", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.types", ROUTE, [
    ("type-origin", "tab", f"{ROUTE}.types.origin"),
    ("type-relic", "tab", f"{ROUTE}.types.relic"),
    ("selected-skin", "conditional-state", f"{ROUTE}.types.skin"),
    ("labels", "text", f"{ROUTE}.types.labels"),
    ("red-dots", "conditional-state", f"{ROUTE}.types.red"),
], "type1 荒陨圣骸，type2 天殒圣骸。")
leaf(f"{ROUTE}.types.origin", f"{ROUTE}.types", "tab", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.types.relic", f"{ROUTE}.types", "tab", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.types.skin", f"{ROUTE}.types", "read", "read-only", "blocked",
     cross_equip + " 老端选中/未选中切换 uizj_007/uizj_008，Unity 当前只改文字颜色。")
leaf(f"{ROUTE}.types.labels", f"{ROUTE}.types", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.types.red", f"{ROUTE}.types", "read", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.suit", ROUTE, [
    ("active-count", "state", f"{ROUTE}.suit.count"),
    ("active-status", "conditional-state", f"{ROUTE}.suit.status"),
    ("two-column-attributes", "text-list", f"{ROUTE}.suit.attributes"),
], "套装显示 active/total、已激活/未激活和左右两列属性。")
leaf(f"{ROUTE}.suit.count", f"{ROUTE}.suit", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.suit.status", f"{ROUTE}.suit", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.suit.attributes", f"{ROUTE}.suit", "read", "read-only", "needs-runtime-verify", runtime)

position_controls = [("layout", "layout", f"{ROUTE}.positions.layout")]
for pos in range(1, 6):
    position_controls.append((f"origin-pos-{pos}", "list-item", f"{ROUTE}.positions.origin-{pos}"))
for pos in range(6, 11):
    position_controls.append((f"relic-pos-{pos}", "list-item", f"{ROUTE}.positions.relic-{pos}"))
position_controls.append(("item-states", "conditional-state", f"{ROUTE}.positions.state"))
page(f"{ROUTE}.positions", ROUTE, position_controls,
     "config_armour_kv 固定 type1=[1..5]、type2=[6..10]；老端底部格 SetShowTips(false)，点击只选择。")
leaf(f"{ROUTE}.positions.layout", f"{ROUTE}.positions", "read", "read-only", "needs-runtime-verify", runtime)
position_block = (cross_equip + " Unity 动态 BaseAwardItem 保留默认详情点击，可能截断父 gp_con 的部位选择；"
                  "老端底部格明确 SetShowTips(false)，且未打造图标应灰阶。")
for pos in range(1, 6):
    leaf(f"{ROUTE}.positions.origin-{pos}", f"{ROUTE}.positions", "tab", "read-only", "blocked", position_block)
for pos in range(6, 11):
    leaf(f"{ROUTE}.positions.relic-{pos}", f"{ROUTE}.positions", "tab", "read-only", "blocked", position_block)
leaf(f"{ROUTE}.positions.state", f"{ROUTE}.positions", "read", "read-only", "blocked", position_block)

page(f"{ROUTE}.selected", ROUTE, [
    ("current-item", "item", f"{ROUTE}.selected.item"),
    ("item-details", "popup", f"{ROUTE}.selected.details"),
    ("equipment-attributes", "text-list", f"{ROUTE}.selected.attributes"),
    ("attribute-scroll-structure", "scroll-structure", f"{ROUTE}.selected.attr-structure"),
    ("attribute-scroll-drag", "drag", f"{ROUTE}.selected.attr-drag"),
], "顶部当前圣骸允许打开详情；未打造属性显示 0 与绿色增量。")
leaf(f"{ROUTE}.selected.item", f"{ROUTE}.selected", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.selected.details", f"{ROUTE}.selected", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.selected.attributes", f"{ROUTE}.selected", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.selected.attr-structure", f"{ROUTE}.selected", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.selected.attr-drag", f"{ROUTE}.selected", "navigation", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.materials", ROUTE, [
    ("slot-1", "item", f"{ROUTE}.materials.slot-1"),
    ("slot-2", "item", f"{ROUTE}.materials.slot-2"),
    ("slot-3", "item", f"{ROUTE}.materials.slot-3"),
    ("empty-lock", "conditional-state", f"{ROUTE}.materials.empty-lock"),
    ("quantity-and-gray", "conditional-state", f"{ROUTE}.materials.quantity"),
    ("material-details", "popup", f"{ROUTE}.materials.details"),
    ("prior-stage-state-cost", "conditional-state", f"{ROUTE}.materials.prior-stage"),
], "最多三材料槽；前阶圣骸状态项只作门禁/展示，不是背包消耗。")
for pos in range(1, 4):
    leaf(f"{ROUTE}.materials.slot-{pos}", f"{ROUTE}.materials", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.materials.empty-lock", f"{ROUTE}.materials", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.materials.quantity", f"{ROUTE}.materials", "read", "read-only", "blocked",
     cross_equip + " 老端普通不足材料图标置灰，Unity 当前只把数量文字置红，BaseAwardItem 未调用 SetGray。")
leaf(f"{ROUTE}.materials.details", f"{ROUTE}.materials", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.materials.prior-stage", f"{ROUTE}.materials", "read", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.make", ROUTE, [
    ("button-state", "button", f"{ROUTE}.make.button"),
    ("insufficient-toast", "conditional-result", f"{ROUTE}.make.insufficient"),
    ("already-made", "conditional-result", f"{ROUTE}.make.already"),
    ("confirm-popup", "popup", f"{ROUTE}.make.confirm"),
    ("pending-single-flight", "state", f"{ROUTE}.make.pending"),
    ("failure-result", "result", f"{ROUTE}.make.failure"),
    ("success-result", "result", f"{ROUTE}.make.success"),
    ("immediate-refresh", "state", f"{ROUTE}.make.immediate"),
    ("close-reopen", "state", f"{ROUTE}.make.reopen"),
    ("success-effect", "effect", f"{ROUTE}.make.effect"),
], "确认时冻结选择/配置/模型/背包指纹；14402 单飞且不乐观扣料/置位。")
leaf(f"{ROUTE}.make.button", f"{ROUTE}.make", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.make.insufficient", f"{ROUTE}.make", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.make.already", f"{ROUTE}.make", "read", "read-only", "needs-runtime-verify", runtime)
page(f"{ROUTE}.make.confirm", f"{ROUTE}.make", [
    ("frozen-material-text", "read", f"{ROUTE}.make.confirm.text"),
    ("cancel", "button", f"{ROUTE}.make.confirm.cancel"),
    ("confirm-14402", "button", f"{ROUTE}.make.confirm.submit"),
], "Unity 安全确认层；老端原页直接提交，但项目硬约束要求二次确认。")
leaf(f"{ROUTE}.make.confirm.text", f"{ROUTE}.make.confirm", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.make.confirm.cancel", f"{ROUTE}.make.confirm", "return", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.make.confirm.submit", f"{ROUTE}.make.confirm", "transaction", "destructive-write", "blocked", transaction)
leaf(f"{ROUTE}.make.pending", f"{ROUTE}.make", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.make.failure", f"{ROUTE}.make", "read", "read-only", "blocked", transaction)
leaf(f"{ROUTE}.make.success", f"{ROUTE}.make", "read", "read-only", "blocked", transaction)
leaf(f"{ROUTE}.make.immediate", f"{ROUTE}.make", "read", "read-only", "blocked", transaction)
leaf(f"{ROUTE}.make.reopen", f"{ROUTE}.make", "read", "read-only", "blocked", transaction)
leaf(f"{ROUTE}.make.effect", f"{ROUTE}.make", "read", "read-only", "blocked",
     cross_equip + " 老端成功调用 ui_dazaochengong(position=-0.85,0.55, scale=1.5)，Unity EquipArmorView 未调用对应成功特效。")

page(f"{ROUTE}.total-attrs", ROUTE, [
    ("open", "button", f"{ROUTE}.total-attrs.open"),
    ("attribute-list", "list", f"{ROUTE}.total-attrs.list"),
    ("empty-state", "conditional-state", f"{ROUTE}.total-attrs.empty"),
    ("scroll-structure", "scroll-structure", f"{ROUTE}.total-attrs.structure"),
    ("scroll-drag-last", "drag", f"{ROUTE}.total-attrs.drag"),
    ("mask-close", "return", f"{ROUTE}.total-attrs.close"),
], "老端 Activity 层弹窗、半透明遮罩、点遮罩关闭。")
leaf(f"{ROUTE}.total-attrs.open", f"{ROUTE}.total-attrs", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.total-attrs.list", f"{ROUTE}.total-attrs", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.total-attrs.empty", f"{ROUTE}.total-attrs", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.total-attrs.structure", f"{ROUTE}.total-attrs", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.total-attrs.drag", f"{ROUTE}.total-attrs", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.total-attrs.close", f"{ROUTE}.total-attrs", "return", "read-only", "blocked",
     cross_equip + " Unity ArmorAttrView 把弹窗主底图 _img_bg 绑定 Hide，未证明存在老端独立遮罩关闭面，可能点击卡片内容即关窗。")

leaf(f"{ROUTE}.root-red", ROUTE, "read", "read-only", "blocked",
     "老端 ArmorModel.UPDATE_RED 会刷新 MainFunc.Equip 主入口红点；Unity Armor 模块未提供等价根红点状态，修复涉及禁止写入的 MainUI/Equip。")

page(f"{ROUTE}.lifecycle", ROUTE, [
    ("tab-away-back", "navigation", f"{ROUTE}.lifecycle.tab-away"),
    ("hide-reopen", "navigation", f"{ROUTE}.lifecycle.reopen"),
    ("event-subscriptions", "state", f"{ROUTE}.lifecycle.events"),
    ("async-award-cleanup", "state", f"{ROUTE}.lifecycle.async-cleanup"),
], "需验证异步 BaseAwardItem 后到、事件解绑、切页/关窗零残留。")
leaf(f"{ROUTE}.lifecycle.tab-away", f"{ROUTE}.lifecycle", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.lifecycle.reopen", f"{ROUTE}.lifecycle", "navigation", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.lifecycle.events", f"{ROUTE}.lifecycle", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.lifecycle.async-cleanup", f"{ROUTE}.lifecycle", "read", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.visual", ROUTE, [
    ("350ms", "ready-frame", f"{ROUTE}.visual.350ms"),
    ("1000ms", "ready-frame", f"{ROUTE}.visual.1000ms"),
    ("ready", "ready-frame", f"{ROUTE}.visual.ready"),
    ("mobile-viewport", "viewport", f"{ROUTE}.visual.mobile"),
    ("wide-viewport", "viewport", f"{ROUTE}.visual.wide"),
], "2D 位置/尺寸/图片/文字/间距需同分辨率 old/unity/overlay/diff。")
for suffix in ("350ms", "1000ms", "ready", "mobile", "wide"):
    leaf(f"{ROUTE}.visual.{suffix}", f"{ROUTE}.visual", "read", "read-only", "needs-runtime-verify", runtime)

page(f"{ROUTE}.performance", ROUTE, [
    ("cold", "timing", f"{ROUTE}.performance.cold"),
    ("warm", "timing", f"{ROUTE}.performance.warm"),
    ("resource-idempotence", "resource", f"{ROUTE}.performance.resources"),
], "资源型页面需第二次 imported=0/configured=0，点击前后零新增。")
leaf(f"{ROUTE}.performance.cold", f"{ROUTE}.performance", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.performance.warm", f"{ROUTE}.performance", "read", "read-only", "needs-runtime-verify", runtime)
leaf(f"{ROUTE}.performance.resources", f"{ROUTE}.performance", "read", "read-only", "needs-runtime-verify", runtime)

manifest = {
    "route": ROUTE,
    "baseline": {
        "observed_at": "2026-08-09T00:00:00+08:00",
        "mode": "static-only",
        "legacy_source": "E:/GitProject/yu_client/h5/src/equipArmor + commonController/ArmorController.ts + commonModel/ArmorModel.ts",
        "unity_source": [
            "Assets/Scripts/Module/Core/Armor",
            "Assets/Prefabs/UI/EquipArmor",
            "Assets/Scripts/Module/Core/Equip/Views/EquipArmorView.cs (read-only)",
            "Assets/Scripts/Generated/UI/EquipArmor (read-only)",
        ],
        "runtime_not_executed": True,
    },
    "nodes": nodes,
}

(OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
(OUT / "results-static.json").write_text(json.dumps({"nodes": results}, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"generated nodes={len(nodes)} leaves={len(results)} blocked={sum(x['status']=='blocked' for x in results)} needs-runtime-verify={sum(x['status']=='needs-runtime-verify' for x in results)}")

