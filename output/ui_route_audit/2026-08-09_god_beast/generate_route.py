import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
ROUTE = "mainui.treasure.god-beast"
LEGACY = "output/ui_route_audit/2026-08-09_god_beast/legacy-source.md"
UNITY = "output/ui_route_audit/2026-08-09_god_beast/unity-source.md"
nodes = []


def page(node_id, parent, note, children):
    nodes.append({
        "id": node_id,
        **({"parent": parent} if parent else {}),
        "type": "page",
        "risk": "read-only",
        "note": note,
        "control_inventory": [
            {"id": control_id, "kind": kind, "child": child_id}
            for control_id, kind, child_id in children
        ],
    })


def leaf(node_id, parent, node_type, risk, note):
    nodes.append({
        "id": node_id,
        "parent": parent,
        "type": node_type,
        "risk": risk,
        "note": note,
    })


root_children = [
    ("secret-treasure-entry", "tab", f"{ROUTE}.entry"),
    ("unlock-condition", "conditional", f"{ROUTE}.unlock"),
    ("main-page", "page", f"{ROUTE}.main"),
    ("add-position-popup", "popup", f"{ROUTE}.add-position"),
    ("bag-popup", "popup", f"{ROUTE}.bag"),
    ("compose-popup", "popup", f"{ROUTE}.compose"),
    ("select-popup", "popup", f"{ROUTE}.select"),
    ("strength-popup", "popup", f"{ROUTE}.strength"),
    ("obsolete-surfaces", "conditional", f"{ROUTE}.obsolete"),
    ("shared-dependencies", "dependency", f"{ROUTE}.shared"),
    ("lifecycle", "lifecycle", f"{ROUTE}.lifecycle"),
    ("visual", "visual", f"{ROUTE}.visual"),
    ("performance", "timing", f"{ROUTE}.performance"),
    ("resources", "dependency", f"{ROUTE}.resources"),
    ("sound", "sound", f"{ROUTE}.sound"),
]
page(ROUTE, None, "老端秘术第4页签荒祖遗骸及其全部子窗；Unity 缺主 GodBeastView，仅有子窗 Prefab。", root_children)
leaf(f"{ROUTE}.entry", ROUTE, "navigation", "read-only", "主界面秘术入口 -> SecretTreasureMainView -> 第4页签 GodBeastView。")
leaf(f"{ROUTE}.unlock", ROUTE, "read", "read-only", "外层秘术需48级/任务100970；GodBeastView需320级且开服第8天。")

main = f"{ROUTE}.main"
main_children = [
    ("overview", "state", f"{main}.overview"),
    ("beast-list", "list", f"{main}.beasts"),
    ("attribute-list", "list", f"{main}.attributes"),
    ("equipment-slots", "slots", f"{main}.equips"),
    ("skill-list", "list", f"{main}.skills"),
    ("fight-state-buttons", "conditional-buttons", f"{main}.fight"),
    ("quick-equip", "button", f"{main}.quick-equip"),
    ("disboard-all", "button", f"{main}.disboard"),
    ("open-bag", "button", f"{main}.open-bag"),
    ("open-compose", "button", f"{main}.open-compose"),
    ("open-add-position", "button", f"{main}.open-add-position"),
    ("gift-push", "conditional", f"{main}.gift"),
    ("red-dots", "conditional", f"{main}.red-dots"),
]
page(main, ROUTE, "GodBeastView 主页面；Unity Prefab/Bind/业务 View 均缺失。", main_children)
leaf(f"{main}.overview", main, "read", "read-only", "17301 总览、fight_count、荒祖遗骸状态/评分/装备/属性即时切片。")

beasts = f"{main}.beasts"
page(beasts, main, "横向荒祖遗骸列表。", [
    ("scroll-structure", "scroll", f"{beasts}.structure"),
    ("selection", "selection", f"{beasts}.selection"),
    ("item-state", "list-item", f"{beasts}.item-state"),
])
leaf(f"{beasts}.structure", beasts, "read", "read-only", "横向列表、裁剪、拖动与末项可达。")
leaf(f"{beasts}.selection", beasts, "read", "read-only", "默认优先选择有红点项，否则首项；切换后更新全部右侧状态。")
leaf(f"{beasts}.item-state", beasts, "read", "read-only", "每项图标、名称、评分、未激活灰态、出战标记、选中缩放及红点。")
leaf(f"{main}.attributes", main, "read", "read-only", "基础属性与装备增量合并后的纵向属性列表。")

