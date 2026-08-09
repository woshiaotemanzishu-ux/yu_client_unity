# MainUI 固定 HUD 验证记录

## schema 6

命令：

`python .agents/skills/audit-game-ui-route/scripts/route_ledger.py validate output/ui_route_audit/2026-08-09_mainui_fixed_hud/route-ledger.json`

结果：`route=mainui.fixed-hud.route schema=6 nodes=103 status={'not-run': 103}`。

## 静态结构

- 8 个独立 Region Prefab 均找到对应业务 View。
- `HudTop` 找到 `MainUIMoneyItem` / `MainUITopBuffItem` 模板。
- `HudTaskTeam` 找到 `ScrollRect`、`RectMask2D`、`VerticalLayoutGroup`、`ContentSizeFitter` 与任务项业务组件。
- `HudSkillBar` 找到 `Slot_0..Slot_3` 与 `MainUISkillItem`。
- `HudAutoBrush`、`HudOnHook` 找到各自 `UIEffectSlot`；`HudNotification` 找到唯一动态项模板和 `HorizontalLayoutGroup`。
- 源码断言通过：Top 为 `Show -> SetMoneyType`，Skill 为 `Show -> SetData`。

## Core 编译

命令：

`dotnet build Shenxiao.Module.Core.csproj --no-restore -v:minimal -p:CustomAfterMicrosoftCommonTargets=E:\GitProject\yu_client_unity\output\ui_route_audit\2026-08-09_guild\include-new-guild-scripts.targets`

结果：`0 errors / 84 warnings`。警告均为仓库既有过时 API 或未赋值测试拦截字段；本轮三个改动文件没有新增编译错误。

使用 output-only targets 的原因：Unity 生成的 csproj 尚未刷新并发新增 Guild/Shop 源文件；未修改 csproj，也未启动 Unity。

## 并发范围审计

任务开始时已存在：

- `MainFuncIconItem.cs` dirty
- `MainUIMoneyItem.cs` dirty
- `RewardFlyService.cs(.meta)` 未跟踪

收口时又观察到 `MainUIDownView.cs`、`MainUIChatView.cs` dirty；它们在本任务开始时不 dirty，属于并发工作区变化，本任务从未读取后写入或修改这些禁止文件。

本任务代码 diff 仅：

- `MainUITopView.cs`
- `MainUISkillView.cs`
- `MainUISkillItem.cs`

另新增本路线 output 目录中的 manifest、ledger、依赖清单、静态结论和本验证记录。未修改任何 Prefab、Generated、Common、Addressables、Docs、Flow、Router、Configs 或 Activity 文件。

## 未运行

未启动 Unity、未启动浏览器、未操作前台、未发账号写事务。因此真实 Web 顺序复验、像素 diff、动态特效两帧、摇杆输入、折叠/通知/冷暖重进仍是运行闸；本验证不替代它们。
