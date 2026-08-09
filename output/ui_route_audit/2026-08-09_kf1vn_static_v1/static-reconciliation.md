# Kf1vn UI 静态三方调和（schema 6 建账批次）

## 本轮边界

- 路线：`mainui.kf1vn`（老端“诸天王者 / 跨服 1vN”，协议族 621）。
- 本轮明确禁止启动或操作 Unity、浏览器、Computer Use，也禁止真实报名、进入、竞猜、观战、领奖、购买、GM 或账号写事务。因此本报告只有静态事实；真实 Web、双 viewport、像素、滚动、特效/模型双帧、声音、cold/warm、即时刷新、关闭重开均未执行，不能标 `done`。
- 开始时 `Assets/Scripts/Module/Core/Kf1vn`、`Assets/Prefabs/UI/Kf1vn`、`Assets/GameRes/resource/game/kf1vn` 无已跟踪或未跟踪 dirty；本轮不写 Generated、Proto、Common、MainUI、Goods/Shop、Dsgt、场景、Addressables、Docs 或项目文件。
- Git HEAD：`92b3f5578a90befb85a6255157f08e482214aa1a`。

## 老端源码 / 配置事实

老端模块包含 26 个 `h5/src/kf1vn/*.ts`（另有 `commonController/Kf1vnController.ts` 与 `commonModel/Kf1vnModel.ts`），页面/条目集合为：

`Kf1vnEnterView`、`Kf1vnShopView`、`Kf1vnShopExchangeView/Item`、`Kf1vnQuizHistoryView/Item`、`Kf1vnRankView/Item`、`Kf1vnRewardView`、`Kf1vnTicketRewardItem`、`Kf1vnPlatformRewardItem`、`Kf1vnWaitSceneView`、`Kf1vnPlatformPlayingView/Item`、`Kf1vnQuizSelectView/Item/RadioItem`、`Kf1vnChallengerDetailView/MsgItem`、`Kf1vnFightSceneView/RoleHpItem`、`Kf1vnMatchSuccessView`、`Kf1vnPlatformMatchSuccessView`、`Kf1vnFightResultView`、`Kf1vnSettlementView`、`Kf1vnTabItem`。

静态调用链确认的玩家控件/状态：

- 入口：商店、排行、奖励、报名/进入；活动阶段 0～6、报名/进行倒计时、规则滚动、历届王者展示、报名/商店红点。
- 商店：兑换/竞猜记录双页签；兑换货币、纵向商品列表、物品详情、等级/限购/售罄、购买；竞猜记录列表、奖励/成本详情、可领/已领和领取红点。
- 排行：资格赛/擂主赛双页签，榜单/空态/本人信息、关闭。
- 奖励：资格积分/擂主挑战双页签，奖励列表、称号资源/特效、物品详情、关闭。
- 等待场景：阶段/轮次/倒计时/战绩、排行（`rankBtn` 与 `lookBtn` 两个入口）、商店、竞猜记录、退出、红点和返回链。
- 擂主赛：三段进度、对阵列表；每场挑战者详情、竞猜、观战；排行、商店、竞猜记录、退出；阶段/场次/下注/观战条件显隐。
- 竞猜确认：挑战者选择、下注档位选择、成本/奖励预览、确认与关闭。
- 战斗/结果：资格赛与擂主赛 HUD、血条列表、倒计时、观战身份、退出确认、匹配演出、结算双样式、奖励滚动、结果音与自动关闭/退出。

读取配置至少包括 `ConfigInstruction`、`config_kf_1vn_time`、`config_kf_1vn_kv`、`config_kf_1vn_match`、`config_kf_1vn_bet`、`config_kf_1vn_bet_opt`、`config_kf_1vn_score_reward`、`config_kf_1vn_race_2_award`、`config_goods_exchange_rule`，并依赖 Goods、Shop、Dsgt、角色头像/模型、场景/自动战斗、Alert、声音和通用物品格。

老端真实运行态本轮未采集；禁止用上述源码/设计场景代替同账号真实运行表现。

## Unity Prefab / Bind / 代码事实

