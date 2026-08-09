# GodBeast 路线树摘要

正式、逐控件拓扑见 `route-manifest.json`，状态见 `route-ledger.json`。本摘要只便于人工导航。

```text
mainui.treasure.god-beast
├─ 入口、开放条件、返回链
├─ GodBeastView 主页（Unity 整页缺失）
│  ├─ 幻兽列表：空/有数据、选中、默认、红点、横向滚动、末项
│  ├─ 属性：基础/加成/战力/助战与出战计数
│  ├─ 5 个装备位 → BeastToolTips → 替换/卸下/强化
│  ├─ 技能列表 → PetSkillView
│  ├─ 出战/召回、快速装备、全部卸下、置灰条件
│  └─ 背包、扩位、铸造入口
├─ GodBeastTipsView 扩位弹窗
├─ GodBeastBagView 背包 → 每格详情
├─ GodBeastComView 铸造
│  ├─ 4 材料位、放入/移除、目标预览、品质下拉、背包滚动
│  ├─ 17310 铸造、Halo 自动铸造、关闭/停止
│  └─ 成功弹窗、效果、即时刷新与重开
├─ GodBeastSelectView 选择装备
│  ├─ 部位过滤、滚动/裁切/末项、每格详情
│  └─ 穿戴/替换/卸下及父页即时刷新
├─ GodBeastStrView 强化
│  ├─ 已穿列表、材料、当前/下级属性、进度/满级
│  ├─ 强化一次/十次
│  └─ 成功效果、即时刷新与重开
├─ 旧 GodBeastStrenView（不可达/实现已注释）
├─ 共享组件与跨模块跳转依赖
└─ 生命周期、资源 ready、声音、性能、两档 viewport、真实 Web
```

所有 143 个叶均显式记录：136 个为 `blocked`，7 个仅为 `needs-runtime-verify`；父节点由叶状态复算后总计 `blocked=161`、`needs-runtime-verify=7`，不存在 `done`。
