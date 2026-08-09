# 背包主路线静态审计（2026-08-09）

## 结论

- 已按 schema 6 建立 `mainui.bag.window` 的完整控件拓扑，共 107 个节点；覆盖背包、仓库、圣印、天启、龙语五个页签，以及熔炼、熔炼属性、扩容、一键使用等子窗。
- 本轮只完成源码、配置、Prefab、Generated Bind、Flow 和协议的静态调和；没有启动 Unity/浏览器，没有操作账号，也没有执行任何真实写事务。因此所有玩家可见/可点击叶子保持 `needs-runtime-verify`，缺实现、跨模块或写事务叶子明确为 `blocked`。
- Bag 私有且静态确定的两个缺陷已修复：熔炼属性按钮不再只弹 Toast，而是打开现有 `SmeltPropView` 并展示 `config_fusion_level.attr_list`；嵌套 Activity 弹窗关闭后，遮罩会重新绑定仍打开的下层 Popup，避免点击穿透背包 Window。

## 权威来源与当前实现

老端主要来源：

- `E:/GitProject/yu_client/h5/src/bag/BagView.ts`
- `E:/GitProject/yu_client/h5/src/bag/BagComponentView.ts`
- `E:/GitProject/yu_client/h5/src/bag/WarehouseView.ts`
- `E:/GitProject/yu_client/h5/src/bag/BagSmeltView.ts`
- `E:/GitProject/yu_client/h5/src/bag/SmeltPropView.ts`
- `E:/GitProject/yu_client/h5/src/bag/ExpandBagView.ts`
- `E:/GitProject/yu_client/h5/src/bag/OneKeyUseView.ts`
- `E:/GitProject/yu_client/h5/src/bag/SelectGiftView.ts`
- `E:/GitProject/yu_client/h5/src/common/ItemUseView.ts`
- `E:/GitProject/yu_client/h5/src/holySeal/HolySealView.ts`
- `E:/GitProject/yu_client/h5/src/revelation/RevelationEquipView.ts`
- `E:/GitProject/yu_client/h5/src/longlanguage/longlanguageView.ts`

Unity 主要来源：

- `Assets/Prefabs/UI/Bag/BagModule.prefab`
- `Assets/Prefabs/UI/Bag/BagEquipmentIcon.prefab`
- `Assets/Scripts/Generated/UI/Bag/**`
- `Assets/Scripts/Module/Core/Bag/BagFlow.cs`
- `Assets/Scripts/Module/Core/Bag/BagController.cs`
- `Assets/Scripts/Module/Core/Bag/BagModel.cs`
- `Assets/Scripts/Module/Core/Bag/ItemUseFlow.cs`
- `Assets/Scripts/Module/Core/Bag/Views/**`

## 页面与控件闭包

| 路线 | 老端控件闭包 | Unity 当前边界 |
|---|---|---|
| 背包 | 角色名/战力/模型/装备槽、背包滚动格、详情/装备对比、一键装备、熔炼、共鸣、扩容、一键使用、守护 1/2、龙珠、红点 | 主背包、熔炼、扩容、一键使用已有 Prefab/View；详情/装备比较属于 Common/Equipment 共享岛；共鸣/守护/龙珠不是 Bag 私有闭包 |
| 熔炼 | 关闭/背景关闭、列表滚动、物品选择、阶数/颜色筛选、自动/一星、熔炼提交、属性窗 | 属性窗与 Activity 遮罩已修；阶数/颜色下拉当前模板隐藏且无完整运行逻辑，登记 blocked；15025 未授权 |
| 扩容 | 关闭/背景/取消、减/加/最大、数量计算器、消耗详情、确认 | 数量步进已实现；CalculatorView 缺运行接线；消耗详情为共享弹窗；15002 未授权 |
| 一键使用 | 关闭/背景、列表、勾选、礼包/经验/其他分类、确认 | 分类与勾选已实现；确认逐项发 15050，未授权；SelectGiftView 仍缺 `config_optional_gift` Unity 配置闭包 |
| 仓库 | 双滚动格、双向双击移动、详情、扩背包/仓库、熔炼、一键使用、共鸣 | 双格/导航已有；详情跨 Common；15003 和扩仓库 15002 未授权 |
| 圣印 | 已装备槽、背包滚动、主/属性/提示/强化/预览/分解/合成/灵魂 | `BagFlow.TabEnabled[2] == false`，对应完整模块不在 Bag Prefab 私有闭包，整页 blocked |
| 天启 | 已装备槽、背包滚动、套装/来源/合成/吞噬/属性 | `BagFlow.TabEnabled[3] == false`，整页 blocked |
| 龙语 | 已装备槽、背包滚动、分页 1-4、商店、属性、主图标 | `BagFlow.TabEnabled[4] == false`，整页 blocked |

