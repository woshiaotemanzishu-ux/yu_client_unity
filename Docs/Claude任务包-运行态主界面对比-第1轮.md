# Claude任务包-运行态主界面对比-第1轮

日期：2026-06-21

目标：停止“只在 Unity 里补零散功能”的推进方式，改为以老 Laya 客户端**运行时页面**为唯一可见真相，从主界面开始逐区块对比、逐区块补齐。第 1 轮只做主界面底部区域：`MainUIDownView` + `MainUIChatView` + 与其直接相连的底部入口点击结果。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/LayaUI转换流水线.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/Shenxiao主界面运行时填充清单.md`
- 本任务包

## 总原则

1. **老端必须看运行时，不看静态 `.scene` 当结论。** 老 Laya 编辑器/scene 和运行时不同，运行时才会打开窗口、重父、赋图、加活动图标、加引导特效。
2. **Unity 可以看预制体，也必须给运行时证据。** Unity 预制体与运行时通常接近，但 View/Flow 代码会造成差异；验收必须有 Unity 运行时截图或编辑期真机渲染截图。
3. **以玩家可见进展为交付。** 每轮都必须留下“老端运行截图/节点树 -> Unity 当前截图/节点或 prefab -> 具体差异 -> 修复 -> 再截图”的证据链。
4. **不做假 UI。** 按老端真实配置、真实协议、真实资源补；无数据时写 blocker，不画假数据、不写 fake/stub。
5. **不点杀转换产物。** 静态布局问题优先修 LayaUI 转换器/默认表/模板/运行时 View 同构；业务 View 只处理状态、数据、事件和必要显隐。
6. **画幅注意：游戏本体是竖屏。** Web 老端可在 1280x720 横向页面中展示，但不能把横屏截图误当成设计本体；底部 HUD 的相对关系、按钮数量、显示条件、点击面板才是本轮重点。

## 已采样的老端运行时基线

老端入口：`http://127.0.0.1:8090/index.html`

采样账号：

- 账号：`90990`
- 密码：`47071`
- 角色：`全久京`

运行时证据文件：

- `D:\GitProject\yu_client_unity\output\playwright\laya8090_after_create_role.png`
- `D:\GitProject\yu_client_unity\output\playwright\laya8090_after_create_role_stage.json`

注意：`output/playwright` 是本地采样证据目录，不要求提交。若需要刷新证据，重新打开老端运行页面，注册或登录同一账号，进入游戏后用 Playwright 截图和 `Laya.stage` 树导出。

老端首屏运行时确认存在的主界面根：

- `MainUITopView`
- `MainUIActivityView`
- `MainUISkillView`
- `MainUIChatView`
- `MainUISecondaryView`
- `MainUITaskTeamView`
- `MainUIDownView`
- `FunctionOpenIcon`
- `FirstRechargeBubble`
- `UIJoyStick`

第 1 轮只验底部相关：

- `MainUIDownView`: 运行时位于 `Activity` 层，`x=703,y=1279,w=870,h=1`；`_box_con` 内含 `_img_bg`、`_gp_icon_con`、`_gp_turn`、经验条 `_img_exp`、经验文本 `_lb_exp="0 / 140000"`。
- `MainUIChatView`: 运行时位于 `Main` 层，`x=778,y=1026,w=720,h=254`；含设置、好友、商城、变强图标；商城图标实际 skin 为 `resource\game\icon\texture\22.png`，变强 ActivityIcon 为 `158`。
- `MainUISkillView`: 虽然不在本轮主改，但底部视觉包含自动战斗/技能区；只做差异记录，不扩散实现。

## 老端源码锚点

- `D:\GitProject\yu_client\h5\src\mainUI\MainUIDownView.ts`
  - `UpdateIconItem`: 按 `MainUIModel.Main_Func_Icons[show_type]` 创建底部功能图标。
  - `_gp_icon_con`: 图标容器。
  - `_gp_turn`: 65 级后可翻页。
- `D:\GitProject\yu_client\h5\src\mainUI\MainFuncIconItem.ts`
  - `SetData`: 设置图标、红点、功能入口。
  - 点击触发 `EventName.SWITCH_MAIN_FUNC_VIEW`。
- `D:\GitProject\yu_client\h5\src\commonModel\MainUIModel.ts`
  - `Main_Func_Icons`
  - `Turn_Open_lv = 65`
  - `GetMainFuncOpenCond`
- `D:\GitProject\yu_client\h5\src\mainUI\MainUIChatView.ts`
  - `_img_setting`、`_img_friend`、`_img_shop`
  - `CreateStrengthenIcon`
  - `ChatModel.WelcomeChat`
- `D:\GitProject\yu_client\h5\src\commonController\MainUIController.ts`
  - `InitMainUI`: 打开首批 HUD views，Unity `MainUIFlow` 必须对齐这个顺序和集合。

## Unity 当前锚点

- `Assets\Prefabs\UI\MainUI\MainUIModule.prefab`
- `Assets\Scripts\Module\Core\MainUI\MainUIFlow.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUIDownView.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainFuncIconItem.cs`
- `Assets\Scripts\Module\Core\MainUI\Views\MainUIChatView.cs`
- `Assets\Scripts\Generated\UI\MainUI\MainUIDownViewBind.cs`
- `Assets\Scripts\Generated\UI\MainUI\MainUIChatViewBind.cs`

## 本轮 P0：建立可重复对比报告

先产出一份本轮报告，路径：

- `Docs/RuntimeCompare/MainUI-Bottom-第1轮.md`

报告必须包含：

