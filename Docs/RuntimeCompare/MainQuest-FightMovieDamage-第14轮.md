# 运行态 fight-movie 真伤害帧 运行态对比 · 第 14 轮

范围:把第 13 轮锁定的 **"单帧 `20001` 只回 `damage=0` 的 engage 帧"** 推进到 **找到并最小复刻"真伤害帧"**。
方法:① 首次拿到老端运行态真连日志(`.playwright-cli/console-2026-06-21*.log`,老端真在打怪);② 直读**服务端权威战斗结算源码** `D:\GitProject\yu_server`(第 11 轮已证 `ClientFightServer` 是死代码、PvE 伤害走服务端);③ 老端 `yu_client` 发包链 + `config_skill` 配置三方交叉。Unity 侧据证据做**最小复刻**(配置驱动,不 hardcode)。

> ## 头条结论(本轮**定案根因**,三方独立证据闭合,彻底解开第 11↔13 轮悖论)
>
> ① **真因 = 普攻主技能是"进战斗 engage 技能",真正扣血的是它的 combo 副技能 —— 本端只发了 engage、从不发副技能。** 服务端 `mod_battle.erl:61-73` 对 **`is_att=0` 技能**专门走一个分支:构造 `NoHurtDerList`(每个防御者 = 当前 hp / `damage=0` / 全 0),`send_active_msg` 广播,**只返回 `MainSkillId`**;`calc_hurt_type/3`(`:594`)对 **`Calc==0`** 直接判 `HURT_TYPE_NOTHING` → `calc_hurt/8`(`:647`)恒回 `{当前hp, 0, 0}`。普攻 `御剑一式 59100001` 实测 **`is_att=0 / calc=0`**(`config_skill.json:2-33`),两条都命中 → **服务端对 59100001 恒回 `damage=0` 的 engage 帧**,与第 13 轮 9+ 条样本(全 `hp=140 / damage=0 / flag=0` + 攻击者上 `iconType=200` 自身 buff)**逐字段吻合**。
>
> ② **真伤害技能 = combo 副技能 `59100002`(御剑一式-副1):`is_att=1 / calc=1`**(`config_skill.json:34-65`)→ 走 `mod_battle.erl:75-91` 真实伤害分支 + `calc_hurt_core` 公式;**与 att 无关**(怪 `10001001` 服务端 **`def=0`**,`data_mon.erl:19589`,`TotalRatio` 公式下限 `max(0.2,…)` → att>0 必 `damage>0`)。`59100001.combo = [{59100001,200},{59100002,0}]`:engage 后 **200ms 对同目标补发副技能 `59100002`** 才真实扣血。
>
> ③ **老端运行态实锤(本轮首次抓到)**:`.playwright-cli/console-2026-06-21*.log`(老端真连测试服、真在打怪 31 分钟)逐帧显示 `send 20024`→`send 20001`(engage)→ **`+305ms` 再 `send 20001`(combo 副技能)**(L1331→L1342),`+300ms` 与 fight-movie `comboSkills [[300,59100002]]` 完全对上。**老端每次攻击发两次 `20001`(engage + 副技能);本端只发一次 engage** —— 这就是 `damage=0` 的全部成因。
>
> ④ **第 11↔13 轮悖论解开**:第 11 轮"时序非成因(`skill_damage_time=0`)"**对**(engage 和副技能间隔由 combo 决定,不是 `skill_damage_time`);第 13 轮"att 非成因(att=50000 仍 0)"**对且更强**(怪 `def=0`,att 本就无关)——两轮都对"非什么",但都没找到"是什么"。**本轮定案"是什么":缺 combo 副技能这一帧。** 第 13 轮"engage 帧/发包形态"方向**正确**,本轮把它精确到"缺副技能 `59100002`"。
>
> ⑤ **Unity 已最小复刻(配置驱动,编译通过)**:`SkillConfigs.GetComboNext` 读 `config_skill[skill].combo` 取副技能 id + 延迟;`SceneCombat` 在 engage `20001` 后按延迟对同目标补发副技能 `20001`(`hp>0` 重过滤,对标老端发包前过滤)。**不 hardcode `59100002`,不本地造假伤害。**
>
> **P4 真连 `damage>0` 本轮未当场抓到(精确卡点,非根因不明)**:smoke 自动登录**已实测连上**测试服(`accname=unity_npc_475823114` → 角色 `云霄42852` career=1 lv3 → `🎉 进入游戏成功(10004)`,Editor.log 实证),但 ① smoke 链止于 10004、**场景/怪同步流程"待接"**(`MonsterCount=0`),② **Play 态下 `Unity_RunCommand` 发现文件不刷新**("Unity not detected",`GetState` 仍通)→ 无法当场驱动"进场景+攻击"。需第 15 轮用第 13 轮那套 **编辑期 `EditorApplication.update` + 手动 `NetManager.Pump`** 驱动链(本轮预算用于定案根因+落地修复)。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 服务端源码(本轮新增·权威) | `D:\GitProject\yu_server`(Erlang)。战斗结算 `src\battle\mod_battle.erl` / `lib_battle_util.erl`;怪表 `src\data\create\data_mon.erl`;GM `src\gm\pp_gm.erl` |
| 老端运行态(本轮首次·权威) | `.playwright-cli\console-2026-06-21T12-29-40-982Z.log`(26020 行,老端真连 `ws 223.109.142.26:10000`、真打怪;新号 `90990` 创角 `全久京` career=3 role 4294967354,scene 10000) |
| 老端源码 | `D:\GitProject\yu_client\h5\src\scene\fight\{FightController,FightMovieInfo,FightVo}.ts`、`skill\SkillManager.ts`;`cdn\assets\resource\config\server\config_skill.json` |
| Unity 后端 | 游戏服 `ws://223.109.142.26:10000`(本轮 smoke 实测连上 + 进游戏 10004) |
| Unity 测试账号/角色 | `unity_npc_475823114` / `云霄42852` roleId `4294967355`(career=1 剑士 lv3,scene 10000;Editor.log 实证) |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 本轮为 P4 取证临时开 smoke(`devAccount`+`autoLoginSmokeTest`+`autoEnterFirstRoleSmokeTest`),**取证后已 `git checkout` 还原**(`git diff Assets/_App/Configs/AppConfig.asset` 为空)。

