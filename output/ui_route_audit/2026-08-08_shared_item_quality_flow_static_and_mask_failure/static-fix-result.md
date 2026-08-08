# 共享物品槽品质流光：静止与 Viewport 泄漏修复记录

## 用户失败证据

- `unity-bag-static-flow-and-viewport-leak.png`：框已回到物品格四周，但同一运行画面中没有时间推进；背包内容滚出 `Viewport` 后，孤立流光框仍覆盖背包/仓库页签。
- 该证据推翻此前“单帧位置正确即可”的判断；共享槽与高频背包代表消费者均先回卷为 `defect`。

## 根因

1. `ui_goods_orange/ui_goods_red/ui_goods_gold/ui_goods_pink/UI_1309/UI_1310` 共 8 个 Legacy Animation 资产包含 80 条 `material._BaseMap_ST` UV 曲线，且没有 `_MainTex_ST` 曲线。
2. `LayaParticleUnlit.shader` 通过 `_UseBaseMapST` 决定读取 `_MainTex_ST` 还是 `_BaseMap_ST`；六个资源闭包内 27 个材质原先没有选择 BaseMap 分支，因此动画组件即使播放，画面仍固定读取未变化的 `_MainTex_ST`。
3. 品质特效由页面根共享 `RawImage` 合成，不属于背包 `ScrollRect → Viewport(RectMask2D) → Content` 的 UGUI 子树；物品内容被裁掉时，共享特效不会自动继承该遮罩。

## 最小修复

- 只给六个品质资源闭包内的 27 个材质设置 `_UseBaseMapST=1`，没有全局修改其他 UI 特效材质。
- `UIEffectStage` 缓存品质 Handle 的有效祖先 `RectMask2D/Mask`，每帧将槽位本地边界与祖先可见区求交，并把交集写入实例 shader 裁切；完全离开 viewport 时将对应 wrapper 隐藏。
- Addressables 返回未激活实例时先激活再播放 Legacy Animation；诊断增加动画数量/播放时间、有效祖先遮罩数、实际裁切矩形和可见比例。
- `BagInteractionCase` 增加真实背包 viewport 遮罩、Legacy Animation 自动播放/AlwaysAnimate、循环 UV 曲线数值变化、shader 和 `_UseBaseMapST` 消费链门禁。

## 静态结果

```text
STATIC_FLOW_AUDIT effects=6 anims=8 baseMapBindings=80 mainTexBindings=0 materials=27 baseMapEnabled=27 shaderMatched=27 autoAlways=6
git diff --check: PASS
Shenxiao.Common.csproj: 0 warning, 0 error
Shenxiao.Module.Core.csproj: 0 warning, 0 error
Shenxiao.Editor.csproj: 0 warning, 0 error
```

## 仍需运行态复验

本轮没有启动或控制 Unity，以下项目仍为 `needs-runtime-verify`，不能由编译或静态资产检查替代：

1. 同一隔离品质 Handle 在两个不同时间点保存 PNG，动画时间/材质属性前进且两帧存在明确像素差。
2. 背包格完整位于 viewport、底部部分进入裁切、完全滚出 viewport 三态；最后一态目标 Handle 的可见像素必须为 0。
3. 代表样本抽查：背包普通格、背包装备位、物品详情图标、共鸣边缘槽；样本失败才扩大同使用形态，不逐页穷举全部消费者。
4. 关闭并重开背包后流光仍会重新播放，关闭页面后没有残留 Handle 或底栏孤框。
