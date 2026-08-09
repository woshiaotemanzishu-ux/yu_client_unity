#!/usr/bin/env python3
"""Generate the immutable Market route manifest and compact static results.

This route-local helper never writes route-ledger.json.  The formal ledger is
created and updated only by audit-game-ui-route/scripts/route_ledger.py.
"""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
ROUTE = "mainui.market"
EVIDENCE = "output/ui_route_audit/2026-08-09_market/static-audit.md"

CATEGORIES = [
    (1, "男性装备"),
    (5, "女性装备"),
    (6, "饰品装备"),
    (4, "圣印装备"),
    (2, "共鸣材料"),
    (3, "其他材料"),
    (7, "神契装备"),
    (8, "启示圣铠"),
]

SUBTYPES = {
    1: [(0, "全部"), (1, "武器"), (2, "铠甲"), (3, "头盔"), (4, "裤子"), (5, "护腕"), (6, "鞋子")],
    2: [(0, "全部"), (1, "共鸣石(男)"), (2, "共鸣石(女)"), (3, "战魂共鸣石"), (4, "饰品共鸣石"), (5, "万物共鸣石"), (6, "神之源核"), (7, "灵之源核")],
    3: [(0, "全部"), (1, "垂神翼影幻化"), (3, "神兵幻化"), (5, "古法符相幻化"), (6, "经验符"), (8, "神巫幻化"), (9, "地境晋升"), (10, "图谱"), (11, "剑魄同修碎片"), (12, "兑换材料"), (13, "道具碎片"), (14, "妖灵升级"), (15, "妖灵碎片"), (16, "妖灵技能书")],
    4: [(0, "全部"), (1, "足部"), (2, "头盔"), (3, "护腿"), (4, "护腕"), (5, "铠甲"), (6, "武器"), (7, "晶核"), (8, "脸具"), (9, "项链"), (10, "戒指"), (11, "圣器")],
    5: [(0, "全部"), (1, "武器"), (2, "衣服"), (3, "发饰"), (4, "裤裙"), (5, "护手"), (6, "鞋子")],
    6: [(0, "全部"), (1, "项链"), (2, "护符"), (3, "手镯"), (4, "戒指")],
    7: [(0, "全部"), (1, "武器"), (2, "头盔"), (3, "铠甲"), (4, "饰品")],
    8: [(0, "全部"), (1, "武器"), (2, "头盔"), (3, "项链"), (4, "上衣"), (5, "护符"), (6, "下装"), (7, "手镯"), (8, "护手"), (9, "戒指"), (10, "鞋子")],
}

QUALITY = [(99, "品质"), (3, "紫色"), (4, "橙色"), (5, "红色"), (6, "暗金"), (7, "粉色")]
STAGE = [(99, "阶数")] + [(i, f"{i}阶") for i in range(1, 17)]
STAR = [(99, "星级"), (0, "0星"), (1, "1星"), (2, "2星"), (3, "3星"), (4, "4星")]

nodes: list[dict] = []


def leaf(node_id: str, parent: str, node_type: str = "read", risk: str = "read-only", note: str = "") -> None:
    node = {"id": node_id, "type": node_type, "risk": risk, "parent": parent}
    if note:
        node["note"] = note
    nodes.append(node)


def page(node_id: str, parent: str | None, controls: list[tuple[str, str, str]], note: str = "") -> None:
    node = {
        "id": node_id,
        "type": "page",
        "risk": "read-only",
        "parent": parent,
        "control_inventory": [
            {"id": control_id, "kind": kind, "child": child}
            for control_id, kind, child in controls
        ],
    }
    if note:
        node["note"] = note
    nodes.append(node)


def add_option_page(node_id: str, parent: str, values: list[tuple[int, str]], prefix: str, note: str) -> None:
    controls = []
    for value, label in values:
        child = f"{node_id}.{prefix}-{value}"
        controls.append((f"{prefix}-{value}", "option", child))
    page(node_id, parent, controls, note)
    for value, label in values:
        leaf(f"{node_id}.{prefix}-{value}", node_id, "navigation", note=f"{label}; cond={value}.")


page(
    ROUTE,
    None,
    [
        ("entry", "entry", f"{ROUTE}.entry"),
        ("shell", "window-shell", f"{ROUTE}.shell"),
        ("buy", "tab-page", f"{ROUTE}.buy"),
        ("plz", "tab-page", f"{ROUTE}.plz"),
        ("sell", "tab-page", f"{ROUTE}.sell"),
        ("legacy", "conditional-routes", f"{ROUTE}.legacy"),
        ("shared-item", "shared-component", f"{ROUTE}.shared-item"),
        ("protocols", "data-contract", f"{ROUTE}.protocols"),
        ("lifecycle", "route-lifecycle", f"{ROUTE}.lifecycle"),
    ],
    "当前老 H5 市场完整静态拓扑；真实玩家运行事实仍待采集。",
)

page(
    f"{ROUTE}.entry",
    ROUTE,
    [
        ("icon-local", "conditional-icon", f"{ROUTE}.entry.icon-local"),
        ("icon-kf", "conditional-icon", f"{ROUTE}.entry.icon-kf"),
        ("level-gate", "condition", f"{ROUTE}.entry.level-gate"),
        ("kf-time", "condition", f"{ROUTE}.entry.kf-time"),
        ("open", "navigation", f"{ROUTE}.entry.open"),
    ],
)
leaf(f"{ROUTE}.entry.icon-local", f"{ROUTE}.entry", note="未到跨服开放时间显示图标 151。")
leaf(f"{ROUTE}.entry.icon-kf", f"{ROUTE}.entry", note="到达 open_time 后显示图标 151@1，并删除 151。")
leaf(f"{ROUTE}.entry.level-gate", f"{ROUTE}.entry", note="服务端 open_lv=90；90 级以下 151xx 静默无回包。")
leaf(f"{ROUTE}.entry.kf-time", f"{ROUTE}.entry", note="15121 open_time、跨天和等级变化刷新入口图标。")
leaf(f"{ROUTE}.entry.open", f"{ROUTE}.entry", "navigation", note="真实图标点击应打开 MarketBaseView；Unity 页面路由缺失。")

