# 神霄 主界面(HUD)运行时填充清单 + 工具生成核对

> 目的: 把"运行时加载的实际 UI"与"工具生成的预制体"两件事彻底讲清,
> 并给出**逐视图、逐模块**的落地顺序。业务行为以 `D:\GitProject\yu_client`
> 老客户端源码为准。本文是导航 + 计划,不是最终实现。
>
> **红线(协议)**: 本文所列协议号均来自阅读老客户端 TS handler 推得,
> **落地前必须逐一对照 `yu_client` + `yu_server` 源码再次确认**,不许凭本文直接写协议。
> (对齐 `Shenxiao进游戏链路.md` §4 红线: 不猜协议字段)

---

## 0. 两个问题的结论

### Q2 工具生成 —— 主界面 HUD 的工具生成基本是**对的**,不是 bug

逐个核对了"界面上有、预制体里找不到"的元素,分三类:

1. **被工具正确排除的"假缺失"(确实不该生成)**
   - `mainUI/MainUIStrongerView` / `MainUIStrongerBtn`: 是**陈旧重复文件**。
     真正运行时用的那份在 `mainStronger/` 目录(TS 里 `base_file="mainStronger"`),
     manifest 已正确生成(`view-prefab` / `inline`)。`mainUI/` 下那两份是废弃副本,orphan 正确。
   - `PlayerHead`: 老客户端所有引用**全是注释掉的**(`//require`、`// import`) → 已死代码,排除正确。
   - `MainUITeamView` / `MainUITaskView` / `FightModeView` / `FightModeItem` / `BaseIcon`:
     无 TS 类、无任何引用 → 死场景。真正的任务/组队是 `MainUITaskTeamView`,已生成。

2. **唯一一个真实的工具漏检(很小)**
   - `MainUIVipTipsView`: 活的(`commonController/MainUIController.ts:422` 用
     `new window["MainUIVipTipsView"]()` 反射实例化),但**没有自定义子类**。
     分析器静态扫描抓不到 `window["名字"]` 这种反射调用 → 误判成 orphan。
   - 它只是个 VIP 提示小弹窗,**不在主 HUD 里**。
   - **修法**: 给 `Tools/LayaUI/analyze_layaui.py` 的 `build_usage` 加一条
     `new window["X"]()` / `window["X"]` 反射引用正则,让纯布局视图落到 `standalone-prefab`。
   - **暂缓执行**: 会重刷 6 万行的 `ui_manifest.json`,在用户在场时再跑。优先级见 §2 末。

### Q1 运行时加载 ≠ Unity 加载 —— 这才是真正的大头,且被**各模块数据管线**卡住

`.scene` 文件里只有空容器 + 条目模板(`_tpl_*`),**真正铺满的列表是运行时由各 View
从模块数据 new 出来的**。这是 Laya 的设计,工具已忠实还原。所以 Unity 里加载出来"空一截"
是正常的——缺的是**数据管线**,不是预制体。

要把每个 HUD 列表填满,必须按老客户端真实协议+配置把对应的 **Manager/Model/Controller**
建起来,再写 View 的填充代码。绝不造假数据(对齐进游戏链路红线)。

---

## 1. 总览: 9 个 HUD 视图 + 依赖模块 + 当前状态

`MainUIModule` 组 = 老客户端 `MainUIController.InitMainUI` 首批打开的 9 个视图:

| # | 视图 | 运行时填什么 | 依赖模块(数据源) | 关键协议(待核对) | 当前 Unity 状态 |
|---|---|---|---|---|---|
| 1 | MainUITopView | 货币条/等级/血量/战力/buff 条 | Role(已有) + **Buff** | 13006货币(已通)/15055 buff | 货币已通,等级/血/战/buff 待补 |
| 2 | MainUIActivityView | 左侧活动图标格子 | **ActivityIcon 系统** + 功能开启表 | 33251/22702 等 | 半成品(5 处填充骨架) |
| 3 | MainUISkillView | 技能快捷栏(普通/降神/海战) | **Skill** + **AutoFight** + **GodBefall** | 13007/21002/44001/44011 | 19 行空壳 |
| 4 | MainUIChatView | 世界/系统滚动消息 | **Chat** | 聊天协议(待查) | 半成品(6 处填充骨架) |
| 5 | MainUISecondaryView | 左右次级图标 + 通知栏 | **ActivityIcon 系统** | 同活动 | 半成品(2 处填充骨架) |
| 6 | MainUITaskTeamView | 任务列表 + 队伍头像列表 | **Task** + **Team** | 30000/30004 任务,队伍协议(待查) | 半成品(6 处填充骨架) |
| 7 | MainUIDownView | 底部功能栏(设置/好友/商城/变强/角色/背包) | **纯配置**(MainUIModel.Main_Func_Icons) | 无协议 | 半成品(4 处填充骨架) |
| 8 | MainUIAutoBrushView | 斩妖第N关 + 自动闯关进度 | **AutoBrush**(控制器已有) | 13307 切换 | 100 行,0 填充 |
| 9 | UIJoyStick | 虚拟摇杆(移动输入可视化) | **Scene/移动输入**(场景线) | 场景级移动协议(非 MainUI 层) | 22 行空壳 |

