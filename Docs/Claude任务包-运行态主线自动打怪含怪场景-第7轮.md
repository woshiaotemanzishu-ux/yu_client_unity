# Claude 任务包 · 运行态主线自动打怪含怪场景 · 第 7 轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须来自运行时 `http://127.0.0.1:8090/index.html`, 不允许把静态 `.scene` 或源码推断当最终真相。

上一轮基线:

- 第 6 轮任务包: `Docs/Claude任务包-运行态战斗打怪链路-第6轮.md`
- 第 6 轮提交: `5082509df`
- 第 6 轮报告: `Docs/RuntimeCompare/MainQuest-Combat-第6轮.md`
- 第 6 轮已确认:
  - 老端第一条打怪链路: 主线 `100020` 完成后激活 `100030`, `DoTask` 自动寻路到怪, 怪物进视野 `12012`/`12007`, 发送 `20024 "c" 1`, 再发送 `20001` 攻击/技能释放请求, 服务端回 `20001` 命中/伤害广播。
  - Unity 点击已学技能 `59100001 御剑一式` 已从 `SkillController.PressSkillHandler` 打到 `SceneCombat.MainRoleAttackTarget`, 并从真实 `SceneManager` 怪物表寻敌。
  - Unity 真连角色 `云霄42852` 位于 `10000 云来镇`, `12002` 快照字节精确 `怪物/采集=0 remaining=0B`, 34 个 NPC 但零怪物, 因此当前只能真实无目标阻塞, 不允许假放。
  - 完整进场景握手 + 主角装配后会话存活约 87s, 比第 5 轮 10-15s remote-close 明显改善。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-Combat-第6轮.md`
8. 老端运行时 `http://127.0.0.1:8090/index.html`

## 本轮边界

本轮只推进“主线任务把角色带进真实含怪场景, 然后技能命中真实怪物目标入口”:

`老端运行态 100030/第一条打怪任务 -> Unity 真实 TaskModel.DoTask/TaskSpeed 等价链路 -> 进入含怪场景或任务点 -> SceneManager 出现真实 MonsterVo -> SceneCombat 命中取到怪分支 -> 朝向/接近/释放边界可见`

禁止:

- 不允许 hardcode 怪物 id、坐标、技能 id 或任务 id 作为最终业务逻辑; 调试 harness 只能探测和取证, 产线代码必须来自真实协议/配置/模型。
- 不允许手写假 MonsterVo、假 SceneObj、假目标、假伤害、假 CD、假特效。
- 不允许用日志冒充玩家可见怪物; “怪物可见”必须有 Unity GameView 截图或运行态节点/渲染证据。
- 不允许猜 `20001`/`20024` 协议发送。只有完整确认老端 fight-movie/AOE 收集链和 Unity 等价数据后才可发真实请求。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind 或用 `transform.Find` 取业务节点。

## P0: 老端运行态第一条打怪任务补证

从 `http://127.0.0.1:8090/index.html` 运行态采样。账号不固定; 没有账号就注册新账号并创建新角色, 从主界面开始顺主线推进。

产物:

- `Docs/RuntimeCompare/MainQuest-AutoCombat-第7轮.md`
- 老端运行时截图: 主线 `100030` 或等价第一条打怪任务, 画面里尽量同时包含任务区、怪物/目标、主角、技能栏、自动战斗状态。
- 老端 console / 节点树 / 协议证据:
  - `100020 -> 100030` 或实际第一条打怪任务的任务推进证据。
  - `DoTask` / `TaskSpeed` / 自动寻路目标坐标或目标对象。
  - 怪物进视野 `12012`/`12007`, 战斗态 `20024`, 攻击请求/广播 `20001`。
  - 若运行态无法重采截图, 必须明确卡在哪一步, 并保留第 6 轮 console 协议流作为旧证据, 不伪造截图。

## P1: Unity 主线自动寻路进入含怪任务点

优先使用真实账号/角色继续第 6 轮状态; 如果任务状态不适合, 可以注册新账号并创建新角色, 但必须记录账号、角色、任务状态和协议。

要求:

- 从真实 `30000`/任务配置/`TaskModel` 取得当前主线, 不造任务。
- 点击主线任务必须走真实 `TaskModel.DoTask` 路径。
- 对齐老端 `TaskSpeed`/自动寻路行为, 把角色移动到 `100030` 或实际第一条打怪任务目标点。
- 如果缺少 Unity 等价 `TaskSpeed`/自动寻路到怪链路, 本轮补最小真实链路, 只使用任务配置、NPC/怪物/场景协议和已有移动系统。
- 如果服务端当前账号已跳过或卡住任务, 真实记录任务 id、服务端回包和卡点; 不用假任务补。

验收:

- Unity GameView 或日志证明: 当前主线任务被点击 -> 真实 DoTask -> 主角移动/寻路 -> 到达含怪任务点或明确协议阻塞。
- 不能只写代码; 必须有运行态截图或可复现 harness 证据。

## P2: SceneManager 出现真实怪物并被 SceneCombat 读取

进入含怪区域后, 只接受真实来源的怪物:

- `12002` 场景快照怪物块。
- `12007` 单怪进视野。
- `12012` 九宫格对象增删。
- 其他已确认的服务器场景协议。

验收:

- `SceneManager.MonsterCount > 0` 或等价真实 MonsterVo 列表证据。
- 怪物在 Unity GameView 可见, 或至少有真实 SceneObj/Renderer 实例证据; 如果当前项目尚未渲染怪物, 必须把“数据有怪但渲染缺失”作为确认问题, 并只补最小真实渲染链路。
- 禁止为了让技能可点而构造假 MonsterVo。

## P3: 技能点击命中真实怪物分支

在 P2 有真实怪物后, 点击已学技能, 例如 `59100001 御剑一式`:

- `SkillController.PressSkillHandler` 必须进入目标型技能分支。
- `SceneCombat.MainRoleAttackTarget` 必须从真实当前目标或最近可攻击怪取得目标。
- `MainRoleAttackMonster` 必须进入真实分支:
  - 范围内: 朝向 + 本地 `EVT_RELEASE_MAIN_SKILL` 边界。
  - 超范围: `MainRoleAgent.MoveToNpc` 接近后再释放边界。
- GameView 或日志证明朝向/接近/释放边界, 不要求本轮完成伤害结算。

如果仍然没有真实怪物, P3 只能报告真实阻塞, 不能扩散到假反馈。

## P4: 真实 `20024` / `20001` 发送只做可证即做

第 6 轮已经采到老端格式:

- `20024`: `SendFmtToGame(20024, "c", 1/2)`
- `20001`: `h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle`

但老端 `20001` 的怪物列表/AOE 中心来自 fight-movie 队列 + 碰撞收集链, 不是单点目标直发。

本轮原则:

- 如果能从老端源码和 Unity 当前数据中完整复现 fight-movie/AOE 收集链, 才接真实 `20024`/`20001` 最小发送。
- 如果只能拿到单个怪物目标但无法复现 AOE 收集, 不发送 `20001`, 只记录差异和下一轮任务。
- 不得为了“打起来”猜包或发半格式协议。

## P5: 只记录差异, 不扩散

只记录, 不编码:

- 完整自动战斗 AI 和挂机循环。
- 完整怪物 AI、仇恨、死亡、掉落、采集。
- 伙伴/神祇/远古奥术/天赋/模块加成。
- 完整技能特效、伤害飘字、战斗结算、战斗音效。
- 活动、副本、BOSS、队伍协同战斗。

## 验收与提交

1. `dotnet build yu_client_unity.slnx -v:minimal`
2. 运行态截图和日志证据写入 `Docs/RuntimeCompare/MainQuest-AutoCombat-第7轮.md`
3. 只提交本轮相关代码和报告, 不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
4. 最终总结必须明确:
   - 老 Laya 运行态第一条打怪任务如何触发、如何自动寻路、如何进战斗。
   - Unity 现在是否能进入含怪场景/任务点。
   - Unity 是否真的有 MonsterVo/怪物渲染/SceneCombat 目标命中。
   - 哪些差异已修, 哪些仍有真实证据阻塞。
   - 下一轮是否进入真实 `20024`/`20001`、怪物渲染、技能 CD/特效、或任务自动打怪循环。
