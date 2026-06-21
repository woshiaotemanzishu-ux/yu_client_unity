# 运行态打怪/技能释放链路 运行态对比 · 第 6 轮

范围:把第 5 轮"技能点击只到 `Scene.MainRoleAttackTarget` 入口边界"的缺口,推进成
**真实目标系统驱动的最小打怪/技能释放链路**——
Unity 侧点击已学技能 → `SkillController.PressSkillHandler`(career/obj 三分支)→ 目标型技能进
**新建 `SceneCombat.MainRoleAttackTarget`**:从**真实 `SceneManager` 怪物表**取当前点击目标 / 寻最近可攻击怪
→ `MainRoleAttackMonster`(像素距离平方判定 + 朝向 + 自动接近)→ 本地 `RELEASE_MAIN_SKILL` 释放边界。
方法:老 Laya 客户端**运行时**(`http://127.0.0.1:8090` 的 console 协议日志)为真相;Unity 侧**真连测试服 + Play 态**端到端取证。

> 头条结论:
> **老端第一条打怪链路已定位为真实协议序列**——主线任务 `100020` 完成后激活 `100030`,`DoTask` 自动寻路 →
> 怪物进视野(`12012`/`12007`)→ `send 20024 "c" 1`(进入战斗态)→ `send 20001`(攻击/技能释放请求,
> `h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle`)→ 服务端回 `20001`(伤害/命中广播,本轮采样 `recv 20001 ×1637`)。
> **Unity 侧技能点击链路已真实打通到 `SceneCombat.MainRoleAttackTarget` 并跑在真连测试服上**:
> 点击已学技能 `59100001 御剑一式`(obj=2 目标技能)→ `PressSkillHandler` → `SceneCombat` 从真实 `SceneManager` 寻敌。
> **真连实测**:角色 `云霄42852` Lv2 出生在场景 `10000 云来镇`(主城),`12002` 快照**字节精确** `怪物/采集=0 remaining=0B`,
> 34 个 NPC 但**零怪物**——`SceneCombat` 正确判定"无可攻击怪 → 真实阻塞,不假放",`CurrentTargetId` 始终 0。
> 即:**Unity 真实怪物链路成立(只是主城不下发怪),技能释放链路已接到真实寻敌入口,无怪时按老端语义记录真实阻塞、绝不造假怪/假伤**。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮在线,HTTP 200,len 25197) |
| 老端协议日志 | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(26020 行,含完整打怪/自动战斗协议流) |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`;游戏服 `ws://223.109.142.26:10000`(本轮自动登录冒烟落此服) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852`,career1 剑士 Lv2,复用第 3/4/5 轮角色) |
| Unity 取证方式 | 编辑器 **Play 态**(`autoLoginSmokeTest`+`autoEnterFirstRoleSmokeTest` 自动登录);临时验证 harness 在存活窗口内 dump `SceneManager` 怪物状态并触发技能释放(harness 已删,不入库) |
| Unity 证据 | 控制台 `[Skill]/[Combat]/[Scene]` 日志、`output/runtime_unity/_combat_verify_v6.txt`、`combat_v6_*.png` |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 均 gitignore,不入库;本报告以路径引用 + 文字/数据描述呈现证据。

---

## 1. 老端运行时真相:第一条打怪链路(`http://127.0.0.1:8090`)

### 1.1 战斗协议直方图(`console-2026-06-21T12-29-40-982Z.log`)

按 `send cmd =NNNNN` / `receive cmd = NNNNN` 统计,与打怪/战斗相关命中(节选):

| 协议 | SEND | RECV | 含义(对照老端源码) |
|---|---|---|---|
| `20001` | 680 | **1637** | 攻击/主技能释放请求与广播(`FightController.AttackRequest`→`WriteBegin(20001)`)。RECV 远多于 SEND = 服务端按每次攻击广播多条命中/伤害 |
| `20024` | 112 | 112 | 进入/退出战斗态(`SendFmtToGame(20024,"c",1/2)`),1:1 配对 |
| `12007` | — | 318 | 单怪进视野(`MonsterVo`)|
| `12012` | — | 124 | 九宫格对象增删(怪物/伙伴/其他/假人) |
| `20008` | — | — | 采集请求(`SendFmtToGame(20008,"iic",...)`,与攻击分流) |

