# MainUI 底部区域 运行态对比 · 第 1 轮

范围：`MainUIDownView`(底部功能栏/经验条)+ `MainUIChatView`(聊天条 + 设置/好友/商城/变强入口)+ 底部入口点击结果。
方法：以老 Laya 客户端**运行时**为唯一真相,逐节点对老 Unity 预制体/运行时,列差异,定本轮修/后续记录。

> 画幅提醒:本轮老端运行证据是 **横屏 Web 页面**(stage 逻辑约 `2276×1280`,各视图 `centerX=0` 居中),
> 游戏本体是竖屏(Unity 设计分辨率 `720×1280`)。故**绝对 x/y 不可直接对**,本轮只对**相对布局 /
> 按钮数量 / 显示条件 / 图标资源 / 点击面板 / 文本**。坐标仅作锚点参考。

---

## 0. 采样基线

| 项 | 值 |
|---|---|
| 老端入口 | `http://127.0.0.1:8090/index.html` |
| 账号 / 角色 | `90990` / `全久京`(首次创角,Lv1) |
| 老端截图 | `output/playwright/laya8090_after_create_role.png` |
| 老端节点树 | `output/playwright/laya8090_after_create_role_stage.json`(UTF-16;`### Result` 头后是扁平节点数组 `{d,cls,name,x,y,w,h,text,skin,child}`) |
| 节点解析副本 | `output/_stage_bottom.txt`(本轮从 stage 抽出的 DownView/ChatView 子树,便于核对) |
| Unity 运行证据 | `output/runtime_unity/play_bottom.png` / `play_role.png` / `play_bag.png`(Play 态 RenderTexture 截图,见 §3) |
| 采样日期 | 2026-06-21 |

> `output/` 是本地证据目录(已 gitignore,不提交);Unity 截图放在新增子目录 `output/runtime_unity/`,不动既有 `output/playwright/`。

---

## 1. 老端运行时节点清单(底部相关)

### 1.1 MainUIDownView(层 `Activity`,root `x=703 y=1279 w=870 h=1`)

| 节点 | cls | x | y | w | h | skin / text | 说明 |
|---|---|---|---|---|---|---|---|
| `_box_con` | Box | 0 | 0 | 870 | 0 | — | 容器 |
| `_img_bg` | Image | 75 | -144 | 720 | 140 | (静态空) | 运行时 `SetTexture("mainUI","uizjmv3_001")` 赋底图 |
| `_gp_icon_con` | Box | 135 | -110 | 545 | 104 | — | **功能图标容器,运行时 `child=2`** |
| `_gp_turn` | Box | 676 | -127 | 115 | 115 | — | 翻面按钮(Lv≥65 才可用) |
| `_Image4` | Image | 75 | -12 | 720 | 12 | `mainUI/texture/ui_pro_bar_1.png` | 经验条底 |
| `_img_exp` | Image | 73 | -12 | ~0 | 12 | `mainUI/texture/exp.png` | 经验条(Lv1 经验 0 → 宽 0) |
| `_lb_exp` | Label | 254 | -12 | 724 | 24 | text=`"0 / 140000"` | 经验文本 |

**关键:`_gp_icon_con` 运行时 `child=2`** —— 首登 Lv1 只铺出 2 个功能图标。结合源码 `GetMainFuncOpenCond`
(角色/背包恒开,宠物/装备/秘宝走开放门槛),这 2 个即 **角色 + 背包**;`_img_bg` 静态无 skin 是因为运行时才赋图。

### 1.2 MainUIChatView(层 `Main`,root `x=778 y=1026 w=720 h=254`)