equips = f"{main}.equips"
equip_children = [(f"slot-{i}", "equipment-slot", f"{equips}.slot-{i}") for i in range(1, 6)] + [
    ("equipped-detail", "popup", f"{equips}.detail")
]
page(equips, main, "五个装备部位；空槽打开选择窗，已穿戴打开同一 BeastToolTips。", equip_children)
for i in range(1, 6):
    leaf(f"{equips}.slot-{i}", equips, "navigation", "read-only", f"部位{i}：品质/星级条件、空槽图/星级、穿戴装备、红点及空/有装备点击分支。")
detail = f"{equips}.detail"
page(detail, equips, "已穿戴装备详情；根据荒祖遗骸出战状态显示不同按钮组合。", [
    ("close", "button", f"{detail}.close"),
    ("replace", "button", f"{detail}.replace"),
    ("disboard", "button", f"{detail}.disboard"),
    ("strength", "button", f"{detail}.strength"),
])
leaf(f"{detail}.close", detail, "return", "read-only", "关闭详情/背景关闭链。")
leaf(f"{detail}.replace", detail, "navigation", "read-only", "打开 GodBeastSelectView 并保留 beastId/position。")
leaf(f"{detail}.disboard", detail, "transaction", "destructive-write", "未出战时17304拆单件；出战时先确认会取消助战。")
leaf(f"{detail}.strength", detail, "navigation", "read-only", "打开 GodBeastStrView，传当前 goods_id。")

skills = f"{main}.skills"
page(skills, main, "配置 skill_ids 动态技能列表。", [
    ("layout", "list", f"{skills}.layout"),
    ("skill-item", "list-item", f"{skills}.item-all"),
])
leaf(f"{skills}.layout", skills, "read", "read-only", "全部技能图标数量、布局、清理与切换荒祖遗骸后刷新。")
leaf(f"{skills}.item-all", skills, "navigation", "read-only", "当前老端每个技能项跳 PetSkillView；不再打开本地 GodBeastSkillView。")

fight = f"{main}.fight"
page(fight, main, "state=0/1/2 三态按钮。", [
    ("inactive-gray", "conditional-button", f"{fight}.inactive"),
    ("ready-fight", "conditional-button", f"{fight}.ready"),
    ("fight-capacity-full", "condition", f"{fight}.full"),
    ("fighting-recall", "conditional-button", f"{fight}.recall"),
])
leaf(f"{fight}.inactive", fight, "read", "read-only", "未激活点击提示需穿戴五件装备。")
leaf(f"{fight}.ready", fight, "transaction", "destructive-write", "有空助战位时17305 state=2出战并即时刷新。")
leaf(f"{fight}.full", fight, "read", "read-only", "助战位已满提示，不得发17305。")
leaf(f"{fight}.recall", fight, "transaction", "destructive-write", "出战中先确认战力下降，再17305 state=1召回。")

quick = f"{main}.quick-equip"
page(quick, main, "一键穿戴的无候选、直接穿戴和经验转移确认分支。", [
    ("no-candidate", "condition", f"{quick}.no-candidate"),
    ("empty-direct", "transaction", f"{quick}.direct"),
    ("transfer-confirm", "transaction", f"{quick}.transfer-yes"),
    ("transfer-cancel-path", "transaction", f"{quick}.transfer-no"),
])
leaf(f"{quick}.no-candidate", quick, "read", "read-only", "没有符合条件装备时提示且不得发17312。")
leaf(f"{quick}.direct", quick, "transaction", "destructive-write", "当前无装备时17312 replace=1一键穿戴。")
leaf(f"{quick}.transfer-yes", quick, "transaction", "destructive-write", "已有装备时确认无损转移熟练度，17312 replace=1。")
leaf(f"{quick}.transfer-no", quick, "transaction", "destructive-write", "已有装备时取消转移选择，17312 replace=0。")

disboard = f"{main}.disboard"
page(disboard, main, "一键卸下的空、非出战、出战确认分支。", [
    ("empty", "condition", f"{disboard}.empty"),
    ("direct", "transaction", f"{disboard}.direct"),
    ("fighting-confirm", "transaction", f"{disboard}.fighting"),
])
leaf(f"{disboard}.empty", disboard, "read", "read-only", "没有穿戴装备时提示，不发协议。")
leaf(f"{disboard}.direct", disboard, "transaction", "destructive-write", "非出战状态17304 position=0卸下全部。")
leaf(f"{disboard}.fighting", disboard, "transaction", "destructive-write", "出战状态确认取消助战后17304 position=0。")
leaf(f"{main}.open-bag", main, "navigation", "read-only", "打开 GodBeastBagView。")
leaf(f"{main}.open-compose", main, "navigation", "read-only", "打开 GodBeastComView。")
leaf(f"{main}.open-add-position", main, "navigation", "read-only", "未达上限打开 GodBeastTipsView；达到6位只提示。")
leaf(f"{main}.gift", main, "navigation", "read-only", "PushGift eShiYao 条件图标及点击目标。")
leaf(f"{main}.red-dots", main, "read", "read-only", "秘卷、穿戴、替换、强化、出战、铸合及页签红点矩阵。")

