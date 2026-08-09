# Jjc / Arena 静态三方调和

## 本轮结论

- 当前玩家入口仍调用 `Assets/Scripts/Module/Core/Jjc/JjcShellView.cs` 的 `JjcShellView(TempShell)` 代码壳；它只显示排名/次数/对手文本和首个挑战按钮。
- `Assets/Prefabs/UI/Arena/ArenaModule.prefab` 已存在，含 12 个 Arena View/Item Bind，但当前 Jjc 生产代码没有加载该 Prefab，也没有消费这些 Bind。完整 Prefab 接管会同时触及入口、战斗场景、Rank/MainUI 和 Common 共享组件，超出本轮唯一文件岛；因此只登记 defect/blocker，不做“可见但不可用”的半接管。
- Unity 已实现 28000/28001/28002/28003/28004/28009/28010/28013/28014 接收切片；其中 28001 与 28004 保持独立快照，GAME_START 顺序为 28004→28001，28003 后顺序为 28004→28002。
- 老端与服务端共同确认：28003 是真实挑战；28005 会扣货币购买次数；28017 会领取突破奖励；28012/28015/28018 属于战斗场景生命周期。按本轮禁止账号写事务、禁止真实挑战/购买/领取/Unity/Web，均未执行。
- 28008 在当前服务端 `pp_jjc.erl` 已注释，按既有 hard-negative/kill 结论不补 UI 发送器。

## 静态指纹

- Git HEAD：`92b3f5578a90befb85a6255157f08e482214aa1a`
- `JjcController.cs`：`83f61c364dbb26cdb158a55a225532263a1e1752cda59db03444d481ca1dfabb`
- `JjcModel.cs`：`182740122b8ebaa645914f814cdb269e8c574d7ede3dc543a82bc6a10b1cf4f5`
- `JjcShellView.cs`：`d5517b03bddb43205889d0c0da395bfb959ac3c46c670f7e59a9280a0cb2c89a`
- `ArenaModule.prefab`：`f17546bfe88346d37182559b46b4b4a0bee44e665e707c8f6e47a0f336414f30`
- `ArenaRankRewardItem.prefab`：`94ca1bb2c8d65f6ea306121524b47b94db07f9eb12662ff37bc8772e500108e3`
- 老端 `ArenaController.ts`：`d27305878ae59bff6802f31990ea4aaef382d17c58b2394ef1c77fa6efc96255`
- 老端 `ArenaModel.ts`：`33534fd1f4b83b29db84c9c380a37e65b62ce0312ca61076c2bad8869cafed6f`
- 服务端 `pt_280.erl`：`38430a5fb070501ca9a7b7e78700867ddb4f29dd3524c1b44e5512bc8fde92b7`
- 服务端 `pp_jjc.erl`：`2efef152254c7184b165ff2a4c6a2b55277589537b486f51a1c8497e0ac5ce54`
- Arena 专属资源文件：176（含 `.meta`）；Generated Arena Bind：12。

## Prefab/Bind 结构

- 顶层：`ArenaEnterView`、`ArenaBattleRecordView`、`ArenaBuyTimesView`、`ArenaRankRewardMainView`、`ArenaRankBreachRewardView`、`ArenaRankRewardView`、`ArenaFightSceneView`、`ArenaResultView`。
- 模板：`ArenaEnterRoleItem`、`ArenaBattleRecordItem`、`ArenaRankTabItem`、`ArenaRankRewardItem`。
- 主页 Bind 覆盖刷新、排行、排行奖励、记录、次数加号、红点、光环、对手 ScrollRect/Content/模板；其他 Bind 覆盖双排行页签、购买弹窗、记录列表、战斗 HUD 和结果弹窗。
- Prefab 默认 active 状态中 `ArenaBattleRecordView=1`，其余顶层业务 View 多为 0；由于 Prefab 未被玩家入口加载，本轮不把该静态值解释为运行缺陷，也不改 YAML。

## 配置与协议

- `config_jjc_config`：key1 最大挑战次数 10、key4 最低排名 3000、key11 场景 24001、key13 准备 3 秒、key14 战斗 30 秒、key16 结算 10 秒、key17 次数恢复 3600 秒、key18 刷新冷却 1 秒。
- `config_jjc_rank_reward`：11 档；`config_jjc_reward_break`：10 档；`config_jjc_buy_num`：20 档。
- `pt_280.erl` 的 28001/02/03/04/09/10/13/14 字段顺序与当前 Unity 解码静态一致；28003 的 `reward_list`/`break_reward_list` 当前 Unity 只读过未留存，完整 ResultView 因而仍是 defect 范围。

## 真实运行缺口

本轮没有启动 Unity、浏览器或 Computer Use，也没有真实 Web 包和同账号老 H5 运行基线。因此以下全部保持 `needs-runtime-verify` 或 `blocked`：真实 Web、双 viewport、old/unity/overlay/diff、滚动拖动/裁剪/末项、3D 模型、特效/动画双帧、cold/warm、协议回包后的即时刷新、关闭重开、战斗场景返回链和性能。

## 文档说明

本轮用户明确把 `Docs` 与 `AGENTS` 列为禁写区；仅新增 Jjc 专属审计 output，不更新项目文档。
