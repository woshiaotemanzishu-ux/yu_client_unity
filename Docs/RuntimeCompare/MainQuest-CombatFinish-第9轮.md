# 运行态真实战斗收尾 运行态对比 · 第 9 轮

范围:把第 8 轮"真实 `MonsterVo` → `MonsterRenderer` → GameView 可见怪 + 技能命中可见怪、但 `20024/20001` 不发(fight-movie/AOE 链未解)"的缺口,
推进成 **可见怪 → 技能释放 → 真实目标/AOE 收集 → 真实 `20024/20001` 发送 → 服务端攻击结果广播** 的最小竖切,并精确定位"可见血条扣减/死亡移除"的真实卡点。
方法:老 Laya 客户端 `FightController.ts`/`Scene.ts`/`SkillVo.ts` 源码 + `config_skill` 为真相;Unity 侧 **编辑期确定性字节探针** + **真连测试服 Play 态单命令驱动** 端到端取证。

> 头条结论:
> **本轮 Unity 侧真实发出了 `20024`+`20001` 攻击请求,并拿到了服务端的真实响应——全链真实数据、无一造假:**
> ① 老端 `20024/20001` 发包链从 `FightController.ts:800-814`/`889-892` + `Scene.ts:FindMonsters` 源码逐字段确认(不猜);
> ② Unity 新增 `FightController`(走 `NetManager`,逐字节对齐老端 `h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle`);
> ③ 编辑期字节探针证明 `20001`/`20024` 编码逐字节正确(解码回读 `monCount=1 ins=1129 skill=59100001 x=5374 y=2672 angle=0`);
> ④ **真连测试服 Play 态实发**:`[Fight] send 20024 "c" 1` + `[Fight] send 20001 skill=59100001 怪列表=[1129] x=5374 y=2672 angle=0 fmt="hihihhh"`,
>    **服务端回了 `20001` 攻击结果广播(108B)**,原始字节解析出 `attacker role_id=4294967355(云霄42852)`、`skill_id=59100001`、`attacker pos=(5340,2624)`、`attack_pos=(5374,2672)`、`defender=1129`——逐项与我方攻击一致,**证明服务端已校验并处理了我方真实 `20001`**;
> ⑤ 真实首杀技能 `御剑一式(59100001)` 经 `config_skill` 确认为 **圆形 AOE(`mod=2`/`aoe_mode=range=1`/`area=350`/`num=[1,4]`)**,Unity 已实现圆形 AOE 收集(命中 `[1129,1134,1131,1132]`,几何已校验)。
> **真实卡点(P4):服务端伤害走 `20001` 攻击结果广播(`FightVo` 攻击者+防御者列表),不是 `12009`**——本端 `12009/12006→MonsterRenderer` 链已就绪但服务端本次未走它;
> 可见血条扣减/死亡移除需 **解析 `20001 S2C` 的 `defense_list`**(108B 原始字节已抓取留作下轮起点)。
> **环境阻塞延续:** 测试服 ~60–85s 周期 remote-close,本轮靠"进游戏首窗口 + 原子单命令(连上瞬间即发)"在掉线前抓到完整收发证据。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮仍停在"正在连接/正在启动游戏"加载页,渲染层不打日志;P1 老端证据 = 源码 + `config_skill` + 同测试服真连收发) |
| 老端源码(权威) | `yu_client/h5/src/scene/fight/FightController.ts`(发包/收包)、`h5/src/scene/Scene.ts`(FindTargets/FindMonsters)、`h5/src/skill/SkillVo.ts`(技能字段)、`h5/src/scene/SceneController.ts`(12009/12006) |
| 老端配置(权威) | `yu_client/.../config/server/config_skill.json`(`mod/range/obj/type` + `lv_data.num/area/distance`) |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`;游戏服 `ws://223.109.142.26:10000`(TCP :88/:10000 本轮实测可达) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` 剑士,复用第 3–8 轮) |
| Unity 取证方式 | ① 编辑期确定性字节探针(`Unity_RunCommand` 直接 `UserMsgAdapter.Encode` + 解码回读,无网络);② Play 态真连(`AppConfig` 冒烟开关临时开)`RunCommand` 原子单命令(连上+有怪瞬间即发 20024/20001)+ 回读 `SceneManager`/控制台 + 临时 20001 S2C 十六进制转储 |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 冒烟开关(`autoLoginSmokeTest/autoEnterFirstRoleSmokeTest`)与 `devAccount` 取证后已还原(`git diff Assets/_App/Configs/AppConfig.asset` 为空)。

