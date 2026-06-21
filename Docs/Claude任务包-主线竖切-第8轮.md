# Claude任务包-主线竖切-第8轮

日期：2026-06-21

目标：第 7 轮已把奖励**货币也成图标格**(按真实 `goods_icon` 路由)+ **嵌套 `{career,[...]}` 职业礼包**按职业解析
(P2,commit `753ff3b39`),并把**背包真实物品页所需协议精确定位**(满背包 = 15010,格式已抄自 `ClientProtocol.json`)。
第 8 轮把背包页**真正落地**:优先 **P1 背包入口真实物品页**(`BagModel` + 15010 协议 + `BagItemRenderer` 复用
`BaseAwardItem` View,走真实 `NetManager`/`BaseController`),与 **P2 `BaseAwardItem` 点击 tips**(物品详情,复用
`GoodsModel` 真实数据)。如某步被真实资源/协议/运行环境阻塞,写清证据并切到下一个玩家可见缺口,不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/Shenxiao协议架构.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第7轮.md` + `Docs/Shenxiao实施进度.md` 第 7 轮段

## 当前基线（第 7 轮已提交，commit `753ff3b39` P2）

- P2:`TaskReward.AppendSpecialGoods` 解析嵌套 `{career,[{type,type_id,count},...]}`(抽 `AppendTriple`,按当前职业过滤);
  RunCommand 实证 career 1/2/3 → 39510031/2/3(各 ×2),无匹配 → 0 项。`TaskFinishView.BuildRewardCells` 按真实
  `config_goods.goods_icon` 路由——货币(金币31/经验32/灵玉34/绑玉35,均有图)进 `BaseAwardItem` 图标格,无图条目走文本(无回归)。
- P1 背包页 = 已定位活服 blocker(满背包协议 15010,见「老端源码锚点」),本轮未写码(核心数据链未通,不写投机解析器)。
- 双编译 0 错。

## 已确认仍缺（按价值排序）

1. **背包入口真实物品页(协议已定位,待落地)**:Unity 仅视图壳(`BagFlow`/`BagComponentView`/`BagItemRenderer` +
   `BagModule.prefab`),**无 `BagModel`、无 bag 协议**。需:① `BagModel`(持 bag_goods_list/容量)+ `BagController`(15010 发/收,
   镜像 `TaskController`/`SceneController`);② `BagItemRenderer` 复用第 6 轮就绪的 `BaseAwardItem` View
   (`SetData(type_id,count)` 真实图标+底板)+ `BagItemData.TypeId`;③ `BagComponentView.OnShow` 用 `BagModel` 铺格。
   **真包来源 = 活服回 15010**(无活服则只能写码 + 解析器单测,无法显真实物品页)。
2. **`BaseAwardItem` 点击 tips**:`SetClickCallBack` 未设时点击应弹物品详情(对标老端 `UIToolTipMgr`/`ItemInfoView`),
   复用 `GoodsModel` 真实数据;现 `BaseAwardItem.OnClick` 默认分支仅 log。**自包含、不依赖活服 → 可独立验收**。
3. **完成弹层货币图标真机截图**:第 7 轮逻辑已就绪,但本会话编辑器 Play 态 config 未加载(`GoodsModel.IsLoaded=False`)未重拍;
   待 config 加载态渲染 `TaskFinishView` 验「九洲灵钱/经验」显图标格(非文本)。
4. **活服整合往返**:登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→30004→30001 刷新→拉背包 15010。

## 老端源码锚点

- **背包协议**(主源 `h5/release/web/resource/config/client/ClientProtocol.json`,数组均 u16 计数前缀):
  - **15010 满背包**:client 送 `pos`(`h`,bag=4,见 `GoodsModel.ts` `GOODS_POS_TYPE.bag`);server 回
    `pos:h, cell_num:h, max_cell:h, cell_gold:c, goods_list:[{goods_id:l, type_id:i, sub_pos:c, cell:h, goods_num:i, bind:c,`
    `trade:c, sell:c, is_drop:c, color:c, expire_time:i, combat_power:i, stren:h, level:h, rating:i, overall_rating:i,`
    `addition_attrlist:[{attr_type:c,attr_value:i,color:c,combat_power:i}], equip_extra_attr:[{color:c,type_id:c,attr_id:h,`
    `attr_val:i,plus_interval:c,plus_unit:i}], equipStage:c, equipStar:c, skill_id:i, skill_lv:c,`
    `awake_list:[{attr_type:h,awake_lv:i,awake_exp:i}]}]`。**显示只需 `type_id`/`goods_num`/`color`,但解析须按序读完每项(含 3 嵌套数组)否则错位。**
  - **15017/15018** = 增量推送(15017 = `pos`+goods_list 同项结构;15018 = `pos`+`{goods_id,goods_num,type_id}` 精简),**非满包**,本轮可暂不接。
  - 老端 TS:`commonModel/BagModel.ts` / `commonController/BagController.ts` / `commonModel/GoodsModel.ts`(`CreateBagList`);
    送包在 `commonController/GoodsController.ts`(`SendFmt` 15010 `pos`)。**字段名/类型照抄,勿改名、勿臆造。**
- **Unity 协议范式**:`Framework/Net/BaseController.cs`(`RegisterProtocal`/`SendFmt`)、`Framework/Net/NetReader.cs`
  (`ReadU8/U16/U32/U64`、`ReadArray`;格式字符 `c=u8 h=u16 i=u32 l=u64 s=string`)、范例 `Task/TaskController.cs:On30000`、
  `Scene/SceneController.cs:On12100`(均 u16 计数数组)。
- **物品格复用**:`Common/Views/BaseAwardItem.cs`(`SetData(typeId,num,...)` → 真实图标 + 品质底板 `com_goods_plate_{color}`);
  bag 槽外覆盖件(grade/star/lock/up/down)在 `Bag/Views/BagItemRenderer.cs`(已克隆 `_tpl_BaseAwardItem` 进 `conta`,
  当前 `SetData` 只设数量未设图标 → 本轮补 `_item.SetData(TypeId,Count)`)。
- **物品 tips**:`common/UIToolTipMgr` / `ItemInfoView`(点击物品格弹详情);Unity 对照 `BaseAwardItem.OnClick` 默认分支 +
  `GoodsModel`;可工作样板 `Assets/Prefabs/UI/Common/ItemInfoItem.prefab`(根已挂 `ItemInfoItem`)。

## 本轮 P0：保护可运行基线

- 确认 worktree 干净;不干净先说明改动归属(可能有 Codex/其他 worker 并行)。
- 核对第 7 轮链路仍在:`rg -n "AppendTriple|ErlangTerm.Kind.List|GetGoodsIcon\(rewards" Assets -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错;新建 .cs 必经 Unity 重导入(csproj 显式列文件)。
- 不重做第 7 轮,除非发现真实回归。

