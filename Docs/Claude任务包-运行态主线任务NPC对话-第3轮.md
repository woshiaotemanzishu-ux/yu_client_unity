# Claude任务包-运行态主线任务NPC对话-第3轮
日期: 2026-06-21

目标: 继续以老 Laya 客户端运行时 `http://127.0.0.1:8090/index.html` 为唯一可见真相, 追主线首登任务 `100010` 的 **点击任务 -> 角色走到 NPC 云霄月华 -> 打开真实对话 -> 接/交任务 -> 30004 推进** 端到端链路。第2轮已把 `MainUITaskTeamView` 真任务条补出来, 本轮必须把“看得见的任务条”推进到“玩家点它真的开始跑任务/对话”。

## 必读

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/LayaUI转换流水线.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/Shenxiao主界面运行时填充清单.md`
- `Docs/RuntimeCompare/MainUI-Bottom-第1轮.md`
- `Docs/RuntimeCompare/MainUI-TaskTeam-第2轮.md`
- 本任务包

## 总原则

1. **老端结论必须来自运行时。** 不允许用 Laya `.scene` 或源码推断代替浏览器运行页截图、`Laya.stage` 节点、console 协议日志。
2. **Unity 必须尽量走真实进游戏链路。** 优先真连服务端登录/进场景/角色/NPC/任务数据; 如环境无法真连, 必须用日志和截图证明具体 blocker, 不用 fake UI 宣称完成。
3. **不造假任务/NPC/对话/奖励。** 可回放老端运行时已观测到的真实 `30000/12101/12102` 数据用于 UI harness, 但必须标注边界; 端到端验收优先真实服务端。
4. **第3轮只解决主线 NPC 对话链路。** `MainUISkillView`、`FunctionOpenIcon`、`FirstRechargeBubble`、队伍、神殿觉醒只记录差异, 不扩散编码。
5. **游戏本体竖屏。** Web 横屏页面坐标只做锚点参考; 对比重点是角色移动、NPC 可见、对话弹层内容、协议链路、任务推进。
6. **不要碰无关脏文件。** 当前已知不要改: `Assets/_App/Fonts/DFPYuanW7 SDF.asset`、`Assets/_App/Fonts/FZYHJW SDF.asset`、`.playwright-cli/`、`output/`。

## 已知前置状态

- 第1轮提交: `ef2cd2799` 底部功能栏对齐。
- 第2轮提交: `ce92ab90a` 任务区对齐:
  - 左侧任务条显示 `[主] 云霄仙域` / `与云霄月华交谈 (0/1)`。
  - 任务文案来自真实 `config_npc[100101]`。
  - 神殿觉醒烘焙假占位已隐藏。
  - 已确认 UI-only harness blocker: `SceneManager.GetNpc(100101)=null`, 即端到端需要真进场景或补齐场景 NPC 链路。

## 老端运行态采样要求

老端入口:

- `http://127.0.0.1:8090/index.html`

可复用账号:

- 账号: `90990`
- 密码: `47071`
- 角色: `全久京`

如果该账号任务已推进导致首登链路不可复现, 注册新账号并创建新角色, 记录账号/角色/时间。

必须产出或复用:

- 任务区截图: 点击任务前。
- 角色移动/NPC 位置截图: 点击任务后角色走向 `云霄月华`。
- 对话弹层截图: `12101/12102` 后打开的 NPC 对话/任务动作。
- 任务推进日志: `30000 -> 12101 -> 12102 -> 30003/30004/30007 -> 30001/30000`。
- `Laya.stage` 节点树导出, 至少覆盖 `MainUITaskTeamView`、NPC 名牌、`DialogueView`。

已有证据可参考:

- `output/playwright/laya8090_after_create_role.png`
- `output/playwright/laya8090_after_create_role_stage.json`
- `output/_oldend_taskarea.png`
- `.playwright-cli/console-*.log`

## 老端源码锚点

