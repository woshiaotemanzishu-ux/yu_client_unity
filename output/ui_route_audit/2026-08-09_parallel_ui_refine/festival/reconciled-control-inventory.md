# Festival 三方调和与组件依赖

| 区域 | 老端源码/配置 | Unity Prefab/Bind | Unity 业务消费者 | 本轮状态 |
|---|---|---|---|---|
| 入口 223 | 条件显示、入口红点、首次引导 | FestivalModule 可加载 | 仅容器级 Toggle | 生命周期静态修复后 NVR |
| 三页签窗口 | 任务/奖励/进阶及各自背景/货币 | 三个 View Bind 都存在 | 无页签选择/窗口壳消费者 | blocked |
| 任务页签 | 1/2/3 类、红点、倒计时 | TaskTabListItem/TaskView | 无配置/列表克隆/点击绑定 | blocked |
| 任务列表 | 状态 0/1/2、前往/领取 | TaskListItem Bind | 无 task config 与 OpenFun 消费者 | blocked |
| 奖励列表 | 等级/普通/高阶奖励及领取态 | AwardListItem/LevelAwardView | 无 lv-exp config/列表消费者 | blocked |
| 进阶两档 | 详情、奖励、支付/勾玉确认 | Commodity/GetReward Bind | 无 fiesta-act/recharge 配置与支付闭环 | blocked |
| 升级弹窗 | 滑条、购买、确认、不再提示 | UpLevel/UpLevelTips Bind | 无 quick-buy/商城闭环 | blocked |
| 返回链 | 遮罩关闭、按钮关闭、跳转关闭 | 多个 close/cancel/goto Bind | 无 BaseView Show/Hide 路由 | blocked |

组件依赖：

- `FestivalAwardListItem`、`FestivalRewardItem`、`FestivalInfoListItem` 依赖共享 `BaseAwardItem` 视觉/详情语义；本轮禁止写 `Common`，仅登记 blocker。
- 任务/奖励/进阶列表必须复用各自现有 Festival 私有模板，不能复制页面节点树。
- 充值与勾玉购买依赖支付、Shop、Confirm/Tips 等共享链；本轮没有交易授权，不能用本地扣费或 19405 发送冒充成功。
- 红点依赖 19401/19403 权威状态；只验证 `ActivityIconManager.SetIconRedDot` 调用存在不能完成运行态闸。

需要的运行态矩阵：任务 1/2/3 类、无数据/有数据、0/1/2 状态、短/长文案；奖励普通/高阶未购/已购、可领/已领、首项/末项；进阶豪华/至尊、直购/勾玉、余额充足/不足；弹窗显示/关闭/重开。所有矩阵均未在本轮执行。
