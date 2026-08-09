# GuBao / 底部秘宝静态三方调和

## 结论

- 老端底部按钮 `res="treasure"` 对应 `MainFunc.Treasure`，点击后打开 `SecretTreasureMainView`。
- `SecretTreasureMainView` 固定五页签：九霄劫魄、山海物华阁、苍龙神章、荒祖遗骸、九霄冥饰。
- 老端 `GuBao` 的真实页面是 `guBao/MonsterMainView`，挂在 `AutoBrushBaseView` 内，并由任务 `tips=89` 进入；它不是底部 `treasure`。
- Unity 当前 `RuneBootstrap` 把 `treasure` 注册为 `RuneFlow.Toggle`，而 `RuneFlow` 明说共享 `SecretTreasureMainView` 尚未生成，因此只打开默认首页 `RuneMainUIView`。这不是与老端等价的秘宝容器。
- 所需实现文件属于 Rune/MonBook/Lung/GodBeast/Unreal 以及共享秘宝容器/路由闭包，不属于本条允许的 `Assets/Scripts/Module/Core/GuBao/` 文件岛；本轮不得跨界修改。
- `GuBaoShellView` 自身是运行时代码建树的 `TempShell`，没有可编辑 Prefab。即使另开“古宝”路线，也应先按 convert-module 边界建立资源闭包，不能把这个临时壳当成已精修 Prefab。

## 证据索引

| 事实 | 文件与位置 | SHA-256 |
|---|---|---|
| HUD treasure 图标定义、点击打开 SecretTreasureMainView | `E:/GitProject/yu_client/h5/src/commonModel/MainUIModel.ts:124`、`:301` | `b6f3ff4ec0986301d61fc5ae342dcdab0dad4f64300465645efc5cd1090c530f` |
| 五页签、五 View、标题/背景/红点 | `E:/GitProject/yu_client/h5/src/rune/SecretTreasureMainView.ts:25` | `3edf22cac84153aae634f9e749da04facf7377a1274882651a1f4d67273aca52` |
| 九霄劫魄控件和 16702/16704 等语义 | `E:/GitProject/yu_client/h5/src/rune/RuneMainUIView.ts:362` | `b85f17832d974cdd5a3620dccffd52696fcb706669fe670ef9add2e6e69549bc` |
| 山海物华阁控件和 44202/44203/44205 | `E:/GitProject/yu_client/h5/src/monBook/MonBookMainView.ts:232` | `bd12683dfbf31f540099d85f18ecb8f301017866d22439d76d5c1787f4dea33d` |
| 苍龙神章控件和 18100/18101/18103 | `E:/GitProject/yu_client/h5/src/lung/LungMainView.ts:316` | `664f4c726ff5373ab6e778b2975c4ad50a29fff142d8516fea76bb4babfb6ca7` |
| 荒祖遗骸控件、确认框和 17304/17305 | `E:/GitProject/yu_client/h5/src/godBeast/GodBeastView.ts:123` | `0030cffaa5b30bc140fd93cdc6aa2b609bea32408322b999f54ec27a90987ea4` |
| 九霄冥饰控件、筛选、滚动与子页 | `E:/GitProject/yu_client/h5/src/unreal/UnrealBagView.ts:128` | `30735e0d914b90f87e74310dc56f6774e38ab83c9fcbad0cf97958a312ff5f4a` |
| 古宝页面属于 MonsterMainView/AutoBrush | `E:/GitProject/yu_client/h5/src/guBao/MonsterMainView.ts:137` | `41da5c8ed2961b888a9d6e20fd5d82446650716a0b1709496ba2a6288371f031` |
| Unity treasure 当前被 RuneBootstrap 注册 | `Assets/Scripts/Module/Core/Rune/RuneBootstrap.cs:9` | `e34da18664fb7cc5ff950fe4dac3ee89769705032047959d24e2aadc3b3ea809` |
| Unity 明确降级为只开首页 | `Assets/Scripts/Module/Core/Rune/RuneFlow.cs:11` | `6fa53c27240bbbdf952ca811cde61e2a23b7bf81cd6b86bc7a4a088efc67ad9c` |
| Unity GuBao 是无 Prefab 的 TempShell | `Assets/Scripts/Module/Core/GuBao/GuBaoShellView.cs:16` | `3a778f6231869cc528f772325e61e986008ffa85da4d51241c55df3d95d24656` |

