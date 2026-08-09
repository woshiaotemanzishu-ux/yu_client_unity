# Team UI 路线静态审计 v3

## 结论

- 路线仍为 `mainui.team`，本次依据 schema 6 拓扑不可变规则新建 v3；v1/v2 均作为 superseded 历史账保留，没有原地改节点树或状态。
- v3 共 `132` 个节点、`105` 个叶子；所有叶和父节点均为 `blocked`，`done=0`、`needs-runtime-verify=0`。
- 未启动/控制 Unity、浏览器或任何前台程序，未执行账号写事务，未新增申请、邀请、创建、匹配、入队等可执行发包绑定。

## v3 拓扑补项

1. `TeamInviteView` 附近页的查询叶提升为生命周期父节点，并拆出：
   - 场景变化定时检测；
   - 变化后 `24053` 重拉；
   - destroy 时 `ClearTimer`。
2. `TeamChangeTargetView` 的 confirm 提升为互斥分支父节点，并拆出：
   - 已有队伍时发送 `24017`；
   - 无队伍时调用 `ChangeTargetSuccess(params)`。
3. v2 已补的 `24010`/`24047` 打开查询、按钮条件组、非队长提示、世界喊话冷却、`join_type` 勾选态、数字键盘、四类上下限失败态、列表项选中态和 Sentient Alert 分支均由 v3 验证脚本逐节点断言保留。

## Team 岛内静态增量

- `TeamHallItem` 与现有 HUD `TeamMainRoleItem` 的在线距离语义统一为：同一场景，或双方场景均满足只读 API `MainUIConfigs.IsFieldScene(sceneId)`，显示“附近”。未修改 `MainUIConfigs` 或任何 MainUI 文件。
- `TeamHallItem` 继续保持纯展示消费：没有申请队伍按钮绑定，也没有恢复老端为空的头像菜单；异步头像采用 render version 防列表复用串行，并在空数据/重用时隐藏旧头像。
- v1 独立 output-only 编译 stub 仅补入既存 `MainUIConfigs.IsFieldScene` 的签名，用于编译当前 Team 展示代码；v1 manifest 与 ledger 的固定 SHA-256 没有变化。

## 仍阻塞

- `mainui.team.view.hall.row-render`：字段显示、列表复用清理和 field-scene 语义虽已静态实现，但 `CustomHeadItem` 的自定义头像/头像框消费位于 Common 跨文件岛，当前无法补齐；同时缺 Unity/真实 Web 列表复用、两档 viewport 与像素证据。因此组合行明确保持 `blocked`。
- Team 页面级 Prefab/View 大部分仍缺失；首次转换依赖 Unity 与老端/Unity 运行快照，本轮禁止启动相关程序，不能静态冒充完成。
- 所有真实写事务仅枚举；未授权账号写入、成功/失败回包、父页即时刷新和关闭重开均未执行。
- `24011` 保持死客户端流负约束；`24042` 保持无老端发送点、回包解码丢弃的死读流边界。
- 声音、滚动裁剪、弹窗身份、返回链、两档 viewport、cold/warm、同账号老 H5/Unity Web 顺序复走均缺运行证据。

## 校验边界

- `route_ledger.py validate`：v3 schema 6 通过。
- 独立 `Team.StaticCheck.csproj`：仅输出到审计目录自身的 `bin/obj`，不触碰共享 Unity `Temp/Core`；`0` warning、`0` error。
- `verify-static.ps1` 硬编码校验 v1 manifest SHA `c118469eeec360a1a53eed12f160ea1e85eff38a12c01e65406b7175250188c8` 与 v1 ledger SHA `b5baf384daf6934dc89a6f2b8078380cb581f3b432c3dabc6d299b1eccecbae3`，并逐项断言 v2/v3 新节点、纯展示代码、Prefab GUID 以及全账 blocked 状态。
- 本轮不触发 Docs/AGENTS 更新：用户明确将其排除为只读范围；审计事实保存在路线输出目录。