> 注: "半成品"指 View.cs 已有部分填充骨架(由用户在 main 上共同开发),需对照本文补全数据接线;
> 不要覆盖用户已写的部分,只补缺口。

---

## 2. 模块建设顺序(分层落地)

用户结论: **"理论上都要补,先做文档,然后按文档进行。"** 下面给出建议顺序——
按 **依赖少→多、风险低→高、已解锁优先** 排,每一层做完可独立验收。

### 第 1 层 · 纯配置 / 已解锁(最先,最快见效,零新协议)
1. **DownView 底部功能栏**: 纯读 `MainUIModel.Main_Func_Icons` 配置 + 各功能开启条件,无协议。
   是 9 个里唯一**完全不依赖协议**的,先做能立刻让底部栏正确。
2. **TopView 补全(非 buff 部分)**: 等级/血量/战力来自 Role(已有 RoleModel/RoleController),
   货币(13006)已通。只缺把 level/hp/maxHp/combatPower 接到 Bind 字段。

### 第 2 层 · 已有控制器、补数据 Model
3. **AutoBrushView**: `AutoBrushController` 已存在,补 `AutoBrushModel`(13307 切换 +
   进度/关卡数据) + View 填充(进度条/关卡文本/自动按钮/红点)。
4. **TaskTeamView 任务列表**: `TaskController` 已存在,补 `TaskModel`(30000 拉列表 / 30004 完成)
   + `MainUITaskItem` 填充(标题/进度/完成特效/三级排序)。先做任务,队伍列表放后面。

### 第 3 层 · 新建中型模块
5. **Buff 模块**: `MainUIModel.RefreshBuff()` 是多源合并(世界等级/物品 buff 15055/VIP/守护/
   面具/周卡/职业技能 buff 等),先做**物品 buff(15055)**主链,其余源逐个接。
   产出 → TopView buff 条 + MainUIBuffView 弹层。
6. **ActivityIcon 系统**: `ActivityIconManager`(ADD_ICON/DELETE_ICON 注册表) +
   `CommonManager.GetFunctionIconCfg`(功能图标配置/开服天/等级开启)。
   产出 → ActivityView + SecondaryView(两者共用同一图标系统)。

### 第 4 层 · 大型模块
7. **Skill 模块(最重)**: `SkillManager.shortcutList`(13007 快捷栏 + 21002 技能列表) +
   `AutoFightManager`(自动战斗状态/临时模式) + `GodBefallModel`(降神 44001/44011)。
   三种填充情形(海战 ship / 降神 god / 普通 shortcut),固定 4 槽菱形布局。
   产出 → SkillView 技能栏 + 自动战斗按钮。
8. **Chat 模块**: `ChatModel`(allChat/sysChat 频道过滤,各 30 条滚动) + 频道协议。
   产出 → ChatView(世界+系统两栏) + ChatItem(富文本/表情/喇叭/超链接)。
9. **Team 队伍列表**: 队伍协议 + `TeamModel.team_member_list` → TaskTeamView 队伍页 +
   MainUITeamHeadItem。

### 第 5 层 · 场景线交叉(随地图/主角一起做)
10. **UIJoyStick + 移动**: 摇杆是输入可视化,数据来自 `SceneManager.curr_joystick_dir`,
    移动协议属**场景级**(非 MainUI 层)。按 `Shenxiao进游戏链路.md` §2.4-2.6
    随地图/主角加载一起做。摇杆对象常驻打开,stick 图默认隐藏(见 §3.9)。

