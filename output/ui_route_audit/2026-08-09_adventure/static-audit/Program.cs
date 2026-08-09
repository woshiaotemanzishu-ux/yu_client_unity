using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: AdventureStaticAudit <repo-root> <output-dir>");
    return 2;
}

string repo = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var utf8 = new UTF8Encoding(false);
const string route = "mainui.adventure";
const string evidence = "output/ui_route_audit/2026-08-09_adventure/static-audit.md";
var nodes = new List<Node>();

void Page(string id, string? parent, params Control[] controls)
    => nodes.Add(new Node(id, "page", "read-only", parent, controls, null));
void Leaf(string id, string parent, string type, string risk, string note)
    => nodes.Add(new Node(id, type, risk, parent, null, note));
Control C(string id, string kind, string child) => new(id, kind, child);

Page(route, null,
    C("entry", "entry", route + ".entry"), C("shell", "page", route + ".shell"),
    C("main", "page", route + ".main"),
    C("shared", "dependency", route + ".shared"), C("lifecycle", "lifecycle", route + ".lifecycle"));

Page(route + ".entry", route,
    C("icon-42701", "navigation", route + ".entry.icon-42701"),
    C("icon-42702", "conditional-navigation", route + ".entry.icon-42702"),
    C("activity-condition", "conditional", route + ".entry.activity-condition"),
    C("board-read", "protocol-read", route + ".entry.board-read"),
    C("mainui-visibility", "conditional", route + ".entry.mainui-visibility"));
Leaf(route + ".entry.icon-42701", route + ".entry", "navigation", "read-only", "当前 config_adventure_kv[12] 的七天入口均为 42701。");
Leaf(route + ".entry.icon-42702", route + ".entry", "navigation", "read-only", "兼容另一版本的 42702 图标身份，当前配置不可达。");
Leaf(route + ".entry.activity-condition", route + ".entry", "read", "read-only", "GodEquipBuildView 开放且 42700 stage/time 窗口有效。");
Leaf(route + ".entry.board-read", route + ".entry", "read", "read-only", "42701 六个只读棋盘状态字段。");
Leaf(route + ".entry.mainui-visibility", route + ".entry", "read", "read-only", "主界面活动图标布局、收纳盒与玩家实际命中。");

Page(route + ".shell", route,
    C("title", "visual", route + ".shell.title"), C("help", "navigation", route + ".shell.help"),
    C("return", "return", route + ".shell.return"), C("money", "state", route + ".shell.money"),
    C("background", "visual", route + ".shell.background"));
Leaf(route + ".shell.title", route + ".shell", "read", "read-only", "天天冒险标题与 adventure.uittmx_019。");
Leaf(route + ".shell.help", route + ".shell", "navigation", "read-only", "instruction_list=42700 的说明弹窗。");
Leaf(route + ".shell.return", route + ".shell", "return", "read-only", "BaseWindow 返回按钮使用 uittmx_003。");
Leaf(route + ".shell.money", route + ".shell", "read", "read-only", "money_list=[2,1,0] 顶部货币栏。");
Leaf(route + ".shell.background", route + ".shell", "read", "read-only", "共享大窗外壳、遮罩与层级。");

Page(route + ".main", route,
    C("visual", "visual", route + ".main.visual"), C("board", "page", route + ".main.board"),
    C("reward-preview", "page", route + ".main.reward-preview"), C("action", "page", route + ".main.action"),
    C("ticket-toggle", "toggle", route + ".main.ticket-toggle"), C("ticket-state", "state", route + ".main.ticket-state"),
    C("cost-state", "state", route + ".main.cost-state"), C("reset-count", "state", route + ".main.reset-count"),
    C("reset-timer", "timer", route + ".main.reset-timer"), C("suit", "navigation", route + ".main.suit"),
    C("shop-open", "navigation", route + ".main.shop-open"), C("halo", "conditional-navigation", route + ".main.halo"),
    C("ad", "conditional-transaction", route + ".main.ad"), C("forward-red", "state", route + ".main.forward-red"),
    C("shop-red", "state", route + ".main.shop-red"));
Leaf(route + ".main.visual", route + ".main", "read", "read-only", "主背景、棋盘、按钮、文字和页面根几何。");
Page(route + ".main.ticket-toggle", route + ".main",
    C("off", "toggle-state", route + ".main.ticket-toggle.off"),
    C("on", "toggle-state", route + ".main.ticket-toggle.on"));
Leaf(route + ".main.ticket-toggle.off", route + ".main.ticket-toggle", "tab", "read-only", "冒险券抵扣关闭态、点击命中与有效成本恢复。");
Leaf(route + ".main.ticket-toggle.on", route + ".main.ticket-toggle", "tab", "read-only", "冒险券抵扣开启态、互斥皮肤与有效成本扣减。");
Leaf(route + ".main.ticket-state", route + ".main", "read", "read-only", "36255038 冒险券数量与每张抵扣 15 勾玉。");
Page(route + ".main.cost-state", route + ".main",
    C("free", "conditional-state", route + ".main.cost-state.free"),
    C("ticket", "conditional-state", route + ".main.cost-state.ticket"),
    C("paid", "conditional-state", route + ".main.cost-state.paid"),
    C("insufficient", "conditional-state", route + ".main.cost-state.insufficient"));
Leaf(route + ".main.cost-state.free", route + ".main.cost-state", "read", "read-only", "免费前进/重置时按钮、成本图标和文案。");
Leaf(route + ".main.cost-state.ticket", route + ".main.cost-state", "read", "read-only", "冒险券抵扣后的券数、抵扣文案与有效成本。");
Leaf(route + ".main.cost-state.paid", route + ".main.cost-state", "read", "read-only", "付费前进/重置的货币图标、准确价格和颜色。");
Leaf(route + ".main.cost-state.insufficient", route + ".main.cost-state", "read", "read-only", "成本不足颜色、按钮状态与不误发送事务。");
Page(route + ".main.reset-count", route + ".main",
    C("server-free", "state", route + ".main.reset-count.server-free"),
    C("default", "state", route + ".main.reset-count.default"),
    C("vip", "conditional-state", route + ".main.reset-count.vip"),
    C("remaining", "state", route + ".main.reset-count.remaining"),
    C("next-vip", "conditional-navigation", route + ".main.reset-count.next-vip"));