**结论:** 老端运行态确有大量真实战斗:怪物进视野(`12007`/`12012`)+ 攻击请求/广播(`20001`)+ 战斗态切换(`20024`)。

> 截图说明:本轮第一条打怪链路的权威证据是上述 **console 协议流 + 老端源码**(§1.2/§1.3);
> 本轮另起无痕浏览器重连 8090 仅落到**登录页**(新会话无持久化账号/SDK 态,`cmdHits={}` 未进游戏),
> 故未重采到老端战斗内画面截图——老端战斗事实以协议流为准,战斗内截图待后续用持久化账号会话补采。

### 1.2 第一条打怪链路时间线(老端 console 实采)

从 `newest_finish_task_id=100020` 推进到第一次 `send 20001`(行号/时间戳为日志实采):

```
[279302ms] 30000 all_task_list: {100030: Array(1)}        ← 主线任务推进到 100030(承接 100020 完成)
[279405ms] ------DoTask------ TaskVo true                  ← 任务自动执行
[279405ms] taskmodel TaskSpeed: 5463 2678                  ← 任务自动寻路(朝目标怪/任务点移动)
[279789ms] receive cmd = 12012                             ← 九宫格对象刷新:怪物进入视野
[280671ms] send cmd =20024                                 ← 进入战斗态(20024 "c" 1)
[280676ms] send cmd =20001                                 ← 第一次攻击/技能释放请求
[280782ms] receive cmd = 20024 / receive cmd = 20001       ← 服务端回战斗态 + 攻击广播(命中/伤害)
... 此后 20001 send/recv 持续(自动战斗刷怪)
```

**老端第一条打怪链路 = 主线任务 `100030` → `DoTask` 自动寻路到怪 → 怪进视野(`12012`/`12007`)→
进入战斗态(`20024`)→ 攻击请求(`20001`)→ 服务端命中/伤害广播(`20001`)。**

### 1.3 老端技能释放源码链(`D:\GitProject\yu_client`,权威字节格式)

| 环节 | 文件:行 | 真实逻辑 |
|---|---|---|
| 点击技能 | `skill/MainUISkillItem.ts` | `Fire(SKILL_SHORTCUT_CLICK, skillVo.id, ONLY_FIRE_ATTACK=1)` |
| 按键处理 | `skill/SkillManager.ts:882` `PressSkillHandler` | `CanAttack` 闸 → `setCurrentSkillId` → 三分支:`carrer==52`→伙伴;`GetSelectType()==1`(自己)→`Fire(RELEASE_MAIN_SKILL)`;else→`Scene.MainRoleAttackTarget()` |
| 取目标 | `scene/Scene.ts:1282` `GetClickTarget` | 返回 `curr_click_target`,已死则清空(无目标时老端 `MainRoleAttackTarget` 直接 `Fire(RELEASE_MAIN_SKILL)` 空放;自动寻最近的 `FindTargets(.,1,.,10000)` 在源码中已注释) |
| 攻击怪 | `scene/Scene.ts:1970` `MainRoleAttackMonster` | 像素距离平方判定 `distance <= attack_range²` → 在范围:`SetDirection`(朝向)+`DoStand`→`Fire(RELEASE_MAIN_SKILL,...,monster.compress_id)`;超范围:`oper_mgr.StartTargetAction`(接近后回调释放) |
| 释放→请求 | `scene/fight/FightController.ts:755` | `Bind(RELEASE_MAIN_SKILL, ...)`→`AttackRequest`→`WriteBegin(20001)` |
| 攻击范围 | `skill/SkillManager.ts:492-508` `GetCurrentAttackRange` | `currentAttackRange_ = skill_distance * 0.8`;`return Math.max(100, currentAttackRange_)` → **下限 100 像素** |

**20001 攻击请求字节格式(`FightController.ts:800-814`,权威):**

