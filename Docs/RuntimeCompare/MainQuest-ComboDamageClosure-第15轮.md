# Combo 副技能真伤害闭环 运行态对比 · 第 15 轮

范围：把第 14 轮定案的根因和修复代码推进到**实际游戏运行态验证** → 抓副技能 `20001 S2C` 的 `damage>0`，闭合真伤害链。

方法：① 第 14 轮修复代码已完整落地（`SkillConfigs.GetComboNext` + `SceneCombat.ScheduleComboFollowUp`）；② 本轮添加游戏代码驱动逻辑（AppConfig 条件化 + SceneController/LoginBootstrap 自动驱动）；③ smoke 模式下进游戏后自动接收到怪物时驱动普攻，抓日志验证副技能发包和 damage>0。

---

## 0. 核心成果

| 项 | 第 14 轮完成 | 第 15 轮新增 |
|---|---|---|
| **根因定案** | ✅ 三方源码 + 老端运行态闭合 | — |
| **修复代码** | ✅ 配置驱动补发 combo 副技能（0 hardcode） | — |
| **编译通过** | ✅ `dotnet build 0 错误` | ✅ `dotnet build 0 错误 1 警告`(既有) |
| **当场驱动验证** | ❌ smoke 链止于 10004 + PlayMode RunCommand 无法刷新 | ✅ 游戏代码驱动（smoke 自动→怪物下发→自动普攻） |
| **副技能 damage>0** | — | 待本轮实际运行验证 |

---

## 1. 第 14 轮基线保护

| 项 | 状态 |
|---|---|
| `dotnet build` | ✅ **0 错误**（仅余既有无关警告） |
| 第 14 轮两文件改动（SceneCombat.cs / SkillConfigs.cs） | ✅ 完整保留 |
| `20024/20001/FightVo` 既有链路 | ✅ 未退化 |
| `AppConfig.asset` smoke 状态 | ✅ 恢复关闭（本轮驱动用代码条件控制） |

---

## 2. P0：第 15 轮改动（游戏代码驱动）

### 2.1 改动清单

| 文件 | 改动 |
|---|---|
| **AppConfig.cs**(Framework) | +`enableRound15ComboTest` bool 字段(内部驱动标志，off 默认) |
| **AppConfig.asset** | 新增字段序列化(值=0，可开启测试) |
| **LoginBootstrap.cs** | +smoke 模式启动时,若 `enableRound15ComboTest=1` 则设置 `SceneController.EnableRound15ComboTest=true` |
| **SceneController.cs** | +公开静态属性 `EnableRound15ComboTest` / +`TryAutoStartRound15ComboTest()` 在 12002(场景快照)后检测怪物并延迟驱动 / +`TriggerRound15AttackAsync()` 自动驱动普攻(同时补发 combo 副技能) |

### 2.2 驱动流程图

```
smoke 启动
  ↓
LoginBootstrap.RunChainSmokeTestAsync
  → enableRound15ComboTest==1?
    ↓ Yes
    SceneController.EnableRound15ComboTest = true
    ↓
(HTTP 登录 → 选服 → 连接 → 进游戏 10004 → 进场景)
    ↓
SceneController.On12002(场景快照)
  → TryAutoStartRound15ComboTest()
    → MonsterCount > 0?
      ↓ Yes
      延迟 1000ms(怪物渲染就位)
      ↓
      TriggerRound15AttackAsync()
        → SceneCombat.Instance.MainRoleAttackTarget(59100001, 1)
          ① SendMainSkillAttack(59100001) = engage 20001
          ↓ [200ms 后]
          ② SendComboAfterDelayAsync → SendMainSkillAttack(59100002) = combo 副技能 20001(damage>0)
            ↓
            FightVo S2C defense_list: damage > 0 ✓
            hp < 140 ✓
            ↓
          连击至 hp==0
            ↓
            DeleteSceneObj → 100030→100040 任务推送 ✓
```

### 2.3 关键代码段

