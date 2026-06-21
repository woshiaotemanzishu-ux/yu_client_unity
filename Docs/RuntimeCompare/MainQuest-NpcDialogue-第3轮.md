# 主线任务 NPC 对话 运行态对比 · 第 3 轮

范围:主线首登任务 `100010` 的端到端链路 ——
**点击任务 → 主角走到 NPC 云霄月华(100101)→ 打开真实对话 → 接/交任务 → 30004 推进 100010→100020**。
方法:以老 Laya 客户端**运行时**为唯一真相;Unity 侧本轮**真连测试服**(非 UI-only harness),逐环节取证对比。

> 头条结论:**第 2 轮记录的卡点 `SceneManager.GetNpc(100101)=null` 是 UI-only harness 的局限,不是真实链路缺口。**
> 真连服务端后,整条 NPC 对话链路**零改码**端到端跑通,与老端逐协议一致。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮在线,HTTP 200,len 24469) |
| 老端账号 / 角色 | `90990` / `全久京`(Lv1 首登) |
| 老端协议日志 | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(完整首登链路 12005/12100/12102/30004) |
| 老端截图 / 节点树 | `output/playwright/laya8090_after_create_role.png` / `..._stage.json`、`output/_oldend_taskarea.png` |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`(可达)→ `get_server_info` 下发游戏服 `ws://223.109.142.26:10000`(`jzy_sh921_test` 测试服) |
| Unity 测试账号 / 角色(本轮新建) | `unity_npc_475823114` / `云霄42852`(career1 sex1);另 `unity_npc_64684916`(0 角色,创角页验证) |
| Unity 运行证据 | `output/runtime_unity/play_real_scene.png`、`play_real_dialogue.png`、`play_real_dialogue2.png`、`play_real_advanced.png`、`_real_chain_log.txt` |
| 采样日期 | 2026-06-21 |

> Unity 取证方式:编辑器 **Play 态**,经 Unity MCP `RunCommand` 装 fire-and-forget 编排,调**真实公开入口**真连测试服:
> `DevLoginAsync → SelectServer → ResolveEndpoint(get_server_info)→ ConnectGame(10000)→ SendCreateRole(10003)→
> EnterGameWithRole(10004)→ GAME_START → SceneEntryFlow(12005/12100)→ MainRoleFlow(主角 agent)→
> TaskModel.DoTask(null)(=任务条点击同源入口)→ DialogueController.ShowTask(12101/12102)→ FinishTask(30004)`。
> **非伪造**:NPC/坐标/对话/奖励/推进全部来自真实服务端回包。`output/`、`.playwright-cli/` gitignore。
> 已知约束:`RunCommand` 编译会触发域重载,断掉 WebSocket → 一次完整真连链路必须在**单条命令**内跑完;
> 不能跨命令保持长连。

---

## 1. 老端运行时真相(首登任务 100010 链路)

老端 console(账号 90990 首登)逐协议:

```
send 12005 iicchh 0 10000 0 0 0 0           // 请求进场景 10000
recv 12005  //handler12005// 0 10000 0 0 0 4339 1669 0 1   // 进场景 10000, 主角 spawn=(4339,1669)
send 12100 i 10000                          // 请求场景 NPC 列表
recv 12100  npc---12100----: 10000 34        // 场景 10000 返回 34 个 NPC(含 100101 云霄月华)
send 12002 / recv 12002                      // 场景快照
…(点任务条 100010 → 走到 NPC → 开对话)…
On12102: 100101 100010 101                   // npc=100101 task=100010 talk=101
…(完成)…
recv 30004                                   // 提交任务
recv 12005 … 0 10200 10000 … → On12102: 100134 100020 290   // 推进:下一主线 100020, NPC=100134
```

