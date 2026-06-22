# Claude 任务包：运行态 combo 副技能实跑验收（第16轮）

## 0. 本轮目标

把第15轮留下的“需手动执行”补成真实运行态证据：在 Unity 真实登录、进游戏、进含怪场景后，用第14/15轮已经落地的 `SkillConfigs.GetComboNext` + `SceneCombat` combo 补发链，抓到同一会话内的：

1. engage 普攻 `59100001` 的 `20001 C2S`。
2. combo 副技能 `59100002` 的第二次 `20001 C2S`。
3. 服务端真实 `20001 S2C` 中 combo 副技能对应 `damage > 0` 或明确证明仍未出正伤害的日志级 blocker。
4. 若出现真实死亡或任务推送，再验证 `100030` 击杀进度推进；没有真实死亡/任务推送时禁止本地伪造进度。

本轮不是继续写“预期可行”的脚手架。成功标准只有两类：真实闭环证据，或可复现的真实阻塞证据。

## 1. 必读与红线

开工前必须读：

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`
- `Docs/Shenxiao重构实施方案.md`
- `Docs/Shenxiao进游戏链路.md`
- `Docs/Claude任务包-运行态combo副技能真连闭环-第15轮.md`
- `Docs/RuntimeCompare/MainQuest-ComboDamageClosure-第15轮.md`
- `Docs/RuntimeCompare/MainQuest-FightMovieDamage-第14轮.md`

红线：

- 老端结论只认运行态 `http://127.0.0.1:8090/index.html` 或老端源码/服务端源码的直接证据，不把静态 `.scene` 当最终真相。
- 禁止本地伪造 `damage/hp/death/任务进度`。
- 禁止用 cube/默认球/假怪物、假协议、假账号属性来证明战斗闭环。
- 禁止把“请用户手动执行”当作本轮完成。
- 禁止提交 `.playwright-cli/`、`output/`、字体 SDF、`Generated/Bind`、临时 `AppConfig` 开关。
- `AppConfig.asset` 可以为取证临时改，但提交前必须还原，并在报告里贴 `git diff Assets/_App/Configs/AppConfig.asset` 为空的证据。

## 2. 当前已确认基线

- 第14轮结果提交：`d260841d0`
  - 定案：`59100001 is_att=0/calc=0` 只是 engage 帧，服务端回 `damage=0`。
  - 真伤害走 combo 副技能 `59100002 is_att=1/calc=1`。
  - Unity 已落地配置驱动 combo 补发：`SkillConfigs.GetComboNext` + `SceneCombat.ScheduleComboFollowUp`。
- 第15轮任务包提交：`4c438b98f`
  - 修复 `.gitattributes` 后，`SceneCombat.cs` / `SkillConfigs.cs` 已是可审阅文本 diff。
- 第15轮结果提交：`c8a4e3da8`
  - 新增 `enableRound15ComboTest`、`LoginBootstrap` smoke 开关、`SceneController` 在 `12002` 后自动驱动普攻。
  - `AppConfig.asset` 当前默认值应为 `enableRound15ComboTest: 0`。
  - 第15轮报告明确未完成实际运行验证：engage/combo/hp/death 仍待实跑。
- Codex 复核：`dotnet build yu_client_unity.slnx -v:minimal` 通过，当前为 6 个既有警告、0 错误。

## 3. P0：保护基线

必须先完成并记录：