---

## 1. P0:第 13 轮基线保护(通过)

| 项 | 结果 |
|---|---|
| `dotnet build yu_client_unity.slnx -v:minimal` | ✓ **0 错误**(改动后 `Shenxiao.Module.Core.csproj` 重编,仅余既有 `MainRoleAgent.cs:206 CS0162` 1 警告;全量与第 13 轮 6 条既有无关警告一致) |
| `20024/20001` C2S、`FightVo` S2C 解析、`MonsterVo→MonsterRenderer` 怪渲染链 | ✓ 未退化(本轮只在 `SceneCombat` 末尾**新增** combo 补发,不动既有 engage 发包/解析/渲染) |
| `RoleModel.BattleAttr` 读取链 | ✓ 未动 |
| `TaskModel` 仍停 `100030` 击杀任务 | ✓ 未本地改任务 |
| 工作树 | ✓ 仅 `SceneCombat.cs`/`SkillConfigs.cs` 两文件改动 + 本报告;`AppConfig.asset` 还原;`.playwright-cli/`、`output/` 未入库 |

---

## 2. P1:老端运行态真伤害帧证据(本轮首次抓到运行态)

第 7–13 轮老端只停"正在连接/正在启动游戏"加载页。本轮 `.playwright-cli/` 留有一份 **2026-06-21 的真连日志**(老端真登录、真创角 `全久京`、真打怪 31 分钟),`send cmd`/`receive cmd` 逐协议可见。

### 2.1 一次完整攻击的发包时序(日志逐行,L1330–1346)

```
[280671ms] send cmd =20024          ← 进战斗态
[280676ms] send cmd =20001          ← 第①次 20001:engage 技能(普攻主技能)
[280782ms] receive cmd = 20001      ← engage 帧回包(damage=0)
[280981ms] send cmd =20001          ← 第②次 20001:+305ms,combo 副技能(真伤害)
[281016ms] receive cmd = 20001      ← 副技能伤害回包
[281104ms] receive cmd = 20001      ← (后续/广播)
[281105ms] receive cmd = 20001
```

