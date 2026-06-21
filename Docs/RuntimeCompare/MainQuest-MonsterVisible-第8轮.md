# 运行态怪物可见渲染 运行态对比 · 第 8 轮

范围:把第 7 轮"真实 `MonsterVo` 已入 `SceneManager`、`SceneCombat` 能命中真实怪、但**无 `MonsterSpawner`、怪只是数据态、GameView 不可见**"的缺口,
推进成 **真实 `MonsterVo` → 最小 `MonsterRenderer` → GameView 可见怪物模型 + 名牌/血条 → 技能点击仍命中同一真实可见怪** 的最小竖切。
方法:老 Laya 客户端 `Monster.ts`/`config_mon` 源码 + 运行时协议流为真相;Unity 侧 **真连测试服 + Play 态** 端到端取证 + **编辑期确定性资源探针**。

> 头条结论:
> **本轮 Unity 侧把"数据态怪"接成了"GameView 可见怪",全链真实数据、无一造假、无 cube 占位:**
> 真连角色 `云霄42852`(roleId 4294967355)进游戏后复位在第一条打怪点 `(5340,2624)` 附近,测试服九宫格 `12002`/`12012` **真实下发 5 只击杀目标怪**
> `type=10001001`(`monster_res=10020103`,`hp=140/140`,`can_attack=1`,服务器实例名 `转运达摩`,实例 `ins=1129/1130/1131/1132/1134`)到 `SceneManager`;
> 新增 `MonsterRenderer` 订阅 `SceneManager.MonsterAdded` → 用真实资源链 `object/monster/model_clothe_10020103/model_clothe_10020103`(**真实蒙皮模型 614 三角/452 顶点**,非 cube)
> 加载成 3D 模型摆进 `SceneCharacterStage` 合成台,**5 只怪全部渲染出来**(`__SceneCharStage/Chars` 下 5 个 `model_clothe_10020103(Clone)` 蒙皮渲染体 + 5 个名牌 + 血条),
> 游戏视图截图可见怪物模型 + 红色名牌 + 血条围在主角周围。
> **P3 同会话验通:** 点击已学目标技能 `59100001` → `SceneCombat.MainRoleAttackTarget` → `CurrentTargetId=1129` —— **正是 5 只可见怪之一**(渲染体 `model_clothe_10020103(Clone)`)。
> **关键事实:** 怪名牌名取**协议 `vo.name`(`转运达摩`)优先**(对标老端 `Monster.ts:326 SetName(this.vo.name)`),`config_mon["1"]`(`转运傀儡`)仅为模板名,只有 boss 升级路径才覆盖。
> **仍阻塞:** 真实 `20024`/`20001` 发送(fight-movie/AOE 链,P4 不猜不发)、怪物朝向/移动插值/受击死亡动作、测试服 ~60–85s 周期 remote-close + 重连不稳定重入。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮在线 HTTP 200,len 25197;但停在"正在连接服务器~/正在启动游戏"加载页,见 §1.4) |
| 老端协议流(权威,复用第 7 轮) | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(含 `12002`/`12007`/`12012` 怪进视野协议) |
| 老端源码(权威) | `yu_client/h5/src/scene/sceneobj/Monster.ts`、`SceneObj.ts`、`util/GameResPath.ts`、`cdn/resource/config/server/config_mon.json` |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`;游戏服 `ws://223.109.142.26:10000`(GM get_server_info 解析) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852` 剑士,复用第 3–7 轮角色) |
| Unity 取证方式 | ① 编辑期确定性资源探针(`Unity_RunCommand` 直接 `AssetDatabase`/`MonsterConfigs`/`ResourcePath`,无网络);② Play 态真连(`AppConfig` 冒烟开关临时开)`RunCommand` 单命令驱动 + 回读 `SceneManager`/`SceneCombat`/节点树 + `ScreenCapture` 游戏视图截图 |
| Unity 证据 | 见下文逐条引用:控制台 `[Scene] monster visible` 日志 + 调用栈、`__SceneCharStage` 节点树、`output/runtime_unity/_mon_v8_gameview.png` 截图 |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/`、同步进来的 `config_mon.json`(`Assets/GameRes/resource/config/server/`,被 gitignore)均不入库;
> `AppConfig.asset` 的冒烟开关(`autoLoginSmokeTest/autoEnterFirstRoleSmokeTest`)与 `devAccount` 取证后已还原(`git diff` 为空)。

---

## 1. 老端运行态真相:第一条打怪怪物如何显示

### 1.1 第一条打怪怪 = 场景常驻 `10001001`(`config_task` 100030 + `config_mon`)

`config_task` 100030 击杀字段(第 7 轮已采):`[["kill","1","10001001","3","10000","5463","2678","1","降服3只转运灵","0","0"]]`
—— 场景 `10000` 内 `(5463,2678)` 击杀 `10001001`×3,任务条文案"降服3只**转运灵**"。

`config_mon`(`yu_client/cdn/resource/config/server/config_mon.json`,数字索引键)`10001001` 行实采:

```json
"10001001":{"0":10001001,"1":"转运傀儡","6":"default_mon","10":10020103,"11":"0.9","14":10020103,"16":-1,"18":5,"19":140, ...}
```

| 列 | 值 | 含义 |
|---|---|---|
| `"0"` | `10001001` | type_id |
| `"1"` | `转运傀儡` | **模板名**(注意:运行时服务器下发的 `vo.name` 实采为 `转运达摩`,见 §1.3) |
| `"10"` | `10020103` | **monster_res(服务端 Body,模型资源 id)** |
| `"11"` | `"0.9"` | icon_scale 模型缩放 |
| `"19"` | `140` | hp(与服务器 `12007` 下发 `hp=140/140` 一致,交叉校验列序无误) |

**`type_id`(10001001)≠ `monster_res`(10020103):模型必须用 `monster_res`,不是 `type_id`** —— 故 `model_clothe_10001001` 不存在,`model_clothe_10020103` 才是真实模型。

### 1.2 老端怪渲染口径(`Monster.ts` / `SceneObj.ts`,权威源码)

| 维度 | 老端实现 | 出处 |
|---|---|---|
| 模型 | 3D 模型 `CreateClotheSprite(this.vo.monster_res)` → `resource/object/monster/objs/model_clothe_${monster_res}.lh` | `Monster.ts:151-152`、`SceneObj.ts:359-360`、`GameResPath.GetObjectPath`(mName=`monster`) |
| 缩放 | `this.SetScale(this.vo.icon_scale)`(config_mon `icon_scale`) | `Monster.ts:140-157` |
| 名牌名 | `name_board.SetName(this.vo.name)`(**协议 vo.name**;boss 升级路径 `level_boss_cfg`/`turn_boss_cfg` 才 `vo.name=cfg.name`,非普通野怪) | `Monster.ts:326`、`Monster.ts:702-708` |
| 血条 | `name_board.SetHp(vo.hp, vo.maxHp)`;`SetHpState` 对采集类(`TASK_COLLECT/COLLECT/...`)`SetHpVisible(false)` | `Monster.ts:173-175,294-308` |
| 血条色 | 可被主角攻击 `SetRedBar()`,否则 `SetGreenBar()` | `Monster.ts:317-322` |

**结论:老端第一条打怪怪 `10001001`(运行名"转运达摩")= 3D 蒙皮模型(`model_clothe_10020103`)+ 头顶 NameBoard(名字 `vo.name` + 红色血条),
场景常驻刷怪(走近 `(5463,2678)` 即由九宫格下发),点击/自动战斗前即可见。**

### 1.3 服务器运行态 `vo.name` 实采 = `转运达摩`(同一测试服,Unity 回读)

本轮真连同一测试服回读 5 只怪的协议 `vo.name` 字段(`MonsterVo` "hhiillhshisiicccccicccclllishi" 第 8 字段 Name),**全部 = `转运达摩`**:

```
MON ins=1129 type=10001001 monster_res=10020103 hp=140/140 canAtk=1 collect=False pos=(5374,2672) name="转运达摩"
MON ins=1130/1131/1132/1134 ... name="转运达摩"
```

即 **运行态老端头顶显示的名字是 `转运达摩`(服务器 `vo.name`),不是 config 模板名 `转运傀儡`** —— Unity 名牌按此口径取 `vo.Name` 优先(§2.1)。

### 1.4 老端运行态截图卡点(精确记录,引用协议流为最低证据)

本轮尝试老端运行态重采怪物截图未成:
- `http://127.0.0.1:8090` 在线但停在"正在连接服务器~/正在启动游戏..."加载页(`page-*.yml` 实采),未进入游戏世界。
- 第 7 轮战斗驱动脚本 `output/oldend_combat_drive_v6.mjs` 现失效(`ERR_MODULE_NOT_FOUND: playwright`,从 `output/` 跑 node 解析不到 playwright),重驱需修 harness + 重跑登录/主线。
- 老端 console **不打印渲染层日志**(无 `CreateClotheSprite`/`model_clothe` 输出),即使重进也只能拿协议流。

