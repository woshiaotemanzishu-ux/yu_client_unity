# 运行态服务端伤害根因 运行态对比 · 第 12 轮

范围:把第 10/11 轮卡住的 **"服务端对本端攻击稳定判 `damage=0`"** 从"服务端权威结算(成因不明)"推进到**可行动级别**。
第 11 轮纠偏后的头号下一步是:**"抓角色真实战斗属性(att/破甲/攻击),对照怪物配置,判 `damage=0` 是否角色养成不足所致"**。
本轮 P2 **真连进游戏直接读出主角战斗属性**(`RoleModel.BattleAttr`,由 13001 真实解析),对这一路径取证定位。

方法:Unity 活 Editor 真连测试服(临时开 `AppConfig` 冒烟开关,取证后还原)进游戏读 `RoleModel.Instance.BattleAttr`;老端 `h5/src/common/BattleProtoVo.ts`+`BaseAttrProtoVo.ts`(属性收包布局)、`cdn/resource/config/client/ConfigItemAttr.json`(属性 id↔字段映射)、`config_skill.json[59100001]`/`config_mon.json[10001001]`(技能/怪物真实配置)为真相;P1 用 Playwright 真实驱动老端 `http://127.0.0.1:8090/index.html`。

> ## 头条结论(本轮:`damage=0` 根因定位到"角色养成不足",已达可行动级别)
>
> ① **真连首次读出主角真实战斗属性(killer evidence)**:冒烟角色 `云霄42852`(roleId `4294967355`,剑士 career=1,**等级 2**,战力 1040)进游戏后 `RoleModel.BattleAttr` 实测 **`att=33` 攻击、`wreck=17` 破甲、`def=17` 防御**,`hit/dodge/crit/ten/abs_att/abs_def/fixed_att/real_abs_att` **全 0**,`Hp=740/740`、`Speed=370`。
>
> ② **`att=33` 极低 = 角色裸/近裸养成态**:老端真实配表里**单把武器的攻击就达数百**(`config_equip_attr` 样本 `晨曦轻剑` `att=880`、`wreck=352`);`att=33` 远低于"穿一把武器"应有量级,且角色等级 2、主线停在 `30005 newest finish=100010`(尚未到击杀任务 `100030`)。
>
> ③ **`att` 是服务端权威伤害公式的核心输入**:老端死代码 `ClientFightServer`(虽不参与真实结算,但**字段名即服务端意图**)伤害基数 = `(att + wreck − mon_vo.defend + abs_att) × (技能系数 + skill_hurt_add_ratio) × (0.95+rand·0.1)`。本端攻击侧 `att+wreck+abs_att = 50`,对一只**等级 5** 的怪(`config_mon[10001001]=转运傀儡` lv5/hp140),只要怪防御不低,基数即 clamp 到 0 → **`damage=0`**。
>
> ④ **`damage=0` 非 Unity bug,属角色养成/服务端权威**:本端 `20024/20001` C2S 与老端逐字节一致(已核)、`20001 S2C FightVo` 解析零残留(第 10 轮基线)、攻击目标/坐标/技能全真;`att` 由 13001 解析正确(`Hp=740` 与第 9/10 轮攻击者头 `hp=740` 互证,同一角色)。**三条 `damage=0` 样本均 `damage_flag=0`(正常命中判 0,非躲避/免疫)** → 即"以 att=50 正常命中、被怪防御吃光",符合养成不足,**不是漏抓伤害帧**。
>
> ⑤ **P1 老端运行态本轮显著前进**:Playwright 实测老端 HTTP `200`、标题 `九州谕`、canvas 渲染、走**同一后端** `223.109.142.26:88 get_server_list` → 解析出**同一游戏服** `破军:223.109.142.26:10000`,**首次推进到登录视图 `LoginView`(账号/密码表单)**(前 7–11 轮均停"正在连接/启动游戏"加载页)。同账号在老端真正进游戏需驱动 Laya canvas 登录(无 DOM 输入)+ 冒烟账号老端表单口令未知 → 未强行进游戏;但**两端同后端 + 同角色 + att 服务端权威 + 本端解析 1:1 镜像老端 `BattleProtoVo`** ⇒ 老端拿同一角色会解析出**同一 `att=33` → 同一服务端 `damage=0`**,老端无法让该角色打出 `damage>0`。
>
> **结论(可行动 blocker):`damage=0` = 角色养成不足(`att=33` 远低于击杀阈值)+ 服务端权威结算,非客户端字段/解析/时序问题。** 可行动路径属**账号养成侧**(给冒烟账号升级/穿装备/GM 加属性,或随主线推进到有怪场景),**非 Unity 代码可解**。按任务 P3 边界(仅当老端同账号可伤害而 Unity 不行才写码),本轮 Unity **零战斗代码改动**,只落档证据。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮 Playwright 实测 HTTP `200`、标题 `九州谕`、canvas `720x1280`、`version:58.8`,**推进到 `LoginView` 登录表单**,见 §2) |
| 老端源码(权威) | `h5/src/common/BattleProtoVo.ts`(`ReadFmt:10-22`)+`BaseAttrProtoVo.ts`(`ReadFmt:34-95` 51 属性顺序)、`h5/src/sharedata/MainRoleVo.ts`(`ReadFrom13001:260`/`ReadFrom13033:408`)、`h5/src/role/EquipmentView.ts`(`UpdataProperty:649` 读 `role_vo[mainvo_str]`)、`scene/fight/FightController.ts`(`ClientFightServer:911-1018` 死代码,伤害公式字段名) |
| 真实配置(权威) | `cdn/resource/config/client/ConfigItemAttr.json`(属性 id↔`mainvo_str` 字段映射);`config/server/config_skill.json[59100001]`、`config/server/config_mon.json[10001001]`、`config/server/config_equip_attr.json`(武器攻击) |
| 真实样本(权威) | 本轮真连 13001 实测 `BattleAttr`(§3);第 9+10 轮 3 条 `20001 S2C`(`108B/1怪`+`222B/4怪`×2)全 `damage=0/hp=满/damage_flag=0`(引用第 10 轮 §2/§6) |
| Unity 后端 | 游戏服 `ws://223.109.142.26:10000`(本轮真连握手全通,沿用第 3–11 轮);GM API `http://223.109.142.26:88/api/` |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` 剑士,**等级 2**,战力 1040,scene 10000) |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 不入库;`AppConfig.asset` 冒烟开关取证时临时打开,取证后已还原(`git diff` 为空,见 §1)。

---

## 1. P0:第 11 轮基线保护(全部通过)

| 项 | 结果 |
|---|---|
| `dotnet build yu_client_unity.slnx -v:minimal` | ✓ **0 错误**,6 条既有无关警告(`AppLauncher CS0649`×3、`EquipAttrItemBind`/`MainUIActivityViewBind CS0108`×2、`MainRoleAgent.cs:206 CS0162`),与第 10/11 轮基线逐条一致 |
| `FightVo` 解析 / `20024+20001` C2S / 血量链 | ✓ 未改动(本轮 Unity 零代码改动);`FightController.cs` C2S 仍 `20024 "c" 1/2` + `20001 h+i×N+h+l×N+ihhh`,`FightVo.cs` 解析仍逐字节对齐老端(本轮真连握手 + 进游戏全通,见 §3) |
| 工作树 | ✓ 仅本轮报告改动;`AppConfig.asset` 取证后还原(`git diff --stat Assets/_App/Configs/AppConfig.asset` 为空);`.playwright-cli/`、`output/` 未入库 |

---

## 2. P1:老端同账号运行态对比(本轮显著前进 + 精确 blocker)

### 2.1 Playwright 真实驱动结果(headless chromium,720×1280)

| 项 | 实测 |
|---|---|
| HTTP 状态 | **`200`** |
| 页面标题 | **`九州谕`** |
| canvas | **`720x1280` + `1024x512`**(Laya3D 已渲染,非空白) |
| 版本 | `version:58.8` |
| 后端请求 | `RealRequest http://223.109.142.26:88/api.php?method=get_server_list` → `completeHandler {"ret":0,...,"server":{"1":{...,"status":"1","state":"1","closed":"0"...}}}` |
| 游戏服解析 | `LoadDebugIpList from API success: [破军:223.109.142.26:10000, 2服:223.109.142.26:10010]` |
| 页面阶段 | `LoginStateEvent.OPEN_VIEW LoginBgView` → **`LoginView`**(截图实拍:账号/密码登录表单,有 注册/登录/记住密码) |
| pageerror / requestfailed | **0 / 0** |