Leaf(route + ".main.reset-count.server-free", route + ".main.reset-count", "read", "read-only", "42701 free_reset_times 的服务端额外重置次数。");
Leaf(route + ".main.reset-count.default", route + ".main.reset-count", "read", "read-only", "VIP 配置 num_type=427/42701 的默认重置次数。");
Leaf(route + ".main.reset-count.vip", route + ".main.reset-count", "read", "read-only", "当前 VIP 特权额外重置次数。");
Leaf(route + ".main.reset-count.remaining", route + ".main.reset-count", "read", "read-only", "服务端额外+默认+VIP-left_times 的剩余重置次数及居中布局。");
Leaf(route + ".main.reset-count.next-vip", route + ".main.reset-count", "navigation", "read-only", "下一 VIP 档额外次数提示与前往充值链。");
Page(route + ".main.reset-timer", route + ".main",
    C("countdown", "timer", route + ".main.reset-timer.countdown"),
    C("resetting", "conditional-state", route + ".main.reset-timer.resetting"));
Leaf(route + ".main.reset-timer.countdown", route + ".main.reset-timer", "read", "read-only", "每日零点倒计时格式、逐秒更新与关页停止。");
Leaf(route + ".main.reset-timer.resetting", route + ".main.reset-timer", "read", "read-only", "零点前后 15 秒显示重置中并禁止提交。");
Leaf(route + ".main.suit", route + ".main", "navigation", "read-only", "OpenFun 125 神装打造跳转。");
Page(route + ".main.shop-open", route + ".main",
    C("button", "navigation", route + ".main.shop-open.button"),
    C("popup", "popup", route + ".shop"));
Leaf(route + ".main.shop-open.button", route + ".main.shop-open", "navigation", "read-only", "点击商店按钮打开 AdventureShopView 并保持主棋盘。");
Page(route + ".main.halo", route + ".main",
    C("feature-hidden", "conditional-state", route + ".main.halo.feature-hidden"),
    C("activated", "conditional-state", route + ".main.halo.activated"),
    C("unactivated", "conditional-navigation", route + ".main.halo.unactivated"),
    C("merge-hidden", "conditional-state", route + ".main.halo.merge-hidden"));
Leaf(route + ".main.halo.feature-hidden", route + ".main.halo", "read", "read-only", "光环功能总开关关闭时 _box_halo 整体隐藏且无点击面。");
Leaf(route + ".main.halo.activated", route + ".main.halo", "read", "read-only", "功能开放且已有光环时 box 显示、tips 隐藏、merge 隐藏。");
Leaf(route + ".main.halo.unactivated", route + ".main.halo", "navigation", "read-only", "功能开放但未激活光环时 tips 显示，点击跳 HaloMainView。");
Leaf(route + ".main.halo.merge-hidden", route + ".main.halo", "read", "read-only", "_img_merge_count 两个老端分支都隐藏且不残留数字。");
Page(route + ".main.ad", route + ".main",
    C("hidden", "conditional-state", route + ".main.ad.hidden"),
    C("visible", "conditional-state", route + ".main.ad.visible"),
    C("confirm", "popup", route + ".main.ad.confirm"),
    C("watch", "transaction", route + ".main.ad.watch"));
Leaf(route + ".main.ad.hidden", route + ".main.ad", "read", "read-only", "广告配置未开或已观看时按钮隐藏且不占点击面。");
Leaf(route + ".main.ad.visible", route + ".main.ad", "read", "read-only", "广告配置开放且未观看时按钮、文案与红点可见。");
Leaf(route + ".main.ad.confirm", route + ".main.ad", "navigation", "read-only", "观看广告确认弹窗的文案、取消与关闭链。");
Leaf(route + ".main.ad.watch", route + ".main.ad", "transaction", "destructive-write", "观看广告后增加免费次数并即时刷新。");
Leaf(route + ".main.forward-red", route + ".main", "read", "read-only", "免费前进次数红点。");
Leaf(route + ".main.shop-red", route + ".main", "read", "read-only", "存在未购免费商品时的商店红点。");

var boardControls = new List<Control>();
for (int i = 0; i <= 30; i++) boardControls.Add(C($"slot-{i:00}", "board-cell", $"{route}.main.board.slot-{i:00}"));
boardControls.Add(C("state-matrix", "state-matrix", route + ".main.board.state-matrix"));
boardControls.Add(C("position", "state", route + ".main.board.position"));
boardControls.Add(C("role-model", "model", route + ".main.board.role-model"));
boardControls.Add(C("result-movement", "animation", route + ".main.board.result-movement"));
boardControls.Add(C("result-dice", "effect", route + ".main.board.result-dice"));
boardControls.Add(C("result-step", "state", route + ".main.board.result-step"));
boardControls.Add(C("result-box", "animation", route + ".main.board.result-box"));
boardControls.Add(C("result-crit", "effect", route + ".main.board.result-crit"));
boardControls.Add(C("result-fly", "animation", route + ".main.board.result-fly"));
boardControls.Add(C("result-reward", "popup", route + ".main.board.result-reward"));
Page(route + ".main.board", route + ".main", boardControls.ToArray());
for (int i = 0; i <= 30; i++)
{
    string slot = $"{route}.main.board.slot-{i:00}";
    Page(slot, route + ".main.board",
        C("state", "conditional-state", slot + ".state"),
        C("detail", "conditional-popup", slot + ".detail"));
    string type = i == 0 ? "起点" : i == 30 ? "终极" : new[] { 4, 8, 12, 16, 20, 25 }.Contains(i) ? "高级" : "普通";
    Leaf(slot + ".state", slot, "read", "read-only", $"当前配置棋盘位置 {i} 的{type}格、未到达/已开箱皮肤与位置几何。");
    if (i == 30)
    {
        Page(slot + ".detail", slot,
            C("reward-01", "shared-item", slot + ".detail.reward-01"),
            C("reward-02", "shared-item", slot + ".detail.reward-02"));
        for (int reward = 1; reward <= 2; reward++)
        {
            string rewardItem = $"{slot}.detail.reward-{reward:00}";
            Page(rewardItem, slot + ".detail",
                C("identity", "shared-item", rewardItem + ".identity"),
                C("detail", "popup", rewardItem + ".detail"));
            Leaf(rewardItem + ".identity", rewardItem, "read", "read-only", $"终极格第 {reward} 个奖励的图标、数量、品质与横排位置。");
            Leaf(rewardItem + ".detail", rewardItem, "navigation", "read-only", $"终极格第 {reward} 个奖励的 ItemTips 身份、内容与关闭链。");
        }
    }
    else
    {
        Leaf(slot + ".detail", slot, "read", "read-only", $"当前配置位置 {i} 无终极多奖励/特殊单物品详情，不误生成可点击格。");
    }
}
Page(route + ".main.board.state-matrix", route + ".main.board",
    C("common-unreached", "conditional-state", route + ".main.board.state-matrix.common-unreached"),
    C("common-opened", "conditional-state", route + ".main.board.state-matrix.common-opened"),
    C("high-unreached", "conditional-state", route + ".main.board.state-matrix.high-unreached"),
    C("high-opened", "conditional-state", route + ".main.board.state-matrix.high-opened"),
    C("ultimate-unreached", "conditional-state", route + ".main.board.state-matrix.ultimate-unreached"),
    C("ultimate-opened", "conditional-state", route + ".main.board.state-matrix.ultimate-opened"),
    C("special-unreachable", "conditional-state", route + ".main.board.state-matrix.special-unreachable"));
