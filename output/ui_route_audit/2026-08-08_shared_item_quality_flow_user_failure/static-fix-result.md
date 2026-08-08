# 共享物品槽品质流光静态修复结果（2026-08-08）

## 用户证据回卷

- Unity 失败画面：`unity-bag-quality-flow-spill.png`。品质动画越过槽位，在背包整页形成巨大折线与偏移框。
- 老端目标：`old-bag-equipment-slots.png`、`old-quality-flow-slot-crop.png`。流光是围绕单个物品格四周播放的动画。
- 旧结论已通过 `results-user-failure.json` 回卷；不能复用 2026-08-07 的流光通过结论。

## 老端事实与根因

- `E:/GitProject/yu_client/h5/src/common/BaseAwardItem.ts` 的六种品质资源固定为 `ui_goods_orange/ui_goods_red/ui_goods_gold/ui_goods_pink/UI_1309/UI_1310`，调用 `AddUIEffect(..., scale=14, loop=true)`。
- `E:/GitProject/yu_client/h5/src/common/UIEffect.ts` 为每个实例按 `parent.width/height` 建立独立 RenderTexture，因此超出格子的动画网格会自动被实例 RT 裁掉。
- Unity 失败实现把相同动画放入整页共享 RenderTexture，却没有给这六种资源开启 `clipToRenderRect`；`BaseAwardItem.prefab/effect_con` 同时为左上角 0×0 宿主，造成整页越界与中心偏移。

## 本轮最小修复

- 保留原动画资源与老端 `scale=14`，没有增加静态框。
- `BaseAwardItem.effect_con` 改为以 130×130 底板为中心的 140×140 宿主；`EquipmentItem.effect_con` 保留老端四边外扩宿主。
- `BaseAwardItem` 与 `EquipmentItem` 不再传固定 `renderSize=140×140`，实例尺寸直接取各自共享 Prefab 宿主。
- 六个精确资源 profile 全部开启 `clipToRenderRect`，不影响其他 UI 特效。

## 防回归门禁

`BagInteractionCase.sharedQualityEffectSetup` 同时要求：

1. 两个共享槽的 `effect_con` 均为非零、围绕底板中心的伸展宿主；
2. 六个资源都精确命中同名 profile 且 `clipToRenderRect=true`；
3. 六个 Prefab 均仍含 `Animation` 或 `ParticleSystem`，防止再次被静态图替换；
4. 全部非空材质仍使用支持 `_UIEffectClip*` 的 `Shenxiao/Effect/LayaParticleUnlit`。

## 已执行验证

- `git diff --check`：通过。
- `dotnet build Shenxiao.Common.csproj --no-restore`：0 warning，0 error。
- `dotnet build Shenxiao.Module.Core.csproj --no-restore`：84 个既有 warning，0 error。
- `dotnet build Shenxiao.Editor.csproj --no-restore`：2 个既有 warning，0 error；新增品质流光门禁已编译。
- 资源静态核查：六组材质 shader GUID 全部指向 `Assets/Shaders/LayaParticleUnlit.shader`。

## 尚未执行

本轮未启动、控制或占用当前 Unity。修后仍须运行定向用例，隔离精确品质流光 Handle，在至少两个动画时间点保存 PNG、非透明像素和 alpha 包围盒，并抽查普通背包格、已穿戴装备槽、详情图标、共鸣边缘槽。完成前状态只能是 `needs-runtime-verify`，不能标 `done`。
