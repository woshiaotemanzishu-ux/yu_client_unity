import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
STATIC_EVIDENCE = "output/ui_route_audit/2026-08-09_vip_main_revision_v2/static-audit.md"

PRIVILEGE_IDS = {
    1: list(range(1, 11)),
    2: list(range(1, 13)),
    3: list(range(1, 13)),
    4: list(range(1, 16)),
    5: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17],
    6: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17],
    7: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    8: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    9: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    10: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    11: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    12: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    13: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    14: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
    15: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
}

WEEKLY_COUNTS = {level: (2 if level <= 3 else 4) for level in range(1, 16)}
CARD_SIDES = {1: (6, 3), 2: (8, 4), 4: (10, 5)}

CONFIG_EVIDENCE = [
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/server/config_vip_card.json", "sha256": "0f17f7a6da10828bedbceac7336c93c39fb96579db374155cff9c8682058f73b"},
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/server/config_vip_config.json", "sha256": "dfdb45285cfe664c05badcb0c250a5861c2d581be5f8939057596401066fbda6"},
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/client/ClientVipPrivilege.json", "sha256": "fd546ce92c9ec19ab99df36d69761481d56082545060631ef5500cf3015770b2"},
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/server/config_recharge_product.json", "sha256": "e521ad0f7d93b6a38c154c3df49ac970e33e60c73e108db0c32054b07c42d421"},
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/server/config_recharge_return.json", "sha256": "4021f4d6d87ed69308b736e8c7bede8a501700387b1e347d7d8b68b4a423d7f4"},
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/client/ClientRechargeShow.json", "sha256": "403a3d2f3fe59f035d5ede0d7ae9bf80a904410558381e50b7bc0b15b5c81c2f"},
    {"path": "E:/GitProject/yu_client/cdn/assets/resource/config/client/ClientVipWelfare.json", "sha256": "4f5ccb17d2e3877a2271624ca2836a17bf5b51d85c049abacd6082ef4c8d9182"},
    {"path": "Assets/GameRes/resource/config/client/clientvipwelfare.json", "sha256": "4f5ccb17d2e3877a2271624ca2836a17bf5b51d85c049abacd6082ef4c8d9182"},
]


class Route:
    def __init__(self):
        self.nodes = []
        self.child_ids = set()

    def page(self, node_id, parent, controls, note=""):
        inventory = []
        for child, kind in controls:
            inventory.append({"id": child.rsplit(".", 1)[-1], "kind": kind, "child": child})
            self.child_ids.add(child)
        node = {"id": node_id, "type": "page", "risk": "read-only", "parent": parent,
                "control_inventory": inventory}
        if note:
            node["note"] = note
        self.nodes.append(node)

    def leaf(self, node_id, parent, note, node_type="read", risk="read-only"):
        self.nodes.append({"id": node_id, "type": node_type, "risk": risk, "parent": parent, "note": note})

    def result(self):
        parents = {node["parent"] for node in self.nodes if node["parent"] is not None}
        results = []
        for node in self.nodes:
            if node["id"] in parents:
                continue
            node_id = node["id"]
            node_type = node["type"]
            if node_type == "tab":
                gates = ["click", "result", "runtime_state"]
            elif node_type == "navigation":
                gates = ["click", "result", "target_identity", "timing"]
            elif node_type == "return":
                gates = ["click", "return_chain"]
            elif node_type == "transaction":
                gates = ["click", "result", "protocol", "immediate", "reopen"]
            else:
                gates = ["runtime_state"]
            if any(token in node_id for token in ("visual", "duration", "discount", "label", "state", "image")):
                if "visual_match" not in gates:
                    gates.append("visual_match")
            blocked = node_type == "transaction"
            result = {
                "id": node_id,
                "status": "blocked" if blocked else "needs-runtime-verify",
                "applicable_gates": gates,
                "gates": {gate: False for gate in gates},
                "evidence": [STATIC_EVIDENCE],
            }
            if blocked:
                result["blocked_reason"] = (
                    "充值、购买、领取、领奖或状态写入叶仅完成枚举；本轮无账号写事务授权，禁止点击，"
                    "禁止发送 45001/45002/45003/45007/45008/15902 或平台支付。"
                )
            else:
                result["runtime_gap"] = (
                    "静态源码、配置或 Prefab 结构已冻结；仍需同账号真实老 H5 与 Unity Web 顺序复走，"
                    "本轮禁止启动 Unity/浏览器，不能以静态通过冒充真实 Web。"
                )
            results.append(result)
        return {"nodes": results}