```
WriteBegin(20001)
WriteFMT("h", request_fight_mon_count)        // 怪物数 u16
for each: WriteFMT("i", monster_instance_id)  // 怪物实例 id u32
WriteFMT("h", role_list.length)               // 玩家目标数 u16
for each: WriteFMT("l", role_id)              // 玩家 id u64
WriteFMT("ihhh", skill_id, floor(attack_x), floor(attack_y), floor(attack_angle))  // u32 + 3×u16(AOE 中心+角度)
SendToGame()
```

**20024 战斗态:** `SendFmtToGame(20024, "c", 1)`(进入)/ `(20024, "c", 2)`(退出),单字节 u8。

> 关键:`20001` 的怪物列表/AOE 中心由老端 **fight-movie 队列 + 碰撞收集链**(`ClientPrePlayFightMovie`→`ROLE_REQUEST_TOFIGHT`)产出,
> 非单点目标直发。本轮不移植该深水链、不猜 AOE 收集格式,故真实 `20001`/`20024` 发送列为**下一轮 blocker**(见 §5)。

---

## 2. P1:Unity 真实怪物目标来源(真连实测)

### 2.1 Unity 已有真实怪物数据链路(读源码)

| 层 | 文件 | 现状 |
|---|---|---|
| 协议层 | `Module/Core/Scene/SceneController.cs` | 已注册并解析 `12002`(场景快照)/`12007`(单怪)/`12012`(九宫格)/`12008`(移动)/`12009`(血量)/`12006`(删除),解析后落 `SceneManager` |
| 数据层 | `Module/Core/Scene/SceneManager.cs` | `_monsters`(实例 id→`MonsterVo`)+ `AllMonsters`/`GetMonster`/`MonsterCount` + 强类型事件 |
| VO | `Module/Core/Scene/Vo/MonsterVo.cs` | 字节顺序对齐 `pt_120 binary_12007`:`X/Y/InstanceId/TypeId/Hp/HpLim/CanAttack/Type/...`;`IsCollect`(采集物)/`IsBoss` 便捷判定 |
| 进场 | `Module/Core/Scene/SceneEntryFlow.cs` | `EVT_GAME_START`→`RequestEnterScene`→`12005`→`12100`(NPC)→`12002`(含怪物块) |

**即:Unity 的怪物链路本就是真实的——`12002`/`12007` 下发的怪物会进 `SceneManager`,可被技能寻敌读取,无需本轮新建。**

### 2.2 真连实测:场景 10000(云来镇/主城)字节精确零怪物

Play 态自动登录进游戏,控制台逐行(节选):

```
[Game]  role ready: 云霄42852 Lv.2 power=1040 scene=10000(4547,1610)
[Scene] request 12005: dunId=0 sceneId=10000
[Scene] 12005 ok: sceneId=10000 dunId=0 pos=(4547,1610)
[Scene] request 12100: local map loaded sceneId=10000
[Scene] npc render: id=100101 ... 云霄月华 / 玄清真人 / 逸尘 / 沈青岚 ...(34 个 NPC)
[Scene] request 12002: npc list loaded sceneId=10000
[Scene] 12002 快照: 玩家=1 怪物/采集=0 伙伴=0 其他=0 假人=0 remaining=0B   ← 字节精确:零怪物
```

`_combat_verify_v6.txt` 心跳时间线(harness 每秒 dump `SceneManager` 真实计数,持续 ~87s):

```
[beat t=36.4] scene=10000 monsters=0 npcs=0  ... hasBase=True  mainRole=False target=0
[beat t=37.4] scene=10000 monsters=0 npcs=34 skills=39 ...
[12002-snapshot] scene=10000 monsters=0 npcs=34 roles=1 mainRoleAgent=False
[beat t=38.4] scene=10000 monsters=0 npcs=34 ... mainRole=True target=0
...（t=38 → t=119 持续 monsters=0 npcs=34 mainRole=True）
```

