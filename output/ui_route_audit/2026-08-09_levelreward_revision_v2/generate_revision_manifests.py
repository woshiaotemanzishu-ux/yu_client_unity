import json
import re
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
LEVEL_DIR = REPO / "output/ui_route_audit/2026-08-09_levelreward_revision_v2"
GROWTH_DIR = REPO / "output/ui_route_audit/2026-08-09_growthbenefits_revision_v2"
LEVEL_CONFIG = Path("E:/GitProject/yu_client/cdn/resource/config/server/config_rush_giftbag.json")
GROWTH_CONFIG = Path("E:/GitProject/yu_client/cdn/resource/config/server/config_grow_welfare_info.json")


class Route:
    def __init__(self, route, baseline, evidence):
        self.route = route
        self.baseline = baseline
        self.evidence = evidence
        self.nodes = []
        self.children = {}

    def page(self, node_id, parent, controls, note=""):
        inventory = []
        for child_id, kind in controls:
            inventory.append({"id": child_id.rsplit(".", 1)[-1], "kind": kind, "child": child_id})
            self.children.setdefault(node_id, []).append(child_id)
        node = {"id": node_id, "type": "page", "risk": "read-only", "parent": parent,
                "control_inventory": inventory}
        if note:
            node["note"] = note
        self.nodes.append(node)

    def leaf(self, node_id, parent, note, node_type="read", risk="read-only"):
        self.nodes.append({"id": node_id, "type": node_type, "risk": risk, "parent": parent, "note": note})

    def manifest(self):
        return {"route": self.route, "baseline": self.baseline, "nodes": self.nodes}

    def results(self, needs_runtime):
        parent_ids = {node["parent"] for node in self.nodes if node["parent"] is not None}
        output = []
        for node in self.nodes:
            if node["id"] in parent_ids:
                continue
            node_id = node["id"]
            if node_id in needs_runtime:
                gates = ["runtime_state"]
                if node["type"] in ("navigation", "return", "tab"):
                    gates += ["click", "target_identity"]
                elif any(token in node_id for token in ("visual", "title", "tips", "identity", "state")):
                    gates += ["visual_match"]
                output.append({
                    "id": node_id,
                    "status": "needs-runtime-verify",
                    "runtime_gap": "静态实现或 Prefab 结构已确定；仍需同账号真实 Unity Web 与老 H5 顺序复走。",
                    "applicable_gates": gates,
                    "gates": {gate: False for gate in gates},
                    "evidence": [self.evidence],
                })
            else:
                reason = "该叶依赖尚未落地的页面业务/配置/共享弹窗或文件岛外路由，静态证据不足。"
                if node["risk"] == "destructive-write":
                    reason = "领取/发奖为未授权写事务；本轮只枚举，禁止点击且不得发送协议。"
                output.append({"id": node_id, "status": "blocked", "blocked_reason": reason,
                               "evidence": [self.evidence]})
        return {"nodes": output}


def count_tuples(value):
    return len(re.findall(r"\{[^{}]*\}", str(value or "")))


def detail_cell(route, cell_id, parent, note):
    controls = [(cell_id + ".detail.identity", "popup-identity"),
                (cell_id + ".detail.main-bg", "popup-background"),
                (cell_id + ".detail.close", "popup-close"),
                (cell_id + ".detail.background-return", "popup-background-return")]
    route.page(cell_id, parent, controls, note)
    route.leaf(controls[0][0], cell_id, "逐格核对具体详情 View 类型与数据身份。", "navigation")
    route.leaf(controls[1][0], cell_id, "详情根尺寸、主底图 Sprite 启用且实际绘制。")
    route.leaf(controls[2][0], cell_id, "详情关闭按钮只关闭当前弹窗并回到原列表。", "return")
    route.leaf(controls[3][0], cell_id, "详情背景/遮罩返回不穿透宿主页面。", "return")


