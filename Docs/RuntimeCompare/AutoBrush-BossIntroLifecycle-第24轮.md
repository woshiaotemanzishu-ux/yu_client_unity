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

在 Play Mode 可从“神霄/重构 UI 生成器”找到 `Scene / BossBornIntro(大妖来袭)` 点预览；也可把 prefab 临时拖入任意 Canvas，进入 Play Mode 自动播放。

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