1. 老端截图路径和采样时间。
2. 老端运行时节点清单：底部相关节点、坐标、尺寸、skin、文本。
3. Unity 当前截图路径和采样方式。
4. Unity 当前 prefab/Bind/运行时节点清单。
5. 明确差异表：按钮数量、按钮名称或图标资源、解锁条件、点击打开面板、红点/特效/引导、经验条、底部聊天/设置/好友/商城/变强。
6. 本轮决定修哪些，哪些只记录到后续轮。

最低验收：

```powershell
Test-Path output\playwright\laya8090_after_create_role.png
Test-Path output\playwright\laya8090_after_create_role_stage.json
rg -n "MainUIDownView|MainUIChatView|MainFuncIconItem|Main_Func_Icons|Turn_Open_lv|GetMainFuncOpenCond" D:\GitProject\yu_client\h5\src Assets\Scripts -S
```

## 本轮 P1：补齐底部功能按钮的真实配置与点击链路

目标：老端底部功能按钮不是“写死 5 个图标”，而是 `MainUIModel.Main_Func_Icons` + 功能开放条件 + `SWITCH_MAIN_FUNC_VIEW`。Unity 侧要把这一层抽成可维护模型，不继续把 `FuncIconLines` 写死在 `MainUIDownView` 里。

要求：

- 引入或完善 Unity `MainUIModel`/等价配置层，表达老端 `Main_Func_Icons`、`Turn_Open_lv`、`GetMainFuncOpenCond`。
- `MainUIDownView` 从模型读取数据，不直接硬编码图标二维数组。
- `MainFuncIconItem` 点击走统一路由，能打开已接入的真实面板：
  - `role` -> 角色页，若现有角色页残缺，必须截图记录残缺点。
  - `bag` -> 背包页，打开真实 `BagComponentView`/当前项目已接入背包页。
  - 未接入功能只记录“路由未注册”，不画假面板。
- 图标资源用 `ResManager` + `GameResPath`，不得字符串硬拼 Addressable。
- 开放条件必须来自已有配置/老端映射；如果配置缺失，写 blocker，不把所有按钮强行全开。

最低验收：

```powershell
dotnet build yu_client_unity.slnx -v:minimal
rg -n "Main_Func_Icons|Turn_Open_lv|GetMainFuncOpenCond|MainUIRouter.Open|FuncIconLines" Assets\Scripts\Module\Core\MainUI Assets\Scripts -S
```

需要截图或运行证据：

- Unity 主界面底部按钮截图。
- 点击 `角色` 后的面板截图。
- 点击 `背包` 后的面板截图。
- 如果点击失败，日志必须指向真实未接入点。

## 本轮 P2：对齐 MainUIChatView 底部入口

目标：老端底部聊天条区域包含设置、好友、商城、变强入口；Unity 要确认每个入口是否存在、资源是否正确、点击是否进入真实面板。

要求：

- 对比老端运行时 `MainUIChatView` 节点：
  - `_img_setting` skin `resource/game/mainUI/texture/mainui_set_icon.png`
  - `_img_friend` skin `resource/game/mainUI/texture/mainui_friend_icon.png`
  - `_img_shop` skin `resource\game\icon\texture\22.png`
  - `ActivityIcon` type `158` 挂在 `_box_strengthen`
- Unity 若已接：给截图和点击结果。
- Unity 若缺资源：写明缺哪个 key/文件。
- Unity 若面板残缺：截图记录，不假装修完。

最低验收：

```powershell
rg -n "CreateStrengthenIcon|_img_setting|_img_friend|_img_shop|_box_strengthen|ActivityIcon|158" Assets\Scripts\Module\Core\MainUI D:\GitProject\yu_client\h5\src\mainUI -S
dotnet build yu_client_unity.slnx -v:minimal
```

## 本轮 P3：只记录，不扩散实现

本轮只记录以下差异，不展开编码，除非 P1/P2 已完成且还有时间：

- `MainUISkillView`: 技能槽、自动战斗、伙伴技能锁。
- `MainUITaskTeamView`: 引导手指、任务点击链。
- `FunctionOpenIcon` 和 `FirstRechargeBubble`。
- 竖屏真实设备布局与横屏 Web 运行页面的差异。

这些进入下一轮任务包，不允许本轮四处开坑导致底部区域也没验收。

## 禁止事项

- 禁止只看 `.scene` 或 Unity prefab 就声明对齐；老端结论必须来自 `http://127.0.0.1:8090/index.html` 运行时。
- 禁止把横屏 Web 截图当竖屏设计结论；要记录实际 canvas/stage 尺寸和 UI 相对布局。
- 禁止伪造按钮、假红点、假活动、假背包、假角色属性。
- 禁止把“dotnet build 通过”当 UI 对齐完成。
- 禁止无截图/无节点证据提交“已完善”。
- 禁止修改用户或其他 worker 的无关改动，当前已知不要碰：
  - `Assets/_App/Fonts/DFPYuanW7 SDF.asset`
  - `Assets/_App/Fonts/FZYHJW SDF.asset`
  - `.playwright-cli/`
  - `output/`

## 交付格式

必须提交：

1. 玩家可见变化。
2. 老端运行时证据路径。
3. Unity 运行时/编辑期真机截图路径。
4. 差异报告 `Docs/RuntimeCompare/MainUI-Bottom-第1轮.md`。
5. 改动文件列表。
6. `dotnet build yu_client_unity.slnx -v:minimal` 结果。
7. 确认问题清单：只写有证据的问题。
8. 下一轮建议：从底部之后进入 `MainUITaskTeamView` 或 `MainUISkillView`，不要笼统写“继续完善”。
