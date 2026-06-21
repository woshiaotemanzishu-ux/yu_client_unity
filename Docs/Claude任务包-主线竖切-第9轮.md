# Claude任务包-主线竖切-第9轮

日期：2026-06-21

目标：第 8 轮已把**背包真实物品页**(`BagModel` + 15010 协议 + `BagController` + 复用 `BaseAwardItem` 铺格)与
**物品点击 tips**(`ItemTipsView`:真名/真图标/品质底板/intro,对标 `UIToolTipMgr.AppendGoodsTips`)双落地
(commit `51dc670e2`),render-path 真机渲染验证(`Temp/shot_p1_bag.png` 2×2 真实物品格、`Temp/shot_p2_tips.png` 福气鞭炮详情)。
背包真实内容仅剩**活服回 15010 实包**一个 blocker(全链已就绪)。第 9 轮优先 **P1 活服整合往返**(把第 1~8 轮串成一条可见主线:
进场景→点 NPC 3D 立绘→接/交任务→完成弹层→30004→30001 刷新→**拉背包 15010 显真背包**→点物品弹 tips),
与 **P2 物品 tips 内容补全 + 装备分支起步**(对标 `GoodsTooltips` 数量/来源 + `EquipToolTips` 属性行)。
如某步被真实资源/协议/运行环境阻塞,写清证据并切到下一个玩家可见缺口,不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/Shenxiao协议架构.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第8轮.md` + `Docs/Shenxiao实施进度.md` 第 8 轮段

## 当前基线（第 8 轮已提交，commit `51dc670e2`）

- P1 背包:`BagModel`/`BagController`(`RegisterProtocal(15010,On15010)` + `EVT_GAME_START` 发 `SendFmt(15010,"h",4)`,注册进 `ControllerHub`);
  `On15010` 用 `NetReader.ReadArray(ReadGoods)` 按 `ClientProtocol.json "15010"` 逐项读(含 3 嵌套数组,只暂存 type_id/goods_num/color/cell);
  `BagItemRenderer._item=BaseAwardItem` 走真实图标;`BagComponentView.OnShow` 用 `BagModel` 铺格,模板由 `BagFlow` 注入;`EVT_BAG_UPDATE` 重铺。
- P2 tips:`GoodsModel.GetGoodsIntro`(config_goods key "2"=intro);`ItemTipsView`(真名/图标/品质底板/intro 富文本);
  `BaseAwardItem.OnClick` 默认弹 tips;`BaseAwardItem`/`BagItemRenderer` 幂等 `EnsureInit`(克隆项不经 Show 也初始化,修全部图标格点击)。
- 双编译 0 错。render-path 真机渲染验证已贴图(P1 2×2 真实物品格 + 88 数量、P2 福气鞭炮真详情)。

## 已确认仍缺（按价值排序）

1. **活服整合往返(真背包 + 真 tips 联动)**:全链已就绪,只缺活服回 15010 实包 → 真实「你的背包有哪些物品」+ 点真实物品弹 tips。
   有活服:Play 跑通「登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→30004→30001 刷新→背包 15010 真背包→点物品 tips」并贴截图/日志。
2. **物品 tips 内容补全(对标 `GoodsTooltips`)**:现 `ItemTipsView` 仅 名/图标/品质底板/intro;老端还有**数量**(quantity_text)、
   **类型文本**(type_text)、**来源**(ways/source,= config_goods `getway` key "3")。补这几项(真实 config 驱动)→ 更接近老端详情。**自包含、可独立验收**。
3. **装备 tips 分支起步(对标 `EquipToolTips`)**:装备类物品(背包主力)点击应显**属性行**(强化/基础属性),非仅 intro。
   需 `BagGoods` 保留 `equip_extra_attr`/`addition_attrlist`(现读过即弃)+ `config_equip_attr`(基础属性,按 goods_id/stage/star)。
   `UIToolTipMgr.DefaultAppendTips` 按 `type==10`(装备)路由 `AppendEquipTips`;Unity 按 `GoodsModel` type(key "9")分支。属性数据部分需活服实装备,先做 config 可证部分。
4. **完成弹层货币图标真机截图(第 7/8 轮 P3 顺延)**:本会话 config 在编辑期可加载(`GoodsModel.IsLoaded=True`),可用第 8 轮的
   编辑期渲染 + RenderTexture 截图法(见实施进度第 8 轮「验证」)渲染 `TaskFinishView`(含货币奖励任务)→ 验「九洲灵钱/经验」显图标格(非文本)。

## 老端源码锚点

- **物品 tips 内容**(`common/GoodsTooltips.ts`):字段 `goods_name`(名)、`type_text`(类型)、`quantity_text`(数量)、
  `intro`(描述,= config_goods key "2")、`ways`(来源,= config_goods `getway` key "3")、`source_txt`/`sourceGp`(来源块);
  按钮(use/sell/...)依装备/物品类型显隐。Unity 现产物:`ItemTipsView`(第 8 轮新增 TEMP 壳,在 `Module/Core/Common/Views`)。
- **config_goods 数字键**(`config_table_default.json` config_goods 字段名→下标,已实证):
  "0"type_id "1"goods_name "2"intro "3"getway(来源) "9"type "10"subtype "14"goods_icon "16"level(需求等级) "17"max_overlap(堆叠上限) "18"color。
  取键统一进 `GoodsModel`(勿散落魔法字符串;新键先 RunCommand 实证再用)。
- **装备 tips**(`common/EquipToolTips.ts` + `commonModel/EquipModel.ts` + `config_equip_attr`):装备属性行(base_rating/recommend_attr/other_attr);
  装备实例属性来自 15010 goods 的 `equip_extra_attr`/`addition_attrlist`(`ClientProtocol.json "15010"`,第 8 轮 `BagController.ReadGoods` 已按序读过、暂未留)。
- **15010 装备容器**:`SendFmt(15010,"h",1)`(pos=equip=1,见 `GoodsModel.GOODS_POS_TYPE.equip`);回包同满包结构,落 `EquipModel`(待建,镜像 `BagModel`)。
- **协议/范式**:`BaseController.RegisterProtocal/SendFmt`、`NetReader.ReadArray`、范例 `BagController.On15010`(第 8 轮)/`TaskController.On30000`。
- **编辑期真机渲染截图法**(无 Play,第 8 轮验证用):建临时 Canvas(ScreenSpaceCamera)+ Camera(targetTexture=RenderTexture)+
  `LayerManager.Init(canvas)` + `ViewManager.Init(lm)`;CJK 字体 `Assets/_App/Fonts/FZYHJW SDF.asset`(编辑期无场景 TMP,需强挂);
  `cam.Render()` + `ReadPixels` 存 PNG;用完 `DestroyImmediate` 清理 + `ViewManager.Init(null)` 复位。ResManager/Addressables + config 编辑期可用,缺图自 cdn 兜底导入。

## 本轮 P0：保护可运行基线

- 确认 worktree 干净;不干净先说明改动归属(可能有 Codex/其他 worker 并行)。
- 核对第 8 轮链路仍在:`rg -n "class BagController|GOODS_CONTAINER_INFO|ItemTipsView|GetGoodsIntro|EnsureInit" Assets/Scripts -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错;新建 .cs 必经 Unity 重导入(csproj 显式列文件)。
- 不重做第 8 轮,除非发现真实回归。

