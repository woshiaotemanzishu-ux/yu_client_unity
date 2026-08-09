# ListDuobao 静态审计（2026-08-09）

- 已有 `ListDuobaoModule.prefab` 与独立 `ListGoodsItem.prefab`，按 `fix-view` 增量修复，未重转。
- 入口保持共享父 `331@110` 不抢占，特殊活动精确注册 `331@116@0`；请求/结果保持老端 33191→33803 不对称语义。
- 修复模块内 Rank/Reward/Goods 克隆未走 `BaseView.Show()` 的绑定缺陷，并覆盖独立 ListGoodsItem 消费。
- 修正主页排名点击面（`_img_bg2/_img_arr/_gp_rank`）、说明 332116、活动倒计时、12 槽首奖励、消耗文本/颜色。
- 补齐排行页签/徽章/未上榜/分数/区服前缀显示，记录页同时消费 LogList/SelfList 并提供空态与内容高度。
- 独立编译：`ListDuobao.Isolated.csproj`，0 error；未启动 Unity、浏览器或前台程序。
- 主页内嵌排行 `ListRankItemBig` 的完整等价表现仍无 Unity 业务组件；真实拖动/裁剪、资源 ready、old/unity 像素和冷暖重开均未执行。
- 单抽、十抽、阶段领取及其结果均为真实账号事务，本批只枚举并保持 blocked。
