# MainUI 固定 HUD 静态核查

## 本轮结论

- 已按老端源码、Unity 独立 Region Prefab、Generated Bind、业务 View 与只读 Flow/Router/Configs 建立 103 节点 schema 6 路线；全部保持 `not-run`，没有把静态检查写成 Unity/Web 完成。
- Top、TaskTeam、Skill、Joystick、AutoBrush、OnHook、Notification、SceneAssist 均有独立可编辑 Prefab；Fold 只有 `MainUIModule.prefab/TurnDisk` 根层实例，本轮避免改共享总装。
- 两处确定性缺陷已修：`MainUITopView` 克隆货币项后补 `Show()`；`MainUISkillView` 克隆/复用技能项后补 `Show()`。两者都先完成 BaseView 的 `EnsureBound/OnInit`，再注入数据。

## 静态通过项

- TaskTeam 的任务行、主线行和队伍行均已显式 `Show()` 后 `SetData()`；任务滚动结构在 Prefab 中为 `ScrollRect -> Viewport(RectMask2D) -> Content(VerticalLayoutGroup/ContentSizeFitter)`。
- AutoBrush 同时检查 `FuncOpenConfig[AutoBrush]` 与 `config_scene.type==1`，CanvasGroup 保持事件接收，完成特效 Handle 有版本化后到丢弃与 Hide/Destroy 清理。
- OnHook 同时消费 13212/13215 模型、功能开放、野外场景、经验 buff/上限，并区分标准/旧版入口；三类特效 Handle 有互斥和清理。
- Notification 只保留一份模板，按真实模型动态生成 8 类瞬时通知，空列表隐藏并停止/归零摆动；没有把 Activity 动态入口固化进此区。
- Joystick 默认只隐藏图形根，保持 View 常驻接收 SceneInput；按压起点、最大半径、释放隐藏逻辑静态存在。

## 未完成 / 运行闸

- Top buff 模板没有真实 buff 列表消费者；VIP/充值/光环/客服红点和完整自定义头像/头像框数据源未全部迁移，保持未完成。
- Skill 缺经验副本自动战斗禁用判定；PartnerAwake/GodSkill 的开放、锁定和刷新仍是跨模块缺口。
- SceneAssist 当前只有默认隐藏和三个路由，婚礼礼物、藏宝图、进度、红包雨、记录等没有权威事件/模型驱动；全部未完成。
- Fold 只可静态确认事件发射和 TaskTeam 消费；Rank/Activity 等跨岛消费者、折叠期间数据刷新、重进一致性必须运行验证。
- 所有视觉像素、两档 viewport、滚动拖动末项、摇杆四象限方向、技能 CD 动态、UIEffect 两时点动态与清理、通知点击后的即时消退、cold/warm/reconnect 均待真实 Unity Web 与老 H5 同账号顺序复验。
- 本轮未启动 Unity/浏览器，未发 13307 等写事务；台账全部 `not-run` 是预期结果。

## 并发边界

- 任务开始时 `MainUIMoneyItem.cs`、`MainFuncIconItem.cs` 已 dirty，`RewardFlyService.cs(.meta)` 为未跟踪文件；均未改动。
- 未改 MainUIDown、MainUIChat、HudBottomBar、HudChatBar、ActivityIconManager、MainUIActivityView、Activity Prefab/route、MainUIFlow、MainUIRouter、MainUIConfigs、Generated、Common、Addressables、Docs。
