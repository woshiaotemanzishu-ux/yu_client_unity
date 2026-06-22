# 场景入口门闩修复 运行态对比 · 第 20 轮

范围：在第 18/19 轮代码基线上，修复编辑器 Pump harness 中 12005 进场景请求从未发送的根因，并验证完整链路 `EVT_GAME_START → SceneEntryFlow → 12005 → 12100 → 12002 快照`。

方法：① 保留第 18 轮战斗基线；② AppConfig 四标志全零；③ 诊断 EVT_GAME_START 订阅缺失；④ 实现 EnsureInstalled 双补丁 + LoadSceneMapAsync 编辑器绕过；⑤ RunCommand Pump harness 真连验证。

---

## 0. 核心成果与状态

| 项 | 第 19 轮 | 第 20 轮 |
|---|---|---|
| **12005 C2S 发出** | ❌ BLOCKED | ✅ request 12005: dunId=0 sceneId=10000 |
| **12005 S2C 收到** | ❌ BLOCKED | ✅ 12005 ok: sceneId=10000 dunId=0 pos=(4339,1669) |
| **12100 请求** | ❌ BLOCKED | ✅ request 12100: editor harness bypass sceneId=10000 |
| **12002 快照到达** | ❌ BLOCKED | ✅ 玩家=1 怪物/采集=0 (主城无怪,符合预期) |
| **12018/12020** | ❌ BLOCKED | ✅ drop list + npc icon 请求成功 |
| **战斗 20001** | ❌ BLOCKED | — 主城无怪,combat 门控正常,待切换至战斗场景 |

**本轮最终判定：✅ 12005 门闩已解除，12005→12100→12002 链路端到端验通（3 次完整循环）**

---

## 1. P0：基线验证

### 1.1 编译验证

```
dotnet build Assembly-CSharp.csproj --no-incremental -p:DefineConstants="UNITY_EDITOR"
→ 0 个错误 / 6 个警告（既有，不引入新问题）
```

### 1.2 AppConfig 配置

```yaml
autoLoginSmokeTest: 0
autoEnterFirstRoleSmokeTest: 0
enableRound15ComboTest: 0
enableRound18ContinuousKill: 0
```

harness 内部直接设标志（不依赖 AppConfig），asset 全零基线保持不变。

---

## 2. P1：Round 19 Blocker 根因分析

### 2.1 现象回溯

第 19 轮 Pump harness（5400 帧 / 574.9s）从未收到 12002，也从未发出 12005。全程只有 10004 进游戏回包，后续协议均为未注册包（15055/17500/51400/18401 …）。

### 2.2 根因定位

**`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` 不在编辑器非 Play 态触发**。

这导致：
- `GameEntryFlow.Install()` 未调用 → `EVT_GAME_ENTERED` 无订阅 → `OnGameEntered` 从不执行 → ControllerHub 未初始化，启动标志门控永远打不开
- `SceneEntryFlow.Install()` 未调用 → `EVT_GAME_START` 无订阅 → `OnGameStart` 从不执行 → `RequestEnterScene` / 12005 永远不发

### 2.3 次级阻塞（LoadSceneMapAsync）

第 20 轮初步修复（P3.1/P3.2 完成后）验证到 12005 可发，但 12100 仍未出现。根因：`LoadSceneMapAsync` 调用 `SceneMapLoader.LoadAsync → ResManager.LoadAsync<TextAsset> → Addressables.LoadResourceLocationsAsync`。在编辑器非 Play 态，Addressables 未完成初始化，`KeyExists` 永久阻塞在 `AddressablesAwaiter.Wait` 的 `Task.Yield()` 循环中。

---

## 3. P3：实现修复

### 3.1 GameEntryFlow.EnsureInstalled()

文件：`Assets/Scripts/Module/Core/Game/GameEntryFlow.cs`

```csharp
private static bool _installed;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Install()
{
    if (_installed) return;
    _installed = true;
    EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);
    EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
    EventDispatcher.On<string>(GlobalEvent.EVT_GAME_START_FLAG_READY, OnGameStartFlagReady);
}

public static void EnsureInstalled() => Install();
```

