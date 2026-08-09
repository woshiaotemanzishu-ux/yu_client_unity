# Bossdomain 静态路线审计

完成层级：页面专属静态接管；未启动 Unity/浏览器，未执行账号写事务，所有叶保持 `blocked` 或 `needs-runtime-verify`。

## 映射与现状

- 老端入口：`CrossServerEnterView` 第 2 页签“镇煞封魂” → `BossDomainView`。
- 老端主树：`BossDomainView` → `BossDomainItem`、`BossDomainBuyView`、`BossDomainHelpAlert`、`BossDomainStageShow`、`BossDomainRewardItem`。
- 战斗场景树：`BossDomainSceneView` → `BossDomainScenePanel`、`BossDomainDoubleView`、`BossDomainResultView`；结算含普通/双倍两套奖励列表。
- Unity Prefab：`Assets/Prefabs/UI/Bossdomain/BossdomainModule.prefab`，已有上述 10 个 Generated Bind 模板；修复前无非 Generated 业务 View 消费者。
- 本轮新增 10 个页面专属 subclass，并把 Prefab 中每个 Bind 的 `m_Script` GUID 与 `m_EditorClassIdentifier` 精准切换为 `Shenxiao.Module.Core.Boss.Views.BossDomain` 业务类；Generated 仍只读。

## 静态确定接入

- 主页加载 `BossConfigs` 后请求 47101 主信息与 47105 取关列表；按 `boss_id` 排序克隆列表项，选择态、人数/刷新态、次数/协助次数读取现有模型。
- 列表人数容量直接来自 `Assets/GameRes/resource/config/server/config_decoration_boss.json` 的 `role_num`；复活剩余时间使用 47101 `reborn_time` 与 `TimeUtil.NowSec()`，没有猜名称、等级、头像或条件文案。
- 关注入口接 47106，购买入口接 47104，掉落日志请求接 47108；协助确认切本页模式，进入 CTA 按普通/协助剩余次数映射 47102 `type=1/2`。等级、九霄冥饰评分、星座套装与高阶 Boss 提示仍是跨模块 blocker，未做真实账号点击验收。
- 场景页只读请求 47114/47109，47111 仙宗召援入口接线，47113 结算事件打开专属结果页；退出未绕过老端共享 Alert 直接发 47103。
- 双倍卡、排名/仙宗协助、奖励格、场景寻路/自动战斗均保留 Prefab 身份并显式登记跨模块 blocker，没有复制共享实现。

## 完整叶与高风险条件

- 主页叶：Boss 列表/选择、关注、协助 Alert 四个按钮/checkbox、购买弹窗四叶、掉落记录、阶段奖励两叶、挑战、模型/评分/条件、Instruction、返回。
- 场景叶：退出确认、47111 仙宗召援、双倍卡三叶、归属排名/说明/仙宗协助三叶、攻击目标、场景即时状态、结算普通/双倍/关闭三叶。
- 写风险：47102 进入、47103 退出、47104 购买、47106 关注、47111 召援、背包双倍卡、Guild 40401/40403、场景战斗均未做账号运行事务。
- 真实收口仍需：同账号老端/Unity Web 两档 viewport 顺序复走、滚动末项、cold/warm、倒计时、关注/购买即时刷新与重开、场景进入/退出/协助/双倍/归属/结算、模型和特效双时点像素。

## 建议独占文件

- `Assets/Scripts/Module/Core/Boss/Views/BossDomain/**`
- `Assets/Prefabs/UI/Bossdomain/BossdomainModule.prefab`
- `output/ui_route_audit/2026-08-09_boss_ui_static_island/boss-domain/**`
