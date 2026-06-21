# Claude任务包-主线竖切-第1轮

日期：2026-06-21

目标：不要再扩散做 UI shell。先把“进游戏后主竖切”打通到可复现、可见、可验收：资源门 -> MainUI/地图 -> NPC -> 任务点击链。

## 必读

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/LayaUI转换流水线.md`

## 本轮已验证事实

- 当前 worktree 有大量本地资源状态：6 个 tracked Addressables/字体资源变更，且大量 untracked `.meta`、生成 UI、字体材质。
- `.gitignore:47` 忽略 `/Assets/Prefabs/UI/`，但本机存在 `Assets/Prefabs/UI/Map/MapModule.prefab`、`Assets/Prefabs/UI/Common/BaseWindowSkin.prefab` 等完成项依赖资源。
- `HEAD` 与 worktree 的 Addressables key 不一致：
  - `prefabs/ui/mainui/mainuimodule`：HEAD 有，worktree 有。
  - `prefabs/ui/map/mapmodule`：HEAD 无，worktree 有。
  - `prefabs/ui/setting/settingmodule`：HEAD 无，worktree 有。
  - `prefabs/ui/common/basewindowskin`：HEAD 无，worktree 有。
  - `prefabs/ui/shop/shopmodule`：HEAD 无，worktree 有。
  - `prefabs/ui/rune/runemodule`：HEAD 无，worktree 有。
- Unity 当前链路已有一部分数据层：
  - `Assets/Scripts/Module/Core/Game/GameEntryFlow.cs` 等待 `13001/10201/30005/13088@300@1/10202@3` 后发 `EVT_GAME_START`。
  - `Assets/Scripts/Module/Core/MainUI/MainUIFlow.cs` 在 `EVT_GAME_START` 打开首批 MainUI view。
  - `Assets/Scripts/Module/Core/Scene/SceneEntryFlow.cs` 在 `EVT_GAME_START` 发 12005。
  - `Assets/Scripts/Module/Core/Scene/SceneController.cs` 已接 12005、12100、12002、12020。
  - `Assets/Scripts/Module/Core/Task/TaskController.cs` 已接 30000、30001、30005。
- Unity 当前缺口也已确认：
  - `NpcAdded/NpcRemoved/NpcChanged` 只有 `SceneManager` 发事件，没有渲染订阅者；`Assets/Scripts/Framework/Scene3D/Npc.cs` 仍是 Phase 0 placeholder。
  - `Assets/Scripts/Module/Core/Task` 没有 `DoTask`、`CLICK_DO_TASK`、`TASK_OPEN_VIEW`、任务寻路/对话执行链。
  - `Assets/Scripts/Module/Core/Map/MapFlow.cs` 只打开 `WorldMapView`；老端顶部地图入口实际打开 `MapEnterView`。

## 老端源码锚点

- 顶部地图入口：`D:\GitProject\yu_client\h5\src\mainUI\MainUITopView.ts:193-194`，`_box_map` 触发 `OPEN_MAP_VIEW, "MapEnterView"`。
- 地图窗口：`D:\GitProject\yu_client\h5\src\map\MapEnterView.ts:24-25`，两个 tab：`AreaMapView` 和 `WorldMapView`。
- 进场/NPC：`D:\GitProject\yu_client\h5\src\scene\SceneController.ts:740-765`，12100 读 NPC 列表后 `SetNpcList` 并请求 12002；`SceneController.ts:465-471` 用 12020 刷 NPC 任务标。
- 任务点击：`D:\GitProject\yu_client\h5\src\commonModel\TaskModel.ts:744-784` 是 `DoTask` 主入口；`TaskModel.ts:2966-2971` 判断 Talk/StartTalk/EndTalk 为找 NPC 任务。
- MainUI 任务栏：`D:\GitProject\yu_client\h5\src\mainUI\MainUITaskTeamView.ts:730-733` 刷任务内容；`MainUITaskTeamView.ts:563-573` 已打开目标 NPC 对话时不重复 DoTask，否则调用 `TaskModel.DoTask()`。

## 本轮任务

### P0：资源可复现门

先解决，不通过不允许声明任何 UI/地图功能完成。

要求：

- 解释并修复 `Assets/Prefabs/UI/` 被忽略但 Addressables 指向其中 prefab 的矛盾。
- 对本轮依赖 key 给出 clean checkout 可复现路径：`mainuimodule`、`mapmodule`、`settingmodule`、`basewindowskin`。
- 如果选择入库 prefab/meta，只改必要 ignore 规则和必要资源，不大面积提交无关生成物。
- 如果选择生成制品不入库，必须给出一条 deterministic 生成命令，并证明生成后 Addressables key 在 HEAD 或生成产物中存在。

验收：

```powershell
git show HEAD:Assets/AddressableAssetsData/AssetGroups/Remote_Prefabs.asset | Select-String -SimpleMatch "prefabs/ui/map/mapmodule"
git show HEAD:Assets/AddressableAssetsData/AssetGroups/Remote_Prefabs.asset | Select-String -SimpleMatch "prefabs/ui/common/basewindowskin"
git check-ignore -v Assets/Prefabs/UI/Map/MapModule.prefab
dotnet build yu_client_unity.slnx -v:minimal
```

通过标准：关键资源不是只存在于 ignored/untracked 本地文件；构建通过；任务报告必须说明采用了“入库”还是“可再生成”路线。

### P1：NPC 可见链路

在 P0 通过后做。目标不是假 NPC，而是让 12100/12103/12020 的真实 NPC 数据进入可见层。

要求：

- 新增或补齐一个 Scene NPC 渲染 flow，订阅 `SceneManager.NpcAdded/NpcRemoved/NpcChanged`。
- NPC 形象/名字/任务标从真实 `NpcVo` + `config_npc` + 现有资源路径/ResManager 读取；缺资源时写明确 blocker，不得写假模型冒充。
- NPC 坐标使用现有地图/主角相对坐标体系，与 `SceneCharacterStage` 注释保持一致。
- 至少让一个真实 12100 NPC 在地图上可见或给出“数据到了但资源缺失”的精确证据。

验收：

```powershell
rg -n "NpcAdded|NpcRemoved|NpcChanged|class .*Npc" Assets/Scripts/Module/Core/Scene Assets/Scripts/Framework/Scene3D -S
dotnet build yu_client_unity.slnx -v:minimal
```

Play 验收需要日志包含：`12100 npc list`、NPC 渲染/资源加载日志、`12020 npc icon refresh`。

### P2：任务点击链路只做最小入口

在 P1 后做最小闭环，不要一次搬完整 TaskModel。

要求：

- 给 Unity `TaskModel/TaskFlow` 增加老端 `DoTask` 的最小等价入口。
- MainUI 任务项点击后能设置 `NowSelectTaskId`，根据当前 `TaskVo.TaskTipsType` 进入正确分支：
  - Talk/StartTalk/EndTalk：定位 NPC，已有对话则不重复打开，否则进入 NPC 交互/对话待接路径。
  - 非 Talk 且完成：打开任务完成弹层的真实入口；未移植则写 blocker。
  - 带 `SceneId/SceneX/SceneY`：走场景寻路/切场景待接路径；未移植则写 blocker。
- 不允许只打印日志就算完成；日志只能用于证明 blocker。

验收：

```powershell
rg -n "DoTask|CLICK_DO_TASK|TASK_OPEN_VIEW|NowSelectTaskId" Assets/Scripts/Module/Core/Task Assets/Scripts/Module/Core/MainUI -S
dotnet build yu_client_unity.slnx -v:minimal
```

Play 验收需要证明：点击 MainUI 任务项后选中态变化，并进入 Talk/寻路/完成弹层之一的真实分支或精确 blocker。

## 禁止事项

- 禁止继续新增无数据、无入口、无验收的 UI shell。
- 禁止用假 NPC、假任务、假场景坐标绕过协议。
- 禁止只在本机 ignored/untracked 资源上验收。
- 禁止把 `dotnet build` 通过当作 Unity 运行通过。

## 本轮交付格式

交付时必须写：

- 改了哪些文件。
- 每个行为对应的 Laya 源码锚点。
- 资源可复现路线和证据。
- 构建/Play 验证结果。
- 未完成项的 blocker，必须带文件、key、日志或协议证据。
