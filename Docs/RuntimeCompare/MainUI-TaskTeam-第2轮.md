# MainUI 任务/队伍区域 运行态对比 · 第 2 轮

范围：`MainUITaskTeamView`(左侧 任务/队伍 tab + 任务条 + 主线任务槽 + 神殿觉醒槽 + 队伍空态)+ 任务点击链路
(`TaskModel.DoTask` → 找 NPC 对话 / 完成弹层)。
方法：以老 Laya 客户端**运行时**为唯一真相(运行页截图 + `Laya.stage` 节点树 + 浏览器 console 协议日志),
逐节点对老 Unity 预制体/运行时,列差异,定本轮修/后续记录。

> 画幅提醒(同第 1 轮):老端运行证据是**横屏 Web**(stage 逻辑 `2276×1280`,截图 `1280×720`,各视图 `centerX=0` 居中),
> 游戏本体竖屏(Unity 设计分辨率 `720×1280`)。故**绝对 x/y 不可直接对**,只对**相对布局 / 显隐 / 任务条文本 / 任务数量 /
> 点击结果 / tab**。坐标仅作锚点参考。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html`(本轮采样时 8090 在线,HTTP 200) |
| 账号 / 角色 | `90990` / `全久京`(Lv1 首登) |
| 老端截图 | `output/playwright/laya8090_after_create_role.png`(1280×720) |
| 老端任务区裁剪 | `output/_oldend_taskarea.png`(左侧任务区放大) |
| 老端节点树 | `output/playwright/laya8090_after_create_role_stage.json`(扁平节点数组,深度截到 d=5) |
| 老端协议日志 | `.playwright-cli/console-2026-06-21T12-29-40-982Z.log`(完整首登链路:30000 / 12101 / 12102 / 30004) |
| Unity 运行证据 | `output/runtime_unity/play_taskteam_before.png`(修前)/ `play_taskteam_after.png`(修后)/ `play_taskfinish.png`(完成弹层) |
| Unity 节点 dump | `output/runtime_unity/_dump_mainline.txt`(修前)/ `_dump_mainline_after.txt`(修后) |
| Unity 采样日志 | `output/runtime_unity/_task_status_after.txt` / `_finish_status.txt` |
| 采样日期 | 2026-06-21 |

> `output/`、`.playwright-cli/` 均 gitignore,不提交。Unity 截图走编辑器 **Play 态** MCP `RunCommand` harness(见 §3.1)。

---

## 1. 老端运行时真相(任务/队伍区域)

### 1.1 `MainUITaskTeamView` 节点树(层 `Main`,root `x=10 y=586 w=255 h=314`,屏幕左侧)

源码 `LoadSuccess`:`display_obj.left=10; bottom=380`,`SwitchView(Task)` 为初始态。stage 抓到(d≤5):

| 节点 | x | y | w | h | 说明 |
|---|---|---|---|---|---|
| `_box_con` | 0 | 0 | 224 | 314 | 容器(child=6) |
| `_box_task_tab` | 0 | 35 | 35 | 143 | **任务** 竖 tab(选中态亮) |
| `_box_team_tab` | 2 | 171 | 35 | 135 | **组队** 竖 tab |
| `_box_temple_awaken` | — | — | — | — | 神殿觉醒槽(首登隐藏) |
| `_box_task` | 41 | 98 | 210 | 214 | 任务区(child=2 → `_box_main_line` + `_panel_task`) |
| `_img_arrow` / `_img_arrow_icon` | 0 | 0 | 32 | 34 | 收起箭头(`mainui_btn_task_back` + `UIrwl_002`) |

`_box_task` 两子(深度被截,从源码补):
- `_box_main_line`:主线引导任务槽。`SetTaskShow` 中 `need_arrow = MainLineTaskNeedShowArrow()` 为真时
  `_box_main_line.visible=true`,`SetMainLineTask` 用 **`MainUITaskMainLineItem`** 渲主线任务(带引导箭头/特效),
  **同时把主线任务从 `_panel_task` 列表里排除**。
- `_panel_task`:其余任务(支线/日常/结义…)的滚动列表,每条 `MainUITaskItem`。

队伍侧:`_box_team`(有队 → `_list_team` 列 `TeamMainRoleItem`)/ `_box_non_team`(无队 → `_img_create_team`「创建队伍」+ `_img_search_team`「查找队伍」)。

### 1.2 老端首登任务(运行时唯一真相)

console 日志(账号 90990 完整首登链路):

```
30000 has_receive_task_list = {100010: Array(1)}   // 首个任务
On12102: 100101 100010 101                          // npc=100101 task=100010 talk=101
… 推进 …
30000 {100020} → On12102: 100134 100020 290
30000 {100040} → On12102: 100102 100040 102
30000 {100050} → On12102: 100103 100050 103
… → {100041}
```

`config_task[100010]`(`Assets/GameRes/resource/config/server/config_task.json`,数字键):

| key | 值 | 含义 |
|---|---|---|
| 1 | `云霄仙域` | 任务名 |
| 2 | `欢迎来到云霄仙域。` | 描述 |
| 3 | `与NPC对话` | tips 模板 |
| 4 | `1` | 类型 = 主线(MAIN_LINE) |
| 14 | `100020` | 下一任务 |
| 18 | `100101` | NPC id |
| 20 | `101` | 对话 id |
| 23 | `[{5,0,150000},{3,0,20000}]` | 奖励:经验 150000 + 九洲灵钱 20000 |
| 29 | `1` | 主线序号 main_line_order |

`config_npc[100101].name = 云霄月华`(运行时确认),与 stage NameBoard「云霄月华」/ 场景模型 `6_100101` 一致。

**首登任务条(`_box_main_line`,见 `output/_oldend_taskarea.png`)**:
- 标题:`[主] 云霄仙域`(主线橙色 `#ff9015`)+ 主线序号 `1`。
- 任务文案:`与<云霄月华(绿)>交谈`(**客户端按 `GetTaskTipsMsgByMainUITaskItem` 现拼:`与`+`config_npc.name`+`交谈`,不是直接用 config tips 的「与NPC对话」**)+ 未完成计数 `(0/1)`。
- 装饰:引导手指(`ui_dianjizhiyin`/`shouzh`)+ 选中黄框 + 右上绿 ✓。
- `_panel_task` 此刻为空(只有主线任务,已进 `_box_main_line`)。