- **一次攻击发两次 `20001`**:engage(`+0`)+ 副技能(`+305ms`)。`+305ms` ≈ fight-movie `ConfigCareerSkillMovies[59x00001].comboSkills = [[300, 59x00002]]`(第 11 轮已记 `59100001 comboSkills=[[300,59100002]]`)。
- 全程这种 `20024 → 20001 → (≈300ms) → 20001` 的成对模式反复出现(L1773–1825、L2052–2063 等),即"engage + 副技能"普攻循环。
- 老端这次是 career=3 `全久京`(普攻 `59300001 → 59300002`),与本端 career=1(`59100001 → 59100002`)**机制同构**(各职业普攻主技能均 `is_att=0`、均带 combo 副技能,见 §3.3);**老端运行态直接证明"每次攻击补发副技能 20001"是老端真实行为,不是本端臆测。**

### 2.2 与第 13 轮本端样本的对照

| | 老端(运行态日志) | 本端(第 13 轮 9+ 样本) |
|---|---|---|
| 第①次 `20001`(engage) | 有 | 有(`damage=0`) |
| 第②次 `20001`(combo 副技能) | **有(`+305ms`)** | **无** ← 差异点 |
| 结果 | 真打怪 31 分钟(真伤害) | 恒 `damage=0` |

> 日志只打 `send/receive cmd =N`(不打 payload),故"副技能回包 `damage>0`"的字节级证据由 §3 服务端源码补足(权威);运行态证据锁定的是**"老端每击发两次 20001"这一行为**,正是本端缺的那一帧。

---

## 3. P2:服务端/老端源码 + 配置交叉确认(三方闭合)

### 3.1 服务端:`is_att=0` engage 分支 + `calc==0` 判 NOTHING(权威·定案)

**① `is_att=0` 技能 → 专门回 `damage=0` 的 engage 帧 —— `mod_battle.erl:61-73`:**
```erlang
{true, AfterEffAer, #skill{is_att=0}=_SkillR, MainSkillId} ->
    InitDerList = [lib_battle_util:init_data(Der) || Der <- DerList],
    NoHurtDerList = [[DSign, XId, DHp, 0, 0, 0, 0, 0, DX, DY, 0, 0, pack_buff(...)] || ...],  %% 每个防御者:hp=当前 / damage=0 / 全 0
    send_active_msg(AfterEffAer, EtsAer, NoHurtDerList, SkillId, ...),                       %% 广播 engage 帧
    {true, EtsAer, MainSkillId};                                                             %% 只返回真伤害技能 id,本帧不结算伤害
```
→ **`NoHurtDerList` 就是第 13 轮抓到的 engage 帧**(防御者全 `hp=140 / damage=0 / flag=0`);同帧 `send_active_msg` 打包攻击者自身 buff = 第 13 轮的 `iconType=200 御剑一式自身 buff`。

**② `calc==0` → `HURT_TYPE_NOTHING` → 恒 0 伤 —— `mod_battle.erl:402,594,647`:**
```erlang
#skill{id=SkillId, calc=Calc, ...} = SkillR,                                  %% :390  Calc=技能 calc 字段
HurtType = calc_hurt_type(Aer, AfOthBuffDer, Calc),                           %% :402
...
calc_hurt_type(Aer, Der, Calc) -> if ... orelse Calc == 0 -> ?HURT_TYPE_NOTHING; ... end.   %% :594
calc_hurt(...) -> if HurtType==?HURT_TYPE_NOTHING orelse ... -> {当前hp, 0, 0}; ... end.      %% :647
```

**③ combo 解析 —— `lib_battle_util.erl:615-689` `check_use_skill`:** 首发普攻(`IsCombo=0`)走 `:643` `case Combo of [{_,LastTime,_}|T] when T/=[]`,把 combo 链尾存进 `skill_combo`(`next_time=LastTime`),返回 engage 技能本身;副技能(`is_combo=1`)走 `:664` 分支,`check_combo_skill`(`:794-831`)匹配上一帧存的 combo 记录 → `COMBO_FINISH/NEXT` → 解出真伤害技能。**`check_combo_skill` 仅靠"上一帧存了 combo 记录 + 本次 skill_id 命中"匹配,无硬性时间过期门 → engage 后补发副技能即生效。**

### 3.2 怪 `10001001` 服务端 `def=0`(att 彻底无关)

