# TopVip 静态源清单（2026-08-09）

## 审计边界

- 本轮只读核对老 H5、当前 Unity C#、Generated Bind 与现有 Prefab；未启动 Unity、浏览器或任何前台程序。
- 未点击、未发送充值、购买、领取或领奖事务。
- 老端最终表现仍需同账号真实 H5/Unity Web 顺序复走；本文件不是运行态证据。

## 老 H5 路由

- 入口条件：`vip_flag >= 4` 且 `level >= 160`，入口图标 451。
- `TopVipBaseView` 四页签：至尊特权、至尊技能、特权任务、至尊商店；含页签红点与关闭链。
- 至尊特权：体验/临时/永久三态、7 个权益说明、说明页、立即体验、升级永久、每日礼包、动态奖励格。
- 永久升级弹窗：当日充值进度、7 日状态、充值入口、永久购买确认、关闭/背景关闭。
- 激活/升级结果弹窗：两种 VIP 类型文案、最多 7 条权益说明、滚动、说明入口、关闭/背景关闭。
- 至尊技能：技能图、技能说明、8 个属性点弹窗、阶段任务列表、任务跳转、任务领奖、奖励格、阶段激活弹窗、UI_2011 动效。
- 特权任务：动态任务列表、滚动、状态、功能跳转、领奖、奖励格。
- 至尊商店：货币/倒计时、三列动态商品、预告/可购/售罄、物品详情、单次/批量购买、售罄提示、列表滚动。

## 老 H5 弹窗/叶节点

- `TopVipSkillIntroduceView`：技能图/名称/说明、说明滚动、确定关闭、背景关闭。
- `TopVipSkillDetailView`：阶段技能/锁定态列表、滚动、关闭、背景关闭。
- `TopVipSkillPointView`：按 8 个点位显示属性说明、背景关闭。
- `TopVipActivationSkillView`：激活技能图/名称/说明、效果、5 秒自动关闭、背景关闭；关闭后请求 45102。
- `TopVipActivityPromotionView`：充值入口、45107 永久购买、进度矩阵、关闭链。
- `TopVipActivityDetailView`：结果说明列表、说明入口、关闭链。

## 协议/事务边界

- 当前 Unity 只注册读取/推送：45101、45102、45104、45109、45110、45111、45112。
- 45103（技能任务领奖）、45105（货币任务领奖）、45106（体验购买）、45107（永久购买）、45108（每日领取）为明确 hard-negative，本轮全部 `blocked`。
- 商店查询/购买为 15301/15302；15302 与批量购买同属未授权购买事务，本轮 `blocked`。
- 不新增孤立 sender/handler，不做乐观状态，不以 GM 或协议直写伪造 UI 事务。

## Unity 现状与 Prefab 身份

- 业务脚本仅有 `TopVipController.cs`、`TopVipModel.cs`。
- `Assets/Prefabs/UI/TopVip` 仅有 `TopVipShopItem.prefab`，根绑定 GUID `27a0681d0fba7754bafe27b0e9e9f770`，对应只读 `TopVipShopItemBind.cs`。
- 该商品项 Bind 含 `buy_gp/buyed_gp/limit_gp/forecast_*`、价格、剩余次数和 `_tpl_BaseAwardItem`。
- 未找到四个完整页签、BaseView、各弹窗或 `TopVipRodItem` 的可编辑 Unity Prefab，也未找到页面业务 View。
- 因完整模块 Prefab 缺失，本轮按要求只读确认并阻断转换；不能用独立商品项替代完整页面。

## 共享依赖

- `BaseAwardItem`：每日礼包、技能任务、特权任务、商店商品详情；需覆盖空/有数据、品质/特效、点击详情与宿主缩放。
- `InstructionView`：至尊 VIP 说明。
- `Alert`、`RechargeView`、`ShopBulkPurchaseView`、`CongratulationObtainView`：均在本文件岛外；充值/购买/领奖不进入。
- BaseWindow/背景遮罩/页签/红点/滚动基础设施仅记录依赖，不修改。

## 静态结论

- 没有可在本文件岛内安全增量修复的完整 TopVip 页面；本轮未改 C# 或 Prefab。
- 所有路由叶均因缺完整 Prefab/业务 View 或事务授权而 `blocked`；任何静态核对均不冒充真实 Web/Unity 通过。