add = f"{ROUTE}.add-position"
page(add, ROUTE, "GodBeastTipsView 扩充助战位确认弹窗。", [
    ("close", "button", f"{add}.close"),
    ("cancel", "button", f"{add}.cancel"),
    ("background-close", "mask", f"{add}.background"),
    ("cost-item", "item", f"{add}.cost"),
    ("level-condition", "conditional-text", f"{add}.level"),
    ("capacity-condition", "condition", f"{add}.capacity"),
    ("confirm", "button", f"{add}.confirm"),
    ("success", "result", f"{add}.success"),
    ("placeholder", "text", f"{add}.placeholder"),
    ("visual", "visual", f"{add}.visual"),
    ("lifecycle", "lifecycle", f"{add}.lifecycle"),
    ("performance", "timing", f"{add}.performance"),
])
leaf(f"{add}.close", add, "return", "read-only", "右上关闭。")
leaf(f"{add}.cancel", add, "return", "read-only", "取消按钮。")
leaf(f"{add}.background", add, "return", "read-only", "Activity遮罩点击关闭且不穿透。")
leaf(f"{add}.cost", add, "navigation", "read-only", "BaseAwardItem显示消耗、拥有/需求颜色及详情弹窗。")
leaf(f"{add}.level", add, "read", "read-only", "按当前扩展位配置显示等级满足/不足文案。")
leaf(f"{add}.capacity", add, "read", "read-only", "默认3位、最多6位；达到上限不应打开确认窗。")
leaf(f"{add}.confirm", add, "transaction", "destructive-write", "17306消耗道具永久扩充助战位。")
leaf(f"{add}.success", add, "read", "destructive-write", "17306成功关闭弹窗、提示扩充成功并即时更新父页计数，重开一致。")
leaf(f"{add}.placeholder", add, "read", "read-only", "清除 lv_txt 的 htmlText 设计占位，运行时仍需核对真实等级文案。")
leaf(f"{add}.visual", add, "read", "read-only", "弹窗尺寸、底图、文字、物品格与按钮像素。")
leaf(f"{add}.lifecycle", add, "read", "read-only", "重复打开、关闭、定时任务与共享物品格清理。")
leaf(f"{add}.performance", add, "navigation", "read-only", "cold/warm first-visible/interactive-ready。")

bag = f"{ROUTE}.bag"
page(bag, ROUTE, "GodBeastBagView 遗骸背包。", [
    ("close", "button", f"{bag}.close"),
    ("background-close", "mask", f"{bag}.background"),
    ("title", "title", f"{bag}.title"),
    ("quality-filter", "dropdown", f"{bag}.quality"),
    ("star-filter", "dropdown", f"{bag}.star"),
    ("items", "list", f"{bag}.items"),
    ("empty-state", "conditional", f"{bag}.empty"),
    ("compose-route", "button", f"{bag}.compose-route"),
    ("acquire-route", "button", f"{bag}.acquire-route"),
    ("visual", "visual", f"{bag}.visual"),
    ("lifecycle", "lifecycle", f"{bag}.lifecycle"),
    ("performance", "timing", f"{bag}.performance"),
])
leaf(f"{bag}.close", bag, "return", "read-only", "关闭按钮。")
leaf(f"{bag}.background", bag, "return", "read-only", "背景遮罩关闭。")
leaf(f"{bag}.title", bag, "read", "read-only", "当前老端隐藏位图标题并覆盖文字遗骸背包。")
leaf(f"{bag}.quality", bag, "read", "read-only", "全部品质及 ConfigGodBeast bag_color_cfg 全选项。")
leaf(f"{bag}.star", bag, "read", "read-only", "全部星级及 ConfigGodBeast bag_star_cfg 全选项。")
bag_items = f"{bag}.items"
page(bag_items, bag, "过滤后的未穿戴遗骸装备列表。", [
    ("structure", "scroll", f"{bag_items}.structure"),
    ("interaction", "drag", f"{bag_items}.scroll"),
    ("item-state", "list-item", f"{bag_items}.item-state"),
    ("detail", "popup", f"{bag_items}.detail"),
])
leaf(f"{bag_items}.structure", bag_items, "read", "read-only", "唯一 ScrollRect/Viewport/Mask/Content 结构。")
leaf(f"{bag_items}.scroll", bag_items, "read", "read-only", "真实射线拖动、裁剪、末项可达。")
leaf(f"{bag_items}.item-state", bag_items, "read", "read-only", "图标、数量、强化等级及已穿戴提示条件。")
bag_detail = f"{bag_items}.detail"
page(bag_detail, bag_items, "每个可见格打开共享 BeastToolTips，仅显示关闭按钮。", [
    ("identity", "popup", f"{bag_detail}.identity"),
    ("close", "button", f"{bag_detail}.close"),
])
leaf(f"{bag_detail}.identity", bag_detail, "read", "read-only", "详情类型、品质底图、属性、部位、描述、根尺寸与遮罩。")
leaf(f"{bag_detail}.close", bag_detail, "return", "read-only", "关闭/背景关闭返回背包并保持筛选与滚动位置。")
leaf(f"{bag}.empty", bag, "read", "read-only", "筛选结果为空时显示暂无装备。")
leaf(f"{bag}.compose-route", bag, "navigation", "read-only", "切主功能 Composite/GodBeast 后关闭背包。")
leaf(f"{bag}.acquire-route", bag, "navigation", "read-only", "打开 CrossServerEnterView 幻兽领并关闭背包。")
leaf(f"{bag}.visual", bag, "read", "read-only", "当前 Prefab 与老端同状态像素对齐。")
leaf(f"{bag}.lifecycle", bag, "read", "read-only", "过滤器、动态格、遮罩、关闭重开清理。")
leaf(f"{bag}.performance", bag, "navigation", "read-only", "cold/warm first-visible/interactive-ready。")

