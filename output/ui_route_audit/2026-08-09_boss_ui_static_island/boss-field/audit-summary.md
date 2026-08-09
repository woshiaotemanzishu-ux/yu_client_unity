# BossField 静态 UI 路线审计

## 完成边界

- 已按 `audit-game-ui-route` 完整枚举 BossField 主页面、Abyss、复活、战魂商店、疲劳和体力找回的控件/条件/弹窗/列表叶；已有 Prefab，按 `fix-view` 增量接管，没有调用 `convert-module`。
- 未启动 Unity、浏览器或任何前台程序，未登录账号，未执行 46003/46007/46045/15302/20004/40060/GoodsUse 等写事务。
- schema 6 拓扑在 `route-manifest.json`，结果在 `results-static.json`；所有叶仅 `blocked` 或 `needs-runtime-verify`，没有手写正式 `route-ledger.json`，没有 `done`。

## 老端到 Unity 映射

| 老端页面/控件 | 当前 Unity |
|---|---|
| `BossFieldEnterView.ts` | `BossFieldModule.prefab/BossFieldEnterView` → `BossFieldEnterView` |
| `BossFieldRoomItem.ts` | `_tpl_BossFieldRoomItem` → `BossFieldRoomItem` |
| `BossFieldRewardItem.ts` | `BossFieldRewardItem.prefab` → `BossFieldRewardItem` |
| `BossAbyssEnterView/RoomItem/FailureView.ts` | 同名 Prefab 节点 → 三个同名 runtime subclass |
| `BossFieldReliveView.ts` | `BossFieldReliveView` → runtime subclass |
| `BossFieldSoulShopView/Item/SubItem/Alert.ts` | 同名 Prefab 节点/模板 → 四个 runtime subclass |
| `BossFieldTiredNewView.ts` | `BossFieldTiredNewView` → runtime subclass |
| `BossFieldVitBuyView.ts` | `BossFieldVitBuyView` → runtime subclass |

13 个活跃 Bind 的 Prefab `m_Script` 已精准换成业务脚本 `.meta` GUID，并同步 `m_EditorClassIdentifier`。所有模板克隆只接受 Prefab 已挂的业务 subclass；若 GUID 失配，记录错误并销毁/跳过，不再 `AddComponent` 生成无序列化绑定的空组件。

疑似旧版分支保持未接管：`BossFieldTiredAlert.ts`、`BossFieldVitView.ts` 源码注释“目测没有使用/废弃”，`BossFieldVitItem` 仅服务该旧 View；`BossFieldResultView` 只找到 scene/json，未找到当前 TS 消费者或 Unity Bind。它们均显式 `blocked`，未以静态猜测删除。

## 静态实现

- `BossFieldFlow` 懒加载合并 Prefab，主页面互斥、弹窗叠加，统一走 `Show/Hide`。
- 主 Field 页面请求 Field/FieldSpecial/FieldInfinite 46000 与 Field 46043，消费现有 `BossModel`，落位体力、提醒和正式 Enter/Remind/Drop/Shop/Tired/VitBuy 入口。
- Abyss 页面请求 type=7，八个具名槽可选择，正式 46003/46007 入口、Failure 倒计时弹窗已接；VIP/费用/场景人数/模型/奖励因权威闭包缺失保持 blocker。
- SoulShop 请求 `15301 type=17`，按三列分组克隆真实 ShopModel 条目，显示配置物品名、价格、限购/售罄，并接正式 `15302` 购买入口；Common 详情、说明、37110001 使用保持 blocker。
- Relive 绑定 `ReliveModel` 击杀者状态；仅已由 `ReliveModel.DEFAULT_RELIVE_TYPE=22` 证明的免费复活入口接正式 Controller，场景专属 mode 不猜。
- Tired/VitBuy 落位权威 Vit/MaxVit/BackVit 与 close/cancel；无 slider 真实值时明确拒绝猜测发送 46045。

## 关键静态缺口与最小修复

1. **动态列表缺模板**：老端 `BossFieldTabItem`、`BossCommonRoomItem`、Abyss 的 `BossSuitItem` 是动态组件，但当前 BossField Generated/Prefab 没有可编辑模板/Bind。最小修复是首次人工补入页面专属模板及序列化引用，再由现有 View 克隆；不能运行时自造视觉树。
2. **配置 API 过窄**：当前 `BossConfigs.BossCfgRow` 只暴露 id/type/scene/x/y/layers/hurt/tired，未覆盖 boss_name/lv/icon/open_lv/max_lv/reborn/drop_lv/ratio_goods/arrow_num/peace、ClientBossBg、VIP/费用等。应由根任务协调配置权威层增加只读字段，页面不得猜名、猜等级或覆盖 Prefab 文案。本轮已移除“Boss+id/场景+id”等无权威占位覆盖。
3. **跨模块闭包**：FirstBlood、Scene 玩家数/不可切场景/同层换目标、VIP卡/充值、Common 详情/Instruction、Guild help、Goods use、声音均只登记 blocker。
4. **写事务**：46003、46007、15302、20004、46045、40060、GoodsUse 必须用授权测试号从真实 UI 触发，验证正式回包、父页即时刷新、关闭重开与重进一致性。

## SoulCurrency 证据

- `36240001` 不是推测：老端 `h5/src/bossField/BossFieldSoulShopView.ts` 直接以该 type id 读取战魂数量并绑定货币变化；老端 `BossFieldEnterView.ts` 亦以 `36240001` 检查战魂商店红点。
- Unity 本轮只通过 `BagModel.Instance.GetTypeGoodsNum(36240001)` 读取并显示，不修改 Goods/Bag；同账号数量和图标仍为 `needs-runtime-verify`。
- buff item `37110001`、疲劳 item `38160001` 同样来自老端对应 View 常量，但写/详情闭包未接，因此保持 blocked。

## 风险与真实验收

- 两类列表都需 `GraphicRaycaster→PointerClick/Drag` 验证 tab、首/中/末 Boss、裁剪和末项；当前静态列表不足，不可写 done。
- 模型必须在相机 Render 后保存 RT 非透明像素；特效需同 Handle 两时间点像素差；Failure/Relive 声音需打开/关闭生命周期。
- SoulShop 需至少覆盖不限购、日/周/终生限购、未售罄/售罄、货币足/不足、15302成功/失败、详情和重开。
- 最终整页需绑定源码/dirty 指纹、Player/catalog 哈希、两档 viewport 和同账号单会话 old H5→Unity Web 报告。

## 本路线独占写文件

- `Assets/Scripts/Module/Core/Boss/Views/BossField/**`
- `Assets/Prefabs/UI/BossField/BossFieldModule.prefab`
- `Assets/Prefabs/UI/BossField/BossFieldRewardItem.prefab`
- `output/ui_route_audit/2026-08-09_boss_ui_static_island/boss-field/**`

`BossController.cs`、`BossModel.cs`、`BossConfigs.cs` 保持未改。需要扩展共享 API 时由根任务统一写者协调。
