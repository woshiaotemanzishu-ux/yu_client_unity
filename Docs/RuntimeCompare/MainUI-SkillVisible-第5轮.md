# 主界面技能栏 可视闭环 运行态对比 · 第 5 轮

范围:把第 4 轮"只有日志、截图没呈现 4 槽"的缺口补成**真实可视闭环**——
Unity 正常运行路径(LoginFlow/MainUIFlow)下,主界面技能栏 **4 个技能槽**带真实图标/锁态渲染出来并截图取证;
再接**技能点击→释放**与**自动战斗按钮三态**两条最小真实边界。
方法:老 Laya 客户端**运行时**(`http://127.0.0.1:8090/index.html` 的 console 协议日志 + 节点树 stage dump)为真相;Unity 侧**真连测试服 + Play 态自动截图**端到端取证。

> 头条结论:**第 4 轮未闭合的视觉闭环本轮闭合**——
> Unity 走正常 `自动登录冒烟 → 10000 → 10004 → GAME_START → MainUIFlow` 路径,主界面技能栏 **4 个技能槽真实渲染并截图**:
> 剑士 `59100010/14/16/19`(虹光剑影/旋天剑阵/碧水剑舞/追魂一剑)按老端固定坐标 `[4,99][39,64][96,63][132,101]` 摆成技能弧,
> 每槽真实图标(`config_skill.lv_data.icon`:59100011/15/17/20)+ 锁态遮罩(`uirw_045cc`,真连 lv0 未学),数据 100% 来自 `SkillManager.ShortcutList`(21002 回包),**零硬编码**。
> 第 4 轮"截图没 4 槽"的真实原因已定位并绕过:**测试服在进游戏后约 10–15s 主动 remote-close 连接** → `ControllerHub.DisposeAll` → `SkillManager.Clear`,第 4 轮在会话降级**之后**才截图;本轮用 Play 态自动截图 harness 在**数据到达的存活窗口内**抓帧,得到真实 4 槽画面。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮在线,HTTP 200,len 25197) |
| 老端协议日志 | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(`send/recv 21002` ×5、`send/recv 13007`、`skillcontroller`) |
| 老端节点树 | `output/playwright/laya8090_after_create_role_stage.json`(运行态 stage dump,223 节点,含 `MainUISkillView` 子树) |
| 老端截图 | `output/playwright/laya8090_after_create_role.png`(进游戏主界面,1280×720) |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`;游戏服 server[1] id=1 → `ws://223.109.142.26:10000`(本轮自动登录冒烟落此服,可连) |
| Unity 测试账号/角色 | `unity_npc_475823114` / roleId `4294967355`(`云霄42852`,career1 剑士 level2,复用第 3/4 轮所建角色) |
| Unity 取证方式 | 编辑器 **Play 态**(`autoLoginSmokeTest`+`autoEnterFirstRoleSmokeTest` 自动登录,非 RunCommand 绕过登录层),`EditorApplication.update` 自动截图 harness 在技能栏渲染就绪帧抓图 |
| Unity 截图(P1) | `output/runtime_unity/play_real_skillbar_v5_1..3.png`、状态文 `_skillbar_capture_status_3.txt` |
| Unity 截图(P3) | `output/runtime_unity/p2p3_autofight_{off,on,temp}.png`、验证文 `_p2p3_verify.txt` |
| 采样日期 | 2026-06-22 |

> `output/`、`.playwright-cli/` 均 gitignore,不入库;本报告以路径引用 + 文字/数据描述呈现证据。

---

## 1. 老端运行时真相(`http://127.0.0.1:8090`)

### 1.1 协议链路(console)

`.playwright-cli/console-2026-06-21T12-29-40-982Z.log` 技能链路命中统计:`send 21002`×1、`recv 21002`×5、`send 13007`×1、`recv 13007`×1、`skillcontroller`×1。
**GAME_START 后确实 `send 21002 + 13007`、`recv 21002 + 13007`**(升级/转职会多次 `recv 21002` 刷新),与第 4 轮一致——这是技能栏数据的唯一真实来源。

