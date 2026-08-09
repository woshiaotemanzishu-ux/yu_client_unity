# Boss 主入口 / 战斗面板静态审计

审计日：2026-08-09。范围仅为 Boss 主入口与 Boss 场景 HUD；未启动或控制 Unity、浏览器、前台程序，未执行账号写事务。静态实现不等于 Unity/Web/老 H5 真实验收，全部叶状态见 `results-static.json`，无 `done`。

## 1. 映射

| Unity 资产 / 代码 | 老端 TypeScript | 老端 scene/json | 数据与配置 |
|---|---|---|---|
| `Assets/Prefabs/UI/Boss/BossModule.prefab` 内 `BossFightSceneView` | `h5/src/boss/BossFightSceneView.ts` | `cdn/resource/game/boss/BossFightSceneView.scene/.json` | `config_boss_cfg`、`config_boss_type`、`config_eudemons_boss_cfg`；协议 20201~20205、46019/46022、46040 等 |
| 同 Prefab 内 `BossDamageItem` | `BossDamageItem.ts` | `BossDamageItem.scene/.json` | 场景实时伤害、46019 前三名、46022 自身；老端最终列表还会合并 assist/instance 数据 |
| 同 Prefab 内 `BossDamageSubItem` | `BossDamageSubItem.ts` | `BossDamageSubItem.scene/.json` | 名次、角色名、伤害/占比显示 |
| 同 Prefab 内 `BossWayFindingItem/SubItem/WayTaskItem` | `BossWayFindingItem.ts`、`BossWayFindingSubItem.ts`、`BossWayTaskItem.ts` | 同名 scene/json | Boss 列表、任务、体力、场景实体/坐标/寻路 |
| 同 Prefab 内 `BossTargetPanel/Item` 与 `Assets/Prefabs/UI/Boss/BossTargetItem.prefab` | `BossTargetPanel.ts`、`BossTargetPanelItem.ts`、`BossTargetItem.ts`、`BossTargetSubItem.ts` | 同名 scene/json | 实时场景目标、归属、血量、攻/受击特效 |
| 同 Prefab 内 `BossRebornTipsView` | `BossRebornTipsView.ts` | `BossRebornTipsView.scene/.json` | 复活关注/前往；老端实际附近复活入口另开 `BossRebornView.ts` |
| 同 Prefab 内 `BossDropRecordView` 与 `Assets/Prefabs/UI/Boss/BossDropRecordItem.prefab` | `BossDropRecordView.ts`、`BossDropRecordItem.ts` | 同名 scene/json | 46002 掉落日志、物品/装备详情共享组件 |
| 同 Prefab 内 `BossHelpPanel` | `BossHelpPanel.ts`、`BossHelpItem.ts` | Suitboss 转换来源 | 40408 显隐已有业务 View；实时助战伤害仍依赖场景 instance 数据 |
| Unity 尚无独立 BossEnterView Bind/Prefab | `BossEnterView.ts` | 老端公共 Boss 主壳/页签体系 | 页签映射到 Field、Abyss/Domain、Personal、Mystery 等独立模块；Unity 主入口宿主属于 MainUI 禁区 |

当前已有 Prefab：

- `Assets/Prefabs/UI/Boss/BossModule.prefab`
- `Assets/Prefabs/UI/Boss/BossTargetItem.prefab`
- `Assets/Prefabs/UI/Boss/BossDropRecordItem.prefab`

原有页面专属 C#：`BossController.cs`、`BossModel.cs`、`BossConfigs.cs`、`KfBoss*`，以及 `Views/BossHelpPanelView.cs`、`Views/BossHelpItemView.cs`。其余 Boss 页面原先只有 `Assets/Scripts/Generated/UI/Boss/*Bind.cs`。

## 2. 本轮最小静态接管

- `BossFightSceneView`：按现有 Controller 的只读能力拉取免战信息/结束时间、死亡减益、Boss 血量，并按老端 5 秒节奏继续查询 46040；显示免战倒计时；点击伤害榜入口打开内嵌榜单。
- `BossDamageItemView`：监听 `EVT_BOSS_DAMAGE_RANK_UPDATE`，消费 `DamageTop3`/`DamageSelf`，管理模板克隆、显隐和关闭。
- `BossDamageSubItemView`：落地名次、角色名与当前协议模型里的绝对伤害。
- `BossModule.prefab` 仅替换上述三个既有 Bind 组件的 `m_Script` GUID 与 `m_EditorClassIdentifier`；序列化字段、层级、坐标、资源均未改。

未接的按钮保持无伪行为：退出没有绕过确认框直发 46004，免战按钮没有假装打开缺失的跨模块页，采集/选怪/寻路/复活没有用静态日志代替真实场景动作。

## 3. 完整控件、条件与叶风险

主入口页：打开、Field/Domain/Personal/Mystery 四类页签、掉落记录、说明、关闭。入口和关闭宿主在 MainUI 禁区；四页签分别属于其他 Boss 文件岛；掉落详情跨 Common/Equipment；因此均 `blocked`。

战斗 HUD：

