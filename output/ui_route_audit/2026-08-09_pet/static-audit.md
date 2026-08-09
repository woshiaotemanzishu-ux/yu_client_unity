# Pet 顶层路线静态审计（2026-08-09）

## 结论

- 已按老端 `MountPetView` 的当前事实建立 schema 6 完整路线，共 **151 个节点**。根路线为 `mainui.pet.window`，覆盖入口、关闭、四个顶层条件页签、关闭后的祝福弹窗，以及培养、等级、技能、水晶、幻化、侍魂装备、神巫出战/跟随/自动、勋印初启、天妖灵魄各条件叶。
- Unity 当前只能承载前两页：`御风云骑` 与 `剑魄同修` 共用 `PetModule.prefab/OutWardBaseView`，通过 `type_id=1/2` 切数据。`PetFlow.TabEnabled={true,true,false,false}` 正确地没有用前两页冒充老端第 3/4 页。
- 老端第 3 页是条件分支：通常为 `PartnerBaseView/神巫`；存在唤醒任务时切为 `PartnerAwakeMainView/勋印初启`，模块标题为“神巫劫境”。Unity 没有对应可达页面。
- 老端第 4 页为 `DemonMainView/天妖灵魄`。仓库虽有转换后的 `DemonMainView.prefab` 和只读协议模型，但没有可从 Pet 顶层进入的完整运行 View/Flow，且写协议仍有意不注册，故不能开放页签。
- 本轮禁止启动 Unity/浏览器且没有培养、穿戴、购买、出战等账号写授权：现有只读/导航叶最多为 `needs-runtime-verify`，写事务、未落 UI 与跨模块依赖全部为 `blocked`；根状态不得完成。

## 老端顶层事实

| 索引 | 当前标签 | 内容 View | 条件与关闭链 | Unity 静态结论 |
|---:|---|---|---|---|
| 0 | 御风云骑 | `HorseComponentView -> OutWardBaseView(type=1)` | `HorseComponentView` 功能开放；关闭时 `etime>0` 打开 `PetBlessView` | 顶层与培养基础 Prefab 已有；大量外窗未落 |
| 1 | 剑魄同修 | `PartnerComponentView -> OutWardBaseView(type=2)` | `PartnerComponentView` 功能开放；关闭时 `etime>0` 打开 `PetBlessView` | 与坐骑共用 Prefab；同样缺外窗 |
| 2 | 神巫 / 勋印初启 | `PartnerBaseView` 或 `PartnerAwakeMainView` | `HavePartnerAwakeTask()` 动态替换标签、标题、背景、开放条件 | 页签当前禁用；不得用 `OutWardBaseView` 代替 |
| 3 | 天妖灵魄 | `DemonMainView` | `DemonMainView` 功能开放 | 页签当前禁用；无完整可达运行页 |

根窗还包含返回按钮、三种货币栏、说明入口、页签红点与主线引导。关闭前两页时的 `PetBlessView` 是 Activity/弹窗身份，不是普通日志提示。

## 前两页 OutWard 控件闭包

`OutWardBaseView` 的完整老端叶包括：

- 3D 外观模型、战力、阶名/阶数、星级、祝福进度、前后外观切换、基础形象/幻化形象状态；
- 培养线：属性摘要、技能球、魔晶槽、培养材料、自动购买、一键提升；
- 等级线：等级/经验/战力、可选材料、一键升级/停止、等级技能列表/详情/升级；
- 属性弹窗；
- 幻化：形象列表、选择、模型、属性/来源、穿戴或取消、激活、升阶、升星、返回；
- 侍魂装备：已穿戴槽、背包、详情、穿戴、强化、打造、返回；
- 条件仙灵愿望入口、任务引导、红点与特效；
- 前两页关闭后的祝福保留弹窗。

当前 `PetModule.prefab` 直接保存 `OutWardBaseView`、模型容器、培养区、技能/魔晶/材料模板与装备入口，但未保存 `OutwardLvSystem`、`IllusionBaseView`、`PetProptityView`、`PetCrystalView`、`PetSkillView`、`PetBlessView` 的可运行 Prefab/View。只有 Generated Bind 不能证明页面可达。

## 协议与数据边界

| 功能族 | Unity 当前协议事实 | 是否足以支撑完整页面 |
|---|---|---|
| OutWard 160 | 已注册并解析 16000/16001/16002/16003/16004/16005/16006/16007/16008/16009/16010/16011/16012/16020/16022/16023/16024/16027/16028/16029/16030；Controller 具备读取、培养、等级、技能、水晶、幻化、自动购买封装 | **协议层基本足够**，但缺外窗 Prefab/View 与真实状态/写事务验证 |
| PetEquip 16014-16017 | 已有 info/wear/strengthen/polish Controller、Model、三页 `PetEquipModule` 与 5 张配表读取器 | 基础闭包存在；缺真实滚动、选中、详情、写回与返回链证据 |
| Partner 142 | 已有 14200/14201/14202/14204/14205；14203 跟随、14206 妖核、14207 传记仍明确不注册 | **不足**；神巫完整出战/跟随/传记与详情页不能完成 |
| Demon 183/509 | 只读快照已接 18301/18302/18303/18307/18311/18314/18315/18317/50901；老端激活、升级、升星、共修等 18304/18305/18306 与商城写协议未接 | **不足**；只能支撑部分数据快照，不能支撑完整天妖页 |
| PartnerAwake | 老端含阶段页签、任务滚动、属性、技能与任务提交链；Unity 顶层无对应 Flow/View | **不足** |

