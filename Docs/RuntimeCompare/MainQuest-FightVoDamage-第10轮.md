# 运行态 20001 伤害解析 运行态对比 · 第 10 轮

范围:把第 9 轮卡住的 **"服务端攻击结果广播 `20001 S2C` 未解析 → 不能真实扣血/死亡/推进 `100030`"** 推进一步:
**逐字段、逐字节解析 `20001 S2C`(`FightVo` 攻击者头 + `defense_list`)→ 把服务端真实新 hp / 死亡喂既有 `SceneManager → MonsterRenderer` 血条/销毁链**,并用第 9 轮真连 `108B` 原始样本做逐字节解析校验。
方法:老 Laya 客户端 `scene/fight/FightVo.ts`(`ReadFromProtocal` 收包布局)+ `FightController.ts`(`handler20001`/`RefreshObjVo`/`ClientFightServer`)为真相;Unity 侧新增逐字节对齐解析 + 第 9 轮 `108B` 样本确定性回放校验。

> 头条结论:
> **`20001 S2C(FightVo)` 字节格式本轮逐字段、逐字节 100% 确认,第 9 轮 `108B` 真实样本解析到 `108/108` 字节零残留:**
> ① 老端 `FightVo.ReadFromProtocal`(`FightVo.ts:73-173`)逐字段确认:攻击者头 `cllicihhhhhh` + 攻击方 buff 列表 + 触发技能列表 + `defense_list`(每防御者 `clliicchhci` + 自身 buff 列表);
> ② 第 9 轮 `108B` 样本独立解析(Python 复算 + Unity 编辑期 harness 双验)= **攻击者** `type=2 role=4294967355 hp=740 skill=59100001 lv=1 pos=(5340,2624) atkPos=(5374,2672) angle=0` + **1 攻击方 buff**`(iconType=200 id=59100001 lv=1 到期≈2026-06)` + **1 防御者** `type_flag=1(怪) id=1129 hp=140 damage=0 damage_flag=0 pos=(5374,2672)`,**末位精确停在第 108 字节,无残留**;
> ③ 字段语义从老端 `RefreshObjVo`(`FightController.ts:1527`)+ `ClientFightServer`(`1000-1002`)确认:**`defender.hp` = 服务端新绝对 hp(非增量),`hp==0 → ForceDoDead`(死亡移除),`damage/damage_flag` 仅飘字、不参与 hp 计算**;
> ④ Unity 已实现真实解析(`FightVo.cs` 镜像老端 `ReadFromProtocal`)并接既有血量链(`On20001Broadcast → ApplyHp / DeleteSceneObj`,与 `12009/12006` 同一渲染出口,不开假血条、不伪造伤害);
> ⑤ **本轮 Play 态真连再验:** 实发完整圆形 AOE `怪列表=[1129,1134,1131,1132]`,服务端回 **`len=222B / defenders=4`** 攻击结果广播,本端 **`remaining=0B` 零残留**解出 4 个防御者并全部喂血量链——**多防御者 + buff 列表对齐在真实大报文上验通**(比编辑期单防御者 harness 更硬)。
> **关键真实发现(P3 卡点本质收窄):跨第 9 轮 + 本轮共抓 3 条真实 `20001 S2C`(`108B/1 怪` + `222B/4 怪`×2),全部 `remaining=0B`、全部 `defender.damage=0 / hp=满 / damage_flag=0`,且每条同帧给攻击者上 `iconType=200` 自身 buff。**
> 即"hp 没变"**不是解析缺口,而是服务端对本端攻击稳定判 0 伤害**——最可能因本端无 fight-movie 动作帧、在释放边界即发(P5 时序问题),真实伤害走未捕获的后续帧广播。
> **解析侧 100% 通(含真连多防御者实证);数据侧(damage>0 样本)受 fight-movie 动作帧缺失 + 测试服 `~60–85s remote-close` 阻塞,本轮未取到。**

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮仍停"正在连接/正在启动游戏"加载页,渲染层不打日志,延续第 7/8/9 轮;P1 证据 = 源码 + 第 9 轮同测试服真连 `108B` 原始样本) |
| 老端源码(权威) | `yu_client/h5/src/scene/fight/FightVo.ts`(收包布局 `ReadFromProtocal`)、`scene/fight/FightController.ts`(`handler20001:298` / `RefreshObjVo:1527` / `ClientFightServer:911`)、`scene/SceneConfig.ts:31`(`SceneBaseType`)、`common/UserMsgAdapter.ts`(`ReadFmt` 格式字符表) |
| 真实样本(权威) | ① 第 9 轮真连 `20001 S2C` **108B/1 怪**原始 hex(`MainQuest-CombatFinish-第9轮.md` §3.3,本报告逐字节解析见 §2);② 本轮 Play 真连 `20001 S2C` **222B/4 怪**广播 ×2(§6 实跑日志,`remaining=0B` 解析) |
| Unity 取证方式 | ① 独立 Python 复算 `108B`;② Unity 编辑期 `Unity_RunCommand` 喂 `108B` hex → `FightVo.ReadFromProtocal`;③ Play 态真连(临时开 `AppConfig` 冒烟开关)实发 20024/20001 + 收 `20001 S2C` → `On20001Broadcast` 解析喂链 + 读控制台 |
| Unity 后端 | 游戏服 `ws://223.109.142.26:10000`(本轮实测 TCP 可达;沿用第 3–9 轮) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` 剑士,scene 10000,5 怪 type 10001001) |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 冒烟开关如临时打开,取证后还原。

---

## 1. P1:老端 `20001 S2C` / `FightVo` 收包布局(源码逐字段,不猜)

### 1.1 `handler20001` 入口 —— `FightController.ts:298-366`

```ts
let handler20001 = () => {
    let fightVo = fight_manager.fightVo_pool.shift() || new Self_FightVo()
    fightVo.protocal_type = 20001
    fightVo.ReadFromProtocal()                       // ← 收包布局全在 FightVo.ReadFromProtocal
    if (!scene_mgr.can_receive_scene_protocal) return
    if (fightVo.attack.role_id == mainrole_vo.role_id || fightVo.attack.is_mainRole_defender) {
        ...
        this.MergeFightDelayList()
        this.playFightMovie(fightVo)                 // ← playFightMovie → RefreshObjVo 落 hp/死亡
    } else
        this.fightHandler_delay_list.push(fightVo)   // 非主角相关:进延迟队列
}
this.RegisterProtocal(20001, handler20001)           // FightController.ts:704
```

**主角攻击怪时 `attack.role_id == 主角` 为真 → 立即 `playFightMovie → RefreshObjVo` 落库。** 本端只需复刻 `ReadFromProtocal` 收包 + `RefreshObjVo` 的 hp/死亡落库(延迟队列/动画播放属后续)。

### 1.2 `FightVo.ReadFromProtocal` 完整字节布局 —— `FightVo.ts:73-173`(权威)

格式字符(`UserMsgAdapter.ReadFmt2Func`,**BIG_ENDIAN**):`c`=u8 `h`=u16 `i`=u32 `I`=i32 `l`=u64(高32<<32|低32) `L`=i64。

**① 攻击者头 `ReadFmt("cllicihhhhhh")`(38 字节):**

| # | 字段 | fmt | 字节 | 含义 |
|---|---|---|---|---|
| 1 | `attacker_type` | c | 1 | 老端 `SceneBaseType`(2=人/玩家) |
| 2 | `role_id` | l | 8 | 攻击者 id(玩家=roleId) |
| 3 | `hp` | l | 8 | 攻击者当前 hp |
| 4 | `anger` | i | 4 | 怒气 |
| 5 | `move_anim` | c | 1 | 位移时播放的动画 |
| 6 | `skill_id` | i | 4 | 技能 id |
| 7 | `skill_level` | h | 2 | 技能等级 |
| 8 | `pos_x` | h | 2 | 攻击者 x |
| 9 | `pos_y` | h | 2 | 攻击者 y |
| 10 | `attack_pos_x` | h | 2 | 攻击点 x(`center_pos`) |
| 11 | `attack_pos_y` | h | 2 | 攻击点 y |
| 12 | `attack_angle` | h | 2 | 攻击朝向 |

**② 攻击方 buff 列表:** `h`(数量) + 每个 buff `ReadFmt("hhiccIIl")`(26 字节)= `iconType(h) buff_effect_id(h) id(i) level(c) diejia(c) integer(I) decimals(I) period(l)`。
> `decimals` 老端再过 `Util.UnsignToSigned`;C# `ReadI32` 已是有符号,无需再转。`period` = 到期 unix 时间戳(ms)。

**③ 攻击触发技能列表:** `h`(数量) + 每个 `i`(skill_id)。`attack_trigger_skill_list`,用于触发技图标,本轮不消费。

**④ 防御者列表(群攻多个):** `h`(数量) + 每个防御者 `ReadFmt("clliicchhci")`(36 字节):

| # | 字段 | fmt | 字节 | 含义 |
|---|---|---|---|---|
| 1 | `type_flag` | c | 1 | 老端 `SceneBaseType`(1怪 2人 5假人);源码注释 `int.8 1怪 2人` |
| 2 | `role_id` | l | 8 | 怪=实例 id;人=roleId |
| 3 | `hp` | l | 8 | **服务端新绝对 hp(0=死亡)** |
| 4 | `anger` | i | 4 | 怒气 |
| 5 | `damage` | i | 4 | 本次伤害(飘字用) |
| 6 | `damage_flag` | c | 1 | **0正常 1躲避 2暴击 3免疫 4会心 5护盾免伤**(`FightController.ts:1002` 注释) |
| 7 | `second_damage_flag` | c | 1 | 次级伤害标记(连暴等) |
| 8 | `pos_x` | h | 2 | 防御者 x(击退后更新) |
| 9 | `pos_y` | h | 2 | 防御者 y |
| 10 | `move_anim` | c | 1 | 受击位移动画 |
| 11 | `breaked_skill_id` | i | 4 | 被打断的技能 id |

**⑤ 每个防御者后再跟自身 buff 列表:** `h`(数量) + 每个 `hhiccIIl`(同 ②)。

> 收包顺序总览:`[攻击者头 38B] [h + buff×N] [h + 触发技×N] [h + (防御者 36B + h + buff×M)×D]`。

### 1.3 字段语义:hp 是"新绝对值",死亡看 `hp==0` —— `RefreshObjVo`(`FightController.ts:1527-1550`)

```ts
this.RefreshObjVo = (defender_list, defender_info_list, attacker, attacker_vo) => {
    ...
    for (let i = 0; i < defender_list.length; i++) {
        let defender = defender_list[i], defender_info = defender_info_list[i]
        if (defender != null && !defender.IsDead()) {
            let obj_vo = defender.GetVo()
            if (defender_info.hp)                       // hp>0 → 设新绝对 hp(非减量)
                obj_vo.ChangeVar("hp", defender_info.hp)
            ...
            if (defender_info.hp == 0)                  // hp==0 → 死亡
                defender.ForceDoDead()
        }
    }
}
```

`defender_info_list` 即 `fightVo.defense_list`(`GetDefenderListFromVo:1507` 按 `type_flag` 取 `scene.GetMonster(role_id)` / `GetRole(role_id)`)。
**老端 `12009`(`SceneController.ts:294` `lll` obj_id/hp/maxHp)走同样 `ChangeVar("hp") + hp==0 DoDead` 落库**——即 `20001 defense_list` 与 `12009` 落到**同一个 hp/死亡出口**,差别只在协议体里多了攻击者/伤害/buff。本端因此把 `20001 defense_list` 接到既有 `SceneManager.ApplyHp/DeleteSceneObj`(= `12009/12006` 出口),不另开假血条路径。

### 1.4 hp 与 damage 各管各:`ClientFightServer`(客户端伤害预测,`FightController.ts:1000-1002`)交叉印证

```ts
defense.hp = local_Math.max(0, mon_vo.hp - final_hurt)   // 新绝对 hp = max(0, 旧hp - 伤害)
defense.damage = final_hurt                               // 伤害值(飘字)
defense.damage_flag = 0                                   // 0正常减血 1躲避 2暴击 3免疫 4会心 5护盾免伤
```

证实:**`hp` 字段是服务端算完后的新绝对 hp(已 clamp≥0),`damage` 只是给飘字用的本次伤害量。本端只用 `hp` 驱动血条/死亡,`damage/damage_flag` 留作后续飘字(P5)。**

### 1.5 `SceneBaseType`(`type_flag` 取值)—— `SceneConfig.ts:31`

```ts
export enum SceneBaseType { Unknown=0, Monster=1, Role=2, Other=4, Fake_Role=5, Npc=6,
    Pet=7, MainRole=8, Horse=9, Drop=10, ... Partner=20, Demon=21 }
