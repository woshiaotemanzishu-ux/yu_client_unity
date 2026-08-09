# MainStronger / 我要变强（158）静态闭包

## 结论

- 本轮已把 `MainUIRouter` 的固定入口 `158` 接入 `MainStrongerFlow.Toggle`，并补齐只读配置解析、推荐聚合、列表渲染与显式跳转注册接口。
- 当前仍不能把路线标为完成：仓库没有 `MainUIStrongerView` 可编辑 Prefab，没有 Unity 侧 `ConfigFeatureBinding`，也没有 `mainStronger` 资源闭包；本轮明确禁止启动 Unity/浏览器、修改 Addressables 及跨 MainUI/Daily/Activity 接线。
- 代码不会伪造推荐项：只有目标模块显式注册 `func -> opener` 且发布对应红点后，功能项才进入列表；技能觉醒也必须由 Task 模块分别发布资格和 opener。
- MainStronger 本身没有独立协议或写事务。老端列表点击只转发到目标功能；技能觉醒走既有任务执行链；广告点击走 `OpenFun` 或 `OPEN_ACT_VIEW`。

## 老端完整拓扑

1. 固定入口 158：`ConfigFuncOpenCondition[MainStronger]`（1级、前置任务 101340）、`MAIN_STRONGER` 数量红点、置顶推荐气泡、点击位置传给页面。
2. 页面根：Activity 层、194×320、透明背景、点背景关闭、右上关闭按钮；页面定位为 `x-width/y-height`。
3. 推荐列表：纵向 `listStrongBtn`，条目显示名称；普通项按 `OpenFun.OpenFunHandler(func)` 跳转；10001 技能觉醒选中任务并执行；点击后关闭页面。
4. 广告：最多 8 条，按 `show_id`；左/中/右三图、圆点、2.4 秒轮播、拖动换页；点击经条件校验后走 `skip_id` 或 `act_skip`，331@98/100 含开服天数改写，再关闭页面。
5. 生命周期：关闭清 timer/update beat，销毁列表管理器与广告缓存；重新打开即时读取当前推荐。

## Unity 现状与本轮改动

- 新增 `MainStrongerBootstrap`：注册 158，非自动重连断线时清理。
- 新增 `MainStrongerConfigs`：解析 `FeatureBinding` 和 `SkillAwakeTask`；缺配置时保持未就绪；排序为置顶优先、置顶 `order` 降序、其余 id 升序。
- 新增 `MainStrongerModel`：仅聚合外部发布的红点/技能觉醒资格，过滤没有已注册 opener 的条目。
- 新增 `MainStrongerFlow`：开放门、Popup 层 Prefab 加载、显式功能 opener、失败提示与释放闭包。
- 增量修改 `MainUIStrongerView/MainUIStrongerBtn`：真实 `ScrollRect.content` 克隆，克隆项调用 `Show`，关闭/重开清理；广告依赖未闭包时整组隐藏。
- 未修改 `MainUIStrongerTalkBoard`：唯一现有消费者位于 MainUI 的 `ActivityIcon.prefab`，属于本轮禁止修改的跨模块契约。

## 缺失闭包与跨模块契约

| 阻塞项 | 当前事实 | 后续精确动作 |
|---|---|---|
| 可编辑 Prefab | `Assets/Prefabs/UI/MainStronger` 不存在 | 获得老端运行时快照后运行 `convert-module`，生成 `MainUIStrongerView/MainUIStrongerBtn`，再用真实 Prefab 回填验证 |
| 客户端配置 | Unity 资源中无 `ConfigFeatureBinding` | 通过现有 ClientConfigSync/Addressables 流水线同步并登记；本轮禁止修改该流水线 |
| 图片资源 | Unity 侧无 `resource/game/mainStronger` | 按转换 manifest 最小同步 atlas/散图并登记 Addressables；不得靠运行时兜底导入冒充交付 |
| 红点与入口显隐 | Unity 无统一 RedDot 发布接线；MainUI 固定实例当前不按 `strongerNum` 增删 | 各业务发布 `PublishRedState`；MainUI 消费 `StrongerCount` 更新 158 显隐/数值；保持 `FuncOpenConfig[MainStronger]` 门 |
| 数字跳转 | Unity 无完整 `OpenFun` 数字分发表 | 已提供 `RegisterFeatureOpener(func, opener)`；目标模块 Bootstrap 按证据逐项注册，不在 MainStronger 猜映射 |
| 技能觉醒 | Task 资格、选中任务、DoTask 属于 Task 模块 | Task 发布 `PublishSkillAwake` 并注册 `RegisterSkillAwakeOpener` |
| 气泡/点击锚点 | `MainUIRouter` 仅传无参 `Action`，ActivityIcon 不传世界坐标 | MainUI 专属接线需传点击锚点并复刻 session 气泡规则；本轮只登记 |
| 广告 | `config_banner`、Activity 状态矩阵与 `OPEN_ACT_VIEW` 跨模块 | Activity 提供只读 banner provider/opener 后在 MainStronger 接入；当前整组隐藏 |

## 静态验证

- `dotnet build output/ui_route_audit/2026-08-09_main_stronger/MainStronger.StaticCompile.csproj -nologo -v:minimal`：通过，0 warning / 0 error。
- `git diff --check -- Assets/Scripts/Module/Core/MainStronger`：通过。
- 旧配置静态检查：`FeatureBinding=42`，`SkillAwakeTask=7`，置顶顺序应为 `40,10001,37`。
- 未启动 Unity、浏览器；未执行账号写事务；未修改 Common/Generated/MainUI/Daily/Activity/Addressables/Docs。

## 运行闸

- `blocked`：Prefab、配置、资源、红点发布者、技能觉醒、广告、点击锚点、全部目标模块 opener。
- `needs-runtime-verify`：158 真实 GraphicRaycaster 点击、开放前后显隐、Popup 层/透明背景/关闭、真实滚动与末项、排序/即时刷新、冷暖打开、关闭重开、同账号老 H5/Unity Web 两档 viewport。