故 P1 老端运行态证据 = **第 7 轮协议流(`12007`/`12012` 把 `10001001` 下发进视野,老端 SceneObj 系统按 §1.2 即渲染)+ §1.1 config + §1.2 源码 + §1.3 同测试服 `vo.name` 实采**。
该测试服与 Unity 真连的是**同一台**,下发的正是同一批实例(`ins=1129` 等),交叉印证老端运行态确有这批可见怪。

---

## 2. P2:Unity 最小真实怪物渲染链(**已跑通,GameView 可见**)

### 2.1 本轮新增/改动

| 文件 | 改动 |
|---|---|
| `Module/Core/Scene/MonsterRenderer.cs`(新) | 订阅 `SceneManager.MonsterAdded/MonsterRemoved/MonsterMoved/MonsterHpChanged`;只渲染真实 `MonsterVo`;模型 = `object/monster/model_clothe_{vo.MonsterRes}/...`,经 `ResManager` 加载摆进 `SceneCharacterStage`(复用 `NpcRenderer` 坐标口径:偏移=怪像素-主角像素,每帧跟随);名牌名 `vo.Name` 优先(回退 `config_mon`)+ 血条(`vo.Hp/HpLim`,采集类不显);待机 `object/monster/action/{res}/idle`;epoch/stale 防过期;`EVT_SCENE_OBJECTS_CLEARED`/真断线清场 |
| `Module/Core/Scene/MonsterConfigs.cs`(新) | `config_mon` 数字键访问器(`"1"`=name、`"10"`=monster_res、`"11"`=icon_scale);缺表优雅降级(空表+Error,渲染回退 `vo.Name`+默认缩放) |
| `Editor/ConfigGenerator/ClientConfigSync.cs` | `SYNC_LIST_SERVER` 加 `config_mon`(从 yu_client 同步进 `Assets/GameRes/resource/config/server/config_mon.json`,gitignore 不入库) |