```

本端 `20001 defense_list` 路由子集:`type_flag==1` → `GetMonster`(怪/采集);`type_flag==2/5` → `GetRole`(玩家/假人)。其余 type 本轮只记录不处理。

### 1.6 老端运行态尝试

本轮再访 `http://127.0.0.1:8090/index.html`,**仍停"正在连接/正在启动游戏"加载页、渲染层不打日志**(与第 7/8/9 轮一致,环境阻塞延续)。
故 P1 字节证据继续以**老端源码 `ReadFromProtocal` + 第 9 轮同测试服真连 `108B` 原始样本**为准——对"逐字节格式"而言,真实样本比运行态截图更硬。

---

## 2. 第 9 轮 `108B` 样本逐字节解析表(killer evidence,独立复算 + Unity harness 双验)

第 9 轮真连原始字节(`MainQuest-CombatFinish-第9轮.md` §3.3):

```
偏移  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F
0x00  02 00 00 00 01 00 00 00 3B 00 00 00 00 00 00 02
0x10  E4 00 00 00 00 00 03 85 CB 61 00 01 14 DC 0A 40
0x20  14 FE 0A 70 00 00 00 01 00 C8 00 00 03 85 CB 61
0x30  01 00 00 00 00 00 00 00 00 00 00 00 01 9E EB E4
0x40  3E B8 00 00 00 01 01 00 00 00 00 00 00 04 69 00
0x50  00 00 00 00 00 00 8C 00 00 00 00 00 00 00 00 00
0x60  00 14 FE 0A 70 00 00 00 00 00 00 00
```

