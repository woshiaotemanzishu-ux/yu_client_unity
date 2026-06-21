# Claude 任务包 - 运行态服务端伤害根因 - 第12轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须优先来自运行时 `http://127.0.0.1:8090/index.html`；运行态抓不到时，必须明确卡点，并用老端源码、真实配置、真连协议证据补足最低证据链。禁止把静态 `.scene` 当最终真相。

上一轮基线:

- 第11轮任务包: `Docs/Claude任务包-运行态fight-movie动作帧时序-第11轮.md`
- 第11轮结果提交: `fd5c76cd6`
- 第11轮报告: `Docs/RuntimeCompare/MainQuest-FightMovieTiming-第11轮.md`
- 第11轮已确认:
  - 老端 `skill_damage_time = movie_cfg.damage_time`，单位秒，默认 `0`。
  - 当前实测技能 `59100001` 御剑一式/普攻的 `damage_time = 0`，全部 `591xx` 剑士技能也为 `0`；老端对该技能也是预播开始后下一帧发 `20001`。
  - 第10轮“按 fight-movie 延迟发包可解决 `damage=0`”假设已证伪；`damage=0` 不是 20001 解析问题，也不是 `59100001` 的动作帧时序问题。
  - `ClientFightServer` 是老端死代码；真实 PvE 伤害走服务端权威结算。
  - 第10轮 `20001 S2C FightVo` 解析和血量链仍是当前基线，不允许本地伪造扣血、死亡或任务进度。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-FightVoDamage-第10轮.md`
8. `Docs/RuntimeCompare/MainQuest-FightMovieTiming-第11轮.md`
9. 当前 Unity 代码:
   - `Assets/Scripts/Module/Core/Scene/FightController.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneCombat.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneController.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneManager.cs`
   - `Assets/Scripts/Module/Core/Scene/MonsterRenderer.cs`
   - `Assets/Scripts/Module/Core/Scene/Vo/FightVo.cs`
   - `Assets/Scripts/Module/Core/Skill/SkillConfigs.cs`
   - `Assets/Scripts/Module/Core/Role/*`
   - `Assets/Scripts/Module/Core/Bag/*`
10. 老端源码中至少查清:
   - `h5/src/scene/fight/FightController.ts`
   - `h5/src/scene/fight/FightVo.ts`
   - `h5/src/scene/fight/FightMovieInfo.ts`
   - `h5/src/skill/SkillVo.ts`
   - `h5/src/role/*`
   - `h5/src/bag/*`
   - `h5/src/equip/*`
   - 所有角色属性、装备、技能、怪物配置、`20024`、`20001`、`100030` 相关定义和调用。

## 本轮目标

把当前真实 blocker 从“服务端返回 `damage=0`”定位到可行动级别:

`同账号老端运行态对比 -> Unity 同场景同怪同技能真连 -> 角色战斗属性/装备/技能/怪物防御取证 -> 判定 damage=0 是账号/养成/服务端公式问题，还是 Unity C2S 字段/状态差异 -> 只在有证据时修 Unity -> 再验真 damage/hp/death/100030`

本轮不以“写更多战斗壳子”为目标，只追一个问题: **为什么服务端对当前 Unity 攻击返回 `damage=0`**。

## 硬性边界

- 不允许本地伪造伤害、扣血、死亡、掉落或任务进度。
- 不允许为了截图直接改 `MonsterVo.Hp`、`FightVo.damage` 或任务计数。
- 不允许猜服务端公式；只能记录客户端可见证据、老端源码/配置证据、真连协议证据。
- 不允许把 `1129`、`1134`、`59100001`、`5374/2672`、怪物 id 等调试样本硬编码进业务逻辑。
- 不允许绕过 `NetManager`，不允许独立 socket。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind，不允许 `transform.Find` 取业务节点。
- `AppConfig.asset` 如需临时打开冒烟账号/自动登录，提交前必须还原。

## P0: 保护第11轮基线

先复核并保护:

- `dotnet build yu_client_unity.slnx -v:minimal` 必须通过，或明确说明不可运行原因。
- 第10/11轮真实怪物可见、技能命中可见怪、`20024/20001` 真发包、`20001 S2C FightVo` 解析、血量链不能退化。
- `20001 S2C` 样本解析仍需 `remaining=0`，不能新增猜字段。
- 工作树只允许本轮报告、必要代码和任务包改动；`.playwright-cli/`、`output/` 不入库。

## P1: 老端同账号运行态对比

必须优先尝试老端运行态 `http://127.0.0.1:8090/index.html`:

- 使用与 Unity 同一账号/角色，进入相同主线阶段或同一可打怪场景。
- 点击同一主线/同一怪/同一技能，抓老端运行态截图、console、协议日志或可观察状态。
- 若老端同账号也 `damage=0`、无法击杀或卡在同一阶段，记录为账号/服务端/养成 blocker。
- 若老端同账号能造成 `damage>0` 或任务推进，而 Unity 仍 `0`，必须对比两端 `20024/20001 C2S` 字段、目标列表、坐标、角度、skill id、fighter 状态、buff/状态字段，定位 Unity 差异。
- 若老端页面仍卡加载，报告必须写出 HTTP 状态、页面状态、console/网络卡点，并用老端源码/配置 + Unity 真连补证，不得假装有老端运行态结论。

产物:

- `Docs/RuntimeCompare/MainQuest-DamageRootCause-第12轮.md`
- 报告必须包含老端运行态尝试、Unity 真连证据、协议字段对比、角色/怪物/技能配置证据、确认 blocker。

## P2: 角色战斗属性、装备、技能、怪物防御取证

从真实协议、运行态 UI、老端源码或真实配置确认:

- 当前角色等级、职业、技能 id、技能等级、是否普攻、是否已装备武器。
- 当前角色攻击/破甲/伤害相关属性字段来源；如果 Unity 没有该协议解析，先找老端字段和协议，不要猜。
- 当前背包/装备/角色面板是否能显示或推导武器、攻击、战力等关键字段。
- `config_mon[10001001]` 或实测怪物配置的 hp、防御、等级、受击限制等字段。
- `config_skill[59100001]` 与当前可用技能中是否存在更合适的真实非普攻/非零伤害路径；若要切技能，必须来自真实角色已解锁数据，不得造技能。

验收:

- 报告中给出“角色属性/装备不足”“Unity C2S 差异”“会话/限频/remote-close 导致未抓后续帧”“服务端公式不可见”之一或多个有证据结论。
- 如果证据显示 Unity 缺角色/装备/技能数据解析，才允许补最小模型和日志；不得为了让伤害变大改本地属性。

## P3: Unity C2S 差异修复边界

只有当 P1/P2 证明“老端同账号可伤害，而 Unity C2S 或角色状态与老端不同”时才写代码:

- 修复真实字段来源，例如 skill id、target id、目标列表、坐标、角度、fight state、装备/角色状态同步。
- 修复必须走现有 `NetManager`、`SceneManager`、`TaskModel`、`SkillConfigs` 等边界。
- 每个新增字段或协议解析都必须有老端源码/真连样本证据。
- 代码后必须重新跑 build，并做至少一个真连验证窗口。

若 P1/P2 不能证明 Unity 差异，本轮不写战斗代码，只把 blocker 落档。

## P4: 真 damage / hp / death / 100030 验证

在有真实原因或修复后验证:

- 抓 `20024/20001 C2S` 与 `20001 S2C`。
- 若出现 `damage>0`、`hp<max` 或 `hp==0`，记录防御者 id、hp、damage、damage_flag、可见血条/死亡截图或 Unity 运行态日志。
- 若出现真实死亡，继续验证 `100030` 降服 3 只是否由真实死亡或服务器任务推送推进。
- 若仍 `damage=0 / hp=满`，必须给出完整 C2S/S2C、角色属性、怪物配置、技能配置、会话/remote-close/限频证据，作为明确 blocker；禁止本地扣血。

## P5: 只记录不扩散

只记录，不编码:

- 完整自动战斗 AI、挂机寻怪循环。
- 完整动作系统、完整 CD、完整特效、音效、伤害飘字。
- 直线/扇形 AOE、PvP 目标、伙伴/神祇/天赋加成。
- 掉落、怪物 AI、活动副本、BOSS、队伍协同。
- 服务端公式改造或 GM 改数；本轮最多定位，不改服务端。

## 验收与提交

1. 建立 `Docs/RuntimeCompare/MainQuest-DamageRootCause-第12轮.md`。
2. `dotnet build yu_client_unity.slnx -v:minimal` 通过，或说明真实不可运行原因。
3. 报告必须明确:
   - 老端同账号运行态是否能造成真实伤害或任务推进。
   - Unity 本轮真连是否仍 `damage=0`。
   - 两端 `20024/20001` 或角色状态是否存在已确认差异。
   - 当前角色属性/装备/技能/怪物配置证据。
   - 是否写了 Unity 代码；若写，为什么有证据必须写。
   - 是否出现真实 `damage>0/hp/death/100030` 推进。
   - 下一轮最小可行动作。
4. 只提交本轮任务包、报告、必要代码，不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
