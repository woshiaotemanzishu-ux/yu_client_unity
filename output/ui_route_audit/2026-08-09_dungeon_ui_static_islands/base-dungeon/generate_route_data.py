import json
from pathlib import Path

ROOT = "mainui.base-dungeon.limit-tower"
OUT = Path(__file__).resolve().parent
SUMMARY = "output/ui_route_audit/2026-08-09_dungeon_ui_static_islands/base-dungeon/audit-summary.md"
ROUNDS = {
    1: list(range(40101, 40111)),
    2: list(range(40201, 40216)),
    3: list(range(40301, 40321)),
}

nodes = []
results = []


def node(node_id, node_type, risk, parent=None, controls=None):
    value = {"id": node_id, "type": node_type, "risk": risk}
    if parent is not None:
        value["parent"] = parent
    if controls is not None:
        value["control_inventory"] = [
            {"id": control_id, "kind": kind, "child": child}
            for control_id, kind, child in controls
        ]
    nodes.append(value)


def blocked(node_id, reason):
    results.append({
        "id": node_id,
        "status": "blocked",
        "blocked_reason": reason,
        "evidence": [SUMMARY],
    })


def runtime(node_id, gap, gates):
    results.append({
        "id": node_id,
        "status": "needs-runtime-verify",
        "runtime_gap": gap,
        "applicable_gates": gates,
        "gates": {gate: False for gate in gates},
        "evidence": [SUMMARY],
    })


open_id = ROOT + ".open"
icon_id = ROOT + ".icon-state"
page_id = open_id + ".page"
node(ROOT, "page", "read-only", controls=[
    ("activity-icon-331-97", "conditional-entry", icon_id),
    ("activity-icon-click", "button", open_id),
])
node(icon_id, "read", "read-only", ROOT)
node(open_id, "navigation", "read-only", ROOT)
node(page_id, "page", "read-only", open_id, controls=[
    ("round-theme", "conditional-visual", page_id + ".round-theme"),
    ("progress", "state", page_id + ".progress"),
    ("countdown", "state", page_id + ".countdown"),
    ("big-reward-state", "conditional-state", page_id + ".big-reward-state"),
    ("big-reward-item", "item", page_id + ".big-reward-item"),
    ("big-reward-claim", "button", page_id + ".big-reward-claim"),
    ("stage-list", "conditional-list", page_id + ".stage-list"),
    ("close", "return", page_id + ".return"),
])
node(page_id + ".round-theme", "read", "read-only", page_id)
node(page_id + ".progress", "read", "read-only", page_id)
node(page_id + ".countdown", "read", "read-only", page_id)
node(page_id + ".big-reward-state", "read", "read-only", page_id)
node(page_id + ".big-reward-item", "navigation", "read-only", page_id)
node(page_id + ".big-reward-claim", "transaction", "destructive-write", page_id)

stage_list = page_id + ".stage-list"
node(stage_list, "page", "read-only", page_id, controls=[
    ("curve-drag", "scroll", stage_list + ".drag"),
    ("round-1", "conditional-group", stage_list + ".round-1"),
    ("round-2", "conditional-group", stage_list + ".round-2"),
    ("round-3", "conditional-group", stage_list + ".round-3"),
])
node(stage_list + ".drag", "read", "read-only", stage_list)
node(page_id + ".return", "return", "read-only", page_id)

runtime(icon_id,
        "61117 驱动图标显隐/红点与会话内持续显示已静态接线；禁止启动 Unity/Web，本轮缺真实账号条件矩阵。",
        ["runtime_state", "reopen"])
runtime(open_id,
        "331@97 已注册 MainUIRouter 并加载现有 DungeonTowerModule/BaseWindowSkin；缺 GraphicRaycaster、目标身份和 cold/warm。",
        ["click", "result", "target_identity", "timing"])
runtime(page_id + ".round-theme",
        "三轮标题/内容底图已按老端固定资源名接线；ui_limit_tower_1..3 大背景当前未入 Unity 资源闭包，且缺真实 Web 像素证据。",
        ["runtime_state", "resource_stable", "visual_match"])
runtime(page_id + ".progress",
        "61117 pass_list 已保留；总关数依赖缺失的 config_limit_tower_round，当前安全隐藏 fill，需配置入库后真实复验。",
        ["runtime_state", "reopen"])
runtime(page_id + ".countdown",
        "over_time 倒计时已按服务端时钟每秒刷新；缺隐藏/重开/到零与真实像素复验。",
        ["runtime_state", "reopen", "visual_match"])