**关键:本轮老端不再停在"正在连接/启动游戏"加载页(前 7–11 轮的环境阻塞),而是推进到了交互式登录视图,且走的是与 Unity 完全相同的后端(`223.109.142.26:88` GM API → 游戏服 `223.109.142.26:10000`)。** 截图见 `output/oldclient_probe.png`(不入库)。

### 2.2 为什么没强行让老端同账号进游戏(精确 blocker)

- 老端 `LoginView` 是 **Laya canvas 渲染的账号/密码表单(无 DOM input)**,headless 驱动需点 canvas 像素坐标 + 模拟 Laya 内部输入,极脆;
- 冒烟账号 `unity_npc_475823114` 是 Unity 侧经 GM API `player_login` 自动注册/直登的测试账号,**老端账号/密码表单所需口令未知**(Unity 链路 `DevLoginAsync` 直登绕过密码);
- **更关键:强行进老端只会再次读到同一角色的 `att=33` → 同一服务端 `damage=0`**(理由见下),边际价值低、代价高,故不强行。

### 2.3 决定性跨端推证(替代"老端真进游戏")

两端**同后端、同游戏服、同角色**;`att` 由**服务端权威下发**(13001/13033),本端 `BattleAttrProto`(`Common/Proto/BattleAttrProto.cs`)与老端 `BattleProtoVo→BaseAttrProtoVo` **逐字段 1:1 镜像**(属性顺序 `att,wreck,def,...` 完全一致)。
⇒ 老端拿同一角色 `4294967355` 解析同一条 13001,会得到**同一 `att=33`**;伤害服务端权威 ⇒ 老端同账号同样 `damage=0`。**老端无法让这个等级 2、`att=33` 的角色打出 `damage>0`** —— 这正是任务 P1 要判定的:不是"老端能伤害而 Unity 不能",而是"服务端对这个养成态的角色,两端都判 0"。