Leaf(route + ".main.board.state-matrix.common-unreached", route + ".main.board.state-matrix", "read", "read-only", "普通格未到达 box_1 与 0.6 缩放。");
Leaf(route + ".main.board.state-matrix.common-opened", route + ".main.board.state-matrix", "read", "read-only", "普通格已到达 box_1_1 与开箱后状态。");
Leaf(route + ".main.board.state-matrix.high-unreached", route + ".main.board.state-matrix", "read", "read-only", "高级格未到达 box_2、uittmx_002b 与 0.7 缩放。");
Leaf(route + ".main.board.state-matrix.high-opened", route + ".main.board.state-matrix", "read", "read-only", "高级格已到达 box_2_1 与开箱后状态。");
Leaf(route + ".main.board.state-matrix.ultimate-unreached", route + ".main.board.state-matrix", "read", "read-only", "终极格 box_3、124x97 与两奖励横排预览。");
Leaf(route + ".main.board.state-matrix.ultimate-opened", route + ".main.board.state-matrix", "read", "read-only", "终极格到达后的奖励/开箱时序与终点重置切换。");
Leaf(route + ".main.board.state-matrix.special-unreachable", route + ".main.board.state-matrix", "read", "read-only", "当前 kv[10]=[]，特殊单物品格为 hard-negative，不虚构可达状态。");
Leaf(route + ".main.board.position", route + ".main.board", "read", "read-only", "42701 location 到 31 个具名棋盘坐标的映射。");
Leaf(route + ".main.board.role-model", route + ".main.board", "read", "read-only", "主角服装、武器、翅膀、头饰、背饰及朝向。");
Leaf(route + ".main.board.result-movement", route + ".main.board", "read", "read-only", "42702 steps 的骰子数字、逐格移动与 idle/run 生命周期。");
Leaf(route + ".main.board.result-dice", route + ".main.board", "read", "read-only", "骰子 UI_2001 的归属、双时间点动态与关闭清理。");
Leaf(route + ".main.board.result-step", route + ".main.board", "read", "read-only", "42702 回包步数图与实际逐格移动数一致。");
Leaf(route + ".main.board.result-box", route + ".main.board", "read", "read-only", "每一到达格的开箱时序、已领取皮肤与下一格推进。");
Leaf(route + ".main.board.result-crit", route + ".main.board", "read", "read-only", "暴击 UI_yjtb_01 的归属、双时间点动态与清理。");
Leaf(route + ".main.board.result-fly", route + ".main.board", "read", "read-only", "逐格奖励飞行轨迹、目标、相位和中途关页零残留。");
Leaf(route + ".main.board.result-reward", route + ".main.board", "read", "read-only", "最终奖励 CongratulationObtainView 身份、内容和关闭链。");

var previewControls = new List<Control> { C("config", "data", route + ".main.reward-preview.config"), C("scroll", "scroll-list", route + ".main.reward-preview.scroll") };
for (int i = 1; i <= 12; i++) previewControls.Add(C($"item-{i:00}", "shared-item", $"{route}.main.reward-preview.item-{i:00}"));
Page(route + ".main.reward-preview", route + ".main", previewControls.ToArray());
Leaf(route + ".main.reward-preview.config", route + ".main.reward-preview", "read", "read-only", "config_adventure_kv[4] 的 12 个固定预览物品。");
Leaf(route + ".main.reward-preview.scroll", route + ".main.reward-preview", "read", "read-only", "254x67 横向 ScrollRect、裁剪、拖动与末项。");
for (int i = 1; i <= 12; i++)
{
    string item = $"{route}.main.reward-preview.item-{i:00}";
    Page(item, route + ".main.reward-preview",
        C("identity", "shared-item", item + ".identity"),
        C("detail", "popup", item + ".detail"));
    Leaf(item + ".identity", item, "read", "read-only", $"奖励预览第 {i} 格的 EquipmentItem 身份、图标、数量和品质。");
    Leaf(item + ".detail", item, "navigation", "read-only", $"奖励预览第 {i} 格的 ItemTips 具体物品、层级与关闭链。");
}

Page(route + ".main.action", route + ".main",
    C("free-throw", "conditional-transaction", route + ".main.action.free-throw"),
    C("ticket-throw", "conditional-transaction", route + ".main.action.ticket-throw"),
    C("paid-throw", "page", route + ".main.action.paid-throw"),
    C("free-reset", "conditional-transaction", route + ".main.action.free-reset"),
    C("paid-reset", "page", route + ".main.action.paid-reset"),
    C("label-state", "conditional-state", route + ".main.action.label-state"),
    C("activity-ended", "conditional-state", route + ".main.action.activity-ended"),
    C("requesting", "conditional-state", route + ".main.action.requesting"),
    C("zero-protect", "conditional-state", route + ".main.action.zero-protect"),
    C("insufficient", "conditional-popup", route + ".main.action.insufficient"),
    C("position-30", "conditional-state", route + ".main.action.position-30"),
    C("no-reset", "conditional-state", route + ".main.action.no-reset"),
    C("exhausted-vip", "conditional-navigation", route + ".main.action.exhausted-vip"));
Leaf(route + ".main.action.free-throw", route + ".main.action", "transaction", "destructive-write", "42702 isCheap 状态下免费投掷。");
Leaf(route + ".main.action.ticket-throw", route + ".main.action", "transaction", "destructive-write", "42702 使用冒险券抵扣投掷。");
Page(route + ".main.action.paid-throw", route + ".main.action",
    C("message", "state", route + ".main.action.paid-throw.message"),
    C("confirm", "transaction", route + ".main.action.paid-throw.confirm"),
    C("cancel", "return", route + ".main.action.paid-throw.cancel"),
    C("dont-remind", "toggle", route + ".main.action.paid-throw.dont-remind"));
