# 运行态主线自动打怪含怪场景 运行态对比 · 第 7 轮

范围:把第 6 轮"技能点击已接到 `SceneCombat.MainRoleAttackTarget` 真实寻敌入口、但主城 10000 无怪所以只能真实阻塞"的缺口,
推进成 **主线任务把角色带进真实含怪任务点 → SceneManager 出现真实 MonsterVo → SceneCombat 命中真实怪 → 朝向/接近/释放边界可见** 的最小竖切。
方法:老 Laya 客户端 **运行时**(`http://127.0.0.1:8090` 的 console 协议流)+ 老端 TS/配置源码为真相;Unity 侧 **真连测试服 + Play 态** 端到端取证。

> 头条结论:
> **本轮 Unity 侧端到端跑通了"主线寻路 → 真实怪进表 → SceneCombat 命中真实怪 → 接近 + 释放边界":**
> 真连角色 `云霄42852`(主线卡在 `100020`,下一条即第一条打怪 `100030`)在 Play 态由 `MainRoleAgent.MoveToNpc` 自动寻路从出生点
> `(4547,1610)` 走向 `100030` 击杀点 `(5463,2678)`;途中服务器按九宫格 **真实下发了击杀目标怪 `10001001`**(实例 `ins=1129`,
> `hp=140/140 can_attack=1`,坐标 `(5374,2672)`,与 `config_task` 100030 击杀目标 **完全一致**)到 `SceneManager`(`MonsterCount=1`);
> 点击已学技能 `59100001` → `SceneCombat.MainRoleAttackTarget` **取到这只真实怪**(`CurrentTargetId=1129`)→ 超范围分支
> (`708px>100px`)→ `MainRoleAgent.MoveToNpc` 自动接近到 `(5340,2624)`(距怪 `~62px`,进攻击范围)→ `FaceTarget` + 本地
> `EVT_RELEASE_MAIN_SKILL` 释放边界,全链真实数据、无一造假。
> **关键事实:** 击杀目标怪 `10001001` 在角色还停在任务 `100020` 时、走近 `(5463,2678)` 即由九宫格下发 → **该怪是场景常驻刷怪、非任务门控**。
> **老端第一条打怪** = 主线 `100030`(开放世界,场景 10000 内寻路到 `(5463,2678)` 打 `10001001`×3),**不是副本**;
> 副本 `10200`(`openDungeonFightSceneView`,经 `send 13305`)是更靠后的主线副本任务 `100041`(斩妖祛秽),本轮不移植。
> **仍阻塞:** 真实 `20024`/`20001` 发送(依赖 fight-movie/AOE 收集链,P4 不猜不发)、怪物渲染(本工程尚无 MonsterSpawner,怪为数据态)、
> 测试服会话不稳(~60s 周期性 remote-close + 重连重建场景把主角复位回出生点,导致同一窗口内多步链路易被打断)。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮在线,HTTP 200,len 25197) |
| 老端协议流(权威) | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(26020 行,含完整 100010→100041 主线 + 打怪/副本协议) |
| 老端 TS/配置源 | `D:\GitProject\yu_client`(`commonModel/TaskModel.ts`、`config_task.json`、`config_npc.json`) |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`;游戏服 `ws://223.109.142.26:10000`(解析自 get_server_info) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` career1 剑士 Lv2,复用第 3–6 轮角色) |
| Unity 取证方式 | 编辑器 Play 态(`AppConfig.autoLoginSmokeTest+autoEnterFirstRoleSmokeTest` 临时开),`Unity_RunCommand` 单命令驱动 `TaskModel.DoTask`/`MainRoleAgent.MoveToNpc`/`SceneCombat`,`Unity_ReadConsole` 取日志 |
| Unity 证据 | 控制台 `[Task]/[Scene]/[Combat]` 日志 + `RunCommand` 回读的 `RoleModel/SceneManager/SceneCombat` 实时状态(下文逐条引用) |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 均 gitignore,不入库;`AppConfig.asset` 的冒烟开关与 devAccount 取证后已还原(git diff 为空)。

---

## 1. 老端运行时真相:第一条打怪是开放世界 `100030`,副本 `100041` 在其后

### 1.1 主线任务推进序(`console-…log` 实采,`30000 all_task_list`)

