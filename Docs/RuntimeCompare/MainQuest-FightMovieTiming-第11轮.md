# 运行态 fight-movie 动作帧时序 运行态对比 · 第 11 轮

范围:把第 10 轮卡住的 **"服务端对本端攻击稳定判 `damage=0` → 不能真实扣血/死亡/推进 `100030`"** 推进一步。
第 10 轮的头号下一步假设是:**"移植 fight-movie 动作帧 → 在 `skill_damage_time` 发 `20001`,看服务端是否返真实伤害"**。
本轮 P1 **逐行追老端发包调用链 + 逐字段查真实 fight-movie / config_skill 配置**,对这一假设做证伪/证实。

方法:老 Laya 客户端 `scene/fight/FightController.ts`(`AttackRequest` / `ClientPrePlayFightMovie` / `onRoleRequestToFightHandler`)+ `scene/fight/FightMovieInfo.ts`(`InitData` / `Update` / `SendMsg`)+ `skill/SkillManager.ts`(`GetFightSkillMovie`)+ `skill/SkillVo.ts`(`getHurtRatio`)为真相;真实配置取 `cdn/resource/config/client/ConfigCareerSkillMovies.json`(fight-movie)与 `config/server/config_skill.json`(技能表)。

> ## 头条结论(本轮纠偏:第 10 轮"时序致 0 伤害"假设被证伪)
>
> ① **`skill_damage_time` 来源 100% 确认 = `movie_cfg.damage_time`**(`FightMovieInfo.ts:138` `skill_damage_time = movie_cfg.damage_time || 0`),`movie_cfg = SkillManager.GetFightSkillMovie(skill_id)`(`SkillManager.ts:519`)= `ConfigCareerSkillMovies[id] || ConfigMonsterSkillMovies[id] || ConfigPartnerFightMovies[id] || ConfigGodSkillMovies[id] || {}`。单位 = **秒**,缺省 = **0**。
>
> ② **真实取值:`59100001`(御剑一式/普攻)`damage_time = 0`**;**所有 `591xx` 剑士技能 `damage_time` 全为 0**;全部 41 条 `ConfigCareerSkillMovies` 里仅 5 条(`595xx`,career 50)`damage_time = 0.3`。
>
> ③ **发包闸只看 `skill_damage_time`**:老端 `FightMovieInfo.Update`(`:343-347`)`if (is_client_pre_play && msg_info && past_time >= skill_damage_time) { SendMsg(); msg_info = null }`。`SendMsg`(`:489-490`)`Fire(ROLE_REQUEST_TOFIGHT)` → `onRoleRequestToFightHandler`(`:782`)`WriteBegin(20001)`。**`damage_time == 0` ⇒ 预播开始后下一帧 `past_time >= 0` 即发** —— 与本端"释放边界即发"**本质同一时刻**(老端晚一帧,可忽略)。
>
> ④ **故第 10 轮"在 `skill_damage_time` 延迟发 `20001` 可让服务端判真实伤害"的假设,对 `59100001` 被证伪**:老端对 `59100001` 也在释放边界(下一帧)发,**时序不是 `damage=0` 的成因**。
>
> ⑤ **`ClientFightServer`(客户端伤害预测公式)是死代码**(全仓库仅声明 `:70` + 定义 `:911`,**无任何调用点**)→ 真实 PvE 伤害走**服务端权威结算**;第 10 轮据 `ClientFightServer` 公式做的伤害推断**不代表服务端**。
>
> ⑥ **`59100001` = `御剑一式`,`is_normal=1`(普攻)、`career=1`(剑士)、`mod=2`/`range=1`(圆形 AOE)**;`lv_data[0].hurt="0"`、`power="0"`,**全部 20 个 career=1 技能 lv1 `hurt` 均为 0**;但顶层 `around_hurt=1000`。即:服务端普攻伤害**很可能不读 `lv_data.hurt`**(那会恒 0),而走武器攻击 / `around_hurt` 等其它机制 —— 本端**无法从客户端侧断定** `damage=0` 真因。
>
> **结论(精确 blocker):时序假设证伪;`damage=0` 真因属服务端权威(最可能=角色战斗属性 att/装备不足,或服务端普攻公式),客户端代码改不动也看不全。** 因 P2 验收("证明 `20001` 不再在点击瞬间发、而在 `skill_damage_time` 之后发")对 `59100001`(`damage_time=0`)**不可成立**,本轮**不堆无效延迟调度器**,保第 10 轮基线稳定;把证据与纠偏后的真实路径落档。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮 HTTP `200` 可达,但渲染层仍停"正在连接/正在启动游戏"加载页、不打日志,延续第 7/8/9/10 轮环境阻塞)。P1 证据 = **老端源码调用链 + 真实 fight-movie/config_skill 配置 + 第 9/10 轮同测试服真连样本** |
| 老端源码(权威) | `scene/fight/FightController.ts`(`onReleaseMainSkill:755` / `AttackRequest:1021` / `ClientPrePlayFightMovie:1410` / `onRoleRequestToFightHandler:782` / `onChangeFightingState:884` / `ClientFightServer:911` 死代码);`scene/fight/FightMovieInfo.ts`(`InitData:100` / `Update:324` / `SendMsg:489`);`skill/SkillManager.ts`(`GetFightSkillMovie:519`);`skill/SkillVo.ts`(`getHurtRatio:181`) |
| 真实配置(权威) | `cdn/resource/config/client/ConfigCareerSkillMovies.json`(fight-movie:`damage_time`/`back_swing`/`spell_time`/`anim`/`comboSkills`);`cdn/resource/config/server/config_skill.json`(技能表:`is_normal`/`career`/`mod`/`range`/`around_hurt`/`lv_data.hurt`) |
| 真实样本(权威,引用第 10 轮) | 跨第 9+10 轮共 3 条真连 `20001 S2C`(`108B/1 怪` + `222B/4 怪`×2),全部 `remaining=0B`、全部 `damage=0 / hp=满 / damage_flag=0`,每条同帧给攻击者上 `iconType=200` 自身 buff(`MainQuest-FightVoDamage-第10轮.md` §2/§6) |
| Unity 后端 | 游戏服 `ws://223.109.142.26:10000`(沿用第 3–10 轮) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` 剑士,scene 10000) |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 冒烟开关本轮未改(无 Play 取证,见 §6 决策)。

---

## 1. P1:老端"技能点击 → `20001` 发包"完整调用链(逐行源码,不猜)

### 1.1 全链路一图

```
[按技能槽]
  └─ SkillManager.PressSkillHandler          // 选取模式分支
       └─ Scene.MainRoleAttackTarget (Scene.ts:1835)   // 取/寻目标 → 范围/朝向/接近
            └─ Fire(FightEvent.RELEASE_MAIN_SKILL, attacker, skill_id, target_compress_id)   // ← 释放边界
  ┌──────────────────────────────────────────────────────────────────────┐
  │ onReleaseMainSkill (FightController.ts:755-758)  绑定 RELEASE_MAIN_SKILL │
  │   └─ this.AttackRequest(attacker, skill_id, target_compress_id) (:1021)  │
  │        ├─ 解析 select_type/aoe_mode/center_pos/目标列表(FindTargets)     │
  │        ├─ target_hiter 置首 monster_list(:1351-1356)                     │
  │        └─ this.ClientPrePlayFightMovie(skill_id, monster_list, role_list, msg_info, attacker) (:1406) │
  └──────────────────────────────────────────────────────────────────────┘
       └─ ClientPrePlayFightMovie (:1410)
            ├─ cfg = SkillManager.GetFightSkillMovie(skill_id)        // fight-movie 配置
            ├─ fight_info.InitData(skill_id, attacker, attacker_info, monster_list, [], TRUE/*client_pre_play*/, msg_info) (:1441)
            ├─ fight_info.PlayActions()        // 播攻击动作 attacker.DoAttack
            └─ attacker.EnterFightingState(6) (:1448)   // → CHANGE_FIGHTING_STATE(true) → 20024 "c" 1
  ─────────────────────────────────────────────────────────────────────────
  [每帧 Runner 驱动 FightMovieInfo.Update]
       └─ FightMovieInfo.Update(now) (:324)
            past_time = now - start_time
            if (is_client_pre_play && msg_info && past_time >= skill_damage_time) {   // (:343)  ← 发包闸
                this.SendMsg();  msg_info = null
            }
       └─ SendMsg (:489)
            └─ Fire(FightEvent.ROLE_REQUEST_TOFIGHT, msg_info) (:490)
  ┌──────────────────────────────────────────────────────────────────────┐
  │ onRoleRequestToFightHandler (FightController.ts:782-822) 绑定 ROLE_REQUEST_TOFIGHT(:823) │
  │   ├─ 重校验 monster_list:仅保留 mon_vo.hp>0 && !mon.IsDead()(:786-798)  │
  │   ├─ WriteBegin(20001)(:800)                                            │
  │   ├─ h+i×N 怪 / h+l×N 人 / ihhh(skill, floor(x), floor(y), floor(angle))(:802-813) │
  │   └─ SendToGame()(:814) + AddReleaseMainSkill + SetLastFightMsg(:815-821)│
  └──────────────────────────────────────────────────────────────────────┘