## 协议与事务边界

| 协议 | 语义 | 本轮状态 |
|---|---|---|
| 15010 / 15017 / 15018 | 背包、仓库及装备权威快照/增量 | 已静态核对注册与 Model 更新；缺真实账号运行态 |
| 15002 | 扩背包/仓库容量 | 破坏性账号写事务，blocked |
| 15003 | 背包与仓库移动 | 破坏性账号写事务，blocked |
| 15024 / 15025 | 熔炼信息 / 熔炼提交 | 15024 静态核对；15025 消耗物品，blocked |
| 15050 | 通用物品使用 | 一键使用逐项发送；会消耗物品，blocked |
| 15201 | 穿戴装备 | 仅允许玩家手动点击一键装备触发；未授权，blocked |

`ItemUseFlow` 已静态确认：藏宝图、时装、选择礼包等专用类型不会误走通用 15050；装备必须等待 `15010 pos=1` 权威装备快照并且评分严格更高才显示候选。登录与 `EVT_BAG_UPDATE` 不会自动触发 15201。

## 共享组件与跨岛登记

- `BaseAwardItem`：背包格、仓库格、一键使用格、熔炼格等共享消费者。本轮未修改，共享状态矩阵和消费者运行抽查待真实 Unity/Web。
- `EquipmentItem`、物品详情、装备对比：属于 Common/Equipment 共享实现；本轮禁止触碰，仅在路线叶登记 blocker。
- `BaseWindowSkin`：共享大窗外壳，本轮未修改。
- `BagActivityBlocker`：BagModule 内私有 Activity 遮罩；Prefab 中已有 `com_sub_bg_7` 与 Popup 层。本轮只修复嵌套 Popup 关闭后的回退绑定，未改 Prefab。

## 静态修复

1. `BagFusionConfigs.GetLevelAttrValues` 解析 `config_fusion_level.attr_list`，保留坏配置警告和空态回退。
2. `BagSmeltView.ShowProperties` 载入熔炼/物品配置，按物品属性名与格式构造累计属性，并打开现有 `SmeltPropView`。
3. `SmeltPropView` 明确使用 Popup 层，消费只读 Presentation，并在关闭时通知 Bag Activity 遮罩。
4. `BagFlow.NotifyActivitySubHidden` 会选择同模块仍显示、层级最高的 Popup 重新绑定遮罩；无剩余 Popup 时才隐藏。
5. 主控交叉审查补齐属性配置异步返回的 show/hide/reopen 失效保护，并恢复老端/Prefab 固定标题“属性加成”与旧端加号空格语义，避免陈旧回调跨生命周期误开子窗。

## 验证分层

- `python -m json.tool route-manifest.json`：通过。
- `route_ledger.py init`：通过，schema 6 / 107 nodes。
- `git diff --check -- Assets/Scripts/Module/Core/Bag`：通过。
- 当前 Framework 源码临时编译后，使用对应临时引用编译完整 `Shenxiao.Module.Core`：通过；仅有既存 `ActivityIcon.cs` 的 TMP `enableWordWrapping` 过时警告。
- 直接复用旧 Bee Framework 引用时，因并发中的 Medal 源码已新增协议常量而出现陈旧引用假失败；重新编译当前 Framework 后消失，不是 Bag 产品缺陷。
- 未运行 `BagInteractionCase`：该用例需要 Unity，违反本轮“不得启动 Unity”的范围。

## 后续运行闸与阻塞

- 运行闸：真实 Prefab `GraphicRaycaster -> PointerClick`、滚动裁切/末项、弹窗层级、嵌套遮罩恢复、角色模型出帧、红点、两档 viewport、关闭重开，以及同账号老 H5 / Unity Web 顺序复走。
- 写闸：15002、15003、15025、15050、15201 必须得到写事务授权，并验证成功/失败回包、父页即时刷新和关闭重开；不得用 GM 直接完成目标事务。
- 实现阻塞：熔炼阶数/颜色筛选、Expand 数量计算器、SelectGift 配置闭包，以及圣印/天启/龙语完整模块。
- 跨岛阻塞：物品详情、装备比较、共鸣、守护、龙珠和共享组件消费者抽查。

本轮没有更新 Docs：父任务明确限定独占写入 Bag 私有代码/Prefab/Generated 与本路线 output，禁止修改 Docs。