### 1.3 老端点击链路(运行时)

`MainUITaskItem.OnClick` → `TaskModel.DoTask`:找 NPC 对话类 → `Scene.MainRoleToNpc`(走到 NPC 身边停下转身)→
`DialogueController.ShowTask` → 发 `12101`(NPC 关联任务)→ `12102`(任务对话 `talk_id`)→ 展示对话 →
点动作节点发 `30003`(接)/`30004`(交)/`30007`(对话事件)→ 服务端回 `30001/30000` 刷新任务栏推进到下一条。

---

## 2. 老端源码逻辑锚点(`yu_client`)

- `mainUI/MainUITaskTeamView.ts`
  - `LoadSuccess`:`left=10/bottom=380`、`SwitchView(Task)`、tab 颜色(亮 `#FFF7D6` / 暗 `#6CFFD3`)。
  - `SetTaskShow`:`need_arrow = MainLineTaskNeedShowArrow()`;主线任务**排除出面板**,改进 `_box_main_line`;
    `task_list.length==1 && !need_arrow` 时移除并强制 arrow。
  - `SetMainLineTask`:`_box_main_line` 用 `MainUITaskMainLineItem` 渲主线任务 + `ShowFinger`。
  - `SetTempleAwaken`:神殿觉醒槽显隐/进度(依赖 `TempleAwakenModel`,无数据/锁定时隐藏)。
- `mainUI/MainUITaskItem.ts`:`SetTitle`(`[tag] name` + 主线序号)、`SetTipsMsg`(调 `GetTaskTipsMsgByMainUITaskItem` + 计数后缀)、`OnClick`→`DoTask`。
- `commonModel/TaskModel.ts`
  - `GetTaskTipsMsgByMainUITaskItem`(ts:2530):**任务文案客户端按类型现拼**——找 NPC 对话=`与`+`config_npc.name(绿)`+`交谈`;击杀=`击杀`+`config_mon.name`;采集/收集/通关副本同理;default 回退 `task_tips_msg`。
  - `DoTask`:找 NPC / 完成提交(30004)/ 寻路 等分支。
- `commonController/DialogueController.ts`:`ShowTask`→12101/12102→30003/30004/30007。

---

## 3. Unity 当前实现 + 运行证据

### 3.1 截图采样方式(编辑器 Play 态 RT 渲染)