compose = f"{ROUTE}.compose"
page(compose, ROUTE, "GodBeastComView 遗骸铸造及自动铸造。", [
    ("close", "button", f"{compose}.close"),
    ("background-close", "mask", f"{compose}.background"),
    ("title", "title", f"{compose}.title"),
    ("preview-title", "text", f"{compose}.preview-title"),
    ("tips-placeholder", "text", f"{compose}.tips-placeholder"),
    ("quality-filter", "dropdown", f"{compose}.quality"),
    ("input-slots", "slots", f"{compose}.inputs"),
    ("target-preview", "item", f"{compose}.target"),
    ("bag-list", "list", f"{compose}.bag"),
    ("tips-state", "conditional-text", f"{compose}.tips"),
    ("forge", "button", f"{compose}.forge"),
    ("auto-compose", "conditional-control", f"{compose}.auto"),
    ("full-composite-route", "button", f"{compose}.composite-route"),
    ("acquire-route", "button", f"{compose}.acquire-route"),
    ("empty-state", "conditional", f"{compose}.empty"),
    ("success-popup", "popup", f"{compose}.success"),
    ("effect", "effect", f"{compose}.effect"),
    ("red-dot", "conditional", f"{compose}.red-dot"),
    ("lifecycle", "lifecycle", f"{compose}.lifecycle"),
    ("visual", "visual", f"{compose}.visual"),
    ("performance", "timing", f"{compose}.performance"),
])
leaf(f"{compose}.close", compose, "return", "read-only", "关闭按钮。")
leaf(f"{compose}.background", compose, "return", "read-only", "背景关闭并停止自动铸造。")
leaf(f"{compose}.title", compose, "read", "read-only", "当前老端隐藏位图标题并覆盖文字遗骸铸造。")
leaf(f"{compose}.preview-title", compose, "read", "read-only", "Prefab 文案已从遗骸装预览修正为当前老端运行覆盖的遗骸预览。")
leaf(f"{compose}.tips-placeholder", compose, "read", "read-only", "清除 tipsLb 的 aaa 转换占位；运行时仍需核对0..4件文案。")
leaf(f"{compose}.quality", compose, "read", "read-only", "ConfigGodBeast com_color_cfg 全选项及 BEAST_COM_ID cookie恢复。")
inputs = f"{compose}.inputs"
page(inputs, compose, "四件同品质同星级输入槽。", [(f"slot-{i}", "equipment-slot", f"{inputs}.slot-{i}") for i in range(4)])
for i in range(4):
    leaf(f"{inputs}.slot-{i}", inputs, "navigation", "read-only", f"输入槽{i}：加入、详情仅出仓按钮、移除、选择表同步。")