| 偏移 | 段 | 字段 | 原始 | 解析值 | 校验 |
|---|---|---|---|---|---|
| `[0]` | 攻击者 | `attacker_type` c | `02` | 2(人) | ✓ |
| `[1..8]` | 攻击者 | `role_id` l | `00..00 01 00 00 00 3B` | 4294967355 | = 我方云霄42852 ✓ |
| `[9..16]` | 攻击者 | `hp` l | `…02 E4` | 740 | 我方主角 hp |
| `[17..20]` | 攻击者 | `anger` i | `00 00 00 00` | 0 | — |
| `[21]` | 攻击者 | `move_anim` c | `00` | 0 | — |
| `[22..25]` | 攻击者 | `skill_id` i | `03 85 CB 61` | 59100001 | = 我方技能 ✓ |
| `[26..27]` | 攻击者 | `skill_level` h | `00 01` | 1 | ✓ |
| `[28..29]` | 攻击者 | `pos_x` h | `14 DC` | 5340 | = 我方坐标 ✓ |
| `[30..31]` | 攻击者 | `pos_y` h | `0A 40` | 2624 | ✓ |
| `[32..33]` | 攻击者 | `attack_pos_x` h | `14 FE` | 5374 | = 我方攻击点 ✓ |
| `[34..35]` | 攻击者 | `attack_pos_y` h | `0A 70` | 2672 | ✓ |
| `[36..37]` | 攻击者 | `attack_angle` h | `00 00` | 0 | ✓ |
| `[38..39]` | buff | `attack_buff_num` h | `00 01` | 1 | 1 个攻击方 buff |
| `[40..41]` | buff#0 | `iconType` h | `00 C8` | 200 | 自身 buff 图标类型 |
| `[42..43]` | buff#0 | `buff_effect_id` h | `00 00` | 0 | — |
| `[44..47]` | buff#0 | `id` i | `03 85 CB 61` | 59100001 | = 技能 id(buff 源=御剑一式) |
| `[48]` | buff#0 | `level` c | `01` | 1 | — |
| `[49]` | buff#0 | `diejia` c | `00` | 0 | — |
| `[50..53]` | buff#0 | `integer` I | `00 00 00 00` | 0 | — |
| `[54..57]` | buff#0 | `decimals` I | `00 00 00 00` | 0 | — |
| `[58..65]` | buff#0 | `period` l | `00..01 9E EB E4 3E B8` | 1782074064568 | 到期 ≈ 2026-06(合理近未来) ✓ |
| `[66..67]` | 触发技 | `attack_trigger_skill_num` h | `00 00` | 0 | 无触发技 |
| `[68..69]` | 防御 | `defense_num` h | `00 01` | 1 | 1 个防御者 |
| `[70]` | 防御#0 | `type_flag` c | `01` | 1(怪) | ✓ |
| `[71..78]` | 防御#0 | `role_id` l | `00..00 04 69` | 1129 | = 我方目标怪 ✓ |
| `[79..86]` | 防御#0 | `hp` l | `00..00 8C` | **140** | **满血(未掉血)** |
| `[87..90]` | 防御#0 | `anger` i | `00 00 00 00` | 0 | — |
| `[91..94]` | 防御#0 | `damage` i | `00 00 00 00` | **0** | **本帧 0 伤害** |
| `[95]` | 防御#0 | `damage_flag` c | `00` | 0(正常) | — |
| `[96]` | 防御#0 | `second_damage_flag` c | `00` | 0 | — |
| `[97..98]` | 防御#0 | `pos_x` h | `14 FE` | 5374 | ✓ |
| `[99..100]` | 防御#0 | `pos_y` h | `0A 70` | 2672 | ✓ |
| `[101]` | 防御#0 | `move_anim` c | `00` | 0 | — |
| `[102..105]` | 防御#0 | `breaked_skill_id` i | `00 00 00 00` | 0 | — |
| `[106..107]` | 防御#0 buff | `buff_num` h | `00 00` | 0 | 无防御者 buff |

