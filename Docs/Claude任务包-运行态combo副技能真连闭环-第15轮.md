# Claude 任务包 - 运行态 combo 副技能真连闭环 - 第15轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

服务端源码: `D:\GitProject\yu_server`

目标: 完全复刻老 Laya 客户端。老端结论必须优先来自运行时 `http://127.0.0.1:8090/index.html`; 运行态暂时抓不到时,必须明确卡点,并用老端源码、真实配置、真实协议和服务端源码补足最低证据链。禁止把静态 `.scene` 当最终真相。

## 上一轮基线

- 第14轮任务包: `Docs/Claude任务包-运行态fight-movie真伤害帧-第14轮.md`
- 第14轮结果提交: `d260841d0`
- 第14轮报告: `Docs/RuntimeCompare/MainQuest-FightMovieDamage-第14轮.md`
- 第14轮已确认:
  - `damage=0` 根因已定案: Unity 只发送普攻主技能 `59100001` engage 帧,缺少老端每击后续补发的 combo 副技能 `59100002`。
  - 服务端 `mod_battle.erl` 证实 `is_att=0/calc=0` 技能只产生 `NoHurtDerList`,即 `damage=0/hp不变`; `59100002 is_att=1/calc=1` 才走真实伤害公式。
  - 老端运行态日志证实每击存在 `20024 -> 20001(engage) -> 约300ms -> 20001(combo副技能)`。
  - Unity 已最小复刻: `SkillConfigs.GetComboNext` 读取 `config_skill.combo`; `SceneCombat` 在 engage `20001` 后按配置延迟对同目标补发 combo 副技能 `20001`。副技能 id 和延迟来自配置,禁止 hardcode。
  - 第14轮 P4 尚未闭环: smoke 已真连进入 `10004`,但场景/怪同步未被该 smoke 链驱动,`MonsterCount=0`; Play 态 `Unity_RunCommand` 文件刷新异常。需要改用第13轮可行的编辑期 `EditorApplication.update` + 手动 `NetManager.Pump` harness。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-FightMovieDamage-第14轮.md`
8. `Docs/RuntimeCompare/MainQuest-PositiveDamage-第13轮.md`
9. `Docs/RuntimeCompare/MainQuest-FightVoDamage-第10轮.md`
10. 当前 Unity 代码:
    - `Assets/Scripts/Module/Core/Scene/SceneCombat.cs`
    - `Assets/Scripts/Module/Core/Scene/FightController.cs`
    - `Assets/Scripts/Module/Core/Scene/SceneController.cs`
    - `Assets/Scripts/Module/Core/Scene/SceneManager.cs`
    - `Assets/Scripts/Module/Core/Scene/MonsterRenderer.cs`
    - `Assets/Scripts/Module/Core/Scene/Vo/FightVo.cs`
    - `Assets/Scripts/Module/Core/Skill/SkillConfigs.cs`
    - `Assets/Scripts/Module/Core/Task/TaskModel.cs`
    - `Assets/Scripts/Module/Core/Role/RoleModel.cs`
11. 老端和服务端至少复查:
    - `h5/src/scene/fight/FightController.ts`
    - `h5/src/scene/fight/FightMovieInfo.ts`
    - `h5/src/scene/fight/FightVo.ts`
    - `h5/src/skill/SkillManager.ts`
    - `src/battle/mod_battle.erl`
    - `src/battle/lib_battle_util.erl`
    - `src/data/create/data_mon.erl`

## 本轮目标

把第14轮已落地的 combo 副技能补发,用真实运行态闭合到服务端权威结果:

`编辑期真实网络 harness 登录 -> 进入已有角色云霄42852 -> 到达100030含怪阶段 -> 同步真实 MonsterVo -> 触发真实攻击 -> 观察 engage 20001 + combo副技能20001 -> 服务器真实 damage>0/hp变化 -> 真实死亡 -> 100030由服务器任务推送推进`

本轮只做这条真连闭环。不要扩散到完整动作、完整特效、完整 CD、完整自动战斗 AI、掉落、伙伴、神祇、活动副本。

## 硬边界

- 禁止本地伪造伤害、扣血、死亡、掉落或任务进度。
- 禁止为了截图改 `MonsterVo.Hp`、`FightVo.damage`、任务计数、`RoleModel.BattleAttr`。
- 禁止再把抬 `att` 当主线。第13轮已经用真实装备 `att=85` 和测试 GM `att=50000` 证伪。
- 禁止 hardcode `59100001`、`59100002`、`10001001` 等样本进入业务逻辑。业务代码必须来自 `config_skill`、`SceneManager`、真实协议和真实状态。
- 禁止绕过 `NetManager`,禁止独立 socket。
- 禁止提交 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig.asset`。
- 如果临时开启冒烟账号/自动登录,提交前必须还原 `AppConfig.asset`,并在报告中写清取证窗口。

## P0: 保护第14轮基线

先复核并保护:

- `dotnet build yu_client_unity.slnx -v:minimal` 必须通过,或明确真实不可运行原因。
- 第14轮 combo 逻辑不得退化:
  - `SkillConfigs.GetComboNext` 仍从 `config_skill.combo` 读副技能 id 和延迟。
  - `SceneCombat` 仍先发 engage `20001`,再按配置延迟补发 combo 副技能 `20001`。
  - `FightController` 的 `20024/20001` C2S 字段、`FightVo` S2C 解析、`MonsterVo -> MonsterRenderer -> GameView` 可见怪链不退化。
  - `RoleModel.BattleAttr`、`TaskModel` 不做本地伪造修改。
