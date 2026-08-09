# DungeonPartner 静态路线审计

## 结论边界

本轮只完成 DungeonPartner 页面岛的静态接管、只读协议刷新链和 schema 6 拓扑。未启动 Unity、浏览器或任何前台程序，未执行账号写事务，因此没有任何叶节点标记为 `done`。页面现有 Prefab 已存在，按 `fix-view` 增量接管，未运行转换器、未重建视觉树。

## 老端与 Unity 映射

| 路线/页面 | 老端权威源 | Unity Prefab / Bind | 本轮状态 |
|---|---|---|---|
| 伙伴副本主页面 | `E:/GitProject/yu_client/h5/src/dungeonPartner/DungeonPartnerView.ts` | `DungeonPartnerModule.prefab` / `DungeonPartnerViewBind` | 业务 subclass 接管；61105 只读刷新已接，章节列表受配置缺口阻塞 |
| 九关卡项 | `DungeonPartnerItem.ts` | 同 Prefab / `DungeonPartnerItemBind` | subclass 接管；点击语义入口保留，动态项与状态矩阵阻塞 |
| 星级宝箱浮层 | `DungeonPartnerStarView.ts` | 同 Prefab / `DungeonPartnerStarViewBind` | subclass 接管；61106、进度与状态显隐静态接线，61108 领取阻塞 |
| 阶段奖励详情 | `DungeonPartnerStageRewardView.ts` | 同 Prefab / `DungeonPartnerStageRewardViewBind` | subclass 接管；关闭与有老端直接证据的标题/提示已接，奖励列表阻塞 |
| 扫荡浮层/条目 | `DungeonPartnerSweepView.ts`、`DungeonPartnerSweepItem.ts` | 同 Prefab / `DungeonPartnerSweepViewBind`、`DungeonPartnerSweepItemBind` | subclass 接管；关闭/模板隐藏已接，章节列表、费用与 61109 阻塞 |
| 挑战弹窗/星级奖励条目 | `DungeonPartnerVsView.ts`、`DungeonPartnerVsItem.ts`、`DungeonPartnerVsRewardItem.ts` | `DungeonPartnerModule.prefab` + `DungeonPartnerVsRewardItem.prefab` | 3 个 subclass 接管；关闭/模板隐藏已接，配置、模型、61001 阻塞 |
| 首杀详情 | `DungeonPartnerFirstKillView.ts` | `DungeonPartnerModule.prefab` / `DungeonPartnerFirstKillViewBind` | subclass 接管；关闭/模板隐藏已接，FirstBlood 18802 阻塞 |
| 战斗 HUD | `DungeonPartnerFightSceneItem.ts`、`DungeonPartnerFightKillInfoItem.ts`、`DungeonPartnerFightStageInfoItem.ts` | 同 Prefab / 3 个 Generated Bind | BaseDungeon/战斗场景跨岛，保持 Generated，不宣称接线 |
| 战斗结算 | `DungeonPartnerResultView.ts` | 同 Prefab / `DungeonPartnerResultViewBind` | BaseDungeon 61002/结果生命周期跨岛，保持 Generated，不宣称接线 |

老端主入口证据位于 `E:/GitProject/yu_client/h5/src/dungeon/DungeonEnterView.ts`：`Index.DungeonPartner = 5` 映射 `dungeonPartner/DungeonPartnerView`。此文件只读，未改 Dungeon/DungeonEnter。

## 完整控件与条件枚举