**末位精确停在第 108 字节,`remaining=0`。** 解析消费 `108/108`,无残留、无错位——**格式 100% 确认**(独立 Python 复算 + Unity 编辑期 harness 双验,见 §3.2 / §6)。

---

## 3. P2:Unity 真实 `20001 S2C` 解析 + 接既有血量链

### 3.1 本轮新增/改动

| 文件 | 改动 |
|---|---|
| `Module/Core/Scene/Vo/FightVo.cs`(新) | `20001/20003` 攻击结果广播体,`ReadFromProtocal(NetReader)` 逐字节镜像老端 `FightVo.ts`:攻击者头 `cllicihhhhhh` + 攻击方 buff 列表 + 触发技列表 + `defense_list`(`clliicchhci` + 防御者 buff 列表)。只解析+留数据,buff 字段读出仅为多防御者字节对齐 |
| `Module/Core/Scene/FightController.cs` | `On20001Broadcast` 从"只记录到达"改为**真实解析 FightVo** + `ApplyDefenseListToScene`:`type_flag` 路由(1怪/2人/5假人)→ `hp>0` 调 `SceneManager.ApplyHp`(新绝对 hp,保留既有 HpLim)、`hp==0` 调 `DeleteSceneObj`(移除可见对象);找不到对象只 warning 不造假;`damage/damage_flag` 仅日志 |

