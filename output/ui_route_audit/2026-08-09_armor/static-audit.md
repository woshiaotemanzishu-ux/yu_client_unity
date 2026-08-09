# Armor（不朽圣骸）全路线静态审计

## 完成边界

- 本轮只对老 H5 源码/配置与 Unity Armor Controller/Model/Configs、现有 `EquipArmor` Prefab、Generated Bind（只读）和 Equip 消费 View（只读）做静态交叉。
- 未启动 Unity、浏览器或前台程序；未登录账号、未发送 14402、未修改资产状态。
- schema 6 共 89 节点、75 叶；叶结果 `blocked=23`、`needs-runtime-verify=52`，正式台账回卷后 `blocked=31`、`needs-runtime-verify=58`，没有 `done`。
- `fix-view` 判定：页面已有可编辑 Prefab，但没有本轮真实 old/unity/diff、像素或射线证据，因此不凭静态猜测改 Prefab。确定的功能/状态差异落在禁止写入的 Equip/MainUI/Common，全部登记 blocker。

## 控件树

`mainui.equip.armor`

- 入口与返回：主界面装备入口 → 装备窗第 6 页签“不朽圣骸”；共享装备窗关闭。
- 数据就绪：打开页发送只读 14401；配置加载中、全量快照加载中；`config_armour_equipment/suit/kv` 资源闭包。
- 阶段列表：9 阶纵向列表、滚动结构、真实拖动与末项、阶段选择、等级锁定行、完成标记、红点。
- 类型页签：荒陨圣骸、天殒圣骸、选中/未选中皮肤、类型红点。
- 套装摘要：已打造数量/总数、已激活/未激活、左右两列套装属性。
- 部位列表：type1 的 pos 1～5、type2 的 pos 6～10，每格选择、选中、已完成、锁定、红点和图标状态。
- 当前圣骸：顶部当前物品、物品详情弹窗、未打造/已打造属性列表、属性滚动结构与拖动。
- 材料：三槽、空槽锁、持有/需求量、充足/不足、材料详情、前阶圣骸状态材料。
- 打造：按钮状态、材料不足、已经打造、安全确认弹窗、确认文案/取消/14402 提交、处理中单飞、失败、成功、即时刷新、关闭重开、成功特效。
- 总属性弹窗：打开、属性列表、空状态、滚动结构/末项、遮罩关闭。
- 条件与生命周期：主装备入口红点、切走页签再回、关窗重开、事件订阅、异步物品格后到清理。
- 视觉/性能：350ms、1000ms、ready，两档 viewport，cold/warm、资源二次幂等与点击后零新增。

每个页面直接控件到子节点的一一映射见 `route-manifest.json/control_inventory[]`；两类各五个部位已分别枚举，未用“部位列表可见”吞掉逐格点击。

## 老端事实与 Unity 静态现状

- 老端 `EquipArmorView` 是装备窗第 6 页签，页面标题“不朽圣骸”；type1“荒陨圣骸”、type2“天殒圣骸”。
- 阶段锁定文案：`open_lv<=370` 显示等级，`open_lv>370` 显示“神创(open_lv-370)”；当前配置开放等级为 450、470、490、520、550、580、610、640、670。
- type1 位置为 1～5，type2 位置为 6～10；底部部位格明确 `SetShowTips(false)`，点击只负责选择。顶部当前圣骸和材料格允许详情。
- 14401 是只读全量树；14402 是真实打造，固定 `stage:u8,type:u8,pos:u8`，成功才用回包切片更新，禁止本地预扣/乐观置位。
- Armor 自有 `ArmorController/ArmorModel/ArmorConfigs` 已静态具备全量替换、失败保留、成功局部合并、等级/前阶/未打造/真实背包材料门禁、状态材料过滤与指纹。
- 页面业务消费者和 Flow 位于 `Assets/Scripts/Module/Core/Equip`，本轮按文件岛只读，不能修改。
- 老端未发现页面专属声音调用；成功演出明确调用 `ui_dazaochengong(position=-0.85,0.55, scale=1.5)`。

## 确定 blockers

1. 装备第 6 页签入口属于 EquipFlow；当前 `TabSpec` 未传 `Label`，页面身份/文案修复在禁写岛。
2. 阶段锁定行未实现“神创N”文案；阶段选中态没有按老端切 `uizj_001/uizj_002`。
3. 类型选中态老端切 `uizj_007/uizj_008`，Unity 当前只改文字颜色。
4. 部位格动态挂 `BaseAwardItem`，未关闭默认详情点击；它可能截断父 `gp_con` 的部位选择，并与老端 `SetShowTips(false)` 冲突。未打造图标也未调用灰阶。
5. 普通不足材料老端图标灰阶，Unity 当前只把数量文字置红。
6. 总属性弹窗把主底图 `_img_bg` 绑定 `Hide`，没有静态证明存在老端独立半透明遮罩关闭面，可能点卡片内容即关闭。
7. 成功后 Unity 未调用老端 `ui_dazaochengong` 特效。
8. 老端 Armor 红点会刷新 `MainFunc.Equip` 主入口；Unity Armor 自有模块没有等价根红点状态，修复跨 MainUI/Equip。
9. 14402 会真实扣材料并更新角色属性，本轮无账号写授权；提交、失败/成功、即时刷新、重开全部 blocked。

## needs-runtime-verify

- 14401 页面刷新、配置/快照 loading、阶段滚动、类型切换、套装摘要、顶部详情、材料详情、安全确认取消、处理中单飞。
- 总属性列表/空态/滚动、共享窗口关闭、页签切换、隐藏重开、事件解绑和异步物品格清理。
- 所有 2D 位置/尺寸/层级/裁剪/图片/文字/间距、350ms/1000ms/ready、两档 viewport、cold/warm 与资源幂等。
- 这些叶只有静态源码/Prefab/Bind 证据；必须通过当前真实 Prefab 的 `GraphicRaycaster→PointerClick` 和同账号真实 Web 才可能完成。

## 本轮改动

- 生产代码/Prefab/资源：无。
- 仅新增 `output/ui_route_audit/2026-08-09_armor/` 下的 manifest、results、正式 schema 6 台账、静态验证器和报告。
- 未修改 Equip、MainUI、Common、Generated、Proto、Addressables、Docs 或项目文件。

## 验证

- 官方 `route_ledger.py init/apply/validate`：通过。
- 独立 output-only `net10.0` 验证器：0 warning、0 error，`VERDICT pass=True`。
- 验证器覆盖：老端控件/成功特效/部位无 tips 语义；14401/14402 权威代码；90/18/2 配置行与开放等级；Prefab/View/ScrollRect/RectMask 存在；manifest 拓扑与控件一一对应；所有叶显式状态；事务 blocked；八类跨岛差异均显式 blocked；关键源 SHA-256。

