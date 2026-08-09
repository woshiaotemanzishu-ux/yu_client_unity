# PushGift/推送礼包静态三方调和

## 范围与结论

- 路线：`mainui.push-gift`
- schema：6
- 拓扑：91 nodes（17 pages、74 leaves），所有 page 的 `control_inventory` 与直接子节点双向一一对应。
- 台账状态：`blocked=31`、`defect=56`、`needs-runtime-verify=4`，根节点为 `blocked`，`done=0`。
- 允许写入岛：`Assets/Scripts/Module/Core/PushGift/`、PushGift 自属 Prefab/资源最小闭包、当前独立 output。
- 实际生产代码/Prefab 改动：无。PushGift 模块起始 clean；本轮只新增当前 output。
- 决策：Unity 只有 `PushGiftController`/`PushGiftModel`，没有 PushGift 自属可编辑 Prefab。老端完整页面依赖 MainUI 入口、共享弹窗队列、Common 物品详情、Shop/Vip 充值、配置与资源导入链；这些均为本轮禁区，资源闭包无法证明在岛内闭合，因此不调用 `convert-module`，也没有可供 `fix-view` 增量修复的 Prefab。

## 权威源

### 老端运行语义/源码/配置

- `E:/GitProject/yu_client/h5/src/commonController/PushGiftController.ts`
- `E:/GitProject/yu_client/h5/src/commonModel/PushGiftModel.ts`
- `E:/GitProject/yu_client/h5/src/pushGift/PushGiftEntView.ts`
- `E:/GitProject/yu_client/h5/src/pushGift/PushGiftView.ts`
- `E:/GitProject/yu_client/h5/src/pushGift/PushGiftItem.ts`
- `E:/GitProject/yu_client/h5/src/pushGift/PushGiftTips.ts`
- `E:/GitProject/yu_client/h5/src/mainUI/GiftPushIcon.ts`
- `E:/GitProject/yu_client/h5/bin/assets/resource/config/server/config_push_gift.json`
- `E:/GitProject/yu_client/h5/bin/assets/resource/config/server/config_push_gift_reward.json`

两份配置各有 135 行，覆盖 14 个 `gift_id` 与 31 个 `sub_id`。本轮只读确认，没有复制或改写配置。

`LvLimitBreakView.ts` 虽复用 `PushGiftTips` 视觉结构，但业务属于 CustomActivity/33105；本轮禁止 Activity，因此仅登记为排除依赖，不纳入 PushGift 路线节点，也不修改。

### Unity 现状

- `Assets/Scripts/Module/Core/PushGift/PushGiftController.cs`
- `Assets/Scripts/Module/Core/PushGift/PushGiftModel.cs`
- 只读消费者：`Assets/Prefabs/UI/MainUI/GiftPushIcon.prefab`
- 只读绑定：`Assets/Scripts/Generated/UI/MainUI/GiftPushIconBind.cs`
- 只读静态用例：`Assets/Editor/CliVerify/Cases/PushGiftCase.cs`

未发现 `Assets/Prefabs/UI/PushGift/` 或 PushGift 自属资源目录；不得把 MainUI 的 `GiftPushIcon.prefab` 冒充 PushGift 完整页面 Prefab。

## 三方调和结果

| 领域 | 老端事实 | Unity 事实 | 结论 |
|---|---|---|---|
| 启动顺序 | `GAME_START` 严格发送 `19104 -> 19101` | `RequestStartup` 同顺序发送 | 静态一致，真实回包/重复启动 `needs-runtime-verify` |
| 活动列表 | 19101 包含 type、标题、礼包名、infos，并驱动新增/更新/删除与自动弹窗 | 解析 type，但仅保存 gift/sub/end/red，标题、礼包名和 infos 无 UI 消费 | UI 与状态分支 `defect` |
| 详情 | 每个 `(gift_id, sub_id)` 显式请求 19102；奖励顺序和重复项有语义 | 复合键缓存存在，保留 wire 顺序与重复项 | 静态一致，真实回包/空奖励/无回包 `needs-runtime-verify` |
| 主入口 | GiftPushIcon 展示红点、倒计时、点击打开指定礼包 | 入口归属 MainUI，PushGift 岛无业务 View | 跨 MainUI `blocked` |
| 共享通知 | PushGift 红点进入主界面汇总 | Model 提供计数，消费者位于 MainUI | 共享通知链 `blocked` |
| 自动弹窗 | 19101 type=1/2/3 进入 ViewGiftPopModel 队列，关闭后继续下一项 | PushGift 岛没有共享队列/弹窗 View | 跨共享队列 `blocked` |
| 主窗口 | PushGiftEntView 动态建礼包页签并按请求 gift 默认选中 | 无 PushGift 窗口 Prefab/View | `defect`，且转换闭包不清 |
| 礼包页 | 标题、说明、倒计时、红点清除、19102 详情、空态、纵向档位滚动 | 无 renderer | `defect` |
| 档位项 | 原价/现价、折扣、限购、售罄、倒计时、奖励列表、购买 | 无 renderer，未注册 19103 | 视觉/状态 `defect`；购买 `blocked` |
| Tips | 自动弹窗含奖励、价格、折扣、倒计时、购买、背景/按钮关闭 | 无 Prefab/View | `defect`；购买/充值 `blocked` |
| 奖励详情 | 共享 EquipmentItem/物品详情 | 依赖 Common | 跨 Common `blocked` |
| 购买结果 | 19103 成功后奖励弹窗、父页即时刷新、钱包/背包更新、重拉 | Unity 故意没有 19103 sender/handler | 按硬约束 `blocked`，不得用静态假实现或账号写伪完成 |
| 充值/绑元兜底 | 余额不足时确认绑元或跳 OpenFun(21) | 跨钱包、Shop/Vip、共享 Router | `blocked` |
| 返回链 | 窗口关闭确认、勾选不再提示、Tips 双关闭路径、弹窗队列续弹 | 无业务 View | `defect`/共享队列 `blocked` |