- `D:\GitProject\yu_client\h5\src\mainUI\MainUITaskTeamView.ts`
  - `mouseEvent` / `MainUITaskItem.OnClick` / `SwitchView`
- `D:\GitProject\yu_client\h5\src\mainUI\MainUITaskItem.ts`
  - 点击任务条触发 `TaskModel.DoTask`
- `D:\GitProject\yu_client\h5\src\commonModel\TaskModel.ts`
  - `DoTask`
  - `GetTaskTipsMsgByMainUITaskItem`
  - 找 NPC / `USE_FLY_SHOE` / `Scene.Instance.MainRoleToNpc`
  - 完成分支 `REQUEST_CCMD_EVENT 30004`
- `D:\GitProject\yu_client\h5\src\scene\Scene.ts`
  - `MainRoleToNpc`: 距离判断、移动、停下转身、`SHOW_TASK`
- `D:\GitProject\yu_client\h5\src\commonController\DialogueController.ts`
  - `SHOW_TASK` -> `12101/12102` -> 对话动作 -> `30003/30004/30007`
- `D:\GitProject\yu_client\h5\src\task\TaskFinishView.ts`
  - 完成弹层领奖/提交 -> `30004`

## Unity 当前锚点

- `Assets\Scripts\Module\Core\MainUI\Views\MainUITaskTeamView.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUITaskItem.cs`
- `Assets\Scripts\Module\Core\Task\TaskModel.cs`
- `Assets\Scripts\Module\Core\Task\TaskController.cs`
- `Assets\Scripts\Module\Core\Dialogue\DialogueController.cs`
- `Assets\Scripts\Module\Core\Dialogue\DialogueView.cs`
- `Assets\Scripts\Module\Core\Scene\MainRoleAgent.cs`
- `Assets\Scripts\Module\Core\Scene\NpcRenderer.cs`
- `Assets\Scripts\Module\Core\Scene\SceneController.cs`
- `Assets\Scripts\Module\Core\Task\TaskFinishView.cs`
- `Assets\Prefabs\UI\MainUI\MainUIModule.prefab`

## 本轮 P0: 建立主线任务 NPC 对话运行态对比报告

先产出并持续更新:

- `Docs/RuntimeCompare/MainQuest-NpcDialogue-第3轮.md`

报告必须包含:

1. 老端运行时账号/角色/时间/截图/节点树/console 协议路径。
2. 老端点击 `100010` 后的可见链路: 任务条 -> 主角移动 -> NPC 名牌 -> 对话弹层 -> 动作按钮 -> 任务推进。
3. Unity 当前运行证据: 真连服务端优先; 若只能 harness, 必须写清楚 harness 与真连差异。
4. 差异表: NPC 是否在场景、主角是否能移动到 NPC、到达后是否 `ShowTask`、12101/12102 是否返回、对话弹层是否显示真实 NPC/任务/奖励、30004 是否推进。
5. 本轮修复项和仍然阻塞项。

最低验收:

```powershell
rg -n "DoTask|MainRoleToNpc|MoveToNpc|ShowTask|12101|12102|30004|USE_FLY_SHOE|DialogueView|NpcRenderer|SceneManager.GetNpc" Assets\Scripts D:\GitProject\yu_client\h5\src -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P1: 真场景 NPC 可见与可寻路

目标: Unity 进游戏后, `config_npc[100101]` 对应的 `云霄月华` 必须在场景中可查、可见、可作为任务目标。

要求:

- 优先用真登录/进场景链路验证 `SceneManager.GetNpc(100101)` 或等价查询返回真实 NPC。
- 若当前进场景链路未加载 NPC, 追 `SceneController` / 地图资源 / NPC 配置 / `NpcRenderer` / 名牌生成原因, 修真实链路。
- 不允许在 UI harness 中手工创建一个假 NPC 来过关; 如必须临时注入只可用于定位, 不可作为验收。
- 需要截图或日志证明:
  - NPC `云霄月华` 可见或不可见的原因。
  - NPC 坐标、sceneId、instanceId/name 与老端 `config_npc[100101]` 对齐。

最低验收:

```powershell
rg -n "config_npc|NpcRenderer|SceneManager|GetNpc|MainRoleAgent|MoveToNpc|NameBoard|100101" Assets\Scripts -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P2: 点击任务后主角走到 NPC 并触发 ShowTask