def add_popup(r, prefix, parent, title):
    controls = [(prefix + ".identity", "popup-identity"), (prefix + ".content", "popup-content"),
                (prefix + ".close", "close"), (prefix + ".background-return", "background-return")]
    r.page(prefix, parent, controls, title)
    r.leaf(controls[0][0], prefix, title + " View 类型、Activity 层、根尺寸与主底图。")
    r.leaf(controls[1][0], prefix, title + " 文案、图片、滚动内容与条件显隐。")
    r.leaf(controls[2][0], prefix, title + " 关闭按钮只关闭当前弹窗。", "return")
    r.leaf(controls[3][0], prefix, title + " 遮罩关闭且不得穿透父页面。", "return")


def add_reward_cell(r, prefix, parent, note):
    controls = [(prefix + ".visual", "reward-visual"), (prefix + ".detail", "item-detail")]
    r.page(prefix, parent, controls, note)
    r.leaf(prefix + ".visual", prefix, note + "：图标、品质框、绑定数量与可点击面。")
    detail = prefix + ".detail"
    detail_controls = [(detail + ".identity", "detail-view-identity"),
                       (detail + ".main-bg", "main-background"),
                       (detail + ".close", "close"),
                       (detail + ".background-return", "background-return")]
    r.page(detail, prefix, detail_controls, "逐格打开具体物品详情，不能以任意通用弹窗冒充。")
    r.leaf(detail + ".identity", detail, "具体物品与详情 View 类型、根尺寸、数据身份、层级和 cold/warm。", "navigation")
    r.leaf(detail + ".main-bg", detail, "详情主底图 Sprite 已启用且真实绘制，内容容器不重叠。")
    r.leaf(detail + ".close", detail, "关闭按钮只关当前详情并返回原格。", "return")
    r.leaf(detail + ".background-return", detail, "遮罩返回原列表且不穿透父页面。", "return")