仅落 `Shenxiao.Module.Core` + `Shenxiao.Editor`,无新增 asmdef、无新依赖、无 `transform.Find`、无手改 Bind。

### 2.2 编辑期确定性资源探针(无网络,证资源链真实)

`Unity_RunCommand` 直接走 `MonsterConfigs` + `AssetDatabase` + `ResourcePath`(与 `MonsterRenderer` 同一 key 构造)实采:

```
config_mon.Get(10001001): name=转运傀儡  body(monster_res)=10020103  iconScale=0.9
prefab path=Assets/GameRes/object/monster/model_clothe_10020103/model_clothe_10020103.prefab  exists=True
ResManager key=object/monster/model_clothe_10020103/model_clothe_10020103  editorAddr=(同)  match=True
model instance: renderObj=model_clothe_10020103(Clone)  renderer=SkinnedMeshRenderer  tris=614  verts=452  mainTex=model_clothe_10020103
idle clip=Assets/GameRes/object/monster/action/10020103/idle.anim  exists=True  legacy=True
```

证明:① `config_mon` 同步后可读出真实名/资源/缩放;② `MonsterRenderer` 构造的 key 与 `ResManager` 编辑期地址**精确相等**(同 `NpcRenderer` 既有路线,必能加载);
③ 是**真实蒙皮 3D 模型(614 三角/452 顶点,贴图 model_clothe_10020103)**,绝非 cube/占位;④ 待机片段存在且为 legacy(可挂 `Animation` 播放)。

