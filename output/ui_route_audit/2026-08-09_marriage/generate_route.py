from __future__ import annotations

import hashlib
import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
REPO = OUT.parents[2]
LEGACY = Path(r"E:\GitProject\yu_client\h5\src\marriage")

nodes: list[dict] = []
results: list[dict] = []


def page(node_id: str, parent: str | None, title: str, controls: list[tuple[str, str, str]], note: str = "") -> None:
    node = {
        "id": node_id,
        "type": "page",
        "risk": "read-only",
        "control_inventory": [
            {"id": control_id, "kind": kind, "child": child_id}
            for control_id, kind, child_id in controls
        ],
        "note": title if not note else f"{title}；{note}",
    }
    if parent is not None:
        node["parent"] = parent
    nodes.append(node)


def leaf(
    node_id: str,
    parent: str,
    node_type: str,
    risk: str,
    legacy: str,
    unity: str,
    status: str,
    reason: str,
) -> None:
    nodes.append(
        {
            "id": node_id,
            "parent": parent,
            "type": node_type,
            "risk": risk,
            "note": f"老端：{legacy}；Unity：{unity}",
        }
    )
    row = {"id": node_id, "status": status, "note": f"老端：{legacy}；Unity：{unity}"}
    if status == "blocked":
        row["blocked_reason"] = reason
    else:
        row["runtime_gap"] = reason
    results.append(row)


def nrv(node_id: str, parent: str, node_type: str, legacy: str, unity: str, gap: str) -> None:
    leaf(node_id, parent, node_type, "read-only", legacy, unity, "needs-runtime-verify", gap)


def blocked_read(node_id: str, parent: str, node_type: str, legacy: str, unity: str, reason: str) -> None:
    leaf(node_id, parent, node_type, "read-only", legacy, unity, "blocked", reason)


def blocked_write(node_id: str, parent: str, legacy: str, unity: str, reason: str) -> None:
    leaf(node_id, parent, "transaction", "destructive-write", legacy, unity, "blocked", reason)


root_controls = [
    ("mainui-love-entry", "entry", "marriage.main"),
    ("popup-ask-list", "popup", "marriage.ask-list"),
    ("push-ask-tips", "conditional-popup", "marriage.ask-tips"),
    ("popup-ask", "popup", "marriage.ask"),
    ("push-break-tips", "conditional-popup", "marriage.break-tips"),
    ("popup-break", "popup", "marriage.break"),
    ("popup-com", "popup", "marriage.com"),
    ("popup-dsgt", "popup", "marriage.dsgt"),
    ("popup-dun-luck", "conditional-popup", "marriage.dun-luck"),
    ("popup-dun-tips", "conditional-popup", "marriage.dun-tips"),
    ("push-flower-tips", "conditional-popup", "marriage.flower-tips"),
    ("popup-flower", "popup", "marriage.flower"),
    ("popup-flow", "popup", "marriage.flow"),
    ("level-fore-show", "conditional-popup", "marriage.fore-show"),
    ("popup-gift-tips", "conditional-popup", "marriage.gift-tips"),
    ("role-fame-honour", "entry", "marriage.honour"),
    ("popup-issue", "popup", "marriage.issue"),
    ("popup-record-com", "popup", "marriage.record-com"),
    ("popup-record-flower", "popup", "marriage.record-flower"),
    ("popup-role-menu", "popup", "marriage.role-menu"),
    ("push-success", "conditional-popup", "marriage.success"),
    ("shared-friend-item", "shared-component", "marriage.shared.friend-item"),
    ("shared-drop-btn", "shared-component", "marriage.shared.drop-btn"),
    ("sound-contract", "audio", "marriage.sound-contract"),
]
page(
    "marriage",
    None,
    "婚恋完整静态路由根",
    root_controls,
    "21 个 MarriageModule 顶层窗口 + 2 个独立共享 Prefab + 页面专属声音契约；当前轮禁止启动 Unity/浏览器，因此不产生 done",
)
blocked_read(
    "marriage.sound-contract",
    "marriage",
    "read",
    "主窗关闭消费 SOUND_UI.CLOSE；旧 RingChange 成功演出消费 3_huode",
    "未发现 Marriage 页面专属声音消费者；RingChange 当前不在 Prefab 可达树",
    "需要真实可达路径、成功回包时点、关闭/切页生命周期与声音播放证据；禁止在未迁移事务按钮上伪播",
)


# Main shell and five embedded tab pages.
page(
    "marriage.main",
    "marriage",
    "MarriageBaseView",
    [
        ("tab-lobby", "tab", "marriage.main.lobby"),
        ("tab-mate", "tab", "marriage.main.mate"),
        ("tab-ring", "tab", "marriage.main.ring"),
        ("tab-gift", "tab", "marriage.main.gift"),
        ("tab-dungeon", "tab", "marriage.main.dungeon"),
        ("close", "button", "marriage.main.close"),
        ("background-close", "background", "marriage.main.background-close"),
        ("default-tab", "conditional-state", "marriage.main.default-tab"),
        ("open-level-gates", "conditional-state", "marriage.main.open-level-gates"),
        ("tab-red-dots", "conditional-state", "marriage.main.tab-red-dots"),
    ],
)
nrv("marriage.main.close", "marriage.main", "return", "关闭主面板，播放关闭音", "_btn_close 已绑定 Hide", "缺真实 GraphicRaycaster 点击、关闭清理与热重开")
blocked_read("marriage.main.background-close", "marriage.main", "return", "click_bg_toClose=true", "未发现等价遮罩点击接线", "当前 Prefab/Flow 未提供可静态确认的背景关闭链，需运行态补证并增量修复")
blocked_read("marriage.main.default-tab", "marriage.main", "read", "单身默认大厅 index=0；已婚默认姻缘 index=1", "MarriageBaseView.OnShow 固定 SelectTab(0)", "婚姻状态驱动的默认页签尚未实现；需接权威角色婚姻态后再做运行验证")
blocked_read("marriage.main.open-level-gates", "marriage.main", "read", "大厅/姻缘使用 ICON_OPEN_LV_2，其余页签使用 OPEN_LV 条件门槛", "五个页签均直接可选，未见等级门槛提示", "等级条件、提示文案与状态刷新未迁移")
blocked_read("marriage.main.tab-red-dots", "marriage.main", "read", "大厅/姻缘/锦囊/副本按 Model 事件刷新红点", "MarriageBaseView.OnShow 固定隐藏五个红点", "红点事件消费、条件矩阵和切页即时刷新未迁移")