`_installed` 守卫：`EventDispatcher.On` 用 `list.Add` 不幂等，防止重复订阅。Play 态由 `[RuntimeInitializeOnLoadMethod]` 自动调用，domain reload 后 `_installed` 重置为 false，不影响 Play 进入。

### 3.2 SceneEntryFlow.EnsureInstalled()

文件：`Assets/Scripts/Module/Core/Scene/SceneEntryFlow.cs`

```csharp
private static bool _installed;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Install()
{
    if (_installed) return;
    _installed = true;
    EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
}

public static void EnsureInstalled() => Install();
```

### 3.3 SceneController.LoadSceneMapAsync 编辑器绕过

文件：`Assets/Scripts/Module/Core/Scene/SceneController.cs:199`

```csharp
private async Task LoadSceneMapAsync(int sceneId)
{
    int version = ++_loadVersion;
#if UNITY_EDITOR
    if (!UnityEngine.Application.isPlaying)
    {
        // Addressables.LoadResourceLocationsAsync blocks in editor non-Play mode; bypass map render.
        SendFmt(Proto.SC_NPC_LIST, "i", sceneId);
        GameLog.Info("Scene", "request 12100: editor harness bypass sceneId={0}", sceneId);
        EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MAP_READY);
        return;
    }
#endif
    SceneMapData data = await SceneMapLoader.LoadAsync(sceneId);
    // ... 既有代码不变
```

Play 态时 `Application.isPlaying == true`，`#if` 块不执行，正常走 Addressables 路径。

---

## 4. P4：Pump Harness 验证结果

### 4.1 驱动配置

| 项 | 值 |
|---|---|
| 驱动方式 | `EditorApplication.update` + `NetManager.Pump()` |
| 最大帧数 | 1500 frames (~150s) |
| EnableRound15ComboTest | true（harness 内部直接设） |
| EnableRound18ContinuousKill | true（harness 内部直接设） |
| devAccount | unity_dev_001 |

### 4.2 关键日志（3 次完整循环）

**登录链（首次）：**

```
[Round20] ★ Pump harness 启动 devAccount=unity_dev_001 maxFrames=1500
[Round20] ① HTTP 登录...
[Round20] ① HTTP ok servers=2
[Round20] ③ endpoint 223.109.142.26:10000
[Round20] ④ WebSocket 已连，等角色列表回包 ...
[Round20] ★角色列表 count=1，自动进游戏
```

**12005→12100→12002 完整链（×3）：**

```
# 第 1 次（首次进游戏）
[Scene] request 12005: dunId=0 sceneId=10000
[Scene] 12005 ok: sceneId=10000 dunId=0 pos=(4339,1669)
[Scene] request 12100: editor harness bypass sceneId=10000   ← 新增绕过立即发送
[Scene] request 12002: npc list loaded sceneId=10000
[Scene] 12002 快照: 玩家=1 怪物/采集=0 伙伴=0 其他=0 假人=0 remaining=0B
[Scene] request 12018/12020: drop list + npc icon

# 第 2 次（第 1 次重连）—— 相同链路
[Scene] request 12005: dunId=0 sceneId=10000
[Scene] 12005 ok: sceneId=10000 dunId=0 pos=(4339,1669)
[Scene] request 12100: editor harness bypass sceneId=10000
[Scene] request 12002: npc list loaded sceneId=10000
[Scene] 12002 快照: 玩家=1 怪物/采集=0 伙伴=0 其他=0 假人=0 remaining=0B
[Scene] request 12018/12020: drop list + npc icon

# 第 3 次（第 2 次重连）—— 相同链路
... （同上，3 次全部完整）
```

**Pump 进度：**

```
[Round20] [Pump] frame=300 elapsed=34.1s r15=True r18=True
[Round20] [Pump] frame=600 elapsed=64.6s r15=True r18=True
[Round20] [Pump] frame=900 elapsed=94.8s r15=True r18=True
```

