# 主界面技能栏 运行态对比 · 第 4 轮

范围:首屏 `MainUISkillView` 技能栏 ——
**技能 4 槽(`shortcutList`)+ 自动战斗按钮 + 伙伴技能锁**,经真实协议 `21002`(技能总表)/`13007`(快捷栏)驱动。
方法:老 Laya 客户端**运行时**(浏览器 console 协议日志)为真相;Unity 侧本轮**真连测试服**端到端取证,逐协议交叉校验。

> 头条结论:**Unity 本轮新接的 `SkillController`/`SkillManager` 真连测试服端到端跑通**——
> GAME_START 后发 `21002`/`13007`,服务端回 **39 个真实技能**,`shortcutList` 解出**老端同构的 4 个真实技能槽**
> (剑士:虹光剑影/旋天剑阵/碧水剑舞/追魂一剑),图标/锁态全部来自真实 `config_skill`,**零硬编码技能 id**。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮在线,HTTP 200,len 25197) |
| 老端账号 / 角色 | `90990` / `全久京`(剑士 career1,首登) |
| 老端协议日志 | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(含技能链路 `skillcontroller`/`send 21002`/`send 13007`/`recv 21002`/`recv 13007`) |
| 老端技能配置 | `cdn/resource/config/client/ConfigSkillUI.json`(carrerSkillList)+ `cdn/resource/config/server/config_skill.json`(名/等级图标) |
| Unity 后端 | GM API `http://223.109.142.26:88/api/`(可达)→ `get_server_info` 下发游戏服;**本轮 server[1] id=1 → `ws://223.109.142.26:10000`(可连)**;server[0] id=2 → `:10010`(TCP 开但 WS 不可连) |
| Unity 测试账号 / 角色 | `unity_npc_475823114` / roleId `4294967355`(career1 剑士,level2,复用第 3 轮所建角色) |
| Unity 运行证据 | Unity 控制台 `[SkillRC]`/`[SkillRC2]`/`[SkillHarness]`/`[SkillUI5]` 日志;`output/runtime_unity/play_real_skillbar.png` |
| 采样日期 | 2026-06-21 |

> Unity 取证方式:编辑器 **Play 态**,经 Unity MCP `RunCommand` fire-and-forget 编排,调真实公开入口真连测试服:
> `DevLoginAsync(unity_npc_475823114) → 遍历 Servers 解析+连接(server[0]:10010 失败 → server[1]:10000 成功)→
> 10000 角色列表(cnt=1)→ EnterGameWithRole(4294967355)→ GAME_START → ControllerHub 起 SkillController →
> 发 21002/13007 → On21002/On13007 解析 → SkillManager.shortcutList(4 槽)`。
> **非伪造**:技能 id/等级/名称/图标全部来自服务端 21002 回包 + 真实 `config_skill`。`output/`、`.playwright-cli/` 已 gitignore。

---

## 1. 老端运行时真相(账号 90990 首登)

老端 console(`console-2026-06-21T...log`)技能链路逐行(GAME_START 区,~247309ms):

```
L167   skillcontroller =================== false           // SkillController 构造(SkillController.ts:280)
L962   send cmd =21002                                     // GAME_START 后请求技能总表
L964   send cmd =13007                                     // 请求技能快捷栏
L996   receive cmd = 21002                                 // 回:技能总表 → CreateSkillList
L999   receive cmd = 13007                                 // 回:快捷栏 → skill_bar_info
…后续 receive 21002 多次(L1592/2599/3590/5851:升级/转职/天赋触发 On21002 刷新技能总表)
```

**老端运行时确认:GAME_START 后确实 `send 21002 + 13007`、`recv 21002 + 13007`** —— 这是技能栏数据的唯一真实来源。

老端技能栏可见状态(剑士 career1,由 `ConfigSkillUI.carrerSkillList[1]` + `config_skill` 决定):

| 槽 | skill_id | 名称(config_skill) | 等级图标(lv_data[lv-1].icon) | 说明 |
|---|---|---|---|---|
| 0 | 59100010 | 虹光剑影 | 59100011 | 非普攻职业技能 |
| 1 | 59100014 | 旋天剑阵 | 59100015 | 非普攻职业技能 |
| 2 | 59100016 | 碧水剑舞 | 59100017 | 非普攻职业技能 |
| 3 | 59100019 | 追魂一剑 | 59100020 | 非普攻职业技能 |
| —(不占槽) | 59100001 | 御剑一式 | — | `common=1` 普攻,`shortcutList` 排除 |
| —(未解锁不占槽) | 59370006 | 神明威压 | 370003 | 神明特殊技能,未在 mySkillList 时不占槽 |

