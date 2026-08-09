# Composite 全路线静态审计

## 结论边界

- 本批次只做静态源码、配置、Prefab、Generated Bind（只读）与业务 View 交叉核查；未启动 Unity、浏览器或任何前台程序，未执行账号写事务。
- schema 6 manifest 共 144 个节点，其中 123 个叶子均显式落为 `blocked` 或 `needs-runtime-verify`，没有任何静态 `done`。
- `static-results.json` 的叶状态为 `blocked=100`、`needs-runtime-verify=23`；正式台账按叶状态回卷后为 `blocked=120`、`needs-runtime-verify=24`。
- 资产事务全部 blocked：15028 规则合成、15020 合成、15019 拆解，以及符文合成前可能触发的 16711 卸下。未发送协议、未新增协议常量/发送器/处理器。

## 顶层控件树

`CompositeView`

- 返回：关闭页时同时关闭材料选择/来源等子弹窗。
- 货币栏：老端 `money_list=[2,1,0]`。
- 道具合成：分类页签、目标物、材料列表、数量减/加/最大/计算器、合成、红点/特效、宝石拆解子页。
- 熔魄铸魂：分类页签、目标预览、材料槽/选择弹窗、合成、忽略觉醒提示、材料来源弹窗、等级锁、红点/特效。
- 影骸战衣合成：分类页签、目标、材料、合成、绑定材料确认、红点/特效。
- 遗骸铸合：分类页签、装备部位列表、目标、五材料槽/选择弹窗、一键添加、合成、绑定确认、锁定/失败/特效状态。
- 九霄冥饰合成：分类页签、六部位页签、目标、材料槽/选择弹窗、一键添加、合成、绑定确认、锁定/失败/特效状态。
- 天骨铸灵：分类页签、装备部位列表、目标、五材料槽/选择弹窗、一键添加、合成、绑定确认、锁定/失败/特效状态。
- 启示铠铸：分类页签、目标、材料、合成、绑定材料确认、红点/特效。
- 灵宠炼合：分类页签、目标、材料、属性滚动区、合成、绑定确认、成功后灵宠跳转、红点/特效。
- 御府铸仪：分类页签、目标、固定材料、五可选材料/选择弹窗、一键添加、合成、规则弹窗、锁定/失败/特效状态。
- 天殒铸灵：分类页签、目标、材料、合成、绑定材料确认、红点/特效。
- 条件入口 `red`：应进入 `RedEnterView/CompositeEquipView`，包含装备分类、八部位、规则下拉、目标、五材料槽、一键添加、两个装备选择入口、规则、排行弹窗、装备拆解页及状态；当前路由静态身份不符。

完整直接子控件映射见 `route-manifest.json` 的每个 `type=page` 节点 `control_inventory[]`；选择弹窗、排行弹窗、拆解页和返回链均已继续拆到叶。

## 页签与配置根

| 序号 | 老端标签 | 老端 View | 配置根 | 开放等级 | Unity 静态状态 |
| ---: | --- | --- | ---: | ---: | --- |
| 1 | 道具合成 | `CompositeGoodsView` | 2000 | 120 | CompositeModule 内有页和业务 View，但数据/事务闭环未迁移 |
| 2 | 熔魄铸魂 | `CompositeRuneView` | 3000 | 50 | 有独立 Prefab，无业务 View，Flow 禁用 |
| 3 | 影骸战衣合成 | `CompositeHolySealView` | 80 | 95 | CompositeModule 内有页和业务 View，仍为降级骨架 |
| 4 | 遗骸铸合 | `GodBeastCompositeView` | 6000 | 350 | 缺可编辑 Prefab、缺业务 View，Flow 禁用 |
| 5 | 九霄冥饰合成 | `CompositeUnrealView` | 5000 | 1 | CompositeModule 内有页和业务 View，仍为降级骨架 |
| 6 | 天骨铸灵 | `GodBefallCompositeView` | 8000 | 400 | CompositeModule 内有页和业务 View，仍为降级骨架 |
| 7 | 启示铠铸 | `CompositeRevelationView` | 72 | 480 | 缺可编辑 Prefab、缺业务 View，Flow 禁用 |
| 8 | 灵宠炼合 | `CompositeGuardView` | 26000 | 120 | CompositeModule 内有页和业务 View，仍为降级骨架；事务是 15028 |
| 9 | 御府铸仪 | `GodCourtComView` | 78 | 490 | CompositeModule 内有页和业务 View，选择弹窗/数据闭环未迁移 |
| 10 | 天殒铸灵 | `CompositeLonglangView` | 7700 | 1 | CompositeModule 内有页和业务 View，仍为降级骨架 |

## 本轮增量修复

- `Assets/Scripts/Module/Core/Composite/CompositeFlow.cs`：补齐老端 10 个页签的准确标签，并给每个 `TabSpec` 赋 `Label`。之前 `TabSpec.Label` 为空，公共页签皮肤无法取得文字。
- 未改 Prefab：现有 Prefab 属人工视觉事实源，本轮没有真实 Unity/Web 像素、交互或生命周期证据，不据静态推测调整视觉。
- 未为缺失页面调用转换器：本轮职责是静态审计，`GodBeastCompositeView` 与 `CompositeRevelationView` 仅记录首次落地 blocker。

## 主要 blocked

- hard-negative 15028：道具合成与灵宠炼合。缺 `CompositeModel/config_compound`、材料选择冻结、背包权威校验、single-flight、成功/失败即时刷新和结果展示闭环。
- 15020：符文、影骸、遗骸、冥饰、天骨、启示、御府、天殒及 red 装备合成；会真实消耗材料并产出物品。
- 15019：宝石/装备拆解；会删除真实物品并返还材料。
- 16711：符文若已穿戴，老端合成前可能先卸下；同样属于真实资产状态写入。
- 缺页面：`GodBeastCompositeView`、`CompositeRevelationView` 无可编辑 Prefab/业务 View。
- 有 Prefab 无业务页：`CompositeRuneView`。
- 现有页面的数据根未闭环：分类、目标、材料、背包数量、红点、锁定、失败态、结果态和选择弹窗多为隐藏模板或日志降级。
- `red` 条件入口当前复用默认 CompositeFlow，道具页身份与老端 `RedEnterView/CompositeEquipView` 不一致；跨模块入口链只读，未越界修改。

## needs-runtime-verify

- 主窗关闭、货币栏、数量减/加/最大/计算器、忽略觉醒开关、一键添加、绑定材料确认、规则/排行弹窗、成功后导航、各子页关闭/返回。
- 上述叶仅表示存在静态 Prefab/Bind/代码线索；仍需真实 `GraphicRaycaster -> PointerClick`、同账号 Unity Web、cold/warm、即时状态、关闭重开、两档 viewport 与老端对照。
- 页面级状态因子叶未完成而保持回卷状态，未绑定真实 Web run、Player/catalog、源码/dirty 指纹或像素证据。

## 验证

- 官方台账：`route_ledger.py init/apply/validate` 通过。
- 独立 output-only 验证器：`static-audit/CompositeStaticAudit.csproj`（`net10.0`）运行通过。
- 验证器检查：10 个老端标签与 Flow 一致、`TabSpec.Label` 已赋值、manifest 节点唯一、页面 `control_inventory` 与直接子节点一致、全部叶有显式结果、全部事务 blocked、15028 原因存在、关键 Prefab/View 存在性、配置根统计及证据文件 SHA-256。
- 机器结果见 `static-verification.json`，当前 `verdict=pass`。