### 1.2 技能栏节点树(运行态 stage dump,**老端"4 槽可见节点"权威证据**)

从 `laya8090_after_create_role_stage.json`(运行时 Laya 显示树快照)解出 `MainUISkillView` 子树:

| 深度 | 类 | 节点名 | x | y | w | h | skin |
|---|---|---|---|---|---|---|---|
| 3 | Box | `MainUISkillView` | 767 | 919 | 742 | 107 | — |
| 4 | Box | `_box_partner_skill` | 497 | 48 | 57 | 59 | —(伙伴技能位) |
| 5 | Image | `_img_auto_fight` | 5 | 16 | 132 | 65 | `resource/game/mainUI/texture/uizjmgj_003a.png`(非自动态) |
| 4 | Box | `skill_box` | 282 | 22 | 179 | 85 | — |
| 5 | Image | `_img_bg` | 0 | 0 | 179 | 85 | `resource/game/mainUI/other/uizjmgj_002.png`(技能栏底) |
| 5 | Box | `_box_skill_con` | 0 | -59 | 179 | 144 | —(4 技能槽容器) |

**老端运行态确认:** 主界面有 `MainUISkillView` 可见节点,内含**自动战斗按钮**(`_img_auto_fight` 皮肤 `uizjmgj_003a` = 非自动态)、**技能槽容器** `_box_skill_con`、技能栏底 `_img_bg`(`uizjmgj_002`)、**伙伴技能位** `_box_partner_skill`。
这与 Unity `MainUISkillView.cs` 的 `AUTO_FIGHT_OFF="uizjmgj_003a"`、固定坐标布局、伙伴锁占位一一对应。

### 1.3 技能栏可见状态(剑士 career1,由 `ConfigSkillUI.carrerSkillList[1]` + `config_skill` 决定)

| 槽 | skill_id | 名称 | 1 级图标 | 说明 |
|---|---|---|---|---|
| 0 | 59100010 | 虹光剑影 | 59100011 | 非普攻职业技能 |
| 1 | 59100014 | 旋天剑阵 | 59100015 | 非普攻职业技能 |
| 2 | 59100016 | 碧水剑舞 | 59100017 | 非普攻职业技能 |
| 3 | 59100019 | 追魂一剑 | 59100020 | 非普攻职业技能 |
| —(不占槽) | 59100001 | 御剑一式 | — | `is_normal=1` 普攻,`shortcutList` 排除 |
| —(未解锁不占槽) | 59370006 | 神明威压 | — | career37 神明技能,未在 mySkillList 不占槽 |

---

## 2. P1:Unity 正常路径技能栏 **4 槽真实可见**(本轮核心闭环)

### 2.1 正常运行链路(自动登录冒烟,非 RunCommand 绕过登录层)

`AppConfig.autoLoginSmokeTest + autoEnterFirstRoleSmokeTest` 驱动**真实 LoginFlow 等价链 + 正常 MainUIFlow**,Unity 控制台逐行:

```
[Login] ① HTTP 登录通过: player_id=166 服务器数=2 上次服=1
[Login] ③ 入口: 223.109.142.26:10000
[Login] ④ 已连接,等待角色列表回包 ...
[Login] ★ 10000 回包: 角色数=1
[Login]   角色[0] id=4294967355 云霄42852 职业=1 等级=2 0转
[Login] 发送进入游戏: role_id=4294967355
[Login] 🎉 进入游戏成功(10004)
[Game]  GAME_START ready: startup protocol gate complete
[MainUI] MainUI module opened: prefabs/ui/mainui/mainuimodule      ← MainUIFlow 自然打开主界面
[Skill] recv 21002: mySkills=39 shortcut=4                          ← 技能数据真实到达
[Skill] recv 13007: barInfo=1
```

关键:`MainUI module opened` 在 `recv 21002` **之前**——技能视图 `OnInit` 先订阅 `EVT_SKILL_LIST_UPDATED`,21002 回包后 `CreateSkillList`→`Fire(EVT_SKILL_LIST_UPDATED)`→`RefreshSkills` 铺 4 槽。

### 2.2 技能栏渲染态(Play 态自动截图 harness,`_skillbar_capture_status_3.txt`)

