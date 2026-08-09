# Compete 338 静态三方调和审计

- 记录时间：2026-08-09（Asia/Shanghai）
- Git HEAD：`92b3f5578a90befb85a6255157f08e482214aa1a`
- 范围：`Assets/Scripts/Module/Core/Compete`、`Assets/Prefabs/UI/Competelist/CompetelistModule.prefab`、`Assets/GameRes/resource/game/competelist` 与本路线 output。
- 起始 dirty：上述生产目标均无 `git status --short` 输出；未发现归属不明的同文件并发改动。
- 运行限制：按本轮指令未启动 Unity、浏览器、Computer Use，未执行账号/GM/挑战/抽取/兑换/购买/领取事务。

## 来源与指纹

| 来源 | SHA-256 |
|---|---|
| `E:/GitProject/yu_client/h5/src/competelist/CompetelistView.ts` | `9533dd82701a42ef6e338c2facb619575424d2c6bf5c87eb0c725883a2646856` |
| `E:/GitProject/yu_client/h5/src/competelist/CompetelistRewardView.ts` | `0d965875e0f197b21b1e5d974dc8ec8d9b5810ecf54950ed3b65d0426896fdbf` |
| `E:/GitProject/yu_client/h5/src/commonController/CompeteListController.ts` | `bf67bda127d1e9826f1043ff81c2b934963a51f699ed20448c228e1a98e77477` |
| `E:/GitProject/yu_client/h5/src/commonModel/CompeteListModel.ts` | `cbd43387908d148d7e384d1d0e330b3a2497659d5f8c99e7e45f7dff9289bd1f` |
| `E:/GitProject/yu_server/src/race_act/pp_race_act.erl` | `76a9d0747ad0d0076e55ca903ce830bfc86d46692e797c6a3ff98b0b4c588957` |
| `E:/GitProject/yu_server/src/pt/pt_338.erl` | `22c039f5e4f907e5224313e22c05bc6dba57a0cb0f00067296042feeba20664f` |
| `Assets/Scripts/Module/Core/Compete/CompeteController.cs` | `f59e2c5ec211c56e84f7c3df59a0818b1cf17e2119ac8079b33dfe37b989cbc6` |
| `Assets/Scripts/Module/Core/Compete/CompeteModel.cs` | `76a1835213ffa78dde5186870956c3c36f426944f145e64e39f4f17fd722e66b` |
| `Assets/Prefabs/UI/Competelist/CompetelistModule.prefab` | `44e7fe011d7f41469f33fe4a0f7b97848f006d2a3aca0da54933afa6fbed2388` |
| Compete 专属资源树（76 文件） | `a9446a8759b5ce975ba8b7743884dd7e7ae01c498d8a4e7238e0a48361991bc5` |

老端配置闭包共六份：`config_race_act_info` 104 项、`stage_reward` 1542 项、`rank_reward` 2184 项、`reward` 3287 项、`ClientConfigRaceActRewardShow` 112 项、`ClientCompetelistSkill` 29 项。对应 SHA-256 依次为：

- `597dc04ecca85bb7995658ce10ba6912508006296662194fcdeb56931d211cf8`
- `0cd7a851ae6baae74d464c66a7cc953a3309d645e819d83d8d224552449eb246`
- `7fa1d60c9152b98e74c9d5dfad00f5bb7236163deee574f102eb8ad060b287b2`
- `5b2f58e67ed50bbf5168fe9c440cd22fc4656a470b6fb8024b4c8c3033989a8d`
- `1302294727b36bb72ce17dc926cc01f9b406a84cb35baace05d0b893a5ce2c09`
- `cbac4528b475e96a71a18f6bb16a300162bd16d59d235be687a7cdaa46bf729b`

## 老端控件、状态与返回链

1. 入口按 `33800` 活动的 `type/subtype` 动态生成页签；每个页签有选中/未选中和红点状态，旧端最低等级条件为 150。
2. 主页面按活动类型切换标题图，按 `buy_end_time` 显示倒计时或“已结束”。`ClientConfigRaceActRewardShow.icon_type` 决定静态图片或模型；技能区由 `config_race_act_info.others.skill` 与 `ClientCompetelistSkill` 联合赋值。
3. 排名区显示本人的排名/积分和纵向排名列表；行状态含前三名图标或数字名次、名称、积分、奖励横列，奖励格可进入物品详情并返回原排名页。
4. 抽取区含一次、十次、折扣和成本状态。两按钮都通过 `33803` 写事务；余额不足可能进入充值/兑换确认链。
5. 阶段奖励区含今日积分、进度、横向阶段列表。`got_type=1/2/3` 分别对应可领取/已领取/未达成；未达成点击仅预览，可领取点击通过 `33804` 写事务，成功后要求奖励结果、红点、父页即时刷新与关闭重开一致。
6. `_img_all` 打开 Activity 层的 `CompetelistRewardView`；弹窗有关闭按钮/遮罩、奖励列表或网格、逐格物品详情与返回链。
7. 页面本身没有“匹配”“挑战”“购买次数”控件；与本页相关的写入口仅抽取、余额不足确认/兑换、阶段奖励领取。没有发现页面专属主动音效调用，只有通用点击声链可能适用。

