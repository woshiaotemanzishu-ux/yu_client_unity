# GuBao / MonsterMainView 静态资源闭包审计

## 结论

- 本路线核对的是老端真实 `guBao/MonsterMainView`，它属于 `AutoBrushBaseView` 的 `eSoap` 子页，并由任务 `TaskTipType.ActiveSoap=89` 进入；它与底部 `treasure/SecretTreasureMainView` 不是同一路线。
- 老端页面不是固定两行列表。它由 26 个古宝配置、102 个碎片配置驱动，当前可展示集合按激活进度扩展；首个古宝为 2 个碎片，后续为 4 个碎片。
- Unity 当前只有 `GuBaoShellView(TempShell)`：运行时代码建树、硬编码 `SOAP_ID=10001` 和 `DEBRIS_COUNT=2`，只显示碎片名、激活态和激活按钮。仓库没有 GuBao/MonsterMainView 自属 Prefab。
- 因为没有可编辑 Prefab，本来应进入首次转换边界判断；但完整落地闭包跨越被禁止的 `AutoBrush` 父容器、`Task/TaskTips` 引导、共享 `BaseAwardItem`、模型链、`OutwardChangedView`、GoodsModel、配置与资源域。依照本轮约束，本路线只登记逐叶 `blocked`，不转换、不改代码。

## 三方调和

| 维度 | 老端运行/源码配置事实 | Unity 现状 | 判定 |
|---|---|---|---|
| 父路由 | `AutoBrushBaseView` 两页签：`AutoBrushMainView`、`MonsterMainView` | 任务 89 直接 `GuBaoShellView.Show()` | 父容器和页签语义缺失，依赖 AutoBrush/Task，越界 |
| 数据集合 | 26 soap、102 debris，显示集合随前序激活扩展 | 固定 soap 10001、固定 2 debris | 仅覆盖首个任务壳，不是完整页面 |
| 主页面 | 名称、战力、模型、左右轮播、红点、总属性、2/4 碎片槽 | 560x480 代码壳与两行文本 | 结构、视觉、交互均不等价 |
| 碎片详情 | 选择后 200ms 滑入；成本物品/数量、属性、跳转、激活/已激活三态 | 行内按钮直接发 13321 | 详情页、成本与条件态缺失 |
| 属性弹窗 | Activity 层 `MonsterPropertyView`；滚动总属性、空态、背景关闭 | 无 | 缺失，且需要独立 Prefab/共享滚动链 |
| 模型与结果 | `[18,n,1]` 模型；全碎片激活后开 `OutwardChangedView` | 无 | 跨共享模型和外观弹窗域 |
| 协议 | 13320 全量；13321 成功后即时刷新战力、碎片、红点与结果弹窗 | 已注册 13320/13321，成功更新临时壳 | 协议底层部分存在，但未做真实事务/即时刷新/重开验证 |
| 引导 | Task 89、AutoBrush step、父页签/返回按钮/激活按钮指引 | Task 入口存在；另有命令行 autopilot 直发激活 | 共享 Task/AutoBrush 域，只读 blocker |

## 控件与状态枚举

正式 manifest 共覆盖以下直接控件和条件块：

- 父页签与父返回链；
- 查看属性、上一古宝、下一古宝及左右箭头红点；
- 古宝名称、激活/未激活图、动态可展示集合与默认选择；
- 2/4 碎片槽，每格的图标、关卡解锁文案、灰态、选中态、红点与点击；
- 总属性列表与碎片详情的互斥显隐和 200ms 动画；
- 碎片详情返回、共享成本物品格、拥有/需求文案、属性列表、前往和激活；
- 未完成古宝的前往按钮、战力、3D 模型、任务手指与全层红点；
- 13320/13321 成败、即时刷新、关闭重开，以及全碎片完成后的共享外观弹窗。

## 配置与资源最小闭包

| 类型 | 静态事实 | SHA-256 / 说明 |
|---|---|---|
| soap 配置 | `config_enchantment_guard_soap.json`，26 项 | `a1f1d4dad22f758fba68815ff11bb9ae9af4688790e67b2525480c82f2938f94` |
| debris 配置 | `config_enchantment_guard_soap_debris.json`，102 项 | `5e05c6379942d08078f8bbaaf2094a9a86d7714310b4c11e678ff30d1e26f6bd` |
| 掉落关卡配置 | `config_enchantment_guard_stage_reward.json`，102 项；碎片物品映射关卡 5..3200 | `5b9c409c332b0256d714b7dda5402a9ee67724bf915c19e2ae67c8662f0f1015` |
| 老端主场景 | 53 个具名/匿名节点、13 个静态 skin | `f42e0e55476f44a41aa9b197f1aef22717df5539543a5a56ceee0f03bb9b596a` |
| 老端碎片格 | 8 节点、4 个 skin | `f72d6ed9d838089572a0e84b472a476026becaccdc8112148a169e7e82eeaa0c` |
| 老端属性弹窗 | 8 节点、3 个 skin | `d30c708b564f2b1f748c320d3cb01a357b2a614fb5aef3ac4e60bd4ebecca77e` |
| 动态资源 | `ui_zy_05/09/10/11`、`com_line_91`、碎片 goodsIcon、模型 `[18,n,1]` | 分属共享图片/物品/模型加载链，不在 GuBao 自属资源闭包内 |