Leaf(route + ".main.action.paid-throw.message", route + ".main.action.paid-throw", "read", "read-only", "alert_go 的投掷成本、票券抵扣与确认文案。");
Leaf(route + ".main.action.paid-throw.confirm", route + ".main.action.paid-throw", "transaction", "destructive-write", "确认后发送 42702 勾玉/绑玉付费投掷。");
Leaf(route + ".main.action.paid-throw.cancel", route + ".main.action.paid-throw", "return", "read-only", "取消/遮罩关闭确认框且不发送 42702。");
Leaf(route + ".main.action.paid-throw.dont-remind", route + ".main.action.paid-throw", "tab", "read-only", "alert_go 本登录不再提示的勾选与会话生命周期。");
Leaf(route + ".main.action.free-reset", route + ".main.action", "transaction", "destructive-write", "42703 免费重置。");
Page(route + ".main.action.paid-reset", route + ".main.action",
    C("message", "state", route + ".main.action.paid-reset.message"),
    C("confirm", "transaction", route + ".main.action.paid-reset.confirm"),
    C("cancel", "return", route + ".main.action.paid-reset.cancel"),
    C("dont-remind", "toggle", route + ".main.action.paid-reset.dont-remind"));
Leaf(route + ".main.action.paid-reset.message", route + ".main.action.paid-reset", "read", "read-only", "alert_ref 的重置成本与确认文案。");
Leaf(route + ".main.action.paid-reset.confirm", route + ".main.action.paid-reset", "transaction", "destructive-write", "确认后发送 42703 付费重置。");
Leaf(route + ".main.action.paid-reset.cancel", route + ".main.action.paid-reset", "return", "read-only", "取消/遮罩关闭确认框且不发送 42703。");
Leaf(route + ".main.action.paid-reset.dont-remind", route + ".main.action.paid-reset", "tab", "read-only", "alert_ref 本登录不再提示的勾选与会话生命周期。");
Leaf(route + ".main.action.label-state", route + ".main.action", "read", "read-only", "location!=30 显示前进，location=30 显示重置。");
Leaf(route + ".main.action.activity-ended", route + ".main.action", "read", "read-only", "活动结束时提示活动已经结束并且不发送 42702/42703。");
Leaf(route + ".main.action.requesting", route + ".main.action", "read", "read-only", "42702 投掷中按钮防重入、提示与回包恢复；42703 不虚构同态。");
Leaf(route + ".main.action.zero-protect", route + ".main.action", "read", "read-only", "零点前后 15 秒显示重置中并禁止提交。");
Leaf(route + ".main.action.insufficient", route + ".main.action", "navigation", "read-only", "冒险券/勾玉不足时准确提示或充值跳转，不发送事务。");
Leaf(route + ".main.action.position-30", route + ".main.action", "read", "read-only", "位置 30 时同一按钮切换为重置语义与对应成本。");
Leaf(route + ".main.action.no-reset", route + ".main.action", "read", "read-only", "无剩余重置次数时禁用/提示且不发送 42703。");
Leaf(route + ".main.action.exhausted-vip", route + ".main.action", "navigation", "read-only", "次数耗尽时 VIP 下一档与前往充值弹窗。");

Page(route + ".shop", route + ".main.shop-open",
    C("identity", "visual", route + ".shop.identity"), C("close", "page", route + ".shop.close"),
    C("query", "protocol-read", route + ".shop.query"), C("refresh", "page", route + ".shop.refresh"),
    C("countdown", "page", route + ".shop.countdown"), C("list", "page", route + ".shop.list"));
Leaf(route + ".shop.identity", route + ".shop", "read", "read-only", "Activity 层弹窗背景、标题、遮罩和根尺寸。");
Page(route + ".shop.close", route + ".shop",
    C("button", "return", route + ".shop.close.button"),
    C("mask", "return", route + ".shop.close.mask"));
Leaf(route + ".shop.close.button", route + ".shop.close", "return", "read-only", "X/返回按钮关闭商店且主棋盘保持。");
Leaf(route + ".shop.close.mask", route + ".shop.close", "return", "read-only", "背景遮罩点击关闭商店且不穿透主棋盘。");
Leaf(route + ".shop.query", route + ".shop", "read", "read-only", "42704 times/refresh_cost/goods_list 六格查询。");
Page(route + ".shop.refresh", route + ".shop",
    C("free-state", "conditional-state", route + ".shop.refresh.free-state"),
    C("free-submit", "conditional-transaction", route + ".shop.refresh.free-submit"),
    C("paid-state", "conditional-state", route + ".shop.refresh.paid-state"),
    C("paid-confirm", "popup", route + ".shop.refresh.paid-confirm"),
    C("insufficient", "conditional-popup", route + ".shop.refresh.insufficient"),
    C("zero-protect", "conditional-state", route + ".shop.refresh.zero-protect"));
Leaf(route + ".shop.refresh.free-state", route + ".shop.refresh", "read", "read-only", "免费刷新时 _lb_ref=免费，cost icon/number 隐藏。");
Leaf(route + ".shop.refresh.free-submit", route + ".shop.refresh", "transaction", "destructive-write", "42706 免费手动刷新与六格即时替换。");
Leaf(route + ".shop.refresh.paid-state", route + ".shop.refresh", "read", "read-only", "付费刷新时 _lb_ref=刷新，准确 cost icon/number 显示。");
Page(route + ".shop.refresh.paid-confirm", route + ".shop.refresh",
    C("message", "state", route + ".shop.refresh.paid-confirm.message"),
    C("confirm", "transaction", route + ".shop.refresh.paid-confirm.confirm"),
    C("cancel", "return", route + ".shop.refresh.paid-confirm.cancel"),
    C("dont-remind", "toggle", route + ".shop.refresh.paid-confirm.dont-remind"));