- `Assets/Prefabs/UI/Kf1vn/Kf1vnModule.prefab` 内含 25 个 Kf1vn 页面/条目根，全部挂 `Shenxiao.Generated.UI.Kf1vn.*Bind`；模块内同时保存 14 处 `_tpl_Kf1vn*` 模板引用。
- `Assets/Prefabs/UI/Kf1vn/Kf1vnTabItem.prefab` 是模块专属共享页签；`Assets/Scripts/Generated/UI/Kf1vn` 共 26 个 Bind。
- `Assets/Scripts/Module/Core/Kf1vn` 只有 `Kf1vnController.cs` 与 `Kf1vnModel.cs`，不存在 `Views/`、业务 View 子类、Flow 或 Bootstrap。全仓静态事实因此是“Prefab/Bind 骨架已存在，但玩家 UI 未由业务代码接管”。
- 现有 Controller/Model 已保存 18 个安全读/推送号：62100/01/03/04/05/08/09/10/12/13/16/17/19/20/23/32/33/35；启动顺序保持 `62101→62133`，62107 仅 C2S send-only。
- 62102（持久报名）、62118（真实扣费竞猜）、62121（切 ghost 观战场景且无成功 ACK）、62134（持久领奖并由背包/邮件发奖）仍是 hard-negative；本轮不能新增 sender/handler 或用本地乐观状态伪闭环。
- Kf1vn 专属资源名对比：老端 `resource/game/kf1vn` 的 96 个图像/图集入口对应 Unity 116 个展开资源；老端缺少同名直拷的 3 项仅为组合 `texture.png/.ktx/.atlas`，Unity 使用展开 PNG + `kf1vn_texture.spriteatlas`。这只证明静态文件名闭包，不能证明 Addressables、运行时加载、像素或幂等门禁。

关键文件 SHA-256：

- `Kf1vnController.cs`：`aa8175a238f53ffae79df88973cdcd17f10e5e34258774ba4c2007736100bbb1`
- `Kf1vnModel.cs`：`08604ba608e78807bd1e668eaeec5df938fbf92bebb06081e8ec4465b906f321`
- `Kf1vnModule.prefab`：`5b1581f7bdbfa5d4173f8fc421ead77c994474e77a43e040a720f9c04dcd4047`
- `Kf1vnTabItem.prefab`：`b21ec69753987fe156f74c4bf8e0c015ccc45d5c5ff593a64353d174de0b8d5d`

## 组件依赖清单

| 组件 | 直接消费者 / 形态 | 本轮结论 |
|---|---|---|
| `Kf1vnTabItem.prefab` | Shop、Rank、Reward；选中/未选中、红点有/无、短/长标题 | Kf1vn 专属共享组件，可读；无运行矩阵证据，NVR |
| `Kf1vnRankItem` | 资格榜/擂主榜；前 3/普通名次、空/有数据 | 模块内模板；无真实滚动/点击证据，NVR |
| `Kf1vnPlatformPlayingItem` | 等待/进行/结束；详情/竞猜/观战/已下注 | 模块内模板；62118/62121 分支 blocked |
| `Kf1vnQuizHistoryItem` | 未结算/可领/已领，成本与奖励 | 模块内模板；62134 领取 blocked |
| `BaseAwardItem` | 商店、历史、竞猜、奖励、结算等 8 类宿主 | Common 共享组件，严格禁写；只登记依赖 |
| `CustomHeadItem` | 入口王者、排行/详情、匹配/结果等 | Common 共享组件，严格禁写；只登记依赖 |
| Scene/AutoFight/RoleHp | FightScene、MatchSuccess、PlatformMatchSuccess | 场景/战斗跨模块依赖；本轮禁区，blocked |
| Goods/Shop/Dsgt | 兑换、成本、奖励、称号特效 | 跨模块/资产事务依赖；本轮禁区，blocked |

## 静态结论与未修复原因

静态确定缺陷是 Kf1vn 业务 View/Flow/Bootstrap 整层缺失，而不是某一个 Prefab 坐标或绑定字段错误。恢复这层会同时触达 4 个 hard-negative 事务、Common 物品格/头像、Goods/Shop/Dsgt、MainUI、场景/自动战斗与声音；这些都超出本轮唯一文件岛和禁区。为避免把孤立骨架误写成“UI 已接”，本轮没有对生产代码或 Prefab 做局部伪修复，只建立完整 schema 6 拓扑与精确 blocker。

后续可执行顺序：先取得允许的真实老端运行基线和两端 Web 路线；再在保持 4 个事务 hard-negative 的前提下，先接只读入口/排行/奖励壳与 Kf1vn 专属组件矩阵；场景/商店/写事务分别取得跨模块授权后成组恢复。任何阶段都必须回原台账逐叶复验。
