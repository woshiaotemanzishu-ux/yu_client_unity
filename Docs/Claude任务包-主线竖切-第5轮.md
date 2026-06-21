# Claude任务包-主线竖切-第5轮

日期：2026-06-21

目标：把第 4 轮的「奖励真实物品名 + NPC 真实名牌/缩放/朝向」继续推进成**成品外观**：优先让
**奖励/物品显示真实图标（落地 goodsIcon，补齐"图标+名称"里缺的图标半）** 与 **对话弹层 NPC 真实头像/立绘**。
如某步被真实资源/协议/运行环境阻塞，写清证据并切到下一个玩家可见缺口，不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第4轮.md` + `Docs/Shenxiao实施进度.md` 第 4 轮段

## 当前基线（第 4 轮已提交，commit `3dff3442c` P1 / `d170a5f54` P2）

- P1：`GoodsModel` + `config_goods` 已接；`TaskReward.ToText` / `TaskFinishView` / 对话奖励摘要 `On12102` 显示**真实物品名**；
  `BaseAwardItem.RefreshIcon` 已接 `GoodsModel` + `ResManager.SetImageAsync`（图标**代码就绪，等资源**）。
- P2：`NpcRenderer` 用 `config_npc` 给场景 NPC 挂**真实名牌**（屏幕跟随 TMP，称号金/名字青）、按 `icon_scale` 缩放、按 `brith_rot` 朝向。
- 奖励/物品**图标当前降级隐藏**（goodsIcon png 未导入）；NPC **头像/立绘未接**。

## 已确认仍缺（按价值排序）

1. **真实物品图标落地**：`BaseAwardItem.RefreshIcon` 已走 `ResManager.SetImageAsync(GameResPath.GetGoodsIconPath(icon))`，
   但 `Assets/GameRes/resource/game/goodsIcon/` 为空 → 加载 false 降级。需把 yu_client `cdn/resource/game/goodsIcon/*.png`
   导入 Unity（SpriteImporter / Addressable），并补品质底板 `com_goods_plate_{color}`（common 图集）。完成即奖励/物品显示真实图标。
2. **NPC 对话头像/立绘**：`config_npc.image`（头像 id）/`icon` → `DialogueView` 弹层头像（走 ResManager）。缺资源写精确 blocker。
3. **活服 Play 往返验证**：登录活服 → 进场景 → NPC 顶名牌可见 + 完成弹层真实物品名/图标 → 30004 → 30001 刷新任务栏。
4. **货币/经验真名**：客户端 `ConfigNotNormalGoods`（未同步）+ `TaskReward` 保留货币 type → exp/coin 显真名（替换"奖励 ×N"）。
5. 背包入口真实物品页（复用 `GoodsModel` + `BaseAwardItem`，需 bag 协议/`BagModel`）。

## 老端源码锚点

- 图标：`GoodsModel.ts` `GetGoodsIcon`/`GetGoodsPath`、`GameResPath.GetGoodsIconPath`（Unity 已对等
  `resource/game/goodsIcon/{icon}.png`）；品质底板：`BaseAwardItem.ts` `SetData` → `AtlasUrl("common","com_goods_plate_"+color)`
  （common 图集存 `com_goods_plate_0..8`）。
- 头像/立绘：`Npc.ts` `NpcClothChange` / `config_npc.image`（NpcVo.head_icon）；对话头像装配看 DialogueController/View 路线。
- 图集导入：菜单 `神霄/资源` SpriteImporter（对标 `Shenxiao重构实施方案.md` §5.3 SpriteImporter 流水线）。

## 本轮 P0：保护可运行基线

- 确认 worktree 是否干净；不干净先说明改动归属（可能有 Codex/其他 worker 并行）。
- 核对第 4 轮链路仍在：`rg -n "GoodsModel|GetGoodsName|class NpcRenderer|CreateNameplate|ApplyBrithRot" Assets/Scripts/Module/Core -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错；新建 .cs 必经 Unity 重导入（`.csproj` 显式列文件，dotnet 单独 build 看不到新文件）。
- 不重做第 4 轮，除非发现真实回归。

## 本轮 P1：真实物品图标落地（图标 + 品质底板）

目标：把 `BaseAwardItem` 的图标从"降级隐藏"变成真实 goodsIcon + 品质底板。

要求：
- 用 SpriteImporter（或既有图集导入路线）把 `cdn/resource/game/goodsIcon/*.png` 导入
  `Assets/GameRes/resource/game/goodsIcon/`（至少覆盖测试任务奖励物品的 icon，如 17/10）；确认
  `GameResPath.GetGoodsIconPath` 产出的 key 能被 ResManager/Addressables 解析。
- 品质底板 `com_goods_plate_{color}`：确认 common 图集导入路径，`BaseAwardItem` 用 `item_bg` 设底板（对标 `AtlasUrl("common",...)`）。
- 不绕过 ResManager / 不拼 Addressable 字符串；缺某 icon → 仍降级 + 精确 blocker（缺哪个 key）。

最低验收：
```powershell
rg -n "GetGoodsIconPath|com_goods_plate|SetImageAsync" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/截图优先：某有 award_list 的任务完成弹层显示真实物品【图标 + 品质底板 + 数量 + 名称】（非降级隐藏）。

## 本轮 P2：NPC 对话弹层真实头像/立绘

目标：`DialogueView` 弹层显示 NPC 真实头像（`config_npc.image`）或立绘（`config_npc.icon`），走 ResManager。

要求：
- `NpcConfigs` 已有 `Image`/`Icon` 字段；`DialogueView` 加头像位，`ResManager.SetImageAsync` 加载；缺资源降级 + 精确 blocker。
- 对照老端 `Npc.ts` `NpcClothChange` / 对话头像装配，确认 `image` → 资源路径映射（头像图集/散图）。

最低验收：
```powershell
rg -n "Image|head_icon|SetImageAsync|class DialogueView" Assets/Scripts/Module/Core/Dialogue -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/截图优先：点 NPC 对话，弹层显示真实 NPC 头像/立绘（非占位/非空）。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **活服 Play 往返脚本化**：若 MCP/会话允许，驱动到「进场景 → NPC 名牌可见 → 完成弹层真实物品名 → 30004」并贴日志/截图证据。
2. **货币/经验真名**：同步 `ConfigNotNormalGoods` + `TaskReward` 保留货币 type → exp/coin 显真名（替换"奖励 ×N"）。
3. **背包入口真实物品页**：主界面背包按钮打开显示真实物品格/空格/货币的完整页（复用 P1 的 GoodsModel + BaseAwardItem）。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成"；禁止无入口/无真实数据/无验收的 UI shell；禁止假 NPC/假对白/假任务/假奖励/假物品名/假图标。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过；禁止卡 blocker 后自然退出（转 P3 或写下一轮包）。
- 禁止大面积手改 generated bind；禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径；
  禁止手改 GameRes 图集产物修通用问题（修导入器/转换器）。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker（文件/协议/key/字段）、下一轮任务包草案（不写"继续完善"）。
