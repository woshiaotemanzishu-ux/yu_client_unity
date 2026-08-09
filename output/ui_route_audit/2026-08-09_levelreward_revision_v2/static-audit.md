# LevelReward revision-v2 静态审计

## 冻结拓扑

- schema 6 修正版拓扑展开为 779 节点、618 叶。
- `config_rush_giftbag` 冻结 22 档；每档分别枚举行显示、received 0/1/2/3/4、行内横向滚动、嵌套滚动冲突、末格及 2–6 个可见奖励格。
- 每个奖励格继续展开具体详情 View 身份、主底图、关闭按钮、背景返回四叶；男女配置数量一致，内容仍须运行态分别核对。
- 补齐开服日 `<=7:331@3_1` / `>7:331@3`、两处故事手指、主线100420强制关壳、`DeleteMe/CHECK_ITEM_USE`、41701 成功/失败/弹窗/banner/即时刷新/飞奖结果链。

## LevelReward 静态实现与层级断言

- Prefab `LevelRewardView` RectTransform fileID `4801658793492258178` 与 inactive `LevelRewardItem` template fileID `7860162908305253298` 的 `m_Father` 都是模块根 fileID `2230253001074255150`。
- `LevelRewardView.OnInit` 因此从 `transform.parent.GetComponentsInChildren<LevelRewardItem>(true)` 查找名称为 `LevelRewardItem` 且 inactive 的 sibling template；不再错误假设 template 位于页面子树或 `__Templates`。
- 41700 只读列表、received 状态、事件即时重建、hide/dispose/destroy 取消订阅已落地；received=0/2/3/4 分别是“条件不足”/“已领取～”/静默/“已被领完~”。
- received=1 只输出明确 blocked 日志，不发送 41701。
- output-only `LevelReward.Isolated.csproj` 编译：0 error；未启动 Unity、浏览器或前台程序。

## 明确 blocker

- 奖励格配置、性别内容、限量分母、共享 EquipmentItem 详情仍无岛内公开契约，未猜测实现。
- 41701 及成功/失败/发奖/飞奖均为未授权写事务，只枚举不点击。
- MainUI 入口、故事手指、任务清理和共享弹窗在文件岛外，只读登记。
- 所有玩家像素、拖动、裁剪、末格、cold/warm、hide/reopen 与真实 Web 证据仍需运行态验证。