page(
    "marriage.main.lobby",
    "marriage.main",
    "大厅 MarriageFriendView",
    [
        ("list-empty", "state", "marriage.main.lobby.empty"),
        ("list-scroll", "list", "marriage.main.lobby.scroll"),
        ("list-self-row", "conditional-row", "marriage.main.lobby.row-self"),
        ("list-other-row", "conditional-row", "marriage.main.lobby.row-other"),
        ("first-page", "button", "marriage.main.lobby.page-first"),
        ("previous-page", "button", "marriage.main.lobby.page-prev"),
        ("next-page", "button", "marriage.main.lobby.page-next"),
        ("last-page", "button", "marriage.main.lobby.page-last"),
        ("flower-record", "button", "marriage.main.lobby.open-flower-record"),
        ("my-confession", "button", "marriage.main.lobby.open-com-self"),
        ("other-confession", "button", "marriage.main.lobby.open-com-other"),
        ("issue", "button", "marriage.main.lobby.open-issue"),
    ],
)
blocked_read("marriage.main.lobby.empty", "marriage.main.lobby", "read", "17200 空列表显示空态", "仅请求17200，未消费 Model 刷空态", "列表表现层尚未接管")
blocked_read("marriage.main.lobby.scroll", "marriage.main.lobby", "read", "循环列表可纵向滚动并按页展示", "MarriageFriendItem 独立 Prefab 仅 Generated Bind", "需 FriendItem 业务 View、ScrollRect 真实拖动与裁剪/末项证据")
blocked_read("marriage.main.lobby.row-self", "marriage.main.lobby", "read", "本人行点击撩一撩提示不能撩自己", "列表未铺设", "列表项业务 View 未接管")
blocked_read("marriage.main.lobby.row-other", "marriage.main.lobby", "navigation", "其他玩家行按状态展示在线/标签/亲密度并打开资料操作", "列表未铺设", "依赖 Friend/Common 共享头像、称号和菜单，超出当前文件岛")
for suffix, legacy in [
    ("page-first", "跳到第1页"),
    ("page-prev", "上一页"),
    ("page-next", "下一页"),
    ("page-last", "末页"),
]:
    blocked_read(f"marriage.main.lobby.{suffix}", "marriage.main.lobby", "read", legacy, "当前仅日志占位", "分页 Model 消费和页码刷新未接线")
for suffix, legacy, target in [
    ("open-flower-record", "打开送花记录", "MarriageRecordFlowerView"),
    ("open-com-self", "打开我的表白", "MarriageRecordComView(Self)"),
    ("open-com-other", "打开对方表白", "MarriageRecordComView(Other)"),
    ("open-issue", "打开发布心愿", "MarriageIssueView"),
]:
    blocked_read(f"marriage.main.lobby.{suffix}", "marriage.main.lobby", "navigation", legacy, f"当前仅日志，目标 {target} 仅 Generated Bind", "目标弹窗业务 View 尚未接管")

page(
    "marriage.main.mate",
    "marriage.main",
    "姻缘 MarriageMainView",
    [
        ("self-model", "model", "marriage.main.mate.self-model"),
        ("mate-state", "conditional-state", "marriage.main.mate.mate-state"),
        ("mate-model", "model", "marriage.main.mate.mate-model"),
        ("mate-menu", "button", "marriage.main.mate.open-menu"),
        ("find", "button", "marriage.main.mate.find"),
        ("ask", "conditional-button", "marriage.main.mate.ask"),
        ("again", "conditional-button", "marriage.main.mate.again"),
        ("break", "conditional-button", "marriage.main.mate.break"),
        ("flower", "conditional-button", "marriage.main.mate.flower"),
        ("flow", "conditional-button", "marriage.main.mate.flow"),
        ("banquet", "conditional-button", "marriage.main.mate.banquet"),
        ("dsgt", "button", "marriage.main.mate.dsgt"),
        ("gift-time", "state", "marriage.main.mate.gift-time"),
        ("intimacy-day", "state", "marriage.main.mate.intimacy-day"),
    ],
)
blocked_read("marriage.main.mate.self-model", "marriage.main.mate", "read", "显示本人3D模型/名字/性别", "已请求17232/17238但未创建模型", "需要 UIModelStage/RT 真实出帧；公共模型链不在本文件岛")
blocked_read("marriage.main.mate.mate-state", "marriage.main.mate", "read", "单身/已婚切换求婚、再续、解除、婚宴、影子与天数", "未消费17232切显隐", "权威状态表现接线缺失")
blocked_read("marriage.main.mate.mate-model", "marriage.main.mate", "read", "已婚显示伴侣3D，单身显示性别占位", "未创建/清理伴侣模型", "需要真实 RT 像素、换态清理和两 viewport 证据")
blocked_read("marriage.main.mate.open-menu", "marriage.main.mate", "navigation", "点击伴侣信息开/关 MarriageRoleMenuView", "未绑定 _box_info", "目标仅 Generated Bind，且依赖 Friend/Chat 禁止岛")
for suffix, legacy, unity in [
    ("find", "打开可求婚好友列表", "已绑定打开 MarriageAskListView"),
    ("ask", "单身显示并打开求婚", "已绑定打开 MarriageAskView"),
    ("again", "已婚显示并带伴侣参数打开再续", "已绑定但未传伴侣参数"),
    ("break", "已婚显示并带伴侣参数打开解除", "已绑定但未传伴侣参数"),
    ("flow", "单身态显示花房动态", "已绑定打开 MarriageFlowView"),
    ("dsgt", "打开恩爱称号列表", "已绑定打开 MarriageDsgtView"),
]:
    nrv(f"marriage.main.mate.{suffix}", "marriage.main.mate", "navigation", legacy, unity, "缺条件显隐、参数、目标身份及关闭重开真实运行证据")
blocked_read("marriage.main.mate.flower", "marriage.main.mate", "navigation", "老端代码绑定送花但 SetDataVisible 恒隐藏该按钮", "Unity 未证明条件分支", "需当前老端真实运行态确认是否为死控件，禁止凭源码猜可见性")
blocked_read("marriage.main.mate.banquet", "marriage.main.mate", "navigation", "已婚显示并打开 marriage2/BanquetApplyView 后关主窗", "当前仅日志", "marriage2/Common/MainUI 不在当前文件岛")
blocked_read("marriage.main.mate.gift-time", "marriage.main.mate", "read", "17238 刷双方礼包剩余天数/尚未获得", "只请求不渲染", "礼包状态表现接线缺失")
blocked_read("marriage.main.mate.intimacy-day", "marriage.main.mate", "read", "好友亲密度与结缘天数即时刷新", "未渲染", "依赖 Friend 与服务器时间公共链，当前文件岛禁止修改")

