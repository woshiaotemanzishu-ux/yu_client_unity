# MainUI 巡检报告 - heartbeat 19:59

时间: 2026-06-23T19:59:10.355Z
范围: MainUI 首屏入口继续巡检；旧 Laya 端 720x1280 运行时为基准；Unity 侧做路由可用性静态审查。

## 已覆盖入口

- 旧端主角光环: 从 MainUI 点击 `(489,83)`，打开 `跨服开启` 弹窗。
  - 证据: `old_laya_click_halo_489_83.png`
  - 旧端源码: `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:143`，触发 `OPEN_VIEW, "HaloMainView"`。
- 旧端客服: 从 MainUI 点击 `(558,83)` 后无可见面板、URL 未变化、未产生新 tab。
  - 证据: `old_laya_click_customer_service_558_83.png`, `old_laya_dom_probe_after_customer_service.json`
  - 旧端源码: `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:153`；微信小游戏调用 `openCustomerServiceConversation()`，非微信调用 `PlatformManager.EyouOpenGM()`。
- Unity MainUI 顶部路由静态审查:
  - `vip` / `recharge` 已注册到 `VipFlow.Open("VipBaseView")` / `VipFlow.Open("RechargeView")`。
  - `halo` 已注册到 `HaloFlow.Toggle`。
  - `customerservice` 未注册，点击会落 `MainUIRoutePlaceholder`，满足“可点击占位”，但不是旧端真实客服行为。

## 发现差异

- 旧端 `主角光环` 打开真实 `HaloMainView`，当前截图呈现为“跨服开启”弹窗；Unity 应打开真实 Halo 模块，不能只停留在占位。
- 旧端 `客服` 在当前 Web 运行环境没有可见 UI，源码显示它依赖平台 API/GM 外链；Unity 当前用 `customerservice` 占位更可控，但后续需要决定是否接平台客服或统一提示。
- Unity 路由交叉表显示以下显式 `Open(key)` 没有 `Register(key)`:
  - `partnerawake`
  - `team_create`
  - `team_search`
  - `templeawaken`
  这些都会走统一占位，不会无响应。

## 共性根因

- MainUI 首屏入口可点击链路基本已经集中到 `MainUIRouter`；当前风险从“按钮无监听”转为“真实模块是否注册/是否足够可用”。
- 旧端平台相关入口不能只看截图，需要同时查旧端源码；客服就是平台 API/外链类入口，Web 运行态可能没有可见效果。
- Unity runtime 截图仍未跑通：MCP `Transport closed`，当前只有旧的 editor scene dump，不能作为 MainUI 可用性验收。

## 已执行生成/代码任务

- 本轮未改代码、未改 prefab、未改转换器。
- 执行 MainUI 路由交叉表审查，确认显式入口的真实注册/占位状态。
- 继续沿用上一轮新增的 `RuntimeUiCaptureTool.PlayThenCaptureFromCommandLine` 作为 Unity 运行态截图工具；本轮未强行启动第二个 Unity Editor，避免项目已打开时触发锁/批处理截图失真。

## 验证截图/命令

- 旧端截图目录: `output/heartbeat_1959/`
- 旧端源码定位:
  - `Select-String -Path D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts -Pattern "cs|customer|客服|halo|光环|跨服|_box" -Context 3,5`
- 路由交叉表命令:
  - PowerShell 扫描 `MainUIRouter.Open("key")` 与 `MainUIRouter.Register("key")`，结果见本报告“发现差异”。
- Unity runtime 输出检查:
  - `output/runtime_unity/20260624_004613/status.txt`
  - 结果仍是 `isPlaying=False`, `nodeCount=1`, `screenshotRequested=false`，不是有效 MainUI 运行态证据。

## Claude / MCP 可用性

- Claude Code:
  - `claude --version` 成功，版本 `2.1.185 (Claude Code)`。
  - 只读分析命令 `claude -p "只读分析 MainUIRouter 入口注册完整性..."` 约 94 秒超时，exit code 124；残留 `claude.exe` 已清理。本轮 Claude 不计入成果。
- Unity MCP:
  - `Unity_RunCommand` ping 仍返回 `Transport closed`。
  - 本轮检查时没有 `relay_win.exe` 残留，说明当前失败不是简单残留进程占槽。

## 下一批页面优先级

1. Unity 运行态: 用 Editor 菜单或命令行工具抓 Play 后 MainUI 截图和节点 dump。
2. Unity 入口验证: `vip`、`recharge`、`halo`、`customerservice` 四个顶部入口逐个点击，确认真实页/占位页。
3. 接入缺口: `customerservice` 要么注册平台客服/GM 处理，要么保持统一占位并记录为平台依赖；`partnerawake`、`team_create`、`team_search`、`templeawaken` 继续占位或接真实模块。
4. 继续旧端证据: 地图、头像设置、buff、fightmode、底部角色/背包/设置/聊天。
