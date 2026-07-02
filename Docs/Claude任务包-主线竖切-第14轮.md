# Claude任务包-主线竖切-第14轮

日期：2026-07-02

目标：第 11~13 轮把「使用物品→背包/货币刷新→玩家可见反馈」支线闭环了。第 14 轮**回归主线本体**——
循环终点是「新号从创角起主线任务链全程可推」,当前最大结构性缺口是 `TaskModel.DoTask` 的类型覆盖:
最小入口只接 找NPC对话/完成/主线副本/场景坐标 四类,老端 `TaskModel.ts:797` switch 有 60+ case
(记忆库 task-execution-infra-readiness:其余类型会卡主线)。本轮先**数据侦察定范围**,再按主线实际用到的类型逐个接。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`
- `Docs/Claude任务包-主线竖切-第13轮.md` + `Docs/Shenxiao实施进度.md` 第 13 轮段

## 当前基线（第 13 轮已提交）

- Toast 视觉化(TipsManager → UILayer.Tip 浮动条,headless 退 log;Confirm 仍 Phase 0 自动 onYes)。
- 出售 15021 协议备货(SellGoods 动态 fmt + On15021;SellView 未移植无 UI 入口)。
- 背包增量 15017/15018/15008/15009、使用 15050、CLI 验证通道(RenderAll 四用例)见 11/12 轮段。
- `TaskModel.DoTask`(TaskModel.cs:408)现覆盖:IsFindNpcTask(Talk/StartTalk/EndTalk)→ DoFindNpcTask、
  IsAllStepFinish → DoFinishTask、TIP_PASS_MAIN_DUNGEON → DoPassMainDungeonTask、带场景坐标 → DoGotoSceneTask;
  其余 tipsType → Warn blocker。

## 本轮 P0：保护可运行基线

- worktree 干净;`dotnet build` 0 错;CLI `CliVerify.RenderAll` EXIT 0(四用例)。
- 不重做 11~13 轮,除非真实回归。

## 本轮 P1：主线 DoTask 类型覆盖(先侦察后补)

**侦察(先做,产出写进进度文档)**:
1. `task_tips_type` 的来源:30001 协议字段?config_task 列?(TaskVo.TaskTipsType 赋值链路回查 TaskController/TaskConfigs)。
2. 统计**主线链**(config_task 主线 kind/chapter 字段界定,列序见 config_table_default.json)实际出现的 tips_type 分布
   (python 脚本跑 config_task.json;老端 TaskModel.ts:744-784+797 switch 对照各 case 行为)。
3. 输出「主线用到的 tipsType → 老端行为 → Unity 现状(已接/缺)」清单,按主线出现顺序排优先级。

**补齐(按清单从最早卡点开始,每类都要老端源码锚点)**:
- 典型预期缺口(以侦察为准,勿臆测):开某界面类(背包/锻造/装备强化 → 面板未移植的给精确 blocker + toast 提示,
  不假装完成)、进副本类(副本入口 opener,对标 OpenFun/DungeonController)、使用道具类(已有 15050 链可复用)、
  等级/战力达标类(纯提示型)。
- 红线:面板未移植的 case 不硬开空面板(记忆库 mainui-router-placeholder-is-deliberate);行为=老端源码,不造流程。

**验收**:dotnet 0 错;能 CLI 验证的(纯逻辑分支)加 ProtoDelta 式用例或日志断言;
主线前 N 个任务的 DoTask 分支逐个 dry-run(构造 TaskVo 喂 DoTask 看日志走向,不臆造完成)。

## 本轮 P2：活服整合往返(条件允许才做)

- 交互 Unity+MCP 可用 → 全链实跑(登录→任务→背包→使用→15017/15018 刷新→toast 可见);否则诚实 blocker。

## 本轮 P3：Confirm 视觉确认框 或 错误码表(二选一,被 P1 卡再做)

- Confirm:老端 Alert.Show(Alert_Type.Two)双按钮;当前自动 onYes 语义对「批量使用先用1个」有实际影响,优先级略高。
- 错误码表:Util.ErrorCodeShow → 错误码→文案配置(查老端 error code 表来源,经 ClientConfigSync 同步真实表,不硬编码)。

## 红线(每轮重复)

- 不造假数据、不硬编码配置兜底;缺表先补真实配置/同步工具/读取器。
- 修通用工具优先于手改 prefab;手改必须记录原因。
- 诚实 blocker;被卡 >15min 切下一个玩家可见缺口。
- 双编译 0 错 + CLI 验证过才 commit;commit 不 push。