leaf(f"{compose}.target", compose, "navigation", "read-only", "按config_eudemons_compose material/reward映射展示目标装备与详情。")
com_bag = f"{compose}.bag"
page(com_bag, compose, "排除已穿戴和已选中的背包遗骸装备列表。", [
    ("structure", "scroll", f"{com_bag}.structure"),
    ("interaction", "drag", f"{com_bag}.scroll"),
    ("item-detail", "popup", f"{com_bag}.item-detail"),
])
leaf(f"{com_bag}.structure", com_bag, "read", "read-only", "已禁用冗余外层Panel ScrollRect，内层bagGp为唯一滚动消费者。")
leaf(f"{com_bag}.scroll", com_bag, "read", "read-only", "真实拖动、裁剪和末项可达。")
leaf(f"{com_bag}.item-detail", com_bag, "navigation", "read-only", "每格 BeastToolTips 仅入仓按钮；最高品质/星级限制提示。")
leaf(f"{compose}.tips", compose, "read", "read-only", "0..3件提示添加4件；4件提示部位随机。")
leaf(f"{compose}.forge", compose, "transaction", "destructive-write", "17310消耗四件装备铸造；无有效comId只提示。")
auto = f"{compose}.auto"
page(auto, compose, "Halo GodBeastComposite 权益控制一键自动铸造。", [
    ("visibility", "condition", f"{auto}.visibility"),
    ("toggle", "toggle", f"{auto}.toggle"),
    ("repeat-loop", "transaction", f"{auto}.repeat"),
])
leaf(f"{auto}.visibility", auto, "read", "read-only", "Halo未开隐藏；已开未激活显示提示；激活显示勾选图。")
leaf(f"{auto}.toggle", auto, "reversible-write", "reversible-write", "51402开启/关闭自动铸造设置，开启前有确认框。")
leaf(f"{auto}.repeat", auto, "transaction", "destructive-write", "连续17310、300ms节拍、取消/材料耗尽、关闭清理与批量奖励汇总。")
leaf(f"{compose}.composite-route", compose, "navigation", "read-only", "功能开放后切 Composite/GodBeast；Unity Composite当前明确disabled。")
leaf(f"{compose}.acquire-route", compose, "navigation", "read-only", "OpenFun 86 前往获取。")
leaf(f"{compose}.empty", compose, "read", "read-only", "无可合成装备显示引导，列表/按钮显隐正确。")
leaf(f"{compose}.success", compose, "navigation", "destructive-write", "单次或自动结束打开 CongratulationObtainView 并核对即时背包刷新/重开。")
leaf(f"{compose}.effect", compose, "read", "read-only", "ui_shenyaohecheng 归属_box_com_effect、scale=2.5、双帧动态及关闭零残留。")
leaf(f"{compose}.red-dot", compose, "read", "read-only", "beastComRed 与按钮红点即时更新。")
leaf(f"{compose}.lifecycle", compose, "read", "read-only", "cookie、筛选、动态Item、自动状态、延迟回调、特效和奖励队列清理。")
leaf(f"{compose}.visual", compose, "read", "read-only", "弹窗、槽位、目标、列表、Halo块、空态与两档viewport。")
leaf(f"{compose}.performance", compose, "navigation", "read-only", "cold/warm及自动循环资源稳定。")

select = f"{ROUTE}.select"
page(select, ROUTE, "GodBeastSelectView 部位装备选择。", [
    ("close", "button", f"{select}.close"),
    ("background-close", "mask", f"{select}.background"),
    ("condition-text", "conditional-text", f"{select}.condition"),
    ("items", "list", f"{select}.items"),
    ("empty-state", "conditional", f"{select}.empty"),
    ("acquire-route", "button", f"{select}.acquire-route"),
    ("visual", "visual", f"{select}.visual"),
    ("lifecycle", "lifecycle", f"{select}.lifecycle"),
    ("performance", "timing", f"{select}.performance"),
])
leaf(f"{select}.close", select, "return", "read-only", "关闭返回主页/详情链。")
leaf(f"{select}.background", select, "return", "read-only", "背景关闭且不穿透。")
leaf(f"{select}.condition", select, "read", "read-only", "按beastId/position显示最低品质、星级和部位要求。")
sel_items = f"{select}.items"
page(sel_items, select, "符合部位/品质/星级条件的未穿戴装备列表。", [
    ("structure", "scroll", f"{sel_items}.structure"),
    ("interaction", "drag", f"{sel_items}.scroll"),
    ("item-state", "list-item", f"{sel_items}.item-state"),
    ("detail", "popup", f"{sel_items}.detail"),
])
leaf(f"{sel_items}.structure", sel_items, "read", "read-only", "已禁用冗余外层Panel ScrollRect，内层_group_item为唯一滚动消费者。")
leaf(f"{sel_items}.scroll", sel_items, "read", "read-only", "真实拖动、裁剪和末项可达。")
leaf(f"{sel_items}.item-state", sel_items, "read", "read-only", "强化等级、已穿戴提示、评分升/降/相同及自身装备条件。")
sel_detail = f"{sel_items}.detail"
page(sel_detail, sel_items, "每格共享 BeastToolTips；未穿戴显示穿戴，已穿戴显示卸下。", [
    ("identity", "popup", f"{sel_detail}.identity"),
    ("close", "button", f"{sel_detail}.close"),
    ("wear", "button", f"{sel_detail}.wear"),
    ("transfer-confirm", "popup", f"{sel_detail}.transfer"),
    ("disboard", "button", f"{sel_detail}.disboard"),
])
leaf(f"{sel_detail}.identity", sel_detail, "read", "read-only", "详情身份、动态品质底图、属性、按钮组状态矩阵。")
leaf(f"{sel_detail}.close", sel_detail, "return", "read-only", "关闭并保留列表位置。")
leaf(f"{sel_detail}.wear", sel_detail, "transaction", "destructive-write", "17303穿戴；目标部位无强化装备时直接replace=0。")
leaf(f"{sel_detail}.transfer", sel_detail, "transaction", "destructive-write", "目标部位已强化时确认/取消熟练度转移，17303 replace=1/0。")
leaf(f"{sel_detail}.disboard", sel_detail, "transaction", "destructive-write", "17304卸下；出战状态先确认会取消助战。")
leaf(f"{select}.empty", select, "read", "read-only", "无符合装备显示空态和前往获取。")
leaf(f"{select}.acquire-route", select, "navigation", "read-only", "打开 CrossServerEnterView 幻兽领并关闭选择窗。")
leaf(f"{select}.visual", select, "read", "read-only", "弹窗、条件文案、列表格与提示图像素。")
leaf(f"{select}.lifecycle", select, "read", "read-only", "参数、列表克隆、关闭重开和17303/17304成功自动关闭。")
leaf(f"{select}.performance", select, "navigation", "read-only", "cold/warm first-visible/interactive-ready。")

