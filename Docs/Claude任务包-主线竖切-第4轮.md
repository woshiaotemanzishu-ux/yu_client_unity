# Claude任务包-主线竖切-第4轮

日期：2026-06-21

目标：把第 3 轮已打通的「走到 NPC → 对话 → 完成弹层发 30004」继续推进成**更可信的玩家可感知度**：优先让
**奖励显示真实物品(图标/名称,替换 type_id 文本)** 与 **场景 NPC 顶真实名牌/缩放/朝向**。如某步被真实
资源/协议/运行环境阻塞,写清证据并切到下一个玩家可见缺口,不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第3轮.md` + `Docs/Shenxiao实施进度.md` 第 3 轮段

## 当前基线(第 3 轮已提交,commit `656490ba3`)

- P1:`MainRoleAgent.MoveToNpc`(直线接近 + 分轴滑行 + 卡死/超时兜底,无 A*)；`DoFindNpcTask` 走到 NPC 再 ShowTask。
- P2:`TaskFinishView`(原生 TEMP 壳)+ `TaskController.SubmitFinish`(30004)+ `TaskReward`(共用奖励解析,
  config_task 字段 23/24,按职业过滤)；`On12102`/`DialogueView` 展奖励摘要。
- 奖励当前以 **type_id × count 文本** 呈现(真实数值,但无图标/名称);GoodsModel/config_goods 未移植。

## 已确认仍缺(按价值排序)

1. **奖励真实物品呈现**:`TaskReward.Entry` 已有真实 type_id/count,但展示是裸数字。需 GoodsModel + config_goods
   把 type_id → 图标/名称/品质,复用已存在的 `BaseAwardItem`(`SetData(typeId,num,...)`,其 GoodsModel 取图标是 TODO)。
2. **NpcRenderer 名牌/缩放/朝向**:config_npc 已导入,`NpcRenderer` 仍用 NpcId 占位、未挂名牌、未用 icon_scale/brith_rot。
3. **对话/弹层立绘头像**:用 config_npc.icon/image 接 NPC 立绘或头像(走 ResManager),缺资源写精确 blocker。
4. **活服 Play 往返验证**:走到 NPC 真实位移、12101/12102 实包、30004 提交后 30001 刷新——需可登录的活服会话。
5. 跨场景 `USE_FLY_SHOE`/飞鞋、A* 寻路、TaskFinishView 自动提交倒计时(老端 close_time)仍缺。

## 老端源码锚点

- `D:\GitProject\yu_client\h5\src\task\TaskFinishView.ts:206-213`(`new EquipmentItem` → `GoodsModel.GetMappingTypeId(vo[0],vo[1])`
  → `item.SetData(type_id, vo[2])`),`commonModel/GoodsModel.ts`(需定位 `GetMappingTypeId` 与 config_goods 映射规则)。
- `D:\GitProject\yu_client\h5\src\scene\sceneobj\Npc.ts:92-169`(NPC 名牌/朝向/缩放,对照 config_npc 字段
  name/title/icon_scale/talk_scale/brith_rot)。
- config 出处:`config_goods`(物品图标/名称/品质,需确认是否在 `ClientConfigSync.SYNC_LIST` 内)、`config_npc.json`。

## 本轮 P0:保护可运行基线

- 确认 worktree 是否干净;不干净先说明改动归属(可能有 Codex/其他 worker 并行改动)。
- 快速核对第 3 轮链路仍在:`rg -n "MoveToNpc|TaskReward|SubmitFinish|class TaskFinishView" Assets/Scripts/Module/Core -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错;新建 .cs 后必经 Unity 重导入(`.csproj` 显式列文件,
  dotnet 单独 build 看不到新文件 → 让 Unity 刷新或临时手加 `<Compile Include>` 仅作本地校验,`.csproj` 已 gitignore)。
- 不重做第 3 轮,除非发现真实回归。

## 本轮 P1:奖励显示真实物品(图标 + 名称)

目标:把 `TaskFinishView` / 对话里的奖励从「物品 {type_id} ×{count}」升级成真实图标 + 名称。

要求:
- 移植/接入 `GoodsModel.GetMappingTypeId(type,subId)` 与 config_goods(图标 res/名称/品质);确认 config_goods 是否已同步,
  未同步则进 `ClientConfigSync.SYNC_LIST_SERVER` 再生成(对标 config_task/config_npc 路线)。
- `TaskReward.Entry` 增补 GoodsModel 映射后的真实 type_id;`TaskFinishView` 用 `BaseAwardItem`(复用,勿手搓)展图标 + count;
  对话奖励摘要可保留文本或换图标行。图标走 `ResManager.SetImageAsync`(勿绕 ResManager / 勿拼 Addressable 字符串)。
- 缺图标资源(.spriteatlas/png 未导)→ 降级显示名称 + count,并写精确 blocker(缺哪个 key)。

最低验收:
```powershell
rg -n "GoodsModel|GetMappingTypeId|BaseAwardItem|config_goods" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/日志/运行期单测优先:某有 award_list 的任务完成弹层显示真实物品图标 + 名称 + 数量(非裸 type_id)。

## 本轮 P2:场景 NPC 真实名牌 / 缩放 / 朝向

目标:`NpcRenderer` 用 config_npc 给场景 NPC 挂真实 name/title、按 icon_scale 缩放、按 brith_rot 定朝向。

要求:
- `NpcConfigs.Get(npcId)` 取 name/title/icon_scale/brith_rot;名牌走已有 3D/UI 文本路线(勿 transform.Find,用 Bind/合成台 API)。
- 缺字段/缺配置降级(不写假名);对照老端 Npc.ts:92-169 的名牌层级与缩放/朝向赋值。

最低验收:
```powershell
rg -n "class NpcRenderer|NpcConfigs|icon_scale|brith_rot" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/日志优先:进场景后 NPC 顶显示真实名字(config_npc.name),缩放/朝向与配置一致。

## 本轮 P3:被 P1/P2 卡住超 15 分钟时的可见 fallback(按序)

1. **对话/弹层立绘头像**:用 config_npc.icon/image 接 NPC 头像/立绘(走 ResManager),缺资源写精确 blocker。
2. **活服 Play 往返脚本化**:若 MCP/会话允许,脚本化驱动到「走到 NPC → 12101 → 完成弹层 → 30004」并贴日志证据。
3. 背包入口:主界面背包按钮打开能显示真实物品格/空格/货币的完整页(复用 P1 的 GoodsModel)。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当「完成」;禁止无入口/无真实数据/无验收的 UI shell;禁止假 NPC/假对白/假任务/假奖励/假物品名。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过;禁止卡 blocker 后自然退出(转 P3 或写下一轮包)。
- 禁止大面积手改 generated bind;禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/静态/运行期单测证据、
确认 blocker(文件/协议/key/字段)、下一轮任务包草案(不写"继续完善")。