### 工具线(可随时插入,独立)
- **修 analyzer 反射引用漏检(MainUIVipTipsView)**: 纯工具线,不碰 View.cs。
  会重刷 manifest,挑用户在场时跑(见 §0 Q2)。

---

## 3. 逐视图运行时填充规格

> 每节给出: 布局(LoadSuccess 关键定位)/ 运行时填充(数据源+模板+布局)/ 协议(待核对)/
> 配置表 / 刷新事件。坐标以老客户端为准,Unity 实现用生成的 `{Name}Bind` 字段,不散落 `transform.Find`。

### 3.1 MainUITopView(顶部条)
- **布局**: `centerX=0, top=0`(刘海则 `top=liuHaiHeight`);层 `Main`。
- **货币条** `SetMoneyType` → `_box_money`(HBox)内 1~3 个 `MainUIMoneyItem`:
  - 数据源 `RoleManager.GetMainRoleVo()`: `jin`(钻/type0)、`jinLock`(绑钻/type1)、`tong`(金/type2)。
  - 金(type2)显示用"万/亿"缩写;item 字体 2× fontSize 再 0.5× scale。
  - **Unity 现状**: `BuildMoneyItems` 已克隆 `_tpl_MainUIMoneyItem` 进 `_box_money`,13006 已通。
- **Buff 条** `RefreshBuff` → `_box_buff` 内最多 4 个 `MainUITopBuffItem`,`SetPosition(i*36, 0)`:
  - 数据源 `MainUIModel.buff_list`(由 `MainUIModel.RefreshBuff()` 多源合并)。
  - item 带圆形遮罩 CD 饼图(半径 18,偏移 -90°),`_lb_time` 仅 ≤99 秒显示。
- **协议(待核对)**: 10006 心跳;buff 物品来自 15055(`GoodsController.On15055` → `GoodsModel.goods_buff_list`)。
- **配置表**: `ConfigNotNormalGoods[2]`(绑钻 goods_id)、`[3]`(金 goods_id);`config_scene`。
- **刷新事件**: `UPDATE_BUFF_DATA`→RefreshBuff;`CHANGE_LEVEL`(延迟50ms)→RefreshLevel;
  `UPDATE_NEWEST_TASK_ID`→UpdateHaloIcon;`RoleManager.BindVar("hp","maxHp")`→RefreshRoleHp。

### 3.2 MainUIActivityView(左侧活动图标)
- **布局**: `_gp_con`,基点 `x=10, y=y+liuhai`;`hgap=5, vgap=20`,每行最多 7 个,行高 `72+20=92`。
- **填充**: 事件 `ActivityIconManager.ADD_ICON` → `CreateActivityIcon(icon_type)`:
  - 数据源 `ActivityIconManager.icon_info_dic`(`InitActivityIcon()` 初始化)。
  - 模板 `new ActivityIcon(this._gp_con)`(`mainUI/ActivityIcon`)。
  - `FormatIconList()` 预分 7 桶后 `RefreshIconPos()` 算 x/y;排序: `location_type`(1/2/3/4/9/10) → `pos_index`(`CommonManager.GetFunctionIconCfg`);预告图(+200+time)置尾。
  - 删除: `DELETE_ICON` 标 `to_delete=true` → 500ms 节流 `TryRefreshAllIcon`。
- **协议(待核对)**: 33251(开服活动广播)、22702(周期榜)。
- **配置表**: `config_rush_rank`、`config_cycle_rank_info`、`UIModelParameter["MainUIActivityView"]`;
  开启条件: 开服天 / 玩家等级(如 top-player 需 ≥130)。
- **刷新事件**: `UPDATE_ACTIVITY_STATE`、`CHANGE_ACTIVITY_STATE`、`REFRESH_ACTIVITY_ICON_RED_DOT`、
  `FirstRechargeModel.ADD_BUBBLE`、`UPDATE_SECONDARY_ICON(Left)`。