page(
    f"{ROUTE}.shell",
    ROUTE,
    [
        ("title", "conditional-title", f"{ROUTE}.shell.title"),
        ("background", "visual", f"{ROUTE}.shell.background"),
        ("currency", "state", f"{ROUTE}.shell.currency"),
        ("tab-buy", "tab", f"{ROUTE}.shell.tab-buy"),
        ("tab-plz", "tab", f"{ROUTE}.shell.tab-plz"),
        ("tab-sell", "tab", f"{ROUTE}.shell.tab-sell"),
        ("tab-auction", "hidden-tab", f"{ROUTE}.shell.tab-auction"),
        ("tab-record", "hidden-tab", f"{ROUTE}.shell.tab-record"),
        ("close", "return", f"{ROUTE}.shell.close"),
    ],
)
leaf(f"{ROUTE}.shell.title", f"{ROUTE}.shell", note="本服 market.uisc_001；跨服 market.uisc_001_kf。")
leaf(f"{ROUTE}.shell.background", f"{ROUTE}.shell", note="三个现行页签均使用 ui_bg_1.jpg。")
leaf(f"{ROUTE}.shell.currency", f"{ROUTE}.shell", note="外窗 money_list=[2,1,0]。")
leaf(f"{ROUTE}.shell.tab-buy", f"{ROUTE}.shell", "tab", note="购买，现行 index=0。")
leaf(f"{ROUTE}.shell.tab-plz", f"{ROUTE}.shell", "tab", note="求购，现行 index=1。")
leaf(f"{ROUTE}.shell.tab-sell", f"{ROUTE}.shell", "tab", note="出售，现行 index=2。")
leaf(f"{ROUTE}.shell.tab-auction", f"{ROUTE}.shell", note="拍卖代码/页签已注释，当前运行态不应出现。")
leaf(f"{ROUTE}.shell.tab-record", f"{ROUTE}.shell", note="记录页签已从 viewClassList/tabStrList 注释移除。")
leaf(f"{ROUTE}.shell.close", f"{ROUTE}.shell", "return", note="关闭 MarketBaseView 并返回主界面。")

# Purchase route.
page(
    f"{ROUTE}.buy",
    ROUTE,
    [
        ("categories", "category-list", f"{ROUTE}.buy.categories"),
        ("subtypes", "subtype-list", f"{ROUTE}.buy.subtypes"),
        ("filters", "filters", f"{ROUTE}.buy.filters"),
        ("list-state", "item-list", f"{ROUTE}.buy.list-state"),
        ("item", "shared-item", f"{ROUTE}.buy.item"),
    ],
)
cat_controls = []
for category_id, label in CATEGORIES:
    child = f"{ROUTE}.buy.categories.type-{category_id}"
    cat_controls.append((f"type-{category_id}", "category", child))
page(f"{ROUTE}.buy.categories", f"{ROUTE}.buy", cat_controls, "ConfigMarket.type_radio_cfg 的 8 个横向大类。")
for category_id, label in CATEGORIES:
    leaf(f"{ROUTE}.buy.categories.type-{category_id}", f"{ROUTE}.buy.categories", "navigation", note=f"{label}; id={category_id}。")

subtype_controls = []
for category_id, _ in CATEGORIES:
    for subtype_id, label in SUBTYPES[category_id]:
        child = f"{ROUTE}.buy.subtypes.t{category_id}-s{subtype_id}"
        subtype_controls.append((f"t{category_id}-s{subtype_id}", "list-cell", child))
page(f"{ROUTE}.buy.subtypes", f"{ROUTE}.buy", subtype_controls, "config_goods_sell_subtype 当前 69 个购买二级格，含各类‘全部’。")
for category_id, _ in CATEGORIES:
    for subtype_id, label in SUBTYPES[category_id]:
        leaf(f"{ROUTE}.buy.subtypes.t{category_id}-s{subtype_id}", f"{ROUTE}.buy.subtypes", "navigation", note=f"type={category_id}, subtype={subtype_id}, {label}；显示挂单数量并进入商品列表。")

page(
    f"{ROUTE}.buy.filters",
    f"{ROUTE}.buy",
    [
        ("quality", "dropdown", f"{ROUTE}.buy.filters.quality"),
        ("stage", "dropdown", f"{ROUTE}.buy.filters.stage"),
        ("star", "dropdown", f"{ROUTE}.buy.filters.star"),
    ],
)
add_option_page(f"{ROUTE}.buy.filters.quality", f"{ROUTE}.buy.filters", QUALITY, "cond", "品质下拉 6 项。")
add_option_page(f"{ROUTE}.buy.filters.stage", f"{ROUTE}.buy.filters", STAGE, "cond", "阶数下拉 17 项。")
add_option_page(f"{ROUTE}.buy.filters.star", f"{ROUTE}.buy.filters", STAR, "cond", "星级下拉 6 项。")

page(
    f"{ROUTE}.buy.list-state",
    f"{ROUTE}.buy",
    [
        ("structure", "scroll", f"{ROUTE}.buy.list-state.structure"),
        ("scroll", "interaction", f"{ROUTE}.buy.list-state.scroll"),
        ("empty", "state", f"{ROUTE}.buy.list-state.empty"),
        ("kf-tip", "conditional-text", f"{ROUTE}.buy.list-state.kf-tip"),
        ("price-sort", "sort", f"{ROUTE}.buy.list-state.price-sort"),
        ("buy-times", "state", f"{ROUTE}.buy.list-state.buy-times"),
    ],
)
leaf(f"{ROUTE}.buy.list-state.structure", f"{ROUTE}.buy.list-state", note="分类/商品两级动态列表、Viewport/Mask/Content 身份。")
leaf(f"{ROUTE}.buy.list-state.scroll", f"{ROUTE}.buy.list-state", note="真实拖动、裁剪、末项可达、切换分类回顶。")
leaf(f"{ROUTE}.buy.list-state.empty", f"{ROUTE}.buy.list-state", note="无商品显示 _group_empty；有商品显示 _group_title。")
leaf(f"{ROUTE}.buy.list-state.kf-tip", f"{ROUTE}.buy.list-state", note="跨服未开且 dayCount>0 时显示 N天后开启跨服交易。")
leaf(f"{ROUTE}.buy.list-state.price-sort", f"{ROUTE}.buy.list-state", note="价格标题点击在升/降序间切换并旋转箭头。")
leaf(f"{ROUTE}.buy.list-state.buy-times", f"{ROUTE}.buy.list-state", note="15114 times_list[0] 显示购买次数/上限。")

