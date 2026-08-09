# DungeonHeart 静态 UI 路线审计

## 完成边界

- 已按 `audit-game-ui-route` 完整枚举入口页、战斗侧栏、条件状态、事务及返回链；已有可编辑 `DungeonHeartModule.prefab`，按 `fix-view` 增量处理，没有重转模块。
- 未启动 Unity、浏览器或任何前台程序，未登录账号，未执行 `61001`、副本结算、退出或任务推进等账号写事务。
- `route-manifest.json` 仅保存 schema 6 拓扑，`results-static.json` 仅保存叶结果；所有叶均为 `blocked` 或 `needs-runtime-verify`，没有 `done`。

## 页面与 Unity 映射

| 老端页面/控件 | 当前 Unity | 结论 |
|---|---|---|
| `h5/src/dungeonHeart/DungeonHeartEnterView.ts` + 同名 JSON | `Assets/Prefabs/UI/DungeonHeart/DungeonHeartModule.prefab/DungeonHeartEnterView` | 现有 Prefab 已由 `DungeonHeartEnterView` 业务 `.meta` GUID 接管，18 个生成 Bind 字段均有序列化引用。 |
| `h5/src/dungeonHeart/DungeonHeartFightSceneItem.ts` + 同名 JSON | `Assets/Prefabs/UI/DungeonCommon/DungeonCommonModule.prefab/DungeonHeartFightSceneItem` | 仅有 Generated Bind，且当前 Dungeon 业务未找到模板消费；该 Prefab/宿主属于本轮禁写跨岛，只登记 blocker。 |
| 老端 `BaseDungeonModel`/`TaskModel` 的 Heart/FinDungeon 分支 | Unity Task/Dungeon 路由 | Task 为禁写跨岛；本岛只提供 `DungeonHeartFlow.Open(int dungeonId)`，不猜完成任务或自动打开条件。 |

入口没有页面专属列表、页签、输入框或弹窗。老端背景遮罩点击不关闭；显式返回只有 `closeBtn`。点击进入会先发送 `61001(taskVo.id)` 再关闭入口，随后战斗侧栏由 `DungeonCommon` 宿主创建。

## 控件、条件与返回链

- 入口视觉：Activity 遮罩/动态 `uihmzc_007` 背景、`I_01..I_06` 副本编号、Boss 等级名与模型、击败目标、当前/推荐战力、职业技能图标/名称/描述、完成挑战奖励提示、`ui_anniu_6` 按钮特效。
- 入口交互：关闭；进入前必须来自指定已完成任务且为 `config_dungeon.type=31`。Unity 本岛只验证显式 id 与类型，任务完成态由 Task 跨岛提供。
- 战斗侧栏：挑战标题、Boss 名、限时击杀条件、通关奖励、职业技能、折叠箭头、整体拖动、场景创建/删除清理。
- 事务叶：`61001` 成败、入口即时关闭、结算结果、技能实际学会、Task 父页即时刷新、关闭重开/退出重进一致性、战斗退出。
- 返回链：入口 `closeBtn → Hide`；进入 `61001 → Hide → 场景/HUD`；战斗退出/结算回到父 Task 状态的链路均为跨岛 blocker。

## 静态确定实现

- `DungeonHeartFlow` 只加载现有 `DungeonHeartModule.prefab` 到 `Popup`，只查找 Prefab 已接管的业务组件；缺组件直接报错，不使用 `AddComponent` 补空脚本。
- `DungeonHeartEnterView` 仅接受显式 `DungeonId`，加载后严格要求 `DungeonConfigs.GetType(id)==31`，否则保持进入按钮隐藏。
- 老端权威文件 `E:/GitProject/yu_client/cdn/resource/config/server/config_dungeon_ui_content.json` 中，键 `31001..31006` 的字段 `5` 分别为同值 `31001..31006`（字段 `2` 均为“限时击败镇魔首领”）；代码只枚举这六条 Boss 映射，不对其它 id 外推。
- 目标文案由映射后的 `config_mon.name` 生成；职业技能从 `config_dungeon_learn_skill` 按 `dun_id+career` 取首项，再从 `config_skill` 取一级图标/名称/描述。
- 编号只允许 `I_01..I_06`，背景使用老端明确的 `dungeonHeart/uihmzc_007`，进入按钮调用已有 `DungeonController.Enter` 正式 `61001`。
- 每次 Show 先清空动态状态，异步渲染用 epoch 与 `IsShown` 拒绝关闭/复开后的后到结果。

## 静态缺口与最小后续修复

1. **Boss 等级和模型**：老端 `bossName` 包含 `config_mon.lv`，模型还依赖 `ConfigDungeonClient.show_effect[10]` 的 position/scale/actions。当前公共配置 API 未暴露这些字段；本岛隐藏 `bossName/bossCon`，不得以副本序号猜等级或以 `Renderer` 存在冒充出帧。后续应由权威配置层提供字段，再补模型相机 RT 两时点证据。
2. **推荐战力**：老端用 `config_dungeon.recommend_power` 与角色战力组合富文本；当前 `DungeonConfigs` 未暴露该字段。本岛隐藏 `monsterText2`，不保留原 Prefab 的误导性“完成本次挑战”占位。
3. **按钮特效**：老端明确调用 `ui_anniu_6`、parent `_box_eff`、position `(0,-1.35)`、scale `1`、loop。当前模块没有已证明的 Unity 资源消费链，宿主安全隐藏；后续需核对资源、归属、两帧像素差和关闭清理。
4. **Task 入口**：应由 Task 唯一写者按真实 FinDungeon 状态调用 `DungeonHeartFlow.Open(taskVo.id)`；本轮禁止触碰 Task，不把“能手动 Open”写成入口已完成。
5. **战斗 HUD**：`DungeonHeartFightSceneItem` 嵌在 `DungeonCommonModule.prefab`，当前仍是 Generated Bind 且未找到业务宿主消费。最小修复需由 DungeonCommon 唯一写者新增业务 subclass、精准切换 Prefab GUID/EditorClassIdentifier，并在宿主按真实 Heart dun type 走 `Show/Hide`；本岛不得改该 Prefab。
6. **真实事务与返回**：使用授权测试号从真实 Task UI 进入，覆盖 `61001` 成功/失败、侧栏折叠/拖动、结算、技能学会、Task 即时刷新、关闭重开和退出重进；本轮全部保持 blocked。

## 真实验收要求

- 入口在 350ms、1000ms、ready 保存资源状态；两档 viewport 对比 old H5/Unity/overlay/diff，并绑定源码 dirty、Player、catalog 哈希。
- close/enter 必须由真实 `GraphicRaycaster→PointerClick` 触发；背景遮罩不得误关页面。
- Boss 模型只有专用相机 Render 后 RT 非透明像素达到门槛才算 ready；按钮特效需同一 Handle 两时间点像素差。
- 战斗侧栏折叠需验证 x=`0/-231`、0.3s、箭头 `90/270`，拖动后位置与退出清理；静态 Transform/组件存在不能完成这些叶。
- 写事务必须核对正式回包、父页即时状态和关闭重开，禁止用账号重登后的最终状态替代即时刷新。

## 本路线独占写文件

- `Assets/Scripts/Module/Core/DungeonHeart/**`
- `Assets/Prefabs/UI/DungeonHeart/**`（本轮无需修改）
- `output/ui_route_audit/2026-08-09_dungeon_ui_static_islands/dungeon-heart/**`

本轮未改共享架构、公共配置或禁区，且 Docs/AGENTS 明确禁止触碰，因此不触发项目文档更新。
