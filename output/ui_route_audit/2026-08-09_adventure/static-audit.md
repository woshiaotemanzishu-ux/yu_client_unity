# Adventure 静态审计

- schema 6 topology: nodes=423, leaves=334
- leaf statuses: blocked=313, needs-runtime-verify=21, done=0
- 当前 Prefab 已由四个业务子类增量接管；入口注册 42701/42702，主页面只读 42701 状态可刷新。
- 42702/42703/42705/42706 未注册、未发送、未执行；42704 因配置及商品模型闭包缺失保持 blocked。
- 未启动 Unity、浏览器或前台程序，未做账号写事务；所有视觉、点击、模型、特效、即时刷新与重开均未静态冒充通过。

## 定向检查

- PASS bootstrap-route-a: Assets/Scripts/Module/Core/Adventure/AdventureBootstrap.cs contains ICON_TYPE_A, AdventureFlow.Toggle
- PASS bootstrap-route-b: Assets/Scripts/Module/Core/Adventure/AdventureBootstrap.cs contains ICON_TYPE_B, AdventureFlow.Toggle
- PASS flow-prefab: Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs contains AdventureModule
- PASS flow-hides-board-template: Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs contains GetComponentsInChildren<AdventureItem>
- PASS flow-hides-shop-template: Assets/Scripts/Module/Core/Adventure/AdventureFlow.cs contains GetComponentsInChildren<AdventureShopItem>
- PASS main-safe-throw: Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs contains 投掷事务尚未完成安全接入
- PASS main-safe-reset: Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs contains 重置事务尚未完成安全接入
- PASS main-ended-guard: Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs contains 活动已经结束
- PASS main-reset-placeholder: Assets/Scripts/Module/Core/Adventure/AdventureMainView.cs contains 剩余重置次数:--
- PASS shop-safe-refresh: Assets/Scripts/Module/Core/Adventure/AdventureShopView.cs contains 商店刷新事务尚未完成安全接入
- PASS model-current-free: Assets/Scripts/Module/Core/Adventure/AdventureModel.cs contains CURRENT_BASE_FREE_THROW_TIMES = 4
- PASS model-endpoint-no-red: Assets/Scripts/Module/Core/Adventure/AdventureModel.cs contains HasFreeThrowRed => HasBoardState && !IsAtResetPosition && HasFreeAction
- PASS controller-board-read: Assets/Scripts/Module/Core/Adventure/AdventureController.cs contains RegisterProtocal(Proto.ADVENTURE_BOARD_STATE
- PASS controller-red-semantics: Assets/Scripts/Module/Core/Adventure/AdventureController.cs contains model.HasFreeThrowRed
- PASS no-write-send: no SendFmt(42702/42703/42705/42706)
- PASS main-business-guid: 77160b1021d0483cb77a24940140dc47
- PASS shop-business-guid: c9d08b16cf654c06952fcf8d88f5cfd4
- PASS board-item-business-guid: 1fc883dbc57747b394a02d55290d153f
- PASS shop-item-business-guid: dacd186b40cd4ed0bde37fec6efd47d2
- PASS legacy-config-kv: expected=14 actual=14
- PASS legacy-config-rand: expected=2 actual=2
- PASS legacy-config-reward: expected=32 actual=32
- PASS legacy-config-loc: expected=600 actual=600
- PASS schema-topology-node-count: nodes=423
- PASS schema-leaf-count: leaves=334
- PASS all-leaves-explicit: no done/not-run leaves
