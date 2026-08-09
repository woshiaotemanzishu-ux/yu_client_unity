# Rank v1 拓扑完整性复核

## 结论

v1 不能继续作为完整拓扑账。按照 schema 6 拓扑不可变规则，v1 全部文件保持不改，新建 v2；v2 在 v1 语义节点基础上增加 10 个独立叶子，并把原 `mainui.rank.entry` 从叶子修正为带 3 个直接控件的页面。

## 老端表面计数

- Rank 专属 Laya JSON 共 4 个、82 个节点：RankView 44、RankItem 22、RankTabButton 4、RankMenuView 12。
- TS `GetChildrenByNames` 序列化绑定共 76 个：RankView 42、RankItem 20、RankTabButton 3、RankMenuView 11。
- 外层窗口：1 个返回按钮、1 个外层“榜单”页签、标题/背景、可选 back_id 返回链。
- 内层榜单：11 个可见页签、4 个隐藏类型、1 个页签纵向滚动区、1 个榜单纵向滚动区。
- 直接点击面：榜首信息区 1、榜单行点击面 1、菜单默认按钮 2、条件举报按钮 1、菜单背景关闭 1；入口导航另计 1。
- 弹窗：RankMenuView 1 个；默认两按钮，自定义头像尝试三按钮，同目标二次触发关闭。
- 模型/特效：榜首角色模型 1 类、坐骑模型 1 类；榜首称号/勋章互斥 1 组；行称号/勋章互斥 1 组；当前配置休眠的页签标题图/特效 1 组。
- 返回/生命周期：主窗关闭、back_id 返回、弹窗背景关闭、同目标切换关闭、动作后菜单关闭；另有 GAME_START 预取、10秒缓存、ReOpen 模型恢复和 DeleteMe 清理。

## v1 漏项

1. `OPEN_RANK_VIEW` 的默认索引、RankView 参数页签和 back_id 深链。
2. 老端 GAME_START 预取与 Unity 开窗请求的裁决偏离。
3. 老端按榜类型 10 秒缓存的 cold/warm 时序。
4. `getRankDate()` 为空时直接返回的未就绪/旧状态残影门禁。
5. RankView 独立装饰层与隐藏分隔线。
6. `_gp_title/_title_img` 当前配置休眠但源码存在的页签称号块。
7. `ReOpen_callback` 榜首模型恢复。
8. RankView/RankItem 的列表管理器、头像和称号特效关闭清理。
9. RankMenuView 的 Activity 层、183×140 根尺寸、点击点全局定位。
10. v1 把入口压成单叶，无法表达路由/参数/预取三个直接控件。

## v2 状态

v2 共 77 个节点。v1 的 54 个非入口叶状态可重放；新增 10 个叶子（其中 3 个是拆分后的入口叶）由 `topology-review-results.json` 提交。所有新增运行相关项仍为 `blocked` 或 `needs-runtime-verify`，没有新增 `done`。

本轮没有修改 v1、业务代码、Prefab、资源、Docs 或任何共享文件；没有 build、Unity、浏览器、账号事务或 Git 写操作。
