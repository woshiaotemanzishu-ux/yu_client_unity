# Claude任务包-主线竖切-第7轮

日期：2026-06-21

目标：第 6 轮已把通用物品格子 `BaseAwardItem.prefab` 修成**可复用**(回填 Bind 组件工具)、并把任务奖励
货币/经验做成**真实名**(special_goods 元组语义实证 = `{type,type_id,count}`)。第 7 轮把这两块**用进真实玩法页面**:
优先**背包入口真实物品页**(复用 `BaseAwardItem` + `GoodsModel` 渲真实物品格),与**奖励货币也成图标格 + 嵌套职业礼包解析**。
如某步被真实资源/协议/运行环境阻塞,写清证据并切到下一个玩家可见缺口,不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第6轮.md` + `Docs/Shenxiao实施进度.md` 第 6 轮段

## 当前基线（第 6 轮已提交，commit `8f700ee4c` P1 / `c776bbfca` P2）

- P1:`LayaBindFiller` 新增可重跑全量回填 `FillAll` + 菜单「神霄/UI/回填 Bind 组件 /(预览不写盘)」;已给 128 prefab 里
  缺组件的 36 处补挂 Bind(含 `BaseAwardItem`/`EquipmentItem`/`bagItemRenderer`/各 *Item)。`BaseAwardItem.prefab` 现可
  `InstantiateAsync + GetComponent<BaseAwardItem>() + SetData` 显真实图标 + 品质底板 + 数量(`Temp/p1_baseawarditem.png`)。
  `TaskFinishView` 已改复用真实 `BaseAwardItem` 格子。
- P2:`ConfigNotNormalGoods` 进 `ClientConfigSync.SYNC_LIST`;`GoodsModel.GetMappingTypeId` 接表(3→31 金币/5→32 经验…);
  `TaskReward` 按 `{type,type_id,count}` 解析,货币显真名(`Temp/p2_reward_panel.png`:经验 ×300000 / 九洲灵钱 ×20000 / 淬魂原石 ×2)。
- 双编译 0 错。

## 已确认仍缺（按价值排序）

1. **背包入口真实物品页**:主界面背包按钮 → 真实物品格/空格/货币完整页。复用第 6 轮已就绪的 `BaseAwardItem`(回填工具已给
   `bagItemRenderer`/`BagModule.prefab` 补组件)+ `GoodsModel`,需 bag 协议(拉背包列表)+ `BagModel`(持有数据)。
2. **奖励货币也成图标格**:完成弹层/对话奖励里货币(经验/金币)目前走 `_rewardText` 文本;老端 `BaseAwardItem` 货币也显
   图标格(货币 goods_icon)。需确认 config_goods 里货币(31/32…)是否有 `goods_icon`(键 14);有则货币也走格子,无则保留文本 + 精确 blocker。
3. **嵌套职业定制礼包**:`config_task` 中 18 处 `{career,[{type,type_id,count},...]}` 嵌套礼包(circle/循环任务)未解析,
   `TaskReward` 现按"非 3 元组"跳过 → 这些任务的职业专属奖励不显示。需按当前职业过滤 + 解析子列表。
4. **活服整合往返**:登录活服 → 进场景 → 点 NPC(对话弹 3D 立绘)→ 接/交任务 → 完成弹层(真实图标+货币真名)→ 30004 → 30001 刷新。
5. **BaseAwardItem 点击 tips**:`SetClickCallBack` 未设时点击应弹物品详情(对标老端 UIToolTipMgr);现仅 log。

## 老端源码锚点

- 背包:`bag/BagView.ts` / `commonModel/BagModel.ts`(物品列表/格子布局);拉背包协议号查 yu_client `bag` 模块 SendFmt;
  Unity 已有壳 `Assets/Scripts/Module/Core/Bag/BagFlow.cs` + `Assets/Prefabs/UI/Bag/BagModule.prefab`(回填工具已补 `bagItemRenderer` 组件)。
- 货币图标:`GoodsModel.ts` `GetGoodsIcon`(货币 goods_id 经 config_goods 取 goods_icon);Unity 对照 `GoodsModel.GetGoodsIcon` + `GameResPath.GetGoodsIconPath`。
- 嵌套礼包:`task/TaskFinishView.ts` `circle_task_normal_reward`/`circle_task_extra_reward` 的 `format_fun`(`vo.type/vo.type_id/vo.count` 具名读取);
  现网 `config_task` 嵌套样本 `[{1,[{0,39510031,2}]},{2,[{0,39510031,2}]}]`(career→子奖励列表)。
- 物品 tips:`common/UIToolTipMgr` / `ItemInfoView`(点击物品格弹详情)。

## 本轮 P0：保护可运行基线

- 确认 worktree 干净;不干净先说明改动归属(可能有 Codex/其他 worker 并行)。
- 核对第 6 轮链路仍在:`rg -n "GetMappingTypeId|GetNotNormalDesc|FillAll|EnsureBindOnWindow|ConfigNotNormalGoods" Assets -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错;新建 .cs 必经 Unity 重导入。
- 不重做第 6 轮,除非发现真实回归。

