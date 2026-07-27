# 主线大妖副本：入场演出与战斗生命周期（第 24 轮）

## 结论

本轮把主线大妖副本从零散布尔量整理为一条明确的表现状态链：

`Idle → Entering → Intro → Fighting → Settling → Exiting → Idle`

- 进入请求发出前先收脚、清除旧攻击目标并冻结移动/攻击。
- `12005` 切场景期间保持冻结；服务端下发的主线大妖占位怪 `type_id=7001` 且 `boss_type>=3` 真正加入场景后，才播放入场演出。
- 演出结束后才启动任务权重自动战斗。
- `13306` 结算到达后立即收脚并冻结，防止结算面板背后继续跑动/攻击。
- 成功结果页由玩家点击或 10 秒倒计时关闭后发送 `61002`；不再固定 2 秒强退。
- 进入 Exiting 时撤掉本轮任务战斗权重；下一轮进入时再重新武装，避免回野外后沿用已结算任务先跑一步。
- `12005` 回到 `dunId=0` 的野外场景后统一复位。`61002` 失败自动重试一次，仍失败则解除冻结，避免永久锁死。

## 老客户端依据

- `yu_client/h5/src/commonController/BaseDungeonController.ts`
  - 怪物加入场景时检查 `config_mon.boss >= 3`，按副本类型白名单去重后调用 `ShowBossBornEffect`。
  - `ShowBossBornEffect` 先 `SetAutoFight(false)`，再打开 `DungeonFightSceneMaskView`。
- `yu_client/h5/src/dungeon/DungeonFightSceneMaskView.ts`
  - 隐藏 Main/Activity 层。
  - 上下遮罩各用 `0.15s` 滑入，播放 `effect_ui_dayaolaixi`，约 `3s` 后各用 `0.15s` 滑出。
  - 收尾后恢复 UI 层并触发 `STARTAUTOFIGHT`。
- `yu_client/h5/laya/pages/resource/game/dungeonCommon/DungeonFightSceneMaskView.scene`
  - 上下遮罩设计尺寸 `870×670`。
  - 横幅宿主 `2000×1280`、`centerY=-300`。
- `yu_client/cdn/resource/effect/objs/ui_effect/effect_ui_dayaolaixi.lh`
  - 资源内部已有 `0.7` 缩放；老端外部调用仍使用 `scale=1.5`。
- `yu_client/h5/src/scene/fight/FightMovieInfo.ts`
  - 有顶层窗口时战斗表现直接返回，因此横幅期间不会穿插攻击动作。
- `yu_client/h5/src/commonController/AutoBrushController.ts` 与 `AutoBrushResultView.ts`
  - 结算页关闭时才发送 `61002`，成功页自身有 10 秒关闭倒计时。

## Unity 实现边界

### 可编辑 prefab

- `Assets/Prefabs/UI/Scene/BossBornIntro.prefab`
- 生成器：`Assets/Editor/UiCreator/Scene/BossBornIntroCreator.cs`

可以直接在 Prefab Mode 调整：

- `MaskTop` / `MaskBottom` 的尺寸、位置和贴图；
- `BannerHost` 的位置与尺寸；
- `UIEffectSlot._scale`（基线 `1.5`）；
- `BossBornEffectPlayer` 的滑入、停留、滑出时间。

生成器只用于需要重建结构时；日常美术微调直接改 prefab，不要重新生成覆盖手调结果。

在 Play Mode 可直接点 `神霄/特效/播放完整 BossBornIntro`，一次播放上下遮罩和 `effect_ui_dayaolaixi` 的完整组合；也可从“神霄/重构 UI 生成器”找到 `Scene / BossBornIntro(大妖来袭)` 点预览。无需逐个打开粒子。

### 反字修复

共享 `UIEffectStage` 保留老端统一镜像规则，不做全局改动。`UIEffectProfileCatalog.asset` 为
`effect_ui_dayaolaixi` 增加 `dungeon_boss_intro` 差异项，以一次资源级 `mirrorX` 抵消该资源的额外镜像。

### 状态所有权

- `AutoBrushBattleFlow` 是阶段和 `CombatFreeze` 的唯一业务所有者。
- `BossBornEffectPlayer` 只播放 prefab，不直接开关自动战斗。
- `MainRoleAgent.StopForPresentation()` 统一取消在途寻路/跳跃/采集动作并落 idle。
- `SceneCombat.SetClickTarget(0)` 在 Entering、Intro、Settling、Exiting 四个冻结阶段清理旧目标。