### 2.3 Play 态真连:5 只真实怪全部渲染可见

真连进游戏后角色复位在第一条打怪点附近,九宫格真实下发 5 只怪,`RunCommand` 回读:

```
role hasBase=True id=4294967355 name=云霄42852 scene=10000 pos=(5340,2624) | agent=True | monsters=5 npcs=34 roles=1
```

`SceneManager.AllMonsters`(全部 `type=10001001 monster_res=10020103 hp=140/140 canAtk=1 collect=False name="转运达摩"`):

| ins | pos |
|---|---|
| 1129 | (5374,2672) |
| 1130 | (5444,2984) |
| 1131 | (5572,2800) |
| 1132 | (5668,2656) |
| 1134 | (5264,2840) |

合成台节点树 `__SceneCharStage/Chars`(14 个 tilt):

```
tilt[0]=MainRoleTilt  model=model_clothe_1111(Clone)        smr=True   (主角)
tilt[1..8]=SceneCharTilt model=model_clothe_100101..100108(Clone) smr=True  (8 NPC,NpcRenderer)
tilt[9..13]=SceneCharTilt model=model_clothe_10020103(Clone) smr=True       (★5 只怪,MonsterRenderer)
   localPos≈(0.34,-1.48)/(3.28,-1.32)/(-0.76,-3.16)/(1.04,-4.60)/(2.32,-2.76)  (相对主角偏移,口径与主角/NPC 一致)
__MonsterNameplates children=5   __MonsterRendererDriver exists=True
```

渲染层自身日志 + 调用栈(证真实协议链路驱动,非手塞):

```
[Scene] monster visible: ins=1129 type=10001001 model=object/monster/model_clothe_10020103/model_clothe_10020103 pos=(5374,2672)
   ← MonsterRenderer.OnMonsterAdded ← SceneManager.AddMonster ← SceneController.ParseMonster ← On12002 ← NetManager.Pump ← AppLauncher.Update
   (ins=1130/1131/1132/1134 同)
```

游戏视图截图:`output/runtime_unity/_mon_v8_gameview.png` —— 主角周围可见多只 `转运达摩` 怪模型 + 红色名牌 + 血条(及上方 NPC),怪物在地图上可见。

清场闭环:测试服 remote-close 时 `EVT_SCENE_OBJECTS_CLEARED → MonsterRenderer.ClearAll`,回读 `__SceneCharStage/Chars` 怪 tilt 归 0(生命周期正确,不残留)。

**验收(P2)对照任务包:**
- `SceneManager.MonsterCount > 0` 且节点/Renderer 证怪可见:**5 只怪 + 5 个 `model_clothe_10020103(Clone)` 蒙皮渲染体 + 截图,达成 ✓**
- 记录至少一个真实怪实例:`type=10001001`、`ins=1129`、坐标 `(5374,2672)`、资源 key `object/monster/model_clothe_10020103/model_clothe_10020103`、渲染对象名 `model_clothe_10020103(Clone)`、截图 `output/runtime_unity/_mon_v8_gameview.png`。**达成 ✓**

---

## 3. P3:技能点击仍命中可见怪(**同会话验通**)

同一 Play 会话(5 只怪可见时)`RunCommand` 驱动已学目标技能 `59100001`:

```
stage rendered monster models=5  nameplates=5
P3 MainRoleAttackTarget(59100001): CurrentTargetId=1129  exists=True  type=10001001  name="转运达摩"  pos=(5374,2672)  monsterRes=10020103  renderObj=model_clothe_10020103(Clone)
```