page(
    f"{ROUTE}.buy.item",
    f"{ROUTE}.buy",
    [
        ("identity", "item-template", f"{ROUTE}.buy.item.identity"),
        ("detail", "popup", f"{ROUTE}.buy.item.detail"),
        ("name-stage", "text", f"{ROUTE}.buy.item.name-stage"),
        ("wear-state", "conditional-state", f"{ROUTE}.buy.item.wear-state"),
        ("seller", "conditional-text", f"{ROUTE}.buy.item.seller"),
        ("buy", "transaction", f"{ROUTE}.buy.item.buy"),
        ("sell", "conditional-transaction", f"{ROUTE}.buy.item.sell"),
        ("repeal", "conditional-transaction", f"{ROUTE}.buy.item.repeal"),
    ],
)
leaf(f"{ROUTE}.buy.item.identity", f"{ROUTE}.buy.item", note="MarketShowItem + EquipmentItem；列表每个可见格均需独立核对。")
leaf(f"{ROUTE}.buy.item.detail", f"{ROUTE}.buy.item", "navigation", note="按物品类型打开装备/圣印/神装/启示/龙狼/时装/普通详情。")
leaf(f"{ROUTE}.buy.item.name-stage", f"{ROUTE}.buy.item", note="名称、数量、阶数、单价。")
leaf(f"{ROUTE}.buy.item.wear-state", f"{ROUTE}.buy.item", note="装备 up/down/ban 条件图。")
leaf(f"{ROUTE}.buy.item.seller", f"{ROUTE}.buy.item", note="指定交易 role_name 条件文本；当前 P2P 主链已死。")
leaf(f"{ROUTE}.buy.item.buy", f"{ROUTE}.buy.item", "transaction", "destructive-write", "15111 购买并核对成功/失败、次数刷新、当前列表和重开。")
leaf(f"{ROUTE}.buy.item.sell", f"{ROUTE}.buy.item", "transaction", "destructive-write", "role_name 且非本人时直接 15117 出售给求购单。")
leaf(f"{ROUTE}.buy.item.repeal", f"{ROUTE}.buy.item", "transaction", "destructive-write", "本人挂单显示撤销并发送 15108。")

# Seek purchase route.
page(
    f"{ROUTE}.plz",
    ROUTE,
    [
        ("tabs", "sub-tabs", f"{ROUTE}.plz.tabs"),
        ("list", "list-page", f"{ROUTE}.plz.list"),
        ("create", "create-page", f"{ROUTE}.plz.create"),
        ("sell-tip", "popup", f"{ROUTE}.plz.sell-tip"),
    ],
)
page(
    f"{ROUTE}.plz.tabs",
    f"{ROUTE}.plz",
    [
        ("list", "tab", f"{ROUTE}.plz.tabs.list"),
        ("create", "tab", f"{ROUTE}.plz.tabs.create"),
    ],
)
leaf(f"{ROUTE}.plz.tabs.list", f"{ROUTE}.plz.tabs", "tab", note="求购列表。")
leaf(f"{ROUTE}.plz.tabs.create", f"{ROUTE}.plz.tabs", "tab", note="我要求购。")

page(
    f"{ROUTE}.plz.list",
    f"{ROUTE}.plz",
    [
        ("scope", "scope-tabs", f"{ROUTE}.plz.list.scope"),
        ("price-sort", "sort", f"{ROUTE}.plz.list.price-sort"),
        ("structure", "scroll", f"{ROUTE}.plz.list.structure"),
        ("scroll", "interaction", f"{ROUTE}.plz.list.scroll"),
        ("empty", "state", f"{ROUTE}.plz.list.empty"),
        ("item", "shared-item", f"{ROUTE}.plz.list.item"),
    ],
)
page(
    f"{ROUTE}.plz.list.scope",
    f"{ROUTE}.plz.list",
    [
        ("all", "radio", f"{ROUTE}.plz.list.scope.all"),
        ("mine", "radio", f"{ROUTE}.plz.list.scope.mine"),
    ],
)
leaf(f"{ROUTE}.plz.list.scope.all", f"{ROUTE}.plz.list.scope", "tab", note="全部：15118 page=1,size=999。")
leaf(f"{ROUTE}.plz.list.scope.mine", f"{ROUTE}.plz.list.scope", "tab", note="我的：15119 空请求。")
leaf(f"{ROUTE}.plz.list.price-sort", f"{ROUTE}.plz.list", note="求购单价升/降序。")
leaf(f"{ROUTE}.plz.list.structure", f"{ROUTE}.plz.list", note="MarketPlzShowItem 纵向列表、Viewport/Mask/Content。")
leaf(f"{ROUTE}.plz.list.scroll", f"{ROUTE}.plz.list", note="真实拖动、裁剪与末项可达。")
leaf(f"{ROUTE}.plz.list.empty", f"{ROUTE}.plz.list", note="空/有数据互斥 _group_empty/_group_title。")
page(
    f"{ROUTE}.plz.list.item",
    f"{ROUTE}.plz.list",
    [
        ("identity", "item-template", f"{ROUTE}.plz.list.item.identity"),
        ("name-stage", "text", f"{ROUTE}.plz.list.item.name-stage"),
        ("player", "conditional-text", f"{ROUTE}.plz.list.item.player"),
        ("sell", "conditional-popup", f"{ROUTE}.plz.list.item.sell"),
        ("repeal", "conditional-transaction", f"{ROUTE}.plz.list.item.repeal"),
    ],
)
leaf(f"{ROUTE}.plz.list.item.identity", f"{ROUTE}.plz.list.item", note="MarketPlzShowItem + EquipmentItem；现有 Unity Prefab 仅 Generated Bind。")
leaf(f"{ROUTE}.plz.list.item.name-stage", f"{ROUTE}.plz.list.item", note="名称、阶数、数量、单价。")
leaf(f"{ROUTE}.plz.list.item.player", f"{ROUTE}.plz.list.item", note="role_name 条件显示；点击玩家菜单代码当前注释为空操作。")
leaf(f"{ROUTE}.plz.list.item.sell", f"{ROUTE}.plz.list.item", "navigation", note="非本人且库存足够时打开 MarketSellTipView；不足提示拥有物品不足。")
leaf(f"{ROUTE}.plz.list.item.repeal", f"{ROUTE}.plz.list.item", "transaction", "destructive-write", "本人求购单撤销，发送 15116。")

