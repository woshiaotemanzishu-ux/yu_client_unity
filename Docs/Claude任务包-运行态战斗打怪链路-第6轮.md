# Claude 任务包 · 运行态战斗打怪链路 · 第 6 轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须来自运行时 `http://127.0.0.1:8090/index.html`, 不允许把静态 `.scene` 或源码推断当最终真相。

上一轮基线:

- 第 5 轮任务包: `Docs/Claude任务包-运行态技能栏可视闭环-第5轮.md`
- 第 5 轮提交: `791ad3225`
- 第 5 轮报告: `Docs/RuntimeCompare/MainUI-SkillVisible-第5轮.md`
- 第 5 轮已确认: Unity 正常自动登录路径下主界面技能栏 4 个技能槽真实可见, 图标/锁态/坐标来自真实 `21002 + 13007 + config_skill + ConfigSkillUI`; `SKILL_SHORTCUT_CLICK` 已推进到 `CanAttack` 子集闸和 `config_skill.career/obj` 三分支; 自动战斗三态入口已补。
- 第 5 轮遗留: 技能释放只到 `Scene.MainRoleAttackTarget` 入口边界; 场景/怪物/命中/特效/伤害飘字/CD 未移植; 测试服进游戏后约 10-15s 主动 remote-close, 截图和验证必须在存活窗口内完成或用真实进场景协议把连接续住。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainUI-SkillVisible-第5轮.md`
8. 老端运行时 `http://127.0.0.1:8090/index.html`

## 本轮边界

本轮只推进“玩家能看到并触发的第一条真实打怪/技能释放链路”:

`老端运行态主线/场景中第一个真实怪物目标 -> Unity 真场景怪物目标来源 -> 点击技能 -> MainRoleAttackTarget/释放入口 -> 真实阻塞或最小释放协议`

不要跳到完整战斗 AI、完整怪物系统、完整伤害结算、完整特效库、伙伴/神祇/天赋/活动。禁止为了截图造假怪、假伤害、假 CD、假目标。

禁止:

- 不允许 hardcode 怪物 id、坐标、技能 id 或任务 id 作为最终逻辑; 调试 harness 可探测, 产线代码必须来自真实协议/配置/模型。
- 不允许手写假 MonsterVo/假 SceneObj 塞给技能释放。
- 不允许用日志冒充玩家可见怪物或命中效果; 可见结论必须有运行态截图/节点或 Unity GameView 证据。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind 或用 `transform.Find` 取业务节点。

## P0: 老端运行态第一条打怪链路证据

从 `http://127.0.0.1:8090/index.html` 运行态采样。账号不固定; 若旧账号状态不适合, 注册新账号并创建新角色, 从主界面点击主线任务自然推进, 直到出现第一条真实打怪/攻击/技能释放链路。

产物:

- `Docs/RuntimeCompare/MainQuest-Combat-第6轮.md`
- 老端运行时截图: 主界面任务推进到第一个打怪/攻击阶段, 画面中怪物/目标/技能栏/自动战斗状态必须尽量可见。
- 老端 console / 节点树 / 协议证据:
  - 任务点击/寻路/进入战斗相关日志或协议。
  - 场景怪物/目标来源, 例如 `Scene.monster_list`、`CreateMonster`、`MainRoleAttackTarget`、`MainRoleAttackMonster`、`RELEASE_MAIN_SKILL`、技能 CD 或释放请求协议。
  - 若老端首屏主线暂时没有怪物, 必须记录从新角色主线走到第一个怪物任务的真实步骤和阻塞点。

## P1: Unity 真实怪物目标来源

先查 Unity 当前场景系统是否已经有真实怪物数据链路:

- 服务器场景协议是否已接入怪物/对象列表。
- `SceneManager`/`SceneEntryFlow`/`Monster`/`SceneObj`/`MainRoleAgent` 是否有可复用真实结构。
- 真实怪物是否来自服务器协议、地图配置或老端配置; 以老端运行态和源码对齐, 不猜。

验收要求:

- 如果 Unity 真连后已有真实怪物, 截图和日志证明怪物可见或至少存在于真实 `SceneManager` 列表, 并能被技能寻敌读取。
- 如果 Unity 没有怪物协议/渲染链路, 本轮必须补到最小真实链路: 只解析真实协议/配置里已有的怪物对象并进入场景对象模型, 不造假对象。
- 如果服务端当前场景确实不下发怪物, 记录真实协议/场景证据, 并把本轮编码转为打通“无怪时的真实阻塞 + 下一轮任务进入含怪场景/任务”。

## P2: `SKILL_SHORTCUT_CLICK -> MainRoleAttackTarget` 最小闭环

在第 5 轮 `SkillController.PressSkillHandler` 的基础上继续推进。

要求:

- 查老端 `SkillManager.PressSkillHandler`、`Scene.MainRoleAttackTarget`、`MainRoleAttackMonster`、`FightEvent.RELEASE_MAIN_SKILL`、释放请求协议的真实链路。
- Unity 侧点击已学技能时, 必须从真实目标系统取目标:
  - 有当前点击目标就用当前目标。
  - 没有当前目标就按老端规则寻最近怪/可攻击目标。
  - 没有真实目标时只记录真实阻塞, 不释放、不假伤害。
- 若释放协议已明确, 可发起最小真实释放请求或本地 `RELEASE_MAIN_SKILL` 等价事件; 但必须确认协议号/格式串来自老端, 禁止猜格式。

验收要求:

- 至少一个已学技能, 例如第 5 轮实测的 `59100001 御剑一式`, 走到真实目标分支。
- 截图/日志证明: 技能点击 -> 选中/找到真实怪物目标或明确无目标阻塞 -> 进入 `MainRoleAttackTarget` / `MainRoleAttackMonster` / 释放入口。
- 不要求本轮完成伤害结算; 若没有服务端回包或怪物对象, 只记录真实 blocker。

## P3: 玩家可见的最小战斗反馈

P2 有真实目标后才做。优先级从低风险到高风险:

1. 选中目标/朝向/靠近目标的玩家可见反馈。
2. 普通攻击或技能释放入口的本地动作边界。
3. 真实 CD 数据存在时才接 `CirCleCdView`; 没有真实 CD 不做假倒计时。
4. 真实特效资源和挂点明确时才播特效; 缺资源只记录。

不得为了“看起来有战斗”补假飘字、假伤害、假怪死亡。

## P4: 只记录差异, 不扩散

只记录, 不编码:

- 完整自动战斗 AI 和挂机循环。
- 完整怪物 AI、仇恨、死亡、掉落、采集。
- 伙伴/神祇/远古奥术/天赋/模块加成。
- 完整技能特效、伤害飘字、战斗结算、战斗音效。
- 活动、副本、BOSS、队伍协同战斗。

## 验收与提交

1. `dotnet build yu_client_unity.slnx -v:minimal`
2. 运行态截图和日志证据写入 `Docs/RuntimeCompare/MainQuest-Combat-第6轮.md`
3. 只提交本轮相关代码和报告, 不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
4. 最终总结必须明确:
   - 旧 Laya 运行态第一条打怪链路是什么。
   - Unity 运行态现在能看到什么、点击技能后发生什么。
   - 哪些差异已修, 哪些仍有真实证据阻塞。
   - 下一轮是否进入真实怪物渲染、技能 CD/特效、或任务自动打怪。