### 3.3 MainUISkillView(技能栏)—— **最重**
- **布局**: `centerX=0, bottom=254`;4 槽菱形固定坐标 `[4,99] [39,64] [96,63] [132,101]`;`GOD_SKILL_MAX=4`。
- **填充三情形**(`UpdateView`):
  1. 海战 `IsSeaHegemonyScene()` → `SeaHegemonyModel.GetShipSkillList()`,补 `{is_lock:true}` 至 4。
  2. 降神 `mainRoleInfo.god_id>0` → `GodBefallModel.CheckAndGetMainRoleGodAllSkill()`,补 `{}` 至 4,用 `MainUISkillItemGod`。
  3. 普通(默认) → `SkillManager.shortcutList`(来自 `ConfigSkillUI.carrerSkillList[career]`,滤掉普攻,按 skill_id 升序,HUD 取前 4),用 `MainUISkillItem`。
  - 容器 `_box_skill_con`;伙伴技能另置 `_box_partner_skill`。item 懒创建 + `SetData(vo)` + `SetVisible`。
- **自动战斗按钮** `_img_auto_fight`: `AutoFightManager.GetAutoFightState()` / `GetTempMode()` →
  关 `uizjmgj_003a` / 开 `uizjmgj_001b` / 临时 `uizjmgj_001a1`。
- **协议(待核对)**: 13007(快捷栏 `on13007`: pos/type/skill_id/is_auto)、21002(技能全列表 `On21002`)、
  44001(降神信息)、44011(降神切换,client→server)。
- **配置表**: `config_skill`、`config_god`、`config_onhook`、`ConfigSkillUI.carrerSkillList`。
- **刷新事件**: `UPDATE_SKILL_LIST`(重建)、`UPDATE_SKILL_BAR_INFO`、`ACTIVE_SKILL`(高亮)、
  `UPDATE_AUTO_FIGHT_STATE`、`AUTO_FIGHT_TEMP_MODE`、`GodBefallEvent.UPDATE_BATTLE_ID/KEEP_TIME`、
  `PartnerModel.REF_PARTNERSKILL`。
- **点击释放**: item.con 点击 → 校验存款态/CD → `FightEvent.SKILL_SHORTCUT_CLICK(skill_id, ONLY_FIRE_ATTACK)`;
  god item → 校验变身/CD → `GodBefallEvent.REQUEST_PROTO, 44011`。

### 3.4 MainUIChatView(聊天)
- **布局**: `centerX=0, bottom=0`;两栏竖滚 `_panel_chat`(`_box_chat_con` VBox)+ `_panel_sys`(`_box_sys_con` VBox);
  滚动条隐藏;左下 `_box_friend/_box_shop/_box_setting/_box_strengthen`(活动图标 158)。
- **填充**: 世界 `ChatModel.allChat()` / 系统 `ChatModel.sysChat()`,各 `MAX_SHOW_ITEM_NUM=30`:
  - `InitChat/InitSys` 循环建 `MainUIChatItem` + `SetData(msg, is_last_msg)`;
  - `UpdateChat/UpdateSys` 新消息滚动: 满 30 则移头插尾;更新后自动滚到底。
- **ChatItem 频道格式**(CHAT_TYPE): 0世界/4公会/5队伍/10系统/13跨服/15阵营/17小跨服/19海/20喇叭。
  富文本: 频道图 `<img>`(108×50)、表情 `disposeChtaFace`(68×44,字 36 scale0.5)、超链接 `<a>` + LINK 事件;
  语音 iType → "[语音信息]"。高度 `contextHeight*0.5+5`。
- **协议(待核对)**: 聊天频道收发协议号需查 `chat/ChatController` / `ChatModel`(本轮未取号)。
- **配置表**: `ChatModel.ViewClassCFG`(频道/开放天/开放等级)、`channelImage`、表情上下限。
- **刷新事件**: `CHAT_MSG_CHANGE`、`CHAT_CACHE_UPDATE`、`UPDATE_MAINUI_CHAT_VIEW`、`REFRESH_FRIEND_AND_MAIL_RED`。

### 3.5 MainUISecondaryView(次级图标 + 通知栏)
- **布局**: `_box_left`(left=0,bottom=290)、`_box_right`(right=0,centerY=250)、
  `_box_notification_bar`(条件 `IsActiveNotification`)、`_box_help`(条件 `IsActiveGuildHelp`);`vgap=hgap=5`。