page(
    "marriage.main.ring",
    "marriage.main",
    "指环 MarriageRingView",
    [
        ("ring-presentation", "model", "marriage.main.ring.presentation"),
        ("partner-state", "state", "marriage.main.ring.partner-state"),
        ("stars-attrs", "state", "marriage.main.ring.attrs"),
        ("progress-effect", "effect", "marriage.main.ring.progress"),
        ("cost-item", "item", "marriage.main.ring.cost"),
        ("upgrade", "button", "marriage.main.ring.upgrade"),
        ("stop-visual", "state", "marriage.main.ring.stop-visual"),
    ],
)
blocked_read("marriage.main.ring.presentation", "marriage.main.ring", "read", "指环模型/图标浮动、名称、战力", "已请求17210但未渲染", "模型与 FightingShowSmallItem 表现链未接")
blocked_read("marriage.main.ring.partner-state", "marriage.main.ring", "read", "在婚/失婚状态图互斥", "未消费角色婚姻态", "RoleModel 婚姻字段未落地且不在本文件岛")
blocked_read("marriage.main.ring.attrs", "marriage.main.ring", "read", "星级、主属性与词条列表", "未消费 RingInfo/config_ring_star", "列表/配置表现接线缺失")
blocked_read("marriage.main.ring.progress", "marriage.main.ring", "read", "祈愿进度与升级补间/成功特效", "未渲染", "需双时间点动画、RT/Canvas 与清理证据")
blocked_read("marriage.main.ring.cost", "marriage.main.ring", "read", "真实消耗物品格与不足态", "未创建 BaseAwardItem", "Common/Bag 共享组件不在文件岛")
blocked_write("marriage.main.ring.upgrade", "marriage.main.ring", "点击按未激活/已激活发17211或17213，校验材料并防连点", "当前仅日志；本轮只恢复17210只读查询", "戒指解锁/升级会消费材料，未获账号写事务授权")
nrv("marriage.main.ring.stop-visual", "marriage.main.ring", "read", "_btn_stop 无点击处理，只是演出状态节点", "已移除 Unity 伪点击绑定", "需 Prefab 射线与演出显隐确认不再形成可点击叶")

page(
    "marriage.main.gift",
    "marriage.main",
    "锦囊 MarriageGiftView",
    [
        ("mate-model", "model", "marriage.main.gift.mate-model"),
        ("gift-state", "state", "marriage.main.gift.state"),
        ("gift-items", "list", "marriage.main.gift.items"),
        ("ask-partner", "text-button", "marriage.main.gift.ask-partner"),
        ("buy", "button", "marriage.main.gift.buy"),
        ("take-return", "button", "marriage.main.gift.take-return"),
        ("take-daily", "button", "marriage.main.gift.take-daily"),
    ],
)
blocked_read("marriage.main.gift.mate-model", "marriage.main.gift", "read", "伴侣模型/占位、名字、性别", "已请求17232/17238但未渲染", "需真实模型出帧与清理证据")
blocked_read("marriage.main.gift.state", "marriage.main.gift", "read", "购买态、每日领取态、倒计时、剩余天数与红点", "只请求不消费", "礼包表现接线缺失")
blocked_read("marriage.main.gift.items", "marriage.main.gift", "read", "每日礼包物品格横排", "未创建 BaseAwardItem", "共享物品格/Common 不在文件岛")
blocked_write("marriage.main.gift.ask-partner", "marriage.main.gift", "点击请Ta赠送并发17240", "未绑定 _lb_give", "会向伴侣发真实请求，未获账号写事务授权")
blocked_write("marriage.main.gift.buy", "marriage.main.gift", "二次确认后17237真实购买", "当前仅日志", "会真实消费货币/购买礼包，未授权")
blocked_write("marriage.main.gift.take-return", "marriage.main.gift", "17239 count_type=1领取返还奖励", "当前仅日志", "真实领奖事务未授权")
blocked_write("marriage.main.gift.take-daily", "marriage.main.gift", "17239 count_type=2领取每日奖励", "当前仅日志", "真实领奖事务未授权")

page(
    "marriage.main.dungeon",
    "marriage.main",
    "副本 MarriageDunView",
    [
        ("count", "state", "marriage.main.dungeon.count"),
        ("rewards", "list", "marriage.main.dungeon.rewards"),
        ("teammate-drop", "dropdown", "marriage.main.dungeon.teammate"),
        ("mate-mark", "state", "marriage.main.dungeon.mate-mark"),
        ("match", "button", "marriage.main.dungeon.match"),
        ("challenge", "button", "marriage.main.dungeon.challenge"),
        ("add-count", "button", "marriage.main.dungeon.add-count"),
        ("help", "button", "marriage.main.dungeon.help"),
    ],
)
blocked_read("marriage.main.dungeon.count", "marriage.main.dungeon", "read", "61020/次数事件刷新剩余/总次数", "当前只日志", "BaseDungeon 禁止岛依赖未接")
blocked_read("marriage.main.dungeon.rewards", "marriage.main.dungeon", "read", "config_dungeon_ui_content 奖励横排", "未创建 EquipmentItem", "配置与共享物品格跨禁止岛")
blocked_read("marriage.main.dungeon.teammate", "marriage.main.dungeon", "read", "下拉覆盖伴侣、异性/高亲密好友和无对象", "独立 MarriageDropBtn 仅 Generated Bind", "依赖 FriendModel 且缺共享下拉业务 View")
blocked_read("marriage.main.dungeon.mate-mark", "marriage.main.dungeon", "read", "选中伴侣时显示伴侣标识", "未消费下拉选择", "下拉链未接")
blocked_read("marriage.main.dungeon.match", "marriage.main.dungeon", "read", "旧 UI 有点击但17245服务端 handler 已注释，当前产品死链", "Unity 只日志且明确不发送", "服务端入口不可达；不得恢复发送")
blocked_write("marriage.main.dungeon.challenge", "marriage.main.dungeon", "校验次数/队友，离线伴侣二次确认后61046", "当前仅日志", "会创建真实副本事务，未获授权且 BaseDungeon 不在文件岛")
blocked_write("marriage.main.dungeon.add-count", "marriage.main.dungeon", "结婚后确认61021购买或17295请伴侣购买", "当前仅日志", "真实购买/邀请写事务未授权")
nrv("marriage.main.dungeon.help", "marriage.main.dungeon", "navigation", "打开说明1721", "已有可见按钮但仅日志", "需接入现有说明路由后做目标身份与返回链；公共路由不在本文件岛")


# Top-level popup/window surfaces in MarriageModule.
page(
    "marriage.ask-list",
    "marriage",
    "MarriageAskListView",
    [
        ("close", "button", "marriage.ask-list.close"),
        ("empty", "state", "marriage.ask-list.empty"),
        ("scroll", "list", "marriage.ask-list.scroll"),
        ("row-ask", "row-button", "marriage.ask-list.row-ask"),
        ("go-main", "button", "marriage.ask-list.go-main"),
    ],
)
nrv("marriage.ask-list.close", "marriage.ask-list", "return", "关闭返回", "已绑定 Hide", "缺真实点击、遮罩层级和热重开")
blocked_read("marriage.ask-list.empty", "marriage.ask-list", "read", "好友返回空时显示空态", "固定显示空态", "Friend 权威快照未接，固定空态不能验收")
blocked_read("marriage.ask-list.scroll", "marriage.ask-list", "read", "可求婚好友循环列表", "模板隐藏且不铺列表", "依赖 FriendModel/MarriageAskListItem，跨禁止岛")
blocked_read("marriage.ask-list.row-ask", "marriage.ask-list", "navigation", "每行求婚按钮打开 MarriageAskView 并带对象", "列表项无业务 View", "共享项未接管")
blocked_read("marriage.ask-list.go-main", "marriage.ask-list", "navigation", "前往 MarriageBaseView", "当前仅日志", "缺目标路由和返回链")

