# Shenxiao 进游戏链路

> 目的: 选角/创角后进入游戏的流程必须按阶段接入,避免把主界面、地图、主角、
> NPC/怪物、弹层一次性混在一起做。业务行为以 `D:\GitProject\yu_client`
> 老客户端源码为准。

## 1. 老客户端事实

老客户端不是选角后直接切一个“新场景”就结束,而是由协议、资源、UI、场景系统共同接管。

关键链路:

1. `LoginController.On10004`
   - `10004` 回包 `result == 1` 表示进入游戏服成功。
   - 随后主动请求 `13001`、`10201`、`30005` 等协议。
   - 设置 `try_game_start` 并重置进游戏接收状态。

2. `ResManager.TriggerGameStart`
   - 设置 `true_game_start = true`。
   - 打开场景协议接收: `SceneManager.can_receive_scene_protocal = true`。
   - 重置主角场景信息。
   - 派发 `EventName.GAME_START`。

3. `MainUIController.InitMainUI`
   - 打开主界面基础视图:
     - `MainUITopView`
     - `MainUIActivityView`
     - `MainUISkillView`
     - `MainUIChatView`
     - `MainUISecondaryView`
     - `MainUITaskTeamView`
     - `MainUIDownView`
     - `MainUIAutoBrushView`
     - `UIJoyStick`

结论: Unity 侧也应先有一个明确的“进游戏接管点”,再分别启动 UI、地图、主角和场景对象。

## 2. Unity 当前目标链路

当前最小目标不是完整游戏世界,而是:

> 选择/创建角色 -> 进入游戏服成功 -> 收到主角基础数据 -> 主界面 UI 出现 -> 地图出现 -> 主角出现。

阶段拆分如下。

### 2.1 协议成功

入口:

- `LoginController.EnterGameWithRole`
- `LoginController.OnEnterGame`

条件:

- `10004` 回包 `result == 1`

事件:

- 派发 `GlobalEvent.EVT_GAME_ENTERED`

只做:

- 表示游戏服进入成功。
- 不直接打开主界面。
- 不直接加载地图。
- 不创建主角/NPC。

### 2.2 主角基础数据就绪

入口:

- `GameEntryFlow`

条件:

- 收到 `13001` 主角全量数据。
- `RoleModel.Instance.HasBaseInfo == true`

事件:

- 当前派发 `GlobalEvent.EVT_ROLE_READY`

职责:

- 注册游戏内控制器: `ControllerHub.InitAll()`。
- 等待主角基础数据。
- 作为 UI/场景接管的第一个可靠门槛。

后续建议:

- 当前已增加更贴近老客户端语义的 `GlobalEvent.EVT_GAME_START`。
- `EVT_GAME_START` 的含义是: 进入游戏服成功,并且启动协议门闩已完成,可以启动主 UI 和场景接管。

### 2.2.1 GAME_START 协议门闩(当前 Unity 规则)

当前 Unity 的 `EVT_GAME_START` 不再只是“13001 主角基础数据已就绪”。
它必须对齐老客户端 `10004` 成功后的启动接收门闩:

- `13001` 主角基础数据就绪。
- `10201` 服务器时间就绪。
- `30005` 最新已完成任务 id 就绪。
- `13088@300@1` 首开计数状态就绪。
- `10202@3` 系统设置就绪。

只有五个标记都完成后, `MainUIFlow`、`SceneEntryFlow` 和其他游戏内控制器
才能启动首屏 UI/场景工作。`EVT_ROLE_READY` 只表示主角数据可用,不能直接打开
MainUI 或进入场景。

### 2.3 主界面 UI 启动

入口:

- `MainUIFlow`

条件:

- 监听 `EVT_GAME_START`。
- 不监听 `EVT_ROLE_READY`; 主角基础数据只是 `GAME_START` 门闩的一部分。

加载:

- `GameResPath.GetUIPrefab("mainUI", "MainUIModule")`
- Addressable key: `prefabs/ui/mainui/mainuimodule`

首批打开(对齐老客户端 `MainUIController.InitMainUI`):

- `MainUITopView`
- `MainUIActivityView`
- `MainUISkillView`
- `MainUIChatView`
- `MainUISecondaryView`
- `MainUITaskTeamView`
- `MainUIDownView`
- `MainUIAutoBrushView`
- `UIJoyStick`

> `UIJoyStick` 应当**被打开**(`new` + `Open()`),对象常驻并保持事件绑定;
> 但其可见的摇杆图(stick art)默认由场景/输入状态控制隐藏,移动输入前不显示
> (老客户端靠 `SceneManager.curr_joystick_dir` + `ShowJoyStick/HideJoyStick` 切换,
> 见 `MainUIController.ts:486-498`)。不要用「整个 GameObject `SetActive(false)`」
> 来表达「摇杆图未显示」——那会连带关掉对象的事件接线。

验收:

- 选角/创角后登录 UI 退下。
- 主界面模块可见。
- 不要求按钮逻辑全部可用。
- 不要求地图/主角/NPC 已出现。

注意:

- 主界面属于 `UILayer.Main`。
- 登录背景属于登录模块,进入游戏后必须退下,否则会挡住主界面。
- 业务 View 后续应继承生成的 `{Name}Bind` 并使用 Bind 字段,不要在业务 View 中散落 `transform.Find`。