## 本轮 P1：背包入口真实物品页（BagModel + 15010 协议 + 复用 BaseAwardItem View）

目标:点主界面背包入口 → 显示真实背包物品(格子=真实图标+品质底板+数量,复用 `BaseAwardItem`),走真实 15010 协议/`BagModel`。

要求:
- 先读 yu_client `BagModel.ts`/`BagController.ts`/`GoodsController.ts`(15010 送/收)+ `ClientProtocol.json` 15010 **格式串照抄**;
  新建 `BagModel.cs`(持 `bag_goods_list`/容量)+ `BagController.cs`(`RegisterProtocal(15010, On15010)` + 进场景后
  `SendFmt(15010,"h",4)`,镜像 `TaskController`);`On15010` 用 `NetReader` 读 `pos/cell_num/max_cell/cell_gold` + `ReadArray`
  读物品(嵌套 3 数组按序读过,显示暂存 `type_id/goods_num/color/cell`)。
- `BagItemRenderer`:`_item` 取 `BaseAwardItem`(非 `BaseAwardItemBind`),`BagItemData` 加 `TypeId`,`SetData` 调
  `_item.SetData(typeId,count)` 显真实图标。**若 `_tpl_BaseAwardItem` 内联模板缺 `BaseAwardItem` View 组件**(先 RunCommand 核)
  → 跑 `神霄/UI/回填 Bind 组件` 或改克隆 `common/BaseAwardItem.prefab`(对标 `TaskFinishView`);不点杀、不手挂。