page(
    "marriage.ask-tips",
    "marriage",
    "MarriageAskTipsView（17222求婚推送）",
    [
        ("profile", "state", "marriage.ask-tips.profile"),
        ("agree", "button", "marriage.ask-tips.agree"),
        ("refuse", "button", "marriage.ask-tips.refuse"),
        ("close-as-refuse", "button", "marriage.ask-tips.close"),
    ],
)
blocked_read("marriage.ask-tips.profile", "marriage.ask-tips", "read", "显示求婚者头像/名/文案/战力", "仅 Generated Bind，无业务 View", "推送弹窗表现未接管")
blocked_write("marriage.ask-tips.agree", "marriage.ask-tips", "17223同意求婚并关闭，随后重拉伴侣/礼包/戒指", "Controller 有 sender/handler，弹窗未接", "真实结婚事务未获授权")
blocked_write("marriage.ask-tips.refuse", "marriage.ask-tips", "17223拒绝求婚并关闭", "弹窗未接", "真实拒绝事务未获授权")
blocked_write("marriage.ask-tips.close", "marriage.ask-tips", "关闭等价拒绝并发17223 type=2", "弹窗未接", "关闭具有写语义，未授权时不得伪作普通返回")

page(
    "marriage.ask",
    "marriage",
    "MarriageAskView",
    [
        ("close", "button", "marriage.ask.close"),
        ("help", "button", "marriage.ask.help"),
        ("self-head", "state", "marriage.ask.self-head"),
        ("target-drop", "dropdown", "marriage.ask.target-drop"),
        ("ring-list", "list", "marriage.ask.ring-list"),
        ("propose", "button", "marriage.ask.propose"),
    ],
)
nrv("marriage.ask.close", "marriage.ask", "return", "关闭返回主面板", "已绑定 Hide", "缺真实点击/遮罩/重开")
nrv("marriage.ask.help", "marriage.ask", "navigation", "打开说明172", "当前仅日志", "说明路由跨公共层，需运行态目标身份")
blocked_read("marriage.ask.self-head", "marriage.ask", "read", "本人/对象头像与名字", "已请求17232但未渲染", "依赖共享头像与 Friend 候选")
blocked_read("marriage.ask.target-drop", "marriage.ask", "read", "好友下拉覆盖无候选/预选对象/切换对象", "模板隐藏", "MarriageDropBtn/DownDropBtn 业务 View 与 FriendModel 未接")
blocked_read("marriage.ask.ring-list", "marriage.ask", "read", "config_propose_cfg 戒指方案列表、选中、成本和称号", "MarriageAskItem 模板隐藏", "配置/共享物品格/选择状态未接")
blocked_write("marriage.ask.propose", "marriage.ask", "校验对象/方案/货币后17231求婚", "当前仅日志", "求婚与消费属于真实写事务，未授权")

page(
    "marriage.break-tips",
    "marriage",
    "MarriageBreakTipsView（17222离婚协商推送）",
    [
        ("profile", "state", "marriage.break-tips.profile"),
        ("agree", "button", "marriage.break-tips.agree"),
        ("refuse", "button", "marriage.break-tips.refuse"),
        ("close-as-refuse", "button", "marriage.break-tips.close"),
    ],
)
blocked_read("marriage.break-tips.profile", "marriage.break-tips", "read", "双方头像/战力与协商离婚文案", "仅 Generated Bind", "推送弹窗业务未接")
blocked_write("marriage.break-tips.agree", "marriage.break-tips", "17235同意离婚", "未接", "真实离婚事务未授权")
blocked_write("marriage.break-tips.refuse", "marriage.break-tips", "17235拒绝离婚", "未接", "真实回应事务未授权")
blocked_write("marriage.break-tips.close", "marriage.break-tips", "关闭等价拒绝并发17235 type=2", "未接", "关闭具有写语义，未授权")

page(
    "marriage.break",
    "marriage",
    "MarriageBreakView",
    [
        ("close", "button", "marriage.break.close"),
        ("profile", "state", "marriage.break.profile"),
        ("cost-state", "conditional-state", "marriage.break.cost"),
        ("peace", "button", "marriage.break.peace"),
        ("force", "button", "marriage.break.force"),
    ],
)
nrv("marriage.break.close", "marriage.break", "return", "普通关闭返回", "已绑定 Hide", "缺真实点击/遮罩/重开")
blocked_read("marriage.break.profile", "marriage.break", "read", "双方头像/名字", "模板隐藏不渲染", "共享头像/Friend 状态未接")
blocked_read("marriage.break.cost", "marriage.break", "read", "按离线48h和常量12切强制/免费离婚与成本", "未渲染", "需要 Friend 在线时间与 Goods 映射，均在禁止岛")
blocked_write("marriage.break.peace", "marriage.break", "17234 type=1协议离婚", "当前仅日志", "真实离婚写事务未授权")
blocked_write("marriage.break.force", "marriage.break", "17234 type=2强制离婚/可能消费", "当前仅日志", "破坏性且可能消费，未授权")

page(
    "marriage.com",
    "marriage",
    "MarriageComView",
    [
        ("close", "button", "marriage.com.close"),
        ("profile", "state", "marriage.com.profile"),
        ("hi", "conditional-button", "marriage.com.hi"),
        ("gift", "conditional-button", "marriage.com.gift"),
        ("ask", "conditional-button", "marriage.com.ask"),
        ("friend", "conditional-button", "marriage.com.friend"),
    ],
)
for suffix, legacy in [("close", "关闭"), ("profile", "头像/名字/公会/亲密度/宣言，标签当前老端隐藏")]:
    blocked_read(f"marriage.com.{suffix}", "marriage.com", "return" if suffix == "close" else "read", legacy, "仅 Generated Bind", "业务 View 未接管且依赖 Friend/Common")
blocked_read("marriage.com.hi", "marriage.com", "navigation", "好友态打招呼/聊天", "未接", "Chat/Friend 禁止岛")
blocked_write("marriage.com.gift", "marriage.com", "打开送花且按分支关注17201", "未接", "含真实关注写事务，未授权")
blocked_read("marriage.com.ask", "marriage.com", "navigation", "满足条件打开求婚", "未接", "条件态依赖 Friend/Role 婚姻状态")
blocked_write("marriage.com.friend", "marriage.com", "添加关注17201", "未接", "真实社交写事务未授权")