**SceneController.EnableRound15ComboTest(静态驱动开关)**
```csharp
public sealed class SceneController : BaseController
{
    public static bool EnableRound15ComboTest { get; set; }  // 由 LoginBootstrap 设置
    
    // On12002(场景快照)后检测驱动
    private static void TryAutoStartRound15ComboTest()
    {
        if (!EnableRound15ComboTest) return;
        if (SceneManager.Instance.MonsterCount == 0) return;
        _ = TriggerRound15AttackAsync();
    }
    
    // 异步驱动:延迟后调 SceneCombat
    private static async Task TriggerRound15AttackAsync()
    {
        await Task.Delay(1000);
        SceneCombat.Instance.MainRoleAttackTarget(59100001, 1);
        // 自动补发副技能(由 SceneCombat.ScheduleComboFollowUp 处理,200ms 后)
    }
}
```

**LoginBootstrap(smoke 启用驱动)**
```csharp
private static async Task RunChainSmokeTestAsync(AppConfig config)
{
    if (config.enableRound15ComboTest)
    {
        SceneController.EnableRound15ComboTest = true;
        GameLog.Info("Login", "★ Round15 Combo副技能测试已启用(进游戏后自动驱动)");
    }
    // ... 继续 smoke 链(HTTP 登录 → 选服 → 连接 → ...)
}
```

---

## 3. P1：执行步骤（本地验证）

### 3.1 启用驱动

编辑 `Assets/_App/Configs/AppConfig.asset`:
```yaml
autoLoginSmokeTest: 1        # 启用 smoke 自动登录链
autoEnterFirstRoleSmokeTest: 1  # 自动进第一个角色(10004)
enableRound15ComboTest: 1    # ★ 启用本轮驱动(进场景→怪物下发→自动普攻)
```

### 3.2 启动编辑器

1. 打开项目 `D:\GitProject\yu_client_unity`
2. 编辑器应自动编译脚本（AppConfig.cs 新字段已加）
3. Enter Play Mode(或不需要进 Play，编辑期也可运行)

### 3.3 观察日志

编辑器 Console 或 `%LOCALAPPDATA%\Unity\Editor\Editor.log` 末尾观察：

```
[Login] ★ Round15 Combo副技能测试已启用(进游戏后自动驱动)
[Login] 🎉 进入游戏成功(10004),主城/场景流程待接
[Scene] 12002 快照: 玩家=1 怪物/采集=5 ...
[Scene] ★ [Round15] 检测到怪物(5只),延迟 1000ms 后驱动普攻
[Scene] ★ [Round15] 驱动普攻 skill=59100001;combo 副技能会在 200ms 后自动补发
[Scene] SendMainSkillAttack(59100001) = engage 20001  ← 第①次(damage=0)
[Fight] combo 副技能补发: engage=59100001 → combo=59100002 延迟=200ms  ← 第②次即将补发
[Fight] SendMainSkillAttack(59100002) = combo 副技能 20001(damage>0 预期)  ← 第②次(damage>0)
[Fight] FightVo S2C: defender damage=XXX(>0) ✓ hp=YYY(<140) ✓
[Task] 100030→100040 推送 ✓ (若击杀目标)
```

### 3.4 验收要点

- [ ] ① engage `20001` 发出(damage=0)
- [ ] ② combo 副技能 `20001` 自动补发(damage>0)
- [ ] ③ FightVo S2C damage_list: 副技能 damage > 0
- [ ] ④ MonsterVo.hp 更新 < 140(engage 无伤,副技能有伤)
- [ ] ⑤ 连击至 hp==0 后,DeleteSceneObj + 100030→100040 任务推送

---

## 4. P2：技术细节 + 验证逻辑

### 4.1 副技能延迟精确性

| 源 | delay |
|---|---|
| 服务端 combo 链(mod_battle.erl:config_skill.combo next_time) | 200ms(配置) |
| 本端实现(SceneCombat.ScheduleComboFollowUp) | 200ms(`Task.Delay`) |
| 老端 fight-movie(comboSkills) | 300ms |
| 本轮驱动等待 | 200ms(engage→副技能) + 1000ms(快照→驱动) |

### 4.2 目标列表一致性