仅落 `Shenxiao.Module.Core`,无新增 asmdef、无 `transform.Find`、无手改 Bind、无独立 socket、无绕 `NetManager`。`defender.hp/death` 经 `SceneManager.MonsterHpChanged → MonsterRenderer.RefreshHp` / `MonsterRemoved → RemoveView`(与 `12009/12006` 同一渲染出口)。

### 3.2 解析正确性(逐字节,与 §2 表一致)

- 独立 Python 复算:`108/108` 字节、字段值与 §2 表逐项一致、`remaining=0`。
- Unity 编辑期 harness(`Unity_RunCommand` 直接喂 `108B` hex 给 `FightVo.ReadFromProtocal`):见 §6 验收。

### 3.3 喂血量链的语义(对标 `RefreshObjVo`)

```
defender.hp > 0  → SceneManager.ApplyHp(insOrRoleId, hp, 既有HpLim) → 血条按新 hp 刷新
defender.hp == 0 → SceneManager.DeleteSceneObj(insOrRoleId)        → 可见模型/名牌/血条移除(= ForceDoDead 最小可视等价)
找不到 MonsterVo/RoleVo → 只 GameLog.Warn,不造假对象
```

> 严守红线:**未解析到服务端新 hp 前不改 `MonsterVo.Hp`**;本轮所有 hp 写入都来自解析出的 `defense_list.hp`,无任何本地伪造。

