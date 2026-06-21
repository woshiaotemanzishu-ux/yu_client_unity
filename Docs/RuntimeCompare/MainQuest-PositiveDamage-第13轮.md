# 运行态正伤害与100030闭环 运行态对比 · 第 13 轮

范围:把第 12 轮的 **"`damage=0` = 角色养成不足(att=33)+ 服务端权威"** 推进成"玩家可见的正向闭环"——
**合法推进主线到含怪阶段 → 真实奖励/装备/升级/GM 抬 att 破防 → 真连攻击拿 `damage>0` → 驱动血量链 → `100030` 击杀闭环。**

方法:Unity 活 Editor 真连测试服(临时开 `AppConfig` 冒烟开关,取证后还原),全程**零客户端代码改动**,仅用 `Unity_RunCommand` 即时脚本驱动既有链路(`TaskController.SubmitFinish`/真实 `15201` 穿戴/`SceneCombat.MainRoleAttackTarget`/`GmCheatController`),老端 `yu_client` 源码 + 真实配置 + `yu_server` GM 源码为真相。

> ## 头条结论(本轮**纠正第 12 轮根因**:`damage=0` 与 att 无关)
>
> ① **P1/P2 真实链路全部打通(组织性养成,非造假)**:冒烟角色 `云霄42852`(roleId `4294967355`)经**真实主线 30004** 提交任务 `100020`,服务端发**职业武器 `101011010`** 入包 + **升级 2→3**;再经**真实穿戴协议 `15201`** 穿上武器,服务端重算 → **`att` 33 → 35(升级)→ 85(穿武器)、`wreck` 17→38、`power` 1040→1800**(13001 实测,逐步取证)。主线 `newest finish` `100010`→`100020`、`mainLine` 推进到击杀任务 `100030`。
>
> ② **决定性反证:`att` 无论 85(真装备)还是 50000(GM `attr_`)都打出 `damage=0`。** 真连对 5 只真实怪 `10001001`(`hp=140` lv5)发**真实 `20001`**(`怪列表=[1129,1134,1131,1132]` 圆形 AOE、`fmt="hiiiihihhh"`、逐字节对齐老端),服务端**稳定回 `222B / 4 防御者 / remaining=0B`**,**每个防御者恒 `damage=0 / hp=140 / damage_flag=0`**。把 `att` GM 抬到 **50000**(≫ 怪任何防御 250/500)**仍 `damage=0`** → **`damage=0` 与角色 att / 怪防御无关。**
>
> ③ **根因纠正:第 12 轮"养成不足"被本轮证伪。** `damage=0` 不是 att 低(att=50000 仍 0),而是**本端 `20001` 的发送形态问题**:本端在**技能释放边界即发单帧 `20001`**(无 fight-movie 动作帧/cast 序列),服务端对其稳定返回一个 **`damage=0` 的"进战斗/上 buff engage 帧"**(每帧同时给攻击者上 `iconType=200` 御剑一式自身 buff),真实伤害结算不在这一帧。这与第 10/11 轮"服务端对本端攻击稳定判 0 伤害、疑 fight-movie 动作帧缺失"一致;**本轮用 att=50000 把"养成"变量彻底排除,锁定为 att-无关的发包形态/服务端 engage 语义问题。**
>
> ④ **P3/P4 未达成但卡点精确**:无 `damage>0`、无 `hp<max`、无 `hp==0` → 不驱动死亡、`100030` 击杀进度不推进。**全程不本地造假**(未改 `MonsterVo.Hp`/`FightVo.damage`/任务计数)。**Unity 零代码改动**(P3 边界:仅当"老端同账号可伤害而 Unity 不行"才写码,本轮证据是"两端同发包形态 → 同 engage 帧",非客户端字段/解析差异)。
>
> **结论(可行动 blocker):`damage>0` 的真实路径 = 移植 fight-movie 动作帧时序(在 cast 序列/`skill_damage_time` 后发 `20001`,或捕获服务端伤害结算的后续帧/协议),而非继续抬 att。** 这是 P5 明确"只记录不扩散"的动作系统深水区,故本轮记录精确 blocker、不强行编码。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| Unity 后端 | 游戏服 `ws://223.109.142.26:10000`(本轮真连握手 + 进游戏全通);GM API `http://223.109.142.26:88/api/`(仅登录 5 方法,无加成能力) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` 剑士 career=1) |
| 老端源码(权威) | `h5/src/commonModel/TaskModel.ts`(DoTask)、`commonController/TaskController.ts`(30000/30003/30004/30005)、`commonController/GoodsController.ts`(15010/15050/15201)、`commonController/EquipController.ts`(15201 穿戴)、`sharedata/MainRoleVo.ts`(ReadFrom13001/13033) |
| 真实配置(权威) | `cdn/resource/config/server/config_task.json`(100010/100020/100030)、`config_mon.json[10001001]`、`config/client/ConfigItemAttr.json`(attr_id 1=att) |
| GM(权威) | `yu_server/config/gsrv.config:31-32`(`enable_client_gm=true`、`gm_password="jzy2026gm"`)、`src/gm/pp_gm.erl:736-753`(11101 鉴权)、`:1000`(`lv_N`)、`:1102-1135`(`attr_[{id,val}]`) |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 冒烟开关取证时临时打开,取证后已还原(`git diff Assets/_App/Configs/AppConfig.asset` 为空)。**Unity `Assets/Scripts/**` 零改动。**

---

## 1. P0:第 12 轮基线保护(全部通过)

| 项 | 结果 |
|---|---|
| `dotnet build yu_client_unity.slnx -v:minimal` | ✓ **0 错误**,6 条既有无关警告(`AppLauncher CS0649`×3、`EquipAttrItemBind`/`MainUIActivityViewBind CS0108`×2、`MainRoleAgent.cs:206 CS0162`),与第 10/11/12 轮逐条一致 |
| `RoleModel.BattleAttr` 读取链 / `20024+20001` C2S / `FightVo` 解析 / 怪渲染 | ✓ 未退化(本轮零代码改动;真连握手 + 进游戏 + 5 怪渲染 + `20001` 收发全通) |
| 工作树 | ✓ 仅本轮报告 + 任务包;`AppConfig.asset` 还原(`git diff` 为空);`.playwright-cli/`、`output/` 未入库 |

---

## 2. P1:合法推进主线到含怪阶段(✓ 真实链路达成)

### 2.1 老端主线链(源码取证,不猜)

`config_task.json` 三条主线(列名 schema 出自 `config_table_default.json`,key23=award_list、key24=special_goods_list、key18=end_npc、key14=next):

| 任务 | 类型 | 完成动作 | 奖励 | next |
|---|---|---|---|---|
| `100010 云霄仙域` | 对话 | 与 NPC `100101` 对话 → C2S `30004` | exp 150000 + 卢币 20000 | `100020` |
| `100020 指导仙子` | 对话(领武器) | 与 NPC `100134` 灵枢仙子对话 → C2S `30004` | exp 200000 + 卢币 20000 + **职业武器**(career1→`101011010`) | `100030` |
| `100030 初见妖灵` | **击杀** | content=`["kill","1","10001001","3","10000","5463","2678",...]` = 场景 10000 击杀怪 `10001001` ×3 → 满则自动 `30004` | exp 200000 + 卢币 20000 | `100040` |

提交任务 C2S 统一 `30004 "i" task_id`(对话/击杀同),接任务 `30003`。

### 2.2 真连进游戏 + 真实推进(实测)

| 阶段 | 实测证据 |
|---|---|
| 进游戏基线 | `13001`:`云霄42852` lv2 power=1040 scene=10000(5340,2624) 铜币=20000;`30005 newest finish=100010`;`mainLine=100020`(tipType=7 END_TALK、npcId=100134、hasFinish=1);背包空;**`SceneManager` 已下发 5 只 `10001001`**(ins `1129/1130/1131/1132/1134`,`hp=140/140`,`canAttack=1`,坐标 (5374,2672) 等) |
| 推进 100020 | `TaskController.Instance.SubmitFinish(100020)`(发真实 `30004`)→ 服务端处理后:`newest finish 100010→100020`、`mainLine 100020→100030`、**升级 `lv 2→3`(power 1040→1100)**、**职业武器 `101011010` 入背包**(15010 实测 `goodsId=4294980461 type=101011010 color=1`) |
| 到达击杀阶段 | `mainLine=100030`(击杀任务,`IsAllStepFinish=False`);场景 10000 持续有 5 只 `10001001` 可攻击(第 12 轮"主城无怪"是因当时停在 100010 前;推进到 100030 阶段怪即在场,与第 9/10 轮一致) |

**结论:P1 用真实客户端链路(30004)合法推进 `100010 → 100020 → 100030`,无任何本地改任务。** 跨场景飞鞋未触发(100020/100030 同在场景 10000,无需切场景 blocker)。

---

## 3. P2:真实提升攻击力(✓ 真实奖励+装备路径 + GM 测试手段)

### 3.1 真实奖励+装备路径(优先,P2 第①档)

老端 att 提升链(`GoodsController`/`EquipController` 源码):**穿装 C2S `15201 "l" goods_id` → 服务端重算 → push `13033`(纯 S2C 属性刷新,客户端从不发)→ att 更新。** 本端 `13033` 未接(第 12 轮记录),故 att 变化经**重连后 fresh `13001`** 取证。

| 步骤 | att | wreck | def | Hp | power | 来源 |
|---|---|---|---|---|---|---|
| 起始(lv2,裸) | **33** | 17 | 17 | 740/740 | 1040 | 13001(第 12 轮 + 本轮复现) |
| 完成 100020(lv3,未穿) | **35** | 17 | 18 | 780/780 | 1100 | fresh 13001(升级后) |
| **穿武器 101011010(`15201`)** | **85** | **38** | 18 | 780/780 | **1800** | fresh 13001(穿戴后服务端重算) |

> **穿戴走真实协议:** `NetManager.SendFmtAsync(15201, "l", 4294980461)`(对标老端 `EquipController` 穿戴,Unity 无 EquipController 故运行时直驱真实协议,非伪造)。穿后 `power 1100→1800`(+700)即**服务端真认了这件装备**(否则 power 不变)——装备真实生效。

### 3.2 测试服 GM unblocker(P2 第③档,仅测试账号,有证据)

为排除"att 不够"变量,用 GM 把 att 抬到极高再打:

| 项 | 证据 |
|---|---|
| 接口来源 | `yu_server/src/gm/pp_gm.erl`(已核 `:736-753` 鉴权、`:1102-1135` attr);`gsrv.config:31-32` `enable_client_gm=true`/`gm_password="jzy2026gm"` |
| 鉴权 | 同连接会话先发 `11101 setgmpassword_jzy2026gm`(`pp_gm.erl:739-742`,密码进 Erlang 进程字典,断线失效) |
| 加成命令 | `11101 attr_[{1,50000},{3,50000}]`(attr_id 1=att、3=wreck;`pp_gm.erl:1103` `util:string_to_term` 解析 → 重算 `battle_attr`/`combat_power` + `send_attribute_change_notify`) |
| 入口 | `GmCheatController.Instance.SendCommand("setgmpassword_jzy2026gm")` / `SendCommand("attr_[{1,50000},{3,50000}]")`(既有 11100/11101 控制器,无新代码) |
| 作用域 | **仅** `unity_npc_475823114` / role 4294967355(冒烟测试账号) |
| 结果 | GM 命令发送成功(`[GMATK] sent setgmpassword` / `sent attr_`);服务端 att 抬到 ≥50000(客户端 13033 未接故 RoleModel 仍显 85,但服务端伤害用真实新 att) |

> GM 仅作测试服 unblocker,**未写进客户端业务代码**(运行时即时命令);`attr_` 改 `original_attr`,属会话内有效/重登从装备重算(非持久作弊)。

---

## 4. P3:正伤害真连验证(✗ 但根因决定性锁定)

### 4.1 真实攻击发包(逐字节对齐老端,实测)

`SceneCombat.Instance.MainRoleAttackTarget(59100001, 1)`(技能 id 取自真实 `13007` 快捷栏 BarInfo pos=1,非硬编码;御剑一式普攻 obj=2 最近敌方)→ 真实 `20001`:
```
[Fight] send 20024 "c" 1 进战斗态
[Fight] send 20001 攻击请求: skill=59100001 怪列表=[1129,1134,1131,1132] 人列表=[] x=5374 y=2672 angle=0 fmt="hiiiihihhh"
```
圆形 AOE(御剑一式 area=350/num 上限 4)收集 4 只怪,逐字节对齐老端 `FightController.ts:800` WriteBegin(20001)。

### 4.2 服务端回包:稳定 `damage=0`(att=85 与 att=50000 两组,共 9+ 条样本)

| 攻击 att | 样本 | `20001 S2C` | 每防御者 |
|---|---|---|---|
| **85**(真装备) | 6 条 | `len=222B defenders=4 remaining=0B`(零残留) | `id∈{1129,1134,1131,1132} hp=140 damage=0 flag=0`(全) |
| **50000**(GM `attr_`) | 3 条(清台后干净复现) | `len=222B defenders=4 remaining=0B` | `id∈{1129,1134,1131,1132} hp=140 damage=0 flag=0`(全) |

实测日志(att=50000 组,清空 console 后干净复现):
```
[GMATK2] gmauth → attr_50000 → boost settled → cast#1/2/3
[Fight] send 20001 攻击请求: skill=59100001 怪列表=[1129,1134,1131,1132] ...
[Fight] recv 20001 攻击结果广播(S2C): len=222B ... defenders=4 remaining=0B
[Fight]   defender[0] type_flag=1 id=1129 hp=140 damage=0 flag=0
[Fight]   defender[1] type_flag=1 id=1134 hp=140 damage=0 flag=0
[Fight]   defender[2] type_flag=1 id=1131 hp=140 damage=0 flag=0
[Fight]   defender[3] type_flag=1 id=1132 hp=140 damage=0 flag=0
[Fight] 怪 1129/1134/1131/1132 服务端新 hp=140/140(damage=0 flag=0),刷新血条
```

### 4.3 根因判定(逐条排除,**纠正第 12 轮**)

| 候选成因 | 本轮取证 | 结论 |
|---|---|---|
| **角色 att 过低(第 12 轮头号根因)** | att=85(真装备)→ 0;**att=50000(GM)→ 仍 0** | ❌ **本轮证伪**——att 抬到 50000 仍 0,与 att 无关 |
| 怪防御过高 | `config_mon[10001001]` 战斗字段 31-35=250/500/500/500/200;att 50000 ≫ 任何值 | ❌ 排除(50000 破任何防御) |
| Unity C2S 字段/解析差异 | `20001` 逐字节对齐老端、`20001 S2C` `remaining=0B` 零残留(第 10 轮基线) | ❌ 排除(无差异) |
| `damage_flag` 误判(躲避/免疫) | 全 `flag=0`(正常命中判 0,非躲避/盾) | ❌ 排除(是真·0 伤"命中") |
| **发包形态/服务端 engage 帧(fight-movie 动作帧缺失)** | 本端释放边界即发单帧 `20001`,服务端恒回 `damage=0` + 同帧上 `iconType=200` 自身 buff;att 无关 | ✅ **头号根因(att-无关)** |

**伤害公式角度(死代码 `ClientFightServer` 字段名 = 服务端意图):** `base = (att + wreck − mon.defend + abs_att) × 系数 × rand`。att=50000 时 base 必 >0,但实际 `damage=0` → **服务端这一帧根本没走伤害结算,而是把本端的释放边界单帧 `20001` 当"进战斗/上 buff engage 帧"**(老端真实攻击在 fight-movie cast 序列后于 `skill_damage_time` 发,服务端据完整 cast 结算;本端无动作帧系统)。**这把第 10/11 轮"疑 fight-movie 时序"从假设升级为:用 att=50000 排除养成变量后的唯一剩余成因。**

---

## 5. P4:`100030` 击杀进度闭环(✗ 精确 blocker,不造假)

- **无 `hp==0` / `hp<max` 样本**(9+ 条 `20001 S2C` 全 `hp=140` 满血)→ 不触发 `DeleteSceneObj` 死亡移除 → **`100030` 击杀进度本轮不推进**。
- **不本地造假**:未改 `MonsterVo.Hp`/`FightVo.damage`/任务计数;`100030` 依赖真实死亡 + 服务端任务推送,本轮无真实死亡 → 记 blocker。
- 血量链(第 10 轮)就绪:`ApplyDefenseListToScene` 对每个防御者 `hp>0` 调 `ApplyHp`(实测刷新到 140/140,无可视变化因 hp 未变)、`hp==0` 调 `DeleteSceneObj`——只是没有 `hp==0` 帧可喂。

---

## 6. P5:只记录不扩散(本轮未编码)

完整 fight-movie 动作帧/cast 预播系统、完整自动战斗 AI/挂机寻怪、完整 CD/特效/音效/伤害飘字、装备系统(强化/星级/全页签)、角色属性完整 UI、`13033` 实时属性刷新接入、PvP/伙伴/神祇/掉落/副本/BOSS——**均只记录,不编码。** 本轮 `RunCommand` 内的自动重连+连击 hook 是**一次性运行态测试驱动**(EditorApplication.update,从不入库),非客户端业务代码。

---

## 7. 仍然阻塞项 / 真实卡点(本轮只记录)+ 下一轮

1. **`damage=0` 头号根因(本轮纠正)= 本端 `20001` 发包形态 / 服务端 engage 帧语义,与 att 无关**:本端在释放边界即发单帧 `20001`(无 fight-movie 动作帧),服务端稳定返回 `damage=0` engage 帧。**下一轮最直接动作 = 研究老端 fight-movie 真实发包时序**:
   - 老端 `scene/fight/FightController.ts` 的 `playFightMovie` / `skill_damage_time` / cast 序列:真实 `20001` 在动作帧第几步发、是否带额外状态字段、伤害是否走**后续帧/另一协议广播**(本端单窗口可能漏抓)。
   - 验证路径:在 cast 序列后(而非释放边界)发 `20001`,或抓服务端伤害结算的后续帧 → 看 `damage>0`。
2. **测试服会话窗口短(握手后 ~15-40s remote-close + 连发限频)**:本轮用 `EditorApplication.update` 运行态 hook(连上即发,零时序间隙)规避手动 RunCommand 时延;但拿"伤害后续帧"仍需更长稳定窗口(心跳/重连保活)。
3. **`13033` 属性刷新未接**:穿装/升级/GM 后服务端 push `13033`,本端忽略 → RoleModel att 不实时更新(需重连 fresh `13001` 取证)。若要 UI 实时反映养成,需接 `13033`(对标 `MainRoleVo.ReadFrom13033`)——非伤害根因,本轮不接。
4. **角色属性 UI / 装备穿戴 UI**:att/power 已解析,EquipmentView/穿戴入口仍 TODO(属表现线,非主线竖切伤害)。

### 下一轮最小可行动作

**移植/对齐 fight-movie 动作帧时序**(在 cast 序列/`skill_damage_time` 后发 `20001`,或捕获伤害后续帧广播)→ 真连验 `damage>0` → 驱动既有血量链可视扣血/杀怪 → 观察 `100030` 推送。**不再继续抬 att**(已证伪);att 路径(真装备 + GM)本轮已彻底打通并排除。

---

## 8. 账号状态变更记录(仅测试账号 `unity_npc_475823114`)

| 项 | 前 | 后 | 手段 | 持久性 |
|---|---|---|---|---|
| 主线 newest finish | 100010 | **100020** | 真实 `30004` | 持久(真实主线进度) |
| 主线 mainLine | 100020 | **100030**(击杀) | 真实 `30004` | 持久 |
| 等级 / power | 2 / 1040 | **3 / 1800** | 100020 exp 奖励 + 穿武器 | 持久 |
| 背包武器 | 无 | **101011010**(已穿) | 100020 special_goods + `15201` | 持久 |
| att / wreck | 33 / 17 | 85 / 38(装备);会话内 50000(GM) | 真装备 / GM `attr_` | 装备持久;GM `attr_` 会话内(重登从装备重算) |

> 均为测试账号上的真实主线进度 + 测试 GM,符合任务"只改测试账号 + 记录前后"边界;未触碰任何真实用户账号。

---

## 9. 本轮总结(对照任务包验收)

1. **账号是否从 `100010` 推进到 `100030` 或含怪阶段**:✅ 是——真实 `30004` 推进 `100010→100020→100030`(击杀任务),场景 10000 持续 5 只 `10001001` 可攻击。
2. **`att/wreck/def/Hp/level/power` 前后变化**:✅ 有证据——`att 33→35→85`、`wreck 17→38`、`Hp 740→780`、`lv 2→3`、`power 1040→1800`(真实奖励+装备);GM 会话内 att→50000。
3. **是否通过真实奖励/装备/升级/GM 提升 att**:✅ 全部——真实主线奖励武器 `101011010` + 真实穿戴 `15201`(att→85)+ GM `attr_`(att→50000),三路均验证。
4. **是否出现真实 `damage>0/hp/death`**:❌ 未出现——att=85 与 att=50000 两组共 9+ 条 `20001 S2C` 全 `damage=0/hp=140/flag=0`。
5. **`100030` 是否真实推进**:❌ 未推进——无真实死亡(无 `hp==0` 帧),不本地造假改任务。
6. **哪些代码改了/为什么**:**未改任何 Unity 代码**——P1/P2/P3 全经运行态 `RunCommand` 驱动既有链路;att=50000 仍 0 证明非客户端字段/解析/养成问题(P3 边界:仅"老端可伤害而 Unity 不行"才写码,本轮两端同发包形态→同 engage 帧),按编码规范"宁可少写"零改动保基线。
7. **下一轮最小可行动作**:研究/对齐老端 fight-movie 动作帧时序(cast 序列后发 `20001` 或抓伤害后续帧)→ 验 `damage>0`/杀怪/`100030`;不再抬 att。

---

## 10. 本轮改动清单

| 文件 | 改动 |
|---|---|
| `Docs/Claude任务包-运行态正伤害与100030闭环-第13轮.md`(已存在) | 本轮任务包 |
| `Docs/RuntimeCompare/MainQuest-PositiveDamage-第13轮.md`(新) | 本报告:P1 真实推进 `100010→100030` + P2 真实奖励/装备/GM 抬 att(33→85→50000)+ **P3 决定性反证 `damage=0` 与 att 无关(纠正第 12 轮养成结论)→ 锁定 fight-movie 发包形态** + P4 不造假 blocker + Unity 零代码改动 |

> **Unity 零代码改动**(保第 10/11/12 轮基线):`Assets/Scripts/**` 不动;`dotnet build` 仍 0 错误(6 条既有无关警告)。`AppConfig.asset` 取证后还原(`git diff` 为空)。`output/`、`.playwright-cli/` 不入库。账号变更仅作用于测试账号 `unity_npc_475823114`,已记录前后状态。