page(
    "marriage.dsgt",
    "marriage",
    "MarriageDsgtView",
    [
        ("close", "button", "marriage.dsgt.close"),
        ("progress", "state", "marriage.dsgt.progress"),
        ("list", "list", "marriage.dsgt.list"),
        ("locked-go-flower", "row-button", "marriage.dsgt.locked-go"),
        ("auto-take", "conditional-transaction", "marriage.dsgt.auto-take"),
    ],
)
nrv("marriage.dsgt.close", "marriage.dsgt", "return", "关闭返回", "已绑定 Hide", "缺真实点击与重开")
blocked_read("marriage.dsgt.progress", "marriage.dsgt", "read", "恩爱值到下一称号阈值进度", "未消费 Mate/config_love_dsgt_cfg", "表现接线缺失")
blocked_read("marriage.dsgt.list", "marriage.dsgt", "read", "10档称号列表，静态/动态称号、属性与解锁态", "模板隐藏", "DsgtModel/动态特效与列表业务未接")
blocked_read("marriage.dsgt.locked-go", "marriage.dsgt", "navigation", "未解锁行：已婚去送花，未婚确认后去寻缘", "列表项无业务 View", "依赖 Alert/Role 状态与目标弹窗")
blocked_write("marriage.dsgt.auto-take", "marriage.dsgt", "爱意达到阈值时自动发17236领取称号", "Controller 有 sender，但 View 未实现", "真实领取资产事务；本轮禁止自动发包")

page(
    "marriage.dun-luck",
    "marriage",
    "MarriageDunLuckView",
    [
        ("answers", "choice-list", "marriage.dun-luck.answers"),
        ("timer", "state", "marriage.dun-luck.timer"),
        ("submit", "button", "marriage.dun-luck.submit"),
        ("gray-state", "state", "marriage.dun-luck.gray"),
        ("close", "button", "marriage.dun-luck.close"),
    ],
)
blocked_read("marriage.dun-luck.answers", "marriage.dun-luck", "read", "克隆答案按钮并本地选中", "仅 Generated Bind", "依赖 BaseDungeon 问答状态")
blocked_read("marriage.dun-luck.timer", "marriage.dun-luck", "read", "答题倒计时及超时状态", "未接", "BaseDungeon/服务器时钟不在文件岛")
blocked_write("marriage.dun-luck.submit", "marriage.dun-luck", "61090提交当前答案", "未接", "真实副本答题写事务未授权")
blocked_read("marriage.dun-luck.gray", "marriage.dun-luck", "read", "提交后灰态并防重复", "未接", "需真实回包/超时证据")
blocked_read("marriage.dun-luck.close", "marriage.dun-luck", "return", "关闭答题窗", "未接", "关闭与倒计时/自动提交生命周期需 BaseDungeon 证据")

page(
    "marriage.dun-tips",
    "marriage",
    "MarriageDunTipsView",
    [
        ("profile", "state", "marriage.dun-tips.profile"),
        ("timer-ready", "state", "marriage.dun-tips.timer"),
        ("accept", "button", "marriage.dun-tips.accept"),
        ("refuse", "button", "marriage.dun-tips.refuse"),
        ("cancel", "button", "marriage.dun-tips.cancel"),
        ("close", "button", "marriage.dun-tips.close"),
    ],
)
blocked_read("marriage.dun-tips.profile", "marriage.dun-tips", "read", "发起方/被邀请方头像名字与主动/被动布局", "仅 Generated Bind", "BaseDungeon 邀请 View 未接")
blocked_read("marriage.dun-tips.timer", "marriage.dun-tips", "read", "准备态、倒计时与灰态", "未接", "需真实邀请生命周期")
for suffix, legacy in [
    ("accept", "61047同意邀请"),
    ("refuse", "61047拒绝邀请"),
    ("cancel", "61046取消自己邀请"),
    ("close", "按主动/被动身份取消或拒绝后关闭"),
]:
    blocked_write(f"marriage.dun-tips.{suffix}", "marriage.dun-tips", legacy, "未接", "真实副本邀请写事务未授权且 BaseDungeon 禁止岛")

page(
    "marriage.flower-tips",
    "marriage",
    "MarriageFlowerTipsView（22304收花推送）",
    [
        ("profile", "state", "marriage.flower-tips.profile"),
        ("go-gift", "button", "marriage.flower-tips.go"),
        ("thanks", "conditional-button", "marriage.flower-tips.thanks"),
        ("add-friend", "conditional-button", "marriage.flower-tips.friend"),
        ("close", "button", "marriage.flower-tips.close"),
    ],
)
blocked_read("marriage.flower-tips.profile", "marriage.flower-tips", "read", "送花者头像/名字/公会/礼物和文案", "仅 Generated Bind", "推送弹窗业务未接")
blocked_read("marriage.flower-tips.go", "marriage.flower-tips", "navigation", "前往 MarriageFlowerView 回礼", "未接", "目标参数与返回链缺失")
blocked_write("marriage.flower-tips.thanks", "marriage.flower-tips", "好友态发22305感谢", "未接", "真实感谢写事务未授权")
blocked_write("marriage.flower-tips.friend", "marriage.flower-tips", "同服非好友态添加好友", "未接", "Friend 写事务未授权且在禁止岛")
blocked_read("marriage.flower-tips.close", "marriage.flower-tips", "return", "关闭", "未接", "业务 View 未接管")

page(
    "marriage.flower",
    "marriage",
    "MarriageFlowerView",
    [
        ("close", "button", "marriage.flower.close"),
        ("help", "button", "marriage.flower.help"),
        ("target-drop", "dropdown", "marriage.flower.target"),
        ("profile", "state", "marriage.flower.profile"),
        ("flower-list", "list", "marriage.flower.list"),
        ("give-row", "row-button", "marriage.flower.give"),
        ("buy-row", "row-button", "marriage.flower.buy"),
    ],
)
nrv("marriage.flower.close", "marriage.flower", "return", "关闭返回", "已绑定 Hide", "缺真实点击/遮罩/重开")
nrv("marriage.flower.help", "marriage.flower", "navigation", "打开说明223", "当前仅日志", "公共说明路由未在文件岛接线")
blocked_read("marriage.flower.target", "marriage.flower", "read", "好友下拉含无对象/预选/切换", "模板隐藏", "FriendModel 与共享下拉业务未接")
blocked_read("marriage.flower.profile", "marriage.flower", "read", "对象头像、公会、亲密度/快加好友互斥", "未渲染", "依赖 Friend/Common")
blocked_read("marriage.flower.list", "marriage.flower", "read", "6类鲜花按持有量/商品排序，显示亲密/魅力/名誉", "模板隐藏", "Bag/Goods/Shop 禁止岛依赖未接")
blocked_write("marriage.flower.give", "marriage.flower", "有花时选数量并发22301", "未接", "真实消耗鲜花写事务未授权")
blocked_read("marriage.flower.buy", "marriage.flower", "navigation", "无花时打开购买链", "未接", "Shop/Bag 禁止岛且可能消费")

page(
    "marriage.flow",
    "marriage",
    "MarriageFlowView",
    [
        ("close", "button", "marriage.flow.close"),
        ("list", "list", "marriage.flow.list"),
        ("go-main", "conditional-row-button", "marriage.flow.go-main"),
        ("go-flower", "conditional-row-button", "marriage.flow.go-flower"),
        ("go-ask-list", "conditional-row-button", "marriage.flow.go-ask-list"),
    ],
)
nrv("marriage.flow.close", "marriage.flow", "return", "关闭", "已绑定 Hide", "缺真实点击与重开")
blocked_read("marriage.flow.list", "marriage.flow", "read", "MarriageFlowCfg 动态列表", "模板隐藏", "配置与 MarriageFlowItem 业务未接")
for suffix, legacy in [("go-main", "按流类型去主婚恋页"), ("go-flower", "按流类型去送花"), ("go-ask-list", "按流类型去寻缘")]:
    blocked_read(f"marriage.flow.{suffix}", "marriage.flow", "navigation", legacy, "列表项无业务 View", "目标路由/参数/返回链未接")