| 任务 | 行号 | 时间戳 | 类型(config_task) |
|---|---|---|---|
| `100010` | 390/515 | 245366ms | 主线·与 NPC 对话(NPC `100101`) |
| `100020` | 1039/1050 | 267865ms | 主线·与 NPC 对话(NPC `100134` 灵枢仙子,发新手武器) |
| **`100030`** | 1191/1201 | 279301ms | **主线·击杀(KILL):怪 `10001001`×3 @ 场景 10000 `(5463,2678)`** |
| `100040` | 1403/1414 | 282183ms | 主线·与 NPC 对话(NPC `100102`,学技能) |
| `100041` | 1590/1602 | 293104ms | 主线·副本(fin_main_dun,副本 10 @ `(4445,3147)`) |

`config_task.json`(`D:\GitProject\yu_client\cdn\...\config\server\config_task.json`)100030 击杀字段:
`[["kill","1","10001001","3","10000","5463","2678","1","降服3只转运灵","0","0"]]` —— 即 **第一条打怪 = 场景 10000 内开放世界击杀,非副本**。

### 1.2 第一条打怪链路时间线(老端 console 实采,行号/时间戳为日志实采)

```
[279301ms] 30000 all_task_list: {100030: Array(1)}        ← 主线推进到 100030(击杀任务)
[279405ms] ------DoTask------ TaskVo true                  ← 任务自动执行
[279405ms] taskmodel//-TaskSpeed-: 5463 2678              ← 自动寻路目标点 = (5463,2678)(击杀点)
[279789ms] receive cmd = 12012                             ← 九宫格对象刷新:怪进入视野
[280671ms] send cmd =20024                                 ← 进入战斗态(20024 "c" 1)
[280676ms] send cmd =20001                                 ← 第一次攻击/技能释放请求
[282113ms] ------DoTaskJump------ {x:4470,y:2841,speed:750}← 寻路途中跳跃位移
[282455ms] receive cmd = 12007 ×4                          ← 单怪进视野(MonsterVo)
... 此后 20001 send/recv 持续(自动战斗刷怪)
```

**老端第一条打怪 = 主线 `100030` → `DoTask` 自动寻路到击杀点 `(5463,2678)` → 怪进视野(`12012`/`12007`)→
进战斗态(`20024`)→ 攻击请求(`20001`)→ 服务端命中/伤害广播(`20001`)。**

### 1.3 副本(`100041`/场景 `10200`)是更靠后的主线副本,本轮不移植

```
[312349ms] ------DoTask------ TaskVo true                  ← 主线推进到 100041(主线副本任务)
[313202ms] send cmd =13305                                 ← 副本进入请求(AutoBrushModel REQUEST_PROTO 13305 "c" 0)
[313292ms] receive 12004 / 12005 → handler12005: 0 10200 10000 0 50000 4445 3147
[313292ms] 测试！请求的场景id是10000,返回的id是10200       ← 请求 10000 服务端返回副本实例 10200
[313294ms] openDungeonFightSceneView                       ← 打开副本战斗视图
[313295ms] send 61019 / recv 61001 / send 61004           ← 副本进入握手
[326222ms] send 61002 → 12083 → 12004/12005 → 回 10000     ← 副本结算后回主城
```

`config_task` 100041 = `[["fin_main_dun","10","0","1","10000","4445","3147",...]]`。即 `100041` 才是第一条 **副本** 打怪,经 `13305` 进副本实例 `10200`(`npc 12100: 10200 0` 零 NPC,证其为战斗副本)。**本轮 Unity 不接 `13305`/副本链(AutoBrush 仅 MainUI 占位,无 13305 移植)。**

### 1.4 老端技能/攻击协议格式(承接第 6 轮,权威字节)

- `20024` 战斗态:`SendFmtToGame(20024,"c",1)`(进)/`(…,2)`(出),单字节。
- `20001` 攻击请求:`WriteBegin(20001)` → `h`(怪数)+`i`×N(怪实例 id)+`h`(人数)+`l`×N(人 id)+`ihhh`(skill_id, floor(x), floor(y), floor(angle))。
- `20001` 的怪列表/AOE 中心由 **fight-movie 队列 + 碰撞收集链**(`ClientPrePlayFightMovie`→`ROLE_REQUEST_TOFIGHT`)产出,非单点目标直发 → 本轮不复现、不发(见 §5)。

---

## 2. P1:Unity 主线寻路进入真实击杀点(真连实测,**已跑通**)

### 2.1 本轮改动:`DoTask` 同场景任务接真实自动寻路

第 6 轮 `TaskModel.DoGotoSceneTask` 对"带场景坐标的任务"只打 blocker 日志("自动寻路到点未移植")。本轮补最小真实链路:

| 方法 | 改动 |
|---|---|
| `TaskModel.DoGotoSceneTask` | 同场景(`task.SceneId==当前 或 0`)→ 复用 `MainRoleAgent.MoveToNpc(SceneX,SceneY)` 自动寻路到任务点(对标老端 DoTask 自动寻路/TaskSpeed);跨场景仍保留飞鞋 blocker(`USE_FLY_SHOE` 未移植) |
| `TaskModel.DoFindNpcTask` | 目标 NPC 不在场景对象表且任务在当前场景 → 直接开对话(`12101`),不臆造跨场景 blocker。对应 `config_npc` `scene:0` 的对话型/浮空 NPC(如灵枢仙子 `100134`) |
| `MainUITaskTeamView.RefreshTaskItems` | 防御性早退(`_panel_task==null`):断线重连期 MainUI 被拆但事件未退订时,`EVT_TASK_SELECT_CHANGED→RefreshTaskItems` 会抛 `MissingReferenceException` 阻断 `DoTask`,本轮修掉 |

仅改 `Shenxiao.Module.Core`,无新增 asmdef、无新依赖。

### 2.2 真连实测:`DoTask` → 自动寻路真实位移

真连角色 `云霄42852` 当前主线 = `100020`(`tipsType=7` 与 NPC `100134` 对话,`hasFinish=1`,`newestFinish=100010`),即 **下一条就是第一条打怪 `100030`**。

`Unity_RunCommand` 驱动 + 回读(同一 Play 窗口,帧号见 `frame=`):

```
DoTask(100020) → [Task] DoTask 找 NPC: NPC 100134 在场景 pos=(5030,2210),主角走过去,到达后开对话(12101)
                 [Scene] MoveToNpc: 自动直线接近目标 (5030,2210)
```
→ 证明 `DoTask` 走真实 `DoFindNpcTask → MoveToNpc` 分支(清洁会话里 NPC `100134` 确在场景对象表,坐标 `(5030,2210)`)。

直接驱动 `MainRoleAgent.MoveToNpc(5463,2678)`(击杀点)取证自动寻路位移:

```
READY agent=True map=True pos=(4547,1610) npc=34 frame=7265   ← 出生点
>> walk started from (4547,1610)
CHECK pos=(4963,2095)  agent=True mon=1 frame=17008           ← 已位移 ~640px,且怪已进表!
... 接近继续 ...
FINAL role pos=(5340,2624) targetId=1129 frame=49044          ← 抵达击杀点附近(距怪 ~62px)
```

**结论(P1):** Unity 主角 **真实自动寻路位移成立**:`(4547,1610)→(4963,2095)→(5340,2624)`,沿直线接近击杀点 `(5463,2678)`;
`MainRoleAgent.Update→AutoStep→Advance` 按真实像素步进 + 撞墙滑行(实测路径 `IsBlockPixel` 全 `False` 可走)。
`DoTask` 点击 → 真实 `DoGotoSceneTask/DoFindNpcTask → MoveToNpc` 链路打通。

---

## 3. P2:SceneManager 出现真实怪并被 SceneCombat 读取(**已跑通,渲染缺失**)

### 3.1 真连实测:九宫格真实下发击杀目标怪

角色寻路接近 `(5463,2678)` 过程中,`SceneManager.MonsterCount` 由 `0`→`1`,`Unity_RunCommand` 回读怪 VO:

```
MON ins=1129 type=10001001 hp=140/140 canAtk=1 collect=False pos=(5374,2672) dist=708px
```

- `type=10001001` 与 `config_task` 100030 击杀目标 **完全一致**(转运灵)。
- `ins=1129`(服务器实例 id)、`hp=140/140`、`can_attack=1`、非采集物、坐标 `(5374,2672)`(正是击杀点)——
  全部来自服务器 `12007`/`12012` 九宫格下发,经 `SceneController.On12007/On12012 → ParseMonster → SceneManager.AddMonster` 真实入表。
- **该怪在角色还停在任务 `100020` 时就下发** → 击杀目标怪 `10001001` 是 **场景常驻刷怪,非任务门控**(走近即进九宫格视野)。

### 3.2 确认问题:怪物"数据有、渲染缺失"

本工程当前 **无 `MonsterSpawner`/怪物渲染层**(全仓 `MonsterAdded` 仅 `SceneManager` 自身声明,无订阅渲染者;主角经 `SceneCharacterStage` 渲染、NPC 经 NPC 渲染链,但怪物/其他玩家渲染属第 6 轮记录的"后续 RoleSpawner/MonsterSpawner")。
故 **怪为数据态**(`MonsterVo` 真实入表、可被技能寻敌读取),**GameView 不可见**。按任务包 P2 口径,记为确认问题"数据有怪但渲染缺失",本轮不补假怪、不强塞渲染(最小真实渲染链路 = 下一轮)。

