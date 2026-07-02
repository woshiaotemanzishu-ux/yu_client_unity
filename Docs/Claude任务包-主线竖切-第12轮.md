# Claude任务包-主线竖切-第12轮

日期：2026-07-02

目标：第 11 轮打通了**批处理 CLI 渲染验证通道**(无 MCP 也能实证)、补了**使用物品 15050 全链**(协议+防重+tips 使用按钮+礼包开出物 toast)、
清了**重复 Bind 历史存量**(嵌套 prefab 自带 + 旧回填 added-override → BindClick 双注册),并真机渲染实证了完成弹层货币图标格 + tips 使用按钮。
第 12 轮优先 **P1 背包增量协议闭环**(15000 单件物品推送 + 15008/15009 货币变动 → 用完物品背包/货币即时刷新,主线「用任务给的物品」闭环),
与 **P2 活服整合往返**(若 Unity 编辑器可交互/MCP 可连;否则继续诚实 blocker 转 P3 toast 视觉化)。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/Shenxiao协议架构.md`
- `Docs/Claude任务包-主线竖切-第11轮.md` + `Docs/Shenxiao实施进度.md` 第 11 轮段

## 当前基线（第 11 轮已提交）

- **CLI 验证通道**:`Assets/Editor/CliVerify/CliVerify.cs`(`RenderAll`/`RenderTaskFinish`/`RenderItemTips`/`CompileCheck`),
  跑法 `Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.CliVerify.RenderAll -logFile Temp/x.log`
  (勿加 -nographics/-quit;断言行前缀 CLIVERIFY;进程码 0过/1异常/2超时/3断言败)。
  关键:`ResManager.EditorPreferFallback`(batch 域 Addressables 永不完成 → 兜底优先,仅编辑器)。
- **使用物品链**:`Proto.USE_GOODS=15050`(发 "li";回包 res/args/goods_id/goods_type_id/goods_num/hp/num/show_goods[])、
  `BagController.UseGoods`(_pendingUse 防重对标 goods_use_dic)+ `On15050`(res==1「使用成功」toast、EVT_GOODS_USE_SUCCESS、
  礼包 type 32/33/84/35 开出物「获得X」toast;CongratulationView+config_gift_box 未移植 → toast 降级,不画假数据);
  `ItemTipsView` 使用按钮(config use key"22" 显隐 + CheckSecondView 专属界面分支表 UseBranchBlocker 诚实降级 + 多个走 Confirm 先用 1 个)。
- **重复 Bind 治理**:`LayaBindFiller.EnsureBindOnWindow` 嵌套实例升级守卫 + 同类型重复清理;
  `RemoveDuplicateBinds`(菜单 神霄/UI/清理重复 Bind 组件;CLI `LayaBindFiller.RemoveDuplicateBindsCli`)已全量清理存量。
- 渲染实证:`Temp/round11_taskfinish_currency.png`(经验 250W/九洲灵钱 10000 图标格)+ `Temp/round11_itemtips_use.png`(V1体验卡 使用/关闭双按钮)。
- 服务端 15050 已核实存在(yu_server pt_150 read/write)。GM API http://223.109.142.26:88/api/ 可达。

## 已确认仍缺（按价值排序）

1. **背包增量协议**:用完物品后背包/货币不刷新(现只有 15010 全量)。老端:15000=自己单件物品推送(schema 同 15010 单项,
   ClientProtocol.json "15000";num==0 视为删除?以老端 On15000 源码为准)、15008={currency_id,num} 单货币、15009=货币列表。
   落 BagModel(增/改/删格)+ RoleModel(货币)→ EVT_BAG_UPDATE/EVT_ROLE_INFO_UPDATE。
2. **活服整合往返**(第 7~11 轮顺延):真背包+真 tips+真实例属性+真使用(15050 活服回包)。需交互 Unity+MCP 或批处理 Play(未验)。
3. **TipsManager.Toast 视觉化**:目前 log-only 壳 → 用完物品「使用成功」「获得X」玩家不可见;老端 Message.show 是浮动文字。
   最小移植:浮动文字条(复用 FZYHJW SDF;结构简单可代码建,或转老端 Message 皮)。CLI 渲染可实证。
4. **CongratulationView(开礼包展示)+ config_gift_box 同步**:show==1 礼包走恭喜获得弹层;需 ClientConfigSync SYNC_LIST 补表 + 视图。
   (CongratulationObtainViewBind 已生成,视图逻辑未移植。)
5. **BatchUseView(批量使用)**:tips 使用按钮 num>1 现 Confirm 降级用 1 个;老端是选数量界面。

## 老端源码锚点

- **15000**:`GoodsController.ts On15000`(单件物品推送 → goodsModel 更新/AddGoods;注意 num 语义与删除路径)。
- **15008/15009**:`GoodsController.ts On15008/On15009` → RoleVo 货币字段 + 事件。货币 currency_id 映射见 ConfigNotNormalGoods。
- **Message.show**:`common/Message.ts`(浮动文字,单条/滚动)。
- **CongratulationView**:`common/CongratulationObtainView.ts` + `config_gift_box`(cdn/resource/config/server/)。
- **批量使用**:`bag/BatchUseView.ts`。

## 本轮 P0：保护可运行基线

- worktree 干净(不干净先说明归属);`rg -n "USE_GOODS|UseGoods|On15050|RemoveDuplicateBinds|EditorPreferFallback" Assets/Scripts Assets/Editor -S` 命中。
- `dotnet build yu_client_unity.slnx -v:minimal` 0 错;批处理 `CliVerify.RenderAll` 退出码 0(两张截图重新生成)。
- 不重做第 11 轮,除非真实回归。

## 本轮 P1：背包增量协议闭环(15000/15008/15009)

- `Proto` 加常量 + `BagController.On15000`(按 ClientProtocol.json "15000" 全字段按序读,对标 On15010.ReadGoods 的读法与暂存策略;
  落 BagModel:同 goods_id 替换/新增,num==0 或删除语义按老端 On15000 源码为准,勿臆造)→ EVT_BAG_UPDATE。
- `RoleController`(或 BagController,按归属)On15008/On15009 → 货币落 RoleModel → EVT_ROLE_INFO_UPDATE(主界面货币条已有绑定则自动刷新)。
- 验收:dotnet 0 错 + 构造 NetReader 假包单测式验证(或 CLI 渲染背包组件喂 BagModel 后走 EVT_BAG_UPDATE 刷新)。
  ★协议字段序必须逐字段对 ClientProtocol.json,漏读/错位即整包错位。

## 本轮 P2：活服整合往返(条件允许才做)

- 交互 Unity + MCP 可用 → 按第 11 轮 P1 原文跑「登录→进场景→NPC→接/交任务→完成弹层→背包→tips→使用物品→15050 回包→背包/货币刷新」全链,
  贴截图+日志。**真实数据以活服为准,禁臆造。**
- 不可用 → 写明 blocker(缺交互 Unity/MCP),转 P3。

## 本轮 P3：Toast 视觉化(P1/P2 被卡或完成后)

- `TipsManager.Toast/Float` 从 log-only 升级为屏幕浮动文字(对标老端 Message.show 的时长/淡出;结构代码建可,样式从简勿精修),
  CLI 渲染实证(截图含浮动文字)。使用成功/获得物品的玩家可见反馈闭环。

## 红线(每轮重复)

- 不造假数据、不硬编码配置兜底;缺表先补真实配置/同步工具/读取器。
- 修通用工具优先于手改 prefab;手改必须记录原因。
- 诚实 blocker;被卡 >15min 切下一个玩家可见缺口。
- 双编译 0 错 + CLI 渲染断言过才 commit;commit 不 push。
