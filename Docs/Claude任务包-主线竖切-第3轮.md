# Claude任务包-主线竖切-第3轮

日期：2026-06-21

目标：把第 2 轮的“点任务 → 转身 → 弹真实对话”推进成玩家可感知的完整交互闭环。优先做 **主角走到 NPC（到达后才开对话）** 与 **任务完成弹层（TaskFinishView + 30004 奖励）**；如某步被真实资源/协议/运行环境阻塞，写清证据并切到下一个玩家可见缺口，不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第2轮.md` + 本轮 `Docs/Shenxiao实施进度.md` 第 2 轮段

## 当前基线（第 2 轮已提交）

- `2a3ae18bc` P1：NPC 对话子系统最小闭环。点 Talk/StartTalk/EndTalk → `DialogueController.ShowTask` 发 12101 → 12102 → `config_talk` 真实对话 → 动作节点发 30003/30004/30007。`Module/Core/Dialogue/*` 全新增。
- `26fad79f3` P2：`MainRoleAgent.FaceTowardPixel` + `TaskModel.DoFindNpcTask` 先转身再 ShowTask。
- config_npc(265)/config_talk(959) 经 `ClientConfigSync` 同步进 GameRes（可再生成）。

## 已确认仍缺（按价值排序）

1. **走到 NPC**：当前点任务是“原地转身 + 立刻弹对话”。老端是 `MainRoleToNpc` 走到 NPC 身边（dist≤2.5 逻辑格）**到达后**才 `SHOW_TASK`。`MainRoleAgent` 只有摇杆移动，无“移动到目标点”。
2. **完成弹层**：`TaskModel.DoFinishTask` 仍是 blocker（非对话的“完成”分支不能落地）；`TaskFinishView` + 30004 奖励未移植。
3. **对话奖励**：`On12102` 里 `award_list`（`special_goods_list` 经 ErlangParser 按职业过滤 + `award_list`）未移植，对话内不展奖励。
4. **NpcRenderer 名牌/缩放/朝向**：config_npc 现已导入，`NpcRenderer` 仍用 NpcId 占位、未挂名牌、未用 icon_scale/brith_rot。
5. **DialogueView 临时壳**：待 LayaUI 转换产出 `DialogueViewBind`/prefab 后替换原生壳；NPC 立绘/头像（config_npc.icon/image）未接。
6. 跨场景 `USE_FLY_SHOE`/飞鞋、12020 头顶任务标 sprite 资源仍缺。

## 老端源码锚点

- `D:\GitProject\yu_client\h5\src\scene\Scene.ts:1417-1472` `MainRoleToNpc`（dist=2.5、check_distance=420、`action_func` 到达回调里 `SetDirection` + `Fire(SHOW_TASK, npc_info.instance_id)`），`:1479-1490` `MainRoleMoveAndCancel`→`MainRoleMove`（A* 寻路移动）。
- `D:\GitProject\yu_client\h5\src\task\TaskFinishView.ts:75-79, 241-245`（完成弹层确认 → `Fire(REQUEST_CCMD_EVENT, 30004, task_id)` → Close）。
- `D:\GitProject\yu_client\h5\src\commonController\DialogueController.ts:45-110` `On12102` 的 `award_list` 装配（`special_goods_list` + `ErlangParser` + 职业过滤）。
- `D:\GitProject\yu_client\h5\src\scene\sceneobj\Npc.ts:92-169`（NPC 名牌/朝向/缩放，对照 config_npc 字段）。
- config 出处：`cdn/resource/config/server/config_npc.json`（name/title/icon/icon_scale/talk_scale/brith_rot），`config_talk.json`（content）。

## 本轮 P0：保护可运行基线

- 确认 worktree 是否干净；不干净先说明改动归属。
- 快速核对第 2 轮链路仍在：`rg -n "class DialogueController|FaceTowardPixel|CC_NPC_TASK_LIST|ShowTask" Assets/Scripts/Module/Core -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错。
- 不重做第 2 轮，除非发现真实回归。

## 本轮 P1：主角走到 NPC，到达后才开对话

目标：把 `DoFindNpcTask` 的“原地转身 + 立刻 ShowTask”改成老端语义——**走到 NPC 身边再 ShowTask**。

要求：
- 给 `MainRoleAgent` 加“移动到目标像素点”模式（对标 `MainRoleToNpc`→`MainRoleMove`）：
  - 优先直线移动 + 现有 `IsBlockPixel` 分轴碰撞滑行（复用 `StepMove` 内核），到达 dist 内触发到达回调。
  - 无 A* 寻路：必须做**卡住检测**（连续 N 帧无位移进展）或**超时兜底**，到达不了时也要把对话开出来，绝不软锁；并 log 真实差异（直线 vs 老端 A*）。
  - 玩家推摇杆即取消自动移动（自动移动让位手动）。
- `DoFindNpcTask`：定位 NPC → `MainRoleAgent.Current.MoveToNpc(npc.X, npc.Y, dist, () => DialogueController.Instance.ShowTask(task.Id))`；若已在 dist 内则立即 ShowTask。
- 已在身边/同格的 NPC 不应来回抖动。

最低验收：
```powershell
rg -n "MoveToNpc|FaceTowardPixel|DoFindNpcTask|ShowTask" Assets/Scripts/Module/Core/Scene Assets/Scripts/Module/Core/Task -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/日志优先：点任务后主角坐标/相机发生位移并最终停在 NPC 附近，停下后才弹对话；到不了的 NPC 输出真实卡住/超时证据。

## 本轮 P2：任务完成弹层 TaskFinishView + 30004

目标：非对话的“完成”分支能落地——`DoFinishTask` 不再只打 blocker。

要求：
- 移植 `TaskFinishView`（老端 `task/TaskFinishView.ts`）最小版：展示任务名 + 奖励列表（来自 `TaskVo`/config）+ “提交”按钮 → 发 30004。无生成 Bind/prefab 则做最小原生壳（标 TEMP），但奖励/入口必须真。
- `TaskModel.DoFinishTask` → 打开 TaskFinishView（对标 `Fire(TASK_OPEN_VIEW, "TaskFinishView")`），不跳弹层直发协议。
- 顺带把对话内 `On12102` 的 `award_list` 接上（`special_goods_list` ErlangParser + 职业过滤），对话/弹层共用奖励解析。

最低验收：
```powershell
rg -n "TaskFinishView|DoFinishTask|CC_TASK_FINISH|award_list|ErlangParser" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/日志优先：全步完成的非对话任务点击后弹完成层、点提交发 30004、服务端推 30001 后任务栏刷新。

## 本轮 P3：被 P1/P2 卡住超 15 分钟时的可见 fallback（按序）

1. **NpcRenderer 名牌**：config_npc 已导入 → 给场景 NPC 挂真实 name/title（`NpcConfigs.Get`），缩放用 icon_scale、朝向用 brith_rot；缺则降级，不写假名。
2. **DialogueView 立绘/头像**：用 config_npc.icon/image 接 NPC 立绘或头像（走 ResManager），缺资源写精确 blocker。
3. 背包入口：主界面背包按钮打开能显示真实物品格/空格/货币的完整页。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 多 agent 拆分建议

- 老端链路 agent：只读 yu_client，提 `MainRoleToNpc`/`MainRoleMove`/`TaskFinishView`/award 解析的参数、回调、字段。
- Unity 实现 agent：只改 yu_client_unity，按老端 agent 输出实现。
- 验收 agent：只做 diff/build/Play/log 检查，只报有证据的问题。

## 禁止事项

- 禁止纯文档/纯日志当“完成”；禁止无入口/无真实数据/无验收的 UI shell；禁止假 NPC/假对白/假任务/假奖励。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过；禁止卡 blocker 后自然退出（转 P3 或写下一轮包）。
- 禁止大面积手改 generated bind；自动移动**必须有卡住/超时兜底**，绝不软锁。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/静态证据、确认 blocker（文件/协议/key/字段）、下一轮任务包草案（不写“继续完善”）。