Unity MCP `RunCommand` 装 fire-and-forget harness:**进 Play 态**(纯编辑期 Addressables 异步不泵,配表/prefab 不解析,
故必须 Play 态)→ 载真实配表(`config_task`/`config_goods`/`config_npc`/`config_talk` 均 `IsLoaded=True`)→
`RoleModel` 播种到首登采样基线(Lv1)→ **回放老端观测到的真实 30000 状态**:`{100010: 找NPC对话→npc100101}`
(经 `TaskModel.SetTaskLists` + `TaskVo.ApplyConfig(100010)`,**非伪造**,是复现 console 日志里服务端实发的首登任务)→
建 `720×1280 ScreenSpaceCamera` 画布(独立 UI 层 + 仅渲该层 + 关其余 Canvas 隔离)→ 实例化 `MainUIModule` 并 Show
全部 HUD(73 视图)→ 相机渲到 `RenderTexture` 存 PNG。点击链路:直接调与点击同源的 `TaskModel.DoTask` 并记录路由分支。

### 3.2 prefab / Bind / 业务 View

- 预制体 `Assets/Prefabs/UI/MainUI/MainUIModule.prefab`;`MainUITaskTeamViewBind` 字段与老端节点一一对应
  (`_box_task_tab/_box_team_tab/_box_task/_box_main_line/_panel_task/_box_temple_awaken/_box_non_team/…`
  + 模板 `_tpl_MainUITaskItem`/`_tpl_TeamMainRoleItem`)。
- `MainUITaskTeamView.cs`:tab 初始态对齐 `SwitchView(Task)`;`RefreshTaskItems` 读真实 `TaskModel.GetTaskListForMainUI()` 铺面板。
- `MainUITaskItem.cs`:`SetData` → `SetTitle`(`[tag] name` + 主线序号)+ `SetTips`(`BuildMainUITips`)+ 完成/选中态;`OnClick`→`DoTask`。
- 数据链:`TaskController` 解析真实 `30000/30001/30005` → `TaskVo + ApplyConfig` → `TaskModel`;`DoTask` 三分支
  (找 NPC→`MainRoleAgent.MoveToNpc`→`DialogueController.ShowTask`12101 / 完成→`TaskFinishView`→30004 / 场景坐标→blocker)。

### 3.3 运行证据要点

**修前(`play_taskteam_before.png` + `_dump_mainline.txt`/`_task_status.txt`):**
- `MainLineTaskVo=100010 needShowArrow=True`;`GetTaskListForMainUI(panel) count=0`(主线 needArrow→排除面板)。
- **`_box_main_line` 激活但空**(无渲染件)——主线任务**不显示**。
- **`_box_temple_awaken` → `_box_open_awaken` 显示烘焙占位**`剑魄同修提升` / `10/35`(`SetTempleAwaken` 未移植,
  prefab 设计期占位透出)——即第 1 轮 `play_bottom.png` 左侧那条「10/35」的真正来源,是**假占位**不是真任务。
- 净效果:首登任务区**既丢真任务、又显假占位**。

**修后(`play_taskteam_after.png` + `_dump_mainline_after.txt`/`_task_status_after.txt`):**
- `config_npc 100101 name='云霄月华'`;`BuildMainUITips` = `与<color=#00fa64>云霄月华</color>交谈 (0/1)`。
- `_box_main_line` 内 `MainUITaskItem(Clone)`:`lblTaskTitle='[主] 云霄仙域'`、`lblTaskTitle2='1'`(主线序号)、
  desc=`与云霄月华交谈 (0/1)`、`_img_done`/`_img_select` INACTIVE。**与老端任务条内容一致**。
- `_box_temple_awaken active=False`——假占位消除。

**点击链路(`_task_status_after.txt` + `play_taskfinish.png`):**
- 找 NPC 分支:`DoTask(100010)` → `tipsType=6 IsFindNpc=True npcId=100101` → `DoFindNpcTask` → `DialogueController.ShowTask`(发 12101)。
  UI-only harness 无场景 NPC(`SceneManager.GetNpc(100101)=null`)→ 精确 blocker:NPC 不在场景 / 跨场景飞鞋未移植。
- 完成分支:`DoTask(完成态任务)` → `DoFinishTask` → `TaskFinishView` 打开,渲真实奖励——
  `经验 ×150000` / `九洲灵钱 ×20000`(`config_task[100010].23` 真实奖励),复用 `BaseAwardItem`(品质底板 + EXP/币图标 +
  老端 `FormatNumber2` 缩写 `15W`/`2W`),按钮「领取奖励」(点 → `TaskController.SubmitFinish` 发 `30004`)。

---

## 4. 差异表

