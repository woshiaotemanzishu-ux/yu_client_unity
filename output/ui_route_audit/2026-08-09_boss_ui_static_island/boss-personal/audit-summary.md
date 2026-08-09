# BossPersonal 静态 UI 路线审计

## 完成边界

- 已按 `audit-game-ui-route` 枚举页面、控件、条件、弹窗和列表叶；已有 Prefab，因此按 `fix-view` 增量接管，没有重转模块。
- 未启动 Unity、浏览器或任何前台程序，未登录账号，未执行 61001 等写事务；本报告不能替代真实 Unity Web/H5 同账号证据。
- schema 6 拓扑在 `route-manifest.json`，静态结果在 `results-static.json`；没有手写正式 `route-ledger.json`，没有任何 `done`。

## 老端到 Unity 映射

| 老端 | Unity Prefab / Bind | 本轮 runtime subclass |
|---|---|---|
| `bossPersonal/BossPersonalEnterView.ts` | `BossPersonalModule.prefab/BossPersonalEnterView` / `BossPersonalEnterViewBind` | `BossPersonalEnterView` |
| `bossPersonal/BossPersonalItem.ts` | `_tpl_BossPersonalItem` / `BossPersonalItemBind` | `BossPersonalItem` |
| `bossPersonal/BossPersonalAlert.ts` | `BossPersonalAlert` / `BossPersonalAlertBind` | `BossPersonalAlert` |
| `bossPersonal/BossVipAddView.ts` | `BossVipAddView` / `BossVipAddViewBind` | `BossVipAddView` |

对应 Prefab 中四个 `m_Script` 已从 Generated Bind GUID 精确切换到上述 subclass GUID；继承字段继续复用原序列化引用。Generated 只读。

## 静态实现

- `BossPersonalFlow` 懒加载合并 Prefab，区分主页面和叠加弹窗，不以 `SetActive` 代替 `Show/Hide`。
- `BossPersonalEnterView` 打开请求 `61020`，订阅 `EVT_DUNGEON_UPDATE`，从 `DungeonModel.TYPE_VIP_PERSON_BOSS` 构建真实列表；选择、次数文本、滚动箭头、加次弹窗和正式 `DungeonController.Enter(61001)` 入口已落位；首挑/红点、VIP/商店跨模块状态仅登记 blocker。
- `BossPersonalItem` 由真实模板克隆并调用 `Show`，逐项选择与 selected 状态已接；first/red 缺少 `ex_data key=10/first_flag` 等价权威字段，当前安全隐藏并保持 `blocked`。
- `BossPersonalAlert` 的 close/cancel/confirm 回调与文案落位已接；`BossVipAddView` 的关闭已接。VIP/SVIP 分支依赖老端 `vip_flag/card` 权威语义，当前接口缺失时两组动态内容安全隐藏，不以 `VipLevel` 猜测。

## 静态确定缺口与最小后续修复

1. Boss 主入口尚未注册 `BossPersonalFlow.Open`。该文件由并行 Boss 主路线统一接线，避免共享写冲突。
2. `config_dungeon_ui_content`、`config_num_by_vip`、`config_num_by_vip_info` 和 `ConfigDungeonClient` 未被当前 `DungeonConfigs` 暴露，导致怪物模型/头像/等级、职业奖励、票券、总次数、VIP卡条件及首挑精确语义不能安全复刻。最小修复应由配置权威层提供只读 API，再在本页面消费；不得页面猜 ID。
3. 掉落记录弹窗属于 Boss 主路线；装备/票券详情属于 Common；VIP卡、充值、Shop type=17 属跨模块消费者。本岛只登记 blocker，不修改禁区或跨岛文件。
4. 首挑/红点与 VIP/SVIP 均不做降级映射：缺老端 `ex_data key=10/first_flag`、`vip_flag/card` 等价权威接口时保持隐藏并显式 `blocked`。
5. 61001 已接正式 Controller，但必须在授权账号上验证：首次免费、票券充足/不足、VIP跳转、场景限制、成功/失败回包、父页即时刷新、关闭重开和退出副本返回。

## 风险与证据门禁

- 交互风险：挑战会消耗次数/票券并切场景；本轮未执行。
- 共享组件风险：EquipmentItem/BaseAwardItem 详情身份、层级和遮罩需 Common 代表消费者证据。
- 动态视觉风险：怪物模型实际 Render、常驻特效、350ms/1000ms/ready、两时间点动态像素尚无证据。
- 列表风险：真实拖动、裁剪、末项可达和每项点击尚未运行。
- 收口要求：当前源码/dirty 指纹、Player/catalog 哈希、两档 viewport、单会话 old H5→Unity Web 顺序复走和 cold/warm 证据齐全后，方可对叶闸提交 true。

## 本路线独占写文件

- `Assets/Scripts/Module/Core/Boss/Views/BossPersonal/**`
- `Assets/Prefabs/UI/BossPersonal/BossPersonalModule.prefab`
- `output/ui_route_audit/2026-08-09_boss_ui_static_island/boss-personal/**`

`BossController.cs`、`BossModel.cs`、`BossConfigs.cs` 保持未改；若后续必须扩 API，应由根任务统一写者协调。