**`CurrentTargetId=1129` 正是 5 只可见/已渲染怪之一**(渲染体 `model_clothe_10020103(Clone)`,坐标 `(5374,2672)`,即 §2.3 的 `ins=1129`)——
即 **可见怪实例 id 与 `SceneCombat.CurrentTargetId` 一致**,P3 验收达成 ✓。

链路未退化:`MonsterRenderer` 只订阅 `SceneManager` 事件读 `vo`,**从不改 `SceneManager`/`SceneCombat` 语义**;第 7 轮"`PressSkill → MainRoleAttackTarget → FindNearestAttackableMonster → 锁定 → 范围/接近/`本地 `EVT_RELEASE_MAIN_SKILL` 释放边界"全保留(第 7 轮已对 `ins=1129` 验通接近+释放边界)。

---

## 4. P4:只记录 `20024`/`20001` blocker(本轮不发)

第 6/7 轮已采全格式串(`20024 "c" 1/2`;`20001` = `h+i×N` 怪 + `h+l×N` 人 + `ihhh` skill/x/y/angle)。`20001` 的怪列表/AOE 中心仍来自老端 **fight-movie 队列 + 碰撞收集链**,非单点直发;
本轮 Unity 已到"可见怪 + 本地释放边界",但仍缺:① 目标列表/AOE 范围收集;② 技能距离/CD/动作帧;③ 服务端伤害广播 → 怪血条扣减(`12009 MonsterHpChanged` 已接渲染层,但杀怪循环未驱动)。按任务包 P4 不猜不发,列下一轮。

---

## 5. P5:只记录不扩散

完整自动战斗 AI/挂机循环、怪物 AI/仇恨/死亡/掉落/采集、伙伴/神祇/天赋加成、技能特效/伤害飘字/战斗结算/音效、活动/副本/BOSS/组队战斗 —— 均不编码,仅记录。

---

## 6. 差异表

| 维度 | 老端运行时 | Unity 第 8 轮 | 结论 |
|---|---|---|---|
| 怪模型 | 3D 蒙皮 `model_clothe_${monster_res}.lh` | `object/monster/model_clothe_{vo.MonsterRes}` 真实蒙皮(614 三角)摆合成台,5 只全渲 | **对齐 ✓(本轮新接)** |
| 模型资源口径 | `monster_res`(≠type_id) | `vo.MonsterRes`(协议),非 hardcode | **对齐 ✓** |
| 名牌名 | `vo.name`(运行名 `转运达摩`) | `vo.Name` 优先(回退 config_mon) | **对齐 ✓(本轮修正口径)** |
| 血条 | `SetHp(vo.hp,vo.maxHp)`,采集不显 | `vo.Hp/HpLim` 填充条,`IsCollect` 不显,`12009` 实时刷 | **对齐 ✓** |
| 缩放 | `vo.icon_scale` | `config_mon["11"]`(0.9),缺表默认 | **对齐 ✓** |
| 怪进表→渲染 | SceneObj 系统 | `MonsterAdded → MonsterRenderer`(调用栈实证 12002 链路) | **对齐 ✓** |
| 命中可见怪 | 点怪/寻敌 → click_target | `MainRoleAttackTarget → CurrentTargetId=1129`=可见怪 | **对齐 ✓** |
| 血条红/绿 | `SetRedBar/SetGreenBar`(按可攻击) | 暂恒红 | 差异(按 `IsCanAttackByMainRole` 区分,下轮) |
| 朝向 | `assign_angle`/移动方向 | 合成台默认朝向(未接 config brith_rot) | 差异(下轮) |
| 移动表现 | 插值平滑 | 按 `vo` 坐标每帧贴位(无插值) | 差异(下轮) |
| 受击/死亡/攻击动作 | 完整动作集 | 仅 idle | 差异(下轮) |
| 攻击请求发送 | `20024`+`20001`(fight-movie/AOE) | 仅本地 `EVT_RELEASE_MAIN_SKILL` | 差异(P4 下轮) |
| 会话稳定性 | 持续在线 | ~60–85s 周期 remote-close + 重连不稳定重入 | 差异(环境阻塞,见 §7) |