strength = f"{ROUTE}.strength"
page(strength, ROUTE, "GodBeastStrView 单件遗骸装备强化。", [
    ("close", "button", f"{strength}.close"),
    ("background-close", "mask", f"{strength}.background"),
    ("equipment-list", "list", f"{strength}.items"),
    ("target-detail", "popup", f"{strength}.target"),
    ("attributes", "list", f"{strength}.attributes"),
    ("attribute-placeholder", "text", f"{strength}.attr-placeholder"),
    ("progress", "progress", f"{strength}.progress"),
    ("material", "item", f"{strength}.material"),
    ("strength-once", "button", f"{strength}.once"),
    ("strength-ten", "button", f"{strength}.ten"),
    ("success", "result", f"{strength}.success"),
    ("effect", "effect", f"{strength}.effect"),
    ("lifecycle", "lifecycle", f"{strength}.lifecycle"),
    ("visual", "visual", f"{strength}.visual"),
    ("performance", "timing", f"{strength}.performance"),
])
leaf(f"{strength}.close", strength, "return", "read-only", "关闭按钮。")
leaf(f"{strength}.background", strength, "return", "read-only", "背景关闭。")
str_items = f"{strength}.items"
page(str_items, strength, "全部已穿戴遗骸装备按评分降序的横向列表。", [
    ("structure", "scroll", f"{str_items}.structure"),
    ("interaction", "drag", f"{str_items}.scroll"),
    ("selection", "list-item", f"{str_items}.selection"),
])
leaf(f"{str_items}.structure", str_items, "read", "read-only", "已禁用冗余外层Panel ScrollRect，内层_list为唯一滚动消费者。")
leaf(f"{str_items}.scroll", str_items, "read", "read-only", "横向拖动、选中项scrollTo及末项可达。")
leaf(f"{str_items}.selection", str_items, "read", "read-only", "默认指定goodsId或首个可强化项，名称/品质/强化等级/选中态。")
leaf(f"{strength}.target", strength, "navigation", "read-only", "目标 EquipmentItem 详情弹窗。")
leaf(f"{strength}.attributes", strength, "read", "read-only", "当前/下级属性、满级单列布局和滚动裁剪。")
leaf(f"{strength}.attr-placeholder", strength, "read", "read-only", "清除 curAttr/nextAttr 的 aa 转换占位；真实属性仍需运行态排版。")
leaf(f"{strength}.progress", strength, "read", "read-only", "当前exp/需求、满级、逐级动画和多级递归。")
leaf(f"{strength}.material", strength, "navigation", "read-only", "39510000材料格、拥有数量、详情与不足条件。")
leaf(f"{strength}.once", strength, "transaction", "destructive-write", "有材料时17311 count=1强化；不足只提示。")
leaf(f"{strength}.ten", strength, "transaction", "destructive-write", "有材料时17311 count=10强化；不足只提示。")
leaf(f"{strength}.success", strength, "read", "destructive-write", "成功效果/提示、父列表即时强化等级、属性、进度与材料刷新、重开。")
leaf(f"{strength}.effect", strength, "read", "read-only", "ui_qianghuachenggong及预留effect1/effect2动态、归属和清理。")
leaf(f"{strength}.lifecycle", strength, "read", "read-only", "Item、Tween、period timer、事件与关闭重开清理。")
leaf(f"{strength}.visual", strength, "read", "read-only", "弹窗、装备横列、属性、进度、按钮与两档viewport。")
leaf(f"{strength}.performance", strength, "navigation", "read-only", "cold/warm first-visible/interactive-ready。")