page(
    f"{ROUTE}.plz.create",
    f"{ROUTE}.plz",
    [
        ("categories", "category-list", f"{ROUTE}.plz.create.categories"),
        ("subtypes", "subtype-list", f"{ROUTE}.plz.create.subtypes"),
        ("filters", "filters", f"{ROUTE}.plz.create.filters"),
        ("goods", "goods-list", f"{ROUTE}.plz.create.goods"),
        ("price", "price-controls", f"{ROUTE}.plz.create.price"),
        ("count", "count-controls", f"{ROUTE}.plz.create.count"),
        ("summary", "state", f"{ROUTE}.plz.create.summary"),
        ("confirm", "transaction", f"{ROUTE}.plz.create.confirm"),
    ],
)
create_cat_controls = []
for category_id, label in CATEGORIES:
    child = f"{ROUTE}.plz.create.categories.type-{category_id}"
    create_cat_controls.append((f"type-{category_id}", "category", child))
page(f"{ROUTE}.plz.create.categories", f"{ROUTE}.plz.create", create_cat_controls, "我要求购的 8 个横向大类。")
for category_id, label in CATEGORIES:
    leaf(f"{ROUTE}.plz.create.categories.type-{category_id}", f"{ROUTE}.plz.create.categories", "navigation", note=f"{label}; id={category_id}。")

create_subtype_controls = []
for category_id, _ in CATEGORIES:
    for subtype_id, label in SUBTYPES[category_id]:
        if subtype_id == 0:
            continue
        child = f"{ROUTE}.plz.create.subtypes.t{category_id}-s{subtype_id}"
        create_subtype_controls.append((f"t{category_id}-s{subtype_id}", "list-cell", child))
page(f"{ROUTE}.plz.create.subtypes", f"{ROUTE}.plz.create", create_subtype_controls, "GetTypeCfgAll(start_index=2) 当前 61 个可求购二级格，不含‘全部’。")
for category_id, _ in CATEGORIES:
    for subtype_id, label in SUBTYPES[category_id]:
        if subtype_id == 0:
            continue
        leaf(f"{ROUTE}.plz.create.subtypes.t{category_id}-s{subtype_id}", f"{ROUTE}.plz.create.subtypes", "navigation", note=f"type={category_id}, subtype={subtype_id}, {label}；无候选提示‘没有物品可供求购’。")

page(
    f"{ROUTE}.plz.create.filters",
    f"{ROUTE}.plz.create",
    [
        ("quality", "dropdown", f"{ROUTE}.plz.create.filters.quality"),
        ("stage", "dropdown", f"{ROUTE}.plz.create.filters.stage"),
        ("star", "dropdown", f"{ROUTE}.plz.create.filters.star"),
    ],
)
add_option_page(f"{ROUTE}.plz.create.filters.quality", f"{ROUTE}.plz.create.filters", QUALITY, "cond", "品质下拉 6 项。")
add_option_page(f"{ROUTE}.plz.create.filters.stage", f"{ROUTE}.plz.create.filters", STAGE, "cond", "阶数下拉 17 项。")
add_option_page(f"{ROUTE}.plz.create.filters.star", f"{ROUTE}.plz.create.filters", STAR, "cond", "星级下拉 6 项。")
page(
    f"{ROUTE}.plz.create.goods",
    f"{ROUTE}.plz.create",
    [
        ("structure", "scroll", f"{ROUTE}.plz.create.goods.structure"),
        ("scroll", "interaction", f"{ROUTE}.plz.create.goods.scroll"),
        ("select", "list-cell", f"{ROUTE}.plz.create.goods.select"),
        ("empty", "state", f"{ROUTE}.plz.create.goods.empty"),
    ],
)
leaf(f"{ROUTE}.plz.create.goods.structure", f"{ROUTE}.plz.create.goods", note="MarketPlzGoodsItem 候选列表与选中框。")
leaf(f"{ROUTE}.plz.create.goods.scroll", f"{ROUTE}.plz.create.goods", note="真实滚动、裁剪、末项可达。")
leaf(f"{ROUTE}.plz.create.goods.select", f"{ROUTE}.plz.create.goods", "navigation", note="选择 type_id，刷新价格/数量上限。")
leaf(f"{ROUTE}.plz.create.goods.empty", f"{ROUTE}.plz.create.goods", note="无候选隐藏控制区并显示空态。")
page(
    f"{ROUTE}.plz.create.price",
    f"{ROUTE}.plz.create",
    [
        ("fixed", "conditional-state", f"{ROUTE}.plz.create.price.fixed"),
        ("custom", "conditional-input", f"{ROUTE}.plz.create.price.custom"),
        ("minus", "stepper", f"{ROUTE}.plz.create.price.minus"),
        ("plus", "stepper", f"{ROUTE}.plz.create.price.plus"),
        ("calculator", "popup", f"{ROUTE}.plz.create.price.calculator"),
        ("ratio", "state", f"{ROUTE}.plz.create.price.ratio"),
    ],
)
leaf(f"{ROUTE}.plz.create.price.fixed", f"{ROUTE}.plz.create.price", note="固定价显示单位价与相对基准百分比。")
leaf(f"{ROUTE}.plz.create.price.custom", f"{ROUTE}.plz.create.price", note="if_custom_price 分支输入总价并钳制 min/max。")
leaf(f"{ROUTE}.plz.create.price.minus", f"{ROUTE}.plz.create.price", note="-10% 基准单位价；低于最小提示。")
leaf(f"{ROUTE}.plz.create.price.plus", f"{ROUTE}.plz.create.price", note="+10% 基准单位价；高于最大提示。")
leaf(f"{ROUTE}.plz.create.price.calculator", f"{ROUTE}.plz.create.price", "navigation", note="自定义价格打开共享 CalculatorView。")
leaf(f"{ROUTE}.plz.create.price.ratio", f"{ROUTE}.plz.create.price", note="价格比率、总价与 VIP 税额即时变化。")
page(
    f"{ROUTE}.plz.create.count",
    f"{ROUTE}.plz.create",
    [
        ("minus", "stepper", f"{ROUTE}.plz.create.count.minus"),
        ("plus", "stepper", f"{ROUTE}.plz.create.count.plus"),
        ("calculator", "popup", f"{ROUTE}.plz.create.count.calculator"),
        ("limit", "state", f"{ROUTE}.plz.create.count.limit"),
    ],
)
leaf(f"{ROUTE}.plz.create.count.minus", f"{ROUTE}.plz.create.count", note="数量 -1，最小 1。")
leaf(f"{ROUTE}.plz.create.count.plus", f"{ROUTE}.plz.create.count", note="数量 +1，不超过配置上限。")
leaf(f"{ROUTE}.plz.create.count.calculator", f"{ROUTE}.plz.create.count", "navigation", note="数量文本打开共享 CalculatorView。")
leaf(f"{ROUTE}.plz.create.count.limit", f"{ROUTE}.plz.create.count", note="上限<=1 时 +/- 和数量输入禁用并置灰。")
leaf(f"{ROUTE}.plz.create.summary", f"{ROUTE}.plz.create", note="总价、VIP 市场税率/税额、货币图标与充足/不足。")
leaf(f"{ROUTE}.plz.create.confirm", f"{ROUTE}.plz.create", "transaction", "destructive-write", "15115 发起求购，消费货币；验证结果、两份列表即时头插和重开。")