`data_mon.erl:19589`(怪 `10001001 转运达摩`):`lv=5 hp_lim=140 att=1 **def=0** hit=0 dodge=0 crit=0 wreck=0`。
→ `calc_hurt_core`(`mod_battle.erl:783`)`BaseHurt = (AttA+WreckA-DefD)*TotalRatio + ...`,`DefD=0` 且 `TotalRatio=max(0.2,…)*Rand`(`:777` 下限 0.2)→ **att>0 必 `damage>0`**。**第 13 轮"`config_mon` 防御 250/500"是客户端表数字键误读**;服务端权威 `def=0`,GM 抬 att 到 50000 对 `def=0` 本就毫无意义 —— 这从另一面坐实"`damage=0` 与 att/养成无关"。

### 3.3 配置:普攻主技能 `is_att=0`、副技能 `is_att=1/calc=1`(直读权威)

`config_skill.json`(本轮直读):

| skill | name | is_normal | is_combo | is_att | calc | combo | 角色 |
|---|---|---|---|---|---|---|---|
| **59100001** | 御剑一式 | 1 | 0 | **0** | **0** | `[{59100001,200},{59100002,0}]` | engage(普攻主技能) |
| **59100002** | 御剑一式-副1 | 1 | **1** | **1** | **1** | 同上 | **真伤害副技能** |

- `59100001.combo`:engage(`59100001`,延迟 `200ms`)→ 副技能(`59100002`)。**副技能 id 与延迟全在配置里,本端只需照读。**
- 跨职业同构(agent 核 `config_skill`):career2 `59200001`、career3 `59300001` 同为普攻主技能 `is_att=0`、各带 `59x00002` 副技能;`SkillManager.BaseAttackSkills` 4 职业普攻链一致。

### 3.4 老端发包链 + S2C(确认/未确认字段)

- **已确认**:老端 `onRoleRequestToFightHandler`(`FightController.ts:782-822`)`20001` C2S 字节序 = `h+i×怪+h+l×人+ihhh(skill,x,y,angle)`(本端逐字段一致);`target_hiter` 置首(主目标),发包前 `hp>0 && !IsDead` 重过滤;`ClientFightServer` 全仓无调用点(死代码,伤害走服务端)。
- **未确认(不影响本轮)**:`59100002` 副技能回包的 `damage` 精确数值(需 P4 真连字节抓);多次 `receive 20001`(L1343–1345)里"副技能伤害 vs 周边广播 vs 连段触发技"的逐条归属;`skill_link` 连段(`[59100001,59100003,59100005,59100008]`,连续点击轮转,非单击伤害必需,本轮不接)。

---

## 4. 根因判定表(逐条,定案)

| 候选成因 | 取证 | 结论 |
|---|---|---|
| 角色 att 过低(第 12 轮) | 怪 `def=0`(服务端),att>0 必 `damage>0` | ❌ 无关(比第 13 轮更强:def=0) |
| 发包时序 / `skill_damage_time`(第 10 轮) | `59100001 damage_time=0`;engage↔副技能间隔由 combo `200ms` 决定 | ❌ 非成因(第 11 轮已证伪,本轮归位到 combo) |
| C2S 字段/解析差异 | `20001` 逐字段对齐、`remaining=0B` | ❌ 无差异 |
| `damage_flag` 误判 | 全 `flag=0`(NOTHING) | —— 是 NOTHING 的表象 |
| **本端只发 engage 技能(`is_att=0/calc=0`),从不发 combo 副技能(`is_att=1/calc=1`)** | 服务端 `is_att=0`→engage 帧 + `calc=0`→NOTHING;副技能 calc=1 走真实公式;老端运行态每击发两次 `20001`(`+305ms`) | ✅ **定案根因** |

---

## 5. P3:Unity 最小复刻(配置驱动,已落码,编译通过)

### 5.1 改动

**① `SkillConfigs.GetComboNext(skillId)`(新增)** —— 读 `config_skill[skill].combo`(JSON 串),定位当前(engage)技能,返回**紧随其后的副技能 id** + **当前元素的延迟 `time`(ms)**;无 combo / 已是链尾 → `(0,0)`。`59100001 → (59100002, 200)`。

