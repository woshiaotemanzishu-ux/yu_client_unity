# Armor 共享组件依赖与状态矩阵

## 依赖清单

| 页面节点 | 共享资产/View | GUID/身份 | 数据/状态/回调 | 本轮边界 |
| --- | --- | --- | --- | --- |
| 整页与内嵌模板 | `Assets/Prefabs/UI/EquipArmor/EquipArmorModule.prefab` | `15fdbb730b5989c42833b6f06e2c00ec` | 14401 树、三张配置、选择状态、14402 回包 | Armor 专属现有 Prefab，只读未改 |
| 属性行 | `Assets/Prefabs/UI/EquipArmor/ArmorAttrItem.prefab` | `f4efb174afc553f488b01f087a7ac62f` | attr id/value、当前/增量/总属性形态 | 同时供页内属性与总属性弹窗 |
| 当前圣骸/部位/材料格 | `Assets/Prefabs/UI/Common/BaseAwardItem.prefab` | `39fc659b34b1fb646b7cbed26e2442d2` | typeId、数量、缩放、详情点击、品质特效 | Common 禁写；部位格默认点击语义已登记 blocker |
| 装备六页签窗 | `Assets/Prefabs/UI/Common/BaseWindowSkin.prefab` | `53f62e6db6a510043b49a8b8cbe8f469` | 6 页签、关闭、内容宿主 | Common/Equip 禁写；页签标签 blocker |
| 打造确认 | `TipsManager.Confirm/ConfirmDialog` | 公共运行时组件 | 冻结材料文案、确认、取消 | Common 禁写，未做真实点击 |
| 物品详情 | `ItemTipsView` | 公共运行时组件 | typeId/数量、遮罩/关闭 | Common 禁写，需逐类身份复验 |

## 组件状态矩阵（全部待运行态）

- `ArmorAttrItem`：已打造当前值、未打造 `0 + 增量`、总属性 `名称 + 增量`、短/长属性名、短/长数值。
- 阶段项：选中/未选中、开放/锁定、完成/未完成、红点有/无、首项/末项和滚动裁剪。
- 部位项：type1/type2 各五格、选中/未选中、打造/未打造、前阶锁定、可打造红点；点击只选部位，不开详情。
- 材料格：空/有数据、普通材料/前阶圣骸状态项、充足/不足、数量短/长、详情开/关、品质特效开/关。
- 总属性弹窗：空/有数据、短列表/长列表、完整/部分/完全滚出、点遮罩关闭且点卡片内容不关闭。
- 页签：装备六页签的选中/未选中、Armor 第 6 页签标签、移动/宽屏 viewport。

本轮没有修改共享组件或 Prefab，且禁止 Unity/真实 Web，故不伪造 `shared_component_identity/component_state_matrix` 运行证据。后续若修 `BaseAwardItem` 消费方式，应按“展示可点详情 / 展示不可点 / 交互选中 / 品质特效”分组抽查 2～4 个代表宿主，再返回 Armor 原路线整页复验。