`config_task[100010]`(数字键):`1=云霄仙域`、`4=1`(主线)、`14=100020`(下一任务)、`18=100101`(NPC)、
`20=101`(对话)、`23=[{5,0,150000},{3,0,20000}]`(奖励 经验150000 + 九洲灵钱20000)、`29=1`(主线序号)。
`config_npc[100101]`:`name=云霄月华`、`title=觉梦仙子`、`scene=0`、`x=0`、`y=0`、`icon=100101`(模型)、`talk=100101`、`brith_rot=270`。
**关键:NPC 坐标不在 config(x=0/y=0),由服务端 12100 按场景下发**——这是第 2 轮 UI-only harness 拿不到 NPC 的根因。

老端可见链路:任务条 `[主] 云霄仙域 / 与<云霄月华>交谈 (0/1)` → 点击 → 主角走到 NPC 身边停下转身 →
`DialogueController.ShowTask` 发 `12101`(NPC 关联任务)→ `12102`(talk_id 101)→ 对话弹层(NPC 名/立绘/任务文案/奖励)→
点动作发 `30003/30004/30007` → 服务端回 `30000/30001` 刷新任务条到 `100020`。

---

## 2. 老端源码逻辑锚点(`yu_client`)

- `scene/SceneController.ts`:`On12100`(scene_id + npc_count + 每 NPC `{npc_id,is_show,scene_id,x,y,args}`)→ `SetNpcList` → `AddNpcVo` → `CreateNpc`。**NPC 位置 100% 来自 12100,config_npc 只给名/模型/朝向/对话 id。**
- `scene/Scene.ts:MainRoleToNpc`:取 NPC 逻辑坐标 → dist≤2.5 逻辑格则立即动作,否则走近 → 停下、主角与 NPC 互相转身 → `Fire(SHOW_TASK, npc.instance_id)`。
- `commonModel/TaskModel.ts:DoTask`:Talk/StartTalk/EndTalk → 找 NPC(同场景 `MainRoleToNpc`,跨场景 `USE_FLY_SHOE` 飞鞋)。
- `commonController/DialogueController.ts`:`ShowTask → 12101 → ShowNpcTalk →(单任务单行)SelectTask → 12102 → 展示对话 → 30003/30004/30007`。

---

## 3. Unity 真连服务端运行证据(本轮 3 次跑批)

### RUN1 — 全新账号创角进游戏端到端(`unity_npc_475823114` / `云霄42852`)

| 环节 | 证据 | 与老端对齐 |
|---|---|---|
| HTTP 登录 | `player_login success servers=2`(GM `223.109.142.26:88`) | ✓ |
| 选服/入口 | `get_server_info → host=223.109.142.26 port=10000` | ✓ |
| 连接+10000 | `connected=True`,`★ 10000 回包 角色数=0 → 创角页` | ✓ |
| 创角进游戏 | `SendCreateRole 云霄42852 career1 sex1` → 自动 10004 进游戏 | ✓ |
| 进场景 | `scene entered: sceneId=10000 rolePos=(4339,1669)` | **== 老端 spawn(4339,1669) ✓** |
| 场景 NPC | `request 12100 sceneId=10000` → `NpcCount=34` | **== 老端 "10000 34" ✓** |
| NPC 100101 | `npc render: id=100101 pos=(4678,1574) name="云霄月华" title="觉梦仙子" brithRot=270` | **坐标真值 + brithRot==config 270 ✓** |
| 主角 agent | `agent=True`(MainRoleFlow 在 map ready 后建 MainRoleAgent at rolePos) | ✓ |
| 主线任务 | `mainTask=100010 mainTaskNpc=100101` | ✓ |
| 点任务走 NPC | `[Task] DoTask 找 NPC: NPC 100101 在场景 pos=(4678,1574),主角走过去,到达后开对话(12101)` | **== MainRoleToNpc ✓** |
| 对话协议 | `send 12101 → recv 12101 tasks=1 → send 12102 taskId=100010 state=3 → recv 12102 talkId=101` | **== 老端 On12102:100101 100010 101 ✓** |
| 对话奖励 | `12102 任务 100010 奖励 2 项: 经验 ×150000 / 九洲灵钱 ×20000` | **== config_task[100010].23 ✓** |
| 对话立绘+视图 | `对话立绘 model_clothe_100101` + `对话视图打开 npcId=100101 talkId=101` | ✓(`play_real_dialogue.png` 云霄月华真对话) |