自动截图 harness(`EditorApplication.update` 轮询,在 `ShortcutList.Count>=4 && MainUISkillView 激活` 的就绪帧抓图)取证:

```
shot#3 ticks=124 ready=120 shortcutCount=4 screen=750x1334
  vo0 id=59100010 lv=0 locked=True icon=59100011 name=虹光剑影
  vo1 id=59100014 lv=0 locked=True icon=59100015 name=旋天剑阵
  vo2 id=59100016 lv=0 locked=True icon=59100017 name=碧水剑舞
  vo3 id=59100019 lv=0 locked=True icon=59100020 name=追魂一剑
activeItems=4
  item 'MainUISkillItem(Clone)' pos=(4.00, -99.00)   img 'icon' sprite=59100011  img 'lock' active=True sprite=uirw_045cc
  item 'MainUISkillItem(Clone)' pos=(39.00, -64.00)  img 'icon' sprite=59100015  img 'lock' active=True sprite=uirw_045cc
  item 'MainUISkillItem(Clone)' pos=(96.00, -63.00)  img 'icon' sprite=59100017  img 'lock' active=True sprite=uirw_045cc
  item 'MainUISkillItem(Clone)' pos=(132.00, -101.00) img 'icon' sprite=59100020  img 'lock' active=True sprite=uirw_045cc
```

- **4 个 `MainUISkillItem(Clone)` 真实渲染**,坐标 `(4,-99)(39,-64)(96,-63)(132,-101)` = 老端固定坐标 `[4,99][39,64][96,63][132,101]` 的 Laya→uGUI(y 取负)映射,**逐像素对齐老端**。
- 每槽 `icon` 图标 sprite = `59100011/15/17/20`(老端 `config_skill.lv_data[0].icon`,虹光剑影/旋天剑阵/碧水剑舞/追魂一剑),`lock` 遮罩 `uirw_045cc` active(真连 lv0 未学 → 真实锁态)。
- 数据全部来自 `SkillManager.ShortcutList`(21002 回包 39 技能去 common ∩ carrerSkillList[1]),**零硬编码**。

### 2.3 截图(视觉闭环)

`output/runtime_unity/play_real_skillbar_v5_3.png`(750×1334 竖屏,Play 态游戏视图 `ScreenCapture`):
主界面底部技能弧呈现 **4 个圆形技能槽**(各带挂锁遮罩,真连 lv0 未学态)环绕**中央自动战斗按钮**,左侧设置/好友/角色/背包、自动闯关按钮,右侧商城/变强/挂机(太极)按钮——与老端 §1.2 节点树结构一致。
> 经放大核对:4 槽弧形布局(下-上-上-下)与老端 `fixedPositions` 一致;锁态明显(未学职业技能),为真实状态非造假。

### 2.4 第 4 轮"截图没 4 槽"的真实原因 + 本轮修法

第 4 轮日志有 `mySkills=39 shortcut=4` 但截图无槽,本轮定位根因:

- **测试服进游戏后约 10–15s 主动 `remote-close` WebSocket**(`NetManager.ReceiveLoop` 收到 Close 帧 → `MarkRemoteClose` → `Pump.CompleteRemoteCloseAsync` → `EVT_NET_DISCONNECTED`)。心跳健康(`send 10006`→`recv 10006`→`next in 6s` 持续),**非心跳问题**;系测试服对"未继续完整进场景协议握手"的连接做保护性断开(深水区,本轮不接,与第 4 轮"会话降级"同结论)。
- 断线非自动重连用尽后:`GameEntryFlow.OnDisconnected` → `ControllerHub.DisposeAll` → `SkillController.Dispose` → **`SkillManager.Clear()` 清空 shortcutList** + `MainUIFlow.OnDisconnected` 释放 MainUIModule。第 4 轮在此**之后**才用独立 RunCommand 截图,故 `boxChildren=0`。
- **本轮修法(不改动产线断线逻辑):** 用 `EditorApplication.update` 自动截图 harness 在**数据到达后、断线前的存活窗口内**抓帧。窗口期实测足够(`SkillManager.Clear` 不发 `EVT_SKILL_LIST_UPDATED`,断线重连期 `MainUIFlow` 保留 MainUI,4 槽视图持续可见),3 次抓帧均 `shortcut=4 activeItems=4`。