page(
    f"{ROUTE}.plz.sell-tip",
    f"{ROUTE}.plz",
    [
        ("identity", "popup", f"{ROUTE}.plz.sell-tip.identity"),
        ("close", "return", f"{ROUTE}.plz.sell-tip.close"),
        ("item", "shared-item", f"{ROUTE}.plz.sell-tip.item"),
        ("goods", "state", f"{ROUTE}.plz.sell-tip.goods"),
        ("vip-tax", "state", f"{ROUTE}.plz.sell-tip.vip-tax"),
        ("confirm", "transaction", f"{ROUTE}.plz.sell-tip.confirm"),
    ],
)
leaf(f"{ROUTE}.plz.sell-tip.identity", f"{ROUTE}.plz.sell-tip", note="MarketSellTipView，UI 层背景遮罩、居中、点击背景关闭。")
leaf(f"{ROUTE}.plz.sell-tip.close", f"{ROUTE}.plz.sell-tip", "return", note="关闭仅收起当前出售确认弹窗。")
leaf(f"{ROUTE}.plz.sell-tip.item", f"{ROUTE}.plz.sell-tip", note="EquipmentItem 及目标物品。")
leaf(f"{ROUTE}.plz.sell-tip.goods", f"{ROUTE}.plz.sell-tip", note="名称×数量、求购价。")
leaf(f"{ROUTE}.plz.sell-tip.vip-tax", f"{ROUTE}.plz.sell-tip", note="VIP、税率与扣税后收益。")
leaf(f"{ROUTE}.plz.sell-tip.confirm", f"{ROUTE}.plz.sell-tip", "transaction", "destructive-write", "15117 出售给求购单，消耗背包物品；验证成功/失败、列表即时刷新和重开。")

# Sell route.
page(
    f"{ROUTE}.sell",
    ROUTE,
    [
        ("bag-tabs", "radio-tabs", f"{ROUTE}.sell.bag-tabs"),
        ("bag", "bag-list", f"{ROUTE}.sell.bag"),
        ("shelf", "shelf-list", f"{ROUTE}.sell.shelf"),
        ("sell-popup", "popup", f"{ROUTE}.sell.sell-popup"),
    ],
)
page(
    f"{ROUTE}.sell.bag-tabs",
    f"{ROUTE}.sell",
    [
        ("equipment", "tab", f"{ROUTE}.sell.bag-tabs.equipment"),
        ("goods", "tab", f"{ROUTE}.sell.bag-tabs.goods"),
    ],
)
leaf(f"{ROUTE}.sell.bag-tabs.equipment", f"{ROUTE}.sell.bag-tabs", "tab", note="神装/装备类可交易且未绑定物品。")
leaf(f"{ROUTE}.sell.bag-tabs.goods", f"{ROUTE}.sell.bag-tabs", "tab", note="普通/圣印/启示/妖灵天赋等可交易且未绑定物品。")
page(
    f"{ROUTE}.sell.bag",
    f"{ROUTE}.sell",
    [
        ("structure", "scroll", f"{ROUTE}.sell.bag.structure"),
        ("scroll", "interaction", f"{ROUTE}.sell.bag.scroll"),
        ("empty", "state", f"{ROUTE}.sell.bag.empty"),
        ("item", "shared-item", f"{ROUTE}.sell.bag.item"),
        ("detail", "popup", f"{ROUTE}.sell.bag.detail"),
        ("up-shelf", "popup", f"{ROUTE}.sell.bag.up-shelf"),
    ],
)
leaf(f"{ROUTE}.sell.bag.structure", f"{ROUTE}.sell.bag", note="5 列 MarketSellItem 背包格列表。")
leaf(f"{ROUTE}.sell.bag.scroll", f"{ROUTE}.sell.bag", note="真实拖动、裁剪、末项可达与切页回顶。")
leaf(f"{ROUTE}.sell.bag.empty", f"{ROUTE}.sell.bag", note="当前筛选没有可交易物品时显示 _group_bag_empty。")
leaf(f"{ROUTE}.sell.bag.item", f"{ROUTE}.sell.bag", note="EquipmentItem、数量/锁定、up/down/ban 条件。")
leaf(f"{ROUTE}.sell.bag.detail", f"{ROUTE}.sell.bag", "navigation", note="按物品类型打开共享详情，携带 upShelf 操作。")
leaf(f"{ROUTE}.sell.bag.up-shelf", f"{ROUTE}.sell.bag", "navigation", note="详情中的上架动作打开 MarketSellCtrlView。")

page(
    f"{ROUTE}.sell.shelf",
    f"{ROUTE}.sell",
    [
        ("count", "state", f"{ROUTE}.sell.shelf.count"),
        ("structure", "scroll", f"{ROUTE}.sell.shelf.structure"),
        ("scroll", "interaction", f"{ROUTE}.sell.shelf.scroll"),
        ("empty", "state", f"{ROUTE}.sell.shelf.empty"),
        ("item", "shared-item", f"{ROUTE}.sell.shelf.item"),
    ],
)
leaf(f"{ROUTE}.sell.shelf.count", f"{ROUTE}.sell.shelf", note="已上架物品 N/market_max_sell_num；当前配置最大 1。")
leaf(f"{ROUTE}.sell.shelf.structure", f"{ROUTE}.sell.shelf", note="2 列 MarketSellGoodsItem 列表。")
leaf(f"{ROUTE}.sell.shelf.scroll", f"{ROUTE}.sell.shelf", note="真实滚动、裁剪与末项可达。")
leaf(f"{ROUTE}.sell.shelf.empty", f"{ROUTE}.sell.shelf", note="无上架物品显示 _group_item_empty。")
page(
    f"{ROUTE}.sell.shelf.item",
    f"{ROUTE}.sell.shelf",
    [
        ("identity", "item-template", f"{ROUTE}.sell.shelf.item.identity"),
        ("detail", "popup", f"{ROUTE}.sell.shelf.item.detail"),
        ("wear-state", "conditional-state", f"{ROUTE}.sell.shelf.item.wear-state"),
        ("shout", "transaction", f"{ROUTE}.sell.shelf.item.shout"),
        ("down-shelf", "transaction", f"{ROUTE}.sell.shelf.item.down-shelf"),
    ],
)
leaf(f"{ROUTE}.sell.shelf.item.identity", f"{ROUTE}.sell.shelf.item", note="MarketSellGoodsItem + EquipmentItem、名称、单价。")
leaf(f"{ROUTE}.sell.shelf.item.detail", f"{ROUTE}.sell.shelf.item", "navigation", note="打开对应类型共享详情并携带 outShelf 操作。")
leaf(f"{ROUTE}.sell.shelf.item.wear-state", f"{ROUTE}.sell.shelf.item", note="装备 up/down/ban 条件图。")
leaf(f"{ROUTE}.sell.shelf.item.shout", f"{ROUTE}.sell.shelf.item", "transaction", "destructive-write", "15122 市场喊话，写入公共/单物品冷却。")
leaf(f"{ROUTE}.sell.shelf.item.down-shelf", f"{ROUTE}.sell.shelf.item", "transaction", "destructive-write", "15108 下架并验证当前列表/背包即时刷新和重开。")

