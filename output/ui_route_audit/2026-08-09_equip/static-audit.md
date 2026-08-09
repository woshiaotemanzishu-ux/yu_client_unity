# Equip 主路线静态审计（2026-08-09）

## 结论

- 已从老端 `EquipView.ts` 固定六页签事实建立 schema 6 路线，共 139 个节点：天殒淬炉、神兵淬炼、骸珀镶嵌、吞天洗魄、神屠九炼、不朽圣骸，以及大师窗、宝石背包、雕刻、洗魄选材、神炼结果、圣骸总属性等子页。
- 当前 Unity 不是“六页均完成”：圣骸页具有较完整的配置驱动选择、材料预览和二次确认链；宝石雕刻的装备选择已有部分真实数据；其余成长页大量仍是空列表、隐藏模板、缺配置或日志占位。
- 本轮不启动 Unity/浏览器、不操作账号、不执行真实写事务；所有运行态叶子保持 `needs-runtime-verify`，缺实现/配置、跨共享岛或写事务叶子标 `blocked`。
- 修复了一个确定性安全缺陷：列表/配置尚未落地时，强化、淬炼、镶嵌、洗魄、神炼和雕刻不再退化为固定武器槽、猜测实例或 `material_type_id=0` 发真实协议。只有真实列表选择与必要配置就绪后才允许进入发送链。

## 老端六页签事实

| 索引 | 老端标签 | 内容 View | Unity 当前来源 | 静态结论 |
|---:|---|---|---|---|
| 0 | 天殒淬炉 | `EquipStrenView` | `EquipModule.prefab` | 装备格/属性/消耗/战力/红点未接，单件与一键按钮现已安全阻断 |
| 1 | 神兵淬炼 | `EquipSmeltView` | `EquipModule.prefab` | 装备格、淬炼配置、属性/消耗和特效未闭包，15251 阻断 |
| 2 | 骸珀镶嵌 | `EquipJewelView` | `JewelModule.prefab` | 主装备格/六槽模板缺失；宝石背包因此不可从主页到达；雕刻装备列表部分可用，材料链缺失 |
| 3 | 吞天洗魄 | `EquipWashView` | `EquipModule.prefab` | 装备列表/洗魄配置/属性与选材未闭包，15212/15213/15252 阻断 |
| 4 | 神屠九炼 | `EquipRefinementView` | `EquipRefinementModule.prefab` | 装备列表、`config_equip_refinement`、属性/消耗/详情/说明未闭包，15255 阻断 |
| 5 | 不朽圣骸 | `EquipArmorView` | `EquipArmorModule.prefab` | 当前实现含阶段、荒陨/天殒类型、部位、属性、材料、14402 二次确认与指纹复核；缺真实运行与写授权 |

根窗口使用共享 `BaseWindowSkin`，六页签当前均被 `EquipFlow.TabEnabled` 开放；“开放”仅表示能尝试加载内容源，不能替代页内叶子完成。

## 入口与返回边界

- 主入口：`MainUIRouter "equip" -> EquipBootstrap -> EquipFlow.Toggle -> BaseWindowSkin + 各内容 Prefab`。
- Role/Bag 的已穿戴装备格进入共享 `CommonModule/ItemTipsView/EquipmentItem` 详情或对比，不直接等价于 Equip 六页签；本轮仅登记入口依赖，禁止修改 Role、Bag、Common。
- `EquipJewelCraveView` 当前直接跳过老端 `EquipJewelCraveEnterView` 外窗身份；没有独立等价返回链，需后续真实目标身份修复，不能用关闭整个 Equip 窗口代替子窗关闭完成。

## 协议与事务边界

| 系统 | 只读/查询 | 写事务 | 当前边界 |
|---|---|---|---|
| 强化 | 15204、15261 | 15205、15260 | 列表/成本/大师条件未闭包；写 blocked |
| 淬炼 | 15250 | 15251 | `config_equip_refine_max/refine_lv/whole_reward` 缺 Unity 闭包；写 blocked |
| 镶嵌 | 15210、15254 | 15208、15209、15211、15215、15216 | 主装备位/宝石槽模板、镶嵌/升级/雕刻材料配置缺失；写 blocked |
| 洗魄 | 15214 | 15212、15213、15252 | 解锁/洗魄/材料/保底配置与列表缺失；写 blocked |
| 神炼 | 无独立查询 | 15255 | 缺 `config_equip_refinement` 与真实实例选择；写 blocked |
| 圣骸 | 14401 | 14402 | 当前 View 有二次确认、材料与状态指纹复核；本轮无账号写授权，确认事务 blocked |
| 穿戴入口 | 15010 pos=1 | 15201 | 属 Bag/Common/EquipWear 共享边界，本轮不执行 |

`EquipReadController` 的 15217/15219/15220～15223/15262 属神装/套装读取与制造链，不是当前 `EquipArmorView` 的 14401/14402 圣骸页面协议；本路线只登记控制器依赖，不把跨功能协议冒充当前页完成。

