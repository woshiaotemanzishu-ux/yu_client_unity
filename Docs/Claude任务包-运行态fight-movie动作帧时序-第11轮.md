# Claude 任务包 - 运行态 fight-movie 动作帧时序 - 第11轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须优先来自运行时 `http://127.0.0.1:8090/index.html`；运行态暂时抓不到时，必须明确写出卡点，并用老端源码、真实配置、真连协议证据补足最低证据链。禁止把静态 `.scene` 当最终真相。

上一轮基线:

- 第10轮任务包: `Docs/Claude任务包-运行态20001伤害解析-第10轮.md`
- 第10轮结果提交: `472c96606`
- 第10轮报告: `Docs/RuntimeCompare/MainQuest-FightVoDamage-第10轮.md`
- 第10轮已确认:
  - `20001 S2C FightVo` 字节格式逐字段确认，`108B` 单防御者样本与 `222B/4怪` 真连样本都能零残留解析。
  - Unity 已新增 `FightVo.cs`，`On20001Broadcast` 已把服务端真实 `defense_list.hp` 接到既有 `SceneManager.ApplyHp/DeleteSceneObj` 链。
  - 三条真连样本全部 `damage=0 / hp=满 / damage_flag=0`，且每条同帧给攻击者上 `iconType=200` 自身 buff。
  - 当前新 blocker: 本端在释放边界立即发 `20001`，缺老端 fight-movie / `skill_damage_time` 动作帧时序，服务端稳定判 0 伤害；因此还不能真实扣血、死亡、推进 `100030`。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-FightVoDamage-第10轮.md`
8. `Docs/Claude任务包-运行态20001伤害解析-第10轮.md`
9. 当前 Unity 代码:
   - `Assets/Scripts/Module/Core/Scene/FightController.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneCombat.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneController.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneManager.cs`
   - `Assets/Scripts/Module/Core/Scene/MonsterRenderer.cs`
   - `Assets/Scripts/Module/Core/Scene/Vo/FightVo.cs`
   - `Assets/Scripts/Module/Core/Skill/SkillConfigs.cs`
10. 老端源码中至少查清:
   - `h5/src/scene/fight/FightController.ts`
   - `h5/src/scene/fight/FightVo.ts`
   - `h5/src/scene/fight/*`
   - `h5/src/scene/Scene.ts`
   - `h5/src/skill/SkillVo.ts` 或真实技能模型位置
   - 所有 `skill_damage_time`、`playFightMovie`、`ClientFightServer`、`onRoleRequestToFightHandler`、`request_fight_mon_list`、`fight-movie`、`attack_trigger_skill` 相关定义和调用

## 本轮目标

把第10轮卡住的真实战斗伤害推进一步:

`可见怪 -> 技能点击/释放 -> 保留真实目标/AOE -> 按老端 skill_damage_time 动作帧时序发 20024/20001 -> 服务端 20001 S2C 返回 damage>0 或更明确的真实 blocker -> 真实 hp/death 驱动血条/死亡 -> 验证 100030 击杀进度`

核心只做 **老端 fight-movie 发包时机的最小可验证复刻**。不要扩散到完整动作系统、完整特效、完整飘字、完整自动战斗 AI、掉落、伙伴/神祇、活动副本。

## 硬性边界

- 不允许本地伪造伤害、扣血、死亡、掉落或任务进度。
- 不允许为了截图直接改 `MonsterVo.Hp`；只有解析到服务器真实新 hp 或死亡字段后才能喂给现有场景链。
- 不允许猜 `skill_damage_time`。时序必须来自老端源码、真实配置、fight-movie 数据或运行态证据。
- 不允许 hardcode `1129`、`1134`、`59100001`、`5374/2672` 作为最终业务逻辑；调试 harness 可以取证，提交前不得落入业务代码。
- 不允许绕过 `NetManager`，不允许独立 socket。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind，不允许 `transform.Find` 取业务节点。
- `AppConfig.asset` 如需临时打开冒烟账号/自动登录，提交前必须还原。

## P0: 保护第10轮基线和代码可审阅性

先复核并保护:

- `dotnet build yu_client_unity.slnx -v:minimal` 或任务包内说明的最小可行 build 必须通过。
- 第10轮 `20001 S2C FightVo` 解析不能退化；`108B` 样本解析仍应 `remaining=0`。
- 真连 `222B/4怪` 解析链不能退化；若环境无法重现，报告必须引用第10轮证据并说明本轮卡点。
- `FightController.cs`、`FightVo.cs` 必须保持可审阅文本 diff。提交前至少检查:
  - `git diff --text HEAD~1..HEAD -- <cs文件>` 可显示代码文本。
  - 若 `git show --stat` 仍显示 `.cs` 为 `Bin`，必须查明原因并在报告写清；能安全归一化为 UTF-8 LF 时必须修复。
- 工作树只允许本轮代码、报告、任务包改动；`.playwright-cli/`、`output/` 不入库。

## P1: 老端 fight-movie / skill_damage_time 证据

必须从老端运行态和源码确认:

- 老端从技能点击/目标选择到 `20001` 发包的完整调用链。
- `skill_damage_time` 的来源、单位、默认值、是否按技能/动作/职业/等级变化。
- `playFightMovie` 或等价 fight-movie 何时触发 `ClientFightServer` / `onRoleRequestToFightHandler`。
- 老端在延迟期间如何保留目标列表、攻击点、角度、AOE 结果；目标移动/死亡时是否重算。
- 老端是否先发 `20024 "c" 1` 再等动作帧发 `20001`，还是二者都在动作帧附近。
- `御剑一式(59100001)` 的真实 `skill_damage_time` 或可推导时序；禁止只凭感觉设延迟。

产物:

- `Docs/RuntimeCompare/MainQuest-FightMovieTiming-第11轮.md`
- 报告必须包含老端运行态尝试、源码调用链、时序字段来源、Unity 当前差异、仍未确认字段清单。

## P2: Unity 最小动作帧发包时序

只在 P1 证据足够时实现:

- 将当前“释放边界立即发 `20001`”调整为按老端 `skill_damage_time` 延迟发包。
- 延迟发包必须保留真实目标/AOE 列表、攻击点、技能 id、角色状态来源；不得在延迟结束时造假目标。
- 如果老端是延迟前锁定目标列表，则 Unity 也锁定；如果老端延迟时重算，Unity 按老端规则重算。
- `20024 "c" 1` 的发送时机必须按老端证据处理；证据不足时保持第10轮行为并记录差异。
- 本轮只做最小 `SkillDamageTiming`/调度器，不做完整 fight-movie 动作/特效系统。

验收:

- 编辑期 harness 能证明 `20001` 不再在点击瞬间发，而是在真实 `skill_damage_time` 之后发。
- 不破坏第10轮 `FightVo` 解析。

## P3: 真连 damage>0 / hp变化验证

P2 后做真实 Play 验证:

- 登录同一账号，进入有怪场景，真实点击/驱动技能。
- 抓 `20024/20001 C2S` 与 `20001 S2C` 日志。
- 若出现 `damage>0`、`hp<max` 或 `hp==0`，必须记录防御者 id、hp、damage、damage_flag、可见血条/死亡截图或 Unity 运行态日志。
- 若仍稳定 `damage=0 / hp=满`，必须记录完整 C2S/S2C、时序参数、服务端 remote-close/限频状态，作为真实 blocker；禁止本地扣血。

## P4: 主线任务 `100030` 击杀进度

在 P3 有真实死亡后验证:

- `100030` 降服 3 只是否由真实死亡或服务器任务推送推进。
- 如有 `30000/30001` 或其他任务推送，记录协议证据。
- 如果击杀可见但任务不推进，记录真实 blocker，不本地改任务。

## P5: 只记录不扩散

只记录，不编码:

- 完整自动战斗 AI、挂机循环、寻怪循环。
- 完整动作系统、完整 CD、完整特效、音效、伤害飘字。
- 直线/扇形 AOE、PvP 目标、伙伴/神祇/天赋加成。
- 掉落、怪物 AI、活动副本、BOSS、队伍协同。

## 验收与提交

1. `dotnet build yu_client_unity.slnx -v:minimal` 或明确说明最小可行 build 及原因。
2. 真实运行态截图/日志/协议证据写入 `Docs/RuntimeCompare/MainQuest-FightMovieTiming-第11轮.md`。
3. 如果实现动作帧延迟，必须有老端时序证据、编辑期时序验证、Play 真连结果。
4. 只提交本轮任务包、代码、报告，不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
5. 最终总结必须说明:
   - 老端 `skill_damage_time`/fight-movie 发包时序是否确认。
   - Unity 是否按动作帧延迟发 `20001`。
   - 是否出现服务器真实 `damage>0/hp变化/death`。
   - 可见血条/怪物销毁是否真实发生。
   - `100030` 任务进度是否真实推进。
   - 仍有真实 blocker 和下一轮建议。