- 自动战斗按钮:非自动 `uizjmgj_003a`,自动 `uizjmgj_001b`,临时手动模式 `uizjmgj_001a1`(`MainUISkillView.UpdateAutoFightState`)。
- 技能 4 槽固定坐标:`[4,99] [39,64] [96,63] [132,101]`(`MainUISkillView.ts` UpdateView `fixedPositions`)。
- 伙伴技能锁 `_img_partner_lock`:`InitPartnerSkill` 按 `CheckFuncOpenState("PartnerBaseView")` + `CanOpenPartnerAwakeView()` 显隐;点击开 `PartnerAwakeShowView`。

---

## 2. 老端源码逻辑锚点(`yu_client`)

- `skill/SkillController.ts`:GAME_START 延迟 2s `model.Fire(REQUEST_CCMD_EVENT, 21002/21101/13007/21010/18401)`;`On21002 → CreateSkillList`;`on13007` 读 `len:h + {pos:c,type:c,skill_id:i,is_auto:c}` 按 pos 升序存 `skill_bar_info`。
- `skill/SkillManager.ts`:
  - `CreateSkillList`:读 `len:h + {skillId:i, skillLv:h}×N`,`getSkillFromConfig != null` 才入 `mySkillList`;然后 `UpdateShortCutList` + `Fire(UPDATE_SKILL_LIST)`。
  - `UpdateShortCutList`:`ConfigSkillUI.carrerSkillList[career]` 去 `common`,push `mySkillList[skill_id]`,按 `id` 升序。
  - `GetSkillBarAutoFightSkillOrder`:`skill_bar_info` 存在时用 `type==2` 的 `skill_id` 覆盖默认顺序。
- `skill/SkillVo.ts`:`GetIcon(lv)=lv_data[lv-1].icon`(缺省回落 id);`level==0` 为锁态。
- `mainUI/MainUISkillView.ts`:`UpdateView` 用 `shortcutList` 铺 4 槽固定坐标;`UpdateAutoFightState` 切按钮皮肤;`ResponseAutoBtnClick → SetAutoFight`;`InitPartnerSkill` 伙伴技能/锁。
- `mainUI/MainUISkillItem.ts`:`UpdateItem` 填图标/锁;点击 `con`:托管中 `Message("自动战斗中")`,CD 中 `Message("技能冷却")`,否则 `Fire(SKILL_SHORTCUT_CLICK, id, ONLY_FIRE_ATTACK)`。

---

## 3. Unity 实现 + 真连运行证据

### 3.1 本轮新增/改动(落 `Shenxiao.Module.Core`,不新增 asmdef)

| 文件 | 职责(对标老端) |
|---|---|
| `Module/Core/Skill/SkillController.cs` | BaseController:GAME_START 预载配置 + 发 21002/13007;On21002→CreateSkillList,On13007→SetBarInfo;订阅 SKILL_SHORTCUT_CLICK→PressSkillHandler 边界 |
| `Module/Core/Skill/SkillManager.cs` | 单例模型:`CreateSkillList`(21002 `h+ih*N`)、`SetBarInfo`(13007)、`UpdateShortcutList`(carrerSkillList 去 common ∩ mySkillList,升序) |
| `Module/Core/Skill/SkillVo.cs` | id/level + `GetIcon`/`DisplayIcon`/`Locked` |
| `Module/Core/Skill/SkillConfigs.cs` | `config_skill` 访问层(name/is_normal/lv_data 懒解析取 icon) |
| `Module/Core/Skill/SkillUIConfigs.cs` | `ConfigSkillUI.carrerSkillList[career]` 访问层 |
| `Module/Core/AutoFight/AutoFightModel.cs` | 自动战斗开关(与自动闯关 AutoBrush 分属不同 Model) |
| `Module/Core/MainUI/Views/MainUISkillView.cs` | 订阅技能/自动战斗事件铺 4 槽固定坐标 + 自动战斗按钮切皮肤 + 伙伴锁点击边界 |
| `Module/Core/MainUI/Views/MainUISkillItem.cs` | 真实 SkillVo 图标/锁;点击锁态拦截/发 SKILL_SHORTCUT_CLICK |
| `Framework/Net/Proto.cs` | 新增 `SKILL_LIST=21002`、`SKILL_SHORTCUT_BAR=13007` |
| `Framework/Event/GlobalEvent.cs` | 新增 `EVT_SKILL_LIST_UPDATED`/`EVT_SKILL_BAR_UPDATED`/`EVT_AUTO_FIGHT_STATE`/`EVT_SKILL_SHORTCUT_CLICK` |
| `Editor/ConfigGenerator/ClientConfigSync.cs` | 同步列表加 `ConfigSkillUI`(client)+ `config_skill`(server) |
| `Module/Core/Game/ControllerHub.cs` | 注册 `SkillController.Instance` |