1. `git status --short`，只能允许既有未跟踪 `.playwright-cli/`、`output/` 或其他明确无关忽略项存在。
2. `git show --numstat d260841d0 -- Assets/Scripts/Module/Core/Scene/SceneCombat.cs Assets/Scripts/Module/Core/Skill/SkillConfigs.cs` 必须显示文本增删，不是 `Bin`。
3. `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错误；警告只记录，不扩散修复。
4. 确认 `Assets/_App/Configs/AppConfig.asset` 初始 `enableRound15ComboTest: 0`。

若 P0 失败，先修 P0。不要进入实跑。

## 4. P1：实跑方式

优先用已有第15轮游戏代码驱动，不再新增一套驱动。

推荐路径：

1. 临时把 `Assets/_App/Configs/AppConfig.asset` 改成：
   - `devAccount: unity_npc_475823114`
   - `autoLoginSmokeTest: 1`
   - `autoEnterFirstRoleSmokeTest: 1`
   - `enableRound15ComboTest: 1`
2. 启动 Unity Editor Play 或可等价触发 `RuntimeInitializeOnLoadMethod` 的真实运行路径。
3. 若 Play 态 `Unity_RunCommand` 文件刷新仍失效，复用第13轮可行方案：编辑期 `EditorApplication.update` 一次性 harness + 手动 `NetManager.Pump()`，但 harness 只能作为临时运行态取证，不入库。
4. 必须尽量在测试服短会话窗口内完成：登录、选角、`10004`、GAME_START 门闩、`12005`、`12100`、`12002`、怪物同步、自动驱动普攻。
5. 取证完成后 Stop/清理，并还原 `AppConfig.asset`。

如果 Unity MCP / RunCommand / Editor.log 获取失败，不要笼统写“环境问题”。必须记录：

- 具体工具/命令。
- 失败时间。
- stdout/stderr 或 Editor.log 末尾。
- 是否还能读 Editor 状态、Console、进程、日志文件。
- 是否已尝试 fallback（Editor.log tail、编辑期 update harness、手动 `NetManager.Pump`）。

## 5. P2：必须采集的证据

报告 `Docs/RuntimeCompare/MainQuest-ComboActualRun-第16轮.md` 必须包含同一会话内的日志或截图证据：

### 5.1 进场景与怪物

- 登录账号、角色名/role_id。
- `10004` 进游戏成功。
- GAME_START 门闩完成。
- `12005` 进场景成功。
- `12100` NPC 列表完成并请求 `12002`。
- `12002` 或 `12007/12012` 后 `SceneManager.MonsterCount > 0`。
- 至少一个真实 `MonsterVo`：instance id、type/id、hp/hpLim、pos、canAttack、isCollect。

### 5.2 两次 20001 C2S

必须证明出现两次真实发包：

- 第一次：`SendMainSkillAttack(59100001)`，记录目标列表、x/y、angle、时间戳。
- 第二次：`combo 副技能补发 engage=59100001 → combo=59100002`，随后 `SendMainSkillAttack(59100002)`，记录同样字段。

如果第二次没有出现，定位是：

- `SkillConfigs.GetComboNext(59100001)` 未读到 combo；
- `ScheduleComboFollowUp` 未触发；
- `NetManager.IsConnected` 为 false；
- 目标重过滤后 alive=0；
- 还是日志/取证缺失。

只能基于真实日志下结论。

### 5.3 服务端 20001 S2C

只接受服务端真实 `20001 S2C`：

- engage `59100001` 若仍 `damage=0/hp=140/flag=0`，记录为符合第14轮结论。
- combo `59100002` 必须尝试抓 `damage>0`、`hp<140` 或 `hp==0`。
- 若 combo 仍 `damage=0`，必须贴完整 C2S/S2C 对比：skill、目标、位置、role/monster id、时间差、连接状态、remote-close/限频证据。

禁止用本地扣血、手改 Hp、伪造 FightVo 样本代替服务器回包。

## 6. P3：允许改代码的唯一条件

默认不改代码。只有以下证据成立才允许最小修复：

- 已真实跑到 `MonsterCount > 0`。
- 已触发第15轮自动驱动。
- 日志证明 combo 第二次 `20001` 没发，且原因在 Unity 代码而不是工具/网络/服务端。

允许修复范围：

- `SceneCombat` 中 combo 调度/目标重过滤。
- `SkillConfigs.GetComboNext` 的配置读取。
- 取证日志缺失导致无法审计的最小日志增强。

不允许：

- 改协议号/格式串。
- 伪造服务端伤害。
- 永久打开 `AppConfig` 测试开关。
- 新增大系统、完整 AI、特效、掉落、伙伴/神祇、副本逻辑。

修代码后必须重新 `dotnet build`，并重新实跑。

## 7. P4：任务进度

只有以下任一真实事件出现，才允许验证 `100030` 推进：

- `20001 S2C` 或场景广播导致怪物真实 `hp==0`。
- `12006 DeleteSceneObj` 真实到达。
- `30000/30001/30004/30005` 任务推送真实到达。

若只出现 `damage>0` 但未死亡，报告“正伤害闭环已过，击杀/任务推进未到达”，不要扩写任务系统。

## 8. P5：报告与提交

必须建立：

- `Docs/RuntimeCompare/MainQuest-ComboActualRun-第16轮.md`

报告必须包含：

- 本轮结论：PASS / BLOCKED / FAIL。
- 命令和取证方式。
- 关键日志摘录。
- 是否出现两次 `20001 C2S`。
- 是否出现 combo `20001 S2C damage>0/hp/death`。
- `AppConfig.asset` 已还原证据。
- `git status --short` 证据。
- 未完成项和下一轮最小任务。

若有代码修复，和报告一起提交。
若无代码修复但有真实证据或明确 blocker，也提交报告。

提交信息建议：

`[运行态对比/combo实跑验收] 第16轮: combo副技能真连实跑证据或阻塞`

## 9. 完成标准

第16轮不能用“用户手动执行”作为完成。可接受的完成只有：

- `PASS`: 真实日志证明 engage `59100001` + combo `59100002` 两次 `20001 C2S`，且服务端 combo `20001 S2C damage>0` 或 hp/death 到达。
- `BLOCKED`: 已真实跑到某一步，并用日志证明阻塞点（Unity 工具、远端断线、服务端不回、combo 未发、目标过滤为空等）。
- `FAIL`: 代码/配置/协议明确错误，并已最小修复或留下准确下一步。

完成后不要自动扩散到动作特效、飘字、掉落、自动战斗 AI、伙伴/神祇、活动副本。
