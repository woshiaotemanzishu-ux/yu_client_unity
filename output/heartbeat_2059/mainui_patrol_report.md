# Shenxiao UI Heartbeat 2059

时间: 2026-06-23T20:59:11.454Z
范围: MainUI 运行时对比巡检, 从主界面优先保证可用入口

## 已覆盖入口

- 旧 Laya 运行时继续使用 720x1280 竖屏视口。
- 已恢复到 `zxczxc/zxczxc` 44 级角色的 MainUI 场景, 并保存相对稳定 HUD: `old_laya_mainui_stable_hud.png`。
- 已有效覆盖 `VIP` 顶部入口: 点击后打开真实 `VIP福利` 页面, 截图为 `old_laya_mainui_click_vip.png`。
- `充值` 本轮不计有效覆盖: 点击后的截图仍是 `VIP福利`, 可能是 VIP 面板未关闭干净或入口触发被状态污染。
- 自动挂机/自动任务停止不计有效覆盖: 多次点击底部热区后反而被引导带入 `万古帝胄/青云套装` 和后续 `大妖来袭` 状态。

## 发现差异

- 旧端 MainUI 的自动推进仍会抢占巡检:
  - 初始 MainUI HUD 可出现。
  - 点击/等待过程中会继续触发奖励、自动战斗、任务引导、套装引导和大妖来袭。
  - 这些状态下继续点击入口会误计覆盖。
- 老端源码确认:
  - `MainUIAutoBrushView` 的挂机按钮并不是纯 UI 状态切换, 而是发送 `AutoBrushModel.REQUEST_PROTO_EVENT` 协议 `13307`, 参数 `1` 表示停止, 参数 `0` 表示开启。
  - `AutoBrushModel.StopAutoBrushState()` 也是发送 `13307, "c", 1`。
  - `TaskModel.GetAutoTaskSetting()` 控制自动任务推进, 多处任务逻辑会在自动任务开启时继续走任务/引导。
  - 顶部客服 `_box_cs` 在 WX 环境走 `openCustomerServiceConversation()`, Web 环境走 `PlatformManager.EyouOpenGM()`。
  - 顶部 VIP `_box_vip` 触发 `EventName.OPEN_VIP_VIEW`。
  - 顶部充值 `_box_recharge` 触发 `VipModel.OPEN_VIEW, 'RechargeView'`。

## 共性根因

- 旧端入口巡检不能只靠坐标点击; 必须先让自动任务/挂机/引导停稳。
- 底部“取消挂机”热区在引导覆盖状态下不可靠, 需要下一轮优先尝试从运行时事件或协议层停止 `AutoBrushModel`/`TaskModel`。
- Unity 侧仍缺运行态节点证据, MCP 未恢复前只能用源码、编译和已捕获旧端截图推进。

## 已执行生成/代码任务

- 本轮未改业务代码、未改 prefab、未改转换器、未重新生成 UI。
- 本轮新增旧端截图证据、源码检索证据和巡检报告。
- Claude Code 只读协作尝试失败: 命令非交互运行 124 秒超时, 未产出结论, 不计为有效协作成果。

## 验证截图/命令

- 旧端截图目录: `output/heartbeat_2059/`
- 关键截图:
  - `old_laya_current_start.png`: 当前旧端场景起点。
  - `old_laya_after_click_exit_scene.png`: 相对稳定 MainUI HUD。
  - `old_laya_mainui_stable_hud.png`: 本轮 HUD 基准副本。
  - `old_laya_mainui_click_vip.png`: VIP 有效打开 `VIP福利`。
  - `old_laya_mainui_click_recharge.png`: 充值尝试无效, 仍显示 `VIP福利`。
  - `old_laya_after_retry_cancel_auto.png`: 自动挂机停止尝试误入 `万古帝胄/青云套装`。
  - `old_laya_after_try_recover_from_suit.png`: 旧端被推进到 `大妖来袭`。
- 源码检索:
  - `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:213`
  - `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:215`
  - `D:\git_res\yu_client\h5\src\commonModel\AutoBrushModel.ts:364`
  - `D:\git_res\yu_client\h5\src\commonModel\AutoBrushModel.ts:367`
  - `D:\git_res\yu_client\h5\src\commonModel\TaskModel.ts:3274`
  - `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:157`
  - `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:161`
  - `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:186`
  - `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:189`
- 编译验证: `dotnet build yu_client_unity.slnx -v:minimal` 通过, 0 warning, 0 error。

## Claude/MCP 可用性

- Claude Code CLI:
  - `claude --help` 正常返回。
  - 只读分析命令:
    `claude --no-session-persistence --permission-mode dontAsk --add-dir D:\git_res\yu_client --tools Read,Grep,Glob -p "..."`
  - 现象: 124 秒超时退出, 同时留下 `claude.exe` 和其子进程 `relay_win.exe --mcp`; 已清理本轮残留进程。
- Unity MCP:
  - 清理 Claude 拉起的残留 `relay_win.exe` 后再次执行 `Unity_RunCommand`。
  - 结果仍为 `Transport closed`。

## 下一批页面优先级

1. 先解决旧端状态机: 优先尝试从运行时对象或协议层停止 `AutoBrushModel.REQUEST_PROTO_EVENT 13307, "c", 1`, 并确认 `TaskModel.GetAutoTaskSetting()` 不再继续推进任务。
2. 固化 MainUI HUD 后再测入口, 不在奖励、引导、剧情、自动战斗、套装页、大妖来袭状态下计覆盖。
3. 下一轮有效入口顺序: `VIP` 已有效, 继续 `充值`, `客服`, `地图`, `聊天`, `设置`, `商城`, `角色`, `背包`。
4. Unity 侧继续先恢复 MCP 或使用 RuntimeCapture Editor 工具取运行态截图/节点 dump; 未拿到运行态前不声明 Unity 点击验收完成。