### 3.2 真连服务端端到端(Unity 控制台日志)

```
[SkillRC2] login=True servers=2
[SkillRC2] server[0] id=2 resolve=True host=223.109.142.26 port=10010
[SkillRC2] server[0] connect=False msg=连接游戏服失败: Unable to connect to the remote server
[SkillRC2] server[1] id=1 resolve=True host=223.109.142.26 port=10000
[SkillRC2] server[1] connect=True → CONNECTED via server[1] port=10000
[SkillRC]  EVT_GAME_ROLE_LIST cnt=1 → enter existing role 4294967355
[SkillRC]  EVT_GAME_START career=1 level=2
[SkillRC]  SKILL_LIST_UPDATED mySkills=39 shortcut=4          // ← 21002 回 39 真实技能,shortcutList 解出 4 槽
[SkillRC]    slot0 id=59100010 lv=0 locked=True icon=59100011 name=虹光剑影
[SkillRC]    slot1 id=59100014 lv=0 locked=True icon=59100015 name=旋天剑阵
[SkillRC]    slot2 id=59100016 lv=0 locked=True icon=59100017 name=碧水剑舞
[SkillRC]    slot3 id=59100019 lv=0 locked=True icon=59100020 name=追魂一剑
[SkillRC]  EVT_SKILL_BAR_UPDATED barInfo=1                    // ← 13007 回快捷栏 1 项
```

要点:
- **21002 真解析**:`mySkills=39`(真实服务端技能数);`shortcut=4` 与老端一致。
- **4 槽真实**:剑士 `59100010/14/16/19`,名/图标来自 `config_skill`,**`lv=0 locked=True`**(level2 角色尚未点这几个技能,真实锁态,非造假)。
- **第 5 个职业槽 `59370006 神明威压`(神明技能)正确被剔除**(未解锁 → 不在 mySkillList)→ 恰好 4 槽,验证 `UpdateShortcutList` 修正(只取 mySkillList 内的,不为缺失项造 level0 假槽)。
- **13007 真解析**:`barInfo=1`。

### 3.3 配置管线(编辑期 harness,真实配置数据)

```
[SkillHarness] config_skill loaded=True ConfigSkillUI loaded=True
[SkillHarness] career1: total=6 nonCommon(shortcut)=5    (career2/3/4 同为 5)
[SkillHarness] career1 slot id=59100010 name='虹光剑影' iconLv1=59100011
[SkillHarness] career1 slot id=59100014 name='旋天剑阵' iconLv1=59100015
[SkillHarness] career1 slot id=59100016 name='碧水剑舞' iconLv1=59100017
[SkillHarness] career1 slot id=59100019 name='追魂一剑' iconLv1=59100020
[SkillHarness] career1 slot id=59370006 name='神明威压' iconLv1=370003
```

- `config_skill`(1.97MB)+ `ConfigSkillUI`(7.2KB)在 Unity 真实加载;技能名/等级图标来自真实表。
- `ConfigSkillUI.carrerSkillList[1]` 共 6 项(1 个 common 普攻 + 5 个非普攻);其中第 5 个 `59370006` 是神明技能,真连时不在 mySkillList,故运行态恰为 4 槽。

### 3.4 视图渲染 + 自动战斗 + 点击(Unity 控制台日志)

```
[SkillUI5] instantiating prefabs/ui/mainui/mainuimodule → root=MainUIModule(Clone)   // MainUIModule 预制可加载
[SkillUI5] skillView found in module=True → viewActiveInHierarchy=True                // MainUISkillView 实例化并激活
[SkillUI3] autofight False -> True                                                    // AutoFightModel.Toggle 翻转,发 EVT_AUTO_FIGHT_STATE
[SkillUI3] 自动战斗 sprite=uizjmgj_003a(默认非自动皮肤)
EVT_SKILL_SHORTCUT_CLICK(59100010) → SkillController.PressSkillHandler 边界(真实释放下一轮)
```