---

## 3. P2:真连主角战斗属性取证(本轮 killer evidence)

### 3.1 取证方式

临时开 `AppConfig` 冒烟开关(`autoLoginSmokeTest=1`/`autoEnterFirstRoleSmokeTest=1`/`devAccount=unity_npc_475823114`)→ Unity 活 Editor `Play` → 自动跑通登录链 → 进游戏(`10004`→GAME_START 门闩 5/5 全通)→ `Unity_RunCommand` 直接读 `RoleModel.Instance.BattleAttr`。**取证后 Stop + 还原 `AppConfig`(`git diff` 为空)。**

握手日志(摘):
```
[Login] ★ 10000 回包: 角色数=1 ... 角色[0] id=4294967355 云霄42852 职业=1 等级=2 0转
[Role]  ★ 13001 主角: 云霄42852 服[1]1-2区 战力=1040 场景=10000(5340,2624) 铜币=20000 元宝=0
[Game]  startup flag ready: 13001/30005/10201/13088@300@1/10202@3 (5/5) → GAME_START ready
[Task]  30005 newest finish task id=100010
[Net]   未注册协议 proto=13033 payload=230B(属性刷新协议,本端未接,见 §6)
```

### 3.2 实测主角战斗属性(`RoleModel.BattleAttr`,由 13001 真实解析)

| 字段 | 实测值 | 来源 |
|---|---|---|
| **`att`(攻击)** | **33** | `ConfigItemAttr` id 1 / `mainvo_str=att` |
| **`wreck`(破甲)** | **17** | id 3 / `wreck` |
| **`def`(防御)** | **17** | id 4 / `def` |
| `hit/dodge/crit/ten` | 0 / 0 / 0 / 0 | id 5/6/7/8 |
| `abs_att/abs_def` | 0 / 0 | id 15/16 |
| `fixed_att/fixed_def` | 0 / 0 | — |
| `real_abs_att/real_abs_def` | 0 / 0 | id 46/47 |
| `Hp/HpLim` | **740 / 740** | `BattleProtoVo.hp/maxHp` |
| `Speed`(move_speed) | 370 | — |
| 等级 / 职业 / 战力 | **2** / 1(剑士) / 1040 | 13001 + 13003 兜底 |