| 维度 | 老端运行时 | Unity 修前 | Unity 修后 | 结论 |
|---|---|---|---|---|
| 视图位置 | 左侧 root(left10/bottom380) | 左侧(prefab 锚点) | 同 | **对齐 ✓** |
| 任务/队伍 tab | 任务亮 + 队伍暗,初始选任务 | 任务亮/队伍暗 ✓ | 同 | **对齐 ✓** |
| 首登任务数据来源 | 真实 `30000`{100010} + config | 回放真实 30000 + config(harness) | 同 | **对齐 ✓(真数据)** |
| 任务标题 | `[主] 云霄仙域` + 序号 1 | (主线不渲染) | `[主] 云霄仙域` + 序号 1 | **对齐 ✓(本轮修)** |
| 任务文案 | `与<云霄月华(绿)>交谈 (0/1)`(客户端拼 config_npc 名) | `与NPC对话 (0/1)`(用 config tips) | `与<云霄月华(绿)>交谈 (0/1)` | **对齐 ✓(本轮修)** |
| 主线任务槽 `_box_main_line` | `MainUITaskMainLineItem` 渲主线任务 | **激活但空** | 复用 `MainUITaskItem` 渲真实主线任务 | **修(P1):空槽→真任务** |
| 神殿觉醒槽 `_box_temple_awaken` | `SetTempleAwaken` 显隐(首登隐藏) | **显假占位 `剑魄同修提升 10/35`** | 隐藏(不显假占位) | **修(P1):去假占位** |
| 面板任务数 | 0(只有主线,已进主线槽) | 0 | 0 | **对齐 ✓** |
| 点击=找NPC对话 | `DoTask`→走NPC→`ShowTask`12101→12102 | 同代码路径(已实现) | 同 + 取证路由 | **对齐 ✓;场景NPC缺=blocker** |
| 点击=完成 | `DoTask`→完成弹层→30004 | `TaskFinishView`(已实现) | 同 + 取证真实奖励渲染 | **对齐 ✓(P2 弹层真奖励)** |
| 引导手指 / 主线箭头 / 特效 | `StoryModel` finger + arrow + `ui_renwulan` | 无 | 无 | **只记录**(StoryModel 未移植) |
| 主线条专属底板视觉 | `MainUITaskMainLineItem`(华丽绿底+✓) | — | 复用普通 `MainUITaskItem` 底板 | **只记录**(无 MainUITaskMainLineItem 转换件) |
| 选中态 `_img_select` | 点任务高亮 | 已实现(`EVT_TASK_SELECT_CHANGED`) | 同 | **对齐 ✓** |
| 队伍 tab / 空态 | `_box_non_team` 创建/查找队伍 | prefab 在,数据未接 | 同 | **只记录**(队伍系统后续轮) |
| 队伍红点 / NPC名牌 | 多源 | 先隐 | 同 | **只记录** |

---

## 5. 本轮决定

**修(P1/P2,3 处真 bug,均由运行态对比暴露):**

1. **主线任务空槽 → 渲真实主线任务**(`MainUITaskTeamView.cs` + `TaskModel.GetMainLineEntry`)。
   主线引导任务被 `GetTaskListForMainUI` 从面板排除(对标老端),但 Unity 此前 `_box_main_line` 只 `SetActive(true)` 不填内容 →
   首登任务区空。本轮:`MainLineTaskNeedShowArrow()` 为真时,复用 `_tpl_MainUITaskItem` 把真实主线任务渲进 `_box_main_line`
   (`TaskModel.GetMainLineEntry()` 提供主线任务 + tips 列表)。引导箭头/手指/华丽主线底板仍 record-only。
2. **任务文案接 config_npc 现拼**(`TaskModel.BuildMainUITips`/`BuildTipBody` + `TaskController` 预载 `config_npc`)。
   对标老端 `GetTaskTipsMsgByMainUITaskItem`:找 NPC 对话类文案=`与`+`config_npc.name(绿 #00fa64)`+`交谈`,
   不再直接用 config tips 的「与NPC对话」。击杀/采集/收集/通关副本类暂回退服务端 tipsMsg(后续轮按 config_mon/config_goods 补)。
3. **神殿觉醒假占位 → 隐藏**(`MainUITaskTeamView.OnInit`)。
   `SetTempleAwaken` 未移植(依赖 `TempleAwakenModel`),prefab `_box_open_awaken` 烘焙了占位 `剑魄同修提升 10/35`,
   首登(觉醒未解锁)老端是隐藏的 → 本轮整槽隐藏,不展示假占位。完整 `SetTempleAwaken` 移植留后续轮。

