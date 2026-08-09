# Dungeon UI 静态路线审计（schema 6）

## 结论

本路线已按现有 Prefab 走 fix-view。共枚举 93 个节点、77 个叶；叶结果为 63 个 blocked、14 个 needs-runtime-verify、0 done；含派生父节点的正式 ledger 为 79 个 blocked、14 个 needs-runtime-verify。未把静态编译或 Prefab 绑定写成真实 Unity/Web 通过。

## 文件岛与事实源

- 唯一写者：`Assets/Scripts/Module/Core/Dungeon/**`、`Assets/Prefabs/UI/DungeonRune/DungeonRuneModule.prefab`、本 output 目录。
- DungeonCommon：`Assets/Prefabs/UI/DungeonCommon/DungeonCommonModule.prefab`（只读，本轮沿用既有 `DungeonBuyTimeView` / `DungeonResultView` 消费链）。
- DungeonRune：`Assets/Prefabs/UI/DungeonRune/DungeonRuneModule.prefab`（已有可编辑 Prefab，增量接管，未重转）。
- Generated Bind 只读；老端对照：`E:/GitProject/yu_client/h5/src/dungeon/*.ts` 与 `dungeonRune/*.ts`。

## 本轮静态确定修复

- 删除 `DungeonRuneShellView` 的运行时代码建树，改为加载真实 `DungeonRuneModule.prefab`。
- Prefab 精确把 Enter、EnterItem、Target、TargetItem、DailyReward 5 个 Generated Bind GUID 替换为 data-only 业务 subclass，并同步 EditorClassIdentifier。
- 入口只读请求 61020 / 61113 / 61115；61113、61115 回包补发既有 `EVT_DUNGEON_UPDATE`，支持当前页即时刷新。
- 61020 列表按真实 `dun_id` 与 `config_dungeon` 名称显示；通过/锁定态没有老端 `rec_data key=5` 时同时隐藏，未用 PermanentCount/DailyCount 猜映射。
- 每日入口按 61115 `daily_status=0/1/2` 复刻提示/打开分支；`unlock_level` 不再冒充当前 floor，弹窗推荐战力红点和目标描述在缺权威配置时安全隐藏。61112/61114/61116、首杀、奖励内容、解锁配置、推荐战力、模型/特效均保持 blocked。
- 模板缺业务组件时 Error + skip；没有 AddComponent fallback。

## 完整叶覆盖

