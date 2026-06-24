# Shenxiao UI Heartbeat 2029

时间: 2026-06-23T20:29:11.777Z
范围: MainUI 运行时对比巡检, 先保证主界面可用入口

## 已覆盖入口

- 旧 Laya 运行时: 重新读取 `http://127.0.0.1:8090/index.html`, 画布仍为 720x1280 竖屏运行时。
- 本轮旧端没有新增可信页面入口覆盖: 当前浏览器状态被奖励弹层、充值弹层、须弥天刃功能页和物品详情污染, 多次点击/Esc/Alt+Left 未回到干净 HUD, 因此不把设置、Buff、战斗模式等坐标点击计入有效基准。
- Unity 静态入口审查: 覆盖底部功能栏、聊天区、任务/队伍区、地图/VIP/充值/光环/客服路由注册情况。

## 发现差异

- 旧端巡检状态机不足: 当前会被运行时弹层和功能页卡住, 单纯坐标点击不可靠。需要下一轮优先建立稳定的“回到 HUD/清弹层/重登干净账号”步骤。
- MainUI 底部首批入口均已有真实路由: `role`, `bag`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`。
- 聊天区入口已有真实路由: `chat`, `setting`, `friend`, `shop`。
- 任务/队伍区存在未迁移入口: `team_create`, `team_search`, `templeawaken` 未注册, 当前应走统一空面板占位, 后续再补真实 Team/TempleAwaken 模块。
- `customerservice` 未注册, 与旧 Web 运行时行为也不完全一致: 旧端客服走平台接口, Web 环境未观察到可见面板。
- 风险点: `MainUIDownView.BuildFuncIcons` 的正常点击依赖模板挂载并回填 `MainFuncIconItem`; 若运行时实例没有该业务组件, fallback 只显示图标, 不会绑定点击。需要 Unity 运行时节点 dump 继续确认。

## 共性根因

- 旧端对比链路缺少可复位状态控制, 导致页面证据容易被临时弹层和功能页污染。
- Unity 运行时证据仍受 MCP 阻塞影响, 只能先做本地编译、源码路由审查和旧端截图证据。
- 未迁移功能应统一收敛到 `MainUIRouter` 占位机制, 避免每个入口散落补空逻辑。

## 已执行生成/代码任务

- 本轮没有改业务代码、没有改 prefab、没有重新生成 UI。
- 本轮仅做运行时截图、源码路由审查、编译验证和巡检报告。

## 验证截图/命令

- 旧端截图证据目录: `output/heartbeat_2029/`
  - `old_laya_current_start.png`
  - `old_laya_after_click_reward_overlay.png`
  - `old_laya_after_close_recharge_overlay.png`
  - `old_laya_after_extra_clear_click.png`
  - `old_laya_after_item_confirm.png`
  - `old_laya_after_feature_top_right.png`
  - `old_laya_after_feature_top_left.png`
  - `old_laya_after_escape_feature.png`
  - `old_laya_after_alt_left_feature.png`
- 旧端节点/画布证据: `output/heartbeat_2029/old_laya_current_probe.json`
- 路由审查命令: `Select-String` 扫描 `Assets/Scripts/Module/**/*.cs` 中 `MainUIRouter.Register("key")`。
- 路由结果:
  - 已注册: `role`, `bag`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`, `chat`, `setting`, `friend`, `shop`, `autobrush_toggle`, `map`, `vip`, `recharge`, `halo`
  - 未注册: `team_create`, `team_search`, `templeawaken`, `customerservice`
- 编译验证: `dotnet build yu_client_unity.slnx -v:minimal` 通过, 0 warning, 0 error。

## Claude/MCP 可用性

- Claude Code CLI: `claude --version` 返回 `2.1.185 (Claude Code)`, CLI 存在。由于本轮未改代码, 未发起 Claude 写码任务。
- Unity MCP: 本轮检查无 `relay_win.exe` 残留; Unity Editor 进程存在; `Unity_RunCommand` 仍失败, 现象为 `Transport closed`。

## 下一批页面优先级

1. 先把旧端恢复到干净 MainUI HUD: 优先尝试新账号/重登/刷新后跳过弹层, 建立稳定清弹层脚本。
2. Unity 侧恢复运行时截图链路: 优先修 MCP 或使用已加入的 Editor RuntimeCapture 菜单/命令行工具拿节点 dump。
3. 实测首批必须能点击: `role`, `bag`, `chat`, `setting`, `shop`, `map`, `vip`, `recharge`, `halo`。
4. 对未迁移入口确认统一占位: `team_create`, `team_search`, `templeawaken`, `customerservice`。
5. 若 `MainFuncIconItem` 未正确回填, 不手改 prefab, 回到 Bind 回填或 LayaUI 转换链路修复后重新生成。
