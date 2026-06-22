# Claude 任务包：运行态连续击杀与任务推进（第18轮）

## 0. 本轮目标

第16轮已经闭合 combo 正伤害：`59100002` 的服务器真实 `20001 S2C damage=62/63`，怪物 hp 从 `140` 降到 `78/77`。

第17轮没有闭合击杀与任务推进：worker 只启动了 Pump loop，未提交报告；Codex 已补交 `Docs/RuntimeCompare/MainQuest-KillAndTaskPush-第17轮.md`，结论为 BLOCKED。

第18轮只做一个目标：在真实运行链路里连续触发 `59100001 engage + 59100002 combo`，直到拿到服务器真实死亡/删除/任务推进，或留下明确 blocker。禁止扩散到特效、完整 AI、掉落、伙伴、神祇、活动副本。

## 1. 必读与红线

开工前必须读：

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/RuntimeCompare/MainQuest-ComboActualRun-第16轮.md`
- `Docs/RuntimeCompare/MainQuest-KillAndTaskPush-第17轮.md`

红线：

- 老端结论只认运行时 `http://127.0.0.1:8090/index.html`、老端源码或服务端源码直接证据。
- 禁止本地伪造 `damage/hp/death/任务进度`。
- 禁止提交 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig.asset`。
- `AppConfig.asset` 可以临时取证，但提交前必须还原，并在报告中贴 `git diff -- Assets/_App/Configs/AppConfig.asset` 为空的证据。
- 本轮不能用“请用户手动执行”作为完成。

## 2. P0：保护第17轮基线

必须先完成并记录：

1. `git status --short`
2. `dotnet build yu_client_unity.slnx -v:minimal`，必须 0 错误。
3. `Assets/_App/Configs/AppConfig.asset` 默认必须为：
   - `devAccount: unity_dev_001`
   - `autoLoginSmokeTest: 0`
   - `autoEnterFirstRoleSmokeTest: 0`
   - `enableRound15ComboTest: 0`
4. 复述第17轮报告中真实证据：
   - `59100001` 的 `20001 S2C damage=0 hp=140 flag=0`
   - `59100002` 的 `20001 S2C damage=62/63 hp=78/77 flag=0`
   - 第17轮未出现 `hp==0/death/delete/100030`。

P0 失败时先修 P0，不进入击杀验证。

## 3. P1：修正实跑 harness，而不是造业务结果

优先复用第15/16轮已证明能进游戏、同步 MonsterVo、发真实 20001 的路径。

允许做的最小修改：

- 让取证 harness 在同一会话里等待场景、主角、怪物和距离条件稳定后再发攻击。
- 若目标距离超过技能范围，必须走真实 `MainRoleAgent/MoveToNpc/StartTargetAction` 等价链路并等待到达；如果主角组件缺失，记录 blocker，不假位移。
- 攻击后从服务器 `20001 S2C` 更新的真实 hp 中重新选择仍存活怪物，继续下一轮。
- 控制循环次数和时间，避免无限 Pump；建议最多 5 轮攻击或 90 秒。
- 增加最小审计日志，能区分每轮 engage C2S、combo C2S、服务器 S2C、防守列表、hp/flag。

禁止：

- 本地扣血；
- 本地删除怪物；
- 本地任务完成；
- 改协议号或猜包；
- 为了通过测试改真实用户账号。

## 4. P2：连续真实攻击取证

必须在同一会话记录：

- 登录账号、角色名、role_id。
- `10004/GAME_START/12005/12100/12002/MonsterVo` 到达。
- 每轮 engage `59100001` 的 `20001 C2S`：
  - 目标列表、x/y、angle、时间戳。
- 每轮 combo `59100002` 的第二次 `20001 C2S`：
  - 目标列表、x/y、angle、时间戳。
- 每轮服务器真实 `20001 S2C`：
  - skill、defender id、hp、damage、flag、pos。
- 若 hp 下降但未死亡，继续下一轮；若后续不能继续，记录最后一条真实 C2S/S2C 和阻塞原因。

## 5. P3：死亡、删除、任务推进验收

只接受以下真实证据：

- `20001 S2C` 中真实 `hp==0` 或服务器死亡 flag；
- 真实删除协议，如 `12006 DeleteSceneObj` 或代码确认的等价协议；
- `SceneManager` 因服务器协议删除真实怪物实例；
- 真实任务推送：`30000/30001/30004/30005` 能证明 `100030` 进度变化或推进到后续任务。

如果死亡发生但任务不推进，必须记录：

- 死亡怪物 id/type 与任务目标是否一致；
- 当前任务 id、目标数量、服务器下发任务状态；
- 是否需要继续击杀多只怪；
- 是否出现任务协议但 Unity 未解析或未应用。

## 6. P4：报告与提交

必须建立：

- `Docs/RuntimeCompare/MainQuest-ContinuousKill-第18轮.md`

报告必须包含：

- 本轮结论：`PASS` / `BLOCKED` / `FAIL`
- 执行命令和取证方式
- `git status --short`
- `AppConfig.asset` 已还原证据
- `dotnet build` 结果
- 连续攻击日志摘录
- 是否出现真实 `hp==0` / death flag / 删除协议
- 是否出现真实任务推送或 `100030 -> 100040`
- 若未闭合，下一轮最小 blocker

提交消息建议：

`[运行态对比/连续击杀] 第18轮: 连续combo击杀与100030任务推进实跑`

## 7. 完成标准

- `PASS`：真实服务器日志证明连续 combo 后出现死亡/删除对象/任务推进之一，并且 Unity 正确应用。
- `BLOCKED`：真实跑到正伤害和连续攻击尝试，但死亡/删除/任务推送没有到达，报告包含完整 C2S/S2C 和连接状态。
- `FAIL`：发现 Unity 解析、分发、移动或状态更新明确错误，并完成最小修复或留下准确下一步。