**② `SceneCombat.SendRealAttackOrBlock` 末尾 → `ScheduleComboFollowUp`(新增)**:engage `20001` 发出后,按 `GetComboNext` 的延迟,对**同一目标列表**补发副技能 `20001`(`SendComboAfterDelayAsync`):
- 延迟用 `Task.Delay`(对标 `LoginController` 既有 async 模式;`fire-and-forget` 但非 `async void`,异常只记录不吞)。
- 发包前按 `hp>0` 重过滤(对标老端 `onRoleRequestToFightHandler` 发包前过滤);`!NetManager.IsConnected` 兜底不发(短会话窗口)。
- 无 combo 链(真实主动技能 / 链尾)→ 不补发,**行为与第 13 轮完全一致**(非普攻零影响)。

> **严守边界**:副技能 id/延迟全来自 `config_skill.combo`,**未 hardcode `59100002`/`200`**;复用真实目标列表/坐标/技能来源;不本地造假伤害/扣血/任务。延迟取配置 `200ms`(服务端 `next_time` 同源);老端 fight-movie 用 `300ms`,二者均 ≥ 服务端 combo 窗口,代码注释已记。

### 5.2 验收

- `dotnet build` ✓ 0 错误(§1)。
- 逻辑链:engage `20001`(59100001)→ `Task.Delay(200ms)` → 副技能 `20001`(59100002,同目标 `hp>0` 过滤)。**首次让本端攻击具备"真伤害帧"那一发**,对齐老端运行态成对 `20001`。

---

## 6. P4:真连 `damage>0`/hp/death/100030(连接已通,当场捕获受阻 → 精确卡点)

### 6.1 已达成(Editor.log 实证)

smoke 自动登录 + 自动进角色,实测:
```
[Login] 已发送账号登录协议 pid=1 accname=unity_npc_475823114 ...
[Login] ★ 10000 回包: 角色数=1 ...
[Login]   角色[0] id=4294967355 云霄42852 职业=1 等级=3 0转
[Login] 发送进入游戏: role_id=4294967355
[Login] 🎉 进入游戏成功(10004),主城/场景流程待接
```
→ **测试服在线、账号/角色状态仍在**(`云霄42852` career=1 lv3),连接链路通。

### 6.2 当场捕获受阻(两点,均非根因不明)

1. **smoke 链止于 `10004`,场景/怪同步"待接"**:`🎉 ... 主城/场景流程待接` —— 自动 smoke 不驱动进场景/怪下发,`MonsterCount=0`(无怪可打)。
2. **Play 态 `Unity_RunCommand` 发现文件不刷新**:进 Play 后 `RunCommand`/`ReadConsole` 恒报 `Unity not detected (no fresh discovery files found)`,而 `GetState` 仍通(与既有"idle/reload 时 RunCommand 暂未检测到"现象一致)→ 无法在 Play 态手动驱动"进场景 + 攻击 + 抓 `20001`"。

> 第 13 轮的真连打怪是在**编辑期**用 `EditorApplication.update` 运行态 hook(手动 `NetManager.Pump` + 驱动 `SceneCombat.MainRoleAttackTarget`)完成的;本轮预算用于**定案根因 + 落地修复**,P4 当场抓 `damage>0` 留第 15 轮(下方给出确定性最小步骤)。**未本地造假任何伤害/hp/death/任务进度。**

### 6.3 `100030` 进度

依赖真实 `hp==0` 死亡 + 服务端任务推送;本轮无真连击杀样本 → 不推进、不本地改任务。修复后(§5)真连即可产出 `damage>0`(§3.2 怪 `def=0` 必正伤),血量链(第 10 轮就绪)`hp==0`→`DeleteSceneObj` 会触发死亡,届时观察 `100030→100040`。

---

## 7. P5:只记录不扩散(本轮未编码)

`skill_link` 连段轮转(连续点击 `59100001→59100003→…`)、完整 fight-movie 动作/特效/音效/伤害飘字、完整自动战斗 AI/挂机寻怪、完整 CD、PvP/伙伴/神祇/掉落/副本/BOSS、`13033` 实时属性刷新 —— **均只记录,不编码**。本轮只补"engage→combo 副技能"这一发,闭合单击真伤害链。

---

## 8. 仍存在的真实卡点 + 第 15 轮最小动作

