# Claude 任务包 · 运行态技能栏可视闭环 · 第 5 轮

执行仓库: `D:\GitProject\yu_client_unity`

老端仓库: `D:\GitProject\yu_client`

目标: 完全复刻老 Laya 客户端。老端结论必须来自运行时 `http://127.0.0.1:8090/index.html`, 不允许把静态 `.scene` 或源码推断当最终真相。

上一轮基线:

- 第 4 轮任务包: `Docs/Claude任务包-运行态主界面技能栏-第4轮.md`
- 第 4 轮提交: `e777953bf`
- 第 4 轮报告: `Docs/RuntimeCompare/MainUI-Skill-第4轮.md`
- 第 4 轮已确认: Unity 真连测试服后 `21002` 回 39 个技能, `shortcutList=4`, `13007 barInfo=1`, 技能 ids/名称/图标来自真实 `config_skill`/`ConfigSkillUI`, `dotnet build` 通过。
- 第 4 轮未闭合: 截图 `output/runtime_unity/play_real_skillbar.png` 未呈现 4 个带图技能槽, 原因记录为 RunCommand 绕过 LoginFlow / MainUIModule 激活与会话状态问题。用户要的是运行时可见效果, 不能只用日志替代视觉闭环。

## 必读

1. `AGENTS.md`
2. `.github/copilot-instructions.md`
3. `Docs/Shenxiao编码规范.md`
4. `Docs/Shenxiao重构实施方案.md`
5. `Docs/LayaUI转换流水线.md`
6. `Docs/Shenxiao进游戏链路.md`
7. `Docs/RuntimeCompare/MainUI-Skill-第4轮.md`
8. 老端运行时 `http://127.0.0.1:8090/index.html`

## 本轮边界

本轮优先关闭主界面技能栏的真实可视闭环, 再接最小技能释放边界。不要跳到完整战斗系统, 不要扩散做伙伴/神祇/天赋/活动。

禁止:

- 不允许 hardcode 技能 id 或假造 shortcutList。
- 不允许用日志冒充“技能栏可见”。日志只能证明协议和数据, 不能证明玩家看到。
- 不允许提交 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
- 不允许手改生成 Bind 或用 `transform.Find` 取业务节点。

## P0: 老端运行时技能栏可见证据

从 `http://127.0.0.1:8090/index.html` 运行态采样, 账号不固定; 若旧账号状态不适合, 注册新账号并创建新角色。

产物:

- `Docs/RuntimeCompare/MainUI-SkillVisible-第5轮.md`
- 老端运行时截图: 首屏主界面技能栏区域必须能看到技能槽/自动战斗按钮/伙伴锁或其真实缺失状态。
- 老端运行时 console / 节点树证据: `send/recv 21002/13007`, `MainUISkillView` 可见节点, 4 槽固定位置或实际运行差异。

## P1: Unity 正常运行路径下技能栏必须可见

用 Unity 真实运行路径验证, 优先走正常 LoginFlow / MainUIFlow / SceneEntryFlow, 避免第 4 轮 RunCommand 绕过登录层导致的残留面板和 MainUIModule 未激活。

验收要求:

- Unity 截图必须显示主界面技能栏 4 个技能槽, 且图标/锁态来自真实 `SkillManager.ShortcutList`。
- 若截图没有 4 槽, 必须定位真实原因并修代码: MainUIFlow 未激活、MainUISkillView 未 Show、事件先后顺序、config 未加载、ResManager 图标未落地、prefab 模板层级或锁态遮挡等。
- 不能只在报告里说“日志证明数据存在”。这轮必须把画面补上。

建议路径:

- 先复用第 4 轮真连方式找到可用服务器: 遍历 `LoginModel.Servers`, `server[0]:10010` 可能 WS 不可连, `server[1]:10000` 已可连。
- 尽量通过正常登录 UI 或等价 LoginFlow 触发进入游戏, 让 MainUIFlow 自然打开 MainUIModule。
- 如果必须用 RunCommand, 单条命令内完成真连、等待 GAME_START、等待 MainUIFlow、等待 `EVT_SKILL_LIST_UPDATED`, 再截图; 不要在截图前触发清空 `SkillManager` 的路径。

## P2: 技能点击到战斗释放边界最小闭环

只有 P1 画面可见后才做。目标不是完整战斗, 而是把点击技能从 `EVT_SKILL_SHORTCUT_CLICK` 往老端 `Scene.MainRoleAttackTarget` 方向推进一个真实边界。

要求:

- 查老端 `SkillManager.PressSkillHandler`、`Scene.MainRoleAttackTarget`、`FightEvent.SKILL_SHORTCUT_CLICK` 的真实链路。
- Unity 侧如果场景无怪/无目标, 只记录真实阻塞; 不要假怪、假伤害、假 CD。
- 若能找到真实怪物/NPC/目标链路, 做最小点击后寻敌/选目标/释放请求或本地边界日志。
- 技能 CD 圆遮罩 `CirCleCdView` 只在有真实 CD 数据或老端明确规则后接, 不造假倒计时。

## P3: 自动战斗按钮真实入口边界

只做最小闭环:

- 按老端 `AutoFightManager`/`MainUISkillView.SetAutoFight` 确认自动战斗按钮三态。
- Unity 继续保持 AutoFight 与 AutoBrush 分离。
- 若没有真实打怪目标, 自动战斗只切状态和记录阻塞; 不做假循环。

## P4: 只记录差异, 不扩散

只记录, 不编码:

- 伙伴技能/伙伴觉醒锁完整逻辑。
- 神祇技能、远古奥术 21101、天赋 21010、模块加成 18401。
- 完整战斗 AI、特效、伤害飘字、战斗结算。
- 其他主界面入口、活动、队伍、充值气泡。

## 验收与提交

1. `dotnet build yu_client_unity.slnx -v:minimal`
2. 运行态截图和日志证据写入 `Docs/RuntimeCompare/MainUI-SkillVisible-第5轮.md`
3. 只提交本轮相关代码和报告, 不带 `.playwright-cli/`、`output/`、字体 SDF、Generated/Bind。
4. 最终总结必须明确:
   - 旧 Laya 运行态看到什么。
   - Unity 运行态现在看到什么。
   - 哪些差异已修, 哪些仍有真实证据阻塞。
   - 下一轮是否可以进入战斗/打怪链路。
