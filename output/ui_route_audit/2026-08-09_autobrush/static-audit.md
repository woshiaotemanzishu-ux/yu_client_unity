# AutoBrush / 斩妖静态精修审计（2026-08-09）

## 本轮边界

- 按 `audit-game-ui-route` schema 6 枚举 MainUI 固定入口、斩妖主窗、排行榜、结算窗、协议、共享依赖和生命周期。
- Unity/浏览器/真实账号写事务均未运行，因此不声明真实像素、交互、性能或写事务完成。
- 唯一实现岛为 `Assets/Scripts/Module/Core/AutoBrush/**`；MainUI/Task/OnHook/Common/Generated/Addressables/Docs 均只读。

## 老端事实与 Unity 对账

- 固定入口必须同时满足 `FuncOpenConfig.CheckFuncOpenState("AutoBrush")` 与 `config_scene.type == 1`。Unity 现有 `MainUIAutoBrushView` 已按该双门禁实现，并在任务、角色等级、场景 ready 时复算。
- HUD 的 `ui_mainDungeon` 仅在 `NeedTimes > 0 && CurrentTimes == NeedTimes` 显示；现有 `ChallengeEffectSlot` 有异步版本取消和隐藏/销毁清理。该文件属于禁改 MainUI，本轮只审计。
- AutoBrushModule 已保存 Main/Rank/RankItem/Result 四个 Prefab Bind。原 Unity 仍把排行榜点击写成日志占位，13301 又把逐条榜单读完后丢弃；这是本轮已修复的 AutoBrush 专属确定性缺口。
- 当前主窗挑战、前往刷怪、阶段领取、结算退出的协议链已存在，但均会改变角色/副本/奖励状态，未授权执行，保持 `blocked`。
- 公会协助需要 Guild 的开放条件、40401 请求和 40403 取消链。Guild 是禁改跨岛，未复制或猜测实现，保持 `blocked`。

## 本轮实现

- `AutoBrushModel` 新增不可变 `RankEntry` 与完整榜单快照。
- 13301 保留 `server_id/server_num/role_id/role_name/rank/level/combat` 全字段，不再只留榜首摘要。
- 主窗和排行榜每次打开均发送只读 13301 刷新。
- 新增 AutoBrush-owned `AutoBrushRankView`，复用现有 RankView/RankItem Bind，覆盖空榜、我的排名/关数、Top3/普通名次、跨服名、列表裁切与回顶、关闭返回。
- 主窗挑战入口补齐激活任务 `100811` 屏蔽、自动斩妖状态与关卡战力三重红点条件；不再把“进度已满”直接等同于红点。
- 主窗阶段奖励宿主直接保存 `UI_partner_skillicon_01` 的 `UIEffectSlot`，只在可领取时加载，并对隐藏、领取后刷新和异步后到统一清理。
- 阶段奖励图标改为读取 `config_enchantment_guard_stage_reward` 的当前/最终 gate，再经 `GoodsModel` 映射真实 goodsIcon；异步图标用版本号拒绝旧 gate 后到覆盖。
- `AutoBrushFlow` 接入排行榜，并在 `EVT_GAME_START` 释放旧模块根，避免重进沿用旧榜单/克隆节点。

## 静态/运行闸结论

- 静态源/Prefab/协议断言与隔离编译结果见同目录脚本和日志。
- schema 6 正式账共 65 节点：`blocked=19`、`needs-runtime-verify=46`、`not-run=0`；57 个叶子中 `blocked=12`、`needs-runtime-verify=45`。
- 所有可运行叶统一保持 `needs-runtime-verify`，所有账号/副本/奖励/公会写链与未实现跨岛协助保持 `blocked`。
- 真实收口仍需同账号旧 H5 与当前 Unity Web 顺序复走：两档 viewport、HUD 双门禁切换、进度未满/刚满、排行榜空/长榜、挑战结果、即时刷新、关闭重开、效果两时点像素及清理。