Leaf(route + ".shop.refresh.paid-confirm.message", route + ".shop.refresh.paid-confirm", "read", "read-only", "付费刷新准确成本确认文案。");
Leaf(route + ".shop.refresh.paid-confirm.confirm", route + ".shop.refresh.paid-confirm", "transaction", "destructive-write", "确认后发送 42706 并即时替换六格及货币。");
Leaf(route + ".shop.refresh.paid-confirm.cancel", route + ".shop.refresh.paid-confirm", "return", "read-only", "取消/遮罩关闭且不发送 42706。");
Leaf(route + ".shop.refresh.paid-confirm.dont-remind", route + ".shop.refresh.paid-confirm", "tab", "read-only", "alert_shop_ref 本登录不再提示的会话状态。");
Leaf(route + ".shop.refresh.insufficient", route + ".shop.refresh", "navigation", "read-only", "刷新费用不足提示/充值跳转，不发送 42706。");
Leaf(route + ".shop.refresh.zero-protect", route + ".shop.refresh", "read", "read-only", "零点前后保护显示刷新中并禁止提交。");
Page(route + ".shop.countdown", route + ".shop",
    C("normal", "timer", route + ".shop.countdown.normal"),
    C("resetting", "conditional-state", route + ".shop.countdown.resetting"));
Leaf(route + ".shop.countdown.normal", route + ".shop.countdown", "read", "read-only", "正常态距商店自动刷新倒计时、逐秒更新与关页停止。");
Leaf(route + ".shop.countdown.resetting", route + ".shop.countdown", "read", "read-only", "零点保护显示刷新中(Ns)并禁止刷新/购买。");

var shopListControls = new List<Control> { C("layout", "scroll-list", route + ".shop.list.layout") };
for (int i = 1; i <= 6; i++) shopListControls.Add(C($"item-{i:00}", "list-item", $"{route}.shop.list.item-{i:00}"));
Page(route + ".shop.list", route + ".shop", shopListControls.ToArray());
Leaf(route + ".shop.list.layout", route + ".shop.list", "read", "read-only", "3 列 x 2 行、ScrollRect/Mask/Content 高度与拖动。");
for (int i = 1; i <= 6; i++)
{
    string item = $"{route}.shop.list.item-{i:00}";
    Page(item, route + ".shop.list",
        C("name", "text", item + ".name"), C("detail", "popup", item + ".detail"),
        C("original-price", "state", item + ".original-price"), C("current-price", "state", item + ".current-price"),
        C("discount-integer", "conditional-state", item + ".discount-integer"),
        C("discount-decimal", "conditional-state", item + ".discount-decimal"),
        C("discount-hidden", "conditional-state", item + ".discount-hidden"),
        C("unbought-paid", "conditional-state", item + ".unbought-paid"),
        C("unbought-free", "conditional-state", item + ".unbought-free"),
        C("purchased", "conditional-state", item + ".purchased"),
        C("limit", "conditional-state", item + ".limit"), C("over", "conditional-state", item + ".over"),
        C("effect1-condition", "conditional-effect", item + ".effect1-condition"),
        C("discount-effect", "effect", item + ".discount-effect"),
        C("buy", "page", item + ".buy"));
    Leaf(item + ".name", item, "read", "read-only", "商品名、EquipmentItem 图标、数量与品质。");
    Leaf(item + ".detail", item, "navigation", "read-only", "商品 EquipmentItem 的准确详情弹窗。");
    Leaf(item + ".original-price", item, "read", "read-only", "原价、货币图标与删除线状态。");
    Leaf(item + ".current-price", item, "read", "read-only", "现价、免费文案、货币图标与不足颜色。");
    Leaf(item + ".discount-integer", item, "read", "read-only", "整数折扣图的资源、位置和显隐。");
    Leaf(item + ".discount-decimal", item, "read", "read-only", "小数折扣 point_gp+big_num+small_num 的布局和显隐。");
    Leaf(item + ".discount-hidden", item, "read", "read-only", "disc<=0 或 >=10 时 _img_discount 与 point_gp 均隐藏。");
    Leaf(item + ".unbought-paid", item, "read", "read-only", "未购买付费商品按钮、价格与无红点状态。");
    Leaf(item + ".unbought-free", item, "read", "read-only", "未购买免费商品、红点与 ui_anniu_5 状态。");
    Leaf(item + ".purchased", item, "read", "read-only", "已购买/已领取后的售罄遮罩、按钮可命中与重开一致性。");
    Leaf(item + ".limit", item, "read", "read-only", "_lb_limit 条件文案的真实可达性与布局。");
    Leaf(item + ".over", item, "read", "read-only", "_img_over 条件遮罩的真实可达性、层级与点击拦截。");
    Leaf(item + ".effect1-condition", item, "read", "read-only", "_gp_effect1 条件容器虽老端赋值链弱，仍核实显隐与足迹。");
    Leaf(item + ".discount-effect", item, "read", "read-only", "UI_yjtb_02 与 ui_anniu_5 动态足迹和清理。");
    string buy = item + ".buy";
    Page(buy, item,
        C("free-claim", "conditional-transaction", buy + ".free-claim"),
        C("paid-confirm", "popup", buy + ".paid-confirm"),
        C("zero-protect", "conditional-state", buy + ".zero-protect"),
        C("insufficient", "conditional-popup", buy + ".insufficient"),
        C("purchased-click", "conditional-state", buy + ".purchased-click"),
        C("success-refresh", "state", buy + ".success-refresh"),
        C("success-reward", "popup", buy + ".success-reward"));
    Leaf(buy + ".free-claim", buy, "transaction", "destructive-write", "42705 免费领取、当前格即时刷新和重开一致性。");
    Page(buy + ".paid-confirm", buy,
        C("message", "state", buy + ".paid-confirm.message"),
        C("confirm", "transaction", buy + ".paid-confirm.confirm"),
        C("cancel", "return", buy + ".paid-confirm.cancel"),
        C("dont-remind", "toggle", buy + ".paid-confirm.dont-remind"));
    Leaf(buy + ".paid-confirm.message", buy + ".paid-confirm", "read", "read-only", "本格商品名、数量与准确勾玉成本确认文案。");
    Leaf(buy + ".paid-confirm.confirm", buy + ".paid-confirm", "transaction", "destructive-write", "alert_shop_buy 确认后发送本格 42705。");
    Leaf(buy + ".paid-confirm.cancel", buy + ".paid-confirm", "return", "read-only", "取消/遮罩关闭且不发送本格 42705。");
    Leaf(buy + ".paid-confirm.dont-remind", buy + ".paid-confirm", "tab", "read-only", "alert_shop_buy 本登录不再提示的会话状态。");
    Leaf(buy + ".zero-protect", buy, "read", "read-only", "零点保护提示刷新中并且不发送本格 42705。");
    Leaf(buy + ".insufficient", buy, "navigation", "read-only", "余额不足走 NotEnoughDiamond，不发送本格 42705。");
    Leaf(buy + ".purchased-click", buy, "read", "read-only", "已购买按钮再次点击提示已购买，不重复发送 42705。");
    Leaf(buy + ".success-refresh", buy, "read", "read-only", "42705 成功后本格 state/红点/货币即时刷新且关闭重开一致。");
    Leaf(buy + ".success-reward", buy, "navigation", "read-only", "42705 成功后的 CongratulationObtainView 内容、层级与关闭链。");
}