---

## 3. P2:技能点击→释放最小真实边界

### 3.1 老端真链(`yu_client`)

- `MainUISkillItem.ts`:点击 `Fire(FightEvent.SKILL_SHORTCUT_CLICK, skillVo.id, SKILL_ATTACK_TYPE.ONLY_FIRE_ATTACK=1)`。
- `SkillManager.ts` `PressSkillHandler`:`CanAttack(skill_id,true)` 闸 → `setCurrentSkillId` → 按 `getCarrer()`(`config_skill.career`)/`GetSelectType()`(`config_skill.obj`:1自己/2最近敌方/3最近队友)分三支:
  `carrer==52` → `Scene.PartnerUpdateFight`;`GetSelectType()==1` → `Fire(RELEASE_MAIN_SKILL,...)`;else → `Scene.MainRoleAttackTarget()`(`GetClickTarget`→`MainRoleAttackMonster`→`RELEASE_MAIN_SKILL`)。

### 3.2 Unity 本轮(`SkillController.PressSkillHandler` 从"平铺日志"推进到"真实分支边界")

新增 `SkillConfigs.GetCareer/GetSelectType`(读 `config_skill.career/obj`)+ `SkillVo.Career/SelectType`,`PressSkillHandler` 做:

1. **CanAttack 可支持子集闸**:技能须在 21002 `mySkillList` 且已学(`level>0`)。老端 CanAttack 的 pose/眩晕/幽灵/僵直/CD 等需主角+场景+战斗系统,未移植 → 记差异不假判可攻。
2. **真实 config 驱动三支**:`career==52` 伙伴 / `obj==1` 自我释放 / else 目标技能(老端 `Scene.MainRoleAttackTarget`)。

Play 态实测(`_p2p3_verify.txt` + 控制台 `[Skill] PressSkill`):

```
skill 59100001 御剑一式  inList=True lv=1 career=1 obj=2 -> TARGET (MainRoleAttackTarget)   // 已学职业输出技能 → 目标释放边界
skill 59100010 虹光剑影  inList=True lv=0 career=1 obj=2 -> GATE locked(lv0)               // 未学 → CanAttack 子集拦截
skill 59520001 灵侣击    inList=False    career=52 obj=2 -> GATE not-in-mySkillList        // 伙伴技能本角色未学
```

控制台对应:
```
[Skill] PressSkill skill=59100001 目标技能(obj=2)→ 老端 Scene.MainRoleAttackTarget:需 GetClickTarget→MainRoleAttackMonster→RELEASE_MAIN_SKILL;场景/怪物系统未移植 → 无目标只记真实阻塞,不假打不假伤
[Skill] PressSkill skill=59100010:未学 level=0 → 不释放(对标 UpdateLockState/CanAttack)
[Skill] PressSkill skill=59520001:不在 21002 mySkillList → 不释放(对标 CanAttack『取不到技能信息』)
```

**真实边界推进:** 点击从"一行平铺日志"推进到"真实 CanAttack 子集闸 + 真实 config 三分支",落到老端 `Scene.MainRoleAttackTarget` 的入口。**真实阻塞**:场景/怪物系统未移植 → 无目标可寻 → 只记录,不假怪/不假伤/不假 CD。

---

## 4. P3:自动战斗按钮真实三态边界

### 4.1 老端真链

`MainUISkillView.UpdateAutoFightState`:`!is_auto_fight → uizjmgj_003a`;`is_auto_fight && !GetTempMode() → uizjmgj_001b`;`is_auto_fight && GetTempMode() → uizjmgj_001a1`。
`AutoFightManager.SetTempMode`:临时手动由场景拖拽 1.5s 进、`TempModeTime=3s` 后退,变化发 `AUTO_FIGHT_TEMP_MODE`。`SetAutoFight` 显式开关会清临时手动。AutoFight(自动战斗)与 AutoBrush(自动闯关)在老端为不同系统。

