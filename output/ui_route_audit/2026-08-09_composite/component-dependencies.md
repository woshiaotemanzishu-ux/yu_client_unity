# Composite 组件依赖与抽查边界

## 现有共享 Prefab

| 共享 Prefab | 静态消费者/用途 | 本轮处理 |
| --- | --- | --- |
| `CompositeGoodsMatItem.prefab` | 道具合成材料格；灵宠炼合材料格 | 只读枚举，未修改 |
| `CompositeHolySealMatItem.prefab` | 影骸战衣材料格；天殒铸灵材料格 | 只读枚举，未修改 |
| `CompositeSelectEquipItem.prefab` | 符文、遗骸、天骨、九霄冥饰的材料选择列表形态 | 只读枚举，业务弹窗闭环 blocked |
| `GodBefallButton.prefab` | 遗骸/天骨等装备部位选择形态 | 只读枚举，未修改 |
| `RingCompositeItem.prefab` | red/戒指复合路线物品格 | 只读枚举，red 路由身份 blocked |
| `CompositeEquipResolveView.prefab` | red 装备拆解页 | 有独立 Prefab，无完整业务闭环；15019 blocked |

`CompositeModule.prefab` 还内嵌页面根、模板节点和弹窗骨架。生成 Bind 只用于核对序列化字段，不作写入。

## 状态矩阵（待运行态）

- 材料格：空/有数据、充足/不足、绑定/非绑定、选中/未选中、1/多材料、不同宿主缩放与裁剪。
- 选择列表：空/有数据、滚动前/后末项、选择/取消、关闭/重开、条件可见/不可见。
- 合成页：未开放/开放、可合成/材料不足、绑定确认前/后、成功/失败、红点有/无、特效开/关。
- 拆解页：空背包/有可拆物、单选/切换、产出列表短/长、确认/取消、成功后即时刷新/重开一致。
- 页签：10 个标签的短/长文案、选中/未选中、禁用/开放、两档 viewport。

本轮只修改 `CompositeFlow` 的页签输入标签，没有修改任何共享 Prefab、序列化 Bind 或共享生命周期。因此不伪造共享组件 runtime 样本；后续真实运行态至少抽查目标页，并从不同材料格/弹窗使用形态各选一个代表消费者。

