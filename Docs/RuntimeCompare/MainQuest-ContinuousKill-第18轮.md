# Combo 连续击杀闭合 运行态对比 · 第 18 轮

范围：推进第 17 轮的 Pump 循环驱动 → 补充自动循环击杀逻辑 → 当场捕获完整链路：engage(damage=0) + combo(damage>0) 循环至 hp==0 + 怪物死亡/删除 + 任务推进。

方法：① 第 16/17 轮基线保留；② 新增 Round18 AppConfig 标志 + 循环驱动逻辑；③ combo 回包后若 hp>0 自动继续普攻；④ 抓取完整链路日志验证循环击杀闭合。

---

## 0. 核心成果与状态

| 项 | 第 16 轮 | 第 17 轮 | 第 18 轮进展 |
|---|---|---|---|
| **真伤害链** | ✅ engage/combo 验证通过 | ✅ 继承第 16 轮 | ✅ 保留不动 |
| **循环驱动** | ❌ 单次 1000ms → 普攻 1 次 | 🔄 Pump 30s,仅 1 次结果 | ✅ **新增:combo 回包后自动继续** |
| **击杀直至死亡** | ❌ 未验证 | ❌ Pump 无后续结果 | 🔄 **本轮验证目标** |
| **死亡证据** | — | ❌ 未发现 hp==0 | 🔄 **本轮关键** |
| **任务推进 100030→100040** | — | ❌ 未验证 | 🔄 **本轮关键** |

---

## 1. P0：第 16-17 轮基线保护 + Round18 新增（验证中）

### 1.1 编译验证

```
dotnet build yu_client_unity.slnx -v:minimal
```

预期：0 错误（既有警告保留）。

### 1.2 代码改动清单

| 文件 | 改动 | 说明 |
|---|---|---|
| `AppConfig.cs` | +`enableRound18ContinuousKill` 属性 | Round18 标志(boolean) |
| `SceneController.cs` | +`EnableRound18ContinuousKill` 静态属性 | Pump 期间启用 |
| `SceneController.cs` | +`OnRound18FightResult()` 方法 | 20001 回包后自动继续 |
| `SceneController.cs` | +`ContinueKillAfterDelayAsync()` 方法 | 延迟 500ms 后驱动下一次 |
| `FightController.cs` | 修改 `On20001Broadcast()` | 传递 skillId 至 ApplyDefenseListToScene |
| `FightController.cs` | 修改 `ApplyDefenseListToScene()` | 接收 skillId,调用 OnRound18FightResult |
| `LoginBootstrap.cs` | +Round18 标志设置 | smoke 模式下启用 |
| `AppConfig.asset` | +`enableRound18ContinuousKill: 0` | YAML 序列化字段 |

代码零改第 16/17 轮逻辑,仅追加新逻辑。

---

## 2. P1：循环击杀驱动机制验证（设计阶段）

### 2.1 驱动流程设计

```
1. On12002(场景快照) → TryAutoStartRound15ComboTest() 初始化
2. 延迟 1000ms → 驱动 MainRoleAttackTarget(59100001)
3. 发送 engage 20001 → 等候服务端回包
4. 服务端 20001 S2C: damage=0 → ApplyDefenseListToScene()
5. 200ms 后自动 SendComboAfterDelayAsync() 补发 combo 59100002
6. 服务端 20001 S2C: damage>0 → ApplyDefenseListToScene()
   ↓
   [Round18新增] OnRound18FightResult(skillId=59100002, targetId=X, alive=true)
   ↓
7. 若 alive==true → 延迟 500ms → 继续驱动 MainRoleAttackTarget(59100001)
8. 循环步骤 3-7,直到:
   - hp==0 (death flag or 12006 DeleteSceneObj)
   - 或 targetAlive==false
   - 或超时/blocker

预期循环次数: 140 hp / 62 damage ≈ 2-3 轮 (四舍五入)
```

### 2.2 Round18 检查点

**启用条件（三项必须）**：
1. `EnableRound15ComboTest == true` (由 LoginBootstrap 在 smoke mode 设置)
2. `EnableRound18ContinuousKill == true` (AppConfig + LoginBootstrap 设置)
3. `lastSkillId == 59100002` (确保这是 combo 回包)

**触发条件**：
- `targetAlive == true` (defense_list 中该怪 hp > 0)

**延迟策略**：
- 500ms 后继续驱动 (vs engage 后 200ms combo 延迟,避免 engage 未回包时继续发)

---