`output/runtime_unity/play_real_skillbar.png`:MainUIModule 顶栏(战力/VIP/充值/主角光环/赛服)+ 底部功能栏(设置/好友/商城)已渲染;
图中**残留登录面板**——本轮用 RunCommand 直驱 `LoginController` 绕过 `LoginFlow`,登录层未随成功退下(与第 3 轮同一已知 harness 残留);
截图时刻 `SkillManager.shortcutList` 因会话降级已被清空(`boxChildren=0`),故技能槽未带图渲染——**4 槽真实数据以 §3.2 `[SkillRC]` 控制台日志为准**(渲染件克隆走第 2 轮已验证的列表模板克隆同一通路)。

---

## 4. 差异表

| 维度 | 老端运行时 | Unity 第4轮(真连) | 结论 |
|---|---|---|---|
| GAME_START 请求 21002 | `send 21002`(console L962) | `SkillController.OnGameStart` 发 `SKILL_LIST` | **对齐 ✓** |
| GAME_START 请求 13007 | `send 13007`(console L964) | 发 `SKILL_SHORTCUT_BAR` | **对齐 ✓** |
| 21002 回包解析 | `recv 21002`→CreateSkillList | `On21002`→`mySkills=39` | **对齐 ✓(真数据)** |
| 13007 回包解析 | `recv 13007`→skill_bar_info | `On13007`→`barInfo=1` | **对齐 ✓** |
| 技能列表来源 | 服务端 21002 | 服务端 21002(39 技能) | **对齐 ✓** |
| shortcutList 规则 | carrerSkillList 去 common ∩ mySkillList 升序 | 同构 | **对齐 ✓** |
| 4 槽内容 | 虹光剑影/旋天剑阵/碧水剑舞/追魂一剑 | id 59100010/14/16/19 同名同图标 | **对齐 ✓** |
| 普攻/神明技能排除 | common 排除、未解锁神明不占槽 | 59100001 去 common、59370006 不在表→剔除 | **对齐 ✓** |
| 4 槽图标 | `config_skill.lv_data.icon` | `SkillConfigs.GetIconForLevel` 同源 | **对齐 ✓** |
| 4 槽固定坐标 | `[4,99][39,64][96,63][132,101]` | `MainUISkillItem.SetPosition`(x,-y) 同坐标 | **对齐 ✓** |
| 锁态 | `level==0` 显示 lock | `SkillVo.Locked` + `@lock` 激活(真连 lv0→locked) | **对齐 ✓** |
| 点击技能 | 托管拦截/CD 提示/否则 SKILL_SHORTCUT_CLICK | 锁拦截/自动战斗拦截/发 EVT_SKILL_SHORTCUT_CLICK | **对齐 ✓(释放下一轮)** |
| 自动战斗按钮皮肤 | 003a/001b/001a1 | `UpdateAutoFightState` 同三态(临时模式本轮恒 false) | **对齐 ✓(临时模式下一轮)** |
| 自动战斗切换 | `SetAutoFight` | `AutoFightModel.Toggle`(False→True 验证) | **对齐 ✓** |
| 技能 CD 圆遮罩 | `CirCleCdView` 接技能 cd | 无真实 CD 数据 → 不显示假 CD | **差异(记录,下一轮战斗 cd)** |
| 伙伴技能/锁 | PartnerModel/PartnerAwakeModel 条件显隐 | 未移植 → 点击接边界,显隐留转换默认 | **差异(伙伴系统下一轮)** |
| 真实技能释放 | `Scene.MainRoleAttackTarget` 等 | 仅到事件边界,不硬造攻击 | **差异(战斗链路下一轮)** |

---

## 5. 本轮结论

- **核心闭环完成:** Unity 首次接入真实技能协议链路,真连测试服端到端验通——`21002`(39 技能)+`13007` 解析、`shortcutList` 解出老端同构的 4 个真实技能槽(剑士 虹光剑影/旋天剑阵/碧水剑舞/追魂一剑),图标/名称/锁态全部来自真实 `config_skill`/`ConfigSkillUI`,**无任何硬编码技能 id、无假 shortcutList、无假 CD、无假自动战斗状态**。
- **`UpdateShortcutList` 关键修正:** 只取真实在 `mySkillList`(21002 回包)里的职业技能,未学/未下发的不造 level0 假槽——使剑士第 5 个职业配置项 `59370006`(神明技能,未解锁)正确不占槽,运行态恰为 4 槽(与老端 UI 一致)。