| 节点 | x | y | w | h | skin | 说明 |
|---|---|---|---|---|---|---|
| `_img_bg` | 0 | 0 | 720 | 254 | `mainUI/other/uizjmv3_003.png` | 聊天条底图 |
| `_panel_chat` | 148 | 3 | 396 | 65 | — | 世界栏(滚动条隐藏) |
| `_panel_sys` | 148 | 83 | 397 | 65 | — | 系统栏(滚动条隐藏) |
| `_box_setting` → `_img_setting` | 4 | 25 | 64 | 64 | `mainUI/texture/mainui_set_icon.png` | 设置 |
| `_box_friend` → `_img_friend` | 72 | 22 | 64 | 64 | `mainUI/texture/mainui_friend_icon.png` | 好友 |
| `_box_shop` → `_img_shop` | 550 | 33 | 64 | 64 | `icon/texture/22.png` | 商城(注:图在 icon atlas,非 mainUI) |
| `_box_shop` → `_img_shop_red` | — | — | 23 | 23 | `mainUI/texture/com_red_point.png` | 商城红点 |
| `_box_shop_effect` | 620 | 15 | 100 | 100 | — | 限购商城特效挂点(空) |
| `_box_strengthen` → `ActivityIcon` → `_img_icon` | 634 | 15 | 72 | 72 | `icon/texture/158.png` | **变强 = ActivityIcon type 158** |

---

## 2. 老端源码逻辑锚点(`yu_client`)

- `commonModel/MainUIModel.ts`
  - `Main_Func_Icons`:两行 `{func,res,story_arr}` —— 行0 `role/bag/pet/equip/treasure`,行1 `red/love/guild/composite/232`(Help 图标 res 即 `"232"`)。
  - `Turn_Open_lv = 65`;`GetTurnState()` = `level >= 65`。
  - `GetMainFuncOpenCond(func)`:Role/Bag 恒 `true`;Pet→`MountPetView`、Equip→`EquipView`、Treasure→`SecretTreasureMainView`、Red→`RedEnterView`、Love→`MarriageBaseView`、Guild→`GuildJoinBaseView`、Composite→`CompositeView`、Help→`GodBefallMainView`(`CheckFuncOpenState`)。
  - `SwitchView(func)`:逐 func 打开各自面板(纯路由,无协议)。
- `mainUI/MainUIDownView.ts`:`UpdateIconItem` 按 `Main_Func_Icons[show_type]` 过 `GetMainFuncOpenCond` 铺 `MainFuncIconItem`,`SetPosition((i-1)*105,0)`;`_gp_turn` 翻面循环 `show_type`。
- `mainUI/MainFuncIconItem.ts`:`SetData`→`SetImageSprite(_img_icon,"mainUI",res)` + 红点 `GetMainFuncRedState`;点击 `Fire(SWITCH_MAIN_FUNC_VIEW,func)`。
- `mainUI/MainUIChatView.ts`:`_box_setting`→`SettingView`、`_box_friend`→`OPEN_FRIEND_VIEW`、`_box_shop`→`OPEN_SHOP_MAIN_VIEW`;`CreateStrengthenIcon` = `ActivityIcon("158")` 挂 `_box_strengthen`;`ChatModel.WelcomeChat` 在 GAME_START 注入 1 条系统消息。

---

## 3. Unity 当前实现 + 运行证据

### 3.1 截图采样方式(编辑器内 Play 态真机渲染)

经 Unity MCP `RunCommand` 装一个截图 harness:Play 态下建 `720×1280` `ScreenSpaceCamera` 画布 + `LayerManager/ViewManager.Init`,
`RoleModel` 播种到**采样基线**(Lv1,exp `0/140000`,复现老端首登状态,非伪造功能数据),`FuncOpenConfig` 真表加载完成
(`IsLoaded=True`),实例化 `MainUIModule` 并 `Show` 全部 HUD 视图,相机渲到 `RenderTexture` 存 PNG;再调与图标点击同源的
`RoleFlow.Open()` / `BagFlow.Open()`(= `MainUIRouter` 注册的 `role`/`bag` 打开器)分别截图。
> 说明:纯编辑期(非 Play)Addressables 异步不泵,运行时异步图/配表不解析,故功能图标只能在 **Play 态**取到真实铺设结果。

