# MainQuest Kill And Task Push - 第17轮

## 结论

BLOCKED。

第17轮 worker 已自然退出，但没有提交本轮要求的 `Docs/RuntimeCompare/MainQuest-KillAndTaskPush-第17轮.md`。Codex 接管后从 worker stdout、Unity `Editor.log` 和工作树复核到以下事实：

- 第16轮正伤害链未退化：`59100001` engage 帧仍是服务器真实 `damage=0`，`59100002` combo 副技能仍能收到服务器真实 `damage=62/63`，怪物 hp 从 `140` 降到 `78/77`。
- 第17轮没有闭合击杀链：未找到服务器真实 `hp==0`、死亡 flag、`12006/DeleteSceneObj` 或等价删除协议。
- 第17轮没有闭合任务推进：未找到能证明 `100030 -> 100040` 的真实 `30000/30001/30004/30005` 推送，日志中仍只有 `30005 newest finish task id=100020` 或 `id=0` 这类不能证明 100030 推进的记录。
- 第17轮 worker 退出前启动了 Pump loop，但只记录到 Pump 完成提示，没有完成后续分析和报告提交。

## 执行与环境

- worker: `C:\Users\Fengx\.codex\claude-workers\yu_client_unity\20260622-092941`
- wrapper PID: `160120`，Claude PID: `159876`
- 复核时两个 PID 均已不存在。
- `exit.txt`: `exit_code=0`
- `stdout.jsonl` 最终 `terminal_reason=completed`，`total_cost_usd=0.2008862`
- `stderr.log`: 0 bytes

## 验证

### Build

Codex 复核执行：

```powershell
dotnet build yu_client_unity.slnx -v:minimal
```

结果：0 警告，0 错误。

### AppConfig

worker 遗留了临时取证配置：

- `devAccount: unity_npc_475823114`
- `autoLoginSmokeTest: 1`
- `autoEnterFirstRoleSmokeTest: 1`
- `enableRound15ComboTest: 1`

Codex 已还原为默认值：

- `devAccount: unity_dev_001`
- `autoLoginSmokeTest: 0`
- `autoEnterFirstRoleSmokeTest: 0`
- `enableRound15ComboTest: 0`

还原后执行：

```powershell
git diff -- Assets/_App/Configs/AppConfig.asset
```

结果为空。

### 工作树

复核时工作树只剩已有可忽略输出：

```text
?? .playwright-cli/
?? output/
```

本轮未提交上述目录。

## 运行态证据

来源：`C:\Users\Fengx\AppData\Local\Unity\Editor\Editor.log`

### engage 59100001

```text
740693: [Fight] send 20001 攻击请求: skill=59100001 怪列表=[1129,1134,1131,1132] 人列表=[] x=5374 y=2672 angle=0 fmt="hiiiihihhh"
740792: [Fight] recv 20001 攻击结果广播(S2C): len=222B attacker type=2 role=4294967355 hp=780 skill=59100001 lv=1 pos=(5340,2624) atkPos=(5374,2672) atkBuff=1 trigger=0 defenders=4 remaining=0B
740807: [Fight]   defender[0] type_flag=1 id=1129 hp=140 damage=0 flag=0 pos=(5374,2672)
740822: [Fight]   defender[1] type_flag=1 id=1134 hp=140 damage=0 flag=0 pos=(5264,2840)
740837: [Fight]   defender[2] type_flag=1 id=1131 hp=140 damage=0 flag=0 pos=(5572,2800)
740852: [Fight]   defender[3] type_flag=1 id=1132 hp=140 damage=0 flag=0 pos=(5668,2656)
```

### combo 59100002

```text
740931: [Combat] combo 副技能补发: engage=59100001 -> combo=59100002 延迟=200ms 目标=[1129,1134,1131,1132]
740951: [Fight] send 20001 攻击请求: skill=59100002 怪列表=[1129,1134,1131,1132] 人列表=[] x=5374 y=2672 angle=0 fmt="hiiiihihhh"
740972: [Fight] recv 20001 攻击结果广播(S2C): len=196B attacker type=2 role=4294967355 hp=780 skill=59100002 lv=1 pos=(5340,2624) atkPos=(5374,2672) atkBuff=0 trigger=0 defenders=4 remaining=0B
740987: [Fight]   defender[0] type_flag=1 id=1132 hp=78 damage=62 flag=0 pos=(5668,2656)
741002: [Fight]   defender[1] type_flag=1 id=1131 hp=78 damage=62 flag=0 pos=(5572,2800)
```

同一段日志的后续检索显示 1134/1129 也保持第16轮结论：hp 约 `77/78`，damage `62/63`，flag `0`。这只能证明正伤害与血条刷新，不构成死亡证据。

### 第17轮 Pump

```text
746372: [Round17] Pump cycle 1: 200 frames processed (~3s elapsed)
750994: [Round17] Pump cycle 2: 400 frames processed (~6s elapsed)
751156: [Round17] Pump cycle 3: 600 frames processed (~10s elapsed)
751541: [Round17] Pump cycle 4: 800 frames processed (~13s elapsed)
751551: [Round17] Pump cycle 5: 1000 frames processed (~16s elapsed)
751561: [Round17] Pump cycle 6: 1200 frames processed (~20s elapsed)
751571: [Round17] Pump cycle 7: 1400 frames processed (~23s elapsed)
751581: [Round17] Pump cycle 8: 1600 frames processed (~26s elapsed)
751591: [Round17] Pump cycle 9: 1800 frames processed (~30s elapsed)
751601: [Round17] ★ Pump loop completed: 1800 cycles, 1800 frames (~30s)
751611: [Round17] Check Editor.log for monster death status and task progression
```

Pump 完成后没有对应的 `hp==0`、死亡 flag、删除对象协议或任务推进证据。

## 确认问题

1. 第17轮 worker 未完成本轮交付：它启动 Pump 后自然退出，未在 wakeup 后完成日志分析，也未提交本轮报告。
2. 连续击杀没有闭合：目前只能复核到一次 `59100002` 正伤害，把怪物打到 `78/77`，没有继续打到 `0` 的真实 S2C 证据。
3. 后续自动驱动链路不稳定：日志出现过目标距离 `814px > range=100px`、需要 `MoveToNpc` 接近的边界，Pump 完成后没有证明接近后再次发出有效 combo。
4. worker 遗留临时 `AppConfig.asset` 修改，已由 Codex 还原。

## 下一轮最小任务

下一轮只做一个目标：在不伪造本地伤害/死亡/任务进度的前提下，修正实跑 harness 或最小运行链，使同一会话能连续触发真实 `59100001 + 59100002`，直到出现以下之一：

- 服务器真实 `20001 S2C hp==0` 或死亡 flag；
- 服务器真实删除对象协议，如 `12006/DeleteSceneObj` 或代码确认的等价协议；
- 真实任务推送证明 `100030` 进度变化或推进到后续任务；
- 明确、可复现的 blocker。