page(
    "marriage.fore-show",
    "marriage",
    "MarriageForeShowView",
    [
        ("countdown", "state", "marriage.fore-show.countdown"),
        ("open", "button", "marriage.fore-show.open"),
        ("close", "button", "marriage.fore-show.close"),
        ("background", "background", "marriage.fore-show.background"),
    ],
)
for suffix, legacy in [("countdown", "等级预告倒计时/人物分支"), ("open", "点击进入婚恋"), ("close", "关闭"), ("background", "背景点击关闭")]:
    blocked_read(f"marriage.fore-show.{suffix}", "marriage.fore-show", "navigation" if suffix == "open" else ("return" if suffix in {"close", "background"} else "read"), legacy, "仅 Generated Bind", "等级弹窗编排/业务 View 未接管")

page(
    "marriage.gift-tips",
    "marriage",
    "MarriageGiftTipsView",
    [
        ("message", "state", "marriage.gift-tips.message"),
        ("go", "button", "marriage.gift-tips.go"),
        ("cancel", "button", "marriage.gift-tips.cancel"),
        ("close", "button", "marriage.gift-tips.close"),
    ],
)
blocked_read("marriage.gift-tips.message", "marriage.gift-tips", "read", "礼包邀请提示", "仅 Generated Bind", "业务 View 未接")
blocked_read("marriage.gift-tips.go", "marriage.gift-tips", "navigation", "前往 MarriageBaseView 第3索引锦囊", "未接", "缺目标页参数/身份")
for suffix in ["cancel", "close"]:
    blocked_read(f"marriage.gift-tips.{suffix}", "marriage.gift-tips", "return", "关闭提示", "未接", "业务 View 未接管")

page(
    "marriage.honour",
    "marriage",
    "MarriageHonourView（角色页名誉入口）",
    [
        ("close", "button", "marriage.honour.close"),
        ("fame", "state", "marriage.honour.fame"),
        ("list", "list", "marriage.honour.list"),
        ("go-flower", "button", "marriage.honour.go"),
    ],
)
nrv("marriage.honour.close", "marriage.honour", "return", "关闭", "MarriageHonourFlow 已绑定", "缺真实 Prefab 点击/遮罩/重开")
nrv("marriage.honour.fame", "marriage.honour", "read", "22303名誉值", "MarriageHonourFlow 读取 Model", "缺同账号运行态状态证据")
nrv("marriage.honour.list", "marriage.honour", "read", "config_fame_lv 纵向档位与解锁态", "已克隆 MarriageHonourItemBind", "缺 ScrollRect 结构、真实拖动、末项可达、长文案和视觉证据")
nrv("marriage.honour.go", "marriage.honour", "navigation", "前往送花", "已调用 MarriageFlow.OpenSubDeferred", "缺目标身份、参数、返回与热重开")

page(
    "marriage.issue",
    "marriage",
    "MarriageIssueView",
    [
        ("profile", "state", "marriage.issue.profile"),
        ("edit", "input", "marriage.issue.edit"),
        ("random", "text-button", "marriage.issue.random"),
        ("tag", "text-button", "marriage.issue.tag"),
        ("publish", "button", "marriage.issue.publish"),
        ("cancel", "button", "marriage.issue.cancel"),
        ("close", "button", "marriage.issue.close"),
    ],
)
for suffix, legacy in [("profile", "本人头像/名字/公会/标签"), ("edit", "宣言输入"), ("random", "随机宣言")]:
    blocked_read(f"marriage.issue.{suffix}", "marriage.issue", "read", legacy, "仅 Generated Bind", "业务 View、输入与共享头像未接")
blocked_read("marriage.issue.tag", "marriage.issue", "navigation", "打开 MarriageTagView", "未接", "老端标签编辑模块未加载，当前产品死链；不得臆造恢复")
blocked_write("marriage.issue.publish", "marriage.issue", "过滤敏感词/空值/冷却后17202发布", "Controller sender存在但 View 未接", "真实发布写事务未授权")
for suffix in ["cancel", "close"]:
    blocked_read(f"marriage.issue.{suffix}", "marriage.issue", "return", "关闭不提交", "未接", "业务 View 未接管")

page(
    "marriage.record-com",
    "marriage",
    "MarriageRecordComView（Self/Other 两种参数）",
    [
        ("close", "button", "marriage.record-com.close"),
        ("self-list", "conditional-list", "marriage.record-com.self"),
        ("other-list", "conditional-list", "marriage.record-com.other"),
        ("empty", "state", "marriage.record-com.empty"),
        ("row-flower", "row-button", "marriage.record-com.flower"),
        ("row-menu", "row-button", "marriage.record-com.menu"),
    ],
)
for suffix, legacy in [("close", "关闭"), ("self", "请求17200 page=2我的关注"), ("other", "请求17200 page=3粉丝"), ("empty", "空表空态"), ("flower", "行送花"), ("menu", "行角色菜单")]:
    blocked_read(f"marriage.record-com.{suffix}", "marriage.record-com", "return" if suffix == "close" else ("navigation" if suffix in {"flower", "menu"} else "read"), legacy, "仅 Generated Bind", "列表/行项目/参数化业务 View 未接，依赖 Friend/Common")

page(
    "marriage.record-flower",
    "marriage",
    "MarriageRecordFlowerView",
    [
        ("close", "button", "marriage.record-flower.close"),
        ("list", "list", "marriage.record-flower.list"),
        ("empty", "state", "marriage.record-flower.empty"),
        ("thanks", "row-button", "marriage.record-flower.thanks"),
        ("gift", "row-button", "marriage.record-flower.gift"),
    ],
)
blocked_read("marriage.record-flower.close", "marriage.record-flower", "return", "关闭", "仅 Generated Bind", "业务 View 未接")
blocked_read("marriage.record-flower.list", "marriage.record-flower", "read", "22302全量送花记录可滚动", "未请求/未铺列表", "列表业务未接")
blocked_read("marriage.record-flower.empty", "marriage.record-flower", "read", "空表空态", "未接", "业务 View 未接")
blocked_write("marriage.record-flower.thanks", "marriage.record-flower", "未感谢行发22305并即时灰化", "未接", "真实感谢写事务未授权")
blocked_read("marriage.record-flower.gift", "marriage.record-flower", "navigation", "按记录对象回赠鲜花", "未接", "目标参数/跨服/Friend 条件未接")