## 验收清单

1. 野外进大妖副本前，角色不再先跑一步或转向旧目标。
2. Boss 加入场景后主 HUD 隐藏，上下遮罩滑入；“大妖来袭”文字方向正常。
3. 横幅位置与老端接近，演出过程中角色和 Boss 不攻击、不移动。
4. 横幅滑出并恢复 HUD 后，自动战斗正常开始，不触发 20 秒无输出判负。
5. `13306` 到达瞬间角色收脚；成功结果页背后无继续跑动。
6. 点击完成或倒计时结束才发 `61002`；回野外后任务链继续，角色不继承副本内旧目标。
7. 编辑器中直接打开 prefab 能调整遮罩、横幅宿主、缩放与三个时段。

## 离线验证

- `dotnet build Shenxiao.Module.Core.csproj --no-restore`：0 error。
- `dotnet build Shenxiao.Editor.csproj --no-restore`：0 error。
- prefab 脚本 GUID、遮罩 Sprite GUID、`UIEffectSlot` 引用和差异表 ID 做静态一致性检查。

## 2026-07-27 补充：不播放与进场折返的根因修复

### 症状与证据

- 用户重新生成 `BossBornIntro.prefab` 后，进入副本仍没有演出；Unity 日志同时出现 `BossBornEffectFlow is abstract`、`missing the class attribute ExtensionOfNativeClass`、`prefab 缺少 BossBornEffectPlayer`。
- 原实现把静态类 `BossBornEffectFlow` 和可挂载组件 `BossBornEffectPlayer` 放在同一个 `BossBornEffectFlow.cs`。该文件的 `.meta` GUID 只能映射到文件主类型，生成器虽然调用了 `AddComponent<BossBornEffectPlayer>()`，Prefab 最终仍会被 Unity 修成 `m_Script: {fileID: 0}`。这是 Unity 资产绑定失败，不是粒子资源或触发条件错误。
- 活服日志在切副本前锁定普通怪 `ins=1154 type=10001004`，随后 `12005` 给出新落点 `(8076,945)`，副本 `12002` 快照只有一个怪，且该怪是 `type=7001` 的大妖。说明副本里没有残留小怪；折返来自 `MainRoleAgent` 内部仍保留上一场景的浮点坐标/自动接近回调，以及任务战斗在副本内仍允许回退选择任意可攻击怪。

### 最终处理

- `BossBornEffectPlayer` 拆到同名独立脚本，`BossBornIntro.prefab` 的 `m_Script` 改为该脚本自己的非零 GUID；生成器以后重建也会得到正确绑定。
- 新增 `神霄/特效/播放完整 BossBornIntro`：进入 Play Mode 且 UI Top 层初始化后，一键实例化并播放完整 Prefab；缺 Prefab、缺 Top 层或缺播放器都会明确弹窗，不再静默黑屏/无响应。
- `12005` 写入服务端权威坐标后，立即调用 `MainRoleAgent.ApplyAuthoritativeScenePosition`，废弃上一场景的自动接近、动作续体和内部坐标，不回发多余 `12001`。
- 大妖实体加入时把本次 Boss `instanceId` 交给 `AutoBrushBattleFlow`；演出结束解冻之前先锁定这个实例。主线大妖任务在 `dunId!=0` 时只接受 Boss 目标，不再回退到普通怪；野外刷小怪以推进大妖次数的原逻辑保持不变。
- `MonsterRenderer.OnMonsterAdded` 在第一个异步配置加载后校验场景 epoch 与实体引用，防止上一场景的在途异步任务在清场后继续创建旧怪视图。

### 验收与防回归

1. 进入 Play Mode 并到主界面，点 `神霄/特效/播放完整 BossBornIntro`，应一次看到上下遮罩与“大妖来袭”完整演出。
2. 连续进入第二个大妖副本：`12005` 后角色保持新落点；演出期间冻结；演出结束后第一目标必须是日志中的 `battle boss bound ins=...` / `boss target locked exact ...`，不能再锁野外普通怪。
3. Prefab 静态检查必须满足 `m_Script.fileID=11500000` 且 GUID 指向 `BossBornEffectPlayer.cs.meta`；禁止再把可挂载组件与不同名静态主类型放在同一个 `.cs`。
4. 本轮离线验证：`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 均为 0 error；活服视觉与第二只大妖的移动链由 Unity 操作复验。