---

## 4. P3:真实扣血/死亡最小闭环 —— 解析侧通,数据侧待 `damage>0` 样本

**关键发现:第 9 轮那条 `108B` 广播的防御者 `hp=140(满)/damage=0/damage_flag=0`——服务端这一帧判 0 伤害**(同帧只给攻击者上了 `iconType=200` 自身 buff)。
即第 9 轮观察到的"`1129` hp 保持 `140/140`"**不是本端解析缺口**,而是**这条广播本就没带伤害**——老端拿到同一条广播也只会 `ChangeVar("hp",140)`(无变化)、不触发死亡。

故本轮结论分两层:
- **解析侧(P2):100% 通,且经真连多防御者样本实证。** 本轮 Play 态真连实发完整圆形 AOE(`怪列表=[1129,1134,1131,1132]`),服务端回 **`len=222B / defenders=4`** 攻击结果广播,本端 `FightVo.ReadFromProtocal` **`remaining=0B` 零残留**逐字段解出 4 个防御者并全部喂既有血量链(日志见 §6)。**这比编辑期单防御者 108B harness 更硬**——多防御者 + buff 列表对齐在真实大报文上验通。
- **数据侧(P3 可视扣血):未取到 `damage>0` 样本——且现已确认服务端"对本端攻击稳定判 0 伤害"。** 跨第 9 轮 + 本轮两次 Play、共抓 **3 条真实 `20001 S2C`**(`108B/1 怪` + `222B/4 怪`×2),**无一例外 `damage=0 / hp=满 / damage_flag=0`,且每条都同帧给攻击者上 `iconType=200` 自身 buff**。即 0 伤害不是偶发、不是解析问题,而是**服务端对"本端这种攻击"稳定返 0 伤害**(成因见 §4.1)。受测试服 `~60–85s remote-close` 阻塞(连发被服务端限频,单窗口仅 1–2 条广播回程),`damage>0`/死亡样本未取到。

### 4.1 服务端稳定判 `damage=0` 的可能成因(只记录,不臆断写码)

1. **发送时机缺动作帧(最可能)**:老端真实发包在技能动作帧 `skill_damage_time`(经 fight-movie 预播 cast 动画后),服务端据"完整 cast 序列"结算伤害;**本端无 fight-movie/动作帧,在释放边界即发**——服务端可能据此把它当"进战斗/上 buff 的 0 伤害 engage 帧",真实伤害走后续帧广播(本端无动作帧 → 拿不到那一帧 / 单窗口掉线前未到)。**此为 fight-movie 动作帧时序问题(P5),非 `20001 S2C` 字段问题。**
2. **`御剑一式` 自身 buff(iconType=200)语义**:每帧首先上 buff(id=技能、到期≈近未来),伤害可能随 buff/层数/后续帧结算(技能机制,P5)。
3. **连发限频**:本轮单窗口连发 5 次仅回 2 条广播 → 服务端按动作节奏限频,印证"非动作帧的密集请求被折叠/判 0"。

**以上均不构成"本地造假"理由**——本端只在解析到真实 `hp<max`/`hp==0` 时才动血条/移除怪;本轮所有 hp 写入都来自真实解析值(全 `=140` 满血,故可视无变化,符合真实)。

---

## 5. P4:主线 `100030` 击杀进度

