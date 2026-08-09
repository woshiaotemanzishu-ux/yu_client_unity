# Rank / 排行榜静态对接审计（2026-08-09）

## 结论

本路线当前为 `blocked`，不是 `done`。Unity 已有 `activity_rank` 路由空壳、`config_ranking/config_medal` 读取、22100/22101 注册与 22101 分页数据模型，但没有 `RankEntView/RankView/RankTabButton/RankItem/RankMenuView` 的可编辑 Prefab 或 Bind；点击入口只能尝试加载缺失的 `ui/rank/RankEntView` 后降级提示。

静态审计同时确认 `RankConfigs.EnsureLoaded()` 原本全仓没有调用点，而 `RequestRankFirstPage()` 依赖其 `rank_max`；若未来直接补上 Prefab，战力榜会从配置要求的 100 条静默退化为兜底 20 条。本批已在干净且明确归属 Rank 的 `RankFlow.OpenAsync()` 中，于实例化内容前补一次配置加载。这是本轮唯一业务代码改动；未触碰共享文件。

本批仅做静态三方调和，未启动 Unity、浏览器或 Computer Use，未运行老 H5/Unity Web，未操作真实账号，也未发任何协议。所有像素、两档 viewport、列表拖动、榜首模型/称号特效双帧、cold/warm、即时刷新、关闭重开证据均为 NVR。

## 老端页面与控件事实

- `RankEntView`：基于 `BaseWindowComponent`，单一外层页签“榜单”，标题 `rank.rank_title`，背景 `uiphb_001.jpg`，内容类为 `RankView`；返回按钮关闭窗口，若带 `back_id` 则关闭后沿 `OpenFun` 返回。
- `RankView`：720×992。右侧 `tabScroller` 为 116×470 的纵向滚动页签；`itemScroller` 为 549×370 的纵向滚动榜单。榜首区域含角色/坐骑模型、名次、VIP、姓名、等级/转生、称号特效或勋章、榜单类型指标；榜单为空时隐藏模型/主要信息并显示“虚位以待”。底部显示本人头像、本人名次和本人对应指标。
- 可见页签按 `sortid`：战力(200)、等级(300)、成就(400)、装备(608)、坐骑(601)、剑魄同修(604)、垂神翼影(603)、问鼎云台(500)、殒锋天刃(607)、古法符相(606)、挂机收益(700)。隐藏：结社(100)、飞骑(602)、第二同修/精灵(605)、爬塔(609)。
- `RankItem`：前 3 名显示名次图标，其余显示数字；真实行显示玩家头像、VIP/隐私 VIP、姓名、称号特效或勋章、等级/转生/境界图标及类型指标；`player_id==0` 显示占位。真实行点击先请求好友菜单数据，再打开角色菜单；本人不打开菜单。
- `RankMenuView`：Activity 层、透明背景、点背景关闭；同一目标再次触发会切换关闭。默认按钮为“查看信息”“加为好友”；自定义头像条件下源码尝试追加“举报头像”。查看信息请求玩家信息后关闭菜单；加好友触发好友写事务；举报进入头像投诉流程。源码末尾无条件隐藏 `btn_3`，且循环边界存在静态风险，实际第三按钮表现必须由老端运行态裁决，不能用源码猜成已生效。
- `RankTabButton.redDisplay`、点赞计数/膜拜分支虽残留，但当前老端没有有效红点消费链，服务端 22102/22103/22104 处理段也已注释，不能新增 Unity 功能。

## 协议与状态链

- 22101：请求 `(rankType,start,len)`；回包含 `rankType/start/len/roleRank/selVal:u64/selSecVal:u32/sum:u32/rankList[]`。Unity 读取宽度与当前服务端 `pt_221.erl` 一致；每页 20，目标条数由 `config_ranking.rank_max` 驱动。
- 22100：老端与 Unity 都保留错误壳；当前服务端未见实际发送路径，不能据此宣称运行通过。
- 22102/22103/22104：老端仍有注册/发送残留，但当前 `pp_common_rank.erl` 的结社榜、点赞信息和膜拜整段被注释。Unity 不注册、不发送是当前存活边界。
- 22105：当前服务端存活，但老端 RankController 从未注册；Unity 按老端事实不迁移。
- 榜单页签点击应触发对应 22101、当页即时刷新榜首/列表/本人区；本批没有真实运行，因此所有即时刷新和重开一致性均未验证。

## Unity 落地与资源闭包

- 当前模块目录仅有 `RankBootstrap.cs`、`RankConfigs.cs`、`RankController.cs`、`RankFlow.cs`、`RankModel.cs`；目标范围在开始时干净。
- `RankFlow.OpenAsync()` 已在实例化 Rank 内容前 `await RankConfigs.EnsureLoaded()`，保证未来 `RequestRankFirstPage()` 读取真实 `rank_max`；实际 Addressable 加载、100 条分页和错误降级仍需运行态验证。
- `Assets/Prefabs/UI/Rank/` 和 `Assets/Scripts/Generated/UI/Rank/` 均不存在。
- Rank 自有资源只有 `rank_texture.spriteatlas` 与 `texture/rank_icon_1.png`；老端 manifest 还依赖 Rank 多张纹理、common/mainUI 纹理、BaseWindowSkin、CustomHeadItem、FriendModel/角色菜单、UIEffect、角色/坐骑模型加载等共享闭包。
- `Schemas/LayaUI/ui_manifest.json` 将 `RankView` 判为 standalone-prefab、`RankMenuView` 判为 view-prefab，并将 `RankItem/RankTabButton` 判为内联模板；完成首次转换会生成/修改 Generated、Prefab、Addressables 等本轮禁止区，还需要 Unity Editor 与老端/Unity 快照，故 `convert-module` 按边界登记为 blocker，没有执行。
- 没有可编辑 Prefab，因此 `fix-view` 不适用，也没有做任何 Prefab/代码增量修复。

## 验证级别

- 已做：目标文件岛初始 dirty 检查；老端 TS/JSON/配置、Unity C#、Laya manifest、当前服务端 `pp_common_rank/pt_221` 静态交叉；配置 SHA-256 同源核对；补齐 Rank 配置加载调用；schema 6 新账初始化/应用/校验；定向 `git diff --check`。
- 未做：Unity/浏览器/Computer Use、构建或编译、真实 Web、账号写事务、协议实发、像素/滚动/模型/特效/性能/cold-warm/即时刷新/重开。

## 后续解锁条件

1. 明确授权首次 `convert-module` 所需的 Rank Prefab/Generated/Addressables 最小闭包，并允许使用现有 Unity 工作区执行定向转换（不能新建 Library）。
2. 先采老端同账号同 viewport 的 Rank 主窗口、11 页签、榜首/空榜、列表首尾、角色菜单两/三按钮状态快照，再落 Unity Prefab。
3. Prefab 落地后切换到 `fix-view`，沿同一路线逐叶复验，并补两档 viewport、列表真实拖动、榜首模型/称号双帧、cold/warm、即时刷新与关闭重开证据。

本轮只补齐 Rank 模块内部已有配置读取器的初始化调用，没有调整公共架构、公共组件、协议字段或构建流水线；且文件岛明确禁止 Docs，因此未更新项目 Docs。
