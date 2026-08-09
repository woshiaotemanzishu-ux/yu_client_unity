# BaseDungeon / 限时塔静态审计摘要

## 范围与事实源

- 路线：`mainui.base-dungeon.limit-tower`，主界面活动入口 `331@97`。
- Unity 已有且实际专属的可编辑 Prefab 为 `Assets/Prefabs/UI/DungeonTower/DungeonTowerModule.prefab`，因此按 `fix-view` 增量接管，未执行首次转换或重建。
- 老端事实源：`E:/GitProject/yu_client/cdn/js/bundle.js` 中 `BaseDungeonController`、`BaseDungeonModel`、`DungeonTowerBaseView`、`DungeonTowerView`、`DungeonTowerItem`；轮次目录来自 `E:/GitProject/yu_client/cdn/resource/config/server/config_limit_tower_round.json`。
- 协议事实源：服务端 `pt_611.erl` 与 `pp_dungeon_sec.erl`；61117 下发 `round/over_time/reward_mode/pass_list`，61118 请求 `Round:u8`、回应 `Code:i32`。

## 完整控件与叶子清单

- 条件入口：活动图标显隐、倒计时、红点、点击打开。
- 页面框架：单页签标题与轮次大背景、返回链、活动内容底图。
- 状态控件：通过进度、结束倒计时、大奖 `reward_mode=0/1/2`、大奖物品详情、领取按钮。
- 关卡区：贝塞尔曲线拖动；轮次 1 的 40101–40110、轮次 2 的 40201–40215、轮次 3 的 40301–40320，共 45 个条件关卡格。
- 每个关卡：选择/通过/可挑战状态、详情、3 个奖励物品详情叶、挑战事务与战斗场景进入。
- schema 6 共 376 个节点，覆盖 3 个条件轮次、45 个关卡、135 个奖励详情叶以及入口、状态、列表、事务、弹窗依赖和返回链。

## 本岛静态实现

- `BaseDungeonController` 注册 `331@97` 路由，保留 61117 `pass_list`，并按服务端格式接入 61118 权威成功回包；本轮未执行任何账号写事务。
- `BaseDungeonFlow` 复用现有 `BaseWindowSkin` 与 `DungeonTowerModule`，不重建人工 Prefab。
- `DungeonTowerView` / `DungeonTowerItemView` 继承只读 Generated Bind，并在 Prefab 中替换为成套业务脚本绑定；模板节点默认隐藏，避免烤制快照冒充真实列表。
- 已接入权威可确定的入口、倒计时、通过数、大奖三态和领取成功后的即时状态更新。总关数、关卡格、奖励和挑战按钮在权威配置/跨岛链缺失时安全隐藏或保持 blocked。

## 显式 blocker 与运行门禁

- Unity 当前缺 `config_limit_tower_round` 与 `config_dungeon_grade`，不能生成真实关卡目录、总进度、奖励或可挑战条件。
- 轮次大背景 `ui_limit_tower_1..3.jpg` 不在当前 Unity 资源闭包；不得用猜测资源替代。
- 老端 6 个复用关卡格沿贝塞尔曲线拖动；缺真实运行几何与 Prefab 具名槽位，禁止用普通纵向列表或代码猜坐标。
- 挑战依赖禁止修改的 Dungeon `61001/TryToEnterDungeon` 与战斗场景生命周期，只登记跨岛 blocker。
- 物品详情和恭喜弹窗依赖 Common 共享链，本岛不复制共享组件。
- 61118 为真实发奖写事务；未获账号写授权，因此未点击、未验证即时到账、恭喜弹窗与关闭重开。
- 未启动 Unity、浏览器或前台程序；GraphicRaycaster 点击、cold/warm、两档 viewport、真实同账号状态矩阵、像素对比与重进一致性均保持 `needs-runtime-verify` 或 `blocked`，没有任何 `done`。

## 静态验证

- schema 6 `init/apply/validate`：376 节点，`blocked=370`、`needs-runtime-verify=6`、`done=0`。
- output-only 独立 `static-compile.csproj`：0 warning / 0 error；编译与还原输出仅位于本路线目录下的 `artifacts/` 与 `obj/`。
- 三个新增业务脚本 `.meta` GUID 唯一；Prefab 中两个 `m_Script` 与 `m_EditorClassIdentifier` 成套，旧 Generated 脚本 GUID 无残留。
- 目标 Prefab 无 `m_Script fileID: 0`，全部 `m_Children` 引用均能解析；JSON 可由 UTF-8 解析；定向 `git diff --check` 通过。
