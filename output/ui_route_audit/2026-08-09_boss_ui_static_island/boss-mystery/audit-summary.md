# BossMystery 静态路线审计

完成层级：静态接管实现；未运行 Unity、浏览器或账号事务，整页仍不得标 `done`。

## 映射与现状

- 老端入口：`CrossServerEnterView` 第 3 页签“太古遗凶” → `BossMysteryEnterView`。
- 老端子树：`BossMysteryEnterView` → `BossMysteryRoomView` → 层页签/`BossMysteryMonItem`；阶段奖励弹窗为 `BossMysteryRewardView` → `BossMysteryRewardItem`。
- Unity Prefab：`Assets/Prefabs/UI/BossMystery/BossMysteryModule.prefab`，包含上述 5 个 Bind 组件与内嵌模板。
- 修复前只有 `Shenxiao.Generated.UI.BossMystery.*Bind`，非 Generated 业务层零消费者，页面静态可见但无数据、点击、列表克隆或协议生命周期。
- 本轮新增页面专属 subclass，并将 Prefab 中 5 个 `m_Script` GUID 精确替换为新 subclass；序列化 Bind 字段继承复用，未改 Generated。

## 静态接入

- `BossMysteryFlow`：加载既有 Boss 配置后请求 46000(type=20)、46037、46039。
- 主页面：订阅 46000/46007/46037/46039 事件，接层内列表、选择、关注、掉落记录请求、阶段奖励弹窗和宝箱切换。
- 房间/列表项：从 `BossModel.GetBossState(20)` 克隆真实模板，接选择与 46003 进入；次数不足本地拦截。
- 奖励弹窗：三档 1/3/6 击杀状态与 46038 领取入口；阈值直接来自 `Assets/GameRes/resource/config/server/config_domain_kill_reward.json` 的 `kill_boss_num`，不是推测。

## 仍需修复/验证

- 46000 只有协议状态，老端页面还依赖 `config_kf_great_demon/config_mon` 的名称、层、等级、头像、模型资源、票券成本与职业奖励；当前显示为保守 ID 文本，不伪造映射。
- 46039 宝箱位置与特殊/普通宝箱切换需要配置语义结合；当前循环选择只是静态可操作接线，必须按老端运行态收敛。
- 模型和奖励物品格尚无共享组件授权闭包，需跨模块依赖清单后接入；缺 RT 双帧非透明像素证据。
- 掉落记录、CrossServer 指引与返回属于共享壳/公共弹窗，按文件岛登记 blocker。
- 所有列表拖动、裁切、末项可达、cold/warm、资源幂等、两档真实 Web 和账号事务均未执行。

## 建议独占文件

- `Assets/Scripts/Module/Core/Boss/Views/BossMystery/**`
- `Assets/Prefabs/UI/BossMystery/BossMysteryModule.prefab`
- `output/ui_route_audit/2026-08-09_boss_ui_static_island/boss-mystery/**`