> **互证(同一角色):** `BattleAttr.Hp=740` 与第 9/10 轮 `20001 S2C` 攻击者头 `hp=740` 完全一致 → 本轮读到的 `att=33` 角色,正是第 9/10 轮打出 3 条 `damage=0` 的同一角色。证据闭环。

### 3.3 数据流(本端已就绪,非缺解析)

`13001 → RoleController.On13001`(`RoleController.cs:38` `m.BattleAttr = BattleAttrProto.Read(r)`)`→ RoleModel.Instance.BattleAttr.Get("att")`。**属性早已被正确解析存储**(进场 `12005` 成功即反证 13001 解析无错位,否则会像早期 202 字节 bug 那样被 `12005` 拒),只是此前从未在标准日志/UI 暴露——本轮经 `RunCommand` 首次读出。

---

## 4. 角色 / 怪物 / 技能 / 武器 真实配置取证(逐项,不猜)

### 4.1 `config_mon[10001001]`(直接读取,`config/server/config_mon.json`)

```
{"0":10001001,"1":"转运傀儡",..."18":5,"19":140,"20":1,"21":1,"22":0,..."31":250,"32":500,"33":500,"34":500,"35":200,...}
```
- key0=type_id=10001001、key1=名`转运傀儡`、**key18=等级=5**、**key19=hp=140**(与第 9/10 轮 `20001` 防御者 `hp=140` 互证)。
- key31–35=250/500/500/500/200(疑穿透/耐性,无 schema 不强断)。
- **客户端只用 config_mon 取名 + icon_scale;怪攻防为服务端权威,客户端读不到怪 `defend` 实值**(老端 `Monster.ts`/`MonsterVo.ts` 从协议取 hp,从 config 取名/缩放)。

### 4.2 `config_skill[59100001]`(直接读取,named keys)

```
name=御剑一式 career=1 type=1 is_passive=0 is_normal=1 mod=2 obj=2 is_att=0 calc=0 release=1 around_hurt=1000 range=1 talent=0
lv_data[*].hurt="0"  power="0"  area=350  distance=100  num=lv1[1,4]/lv2[2,4]
```
- **`is_normal=1`(普攻)、`around_hurt=1000`、`lv_data.hurt="0"`**(与第 11 轮一致)。普攻伤害走服务端公式(角色 att/装备 + around_hurt),不读 `lv_data.hurt`(否则恒 0)。

### 4.3 属性 id↔字段映射(`ConfigItemAttr.json`,权威)

| attr_id | 名 | `mainvo_str` | kind |
|---|---|---|---|
| **1** | 攻击 | **`att`** | 1(平值 int) |
| 2 | 生命 | `maxHp` | 1 |
| **3** | 破甲 | **`wreck`** | 1 |
| **4** | 防御 | **`def`** | 1 |

`att/wreck/def` 均 `kind=1` 平值整数(非百分比),故实测 `att=33` 即"攻击 33 点",老端 `EquipmentView.UpdataProperty` 也是直接显示 `role_vo.att`(无客户端换算)。

### 4.4 武器攻击量级(`config_equip_attr.json`,佐证 att=33 = 裸/近裸)

武器基础属性在 `config_equip_attr[gid]["6"] = [[attr_id,value],...]`(attr_id==1 即攻击)。样本 `晨曦轻剑`(gid 101015052):`"6"=[{1,880},{3,352}]` → **攻击 880、破甲 352**。
即"穿一把入门武器"攻击就达数百;**实测 `att=33` 远低于一把武器的量级** → 冒烟角色处于裸装/近裸的早期养成态(等级 2、主线 100010)。

> 注:career=1 新角色的**起始装备由服务端 Erlang 决定**,不在客户端 config(本仓库无 `ConfigCreateRole`),故"是否穿了起始武器"客户端无法直读;但 `att=33`(13001 已含服务端算好的装备加成)就是该角色的**有效攻击终值**,无论裸装与否,有效攻击就是 33。