page(
    "marriage.role-menu",
    "marriage",
    "MarriageRoleMenuView",
    [
        ("outside-close", "background", "marriage.role-menu.outside"),
        ("look", "button", "marriage.role-menu.look"),
        ("chat", "conditional-button", "marriage.role-menu.chat"),
        ("ask", "conditional-button", "marriage.role-menu.ask"),
        ("friend", "conditional-button", "marriage.role-menu.friend"),
    ],
)
for suffix, legacy in [("outside", "点击滚动面外/舞台关闭"), ("look", "查看资料"), ("chat", "好友态私聊"), ("ask", "符合条件求婚")]:
    blocked_read(f"marriage.role-menu.{suffix}", "marriage.role-menu", "return" if suffix == "outside" else "navigation", legacy, "仅 Generated Bind", "依赖 Friend/Chat/资料页，均在禁止岛")
blocked_write("marriage.role-menu.friend", "marriage.role-menu", "非好友态添加好友", "未接", "真实 Friend 写事务未授权且文件岛禁止")

page(
    "marriage.success",
    "marriage",
    "MarriageSuccessView",
    [
        ("profile", "state", "marriage.success.profile"),
        ("go-banquet", "button", "marriage.success.go"),
        ("close", "button", "marriage.success.close"),
    ],
)
blocked_read("marriage.success.profile", "marriage.success", "read", "双方头像/战力/结婚结果演出", "仅 Generated Bind", "结果弹窗业务/特效未接")
blocked_write("marriage.success.go", "marriage.success", "打开婚宴并发17235 type=1", "未接", "真实回应/婚宴事务未授权")
blocked_write("marriage.success.close", "marriage.success", "关闭并发17235 type=2", "未接", "关闭具有写语义，未授权")

page(
    "marriage.shared.friend-item",
    "marriage",
    "MarriageFriendItem.prefab",
    [
        ("identity", "component", "marriage.shared.friend-item.identity"),
        ("states", "state-matrix", "marriage.shared.friend-item.states"),
        ("flirt", "button", "marriage.shared.friend-item.flirt"),
        ("touch", "click-area", "marriage.shared.friend-item.touch"),
    ],
)
blocked_read("marriage.shared.friend-item.identity", "marriage.shared.friend-item", "read", "大厅/表白记录共用候选项", "独立 Prefab 仅 MarriageFriendItemBind", "缺业务 View/GUID 实例链与消费者运行抽查")
blocked_read("marriage.shared.friend-item.states", "marriage.shared.friend-item", "read", "本人/他人、在线/离线、标签空/有、好友/非好友、VIP态", "未渲染", "组件状态矩阵未实现且依赖 Friend/Common")
blocked_read("marriage.shared.friend-item.flirt", "marriage.shared.friend-item", "navigation", "本人提示，其他人打开 MarriageComView", "无业务脚本", "共享项点击未接")
blocked_read("marriage.shared.friend-item.touch", "marriage.shared.friend-item", "read", "老端 touch_gp 当前仅拦本人，其余 FriendMenu 调用已注释", "无业务脚本", "需真实老端确认当前是否死点击面，禁止臆造")

page(
    "marriage.shared.drop-btn",
    "marriage",
    "MarriageDropBtn.prefab",
    [
        ("identity", "component", "marriage.shared.drop-btn.identity"),
        ("toggle", "button", "marriage.shared.drop-btn.toggle"),
        ("options", "list", "marriage.shared.drop-btn.options"),
        ("states", "state-matrix", "marriage.shared.drop-btn.states"),
    ],
)
blocked_read("marriage.shared.drop-btn.identity", "marriage.shared.drop-btn", "read", "Ask/Flower/Dungeon 三类宿主共享下拉", "独立 Prefab 仅 MarriageDropBtnBind", "缺业务 View 与消费者实例链")
blocked_read("marriage.shared.drop-btn.toggle", "marriage.shared.drop-btn", "read", "点击展开/收起并旋转箭头", "未接", "共享下拉业务未实现")
blocked_read("marriage.shared.drop-btn.options", "marriage.shared.drop-btn", "read", "选项列表、当前文案、点击回调", "模板存在但无业务", "缺真实滚动/裁剪/选中")
blocked_read("marriage.shared.drop-btn.states", "marriage.shared.drop-btn", "read", "空/1/多项、长短名字、预选、展开/收起、不同宿主", "未接", "状态矩阵与代表宿主抽查缺失")


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


legacy_files = sorted(LEGACY.glob("*.ts"))
unity_files = sorted((REPO / "Assets/Scripts/Module/Core/Marriage").rglob("*.cs"))
generated_files = sorted((REPO / "Assets/Scripts/Generated/UI/Marriage").glob("*Bind.cs"))
prefab_files = sorted((REPO / "Assets/Prefabs/UI/Marriage").glob("*.prefab"))

inventory = {
    "recorded_at": "2026-08-09T00:00:00+08:00",
    "scope": "static-only; no Unity, browser, account mutation, packet send, build or deployment",
    "legacy": {
        "root": str(LEGACY),
        "ts_count": len(legacy_files),
        "files": [{"path": str(p), "sha256": sha256(p)} for p in legacy_files],
    },
    "unity": {
        "business_cs_count": len(unity_files),
        "generated_bind_count": len(generated_files),
        "prefabs": [{"path": str(p.relative_to(REPO)), "sha256": sha256(p)} for p in prefab_files],
        "business_files": [str(p.relative_to(REPO)) for p in unity_files],
        "generated_binds": [p.stem for p in generated_files],
        "module_top_level_windows": [
            "MarriageAskListView", "MarriageAskTipsView", "MarriageAskView", "MarriageBaseView",
            "MarriageBreakTipsView", "MarriageBreakView", "MarriageComView", "MarriageDsgtView",
            "MarriageDunLuckView", "MarriageDunTipsView", "MarriageFlowerTipsView", "MarriageFlowerView",
            "MarriageFlowView", "MarriageForeShowView", "MarriageGiftTipsView", "MarriageHonourView",
            "MarriageIssueView", "MarriageRecordComView", "MarriageRecordFlowerView", "MarriageRoleMenuView",
            "MarriageSuccessView",
        ],
        "prefab_default_active_top_level": ["MarriageAskListView"],
        "prefab_default_inactive_top_level_count": 20,
        "business_view_attached": [
            "MarriageAskListView", "MarriageAskView", "MarriageBaseView", "MarriageBreakView",
            "MarriageDsgtView", "MarriageFlowerView", "MarriageFlowView",
        ],
        "special_flow_bound": ["MarriageHonourView"],
        "generated_bind_only_top_level": [
            "MarriageAskTipsView", "MarriageBreakTipsView", "MarriageComView", "MarriageDunLuckView",
            "MarriageDunTipsView", "MarriageFlowerTipsView", "MarriageForeShowView", "MarriageGiftTipsView",
            "MarriageIssueView", "MarriageRecordComView", "MarriageRecordFlowerView", "MarriageRoleMenuView",
            "MarriageSuccessView",
        ],
    },
    "protocol_boundary": {
        "read_queries_restored_on_open": [17200, 17210, 17232, 17238],
        "dead_or_defensive_only": [17212, 17245, 17246],
        "write_transactions_blocked": [
            17201, 17202, 17211, 17213, 17223, 17231, 17234, 17235, 17236, 17237, 17239, 17240,
            17295, 17297, 22301, 22305, 61021, 61046, 61047, 61090,
        ],
        "notes": [
            "不发送任何真实协议；清单仅由老端调用点、Unity Controller/Proto 与当前服务端负约束静态调和。",
            "17245 服务端 handler 已注释；Unity 不得恢复自动匹配发送。",
            "求婚、结婚、离婚、送礼、赠花、领奖、购买、添加好友、进入副本均保留 blocked。",
        ],
    },
}