runtime(page_id + ".big-reward-state",
        "reward_mode 0/1/2 显隐及 61118 成功后即时切换已静态接线；未执行账号写事务，缺三态运行证据。",
        ["runtime_state", "immediate", "reopen"])
blocked(page_id + ".big-reward-item",
        "大奖物品详情依赖 config_limit_tower_round.big_reward 与 Common 物品详情链；二者不在本岛可写闭包。")
blocked(page_id + ".big-reward-claim",
        "61118 是真实发奖写事务；本轮无账号写授权且缺 Common 恭喜弹窗/奖励即时到账与重开证据，禁止执行。")
blocked(stage_list + ".drag",
        "老端为 6 个复用项沿贝塞尔曲线拖动；Unity 缺权威关卡目录与具名曲线槽位，禁止用普通纵向列表或代码猜坐标。")
runtime(page_id + ".return",
        "入口复用 BaseWindowSkin 返回链并调用 View.Hide；未做真实射线点击、遮罩优先级和关闭重开。",
        ["click", "return_chain", "reopen"])

for round_no, dungeon_ids in ROUNDS.items():
    round_id = stage_list + f".round-{round_no}"
    round_controls = []
    for index, dungeon_id in enumerate(dungeon_ids, 1):
        tab_id = round_id + f".stage-{dungeon_id}"
        round_controls.append((f"stage-{dungeon_id}", "list-item", tab_id))
    node(round_id, "page", "read-only", stage_list, round_controls)

    for index, dungeon_id in enumerate(dungeon_ids, 1):
        tab_id = round_id + f".stage-{dungeon_id}"
        detail_id = tab_id + ".detail"
        node(tab_id, "tab", "read-only", round_id)
        node(detail_id, "page", "read-only", tab_id, controls=[
            ("stage-state", "state", detail_id + ".state"),
            ("reward-list", "list", detail_id + ".rewards"),
            ("challenge", "button", detail_id + ".challenge"),
        ])
        node(detail_id + ".state", "read", "read-only", detail_id)
        rewards_id = detail_id + ".rewards"
        node(rewards_id, "page", "read-only", detail_id, controls=[
            ("reward-1", "item", rewards_id + ".item-1"),
            ("reward-2", "item", rewards_id + ".item-2"),
            ("reward-3", "item", rewards_id + ".item-3"),
        ])
        for reward_index in range(1, 4):
            reward_id = rewards_id + f".item-{reward_index}"
            node(reward_id, "navigation", "read-only", rewards_id)
            blocked(reward_id,
                    f"关卡 {dungeon_id} 第 {reward_index} 个奖励需 config_dungeon_grade 与 Common 物品详情；本岛不复制共享组件、不猜奖励映射。")
        node(detail_id + ".challenge", "transaction", "destructive-write", detail_id)

        blocked(tab_id,
                f"关卡 {dungeon_id} 属当前老端 round={round_no} 固定目录；Unity 缺 config_limit_tower_round 闭包和曲线列表实现，尚不能生成/点击真实格。")
        blocked(detail_id + ".state",
                f"关卡 {dungeon_id} 的名称、推荐战力、前置可挑战、当前战力颜色与选中红点依赖 config_dungeon + Role 权威态，未做同账号运行核查。")
        blocked(detail_id + ".challenge",
                f"关卡 {dungeon_id} 挑战进入需 Dungeon 61001/TryToEnterDungeon/战斗场景生命周期；跨岛且为真实账号操作，只登记 blocker。")

manifest = {
    "route": ROOT,
    "baseline": {
        "legacy_bundle": "E:/GitProject/yu_client/cdn/js/bundle.js",
        "legacy_round_config": "E:/GitProject/yu_client/cdn/resource/config/server/config_limit_tower_round.json",
        "unity_prefab": "Assets/Prefabs/UI/DungeonTower/DungeonTowerModule.prefab",
        "generated_binds": [
            "Assets/Scripts/Generated/UI/DungeonTower/DungeonTowerViewBind.cs",
            "Assets/Scripts/Generated/UI/DungeonTower/DungeonTowerItemBind.cs",
        ],
    },
    "nodes": nodes,
}

(OUT / "route-manifest.json").write_text(
    json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "results-static.json").write_text(
    json.dumps({"nodes": results}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"nodes": len(nodes), "leaves": len(results)}, ensure_ascii=False))
