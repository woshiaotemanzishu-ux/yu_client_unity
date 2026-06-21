# Claude任务包-主线竖切-第10轮

日期：2026-06-21

目标：第 9 轮把**物品 tips 补到接近老端**(数量/类型/获取途径 + 装备 `type==10` 基础属性行:base_attrlist+真名 + 部位/阶/星/等级/职业,真实 config 驱动)、
**背包格→真实点击→tips 端到端联动**(编辑期真机渲染,数量经真实点击透传 88),并把 `equip_extra_attr` 等 3 实例数组在 `BagGoods` 暂存(地基)。
第 10 轮把装备 tips 推到**实例 + config 极品/专有属性**:优先 **P1 活服整合往返**(把第 1~9 轮串成一条可见主线并真机验证),
与 **P2 装备 tips 实例透传 + 极品/专有属性(config 可证部分)**。如某步被真实资源/协议/运行环境阻塞,写清证据并切到下一个玩家可见缺口,不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/Shenxiao协议架构.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第9轮.md` + `Docs/Shenxiao实施进度.md` 第 9 轮段

## 当前基线（第 9 轮已提交）

- P2a tips 补全:`GoodsModel` 扩键(getway"3"/type"9"/subtype"10"/equip_type"13"/career"15"/level"16"/base_attrlist"26")+ `GetGoodsTypeName`(GoodsType.type_name)/
  `GetAttrName`(ConfigItemAttr.name)/`GetEquipPosName`/`GetCareerName`/`GetGoodsGetway`/`IsEquip`;`ItemTipsView.Show(typeId,num)` 显 类型/数量/获取途径(`[]` 抑制);
  `BaseAwardItem._num` 随 SetData/SetCount 同步、`OnClick` 透传数量。
- P2b 装备分支:`ItemTipsView` 按 `type==10` 走 `AppendEquip`(部位/阶/星/等级/职业 + base_attrlist 经 `ErlangParser`+`GetAttrName` 的基础属性行 + base_rating);
  `GoodsModel.GetBaseAttrs`/`GetEquipAttr`;`BagController.ReadGoods` 暂存 `addition_attrlist`/`equip_extra_attr`/`awake_list` + stren/rating 进 `BagGoods`(`On15010` 日志 `equipWithInstAttr`)。
- 配表:`ClientConfigSync` 加 `GoodsType`/`ConfigItemAttr`/`config_equip_attr`(编辑期 AssetDatabase 兜底加载,未进 Addressables)。
- 双编译 0 错。RunCommand 数据实证 + 3 张真机渲染截图(装备 tips / 普通 tips / 背包格→点击→tips 联动)。

## 已确认仍缺（按价值排序）

1. **活服整合往返(真背包 + 真 tips + 真实例属性)**:全链已就绪,只缺活服回 15010 实包 → 真实物品 + 装备实例「极品/强化」属性。
   有活服:Play 跑通「登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→30004→30001 刷新→背包 15010 真背包→点物品 tips(普通显描述/装备显属性)」并贴截图/日志。
2. **装备 tips 实例透传 + config 极品/专有属性**:现 tips 仅收 `typeId`(`equip_extra_attr`/`stren` 在 `BagGoods` 暂存但未进 tips)。
   ① 把点击的 `BagGoods` 实例透传进 tips(`BaseAwardItem` 持 goods 实例引用或回调带实例)→ 实例「极品 `equip_extra_attr` / 强化 `stren`」属性行(需活服实装备,缺则精确 blocker);
   ② **config 可证部分**:`config_equip_attr` 的 **极品预览 `recommend_attr`(key 5)** + **专有属性 `other_attr`(key 6)** 解析显示(对标 `EquipToolTips.SetBestPro`(无实例时 `recommend_attr` 预览「随机生成 N 条」)/`SetRedPro`(`other_attr` 专有))。**自包含、可独立验收**(先 RunCommand 实证非空样本)。
3. **数值格式化 + 等级需求红字**:base_attrlist 大数(如 5300000000)按 `WordManager.ConvertNum`(万/亿)显;等级需求当 `RoleModel` 角色等级 < 需求时标红(对标 GoodsTooltips/EquipToolTips level 红字)。
4. **完成弹层货币图标真机截图(第 7/8/9 轮 P3 顺延)**:编辑期真机渲染 `TaskFinishView`(含货币奖励任务)→ 验「九洲灵钱/经验」显图标格(非文本)。

## 老端源码锚点

- **装备 tips 极品/专有**(`common/EquipToolTips.ts` + `config_equip_attr`):
  - `SetBestPro`:无实例 → `cfg.recommend_attr`(`config_equip_attr` key 5)经 `ErlangParser.Parse` → 极品属性预览「随机生成 N 条」(N 按 color:3→1/4→2/5,6→3/7→4,见 `GetBestProNum`);有实例 → `goods_vo.equip_extra_attr` 经 `equip_model.shortExtraAttrColor`。
  - `SetRedPro`:`cfg.other_attr`(key 6)经 Parse → `Util.GetAttrStr(list)` 专有属性行。
  - `GetBaseAndStrenProStrArr`:`basic.base_attrlist` 基础属性(第 9 轮已落)+ `config_equip_stren_lv[equip_type@1].attr_list` × `goods_vo.stren` 强化加值(需实例 stren)。
  - 评分 `score`=`goods_vo.rating`(实例)兜底 `config_equip_attr.base_rating`;部位/职业/等级第 9 轮已落。
  - **`recommend_attr`/`other_attr` 格式先 RunCommand 实证非空样本**(多数基础装备为 `"[]"`;格式疑同 base_attrlist `[{attr_id,val},...]`,勿臆造)。
- **实例透传**:`BagGoods`(第 9 轮已存 `ExtraAttrs`/`AdditionAttrs`/`AwakeList`/Stren/Rating)→ 需 `BagItemRenderer`/`BaseAwardItem` 点击带 `BagGoods` 实例到 `ItemTipsView.Show`(现仅 typeId)。
- **数值/属性名**:`WordManager.ConvertNum`(万/亿)、`GoodsModel.GetAttrName`(第 9 轮已落 ConfigItemAttr.name)。
- **协议/范式**:`BaseController.RegisterProtocal/SendFmt`、`NetReader.ReadArray`、`BagController.On15010`(第 9 轮已存 3 数组)/`TaskController.On30000`。
- **编辑期真机渲染截图法**(无 Play,第 8/9 轮验证用):临时 Canvas(ScreenSpaceCamera)+ Camera(targetTexture=RenderTexture)+ `LayerManager.Init(canvas)` + `ViewManager.Init(lm)`;
  CJK 字体 `Assets/_App/Fonts/FZYHJW SDF.asset`(渲染后强挂 tips 文本 + `ForceMeshUpdate`);`cam.Render()` + `ReadPixels` 存 PNG;`Button.onClick.Invoke()` 可在编辑期触真实点击;用完 `DestroyImmediate` + `ViewManager.Init(null)`。

## 本轮 P0：保护可运行基线

- 确认 worktree 干净;不干净先说明改动归属(可能有 Codex/其他 worker 并行)。
- 核对第 9 轮链路仍在:`rg -n "GetGoodsTypeName|GetBaseAttrs|GetEquipAttr|AppendEquip|class EquipExtraAttr" Assets/Scripts/Module/Core -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错;改 .cs 后 Unity 重导入重编译(域重编后 RunCommand 实证 + console 0 Error)。
- 不重做第 9 轮,除非发现真实回归。