- `BagComponentView.OnShow`:用 `BagModel` 铺格(网格/LoopScrollView 按既有范式,克隆 `BagItemRenderer`)。
- **真实物品需活服回 15010**:有活服 → Play 截图真实背包;无活服 → RunCommand 喂**真实 15010 字节序列**(活服抓包)单测解析 +
  渲染一格真实图标(对标第 6 轮 `BaseAwardItem` 验收);**不造假背包/不臆造字段**;仍缺真包则写明并保留 blocker(协议码已就绪)。

最低验收:
```powershell
rg -n "class BagModel|class BagController|15010|ReadArray|SetData\(.*TypeId" Assets/Scripts/Module/Core/Bag -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先:背包页显真实物品格(真实图标+品质底板+数量),非占位/非降级;无活服则解析器运行期单测 + 单格真实渲染 + 精确 blocker。

## 本轮 P2：BaseAwardItem 点击 tips（物品详情，复用 GoodsModel）

目标:点任意 `BaseAwardItem` 物品格(完成弹层/背包)→ 弹物品详情(名/品质/描述/图标),对标老端 `UIToolTipMgr`/`ItemInfoView`。

要求:
- 读老端 `common/UIToolTipMgr`/`ItemInfoView`(点击弹详情的内容与布局);Unity 看 `ItemInfoView` 是否已有转换产物
  (`ItemInfoItem.prefab` 根已挂 `ItemInfoItem`)可复用,缺则按任务包许可做最小原生壳(同 `TaskFinishView` TEMP 壳约定)。
- `BaseAwardItem.OnClick` 默认分支(`_clickCb==null`)由 log 改为弹 tips:经 `GoodsModel.GetGoodsBasicByTypeId` 取真名/图标/品质;
  描述文本读 config_goods(**先 RunCommand 实证描述字段的数字键**,如键 "2",勿臆造)。
- 缺资源/缺字段 → 精确 blocker,不画假详情。

最低验收:
```powershell
rg -n "OnClick|UIToolTip|ItemInfo|GetGoodsBasicByTypeId" Assets/Scripts/Module/Core/Common -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先:点真实物品格弹真实详情(真名+图标+品质+描述),非占位。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **完成弹层货币图标真机截图**:config 加载态渲染 `TaskFinishView`(含货币奖励的任务)→ 验「九洲灵钱/经验」显图标格(第 7 轮逻辑已就绪)。
2. **活服整合往返**:若会话允许,驱动「进场景→点 NPC 弹 3D 立绘→接/交任务→完成弹层(图标+货币图标格)→30004→30001 刷新→拉背包 15010」并贴截图/日志。
3. **立绘/格子构图微调**:对话立绘 scale/position/朝向按老端 talk_scale;完成弹层格子数量角标位置/样式对老端微调。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成";禁止无入口/无真实数据/无验收的 UI shell;禁止假物品/假背包/假奖励/假图标/假详情。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过;禁止卡 blocker 后自然退出(转 P3 或写下一轮包)。
- 禁止大面积手改 generated bind;禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径;禁止绕过 NetManager 收发 / 自写字节解码
  (格式串照抄 yu_client);**禁止凭未验证的协议/字段实现背包数据**(15010 格式以 `ClientProtocol.json` 为准,真实物品以活服实包为准)。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker(文件/协议/key/字段)、下一轮任务包草案(不写"继续完善")。