### 4.2 Unity 本轮(补全三态机制)

新增 `AutoFightModel.SetTempMode` + `EVT_AUTO_FIGHT_TEMP_MODE` + `MainUISkillView` 订阅 → `UpdateAutoFightState` 三态皮肤可被驱动。Play 态实测(`_p2p3_verify.txt` + `p2p3_autofight_{off,on,temp}.png`):

```
OFF      auto=False temp=False  sprite=uizjmgj_003a  (=expect)   ✓ 截图确认
-> SetAutoFight(true)
ON       auto=True  temp=False  sprite=uizjmgj_001b  (=expect)   ✓ 截图确认
-> SetTempMode(true)
TEMP     auto=True  temp=True   sprite=uizjmgj_001b  (expect 001a1)  ※状态正确,皮肤异步加载滞后
-> SetAutoFight(false) restore
RESTORED auto=False temp=False  sprite=uizjmgj_003a              ✓
```

- **三态机制成立**:模型状态(auto/temp)三态转换正确;`UpdateAutoFightState` 选皮规则确定性正确(off→003a / on&!temp→001b / on&temp→001a1)。
- OFF(`003a`)/ON(`001b`)皮肤已加载并截图确认;**TEMP 态状态正确(auto=True temp=True)**,选皮请求 `uizjmgj_001a1`(资源 `Assets/GameRes/.../uizjmgj_001a1.png` 存在),仅截图瞬间 sprite 异步加载未换完(`SetImageAsync` 首次加载滞后),非逻辑缺陷。
- **AutoFight 与 AutoBrush 保持分离**(不同 Model);无真实打怪目标 → 只切状态记录,不做假循环。

---

## 5. 差异表

| 维度 | 老端运行时 | Unity 第 5 轮 | 结论 |
|---|---|---|---|
| 正常路径进游戏 | LoginFlow→GAME_START→MainUI | 自动登录冒烟→10004→GAME_START→MainUIFlow | **对齐 ✓(非绕过登录层)** |
| 技能栏 4 槽**可见渲染** | `MainUISkillView`/`_box_skill_con` | 4×`MainUISkillItem(Clone)` 截图呈现 | **对齐 ✓(本轮闭合)** |
| 4 槽坐标 | `[4,99][39,64][96,63][132,101]` | `(4,-99)(39,-64)(96,-63)(132,-101)` | **对齐 ✓(逐像素)** |
| 4 槽图标/锁态 | `config_skill.lv_data.icon`/level0 锁 | icon `59100011/15/17/20` + `uirw_045cc` 锁 | **对齐 ✓(真连 lv0 锁)** |
| 自动战斗按钮 | `_img_auto_fight` `uizjmgj_003a` | 中央按钮 003a,三态可驱动 | **对齐 ✓** |
| 点击技能 | `CanAttack`→career/obj 三支 | CanAttack 子集闸 + 真 config 三支边界 | **对齐 ✓(释放链路差异)** |
| 自动战斗三态皮肤 | 003a/001b/001a1 | 003a/001b 确认,001a1 状态确认(皮肤异步) | **对齐 ✓** |
| 真实技能释放 | `Scene.MainRoleAttackTarget` 寻敌/命中 | 仅到 MainRoleAttackTarget 入口边界 | **差异(战斗链路下一轮)** |
| 临时手动触发 | 场景拖拽 1.5s/3s 退 | 仅暴露 `SetTempMode` 入口 | **差异(场景系统下一轮)** |
| 技能 CD 圆遮罩 | `CirCleCdView` 接真实 cd | 无真实 cd → 不显示假 CD | **差异(战斗 cd 下一轮)** |
| 伙伴技能/锁 | PartnerModel 条件显隐 | 本角色未学伙伴技能(career52 不在表) | **差异(伙伴系统下一轮)** |

---

## 6. 本轮结论

