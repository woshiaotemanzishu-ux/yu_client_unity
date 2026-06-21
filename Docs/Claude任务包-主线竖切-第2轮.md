# Claude任务包-主线竖切-第2轮

日期：2026-06-21

目标：把第 1 轮的“NPC 可见 + 任务点击入口”继续推进成玩家可感知的主线闭环。优先做 NPC 对话子系统与任务点击执行链；如果某一步被真实资源、协议或运行环境阻塞，必须写清证据并立刻切到下一个玩家可见缺口，不允许只做日志、文档或空 UI。

## 必读

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第1轮.md`

## 当前基线

第 1 轮已提交：

- `0f66cca9b`：P0 资源门，主竖切结构闭包入库，关键 Addressables key 在 clean checkout 可解析。
- `0dd7b327b`：P1 NPC，12100/12103/12020 真实数据进入场景 NPC 渲染层。
- `6581c648b`：P2 任务点击，`TaskModel.DoTask` 最小入口 + 任务项点击链 + 选中态。
- `25a1a76a5`：第 1 轮收尾文档，统一下一 blocker 为 NPC 对话子系统。

已确认仍缺：

- Unity 侧没有完整 `DialogueController/DialogueModel/DialogueView`。
- `TaskModel.DoTask` 对 Talk/StartTalk/EndTalk 只到待接路径，还不能打开真实 NPC 对话。
- `TaskFinishView` 未迁移，完成任务分支不能落地。
- `Scene.MainRoleToNpc`、自动寻路、`USE_FLY_SHOE` 未迁移。
- `config_npc`、`configNpcTalk`、`ClientNpcConfig` 的 Unity 数据接入需要用真实源证明，不能写死。

## 老端源码锚点

优先从这些文件搬行为，不允许凭感觉重写：

- `D:\GitProject\yu_client\h5\src\commonController\DialogueController.ts`
- `D:\GitProject\yu_client\h5\src\commonModel\DialogueModel.ts`
- `D:\GitProject\yu_client\h5\src\dialogue\DialogueView.ts`
- `D:\GitProject\yu_client\h5\src\dialogue\DialogueTaskVo.ts`
- `D:\GitProject\yu_client\h5\src\dialogue\DialogueNodeVo.ts`
- `D:\GitProject\yu_client\h5\src\dialogue\NpcDialogVo.ts`
- `D:\GitProject\yu_client\h5\src\task\TaskFinishView.ts`
- `D:\GitProject\yu_client\h5\src\commonModel\TaskModel.ts`
- `D:\GitProject\yu_client\h5\src\scene\Scene.ts`
- `D:\GitProject\yu_client\h5\src\scene\sceneobj\Npc.ts`
- `D:\GitProject\yu_client\cdn\resource\config\client\configNpcTalk.json`
- `D:\GitProject\yu_client\cdn\resource\config\client\ClientNpcConfig.json`
- `D:\GitProject\yu_client\cdn\resource\config\server\config_npc.json`

## 本轮 P0：不要空转，先保护可运行基线

要求：

- 开始前确认 worktree 是否干净；如果不干净，先说明是谁的改动范围，不要覆盖。
- 快速检查第 1 轮关键链路仍存在：`NpcRenderer`、`TaskModel.DoTask`、`MainUITaskItem` 点击、Addressables P0 key。
- 不允许把本轮时间花在重做第 1 轮资源门，除非发现真实回归。

最低验收：

```powershell
git status --short
rg -n "class NpcRenderer|NpcAdded|NpcChanged|DoTask|NowSelectTaskId" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P1：NPC 对话子系统最小闭环

目标：点击 Talk/StartTalk/EndTalk 任务时，Unity 能按老端逻辑定位 NPC，并打开真实对话入口或给出精确阻塞，而不是只打印“待接”。

要求：

- 新增或补齐 Unity 对话模块，命名和目录按现有模块风格落位，例如 `Assets/Scripts/Module/Core/Dialogue`。
- 从老端 `DialogueController.ts`、`DialogueModel.ts`、`DialogueView.ts` 提取最小等价行为：
  - 协议号、事件名、数据字段必须来自老端源码。
  - `SHOW_TASK` 或等价事件必须接到真实 UI/任务链路。
  - 对话内容必须来自 `configNpcTalk.json` 或服务端协议数据，不允许写死一句假对白冒充。
  - NPC 身份、名字、任务状态必须来自 `NpcVo`、`ClientNpcConfig`、`config_npc` 或 12100/12020 真实数据。
