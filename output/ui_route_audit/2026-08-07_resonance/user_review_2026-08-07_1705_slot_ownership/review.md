# 共鸣流光归属与共享格回归复查

> 复查时间：2026-08-07 17:05（Asia/Shanghai）
>
> 状态：用户已纠正特效归属，代码、共享 Prefab 身份与代表性静态门禁已按新结论修复；新的 Unity 真实运行态尚未执行，相关路线保持 `needs-runtime-verify`。

## 用户纠正

1. 金橙色流光属于“已穿戴装备物品槽”的状态效果，不属于共鸣页面中央的当前/下一阶展示图标。
2. 共鸣边缘部位格与背包/装备栏的已穿戴格可复用共享 `EquipmentItem` 能力，但只有明确的已穿戴槽宿主可以 opt-in；材料、奖励、详情、普通背包格、共鸣中央展示和预览不能自动继承。
3. 修改共享格后无需把约 80 个引用页面逐一运行，但必须静态列出影响面并按有数据/空槽、特效开/关、直接/嵌套、展示/交互和不同宿主缩放分组，运行态抽查目标页及每个实质差异形态的独立代表；任一代表失败再扩大同组范围。

本次人工证据：

- `reference_equipped_slot_flow.png`：已穿戴装备槽应有的贴格流光。
- `regression_bag_blank_slots.png`：共享 Prefab 根组件丢失后，背包装备位和普通格一起退化为空灰格。
- `reference_resonance_page.png`：共鸣页正确参考。
- `regression_resonance_central_slot_effect.png`：错误把槽位流光/倍率加到共鸣中央当前与下一阶图标。
- `consumer-inventory.md`：按源 Prefab GUID 枚举 `EquipmentItem` 81 个、`BaseAwardItem` 76 个直接 Prefab 消费者，并记录本轮分组与代表样本。

## 根因与修复边界

1. `EquipmentItem.prefab` 根上的 `EquipmentItem` 以及 `BaseAwardItem.prefab` 根上的 `BaseAwardItem` 曾被删除。背包装备位通过 `GetComponent<EquipmentItem>()`、普通格通过 `GetComponent<BaseAwardItem>()` 取不到运行时组件，因此表现为大面积空灰格。现只恢复这两个共享根组件及原序列化引用，保留已经确认的透明空 Image 修正，不重建共享 Prefab。
2. `EquipmentItem.SetSuitEffect` 保留为共享装备槽能力，但 `SetData` 默认清除特效；只有共鸣边缘部位格和背包装备位在确认“传入实例就是该部位当前已穿戴实例”且共鸣状态满足时显式调用。普通物品格与其他展示消费者默认保持 off。
3. `ResonancePresenter` 的中央当前/下一阶与预览继续走页面自己的 `effBox1/effBox2/preview.effBox` 链，缩放按当前转换资源和页面宿主独立映射；撤销把老端数值 `10` 机械写入页面 Presenter 的错误。共享资源名只表示资源相同，不表示槽位宿主与页面宿主可以共用最终倍率。
4. 背包订阅 `EVT_EQUIP_SUIT_UPDATE`，配置或共鸣状态到齐后重绑已穿戴槽；普通背包网格不订阅槽位特效，也不会被批量点亮。

## 本轮代表性检查矩阵

| 形态 | 代表消费者 | 当前门禁 | 当前状态 |
|---|---|---|---|
| 共享根身份 | `EquipmentItem.prefab`、`BaseAwardItem.prefab` | 根组件与嵌套模板序列化引用存在 | 静态已修，待编译 |
| 目标页、特效 on | 共鸣边缘部位 `EquipSuitPosItem → EquipmentItem` | 精确已穿戴实例、阶级、Handle、二维足迹与清理 | 代码已修，运行待验 |
| 独立高频页、特效 on/off | `BagEquipmentIcon → EquipmentItem` | 空槽/有装备、状态更新、显式 opt-in | 静态已接，运行待验 |
| 独立高频页、普通格 off | `BagItemRenderer → BaseAwardItem` | 根组件可取、普通格不拥有套装流光 | 静态已接，运行待验 |
| 同页不同宿主 off | 共鸣中央当前/下一阶 `EquipmentItem` | 不拥有 `_suitEffect`；页面特效保持独立倍率 | 代码门禁已加，运行待验 |

这里的“运行待验”不能由 C# 编译、Prefab YAML 或历史截图替代。后续获得不占用用户前台的 Unity 验收窗口后，只需先跑共鸣目标页、背包装备位和背包普通格三类代表；样本全部通过即可收口本次共享改动，失败时才扩查同组消费者。
