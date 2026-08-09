# Skill/技能完整路线静态审计（2026-08-09）

结论：完成了实现前的全路线静态闭包和一项 Skill 专属确定性修复；没有启动 Unity、浏览器或账号写事务。本路线不是 UI 精修完成，所有叶子只能是 `needs-runtime-verify` 或 `blocked`。

## 页面拓扑与事实边界

- 固定 HUD 使用 `Assets/Prefabs/UI/MainUI/Regions/HudSkillBar.prefab`，包含四个普通槽、自动战斗、伙伴锁、神技项及现成 `CdMaskImage/CdCountdownLabel`；它属于 MainUI 文件岛，本轮只读。
- 角色技能窗口复用 `Assets/Prefabs/UI/Role/RoleModule.prefab`，由 `RoleFlow.OpenSkillAsync` 套公共窗，当前老端和 Unity 都是“主动技能 / 被动技能 / 天赋”三页签；已有 Prefab，不允许重新转换。
- 老端 `ForbiddenSkillView`、`21101..21104` 远古奥术页签已从当前可达 tab 注释；`21003..21005` 服务端 handler 已注释。两组都不能恢复成假入口。
- 被动技能页当前老端的无条件 `else if (1)` 隐藏材料和升级按钮；Unity 只读展示与当前事实一致，不能仅因 `21001` API 存在就开放升级。
- 天赋四转开放，含系别页签、树、详情、条件、消耗、学习与重置。`21011` 是可恢复写；`21012` 会消费/自动购买重置道具，属于未授权破坏性事务。

## 本轮 Skill 文件岛修复

`SkillController.OnGameStart` 为避免 Addressables 配置阻塞，先发 `21002/13007` 再等待 `config_skill/ConfigSkillUI`。首包若先到，`SkillManager` 会先用 21002 全表回退铺槽；此前配置加载完成后没有重算，整次登录会保留回退顺序直到下一次 21002。

本轮新增 `SkillManager.RefreshShortcutListAfterConfigLoad()`，在配置加载完成后只重算派生 `ShortcutList` 并发 `EVT_SKILL_LIST_UPDATED`；不改 21002 权威等级、不触发新技能浮条、不发写协议。同步修正 `SkillController` 已过时的“20001 尚未实现”注释：当前真实 20001 已由 `SceneCombat -> FightController` 发出，但完整老端 `CanAttack` 姿态/场景禁用矩阵仍未闭合。

修改文件仅：

- `Assets/Scripts/Module/Core/Skill/SkillManager.cs`
- `Assets/Scripts/Module/Core/Skill/SkillController.cs`

## 控件/组件依赖与明确 blocker

- HUD CD：Prefab 已有静态 CD 节点，但 `MainUISkillItemBind` 未绑定它们，`MainUISkillItem` 仍运行时创建另一套 `CdMask/CdLabel`。这违反“人工 Prefab 为视觉事实源”的长期方向；修复需 MainUI + Generated 共同变更，本轮禁改，只登记。
- HUD 四槽：普通场景静态代码存在；海域争霸/神祇替换分支、全局快捷技能禁用、经验副本禁止切 AutoFight 尚未等价。
- 伙伴技能：需要 Partner/PartnerAwake 的开放、技能、场景隐藏、14204 与 `PartnerAwakeShowView`；当前 Unity 只保留日志/路由边界。
- 神技：`MainUISkillItemGod` 尚缺 44011、搬砖/变身/CD 拦截、双遮罩、持续计时和变身特效，不能用普通四槽代替。
- 主动页：六槽、选择、锁、当前/下级说明、满级态静态存在；完整任务锁、突破技能替换及老端运行矩形未验。
- 解锁浮条：`SkillShowView` 缺真实图标和 300ms 上浮 + 800ms 停留 + 300ms 淡出表现。
- 天赋详情：`InnateInfoItem` 没有生成 Bind，且两个 ScrollRect 仍有运行时根查找兜底；需 Role/Generated/Prefab 文件岛修复并做真实拖动/末项可达。
- AutoFight 顺序：13007 已存 `pos/type/skill_id/is_auto`，但当前实际消费者走 `GetNextCombatSkill()`，没有完整复刻老端快捷栏/默认 `auto_fight_order`、经验副本门与特殊场景分支；跨 AutoFight/MainUI，未半修。
- 技能觉醒：MainStronger id 10001 最终进入 Task `DoTask`，不是 SkillSubView 第四页签；Task/MainStronger 只登记依赖。
- CD：本地 `ResetSkill/GetCdLeftMs`、服务端 20018 清除和 20027 截止时间接入静态存在；必须以两个时间点像素、点击拦截和归零清理验证。
- 动作/特效/声音：`SkillMovieConfigs` 读取动作、粒子和声音参数；`SceneCombat` 消费 `sound_res/sound_start_time/hiter` 并调用 `AudioManager.PlaySkill`。页面没有专属 BGM/升级成功音；点击声与天赋升级大特效仍需真实链验证。

## 协议与刷新契约

| 协议 | 静态状态 | 当前页契约 |
|---|---|---|
| 21002 | 已注册 | 权威技能总表，立即刷新列表；配置后重算竞态已修 |
| 13007 | 已注册 | 快捷栏全量，成功回包后刷新 HUD；自动选技顺序仍跨岛 |
| 21001 | 已注册 | 成功后服务端推 21002；当前被动页按钮应隐藏 |
| 21010/21011 | 已注册 | 天赋全量/学习；21011 成功后本端重拉 21010 |
| 21012 | 已注册 | 天赋重置，可能自动购买并消耗道具；未授权不执行 |
| 13008/13010 | 已注册 | 保存/交换后重拉 13007；当前老端无可达 UI 触发源 |
| 12093/18401 | 已注册 | 职业技能 buff / 模块加成只读数据 |
| 21003..21005 | 不实现 | 当前服务端 handler 已注释 |
| 21101..21104 | 不开放 | 当前角色技能页签不可达 |
| 44011/14204 | 跨岛 | 神技/伙伴依赖，不属于普通角色技能页协议 |

## 运行验证用例（本轮未执行）

1. 同账号冷开角色页，点“技能”，逐走主动、被动、天赋全部控件，关闭后回角色原页签；再暖开并记录耗时。
2. 主动页逐点六槽，覆盖未开放、任务锁、已学、满级、特殊心法和突破显示；滚动当前/下级长文案。
3. 被动页拖到末项，逐项开详情，确认材料/升级区在当前版本始终隐藏。
4. 四转前后验证天赋门；逐点四系、全部树节点、条件不足/满级/点数不足。21011 只在授权可恢复配方下执行；21012 默认 blocked。
5. HUD 覆盖 0/1/4 技能、锁定、托管、普通/临时自动、经验副本、伙伴有/无、神技有/无；CD 保存至少两时点动态像素与归零清理。
6. 手动技能真实点击必须验证 20001、成功/失败回包、即时 HP/CD/动作/粒子/声音与关闭重开；完整 CanAttack 场景矩阵单独覆盖。
7. 在两档 viewport 上保存 old/unity/overlay/diff，绑定 Player/catalog、源码/dirty 指纹和同会话顺序运行。

## 本轮边界

- 未改 MainUI、Role、Task、PartnerAwake、GodSkill、AutoBrush、Common、Generated、Addressables、Docs。
- 未启动 Unity/浏览器，未构建 WebGL，未点击任何账号写事务。
- 当前结果只代表“静态准备完成”；`route-ledger.json` 根状态必须保持非 done。