```

### 1.2 关键代码摘录(权威)

**① fight-movie 配置取出 + `skill_damage_time` 赋值 —— `FightMovieInfo.InitData`(`FightMovieInfo.ts:107,138`):**
```ts
movie_cfg = local_SkillManager.GetFightSkillMovie(skill_id1);   // :107
...
skill_damage_time = movie_cfg.damage_time || 0;                 // :138  ← 配置的技能伤害时间(秒)
```

**② 发包闸只看 `skill_damage_time` —— `FightMovieInfo.Update`(`:343-347`):**
```ts
if (is_client_pre_play && msg_info && past_time >= skill_damage_time) {
    this.SendMsg();          // → Fire(ROLE_REQUEST_TOFIGHT)
    msg_info = null          // 每次预播只发一次
}
```
> `is_client_pre_play=true` 时 `show_damage=!_client_pre_play=false`,故同函数里"受击/飘字"块(`:329-341`)在预播阶段不跑;**唯一作用就是到 `skill_damage_time` 把 `msg_info` 发给服务端**。

**③ `SendMsg` → 发包事件 —— `FightMovieInfo.ts:489-490`:**
```ts
this.SendMsg = () => { GlobalEventSystem.Fire(FightEvent.ROLE_REQUEST_TOFIGHT, msg_info); msg_info = null; };
```

**④ `GetFightSkillMovie` 配置来源 —— `SkillManager.ts:519-521` + 绑定 `:162-165`:**
```ts
this.GetFightSkillMovie = (skill_id) =>
    ConfigCareerSkillMovies_[skill_id] || ConfigMonsterSkillMovies_[skill_id]
    || ConfigPartnerFightMovies_[skill_id] || ConfigGodFightMovies_[skill_id] || {};