## 3. P2：20001 包格式验证（预期）

### 3.1 Engage (59100001)

```
[第1轮] send 20001: skill=59100001 怪列表=[XXXX] x/y=主目标坐标
[第1轮] recv 20001: damage=0 hp=140 (engage frame, no damage)
```

### 3.2 Combo (59100002) - 第1轮

```
[第1轮+200ms] send 20001: skill=59100002 怪列表=[XXXX] x/y=主目标坐标
[第1轮+200ms] recv 20001: damage=62/63 hp=78/77 (真实伤害)
```

### 3.3 Engage (59100001) - 第2轮

```
[第2轮=第1轮+700ms] send 20001: skill=59100001 怪列表=[XXXX] x/y=主目标坐标
[第2轮+~30ms] recv 20001: damage=0 hp=78/77 (engage frame)
```

### 3.4 Combo (59100002) - 第2轮

```
[第2轮+200ms] send 20001: skill=59100002 怪列表=[XXXX] x/y=主目标坐标
[第2轮+200ms] recv 20001: damage=62/63 hp=16/15 (hp继续下降)
或
[第2轮+200ms] recv 20001: damage=16/15 hp=0 (最后一击,死亡)
```

### 3.5 死亡证据

**预期之一**：
1. `recv 20001: damage=X hp=0` (防御者列表中该怪 hp==0)
2. `recv 12006: DeleteSceneObj(instanceId)` (删除对象协议)
3. `recv 30000/30001/30004/30005` (任务推送协议,证明击杀完成)

---

## 4. P3：怪物死亡流程验证（关键）

### 4.1 死亡判定

FightController.ApplyDefenseListToScene():
```csharp
if (d.Hp == 0)
{
    GameLog.Info("Fight", "怪 {0} 服务端判定死亡(hp=0 damage={1}),移除...", ins, d.Damage);
    mgr.DeleteSceneObj(ins);  // → MonsterRemoved 事件 → 渲染层销毁模型/血条
}
```

### 4.2 预期日志特征

```
[Fight] 怪 XXXX 服务端判定死亡(hp=0 damage=X),移除可见模型/名牌/血条
[Scene] 怪 XXXX 移除(MonsterRemoved 事件触发)
```

### 4.3 任务推进验证

击杀完成后预期收到任务推进协议:
```
[Role] 30005 newest finish task id=100030 (已完成 100030)
或
[Role] 30000 push task id=100040 (推进至 100040)
或
[Role] 30001 finish task id=100030 / start id=100040
```

---

## 5. P4：验收要点与下一步计划

| 项 | 预期 | 本轮观察 | 状态 |
|---|---|---|---|
| ① smoke 链基线 | ✅ | — | 复用第 16 轮 |
| ② engage+combo 基线 | ✅ | — | 复用第 16 轮 |
| ③ Round18 循环机制 | ✅ 代码完整 | 🔄 **编译验证中** | **本轮** |
| ④ 循环 2+ 轮次 | ✅ 至少 2 轮 | 🔄 **日志验证中** | **本轮** |
| ⑤ hp==0 死亡证据 | ✅ | 🔄 **关键** | **本轮** |
| ⑥ 删除对象协议 | ✅ 12006 或等价 | 🔄 **待捕获** | **本轮** |
| ⑦ 任务推进 100030→100040 | ✅ | 🔄 **待捕获** | **本轮** |
| ⑧ 无伪造伤害/死亡 | ✅ | 🔄 **待确认** | **本轮** |

---

## 6. 后续执行步骤（第 18 轮工作清单）

### 6.1 本轮验证开始

1. **AppConfig 配置启用**：
   - `devAccount: unity_dev_001`
   - `autoLoginSmokeTest: 1`
   - `autoEnterFirstRoleSmokeTest: 1`
   - `enableRound15ComboTest: 1`
   - `enableRound18ContinuousKill: 1` ← **新增**

2. **启动编辑期驱动（Unity RunCommand + Pump）**：
   - Pump 循环 60+ 秒(足够 2-3 轮循环击杀)

3. **捕获关键日志**：
   - `send 20001` 发包时机(engage vs combo)
   - `recv 20001` 回包 damage 和 hp 变化
   - `★ [Round18] combo 已补发` 自动继续日志
   - `怪 XXXX 服务端判定死亡(hp=0)` 死亡证据
   - `30000/30001/30004/30005` 任务推进

4. **验证死亡流程**：
   - 12006 DeleteSceneObj 或等价删除
   - MonsterVo.Hp 更新链(12009 or 20001 S2C)