`SUMMARY: scene=10000 npcCount=34 npc100101=True agent=True mainTask=100010 dlgOpen=True dlgNpc=100101`

场景 10000 真实 NPC 名册(12100 节选,均带真实坐标/称号):
`100101 云霄月华(4678,1574)`、`100102 玄清真人(4432,2816)`、`100103 逸尘(5004,3330)`、`100104 沈青岚(6762,2990)`、
`100105 沈若兮(6711,2598)`、`100106 采薇(7750,1909)`、`100107 沈长风(7967,1371)`、`100108 吟风(8141,939)`、`100109 沈天衡(9297,947)` …(共 34)。

### RUN3 — 复用角色,干净会话验 30004 推进

| 环节 | 证据 |
|---|---|
| 复用进游戏 | `roleCount=1` → `EnterGameWithRole` → `ready scene=10000 npc100101=True agent=True taskBefore=100010` |
| 点任务开对话 | `DoTask(null)` → `dialogue open=True npc=100101` |
| 交任务 30004 | `完成任务 → [Dialogue] send 30004 finish task=100010` |
| 推进 | `SUMMARY3 advanced=True taskBefore=100010 taskAfter=100020 afterNpc=100134` |

**== 老端 "30000{100020} → On12102:100134 100020 290":推进到 100020,下一 NPC=100134 ✓**
(截图 `play_real_dialogue2.png` / `play_real_advanced.png`)

---

## 4. 差异表

| 维度 | 老端运行时 | Unity 第2轮(UI-only harness) | Unity 第3轮(真连服务端) | 结论 |
|---|---|---|---|---|
| 进场景 12005 | scene 10000 spawn(4339,1669) | 无(无连接) | scene=10000 rolePos=(4339,1669) | **对齐 ✓(真连)** |
| 场景 NPC 12100 | 10000 → 34 NPC | **无(GetNpc=null blocker)** | NpcCount=34 真实回包 | **blocker 解除 ✓** |
| NPC 100101 在场景 | 云霄月华(server 坐标) | **null** | id=100101 (4678,1574) name=云霄月华 brithRot270 | **对齐 ✓(真坐标)** |
| 主角可移动到 NPC | MainRoleToNpc | 无主角 agent | agent=True,MoveToNpc 走到 (4678,1574) | **对齐 ✓** |
| 到达后 ShowTask | Fire(SHOW_TASK)→12101 | 直接发 12101(无走近) | 走到后发 12101 | **对齐 ✓** |
| 12101/12102 回包 | On12102:100101 100010 101 | 无回包(发出即卡) | recv 12101 tasks=1 / recv 12102 talkId=101 | **对齐 ✓** |
| 对话弹层内容 | NPC名/立绘/文案/奖励 | 回放数据(harness 边界) | 真回包:云霄月华+talk101+经验150000/九洲灵钱20000 | **对齐 ✓(真数据)** |
| 30004 推进 | 100010→100020(NPC100134) | 仅打开完成弹层(无连接) | send 30004 → advance 100010→100020 afterNpc=100134 | **对齐 ✓(端到端)** |

---

## 5. 本轮结论

- **核心结论:NPC 对话主线链路在真连服务端下零改码端到端跑通。** 第 2 轮的 `GetNpc(100101)=null` 仅是 UI-only harness(无连接、无 12100)的局限;真实链路(`SceneEntryFlow→12005→12100→ParseNpc→AddNpc→NpcRenderer`、`MainRoleFlow→MainRoleAgent`、`DoTask→DoFindNpcTask→MoveToNpc→ShowTask→12101/12102→DialogueView`、`FinishTask→30004→30000/30001 推进`)此前已经完整且正确,只是从未真连过。
- **本轮无业务代码改动**(仅用 RunCommand 编排驱动真连取证,不改仓库代码)。按编码规范"端到端验证一个真实样本再铺开",样本已验通 → 进入下一模块,而非为已通链路补防护代码。