---

## 7. 仍然阻塞项 / 真实卡点(本轮只记录)

1. **测试服会话不稳定(环境阻塞,延续第 7 轮):** 真连 `ws://223.109.142.26:10000` 约 ~60–85s 周期 remote-close;断线后 `SceneManager.Clear`+`EVT_SCENE_OBJECTS_CLEARED` 触发 `MonsterRenderer.ClearAll`(怪 tilt 归 0,行为正确),但**重连常不稳定重入**(`role.hasBase` 回 false、怪/NPC 不重新下发,本轮实测等待 ~85s 仍未重入)。本轮靠"进游戏首窗口快取"在掉线前抓全 P2+P3 证据。
2. **真实 `20001`/`20024` 发送:** 依赖 fight-movie/AOE 收集链(P4)。
3. **怪血条扣减/死亡:** `12009 MonsterHpChanged` 渲染层已接(`RefreshHp`),但杀怪循环(真实伤害广播)未驱动,无法验血条扣减→死亡移除。
4. **朝向/移动插值/受击死亡动作:** 本轮仅 idle + 每帧贴位,未接 `assign_angle` 朝向、移动插值、`behit/death/attack` 动作。
5. **血条红/绿:** 暂恒红,未按 `IsCanAttackByMainRole`(老端 `SetRedBar/SetGreenBar`)区分。
6. **老端运行态截图:** 老端停在加载页 + 第 7 轮 harness 失效 + 渲染层不打日志(§1.4),本轮用协议流+源码+同测试服 `vo.name` 取证,未重采老端画面截图。

### 下一轮建议

进入 **真实战斗收尾闭环**:① 复现 fight-movie/AOE 收集链 → 接真实 `20024`/`20001` + 服务端伤害广播 → 怪血条扣减/死亡移除(可视杀怪);
② 怪朝向(`config_mon` brith_rot/移动方向)+ 移动插值 + `behit/death` 动作;③ 血条红/绿按 `IsCanAttackByMainRole`;
④ 缓解测试服 remote-close(心跳/重连重入场景修复),让"主线寻路→打怪→结算"可一次跑完;⑤ 任务 `100020→100030` 自动推进 + 自动打怪循环。

---

## 8. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误**(含本轮新增 `MonsterRenderer.cs`/`MonsterConfigs.cs`,csproj 已由 Unity 重生成纳入);既有无关警告组不变(`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162)。
- Unity 编译:新脚本 0 报错(活 Editor 域重载后控制台 0 Error,权威编译路径)。
- 编辑期资源探针(§2.2):config/key/模型/动作全真。
- Play 态真连(§2.3/§3):5 只真实怪渲染可见 + 名牌/血条 + 技能命中可见怪 `ins=1129`,全链跑通。

---

## 9. 本轮改动清单(落 `Shenxiao.Module.Core` + `Shenxiao.Editor`,不新增 asmdef)

| 文件 | 改动 |
|---|---|
| `Module/Core/Scene/MonsterRenderer.cs`(新) | 最小真实怪渲染层(订阅 4 事件 + 真实模型/名牌/血条/idle + 清场闭环) |
| `Module/Core/Scene/MonsterConfigs.cs`(新) | `config_mon` 数字键访问器(name/monster_res/icon_scale),缺表降级 |
| `Editor/ConfigGenerator/ClientConfigSync.cs` | `SYNC_LIST_SERVER` 加 `config_mon` |

> 取证用 `AppConfig.asset` 冒烟开关与 `devAccount`(临时 `unity_npc_475823114`)已还原(`git diff` 为空);同步进来的 `config_mon.json`、`output/`、`.playwright-cli/` 均 gitignore/不入库;无临时 harness 脚本入库。