Page(route + ".shared", route,
    C("equipment-identity", "shared-component", route + ".shared.equipment-identity"),
    C("equipment-matrix", "state-matrix", route + ".shared.equipment-matrix"),
    C("effects", "dynamic-render", route + ".shared.effects"),
    C("reward-fly", "shared-service", route + ".shared.reward-fly"),
    C("popup", "shared-popup", route + ".shared.popup"));
Leaf(route + ".shared.equipment-identity", route + ".shared", "read", "read-only", "三类 EquipmentItem 消费者及 Common Prefab/GUID 身份。");
Leaf(route + ".shared.equipment-matrix", route + ".shared", "read", "read-only", "普通/高级/终极/特殊、预览/商店、空/有数据状态矩阵。");
Leaf(route + ".shared.effects", route + ".shared", "read", "read-only", "UI_2001/UI_yjtb_01/UI_yjtb_02/ui_anniu_5 的归属、RGBA、双帧与清理。");
Leaf(route + ".shared.reward-fly", route + ".shared", "read", "read-only", "逐格 RewardFlyService 目标、轨迹、相位和中途关页零残留。");
Leaf(route + ".shared.popup", route + ".shared", "read", "read-only", "物品详情、确认框、VIP 与统一奖励弹窗身份链。");

Page(route + ".lifecycle", route,
    C("cold", "lifecycle", route + ".lifecycle.cold"), C("warm", "lifecycle", route + ".lifecycle.warm"),
    C("viewports", "visual", route + ".lifecycle.viewports"), C("resource-stable", "resource", route + ".lifecycle.resource-stable"),
    C("performance", "performance", route + ".lifecycle.performance"), C("disconnect", "lifecycle", route + ".lifecycle.disconnect"));
Leaf(route + ".lifecycle.cold", route + ".lifecycle", "read", "read-only", "冷开首屏 350ms/1000ms/ready 与配置/模型/特效加载。");
Leaf(route + ".lifecycle.warm", route + ".lifecycle", "read", "read-only", "主页面/商店隐藏重开无重复模板、事件和残影。");
Leaf(route + ".lifecycle.viewports", route + ".lifecycle", "read", "read-only", "720x1280 与 1920x1080 老 H5/Unity Web 对比。");
Leaf(route + ".lifecycle.resource-stable", route + ".lifecycle", "read", "read-only", "四配置、31 格、12 预览、6 商店格和效果资源点击前闭包。");
Leaf(route + ".lifecycle.performance", route + ".lifecycle", "read", "read-only", "cold/warm 耗时、模型/列表分配和释放。");
Leaf(route + ".lifecycle.disconnect", route + ".lifecycle", "read", "read-only", "非自动重连断线释放 Module，自动重连不抢清理。");

var baseline = new Dictionary<string, object?>
{
    ["authority"] = "同账号、同状态、同 viewport 的当前老 H5 最终表现是唯一验收标准；本批禁止 Unity/Web/账号写，只声明静态实现与缺口。",
    ["legacy_sources"] = new[]
    {
        "E:/GitProject/yu_client/h5/src/adventure/AdventureWindowView.ts",
        "E:/GitProject/yu_client/h5/src/adventure/AdventureMainView.ts",
        "E:/GitProject/yu_client/h5/src/adventure/AdventureItem.ts",
        "E:/GitProject/yu_client/h5/src/adventure/AdventureShopView.ts",
        "E:/GitProject/yu_client/h5/src/adventure/AdventureShopItem.ts",
        "E:/GitProject/yu_client/h5/src/commonController/AdventureController.ts",
        "E:/GitProject/yu_client/h5/src/commonModel/AdventureModel.ts"
    },
    ["unity_sources"] = new[]
    {
        "Assets/Prefabs/UI/Adventure/AdventureModule.prefab",
        "Assets/Scripts/Module/Core/Adventure/AdventureBootstrap.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureController.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureModel.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureShopView.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureItem.cs",
        "Assets/Scripts/Module/Core/Adventure/AdventureShopItem.cs"
    },
    ["protocol_inventory"] = new Dictionary<string, object>
    {
        ["reads"] = new[] { "42700", "42701", "42704" },
        ["writes"] = new[] { "42702", "42703", "42705", "42706" },
        ["note"] = "本批仅保留现有 42700/42701 只读链；未注册、未发送、未执行四个写事务。42704 因配置/商品模型缺口保持 blocked。"
    },
    ["config_inventory"] = new Dictionary<string, object>
    {
        ["config_adventure_kv"] = 14, ["config_adventure_rand"] = 2,
        ["config_adventure_reward"] = 32, ["config_adventure_loc"] = 600,
        ["unity_status"] = "四份配置均未进入 Unity 可达闭包，禁止修改 Addressables，保持 blocker。"
    }
};

var manifest = new { route, baseline, nodes };
var jsonOptions = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
File.WriteAllText(Path.Combine(output, "route-manifest.json"), JsonSerializer.Serialize(manifest, jsonOptions) + "\n", utf8);

var nrv = new HashSet<string>(StringComparer.Ordinal)
{
    route + ".entry.icon-42701", route + ".entry.board-read",
    route + ".shell.title", route + ".main.visual",
    route + ".main.ticket-toggle.off", route + ".main.ticket-toggle.on",
    route + ".main.cost-state.free", route + ".main.reset-timer.countdown",
    route + ".main.shop-open.button", route + ".main.forward-red",
    route + ".main.action.label-state", route + ".main.action.activity-ended",
    route + ".shop.identity", route + ".shop.close.button", route + ".shop.countdown.normal", route + ".shop.list.layout",
    route + ".lifecycle.cold", route + ".lifecycle.warm", route + ".lifecycle.viewports",
    route + ".lifecycle.performance", route + ".lifecycle.disconnect"
};
var results = new List<Dictionary<string, object>>();
foreach (Node node in nodes.Where(n => !nodes.Any(x => x.Parent == n.Id)))
{
    var result = new Dictionary<string, object> { ["id"] = node.Id, ["evidence"] = new[] { evidence } };
    if (nrv.Contains(node.Id))
    {
        result["status"] = "needs-runtime-verify";
        result["runtime_gap"] = RuntimeGap(node.Id);
    }
    else
    {
        result["status"] = "blocked";
        result["blocked_reason"] = BlockedReason(node);
    }
    results.Add(result);
}
File.WriteAllText(Path.Combine(output, "results-static.json"), JsonSerializer.Serialize(new { nodes = results }, jsonOptions) + "\n", utf8);