老端场景显式依赖 `common`、`common2`、`common4` 和 `monster` 图集；运行时还通过 `BaseAwardItem`、`GoodsModel`、`CustomActivityModel.ShowViewModel`、`OutwardChangedView` 和 `AutoBrushBaseView` 跨域。Unity 仓库 `git ls-files '*.prefab'` 对 `GuBao|MonsterMain|MonsterProperty` 无匹配，`Assets/GameRes/resource/game/monster` 也不存在可直接认领的 Unity 页面资源目录。因此无法证明一个仅属于 GuBao 的可编辑 Prefab/资源最小闭包。

## 源码证据

| 事实 | 文件 | SHA-256 |
|---|---|---|
| 老端页面控件、显隐、跳转、激活、模型与结果弹窗 | `E:/GitProject/yu_client/h5/src/guBao/MonsterMainView.ts` | `41da5c8ed2961b888a9d6e20fd5d82446650716a0b1709496ba2a6288371f031` |
| 老端动态集合、属性、关卡和红点规则 | `E:/GitProject/yu_client/h5/src/commonModel/MonsterModel.ts` | `e958e1efa5da095e326cd41ce44c646b925aa41150c9447b027dfb6dfbcc5673` |
| 老端碎片格状态 | `E:/GitProject/yu_client/h5/src/guBao/MonsterIconItem.ts` | `97de1dec2431f343f4eb22c491181022d24703a73575ed1f87b26a905f0f6291` |
| 老端总属性弹窗 | `E:/GitProject/yu_client/h5/src/guBao/MonsterPropertyView.ts` | `a2f9413862a6fa95db0ed184f2bbcfb921e970a013388642f3712c20ad2e4542` |
| 老端 13320/13321 接收与事件 | `E:/GitProject/yu_client/h5/src/commonController/MonsterController.ts` | `1fd9cd96163ff4775b8c07ed91850e480a95f0bfcf17c2ff1b1a43bffd23b708` |
| Unity 临时壳 | `Assets/Scripts/Module/Core/GuBao/GuBaoShellView.cs` | `3a778f6231869cc528f772325e61e986008ffa85da4d51241c55df3d95d24656` |
| Unity 数据/配置读取 | `Assets/Scripts/Module/Core/GuBao/GuBaoModel.cs` | `9a6d00526593d2ea7b7368a227974fba46b74035fb34ef4d378fb0d818e18e63` |
| Unity 协议控制器 | `Assets/Scripts/Module/Core/GuBao/GuBaoController.cs` | `894226712f3aff3fcff60086e90e5564f67cf4d7d928d84d2c462db4206eea49` |
| Unity 任务入口，共享越界 | `Assets/Scripts/Module/Core/Task/TaskModel.cs` | `d3827fb0aa44c1a329ef09dfa82b1773431eae0cac5dc03f42904f5fbf6bd893` |
| Unity 命令行任务 autopilot，共享越界 | `Assets/Scripts/Module/Core/Task/TaskSystemAutoPilot.cs` | `0839817faefdebe9705ed3765a15ded09377d8fe68101f2943102aa736454899` |

## Blocker

1. **Prefab blocker**：GuBao/MonsterMainView 没有可编辑 Prefab，现有代码壳不能作为人工视觉事实源。
2. **闭包 blocker**：首次落地必须同时处理 AutoBrush 父容器、共享物品格、模型、结果弹窗、任务引导、共享配置与资源；本轮明确禁止修改这些域，无法建立无重叠最小闭包。
3. **真实运行 blocker**：本轮禁止 Unity、浏览器、构建、真实账号和 GM，故两 viewport、像素、滚动、模型双帧、cold/warm、即时刷新、关闭重开均为 NVR/blocked。
4. **写事务 blocker**：13321 会消耗碎片并持久化账号状态；本轮禁止真实写事务，不能验证成功/失败回包、即时 UI 刷新和重开一致性。
5. **共享 Task blocker**：任务 89 的入口、手指步骤和命令行 autopilot 属于 `Task/TaskTips` 域；只读记录，不覆盖、不改动。

## 本轮改动边界

- 仅新增本目录的 manifest、ledger、results、静态调和报告及聚合 master。
- 未修改 C#、Prefab、资源、配置、Addressables、Generated、Docs、AutoBrush、Task/TaskTips 或共享路由。
- 未运行 build、Unity、浏览器、GM 或真实账号操作。
- 这是独立新拓扑账，不沿用或抬升任何旧账；SecretTreasure v1 冻结拓扑保持不变。