### 3.2 预制体 / Bind / 业务 View

- 预制体 `Assets/Prefabs/UI/MainUI/MainUIModule.prefab`;`MainUIDownViewBind` / `MainUIChatViewBind` 字段与老端节点一一对应(含 `_tpl_MainFuncIconItem` / `_tpl_ActivityIcon` 模板)。
- 本轮新增 `Assets/Scripts/Module/Core/MainUI/MainUIModel.cs`:`MainFunc` 枚举 + `MainFuncIcons`(两行表)+ `TurnOpenLv=65` + `GetMainFuncOpenCond` + `GetTurnState`,逐项对齐老端。
- `MainUIDownView.cs`:改为读 `MainUIModel`(不再自带硬编码二维数组/翻面常量);`BuildFuncIcons` 用 `GetMainFuncOpenCond` 过滤,`MainFuncIconItem.SetData(MainFuncIcon)`;点击 `MainUIRouter.Open(res)`。
- `MainUIChatView.cs`:`_img_setting→"setting"`、`_img_friend→"friend"`、`_img_shop→"shop"`、`_box_strengthen→ActivityIcon("158")`,`CreateWelcomeSystemMessage` 注入系统欢迎条。
- 路由注册(各模块 Bootstrap):`role/bag/pet/equip/treasure/red/love/guild/composite/232/setting/friend/shop/chat` 全部已 `MainUIRouter.Register`。

### 3.3 运行证据要点

- `play_bottom.png`:底部功能栏铺出 **角色 + 背包** 2 个图标(与老端 `child=2` 一致);设置/好友/商城/变强(太极 158)四入口在;经验文本 `0 / 140000`。
- `play_role.png`:点角色 → 打开**角色五标签窗**(`BaseWindowSkin` + tab0 人物 `EquipmentView`:等级/属性条/极品属性区在);装备槽/3D 模型为空(需服务端装备+模型数据,本轮范围外)。
- `play_bag.png`:点背包 → 打开**背包五标签窗**(`BaseWindowSkin` + `BagComponentView` + 一键装备/装备吞噬/共鸣打造/容器扩展/一键使用 子按钮);背包格为空(需 `15010` 服务端数据,本轮范围外)。

---

## 4. 差异表

