# Claude 任务包：运行态连续击杀实跑验收（第19轮）

## 0. 本轮目标

第18轮 worker 已完成连续击杀驱动代码与待验收报告，但没有实际跑出新的运行态证据：

- `Docs/RuntimeCompare/MainQuest-ContinuousKill-第18轮.md` 当前只能作为设计/待验收记录，不能当闭环报告。
- 第16轮已证明 `59100001 engage` 的 `20001 S2C damage=0`，以及 `59100002 combo` 的 `20001 S2C damage=62/63`、怪物 hp 到 `78/77`。
- 第17轮只跑到 Pump loop，未出现 `hp==0`、death flag、`12006/DeleteSceneObj` 或 `100030` 推进。
- 第18轮新增 `enableRound18ContinuousKill` 与 combo 后继续驱动逻辑，但没有完成真实 Unity 运行态验证。

第19轮只做一件事：实际运行 Unity 真链路或等价 `EditorApplication.update + NetManager.Pump` harness，验证第18轮连续击杀代码是否能让同一会话内的真实服务器 `20001 S2C` 从 hp `140 -> 78/77 -> ... -> 0`，并观察死亡/删除/任务推进。禁止把预期流程、设计表格、源码推断当完成。

## 1. 必读与红线

开工前必须读：

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/RuntimeCompare/MainQuest-ComboActualRun-第16轮.md`
- `Docs/RuntimeCompare/MainQuest-KillAndTaskPush-第17轮.md`
- `Docs/RuntimeCompare/MainQuest-ContinuousKill-第18轮.md`

红线：

- 老端结论只认运行时 `http://127.0.0.1:8090/index.html`、老端源码或服务端源码直接证据。
- 禁止本地伪造 `damage/hp/death/任务进度`。
- 禁止提交 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig.asset` 开关。
- `AppConfig.asset` 可以临时开启取证，但结束前必须还原为默认值，并在报告中贴 `git diff -- Assets/_App/Configs/AppConfig.asset` 为空或仅保留默认序列化字段的证据。
- 本轮不能用“请用户手动执行”作为完成。
- 若实跑环境阻塞，必须记录明确 blocker 和完整命令/日志，不要写成 PASS。

## 2. P0：保护第18轮基线

先完成并记录：

1. `git status --short`
2. `dotnet build yu_client_unity.slnx -v:minimal`，必须 0 错误。
3. 确认 `Assets/_App/Configs/AppConfig.asset` 默认：
   - `devAccount: unity_dev_001`
   - `autoLoginSmokeTest: 0`
   - `autoEnterFirstRoleSmokeTest: 0`
   - `enableRound15ComboTest: 0`
   - `enableRound18ContinuousKill: 0`
4. 复核第18轮代码存在且默认关闭：
   - `AppConfig.enableRound18ContinuousKill`
   - `SceneController.EnableRound18ContinuousKill`
   - `SceneController.OnRound18FightResult`
   - `SceneController.ContinueKillAfterDelayAsync`
   - `FightController.ApplyDefenseListToScene(vo, skillId)`

P0 失败先修 P0，不进入实跑。

## 3. P1：实际启动取证链路

必须使用真实账号链路，不允许 mock：

- 账号：`unity_dev_001`
- 角色：当前同账号可进游戏角色（记录 role_id / 角色名）
- 临时启用：
  - `autoLoginSmokeTest: 1`
  - `autoEnterFirstRoleSmokeTest: 1`
  - `enableRound15ComboTest: 1`
  - `enableRound18ContinuousKill: 1`

允许使用 Unity 编辑期 `EditorApplication.update + NetManager.Pump` 或已有 smoke harness。要求同一会话内记录：

- `10004`
- `GAME_START`
- `12005`
- `12100`
- `12002`
- `MonsterVo`
- `30000/30005` 当前任务状态

若无法启动 Unity 或 Pump，报告必须包含具体命令、退出码、Editor.log 尾部、卡住阶段。

## 4. P2：连续 engage + combo 真实证据

必须记录至少两轮，除非第一轮已经真实死亡：

每轮必须包含：

- engage `59100001` 的 `20001 C2S`
  - 目标怪列表
  - `x/y/angle`
  - 时间戳或日志顺序
- engage `59100001` 的服务器真实 `20001 S2C`
  - defender id
  - hp
  - damage
  - flag
- combo `59100002` 的第二次 `20001 C2S`
  - 目标怪列表
  - `x/y/angle`
  - 时间戳或日志顺序
- combo `59100002` 的服务器真实 `20001 S2C`
  - defender id
  - hp
  - damage
  - flag
- `[Round18] combo 已补发` 与 `[Round18] 继续击杀` 日志是否出现。

如果只出现第一轮 `hp=78/77`，没有第二轮，必须定位是：

- `OnRound18FightResult` 没触发；
- `targetAlive` 判断错误；
- `SceneManager.GetMonster` 找不到；
- 主角距离/寻路卡住；
- `MainRoleAgent` 未装配；
- NetManager/Pump 停止；
- 其他明确原因。

## 5. P3：死亡、删除与任务推进

只接受真实服务器证据：

- `20001 S2C` 中真实 `hp==0` 或服务端死亡 flag；
- `12006 DeleteSceneObj` 或源码确认等价删除协议；
- `SceneManager` 因服务器协议删除真实怪物实例；
- 真实任务推送 `30000/30001/30004/30005` 证明 `100030` 进度变化或推进到后续任务。

若出现死亡但任务不推进，必须记录：

- 死亡怪物 `ins/type_id`；
- 当前任务 id 与目标配置；
- 是否需要击杀多只；
- 是否服务端已推任务但 Unity 未解析/未应用；
- 是否账号当前任务已不是 `100030`。

## 6. P4：报告与提交

更新或重写：

- `Docs/RuntimeCompare/MainQuest-ContinuousKill-第18轮.md`

报告必须把状态改为三选一：

- `PASS`：真实服务器死亡/删除/任务推进至少一项闭合，并有日志证据；
- `BLOCKED`：实际跑了连续攻击，但死亡/删除/任务推进未到达，报告给出完整 C2S/S2C 和 blocker；
- `FAIL`：Unity 代码/解析/状态更新有明确错误，完成最小修复并记录验证。

报告必须包含：

- 执行命令与取证方式；
- `git status --short`；
- `dotnet build` 结果；
- `AppConfig.asset` 还原证据；
- 连续攻击日志摘录；
- 是否出现真实 `hp==0` / death flag / 删除协议；
- 是否出现真实任务推送或 `100030 -> 100040`；
- 若未闭合，下一轮最小 blocker。

提交消息建议：

`[运行态对比/连续击杀实跑] 第19轮: 实跑验证 Round18 连续 combo 击杀与 100030 推进`

## 7. 完成标准

- 不能只提交代码设计、预期表、操作说明。
- 必须有本轮新产生的运行态日志，或明确环境 blocker。
- 结束前临时 `AppConfig.asset` 必须还原，不能把 smoke 开关提交为 1。
- 若本轮仍未闭合，下一轮任务包必须以真实 blocker 为起点，不重复已证伪路径。