## 本轮 P1：背包入口真实物品页（复用 BaseAwardItem + GoodsModel）

目标:点主界面背包入口 → 显示真实背包物品(格子=真实图标+品质底板+数量,复用 `BaseAwardItem`),走真实 bag 协议/`BagModel`。

要求:
- 先读 yu_client `BagView.ts`/`BagModel.ts` + bag 拉取协议(格式串照抄,走 NetManager/BaseController),确认背包数据结构与协议号。
- 物品格复用 `BaseAwardItem`(`InstantiateAsync` + `SetData(typeId,count)`),虚拟列表/网格按既有列表范式;**不自建图标格**。
- 缺协议/缺 BagModel → 写精确 blocker 并切 P3,不做假物品页。

最低验收:
```powershell
rg -n "class BagModel|BagController|InstantiateAsync.*BaseAwardItem|SetData" Assets/Scripts/Module/Core/Bag -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先:背包页显示真实物品格(真实图标 + 品质底板 + 数量),非占位/非降级。

## 本轮 P2：奖励货币也成图标格 + 嵌套职业礼包解析

目标:完成弹层/对话奖励里货币(经验/金币)也显图标格(若有图);嵌套 `{career,[...]}` 礼包按当前职业解析进奖励。

要求:
- 货币图标:确认 config_goods 里 31(九洲灵钱)/32(经验)等是否有 `goods_icon`(键 14)。有 → 货币也走 `BaseAwardItem` 格子
  (`TaskReward.Entry.IsCurrency` 不再强制走文本);无 → 保留文本 + 写明缺哪个 goods_icon(精确 blocker)。
- 嵌套礼包:`TaskReward.AppendSpecialGoods` 增嵌套分支——元素是 `{career, [子元组]}` 时按 `career==当前职业` 过滤、
  解析子列表(子元组同 `{type,type_id,count}`,经 `GetMappingTypeId`)。career 参数此时启用(第 6 轮已预留)。
- 不臆造:嵌套样本以现网 config_task 实数据校验(RunCommand 运行期单测)。

最低验收:
```powershell
rg -n "IsCurrency|career|GetMappingTypeId|\[.*\]" Assets/Scripts/Module/Core/Task/TaskReward.cs -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/日志优先:某含嵌套礼包的任务按职业显职业专属奖励;货币显图标格(或文本 + 精确 blocker)。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **活服整合往返**:若会话允许,驱动「进场景 → 点 NPC 弹 3D 立绘 → 接/交任务 → 完成弹层(真实图标+货币真名)→ 30004 → 30001 刷新」并贴截图/日志。
2. **BaseAwardItem 点击 tips**:点物品格弹物品详情(对标老端 UIToolTipMgr/ItemInfoView),复用 GoodsModel 真实数据。
3. **立绘/格子构图微调**:对话立绘 scale/position/talk_scale/朝向按老端;完成弹层格子数量角标位置/样式对老端微调。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成";禁止无入口/无真实数据/无验收的 UI shell;禁止假物品/假背包/假奖励/假图标。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过;禁止卡 blocker 后自然退出(转 P3 或写下一轮包)。
- 禁止大面积手改 generated bind;禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径;
  禁止手改 GameRes 图集产物修通用问题(修导入器/转换器);**禁止凭未验证的协议/字段实现背包数据**。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker(文件/协议/key/字段)、下一轮任务包草案(不写"继续完善")。