**验(P2,已实现,本轮补运行证据):**
- 点任务 → `TaskModel.DoTask` 三分支:找 NPC(→`MainRoleAgent.MoveToNpc`→`DialogueController.ShowTask`12101/12102)、
  完成(→`TaskFinishView` 真奖励→30004)、场景坐标(blocker)。截图证完成弹层真实奖励渲染。

**只记录(后续轮):**
- 引导手指 / 主线箭头 / `ui_renwulan` 特效(`StoryModel` 未移植)。
- `MainUITaskMainLineItem` 专属华丽底板(无 Unity 转换件,暂复用 `MainUITaskItem`)。
- `SetTempleAwaken` 完整移植(`TempleAwakenModel` 真显隐/进度/觉醒入口)。
- 击杀/采集/收集/通关副本类任务文案的 config_mon/config_goods 现拼。
- 队伍系统(创建/查找/成员列表 `TeamMainRoleItem`、队伍红点)。
- 任务条完成态绿框特效、`cacheAs` 优化。

---

## 6. 确认问题清单(仅有证据的)

1. **`_box_main_line` 此前空槽**:`_dump_mainline.txt` 显 `_box_main_line` 无子;`_task_status.txt` 显 `panel count=0`。已修。
2. **`_box_temple_awaken` 显假占位**:`_dump_mainline.txt` 显 `_box_open_awaken` 激活带 `_lb_open_awaken_task_desc='剑魄同修提升'`、`_lb_open_awaken_progress='10/35'`(prefab 烘焙占位,非真数据)。已修(隐藏)。
3. **任务文案未接 NPC 名**:修前 `tip='与NPC对话 (0/1)'`,老端运行时为 `与云霄月华交谈`。已修为 `与<云霄月华>交谈 (0/1)`。
4. **找 NPC 链路在 UI-only / 无场景态卡点**:`SceneManager.GetNpc(100101)=null`(harness 无场景 NPC),`DoTask` 走到找 NPC 分支后无法走近/开对话 → 跨场景飞鞋(`USE_FLY_SHOE`)+ 进游戏后场景 NPC 装配(统一前置 = NPC 对话子系统进场景)是端到端 P2 的真实 blocker。本轮 UI 层与协议入口已就绪并取证,完整端到端需真连服务端登录进场景。
5. **对话已开去重未迁移**:老端 `MainUITaskTeamView.ts:563-573` 用 `DialogueModel.dialog_is_open` + NPC 匹配去重;Unity `DialogueModel.DialogIsOpen` 已在,`DialogueController.ShowTask` 已有同 NPC 去重,但任务自动推进(`mouseEvent` 定时 `DoTask`)未迁移 → 记录为确认风险,不臆造。

---

## 7. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误**(6 个既有无关警告:`AppLauncher` CS0649 ×3、生成 Bind CS0108 ×2、`MainRoleAgent.cs:206` CS0162)。
- Unity 编辑器重编译 → **0 error**(`GetConsoleLogs` Error=0);Play 态 harness 跑通(`views=73 shown=73`)。
- 改动文件:
  - `Assets/Scripts/Module/Core/Task/TaskModel.cs`(`BuildTipBody` 接 config_npc;`GetMainLineEntry`)
  - `Assets/Scripts/Module/Core/Task/TaskController.cs`(`OnGameStart` 预载 `config_npc`)
  - `Assets/Scripts/Module/Core/MainUI/Views/MainUITaskTeamView.cs`(`_box_main_line` 渲主线任务;隐 `_box_temple_awaken` 假占位)
- 运行证据:`output/runtime_unity/play_taskteam_before.png` / `play_taskteam_after.png` / `play_taskfinish.png`。

---

## 8. 后续轮建议

1. **NPC 对话子系统进场景端到端**(统一前置 blocker):真连服务端登录 → 场景 NPC 装配 → 点主线任务 → 走到 `云霄月华` →
   `12101/12102` 真对话 → `30004` 推进 `100010→100020`,补端到端 P2 截图。
2. **`MainUISkillView`**:技能 4 槽(`shortcutList` 13007/21002)+ 自动战斗按钮态 + 伙伴技能锁(stage 已见 `_box_partner_skill`/`auto_box`/`skill_box`)。
3. **`SetTempleAwaken` 完整移植**(`TempleAwakenModel`)+ 任务击杀/采集类文案 config_mon/config_goods 现拼。