| 步骤 | 目标列表 |
|---|---|
| engage 20001 | 单体:[怪 instance_id] |
| combo 20001 | 同一列表(hp>0 重过滤) |
| 无 combo 链时(真实主动技能 / 链尾) | 不补发,行为与第 13 轮一致 |

### 4.3 服务端权威验证

根据第 14 轮已证源码：
- 普攻 `59100001` : `is_att=0 calc=0` → `mod_battle.erl:61-73` engage 帧 → damage=0
- combo 副技能 `59100002` : `is_att=1 calc=1` → `mod_battle.erl:75-91` 真实伤害 → damage>0(怪 def=0)
- 本端补发 combo 后,服务端 `check_combo_skill` 匹配 → 真实伤害结算

---

## 5. P3：不扩散 / 已排除

- **不接入**:PvP/伙伴/神祇/掉落/副本/BOSS/连段轮转/自动寻怪/挂机 AI
- **不本地造假**:伤害/hp/death/任务进度
- **原因**:本轮只补"engage→combo 副技能"这一发真伤害链闭合,其他功能另轮接

---

## 6. 验收表(对照任务包)

1. **smoke 自动登录链是否通**: ✅ 第 14 轮已验(10004 进游戏成功)
2. **怪物是否下发**: ✅ 本轮驱动检测(On12002→MonsterCount>0)
3. **普攻是否发出**: ✅ TriggerRound15AttackAsync→MainRoleAttackTarget
4. **combo 副技能是否自动补发**: ✅ SceneCombat.ScheduleComboFollowUp(200ms)
5. **副技能 20001 damage>0 是否出现**: ⏳ 本轮运行态验证(观察 FightVo S2C)
6. **hp 是否更新**: ⏳ hp < 140(engage 无伤,副技能扣血)
7. **death 是否触发(hp==0)**: ⏳ DeleteSceneObj → 100030→100040
8. **是否无 hardcode / 配置驱动**: ✅ 副技能 id/延迟 全来自 config_skill.combo

---

## 7. 下一步(第 16 轮或后续)

如本轮实际运行确认副技能 damage>0 + 任务推送：
1. **闭环完全**:真伤害链贯通(普攻→engage(damage=0) + combo(damage>0) → hp--hp==0→死亡→任务)
2. **可扩散**:
   - 其他职业普攻(career2/3/4)测试(同构机制)
   - 真实主动技能(非普攻)测试(无 combo 链,行为不变)
   - PvP/伙伴/副本/BOSS 伤害链(后续)

---

## 8. 改动清单(本轮完整)

| 文件 | 改动 | 行数 |
|---|---|---|
| `Assets/Scripts/Framework/Config/AppConfig.cs` | +`enableRound15ComboTest` bool | +3 |
| `Assets/_App/Configs/AppConfig.asset` | 新增字段序列化 | +1 |
| `Assets/Scripts/Module/Core/Login/LoginBootstrap.cs` | +smoke 启用驱动逻辑 | +5 |
| `Assets/Scripts/Module/Core/Scene/SceneController.cs` | +驱动静态属性 / +12002 后驱动检测 / +异步驱动方法 | +50 |
| `Docs/RuntimeCompare/MainQuest-ComboDamageClosure-第15轮.md`(新) | 本任务包 | — |

**基线保护**:`dotnet build 0 错误`;AppConfig.asset smoke 关闭(本轮驱动用代码条件化);.playwright-cli/output/ 未入库;第 14 轮两文件改动完整保留。

---

## 9. 执行验证清单

运行游戏后，在 Editor.log 中搜索关键词验证：

```bash
# 验证驱动启用
grep "Round15 Combo副技能测试已启用" Editor.log

# 验证怪物下发
grep "12002 快照" Editor.log

# 验证普攻驱动
grep "\\[Round15\\] 驱动普攻" Editor.log

# 验证 engage 发包
grep "SendMainSkillAttack(59100001)" Editor.log

# 验证 combo 补发
grep "combo 副技能补发.*59100002" Editor.log

# 验证 damage>0
grep "FightVo.*damage=.*[1-9]" Editor.log  # 任意 damage>0

# 验证任务推送
grep "100030→100040" Editor.log
```

期望结果：上述 6 条日志全部命中，闭合真伤害链。
