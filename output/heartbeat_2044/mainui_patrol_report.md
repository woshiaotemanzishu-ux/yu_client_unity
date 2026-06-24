# Shenxiao UI Heartbeat 2044

时间: 2026-06-23T20:44:11.511Z
范围: MainUI 运行时对比巡检, 先保证主界面可用入口

## 已覆盖入口

- 旧 Laya 运行时基准已重新建立: in-app browser 显式使用 720x1280 竖屏视口打开 `http://127.0.0.1:8090/index.html`。
- 已跑通旧端登录/选服/选角/进游戏路径:
  - 预填账号 `21055` 可进游戏, 但进入后关闭十倍返利后被强制带到转职页, 不适合 MainUI 连续入口巡检。
  - `zxczxc/zxczxc` 可登录, 选择 44 级角色可进入 MainUI。
- 已得到旧端 MainUI 可用基准截图: `old_laya_mainui_clean_hud_baseline.png`。
- 已尝试点击一个可见入口: 原计划点击客服, 但由于角色自动移动/剧情触发导致进入剧情对话, 不计为客服入口覆盖。

## 发现差异

- 旧端运行时不是静态 HUD: 进入后会连续出现挂机收益、奖励、技能获得、新手剧情、升级/玩家风采等运行时弹层或引导。
- 低级账号可拿到 MainUI HUD, 但仍会被任务剧情和自动战斗推进污染; 高级账号会被转职强引导卡住。
- 旧端客服入口不能继续用固定坐标粗点: MainUI 中角色移动、剧情触发和相机位置会改变可点区域, 本轮误点触发了剧情, 说明必须先暂停/稳定自动任务或采用更可靠的节点/状态识别。
- Unity 侧仍不能用 MCP 获取运行态截图或节点 dump; 本轮只能沿用静态路由审查和编译结果。

## 共性根因

- 巡检缺少“稳定 MainUI 状态机”: 登录后需要按账号/角色等级分别处理首弹层、强引导、任务剧情、自动战斗和奖励层。
- 旧端基准必须先冻结到干净 HUD, 再逐入口点击; 否则入口点击会被剧情/自动任务截获。
- Unity 运行时链路的主要阻塞仍是 MCP transport, 不是 `relay_win.exe` 残留。

## 已执行生成/代码任务

- 本轮未改业务代码、未改 prefab、未改转换器、未重新生成 UI。
- 本轮新增巡检证据和报告文件。

## 验证截图/命令

- 旧端证据目录: `output/heartbeat_2044/`
- 关键旧端截图:
  - `old_laya_fresh_start.png`: 720x1280 登录页。
  - `old_laya_after_login_click.png`: 预填账号登录后选服页。
  - `old_laya_after_role_enter_button.png`: 预填账号进游戏后十倍返利弹层。
  - `old_laya_after_close_first_recharge.png`: 关闭十倍返利后转职页。
  - `old_laya_zxczxc_refilled_login.png`: `zxczxc/zxczxc` 重填成功。
  - `old_laya_zxczxc_after_enter_server.png`: `zxczxc` 选角页。
  - `old_laya_zxczxc_lv44_after_enter_button.png`: 44 级角色进入后挂机收益弹层。
  - `old_laya_zxczxc_lv44_hud_after_tutorial_wait.png`: 可用 MainUI HUD 基准。
  - `old_laya_mainui_click_customerservice.png`: 客服坐标尝试误触发剧情, 不计有效覆盖。
- 节点/运行证据:
  - `old_laya_after_login_probe.json`
  - `old_laya_after_enter_probe.json`
  - `old_laya_after_role_enter_logs.json`
  - `old_laya_zxczxc_lv44_logs.json`
- 编译验证: `dotnet build yu_client_unity.slnx -v:minimal` 通过, 0 warning, 0 error。
- 浏览器视口: 本轮开始设为 720x1280, 结束已 reset, 下一轮需重新设置。

## Claude/MCP 可用性

- Claude Code CLI: `claude --version` 返回 `2.1.185 (Claude Code)`。本轮无代码修改, 未发起 Claude 写码任务。
- Claude 残留: 初次进程检查短暂看到一个无命令行 `claude.exe`, 2 秒后复查已消失。
- Unity MCP: 无 `relay_win.exe` 残留; Unity Editor 进程存在; `Unity_RunCommand` 仍失败, 现象为 `Transport closed`。

## 下一批页面优先级

1. 固化旧端 MainUI 清理状态机: 使用 `zxczxc` 的 44 级角色, 按顺序处理挂机收益、奖励、技能获得、剧情/玩家风采弹层。
2. 在干净 HUD 上先停掉/绕开自动任务推进, 再点击入口; 不再用会误触剧情的粗略固定坐标。
3. 优先实测 MainUI 必须可用入口: `role`, `bag`, `chat`, `setting`, `shop`, `map`, `vip`, `recharge`, `halo`。
4. Unity 侧继续优先恢复 MCP 或改用 RuntimeCapture Editor 命令行截图; 拿不到运行态节点前, 不声明入口点击验收完成。
5. 若发现 Unity 静态 UI 缺背景、窗框、按钮皮肤或 Bind, 回到 LayaUI 转换链路/Bind 回填/资源映射修复, 不手改 prefab 作为最终方案。