## 本轮 P1：活服整合往返（真背包 + 真 tips + 真实例属性）

目标:把第 1~9 轮串成一条可见主线并真机验证;真背包物品 + 点物品弹 tips(普通显描述、装备显属性)是收尾。

要求:
- **有活服**:驱动 Play 跑通「登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→发 30004→30001 刷新→背包 15010 回真满包→
  `BagComponentView` 显真实物品格→点物品格弹 `ItemTipsView`(装备显基础属性 + 实例极品/强化)」;贴关键截图(真背包页 + 真物品/装备 tips)+ 日志
  (`15010 bag: cellNum/maxCell/goods=N equipWithInstAttr=M`、`EVT_BAG_UPDATE`、tips 真名/属性)。**真实物品/属性以活服实包为准,禁臆造/禁假背包/禁假属性。**
- **无活服**:诚实声明 blocker(同第 7~9 轮),并把可见性最大化:用编辑期真机渲染 + 真实点击演示「真实 config 物品格 → 点格 → tips(装备基础属性 + config 极品/专有)」端到端联动
  (渲染路径用真实 config 单元格;不把假数据塞进 `BagModel`,不画假实例属性)。

最低验收:
```powershell
rg -n "class BagController|EVT_BAG_UPDATE|equipWithInstAttr|ItemTipsView" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
有活服 → 真背包页 + 真装备 tips(基础+实例属性)截图 + 链路日志;无活服 → 编辑期端到端 UI 联动截图(铺格→点格→tips 含 config 极品/专有)+ 精确 blocker(仅缺活服实包/实装备)。

## 本轮 P2：装备 tips 实例透传 + 极品/专有属性（对标 EquipToolTips SetBestPro/SetRedPro）

目标:`ItemTipsView` 装备分支从「基础属性」补到 **config 极品预览 + 专有属性**,并打通**实例透传**地基(为活服实装备的极品/强化属性行铺路)。

要求:
- **config 极品/专有(自包含,真实 config)**:`GoodsModel` 加 `GetEquipRecommendAttrs`(`config_equip_attr.recommend_attr` key5)+ `GetEquipOtherAttrs`(`other_attr` key6),
  同 `GetBaseAttrs` 走 `ErlangParser`+`GetAttrName`(**先 RunCommand 实证非空样本的格式**,空 `"[]"` 跳过);`ItemTipsView.AppendEquip` 加「极品属性(预览 N 条)」「专有属性」段(缺则不显,不占位)。
- **实例透传地基**:`BaseAwardItem`/`BagItemRenderer` 点击带 `BagGoods` 实例(或 typeId+实例引用)到 `ItemTipsView.Show` 重载;装备有实例 `equip_extra_attr`/`stren` → 显实例极品/强化行(真实例缺活服 → 精确 blocker,不画假属性)。
- 缺资源/缺字段 → 精确 blocker;不臆造键、不画假属性。

最低验收:
```powershell
rg -n "recommend_attr|other_attr|GetEquipRecommendAttrs|equip_extra_attr|ShowWithGoods" Assets/Scripts/Module/Core/Common -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先:装备 tips 显基础 + config 极品/专有属性(真实 config),非占位;实例极品/强化缺活服则精确 blocker。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **数值格式化 + 等级红字**:base_attrlist 大数按 `WordManager.ConvertNum`(万/亿);等级需求当 `RoleModel` 等级 < 需求标红(对标老端 level 红字)。
2. **完成弹层货币图标真机截图**:编辑期真机渲染 `TaskFinishView`(含货币奖励任务)→ 验「九洲灵钱/经验」显图标格(第 7 轮逻辑已就绪)。
3. **新配表 Addressables 自动分组**:跑「神霄/资源/Addressable 自动分组」让 `GoodsType`/`ConfigItemAttr`/`config_equip_attr`(及 config_goods)进组,验 live Play 配置加载不走兜底(注意是否动到已入库 AddressableAssetSettings,污染则说明)。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成";禁止无入口/无真实数据/无验收的 UI shell;禁止假物品/假背包/假奖励/假图标/假详情/假属性。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过;禁止卡 blocker 后自然退出(转 P3 或写下一轮包)。
- 禁止大面积手改 generated bind;禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径;禁止绕过 NetManager 收发 / 自写字节解码
  (格式串照抄 yu_client);**禁止凭未验证的协议/字段实现**(15010/config_goods/config_equip_attr 键以 `ClientProtocol.json`/`config_table_default.json` + RunCommand 实证为准,真实物品/属性以活服实包为准)。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker(文件/协议/key/字段)、下一轮任务包草案(不写"继续完善")。
