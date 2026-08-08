# 共鸣物品格流光框第二次人工复查

> 复查时间：2026-08-07 16:32（Asia/Shanghai）
>
> 状态：历史第二轮诊断，已被 17:05 的槽位归属纠正部分推翻。本文只继续证明旧“任意 alpha 像素”门禁无效；“把基础 `scale=10` 同时写入共享槽和页面 Presenter”不再是当前修复结论。最新结论见 `../user_review_2026-08-07_1705_slot_ownership/review.md`，相关路线保持 `needs-runtime-verify`。

## 用户指出的精确现象

这次目标不是物品格位置、静态品质底框或外围卡片装饰，而是紧贴方形装备图标四周持续流动的金橙色 `ui_shenzhuang01/02/03` 特效。第一次修复后 Unity 仍没有形成可辨认的流光框。

证据文件：

- `user_feedback_full_flow_frame.png`：共鸣整页与红框位置。
- `user_feedback_crop_flow_frame.png`：老端物品格流光框局部特写。

## 权威源码与根因

> 以下 1～4 条记录当时的源码对照和第二轮判断。后续截图证明第 2、4 条把“老端参数存在”错误外推成“Unity 所有宿主都应直接使用同一最终倍率”；该外推已撤销。旧数值仍可作为源语义输入，但必须先区分槽位特效与页面特效所有者，再结合转换资源和宿主校准。

1. 老端 `h5/src/common/EquipmentItem.ts:672-699` 的 `SetSuitEffAni` 按阶级选择 `effBox/effBox1/effBox2`，并明确调用 `AddUIEffect(aniName, box, pos, 10, true)`。
2. 老端二、三阶宿主的 `1.3` 只是附加倍率；基础特效倍率仍然是 `10`。主界面和预览也分别在 `EquipSuitMianView.ts:478`、`EquipSuitPreviewTips.ts:118` 传入 `10`。
3. 老端 `h5/src/common/UIEffect.ts:141-149,218-221` 把数值 scale 展开为三轴并直接写入特效对象 local scale；它不是可省略的默认参数。
4. Unity 的资源、Addressables 地址、共享 `EquipmentItem` 宿主和异步 Handle 都存在。问题不是资源丢失，而是迁移时只保留了阶级附加倍率 `1/1.3`，漏掉基础倍率 `10`，所以旧门禁只能捕获约 18 个零星亮像素，人眼看不到完整流光框。

## 修复

> 本节是随后被纠正的历史实现记录，不是当前代码应满足的状态。共享装备槽的 opt-in 能力保留；`ResonancePresenter` 的基础倍率 `10` 已撤销并恢复页面独立映射。

- `EquipmentItem.SetSuitEffect` 统一按老端恢复物品格倍率：一阶 `(10,10,10)`，二阶 `(13,10,10)` 且 `x=0.5`，三阶 `(13,13,13)`；刷新、隐藏和销毁仍由共享组件释放 Handle。
- `ResonancePresenter.GetEffectScale` 同步恢复主展示与预览的基础倍率 `10`，二/三阶继续叠加老端 `1.2`。
- `ResonanceRouteCase` 不再以“Handle 存在且任意 alpha 像素不少于 8”判通过。新门禁隔离目标 `EquipmentItem._suitEffect`，单独渲染其通道，保存 PNG 并同时要求非透明像素数、alpha 包围盒宽度和高度达到可辨认二维框面；当前阈值为 `pixels>=150 && width>=24 && height>=24`。

## 验证边界

- `Shenxiao.Module.Core.csproj`：离线编译 0 warning / 0 error。
- `Shenxiao.Editor.csproj`：离线编译 0 warning / 0 error，新门禁可编译。
- 共鸣 458 节点台账已回卷为 `done=264 / blocked=80 / needs-runtime-verify=114`，更新脚本语法、通用台账自测和该台账实际校验均通过；22 个部位展示叶和全局特效叶保持待验。
- 当前用户 Unity 编辑器仍占用项目，`Library/ScriptAssemblies` 中 Core/Editor DLL 时间仍为 16:00，尚未重新加载 16:32 后的本次实现；本轮没有抢占前台、重启 Unity、另建 Library 或启动第二个 batchmode。因此不能把编译结果写成流光框已在运行态通过。
- 后续必须在新的不可变目录执行修后的真实运行路线，至少保存隔离后的 `position_ui_shenzhuang01.png`、足迹指标、整页截图和关闭/重开清理结果；旧的 `18/35/62/57` 任意像素计数不得继续作为流光框完成证据。

## 后续纠正

17:05 的用户证据同时展示了共鸣中央误加贴身框与背包共享格变灰，最终确认：流光属于明确 opt-in 的已穿戴装备槽；共鸣中央当前/下一阶属于页面独立特效链；背包空灰格的直接原因是共享 `EquipmentItem`/`BaseAwardItem` 根组件丢失。当前修复、代表性抽查矩阵和证据文件统一记录在 `../user_review_2026-08-07_1705_slot_ownership/review.md`。
