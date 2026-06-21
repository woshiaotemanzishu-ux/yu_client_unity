# Claude 任务包 · 运行态怪物可见渲染 · 第 8 轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须来自运行时 `http://127.0.0.1:8090/index.html`, 不允许把静态 `.scene` 或源码推断当最终真相。

上一轮基线:

- 第 7 轮任务包: `Docs/Claude任务包-运行态主线自动打怪含怪场景-第7轮.md`
- 第 7 轮代码/报告提交: `79a2bee28`
- 第 7 轮报告: `Docs/RuntimeCompare/MainQuest-AutoCombat-第7轮.md`
- 第 7 轮已确认:
  - 老端第一条打怪是开放世界主线 `100030`, 场景 `10000` 内寻路到 `(5463,2678)` 打 `10001001` x3, 不是副本。
  - Unity 真连测试服能由主线/任务点自动寻路进入含怪九宫格区域。
  - 服务器真实下发 `MonsterVo ins=1129 type=10001001 hp=140/140 can_attack=1 pos=(5374,2672)` 到 `SceneManager`。
  - `SceneCombat.MainRoleAttackTarget(59100001)` 能从真实怪取得目标, 超范围接近到约 62px 后进入本地 `EVT_RELEASE_MAIN_SKILL` 释放边界。
  - 当前确认问题: 工程没有 `MonsterSpawner`, `MonsterAdded` 只有 `SceneManager` 声明没有渲染订阅, 因此怪物只是数据态, GameView 不可见。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainQuest-AutoCombat-第7轮.md`
8. `Docs/Claude任务包-运行态主线自动打怪含怪场景-第7轮.md`
9. 现有 Unity 渲染参考:
   - `Assets/Scripts/Module/Core/Scene/NpcRenderer.cs`
   - `Assets/Scripts/Common/UI3D/SceneCharacterStage.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneManager.cs`
   - `Assets/Scripts/Module/Core/Scene/Vo/MonsterVo.cs`
   - `Assets/Scripts/Module/Core/Scene/SceneCombat.cs`
10. 老端运行时 `http://127.0.0.1:8090/index.html`

## 本轮边界

本轮只解决一个玩家可见问题:

`真实 MonsterVo -> 最小 MonsterSpawner/MonsterRenderer -> GameView 可见怪物 -> 技能点击仍命中同一真实怪 -> 记录下一步 20024/20001 blocker`

禁止:

- 不允许造假怪、假 MonsterVo、假 SceneObj、假伤害、假 CD、假特效。
- 不允许 hardcode `10001001`、`1129`、`5463/2678`、`59100001` 作为产线逻辑。调试 harness 可以探测, 提交前必须删除。
- 不允许用 cube、默认球、纯色方块等假模型冒充真实怪物。如果真实资源链找不到, 必须记录资源 blocker, 不能把假占位当完成。
- 不允许为了显示怪物改协议数据或改 `SceneManager` 的真实语义。
- 不允许猜 `20024`/`20001` 发送。可继续记录 fight-movie/AOE blocker, 但本轮不以发包为目标。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind 或用 `transform.Find` 取业务节点。

## P0: 保护第 7 轮基线

先复核并保护:

- `dotnet build yu_client_unity.slnx -v:minimal` 必须仍然通过。
- 第 7 轮主线寻路到任务点、真实 `MonsterVo` 入 `SceneManager`、`SceneCombat` 命中真实怪的链路不能退化。
- `TaskModel`、`MainUITaskTeamView` 第 7 轮改动不要被重写成假逻辑。
- `AppConfig.asset` 取证开关和 devAccount 如需临时改, 提交前必须还原。

## P1: 老端运行态怪物可见证据

从 `http://127.0.0.1:8090/index.html` 运行态补证或复用第 7 轮 console 证据, 重点只看玩家可见怪物:

- 第一条打怪 `100030` 任务点附近怪物 `10001001` 在老端画面中如何显示。
- 记录老端运行时截图、节点树或可观察图层, 至少说明:
  - 怪物是否有名牌/血条/任务标识。
  - 怪物相对主角和地图的位置表现。
  - 点击/自动战斗前是否已经可见。

产物:

- `Docs/RuntimeCompare/MainQuest-MonsterVisible-第8轮.md`
- 老端运行时截图/console/节点证据路径写入报告。
- 若老端运行态无法重采截图, 必须写清卡点, 并引用第 7 轮老端协议流作为最低证据。

## P2: Unity 最小真实怪物渲染链

实现 `MonsterSpawner` / `MonsterRenderer` 或等价最小渲染层, 但必须基于真实数据和已有渲染体系:

- 订阅 `SceneManager.MonsterAdded`、`MonsterRemoved`、`MonsterMoved`、`MonsterHpChanged`。
- 只渲染真实 `MonsterVo`; 不创建假 `MonsterVo`。
- 优先复用 `NpcRenderer` + `SceneCharacterStage` 的坐标口径:
  - 屏幕/合成台偏移 = 怪物像素坐标 - 主角像素坐标。
  - 跟随 `MainRoleAgent`/主角移动每帧更新位置。
  - 从 `SceneManager` 移除时销毁渲染对象。
- 怪物资源必须走真实资源链:
  - 先查老端 `config_monster`/等价配置字段, 找到 `10001001` 的模型、动作或 icon 来源。
  - Unity 加载必须走 `ResManager` / `GameResPath` / `ResourcePath` / `AssetHub` 既有路线, 不直接拼 Addressable 路径。
  - 如果缺转换资源, 记录具体缺失资源和转换入口, 不用假 cube 代替。
- 若当前只能做到 nameplate/2D sprite/静态模型中的一类, 必须说明真实依据和缺口, 不把半成品说成完整怪物。

验收:

- 真连测试服走到 `(5463,2678)` 附近后, `SceneManager.MonsterCount > 0` 且 GameView/节点/Renderer 证明怪物可见。
- 报告中记录至少一个真实怪实例: `type=10001001`, 实例 id、坐标、资源 key、渲染对象名、截图路径或节点树。

## P3: 技能点击仍命中可见怪

在 P2 可见怪物成立后, 复跑第 7 轮技能链:

- 点击已学目标技能, 例如当前真实快捷栏里的 `59100001`。
- `SceneCombat.MainRoleAttackTarget` 必须命中同一只真实可见怪或当前最近可攻击怪。
- 超范围接近/范围内朝向/本地 `EVT_RELEASE_MAIN_SKILL` 边界必须保留。
- 不能为了可见怪改成假 target 或直接释放。

验收:

- 日志或 harness 证明可见怪实例 id 与 `SceneCombat.CurrentTargetId` 一致, 或说明最近怪选择的真实依据。
- GameView 截图里至少能看到怪物和主角/任务点相对位置。

## P4: 只记录 20024/20001 blocker, 不猜包

本轮不强行接真实战斗发包。只允许在已有证据基础上记录:

- 老端 `20024`/`20001` 仍依赖 fight-movie/AOE 收集链。
- Unity 当前已到“可见怪 + 本地释放边界”, 但还缺:
  - 目标列表/范围收集。
  - 技能距离/CD/动作帧。
  - 服务端伤害广播与血条更新。

如果意外完整确认 fight-movie/AOE 等价链, 可以写下一轮任务包, 不要在本轮扩散实现。

## P5: 只记录差异, 不扩散

只记录, 不编码:

- 完整自动战斗 AI 和挂机循环。
- 怪物 AI、仇恨、死亡、掉落、采集。
- 伙伴/神祇/远古奥术/天赋/模块加成。
- 完整技能特效、伤害飘字、战斗结算、战斗音效。
- 活动、副本、BOSS、队伍协同战斗。

## 验收与提交

1. `dotnet build yu_client_unity.slnx -v:minimal`
2. 运行态截图和日志证据写入 `Docs/RuntimeCompare/MainQuest-MonsterVisible-第8轮.md`
3. 只提交本轮相关代码、报告和必要资源配置, 不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
4. 最终总结必须明确:
   - 老 Laya 运行态第一条打怪怪物如何显示。
   - Unity 是否从真实 `MonsterVo` 渲染出可见怪物。
   - 使用了哪个真实资源 key / 资源路径 / 配置来源。
   - 技能点击是否仍命中可见怪。
   - 哪些差异已修, 哪些仍有真实证据阻塞。
   - 下一轮是否进入真实 `20024`/`20001`、技能 CD/特效、怪物血条/受击、或任务自动打怪循环。