### 卡点(均已精确,非根因不明)
1. **P4 当场 `damage>0` 未抓**:smoke 链止于 10004(场景/怪流程待接)+ Play 态 RunCommand 发现文件不刷新。**非根因问题**(根因已三方定案),是取证驱动方式问题。
2. **`59100002` 副技能回包 `damage` 精确字节**:需真连抓(预期 ~`(att+wreck)*0.2` 量级,服务端权威)。

### 第 15 轮最小动作(确定性,按第 13 轮编辑期 harness)
1. 临时开 smoke(取证后还原 `AppConfig`);**编辑期**(非 Play)用 `Unity_RunCommand` + `EditorApplication.update`:每 tick 手动 `NetManager.Pump()` 保活,驱动登录链 → `EnterGameWithRole` → 等 `SceneManager.MonsterCount>0`(必要时手动发进场景/怪同步协议,对标第 13 轮)。
2. 进场景有怪后,调 `SceneCombat.Instance.MainRoleAttackTarget(真实普攻 skillId, 2)` —— 本轮修复会自动 engage + `200ms` 后补发副技能 `59100002`;抓 `20001 S2C`,确认 `defender damage>0 / hp<140`。
3. 连击至 `hp==0`,看 `DeleteSceneObj` 死亡 + `100030→100040` 推送。
4. 若副技能仍 `damage=0`:抓完整 C2S/S2C 字节,核 combo 是否被 `check_combo_skill` 拒(`ERR_COMBO`)、延迟是否需调到 `300ms`(对齐 fight-movie)——但 §3 源码已证机制成立,概率低。

---

## 9. 对照任务包验收

1. **老端运行态是否抓到真伤害帧/后续帧**:✅ 首次抓到 —— 运行态日志证明老端每击发**两次 `20001`**(engage + `+305ms` combo 副技能);"真伤害帧"= combo 副技能 `20001`。
2. **老端源码/配置确认的发包时机、目标锁定、未确认字段**:✅ 时机=engage 后 combo `200ms`(配置 `next_time`)/老端 fight-movie `300ms`;目标=engage 锁定列表、副技能发包前 `hp>0` 重过滤;未确认=副技能回包 `damage` 精确字节(§3.4)。
3. **Unity 是否按证据调整 `20001` 时机/后续协议**:✅ 新增 `config_skill.combo` 驱动的副技能 `20001` 补发(`SkillConfigs.GetComboNext` + `SceneCombat.ScheduleComboFollowUp`),不 hardcode、不造假;`dotnet build` 0 错误。
4. **是否出现服务器真实 `damage>0/hp/death`**:❌ 本轮当场未抓(§6 卡点,非根因不明);源码已证 `def=0` + 副技能 `calc=1` 必 `damage>0`。
5. **`100030` 是否真实推进**:❌ 未推进(无真连击杀样本,不本地改任务)。
6. **代码改了什么/为什么**:`SkillConfigs.cs`(+`GetComboNext`)、`SceneCombat.cs`(+combo 副技能补发);因老端运行态 + 服务端源码 + 配置三方定案"本端缺 combo 副技能那一发",据真实协议/配置最小接入。
7. **下一轮最小动作**:§8 —— 编辑期 harness 真连抓副技能 `20001` 的 `damage>0` → 杀怪 → `100030` 推送。

---

## 10. 本轮改动清单

| 文件 | 改动 |
|---|---|
| `Docs/Claude任务包-运行态fight-movie真伤害帧-第14轮.md`(已存在) | 本轮任务包 |
| `Docs/RuntimeCompare/MainQuest-FightMovieDamage-第14轮.md`(新) | 本报告 |
| `Assets/Scripts/Module/Core/Skill/SkillConfigs.cs` | +`GetComboNext`:读 `config_skill[skill].combo` 取副技能 id + 延迟(配置驱动) |
| `Assets/Scripts/Module/Core/Scene/SceneCombat.cs` | +`ScheduleComboFollowUp`/`SendComboAfterDelayAsync`:engage `20001` 后按 combo 延迟对同目标补发副技能 `20001`(承载真伤害);+`using System.Threading.Tasks`/`Shenxiao.Framework.Net` |

> **基线保护**:`dotnet build` 0 错误(仅余既有无关警告);既有 `20024/20001`/`FightVo`/怪渲染链未退化;`TaskModel` 未改;`AppConfig.asset` smoke 取证后 `git checkout` 还原(`git diff` 空);`output/`、`.playwright-cli/` 不入库。
