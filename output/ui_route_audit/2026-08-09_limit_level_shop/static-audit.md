# LimitLevelShop 静态审计（2026-08-09）

- 已有可编辑 `LimitLevelShopModule.prefab`，因此按 `fix-view` 增量接管，未重转模块。
- 61200 现保留 `grade_state/old_grade_state/act_condition/open_times`，并按老端自动请求 61203；61201/15804 未注册、未发送。
- 61201..61225 图标路由按 `act_condition.pic` 定位精确礼包；type=66 明确不误开普通限购页。
- Prefab 的 View/Tab/Reward 保留原序列化字段并换为业务子类；页签、只读价格/奖励/倒计时/关闭已接。
- `show_model/show_effect` 仍缺真实模型/特效消费与两时点像素证据；MainUI 当前对 612 图标的实际布局过滤在本文件岛之外。
- 独立编译：`LimitLevelShop.Isolated.csproj`，0 error；未启动 Unity、浏览器或前台程序。
- 真实 Web、两档 viewport、滚动末项、共享奖励详情、冷暖重开、动态模型/特效均未执行，不得标 done。
- 购买/支付为账号写事务，台账保持 blocked。