- **填充**: 同 ActivityIcon 系统,按 `location_type`(Left/Right/Notice)分流
  `CreateLeftActivityIcon/CreateRightActivityIcon/CreateNoticeActivityIcon`:
  - Left: `x=floor(i/7)*77, y=-(i%7)*77`;Right: 变强图标 158 固定 `(-249,6)`,其余按图宽变长;排序 `pos_index`。
  - 612 限购图标特殊: 在次级视图隐藏,effect2 移到 `MainUIChatView._box_shop_effect`。
- **刷新事件**: `ADD_ICON/DELETE_ICON`(按 location 过滤)、`UPDATE_ACTIVITY_STATE`、
  邮件/聊天/福利/红包/限时礼包等各 model 红点事件。

### 3.6 MainUIDownView(底部功能栏)—— **第 1 层先做**
- **布局**: `_gp_icon_con`(HBox)居中 bottom=0;图标中心间距 105;左端 `_gp_turn`(折叠按钮)。
- **填充**(`UpdateIconItem`): 数据源 `MainUIModel.Main_Func_Icons[show_type]`(静态 2D 数组 `{func,res,story_arr}`):
  - 逐项校验 `MainUIModel.GetMainFuncOpenCond(func)`(等级/开服门槛);`new MainFuncIconItem(_gp_icon_con)`;
    `SetPosition((index-1)*105, 0)`;不开启的 `SetVisible(false)` 不移除。
  - **无协议**——纯配置。这是先做它的原因。
- **MainFuncIconItem**: `SetData({func,res,story_arr})` → `ResManager.SetImageSprite(_img_icon, "mainUI", res)`;
  红点 `MainUIModel.GetMainFuncRedState(func)`;点击 `Fire(SWITCH_MAIN_FUNC_VIEW, func, index)`。
- **刷新事件**: `GAME_START`→UpdateView、`UPDATE_NEWEST_TASK_ID_NOT_DELAY`、`CHANGE_LEVEL`(翻页解锁)、
  `turn_red_dot` 绑定、`SELECT_STORY_TARGET`。

### 3.7 MainUITaskTeamView(任务/队伍)
- **布局**: `left=10, bottom=380`;`_box_task`(任务)/`_box_team`(队伍)切换;`_panel_task`(h≈212-214)。
- **任务填充**(`SetTaskItem`→`SetTaskShow`): 数据源 `TaskModel.GetAllTaskList()`;
  模板 `MainUITaskItem` 入 `task_item_list[]`;`SetPosition(0, t_h)` 累加 +2px;
  三级排序 `sort_index→sort_sub_index→same_type_order_index→task_id`;按条件过滤主线/觉醒/守护等。
- **队伍填充**(`UpdateTeamData`): 数据源 `TeamModel.team_info.team_member_list`;模板 `TeamMainRoleItem`/`MainUITeamHeadItem`
  经 `LoopScrowViewMgr` 入 `_list_team`;不足 `TEAMER_MAX` 补空 `{}`。
- **主线任务**(`SetMainLineTask`): 单条 `MainUITaskMainLineItem` 入 `_box_main_line`,`visible=need_arrow`。
- **协议(待核对)**: 30000(查任务列表)、30004(完成);队伍协议(待查 `team/TeamController`)。
- **刷新事件**: `UPDATE_ALL_TASK_FROM_30000`、`TEAM_UPDATE_TERAM_INFO`、`RoleManager.CHANGE_LEVEL`。
- **MainUITaskItem**: `SetData(task)`→标题 `[tag]name` 按 TaskType 着色 + 进度 `(cur/max)`;
  文本 x6/y32/宽215/行18;完成加 `ui_renwulan` 特效;高度 `lineHeight+32+5` 回调父布局。

### 3.8 MainUIAutoBrushView(斩妖/自动闯关)—— **第 2 层**
- **布局**: `x=26, bottom=262`,scale 0.9;`click_gp`/`_box_auto_level`/`_lb_auto_level`/`_lb_level`/`_img_progress`/`_img_red(2)`。
- **填充**: 数据源 `AutoBrushModel`:
  - 关卡 `GetLevel()` → `第${level+1}关`;状态 `GetAutoBrushStrangeState()` → 按钮 "取消自动"/"自动闯关";
  - 进度 `GetBrushStrangeInfo()` → `_img_progress.width = cur/need * 宽`;
  - 红点 `_img_red`(怪物/阶段)、`_img_red2`(刷怪 Brush 标志);主线任务未完时抑制刷怪红点。
