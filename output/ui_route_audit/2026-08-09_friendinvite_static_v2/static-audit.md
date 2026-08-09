# FriendInvite 静态路线审计 v2（schema 6）

## 版本与边界

- v1 保留为 superseded：其 70 节点拓扑遗漏背景关闭、共享 TabItem/HelpItem 状态矩阵、主按钮条件和 Reset/晚到/解绑生命周期叶。
- v2 是唯一权威静态台账，共 83 节点。既有 Prefab 按 `audit-game-ui-route → fix-view` 增量处理，没有 convert/rebake/Creator。
- 未启动 Unity、浏览器或前台程序；未登录账号；未执行分享、领取、兑换或发奖。

## QA 代码修复

- `FriendInviteMainView.OnShow` 只订阅更新并消费现有 Model 快照，不再重复 `RequestStartup`。启动序列仍由 GAME_START 权威链发送。
- 更新事件改为 `OnShow` 订阅、`OnHide`/`OnDispose` 解绑；`PrepareForRelease` 为 Reset 释放前提供幂等解绑。
- `FriendInviteFlow` 增加 generation、`try/finally` 和异常清理。Reset 递增 generation；await 晚到实例立即 Release，不能回填 `_moduleRoot` 或 Show。

## 补全拓扑

- 主窗补背景关闭；preview、instruction、全部页签和隐藏页面按真实实现缺失标 `blocked`。
- 主按钮补每日上限、recover_time 冷却、not_fire 防重三分支。
- `FriendInviteTabItem` 补标签渲染、选中态、主窗单页/四页与 Shop 共享消费者。
- `FriendInviteHelpItem` 补 status=0/1/2/3；status=2 的 Help lv=10 / Level lv=180 均通向 34007，写事务 blocked。
- 删除无老端点击事实的 `help.slot-detail` / `level.slot-detail` 伪叶。
- 11301 定义为 `read/read-only`，但当前 hard-negative 且无消费者，明确 blocked；11302 仍为写事务 blocked。
- 34010 与 34011 拆为两个独立 absent/KILL 叶并 blocked。
- 补真断线 Reset、await 晚到释放、订阅解绑三个生命周期叶。

## 状态边界

- 34002/03/04/07/09、11302 以及兑换写事务均未发送。
- 34010/34011 不存在常量、注册、sender 或 UI；11301/11302 未接入。
- 静态实现过的主窗快照、关闭和生命周期修复仍只能 `needs-runtime-verify`；其余缺实现或未授权叶均 `blocked`；0 `done`。

