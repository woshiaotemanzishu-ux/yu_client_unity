# MainUI 巡检报告 - heartbeat 19:44

时间: 2026-06-23T19:44:10.527Z
范围: MainUI 运行时入口优先；旧 Laya 端 720x1280 竖屏运行时为基准；Unity 侧补运行态截图/节点证据链。

## 已覆盖入口

- 旧端 MainUI 初始运行态: `old_laya_start_state.png`, `old_laya_dom_probe_start.json`
  - DOM 只能看到 `canvas#layaCanvas`，尺寸 720x1280；Laya stage 运行时节点未从 DOM 直接暴露。
- VIP: 点击 `(336,83)`，打开真实 `VIP福利` 页面。
  - 证据: `old_laya_click_vip_336_83.png`
- VIP 关闭: 点击 `(674,160)`，回到 MainUI。
  - 证据: `old_laya_after_close_vip_674_160.png`
- 充值: 从关闭后的 MainUI 点击 `(410,83)`，打开真实 `十倍返利` 充值页。
  - 证据: `old_laya_click_recharge_clean_410_83.png`

## 发现差异

- 旧端顶部 `VIP` 和 `充值` 都是 MainUI 上的真实入口，不是静态图标；Unity 侧必须保证同名入口可点击。
- 已迁移模块应打开真实页面；未迁移模块至少走统一空面板占位，不能点击无反馈。
- 旧端弹窗关闭坐标需要命中右上角 X；上一轮误点底部区域导致 `充值` 截图仍停在 VIP 页，已重新采集干净证据。
- Unity 侧之前只有 Editor 当前场景截图工具，无法自动进入 Play 后等待 MainUI 初始化；这会导致抓到空 `UIRoot`，不能用于 MainUI 对比。

## 共性根因

- 当前最大阻塞不是单个 prefab，而是 Unity 运行态取证链不完整：必须能在 Play 后等待登录/进游戏/MainUI 初始化，再抓截图和节点 dump。
- 静态 UI 问题仍按 LayaUI 通用转换链路处理：背景、窗框、皮肤、尺寸、默认图片、模板、Bind、Addressables 分组不直接手改 prefab 作为最终方案。
- MainUI 入口行为属于运行时接入：按钮绑定、路由、真实页/占位页打开反馈，应由 MainUI view/router/flow 负责。

## 已执行生成/代码任务

- 修改 `Assets/Editor/RuntimeCapture/RuntimeUiCaptureTool.cs`，新增:
  - Unity 菜单: `神霄/调试/UI运行态/进入Play后延迟截图+节点Dump`
  - 命令行入口: `Shenxiao.Editor.RuntimeCapture.RuntimeUiCaptureTool.PlayThenCaptureFromCommandLine`
  - 参数: `-runtimeCaptureDelaySeconds 60`
  - 命令行模式截图后延迟 2 秒退出，避免 `ScreenCapture.CaptureScreenshot` 未落盘。
- 未手工修改 prefab。
- 未修改转换器或业务 View/Flow；本轮只补 Unity 运行态验收工具。

## 验证截图/命令

- 旧端截图目录: `output/heartbeat_1944/`
- 编译命令: `dotnet build yu_client_unity.slnx -v:minimal`
  - 结果: 成功，0 warnings，0 errors。
- Claude Code 只读审查命令:
  - `claude -p "只读审查 Assets/Editor/RuntimeCapture/RuntimeUiCaptureTool.cs 本次变更，只看明显编译风险、Unity Editor API 误用、命令行 Play 延迟截图逻辑问题。不要改文件。10条以内。"`
  - 结果: 约 64 秒超时，exit code 124，无有效审查输出；本次不计入协作成果。
- Unity 运行态截图后续命令建议:
  - `Unity.exe -projectPath D:\git_res\yu_client_unity -executeMethod Shenxiao.Editor.RuntimeCapture.RuntimeUiCaptureTool.PlayThenCaptureFromCommandLine -runtimeCaptureDelaySeconds 60`
  - 注意: 不要额外传 `-quit`，该方法会在截图落盘后退出 Unity。

## Claude / MCP 可用性

- Claude Code CLI: 可调用，但本轮只读审查请求超时；后续代码改动仍先尝试 Claude，若再次超时需拆成更小请求或记录后由 Codex 接手。
- Unity MCP: 本轮检查未发现残留 `relay_win.exe`；`Unity_RunCommand` 仍报 `Transport closed`，暂按 MCP 阻塞处理，用本地编译和新增 Editor 命令继续推进。

## 下一批页面优先级

1. 用新增 Play 延迟截图工具抓 Unity MainUI 运行态截图和节点 dump。
2. 对比旧端 `VIP` / `充值` 与 Unity 点击结果：真实页优先，未迁移则统一占位。
3. 继续 MainUI 顶部入口: `主角光环`、`客服`、活动图标、`夺宝`、`超值礼包`、`成长福利`、`祭典`、`直升V4`。
4. 继续 MainUI 底部/侧边入口: 角色、背包、设置、聊天、任务/队伍、自动闯关、副本/大妖/日常。