def build():
    r = Route()
    root = "mainui.vip.v2"
    root_controls = [(root + ".benefit-tab", "tab"), (root + ".card-tab", "tab"),
                     (root + ".header", "conditional-header"), (root + ".card", "card-page"),
                     (root + ".benefit", "benefit-page"), (root + ".recharge", "recharge-page"),
                     (root + ".vip-hide", "write-toggle"), (root + ".close", "return"),
                     (root + ".sound", "sound")]
    r.page(root, None, root_controls, "VIP 主页面 revision-v2；默认福利页，关闭后下次仍回福利页。")
    r.leaf(root + ".benefit-tab", root, "默认选中特权福利页；关闭重开仍为福利页。", "tab")
    r.leaf(root + ".card-tab", root, "切换至特权卡页并刷新卡型。", "tab")
    r.leaf(root + ".vip-hide", root, "VIP 显示开关写入 45008，仅枚举不点击。", "transaction", "destructive-write")
    r.leaf(root + ".close", root, "关闭 VIP，释放订阅并重置默认福利页。", "return")
    r.leaf(root + ".sound", root, "打开、页签、按钮及弹窗声音按老端调用时机核对。")

    header = root + ".header"
    header_controls = [(header + ".level-exp", "state"), (header + ".day-exp", "conditional-text"),
                       (header + ".active-card", "state"), (header + ".tab-visibility", "conditional-visibility")]
    r.page(header, root, header_controls)
    r.leaf(header + ".level-exp", header, "VIP 等级、经验、满级状态与进度条。")
    r.leaf(header + ".day-exp", header, "type4 激活显示每日登录+5点经验；无激活显示购买提示；其他激活为空。")
    r.leaf(header + ".active-card", header, "仅 IsActive==1 且 Time==0 或未过期，取最大 CardType。")
    r.leaf(header + ".tab-visibility", header, "卡页隐藏 recharge/vip/day-exp/exp/Image4/cost左右/Image2/Image3/highlight；福利页显示。")

    card = root + ".card"
    card_controls = [(card + ".selector", "card-selector"), (card + ".types", "card-types"),
                     (card + ".rule", "rule-popup"), (card + ".free-tips", "free-popup"),
                     (card + ".expired-tips", "expired-popup")]
    r.page(card, root, card_controls)
    r.leaf(card + ".selector", card, "卡型 1/2/4 默认 type4；选择态和当前数据同步。", "tab")
    types = card + ".types"
    r.page(types, card, [(types + f".type{t}", "card") for t in (1, 2, 4)])
    for card_type in (1, 2, 4):
        cp = types + f".type{card_type}"
        left_count, right_count = CARD_SIDES[card_type]
        controls = [(cp + ".visual", "card-visual"), (cp + ".duration", "duration"),
                    (cp + ".discount", "discount-state"), (cp + ".expiry", "expiry-state"),
                    (cp + ".show-tips", "three-tips"), (cp + ".privileges", "privilege-columns"),
                    (cp + ".rule", "rule-navigation"), (cp + ".action", "transaction")]
        r.page(cp, types, controls, f"卡型 {card_type}：{left_count}+{right_count} 条左右特权。")
        r.leaf(cp + ".visual", cp, "卡底、卡名、主图、价格区、免费/付费状态与选中视觉。")
        r.leaf(cp + ".duration", cp, {1: "固定 30天", 2: "固定 90天", 4: "固定 180天"}[card_type])
        r.leaf(cp + ".discount", cp, "折扣图仅按当前权威配置与真实状态显示；未安全接配置不得编造。")
        r.leaf(cp + ".expiry", cp, "永久/到期/剩余时间必须用老端精确状态；无法精确计算时为空。")
        tips = cp + ".show-tips"
        r.page(tips, cp, [(tips + f".tip{i}", "rich-text") for i in range(1, 4)])
        for i in range(1, 4):
            r.leaf(tips + f".tip{i}", tips, f"卡型 {card_type} show_tips[{i - 1}] 独立富文本、颜色与换行。")
        priv = cp + ".privileges"
        side_controls = [(priv + ".left", "left-column"), (priv + ".right", "right-column")]
        r.page(priv, cp, side_controls)
        for side, count in (("left", left_count), ("right", right_count)):
            sp = priv + "." + side
            r.page(sp, priv, [(sp + f".item{i}", "privilege-row") for i in range(1, count + 1)])
            for i in range(1, count + 1):
                r.leaf(sp + f".item{i}", sp, f"卡型 {card_type} {side}_show_tip[{i - 1}] 图标、富文本与顺序。")
        r.leaf(cp + ".rule", cp, "更多/规则按钮打开 VipRuleView。", "navigation")
        protocol = "45007 免费领取" if card_type in (1, 2) else "45003 购买/激活"
        r.leaf(cp + ".action", cp, protocol + "；只枚举不点击，不在 tips 弹窗伪造领取。", "transaction", "destructive-write")

    rule = card + ".rule"
    rule_controls = [(rule + ".identity", "popup-identity"), (rule + ".list", "scroll-list"),
                     (rule + ".close", "close"), (rule + ".background-return", "background-return")]
    r.page(rule, card, rule_controls)
    r.leaf(rule + ".identity", rule, "VipRuleView 根尺寸、主底图、标题与层级。")
    r.leaf(rule + ".list", rule, "规则模板动态列表、顺序、滚动裁剪与末项可达。")
    r.leaf(rule + ".close", rule, "规则关闭返回当前卡型。", "return")
    r.leaf(rule + ".background-return", rule, "规则遮罩关闭且不穿透。", "return")

    free = card + ".free-tips"
    free_controls = [(free + ".identity", "popup-identity"), (free + ".tips", "tip-list"),
                     (free + ".timer", "conditional-state"), (free + ".get-btn-return", "return"),
                     (free + ".close", "return"), (free + ".background-return", "return")]
    r.page(free, card, free_controls, "VipTipsView get_btn 的 45007 已在老端注释；按钮只 Close。")
    r.leaf(free + ".identity", free, "VipTipsView 身份、根尺寸、底图与标题。")
    r.leaf(free + ".tips", free, "vip_tips 动态模板、图标、富文本、hide/new 状态。")
    r.leaf(free + ".timer", free, "老端当前计时调用注释；不得伪造倒计时或自动领取。")
    r.leaf(free + ".get-btn-return", free, "get_btn 仅 Close；免费领取只属于 card.action。", "return")
    r.leaf(free + ".close", free, "关闭按钮返回卡页。", "return")
    r.leaf(free + ".background-return", free, "遮罩关闭且不穿透。", "return")
    add_popup(r, card + ".expired-tips", card, "VipInvalidView 过期提示")

    benefit = root + ".benefit"
    benefit_controls = [(benefit + ".level-selector", "level-selector"), (benefit + ".levels", "15-level-pages"),
                        (benefit + ".exclusive-confirm", "confirm-popup"), (benefit + ".weekly-popup", "weekly-popup")]
    r.page(benefit, root, benefit_controls)
    r.leaf(benefit + ".level-selector", benefit, "当前/上一/下一 VIP 等级与左右红点、边界和切页回顶。", "tab")
    levels = benefit + ".levels"
    r.page(levels, benefit, [(levels + f".lv{level}", "level-page") for level in range(1, 16)])
    for level in range(1, 16):
        lp = levels + f".lv{level}"
        controls = [(lp + ".visual", "level-visual"), (lp + ".privileges", "privilege-list"),
                    (lp + ".exclusive-rewards", "four-reward-cells"), (lp + ".exclusive-action", "transaction"),
                    (lp + ".weekly-rewards", "weekly-reward-cells"), (lp + ".weekly-action", "transaction")]
        r.page(lp, levels, controls, f"VIP{level}：{len(PRIVILEGE_IDS[level])} 条特权、4 格专享、{WEEKLY_COUNTS[level]} 格周礼包。")
        r.leaf(lp + ".visual", lp, f"VIP{level} 标题、特效/图片配置、选中状态与首屏 ready。")
        pp = lp + ".privileges"
        r.page(pp, lp, [(pp + f".privilege{pid}", "privilege-row") for pid in PRIVILEGE_IDS[level]])
        for pid in PRIVILEGE_IDS[level]:
            r.leaf(pp + f".privilege{pid}", pp, f"VIP{level} ClientVipPrivilege[{pid}] 富文本、new/hide 与顺序。")
        ep = lp + ".exclusive-rewards"
        r.page(ep, lp, [(ep + f".cell{i}", "reward-cell") for i in range(1, 5)])
        for i in range(1, 5):
            add_reward_cell(r, ep + f".cell{i}", ep,
                            f"VIP{level} config_vip_config rewards[{i - 1}]")
        r.leaf(lp + ".exclusive-action", lp, f"VIP{level} 45001 专享奖励领取；仅枚举不点击。", "transaction", "destructive-write")
        wp = lp + ".weekly-rewards"
        r.page(wp, lp, [(wp + f".cell{i}", "reward-cell") for i in range(1, WEEKLY_COUNTS[level] + 1)])
        for i in range(1, WEEKLY_COUNTS[level] + 1):
            add_reward_cell(r, wp + f".cell{i}", wp,
                            f"VIP{level} config_vip_config week_reward_list[{i - 1}]")
        r.leaf(lp + ".weekly-action", lp, f"VIP{level} 周礼包购买/领取 45002；仅枚举不点击。", "transaction", "destructive-write")

    exclusive = benefit + ".exclusive-confirm"
    exclusive_controls = [(exclusive + ".identity", "popup-identity"), (exclusive + ".rewards", "reward-list"),
                          (exclusive + ".confirm", "transaction"), (exclusive + ".cancel", "return"),
                          (exclusive + ".close", "return"), (exclusive + ".background-return", "return")]
    r.page(exclusive, benefit, exclusive_controls)
    r.leaf(exclusive + ".identity", exclusive, "专享奖励确认弹窗身份、标题、主底图与当前 VIP 级。")
    r.leaf(exclusive + ".rewards", exclusive, "当前等级四格奖励与详情。")
    r.leaf(exclusive + ".confirm", exclusive, "确认发送 45001；仅枚举不点击。", "transaction", "destructive-write")
    for suffix in ("cancel", "close", "background-return"):
        r.leaf(exclusive + "." + suffix, exclusive, "取消/关闭只回福利页且不穿透。", "return")
    weekly = benefit + ".weekly-popup"
    weekly_controls = [(weekly + ".identity", "popup-identity"), (weekly + ".cost", "cost-state"),
                       (weekly + ".rewards", "reward-list"), (weekly + ".confirm", "transaction"),
                       (weekly + ".cancel", "return"), (weekly + ".close", "return"),
                       (weekly + ".background-return", "return")]
    r.page(weekly, benefit, weekly_controls)
    r.leaf(weekly + ".identity", weekly, "周礼包弹窗身份、标题、主底图与当前 VIP 级。")
    r.leaf(weekly + ".cost", weekly, "week_cost 与货币图标；充足/不足状态。")
    r.leaf(weekly + ".rewards", weekly, "当前级周礼包动态奖励格与详情。")
    r.leaf(weekly + ".confirm", weekly, "确认发送 45002；仅枚举不点击。", "transaction", "destructive-write")
    for suffix in ("cancel", "close", "background-return"):
        r.leaf(weekly + "." + suffix, weekly, "取消/关闭只回福利页且不穿透。", "return")

    recharge = root + ".recharge"
    recharge_controls = [(recharge + ".header", "header"), (recharge + ".snapshot", "ordered-15800"),
                         (recharge + ".candidates", "14-candidates"), (recharge + ".dynamic15901", "dynamic-template"),
                         (recharge + ".activity-show", "activity-popup"), (recharge + ".scroll-to-8", "scroll"),
                         (recharge + ".more", "platform-payment"), (recharge + ".return", "return"),
                         (recharge + ".close", "return"), (recharge + ".sound", "sound")]
    r.page(recharge, root, recharge_controls)
    rh = recharge + ".header"
    r.page(rh, recharge, [(rh + ".level-exp", "state"), (rh + ".day-exp", "conditional-text"),
                          (rh + ".max-level", "conditional-visibility")])
    r.leaf(rh + ".level-exp", rh, "VIP 等级/经验/进度。")
    r.leaf(rh + ".day-exp", rh, "仅 type4 有效卡显示每日登录+5点经验；否则为空，满级隐藏。")
    r.leaf(rh + ".max-level", rh, "满级隐藏 cost_left/cost_right/diamond/day_exp/exp，高亮满且显示满级。")
    snap = recharge + ".snapshot"
    r.page(snap, recharge, [(snap + ".wire-order-duplicates", "ordered-list"),
                            (snap + ".single-update", "15801-update"),
                            (snap + ".success-notice", "15802-state"),
                            (snap + ".total-gold", "15803-state")])
    r.leaf(snap + ".wire-order-duplicates", snap, "15800 保留 wire 顺序与重复 product_id；字典仅兼容查找。")
    r.leaf(snap + ".single-update", snap, "15801 更新有序快照全部匹配项；不存在不得插入。")
    r.leaf(snap + ".success-notice", snap, "15802 充值成功通知只刷新既有状态。")
    r.leaf(snap + ".total-gold", snap, "15803 累计充值数值刷新。")
    candidates = recharge + ".candidates"
    candidate_ids = [(ptype, pid) for ptype in (1, 2) for pid in range(2, 9)]
    r.page(candidates, recharge, [(candidates + f".type{ptype}-product{pid}", "candidate") for ptype, pid in candidate_ids],
           "14 个 type1/type2 候选；显示集合必须与 15800 有序快照取交集。")
    for ptype, pid in candidate_ids:
        cp = candidates + f".type{ptype}-product{pid}"
        controls = [(cp + ".config", "config"), (cp + ".visible-intersection", "conditional-state"),
                    (cp + ".return-state", "bonus-state"), (cp + ".pay", "transaction"),
                    (cp + ".detail", "navigation")]
        r.page(cp, candidates, controls, f"product_type={ptype}, product_id={pid} 候选；不伪造当前是否下发。")
        r.leaf(cp + ".config", cp, "价格、商品名、图标、数量与 product_type 来自当前配置。")
        r.leaf(cp + ".visible-intersection", cp, "仅当 15800 快照存在匹配项才显示；保持 wire 顺序与重复。")
        r.leaf(cp + ".return-state", cp, "首充/返利状态来自 recharge_return 与 15800 return_type。")
        r.leaf(cp + ".pay", cp, "平台支付/购买；仅枚举不点击。", "transaction", "destructive-write")
        r.leaf(cp + ".detail", cp, "商品奖励区打开 ActivityRechargeShow。", "navigation")

    dynamic = recharge + ".dynamic15901"
    dynamic_controls = [(dynamic + ".request-template", "15901-read"), (dynamic + ".reward-cell-template", "reward-cell-template"),
                        (dynamic + ".state0", "state"),
                        (dynamic + ".state1", "state"), (dynamic + ".state2", "state"),
                        (dynamic + ".left-count", "conditional-text"), (dynamic + ".pay", "transaction"),
                        (dynamic + ".claim", "transaction"), (dynamic + ".sold-out", "state"),
                        (dynamic + ".detail", "navigation"), (dynamic + ".detail-return", "return")]
    r.page(dynamic, recharge, dynamic_controls, "15901 仅冻结动态模板和状态机，不伪造 product_id/left_count 数量。")
    r.leaf(dynamic + ".request-template", dynamic, "type2 商品按真实下发 product_id 请求/绑定 15901，只读模板待运行态。")
    add_reward_cell(r, dynamic + ".reward-cell-template", dynamic,
                    "15901 商品奖励格动态模板；运行时数量按实际商品配置展开，不静态伪造")
    r.leaf(dynamic + ".state0", dynamic, "state=0：支付可用，领取按钮隐藏。")
    r.leaf(dynamic + ".state1", dynamic, "state=1：支付禁用，领取按钮显示。")
    r.leaf(dynamic + ".state2", dynamic, "state=2：支付与领取均隐藏。")
    r.leaf(dynamic + ".left-count", dynamic, "left_count>0 显示剩余N天；否则显示已全部领取；N 不静态伪造。")
    r.leaf(dynamic + ".pay", dynamic, "state=0 平台支付；仅枚举不点击。", "transaction", "destructive-write")
    r.leaf(dynamic + ".claim", dynamic, "state=1 发送 15902；仅枚举不点击。", "transaction", "destructive-write")
    r.leaf(dynamic + ".sold-out", dynamic, "state=2/left_count=0 的领完视觉与禁用态。")
    r.leaf(dynamic + ".detail", dynamic, "奖励区打开 ActivityRechargeShow。", "navigation")
    r.leaf(dynamic + ".detail-return", dynamic, "详情关闭返回同一商品和滚动位置。", "return")

    activity = recharge + ".activity-show"
    activity_controls = [(activity + ".visibility", "multi-condition"), (activity + ".identity", "popup-identity"),
                         (activity + ".mode", "regular-fallback"), (activity + ".list", "dynamic-list"),
                         (activity + ".row-template", "row-template"), (activity + ".pay", "transaction"),
                         (activity + ".close", "return"), (activity + ".background-return", "return"),
                         (activity + ".sound", "sound")]
    r.page(activity, recharge, activity_controls)
    r.leaf(activity + ".visibility", activity, "ClientRechargeShow product_ids、open_lv、CustomActivity 充值列表与最高档 fallback 9999 多条件链。")
    r.leaf(activity + ".identity", activity, "ActivityRechargeShow Activity 层、根尺寸、底图、标题、金额与字体。")
    r.leaf(activity + ".mode", activity, "真实 product 模式与 product_id=9999 fallback 模式，不能互相冒充。")
    alist = activity + ".list"
    r.page(alist, activity, [(alist + ".structure", "scroll-structure"), (alist + ".dynamic-count", "dynamic-count"),
                             (alist + ".last-spacer", "fake-spacer")])
    r.leaf(alist + ".structure", alist, "ScrollRect→Viewport/Mask→Content/Layout，拖动裁剪和末项可达。")
    r.leaf(alist + ".dynamic-count", alist, "活动阶段动态行数、顺序与高度；不得静态伪造。")
    r.leaf(alist + ".last-spacer", alist, "末尾 fake item 仅作间距，不显示真实活动内容。")
    row = activity + ".row-template"
    row_controls = [(row + ".real-state", "real-row"), (row + ".fake-state", "fake-row"),
                    (row + ".rewards", "horizontal-reward-list"),
                    (row + ".jump", "navigation"), (row + ".jump-return", "return")]
    r.page(row, activity, row_controls)
    r.leaf(row + ".real-state", row, "真实活动行标题、进度、条件、按钮与已完成状态。")
    r.leaf(row + ".fake-state", row, "fake spacer 行关闭业务内容，只保留布局间距。")
    rewards = row + ".rewards"
    r.page(rewards, row, [(rewards + ".cell-template", "dynamic-reward-cell")],
           "ActivityRechargeShowItem 动态横向奖励格；真实数量由活动数据决定。")
    add_reward_cell(r, rewards + ".cell-template", rewards,
                    "ActivityRechargeShow 动态奖励格模板；每个运行时克隆均须复走本详情链")
    r.leaf(row + ".jump", row, "按钮按 base_type/show_id 跳 CustomActivity 或 OpenFun。", "navigation")
    r.leaf(row + ".jump-return", row, "目标页关闭后返回链与原充值页状态。", "return")
    r.leaf(activity + ".pay", activity, "平台支付；仅枚举不点击。", "transaction", "destructive-write")
    r.leaf(activity + ".close", activity, "关闭按钮返回 RechargeView。", "return")
    r.leaf(activity + ".background-return", activity, "遮罩关闭且不穿透 RechargeView。", "return")
    r.leaf(activity + ".sound", activity, "弹窗打开、关闭、跳转和支付按钮声音。")
    r.leaf(recharge + ".scroll-to-8", recharge, "老端 down_img 精确 scrollTo(8)；当前仅到底部近似，必须运行态核对，不扩大实现。")
    r.leaf(recharge + ".more", recharge, "更多充值/平台支付入口；仅枚举不点击。", "transaction", "destructive-write")
    r.leaf(recharge + ".return", recharge, "recharge_btn 返回 VIP，并按关闭生命周期使下次默认福利页。", "return")
    r.leaf(recharge + ".close", recharge, "关闭 RechargeView，释放订阅。", "return")
    r.leaf(recharge + ".sound", recharge, "打开、滚动、返回、详情与按钮声音。")

    assert sum(len(v) for v in PRIVILEGE_IDS.values()) == 216
    assert 15 * 4 == 60
    assert sum(WEEKLY_COUNTS.values()) == 54
    assert sum(sum(v) for v in CARD_SIDES.values()) == 36
    assert len(candidate_ids) == 14
    ids = [node["id"] for node in r.nodes]
    assert len(ids) == len(set(ids)), "duplicate node id"
    known = set(ids)
    for node in r.nodes:
        if node["parent"] is not None:
            assert node["parent"] in known, (node["id"], node["parent"])
        if node["type"] == "page":
            for item in node["control_inventory"]:
                assert item["child"] in known, (node["id"], item["child"])

    baseline = {
        "manifest_revision": 2,
        "supersedes": "output/ui_route_audit/2026-08-09_vip_main/route-ledger.json",
        "authority": "当前老 H5 同账号同状态同 viewport 的真实玩家表现；静态证据不替代真实 Web。",
        "frozen_inventory": {
            "card_types": [1, 2, 4], "card_show_tips_each": 3,
            "card_privilege_counts": {"1": 9, "2": 12, "4": 15},
            "benefit_levels": 15, "benefit_privilege_rows": 216,
            "exclusive_reward_cells": 60, "weekly_reward_cells": 54,
            "recharge_type1_type2_candidates": 14,
            "recharge_visibility": "candidate intersects ordered 15800 snapshot",
            "dynamic_15901_states": [0, 1, 2],
        },
        "protocol_inventory": {
            "read_or_push": ["15800", "15801", "15802", "15803", "15901", "45004", "45005", "45006"],
            "blocked_writes": ["45001", "45002", "45003", "45007", "45008", "15902", "platform-payment"],
        },
        "config_evidence": CONFIG_EVIDENCE,
        "legacy_sources": [
            "E:/GitProject/yu_client/h5/src/vip/VipView.ts",
            "E:/GitProject/yu_client/h5/src/vip/VipCardItem.ts",
            "E:/GitProject/yu_client/h5/src/vip/VipTipsView.ts",
            "E:/GitProject/yu_client/h5/src/vip/VipRuleView.ts",
            "E:/GitProject/yu_client/h5/src/vip/VipInvalidView.ts",
            "E:/GitProject/yu_client/h5/src/vip/VipPrivilegeShowView.ts",
            "E:/GitProject/yu_client/h5/src/vip/RechargeView.ts",
            "E:/GitProject/yu_client/h5/src/vip/RechargeItem.ts",
            "E:/GitProject/yu_client/h5/src/activityRechargeShow/ActivityRechargeShow.ts",
            "E:/GitProject/yu_client/h5/src/activityRechargeShow/ActivityRechargeShowItem.ts",
        ],
        "unity_sources": [
            "Assets/Prefabs/UI/Vip/VipModule.prefab",
            "Assets/Scripts/Module/Core/Vip/VipModel.cs",
            "Assets/Scripts/Module/Core/Vip/VipController.cs",
            "Assets/Scripts/Module/Core/Vip/VipFlow.cs",
            "Assets/Scripts/Module/Core/Vip/Views/VipBaseView.cs",
            "Assets/Scripts/Module/Core/Vip/Views/RechargeView.cs",
        ],
        "component_dependencies": [
            {"component": "BaseView/BaseWindow lifecycle", "scope": "read-only outside island"},
            {"component": "EquipmentItem/Common item detail", "scope": "read-only outside island"},
            {"component": "ActivityRechargeShow", "scope": "old-client popup chain; Unity route absent/blocked"},
            {"component": "platform payment SDK", "scope": "transaction blocked"},
            {"component": "UIEffect/model presentation", "scope": "runtime-only verification"},
        ],
    }
    manifest = {"route": root, "baseline": baseline, "nodes": r.nodes}
    (OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (OUT / "results-static.json").write_text(json.dumps(r.result(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    leaves = len(r.result()["nodes"])
    print(json.dumps({"nodes": len(r.nodes), "leaves": leaves, "pages": len(r.nodes) - leaves}, ensure_ascii=False))


if __name__ == "__main__":
    build()
