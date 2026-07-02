# Claude任务包-主线竖切-第13轮

日期：2026-07-02

目标：第 12 轮闭环了**背包增量协议**(15017 全字段/15018 数量/15008/15009 特殊积分,合成包实证读序与增删改语义)。
第 13 轮优先 **P1 Toast 视觉化**(使用成功/获得物品的玩家可见反馈,老端 SysInfo mini 消息;CLI 渲染可实证),
**P2 活服整合往返**(若交互 Unity/MCP 可用),**P3 出售链 15021 送包**(SellView 未移植前先把协议+数据层备好则缓)。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao协议架构.md`
- `Docs/Claude任务包-主线竖切-第12轮.md` + `Docs/Shenxiao实施进度.md` 第 12 轮段

## 当前基线（第 12 轮已提交）

- 背包增量:`Proto.GOODS_LIST_UPDATE=15017`(pos:h + goods_list[u16×同15010单项],复用 ReadGoods)/
  `GOODS_NUM_UPDATE=15018`({goods_id:l,goods_num:i,type_id:i})/`SPECIAL_SCORE_UPDATE=15008`/`SPECIAL_SCORE_LIST=15009`;
  `BagModel.Upsert`(num<=0删/有则整项替换/新增)+`UpdateNum`+`SpecialScores`;EVT_BAG_UPDATE/EVT_SPECIAL_SCORE_UPDATE。
- ⚠纠正:第 12 轮任务包原写 15000——老端 On15000=AddDynamic(装备**动态属性缓存**,非背包内容);
  真正的背包增量是 **15017/15018**(On15017/On15018 → UpdateBagGoods)。字段序以 ClientProtocol.json 为准。
- 验证:`CliVerify.ProtoDelta`(大端合成包 → 反射调私有 handler → 断言增/改/删/积分/非背包pos跳过)已入 RenderAll。
- CLI 通道/使用物品链/重复 Bind 治理见第 11 轮段。

## 已确认仍缺（按价值排序）

1. **Toast 视觉化**:TipsManager.Toast/Float 仍 log-only → 「使用成功」「获得X」玩家不可见。
   老端锚点:`sysInfo/SysInfoController.ts`(Message.show → APPEND_MSG → SysInfoType.MINI)+ `SysInfoMiniMgr`(滚动 mini 条)+ `MessageItem`。
   Unity 落法建议:TipsManager 保持无依赖,加 `public static Action<string> Renderer` 钩子(null 时照旧 log,headless 安全);
   Module 层 bootstrap 注入实现(Top 层 TMP 浮动条,复用 FZYHJW SDF;简单上浮+淡出+并发排队,样式从简)。CLI 渲染截图实证。
2. **活服整合往返**(第 7~12 轮顺延):需交互 Unity+MCP。GM API 223.109.142.26:88 在线。
3. **出售链**:15021 送包(WriteBegin(15021)+h count+逐项 l goods_id/i num)+ 回包 res==1「出售成功」;SellView 未移植 → tips 出售按钮暂缓,协议+数据层可先备。
4. **CongratulationView + config_gift_box 同步**、**BatchUseView**(同第 12 轮)。
5. **装备强化加值 config_equip_stren_lv**(第 11 轮任务包顺延项,活服实例属性显示的 config 部分)。

## 本轮 P0：保护可运行基线

- worktree 干净;`rg -n "GOODS_LIST_UPDATE|On15017|Upsert|SpecialScores|ProtoDelta" Assets/Scripts Assets/Editor -S` 命中;
- `dotnet build` 0 错;批处理 `CliVerify.RenderAll` 退出码 0(protoDelta+两渲染用例)。
- 不重做第 11/12 轮,除非真实回归。

## 本轮 P1：Toast 视觉化(玩家可见反馈闭环)

- 按「已确认仍缺 1」实现;红线:逻辑代码不写样式精修(定位/字号/淡出参数从简即可,后续用户编辑器手调);
  渲染实证:CLI 用例调 TipsManager.Toast("使用成功") → 截图断言浮动条文本渲出(TMP text 非空 + PNG)。
- 老端行为对齐点:多条排队上移、约 2~3 秒淡出(SysInfoMiniMgr 有队列;从简实现但别只显示一条就丢队列)。

## 本轮 P2：活服整合往返(条件允许才做)

- 同第 12 轮 P2 原文;不可用 → 诚实 blocker 转 P3。

## 本轮 P3：出售协议备货(15021)

- `Proto.SELL_GOODS=15021` 送包(对标 OnSellGoodsHandler:h count + 逐项 l goods_id/i num)+ `On15021`(res==1「出售成功」toast,否则错误码);
  `BagController.SellGoods(List<(goodsId,num)>)`。SellView 未移植 → 不接 tips 按钮(老端出售按钮开 SellView,不直发),只备协议层;
  验证走 ProtoDelta 式合成包(回包)+ dotnet 0 错。

## 红线(每轮重复)

- 不造假数据、不硬编码配置兜底;缺表先补真实配置/同步工具/读取器。
- 修通用工具优先于手改 prefab;手改必须记录原因。
- 诚实 blocker;被卡 >15min 切下一个玩家可见缺口。
- 双编译 0 错 + CLI 验证过才 commit;commit 不 push。
