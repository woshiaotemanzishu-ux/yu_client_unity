# Claude任务包-主线竖切-第6轮

日期：2026-06-21

目标：把第 5 轮的「奖励真实图标 + 品质底板 + NPC 对话 3D 立绘」继续推进成**可复用 + 整合可见**：
优先**打通通用物品格子 `BaseAwardItem.prefab` 的复用**（让背包/装备/弹层都能挂真实图标格），
与**货币/经验真名**（先定元组语义，避免误标）。如某步被真实资源/协议/运行环境阻塞，写清证据并切到下一个
玩家可见缺口，不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第5轮.md` + `Docs/Shenxiao实施进度.md` 第 5 轮段

## 当前基线（第 5 轮已提交，commit `f29f7831e` P1 / `97324b93f` P2）

- P1：`GoodsModel` 键已订正（`"14"`=goods_icon、`"18"`=color；`"9"`/`"10"` 是 type/subtype）；`BaseAwardItem.RefreshIcon`
  接真实图标 + 品质底板 `com_goods_plate_{color}`；`TaskFinishView` 完成弹层有真实图标行；资源经 ResManager 兜底自
  yu_client/cdn 自动落地。
- P2：`DialogueView` 接 NPC 对话 **真实 3D 立绘**（`object/npc/model_clothe_{icon}` + `UIModelStage` + idle），
  缺模型降级 + 精确 blocker。
- 双编译 0 错；可见证据 = `Temp/p1_reward_row.png`、`Temp/p2_npc_lihui.png`（RunCommand 渲染真实资源）。

## 已确认仍缺（按价值排序）

1. **`BaseAwardItem.prefab` 缺 Bind 组件**：根仅 `RectTransform`、无 `BaseAwardItem`/`BaseAwardItemBind`
   （对照 `ItemInfoItem.prefab` 根挂 `ItemInfoItem`，可正常 `GetComponent` + SetData）。`BaseView.EnsureBound` 只校验
   **序列化引用**、不按名运行时绑定 → 该 prefab 经 `ResManager.InstantiateAsync` + `GetComponent<BaseAwardItem>()` 拿到的是 null。
   **修一处，背包/装备/弹层全部解锁**用真实 `BaseAwardItem` 格子（含本轮已就绪的图标+底板逻辑）。
2. **货币/经验真名**：`ConfigNotNormalGoods`（client 配置，type→goods_id，如 1→34 灵玉/3→31 金币/5→32 经验）
   可经 `SYNC_LIST` 同步；但 special_goods_list 元组 `{5,0,150000}` 首元是 currency-type 还是 career **未定**
   → 先定语义再实现，否则误标 = 假数据。
3. **活服 Play 整合往返**：登录活服 → 进场景 → 点 NPC（对话弹 3D 立绘）→ 接/交任务 → 完成弹层（真实图标行）→ 30004。
4. **立绘/格子构图微调**：对话立绘 scale/position/talk_scale/朝向；数量>1 角标（找 count>1 的真实任务奖励验证）。

## 老端源码锚点

- 物品格子组件化：`common/BaseAwardItem.ts`（`SetData`/`item_bg.skin = AtlasUrl("common","com_goods_plate_"+color)`）；
  Unity 对照可工作样板 = `Assets/Prefabs/UI/Common/ItemInfoItem.prefab`（根已挂 `ItemInfoItem`）。
- 货币映射：`commonModel/GoodsModel.ts:2972-2991` `GetMappingTypeId(_type,type_id)`（_type∈{-1,255,其它} → `ConfigNotNormalGoods[_type].goods_id`）；
  元组语义两处对照：`task/TaskFinishView.ts:152-174`（按 vo[1] 过滤）vs `commonController/DialogueController.ts:64-69`（按 vo[0]）—— 注意是**早期 4 元组残留**，须以活服 12102 实包为准。
- UI 转换器补 Bind 组件：`Editor/LanhuCreator/`（对照 `Docs/LanhuCreator接入规范.md`：生成 Prefab + `*Bind.cs` 时应把 Bind 组件挂上并回填序列化引用）。

## 本轮 P0：保护可运行基线

- 确认 worktree 干净；不干净先说明改动归属（可能有 Codex/其他 worker 并行）。
- 核对第 5 轮链路仍在：`rg -n "K_ICON|GetDisplayColor|com_goods_plate|ShowNpcModel|UIModelStage" Assets/Scripts/Module/Core -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错；新建 .cs 必经 Unity 重导入。
- 不重做第 5 轮，除非发现真实回归。