var checks = new List<Check>();
void CheckText(string name, string path, string token)
{
    string text = File.ReadAllText(Path.Combine(repo, path));
    checks.Add(new Check(name, text.Contains(token, StringComparison.Ordinal), path + " contains " + token));
}
CheckText("bootstrap-route-a", "Assets/Scripts/Module/Core/Adventure/AdventureBootstrap.cs", "ICON_TYPE_A, AdventureFlow.Toggle");
CheckText("bootstrap-route-b", "Assets/Scripts/Module/Core/Adventure/AdventureBootstrap.cs", "ICON_TYPE_B, AdventureFlow.Toggle");
CheckText("flow-prefab", "Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs", "AdventureModule");
CheckText("flow-hides-board-template", "Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs", "GetComponentsInChildren<AdventureItem>");
CheckText("flow-hides-shop-template", "Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs", "GetComponentsInChildren<AdventureShopItem>");
CheckText("main-safe-throw", "Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs", "投掷事务尚未完成安全接入");
CheckText("main-safe-reset", "Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs", "重置事务尚未完成安全接入");
CheckText("main-ended-guard", "Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs", "活动已经结束");
CheckText("main-reset-placeholder", "Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs", "剩余重置次数:--");
CheckText("shop-safe-refresh", "Assets/Scripts/Module/Core/Adventure/AdventureShopView.cs", "商店刷新事务尚未完成安全接入");
CheckText("model-current-free", "Assets/Scripts/Module/Core/Adventure/AdventureModel.cs", "CURRENT_BASE_FREE_THROW_TIMES = 4");
CheckText("model-endpoint-no-red", "Assets/Scripts/Module/Core/Adventure/AdventureModel.cs", "HasFreeThrowRed => HasBoardState && !IsAtResetPosition && HasFreeAction");
CheckText("controller-board-read", "Assets/Scripts/Module/Core/Adventure/AdventureController.cs", "RegisterProtocal(Proto.ADVENTURE_BOARD_STATE");
CheckText("controller-red-semantics", "Assets/Scripts/Module/Core/Adventure/AdventureController.cs", "model.HasFreeThrowRed");

string moduleText = string.Join("\n", Directory.GetFiles(Path.Combine(repo, "Assets/Scripts/Module/Core/Adventure"), "*.cs").Select(File.ReadAllText));
bool noWriteSend = !Regex.IsMatch(moduleText, @"SendFmt\s*\(\s*(42702|42703|42705|42706)\b");
checks.Add(new Check("no-write-send", noWriteSend, "no SendFmt(42702/42703/42705/42706)"));
string prefab = File.ReadAllText(Path.Combine(repo, "Assets/Prefabs/UI/Adventure/AdventureModule.prefab"));
foreach ((string name, string guid) in new[]
{
    ("main-business-guid", "77160b1021d0483cb77a24940140dc47"),
    ("shop-business-guid", "c9d08b16cf654c06952fcf8d88f5cfd4"),
    ("board-item-business-guid", "1fc883dbc57747b394a02d55290d153f"),
    ("shop-item-business-guid", "dacd186b40cd4ed0bde37fec6efd47d2")
}) checks.Add(new Check(name, prefab.Contains(guid, StringComparison.Ordinal), guid));

string oldCfg = Path.Combine(Path.GetDirectoryName(repo)!, "yu_client/cdn/resource/config/server");
foreach ((string name, int count) in new[] { ("kv", 14), ("rand", 2), ("reward", 32), ("loc", 600) })
{
    string path = Path.Combine(oldCfg, "config_adventure_" + name + ".json");
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
    int actual = doc.RootElement.EnumerateObject().Count();
    checks.Add(new Check("legacy-config-" + name, actual == count, $"expected={count} actual={actual}"));
}
checks.Add(new Check("schema-topology-node-count", nodes.Count == 423, "nodes=" + nodes.Count));
checks.Add(new Check("schema-leaf-count", results.Count == 334, "leaves=" + results.Count));
checks.Add(new Check("all-leaves-explicit", results.All(r => (string)r["status"] is "blocked" or "needs-runtime-verify"), "no done/not-run leaves"));

int blocked = results.Count(r => (string)r["status"] == "blocked");
int runtime = results.Count - blocked;
var verification = new
{
    verdict = checks.All(c => c.Pass) ? "pass" : "fail",
    checks,
    topology = new { node_count = nodes.Count, leaf_count = results.Count, blocked, needs_runtime_verify = runtime }
};
File.WriteAllText(Path.Combine(output, "static-verification.json"), JsonSerializer.Serialize(verification, jsonOptions) + "\n", utf8);

var audit = new StringBuilder();
audit.AppendLine("# Adventure 静态审计").AppendLine();
audit.AppendLine($"- schema 6 topology: nodes={nodes.Count}, leaves={results.Count}");
audit.AppendLine($"- leaf statuses: blocked={blocked}, needs-runtime-verify={runtime}, done=0");
audit.AppendLine("- 当前 Prefab 已由四个业务子类增量接管；入口注册 42701/42702，主页面只读 42701 状态可刷新。");
audit.AppendLine("- 42702/42703/42705/42706 未注册、未发送、未执行；42704 因配置及商品模型闭包缺失保持 blocked。");
audit.AppendLine("- 未启动 Unity、浏览器或前台程序，未做账号写事务；所有视觉、点击、模型、特效、即时刷新与重开均未静态冒充通过。").AppendLine();
audit.AppendLine("## 定向检查").AppendLine();
foreach (Check check in checks) audit.AppendLine($"- {(check.Pass ? "PASS" : "FAIL")} {check.Name}: {check.Detail}");
File.WriteAllText(Path.Combine(output, "static-audit.md"), audit.ToString(), utf8);