def build_level():
    config = json.loads(LEVEL_CONFIG.read_text(encoding="utf-8"))
    tiers = sorted(((int(key), value) for key, value in config.items()), key=lambda item: item[0])
    assert len(tiers) == 22

    baseline = {
        "manifest_revision": 2,
        "authority": "当前老 H5 同账号同状态同 viewport 的真实结果为唯一权威；本 revision 冻结完整拓扑。",
        "legacy_sources": [
            "E:/GitProject/yu_client/h5/src/levelReward/LevelRewardView.ts",
            "E:/GitProject/yu_client/h5/src/levelReward/LevelRewardItem.ts",
            "E:/GitProject/yu_client/h5/src/levelReward/LevelReward.ts",
            "E:/GitProject/yu_client/cdn/resource/config/server/config_rush_giftbag.json",
        ],
        "unity_sources": [
            "Assets/Prefabs/UI/LevelReward/LevelRewardModule.prefab",
            "Assets/Scripts/Module/Core/LevelReward/LevelRewardFlow.cs",
            "Assets/Scripts/Module/Core/LevelReward/Views/LevelRewardView.cs",
            "Assets/Scripts/Module/Core/LevelReward/Views/LevelRewardItem.cs",
            "Assets/Scripts/Module/Core/LevelReward/Views/LevelReward.cs",
            "Assets/Scripts/Module/Core/RushGift/RushGiftModel.cs",
            "Assets/Scripts/Module/Core/RushGift/RushGiftController.cs",
        ],
        "protocol_inventory": {"reads": ["41700"], "writes": ["41701"],
                               "note": "41701 及成功/失败结果链全部只枚举不点击。"},
        "frozen_inventory": {"tier_count": 22, "reward_cells_per_tier": "2..6",
                             "received_states": [0, 1, 2, 3, 4]},
        "component_dependencies": [
            {"component": "EquipmentItem/Common detail", "scope": "read-only outside island"},
            {"component": "CongratulationObtainView", "scope": "41701 success popup, read-only dependency"},
            {"component": "RewardFlyService", "scope": "41701 success animation, read-only dependency"},
            {"component": "ArrowComponent/story finger", "scope": "two finger consumers, read-only dependency"},
        ],
    }
    evidence = "output/ui_route_audit/2026-08-09_levelreward_revision_v2/static-audit.md"
    r = Route("mainui.level-reward", baseline, evidence)
    root_controls = [("mainui.level-reward.entry", "conditional-entry"),
                     ("mainui.level-reward.shell", "page-shell"),
                     ("mainui.level-reward.list", "vertical-list"),
                     ("mainui.level-reward.tiers", "frozen-tier-inventory"),
                     ("mainui.level-reward.claim-result", "transaction-result"),
                     ("mainui.level-reward.story-fingers", "conditional-guidance"),
                     ("mainui.level-reward.return", "return"),
                     ("mainui.level-reward.lifecycle", "route-lifecycle")]
    r.page("mainui.level-reward", None, root_controls)

    entry = [("mainui.level-reward.entry.open-day-le7", "condition"),
             ("mainui.level-reward.entry.open-day-gt7", "condition"),
             ("mainui.level-reward.entry.red", "state"),
             ("mainui.level-reward.entry.open", "navigation")]
    r.page("mainui.level-reward.entry", "mainui.level-reward", entry)
    r.leaf(entry[0][0], "mainui.level-reward.entry", "开服日<=7 使用 331@3_1 入口资源/位置。")
    r.leaf(entry[1][0], "mainui.level-reward.entry", "开服日>7 使用 331@3 入口资源/位置。")
    r.leaf(entry[2][0], "mainui.level-reward.entry", "任一 received=1 时入口红点。")
    r.leaf(entry[3][0], "mainui.level-reward.entry", "玩家可见入口真实点击打开现有 Prefab。", "navigation")

    shell = [("mainui.level-reward.shell.identity", "page-identity"),
             ("mainui.level-reward.shell.title", "image"),
             ("mainui.level-reward.shell.tips", "text-image")]
    r.page("mainui.level-reward.shell", "mainui.level-reward", shell)
    r.leaf(shell[0][0], "mainui.level-reward.shell", "页面根尺寸、Activity 层身份与背景。")
    r.leaf(shell[1][0], "mainui.level-reward.shell", "标题图最终玩家像素。")
    r.leaf(shell[2][0], "mainui.level-reward.shell", "提示图、Label、条件文案和裁剪。")

    listing = [("mainui.level-reward.list.structure", "scroll-structure"),
               ("mainui.level-reward.list.template-sibling", "prefab-template"),
               ("mainui.level-reward.list.sort", "sort"),
               ("mainui.level-reward.list.empty", "state"),
               ("mainui.level-reward.list.vertical-drag", "interaction"),
               ("mainui.level-reward.list.last-tier", "interaction")]
    r.page("mainui.level-reward.list", "mainui.level-reward", listing)
    r.leaf(listing[0][0], "mainui.level-reward.list", "Vertical ScrollRect→Viewport/Mask→Content/Layout。")
    r.leaf(listing[1][0], "mainui.level-reward.list", "View 与 inactive LevelRewardItem 是模块根 sibling；从 transform.parent 定位模板。")
    r.leaf(listing[2][0], "mainui.level-reward.list", "received=1 最前、2 最后、其余 received 升序，再 lv 升序。")
    r.leaf(listing[3][0], "mainui.level-reward.list", "41700 未到/权威空包不造假行。")
    r.leaf(listing[4][0], "mainui.level-reward.list", "真实纵向拖动与裁剪。")
    r.leaf(listing[5][0], "mainui.level-reward.list", "末档可达并能打开其奖励详情。")

    tier_controls = []
    for key, value in tiers:
        tier_controls.append((f"mainui.level-reward.tiers.lv-{key}", "tier-row"))
    r.page("mainui.level-reward.tiers", "mainui.level-reward", tier_controls,
           "冻结 config_rush_giftbag 的 22 档，每档逐行、逐状态、逐奖励格。")

    needs = {
        "mainui.level-reward.entry.open",
        "mainui.level-reward.shell.identity", "mainui.level-reward.shell.title", "mainui.level-reward.shell.tips",
        "mainui.level-reward.list.structure", "mainui.level-reward.list.template-sibling",
        "mainui.level-reward.list.sort", "mainui.level-reward.list.empty",
        "mainui.level-reward.list.vertical-drag", "mainui.level-reward.list.last-tier",
    }

    for key, value in tiers:
        tier_id = f"mainui.level-reward.tiers.lv-{key}"
        base_man = count_tuples(value.get("bag_gift_man"))
        base_woman = count_tuples(value.get("bag_gift_woman"))
        limit_man = count_tuples(value.get("limit_gift_man"))
        limit_woman = count_tuples(value.get("limit_gift_woman"))
        assert base_man == base_woman and limit_man == limit_woman
        cell_count = base_man + limit_man
        assert 2 <= cell_count <= 6
        row = [(tier_id + ".level-destiny", "conditional-text"),
               (tier_id + ".remain", "conditional-text"),
               (tier_id + ".reward-strip", "horizontal-reward-list"),
               (tier_id + ".received", "state-matrix")]
        r.page(tier_id, "mainui.level-reward.tiers", row,
               f"配置键 {key}, bag_lv={value.get('bag_lv')}, 可见奖励格={cell_count}。")
        r.leaf(row[0][0], tier_id, "bag_lv<=370 等级显示；>370 转 destiny_gp 与 bag_lv-370。")
        r.leaf(row[1][0], tier_id, "bag_upperlimit、draw/remain 与领完文案颜色。")

        strip = row[2][0]
        strip_controls = [(strip + ".structure", "scroll-structure"),
                          (strip + ".horizontal-drag", "interaction"),
                          (strip + ".nested-conflict", "nested-scroll"),
                          (strip + ".last-cell", "interaction")]
        for index in range(1, cell_count + 1):
            strip_controls.append((strip + f".cell-{index}", "reward-cell"))
        r.page(strip, tier_id, strip_controls)
        r.leaf(strip_controls[0][0], strip, "行级 _Scroller1/_gp_item_con 横向 ScrollRect 结构。")
        r.leaf(strip_controls[1][0], strip, "从可命中奖励格横向拖动。")
        r.leaf(strip_controls[2][0], strip, "行横向拖动与页面纵向拖动方向仲裁，无误触/锁死。")
        r.leaf(strip_controls[3][0], strip, "2–6 格场景末格完整可达且裁剪正确。")
        for index in range(1, cell_count + 1):
            kind = "limit_gift_{sex}" if index > base_man else "bag_gift_{sex}"
            detail_cell(r, strip + f".cell-{index}", strip,
                        f"lv={key} 奖励格 {index}/{cell_count}, 来源={kind}，男女配置数量一致但内容需分别核对。")

        received = row[3][0]
        states = [(received + ".state-0", "condition-result"),
                  (received + ".state-1-claim", "transaction"),
                  (received + ".state-2", "received-result"),
                  (received + ".state-3", "silent-result"),
                  (received + ".state-4", "sold-out-result")]
        r.page(received, tier_id, states)
        r.leaf(states[0][0], received, "received=0 点击反馈『条件不足』，不发协议。")
        r.leaf(states[1][0], received, "received=1 才允许 41701(lv)；本轮禁发。", "transaction", "destructive-write")
        r.leaf(states[2][0], received, "received=2 显示已领取，点击语义『已领取～』且不发协议。")
        r.leaf(states[3][0], received, "received=3 点击静默且不发协议。")
        r.leaf(states[4][0], received, "received=4 点击反馈『已被领完~』且不发协议。")
        needs.update({states[0][0], states[2][0], states[3][0], states[4][0]})

    claim = [("mainui.level-reward.claim-result.success-popup", "result-popup"),
             ("mainui.level-reward.claim-result.success-banner", "result-banner"),
             ("mainui.level-reward.claim-result.failure-error", "error-result"),
             ("mainui.level-reward.claim-result.immediate-refresh", "state-refresh"),
             ("mainui.level-reward.claim-result.reward-fly", "reward-animation"),
             ("mainui.level-reward.claim-result.task-100420-close", "conditional-close"),
             ("mainui.level-reward.claim-result.delete-me-check-item-use", "cleanup")]
    r.page("mainui.level-reward.claim-result", "mainui.level-reward", claim)
    r.leaf(claim[0][0], "mainui.level-reward.claim-result", "41701 成功打开 CongratulationObtainView(data,10)。", "transaction", "destructive-write")
    r.leaf(claim[1][0], "mainui.level-reward.claim-result", "成功弹窗 banner=LevelReward。", "transaction", "destructive-write")
    r.leaf(claim[2][0], "mainui.level-reward.claim-result", "41701 失败走 ErrorCodeShow，父页不假刷新。", "transaction", "destructive-write")
    r.leaf(claim[3][0], "mainui.level-reward.claim-result", "成功重发41700并即时刷新当前父页。", "transaction", "destructive-write")
    r.leaf(claim[4][0], "mainui.level-reward.claim-result", "成功后通用奖励飞行，多时点与关页零残留。", "transaction", "destructive-write")
    r.leaf(claim[5][0], "mainui.level-reward.claim-result", "主线100420点击任意领取面后的强制关壳分支。", "transaction", "destructive-write")
    r.leaf(claim[6][0], "mainui.level-reward.claim-result", "DeleteMe/CHECK_ITEM_USE 清理与任务链一致。", "transaction", "destructive-write")

    fingers = [("mainui.level-reward.story-fingers.page", "page-guidance"),
               ("mainui.level-reward.story-fingers.row", "row-guidance")]
    r.page("mainui.level-reward.story-fingers", "mainui.level-reward", fingers)
    r.leaf(fingers[0][0], "mainui.level-reward.story-fingers", "页面级故事手指条件、位置与清理。")
    r.leaf(fingers[1][0], "mainui.level-reward.story-fingers", "可领取行级 ArrowComponent 条件、位置与清理。")

    r.leaf("mainui.level-reward.return", "mainui.level-reward", "入口再次点击/外层返回关闭并回主界面。", "return")
    needs.add("mainui.level-reward.return")
    life = [("mainui.level-reward.lifecycle.update", "live-update"),
            ("mainui.level-reward.lifecycle.warm", "reopen"),
            ("mainui.level-reward.lifecycle.visual", "visual"),
            ("mainui.level-reward.lifecycle.performance", "performance"),
            ("mainui.level-reward.lifecycle.sound", "sound")]
    r.page("mainui.level-reward.lifecycle", "mainui.level-reward", life)
    for child, note in zip(life, ["41700 同页即时刷新且无重复克隆。", "hide/reopen/ReleaseInstance 无事件悬挂。",
                                  "720x1280/1920x1080 old/unity/overlay/diff。", "cold/warm 与资源稳定。",
                                  "页面专属声音/通用点击声与关闭后无串台。"]):
        r.leaf(child[0], "mainui.level-reward.lifecycle", note)
        needs.add(child[0])

    return r, needs