## 完整控件树摘要

正式控件树已冻结在 `route-manifest.json`，覆盖：

1. 启动 19104/19101、19101 type 分支、等级变化重拉、19102 复合键缓存与过期。
2. MainUI GiftPushIcon 身份、红点、倒计时、点击、过期隐藏。
3. 通知汇总计数、事件刷新、点击返回链。
4. 自动弹窗队列、type=1/2/3 入队、顺序与关闭后续弹。
5. PushGiftEntView 窗口壳、动态页签、标签/红点/默认选中、关闭确认。
6. PushGiftView 标题、说明、详情请求、红点清除、倒计时、空态、滚动和末项可达。
7. PushGiftItem 档位身份、价格、折扣、限购、售罄、计时、奖励、购买、余额不足、结果刷新。
8. 奖励列表、裁剪/滚动、逐格详情与品质特效状态。
9. PushGiftTips 身份、详情、价格/折扣、计时、奖励、购买、售罄、充值、按钮/背景关闭。
10. hide、关闭重开、cold/warm、即时刷新。

## 协议与风险边界

- `19101`：活动列表/变更，只读接收；当前 UI 分支不完整。
- `19102`：显式 keyed detail query。按项目约束，不能把它改成隐式预取，也不能为它新增共享事件、红点或弹窗副作用；因此本轮没有添加所谓“详情更新事件”。
- `19103`：真实购买事务。项目硬约束要求完整可编辑页、配置与扣费冻结、确认、singleflight、错误出口、奖励弹窗、钱包/背包即时刷新、19101/19102 重拉和关闭重开闭环；当前条件不成立，且用户禁止账号写，因此发送、注册、模拟成功全部 `blocked`。
- `19104`：启动清理/同步请求，只保持现有 `19104 -> 19101` 顺序，不注册伪 handler。

## 未实施生产修复的理由

1. 现有 `RequestStartup` 和 19102 复合键缓存已满足本轮可静态证明的协议边界。
2. 添加无消费者的数据字段、空 View、日志按钮或临时代码建树不会形成玩家可见闭环，反而会掩盖缺少 Prefab、配置、资源和共享依赖的事实。
3. 页面首次落地需要跨 Generated、Addressables、Common、MainUI、Shop/Vip 与共享队列；用户明确禁止这些目录，资源最小闭包也无法证明，因此按 `convert-module` 边界停止。
4. 没有 PushGift 自属可编辑 Prefab，故 `fix-view` 无目标；不存在可安全增量修复后回原路线复验的对象。

## 验证

- `route_ledger.py init`：通过，schema 6，91 nodes。
- `route_ledger.py apply`：通过，正式账由通用脚本原子更新。
- `route_ledger.py validate`：通过，`blocked=31 / defect=56 / needs-runtime-verify=4`。
- manifest 自检：节点 ID 唯一；单根；17 个 page 均有非空 `control_inventory`；90 个 inventory 引用与 90 条父子边逐项一致；无孤儿、无 page 外挂子节点。
- 配置只读统计：两份配置 JSON 均可解析，各 135 行、14 gift_id、31 sub_id。
- 本轮未启动 Unity、浏览器或 Computer Use；未使用账号、GM、购买、领取、充值或任何写事务。
- 本轮未执行全 Core、Unity、WebGL 或 Addressables 构建；无生产代码变更，因此没有伪造编译结论。
- 真实 Web、两 viewport、像素、拖动、末项点击、模型/特效双帧、协议回包、即时刷新、关闭重开、cold/warm 全部未验证；正式状态保持 NVR/blocked/defect。

## 文件清单

仅新增：

- `output/ui_route_audit/2026-08-09_pushgift_static_v1/route-manifest.json`
- `output/ui_route_audit/2026-08-09_pushgift_static_v1/route-ledger.json`
- `output/ui_route_audit/2026-08-09_pushgift_static_v1/results-static-001.json`
- `output/ui_route_audit/2026-08-09_pushgift_static_v1/run-static-001/static-reconciliation.md`

未修改 `Assets/Scripts/Module/Core/PushGift/`、任何 Prefab/资源、MainUI、Activity、Welfare、Vip、Shop、Daily、Common、Generated、Proto、Addressables、Docs 或项目文件。按用户本轮显式禁区，不触发 Docs 更新。
