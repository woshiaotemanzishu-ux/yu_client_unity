# Shenxiao UI 巡检 22:14

## 已覆盖入口

- 旧 Laya 客户端继续按 720x1280 竖屏运行态取证，当前页面保存为 `old_runtime_current.png`。
- 当前旧端不是干净 MainUI，而是被运行时功能页接管到 `仙宗` 页面，继续证明普通账号/角色状态不能直接作为稳定巡检基准。
- Unity 侧本轮覆盖 MainUI 商城入口链路:
  - `ShopBootstrap` 已注册 `MainUIRouter.Register("shop", ShopFlow.Toggle)`。
  - `Assets/Prefabs/UI/Shop/ShopModule.prefab` 仍不存在。
  - `role/bag/setting/chat/map` 对应模块 prefab 存在。

## 发现差异

- 旧端运行态持续被活动/功能页接管，上轮是奖励、剧情、首充、御风云骑，本轮截图停在仙宗页。这个状态下不适合做像素级 MainUI 对比。
- Unity 的 `shop` 入口之前属于已注册但缺目标产物，点击后可能加载失败且没有统一空面板反馈；这不满足“未迁移模块至少可点击并打开占位”的要求。

## 共性根因

- 旧端对比前置状态仍未固定，需要可重复清理弹层/活动页/自动任务/剧情接管，或使用一个已清理的巡检角色。
- `shop` 的真实页面缺失属于 LayaUI 转换/回填/Addressables 产物缺口，不应手改 prefab 临时补页面。
- 运行时兜底属于业务 Flow/Router 边界: 已注册入口如果目标产物缺失，必须降级到 `MainUIRoutePlaceholder`，避免按钮可点但无反馈。

## 已执行生成/代码任务

- 未执行 Unity 菜单生成，未修改 prefab、Generated、Addressables 配置。
- 优先尝试 Claude Code CLI:
  - 命令: `claude -p "...ShopFlow...加载失败时回落 MainUIRoutePlaceholder..."`
  - 现象: 184 秒超时，未返回结果；检查 `ShopFlow.cs` 无落盘改动，随后由 Codex 接手。
- Codex 已修改 `Assets/Scripts/Module/Core/Shop/ShopFlow.cs`:
  - 引入 `Shenxiao.Module.Core.MainUI`。
  - `BaseWindowSkin` / `ShopModule` 加载异常或返回 null 时，释放半加载对象并显示 `MainUIRoutePlaceholder.Show("shop")`。
  - 缺 `BaseWindowSkinView` 时同样释放并回落占位。

## 验证截图/命令

- 旧端截图: `D:\git_res\yu_client_unity\output\heartbeat_2214\old_runtime_current.png`
- 旧端 DOM 证据: `D:\git_res\yu_client_unity\output\heartbeat_2214\old_runtime_current_dom.txt`，当前浏览器面返回空 DOM，文件内已记录“old Laya runtime exposes canvas/visual state only”。
- prefab 存在性:
  - `Assets/Prefabs/UI/Shop/ShopModule.prefab`: False
  - `Assets/Prefabs/UI/Setting/SettingModule.prefab`: True
  - `Assets/Prefabs/UI/Bag/BagModule.prefab`: True
  - `Assets/Prefabs/UI/Chat/ChatModule.prefab`: True
  - `Assets/Prefabs/UI/Map/MapModule.prefab`: True
  - `Assets/Prefabs/UI/Role/RoleModule.prefab`: True
- 编译:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - 结果: build success，0 errors，1 warning。
  - warning: `Assets\Scripts\Module\Core\Scene\MainRoleAgent.cs(206,17) CS0162`，与本轮 `ShopFlow` 改动无关。
- 浏览器 720x1280 视口已在截图后 reset。

## Claude/MCP 可用性

- `claude --version`: `2.1.185 (Claude Code)`。
- Claude Code 可启动，但本轮实际代码任务 `claude -p` 超时 184 秒，未产出改动，不计入成果。
- Unity MCP:
  - 本轮未发现残留 `relay_win.exe`。
  - `Unity_RunCommand` 仍返回 `Transport closed`。
  - 因无可用 MCP，本轮未执行 Unity Editor 菜单生成。

## 下一批页面优先级

1. 继续先保 MainUI 可用入口: 对 `customerservice/team_* / onhook / partnerawake / templeawaken / tt_record` 等入口做 route catalog，确认 real / placeholder / missingPrefab 三类。
2. `shop` 下一步不要手工造 prefab；应通过 LayaUI 转换器/回填/Addressables 生成 `ShopModule`，然后再把占位回落视为异常兜底。
3. 旧端需要巡检前置清理方案，否则每 15 分钟都会被随机活动页吞掉对比时间。
4. 继续首批真实页深巡: `role`、`bag`、`setting`、`chat`、`map`，验证入口、背景/窗框、关闭/返回、主要按钮可点。