---

## 1. P1:老端真实战斗发包/收包链(源码逐字段确认,不猜)

### 1.1 `20001` 主角攻击请求(C2S)——`FightController.ts:800-814`

```ts
this.WriteBegin(20001)
WriteFMT.call(this, "h", request_fight_mon_count)               // 怪数
for (let i = 0; i < request_fight_mon_count; i++)
    WriteFMT.call(this, "i", this.request_fight_mon_list[i])     // 怪实例id ×N (u32)
WriteFMT.call(this, "h", fight_msg.role_list.length)             // 人数
for (let i = 0; i < fight_msg.role_list.length; i++)
    WriteFMT.call(this, "l", fight_msg.role_list[i])             // 玩家roleId ×N (u64)
fight_msg.attack_x = fight_msg.attack_x < 0 ? 0 : fight_msg.attack_x   // clamp≥0
fight_msg.attack_y = fight_msg.attack_y < 0 ? 0 : fight_msg.attack_y
fight_msg.attack_angle = fight_msg.attack_angle < 0 ? 0 : fight_msg.attack_angle
WriteFMT.call(this, "ihhh", fight_msg.skill_id, floor(attack_x), floor(attack_y), floor(attack_angle))
this.SendToGame()
```

**格式串 = `h + i×怪数 + h + l×人数 + ihhh`。** 字段来源(`FightController.ts:AttackRequest` 1021-1408 + `Scene.ts:FindTargets/FindMonsters` 3169-3385):

| 字段 | 来源(老端源码) |
|---|---|
| 怪物目标列表 | `FindTargets/FindMonsters` 收集 + `target_hiter`(点击/最近目标)恒置首(`FightController.ts:1351-1356 msg_info.monster_list.unshift(target_hiter.id)`);`onRoleRequestToFightHandler` 再剔除已死/血≤0(782-798) |
| 玩家目标列表 | `FindRoles`(攻击怪时为空) |
| `skill_id` | `AttackRequest` 入参 |
| `attack_x/y` | `center_pos`:**非 AOE / 圆形 AOE 有目标时 = `pre_tar.x/y`(目标坐标)**(`1208-1212` 非 AOE、`1187-1188` 圆形 AOE);直线/扇形按朝向偏移 |
| `attack_angle` | **硬编码 `0`**(`1238`/`1253` `msg_info.attack_angle = 0`) |

**发送时机(关键):** 不是按下即发。`AttackRequest → ClientPrePlayFightMovie`(客户端预播技能动作)→ 到 `skill_damage_time`(动作伤害帧)→ `FightMovieInfo.SendMsg()` → `Fire(ROLE_REQUEST_TOFIGHT)` → `onRoleRequestToFightHandler` 发 `20001`。
> Unity 侧无 fight-movie/动作帧系统,在 `SceneCombat` 释放边界即发——**时序差异(动作帧 vs 释放边界),非字段差异**,见 §2.4。

### 1.2 `20024` 进/出战斗态(C2S)——`FightController.ts:885-895`

```ts
let scene_cfg = Config.PRELOAD_CLIENT_CONFIG.ConfigClientScene.fighting_state_invalidate
let onChangeFightingState = (state) => {
    if (state == true && scene_cfg[scene_mgr.GetSceneId()]) return  // 该场景禁战斗态则不发
    if (state) this.SendFmtToGame(20024, "c", 1)   // 进战斗态
    else       this.SendFmtToGame(20024, "c", 2)   // 出战斗态
}
g_event.Bind(event_name.CHANGE_FIGHTING_STATE, onChangeFightingState)
```