#### HudNavBar 经验条可视化维护（2026-07-30）

- `ExpBarFill` 的 RectTransform 始终保存完整轨道尺寸，图片使用从左向右的 `Image.Type.Filled`。`MainUIDownView` 只同步 `fillAmount`，禁止再按经验比例写 `sizeDelta.x`；否则中心 Pivot 会让图片左右同时缩短，并覆盖 Prefab 中的尺寸调整。
- 经验特效定位由 `ExpBarFill` 上不可交互的 Slider 驱动：`ExpBarEffectTrack/ExpBarEffectHandle` 是纯机械定位结构，真实 `ExpBarSparkleSlot` 是 Handle 下的普通子节点。Slider 只改 Handle anchors，特效挂点的 Size、Pivot、Pos X/Y 均可在 Prefab 中直接调整。
- `HudNavBar.prefab` 已进入人工维护阶段并作为唯一视觉源；页面 `HudNavBarCreator` 已删除，`MainUIModuleCreator` 只嵌套现有 Region。资产缺失时必须从 Git 恢复，不得自动重生成覆盖人工界面。
- `HudNavBarCase` 覆盖 0/25/50/100%、越界夹取和轨道改宽，逐次断言填充图片与真实特效挂点的位置、尺寸及 Anchors 不被运行时进度覆盖。

### 2.4 地图加载

入口建议:

- 新增 `SceneEntryFlow`

条件:

- `EVT_GAME_START`。
- `RoleModel` 中已有 `SceneId/X/Y`。

职责:

- 根据主角场景数据确定加载哪个地图。
- 对齐老客户端 `SceneManager` / `MapManager` / `Scene` 的真实规则。
- 地图资源、地图 id 与资源 id 的对应关系必须查老客户端配置,不能猜。

验收:

- 进入游戏后能看到正确地图底图或地图场景。
- 不要求主角/NPC 同步完成。

### 2.5 主角加载

入口建议:

- 新增 `MainRoleFlow` 或归入场景系统的主角创建流程。

条件:

- 地图容器已准备。
- `RoleModel` 已有职业、性别、外观、坐标等必要数据。

职责:

- 加载主角模型。
- 挂载武器、翅膀、特效等外观。
- 设置初始坐标、朝向、层级。

验收:

- 地图上能看到自己的角色。
- 坐标和朝向与协议数据一致。

### 2.6 NPC、怪物、其他玩家、场景特效

入口建议:

- 独立场景对象系统。

条件:

- 场景协议接收规则确认。
- 场景对象 vo 结构确认。

职责:

- NPC 加载。
- 怪物加载。
- 其他玩家加载。
- 传送点、场景特效、采集物等对象加载。

验收:

- 按协议和地图配置出现,不写假对象。

### 2.7 自动弹层与系统入口

入口:

- 各业务 Controller 监听 `EVT_GAME_START` 或更细粒度事件。

原则:

- 主 UI 和地图可见之前,不要抢先弹大量窗口。
- 弹层顺序必须对齐老客户端 Controller 的监听顺序和条件。
- 未确认的自动弹层先记录,不要临时写死。

## 3. 阶段验收顺序

必须按下面顺序推进:

1. UI 验收
   - 选角/创角后主界面 UI 出现。
   - 登录背景退下。

2. 地图验收
   - 正确地图资源出现。
   - 地图 id 来源清楚。

3. 主角验收
   - 自己的角色模型出现在地图上。
   - 坐标/朝向/外观来源清楚。

4. 场景对象验收
   - NPC、怪物、其他玩家按真实协议或真实配置出现。

5. 弹层验收
   - 登录后自动弹层按老客户端逻辑逐个接入。

## 4. 红线

- 不要为了主 UI 新建 Unity Scene。主 UI 是 UGUI 模块,不是 Unity 场景。
- 不要把地图、主角、NPC、弹层混在一个 Flow 里。
- 不要写 mock/fake/stub 数据来假装进游戏成功。
- 不要猜协议字段。协议格式和行为查 `yu_client` / 服务端源码。
- 不要手改转换产物来修通用问题;通用问题修转换器。
- 不要在业务 View 里散落 `transform.Find`;使用生成 Bind 字段。
- 不要让登录层背景留在 `Window` 层挡住 `Main` 层主界面。

## 5. 当前落点

当前 Unity 已有:

- `LoginController`: 负责 `10004` 进入游戏。
- `GameEntryFlow`: 负责 `EVT_GAME_ENTERED -> 注册游戏内控制器 -> 等 13001/10201/30005/13088@300@1/10202@3 -> EVT_GAME_START`。
- `MainUIFlow`: 负责 `EVT_GAME_START -> MainUIModule`。

下一步建议:

1. 把 `MainUIFlow` 从“激活子节点”推进到“对齐老客户端 MainUI View 初始化”。
2. `EVT_GAME_START` 已落地,UI 和场景都挂到这个统一接管事件。
3. 新增 `SceneEntryFlow`,只负责地图加载。
4. 地图稳定后再接主角加载。
5. 最后再处理 NPC/怪物/弹层。