- 主页面：61105/61106 只读快照、左翻页、右翻页、横向滑动、章节名称/说明、每章九关离散地图、星级宝箱入口、扫荡入口、最大收益奖励条、星/扫荡/左右章/首杀红点、外层返回。
- 关卡项：伙伴名称/碎片、伙伴或怪物模型、怪物图标、VS 点击区、锁定、前置关卡、等级条件、1/2/3 星、已完成、首杀者、首杀可领取与红点。
- 挑战弹窗：关闭、关卡标题、三档首次星级奖励列表、怪物模型、等级条件、推荐战力、挑战按钮；挑战还依赖组队检查、场景进入与 BaseDungeon 61001。
- 星级浮层：总星进度、三个奖励箱、已领遮罩、可领红点、透明背景关闭；每个箱根据 `status` 分支到 61108 领取或阶段奖励详情。
- 阶段奖励详情：关闭、分数标题、未领取提示、奖励列表、弹窗遮罩/返回链。
- 扫荡浮层：关闭、VIP 剩余次数、货币图标/数量、规则提示、纵向章节列表；每项含章节标题、星级百分比、按星数缩放的奖励列表、扫荡按钮、确认 Alert 与 61109。
- 战斗 HUD：降星倒计时/进度、当前星级、61110 怪物击杀进度列表、三档首次星级奖励列表、场景 session 与晚包隔离。
- 结果页：奖励列表、1/2/3 星、手动退出、三秒自动退出、结果音、61002 离场、返回 DungeonEnter/DungeonPartner。

## 静态确定实现

- `DungeonPartnerModel` 新增当前章节、总星数计算以及 61105/61106 快照事件；Controller 现有安全解析回调可即时驱动已打开页面刷新。
- 主页面、Star、Sweep、Vs、FirstKill、StageReward 及 3 类条目共 10 个业务 subclass 已创建，均有独立 `.meta`；所有对应 Prefab `m_Script` 和 `m_EditorClassIdentifier` 已精确切换。
- 所有运行时列表模板在各自业务 View 初始化时隐藏，避免原模板与动态克隆重复。由于配置/共享组件闭包缺失，本轮不克隆假项。
- Star 的 27 分母来自老端 `DungeonPartnerStarView.ts` 的 `total_score / 27 * 420`；Sweep 条目同样来自 `DungeonPartnerSweepItem.ts` 的 `cur_score / 27 * 100`，不是猜测。
- StageReward 的“`score` 星奖励 / 本局总星数达到 `score` 星即可领取”和 VsItem 的“首次 `star` 星通关”均来自老端直接赋值；没有权威来源的名称、条件、战力和奖励未覆盖 Prefab。

## 明确 blocker 与风险

- Unity 当前未找到老端 `config_partner_dun_chapter`/`ConfigDungeonClient` 的完整对应闭包；只看到不足以恢复语义的 `config_dungeon.json`。章节数量、关卡 ID、名称、离散槽、条件、奖励、推荐战力、伙伴/怪物模型均不得猜。
- `Schemas/ProtocolCoverage/hard_negative_constraints.json` 对 61108 明确禁止缺权威 claim 状态与奖励闭环的孤立发送器；对 61109 明确要求真实费用、次数、VIP、奖励与失败分支；对 61110 明确要求 dungeon/session 身份和晚包隔离。本轮三者均未注册/发送。
- 61001/61002 属 BaseDungeon 场景事务，18802 属 FirstBlood，Role/VIP/Currency、Common EquipmentItem/CommonRewardItem、共享 UI 模型渲染、外层 DungeonEnter/BaseWindow 均是跨岛依赖，只登记 blocker。
- 4 个战斗/结果 Bind 保持 Generated 是有意边界，不是遗漏；页面岛没有战斗 session 所有权，接管反而会制造失真的半链路。
- 未执行 Unity/Web、同账号协议回包、真实拖动、弹窗层级、模型 RenderTexture、两档 viewport、cold/warm、即时刷新和关闭重开；静态编译/Prefab GUID 只能证明代码与序列化结构自洽。

## 独占文件清单

- `Assets/Scripts/Module/Core/DungeonPartner/DungeonPartnerModel.cs`
- `Assets/Scripts/Module/Core/DungeonPartner/Views.meta`
- `Assets/Scripts/Module/Core/DungeonPartner/Views/*.cs` 与对应 `.meta`
- `Assets/Prefabs/UI/DungeonPartner/DungeonPartnerModule.prefab`
- `Assets/Prefabs/UI/DungeonPartner/DungeonPartnerVsRewardItem.prefab`
- `output/ui_route_audit/2026-08-09_dungeon_ui_static_islands/dungeon-partner/**`

本轮不触发文档更新：用户明确禁止修改 Docs/AGENTS，且成果是本路线临时静态审计与页面专属接管，不改变公共架构或共享流水线。
