# GrowthBenefits 静态盘点（2026-08-09）

## 完成边界

- 本轮仅核对老端源码/配置、Unity Prefab/Bind/业务代码；未启动 Unity、浏览器或前台程序。
- 未执行 41722 领取或任何账号写事务。
- 全部只读叶最多为 `needs-runtime-verify`，领取/跨岛缺实现叶为 `blocked`；不声称真实 Web、玩家可见像素、拖动、即时刷新、重开或性能完成。

## 老端页面与控件事实

- 41720 图标打开外层 `GrowthForceView`；其中成长福利页 `GrowthBenefitsView` 与战力福利页并列，后者条件显示。
- 成长福利页：横向 1～7 天页签、纵向任务列表、不可见红点提示动画、未到底箭头。
- 默认页：优先第一个有红点日，否则第一个已解锁且未全领日；任务按可领→前往→已领排序并滚到首个可领项。
- 页签覆盖选中/未选、解锁/未解锁、红点、全领完成图；未来日点击提示第 N 天解锁。
- 任务行覆盖描述、`(process/target)`、奖励横列、前往/领取/已领取三态；前往走 `OpenFun(jump_id)` 并关闭外层，领取仅 status=1 发 41722。
- 老端源码中未发现页面专属声音调用。

## Unity 静态现状

- `Assets/Prefabs/UI/GrowthBenefits/GrowthBenefitsModule.prefab` 与四个 Generated Bind 已存在，不能重转。
- Prefab 的四个根仍直接绑定 Generated Bind；`Assets/Scripts/Module/Core/GrowthBenefits` 只有图标/协议 Model+Controller，没有页面业务 View、外层 GrowthForceView 或可达打开 Flow。
- 当前 `GrowthBenefitsModel` 只保存 `task_id -> status`，41720/41721 的 `process` 被读掉；配置任务 ID 和开放等级硬编码，不能驱动页签、描述、条件目标、奖励、jump_id。
- 41720/41721/41722 协议已注册；但 41722 当前成功回包只更新入口图标，没有已打开页面事件消费者。

## 组件依赖与状态矩阵

- `GrowthForceView`：成长/战力两外层页签及关闭链；当前 Unity 文件岛外缺实现。
- `GrowthBenefitTaskItem`：缺状态/不可领、可领、已领；短/长描述、不同进度/目标、1～N 奖励、红点开关。
- `BaseAwardItem`：78x78 奖励格；空/有数据、数量1/多、锁定态、详情弹窗。
- `OpenFun`：jump_id 导航；跨模块，只登记，不猜映射。

## 静态缺陷与 blocker

1. 入口仅能增删图标，缺图标点击到 GrowthForce/GrowthBenefits 的页面路由。
2. GrowthBenefits Prefab 直接挂 Generated Bind，无业务 View 接管；打开后也不会生成页签/任务/奖励或绑定点击。
3. Model 丢弃 process，硬编码配置任务集合，无法实现页面展示。
4. 外层 GrowthForceView/战力页位于本文件岛外，不能在本支线创建替代壳。
5. `OpenFun` 与共享 `BaseAwardItem` 位于岛外，不复制私有实现。
6. 41722 是发奖写事务，本轮无授权且明确禁止点击，保持 `blocked`。
7. 当前无法验证同账号真实 Web、滚动、两档 viewport、cold/warm、声音与生命周期，均保持 `needs-runtime-verify` 或因缺实现 `blocked`。
