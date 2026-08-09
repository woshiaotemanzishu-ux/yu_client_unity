# LevelReward 静态盘点（2026-08-09）

## 完成边界

- 本轮仅核对老端源码/配置、Unity Prefab/Bind/业务代码；未启动 Unity、浏览器或前台程序。
- 未执行 41701 领取、发奖或任何账号写事务。
- 因此全部只读叶最多为 `needs-runtime-verify`，领取叶为 `blocked`，不声称真实 Web、玩家可见像素、拖动、即时刷新、重开或性能完成。

## 老端页面与控件事实

- `LevelRewardView`：标题 `_img_title`、提示 `_img_tips/_Label1`、纵向礼包列表 `_list_item_con`；打开即发空包 41700，41700 更新时重排列表。
- `LevelRewardItem`：等级、剩余数量、横向奖励格、领取面、红点、已领图、未达/领完灰态；`received=0/1/2/3/4` 分别覆盖未达、可领、已领、禁用/领完状态。
- `LevelReward`：共享 `EquipmentItem` 奖励格；限量奖励根据余量切换 `_img_limit/_img_mask/_img_draw`。
- 41701 仅 `received=1` 可发送，成功后老端重发 41700、刷新当前行并播放飞奖励；主线 100420 路径还会关闭外壳。
- 老端源码中未发现本页专属 `PlaySoundEffect/PlayFightingVoice/PlaySceneSound` 调用。

## Unity 静态现状

- `Assets/Prefabs/UI/LevelReward/LevelRewardModule.prefab` 已存在并绑定页面专属 `LevelRewardView/LevelRewardItem/LevelReward`，不能重转。
- Prefab 已保存两层 ScrollRect、行模板、奖励模板与全部 Bind 节点。
- `LevelRewardView` 当前明确为空列表降级；`LevelRewardItem` 只写等级/余量占位，领取面只输出日志；`LevelReward` 隐藏共享奖励模板，未绑定真实奖励数据。
- 41700/41701 权威模型与协议已经存在于 `RushGiftModel/RushGiftController`，但 LevelReward 页面尚未消费该链；该共享链在本文件岛外，本轮不修改。

## 组件依赖与状态矩阵

- `EquipmentItem`：LevelReward 每个奖励格的共享物品组件；需验空/有数据、数量 1/多、锁定/未锁、限量可用/领完、详情弹窗。
- `RewardFlyService`/老端 `ShowActFlyReward`：只允许在 41701 权威成功后触发；需验多时点轨迹与关页零残留。
- `ArrowComponent/story finger`：只在主线任务与可领取等级匹配时显示；当前不在 LevelReward 文件岛内实现。

## 静态缺陷与 blocker

1. 主列表不会生成任何行，玩家页为空。
2. 行领取面是假点击日志，不得作为功能接入。
3. 奖励配置、性别分支、限量遮罩与共享物品详情未接入。
4. 41701 是发奖写事务，本轮无授权且明确禁止点击，保持 `blocked`。
5. 入口显隐/红点位于 MainUI/CustomActivity 文件岛外，只读登记。
6. 当前无法验证老端/Unity 同账号真实 Web、滚动、两档 viewport、cold/warm、声音与生命周期，均保持 `needs-runtime-verify`。
