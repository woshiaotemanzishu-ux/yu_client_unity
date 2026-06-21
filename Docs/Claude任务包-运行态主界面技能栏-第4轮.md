# Claude任务包-运行态主界面技能栏-第4轮
日期: 2026-06-21

目标: 继续以老 Laya 客户端运行时 `http://127.0.0.1:8090/index.html` 为唯一可见真相, 从主界面 `MainUISkillView` 开始对齐技能栏。第3轮已经证明主线 NPC 对话端到端真连跑通, 本轮必须把首屏玩家可见的技能 4 槽、自动战斗按钮、伙伴技能锁的运行态差异记录清楚, 并让 Unity 至少显示真实技能数据, 不再停留在空模板/TODO。

## 必读

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/LayaUI转换流水线.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/Shenxiao主界面运行时填充清单.md`
- `Docs/RuntimeCompare/MainUI-Bottom-第1轮.md`
- `Docs/RuntimeCompare/MainUI-TaskTeam-第2轮.md`
- `Docs/RuntimeCompare/MainQuest-NpcDialogue-第3轮.md`
- 本任务包

## 总原则

1. **老端结论必须来自运行时。** 不允许用 Laya `.scene` 或源码推断代替浏览器运行页截图、`Laya.stage` 节点树、console 协议日志。
2. **Unity 必须尽量走真实进游戏链路。** 优先真连服务端登录/进场景/GAME_START 后观察 MainUI; 如环境无法真连, 必须写清楚 blocker, 不用 fake UI 宣称完成。
3. **技能数据不能造假。** 技能列表来自真实协议 `21002`、快捷栏来自真实协议 `13007`, 图标/名称/等级来自真实 `config_skill`/`ConfigSkillUI` 或已生成配置。禁止为了截图手工填 4 个假技能。
4. **本轮只做主界面技能栏最小可见闭环。** 完整战斗 AI、技能释放命中、技能升级页、天赋/远古奥术、伙伴系统深水区只记录差异, 不扩散编码。
5. **游戏本体竖屏。** Web 横屏页面只做锚点参考; 对比重点是技能栏相对位置、4 槽内容、锁态/CD/点击行为、自动战斗按钮态。
6. **不要碰无关脏文件。** 当前已知不要改: `Assets/_App/Fonts/DFPYuanW7 SDF.asset`、`Assets/_App/Fonts/FZYHJW SDF.asset`、`.playwright-cli/`、`output/`。

## 已知前置状态

- 第1轮提交: `ef2cd2799` 底部功能栏对齐。
- 第2轮提交: `ce92ab90a` 任务区对齐。
- 第3轮提交: `a856aad25` 主线 NPC 对话真连端到端验收报告。
- 第3轮真连证据:
  - `sceneId=10000`, `NpcCount=34`, `NPC 100101 云霄月华 pos=(4678,1574)`。
  - 点击主线任务可 `MoveToNpc -> 12101/12102 -> 30004 -> 100010→100020`。
  - 后续真连测试需注意: `RunCommand` 编译触发域重载会断 WebSocket, 完整真连链路必须在单条命令内跑完。
- Unity 当前技能栏代码是降级态:
  - `Assets/Scripts/Module/Core/MainUI/Views/MainUISkillView.cs` 只设置自动战斗默认图、隐藏模板, 未接真实 `SkillManager`。
  - `Assets/Scripts/Module/Core/MainUI/Views/MainUISkillItem.cs` 只接 `SkillItemData` DTO, 点击只打 `待对接技能释放` 日志。
  - `Assets/Scripts/Module/Core/AutoBrush/*` 已有最小自动闯关控制器, 但不是老端 `AutoFightManager` 的完整自动战斗。

## 老端运行态采样要求

老端入口:

- `http://127.0.0.1:8090/index.html`

优先账号:

- 账号: `90990`
- 密码: `47071`
- 角色: `全久京`

如果该账号状态已不适合采样, 注册新账号并创建新角色, 记录账号/角色/时间。注册很简单, 不要因为账号问题阻塞。

必须产出:

- 老端主界面技能栏截图: 进入游戏后首屏, 需能看到 `MainUISkillView` 的 4 个技能槽、自动战斗按钮、伙伴技能锁/伙伴技能位。
- 老端技能栏节点树: 至少覆盖 `MainUISkillView`、`MainUISkillItem`、`_box_skill_con`、`_img_auto_fight`、`_box_partner_skill`、`_img_partner_lock`。
- 老端 console 协议日志: 捕获 GAME_START 后技能相关请求/回包, 至少确认 `21002`、`13007` 是否发生, 以及 `shortcutList` 或快捷栏最终内容。
- 老端点击证据:
  - 点击普通技能槽: 记录是否触发 `FightEvent.SKILL_SHORTCUT_CLICK`、是否因冷却/自动战斗/锁态弹提示。
  - 点击自动战斗按钮: 记录按钮皮肤从 `uizjmgj_003a` 到 `uizjmgj_001b`/`uizjmgj_001a1` 的条件, 以及可见提示。
  - 点击伙伴锁: 记录打开 `PartnerAwakeShowView` 或等级/功能未开提示。

已有可参考证据:

- `output/playwright/laya8090_after_create_role.png`
- `output/playwright/laya8090_after_create_role_stage.json`
- `.playwright-cli/console-*.log`

注意: 以上旧证据不一定覆盖技能栏, 本轮必须补技能栏专项证据。

## 老端源码锚点

- `D:\GitProject\yu_client\h5\src\mainUI\MainUISkillView.ts`
  - `LoadSuccess`: `centerX=0`, `bottom=254`, `UpdateView`, `UpdateAutoFightState`, `InitPartnerSkill`。
  - `UpdateView`: 普通场景使用 `SkillManager.GetInstance().shortcutList`; 固定 4 个坐标 `[4,99] [39,64] [96,63] [132,101]`。
  - `UpdateAutoFightState`: 非自动 `uizjmgj_003a`, 自动 `uizjmgj_001b`, 临时模式 `uizjmgj_001a1`。
  - `ResponseAutoBtnClick` / `SetAutoFight`: 切换自动战斗并缓存位置、清 NPC 目标。
  - `InitPartnerSkill`: 功能开启、伙伴技能、伙伴觉醒锁态。
- `D:\GitProject\yu_client\h5\src\mainUI\MainUISkillItem.ts`
  - `SetData`: 使用 `skill_vo.GetIcon()` / `GameResPath.GetSkillIcon` 填图。
  - `UpdateLockState`: `skill_vo.level == 0` 或快捷技能禁用时显示 `lock`。
  - 点击 `con`: 非托管、非 CD 时 `FightEvent.SKILL_SHORTCUT_CLICK(skillVo.id, ONLY_FIRE_ATTACK)`。
  - `CirCleCdView`: 技能 CD 遮罩。
- `D:\GitProject\yu_client\h5\src\skill\SkillController.ts`
  - `GAME_START` 后延迟请求 `21002`, `21101`, `13007`, `21010`, `18401`。
  - `On21002`: `SkillManager.CreateSkillList()`。
  - `On13007`: 读取快捷栏 `{pos:c,type:c,skill_id:i,is_auto:c}`, 排序后 `skill_bar_info`, 刷新快捷栏。
- `D:\GitProject\yu_client\h5\src\skill\SkillManager.ts`
  - `CreateSkillList`: 读取 `h + (skillId:i, skillLv:h)*N`, 建 `mySkillList`, 刷 `shortcutList`。
  - `UpdateShortCutList`: `ConfigSkillUI.carrerSkillList[career]` 去掉 `common` 后排序。
  - `GetSkillBarAutoFightSkillOrder`: 若 `skill_bar_info` 存在, 使用 type=2 的 `skill_id` 覆盖默认顺序。
  - `PressSkillHandler`: `CanAttack -> setCurrentSkillId -> Scene.MainRoleAttackTarget/RELEASE_MAIN_SKILL/PartnerUpdateFight`。
- `D:\GitProject\yu_client\h5\src\autofight\AutoFightManager.ts`
- `D:\GitProject\yu_client\h5\src\scene\fight\FightEvent.ts`

## Unity 当前锚点

- `Assets\Scripts\Module\Core\MainUI\Views\MainUISkillView.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUISkillItem.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUISkillItemGod.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUIAutoBrushView.cs`
- `Assets\Scripts\Module\Core\AutoBrush\AutoBrushController.cs`
- `Assets\Scripts\Module\Core\AutoBrush\AutoBrushModel.cs`
- `Assets\Scripts\Module\Core\UiComponent\Views\CirCleCdView.cs`
- `Assets\Scripts\Framework\Net\Proto.cs`
- `Assets\Scripts\Framework\Res\GameResPath.cs`
- `Assets\Scripts\Generated\UI\MainUI\MainUISkillViewBind.cs`
- `Assets\Scripts\Generated\UI\MainUI\MainUISkillItemBind.cs`
- `Assets\Prefabs\UI\MainUI\MainUIModule.prefab`

## 本轮 P0: 建立技能栏运行态对比报告

先产出并持续更新:

- `Docs/RuntimeCompare/MainUI-Skill-第4轮.md`

报告必须包含:

1. 老端运行时账号/角色/时间/截图/节点树/console 协议路径。
2. 老端技能栏可见状态: 4 槽技能 id/name/icon/level/lock/CD, 自动战斗按钮皮肤, 伙伴锁/伙伴技能位。
3. Unity 当前运行证据: 真连服务端优先; 若只能 harness, 必须写清楚 harness 与真连差异。
4. 差异表: 协议是否请求, 技能列表是否有数据, 快捷栏是否有数据, 4 槽是否显示真实技能, 点击是否走真实事件, 自动战斗按钮是否可切换, 伙伴锁是否按功能开关显示。
5. 本轮修复项和仍然阻塞项。

最低验收:

```powershell
rg -n "MainUISkillView|MainUISkillItem|SkillController|SkillManager|shortcutList|13007|21002|SKILL_SHORTCUT_CLICK|AutoFight|CirCleCdView|_img_partner_lock" Assets\Scripts D:\GitProject\yu_client\h5\src -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P1: Unity 接入真实技能协议与模型

目标: Unity GAME_START 后按老端同构请求并解析真实技能数据, 让 `MainUISkillView` 能拿到真实技能列表和快捷栏。

要求:

- 新增/补齐 `SkillController` / `SkillManager` / `SkillVo` 时必须落在正确模块与 asmdef, 不新增 asmdef。
- `21002` 解析必须对齐老端 `ReadFmt("h") + ReadFmt("ih")*N`。
- `13007` 解析必须对齐老端 `len:h + {pos:c,type:c,skill_id:i,is_auto:c}*N`。
- `shortcutList` 规则必须对齐:
  - 默认使用 `ConfigSkillUI.carrerSkillList[RoleModel.career]` 去掉 `common`。
  - 如果 `skill_bar_info` 存在, 以 type=2 的技能栏配置覆盖默认顺序。
- 不允许为 Lv1 新角色硬编码技能 id; 如果服务端未回数据, 报告协议回包和 blocker。
- `Proto.cs` 可以新增缺失常量, 但不能改已有协议号语义。

最低验收:

```powershell
rg -n "21002|13007|SkillController|SkillManager|SkillVo|shortcutList|ConfigSkillUI|carrerSkillList" Assets\Scripts -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P2: 技能 4 槽真实可见

目标: Unity 进游戏后, `MainUISkillView` 显示老端同构的 4 个技能槽, 图标、锁态、位置来自真实数据/配置。

要求:

- `MainUISkillView` 订阅技能列表/快捷栏变化并刷新。
- 按老端固定坐标 `[4,99] [39,64] [96,63] [132,101]` 或 prefab 已转出的等价位置摆放, 不做随意像素微调。
- `MainUISkillItem.SetData` 使用真实 `SkillVo` 或真实 DTO:
  - `SkillId`
  - `Level`
  - `Icon`
  - `Name`
  - `Locked`
  - `Cd`
- 图标加载走 `ResManager.SetImageAsync` + `GameResPath.GetSkillIcon` / `GetSkillIconPath`, 禁止 `Resources.Load` 或字符串裸拼 Addressable 之外路径。
- `CirCleCdView` 本轮可先接最小 CD 展示/隐藏; 如果没有真实 CD, 记录差异, 不用假 CD 动画。

最低验收:

```powershell
rg -n "RefreshSkills|SetData\\(|SkillItemData|GetSkillIcon|CirCleCdView|UPDATE_SKILL_LIST|UPDATE_SKILL_BAR_INFO" Assets\Scripts\Module\Core Assets\Scripts\Framework -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P3: 点击链路与自动战斗按钮最小闭环

目标: 技能槽点击和自动战斗按钮不再只是 TODO 日志, 至少走到老端同名事件/状态边界。

要求:

- 技能点击:
  - 锁住时不可点或提示, 不发技能事件。
  - 非锁且未 CD 时发 Unity 等价 `SKILL_SHORTCUT_CLICK(skillId, ONLY_FIRE_ATTACK)` 事件或明确的 `GlobalEvent` 常量。
  - 如果完整 `Scene.MainRoleAttackTarget` / 技能释放未移植, 在报告中标为下一轮, 不在本轮硬造攻击。
- 自动战斗按钮:
  - 默认皮肤 `uizjmgj_003a`。
  - 切换状态后皮肤 `uizjmgj_001b`; 如实现临时模式再使用 `uizjmgj_001a1`。
  - 状态来源可以先接最小 `AutoFightModel/AutoFightManager` 等价层, 但不能与已有 `AutoBrushModel` 混淆; 自动闯关不是自动战斗。
  - 点击时应记录真实状态切换日志和可见截图。
- 伙伴锁:
  - 如果伙伴系统未开, 只按老端条件显示锁/隐藏技能位, 点击记录未开或打开入口的真实行为。

最低验收:

```powershell
rg -n "SKILL_SHORTCUT_CLICK|ONLY_FIRE_ATTACK|UpdateAutoFightState|ResponseAutoBtnClick|SetAutoFight|uizjmgj_003a|uizjmgj_001b|_img_partner_lock" Assets\Scripts D:\GitProject\yu_client\h5\src -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P4: 只记录, 不扩散编码

只记录以下差异, 不在本轮展开:

- 完整技能释放动作、寻敌、命中、伤害、特效。
- 角色技能面板、主动/被动技能升级、天赋、远古奥术。
- 伙伴系统完整数据和伙伴技能释放。
- 神祇变身技能、海域特殊技能、跨服/副本特殊技能。
- `FunctionOpenIcon`、`FirstRechargeBubble`、完整队伍系统。

## 禁止事项

- 禁止只用源码或静态 prefab 结论替代运行证据。
- 禁止 hardcode Lv1 技能 id、假 `shortcutList`、假 CD、假自动战斗状态。
- 禁止把 `AutoBrush` 自动闯关当作 `AutoFight` 自动战斗交付。
- 禁止修改 Generated Bind/Config 输出文件。
- 禁止提交 `output/`、`.playwright-cli/` 或字体 SDF 脏文件。
- 禁止为技能 UI 新建 Unity Scene; 仍在 `MainUIModule` 和 `MainUIFlow` 内接入。

## 交付格式

必须提交:

1. 玩家可见变化。
2. 老端运行时证据路径。
3. Unity 运行时/真连/Play harness 证据路径。
4. 差异报告 `Docs/RuntimeCompare/MainUI-Skill-第4轮.md`。
5. 改动文件列表。
6. `dotnet build yu_client_unity.slnx -v:minimal` 结果。
7. 确认问题清单: 只写有证据的问题。
8. 下一轮建议: 若技能栏可见闭环完成, 进入首个缺口最大且用户可见的战斗/打怪链路; 若未完成, 下一轮继续当前 blocker。