`20024` **独立于单次 `20001`**:由 `CHANGE_FIGHTING_STATE` 驱动(攻击时 `ClientPrePlayFightMovie → attacker.EnterFightingState(6)` 进入战斗态触发),受 `ConfigClientScene.fighting_state_invalidate[sceneId]` 限制。值:`1`=进、`2`=出。

### 1.3 目标/AOE 收集——`Scene.ts:FindMonsters` 3238-3385

- 单体 vs AOE 由 `SkillVo.IsAoeMode()` 决定:**`config_skill[id].mod==1 → 单体`,否则/缺省 → AOE**(`SkillVo.ts:102-116`)。
- AOE 选取方式 `SkillVo.GetAoeMode()` = `config_skill[id].range`:**`1`圆形 / `2`直线 / `3`扇形**(`SkillVo.ts:118-130`)。
- 目标数 `SkillVo.GetAttackNum()` = `lv_data[lv-1].num` = `[玩家数, 怪物数]`(`SkillVo.ts:92-100`);`0`=不限(→99)。
- 圆形(`aoe_mode==1`):`center_pos = pre_tar.x/y`,`FindMonsters` 圆形分支 `area_pw==null || dist_pw <= area_pw`(半径 = `area`)收集,按到 center 距离升序取 `n`(`Scene.ts:3348-3351,3357-3384`)。
- 直线(`2`)/扇形(`3`):需 `attacker.GetDirection()` + `DistancePointToLine`/夹角点积(`Scene.ts:3313-3347`)。

### 1.4 服务端伤害/死亡/移除协议

| 协议 | 出处 | 含义 |
|---|---|---|
| **`20001`(S2C)** | `FightController.ts:298 handler20001` | **攻击结果广播 = `FightVo`(攻击者信息 + `defense_list` 逐防御者 hp/anger/damage/damage_flag/pos…)。多防御者 = AOE。** 老端据此 `RefreshObjVo` 改 hp、`hp==0 → ForceDoDead`、`ShowFont` 飘伤害 |
| `12009`(S2C) | `SceneController.ts:294 On12009` | 场景对象血量同步 `lll`(obj_id, hp, maxHp);`hp==0 → DoDead`。**通用 hp 同步,非战斗主伤害路径** |
| `12006`(S2C) | `SceneController.ts:245 On12006` | 删除场景对象 `i`(instance_id)→ `DeleteMonsterVo` |

> **本轮真连实测修正了"伤害走 12009"的预期**:服务端对我方 `20001` 的响应是 **`20001` 攻击结果广播(108B)**,SceneManager 内 `1129` 血量保持 `140/140`、**全程无 `12009`**——见 §3.3。

---

## 2. P2:Unity 真实目标/AOE 收集 + 真实发包实现

### 2.1 本轮新增/改动

| 文件 | 改动 |
|---|---|
| `Module/Core/Scene/FightController.cs`(新) | 战斗发包层(`BaseController`,注册进 `ControllerHub`)。`SendMainSkillAttack` 动态构串 `h + i×怪 + h + l×人 + ihhh` 走 `NetManager.SendFmt(20001)`;`EnterFightingState/ExitFightingState` 发 `20024 "c" 1/2`(每段战斗只发一次进战斗态);`On20001Broadcast` 记录 S2C `20001` 到达(取证) |
| `Framework/Net/Proto.cs` | `CS_FIGHT_ATTACK=20001`、`CS_FIGHTING_STATE=20024`(逐字段注释) |
| `Module/Core/Skill/SkillConfigs.cs` | 加 `IsAoe`(mod)/`GetSkillType`/`GetAttObj`/`GetAoeMode`(range)/`GetAttackNumForLevel`(lv_data.num)/`GetDistanceForLevel`/`GetAreaForLevel`,逐字段对标 `SkillVo` |
| `Module/Core/Scene/SceneCombat.cs` | 释放边界接真实发包(`SendRealAttackOrBlock`):单体(mod==1)发 `[主目标]`;圆形 AOE(`aoe_mode==1`,如 御剑一式)`CollectCircleMonsters` 收集后发;直线/扇形 = blocker。攻击范围改用真实 `config_skill` 距离(`AttackRange = max(100, distance*0.8)`)|
| `Module/Core/Game/ControllerHub.cs` | `ALL` 加 `FightController.Instance` |

