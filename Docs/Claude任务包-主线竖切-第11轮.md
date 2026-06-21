# Claude任务包-主线竖切-第11轮

日期：2026-06-21

目标：第 10 轮把装备 tips 推到 **config 极品预览(`recommend_attr` 随机生成 N 条)+ 专有属性(`other_attr`)**、并打通**实例透传**(`ItemTipsView.Show(BagGoods)` 重载 + 格点击带 `BagGoods` 实例)——真机渲染 + RunCommand 逐值实证。
第 11 轮优先 **P1 活服整合往返**(把第 1~10 轮串成一条可见主线并真机验证;真背包 + 真装备实例「极品/强化」属性是收尾),与 **P2 完成弹层货币图标真机截图(第 7~9 轮 P3 顺延)+ 普通物品 tips GoodsTooltips 收尾**(自包含、render 可验)。如某步被真实资源/协议/运行环境阻塞,写清证据并切到下一个玩家可见缺口,不允许只做日志/文档/空 UI。

## 必读

- `AGENTS.md`、`.github/copilot-instructions.md`
- `Docs/Shenxiao编码规范.md`、`Docs/Shenxiao重构实施方案.md`、`Docs/Shenxiao进游戏链路.md`、`Docs/Shenxiao协议架构.md`、`Docs/LayaUI转换流水线.md`
- `Docs/Claude任务包-主线竖切-第10轮.md` + `Docs/Shenxiao实施进度.md` 第 10 轮段

## 当前基线（第 10 轮已提交）

- 装备 tips config 极品/专有:`GoodsModel.GetEquipRecommendAttrs`(key5 嵌套 `[{100,{color,attr_id,v2,tmpl,v4}}]` 预览)/`GetEquipOtherAttrs`(key6 `[{attr_id,val}]` 专有)/`GetBestProNum`(color 3→1/4→2/5,6→3/7→4)/`FormatAttrValue`(kind==2 万分比)/`GetAttrKind`;
  `ItemTipsView.AppendBestPro`(有实例 `equip_extra_attr` 真值,否则 config 预览)+`AppendOtherPro`(专有);TEMP 壳面板加高避遮挡。
- 实例透传:`ItemTipsView.Show(BagGoods)` 重载 + `BagItemData.Goods` + `BagItemRenderer` 格点击带实例 + `BagComponentView` 透传真实 `vo`。
- 工具:`ResManager.ReleaseInstance` editor 兜底实例 edit mode 用 `DestroyImmediate`(惠及所有编辑期 harness)。
- 双编译 0 错。RunCommand 逐值实证(晨曦/沧溟/九霄轻剑 base/recommend/other 对回真实 config)+ 2 张真机渲染截图(装备 tips 极品/专有 / 真格→真点击→实例重载 tips)。

## 已确认仍缺（按价值排序）

1. **活服整合往返(真背包 + 真 tips + 真实例属性)**:全链已就绪(协议/Model/解析/渲染/点击→tips/实例透传 `Show(BagGoods)` 重载),只缺活服回 15010 实包 → 真实物品 + 装备实例「极品 `equip_extra_attr` / 强化 `stren`」真值。
   有活服:Play 跑通「登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→30004→30001 刷新→背包 15010 真满包→点物品 tips(普通显描述/装备显基础+实例极品/强化)」并贴截图/日志。
2. **完成弹层货币图标真机截图(第 7/8/9/10 轮 P3 顺延)**:编辑期真机渲染 `TaskFinishView`(含货币奖励任务,如 经验/九洲灵钱)→ 验「九洲灵钱/经验」显**图标格**(`BaseAwardItem` 复用,非文本)。第 7 轮逻辑已就绪,缺 render 实证。**自包含、非 live 阻塞**。
3. **普通物品 tips 与老端 `GoodsTooltips` 收尾**:当前普通物品显 类型/数量/描述/获取途径;老端 `GoodsTooltips` 另有 使用/出售/批量 等按钮区与堆叠数值格式化。挑 render 可验、真实 config 驱动部分补(按钮逻辑无活服可先壳,但**不画假数据**)。
4. **装备强化加值数值(`config_equip_stren_lv`)**:`EquipToolTips.GetBaseAndStrenProStrArr` 的强化部分 = `config_equip_stren_lv[equip_type@1].attr_list` × `goods_vo.stren`。需补表进 SYNC_LIST + `GoodsModel` 解析;**实际加值需实例 stren(活服)**,config 部分(每级增量)可自包含预览。
5. **新配表 Addressables 自动分组**:`GoodsType`/`ConfigItemAttr`/`config_equip_attr`(及 config_goods)走编辑期兜底,live Play 前需「神霄/资源/Addressable 自动分组」(注意勿污染已入库 `AddressableAssetSettings`,污染则说明)。