obsolete = f"{ROUTE}.obsolete"
page(obsolete, ROUTE, "Prefab 中存在但当前老端玩家路线不使用的旧表面。", [
    ("GodBeastStrenView", "dead-view", f"{obsolete}.stren-view"),
    ("GodBeastSkillView", "dead-view", f"{obsolete}.skill-view"),
])
leaf(f"{obsolete}.stren-view", obsolete, "read", "read-only", "老端文件全部实现已注释，入口改走GodBeastComView/GodBeastStrView；不得把旧Prefab当当前功能。")
leaf(f"{obsolete}.skill-view", obsolete, "read", "read-only", "当前技能项明确跳PetSkillView，本地GodBeastSkillView不在现行点击链。")

shared = f"{ROUTE}.shared"
shared_children = [
    ("equipment-item", "shared-component", f"{shared}.equipment-item"),
    ("base-award-item", "shared-component", f"{shared}.base-award"),
    ("beast-tooltip", "shared-popup", f"{shared}.beast-tooltip"),
    ("dropdown", "shared-component", f"{shared}.dropdown"),
    ("alert", "shared-popup", f"{shared}.alert"),
    ("reward-popup", "shared-popup", f"{shared}.reward-popup"),
    ("pet-skill", "external-page", f"{shared}.pet-skill"),
    ("halo", "external-module", f"{shared}.halo"),
    ("composite", "external-module", f"{shared}.composite"),
    ("boss", "external-module", f"{shared}.boss"),
    ("shell", "external-module", f"{shared}.shell"),
]
page(shared, ROUTE, "只读影响面；本岛禁止修改 Common/Pet/Halo/Composite/Boss/MainUI。", shared_children)
for slug, label in [
    ("equipment-item", "EquipmentItem：Bag/Select/Com/Strength/主页五槽的同一物品格。"),
    ("base-award", "BaseAwardItem：扩位消耗与强化材料。"),
    ("beast-tooltip", "BeastToolTips：关闭/卸下/穿戴/替换/入仓/出仓/强化七按钮状态矩阵。"),
    ("dropdown", "DownDropBtn：背包品质/星级及铸造品质。"),
    ("alert", "Alert：召回、卸下、经验转移、Halo设置确认。"),
    ("reward-popup", "CongratulationObtainView：铸造成功奖励。"),
    ("pet-skill", "PetSkillView：当前技能详情目标。"),
    ("halo", "Halo privilege 6及51402设置。"),
    ("composite", "Composite/GodBeast完整铸合页；Unity当前disabled。"),
    ("boss", "CrossServerEnterView/Eudaemon_Ridge_Boss获取路线。"),
    ("shell", "SecretTreasureMainView/BaseWindow入口、标题、页签和返回链。"),
]:
    leaf(f"{shared}.{slug}", shared, "read", "read-only", label)

leaf(f"{ROUTE}.lifecycle", ROUTE, "read", "read-only", "模块加载、子窗互斥、Activity遮罩、隐藏/重开、延迟/事件/动态项/特效清理。")
leaf(f"{ROUTE}.visual", ROUTE, "read", "read-only", "主页面缺失；子窗仅静态Prefab，缺同账号老端/Unity/overlay/diff。")
leaf(f"{ROUTE}.performance", ROUTE, "navigation", "read-only", "入口及每个子窗cold/warm，首次资源ready 350ms/1000ms/final。")

resources = f"{ROUTE}.resources"
page(resources, ROUTE, "配置、图片与协议最小闭包。", [
    ("configs", "config", f"{resources}.configs"),
    ("textures", "resource", f"{resources}.textures"),
    ("protocol", "protocol", f"{resources}.protocol"),
])
leaf(f"{resources}.configs", resources, "read", "read-only", "Unity缺config_eudemons_item/equip_pos/equip_attr/strength/compose/cfg及ConfigGodBeast。")
leaf(f"{resources}.textures", resources, "read", "read-only", "GodBeast专属纹理存在，但缺当前配置可达闭包、幂等预检和点击后零新增证据。")
leaf(f"{resources}.protocol", resources, "read", "read-only", "Unity只实现17300/01/02/08/09；缺03/04/05/06/07/10/11/12业务闭环。")
leaf(f"{ROUTE}.sound", ROUTE, "read", "read-only", "老端页面仅声明通用点击声，成功演出由公共大特效触发；缺Unity实际声音消费者和生命周期证据。")


manifest = {
    "route": ROUTE,
    "baseline": {
        "scope": "SecretTreasure GodBeast current route and all reachable child popups",
        "legacy_source": LEGACY,
        "unity_source": UNITY,
        "runtime_evidence": "not-collected-by-user-scope",
    },
    "nodes": nodes,
}