// ConfigCareerSkillMovies_ = PRELOAD_CLIENT_CONFIG.ConfigCareerSkillMovies; (:162)
```

**⑤ `20001` C2S 发送体(本端已逐字段对齐)—— `onRoleRequestToFightHandler`(`:800-814`):**
```ts
this.WriteBegin(20001)
WriteFMT("h", request_fight_mon_count); for(...) WriteFMT("i", mon_id)    // 怪列表(重校验后)
WriteFMT("h", role_list.length);        for(...) WriteFMT("l", role_id)   // 人列表
WriteFMT("ihhh", skill_id, floor(attack_x), floor(attack_y), floor(attack_angle))
this.SendToGame()
```

### 1.3 `20024`("c" 进/出战斗态)的发送时机

`onChangeFightingState`(`FightController.ts:884-895`,绑定 `CHANGE_FIGHTING_STATE`):
```ts
let scene_cfg = Config.PRELOAD_CLIENT_CONFIG.ConfigClientScene.fighting_state_invalidate
let onChangeFightingState = (state) => {
    if (state == true && scene_cfg[scene_mgr.GetSceneId()]) return   // 该场景禁战斗态则不发
    if (state) this.SendFmtToGame(20024, "c", 1)
    else       this.SendFmtToGame(20024, "c", 2)
}
```
触发点 = `ClientPrePlayFightMovie:1448` `attacker.EnterFightingState(6)`(`MainRole.ts:204-207` `is_fighting_state` 翻转时 `Fire(CHANGE_FIGHTING_STATE, true)`)。
**顺序:`20024 "c" 1` 在预播开始(释放边界)发;`20001` 在 `skill_damage_time` 后发。** `damage_time=0` 时二者仅差一帧。
> 本端(`FightController.cs:64 EnterFightingState` + `SceneCombat.cs:236-237`)在释放边界同帧先发 `20024 "c" 1` 再发 `20001` —— **顺序与老端一致**,间隔(一帧)可忽略。

### 1.4 目标列表锁定 / 重算规则(老端)

- **锁定点 = 释放边界(`AttackRequest`)**:`FindTargets`/`target_hiter` 在 `AttackRequest` 内一次性算出 `monster_list`,塞进 `msg_info.monster_list`,随预播缓存到 `skill_damage_time`。
- **发包前重校验(`onRoleRequestToFightHandler:786-798`)**:发 `20001` 时遍历缓存的 `monster_list`,**只保留 `mon_vo.hp>0 && !mon.IsDead()`**,死亡/移除的剔除(并对未进受击态的怪 `ForceDoDead`)。
- 即:**延迟期内目标列表不重新搜怪,但发包时刻按"死没死/在不在"过滤**。本端 `SceneCombat.CollectCircleMonsters`(过滤 `Hp>0`/`!IsCollect`/`CanAttack==1`)+ `SendMainSkillAttack` 已等价(本端在释放边界即收即发,无延迟期,故无需"延迟末过滤")。

### 1.5 老端运行态尝试

本轮再访 `http://127.0.0.1:8090/index.html`:HTTP `200`(静态页可达),但游戏渲染层仍停"正在连接/正在启动游戏"、不打日志(延续第 7/8/9/10 轮)。
故 P1 时序证据以**源码调用链 + 真实 fight-movie/config_skill 配置**为准——对"发包时机字段"而言,源码 + 配置比加载页截图更硬。