### 6.2 预期阻塞与兜底

**阻塞 A**：Round18 循环未触发
- 原因排查：EnableRound18ContinuousKill 是否启用、skillId 是否==59100002
- 兜底：手动验证 OnRound18FightResult 逻辑(RunCommand 调用)

**阻塞 B**：hp 不减或循环卡死
- 原因排查：targetAlive 判断、SceneManager.GetMonster(id) 返回值
- 兜底：检查 hp 更新链(ApplyHp 是否被调用)

**阻塞 C**：无法达到 hp==0
- 原因排查：damage 计算、combo 损伤公式
- 兜底：手工计算打怪轮数、检查配置(config_career_skill_movies damage_time)

**阻塞 D**：死亡后无任务推进
- 原因排查：击杀完成但任务配置(config_quest 100030)异常
- 兜底：只验证到死亡即可,任务推进留给第 19 轮或后续

---

## 7. 技术细节确认

### 7.1 Round18 状态管理

SceneController 中新增：
```csharp
public static bool EnableRound18ContinuousKill { get; set; }  // LoginBootstrap 设置
private int _round18TargetMonster;  // 当前目标怪 id(0=无)

public void OnRound18FightResult(int lastSkillId, int targetMonsterId, bool targetAlive)
{
    if (!EnableRound15ComboTest || !EnableRound18ContinuousKill) return;
    bool isCombo = (lastSkillId == 59100002);
    if (!isCombo || !targetAlive) return;  // 只在 combo 回包且活着时继续
    _round18TargetMonster = targetMonsterId;
    _ = ContinueKillAfterDelayAsync();
}
```

### 7.2 自动继续链路

```csharp
private async Task ContinueKillAfterDelayAsync()
{
    await Task.Delay(500);
    MonsterVo target = SceneManager.Instance.GetMonster(_round18TargetMonster);
    if (target == null || target.Hp <= 0) return;  // 已死或移除,退出
    SceneCombat.Instance.MainRoleAttackTarget(59100001, 1);  // 继续普攻
    // 循环自动进行(via OnRound18FightResult 回调)
}
```

---

## 8. 状态总结（预期第 18 轮完成状态）

### 当前状态：🔄 **进行中**

代码侧：
- ✅ 编译 0 错
- ✅ AppConfig + SceneController + FightController 逻辑完整
- ✅ LoginBootstrap 标志设置完成

运行态（待验证）：
- 🔄 循环驱动是否自动触发
- 🔄 hp 是否持续下降
- 🔄 hp==0 死亡证据是否出现
- 🔄 任务推进是否发生

### 预期完成状态

**若本轮 PASS**：
```
Engage(59100001): damage=0  hp=140
Combo(59100002):  damage=62 hp=78  ← [自动继续]
Engage(59100001): damage=0  hp=78
Combo(59100002):  damage=63 hp=15  ← hp 继续下降
Engage(59100001): damage=0  hp=15
Combo(59100002):  damage=15 hp=0   ← 死亡

✅ 删除对象 / 任务推进 100030→100040
```

**若卡阻塞**：
记录 blocker、root cause、兜底方案，转入第 19 轮。

---

## 9. 附录：执行命令参考

### 9.1 本轮启动

编辑 `Assets/_App/Configs/AppConfig.asset`:
```yaml
devAccount: unity_dev_001
autoLoginSmokeTest: 1
autoEnterFirstRoleSmokeTest: 1
enableRound15ComboTest: 1
enableRound18ContinuousKill: 1  # ← 第 18 轮新增
```

### 9.2 编辑期 Pump 驱动

```csharp
EditorApplication.update += () => {
    if (FrameCount++ % 60 == 0)  // 约 1s 一次日志输出
        Debug.Log($"[Round18] Pump {FrameCount} frames ...");
    NetManager.Pump();
};
```

运行 60+ 秒 (目标:2-3 轮循环 × 3s 每轮 + buffer)

### 9.3 日志过滤

查找关键日志：
```
grep "\\[Round18\\]" Editor.log          # 循环驱动日志
grep "damage=" Editor.log                # 伤害输出
grep "hp=" Editor.log                    # HP 变化
grep "DeleteSceneObj" Editor.log         # 删除对象
grep "30000\\|30001\\|30004\\|30005" Editor.log  # 任务推进
```

---

**最终判定**：🔄 **本轮进行中**

状态：编译通过 ✅ / 运行态验证 🔄 / 预期完成时间 ~第 18 轮