- 如果 `DialogueView` 的 Laya UI 转换产物已存在，优先复用 generated bind/prefab；如果不存在，允许做最小可交互原生 Unity UI，但必须标注为临时壳，并且数据/入口必须是真的。
- 如果协议或资源缺失导致不能打开完整视图，必须留下可验证的 blocker：缺哪个 key、哪个协议、哪个 prefab、哪个字段。

最低验收：

```powershell
rg -n "class .*Dialogue|DialogueModel|DialogueController|SHOW_TASK|configNpcTalk|DialogueView" Assets/Scripts -S
dotnet build yu_client_unity.slnx -v:minimal
```

Play 或运行日志验收优先级更高：

- 点击主界面任务项后，能看到选中态变化。
- Talk/StartTalk/EndTalk 任务进入 NPC 对话入口。
- 日志能串起：任务点击 -> `DoTask` -> NPC 定位 -> 对话数据 -> 对话 UI 打开或精确 blocker。

## 本轮 P2：主角到 NPC 的可见动作

目标：角色不能再只是站着。先做最小可见行为：任务点击后，主角朝 NPC 或移动到 NPC 附近；如果完整寻路未完成，也必须让玩家能看到“任务驱动角色发生动作”。

要求：

- 对照老端 `Scene.MainRoleToNpc`、`TaskModel.DoTask` 找出最小链路。
- 优先复用 Unity 现有 `MainRoleAgent`、`MainRoleFlow`、`SceneInput`、`SceneManager`，不要另起一套移动系统。
- 如果没有寻路网格，允许做直线移动/朝向 NPC 的临时实现，但必须标明与老端差异，并保证不会阻塞后续寻路替换。
- 对跨地图、飞鞋、传送分支只记录真实 blocker，不要假装完成。

最低验收：

```powershell
rg -n "MainRoleToNpc|MoveToNpc|MainRoleAgent|SceneInput|DoTask" Assets/Scripts/Module/Core/Scene Assets/Scripts/Module/Core/Task -S
dotnet build yu_client_unity.slnx -v:minimal
```

Play 或运行日志验收优先级更高：

- 点击任务后，主角坐标、朝向或目标点发生变化。
- 对不可达 NPC，输出原因来自真实场景/NPC 数据。

## 本轮 P3：任务完成弹层或下一个可见 UI

如果 P1/P2 被真实 blocker 卡住超过 15 分钟，不要结束；改做下一个最接近主线的可见面，按顺序选择：

1. `TaskFinishView`：任务完成弹层真实入口和老端数据字段。
2. 背包入口：让主界面背包按钮打开一个能显示真实物品格/空格/货币状态的完整页，而不是残缺壳。
3. 角色属性入口：让角色属性页显示真实角色字段，并能关闭/切换基础标签。

每个 fallback 都必须带老端源码锚点、Unity 入口、构建结果和可见验收证据。

## 多 agent 拆分建议

本轮允许并建议拆三个方向并行：

- 老端链路 agent：只读 `yu_client`，提取 `Dialogue/Task/Scene` 的协议、事件、字段、UI 入口，不写 Unity。
- Unity 实现 agent：只改 `yu_client_unity`，按老端 agent 输出实现最小可见闭环。
- 验收 agent：只做 diff/build/Play/log 检查，只报告有证据的问题，不猜测。

## 禁止事项

- 禁止再提交纯文档或纯日志作为“完成”。
- 禁止新增没有入口、没有真实数据、没有验收的 UI shell。
- 禁止写假 NPC、假对白、假任务、假背包数据来制造进展。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过。
- 禁止卡在一个 blocker 后自然退出；必须转入 P3 fallback 或写下一轮可执行任务包。
- 禁止大面积手改 generated bind；需要改转换器或运行时层时必须说明原因。

## 本轮交付格式

Claude worker 结束前必须输出并尽量提交：

- 本轮玩家可见变化：进入游戏后玩家能看到什么新增行为。
- 改动文件列表。
- 每个核心行为对应的 Laya 源码锚点。
- `dotnet build` 结果。
- Play/日志/静态验证证据。
- 确认 blocker：必须包含文件、协议、资源 key、日志或字段证据。
- 下一轮最高价值任务包草案，不能只写“继续完善”。