配置精确检查：

- 已存在：`config_mount_star/stage/goods/prop/figure/figure_stage/figure_star/skill/level`；
- 已存在：`config_pet_equip_pos/pos_lv/stage/star/goods`；
- 目标 server 路径下未找到 `config_mount_level_skill`、`config_partner`、`config_partner_awake`、`config_demons*` 这些直观名称；这不等于仓库中绝对无等价表，后续必须按当前读取器和 Addressables 地址精确确认，禁止凭文件名猜映射。

## 组件依赖与共享外观边界

| 组件 | 身份 | 消费者/用途 | 本轮边界 |
|---|---|---|---|
| `PetModule.prefab` | GUID `03ca9559d312d3a42829b05c7ec0e8da` | 坐骑/剑魄同修共用培养主面 | 已人工接管，禁止重转；本轮未改 |
| `PetRoundItem.prefab` | GUID `14246b96b2640e248bdd3fb21499eeab` | 技能球、魔晶槽的共享圆形项 | 需空/有数据、锁定/解锁、数量上限与点击详情矩阵 |
| `PetEquipModule.prefab` | GUID `9f3b25bc238f30a47a9e7d0f5a7b0db3` | 坐骑/伙伴装备背包、强化、打造三页 | 跨 PetEquip；本轮只登记，不修改 |
| `PetEquipOutItem.prefab` | GUID `58c6034fdf042f3469a2deff13838a13` | 主培养页装备槽 | 需空槽/已穿戴、不同 type、红点与详情抽查 |
| `BaseAwardItem.prefab` | GUID `39fc659b34b1fb646b7cbed26e2442d2` | 培养、等级、幻化、装备材料 | Common 共享；禁止修改，需足/不足、特效开/关抽查 |
| `BaseWindowSkin.prefab` | GUID `53f62e6db6a510043b49a8b8cbe8f469` | 顶层四标签窗与 PetEquip 外窗 | Common 共享；禁止修改，需两档 viewport/关闭重开 |
| `DemonMainView.prefab` | GUID `9ec5b1ac406c96941aaf67834aadd817` | 天妖页转换产物 | 仅有 Prefab 不等于已接可运行 View |
| `UIModelStage` + OutWard 外观配置/资源 | 代码共享模型台 | 坐骑、剑魄、幻化、神巫、天妖模型 | 本轮禁止改 Role-OutWard/Addressables；必须分别验证模型存在、比例、朝向、常驻特效与清理 |

## 本轮确定性修复判断

没有修改 Pet 正式代码或 Prefab，原因不是“没有问题”，而是当前确定缺口都落在以下边界：

1. 唯一可编辑的 `Assets/Scripts/Module/Core/Pet/Views/OutWardBaseView.cs` 在本轮开始前已有并发脏改动，且父任务明确禁止触碰 Role-OutWard 共享链；
2. 幻化、等级技能、水晶/技能详情、属性、祝福需要新增/转换 Prefab 与 View，不能在已有 `PetModule.prefab` 上靠日志或临时代码伪造；
3. 神巫/勋印初启/天妖需要独立内容页面与协议闭包，不能通过开启 `PetFlow.TabEnabled[2/3]` 冒充完成；
4. 本轮无账号写授权，不能以真实 160xx/142xx/183xx 消耗、穿戴、培养、出战事务来“验证”静态猜测。

因此保留 `PetFlow` 当前对第 3/4 页禁用的安全闸，且没有重转或重建任何 Prefab。

## 后续最短闭包顺序

1. 先完成并合并当前并发中的 `OutWardBaseView` 增量，再静态复核培养主面、模型、材料、装备入口；
2. 用首次转换流程分别落地缺失的 `OutwardLvSystem`、`PetSkillView`、`PetCrystalView`、`PetProptityView`、`IllusionBaseView/OutwardStarView`、`PetBlessView`，随后回到 fix-view 增量精修；
3. 用现有 PetEquip 三页跑只读真实状态，取得专用账号授权后再测 16015/16016/16017；
4. 单独闭包 `PartnerBaseView` 与条件 `PartnerAwakeMainView`，补齐 14203/14206/14207 及任务协议后再开放第 3 页；
5. 单独闭包 `DemonMainView` 与全部子页/写协议后再开放第 4 页；
6. 最后在同账号、同状态、两档 viewport 的老 H5/Unity Web 单会话中顺序复走四页和关闭祝福弹窗。

## 验证分层

- `route_ledger.py init`：schema 6，151 nodes，已通过；
- 本报告与 manifest：纯静态，只证明控件树、协议边界、Prefab/代码缺口和禁止开放边界；
- 未启动 Unity，未启动浏览器，未执行培养、购买、穿戴、强化、打造、出战或任务提交；
- 未修改 MainUI/Common/BaseWindow/Equip/Bag/Role-OutWard/Generated/Addressables/Docs；
- 未更新 Docs：父任务明确禁止，结论保存在本路线 output，待主控统一回写。
