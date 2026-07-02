# Claude任务包-主线竖切-第15轮

日期：2026-07-02

目标：第 14 轮完成主线 DoTask 全类型覆盖(27 种 ctype:已接 8 类 + 26 系统结构化降级 + 未知兜底)并产出
**`Docs/主线卡点路线图.md`**(权威:server data_task.erl,24 个未移植系统按链序排列)。
从本轮起按路线图逐系统攻坚:**P1 = 链序第一闸「剑魄同修」(ctype 25,task 100190,链序 #20)**——
新号推主线最先真实卡住的系统。目标不是完整 UI,而是「任务条件可达成」的最小闭环:数据层 + 协议 + 培养动作。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`、`Docs/Shenxiao编码规范.md`、`Docs/Shenxiao协议架构.md`
- `Docs/主线卡点路线图.md`(本轮起的攻坚顺序)
- `Docs/Claude任务包-主线竖切-第14轮.md` + `Docs/Shenxiao实施进度.md` 第 14 轮段

## 当前基线(第 14 轮已提交)

- DoTask 覆盖:TIP_LV/TIP_WELCOME 常量 + UNPORTED_TIP_SYSTEM 26 系统映射(权威统计注释齐)→
  Welcome no-op / 降级 toast(真实 config 文案 + 系统名)+ 精确 blocker 日志 / 未知类型兜底 Warn;
  降级检查先于通用寻路(老端这些 case 不走坐标)。
- CLI `DoTaskCoverage` 用例:真实主线任务 + 服务端权威 tipsType,断言 5 分支日志走向;已入 RenderAll(六用例)。

## 本轮 P1:剑魄同修(TrainPartner)最小闭环

**侦察(先做)**:
1. 老端 `PartnerModel.ts`/`PartnerController.ts`(或同名 commonModel/commonController):协议号段、数据结构
   (同修列表/阶/星/培养消耗)、培养动作的发包(升星/升阶)。
2. 服务端 `src/partner/`(或对应模块):read/write 协议、培养条件与消耗表(data_partner*.erl)。
3. task 100190 的条件:ctype 25 id/need_num 含义(id 填阶数,数量填星数,老端枚举注释)→ 需要培养到几阶几星。
4. 配表:config_partner*(ClientConfigSync SYNC_LIST 是否已含;缺则补真实表)。

**最小闭环(以侦察为准)**:
- Proto 常量 + PartnerController(请求列表/培养动作/回包解析落 PartnerModel)+ GameStart 首包请求。
- DoTask ctype 25 从降级映射改接真实入口:面板未移植前,先做「一键培养到任务需求」的最小交互?
  ——**不行,勿臆造玩法**:老端点击开 Partner 面板由玩家操作。最小可推方案=移植培养协议 + 临时 TEMP 壳
  (同 ItemTipsView 约定:显示当前阶星/消耗/「培养」按钮,数据全真)。样式从简,后续用户重做 UI。
- 培养消耗走真实背包/货币(15017/15018/15008 增量已通,培养后服务端推更新)。

**验收**:dotnet 0 错;CLI 合成包用例(培养回包读序+Model 落库);TEMP 壳渲染截图(真实 config 阶星/消耗);
活服可用则实跑培养一次(需交互 Unity,不可用则诚实 blocker)。

## 本轮 P2:活服整合往返(条件允许才做)

- 同前;有交互 Unity+MCP 则跑「新号推主线到 100190 → 开同修壳 → 培养 → 任务完成 30004」。

## 本轮 P3:路线图第二闸预研(坐骑 ctype 23,task 100330)

- 只侦察不实现:老端 Mount 协议/数据结构/配表清单,写进下一轮任务包。

## 红线(每轮重复)

- 不造假数据、不硬编码配置兜底;缺表先补真实配置/同步工具/读取器。
- 修通用工具优先于手改 prefab;TEMP 壳允许但注明并保持数据全真。
- 诚实 blocker;被卡 >15min 切下一个玩家可见缺口。
- 双编译 0 错 + CLI 验证过才 commit;commit 不 push。