## 老端源码锚点

- **完成弹层货币图标**(`TaskFinishView` + `BaseAwardItem`):货币/经验 type_id 经 `GoodsModel.GetMappingTypeId`(type→ConfigNotNormalGoods.goods_id,如 5→32 经验)→ 真实 goods_id → `BaseAwardItem.SetData` 显图标 + 品质底板(对标老端奖励行图标格,非纯文本)。第 7 轮 `TaskReward`/`GetMappingTypeId`/`GetNotNormalDesc` 已落,缺 render 截图实证。
- **普通物品 tips**(`common/GoodsTooltips`):`type_text`/`quantity_text`/`intro`/`ways` 已落;剩 使用/出售按钮 与 `WordManager.ConvertNum`(万/亿)数量格式化(`GoodsModel.FormatAttrValue` 同款思路,数量大数走 ConvertNum)。
- **强化加值**(`config_equip_stren_lv` + `EquipToolTips.GetBaseAndStrenProStrArr`):`cfg=config_equip_stren_lv[equip_type@"1"]`,`stren_list=ErlangParser.Parse(cfg.attr_list)`;逐 base 属性匹配 `stren_list[i][0]==attr_id` → 加值 `stren_list[i][1] * stren`(绿字 `#0a953e`)。
- **编辑期真机渲染截图法**(无 Play,第 8/9/10 轮验证用,已稳定):临时 Canvas(ScreenSpaceCamera)+ Camera(targetTexture=RenderTexture)+ `new LayerManager().Init(canvas)` + `ViewManager.Init(lm)`;
  CJK 字体 `Assets/_App/Fonts/FZYHJW SDF.asset`(渲染后强挂文本 + `ForceMeshUpdate`);`cam.Render()` + `ReadPixels` 存 PNG;`Button.onClick.Invoke()` 可在编辑期触真实点击;用完 `DestroyImmediate` + `ViewManager.Init(null)`。
  config 编辑期异步加载:`GoodsModel.EnsureLoaded()` 需编辑器 tick 推进(idle 时分多次 RunCommand 推进 `IsLoaded`,loaded 后同 Execute 内 `result.Log` 同步 dump)。

## 本轮 P0：保护可运行基线

- 确认 worktree 干净;不干净先说明改动归属(可能有 Codex/其他 worker 并行)。
- 核对第 10 轮链路仍在:`rg -n "GetEquipRecommendAttrs|GetEquipOtherAttrs|AppendBestPro|GetBestProNum|Show\(Bag" Assets/Scripts/Module/Core -S`。
- `dotnet build yu_client_unity.slnx -v:minimal` 必须 0 错;改 .cs 后 Unity 重导入重编译(域重编后 RunCommand 实证 + console 0 Error)。
- 不重做第 10 轮,除非发现真实回归。

## 本轮 P1：活服整合往返（真背包 + 真 tips + 真实例属性）

目标:把第 1~10 轮串成一条可见主线并真机验证;真背包物品 + 点物品弹 tips(普通显描述、装备显基础+实例极品/强化)是收尾。

要求:
- **有活服**:驱动 Play 跑通「登录→进场景→点 NPC(3D 立绘)→接/交任务→完成弹层(图标+货币图标格)→发 30004→30001 刷新→背包 15010 回真满包→
  `BagComponentView` 显真实物品格→点物品格弹 `ItemTipsView`(装备显基础属性 + 实例极品 `equip_extra_attr`/强化 `stren`)」;贴关键截图(真背包页 + 真物品/装备 tips)+ 日志
  (`15010 bag: cellNum/maxCell/goods=N equipWithInstAttr=M`、`EVT_BAG_UPDATE`、tips 真名/实例属性)。**真实物品/属性以活服实包为准,禁臆造/禁假背包/禁假属性。**