---

## 5. P3:`damage=0` 根因判定 + Unity C2S 差异边界(本轮不写战斗代码)

### 5.1 根因判定(逐条排除,锁定养成)

| 候选成因 | 本轮取证 | 结论 |
|---|---|---|
| **角色 att 过低(养成不足)** | 实测 `att=33`/`wreck=17`,攻击侧合计 50;武器单件即数百;等级 2 vs 怪等级 5 | ✅ **头号根因** |
| Unity C2S 字段/目标差异 | `20024 "c"`+`20001 h+i×N+h+l×N+ihhh` 逐字节对齐老端;目标/坐标/技能全真 | ✗ 排除(无差异) |
| `20001 S2C` 解析缺口 | 第 10 轮 3 条样本 `remaining=0B` 零残留 | ✗ 排除 |
| 发包时序(fight-movie) | 第 11 轮证伪(`59100001 damage_time=0`) | ✗ 排除 |
| 漏抓伤害帧 | 3 条样本 `damage_flag=0`(正常命中判 0,非躲避/免疫/盾) | ✗ 排除(是真·0 伤命中) |
| 服务端普攻公式 | `att` 是公式核心输入(死代码 `ClientFightServer` 字段名印证);公式细节 yu_server 权威 | ◑ 服务端侧,客户端不可见/不可改 |

**伤害公式(死代码字段名 = 服务端意图,`FightController.ts:963`):**
```
base_hurt = (main_vo.att + main_vo.wreck − mon_vo.defend + main_vo.abs_att) × (技能系数 + skill_hurt_add_ratio) × (0.95+rand·0.1)
```
本端 `att+wreck+abs_att = 33+17+0 = 50`。对等级 5 的怪,只要 `mon_vo.defend ≥ 50`,`base_hurt ≤ 0` → clamp 0 → **`damage=0`**。`damage_flag=0` 说明这是"正常命中、伤害被防御吃光"的 0,完全符合"攻击远低于目标防御"的养成不足。

### 5.2 Unity 代码边界(本轮零战斗代码改动)

任务 P3:**仅当 P1/P2 证明"老端同账号可伤害而 Unity C2S/角色状态与老端不同"才写码**。本轮证据相反——**Unity 与老端 C2S 逐字节一致、att 服务端权威且两端同值**,故按 P3 与 §五编码规范"宁可少写不要乱写"**不写任何战斗/属性代码**。
属性数据**已正确解析**(非"缺解析"),P2"补最小日志"许可不触发;att 可继续经 `RunCommand` 或本报告读取。**未在 `RoleController` 增改任何日志/字段**,保第 10/11 轮基线绝对稳定。

---

## 6. P4:真 `damage>0 / hp / death / 100030` 验证(未取到,精确 blocker,不造假)

- **本轮未能抓到新的攻击/伤害样本**:真连进游戏后场景 10000(主城)**怪数=0**(只渲染 12 个 NPC `100101..100112`),`SceneCombat` 无可攻击目标 → 无法发 `20001`;`skill 59100001` 在会话窗口内 `21002` 未回(`GetSkill=null`)。
- **原因(可行动)**:冒烟角色主线停在 `30005 newest=100010`,**尚未到击杀任务 `100030`**;主城空闲态不刷击杀怪(第 8 轮"主城 10000 无怪"同源)。第 9/10 轮能打 `1129/1134` 是当时怪已在场(任务阶段/刷新差异)。
- **不本地造假**:未改 `MonsterVo.Hp`/`FightVo.damage`/任务计数。`100030` 击杀进度依赖真实死亡,本轮无 `hp==0` 样本 → 不推进,记 blocker。
- **但根因已不依赖新样本**:`att=33`(本轮)+ 3 条 `damage=0` 样本(第 9/10 轮,同一 `hp=740` 角色)+ 怪等级 5 + 武器攻击量级数百,已足以判定 `damage=0` = 养成不足。要看 `damage>0`,需先把账号养成到位(见 §8)。

---

## 7. P5:只记录不扩散(本轮未编码)