### 记录的真实 blocker(非主线核心,本轮只记录不扩散)

1. **部分场景 NPC 模型未转换/未入库**:`id=100109` 等 → `[Res] load failed key=object/npc/model_clothe_100109` → `[Scene] npc model 缺失(blocker):NPC 数据已到但形象不可见`。处置:`神霄/资源` 转该批 NPC 模型(对标地图瓦片离线转换)。**主线 NPC 100101 云霄月华模型正常,不受影响。**
2. **30004 回包(28B)未挂处理器**:`[Net] 未注册协议 proto=30004`。任务推进已由服务端 `30000/30001` 推送完成,但 30004 回包内容(完成确认/效果)暂未解析 → 后续可挂处理器补完成态特效/确认。
3. **驱动登录残留(非真实 bug)**:本轮用 `RunCommand` 直调 `LoginController` 绕过 `LoginFlow` UI,登录面板未随成功退下,故 `play_real_scene.png` 中部有登录面板残影。正常 UI 流程登录成功会关闭登录层。

---

## 6. 确认问题清单(仅有证据)

1. 第 2 轮 blocker 性质修正:`GetNpc(100101)=null` 是 harness 无连接所致,**非链路缺口**;真连下 `NpcCount=34、GetNpc(100101)=(4678,1574)`。证据:RUN1 SUMMARY。
2. NPC 坐标来源确认:config_npc[100101] `x=0/y=0`,真实坐标 `(4678,1574)` 来自 server 12100;Unity `NpcVo` 同源解析正确。
3. 端到端推进确认:真 30004 → 任务 `100010→100020`、NPC `100101→100134`,与老端一致。证据:RUN3 SUMMARY3。
4. NPC 模型缺失 blocker(id=100109 等):有 `[Res] load failed` + `[Scene] npc model 缺失` 双日志为证;主线 100101 不在缺失之列。
5. 30004 回包未挂处理器:有 `[Net] 未注册协议 proto=30004 payload=28B` 为证;推进经 30000/30001 完成不受影响。

---

## 7. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误 / 6 既有无关警告**(`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162),与第 2 轮同一组,无新增。
- `rg` 锚点(`DoTask/MoveToNpc/ShowTask/12101/12102/30004/SceneManager.GetNpc/NpcRenderer`)在 `Assets\Scripts` 与 `yu_client/h5/src` 均命中,链路两端齐备。
- Unity 真连 Play 取证:RUN1 端到端(login→创角→进场景10000→34NPC→NPC100101→走近→12101/12102→对话)、RUN3(30004→100010→100020)均跑通,截图/日志见 §0。

---

## 8. 后续轮建议

按本任务包"若 NPC 对话端到端完成 → 进入 `MainUISkillView`":NPC 对话主线端到端**已完成**,建议下一轮转 `MainUISkillView`(技能 4 槽 `shortcutList` 13007/21002 + 自动战斗按钮态 + 伙伴技能锁)。

并行可收口的小项(非阻塞主线):
1. **场景 NPC 模型批量转换**(`神霄/资源`):补 `model_clothe_100109` 等缺失 NPC 形象,让场景 34 NPC 全可见。
2. **30004 回包处理器**:挂 On30004 解析完成确认/奖励效果(对标老端 `TaskController.On30004`)。
3. **登录层退场**:正常 `LoginFlow` 登录成功关闭登录层(本轮残留仅因绕过 LoginFlow 直驱,记录备查)。
4. **跨场景飞鞋 `USE_FLY_SHOE`**:主线推进到 100020(NPC 100134 可能在他场景)后需要;当前同场景链路已通,跨场景留待出现真实跨场景任务时补。