## 本轮 P1：活服整合往返（真背包 + 真 tips 联动）

目标:把第 1~8 轮串成一条可见主线并真机验证;背包真实物品 + 点物品弹 tips 是这条线的收尾。

要求:
- **有活服**:驱动 Play 跑通「登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→发 30004→30001 刷新任务栏→
  背包 15010 回真满包→`BagComponentView` 显真实物品格→点物品格弹 `ItemTipsView` 真详情」;贴关键截图(真背包页 + 真物品 tips)+ 日志
  (`15010 bag: cellNum/maxCell/goods=N`、`EVT_BAG_UPDATE`、tips 真名/intro)。**真实物品以活服实包为准,禁臆造/禁假背包。**
- **无活服**:诚实声明 blocker(同第 7/8 轮),并把可见性最大化:用编辑期真机渲染截图法(P0 锚点)演示「BagComponentView 经 BagModel 铺格 +
  背包格点击 → ItemTipsView」的**端到端 UI 联动**(渲染路径用真实 config 驱动的单元格;不把假数据塞进 BagModel)。

最低验收:
```powershell
rg -n "class BagController|EVT_BAG_UPDATE|SetItemTemplate|ItemTipsView" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
有活服 → 真背包页 + 真物品 tips 截图 + 链路日志;无活服 → 编辑期端到端 UI 联动截图(铺格→点格→tips)+ 精确 blocker(仅缺活服实包)。

## 本轮 P2：物品 tips 内容补全 + 装备分支起步（对标 GoodsTooltips / EquipToolTips）

目标:`ItemTipsView` 从「名/图标/品质/描述」补到更接近老端 `GoodsTooltips`,并为装备类起属性行(对标 `EquipToolTips`)。

要求:
- **GoodsTooltips 补全(自包含,真实 config 驱动)**:加 **数量**(传入 num,>1 显)、**类型文本**(由 type/subtype 映射文案,先 RunCommand 实证键值)、
  **来源**(config_goods `getway` key "3";先 RunCommand 实证该键内容再用)。`GoodsModel` 加对应取值(勿散落魔法字符串)。
- **装备分支起步**:`ItemTipsView.Show` 按 `GoodsModel` type(key "9")分流:装备(type==10)走属性行(先做 `config_equip_attr` 可证的基础属性;
  装备实例 `equip_extra_attr` 需 `BagGoods` 保留——可本轮把 `BagController.ReadGoods` 跳读的 3 数组改为暂存)。属性数据真实部分需活服实装备 → 缺则精确 blocker,不画假属性。
- 缺资源/缺字段 → 精确 blocker;不臆造键、不画假详情。

最低验收:
```powershell
rg -n "getway|quantity|type_text|GetGoodsGetway|EquipTip" Assets/Scripts/Module/Core/Common -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先:点物品弹更完整详情(名+图标+品质+描述+**数量+来源**);装备类显基础属性行(真实 config),非占位。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **完成弹层货币图标真机截图**:用编辑期真机渲染截图法渲染 `TaskFinishView`(含货币奖励的任务)→ 验「九洲灵钱/经验」显图标格(第 7 轮逻辑已就绪、第 8 轮证 config 编辑期可加载)。
2. **背包格覆盖件起步**:装备类背包格显 grade/star(BagItemRenderer 覆盖件,依 `config_equip_attr` stage/star);缺活服实装备则 config 可证部分 + blocker。
3. **背包子窗真数据**:一键使用/熔炼/扩展(现 log 桩)接最小真实数据或精确 blocker。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成";禁止无入口/无真实数据/无验收的 UI shell;禁止假物品/假背包/假奖励/假图标/假详情/假属性。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过;禁止卡 blocker 后自然退出(转 P3 或写下一轮包)。
- 禁止大面积手改 generated bind;禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径;禁止绕过 NetManager 收发 / 自写字节解码
  (格式串照抄 yu_client);**禁止凭未验证的协议/字段实现**(15010/config_goods 键以 `ClientProtocol.json`/`config_table_default.json` + RunCommand 实证为准,真实物品/属性以活服实包为准)。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker(文件/协议/key/字段)、下一轮任务包草案(不写"继续完善")。
