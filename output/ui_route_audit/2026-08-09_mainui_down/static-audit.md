# MainUI 底部菜单静态闭包（2026-08-09）

## 结论

- 路线：`mainui.bottom.fixed`，共 104 个节点；所有 page 均保存了直接控件清单，父状态由叶子推导。
- 当前 Unity 主 Prefab 是 `HudNavBar.prefab`（GUID `0715a5e771d183e4e92144afd89442c8`）与 `HudChatBar.prefab`（GUID `f15aec70ee87ee948a49d1e872380b11`），`MainUIModule.prefab` 以这两个 Region 实例组成主界面。`HudBottomBar.prefab` 同时保留旧合并副本，但不作为当前路线事实源。
- 本轮只修改两个干净、专属 View：好友/邮件红点和入口分支恢复老端语义；经验特效改为经验刷新时显示、约 1 秒后隐藏并复用 Handle。
- Prefab/Bind 的目标节点完整，未发现必须修改 Region Prefab 的静态确定缺陷，因此没有改 Prefab、Generated、MainUIRouter、MainUIModel、Activity 或其它 MainUI 区域。
- 由于禁止启动 Unity/浏览器，本轮没有任何叶子可以标记 `done`；红点聚合、限购商城特效、强化页路由按真实边界标 `blocked`。

## 老端完整控件拓扑

### MainUIDownView

- 固定背景 `_img_bg`。
- 经验块：`_img_exp` 填充、`_lb_exp` 文案、`_box_exp_effect` 上的 `ui_expbar`；老端经验刷新时播放，约 1 秒后隐藏。
- 翻面块：`_gp_turn/_img_turn`、翻面红点 `_img_red`；65 级开放，`show_type=0/1` 循环，图像 015/016 随行变化，红点汇总当前隐藏行。
- 第一行：`role / bag / pet / equip / treasure`。
- 第二行：`red / love / guild / composite / 232`。
- 每个动态图标都有：功能开放条件、图标资源、红点、点击路由、任务故事指引/返回链。
- 任务故事指引是条件特效，不是独立固定菜单入口。

### MainUIChatView

- 普通聊天 ScrollRect/Content/条目与系统消息 ScrollRect/Content/条目；滚动条不显示，点击列表区域打开完整聊天页。
- 固定设置入口。
- 固定好友入口 + 聚合红点；点击分支为：有好友申请或未读私聊时进好友；仅邮件有红时进邮件；两边都无红时进好友。
- 固定商城入口 + 红点 + 限购商城特效槽。
- 固定强化入口为 `ActivityIcon("158")`，不是活动栏中的普通列表项。
- 老端遗留 market 字段/入口已注释且不在当前 GetComponents 中，故作为“当前版本不存在”的负证据记录，不虚增路由节点。

## Unity 映射

| 老端控件族 | 当前 Unity | 静态结论 |
|---|---|---|
| 底部两行功能图标 | `HudNavBar/MainUIDownView` + `MainFuncIconItem` | 两行配置、开放条件、槽位、点击路由存在 |
| 经验条 | `_img_exp/_lb_exp/_box_exp_effect` | 数值/文案存在；本轮恢复 1 秒脉冲生命周期 |
| 翻面 | `_img_turn/_img_red` | 等级和图像切换存在；聚合红点缺共享模型 |
| 聊天与系统消息 | `HudChatBar/MainUIChatView` 两个 ScrollRect | 权威 ChatModel 数据、刷新、回底存在 |
| setting/friend/shop | 可见 Image 点击面 | setting/shop 路由存在；本轮补 friend/mail 权威分支 |
| stronger | `_box_strengthen` + `ActivityIcon("158")` | 展示链存在，目的页路由/实现不完整 |

## 路由与协议边界

- 已静态确认注册：`role`、`bag`、`pet`、`equip`、`treasure`、`red`、`love`、`guild`、`composite`、`232`、`chat`、`setting`、`friend`、`email`、`shop`。
- 底栏点击自身只做 `MainUIRouter.Open`，不直接发送写协议；各目的模块负责自己的请求和事务。聊天 HUD 消费 ChatModel 的 11001/11010 等权威消息，不从底栏伪造数据。
- 好友红点读取 `FriendModel.HaveNewApply`、`ChatModel.TotalPrivateUnread`、`MailModel.HasUnread`，监听对应三类更新事件；不执行好友/邮件写事务。
- `158` 没有 Router 注册，`MainUIStrongerView` 仍为空壳；因此强化点击和返回链保持 blocked。
- 商城红点已有 `EVT_SHOP_RED_DOT`；限购特效依赖 Activity 配置/时间窗，本轮不跨岛修改。

## 本轮增量修复

1. `MainUIChatView.cs`
   - 订阅/注销好友申请、邮件未读、私聊未读事件。
   - 恢复好友入口聚合红点。
   - 恢复老端 friend/email 精确点击分支。
2. `MainUIDownView.cs`
   - 记录经验/经验上限/等级变化。
   - 经验变化或界面显示时复用 `ui_expbar` Handle，显示约 1 秒后隐藏。
   - 销毁时递增版本、隐藏宿主并释放 Handle，避免异步后到残留。

## 明确阻塞与后续运行闸

- 动态功能图标红点和翻面红点：需要共享 `MainUIModel.GetMainFuncRedState`/事件聚合以及 `MainFuncIconItem` 消费者修复；本轮按文件岛边界未改。
- 强化页：需要新增专属 Flow/Bootstrap 并实现 `MainUIStrongerView`，然后注册 `158`；不能用其它页替代。
- 限购商城特效：需要 ActivityIconManager 的当前活动配置、时间窗和真实特效链；本轮禁止触碰。
- 运行验收仍需：两个 viewport、同账号老 H5/Unity Web、两行逐图标点击与返回、等级 64/65、各图标红点矩阵、好友/邮件四态、商城红点/限购态、聊天首/末条滚动、经验特效两时点像素、冷暖耗时、关闭重开与 Player/catalog/Git 指纹绑定。
- 本轮未启动 Unity、未启动浏览器、未执行任何账号写事务。

## 静态验证

- `route_ledger.py init/apply/validate`：schema 6 通过，104 节点；应用结果为 30 个父/叶 `blocked`、74 个 `needs-runtime-verify`。
- 页面控件清单断言：每个 `type=page` 的 `control_inventory.child` 与直接子节点集合精确相等。
- `dotnet build Shenxiao.Module.Core.csproj --no-restore`：被并发中的非本路线文件阻断，错误仅位于 `ShopFlow.cs`（缺 `ShopBulkPurchaseView`）、`GuildHelpFlow.cs`（缺 `GuildHelpRuntime`）、`GuildMainFlow.cs`（缺 `GuildJoinRuntime`）。
- `dotnet build output/ui_route_audit/2026-08-09_mainui_down/MainUIBottom.StaticCompile.csproj --no-restore`：通过，0 警告、0 错误；该隔离编译引用当前 Unity/项目程序集并只编译本轮两个修改文件，不把全 Core 的并发缺口伪装成成功。
- `git diff --check`：通过；范围审计仅发现两个 MainUI 专属 View 与本路线 output，没有 Prefab/Generated/共享 Router/Model/Activity/其它 MainUI 区域改动。