---

## 4. P3:技能点击命中真实怪分支(**已跑通到释放边界**)

`Unity_RunCommand` 驱动已学技能 `59100001`(round5 实测 lv1 已学、obj=2 目标技能):

```
SceneCombat.MainRoleAttackTarget(59100001,1) → CurrentTargetId=1129
[Combat] MainRoleAttackMonster skill=59100001 目标怪 ins=1129 距离=708px > range=100px
         → 自动接近(MoveToNpc)到攻击范围后释放(对标 StartTargetAction)
[Combat] RELEASE_MAIN_SKILL(本地) skill=59100001 target ins=1129(compress_id 等价) attackType=1
         (调用栈: MainRoleAgent.FinishAutoMove → ApproachThenRelease 回调 → ReleaseMainSkill)
FINAL role pos=(5340,2624) targetId=1129 (距怪 ~62px ≤ range=100px)
```

**结论(P3):** 完整命中真实怪分支跑通:
`PressSkill 目标技能 → SceneCombat.MainRoleAttackTarget` → `GetClickTarget??FindNearestAttackableMonster` **取到真实怪 ins=1129** →
锁定 `CurrentTargetId=1129` → `MainRoleAttackMonster` 像素距离平方判定 `708px>100px` → **超范围分支** `ApproachThenRelease` →
`MainRoleAgent.MoveToNpc` 自动接近到 `(5340,2624)`(进攻击范围)→ 到达回调里 `FaceTarget`(朝向)+ 本地 `EVT_RELEASE_MAIN_SKILL` 释放边界。
朝向/接近/释放三段在真实怪上全部执行;真实 `20001` 发送按 P4 不发(下轮)。

---

## 5. P4:真实 `20024`/`20001` 发送(本轮仍不发,记录)

格式串第 6 轮已采全(§1.4)。但 `20001` 的怪列表/AOE 中心来自老端 **fight-movie 队列 + 碰撞收集链**,非单点目标直发;
本轮 Unity 已能拿到单个真实目标怪 `ins=1129`,但 **未复现 AOE 收集链** → 按任务包"只可证即做"原则,**不发 `20001`/`20024`,不猜半格式**,列为下一轮 blocker。

---

## 6. P5:只记录不扩散

完整自动战斗 AI/挂机循环、怪物 AI/仇恨/死亡/掉落/采集、伙伴/神祇/天赋加成、技能特效/伤害飘字/战斗结算/音效、活动/副本/BOSS/组队战斗 —— 均不编码,仅记录。

---

## 7. 差异表

| 维度 | 老端运行时 | Unity 第 7 轮 | 结论 |
|---|---|---|---|
| 第一条打怪触发 | 主线 `100030` → `DoTask` 自动寻路到击杀点 `(5463,2678)` | `DoTask`→`DoGotoSceneTask`→`MoveToNpc` 自动寻路到同坐标 | **对齐 ✓(本轮新接)** |
| 自动寻路位移 | `TaskSpeed` + 寻路(含 `DoTaskJump`) | `MainRoleAgent.MoveToNpc` 直线接近 + 撞墙滑行(实测 `(4547,1610)→(5340,2624)`) | **对齐 ✓(无 A*/无跳跃,直线兜底)** |
| 含怪场景/怪数据 | 九宫格 `12012`/`12007` 下发怪 | `12007`/`12012`→`SceneManager`,实测 `10001001 ins=1129` 入表 | **对齐 ✓(字节精确)** |
| 第一条打怪是否副本 | 否,开放世界场景 10000;副本是更后的 `100041`(经 `13305`→`10200`) | 同(本轮接开放世界击杀,副本 `13305` 不移植) | **对齐 ✓** |
| 目标型技能寻敌 | `Scene.MainRoleAttackTarget`→`FindTargets` | `SceneCombat.MainRoleAttackTarget`→`FindNearestAttackableMonster` 取真实 `ins=1129` | **对齐 ✓** |
| 范围/朝向/接近/释放 | 像素距离² + `SetDirection` + `StartTargetAction` + `RELEASE_MAIN_SKILL` | 像素距离²(708>100)+ `MoveToNpc` 接近 + `FaceTarget` + 本地 `EVT_RELEASE_MAIN_SKILL` | **对齐 ✓(实跑)** |
| 怪物渲染 | fight 场景内可见 | 无 MonsterSpawner → 怪为数据态、GameView 不可见 | **差异(最小渲染链下一轮)** |
| 攻击请求发送 | `20024 "c"` + `20001`(fight-movie/AOE 链) | 仅到本地 `EVT_RELEASE_MAIN_SKILL`,不发 20001 | **差异(真实协议下一轮)** |
| 对话型 NPC(scene:0)| 直接开对话(不走近) | `DoFindNpcTask` 同场景空对象 → 直接 `ShowTask`(12101) | **对齐 ✓(本轮修)** |
| 会话稳定性 | 持续在线(分钟级) | 测试服 ~60s 周期 remote-close + 重连重建场景把主角复位回出生点 | **差异(环境阻塞,见 §8)** |

