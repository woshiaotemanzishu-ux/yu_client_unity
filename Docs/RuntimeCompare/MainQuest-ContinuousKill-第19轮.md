# Combo 连续击杀闭合 运行态对比 · 第 19 轮

范围：在第 18 轮代码基线上执行完整端到端验证——Pump 驱动 574.9s，确认 enableRound18ContinuousKill 标志生效，捕获 engage+combo+循环击杀+死亡+任务推进完整链路，或精确定位 blocker。

方法：① 保留第 16/17/18 轮基线；② AppConfig 四标志全开；③ RunCommand 驱动 EditorApplication.update+NetManager.Pump（~575s）；④ 真连服务端验证链路。

---

## 0. 核心成果与状态

| 项 | 第 16 轮 | 第 17 轮 | 第 18 轮 | 第 19 轮进展 |
|---|---|---|---|---|
| **真伤害链** | ✅ engage/combo 验证 | ✅ 继承 | ✅ 继承 | ✅ 保留不动 |
| **循环驱动代码** | ❌ 无 | 🔄 Pump 30s | ✅ 代码完整 | ✅ 继承 |
| **循环驱动运行态** | — | ❌ 无后续 | 🔄 待验证 | ❌ **BLOCKED** |
| **击杀至死亡** | — | — | 🔄 目标 | ❌ **BLOCKED** |
| **任务推进 100030→100040** | — | — | — | ❌ **BLOCKED** |

**本轮最终判定：🔴 BLOCKED**

---

## 1. P0：基线验证

### 1.1 编译验证

第 18 轮代码（commit d260841d0）继承，本轮无新改动。编译 0 错误（既有警告保留）。

### 1.2 Round18 代码清单确认

| 文件 | 内容 |
|---|---|
| `AppConfig.cs` | `enableRound18ContinuousKill` 字段 ✅ |
| `SceneController.cs:27` | `EnableRound18ContinuousKill` 静态属性 ✅ |
| `SceneController.cs:298` | `OnRound18FightResult()` ✅ |
| `SceneController.cs:311` | `ContinueKillAfterDelayAsync()` ✅ |
| `FightController.cs:227` | 调用 `OnRound18FightResult(skillId, ...)` ✅ |
| `LoginBootstrap.cs:57-59` | smoke 模式下设置两标志 ✅ |
| `AppConfig.asset` | 本轮验证期设 1，验证后已恢复 0 ✅ |

### 1.3 AppConfig 配置（验证期间）

```yaml
autoLoginSmokeTest: 1
autoEnterFirstRoleSmokeTest: 1
enableRound15ComboTest: 1
enableRound18ContinuousKill: 1
```

验证后已恢复全零（`git diff AppConfig.asset` = 无输出）。

---

## 2. P1：Pump 驱动执行结果

### 2.1 驱动配置

- 驱动方式：`EditorApplication.update` + `NetManager.Pump()`，约 10fps
- 总帧数：5400 frames（max）
- 实际耗时：574.9s

### 2.2 关键日志摘录

```
[Round19] ★角色列表 count=1,自动进游戏
[Login] 发送进入游戏 role_id=4294967350
[Net] sent proto=10004 bytes=58
[Login] 🎉 进入游戏成功(10004),主城/场景流程待接
[Login] 10006 heartbeat sent
... (heartbeats x4)
[Round19] [Pump] frame=300 elapsed=30.5s r18=True
[Net] websocket close received status=NormalClosure desc=
[Login] game socket disconnected, auto reconnect in 2s role_id=4294967350 left=1
[Net] connecting ws://223.109.142.26:10000
... (reconnect 10000+10004 success)
[Round19] [Pump] frame=600 elapsed=61.1s r18=True
[Net] websocket close received status=NormalClosure desc=
[Login] game socket disconnected, auto reconnect in 2s role_id=4294967350 left=0
... (no more reconnect)
[Round19] [Pump] frame=5400 elapsed=574.9s r18=True
[Round19] ★Pump结束 frame=5400 elapsed=574.9s
```

### 2.3 r18 标志确认

全程 `r18=True` ✅ — `EnableRound18ContinuousKill` 在 smoke 链完成后正确设置。

---

## 3. P2：战斗捕获结果

### 3.1 关键发现：全程无 12002 快照到达

Pump 期间两次会话（含重连一次）均未收到 `12002` 场景快照。

**10004 回包后实收协议序列**：

| proto | 大小 | 处理 |
|---|---|---|
| 15055 | 10B | 未注册（预期内）|
| 17500 | 63B | 未注册 |
| 51400 | 8B | 未注册 |
| 18401 | 2B | 未注册 |
| 13215 | 8B | 未注册 |
| 51300×5 | 457B×5 | 未注册 |
| 10004 | 8B | **进入游戏** ✅ |
| 13033 | 230B | 未注册 |
| 41721 | 67B | 未注册 |
| 19101 | 760B | 未注册 |
| 19004 | 35B | 未注册 |
| **12002** | — | **❌ 未出现** |
| **12005** | — | **❌ 未出现** |