- 复查 `.gitattributes` 后,`git show --numstat d260841d0 -- Assets/Scripts/Module/Core/Scene/SceneCombat.cs Assets/Scripts/Module/Core/Skill/SkillConfigs.cs` 应可显示文本增删,不应再因 `-diff` 被显示为 binary。如果仍显示 Bin,先查明原因再继续。

## P1: 建立可复现编辑期真连 harness

优先复用第13轮可行方式,不要再卡在 Play 态 `Unity_RunCommand` 文件刷新:

- 用编辑期 `EditorApplication.update` 或等价一次性 harness,每 tick 手动 `NetManager.Pump()` 保活。
- 登录测试服账号 `unity_npc_475823114`,进入真实角色 `云霄42852`。
- 读取并记录 `30000/30001/30004/30005` 中当前主线状态;若仍在 `100030`,继续进入含怪任务点;若已经变化,先记录真实变化,不要本地改任务。
- 确认场景 `10000` 或任务配置对应场景进入成功,`SceneManager` 中出现真实 `MonsterVo`,至少记录:
  - monster instance id
  - config id
  - hp/maxHp
  - x/y
  - renderer 或 GameView 可见证据
- 若 `MonsterCount=0`,必须记录完整协议/日志卡点: 是否没有发进入场景请求、是否未处理场景对象推送、是否怪由任务触发生成、是否服务端没有推对象。

## P2: 验证 combo 副技能真实发包

在有真实可见怪后触发真实攻击:

- 调用真实主角攻击入口,例如 `SceneCombat.Instance.MainRoleAttackTarget(...)` 或主线任务自动打怪等价链路。
- 记录完整 C2S/S2C:
  - `20024` 是否进入战斗。
  - 第一次 `20001` 的 skill id、目标列表、x/y/angle、时间戳。
  - combo 副技能第二次 `20001` 的 skill id、目标列表、x/y/angle、时间戳。
  - 第二次 `20001` 与第一次之间的间隔是否来自 `config_skill.combo` 延迟。
- 如果没有第二次 `20001`,必须定位:
  - `GetComboNext` 是否返回 `(0,0)`;
  - `NetManager.IsConnected` 是否为 false;
  - 发包前 `alive.Count` 是否为 0;
  - `SceneManager.GetMonster(ins)` 是否找不到真实对象;
  - 是否异常被 `GameLog.Warn("Combat", "combo 副技能补发异常...")` 记录。

## P3: 验证服务器真实 damage/hp

只有服务器 `20001 S2C` 返回后才算数:

- 对 combo 副技能回包解析 `FightVo`,记录每个 defender:
  - defender id
  - hp
  - damage
  - damage_flag
  - skill id
  - target instance id 是否与真实 `SceneManager` 怪一致
- 通过条件:
  - 出现服务器真实 `damage>0`;或
  - 出现服务器真实 `hp<maxHp`;或
  - 出现服务器真实 death/delete scene object。
- 如果第二次 `20001` 仍是 `damage=0/hp不变`,禁止本地修饰结果,必须记录:
  - combo 副技能 skill id 是否为 `59100002` 或当前职业配置对应副技能;
  - 服务端是否返回 `ERR_COMBO`、限频、remote-close 或其它错误;
  - 发包间隔是否需要按老端运行态 `约300ms` 而不是配置 `200ms`;
  - 目标列表是否与第一次 engage 一致且仍存活;
  - 服务器回包是否仍只对应 engage 帧而非副技能。

## P4: 验证真实死亡和 100030 推进

只有 P3 有真实伤害后继续:

- 连续真实攻击直到怪物 `hp==0` 或服务端移除对象。
- 记录 `MonsterRenderer` 血条变化、死亡移除、`DeleteSceneObj` 或等价运行态证据。
- 等待并记录服务器任务推送:
  - `30000`
  - `30001`
  - `30005`
  - 或 `100030 -> 100040` 等真实推进。
- 只有真实死亡或服务器任务推送后,才允许写报告说 `100030` 闭环通过。

## P5: 只记录不扩散

只记录,不编码:

- 完整自动战斗 AI、挂机循环、寻怪循环。
- 完整动作、完整特效、完整 CD、音效、伤害飘字。
- 直线/扇形 AOE、PvP、伙伴、神祇、天赋加成。
- 掉落、怪物 AI、活动副本、BOSS、队伍协同。

## 验收与提交

1. 建立 `Docs/RuntimeCompare/MainQuest-ComboDamageClosure-第15轮.md`。
2. `dotnet build yu_client_unity.slnx -v:minimal` 通过,或说明真实不可运行原因。
3. 最终报告必须明确:
   - 是否用编辑期 harness 真连进入含怪场景。
   - 是否出现真实 MonsterVo 和可见怪。
   - 是否出现 engage `20001` + combo 副技能 `20001` 两次发包。
   - combo 副技能是否产生服务器真实 `damage>0/hp变化/death`。
   - `100030` 是否由真实死亡或任务推送推进。
   - 仍有真实 blocker 和下一轮最小动作。
4. 只提交本轮任务包、必要代码、报告和必要的文本属性修复。不得提交 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig.asset`。