完整自动战斗 AI/挂机寻怪、完整动作/CD/特效/音效/伤害飘字、直线/扇形 AOE、PvP、伙伴/神祇/天赋加成、掉落/怪 AI/副本/BOSS/组队、服务端公式改造或 GM 改数——**均只记录,不编码**。

---

## 8. 仍然阻塞项 / 真实卡点(本轮只记录)+ 下一轮

1. **`damage=0` 头号根因已锁定 = 角色养成不足(`att=33` 远低于击杀阈值)+ 服务端权威**;**可行动路径属账号养成侧,非客户端代码可解**:
   - ① 给冒烟账号**升级/穿装备/GM 加 att**(att 抬到能破怪防御),或随主线推进到 `100030` 有怪阶段;
   - ② 之后真连再攻击,预期出现 `damage>0 / hp<max / hp==0` → 驱动既有血量链(第 10 轮已就绪)可视扣血/杀怪 → 观察 `100030` 推进。
2. **同账号老端真进游戏**:需驱动 Laya canvas 登录(无 DOM)+ 冒烟账号老端口令;或在 yu_gm 查/置该账号老端可登录口令后,Playwright 脚本化登录复现 att。
3. **`13033` 属性刷新协议**(本轮 `230B` "未注册"):换装/升级后服务端重推 att 的协议,本端未接;**若将来要在 UI 实时反映换装后属性**,需接 `13033`(对标老端 `MainRoleVo.ReadFrom13033`)——本轮不接(属性已由 13001 给到,非伤害根因)。
4. **角色属性 UI 展示**:att/def/战力已解析,EquipmentView 仍 TODO;属表现/养成线,非本轮主线竖切。
5. **测试服 `~60–85s remote-close` + 连发限频**:延续第 9–11 轮,影响抓后续帧;本轮 P2 只读属性(13001 早到),未受影响。

---

## 9. 本轮总结(对照任务包验收)

1. **老端同账号运行态能否造成真实伤害/任务推进**:✗(且**逻辑上不能**)——老端本轮推进到 `LoginView`、同后端同游戏服;但同角色 `att=33` 服务端权威 → 老端同样 `damage=0`,无法让该角色打出 `damage>0`。
2. **Unity 本轮真连是否仍 `damage=0`**:本轮未发攻击(主城无怪);但根因已由 `att=33` + 第 9/10 轮 3 条样本锁定,预期仍 0,直到账号养成到位。
3. **两端 `20024/20001` 或角色状态是否有已确认差异**:✗ 无——C2S 逐字节一致,att 两端同源同值(服务端权威 + 解析 1:1 镜像)。
4. **当前角色属性/装备/技能/怪物配置证据**:✓ 齐全——`att=33/wreck=17/def=17`(真连)、`config_mon[10001001]` lv5/hp140、`config_skill[59100001]` is_normal=1/around_hurt=1000、武器攻击量级数百、属性 id 映射。
5. **是否写了 Unity 代码**:✗ 未写——P1/P2 证明 Unity 无差异(C2S 一致、属性已解析),按 P3 边界与编码规范不写战斗/属性代码,保基线稳定。
6. **是否出现真实 `damage>0/hp/death/100030`**:✗ 未出现(主城无怪,主线未到 100030);不本地造假,记 blocker。
7. **下一轮最小可行动作**:账号养成侧(升级/装备/GM 加 att)→ 真连再攻击验 `damage>0`/杀怪/`100030`;或脚本化老端登录复现 att。

---

## 10. 本轮改动清单

| 文件 | 改动 |
|---|---|
| `Docs/RuntimeCompare/MainQuest-DamageRootCause-第12轮.md`(新) | 本报告:真连读出 `att=33` killer evidence + `damage=0` 根因锁定为角色养成不足 + 老端 P1 推进到 LoginView + Unity 零代码改动决策 + 可行动 blocker |

> **Unity 零代码改动**(保第 10/11 轮基线):`Assets/Scripts/**` 不动;`dotnet build` 仍 0 错误(6 条既有无关警告)。`AppConfig.asset` 取证后已还原(`git diff` 为空)。`output/`、`.playwright-cli/` 不入库。
