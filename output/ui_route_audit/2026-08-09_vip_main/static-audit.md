# VIP 主页面静态审计

## 范围与边界

- Unity 现有可编辑页面：`Assets/Prefabs/UI/Vip/VipModule.prefab`，因此只允许 `fix-view` 增量修复，不得重转。
- 老端事实源：`E:/GitProject/yu_client/h5/src/vip/*.ts`、`h5/src/commonController/VipController.ts` 与 `cdn/resource/game/vip/*.json`。
- 本轮未启动 Unity、浏览器或前台程序，未执行账号写事务。
- `45001/45002/45003/45007/45008`、`15902`、商品支付和外部支付均只枚举并标记 `blocked`。

## 已冻结控件树

- VIP 主窗：特权卡页签、特权福利页签、充值跳转、VIP 等级显隐、VIP 等级/经验/每日经验状态、关闭、声音合同。
- 特权卡：卡型 1/2/4 selector、卡详情/折扣/倒计时状态、左右特权说明列表、激活/购买、规则弹窗、免费卡提示、过期卡提示。
- 特权福利：上一/下一 VIP 等级、特权说明列表、专享奖励列表、专享礼包确认/取消、周礼包状态/奖励弹窗/领取。
- 充值子页：返回 VIP、关闭、下滑、商品列表、商品支付、活动奖励详情、福利领取、更多支付、VIP 经验状态。

## 静态差异

- `VipBaseView.OnInit` 当前把所有 VIP 子模板隐藏，两个一级页签和卡/福利内容没有运行时消费者。
- `VipBaseView.recharge_btn` 当前只写日志，没有打开已有 `RechargeView`。
- `RechargeView._btn_recharge` 当前只写日志，没有返回已有 `VipBaseView`。
- `RechargeView` 的商品模板被隐藏且没有只读商品列表消费者；支付、福利领取与外部支付保持阻塞，禁止为了“可点击”孤立接写协议。
- Unity 只读 `45000/45004/45005/45006/15800/15801/15802/15803/15901` 状态已存在；完整 VIP UI 与状态矩阵仍需真实运行复验。

## 共享组件依赖

- `VipAwardItem` 老端嵌套共享 `BaseAwardItem`；Unity 不得复制 Common 物品详情/奖励格。
- `RechargeItem.prefab` 是商品列表共享项；主页面只负责列表宿主和输入数据。
- `VipCardItem/VipSubFlagItem/VipInstructionItem/VipTabButton/VipTopCardItem` 当前作为 `VipModule.prefab` 内模板存在，页面不得另造平行节点树。

## 运行态缺口

- 需要同账号老 H5/Unity Web 顺序对照，覆盖两档 viewport、两页签、三卡型、等级左右边界、各条件状态、所有弹窗、列表真实拖动、关闭/热重开、cold/warm 和视觉 diff。
- 任何领取、购买、免费卡激活、VIP 显隐或支付操作都需要本轮独立授权、前置/结果指纹与权威回包闭环；本轮不执行。