def build_growth():
    config = json.loads(GROWTH_CONFIG.read_text(encoding="utf-8"))
    tasks = sorted(config.values(), key=lambda value: int(value["task_id"]))
    days = {day: [] for day in range(1, 8)}
    for task in tasks:
        days[int(task["open_day"])].append(task)
    assert len(tasks) == 36
    assert [len(days[day]) for day in range(1, 8)] == [6, 5, 5, 5, 5, 5, 5]

    baseline = {
        "manifest_revision": 2,
        "authority": "当前老 H5 同账号同状态同 viewport 的真实结果为唯一权威；本 revision 冻结7天36任务及逐格拓扑。",
        "legacy_sources": [
            "E:/GitProject/yu_client/h5/src/growthForce/GrowthForceView.ts",
            "E:/GitProject/yu_client/h5/src/growthBenefits/GrowthBenefitsView.ts",
            "E:/GitProject/yu_client/h5/src/growthBenefits/GrowthBenefitsTabItem.ts",
            "E:/GitProject/yu_client/h5/src/growthBenefits/GrowthBenefitTaskItem.ts",
            "E:/GitProject/yu_client/h5/src/growthBenefits/GrowthBenefitsAwardItem.ts",
            "E:/GitProject/yu_client/cdn/resource/config/server/config_grow_welfare_info.json",
        ],
        "unity_sources": [
            "Assets/Prefabs/UI/GrowthBenefits/GrowthBenefitsModule.prefab",
            "Assets/Scripts/Generated/UI/GrowthBenefits/GrowthBenefitsViewBind.cs",
            "Assets/Scripts/Generated/UI/GrowthBenefits/GrowthBenefitsTabItemBind.cs",
            "Assets/Scripts/Generated/UI/GrowthBenefits/GrowthBenefitTaskItemBind.cs",
            "Assets/Scripts/Generated/UI/GrowthBenefits/GrowthBenefitsAwardItemBind.cs",
            "Assets/Scripts/Module/Core/GrowthBenefits/GrowthBenefitsModel.cs",
            "Assets/Scripts/Module/Core/GrowthBenefits/GrowthBenefitsController.cs",
        ],
        "protocol_inventory": {"reads": ["41720", "41721"], "writes": ["41722"],
                               "note": "41722 领取全部只枚举不点击；战力页签属于外层依赖。"},
        "frozen_inventory": {"days": 7, "tasks": 36, "tasks_per_day": [6, 5, 5, 5, 5, 5, 5],
                             "rewards_per_task": "1..3"},
        "component_dependencies": [
            {"component": "GrowthForceView", "scope": "outer shell outside writable island"},
            {"component": "BaseAwardItem/Common detail", "scope": "read-only shared dependency"},
            {"component": "OpenFun", "scope": "jump navigation outside writable island"},
            {"component": "Combat welfare page", "scope": "conditional outer tab outside writable island"},
        ],
    }
    evidence = "output/ui_route_audit/2026-08-09_growthbenefits_revision_v2/static-audit.md"
    r = Route("mainui.growth-benefits", baseline, evidence)
    root = [("mainui.growth-benefits.entry", "conditional-entry"),
            ("mainui.growth-benefits.outer", "outer-shell"),
            ("mainui.growth-benefits.tabs", "day-tabs"),
            ("mainui.growth-benefits.days", "frozen-day-task-inventory"),
            ("mainui.growth-benefits.combat", "conditional-tab-page"),
            ("mainui.growth-benefits.lifecycle", "route-lifecycle")]
    r.page("mainui.growth-benefits", None, root)

    entry = [("mainui.growth-benefits.entry.visible", "condition"),
             ("mainui.growth-benefits.entry.red", "state"),
             ("mainui.growth-benefits.entry.open", "navigation")]
    r.page("mainui.growth-benefits.entry", "mainui.growth-benefits", entry)
    r.leaf(entry[0][0], "mainui.growth-benefits.entry", "等级/任务全领/战力福利组合显隐。")
    r.leaf(entry[1][0], "mainui.growth-benefits.entry", "已解锁日存在 status=1 的红点。")
    r.leaf(entry[2][0], "mainui.growth-benefits.entry", "入口打开 GrowthForce 外层并选成长福利。", "navigation")

    outer = [("mainui.growth-benefits.outer.identity", "page-identity"),
             ("mainui.growth-benefits.outer.dynamic-bg", "dynamic-image"),
             ("mainui.growth-benefits.outer.dynamic-title", "dynamic-image"),
             ("mainui.growth-benefits.outer.dynamic-name", "dynamic-text"),
             ("mainui.growth-benefits.outer.one-tab-center", "layout-state"),
             ("mainui.growth-benefits.outer.two-tabs-center", "layout-state"),
             ("mainui.growth-benefits.outer.close-box", "return"),
             ("mainui.growth-benefits.outer.background-return", "return")]
    r.page("mainui.growth-benefits.outer", "mainui.growth-benefits", outer)
    for child, note in zip(outer, ["GrowthForce Activity层身份、根尺寸与子页宿主。", "按页签切换的动态背景。",
                                   "按页签切换的动态标题。", "动态页签名称/选中态。", "仅1页签时整体居中。",
                                   "2页签时整体居中且点击面不被布局拉伸。", "closeBox 关闭外层返回主界面。",
                                   "背景遮罩关闭外层且不穿透主界面。"]):
        r.leaf(child[0], "mainui.growth-benefits.outer", note, "return" if "return" in child[1] else "read")

    tabs = [("mainui.growth-benefits.tabs.structure", "scroll-structure"),
            ("mainui.growth-benefits.tabs.default-red-day", "default-selection"),
            ("mainui.growth-benefits.tabs.complete-removal", "conditional-removal"),
            ("mainui.growth-benefits.tabs.horizontal-drag", "interaction"),
            ("mainui.growth-benefits.tabs.clipping", "clipping"),
            ("mainui.growth-benefits.tabs.last-day", "interaction"),
            ("mainui.growth-benefits.tabs.overflow-red", "conditional-indicator")]
    r.page("mainui.growth-benefits.tabs", "mainui.growth-benefits", tabs)
    for child, note in zip(tabs, ["7日横向 ScrollRect→Viewport/Mask→Content/Layout。", "优先首个红点日，否则首个解锁未全领日。",
                                  "当日任务全领后的完成页签移除/完成态。", "从页签真实横向拖动。", "首尾与半入视口裁剪。",
                                  "第7日页签可达并可点击。", "不可见红点页对应右侧提示动画。"]):
        r.leaf(child[0], "mainui.growth-benefits.tabs", note)

    day_controls = [(f"mainui.growth-benefits.days.day-{day}", "day-page") for day in range(1, 8)]
    r.page("mainui.growth-benefits.days", "mainui.growth-benefits", day_controls,
           "冻结7天：第1日6任务，其余每日5任务。")

    for day in range(1, 8):
        day_id = f"mainui.growth-benefits.days.day-{day}"
        controls = [(day_id + ".tab-selected", "tab-state"),
                    (day_id + ".tab-locked", "tab-state"),
                    (day_id + ".tab-red", "tab-state"),
                    (day_id + ".tab-complete", "tab-state")]
        controls += [(day_id + f".task-{int(task['task_id'])}", "task-row") for task in days[day]]
        r.page(day_id, "mainui.growth-benefits.days", controls,
               f"第{day}日，冻结任务数={len(days[day])}。")
        r.leaf(controls[0][0], day_id, "选中/未选视觉与内容切换。")
        r.leaf(controls[1][0], day_id, "未来日锁定，点击提示第N天解锁。")
        r.leaf(controls[2][0], day_id, "当日任一 status=1 红点。")
        r.leaf(controls[3][0], day_id, "当日全领取后的完成图与页签移除规则。")

        for task in days[day]:
            task_id_num = int(task["task_id"])
            task_id = day_id + f".task-{task_id_num}"
            reward_count = count_tuples(task.get("reward"))
            assert 1 <= reward_count <= 3
            jump_id = int(task.get("jump_id", 0))
            task_controls = [(task_id + ".description", "text"),
                             (task_id + ".progress", "progress"),
                             (task_id + ".state-0-jump", "navigation"),
                             (task_id + ".state-1-claim", "transaction"),
                             (task_id + ".state-2-received", "received-state"),
                             (task_id + ".award-list", "horizontal-award-list")]
            r.page(task_id, day_id, task_controls,
                   f"task_id={task_id_num}, desc={task.get('desc')}, jump_id={jump_id}, rewards={reward_count}。")
            r.leaf(task_controls[0][0], task_id, "配置 desc 与长短文案布局。")
            r.leaf(task_controls[1][0], task_id, "41720/41721 process/condition 与达标颜色。")
            jump_note = f"status=0/缺状态时 OpenFun({jump_id}) 并关闭 GrowthForce。"
            if jump_id == 0:
                jump_note += " 特殊分支：即使 jumpBox 可见仍调用 OpenFun(0)+关壳。"
            r.leaf(task_controls[2][0], task_id, jump_note, "navigation")
            r.leaf(task_controls[3][0], task_id, "status=1 才发送 41722(task_id)；本轮禁发。",
                   "transaction", "destructive-write")
            r.leaf(task_controls[4][0], task_id, "status=2 已领取图、按钮隐藏与重开一致。")

            award = task_controls[5][0]
            award_controls = [(award + ".structure", "scroll-structure"),
                              (award + ".horizontal-drag", "interaction"),
                              (award + ".clipping", "clipping"),
                              (award + ".last-cell", "interaction")]
            award_controls += [(award + f".cell-{index}", "reward-cell") for index in range(1, reward_count + 1)]
            r.page(award, task_id, award_controls)
            r.leaf(award_controls[0][0], award, "awardList 横向结构与 BaseAwardItem 宿主。")
            r.leaf(award_controls[1][0], award, "从奖励格真实横向拖动。")
            r.leaf(award_controls[2][0], award, "1–3格完整/半入/离开视口裁剪。")
            r.leaf(award_controls[3][0], award, "末格可达且点击命中末格。")
            for index in range(1, reward_count + 1):
                detail_cell(r, award + f".cell-{index}", award,
                            f"day={day}, task_id={task_id_num}, reward={index}/{reward_count}。")

    combat = [("mainui.growth-benefits.combat.tab-condition", "condition"),
              ("mainui.growth-benefits.combat.identity", "page-identity"),
              ("mainui.growth-benefits.combat.return-growth", "tab-return"),
              ("mainui.growth-benefits.combat.return-main", "return")]
    r.page("mainui.growth-benefits.combat", "mainui.growth-benefits", combat)
    r.leaf(combat[0][0], "mainui.growth-benefits.combat", "战力福利页签条件显隐。")
    r.leaf(combat[1][0], "mainui.growth-benefits.combat", "战力福利目标页具体 View 身份与动态壳资源。", "navigation")
    r.leaf(combat[2][0], "mainui.growth-benefits.combat", "从战力页签返回成长福利且状态保持。", "return")
    r.leaf(combat[3][0], "mainui.growth-benefits.combat", "从战力页关闭外层回主界面。", "return")

    life = [("mainui.growth-benefits.lifecycle.update", "live-update"),
            ("mainui.growth-benefits.lifecycle.day-change", "day-change"),
            ("mainui.growth-benefits.lifecycle.warm", "reopen"),
            ("mainui.growth-benefits.lifecycle.visual", "visual"),
            ("mainui.growth-benefits.lifecycle.performance", "performance"),
            ("mainui.growth-benefits.lifecycle.sound", "sound")]
    r.page("mainui.growth-benefits.lifecycle", "mainui.growth-benefits", life)
    for child, note in zip(life, ["41720全量/41721增量/41722结果的同页即时刷新。", "跨天解锁、红点和默认页变化。",
                                  "hide/reopen 无重复订阅、旧页签或旧滚动。", "两档 viewport old/unity/overlay/diff。",
                                  "cold/warm、列表分配与资源稳定。", "通用点击声/结果声与关闭后无串台。"]):
        r.leaf(child[0], "mainui.growth-benefits.lifecycle", note)

    # 当前仅有 Prefab/Bind 与协议模型，外层路由、业务 View、配置数据消费者都缺失。
    needs = {"mainui.growth-benefits.outer.identity", "mainui.growth-benefits.lifecycle.visual",
             "mainui.growth-benefits.lifecycle.performance", "mainui.growth-benefits.lifecycle.sound"}
    return r, needs


def write(route, needs, directory):
    directory.mkdir(parents=True, exist_ok=True)
    (directory / "route-manifest.json").write_text(
        json.dumps(route.manifest(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (directory / "results-static.json").write_text(
        json.dumps(route.results(needs), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    leaf_count = len(route.results(needs)["nodes"])
    print(f"{route.route}: nodes={len(route.nodes)} leaves={leaf_count} needs={len(needs)}")


if __name__ == "__main__":
    level, level_needs = build_level()
    growth, growth_needs = build_growth()
    write(level, level_needs, LEVEL_DIR)
    write(growth, growth_needs, GROWTH_DIR)