目标: 点击 `MainUITaskItem` 的主线任务后, Unity 走老端同构流程: 设置选中态 -> `TaskModel.DoTask` -> 找到 NPC -> `MainRoleAgent.MoveToNpc` -> 到达/停下/转身 -> `DialogueController.ShowTask(100101)`。

要求:

- 使用真实 `TaskModel` 和任务条点击入口, 不直接调用 `ShowTask` 冒充点击链路。
- 到达后才 `ShowTask`; 不允许一点击就瞬开对话, 除非老端距离已经在范围内且日志证明。
- 如果寻路/飞鞋/跨场景不完整, 写清楚 blocker 和当前最小修复。
- 留运行证据:
  - 点击前任务条截图。
  - 点击后移动/到达/NPC 对话触发日志。
  - 若失败, 精确到 `GetNpc=null`、坐标缺失、角色 agent 不存在、移动超时、场景未加载等。

最低验收:

```powershell
rg -n "EVT_TASK_SELECT_CHANGED|DoFindNpcTask|MoveToNpc|TurnTo|ShowTask\\(|send 12101|SceneManager.GetNpc" Assets\Scripts\Module\Core -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P3: 对话弹层与任务推进

目标: 到达 NPC 后, 对话弹层显示老端同构的 NPC 名、立绘/头像、任务动作、奖励; 点动作能走真实协议并推进任务。

要求:

- `DialogueController.ShowTask(100101)` 必须发真实 `12101`, `12102` 返回任务对话后刷新 `DialogueView`。
- 对话中任务动作必须走真实 `30003/30004/30007`, 不允许本地直接改任务状态。
- `TaskFinishView` 完成弹层已有截图证据, 本轮只在主线 NPC 对话链路需要时复用/补证。
- 若服务端无法返回或未连接, 写明协议层 blocker, 不画假对话。

最低验收:

```powershell
rg -n "DialogueController|DialogueView|12101|12102|30003|30004|30007|TaskFinishView|RewardSummary" Assets\Scripts\Module\Core -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P4: 只记录, 不扩散编码

只记录以下差异, 不在本轮展开:

- `MainUISkillView`: 技能 4 槽、自动战斗按钮、伙伴技能锁。
- `FunctionOpenIcon` / `FirstRechargeBubble`。
- 队伍系统完整创建/搜索/成员列表。
- `SetTempleAwaken` 完整移植。
- 击杀/采集/收集/副本文案的全部任务类型覆盖。

## 禁止事项

- 禁止只用源码或静态 prefab 结论替代运行证据。
- 禁止 fake NPC、fake dialogue、fake task progress、fake rewards。
- 禁止把 harness 回放当作真服端到端完成; 可以作为 UI/代码路径证据, 但要标注边界。
- 禁止提交 `output/`、`.playwright-cli/` 或字体 SDF 脏文件。
- 禁止绕开任务点击入口直接调用对话弹层后宣称 P2 完成。

## 交付格式

必须提交:

1. 玩家可见变化。
2. 老端运行时证据路径。
3. Unity 运行时/真连/Play harness 证据路径。
4. 差异报告 `Docs/RuntimeCompare/MainQuest-NpcDialogue-第3轮.md`。
5. 改动文件列表。
6. `dotnet build yu_client_unity.slnx -v:minimal` 结果。
7. 确认问题清单: 只写有证据的问题。
8. 下一轮建议: 若 NPC 对话端到端完成, 进入 `MainUISkillView`; 若未完成, 下一轮继续当前 blocker, 不转移焦点。