page(
    f"{ROUTE}.sell.sell-popup",
    f"{ROUTE}.sell",
    [
        ("identity", "popup", f"{ROUTE}.sell.sell-popup.identity"),
        ("close", "return", f"{ROUTE}.sell.sell-popup.close"),
        ("item", "shared-item", f"{ROUTE}.sell.sell-popup.item"),
        ("own", "state", f"{ROUTE}.sell.sell-popup.own"),
        ("shout-toggle", "toggle", f"{ROUTE}.sell.sell-popup.shout-toggle"),
        ("price", "price-controls", f"{ROUTE}.sell.sell-popup.price"),
        ("count", "count-controls", f"{ROUTE}.sell.sell-popup.count"),
        ("vip-tax", "state", f"{ROUTE}.sell.sell-popup.vip-tax"),
        ("confirm", "transaction", f"{ROUTE}.sell.sell-popup.confirm"),
    ],
)
leaf(f"{ROUTE}.sell.sell-popup.identity", f"{ROUTE}.sell.sell-popup", note="MarketSellCtrlView，Activity 层、背景遮罩、居中、点击背景关闭。")
leaf(f"{ROUTE}.sell.sell-popup.close", f"{ROUTE}.sell.sell-popup", "return", note="关闭当前上架弹窗，底层市场保持。")
leaf(f"{ROUTE}.sell.sell-popup.item", f"{ROUTE}.sell.sell-popup", note="EquipmentItem、名称与拥有数量。")
leaf(f"{ROUTE}.sell.sell-popup.own", f"{ROUTE}.sell.sell-popup", note="当前可出售拥有量。")
leaf(f"{ROUTE}.sell.sell-popup.shout-toggle", f"{ROUTE}.sell.sell-popup", note="check/toggle：是否在上架时喊话，默认 1。")
page(
    f"{ROUTE}.sell.sell-popup.price",
    f"{ROUTE}.sell.sell-popup",
    [
        ("fixed", "conditional-state", f"{ROUTE}.sell.sell-popup.price.fixed"),
        ("custom", "conditional-input", f"{ROUTE}.sell.sell-popup.price.custom"),
        ("minus", "stepper", f"{ROUTE}.sell.sell-popup.price.minus"),
        ("plus", "stepper", f"{ROUTE}.sell.sell-popup.price.plus"),
        ("calculator", "popup", f"{ROUTE}.sell.sell-popup.price.calculator"),
        ("ratio", "state", f"{ROUTE}.sell.sell-popup.price.ratio"),
    ],
)
leaf(f"{ROUTE}.sell.sell-popup.price.fixed", f"{ROUTE}.sell.sell-popup.price", note="固定价单价与百分比。")
leaf(f"{ROUTE}.sell.sell-popup.price.custom", f"{ROUTE}.sell.sell-popup.price", note="自定义总价并钳制上下限。")
leaf(f"{ROUTE}.sell.sell-popup.price.minus", f"{ROUTE}.sell.sell-popup.price", note="价格 -10% 基准。")
leaf(f"{ROUTE}.sell.sell-popup.price.plus", f"{ROUTE}.sell.sell-popup.price", note="价格 +10% 基准。")
leaf(f"{ROUTE}.sell.sell-popup.price.calculator", f"{ROUTE}.sell.sell-popup.price", "navigation", note="打开共享 CalculatorView 输入价格。")
leaf(f"{ROUTE}.sell.sell-popup.price.ratio", f"{ROUTE}.sell.sell-popup.price", note="价格百分比、总价与税费同步。")
page(
    f"{ROUTE}.sell.sell-popup.count",
    f"{ROUTE}.sell.sell-popup",
    [
        ("minus", "stepper", f"{ROUTE}.sell.sell-popup.count.minus"),
        ("plus", "stepper", f"{ROUTE}.sell.sell-popup.count.plus"),
        ("calculator", "popup", f"{ROUTE}.sell.sell-popup.count.calculator"),
        ("limit", "state", f"{ROUTE}.sell.sell-popup.count.limit"),
    ],
)
leaf(f"{ROUTE}.sell.sell-popup.count.minus", f"{ROUTE}.sell.sell-popup.count", note="数量 -1。")
leaf(f"{ROUTE}.sell.sell-popup.count.plus", f"{ROUTE}.sell.sell-popup.count", note="数量 +1。")
leaf(f"{ROUTE}.sell.sell-popup.count.calculator", f"{ROUTE}.sell.sell-popup.count", "navigation", note="打开共享 CalculatorView，最大为可卖数量。")
leaf(f"{ROUTE}.sell.sell-popup.count.limit", f"{ROUTE}.sell.sell-popup.count", note="数量受拥有量与配置上限约束；上限<=1 时禁用。")
leaf(f"{ROUTE}.sell.sell-popup.vip-tax", f"{ROUTE}.sell.sell-popup", note="当前 VIP、税率、总价、税额。")
leaf(f"{ROUTE}.sell.sell-popup.confirm", f"{ROUTE}.sell.sell-popup", "transaction", "destructive-write", "15106 上架，消耗/锁定背包实例；验证成功/失败、背包和上架列表即时刷新、重开。")

