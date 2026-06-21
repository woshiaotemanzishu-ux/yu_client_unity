# Claude任务包-运行态主界面对比-第2轮
日期: 2026-06-21

目标: 继续用老 Laya 客户端运行时 `http://127.0.0.1:8090/index.html` 作为唯一可见真相, 从主界面底部之后进入 **MainUITaskTeamView 任务/队伍区域**。本轮重点不是做假任务, 而是把玩家首屏最显眼的任务栏、任务点击、自动走向 NPC/对话/完成弹层链路按老端运行时逐项对比并补齐。

## 必读

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/LayaUI转换流水线.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/Shenxiao主界面运行时填充清单.md`
- `Docs/RuntimeCompare/MainUI-Bottom-第1轮.md`
- 本任务包

## 总原则

1. **老端结论必须来自运行时, 不是 `.scene`。** Laya 编辑器 scene 只能辅助找节点名, 最终结论必须来自浏览器运行页截图和 `Laya.stage` 节点树。
2. **Unity 必须给运行证据。** 允许看 prefab/Bind, 但验收必须有 Play 态截图、编辑期真机渲染截图, 或可复现的日志/节点证据。
3. **不造假数据。** 任务、NPC、对话、背包、奖励、红点都走真实配置和协议。没有服务端数据就写 blocker, 不画 fake/stub。
4. **本轮范围收敛。** 主做 `MainUITaskTeamView` 和任务点击链路; `MainUISkillView`、`FunctionOpenIcon`、`FirstRechargeBubble` 只记录差异, 不扩散编码。
5. **游戏是竖屏。** 老端 Web 可在横向页面中居中展示, 坐标只能做锚点参考; 重点对比相对布局、显隐条件、按钮数量、文本、点击结果。
6. **不要碰无关脏文件。** 当前已知不要改: `Assets/_App/Fonts/DFPYuanW7 SDF.asset`、`Assets/_App/Fonts/FZYHJW SDF.asset`、`.playwright-cli/`、`output/`。

## 运行态采样基线

老端入口:

- `http://127.0.0.1:8090/index.html`

可复用账号:

- 账号: `90990`
- 密码: `47071`
- 角色: `全久京`

已有老端证据:

- `D:\GitProject\yu_client_unity\output\playwright\laya8090_after_create_role.png`
- `D:\GitProject\yu_client_unity\output\playwright\laya8090_after_create_role_stage.json`

如果已有证据不足以展开 `MainUITaskTeamView`, 必须重新打开老端运行页, 登录同一账号; 若任务状态已被推进导致首登链路缺失, 可注册新账号并创建新角色, 记录账号/角色和截图路径。运行时节点树必须从 `Laya.stage` 导出, 不用静态 `.scene` 代替。

## 老端源码锚点

- `D:\GitProject\yu_client\h5\src\commonController\MainUIController.ts`
  - `InitMainUI`: 打开首屏 HUD views, 包含 `MainUITaskTeamView`。
- `D:\GitProject\yu_client\h5\src\mainUI\MainUITaskTeamView.ts`
  - `LoadSuccess`: 位置、任务/队伍 tab 初始态、`SwitchView(Task)`。
  - `UPDATE_ALL_TASK_FROM_30000`: 任务数据刷新。
  - `SwitchView`: 任务/队伍切换、打开 `TaskView`。
  - 任务项点击: 走 `TaskModel.DoTask`。
- `D:\GitProject\yu_client\h5\src\mainUI\MainUITaskItem.ts`
  - 单条任务显示、标题、进度/tips、选中态、完成态、点击。
- `D:\GitProject\yu_client\h5\src\commonModel\TaskModel.ts`
  - `UPDATE_ALL_TASK_FROM_30000`
  - `DoTask`
  - `GetHasReceiveTaskList`
  - `Find` / NPC / `USE_FLY_SHOE` / `Scene.Instance.MainRoleToNpc`
  - `REQUEST_CCMD_EVENT` 30000/30004
- `D:\GitProject\yu_client\h5\src\commonController\TaskController.ts`
  - `30000` 任务列表解析
  - `30004` 完成/提交
- `D:\GitProject\yu_client\h5\src\scene\Scene.ts`
  - `MainRoleToNpc`: 走到 NPC 附近、停下转身后才 `SHOW_TASK`。
- `D:\GitProject\yu_client\h5\src\commonController\DialogueController.ts`
  - `SHOW_TASK` -> 12101/12102 -> 对话任务 -> 30003/30004/30007。

## Unity 当前锚点

- `Assets\Prefabs\UI\MainUI\MainUIModule.prefab`
- `Assets\Scripts\Generated\UI\MainUI\MainUITaskTeamViewBind.cs`
- `Assets\Scripts\Generated\UI\MainUI\MainUITaskItemBind.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUITaskTeamView.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUITaskItem.cs`
- `Assets\Scripts\Module\Core\Task\TaskModel.cs`
- `Assets\Scripts\Module\Core\Task\TaskController.cs`
- `Assets\Scripts\Module\Core\Scene\MainRoleAgent.cs`
- `Assets\Scripts\Module\Core\Dialogue\DialogueController.cs`
- `Assets\Scripts\Module\Core\Task\TaskFinishView.cs`

## 本轮 P0: 建立任务/队伍区域运行态对比报告

先产出并持续更新:

- `Docs/RuntimeCompare/MainUI-TaskTeam-第2轮.md`

报告必须包含:

1. 老端运行时截图路径、采样账号/角色/时间、stage/canvas 尺寸。
2. 老端 `MainUITaskTeamView` 节点树: root 坐标、任务/队伍 tab、任务列表容器、任务条目、主线箭头、队伍空态/按钮、显隐条件。
3. 老端当前首登任务文本、任务类型、任务条目数量、是否有引导手指/选中态/完成态。
4. Unity 当前截图路径和采样方式。
5. Unity prefab/Bind/运行态节点清单。
6. 差异表: 位置、显隐、任务条文本、任务数量、点击结果、任务/队伍 tab、红点/引导/箭头、队伍空态。
7. 本轮修复项和后续只记录项。

最低验收:

```powershell
Test-Path output\playwright\laya8090_after_create_role.png
Test-Path output\playwright\laya8090_after_create_role_stage.json
rg -n "MainUITaskTeamView|MainUITaskItem|UPDATE_ALL_TASK_FROM_30000|DoTask|MainRoleToNpc|SHOW_TASK|30000|30004" D:\GitProject\yu_client\h5\src Assets\Scripts -S
```

## 本轮 P1: 任务列表首屏对齐

目标: 玩家进入游戏后, Unity 主界面左侧/任务区域必须能显示真实任务条目, 文本、任务类型、选中态、完成态、主线箭头/引导记录要和老端运行时一致。

要求:

- `MainUITaskTeamView` 初始态对齐老端 `SwitchView(Task)`。
- 任务数据只能来自真实 `TaskModel`/`30000`/配置解析; 不允许硬编码一个假任务。
- `MainUITaskItem` 标题、任务类型颜色、tips 富文本、完成图标、选中态要按老端运行时差异补齐。
- 如果老端运行时有引导手指/主线箭头/特效, Unity 本轮至少要记录明确差异; 若已有 Story/Guide 数据链路可用, 才可接入。
- 队伍 tab/队伍空态只对比和记录, 不做完整队伍系统。

最低验收:

```powershell
dotnet build yu_client_unity.slnx -v:minimal
rg -n "EVT_TASK_LIST_UPDATED|GetTaskListForMainUI|BuildMainUITips|MainUITaskItem|_img_select|_img_done|_box_finger_con|_box_main_line" Assets\Scripts\Module\Core -S
```

必须留证:

- Unity 主界面任务区域截图。
- 截图中的任务文本/数量与老端运行时对比结论。
- 若 Unity 没有任务, 必须证明是 30000 未返回/账号状态/服务端数据问题, 不允许写“已对齐”。

## 本轮 P2: 任务点击链路真实可走

目标: 点击任务条目后, Unity 走老端同构链路: `TaskModel.DoTask` -> 寻路/走向 NPC 或打开完成弹层 -> 到达 NPC 后才 `DialogueController.ShowTask` -> 真实协议 12101/12102/30004。

要求:

- 点击任务项必须设置选中态并触发真实 `TaskModel.DoTask`, 不直接打开假 UI。
- NPC 任务分支必须经 `MainRoleAgent.MoveToNpc`/等价运行时移动, 到达/兜底后才 `ShowTask`。
- 完成任务分支必须走 `TaskFinishView` 显示真实奖励, 点击领取/提交后发 30004。
- 如果老端当前任务点击会直接飞鞋/跨场景/自动战斗, Unity 只能接入已有真实系统; 不完整则写 blocker 和证据。
- 对话已开且 NPC 匹配的去重逻辑如未迁移, 要记录为确认风险; 不要编造 `DialogueModel`。

最低验收:

```powershell
dotnet build yu_client_unity.slnx -v:minimal
rg -n "DoTask|MoveToNpc|ShowTask|SubmitFinish|TaskFinishView|send 30004|12101|12102|EVT_TASK_SELECT_CHANGED" Assets\Scripts\Module\Core -S
```

必须留证:

- 点任务前截图。
- 点任务后选中态/移动/NPC 对话/完成弹层中的至少一种真实结果截图或日志。
- 如果链路卡住, 记录具体卡点: 没任务、没 NPC、坐标缺失、移动不到达、对话配置缺失、协议未返回、Popup 层未初始化等。

## 本轮 P3: 只记录, 不扩散编码

只记录以下差异, 除非 P0/P1/P2 已完整闭环且仍有时间:

- `MainUISkillView`: 技能 4 槽、自动战斗按钮、伙伴技能锁。
- `FunctionOpenIcon`: 功能开启图标和弹出表现。
- `FirstRechargeBubble`: 首充气泡。
- `MainUIActivityView`/顶部活动入口。
- 队伍系统完整创建/搜索/成员列表。

## 禁止事项

- 禁止只用静态 `.scene` 或 Unity prefab 宣称老端对齐。
- 禁止伪造任务、NPC、奖励、红点、引导、活动。
- 禁止把 `dotnet build` 通过当作 UI 对齐完成。
- 禁止提交 `output/`、`.playwright-cli/` 或字体 SDF 脏文件。
- 禁止绕开老端运行时截图/节点树直接写“按源码推断”。

## 交付格式

必须提交:

1. 玩家可见变化。
2. 老端运行时证据路径。
3. Unity 运行时/编辑期真机截图路径。
4. 差异报告 `Docs/RuntimeCompare/MainUI-TaskTeam-第2轮.md`。
5. 改动文件列表。
6. `dotnet build yu_client_unity.slnx -v:minimal` 结果。
7. 确认问题清单: 只写有证据的问题。
8. 下一轮建议: 从 `MainUISkillView`、`FunctionOpenIcon`、或任务链路 blocker 中选一个明确方向。