### 4.3 调用栈（关键帧）

12005 发出路径（与 Pump 驱动直接相关）：
```
CommandScript:Pump
→ NetManager:Pump
→ NetManager:Dispatch
→ GameStartController:On10202
→ GameEntryFlow:OnGameStartFlagReady   ← EnsureInstalled() 修复点
→ EventDispatcher:Emit(EVT_GAME_START)
→ SceneEntryFlow:OnGameStart           ← EnsureInstalled() 修复点
→ SceneController:RequestEnterScene
→ [Net] sent proto=12005
```

12100 发出路径（编辑器绕过）：
```
SceneController:On12005
→ LoadSceneMapAsync
→ #if UNITY_EDITOR !isPlaying → bypass
→ SendFmt(SC_NPC_LIST, sceneId)        ← 绕过 Addressables 阻塞
```

### 4.4 战斗捕获

| 协议 | 状态 | 原因 |
|---|---|---|
| 20001 (攻击) | — | 12002 快照 怪物=0（主城 10000 无怪，符合预期） |
| TryAutoStartRound15ComboTest | 执行但无目标 | EnableRound15ComboTest=true 已确认，SceneManager.Monsters 为空 |
| ContinueKillAfterDelayAsync | 未触发 | EnableRound18ContinuousKill=true 已确认，同上 |

**主城战斗空结果是预期行为**，非回归。本轮验证目标是 12005 门闩，已达成。

---

## 5. 附录：时序记录

| 里程碑 | 日志 |
|---|---|
| harness 启动 | `★ Pump harness 启动` |
| ① HTTP 登录 | `[Round20] ① HTTP ok servers=2` |
| ③ endpoint 解析 | `223.109.142.26:10000` |
| ④ WebSocket 连接 | `等角色列表回包 ...` |
| 角色列表 | `★角色列表 count=1，自动进游戏` |
| EVT_GAME_START | On10202 callback（启动标志门控全满） |
| **12005 C2S** | `request 12005: dunId=0 sceneId=10000` |
| **12005 S2C** | `12005 ok: sceneId=10000 dunId=0 pos=(4339,1669)` |
| **12100（绕过）** | `request 12100: editor harness bypass sceneId=10000` |
| **12002 快照** | `玩家=1 怪物/采集=0 … remaining=0B` |
| NormalClosure | `left=1` 自动重连，3 次循环完成 |
| frame=900 | `elapsed=94.8s r15=True r18=True` |

---

## 6. 状态总结

### 当前状态：✅ DONE — 12005 门闩已解除

| 项 | 状态 | 备注 |
|---|---|---|
| ① 编译 0 错 | ✅ | 三处修复，0 新错误 |
| ② EVT_GAME_START 订阅 | ✅ | EnsureInstalled 双修复 |
| ③ 12005 C2S 发出 | ✅ | request 12005: sceneId=10000 ×3 |
| ④ 12005 S2C 收到 | ✅ | 12005 ok: sceneId=10000 ×3 |
| ⑤ 12100 发出 | ✅ | editor harness bypass ×3 |
| ⑥ 12002 快照到达 | ✅ | 玩家=1 怪物=0 ×3 |
| ⑦ 12018/12020 后续 | ✅ | drop list + npc icon ×3 |
| ⑧ 战斗 20001 | — | 主城无怪，待玩家进入战斗场景后自动触发 |
| ⑨ AppConfig 全零恢复 | ✅ | harness 内部设标志，asset 不改 |

### 下一轮目标

本轮证明：12005 门闩全链路已通。战斗链路（20001 engage+combo+damage）在 Round 15-18 代码中已实现，等待服务端将玩家置于含怪场景（dunId/sceneId 指向战斗区域）后将自动触发。

---

**最终判定**：✅ **DONE** — `EVT_GAME_START → SceneEntryFlow → 12005 → 12100 → 12002` 三轮完整通过，Round 19 BLOCKED 根因已修复。