parent_ids = {n["parent"] for n in nodes if "parent" in n}
leaves = [n for n in nodes if n["id"] not in parent_ids]
needs_runtime = {
    f"{add}.placeholder": "已清除lv_txt的htmlText占位；缺真实等级状态、TMP排版和两档viewport证据。",
    f"{compose}.preview-title": "已同步当前老端运行覆盖文案遗骸预览；缺真实Canvas字号、位置和像素证据。",
    f"{compose}.tips-placeholder": "已清除tipsLb的aaa占位；缺0..4件真实数据状态文案和排版证据。",
    f"{com_bag}.structure": "已禁用冗余外层ScrollRect并保留内层bagGp；缺GraphicRaycaster真实拖动、裁剪和末项可达证据。",
    f"{sel_items}.structure": "已禁用冗余外层ScrollRect并保留内层_group_item；缺GraphicRaycaster真实拖动、裁剪和末项可达证据。",
    f"{str_items}.structure": "已禁用冗余外层ScrollRect并保留内层_list；缺GraphicRaycaster真实拖动、裁剪和末项可达证据。",
    f"{strength}.attr-placeholder": "已清除curAttr/nextAttr的aa占位；缺真实短/长属性、满级和滚动排版证据。",
}


def blocked_reason(n):
    node_id = n["id"]
    if n["risk"] == "destructive-write" or n["type"] == "transaction":
        return "该叶会穿戴、卸下、出战、召回、扩位、铸造、强化或消耗真实物品；本轮禁止账号写事务，且Unity业务协议闭环未实现。"
    if n["risk"] == "reversible-write":
        return "该叶会写Halo设置；本轮禁止账号写事务，且跨Halo文件岛不得补实现。"
    if node_id.startswith(shared):
        return "该共享组件/跨模块消费者在GodBeast唯一写者范围外；只能记录影响面，不能修改或静态代验运行状态。"
    if node_id == f"{resources}.configs":
        return "GodBeast页面所需客户端/服务端配置在Unity当前配置目录缺失，无法从现有wire模型派生页面数据。"
    if node_id == f"{resources}.protocol":
        return "当前GodBeastController仅为读侧，缺17303/04/05/06/07/10/11/12收发及对应Model事件；事务不可静态完成。"
    if node_id.startswith(f"{ROUTE}.obsolete"):
        return "当前老端玩家路线已停用该表面；Unity Prefab虽存在，但没有权威入口，不得把旧页面误接为当前功能。"
    if node_id in (f"{ROUTE}.entry", f"{ROUTE}.unlock") or node_id.endswith(".open-bag") or node_id.endswith(".open-compose") or node_id.endswith(".open-add-position"):
        return "GodBeast主页面、SecretTreasure外壳或入口Flow缺失且位于本岛外/无Prefab；无法形成真实点击链。"
    if node_id == f"{ROUTE}.visual" or node_id == f"{ROUTE}.performance":
        return "主页面缺失，且本轮禁止启动Unity/浏览器；无法采集当前Player/catalog、两档viewport或cold/warm证据。"
    if node_id == f"{ROUTE}.lifecycle":
        return "Unity没有GodBeast Flow/View业务层，无法静态证明模块互斥、关闭重开及异步清理。"
    if node_id == f"{ROUTE}.sound":
        return "Unity没有GodBeast页面消费者且本轮禁止运行；通用点击声或资源存在不能证明页面声音时序。"
    if node_id.startswith(main):
        return "Unity缺整个GodBeastView主Prefab、Bind和业务View；该主页面叶无法在现有子窗Prefab中实现。"
    return "Unity仅有原始Bind子窗且没有GodBeast View/Flow业务脚本或所需配置；本轮又禁止Unity/真实Web，无法完成真实点击、状态、视觉与生命周期闸。"


results = {"updated_at": "2026-08-09T18:00:00+08:00", "nodes": []}
for n in leaves:
    item = {"id": n["id"]}
    if n["id"] in needs_runtime:
        item.update({"status": "needs-runtime-verify", "runtime_gap": needs_runtime[n["id"]]})
    else:
        item.update({"status": "blocked", "blocked_reason": blocked_reason(n)})
    item["evidence"] = [LEGACY, UNITY]
    results["nodes"].append(item)

ids = [n["id"] for n in nodes]
assert len(ids) == len(set(ids))
assert all(n.get("parent") in set(ids) for n in nodes if n.get("parent"))
assert {r["id"] for r in results["nodes"]} == {n["id"] for n in leaves}

(OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "results-static-boundary.json").write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"generated nodes={len(nodes)} leaves={len(leaves)} runtime={len(needs_runtime)} blocked={len(leaves)-len(needs_runtime)}")