## 本轮 P1：让 `BaseAwardItem.prefab` 可复用（补 Bind 组件）

目标：通用物品格子可经 prefab 实例化拿到 `BaseAwardItem` 组件、`SetData` 即显真实图标+底板+数量。

要求：
- **优先修工具（不点杀单个 prefab）**：定位 `LanhuCreator`/UI 转换器为何对 BaseAwardItem 没挂 Bind 组件（对照 ItemInfoItem 为何挂上了），
  修转换器通用规则补挂 + 回填序列化引用并重跑；若转换器一时改不动，可加一个**可重跑的回填 Editor 工具**（菜单 `神霄/UI/回填 Bind 组件`）
  扫 `Assets/Prefabs/UI/**` 给缺组件的 `*.prefab` 按 `*Bind` 节点名补挂 + 回填（一次性、可重跑、出报告）。
- 改完用真实 `BaseAwardItem`（去掉 `TaskFinishView` 的自建图标行，换 `InstantiateAsync` + `SetData` 复用）验证一处。

最低验收：
```powershell
rg -n "BaseAwardItem|GetComponent<BaseAwardItem>|InstantiateAsync" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先：实例化 `BaseAwardItem.prefab` + `SetData(101011010,1)` 显真实图标+绿底板（非 null 组件、非降级）。

## 本轮 P2：货币/经验真名（先定语义，再实现）

目标：完成弹层/对话奖励里的货币/经验从「奖励 ×N」升级成真实名（经验/金币/灵玉…）。

要求：
- **先定元组语义**：以活服 12102 实包（或老端运行期 trace）确认 special_goods_list `{a,b,c}` 各位含义与货币类型来源；
  写清证据再改 `TaskReward`（**禁止凭两处矛盾的老端静态码猜**）。
- `ConfigNotNormalGoods` 进 `ClientConfigSync.SYNC_LIST` 同步；`GoodsModel.GetMappingTypeId` 接 ConfigNotNormalGoods
  （_type→goods_id）→ 货币 type 还原成真实 goods_id → `GetGoodsName`/`GetGoodsIcon`。
- 缺资源/语义未定 → 保持「奖励 ×N」+ 精确 blocker，**不臆造**。

最低验收：
```powershell
rg -n "ConfigNotNormalGoods|GetMappingTypeId|special_goods" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/日志优先：某任务奖励货币显真实名（经验/金币），非「奖励 ×N」。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **活服 Play 整合往返**：若会话允许，驱动到「进场景 → 点 NPC 弹 3D 立绘 → 接/交任务 → 完成弹层真实图标行 → 30004」并贴截图/日志。
2. **背包入口真实物品页**：主界面背包按钮打开显示真实物品格/空格/货币的完整页（复用 P1 的 `BaseAwardItem` + GoodsModel，需 bag 协议/`BagModel`）。
3. **立绘/格子构图微调**：对话立绘 scale/position/朝向按老端 talk_scale；找 count>1 真实奖励验证数量角标。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成"；禁止无入口/无真实数据/无验收的 UI shell；禁止假 NPC/假对白/假任务/假奖励/假物品名/假图标/假头像。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过；禁止卡 blocker 后自然退出（转 P3 或写下一轮包）。
- 禁止大面积手改 generated bind；禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径；
  禁止手改 GameRes 图集产物修通用问题（修导入器/转换器）；**禁止凭未验证的元组语义实现货币真名**。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker（文件/协议/key/字段）、下一轮任务包草案（不写"继续完善"）。