## 组件依赖清单

| 组件 | 身份 | 直接宿主/用途 | 状态矩阵与本轮边界 |
|---|---|---|---|
| `BaseWindowSkin.prefab` | GUID `53f62e6db6a510043b49a8b8cbe8f469` | Equip 六页签窗口框、关闭、标题/背景 | 共享 Common；未修改。需六页切换、关闭/重开、两档 viewport 抽查 |
| `BaseAwardItem.prefab` | GUID `39fc659b34b1fb646b7cbed26e2442d2` | 淬炼消耗、洗魄材料、神炼成本、雕刻材料 | 共享 Common；未修改。需空/有数据、足/不足、特效开/关、滚动裁切状态 |
| `EquipmentItem.prefab` | GUID `4b61db156a07d9c4182c38b719828178` | 当前装备、装备槽、雕刻/神炼/圣骸展示 | 共享 Common；未修改。详情与对比只登记跨岛依赖 |
| `EquipAttrItem.prefab` | GUID `1a5c2539cadb4f04184fc56c4f531632` | 强化/淬炼当前与下级属性行 | Equip 私有共享；当前 `EquipAttrItem` 仍含属性名/升幅占位，blocked |
| `EquipMasterItem.prefab` | GUID `89ab102a6bb20254fa5e916874c8e0fe` | 强化大师、镶嵌大师当前/下一阶属性 | Equip 私有共享；两处属性列表均未铺，激活写入口已阻断 |
| `EquipWashItem` 嵌入模板 | `EquipModule/EquipRefinementModule` 内实例 | 洗魄与神炼装备列表 | 两页共用数据形状；当前父列表未实例化，不能用固定武器槽代替 |
| `EquipJewelCraveSubItem` 嵌入模板 | `JewelModule.prefab` | 雕刻装备位列表 | 已按真实已穿戴位克隆；需空/有装备、选择切换、滚动和详情运行验证 |
| `ArmorTabItem/ArmorItem/ArmorAttrItem` 嵌入模板 | `EquipArmorModule.prefab` | 圣骸阶段、部位、属性与材料 | EquipArmor 私有；需多阶段、两类型、可造/不可造、空/有材料与滚动状态 |

本轮没有改任何共享组件，因此不扩大到 Common/Bag/Role 消费者；共享组件状态矩阵只作为后续运行闸登记。

## 本轮 Equip 私有修复

1. `EquipStrenView`、`EquipSmeltView`：选中部位默认改为 0；单件/一键事务必须先由真实装备格建立选择。
2. `EquipJewelView`：移除固定武器槽升级；仅接受真实装备位与实例 ID，未选择时不发 15215。
3. `EquipWashView`：移除固定武器槽；缺真实选择或 `config_equip_wash_unlock_lv` 时不发 15213/15252。
4. `EquipWashPropItem`：缺解锁配置时不再依赖服务端兜底发 15212。
5. `EquipRefinementView`：移除固定武器实例；未由列表选择时不发 15255。
6. `EquipJewelCraveView`：材料列表/配置缺失时不再发送 `material_type_id=0` 的 15211。
7. `EquipStrenMasterView`、`EquipJewelMasterView`：当前/下一阶条件与属性尚未展示时阻断 15260 激活。

## 后续闭包顺序

1. 补 EquipStren/Smelt/Jewel 主装备位模板、真实选择和当前装备展示；再接属性、战力、消耗、红点与特效。
2. 定向同步并登记缺失配置：淬炼、镶嵌/宝石等级与槽位解锁、雕刻材料/限制、洗魄解锁/消耗/属性、神炼。
3. 恢复宝石六槽到 `EquipJewelBagView` 的真实可达链，以及雕刻外窗身份/返回链。
4. 铺洗魄/神炼共享装备列表与洗魄四槽、选材窗、详情/说明跨岛依赖。
5. 先对圣骸跑真实只读选择、滚动、确认后取消；取得专用账号消耗授权后再执行 14402 成功/失败/即时刷新/重开。

## 验证分层

- `route_ledger.py init`：schema 6 / 139 nodes，通过。
- 本轮前半在当前 Framework 引用下完整 `Shenxiao.Module.Core` 编译通过（仅既存 TMP 过时 API 警告）；最终复编时被并发中的非 Equip 文件 `GuildHelpFlow/ShopFlow/GuildMainFlow` 引用尚未落地的新类型阻断。为排除该外部漂移，九个最终 Equip 改动文件使用原 Core 引用做隔离编译，通过。
- `git diff --check -- Assets/Scripts/Module/Core/Equip`：通过。
- 未启动 Unity，未运行依赖 Unity 的 CliVerify；未执行 152xx/14402/15201 账号事务。
- 未更新 Docs：父任务明确禁止修改 Docs，本轮结论完整保存在路线 output，待主控统一回写总文档。