---

## 2. `skill_damage_time` 真实取值(逐配置,不猜)

`ConfigCareerSkillMovies.json` 全表 41 条;`damage_time` 单位 = **秒**(与 `back_swing`/`spell_time` 同尺度,如 `0.567`),缺省 0。

| 技能 id | 名称 | `damage_time`(秒) | `anim` | `comboSkills` |
|---|---|---|---|---|
| **59100001** | **御剑一式(本端实测攻击用)** | **0** | attack | `[[300,59100002]]` |
| 59100003 | 御剑二式 | 0 | attack2 | `[[200,59100004]]` |
| 59100005 | 御剑三式 | 0 | attack3 | `[[200,59100006],[200,59100007]]` |
| 59100008/10/14/16/19 … | 御剑四式/技能 1-4 … | 0 | attack4/skill1-4 | … |
| 59500001 | 龙渊称霸(career 50) | 0.3 | attack | — |
| 59500002 | 御风神矢(career 50) | 0.3 | attack | — |
| 59500003/07/08(career 50) | … | 0.3 | attack | — |

- **全部 `591xx`(剑士)`damage_time = 0`**;**仅 5 条 `595xx`(career 50)= 0.3**。
- **本端实测攻击技能 `59100001` 的 `skill_damage_time = 0`** —— 即老端对该技能"预播下一帧(`past_time>=0`)即发 `20001`"。

> 换算备注:若后续做延迟调度,毫秒 = `damage_time * 1000`(`0 → 0ms`,`0.3 → 300ms`)。**本端实测技能为 0ms,故无可观察延迟。**

---

## 3. 关键纠偏:为什么"按 `skill_damage_time` 延迟发包"解不了 `damage=0`