**结论(P1):**
- Unity 真实怪物链路成立:`12002` 快照**字节精确解析**(`remaining=0B`,无错位),NPC(`12100`,34 个)/玩家(自身)正常下发。
- 场景 `10000 云来镇`(主城)**服务端确实不下发任何怪物**(`怪物/采集=0`)——属任务包 P1 预判的"服务端当前场景确实不下发怪物"分支,记录真实阻塞,不造假怪。
- 老端第一条打怪在主线任务 `100030` 自动寻路抵达的**含怪场景/任务点**,Unity 主线自动寻路打怪进含怪场景属下一轮(见 §5)。
- **附带发现**:完成完整进场景握手(`12005→12100→12002→12018/12020`)且主角已装配(`mainRole=True`)后,**会话存活 ~87s**(远超第 5 轮观察到的 10–15s 主动 remote-close),期间仅有 21002/13001 重连小抖动(`skills` 短暂 0→39)。即第 5 轮的 remote-close 在"完整走完进场景协议 + 主角装配"后显著缓解。

---

## 3. P2:`SKILL_SHORTCUT_CLICK → MainRoleAttackTarget` 最小真实闭环

### 3.1 Unity 本轮新增 `SceneCombat`(对标老端 `Scene.ts` 战斗方法)

新建 `Module/Core/Scene/SceneCombat.cs`,**目标 100% 来自真实 `SceneManager` + `RoleModel`,无造假**:

| 方法 | 对标老端 | 行为 |
|---|---|---|
| `MainRoleAttackTarget(skillId, attackType)` | `Scene.MainRoleAttackTarget` | `GetClickTarget()` ?? `FindNearestAttackableMonster()`;有怪走 `MainRoleAttackMonster`,无怪只记真实阻塞 |
| `GetClickTarget()` | `Scene.GetClickTarget` | 返回 `CurrentTargetId` 对应 `MonsterVo`,已死/已移除清空返回 null |
| `FindNearestAttackableMonster()` | `Scene.FindTargets(attack_type_all,1,.,10000)` | 遍历 `AllMonsters`,过滤 `!IsCollect && CanAttack==1 && Hp>0`,按主角像素距离平方取最近 |
| `MainRoleAttackMonster(mon,...)` | `Scene.MainRoleAttackMonster` | 距离平方 `dist²` 比较 `range²`;命中:朝向 + 释放边界;超范围:`MainRoleAgent.MoveToNpc` 接近后释放 |
| `ReleaseMainSkill(...)` | `Fire(RELEASE_MAIN_SKILL,...,compress_id)` | 发本地 `EVT_RELEASE_MAIN_SKILL(skillId, targetInstanceId)`;真实 20001 不发(不猜格式) |

攻击范围用老端下限 `100px`(`Math.max(100, skill_distance*0.8)` 的真实下限;精确倍率需 `config_skill` 攻击距离字段,下一轮接,不臆造)。

`SkillController.PressSkillHandler` 的 else 分支(`career!=52 && obj!=1`)→ `SceneCombat.Instance.MainRoleAttackTarget(skillId, attackType)`。
依赖单向 `Skill → Scene`(同 `Shenxiao.Module.Core` 程序集,无新增 asmdef,无 `Scene→Skill` 反向依赖)。

### 3.2 真连实测:技能点击 → 真实寻敌 → 无怪真实阻塞

Play 态 harness 触发已学技能 `59100001 御剑一式`(round5 实测 lv1 已学、obj=2 目标技能):

```
[Skill]  PressSkill skill=59100001 目标技能(obj=2)→ SceneCombat.MainRoleAttackTarget(真实 SceneManager 怪物寻敌)
[Combat] MainRoleAttackTarget skill=59100001: 无当前点击目标且 SceneManager 无可攻击怪(monsters=0)
         → 真实阻塞,不假放(对标老端无 click_target 分支)
```

`_combat_verify_v6.txt`:

```
>> FIRE EVT_SKILL_SHORTCUT_CLICK skill=59100001 trigger=skills-21002 monsters=0 role=(4547,1610)
<< AFTER fire: SceneCombat.CurrentTargetId=0
```

**真实边界推进:** 技能点击从第 5 轮"一行平铺日志"推进到"真实 `SceneManager` 寻敌入口":
`PressSkillHandler`(obj=2)→ `SceneCombat.MainRoleAttackTarget` → 遍历真实怪物表 → **场景 10000 无怪 → 按老端语义记录真实阻塞、不假放、`CurrentTargetId` 保持 0、不造假怪/假伤**。
`MainRoleAttackMonster`/接近/朝向/释放分支已代码就绪(`SceneCombat`),待进入含怪场景即可命中(下一轮)。

