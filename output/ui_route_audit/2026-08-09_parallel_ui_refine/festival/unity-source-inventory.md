# Festival Unity 源码/Prefab/Bind 清单

- 私有代码：`Assets/Scripts/Module/Core/Festival/{FestivalBootstrap,FestivalController,FestivalFlow,FestivalModel}.cs`。
- 可编辑 Prefab：`Assets/Prefabs/UI/Festival/FestivalModule.prefab`，以及 `FestivalInfoListItem.prefab`、`FestivalRewardItem.prefab`。
- 只读生成 Bind：`Assets/Scripts/Generated/UI/Festival/*Bind.cs`（本轮严格禁写）。
- FestivalModule 内已有 12 个 Bind：AwardListItem、CommodityView、GetRewardView、GoToAscendingOrderView、InfoListItem、LevelAwardView、RewardItem、TaskListItem、TaskTabListItem、TaskView、UpLevelTipsView、UpLevelView。
- Flow 现状：只加载/显隐整个 `FestivalModule`，没有选择 Task/Award/Commodity 子页、没有克隆列表项、没有把 Bind 接到 Model/Controller、没有首次查看与弹窗路由。
- Model/Controller 现状：19401/19403 基础数据与红点已有；19402/19404 只保存最近结果；19405 只有发送器且无同号回包。没有 Unity 侧 `config_fiesta_*` 消费者或页面业务 View。
- Prefab 结构静态可见多个 `ScrollRect→Viewport→Content` 候选，但本轮禁止 Unity/浏览器，不能证明序列化 Bind 指向唯一真实 content、真实拖动、裁剪或末项可达。
- 起始 dirty：Festival 私有代码、Festival Prefab/专属资源和本路线 output 均为 0。

本轮唯一实现修复：`FestivalFlow` 增加期望显隐状态与请求 generation。关闭/Reset 会使在途加载失效；旧加载完成后释放实例，不再在断线或用户已关闭后把窗口异步弹回；重开落在旧加载期间时会在旧实例释放后按最新 generation 重新加载。该结论仅由源码检查支持，仍需 Unity 生命周期实证。