manifest = {
    "route": "marriage",
    "baseline": {
        "legacy_source": "E:/GitProject/yu_client/h5/src/marriage",
        "unity_prefab": "Assets/Prefabs/UI/Marriage/MarriageModule.prefab",
        "scope": "static-only",
    },
    "nodes": nodes,
}

node_ids = [node["id"] for node in nodes]
node_id_set = set(node_ids)
if len(node_ids) != len(node_id_set):
    raise ValueError("duplicate route node id")
children: dict[str, set[str]] = {node_id: set() for node_id in node_ids}
root_ids: list[str] = []
for node in nodes:
    parent = node.get("parent")
    if parent is None:
        root_ids.append(node["id"])
    else:
        if parent not in node_id_set:
            raise ValueError(f"missing parent {parent} for {node['id']}")
        children[parent].add(node["id"])
if root_ids != ["marriage"]:
    raise ValueError(f"unexpected roots: {root_ids}")
for node in nodes:
    if node["type"] != "page":
        continue
    inventory_children = [item["child"] for item in node["control_inventory"]]
    if len(inventory_children) != len(set(inventory_children)):
        raise ValueError(f"duplicate control child in {node['id']}")
    if set(inventory_children) != children[node["id"]]:
        raise ValueError(
            f"control inventory mismatch in {node['id']}: "
            f"inventory={sorted(inventory_children)}, children={sorted(children[node['id']])}"
        )
leaf_ids = {node_id for node_id, child_ids in children.items() if not child_ids}
result_ids = [row["id"] for row in results]
if len(result_ids) != len(set(result_ids)):
    raise ValueError("duplicate result id")
if set(result_ids) != leaf_ids:
    raise ValueError(f"result/leaf mismatch: result_only={sorted(set(result_ids) - leaf_ids)}, leaf_only={sorted(leaf_ids - set(result_ids))}")
for row in results:
    if row["status"] not in {"blocked", "needs-runtime-verify"}:
        raise ValueError(f"forbidden leaf status: {row['id']}={row['status']}")
    required_reason = "blocked_reason" if row["status"] == "blocked" else "runtime_gap"
    if not row.get(required_reason):
        raise ValueError(f"missing {required_reason}: {row['id']}")

static_validation = {
    "schema_target": 6,
    "route": "marriage",
    "checks": {
        "unique_node_ids": True,
        "single_root": root_ids == ["marriage"],
        "all_parents_exist": True,
        "page_control_inventory_matches_direct_children": True,
        "results_exactly_cover_leaves": True,
        "leaf_statuses_only_blocked_or_needs_runtime_verify": True,
        "no_done_leaves": True,
    },
    "metrics": {"done_leaf_count": 0},
}

controller_text = (REPO / "Assets/Scripts/Module/Core/Marriage/MarriageController.cs").read_text(encoding="utf-8-sig")
view_text = {
    name: (REPO / f"Assets/Scripts/Module/Core/Marriage/Views/{name}.cs").read_text(encoding="utf-8-sig")
    for name in ["MarriageAskView", "MarriageFriendView", "MarriageGiftView", "MarriageMainView", "MarriageRingView"]
}
source_checks = {
    "controller_declares_request_personals_list": "public void RequestPersonalsList(int page)" in controller_text,
    "controller_declares_request_ring_info": "public void RequestRingInfo()" in controller_text,
    "controller_declares_request_my_mate": "public void RequestMyMate()" in controller_text,
    "controller_declares_request_gift_info": "public void RequestGiftInfo()" in controller_text,
    "ask_requests_my_mate": "MarriageController.Instance.RequestMyMate();" in view_text["MarriageAskView"],
    "friend_requests_first_personals_page": "MarriageController.Instance.RequestPersonalsList(1);" in view_text["MarriageFriendView"],
    "gift_requests_gift_and_mate": all(
        call in view_text["MarriageGiftView"]
        for call in ["MarriageController.Instance.RequestGiftInfo();", "MarriageController.Instance.RequestMyMate();"]
    ),
    "main_requests_mate_and_gift": all(
        call in view_text["MarriageMainView"]
        for call in ["MarriageController.Instance.RequestMyMate();", "MarriageController.Instance.RequestGiftInfo();"]
    ),
    "ring_requests_ring_info": "MarriageController.Instance.RequestRingInfo();" in view_text["MarriageRingView"],
    "ring_stop_has_no_fake_click_binding": "BindBtn(_btn_stop" not in view_text["MarriageRingView"],
}
failed_source_checks = sorted(name for name, ok in source_checks.items() if not ok)
if failed_source_checks:
    raise ValueError(f"source checks failed: {failed_source_checks}")
static_validation["checks"].update(source_checks)
static_validation["modified_view_sha256"] = {
    name: sha256(REPO / f"Assets/Scripts/Module/Core/Marriage/Views/{name}.cs")
    for name in view_text
}

OUT.mkdir(parents=True, exist_ok=True)
(OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "static-source-inventory.json").write_text(json.dumps(inventory, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "static-validation.json").write_text(json.dumps(static_validation, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

blocked_count = sum(1 for x in results if x["status"] == "blocked")
nrv_count = sum(1 for x in results if x["status"] == "needs-runtime-verify")
page_count = sum(1 for x in nodes if x["type"] == "page")
lines = [
    "# Marriage UI 静态路由矩阵",
    "",
    "> 仅静态盘点；未启动 Unity/浏览器，未登录账号，未发送任何协议，未执行任何账号写事务。",
    "",
    f"- 节点：{len(nodes)}（页面 {page_count}，叶 {len(results)}）",
    f"- 叶状态：blocked={blocked_count}，needs-runtime-verify={nrv_count}，done=0",
    f"- 老端 TS：{len(legacy_files)}；Unity Marriage 业务 C#：{len(unity_files)}；Generated Bind：{len(generated_files)}",
    "- 顶层窗口：21；独立共享 Prefab：MarriageFriendItem / MarriageDropBtn",
    "",
    "| 叶 ID | 状态 | 原因/运行缺口 |",
    "|---|---|---|",
]
for row in results:
    reason = row.get("blocked_reason") or row.get("runtime_gap") or ""
    lines.append(f"| `{row['id']}` | {row['status']} | {reason.replace('|', '/')} |")
(OUT / "route-matrix.md").write_text("\n".join(lines) + "\n", encoding="utf-8")

print(json.dumps({
    "nodes": len(nodes),
    "pages": page_count,
    "leaves": len(results),
    "blocked": blocked_count,
    "needs_runtime_verify": nrv_count,
}, ensure_ascii=False))
