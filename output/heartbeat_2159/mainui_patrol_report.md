# Shenxiao UI 巡检 21:59

## 已覆盖入口

- 旧 Laya 客户端按 720x1280 竖屏运行态重新执行: 登录 `zxczxc/zxczxc` -> 踏入仙界 -> 选角页 -> 选择 `蘭世然 0转67级` -> 进入 MainUI。
- 保存运行时截图链:
  - `old_runtime_00_current.png`: 登录页。
  - `old_runtime_01_after_login.png`: 登录后进入踏入仙界页。
  - `old_runtime_02_after_enter.png`: 选角页。
  - `old_runtime_03_selected_mid_role.png`: 选中中等级角色。
  - `old_runtime_04_after_mid_enter.png`: MainUI 首屏可见，HUD/活动/任务/自动闯关/底部栏均出现。
  - `old_runtime_05_mainui_after_wait.png`: 等待后触发挂机/奖励弹层。
  - `old_runtime_06_after_reward_close.png` 至 `old_runtime_11_after_close_retry.png`: 关闭奖励后连续进入剧情、首充、御风云骑页面。
- Unity 静态入口检查:
  - `role/bag/setting/chat/map` 均已有 Bootstrap 注册且 prefab 产物存在。
  - `shop` 已注册，但 `Assets/Prefabs/UI/Shop/ShopModule.prefab` 不存在。
  - `customerservice/team_* / onhook / partnerawake / templeawaken / tt_record` 等未注册入口应走 `MainUIRoutePlaceholder`。

## 发现差异

- 旧端中等级角色可以短暂进入 MainUI，但会被运行时奖励、剧情、首充、坐骑幻化页面连续接管。此状态下无法稳定点击设置/背包/角色等入口做像素级对比。
- 旧端 DOM 仍只有 canvas 泛节点，`domSnapshot` 不提供 UI 节点结构；本轮证据必须依赖运行时截图和坐标点击。
- Unity MainUI 路由地基存在，但 `shop` 属于更危险的缺口: 它不是未注册占位，而是已注册真实 Flow 后缺目标 prefab，点击时大概率不是统一空面板。

## 共性根因

- 旧端巡检前置状态未固定: 自动任务、挂机收益、剧情任务、首充活动、成长页面会抢占输入。15 分钟巡检若继续用普通账号随机角色，会大量时间消耗在清弹层，结果不可复用。
- Unity 侧共性问题仍应走通用链路:
  - 静态 UI、背景、窗框、默认图、模板、Bind 回填、Addressables 分组归 LayaUI 转换/回填/分组。
  - 点击后打开哪个 Flow、默认页、动态列表、真实数据刷新、运行时换图归业务 View/Flow。
- MainUI 当前最小真实缺口不是手改 prefab，而是补齐生成产物和注册状态审计: `shop` 需要通过转换器/回填/Addressables 生成 `ShopModule`，或让 `ShopFlow` 加载失败时回落统一占位。

## 已执行生成/代码任务

- 未执行 Unity 菜单生成，未修改 prefab，未改业务代码。
- Claude Code CLI 已执行只读分析:
  - 命令: `claude -p "...MainUI 入口点击/路由/占位机制..."`
  - 结果: 成功返回，定位 `MainUIRouter`、`MainUIRoutePlaceholder`、`MainUIFlow`、各 Bootstrap 注册和 `ShopModule` 缺口。
- 子任务已执行只读分析，结果与 Claude 一致: 六个主目标中 `shop` 缺 prefab，其余主目标产物存在；动态活动入口大多应先走占位或等待模块移植。

## 验证截图/命令

- 旧端运行时截图目录: `D:\git_res\yu_client_unity\output\heartbeat_2159`
- 编译验证:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - 结果: 0 warnings, 0 errors。
- prefab 存在性验证:
  - `Assets/Prefabs/UI/Shop/ShopModule.prefab`: False
  - `Assets/Prefabs/UI/Setting/SettingModule.prefab`: True
  - `Assets/Prefabs/UI/Bag/BagModule.prefab`: True
  - `Assets/Prefabs/UI/Chat/ChatModule.prefab`: True
  - `Assets/Prefabs/UI/Map/MapModule.prefab`: True
  - `Assets/Prefabs/UI/Role/RoleModule.prefab`: True
- 浏览器视口已在结束前 reset。

## Claude/MCP 可用性

- Claude Code CLI 可用: `2.1.185 (Claude Code)`；本轮已成功产出只读定位。
- Unity MCP:
  - 首次 `Unity_RunCommand` 返回 `Transport closed`。
  - 检查到 `relay_win.exe --mcp`，父进程为 Codex，不是 Unity Editor。
  - 已 `Stop-Process -Id 44512 -Force` 清理该 relay。
  - 重试 `Unity_RunCommand` 仍返回 `Transport closed`。
  - 清理后未再发现 `relay_win.exe`，本轮 MCP 判定阻塞。

## 下一批页面优先级

1. 先建立巡检前置状态: 需要可重复清理旧端弹层/自动任务/剧情接管，或者准备一个已清活动、已停自动任务的巡检账号/角色。否则无法进行稳定像素级对比。
2. Unity 侧先处理 `shop`: 不直接手修 prefab，优先通过 LayaUI 转换器/回填/Addressables 产出 `ShopModule`；短期可加 `ShopFlow` 加载失败回落统一占位，避免已注册入口打开失败。
3. 保持 `role/bag/setting/chat/map` 为首批真实页巡检对象；每页必须验证入口可点击、窗口有背景/窗框、关闭/返回可用、主要按钮可点。
4. 未迁移入口继续走 `MainUIRoutePlaceholder`，不要逐个假 Flow 精修；用 route catalog 记录 real/placeholder/missingPrefab 差集。
