# Team UI 路线静态审计 v4

## 结论

- 路线保持 `mainui.team`，按 schema 6 拓扑不可变规则从修正 manifest 全新 `init/apply/validate`。
- v4 共 `140` 节点、`111` 叶；所有叶及父节点均为 `blocked`，`done=0`、`needs-runtime-verify=0`。
- v1/v2/v3 原账均保留；v3 因把老端查询逻辑误记成“场景变化定时刷新”而 superseded，未原地改其 manifest 或 ledger。
- 本轮代码与 Prefab 保持 v3 已验证现状，只新增 v4 审计输出；未启动 Unity、浏览器或前台程序，未执行账号写事务。

## v3 语义错误与 v4 修正

老端 `TeamInviteView.GetAreaScenePlayer/onTimer` 的真实路径不是监控场景变化：

1. 进入附近查询时先对当前 `scene_mgr.GetSceneId()` 发送一次 `24053`。
2. 只有当前 scene 满足 `IsFieldScene()` 时，才清理旧 timer 并建立 `0.1s` period timer。
3. timer 使用 `GetAllFieldScene(ClientMapConfig)` 取得全部野外 scene；遇到当前 scene 时跳过并继续，其余 scene 逐个发送 `24053`。
4. 遍历到末项时 `ClearTimer`。
5. 页面 `Remove` 再兜底 `ClearTimer`。

v4 将其拆成当前 scene 首查、野外条件扇出、timer 生命周期、GetAllFieldScene 遍历父节点、跳过当前 scene、逐 scene 查询、遍历结束清理和 Remove 清理。v3 的节点 `mainui.team.view.invite.nearby.query.scene-change-timer` 及其旧同级查询/销毁叶不进入 v4。

## 状态边界

- 上述查询/生命周期仅从老端静态源码确认；Unity 页面级 `TeamInviteView` 尚未落地，无法验证 timer、返回聚合、销毁清理及真实 UI 更新，故全部 `blocked`。
- `hall.row-render` 继续因 Common `CustomHeadItem` 自定义头像/头像框缺口及无 Unity/真实 Web 证据保持 `blocked`。
- 申请、邀请、创建队伍、匹配、入队、投票等真实写事务仅枚举，未新增可执行绑定或发包。
- `24011`/`24042` 继续保持既定 dead 边界；页面级 Prefab、弹窗、列表、声音与运行链均未被静态结果冒充完成。

## 验证

- `verify-static.ps1` 明确断言 v4 新父子节点、旧 `scene-change-timer` 等 v3 错误节点不存在，以及 `140/111` 固定计数和全账 `blocked`。
- 固定校验 v1 manifest SHA `c118469eeec360a1a53eed12f160ea1e85eff38a12c01e65406b7175250188c8`、v1 ledger SHA `b5baf384daf6934dc89a6f2b8078380cb581f3b432c3dabc6d299b1eccecbae3`；同时固定 v3 manifest/ledger SHA，证明未修改 superseded 原账。
- 验证脚本调用 `route_ledger.py validate`，并使用 v1 独立 output-only `Team.StaticCheck.csproj` 编译当前 TeamHallItem；不触碰 Unity `Temp/Core`。
- 本轮不更新 Docs/AGENTS：它们在用户指定范围中只读，路线审计事实保存在 v4 输出目录。
