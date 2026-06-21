# Claude 任务包 · 运行态真实战斗收尾 · 第 9 轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须来自运行时 `http://127.0.0.1:8090/index.html`, 不允许把静态 `.scene` 或源码推断当最终真相。

上一轮基线:

- 第 8 轮任务包: `Docs/Claude任务包-运行态怪物可见渲染-第8轮.md`
- 第 8 轮代码/报告提交: `825cc74d7`
- 第 8 轮报告: `Docs/RuntimeCompare/MainQuest-MonsterVisible-第8轮.md`
- 第 8 轮已确认:
  - Unity 真连测试服已把真实 `MonsterVo` 渲染成 GameView 可见怪物。
  - `SceneManager` 真实下发 5 只 `type=10001001` 怪, `monster_res=10020103`, `hp=140/140`, `vo.name=转运达摩`。
  - `MonsterRenderer` 使用真实资源 `object/monster/model_clothe_10020103/model_clothe_10020103`, 真实蒙皮模型 614 tris/452 verts, 不是 cube/占位。
  - GameView/节点证明 5 只 `model_clothe_10020103(Clone)` 可见, 有名牌/血条。
  - 技能 `59100001` 点击后 `SceneCombat.CurrentTargetId=1129`, 命中同一只可见怪。
  - 当前 blocker: 真实 `20024/20001` 发送仍依赖老端 fight-movie/AOE 收集链, 本轮未猜包; 怪血条扣减/死亡移除还未跑通。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-MonsterVisible-第8轮.md`
8. `Docs/Claude任务包-运行态怪物可见渲染-第8轮.md`
9. 现有 Unity 链路:
   - `Assets/Scripts/Module/Core/Scene/SceneCombat.cs`
   - `Assets/Scripts/Module/Core/Scene/MonsterRenderer.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneController.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneManager.cs`
   - `Assets/Scripts/Module/Core/Role/MainRoleAgent.cs`
10. 老端运行时 `http://127.0.0.1:8090/index.html`
11. 老端源码中与本轮直接相关的战斗链路, 至少包含:
   - `h5/src/scene/sceneobj/Monster.ts`
   - `h5/src/scene/sceneobj/Character.ts`
   - `h5/src/scene/Scene.ts` / `SceneManager` 等真实发包入口
   - 任何 `20024`、`20001`、fight-movie、AOE、skill movie、target collect 相关文件

## 本轮边界

本轮只解决一个玩家可见闭环:

`可见怪 -> 技能释放 -> 老端等价目标/AOE 收集 -> 真实 20024/20001 或明确 blocker -> 服务端 12009/死亡移除 -> 可见血条扣减/怪消失`

禁止:

- 不允许猜 `20024`/`20001` 字段或发送时机。没有老端运行态/源码证据, 就只记录 blocker。
- 不允许造假伤害、假扣血、假死亡、假掉落、假 CD、假特效。
- 不允许 hardcode `10001001`、`1129`、`5463/2678`、`59100001` 作为最终业务逻辑。调试 harness 可以取证, 提交前必须删除。
- 不允许为了通过验证直接改 `SceneManager` 的真实怪物数据。
- 不允许绕过 `NetManager` 收发协议或自己写独立 socket。
- 不允许把本地 `EVT_RELEASE_MAIN_SKILL` 当作真实战斗完成; 必须区分本地释放边界与服务端伤害广播。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind, 不允许 `transform.Find` 取业务节点。

## P0: 保护第 8 轮基线

先复核并保护:

- `dotnet build yu_client_unity.slnx -v:minimal` 必须仍然通过。
- 真实 `MonsterVo -> MonsterRenderer -> GameView 可见怪` 不退化。
- 技能点击仍能命中真实可见怪, 不能改成假 target。
- `AppConfig.asset` 如需临时打开冒烟账号/自动登录, 提交前必须还原。

## P1: 老端运行态/源码确认真实战斗发包链

从老端运行态和源码确认, 不猜:

- 第一条打怪任务 `100030` 对可见怪释放技能时, `20024` 与 `20001` 的触发顺序、字段来源和发送时机。
- fight-movie / AOE / target collect 链路到底如何收集怪物列表:
  - 单目标技能如何决定主目标。
  - AOE 技能如何收集附近怪。
  - 坐标 `x/y/angle` 来自角色、技能动作帧、目标还是鼠标/点击点。
- `20001` 的怪物列表、玩家列表、skill、x、y、angle 是否已有完整老端证据。
- 老端服务端返回的扣血/死亡/移除协议, 尤其 `12009`、`12010/12011/12012` 或其他实际消息。

产物:

- `Docs/RuntimeCompare/MainQuest-CombatFinish-第9轮.md`
- 必须写明老端运行态是否成功进入游戏并抓到 `20024/20001`。如果运行态仍卡加载页, 必须写清卡点, 并用源码 + 既有协议流补最低证据。

## P2: Unity 真实目标/AOE 收集最小实现

在不发包之前, 先建立可验证的目标收集:

- 从 `SceneCombat.CurrentTargetId` 或最近可攻击怪取得真实目标。
- 只基于真实 `SceneManager.AllMonsters`、技能配置、主角位置/朝向/距离收集目标。
- 单目标技能至少产出一个真实怪实例 id; AOE 只在老端证据清楚时实现。
- 写明本轮使用的技能类型、技能配置来源、距离/范围来源。

验收:

- Play 态真连可见怪存在时, 目标收集结果包含可见怪实例 id, 并与 GameView/节点证据一致。
- 如果缺技能距离/AOE 配置, 只记录 blocker, 不写假范围。

## P3: 真实 `20024/20001` 发送或明确阻塞

只有 P1/P2 证据完整才允许接真实发包:

- 通过 `NetManager` 按老端格式串发送, 不绕过框架。
- `20024` 是否先发、发什么值, 必须有老端证据。
- `20001` 字段必须逐项对齐老端来源:
  - 怪物目标列表
  - 玩家目标列表
  - skill id
  - x/y
  - angle
- 发送后必须记录服务器响应, 尤其扣血、死亡或拒绝原因。

如果任一字段无法确认, 不要发送, 报告中写精确 blocker 和下一步取证方式。

## P4: 可见血条扣减/死亡移除最小闭环

只有 P3 真实发包并拿到服务端广播后才推进:

- `12009 MonsterHpChanged` 到达时, `MonsterRenderer` 血条必须真实扣减。
- 怪物死亡/移除协议到达时, 可见模型和名牌必须销毁。
- 任务击杀进度如果服务端返回更新, 任务区应真实变化; 没有协议就记录 blocker。

禁止:

- 不允许本地伪造扣血或死亡。
- 不允许为了截图直接改 `MonsterVo.Hp`。

## P5: 只记录差异, 不扩散

只记录, 不编码:

- 完整自动战斗 AI、挂机循环、寻怪循环。
- 完整技能 CD、动作帧、特效、音效、伤害飘字。
- 怪物 AI、仇恨、掉落、采集。
- 伙伴/神祇/远古奥术/天赋/模块加成。
- 活动、副本、BOSS、队伍协同战斗。

## 验收与提交

1. `dotnet build yu_client_unity.slnx -v:minimal`
2. 真实运行态截图/日志证据写入 `Docs/RuntimeCompare/MainQuest-CombatFinish-第9轮.md`
3. 若接入发包, 必须有服务端响应证据; 若未接入, 必须有清楚 blocker。
4. 只提交本轮相关代码、报告和必要配置, 不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
5. 最终总结必须明确:
   - 老端 `20024/20001` 链路是否被运行态/源码确认。
   - Unity 是否实现真实目标/AOE 收集。
   - Unity 是否发送真实协议; 若未发送, 卡在哪个字段。
   - 是否出现服务端扣血/死亡广播。
   - 可见血条/怪物销毁是否真实发生。
   - 下一轮是否进入自动打怪循环、动作/特效、血条红绿/朝向/插值。