---

## 4. P3:玩家可见的最小战斗反馈(代码就绪 + 无真实数据项不造假)

P3 要求"P2 有真实目标后才做",且优先级从低风险到高风险。本轮真连场景 10000 无怪,**目标侧可见反馈在运行态被无怪阻塞**,但代码按老端语义就位、且**无真实数据的项一律不造假**:

| P3 项 | 本轮处置 |
|---|---|
| ① 选中目标/朝向/靠近目标 | **代码就绪**:`SceneCombat.FaceTarget`→`MainRoleAgent.FaceTowardPixel`(朝向);超范围→`MainRoleAgent.MoveToNpc`(直线接近 + 撞墙滑行 + 卡死/超时兜底)。真连实测 `mainRole=True`(3D 主角已装配),有怪即可见;无怪不触发 |
| ② 普攻/技能释放入口本地动作边界 | **已接**:本地 `EVT_RELEASE_MAIN_SKILL(skillId, targetInstanceId)` 释放边界事件 |
| ③ 真实 CD 圆遮罩(`CirCleCdView`)| **不做**:本轮无真实 CD 数据(21002/13007 未携释放 CD)→ 不显示假倒计时 |
| ④ 真实特效/挂点 | **不做**:释放特效资源/挂点未确认 → 不播假特效 |

**不为"看起来有战斗"补假飘字/假伤害/假怪死亡。**

---

## 5. 差异表

| 维度 | 老端运行时 | Unity 第 6 轮 | 结论 |
|---|---|---|---|
| 第一条打怪触发 | 主线 `100030` → `DoTask` 自动寻路到怪 | 主线自动寻路打怪未移植,角色停在主城 10000 | **差异(主线自动打怪下一轮)** |
| 怪物数据链路 | `12007`/`12012` → `monster_list` | `12002`/`12007`/`12012` → `SceneManager._monsters`(字节精确) | **对齐 ✓** |
| 当前场景怪物 | 含怪场景(任务点)有怪 | 场景 10000 云来镇(主城)`怪物=0`(服务端不下发) | **对齐 ✓(真实无怪,非缺陷)** |
| 技能点击分支 | `CanAttack`→career/obj 三支 | `CanAttack` 子集闸 + 真 config 三支 | **对齐 ✓** |
| 目标型技能寻敌 | `Scene.MainRoleAttackTarget`→`GetClickTarget`/`FindTargets` | `SceneCombat.MainRoleAttackTarget`→`GetClickTarget`/`FindNearestAttackableMonster` | **对齐 ✓(真实 SceneManager 寻敌)** |
| 无目标处置 | 无 click_target → 空放(需 fight 系统) | 无怪 → 记录真实阻塞,不假放 | **对齐 ✓(不造假)** |
| 范围/朝向/接近 | 像素距离² + `SetDirection` + `StartTargetAction` | 像素距离² + `FaceTowardPixel` + `MoveToNpc`(代码就绪) | **对齐 ✓(待含怪场景实跑)** |
| 攻击请求发送 | `20024 "c"` + `20001`(h+i×N+h+l×N+ihhh,fight-movie/AOE 链) | 仅到本地 `RELEASE_MAIN_SKILL` 边界,不发 20001 | **差异(真实协议发送下一轮)** |
| 技能 CD 圆遮罩 | `CirCleCdView` 接真实 cd | 无真实 cd → 不显示假 CD | **差异(战斗 cd 下一轮)** |
| 释放特效/伤害飘字 | fight-movie + `fight_damage_mgr` | 不播假特效/不补假飘字 | **差异(特效/结算下一轮)** |
| 会话存活 | 持续在线(分钟级) | 完整进场景 + 主角装配后 ~87s(第 5 轮为 10–15s) | **改善 ✓(完整握手缓解 remote-close)** |

---

## 6. 本轮结论