| # | 第 10 轮假设 | 本轮源码/配置取证 | 结论 |
|---|---|---|---|
| 1 | 本端在释放边界即发、老端在 `skill_damage_time` 发,时序差致服务端判 0 伤 | `59100001` `damage_time=0`(§2);老端发包闸 `past_time>=0`(§1.2②)→ **老端也在释放边界下一帧发** | **证伪**:时序对 `59100001` 无差异 |
| 2 | 客户端伤害预测(`ClientFightServer`)说明伤害走 hp 增量 | `ClientFightServer` 全仓**无调用点**(仅声明 `:70`/定义 `:911`)→ 死代码 | **服务端权威**:第 10 轮据此公式的推断不代表服务端 |
| 3 | 御剑一式应造成 50-120% 伤害(desc) | `config_skill[59100001]`:`is_normal=1`(普攻)、`lv_data[0].hurt="0"`、`power="0"`;**全部 20 个 career=1 技能 lv1 `hurt`均为 0**;但顶层 `around_hurt=1000` | 服务端普攻伤害**不读 `lv_data.hurt`**(否则恒 0),走武器攻击/`around_hurt`等;客户端**无法断定/无法修改** |
| 4 | `20024` 时机差异 | 老端释放边界发 `20024 "c" 1`、`skill_damage_time` 发 `20001`;本端同帧先 `20024` 后 `20001` | 顺序一致,间隔一帧可忽略,非成因 |

**`config_skill[59100001]` 关键字段(权威):**
```
name=御剑一式  career=1  type=1  is_normal=1  mod=2  obj=2  range=1
is_att=0  calc=0  release=1  around_hurt=1000  time=567(=back_swing 0.567s)
lv_data[0]: distance=100  area=350  num=[1,4]  hurt="0"  power="0"  cd="0"
```
`SkillVo.getHurtRatio`(`SkillVo.ts:181-185`)`return level_vo ? level_vo.hurt : 0` —— 读的就是 `lv_data[lv-1].hurt`(=0)。死代码 `ClientFightServer` 用它 `* 0.0001` 做乘数 → 预测恒 0;但**真实服务端不跑这段**。

> 即:**`damage=0` 既不是本端解析缺口(第 10 轮已证 `remaining=0B`),也不是发包时序(本轮证伪),而是服务端对"该角色 + 该普攻 + 当前战斗属性"权威结算为 0。** 最可能成因(只记录,不臆断写码):①角色战斗属性(att/破甲)不足或未穿装备 → 普攻打在怪防御下取 0;②伤害走未捕获的后续帧(测试服 `~60–85s remote-close` + 连发限频,第 10 轮 blocker);③服务端普攻公式细节(`around_hurt`/武器系数)本端不可见。**三者都需 Play 抓角色属性 + 拉长会话窗口才能区分,非本轮客户端代码可解。**

---

## 4. Unity 当前实现 vs 老端(逐项)

| 维度 | 老端 | 本端(第 10 轮基线) | 差异 / 是否需改 |
|---|---|---|---|
| 释放边界事件 | `Fire(RELEASE_MAIN_SKILL)` → `onReleaseMainSkill` → `AttackRequest` | `EventDispatcher.Emit(EVT_RELEASE_MAIN_SKILL)` + `SendRealAttackOrBlock`(`SceneCombat.cs:187-194`) | 等价边界;**老端 `EVT_RELEASE_MAIN_SKILL` 即此边界** |
| 预播 → 发包 | 预播(`ClientPrePlayFightMovie`)缓存 `msg_info`,`Update` 到 `skill_damage_time` 发 | 释放边界即发 `20001`(无预播/无 fight-movie) | **对 `damage_time=0` 技能行为等价**;对 `damage_time>0` 技能本端会早发 `≤0.3s` |
| `20024` 顺序 | 边界发 `c 1`,`skill_damage_time` 发 `20001` | 同帧先 `c 1` 后 `20001` | 顺序一致,间隔一帧 |
| 目标列表 | 边界锁定 + 发包前过滤死亡/移除 | 边界收集(`CollectCircleMonsters` 过滤 hp>0)即发 | 等价(本端无延迟期) |
| 伤害结算 | 服务端权威(`ClientFightServer` 死代码) | 解析服务端 `20001 S2C` 喂血量链(第 10 轮) | 等价(都服务端权威) |
| `damage=0` | 同角色/同普攻老端也会判 0(服务端同一公式) | 实测 3 条样本全 0 | **非本端差异** |