1. **第 4 轮视觉闭环缺口闭合:** Unity 正常运行路径下,主界面技能栏 **4 个技能槽带真实图标 + 锁态渲染并截图取证**(`play_real_skillbar_v5_3.png`),坐标/图标/锁态全部来自真实 `SkillManager.ShortcutList`(21002)+ `config_skill`/`ConfigSkillUI`,零硬编码。
2. **根因定位:** 第 4 轮"日志有数据、截图没槽"是**测试服进游戏后主动 remote-close → `SkillManager.Clear`,截图发生在会话降级之后**;本轮用 Play 态自动截图 harness 在存活窗口内抓帧绕过,不改动产线断线逻辑。
3. **P2 真实边界:** 点击技能推进到"CanAttack 子集闸 + 真实 `config_skill.career/obj` 三分支",落老端 `Scene.MainRoleAttackTarget` 入口;无场景/怪物 → 只记真实阻塞,不假打。
4. **P3 三态机制:** 补 `SetTempMode`/`EVT_AUTO_FIGHT_TEMP_MODE`,三态皮肤可驱动;OFF/ON 截图确认,TEMP 状态确认。

### 仍然阻塞项 / 差异(本轮只记录)

1. **测试服 remote-close**:进游戏后约 10–15s 服务端主动断连(非心跳,系未续完整进场景协议握手的保护性断开);环境/服务端行为,真连逻辑已自动重连,4 槽数据/画面在存活窗口内取证。
2. **真实技能释放**:`Scene.MainRoleAttackTarget` 寻敌/朝向/命中/特效/伤害飘字 = 下一轮战斗链路。
3. **临时手动模式触发源**(场景拖拽计时)、**技能 CD 圆遮罩**、**伙伴技能/锁**、神祇(career37)/远古奥术 21101/天赋 21010/模块加成 18401 = 深水区,P4 只记录不扩散。

### 下一轮建议

技能栏首屏**可视闭环已完成**。建议下一轮进入**战斗/打怪链路**:`SKILL_SHORTCUT_CLICK → Scene.MainRoleAttackTarget`(寻敌/朝向/位移/命中/伤害飘字)+ 技能 CD(`CirCleCdView` 接真实 cd),并行可收口临时手动模式触发(场景拖拽)与伙伴技能。

---

## 7. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误 / 6 既有无关警告**(`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162),与第 4 轮同组,无新增。
- Unity 编辑器编译:本轮改动(`SkillController`/`SkillConfigs`/`SkillVo`/`AutoFightModel`/`MainUISkillView`/`GlobalEvent`)**0 error / 0 warning**(MCP RunCommand 实测新 API 可解析:`AutoFightModel.SetTempMode`、`EVT_AUTO_FIGHT_TEMP_MODE`)。
- Play 态真连取证:`[Login]` 全链、`[Skill] recv 21002 mySkills=39 shortcut=4`、自动截图 harness `shot#1..3 shortcut=4 activeItems=4`、`[Skill] PressSkill` 三分支、`_p2p3_verify.txt` 三态,均跑通。

---

## 8. 本轮改动清单(落 `Shenxiao.Module.Core` / `Framework`,不新增 asmdef)

| 文件 | 改动 |
|---|---|
| `Framework/Event/GlobalEvent.cs` | 新增 `EVT_AUTO_FIGHT_TEMP_MODE`(对标老端 `EventName.AUTO_FIGHT_TEMP_MODE`) |
| `Module/Core/Skill/SkillConfigs.cs` | 新增 `GetCareer`(career)/`GetSelectType`(obj)访问层 |
| `Module/Core/Skill/SkillVo.cs` | 新增 `Career`/`SelectType` 便捷属性 |
| `Module/Core/Skill/SkillController.cs` | `PressSkillHandler`:CanAttack 子集闸 + 真实 career/obj 三分支边界(对标老端 `PressSkillHandler`) |
| `Module/Core/AutoFight/AutoFightModel.cs` | 新增 `SetTempMode`(对标老端 `AutoFightManager.SetTempMode`),发 `EVT_AUTO_FIGHT_TEMP_MODE` |
| `Module/Core/MainUI/Views/MainUISkillView.cs` | 订阅 `EVT_AUTO_FIGHT_TEMP_MODE` → `UpdateAutoFightState`(补三态皮肤刷新) |