### 3.2 Round15/Round18 未触发

由于 12002 未到达，`SceneController.On12002()` 未执行，`TryAutoStartRound15ComboTest()` 未调用，`MainRoleAttackTarget(59100001)` 未发出。

**20001 发包：0 次 / 收包：0 次**

### 3.3 已知可用的战斗证据（预先存在会话）

在 Round19 harness 启动之前（行 739066-742778），独立会话中确认：
```
send 20001 skill=59100001 (engage)
recv 20001 damage=0  hp=140  (engage帧)
send 20001 skill=59100002 (combo)
recv 20001 damage=62 hp=78
                damage=63 hp=77
```
该会话中 `EnableRound18ContinuousKill=false`（harness 尚未启动），故循环未触发。

---

## 4. P3：怪物死亡验证

❌ BLOCKED（依赖 P2 战斗捕获）

---

## 5. P4：根因分析与下一步

### 5.1 Blocker 根因

**核心问题**：10004 进入游戏后，服务端没有发送 12002 场景快照（含怪物）。

**分析**：
1. 服务端记录玩家位置为**主城**（task=100020 阶段，非战斗区域）
2. 客户端未实现 **12005（进场景请求）** —— 老端在 10004 成功后发 12005 请求进入上次所在场景；本端只接收 10004 回包，不发 12005
3. 无 12005 → 服务端不推送该场景的 12002 快照 → On12002 未触发 → 全自动链路断裂

**旁证**：重连（left=1）后同样未收到 12002，说明不是首次登录的特有行为。

### 5.2 Pre-harness 会话有战斗的原因

行 739066-742778 的战斗会话是**已存在的 WebSocket 会话**，服务端已将玩家放置于战斗场景（pos=5340,2624，5 只 10001001），NormalClosure 后服务端主动推送 12002 继续该场景。但我们 harness 每次重新发 10004 相当于重新进入游戏，服务端回到主城默认位置。

### 5.3 第 20 轮方向（三选一）

| 方向 | 描述 | 难度 |
|---|---|---|
| **A. 实现 12005 进场景** | 10004 后发 12005(scene_id)，服务端推 12002；需解析 12005 格式 | 中 |
| **B. 任务推进到进战斗区** | 让服务端把玩家放到战斗场景（通过完成前置任务）| 高 |
| **C. 利用已有战斗会话** | 不 fresh login，在现有会话活跃期（87s 内）注入 Round18 驱动 | 低 |

**推荐方向 A**：实现 12005 进场景请求，服务端回 12002，On12002 触发后 Round15/Round18 自动运转。

---

## 6. 附录：Pump 时序记录

| 里程碑 | 时间戳 | 日志 |
|---|---|---|
| harness 启动 | 行 752087 | `★ Round19 Pump harness 启动` |
| 进游戏 | 行 752763 | `★角色列表 count=1，自动进游戏` |
| frame=300 | 行 753267 | `elapsed=30.5s r18=True` |
| 第一次断开 | 行 753xxx | `NormalClosure left=1` |
| frame=600 | 行 754xxx | `elapsed=61.1s r18=True` |
| 第二次断开 | 行 754xxx | `NormalClosure left=0` |
| frame=5400 | 行 755xxx | `elapsed=574.9s r18=True` |
| ★Pump结束 | 行 755xxx | `★Pump结束 frame=5400 elapsed=574.9s` |

---

## 7. 状态总结

### 当前状态：🔴 BLOCKED

| 项 | 状态 | 备注 |
|---|---|---|
| ① 编译 0 错 | ✅ | 第 18 轮代码继承 |
| ② r18 标志生效 | ✅ | 全程 r18=True 确认 |
| ③ smoke 链完整 | ✅ | HTTP→WebSocket→10004 两次 |
| ④ 12002 快照到达 | ❌ | **Blocker** —— 未发 12005 |
| ⑤ engage+combo 捕获 | ❌ | 依赖 ④ |
| ⑥ 循环击杀(Round18) | ❌ | 依赖 ④ |
| ⑦ hp==0 死亡证据 | ❌ | 依赖 ④ |
| ⑧ 任务推进 100030→100040 | ❌ | 依赖 ④ |

### 下一轮目标（第 20 轮）

实现 `LoginController` 在 10004 成功后发送 **12005 进场景请求**，服务端回 12002 快照（含怪物），触发 Round15/Round18 自动循环击杀链路，完成本轮所有验收项。

---

**最终判定**：🔴 **BLOCKED** — 10004 后无 12002（未实现 12005 进场景），全自动链路断裂于场景入口。
