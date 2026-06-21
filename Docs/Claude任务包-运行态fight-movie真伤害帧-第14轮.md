# Claude 任务包 - 运行态 fight-movie 真伤害帧 - 第14轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须优先来自运行时 `http://127.0.0.1:8090/index.html`; 运行态暂时抓不到时, 必须明确卡点, 并用老端源码、真实配置、真连协议证据补足最低证据链。禁止把静态 `.scene` 当最终真相。

## 上一轮基线

- 第13轮任务包: `Docs/Claude任务包-运行态正伤害与100030闭环-第13轮.md`
- 第13轮结果提交: `1f1df5555`
- 第13轮报告: `Docs/RuntimeCompare/MainQuest-PositiveDamage-第13轮.md`
- 第13轮已确认:
  - 冒烟角色已用真实 `30004` 从 `100020` 推进到 `100030`; `100030` 是场景 `10000` 击杀怪 `10001001` x3。
  - 真实奖励和装备路径有效: `att 33 -> 35 -> 85`, `wreck 17 -> 38`, `power 1040 -> 1800`; 服务端确认职业武器 `101011010` 生效。
  - 测试服 GM 只作用于专用测试账号, 曾把 `att` 抬到 `50000` 做排除变量。
  - `att=85` 与 `att=50000` 两组真连 `20001` 均返回 `damage=0 / hp=140 / flag=0`; 因此第12轮“养成不足导致 damage=0”结论已被证伪。
  - 当前可行动 blocker: Unity 在技能释放边界发单帧 `20001`; 服务端稳定返回疑似“进战斗/上 buff engage 帧”, 真伤害不在这一帧。下一步必须对齐老端 fight-movie/cast/真伤害帧, 不再继续抬 `att`。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-PositiveDamage-第13轮.md`
8. `Docs/RuntimeCompare/MainQuest-FightMovieTiming-第11轮.md`
9. `Docs/RuntimeCompare/MainQuest-FightVoDamage-第10轮.md`
10. 当前 Unity 代码:
    - `Assets/Scripts/Module/Core/Scene/FightController.cs`
    - `Assets/Scripts/Module/Core/Scene/SceneCombat.cs`
    - `Assets/Scripts/Module/Core/Scene/SceneController.cs`
    - `Assets/Scripts/Module/Core/Scene/SceneManager.cs`
    - `Assets/Scripts/Module/Core/Scene/MonsterRenderer.cs`
    - `Assets/Scripts/Module/Core/Scene/Vo/FightVo.cs`
    - `Assets/Scripts/Module/Core/Skill/SkillConfigs.cs`
11. 老端源码中至少查清:
    - `h5/src/scene/fight/FightController.ts`
    - `h5/src/scene/fight/FightVo.ts`
    - `h5/src/scene/fight/*`
    - `h5/src/scene/Scene.ts`
    - 所有 `fight-movie`, `playFightMovie`, `skill_damage_time`, `ClientFightServer`, `onRoleRequestToFightHandler`, `request_fight_mon_list`, `attack_trigger_skill`, `20024`, `20001` 相关定义和调用。

## 本轮目标

把第13轮锁定的“单帧 `20001` 只返回 `damage=0` engage 帧”继续往前推进:

`老端运行态技能点击 -> fight-movie/cast 序列 -> 真正触发伤害帧或后续协议 -> Unity 按同一时机/同一目标来源发包 -> 服务端真实 damage>0 或得到更精确 blocker -> 真实 hp/death -> 100030 击杀进度`

本轮只做 **真伤害帧/后续帧证据和最小复刻**。不要扩散到完整动作系统、特效、飘字、完整自动战斗 AI、掉落、伙伴、神祇、活动副本。

## 硬边界

- 不再把“继续抬 att”当主线。第13轮已用 `att=50000` 排除养成变量; 除非发现第13轮证据有明确错误, 否则禁止再花主要时间做属性提升。
- 不允许本地伪造伤害、扣血、死亡、掉落或任务进度。
- 不允许为了截图直接改 `MonsterVo.Hp`、`FightVo.damage`、任务计数或 `RoleModel.BattleAttr`。
- 不允许 hardcode `1129`、`1134`、`59100001`、`5374/2672` 等调试样本进入业务逻辑。
- 不允许绕过 `NetManager`, 不允许独立 socket。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig.asset`。
- 如果运行态需要临时打开冒烟账号/自动登录, 提交前必须还原 `AppConfig.asset`, 并在报告里写清取证窗口。

## P0: 保护第13轮基线

先复核并保护:

- `dotnet build yu_client_unity.slnx -v:minimal` 必须通过, 或明确真实不可运行原因。
- 第13轮已确认链路不能退化:
  - `RoleModel.BattleAttr` 读取。
  - `20024/20001` C2S 真发包。
  - `FightVo` S2C 解析。
  - `MonsterVo -> MonsterRenderer -> GameView` 怪物可见链。
  - `TaskModel` 当前仍停在 `100030` 击杀任务, 不本地改任务。
- `AppConfig.asset`、`.playwright-cli/`、`output/` 不入库。

## P1: 老端运行态真伤害帧证据

必须从老端运行时 `http://127.0.0.1:8090/index.html` 采集或明确卡点:

- 用同一后端、同一类测试账号或新测试账号进入主线含怪阶段。
- 点击真实技能/自动打怪, 抓取:
  - 技能点击到 fight-movie 开始的节点/日志/console。
  - `20024` 与 `20001` 的实际发送时机、次数、间隔。
  - 是否存在第二个 `20001`、后续 `20001 S2C`、或其它伤害广播协议。
  - 真正出现 `damage>0` / `hp<max` / `hp==0` 时对应的 C2S/S2C 前后帧。
- 如果老端运行态短时间抓不到, 报告必须写清:
  - 卡在登录、创角、主线、怪物、技能、网络代理、console 注入还是截图/录制。
  - 已抓到的运行态截图、节点树或 console 片段。
  - 用老端源码和配置补足到哪里, 还缺哪一帧证据。

产物: `Docs/RuntimeCompare/MainQuest-FightMovieDamage-第14轮.md`

## P2: 老端源码/配置交叉确认

围绕“真伤害帧到底是什么”查清:

- `playFightMovie` / fight-movie 数据如何调度技能释放、受击、伤害时点。
- `skill_damage_time` 对 `59100001` 的真实含义: 单位、默认值、是否为 0、是否代表立即伤害还是由 fight-movie 其它字段接管。
- `ClientFightServer` / `onRoleRequestToFightHandler` / `request_fight_mon_list` 是否只在一处发 `20001`, 还是有前置/后置状态、锁目标、连击、普通攻击循环。
- 老端是否先发 `20024 "c" 1`, 再在动作帧发 `20001`; 或 `20024/20001` 都在动作帧附近。
- 目标/AOE 列表是点击瞬间锁定、动作帧重算, 还是由 fight-movie 阶段刷新。

报告要列出: 已确认字段、未确认字段、源码路径/函数名、对应 Unity 当前差异。

## P3: Unity 最小复刻, 仅限证据足够时

只有 P1/P2 证明清楚后才允许写 Unity 修复:

- 如果老端确认 `20001` 应延迟到某个真伤害帧, 则 Unity 最小增加 `SkillDamageTiming` 或等价调度, 把当前释放边界立即发包改为老端时机。
- 调度必须保留真实目标/AOE/攻击点/角度/技能 id 来源; 按老端规则决定锁定或重算目标。
- 如果老端确认需要后续帧/第二次 `20001` 或其它协议, 必须按真实协议接入, 禁止猜包。
- 不做完整动作、完整特效、完整 CD、完整自动战斗 AI; 本轮只让真实伤害链闭环。

验收:

- 编辑期或 Play 日志能证明 `20001` 不再在错误边界发出, 而是在老端证据对应的真伤害帧发出。
- 第13轮 `FightVo` 解析与怪物可见链不退化。

## P4: 真连 damage>0 / hp / death / 100030

P3 后做真实 Play 验证:

- 当前测试账号应已在 `100030` 含怪阶段; 如果账号状态变化, 先记录 `30000/30001/30005`。
- 真连点击技能或主线自动打怪, 记录 `20024/20001 C2S` 与 `20001 S2C`。
- 若出现 `damage>0`、`hp<max` 或 `hp==0`, 必须记录防御者 id、hp、damage、damage_flag、血条/死亡可见证据。
- 只有真实死亡或服务器任务推送后, 才验证 `100030` 是否推进到 `100040`。
- 若仍稳定 `damage=0`, 必须记录完整 C2S/S2C、时序参数、目标列表、服务端 remote-close/限频状态, 作为新的精确 blocker; 禁止本地扣血。

## P5: 只记录不扩散

只记录, 不编码:

- 完整自动战斗 AI、挂机循环、寻怪循环。
- 完整动作系统、完整 CD、完整特效、音效、伤害飘字。
- 直线/扇形 AOE、PvP、伙伴、神祇、天赋加成。
- 掉落、怪物 AI、活动副本、BOSS、队伍协同。

## 验收与提交

1. 建立 `Docs/RuntimeCompare/MainQuest-FightMovieDamage-第14轮.md`。
2. `dotnet build yu_client_unity.slnx -v:minimal` 通过, 或说明真实不可运行原因。
3. 最终报告必须明确:
   - 老端运行态是否抓到真伤害帧/后续帧。
   - 老端源码/配置确认的发包时机、目标锁定规则和未确认字段。
   - Unity 是否按证据调整 `20001` 时机或后续协议。
   - 是否出现服务器真实 `damage>0/hp变化/death`。
   - `100030` 是否真实推进。
   - 仍有真实 blocker 和下一轮最小动作。
4. 只提交本轮任务包、必要代码、报告; 不带 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig.asset`。