## 协议调和

| 协议 | 当前服务端语义 | 老端页面 | Unity 当前 |
|---|---|---|---|
| 33800 | 活动列表 | 进入/等级变化请求，生成入口与页签 | 已注册并解析，更新主界面活动图标；本轮未运行 |
| 33801 | `type/subtype` 页面信息：开放、积分、今日积分、一次/十次成本、奖励、阶段、世界等级 | 页面数据与阶段状态 | 已注册并保留 raw 快照；没有 Compete 业务 View 消费 |
| 33802 | `type/subtype` 排名信息：积分、排名、排名列表 | 排名列表 | 已注册并保留 raw 快照；没有 Compete 业务 View 消费 |
| 33803 | `type/subtype/times` 抽取，回包含错误码与奖励 | 一次/十次，成功后刷新 33801/33802 | CompeteController 未注册；注释称由别的活动控制器持有，未发现本 Prefab 消费链；本轮禁止事务 |
| 33804 | `type/subtype/reward_id` 阶段领奖 | 可领取项点击，成功后刷新 | Unity Compete 未注册；本轮禁止领取 |
| 33805 | 许愿界面信息 | 当前竞榜主页面无对应控件 | codec 存在，当前服务端 handler 注释，不可达 |
| 33806 | 许愿抽取 | 当前竞榜主页面无对应控件 | codec 存在，当前服务端 handler 注释，不可达且属写事务 |
| 33807 | 许愿钥匙/充值信息 | 老模型曾保留链 | codec 存在，当前服务端 handler 注释，不可达 |

## Unity 静态现状

- `CompetelistModule.prefab` 已存在，且绑定五个 Generated Bind：主视图、总奖励弹窗、阶段项、排名项、奖励项。五份 Bind 源码 SHA-256 分别为 `9182e9...`、`ead3bd...`、`326bd2...`、`099ca0...`、`9ae715...`。
- Prefab 中只发现 Generated Bind 与标准 uGUI 组件，没有 `Module.Core.Compete` 业务 View/Flow。`Assets/Scripts/Module/Core/Compete` 也只有 Controller/Model；未发现 338 活动键对应的 `MainUIRouter.Register`。
- Prefab 有 4 个 `ScrollRect`、4 个 `RectMask2D`、7 个 `HorizontalLayoutGroup`，但 `VerticalLayoutGroup=0`、`GridLayoutGroup=0`、`ContentSizeFitter=0`。因此排名纵向动态高度、总奖励网格/纵向增长不能由当前 Prefab 结构静态证明；真实拖动、裁切、末项可达仍是 NVR。
- Unity 当前没有上述六份 race-act JSON。`configpreloadreslist.json` 虽列出四份 server 配置路径，但仓库相应文件不存在；两份 client 配置也不存在。标题/模型或图片/技能/成本/阶段/排名奖励均不能在允许文件岛内凭猜测恢复。
- `Assets/GameRes/resource/config/client/configcustomactivityshow.json` 和 `uimodelparameter.json` 含 `CompetelistView` 规则，但模型呈现、共享物品详情、Activity 弹层和 BaseWindow 返回链属于跨模块依赖，本轮只登记，不抢共享文件。

## 修复决策

已有 Prefab，因此按 fix-view 评估增量修复；本轮没有执行生产修改。理由：入口路由、业务 View、六份配置闭包、共享物品组件和模型呈现链必须同时成立，单独给 Prefab 加点击或写一个残缺 Flow 会把不可用页面暴露给玩家；在禁止修改 MainUI/Generated/ClientConfigSync/Addressables/Common 且禁止运行验证的条件下，没有静态可证的最小闭包。布局坐标和动态列表也不能脱离老端真实矩形与拖动证据猜改。

正式账采用 `revision-v3`：49 节点、40 叶。初版遗漏动态页签，v2 补页签后又发现 33805/33806 需显式登记；依据拓扑不可变规则保留旧账并新建修正版，没有改写旧账制造证据。

## 完成边界

- 已完成：源码/配置/Prefab/Bind/服务端协议静态枚举，控件树、协议族、组件依赖、风险和 blocker 建账。
- 未完成且不得伪 done：老端真实运行、Unity Web、720x1280 与 1920x1080、逐像素 old/unity/overlay/diff、滚动三态、模型/特效双帧、350ms/1000ms/ready、cold/warm、事务即时刷新与关闭重开。
- 本轮不触发生产文档更新：只新增路线专属审计输出，未调整架构、公共组件、协议实现、构建发布方式或生产代码。