**结论:对本端实测技能 `59100001`,Unity 与老端发包时序行为等价。** 唯一真实差异是"对 `damage_time>0` 技能(如 `595xx`)本端会早发 `≤0.3s`"——但 `595xx` 是 career 50、非主线剑士首杀技能,且其 `damage=0` 与否无样本,不在本轮主线竖切范围。

---

## 5. P2 决策:不堆无效延迟调度器(理由 + 代价权衡)

任务 P2 验收:**"编辑期 harness 能证明 `20001` 不再在点击瞬间发,而是在真实 `skill_damage_time` 之后发"**。

- 对本端实测技能 `59100001`,`skill_damage_time = 0` → **"在 `skill_damage_time` 之后发"≡"在点击瞬间(下一帧)发"**,该验收**逻辑上不可成立**。
- 实现一个对 `59100001` 延迟 `0ms` 的"调度器"= 把"同帧发"改成"下一帧发",**对服务端 `damage=0` 零影响**,却要新增:`ConfigCareerSkillMovies` 同步 + `FightMovieConfigs` 读取层 + `SkillDamageTiming` 调度器 + 每帧驱动(本仓库 plain-C# 控制器无 Runner,只有 `AppLauncher.Update→NetManager.Pump`,Framework 不可反向依赖 Core),**对稳定的第 10 轮基线是纯风险、零收益**。
- 任务硬性边界:`只在 P1 证据足够时实现`、`禁止只凭感觉设延迟`、`不允许猜 skill_damage_time`。**P1 证据充分且指向 `0`,据此"忠实复刻"就是"下一帧发"**,无可验收的延迟可造。

**故本轮按"精确 blocker 优先于 shell"取舍:不实现无效调度器,保第 10 轮 `FightVo` 解析 + 喂血量链基线稳定,把纠偏证据与真实下一步落档。** 若将来要复刻 `595xx`(career 50,`damage_time=0.3`)的可见延迟,再引入 `FightMovieConfigs`(读真实 `damage_time`)+ 调度器,届时验收可成立(0.3s 可观测)——本轮主线不需要。

> 已勘明的最小实现路线(留给将来,**本轮未落码**):①`ClientConfigSync.SYNC_LIST` 加 `"ConfigCareerSkillMovies"`(→ `Assets/GameRes/resource/config/client/`,gitignore 不入库,仅注册行入库);②新增 `FightMovieConfigs`(镜像 `SkillConfigs`,`GetDamageTimeMs = damage_time*1000`);③`SceneCombat` 释放边界改"捕获目标/AOE/技能/坐标 → 经调度器到 `damage_time` 再 `SendMainSkillAttack`,每次按一次,发前重过滤死亡怪";④驱动用 `Task.Delay`(主线程 SynchronizationContext,本仓库已用 async/await)或 Core 侧 MonoBehaviour ticker。

---

## 6. P3 / P4:本轮无新 Play 取证(决策)+ 精确 blocker

- **P3(`damage>0`/hp 变化)**:本轮 P1 已证"时序非成因",新 Play 若仍用 `59100001` 普攻、同冒烟角色,**预期仍 `damage=0`**(与第 10 轮 3 条样本一致)——服务端对该角色该普攻权威判 0。在未先解决"角色战斗属性/会话窗口"前,再抓一窗只会复制第 10 轮结论,故本轮**不重复 Play**(未动 `AppConfig` 冒烟开关,`git diff AppConfig.asset` 为空)。
- **要取到 `damage>0` 的真实前置**(纠偏后):
  1. **抓角色真实战斗属性**(att/破甲/攻击,来自角色属性协议)+ 对照 `config_mon[10001001]`(怪 hp=140,防御/属性在数字键 `31..35`)→ 判断是否 att≤防御导致 0;
  2. **确认是否需穿装备/升级**(冒烟角色可能裸装 → 普攻 0);
  3. **拉长会话窗口**(心跳/重连,缓解 `~60–85s remote-close` + 连发限频)抓后续帧广播;
  4. 若服务端普攻走 `around_hurt`/武器系数,需 yu_server 侧确认(超客户端可见范围)。
- **P4(`100030` 击杀进度)**:依赖真实 `hp==0` 死亡事件;本轮无新样本 → 不推进,记 blocker(不本地改任务)。

---

## 7. 仍未确认字段 / 真实卡点清单(本轮只记录)

1. **`damage=0` 服务端真因(头号 blocker,已从"时序"纠偏到"服务端权威结算")**:候选 = 角色战斗属性不足 / 普攻走 `around_hurt` 或武器系数 / 后续帧未捕获;均需 Play 抓属性 + 拉窗,非客户端代码可解。
2. **角色 att/破甲实值**:`20001 S2C` 攻击者头不含 att(只含 hp/skill/pos),需另抓角色属性协议。
3. **服务端普攻伤害公式**:`is_normal=1` 是否走 `lv_data.hurt`(=0)以外路径(`around_hurt=1000`/武器),yu_server 权威,客户端不可见。
4. **`595xx`(career 50,`damage_time=0.3`)可见延迟**:本端无该职业样本,延迟复刻留将来。
5. **测试服 `~60–85s remote-close` + 连发限频**:第 10 轮 blocker 延续,影响后续帧广播捕获。
6. **老端运行态截图**:仍停加载页(延续第 7/8/9/10 轮),时序用源码 + 真实配置取证。

---

## 8. 本轮总结(对照任务包验收 5 条)

1. **老端 `skill_damage_time`/fight-movie 发包时序是否确认**:✓ **逐行确认** —— 发包链 `RELEASE_MAIN_SKILL→AttackRequest→ClientPrePlayFightMovie→FightMovieInfo.Update(past_time>=skill_damage_time)→SendMsg→ROLE_REQUEST_TOFIGHT→onRoleRequestToFightHandler→WriteBegin(20001)`;`skill_damage_time = movie_cfg.damage_time`(`ConfigCareerSkillMovies`),**`59100001 = 0`、591xx 全 0、仅 595xx=0.3**。
2. **Unity 是否按动作帧延迟发 `20001`**:✗(**有意不做**)——P1 证据显示 `59100001` `damage_time=0`,延迟为 0、P2 验收不可成立,堆调度器对 `damage=0` 零收益且增风险;保第 10 轮基线稳定(理由见 §5)。
3. **是否出现服务端真实 `damage>0/hp变化/death`**:✗ —— 本轮无新 Play(预期仍 0,见 §6);**关键纠偏:`damage=0` 非时序、非解析,属服务端权威结算**(§3)。
4. **可见血条/怪物销毁是否真实发生**:✗ —— 解析+喂链侧第 10 轮已就绪,缺 `damage>0/hp==0` 真实样本;不本地造假。
5. **`100030` 任务进度是否真实推进**:✗ —— 未驱动到死亡 → 不推进,记 blocker(不本地改任务)。
6. **仍有真实 blocker + 下一轮**:见 §7;**纠偏后的真实路径(§6)= 抓角色战斗属性 + 判 att/装备 + 拉长会话窗口,而非移植 fight-movie 延迟**。

---

## 9. 本轮改动清单

| 文件 | 改动 |
|---|---|
| `Docs/RuntimeCompare/MainQuest-FightMovieTiming-第11轮.md`(新) | 本报告:老端发包时序逐行确认 + `skill_damage_time` 真值表 + **第 10 轮时序假设证伪** + `damage=0` 服务端权威纠偏 + P2 不实现决策 + 精确 blocker |

> **无代码改动**(保第 10 轮基线):`Assets/Scripts/Module/Core/Scene/{FightController,SceneCombat,Vo/FightVo}.cs` 等不动;`build` 仍 0 错误(6 条既有无关警告)。`output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 未改。

### 下一轮建议(纠偏后的真实路径)

① **抓角色真实战斗属性**(att/破甲/攻击),对照 `config_mon[10001001]` 防御 → 判 `damage=0` 是否 att≤防御所致;
② **确认冒烟角色是否裸装/需升级/需换 hurt>0 技能**(career=1 lv1 全 `hurt=0`,可能要更高级技能或装备加成);
③ **拉长会话窗口**(心跳/重连重入)缓解 `remote-close` + 限频,抓后续帧广播看是否 `damage>0`;
④ 若仍 0,则 `damage=0` 锁定为服务端普攻公式 / 角色养成 blocker(yu_server 侧),客户端侧此路已到边界,转其它主线竖切子系统。