1. **老端第一条打怪链路定位完成:** 主线任务 `100030` → `DoTask` 自动寻路 → 怪进视野(`12012`/`12007`)→
   进入战斗态(`20024 "c" 1`)→ 攻击/技能释放请求(`20001`:`h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle`)→
   服务端命中/伤害广播(`recv 20001 ×1637`)。20001 怪物列表/AOE 中心由 fight-movie + 碰撞收集链产出。
2. **Unity 技能释放链路接到真实寻敌入口:** 新建 `SceneCombat`(对标老端 `Scene.MainRoleAttackTarget`/`MainRoleAttackMonster`/
   `GetClickTarget`),`PressSkillHandler`(obj=2)→ 从真实 `SceneManager` 怪物表寻最近可攻击怪。真连实测点击 `59100001` 走通到 `SceneCombat`。
3. **P1 真实结论:** Unity 怪物链路成立(`12002` 字节精确解析),但角色出生场景 `10000 云来镇`(主城)**服务端零怪物下发**;
   `SceneCombat` 正确判定"无可攻击怪 → 真实阻塞,不假放",`CurrentTargetId` 始终 0,**不造假怪/假伤/假 CD**。
4. **P3 处置:** 朝向/接近(`FaceTowardPixel`/`MoveToNpc`)代码就绪、`mainRole=True` 可见前提成立,被无怪运行态阻塞;
   无真实 CD/特效数据的项一律不做。

### 仍然阻塞项 / 差异(本轮只记录)

1. **主城无怪 → 无法运行态命中"取到怪"分支**:需主线自动寻路打怪进入含怪场景/任务(老端 `100030` 等价),= 下一轮。
2. **真实 `20001`/`20024` 攻击请求发送**:依赖老端 fight-movie 队列 + AOE 碰撞收集链(`ClientPrePlayFightMovie`→`ROLE_REQUEST_TOFIGHT`),
   格式串虽已采(§1.3),但 AOE 怪物列表收集逻辑深、本轮不猜不发,= 下一轮。
3. **技能 CD(`CirCleCdView`)/释放特效/伤害飘字/战斗结算** = 深水区,无真实数据不造假,= 下一轮。
4. **精确攻击范围**(`skill_distance*0.8`)需 `config_skill` 攻击距离字段,本轮用真实下限 `100px`,= 下一轮接字段。

### 下一轮建议

进入**真实含怪场景**:接主线任务自动寻路(`DoTask`/`TaskSpeed`)把主角带到 `100030` 等价的含怪任务点 →
`SceneCombat` 命中"取到真实怪"分支(朝向/接近/释放运行态可见)→ 再接真实 `20024`/`20001` 攻击请求(含 fight-movie/AOE 收集)与服务端命中广播。

---

## 7. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误 / 6 既有无关警告**(`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162),与第 5 轮同组,无新增。
- Play 态真连取证:`[Login]` 全链 → `[Game] role ready scene=10000` → `[Scene] 12002 快照 怪物/采集=0 remaining=0B` →
  `[Skill] PressSkill 59100001 → SceneCombat.MainRoleAttackTarget` → `[Combat] 无可攻击怪 → 真实阻塞`,均跑通;会话存活 ~87s。

---

## 8. 本轮改动清单(落 `Shenxiao.Module.Core` / `Framework`,不新增 asmdef)

| 文件 | 改动 |
|---|---|
| `Framework/Event/GlobalEvent.cs` | 新增 `EVT_RELEASE_MAIN_SKILL`(对标老端 `FightEvent.RELEASE_MAIN_SKILL`);更新 `EVT_SKILL_SHORTCUT_CLICK` 注释 |
| `Module/Core/Scene/SceneCombat.cs` | **新增**:`MainRoleAttackTarget`/`MainRoleAttackMonster`/`GetClickTarget`/`SetClickTarget`/`FindNearestAttackableMonster`(对标老端 `Scene.ts` 战斗方法;真实 `SceneManager` 寻敌 + 范围/朝向/接近 + 本地释放边界) |
| `Module/Core/Skill/SkillController.cs` | `PressSkillHandler` else 分支 → `SceneCombat.Instance.MainRoleAttackTarget`;更新类注释 |

> 验证用临时 harness(`Assets/Editor/_CombatVerifyHarness.cs`)与 `AppConfig.asset` 冒烟开关均已还原/删除,不入库。
