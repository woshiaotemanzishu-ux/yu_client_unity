# NPC 对话 / 任务交互静态审计（2026-08-09）

## 边界与权威

- 路线：`scene.dialogue.npc-task`；schema 6 共 56 节点。
- 老端最终表现以 `DialogueView.ts`、`DialogueController.ts` 在同账号、同状态、同 viewport 的真实运行结果为唯一权威；本轮没有启动 Unity/浏览器，也没有点击会触发 `30003/30004/30007` 的任务写动作。
- 已有 `DialogueModule.prefab` 与 `DialogueViewBind`，因此只走 `fix-view` 增量修复；没有重转模块、没有运行 Creator、没有改 Prefab/Generated/Common/MainUI/Addressables。

## 控件与条件分支

- 全屏背景/点击面：老端 0.75 遮罩，婚宴播放态无背景且不可点击跳过。
- 说话者：NPC、当前角色、内容块 `id` 覆盖 NPC、系统旁白、婚宴新人/司仪；模型切说话者时带入场动画，NPC 还会在 `casual/idle` 间恢复与重播。
- 底部对话：姓名、正文、多段继续、跳过、自动倒计时。
- 完成/奖励：完成正文、共享 `EquipmentItem` 奖励格、任务动作和倒计时。
- 入口与事务：`12101` NPC 任务列表、`12102` 任务对话；动作叶为 `30003` 接取、`30004` 完成、`30007` 对话事件，并须等待 `30000/30001` 权威刷新及关闭重开一致性。
- 生命周期：停止自动任务、切场景迟到回包、模型/奖励异步失效、加载失败重试、冷/热打开和两档真实 Web。

## 本轮静态修复

1. 对话由 Unity `Tip` 层承接老端 `Message` 层，并隐藏/恢复 `Main + Popup(Activity)`；不再把对话自身放进 Popup 后错误隐藏 Window。
2. 复用 Prefab 已绑定且铺满屏幕的 `_img_bg` 作为唯一点击面，删除运行时给人工 Prefab 根节点追加 `Image` 的做法；其余 Graphic 只显示、不重复响应同一点击。
3. Prefab/Bind 缺失时释放残根；异步加载失败会清除失败 Task，后续打开可重试。
4. 同一内容块有多个动作节点时执行老端最终覆盖的最后一项；补入 `TRIGGER_AND_FINISH` 动作分类，并恢复该分支“完成任务”按钮语义。
5. 接入 `EVT_SCENE_OBJECTS_CLEARED`：先置 `ChangeSceneClose`、关闭对话并拒绝迟到 `12102`；进游戏状态 Reset 提前到配置 await 之前，避免迟到 Reset 覆盖切场景闸。配置等待期间切场景也会取消待发送的 `12101`。

## 明确阻塞

- 当前 Unity 仍把所有正文说话者按基础 `NpcId` 展示；老端的 ROLE 主角模型、内容块 `id` 覆盖、99999 系统旁白、婚宴新人/司仪身份和婚宴禁止跳过尚未接入。
- 老端底栏 80ms、模型 120ms 入场动画，NPC `casual → idle → 6s replay` 与场景 NPC 动作联动尚未复刻。
- `STOP_AUTO_DO_TASK` 对话关闭事件在当前 `GlobalEvent` 没有等价项；需与 Task/AutoFight 共享停止语义统一落地，不能私造页面事件。
- 任务写动作没有本轮授权，故 `30003/30004/30007`、自动倒计时触发写、成功/失败即时刷新与重开全部 blocked。
- 真实 Prefab 点击、0.75 像素、模型 RenderTexture、奖励格、两档 viewport、冷/热耗时、切场景和 same-build Web 尚未运行，静态存在不能升级为 `done`。

## 验证分层

- `route_ledger.py init/validate`：schema 6 / 56 nodes。
- `verify-static.ps1`：检查层级映射、Prefab 点击面、无运行时根 Image、最后动作节点、`TRIGGER_AND_FINISH`、切场景闸和失败重试。
- `git diff --check`：Dialogue 专属代码与审计目录通过。
- `Dialogue.Isolated.csproj` 显式纳入本轮三个 Dialogue 改动文件并引用当前 Framework/Generated/Common/Core：0 warning / 0 error。
- 完整 `Shenxiao.Module.Core.csproj` 当前只被 Unity 尚未刷新进生成 csproj 的并发新类 `GuildHelpRuntime/GuildJoinRuntime/ShopBulkPurchaseView` 阻断；三条对应子路线均已用显式 targets 隔离编译通过，不是 Dialogue 产品错误。待 Unity 获授权刷新或最终统一生成源列表后再跑整 Core。