var inventory = new StringBuilder();
inventory.AppendLine("# Adventure source inventory").AppendLine();
inventory.AppendLine("- 老端页面：AdventureWindowView / AdventureMainView / AdventureItem / AdventureShopView / AdventureShopItem");
inventory.AppendLine("- 老端模型与控制器：AdventureModel / AdventureController");
inventory.AppendLine("- Unity：AdventureModule.prefab + Adventure 模块 9 个 C# 文件");
inventory.AppendLine("- 配置：kv=14、rand=2、reward=32、loc=600；Unity 当前均无可达配置资产。");
inventory.AppendLine("- 协议：42700/42701/42704 只读；42702 投掷、42703 重置、42705 购买、42706 手动刷新属于写事务。");
File.WriteAllText(Path.Combine(output, "source-inventory.md"), inventory.ToString(), utf8);

var deps = new StringBuilder();
deps.AppendLine("# Adventure component dependencies").AppendLine();
deps.AppendLine("| 组件 | 消费者 | 本岛结论 |").AppendLine("|---|---|---|");
deps.AppendLine("| EquipmentItem/Common | 31 棋盘格中的特殊/终极格、12 奖励预览、6 商店格 | Common 禁写；身份、详情、品质和状态矩阵 blocked |");
deps.AppendLine("| BaseWindow/Common | AdventureWindowView 标题、返回、货币、说明 | Common 禁写；外壳运行态 blocked |");
deps.AppendLine("| UI 模型/特效 | 主角、UI_2001、UI_yjtb_01、UI_yjtb_02、ui_anniu_5 | 需双时间点真实出帧；blocked |");
deps.AppendLine("| RewardFlyService/统一奖励弹窗 | 逐格奖励与最终奖励 | 未触发 42702，账号写禁止；blocked |");
deps.AppendLine("| MainUI/ActivityIcon | 42701/42702 活动入口与红点 | 本岛只注册路由/红点数据；实际布局命中 blocked |");
deps.AppendLine("| Halo/GodEquip/Advertising | 免费重置、神装跳转、广告增次 | 跨岛只记 blocker |");
File.WriteAllText(Path.Combine(output, "component-dependencies.md"), deps.ToString(), utf8);

var matrix = new StringBuilder();
matrix.AppendLine("# Adventure route matrix").AppendLine();
matrix.AppendLine("| leaf | status | gap |").AppendLine("|---|---|---|");
foreach (Dictionary<string, object> result in results)
{
    string gap = result.TryGetValue("blocked_reason", out object? b) ? (string)b : (string)result["runtime_gap"];
    matrix.AppendLine($"| {result["id"]} | {result["status"]} | {gap.Replace("|", "\\|")} |");
}
File.WriteAllText(Path.Combine(output, "route-matrix.md"), matrix.ToString(), utf8);

Console.WriteLine($"ADVENTURE_STATIC_AUDIT verdict={verification.verdict} checks={checks.Count} nodes={nodes.Count} leaves={results.Count} blocked={blocked} nrv={runtime}");
return checks.All(c => c.Pass) ? 0 : 1;

static string RuntimeGap(string id)
{
    if (id.EndsWith("viewports", StringComparison.Ordinal)) return "需 720x1280 与 1920x1080 同账号老 H5/Unity Web 像素对比。";
    if (id.Contains("lifecycle", StringComparison.Ordinal)) return "需真实 Prefab 冷/热开、关闭清理、断线和性能证据。";
    if (id.EndsWith("close", StringComparison.Ordinal) || id.EndsWith("shop-open", StringComparison.Ordinal)) return "需 GraphicRaycaster 真实点击、层级、返回链和暖重开。";
    if (id.Contains("entry", StringComparison.Ordinal)) return "需真实活动时间窗、入口图标与 42701 回包验证。";
    return "静态接管已完成，仍需同路径真实 Unity/Web 状态、点击、视觉与生命周期复验。";
}

static string BlockedReason(Node node)
{
    if (node.Type == "transaction" || node.Risk == "destructive-write")
        return "真实账号写事务；本轮禁止发送或执行，成功/失败、即时刷新与重开不得静态冒充。";
    if (node.Id.Contains("board.slot", StringComparison.Ordinal) || node.Id.Contains("reward-preview", StringComparison.Ordinal))
        return "Unity 缺 config_adventure_kv/loc/reward/rand 可达闭包，且详情依赖禁写 Common，无法生成或逐格真实核对。";
    if (node.Id.Contains("shop.list.item", StringComparison.Ordinal) || node.Id.EndsWith("shop.query", StringComparison.Ordinal))
        return "42704 商品模型、六格数据和 Common EquipmentItem 消费链尚未迁移；禁止用烤制模板伪造玩家数据。";
    if (node.Id.Contains("role-model", StringComparison.Ordinal) || node.Id.Contains("result-", StringComparison.Ordinal) || node.Id.Contains("shared", StringComparison.Ordinal))
        return "依赖禁写的共享模型/特效/详情/奖励链及真实双帧像素或账号事务，本岛不能闭环。";
    if (node.Id.Contains("suit", StringComparison.Ordinal) || node.Id.Contains("halo", StringComparison.Ordinal) || node.Id.Contains("ad", StringComparison.Ordinal))
        return "跨 GodEquip/Halo/Advertising 文件岛，只允许记录 blocker。";
    if (node.Id.Contains("shell", StringComparison.Ordinal) || node.Id.Contains("mainui-visibility", StringComparison.Ordinal))
        return "依赖禁写的 MainUI/Common/BaseWindow 外壳与运行态入口，当前不能完成。";
    if (node.Id.Contains("cost-state", StringComparison.Ordinal) || node.Id.Contains("ticket-state", StringComparison.Ordinal) || node.Id.Contains("reset-count", StringComparison.Ordinal) || node.Id.Contains("shop-red", StringComparison.Ordinal))
        return "依赖缺失冒险配置、背包货币、VIP/光环或商店权威状态，静态占位不算真实状态。";
    if (node.Id.Contains("resource-stable", StringComparison.Ordinal))
        return "缺四份配置与动态资源的 Addressables 闭包，且本轮禁止修改 Addressables。";
    return "缺真实 Unity/Web、配置或跨岛依赖；静态证据不足以完成该叶。";
}

sealed record Control(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("child")] string Child);

sealed record Node(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("risk")] string Risk,
    [property: JsonPropertyName("parent")] string? Parent,
    [property: JsonPropertyName("control_inventory")] IReadOnlyList<Control>? ControlInventory,
    [property: JsonPropertyName("note")] string? Note);

sealed record Check(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("pass")] bool Pass,
    [property: JsonPropertyName("detail")] string Detail);