- `core.dungeon.common.buy.content` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.buy.close` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.buy.cancel` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.buy.confirm` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.common.buy.vip-add` — blocked: 依赖 VIP/分享/Activity/强化/推送礼包等禁区或跨模块入口，仅登记 blocker。
- `core.dungeon.common.victory.background-close` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.victory.stars` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.victory.reward-list` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.victory.exp` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.victory.layer` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.victory.equip-reward` — blocked: 奖励内容/领取需要专属配置与 61112/61114 回包；本轮只接只读 61113/61115 原始状态，不伪造奖励。
- `core.dungeon.common.victory.kill` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.victory.copper` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.victory.treasure-effect` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.victory.timer` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.victory.left-action` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.common.victory.right-action` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.common.victory.share` — blocked: 依赖 VIP/分享/Activity/强化/推送礼包等禁区或跨模块入口，仅登记 blocker。
- `core.dungeon.common.failure.background-close` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.failure.close` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.common.failure.go` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.failure.goods-list` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.failure.strength-list` — blocked: 依赖 VIP/分享/Activity/强化/推送礼包等禁区或跨模块入口，仅登记 blocker。
- `core.dungeon.common.failure.activity` — blocked: 依赖 VIP/分享/Activity/强化/推送礼包等禁区或跨模块入口，仅登记 blocker。
- `core.dungeon.common.fight.leave` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.common.fight.countdown` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.start-countdown` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.floor` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.exit-countdown` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.guard` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.guild-help` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.score` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.mainline-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.marriage-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.heart-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.copper-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.rein-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.kf-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.dragon-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight.exp-subview` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight-mask.born-effect` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.fight-mask.top-bottom-mask` — blocked: 依赖战斗场景、场景管理、计时/波次/模型或跨岛子面板；均不在本文件岛授权范围。
- `core.dungeon.common.guild-help.head` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.guild-help.name-title` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.result-effect.effect` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.common.award-item.award-content` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.rune.enter.floor-list` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.rune.enter.floor-item-state` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.rune.enter.background-list` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.rune.enter.line-list` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.rune.enter.unlock` — blocked: 依赖 config_dungeon_rune_lv_data、解锁配置/特效和 61116 写事务；当前闭包不完整。
- `core.dungeon.rune.enter.challenge` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.rune.enter.daily-open` — blocked: 已按 61115 daily_status 静态接入 0/1/2 分支，但弹窗内当前 floor、推荐战力、奖励内容与领取事务仍缺权威闭包，不能标静态完成。
- `core.dungeon.rune.enter.first-open` — blocked: 依赖 FirstBlood 模块、首杀配置与回包，属于跨岛依赖，本岛不猜映射。
- `core.dungeon.rune.enter.target-open` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.rune.enter.reward-preview` — blocked: 奖励内容/领取需要专属配置与 61112/61114 回包；本轮只接只读 61113/61115 原始状态，不伪造奖励。
- `core.dungeon.rune.enter.gift` — blocked: 依赖 VIP/分享/Activity/强化/推送礼包等禁区或跨模块入口，仅登记 blocker。
- `core.dungeon.rune.enter.red-state` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.rune.enter.return-chain` — blocked: 缺真实 Unity/Web 同路运行证据，当前静态审计不能替代玩家可见表现。
- `core.dungeon.rune.daily.close` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.rune.daily.continue` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.rune.daily.claim` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.rune.daily.reward-list` — blocked: 奖励内容/领取需要专属配置与 61112/61114 回包；本轮只接只读 61113/61115 原始状态，不伪造奖励。
- `core.dungeon.rune.daily.status` — blocked: 61115 unlock_level 不是老端弹窗当前 floor；推荐战力比较与 floor 权威字段缺失，相关控件已安全隐藏。
- `core.dungeon.rune.first.close` — blocked: 依赖 FirstBlood 模块、首杀配置与回包，属于跨岛依赖，本岛不猜映射。
- `core.dungeon.rune.first.tabs` — blocked: 依赖 FirstBlood 模块、首杀配置与回包，属于跨岛依赖，本岛不猜映射。
- `core.dungeon.rune.first.rank-list` — blocked: 依赖 FirstBlood 模块、首杀配置与回包，属于跨岛依赖，本岛不猜映射。
- `core.dungeon.rune.first.reward-list` — blocked: 依赖 FirstBlood 模块、首杀配置与回包，属于跨岛依赖，本岛不猜映射。
- `core.dungeon.rune.first.claim` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.rune.unlock.close` — blocked: 依赖 config_dungeon_rune_lv_data、解锁配置/特效和 61116 写事务；当前闭包不完整。
- `core.dungeon.rune.unlock.rune-icon` — blocked: 依赖 config_dungeon_rune_lv_data、解锁配置/特效和 61116 写事务；当前闭包不完整。
- `core.dungeon.rune.unlock.name-tips` — blocked: 依赖 config_dungeon_rune_lv_data、解锁配置/特效和 61116 写事务；当前闭包不完整。
- `core.dungeon.rune.target.close` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.rune.target.one-key-claim` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。
- `core.dungeon.rune.target.target-list` — needs-runtime-verify: 代码/Prefab 静态链已落位；按本轮约束未启动 Unity/真实 Web，仍需同账号运行态验证点击、数据、布局、滚动、关闭重开与即时刷新。
- `core.dungeon.rune.target.target-item-state` — blocked: 61113 只有 dun_id/reward_type/reward_status；缺老端专属配置 desc 与奖励内容，不能用协议原始值拼玩家文案。
- `core.dungeon.rune.target.item-claim` — blocked: 该叶为正式协议/领取/挑战/退出写事务；本轮禁止账号写，且需成功回包、即时刷新与重开证据。

## 明确边界

未启动/控制 Unity、浏览器或前台程序，未执行账号写、GM、挑战、领取、退出等事务；未构建全 Core、Unity 或 WebGL。战斗场景、MainLine/Marriage/Heart/Dragon/Exp/Copper/Rein/KF 子面板、FirstBlood、VIP、Activity、Common 奖励配置消费者等跨岛依赖只登记 blocker。