---

## 8. 仍然阻塞项 / 真实卡点(本轮只记录)

1. **测试服会话不稳定(环境阻塞):** 真连 `ws://223.109.142.26:10000` 约 ~60s 周期性 remote-close;断线后会重新跑登录链并重建场景,
   把主角 `MainRoleAgent` 销毁重建、坐标 **复位回出生点 `(4547,1610)`**(本轮观测到 3 次 `main role ready` 重建)。
   表现:同一寻路窗口若被重连打断,主角会"瞬移回出生点、位移看似为 0"——首次取证即因此误判,后改用"短窗口快取"才在重连前抓到位移与命中。
   且重连后场景常 **未稳定重入**(`SceneMapLoader/MainRoleAgent` 仍为空)。该不稳定使多步链路(进对话→进 100030→寻路→打怪)难以一次跑完。
2. **怪物渲染缺失:** 无 `MonsterSpawner`,怪为数据态(P2)。
3. **真实 `20001`/`20024` 发送:** 依赖 fight-movie/AOE 收集链(P4)。
4. **主线推进 `100020→100030`:** 老端经对话/`30004` 完成提交推进;本轮取证聚焦"已到含怪点 + 命中真实怪",
   未强行推进任务状态(击杀怪本就场景常驻,无需先到 `100030` 即可命中);完整对话推进 + 自动交任务留后续。
5. **精确攻击范围:** 仍用真实下限 `100px`,精确 `skill_distance*0.8` 需 `config_skill` 攻击距离字段(下一轮)。

### 下一轮建议

进入 **真实战斗收尾**:① 补最小 `MonsterSpawner`(订阅 `SceneManager.MonsterAdded` 渲怪到合成台,让 P2 GameView 可见);
② 复现 fight-movie/AOE 收集链后接真实 `20024`/`20001` + 服务端命中/伤害广播;③ 缓解测试服 remote-close(心跳/重连重入场景修复),
让"主线寻路→打怪→结算"可一次跑完;④ 接 `config_skill` 攻击距离/CD。

---

## 9. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误 / 6 既有无关警告**(`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162),与第 5/6 轮同组,无新增。
- Play 态真连取证(单 Play 窗口,帧号连续):`role ready 云霄42852 scene=10000` → `DoTask(100020)→MoveToNpc` → 主角位移 `(4547,1610)→(4963,2095)→(5340,2624)` →
  `SceneManager` 怪 `ins=1129 type=10001001` 入表 → `SceneCombat.MainRoleAttackTarget(59100001)` 锁定 `1129` → 超范围接近 → `RELEASE_MAIN_SKILL` 释放边界,全链跑通。

---

## 10. 本轮改动清单(落 `Shenxiao.Module.Core`,不新增 asmdef)

| 文件 | 改动 |
|---|---|
| `Module/Core/Task/TaskModel.cs` | `DoGotoSceneTask` 同场景接 `MainRoleAgent.MoveToNpc` 自动寻路到任务点(P1 真实链路);`DoFindNpcTask` 对 `scene:0` 对话型 NPC(空场景对象)直接开对话;新增 `OnArriveTaskPoint`/`TaskPointArriveLogicDist` |
| `Module/Core/MainUI/Views/MainUITaskTeamView.cs` | `RefreshTaskItems` 防御性早退(`_panel_task==null`):修断线重连期 `EVT_TASK_SELECT_CHANGED→RefreshTaskItems` 抛 `MissingReferenceException` 阻断 `DoTask` 的真实 bug |

> 取证用 `AppConfig.asset` 冒烟开关(`autoLoginSmokeTest/autoEnterFirstRoleSmokeTest`)与 `devAccount` 已还原(git diff 为空),不入库;无临时 harness 脚本入库。