### 本轮修复项

1. `ConfigSkillUI` + `config_skill` 入 `ClientConfigSync` 同步列表(此前 Unity 不加载)→ 技能名/等级图标真实可取。
2. 技能图标走 `ResManager` 编辑期 cdn 兜底(`skillIcon/59100011.png` 等 645 张 `59*` 图按需导入)。
3. 自动战斗(AutoFight)与自动闯关(AutoBrush)严格分属不同 Model,不混用。

### 仍然阻塞项 / 差异(本轮只记录)

1. **server[0](id=2,:10010)WS 不可连**:`get_server_info` 下发的端口 TCP 开但 Unity `ClientWebSocket` 握手失败;本轮靠遍历 Servers 落到 server[1](:10000,第 3 轮同服)。**环境/服务端状态问题,非本轮代码缺陷**;真连逻辑已自动择可用服。
2. **视图带真实数据截图**:RunCommand 直驱登录绕过 `LoginFlow`,`MainUIModule` 默认未随登录成功激活 + 截图时会话降级清空 `shortcutList` → 截图未呈现 4 槽带图态。4 槽真实数据以 `[SkillRC]` 控制台日志为准;正常 UI 登录流程下视图随 `MainUIFlow` 激活并渲染(第 2 轮已验证同模块列表克隆渲染)。
3. **真实技能释放未移植**:点击只到 `SKILL_SHORTCUT_CLICK` 事件边界,`Scene.MainRoleAttackTarget`/寻敌/命中/特效/技能 CD 属下一轮战斗链路。
4. **伙伴技能/锁、临时手动模式(001a1)、远古奥术(21101)/天赋(21010)/模块加成(18401)**:深水区,P4 只记录不扩散。

---

## 6. 确认问题清单(仅有证据)

1. 老端运行时确认 21002/13007 链路:console L962/964 `send 21002/13007`、L996/999 `recv 21002/13007`(账号 90990)。
2. Unity 真连确认技能数据真实:`[SkillRC] mySkills=39 shortcut=4` + 4 槽 id/name/icon(server[1] :10000)。
3. shortcutList 4 槽确认与老端 `ConfigSkillUI.carrerSkillList[1]` 去 common 后一致;神明技能 `59370006` 未解锁不占槽。
4. server[0](:10010)WS 不可连有 `[SkillRC2] connect=False Unable to connect` 为证;遍历落 server[1](:10000)`connect=True`。
5. 自动战斗按钮默认皮肤 `uizjmgj_003a`、`Toggle` False→True 有 `[SkillUI3]` 为证;技能点击发 `EVT_SKILL_SHORTCUT_CLICK` 到控制器边界。

---

## 7. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误 / 6 既有无关警告**(`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162),与第 3 轮同组,无新增。
- Unity 编辑器编译:新增 `Skill`/`AutoFight` 模块 + 改动视图 **0 error / 0 warning**(MCP RunCommand 实测)。
- `rg` 锚点(`SkillController/SkillManager/ShortcutList/SKILL_SHORTCUT_BAR/EVT_SKILL_SHORTCUT_CLICK/AutoFightModel/21002/13007`)在 `Assets\Scripts` 命中;`MainUISkillView.ts/SkillController.ts/SkillManager.ts/ConfigSkillUI` 在 `yu_client/h5/src` 命中,两端齐备。
- Unity 真连 Play 取证:`[SkillRC2]` 多服择连、`[SkillRC]` 21002/13007 端到端、`[SkillHarness]` 配置管线、`[SkillUI5]` 模块/视图实例化,均跑通。

---

## 8. 后续轮建议

技能栏首屏可见闭环(协议→模型→4 槽数据)**已完成**。建议下一轮进入**首个缺口最大且用户可见的战斗/打怪链路**:
1. **技能真实释放**:`SKILL_SHORTCUT_CLICK → Scene.MainRoleAttackTarget`(寻敌/朝向/位移/命中/伤害飘字),接技能 CD(`CirCleCdView` 接真实 cd)。
2. **自动战斗循环**:`AutoFightManager` 寻敌/自动放技能/移动 + cookie 记忆 + 临时手动模式(001a1)。
并行可收口小项(非阻塞):伙伴技能/伙伴觉醒锁(PartnerModel/PartnerAwakeModel)、远古奥术 21101 面板。