| 维度 | 老端运行时 | Unity 当前 | 结论 |
|---|---|---|---|
| 功能按钮数量 | `_gp_icon_con child=2`(Lv1:角色+背包) | Play 截图 2 个(角色+背包) | **对齐 ✓** |
| 功能配置来源 | `Main_Func_Icons` + `GetMainFuncOpenCond` | 新建 `MainUIModel` 同结构两行表 + 同 view 映射;DownView 去硬编码 | **对齐 ✓(P1 本轮)** |
| 翻面解锁 | `Turn_Open_lv=65` | `MainUIModel.TurnOpenLv=65` / `GetTurnState` | **对齐 ✓** |
| 图标资源 | mainUI atlas res 名 | 同名 res 经 `ResManager.SetImageAsync(GameResPath.GetIcon)` | **对齐 ✓** |
| 点击打开面板 | `SwitchView(func)` | `MainUIRouter.Open(res)`,全 key 已注册 | **对齐 ✓(角色/背包已截图验证开真窗)** |
| 经验条文本 | `_lb_exp "0 / 140000"` | `RefreshExp` → `"0 / 140000"` | **对齐 ✓** |
| 经验条宽度 | `_img_exp` 按 `exp/exp_lim`(Lv1=0) | `width=722*persent`(=0) | **对齐 ✓** |
| 设置/好友/商城入口 | 点 `_box_*` → Setting/Friend/Shop | `_img_setting/friend/shop` → 路由 `setting/friend/shop` | **对齐 ✓(P2)** |
| 设置/好友/商城图标 | `mainui_set_icon` / `mainui_friend_icon` / `icon/22` | 预制体烘焙同图(Play 截图三图标在) | **对齐 ✓** |
| 变强(强化) | `ActivityIcon` type `158` 挂 `_box_strengthen` | `CreateStrengthenIcon`→`ActivityIcon("158")`(Play 截图太极图标在) | **对齐 ✓(P2)** |
| 系统欢迎消息 | `ChatModel.WelcomeChat` 注入 1 条 | `CreateWelcomeSystemMessage` 注入(Play 截图系统条在) | **对齐 ✓(客户端本地)** |
| 角色页内容 | 老端五标签真窗 | 五标签窗;tab0 人物已接,余 4 标签 disabled;装备/模型数据空 | **残缺记录**(装备+模型数据管线,后续轮) |
| 背包页内容 | 老端五标签真窗 | 五标签窗(5/5 开);背包格空 | **残缺记录**(`15010` 背包数据,后续轮) |
| 功能图标红点 | `GetMainFuncRedState(func)` 多源 | `SetRedState(false)` 先隐 | **只记录**(各业务 Model 红点未移植) |
| 引导手指 | `story_arr` + `SELECT_STORY_TARGET` | 未移植 | **只记录**(StoryModel) |
| 经验升级特效/补间 | `ui_expbar` + 补间 | `_box_exp_effect` 先隐;去补间落终值 | **只记录**(AddUIEffect 未移植) |
| 商城/好友红点 | `ShopModel` / `MainUIModel.friend_red` | `_img_shop_red`/`_img_friend_red` 先隐 | **只记录** |
| 限购商城特效 | `_box_shop_effect`(ActivityIcon 612 重父) | `_box_shop_effect` 先隐 | **只记录** |
| 世界/系统滚动消息 | `allChat`/`sysChat` 各 30 | 未接(不造假) | **只记录**(ChatModel 后续轮) |

---

## 5. 本轮决定

- **修(P1)**:抽出 Unity `MainUIModel`(`Main_Func_Icons`/`Turn_Open_lv`/`GetMainFuncOpenCond`/`GetTurnState`),`MainUIDownView` 去硬编码二维数组,点击统一走 `MainUIRouter`;角色/背包点击端到端打开真窗(已截图)。
- **验(P2)**:`MainUIChatView` 设置/好友/商城/变强四入口的资源、路由、强化 `ActivityIcon(158)` 对齐(已在 main 上接好,本轮补运行证据)。
- **只记录(后续轮)**:功能图标红点多源、引导手指、经验升级特效/补间、聊天滚动消息(ChatModel)、角色装备/模型数据、背包 `15010` 数据、限购商城特效、商城/好友红点。

---

## 6. 验收命令结果

- `dotnet build yu_client_unity.slnx -v:minimal` → **0 错误**(仅 1 个既有无关警告 `MainRoleAgent.cs(206) CS0162`);新增 `MainUIModel.cs` 已进 `Shenxiao.Module.Core.csproj`(Unity 同步)。
- Unity 编辑器内编译 → **0 error / 0 warning**(`GetConsoleLogs`)。
- `rg` 锚点核对:老端 `Main_Func_Icons/Turn_Open_lv/GetMainFuncOpenCond` 与 Unity `MainUIModel` 字段、`MainUIChatView` 四入口字段均命中。

---

## 7. 后续轮建议

1. **下一轮进 `MainUITaskTeamView`**:任务列表(`30000`/`30004`)+ 队伍列表 —— 底部之后最显性的玩家可见块,且 `TaskController` 已存在。
2. 或 **`MainUISkillView`**:技能 4 槽(`shortcutList` 13007/21002)+ 自动战斗按钮态。
3. 底部遗留收口:背包格 `15010` 数据 + 角色装备/模型数据(让角色/背包真窗内容不空)、功能图标红点多源、经验升级特效。
