# MainUI 巡检报告 - heartbeat 20:14

时间: 2026-06-23T20:14:10.573Z
范围: MainUI 顶部入口继续巡检；旧 Laya 端 720x1280 运行时为基准；Unity 侧做顶部路由注册审查。

## 已覆盖入口

- 旧端地图: 从 MainUI 点击右上角地图区域 `(660,78)`，打开全屏 `地图` 页。
  - 证据: `old_laya_click_map_660_78.png`
  - 旧端源码: `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:193`，触发 `OPEN_MAP_VIEW, "MapEnterView"`。
- 旧端进游戏状态恢复:
  - 页面重载后进入登录/选服/选角/剧情链路，最终通过跳过剧情回到 MainUI。
  - 证据: `old_laya_after_reload_15s.png`, `old_laya_after_login_18s.png`, `old_laya_after_click_role_bottom_18s.png`, `old_laya_after_repeated_skip_5s.png`
- 旧端头像设置、buff、fightmode:
  - 本轮截图被挂机收益和充值弹窗干扰，不计入有效运行时入口证据。
  - 仅记录旧端源码行为:
    - 头像: `SettingModel.SETTING_OPEN_VIEW, "SettingView"`
    - buff: 有 buff 时 `OPEN_VIEW, "MainUIBuffView", true`，否则提示 `当前没有Buff`
    - fightmode: 场景允许时 `OPEN_VIEW, "MainUIFightModeView", true`，否则提示 `当前场景不允许切换PK状态`

## 发现差异

- 旧端 `地图` 是全屏真实页，不是弹窗；进入后没有明显右上角关闭按钮，容易阻断后续坐标巡检。
- Unity 顶部入口注册状态:
  - `map`: 已注册到 `MapBootstrap.cs:17`
  - `setting`: 已注册到 `SettingBootstrap.cs:18`
  - `buff`: 已注册到 `MainUIOverlayBootstrap.cs:16`
  - `fightmode`: 已注册到 `MainUIOverlayBootstrap.cs:17`
  - `vip`: 已注册到 `VipBootstrap.cs:17`
  - `recharge`: 已注册到 `VipBootstrap.cs:18`
  - `halo`: 已注册到 `HaloBootstrap.cs:16`
  - `customerservice`: 未注册，会走统一 `MainUIRoutePlaceholder`

## 共性根因

- 旧端自动弹层会污染点击巡检；后续自动化必须先做“清弹窗/确认 HUD 干净”的前置步骤，再采入口。
- 地图这种全屏页需要单独的退出策略；不能假设所有页面都有统一 X。
- Unity MainUI 顶部入口大多已经接到 `MainUIRouter`，剩余差异主要是页面真实可用程度与 Unity runtime 截图/点击验收尚未跑通。

## 已执行生成/代码任务

- 本轮未改代码、未改 prefab、未改转换器。
- 执行旧端运行时截图采集、旧端源码定位、Unity 顶部路由注册审查。
- 保留上一轮新增的 Unity 运行态截图工具；本轮 MCP 仍断，未通过 Unity 菜单触发。

## 验证截图/命令

- 旧端截图目录: `output/heartbeat_2014/`
- 旧端源码定位:
  - `Select-String -Path D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts -Pattern "_box_fight_mode|_box_buff|_box_map|_img_head|SettingModel|OPEN_MAP_VIEW|MainUIFightModeView|MainUIBuffView" -Context 2,4`
- Unity 顶部路由审查:
  - PowerShell 扫描 `MainUIRouter.Register("key")`，见本报告“发现差异”。
- 编译:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - 结果: 成功，0 warnings，0 errors。

## Claude / MCP 可用性

- Claude Code:
  - `claude --version` 成功，版本 `2.1.185 (Claude Code)`。
  - 本轮无代码修改，未调用 Claude 做代码审查；无可计入协作成果。
- Unity MCP:
  - 本轮开始前无 `relay_win.exe` 残留。
  - `Unity_RunCommand` ping 仍为 `Transport closed`。
  - 继续用本地编译、旧端截图和静态审查推进。

## 下一批页面优先级

1. 旧端先清弹窗，重采头像设置、buff、fightmode 的有效运行时截图。
2. Unity 运行态截图: 需要从当前 Editor 菜单触发 `神霄/调试/UI运行态/进入Play后延迟截图+节点Dump`，或修复 MCP 后调用。
3. Unity 点击验收: `map`、`setting`、`buff`、`fightmode` 对照旧端真实页/提示/占位。
4. 继续底部入口: 角色、背包、聊天、商城、自动闯关。