依赖真实死亡事件(`defense_list.hp==0` 或服务端任务推送)。本轮未取到 `hp==0` 样本 → **未驱动到击杀 → `100030` 进度本轮不推进,记 blocker**(不本地改任务)。
下一步:稳定窗口连续攻击抓 `hp==0` 广播 → 触发本端 `DeleteSceneObj` 可视杀怪 → 观察是否伴随 `100030` 任务推送(若有 `30000/30001` 等任务协议,记录字节证据)。

---

## 6. 验收命令结果

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`:**✓ 0 错误**(Unity `AssetDatabase.Refresh` 后**重新生成 csproj 已纳入 `FightVo.cs`**——`Compile Include` 第 171 行;仅余既有无关警告 `MainRoleAgent.cs:206 CS0162` / `AppLauncher CS0649` / Generated Bind CS0108,与本轮改动无关)。
- Unity 活 Editor `AssetDatabase.Refresh` 重导入 + 域重载编译:**✓ 0 Error**(`FightVo.cs` 重导入纳入编译单元、`FightController.cs` 改动一并编过;`Unity_ReadConsole` Error 过滤 0 条)。
- 编辑期 `108B` 样本解析 harness(`Unity_RunCommand` 喂 hex → `FightVo.ReadFromProtocal`):**✓ PASS**,实跑输出:
  ```
  payloadLen=108 remainingAfterParse=0
  attacker: type=2 role=4294967355 hp=740 skill=59100001 lv=1 pos=(5340,2624) atkPos=(5374,2672) angle=0 buffs=1 trigger=0
    atkBuff[0]: iconType=200 id=59100001 lv=1 period=1782074064568
  defenders=1
    def[0]: typeFlag=1 id=1129 hp=140 damage=0 flag=0 pos=(5374,2672) breaked=0 buffs=0
  ASSERT_MATCH_REPORT=True
  ```
  逐字段与 §2 表一致,消费 `108/108`、`remaining=0`。
- Play 态真连(`unity_npc_475823114`/role 4294967355,scene 10000,5 怪)实发 + 实收 `20001 S2C`:**✓ 解析全通**——真连实发完整圆形 AOE `怪列表=[1129,1134,1131,1132]`(本轮比第 9 轮多发了全 AOE 列表),服务端回 **`len=222B` / `defenders=4`** 攻击结果广播,本端 `FightVo.ReadFromProtocal` **`remaining=0B` 零残留**逐字段解析 4 个防御者并喂血量链(日志 `怪 1129/1134/1131/1132 服务端新 hp=140/140 刷新血条`)。**但 4 个防御者 `damage=0 / hp=140(满)`**——与第 9 轮 `108B` 一致,服务端这帧仍判 0 伤害(详见 §4)。两轮 Play(各 1 窗口)共抓 3 条真实广播(`108B/1 怪` + `222B/4 怪`×2),**全部 `remaining=0B`、全部 `damage=0`**;`damage>0`/死亡样本受 `~60–85s remote-close` 阻塞未取到(连发被服务端限频,单窗口仅 1–2 条广播回程)。
- `AppConfig.asset` 冒烟开关 / `devAccount`:**✓ 已还原**(取证时临时 `autoLoginSmokeTest/autoEnterFirstRoleSmokeTest=true` + `devAccount=unity_npc_475823114`;取证后还原为 `unity_dev_001`/`false`,`git diff Assets/_App/Configs/AppConfig.asset` 为空)。

---

## 7. 仍然阻塞项 / 真实卡点(本轮只记录)

1. **服务端对本端攻击稳定判 `damage=0`(P3/P4 新头号卡点)**:解析侧 + 喂链侧全就绪(真连 `222B/4 怪`已驱动血条刷新),但 3 条真实样本全 `damage=0`。最可能因**本端缺 fight-movie 动作帧、在释放边界即发**(老端在 `skill_damage_time` 发),服务端据此当 0 伤害 engage 帧。下一轮:移植 fight-movie 动作帧 → 在 `skill_damage_time` 发 `20001`,看服务端是否返真实伤害。
2. **`damage>0 / hp==0` 样本会话窗口**:测试服 `~60–85s remote-close` + 连发限频(单窗口仅 1–2 条广播回程),即便伤害走后续帧也抓不全。下一轮:心跳/重连重入延长窗口。
3. **攻击方/防御方 buff 表现、`damage_flag` 飘字**:字段已解析(读出保持对齐),表现(buff 图标、伤害飘字、暴击/会心配色)属 P5,未编码。
4. **主角自身被击(`defender.role_id==主角`)**:本端主角 hp 在 `RoleModel`(非场景 `_roles` 表),`ApplyDefenseListToScene` 命中主角时只 warning;主角血条 UI 属后续。
5. **直线/扇形 AOE(`aoe_mode 2/3`)**:本端只圆形(延续第 9 轮),与本轮 `20001 S2C` 解析无关,记录。
6. **老端运行态截图**:仍停加载页(延续第 7/8/9 轮),P1 用源码 + 真连 `108B/222B` 样本取证。

### 下一轮建议

① **fight-movie 动作帧时序**(在 `skill_damage_time` 发 `20001`)→ 验证服务端是否对"完整 cast 序列"返真实伤害 = 解 `damage=0` 的最直接路径;
② 缓解测试服 `remote-close`(心跳/重连重入)→ 拉长窗口抓 `damage>0 / hp==0` 广播 → 可视扣血/杀怪;
③ 杀怪后观察 `100030` 击杀进度 + 抓任务推送协议;
④ `damage_flag` 飘字(0正常/1躲避/2暴击/3免疫/4会心/5护盾)+ buff 图标。

---

## 8. 本轮总结(对照任务包验收 5 条)

1. **老端 `20001 S2C FightVo.defense_list` 是否逐字段确认**:✓ **逐字段、逐字节 100% 确认**(`FightVo.ts:73-173` 源码 + 真实样本解析零残留:第 9 轮 `108B/1 怪` Python+编辑期 harness 双验,本轮 Play 真连 `222B/4 怪`×2 实证多防御者对齐)。
2. **Unity 是否实现真实解析**:✓ `FightVo.cs` 镜像老端 `ReadFromProtocal`;`On20001Broadcast` 真实解析 + 接既有 `SceneManager.ApplyHp/DeleteSceneObj` 血量链(与 `12009/12006` 同出口,不开假血条)。Play 真连 `222B/4 怪`广播 `remaining=0B` 解出 4 防御者并全部喂链(日志见 §6)。
3. **是否出现服务端真实 hp/damage/death 数据**:◑ 出现真实 `hp/damage/damage_flag` 字段且解析正确,但 **3 条真实样本(`108B`+`222B`×2)全部 `damage=0 / hp=满`**——服务端对本端攻击稳定判 0 伤害(最可能因本端缺 fight-movie 动作帧、释放边界即发,P5 时序);`damage>0/death` 样本未取到。
4. **可见血条/怪物销毁是否真实发生**:✗(精确 blocker)——解析侧 + 喂链侧就绪(真连已驱动 4 怪血条刷新,只是新 hp=满故无可视变化),缺 `damage>0/hp==0` 广播;**不本地造假扣血/死亡**。
5. **`100030` 任务进度是否真实推进**:✗ 未驱动到死亡 → 不推进,记 blocker(不本地改任务)。
6. **仍有真实 blocker + 下一轮**:① fight-movie 动作帧时序(在 `skill_damage_time` 发 20001,使服务端判真实伤害)= 新头号 blocker;② 测试服会话稳定性(抓带伤害/死亡广播);③ `damage_flag` 飘字 / buff 表现。

---

## 9. 本轮改动清单(落 `Shenxiao.Module.Core`,不新增 asmdef)

| 文件 | 改动 |
|---|---|
| `Module/Core/Scene/Vo/FightVo.cs`(新) | `20001 S2C` 攻击结果广播体 + `ReadFromProtocal` 逐字节镜像老端 `FightVo.ts` |
| `Module/Core/Scene/FightController.cs` | `On20001Broadcast` 真实解析 FightVo + `ApplyDefenseListToScene`(喂既有血量链,hp>0 刷血条/hp==0 移除怪) |

> `output/`、`.playwright-cli/` 不入库;无临时 harness 脚本入库(取证走 `Unity_RunCommand` 即时执行)。`AppConfig.asset` 取证开关如临时打开,提交前还原。