## Unity 侧资源/代码现状

| 路线页 | 当前 Unity 现状 | 文件岛判定 |
|---|---|---|
| 秘宝共享容器 | 无 `SecretTreasureMainView` Prefab/View | 需要共享/多模块实现，越界 |
| 九霄劫魄 | `RuneModule.prefab` + `RuneMainUIView`，但大量模板隐藏、操作仅日志降级 | Rune 岛，越界 |
| 山海物华阁 | 仅 `MonBookController/Model`，无页面 Prefab/View | MonBook 岛，越界 |
| 苍龙神章 | 仅 `LungController/Model`，无页面 Prefab/View | Lung 岛，越界 |
| 荒祖遗骸 | 有 `GodBeastModule.prefab`，但 Core 下仅 Controller/Model，无业务 View | GodBeast 岛，越界 |
| 九霄冥饰 | 仅 `UnrealController/Model/Configs`，无页面 Prefab/View；`UnrealConfigs.cs(.meta)` 开始时已 untracked dirty | Unreal 岛且存在并发归属风险，越界 |
| 古宝 | `GuBaoShellView` 运行时代码建树，无 Prefab；仅任务/挂机子页入口 | 与底部秘宝不是同一路线 |

## 控件与状态覆盖

`route-manifest.json` 共列出根入口、5 个页面、全局返回链及各页直接控件。静态清单覆盖：

- 5 个一级页签与各页标题/背景/红点状态；
- 符文 10 槽、查看、分解、挑战、两种寻宝、合成、转化、镶嵌、替换、获取、升级、觉醒、技能；
- 图鉴分类/卡片列表、组合、属性、激活/升级、分解、礼包；
- 龙纹槽位、背包、熔炉、详情滚动、卸下、替换、升级；
- 遗骸列表、助战位、拆装、背包、召回、出战、快速穿戴、合成/强化、模型与特效；
- 冥饰装备槽、背包滚动、部位/颜色筛选、强化、获取、合成、分解、礼包与空态。

由于本轮禁止老端/Unity 真实运行，动态配置产生的实际页签数量、列表项数量、每个列表格弹窗身份、条件显隐集合仍不能声明为运行态完整；台账因此全部保持 `blocked`，没有任何 `done`。

## Blocker

1. **身份/文件岛 blocker**：请求名称中的 GuBao 与底部 treasure 不是同一功能；真正的秘宝实现需修改禁止触碰的 Rune/MonBook/Lung/GodBeast/Unreal/共享容器文件。
2. **Prefab blocker**：共享容器缺失；MonBook/Lung/Unreal 无可编辑页面 Prefab，GodBeast 缺业务 View；本轮资源闭包和共享依赖不清，禁止大转换。
3. **初始 dirty blocker**：`Assets/Scripts/Module/Core/Unreal/UnrealConfigs.cs(.meta)` 在本路线开始时已是 untracked，归属不能证明不重叠。
4. **运行门禁 blocker**：用户本轮禁止 Unity、浏览器、真实账号、GM、构建，因此无法完成两 viewport、真实点击/滚动、协议、即时刷新、关闭重开、cold/warm、模型/特效双帧与资源幂等。
5. **写事务授权 blocker**：升级、激活、卸下、召回、出战、快速穿戴等会消耗或持久化账号状态，本轮明确禁止真实写事务。

## 本轮改动边界

- 只新增本目录的 manifest、ledger、results 与静态调和报告。
- 未修改任何 C#、Prefab、资源、Addressables、Generated、Docs、Router/Flow/Configs。
- 未运行 build、Unity、浏览器、GM 或真实账号操作。
- 该轮只沉淀静态阻塞账，不触发仓库 Docs 更新；用户同时明确禁止修改 Docs。