# Hidden legacy routes retained as conditions, not current visible tabs.
page(
    f"{ROUTE}.legacy",
    ROUTE,
    [
        ("auction", "disabled-route", f"{ROUTE}.legacy.auction"),
        ("record", "disabled-route", f"{ROUTE}.legacy.record"),
    ],
)
leaf(f"{ROUTE}.legacy.auction", f"{ROUTE}.legacy", note="拍卖 imports/viewClassList/tabStrList 均注释；仅残留 scene 资产，不是现行可见路线。")
page(
    f"{ROUTE}.legacy.record",
    f"{ROUTE}.legacy",
    [
        ("gate", "disabled-tab", f"{ROUTE}.legacy.record.gate"),
        ("request", "read-protocol", f"{ROUTE}.legacy.record.request"),
        ("list", "item-list", f"{ROUTE}.legacy.record.list"),
        ("empty", "state", f"{ROUTE}.legacy.record.empty"),
    ],
)
leaf(f"{ROUTE}.legacy.record.gate", f"{ROUTE}.legacy.record", note="MarketBaseView 当前移除了记录页签。")
leaf(f"{ROUTE}.legacy.record.request", f"{ROUTE}.legacy.record", note="MarketRecordDealView 代码可发 15112，但当前无入口。")
leaf(f"{ROUTE}.legacy.record.list", f"{ROUTE}.legacy.record", note="MarketRecordItem 按 time 降序，买入/卖出条件组。")
leaf(f"{ROUTE}.legacy.record.empty", f"{ROUTE}.legacy.record", note="record_list 为空时显示 _group_empty。")

# Shared prefab identity and cross-island consumer closure.
page(
    f"{ROUTE}.shared-item",
    ROUTE,
    [
        ("prefab", "prefab-identity", f"{ROUTE}.shared-item.prefab"),
        ("bind", "binding", f"{ROUTE}.shared-item.bind"),
        ("equipment", "nested-shared", f"{ROUTE}.shared-item.equipment"),
        ("chat-consumer", "consumer", f"{ROUTE}.shared-item.chat-consumer"),
        ("runtime-owner", "lifecycle", f"{ROUTE}.shared-item.runtime-owner"),
    ],
)
leaf(f"{ROUTE}.shared-item.prefab", f"{ROUTE}.shared-item", note="Assets/Prefabs/UI/Market/MarketPlzShowItem.prefab 是 Market 岛唯一现有 Prefab。")
leaf(f"{ROUTE}.shared-item.bind", f"{ROUTE}.shared-item", note="根仅挂 Generated MarketPlzShowItemBind；Generated 只读。")
leaf(f"{ROUTE}.shared-item.equipment", f"{ROUTE}.shared-item", note="嵌套 Common EquipmentItem Prefab，属于共享组件。")
leaf(f"{ROUTE}.shared-item.chat-consumer", f"{ROUTE}.shared-item", note="ChatModule.prefab 直接把该 Prefab 作为 _tpl_MarketPlzShowItem 消费；Chat 岛只读。")
leaf(f"{ROUTE}.shared-item.runtime-owner", f"{ROUTE}.shared-item", note="Market 岛没有业务子类负责数据、按钮显隐、15116/15117 与生命周期。")

# Controller/model static data coverage.
protocol_specs = [
    (15100, "S2C错误推送", False),
    (15101, "一级分类挂单数量", False),
    (15102, "二级商品列表", False),
    (15106, "上架", True),
    (15108, "下架", True),
    (15109, "我的上架列表", False),
    (15111, "购买", True),
    (15112, "交易记录", False),
    (15114, "购买次数", False),
    (15115, "发起求购", True),
    (15116, "撤销求购", True),
    (15117, "出售给求购单", True),
    (15118, "全部求购", False),
    (15119, "我的求购", False),
    (15120, "删除推送", False),
    (15121, "跨服开放时间/图标", False),
    (15122, "市场喊话", True),
]
protocol_controls = [(str(cmd), "write-protocol" if write else "read-protocol", f"{ROUTE}.protocols.p{cmd}") for cmd, _, write in protocol_specs]
protocol_controls.append(("exclusions", "negative-contract", f"{ROUTE}.protocols.exclusions"))
page(f"{ROUTE}.protocols", ROUTE, protocol_controls, "Unity Controller/Model 的 151xx 静态覆盖；不等于页面完成。")
for cmd, label, write in protocol_specs:
    if write:
        leaf(f"{ROUTE}.protocols.p{cmd}", f"{ROUTE}.protocols", "transaction", "destructive-write", f"{cmd} {label}；本轮禁止发送。")
    else:
        leaf(f"{ROUTE}.protocols.p{cmd}", f"{ROUTE}.protocols", note=f"{cmd} {label}；Controller/Model 静态实现存在，待玩家运行验证。")
leaf(f"{ROUTE}.protocols.exclusions", f"{ROUTE}.protocols", note="15103/15104/15105/15107/15110/15113 为定义缺失、老端空消费或死链，Unity 明确不注册/不发送。")

page(
    f"{ROUTE}.lifecycle",
    ROUTE,
    [
        ("open-close", "route-lifecycle", f"{ROUTE}.lifecycle.open-close"),
        ("cold-warm", "performance", f"{ROUTE}.lifecycle.cold-warm"),
        ("viewports", "visual", f"{ROUTE}.lifecycle.viewports"),
        ("scroll-clipping", "interaction", f"{ROUTE}.lifecycle.scroll-clipping"),
        ("state-refresh", "state", f"{ROUTE}.lifecycle.state-refresh"),
        ("resource-stability", "resource", f"{ROUTE}.lifecycle.resource-stability"),
    ],
)
leaf(f"{ROUTE}.lifecycle.open-close", f"{ROUTE}.lifecycle", note="默认购买页、页签互斥、弹窗优先关闭、热重开零重复回调。")
leaf(f"{ROUTE}.lifecycle.cold-warm", f"{ROUTE}.lifecycle", note="click→first-visible/interactive-ready 冷热耗时。")
leaf(f"{ROUTE}.lifecycle.viewports", f"{ROUTE}.lifecycle", note="720x1280 与 1920x1080 老 H5/Unity 真实 Web old/unity/overlay/diff。")
leaf(f"{ROUTE}.lifecycle.scroll-clipping", f"{ROUTE}.lifecycle", note="分类、商品、背包、上架、求购候选列表的真实拖动与祖先裁剪。")
leaf(f"{ROUTE}.lifecycle.state-refresh", f"{ROUTE}.lifecycle", note="成功/失败、父页即时刷新、关闭重开与被动 15120。")
leaf(f"{ROUTE}.lifecycle.resource-stability", f"{ROUTE}.lifecycle", note="首轮/二轮资源预检、二轮 imported=0/configured=0、点击后零新增。")