- 伤害榜：入口/收起、前三列表、自身行已静态接管，均 `needs-runtime-verify`；说明与公会求助是跨模块/账号写事务，`blocked`。
- 寻路任务：Boss/任务页签、Boss 列表滚动与选择、任务列表滚动与选择、说明、体力增加、体力进度。均因缺 `BossSceneManager` 等价层或跨 Task/Common/账号写事务而 `blocked`。
- 采集：普通、稀有、水晶、秘境宝箱、秘境秘宝。均依赖场景实体、坐标、寻路与采集事务，`blocked`。
- 目标列表：滚动、逐项选怪、归属状态、攻/受击特效。均依赖实时场景对象、战斗事件与双时点像素证据，`blocked`。
- 条件块：场景状态、圣域普通/稀有/水晶计数、特殊 Boss、复活、助战、跨服/开/守按钮。均缺对应运行时或属于跨岛，`blocked`。
- 退出：老端先按 Boss 类型分流并走确认弹窗；直接调用 `LeaveBoss` 会改变场景且丢确认语义，故 `blocked`。
- 免战：20201/20203 只读刷新与倒计时为 `needs-runtime-verify`；打开 `BossWarFreeView`/使用保护仍 `blocked`。
- Boss 大血条：46040 五秒查询已接，仍缺场景 `hiter_vo` 消费和真实玩家可见像素，`needs-runtime-verify`。

列表必须在真实运行补验 `ScrollRect -> Viewport(RectMask2D) -> Content`、拖动后位移/裁切、末项可达、条目点击后的准确弹窗身份。所有条件块须覆盖显示/隐藏两态，动态特效须有同一实例两个时点像素差。

## 4. 静态确定缺陷与最小后续方案

1. **主入口壳缺失**：Unity 没有 BossEnterView 业务 Bind/Prefab，且 MainUI 入口禁止本岛写。后续由 MainUI 所有者建立入口到各独立 Boss 模块的只读路由，不能在 BossModule 内复制主壳。
2. **大部分内嵌页只有 Generated Bind**：WayFinding、Target、DropRecord、RebornTips 都无业务消费者。按本 manifest 分页增量接管既有组件 GUID；不要重转 BossModule。
3. **场景运行时断链**：老端 1994 行 `BossFightSceneView.ts` 大量依赖 `BossSceneManager`、实时怪物、坐标、导航、采集、攻击和 instance_id。最小正确修复是先提供场景只读适配接口，再由各 View 消费；不能从配置猜当前场景对象。
4. **伤害口径不完整**：Unity 当前模型只有 46019 前三名和 46022 自身绝对伤害；老端榜单会按当前 Boss 总伤害、assist 关系计算百分比和完整排序。当前 UI 仅显示确有的数据，百分比/奖牌 Sprite 必须在场景适配层到位后按真实老端校准。
5. **复活页面身份缺口**：现有 `BossRebornTipsViewBind` 不能替代老端 `BossRebornView.ts`（附近死亡 Boss 复活）。缺 Prefab 的后者才适用未来 `convert-module`；现有 Tips 页适用 `fix-view`。
6. **掉落详情跨共享组件**：DropRecord 条目最终打开物品/装备详情，必须引用现有 Common/Equipment 共享 Prefab/View；本岛不得复制详情树。
7. **Boss 血量仅有请求**：46040 轮询存在不等于大血条出帧。后续需补响应到当前场景 Boss `hiter_vo`/血条消费者，再以真实场景生命周期验证。

## 5. 建议独占写文件清单

本轮实际独占：

- `Assets/Scripts/Module/Core/Boss/Views/BossMain/BossMainFlow.cs(.meta)`
- `Assets/Scripts/Module/Core/Boss/Views/BossMain/BossFightSceneView.cs(.meta)`
- `Assets/Scripts/Module/Core/Boss/Views/BossMain/BossDamageItemView.cs(.meta)`
- `Assets/Scripts/Module/Core/Boss/Views/BossMain/BossDamageSubItemView.cs(.meta)`
- `Assets/Prefabs/UI/Boss/BossModule.prefab`（仅三个组件身份）
- 本目录下 `route-manifest.json`、`results-static.json`、`audit-summary.md` 与独立验证 csproj/artifacts

后续若继续本岛，建议仍只独占 `Assets/Scripts/Module/Core/Boss/Views/BossMain/` 内新增业务 View，以及上述三个 Boss 专属 Prefab 中确有绑定依据的组件身份；不改现有 Controller/Model/Configs，也不触碰 MainUI/Common/BaseWindow/Generated/Proto/ClientConfigSync/Addressables/Docs/AGENTS。

## 6. 验证边界

- schema 6：`route-manifest.json` 仅拓扑，`results-static.json` 仅叶状态；不手写正式 `route-ledger.json`。
- 静态编译：使用本目录 `BossMain.Static.Isolated.csproj`，输出与中间文件只落本目录 `artifacts/`，不使用共享 Temp/Core 构建。
- 未执行：Unity 编译/PlayMode、WebGL 构建、真实 Prefab 点击、同账号老 H5 对比、账号写事务、场景重进与动态像素门禁。
- 文档未更新：本轮用户明确禁止写 `Docs/AGENTS`，故审计证据仅落授权的 output 目录。