- **协议(待核对)**: 13307(切换自动 0/1);13305(领奖,代码中注释)。
- **开启门槛**: `CommonManager.CheckFuncOpenState("AutoBrush")` 且 `SceneManager.IsFieldScene()`。
- **刷新事件**: `UPDATE_BRUSH_STRANGE_INFO/STATE`、`UPDATE_LEVEL`、`UPDATE_NEWEST_TASK_ID`、
  `MonsterModel.UPDATE_RED_DOT`、`SCENE_CHANGED`。

### 3.9 UIJoyStick(虚拟摇杆)—— **第 5 层(随场景线)**
- **布局**: `_gp_root`(触点锚)/`_img_bg`/`_img_arrow`/`_img_middle_circle`;`x=0,y=0` 全屏;`mouseEnabled=false` 透传。
- **填充/逻辑**: 输入源 `SceneManager.curr_joystick_dir`(归一化 Vector2):
  - `BindOne("curr_joystick_dir", ...)`: 有值且非 ExpScene → `ShowJoyStick()`;空 → `HideJoyStick()`。
  - `UpdatePos`: root = `curr_click_start_pos`;stick = `(100+dir.x*dist, 100+dir.y*dist)`,
    `dist=Min(98-44.5, curr_click_move_dist)`;按象限算 rotation。
- **协议**: 摇杆本身**无协议**,移动发送属**场景级**(SceneController/MainRole),非 MainUI 层。
- **关键约束**: 摇杆对象常驻 `Open()`,事件绑定不断;"未显示"靠 `ShowJoyStick/HideJoyStick` 切 stick 图,
  **不要整个 GameObject `SetActive(false)`**(会断事件接线)。见 `MainUIController.ts:486-498`。

---

## 4. 共用基础设施(建模块时优先抽出)

这些被多个视图复用,建第一个用到它们的模块时一并抽好,避免重复:

- **ActivityIconManager**: 图标注册表(`icon_info_dic` + `ADD_ICON/DELETE_ICON/UPDATE_SINGLE_ICON/SET_ICON_TEXT`),
  供 ActivityView + SecondaryView 共用。
- **CommonManager.GetFunctionIconCfg / CheckFuncOpenState**: 功能开启判定(等级/开服天/功能表),
  供活动图标、底部栏、AutoBrush 共用。
- **MainUIModel**: `buff_list`/`Main_Func_Icons`/`GetMainFuncOpenCond`/`GetMainFuncRedState`/各类 UI 状态事件中枢。
- **LoopScrowViewMgr**(虚拟滚动): 队伍列表、Buff 弹层用。Unity 侧需有对应的虚拟列表组件或等价实现。
- **CD 圆形遮罩(饼图)**: TopBuffItem / BuffItem / SkillItemGod 都用,抽一个 Unity CD 遮罩组件。

---

## 5. 验收方式(每模块独立)

按 §2 顺序,每完成一层就验收一次,不要等全部做完:

1. **DownView**: 底部 6 个功能图标按配置正确显示/红点/点击事件触发。
2. **TopView**: 等级/血条/战力/货币与协议数据一致;buff 条随 15055 出现。
3. **AutoBrush**: "第N关"+进度条+自动按钮状态与 AutoBrushModel 一致。
4. **Task**: 任务列表按 30000 数据铺开,排序/进度/完成特效正确。
5. **Activity/Secondary**: 活动图标按开服天/等级/功能表出现,红点正确。
6. **Skill**: 技能栏按 shortcutList 铺 4 槽,降神/海战切换正确,自动战斗按钮态正确。
7. **Chat**: 世界+系统两栏滚动,频道着色/表情/喇叭正确。
8. **Team**: 队伍列表按队伍协议铺开。
9. **JoyStick**: 随场景线,移动输入时显示,松手隐藏(对象常驻)。

---

## 6. 红线(重申)

- 不写 mock/fake/stub 数据假装填充成功——没数据就先不填,或先做无协议的(DownView)。
- 不猜协议——本文协议号落地前逐一对照 `yu_client` + `yu_server` 源码。
- 通用工具问题修转换器/分析器,不手改转换产物。
- 业务 View 用生成的 `{Name}Bind` 字段,不散落 `transform.Find`。
- 不覆盖用户在 main 上已写的 View 填充骨架,只补缺口。