manifest = {
    "route": ROUTE,
    "baseline": {
        "authority": "Current old H5 under the same account, state and viewport is final authority. This manifest freezes static topology only and claims no runtime gate.",
        "legacy_sources": [
            "E:/GitProject/yu_client/h5/src/commonController/MarketController.ts",
            "E:/GitProject/yu_client/h5/src/commonModel/MarketModel.ts",
            "E:/GitProject/yu_client/h5/src/market/*.ts",
            "E:/GitProject/yu_client/cdn/resource/config/client/ConfigMarket.json",
            "E:/GitProject/yu_client/cdn/resource/config/server/config_goods_sell.json",
            "E:/GitProject/yu_client/cdn/resource/config/server/config_goods_sell_type.json",
            "E:/GitProject/yu_client/cdn/resource/config/server/config_goods_sell_subtype.json",
        ],
        "unity_sources": [
            "Assets/Scripts/Module/Core/Market/MarketController.cs",
            "Assets/Scripts/Module/Core/Market/MarketModel.cs",
            "Assets/Prefabs/UI/Market/MarketPlzShowItem.prefab",
            "Assets/Scripts/Generated/UI/Market/MarketPlzShowItemBind.cs (read-only)",
            "Assets/Prefabs/UI/Chat/ChatModule.prefab (read-only consumer)",
        ],
        "static_counts": {
            "visible_primary_tabs": 3,
            "top_categories": len(CATEGORIES),
            "buy_subtype_cells": sum(len(SUBTYPES[c]) for c, _ in CATEGORIES),
            "plz_subtype_cells": sum(len([s for s in SUBTYPES[c] if s[0] != 0]) for c, _ in CATEGORIES),
            "quality_options": len(QUALITY),
            "stage_options": len(STAGE),
            "star_options": len(STAR),
        },
        "protocol_inventory": {
            "read_or_passive": [str(cmd) for cmd, _, write in protocol_specs if not write],
            "writes": [str(cmd) for cmd, _, write in protocol_specs if write],
            "excluded": ["15103", "15104", "15105", "15107", "15110", "15113"],
            "note": "No account write or Market protocol request was executed in this pass.",
        },
        "component_dependencies": [
            {
                "component": "MarketPlzShowItem.prefab",
                "consumers": ["old MarketPlzListView", "Unity ChatModule._tpl_MarketPlzShowItem"],
                "scope": "Existing Market-path Prefab is cross-route shared; no evidence-backed mutation in this pass.",
            },
            {
                "component": "EquipmentItem",
                "consumers": ["MarketShowItem", "MarketPlzShowItem", "MarketPlzGoodsItem", "MarketSellItem", "MarketSellGoodsItem", "MarketSellCtrlView", "MarketSellTipView"],
                "scope": "Common shared component; read-only outside this edit island.",
            },
            {
                "component": "DownDropBtn / CalculatorView / typed tooltips",
                "consumers": ["MarketBuyCtrlView", "MarketPlzView", "MarketPlzCtrlView", "MarketSellCtrlView", "Market list item details"],
                "scope": "Common runtime dependency; read-only outside this edit island.",
            },
            {
                "component": "BaseWindowComponent",
                "consumers": ["MarketBaseView"],
                "scope": "Shared shell; Unity page Prefab/runtime owner is missing.",
            },
        ],
    },
    "nodes": nodes,
}

parents = {node["parent"] for node in nodes if node["parent"] is not None}
leaves = [node for node in nodes if node["id"] not in parents]

needs_runtime = {
    f"{ROUTE}.entry.icon-local",
    f"{ROUTE}.entry.icon-kf",
    f"{ROUTE}.entry.level-gate",
    f"{ROUTE}.entry.kf-time",
    f"{ROUTE}.protocols.exclusions",
}
needs_runtime.update(
    f"{ROUTE}.protocols.p{cmd}"
    for cmd, _, write in protocol_specs
    if not write
)

results_nodes = []
for node in leaves:
    node_id = node["id"]
    if node_id in needs_runtime:
        results_nodes.append(
            {
                "id": node_id,
                "status": "needs-runtime-verify",
                "runtime_gap": "Market Controller/Model static implementation exists, but no Unity/player/real-Web session was run and no player-visible claim is made.",
                "note": "Static data-chain finding only; runtime behavior remains unverified.",
                "applicable_gates": ["runtime_state"],
                "gates": {"runtime_state": False},
                "evidence": [EVIDENCE],
            }
        )
        continue

    if node["risk"] == "destructive-write" or node["type"] in {"transaction", "destructive-write"}:
        reason = "This leaf sends an account/server write or consumes/locks goods or currency. The pass explicitly forbids Market write transactions, so it was not executed."
    elif node_id.startswith(f"{ROUTE}.shared-item"):
        reason = "The only existing Market Prefab is a Generated-Bind-only shared template consumed by ChatModule; its runtime owner and cross-route representative verification are outside this file island."
    elif node_id.startswith(f"{ROUTE}.legacy") or node_id in {f"{ROUTE}.shell.tab-auction", f"{ROUTE}.shell.tab-record"}:
        reason = "The current old-client route removes/comments this tab, and Unity has no editable page Prefab/runtime View for it."
    elif node_id.startswith(f"{ROUTE}.lifecycle"):
        reason = "No Market page Prefab/runtime View exists in Unity, and this pass may not launch Unity or real Web; visual, interaction, lifecycle and performance evidence cannot be produced."
    elif node_id == f"{ROUTE}.entry.open":
        reason = "The icon/data layer exists, but Unity has no Market page Prefab, runtime View or registered click target; conversion is outside this agent's edit island."
    else:
        reason = "The old-client control is statically enumerated, but Unity has no editable Market page Prefab/runtime View. The delegated island forbids converting missing pages or modifying shared/Common UI."
    results_nodes.append(
        {
            "id": node_id,
            "status": "blocked",
            "blocked_reason": reason,
            "note": "Blocked without inventing a page, crossing the file island, launching runtime tools or executing a write transaction.",
            "evidence": [EVIDENCE],
        }
    )

(ROOT / "route-manifest-v2.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(ROOT / "results-static-v2.json").write_text(json.dumps({"nodes": results_nodes}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

needs_count = sum(item["status"] == "needs-runtime-verify" for item in results_nodes)
blocked_count = sum(item["status"] == "blocked" for item in results_nodes)
print(f"manifest nodes={len(nodes)} leaves={len(leaves)} needs-runtime-verify={needs_count} blocked={blocked_count}")