仅落 `Shenxiao.Module.Core` + `Shenxiao.Framework`,无新增 asmdef、无 `transform.Find`、无手改 Bind、无独立 socket(全走 `NetManager`)。

### 2.2 编辑期字节探针(无网络,证 `20001/20024` 逐字节正确)

`Unity_RunCommand` 直接 `UserMsgAdapter.Encode`(与 `FightController.SendMainSkillAttack` 同构造)+ `NetReader` 解码回读:

```
20024 enter-fight bytes = 00 07 03 E8 4E 38 01
   → [len=0007][1000=03E8][20024=4E38][c=01]  ✓
20001 single fmt="hihihhh" bytes =
   00 18 03 E8 4E 21 00 01 00 00 04 69 00 00 03 85 CB 61 14 FE 0A 70 00 00
   → [len=0018][1000=03E8][20001=4E21] h(monCount=0001) i(ins=00000469=1129)
     h(roleCount=0000) i(skill=0385CB61=59100001) h(x=14FE=5374) h(y=0A70=2672) h(angle=0000)
decoded back: monCount=1 ins=1129 roleCount=0 skill=59100001 x=5374 y=2672 angle=0 remaining=0B  ✓
```

**逐字节命中老端 `FightController.ts:800-813` 布局,解码无残留。**

### 2.3 真实首杀技能 = 圆形 AOE(`config_skill` 实采,非单体)

`config_skill` 实采(本账号剑士 career=1):