- **无活服**:诚实声明 blocker(同第 7~10 轮),转 P2(不在 P1 空耗)。实例透传链路第 10 轮已全通,只差活服实例数组。

最低验收:
```powershell
rg -n "class BagController|EVT_BAG_UPDATE|equipWithInstAttr|Show\(Bag" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
有活服 → 真背包页 + 真装备 tips(基础+实例属性)截图 + 链路日志;无活服 → 精确 blocker(仅缺活服实包/实装备)+ 立即转 P2。

## 本轮 P2：完成弹层货币图标真机截图 + 普通物品 tips 收尾（自包含、render 可验）

目标:把长期顺延的「完成弹层货币奖励显图标格」真机渲染验掉,并补普通物品 tips 与老端 `GoodsTooltips` 的 render 可验差距。

要求:
- **完成弹层货币图标**:编辑期真机渲染 `TaskFinishView`(喂含货币/经验奖励的真实任务奖励数据,经 `GoodsModel.GetMappingTypeId` 映射 type→goods_id)→ 验奖励行「经验/九洲灵钱」显**真实图标 + 品质底板**(`BaseAwardItem`,非纯文本)。缺图标资源则兜底 + 精确 blocker(写明缺哪个 goods_icon key)。
- **普通物品 tips 收尾**:数量大数走 `WordManager.ConvertNum`(万/亿,对标 `GoodsModel.FormatAttrValue` 同款);其余 render 可验、真实 config 驱动差距按值补。**按钮逻辑(使用/出售)无活服可先壳但不画假数据**。
- 缺资源/缺字段 → 精确 blocker;不臆造键、不画假奖励/假图标。

最低验收:
```powershell
rg -n "TaskFinishView|GetMappingTypeId|BaseAwardItem" Assets/Scripts/Module/Core -S
dotnet build yu_client_unity.slnx -v:minimal
```
Play/RunCommand 截图优先:完成弹层货币奖励显图标格(真实 config 驱动),非占位/非纯文本。

## 本轮 P3：被 P1/P2 卡住超 15 分钟的可见 fallback（按序）

1. **装备强化加值 config 预览**:`config_equip_stren_lv` 进 SYNC_LIST + `GoodsModel` 解析每级增量;tips 装备分支显「强化每级 +X」config 预览(实际加值需实例 stren,活服补)。
2. **数值格式化 + 等级红字**:tips 数量/评分大数按 `WordManager.ConvertNum`(万/亿);等级需求当 `RoleModel` 等级 < 需求标红(对标老端 level 红字,需角色数据则精确 blocker)。
3. **新配表 Addressables 自动分组**:跑「神霄/资源/Addressable 自动分组」让 `GoodsType`/`ConfigItemAttr`/`config_equip_attr`(及 config_goods)进组,验 live Play 配置加载不走兜底(注意是否动到已入库 `AddressableAssetSettings`,污染则说明)。

每个 fallback 必须带老端锚点、Unity 入口、构建结果、可见验收证据。

## 禁止事项

- 禁止纯文档/纯日志当"完成";禁止无入口/无真实数据/无验收的 UI shell;禁止假物品/假背包/假奖励/假图标/假详情/假属性。
- 禁止用 `dotnet build` 通过代替 Unity 运行通过;禁止卡 blocker 后自然退出(转 P2/P3 或写下一轮包)。
- 禁止大面积手改 generated bind;禁止绕过 ResManager 加载资源 / 字符串拼 Addressable 路径;禁止绕过 NetManager 收发 / 自写字节解码
  (格式串照抄 yu_client);**禁止凭未验证的协议/字段实现**(15010/config_goods/config_equip_attr/config_equip_stren_lv 键以 `ClientProtocol.json`/`config_table_default.json` + RunCommand 实证为准,真实物品/属性以活服实包为准)。

## 交付格式

玩家可见变化、改动文件、每个行为的 Laya 锚点、`dotnet build` 结果、Play/日志/截图/运行期单测证据、
确认 blocker(文件/协议/key/字段)、下一轮任务包草案(不写"继续完善")。