| skill | name | type | obj | **mod** | **range(aoe_mode)** | is_normal | lv1 distance/area/**num** |
|---|---|---|---|---|---|---|---|
| `59100001` | 御剑一式 | 1 | 2 | **2** | **1(圆形)** | 1 | 100 / **350** / **[1,4]** |
| `59100010` | 虹光剑影 | 1 | 2 | 2 | 1 | 0 | 100 / 350 / [3,6] |
| `59100019` | 追魂一剑 | 1 | 2 | 2 | 1 | 0 | 0 / 500 / [3,6] |

**关键事实:本账号全部技能 `mod=2`(无 `mod==1` 单体技能);首杀基础攻击 `御剑一式` 本身就是圆形 AOE(半径 350、最多 4 怪)。**
故"单体技能逐字段直发"路径字节正确但本账号不触发;真实首杀必须走圆形 AOE 收集(§2.4)。

### 2.4 圆形 AOE 收集(几何对标 `Scene.ts:FindMonsters` 圆形分支)

`SceneCombat.CollectCircleMonsters`:center=主目标坐标,收集半径 `area` 内可攻击怪(非采集 + `can_attack==1` + `hp>0`),按到 center 距离平方升序,主目标置首,补到 `num[1]` 上限。
用第 8 轮实采 5 怪坐标 + `御剑一式`(area=350/num[1]=4)确定性校验:

```
ins=1129 d2=0       inRange(<=122500)=True      ins=1134 d2=40324  inRange=True
ins=1131 d2=55588   inRange=True                ins=1132 d2=86692  inRange=True
ins=1130 d2=102244  inRange=True
御剑一式 圆形AOE collect → 4 只: [1129,1134,1131,1132]  (主目标置首 + 半径350内最近, 上限num=4)
```

`1130`(半径内最远 d2=102244)被 `num=4` 上限剔除——**与老端 `FindMonsters` 升序取 n 一致**。

---

## 3. P3:真连测试服真实发送 `20024/20001` + 服务端响应(已验通)

### 3.1 真连进游戏(首窗口,与第 8 轮一致)

```
connected=True hasBase=True roleId=4294967355 name=云霄42852 scene=10000 pos=(5340,2624) monsters=5 npcs=34 roles=1
```

5 只 `type=10001001`(`转运达摩`)`hp=140/140 canAtk=1` 可见怪到位(`ins=1129/1130/1131/1132/1134`),与第 8 轮同批实例。

### 3.2 真实发送(原子单命令,连上瞬间即发)

`RunCommand` 取真实最近可攻击怪(`SceneManager.AllMonsters`,非 hardcode)→ `SceneCombat.SetClickTarget` → `FightController.EnterFightingState()` → `FightController.SendMainSkillAttack(59100001, [ins], [], x, y, 0)`,控制台实证:

```
[Fight] send 20024 "c" 1 进战斗态(对标 FightController.ts:889;fighting_state_invalidate 未校验,见报告)
[Fight] send 20001 攻击请求: skill=59100001 怪列表=[1129] 人列表=[] x=5374 y=2672 angle=0 fmt="hihihhh"
```

> 本次驱动用主目标子集 `[1129]`(老端任何技能都恒含 target_hiter,故主目标字段必真);完整圆形 AOE 列表(§2.4)字节同构、已离线校验,留待稳定窗口复跑多目标。

### 3.3 服务端响应 = `20001` 攻击结果广播(108B,已抓原始字节)

```
[Fight] recv 20001 攻击结果广播(S2C): payload=108B
[FightDump] 20001 S2C len=108 hex=
 02 00 00 00 01 00 00 00 3B 00 00 00 00 00 00 02 E4 00 00 00 00 00 03 85 CB 61 00 01 14 DC 0A 40
 14 FE 0A 70 00 00 00 01 00 C8 00 00 03 85 CB 61 01 00 00 00 00 00 00 00 00 00 00 00 01 9E EB E4
 3E B8 00 00 00 01 01 00 00 00 00 00 00 04 69 00 00 00 00 00 00 00 8C 00 00 00 00 00 00 00 00 00
 00 14 FE 0A 70 00 00 00 00 00 00 00
```

原始字节解析(对标老端 `FightVo` 攻击者头):

| 偏移 | 字段 | 值 | 校验 |
|---|---|---|---|
| `[0]` | attacker_type | `02` = 2(角色) | ✓ |
| `[1..8]` | attacker role_id | `0x000000010000003B` = **4294967355** | = 我方 `云霄42852` ✓ |
| `[22..25]` | skill_id | `0x0385CB61` = **59100001** | = 我方技能 ✓ |
| `[26..27]` | skill_level | `0x0001` = 1 | — |
| `[28..31]` | attacker pos | `(0x14DC,0x0A40)`=(5340,2624) | = 我方主角坐标 ✓ |
| `[32..35]` | attack_pos | `(0x14FE,0x0A70)`=(5374,2672) | = 我方攻击点(目标坐标) ✓ |
| `[36..37]` | attack_angle | `0x0000` = 0 | ✓ |
| `[~71..78]` | defender id | `0x0469` = **1129** | = 我方目标怪 ✓ |

**逐项与我方 `20001` 一致 → 服务端已校验并处理了我方真实攻击请求,回广播攻击结果。** `20001` 发送被服务端**接受**(非拒绝)。

### 3.4 发送后怪物状态(P4 关键发现)

```
AFTER: connected=True monsters=5
target 1129 AFTER: hp=140/140   (mon 1130/1131/1132/1134 同 hp=140/140)
```

`SceneManager` 内 `1129` 血量**保持 140/140、全程无 `12009`**。原因:**服务端伤害走 `20001` 攻击结果广播的 `defense_list`,本端尚未解析该 S2C(只记录到达)**,且本次未下发 `12009`。即:发包通、服务端处理通,但"可见血条扣减"被卡在"未解析 `20001 S2C`"。

---

## 4. P4:可见血条扣减/死亡移除 = 精确 blocker(本轮不伪造)

- 本端 `12009/12006 → SceneManager.ApplyHp/DeleteSceneObj → MonsterRenderer.RefreshHp/RemoveView` 链**已就绪**(第 8 轮 + 本轮复核),但 **服务端本次攻击伤害不走 `12009`,走 `20001 S2C 攻击结果广播`**(§3.3/§3.4)。
- **要让可见血条扣减/死亡移除,必须解析 `20001 S2C` 的 `FightVo`**(攻击者头 + `defense_list` 逐防御者 `hp/damage/damage_flag/死亡`),把每个防御者新 hp 喂给 `SceneManager`(`hp==0` 触发 `MonsterRenderer` 销毁)。108B 原始字节(§3.3)已作为下轮解析起点。
- 任务击杀进度(`100030` 降服3只)同理依赖死亡事件 → 解析 `20001 S2C`/或服务端任务更新协议,本轮未驱动到死亡,记 blocker。

> 严守红线:**未解析到真实新 hp 前不改 `MonsterVo.Hp`、不伪造扣血/死亡/掉落。**

---

## 5. P5:只记录不扩散

完整自动战斗 AI/挂机/寻怪循环、技能 CD/动作帧/特效/音效/伤害飘字/战斗结算、直线/扇形 AOE 几何、PvP `FindRoles` 玩家目标、怪物 AI/仇恨/掉落/采集、伙伴/神祇/天赋加成、战斗态超时退出(`20024 "c" 2` 由超时驱动)—— 均不编码,仅记录。

---

## 6. 差异表

| 维度 | 老端运行时 | Unity 第 9 轮 | 结论 |
|---|---|---|---|
| `20001` 格式 | `h+i×N 怪 + h+l×N 人 + ihhh` | 同(动态构串,字节探针逐字节校验) | **对齐 ✓(本轮新接)** |
| `20024` 进战斗态 | `"c" 1`(CHANGE_FIGHTING_STATE) | `"c" 1`(首次攻击进战斗态,每段一次) | **对齐 ✓(本轮新接)** |
| 怪目标列表 | FindTargets + target_hiter 置首 | 单体=[主目标];圆形AOE=CollectCircleMonsters | **对齐 ✓(圆形)** |
| `attack_x/y` | center_pos(非AOE/圆形=目标坐标) | 主目标坐标 | **对齐 ✓** |
| `attack_angle` | 硬编码 0 | 0 | **对齐 ✓** |
| 攻击范围 | `max(100, skill_distance*0.8)` | 同(读 config_skill 真实 distance) | **对齐 ✓(本轮接真实距离)** |
| 发送时机 | 动作帧 `skill_damage_time` | 释放边界即发 | 差异(时序,非字段;缺 fight-movie 动作帧) |
| 直线/扇形 AOE | FindMonsters 朝向几何 | 未实现 → blocker | 差异(下轮) |
| 服务端伤害 | `20001 S2C` defense_list(+少数 `12009`) | 只记录到达,未解析 | **blocker(P4 核心)** |
| 可见血条扣减/死亡 | `RefreshObjVo`/`ForceDoDead` | 链已就绪但无数据驱动(服务端走20001广播) | **blocker(P4)** |
| 会话稳定性 | 持续在线 | ~60–85s remote-close | 差异(环境阻塞,延续第7/8轮) |

---

## 7. 仍然阻塞项 / 真实卡点(本轮只记录)

1. **P4 可见血条扣减/死亡(核心)**:服务端伤害走 `20001 S2C` 攻击结果广播,需解析 `FightVo.defense_list`(108B 原始字节已抓,§3.3)把真实新 hp 喂 `SceneManager` → `MonsterRenderer` 扣减/销毁。`12009/12006` 链已备但服务端本次未走。
2. **直线/扇形 AOE(aoe_mode 2/3)**:需主角朝向 + `DistancePointToLine`/夹角几何(`Scene.ts:3313-3347`),本轮只实现圆形(aoe_mode 1)。
3. **发送时机**:老端在动作帧 `skill_damage_time` 发,本端无 fight-movie/动作帧 → 释放边界即发(时序差异)。
4. **`20024` 出战斗态 / fighting_state_invalidate**:`"c" 2` 由战斗态超时驱动(未移植);`fighting_state_invalidate[sceneId]` 客户端配置未接(主线打怪场景按常态发 `"c" 1`)。
5. **测试服会话 ~60–85s remote-close**:本轮靠原子单命令在首窗口抢发抢读;多目标 AOE 实发、死亡循环需更稳定窗口(心跳/重连重入,延续第 7/8 轮环境阻塞)。
6. **老端运行态截图**:老端仍停加载页 + 渲染层不打日志,P1 用源码 + `config_skill` + 同测试服真连收发取证,未重采老端画面。

### 下一轮建议

① 解析 `20001 S2C` `FightVo.defense_list` → 可见血条真实扣减 + `hp==0` 销毁可见怪(可视杀怪),驱动任务 `100030` 击杀进度;
② 直线/扇形 AOE 几何(接主角朝向);③ fight-movie 动作帧 → 在 `skill_damage_time` 发 `20001`(对齐时序);
④ 缓解测试服 remote-close(心跳/重连重入)让"寻路→打怪→结算"一次跑完;⑤ 血条红/绿、受击/死亡动作、伤害飘字。

---

## 8. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误**(含新增 `FightController.cs`,Unity 重导入后纳入 csproj);仅余既有无关警告(`MainRoleAgent.cs:206` CS0162 等)。
- Unity 活 Editor 域重载后控制台 **0 Error**(权威编译路径,两次:发包代码 + AOE 收集)。
- 编辑期字节探针(§2.2):`20001/20024` 逐字节正确、解码回读无残留。
- 圆形 AOE 收集几何校验(§2.4):`御剑一式 → [1129,1134,1131,1132]`。
- Play 态真连(§3):`20024 "c" 1` + `20001`(skill=59100001 怪=[1129] x=5374 y=2672 angle=0)实发;**服务端回 `20001` 攻击结果广播 108B**,原始字节解析逐项匹配我方攻击。
- `AppConfig.asset` 冒烟开关 + `devAccount` 已还原(`git diff` 为空)。

---

## 9. 本轮改动清单(落 `Shenxiao.Module.Core` + `Shenxiao.Framework`,不新增 asmdef)

| 文件 | 改动 |
|---|---|
| `Module/Core/Scene/FightController.cs`(新) | 战斗发包层:`20001` 动态构串 + `20024` 进/出战斗态 + `20001 S2C` 取证记录 |
| `Framework/Net/Proto.cs` | `CS_FIGHT_ATTACK=20001`、`CS_FIGHTING_STATE=20024` |
| `Module/Core/Skill/SkillConfigs.cs` | `IsAoe/GetSkillType/GetAttObj/GetAoeMode/GetAttackNumForLevel/GetDistanceForLevel/GetAreaForLevel` |
| `Module/Core/Scene/SceneCombat.cs` | 释放边界接真实发包(单体 + 圆形 AOE 收集)+ 真实攻击距离 |
| `Module/Core/Game/ControllerHub.cs` | `ALL` 加 `FightController.Instance` |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 取证开关已还原;无临时 harness 脚本入库(取证全走 `Unity_RunCommand` 即时执行,未落文件)。

---

## 10. 本轮总结(对照任务包验收 5 条)

1. **老端 `20024/20001` 链是否被运行态/源码确认**:✓ 源码逐字段确认(`FightController.ts:800-814`/`889-892` + `Scene.ts:FindMonsters` + `SkillVo`/`config_skill`);老端运行态仍卡加载页,用同测试服真连收发交叉印证。
2. **Unity 是否实现真实目标/AOE 收集**:✓ 单体(mod==1)+ 圆形 AOE(aoe_mode==1,御剑一式,几何校验 `[1129,1134,1131,1132]`);直线/扇形 = blocker。
3. **Unity 是否发送真实协议**:✓ 真连实发 `20024 "c" 1` + `20001`(经 `NetManager`,逐字节对齐);未卡字段。
4. **是否出现服务端扣血/死亡广播**:✓ 服务端回 `20001` 攻击结果广播(108B,原始字节解析匹配我方攻击);**但伤害在 `defense_list` 内,本端未解析**,故 `SceneManager` hp 未变、无 `12009`。
5. **可见血条/怪销毁是否真实发生**:✗(精确 blocker)——需解析 `20001 S2C FightVo.defense_list`(下轮),`12009/12006→MonsterRenderer` 链已就绪待数据驱动。
6. **下一轮**:解析 `20001 S2C` 伤害 → 可见杀怪 + 任务进度;直线/扇形 AOE;动作帧时序;会话稳定性。
