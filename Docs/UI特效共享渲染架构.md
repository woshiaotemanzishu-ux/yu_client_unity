# UI 特效共享渲染架构

## 目标

把 Laya `UIEffect` 资源接入 Unity UI 时，运行时资源量必须由 UI 层级决定，而不是由特效实例数决定。

- 禁止“一个特效实例对应一台 Camera 和一张 RenderTexture”。
- 业务只提交“资源、宿主、位置、缩放、旋转和差异配置”，不管理 Camera、RenderTexture 和渲染层。
- 同一 UI 作用域、同一渲染带中的全部特效共享一条渲染通道。
- Camera/RenderTexture 的数量由活跃 UI 作用域和固定渲染带决定，不再随特效实例数线性增加。
- 保留旧端坐标、水平镜像和 additive 合成语义，避免迁移业务逐个重算。

## 最终结构

```text
UIEffectStage（兼容入口）
    -> UIEffectService（实例与生命周期）
        -> UIEffectChannel(UILayer 或 UIEffectScope, Underlay / Overlay)
            -> 每个作用域、每个活跃渲染带 1 Camera
            -> 1 full-scope RenderTexture
            -> 1 full-scope additive RawImage
            -> N EffectHandle / EffectWrapper / effect prefab
        -> UIEffectProfileCatalog（公共参数 + 按资源差异表）
```

`UIEffectStage` 只保留现有调用签名，底层不再创建实例私有 Camera、RenderTexture 或 RawImage。

## 通道规则

1. 默认按宿主 `RectTransform` 所属的 `UILayer` 自动选通道。
2. 复杂窗口可在 Prefab 根节点添加 `UIEffectScope`，让窗口内特效共享窗口级通道，保持窗口自身的 sibling、遮挡和堆叠关系；不需要业务代码。
3. 每个作用域提供固定的 `Underlay`、`Overlay` 两个渲染带，常态只创建 `Overlay`；没有活跃特效时立即关闭 Camera 和 RawImage。
4. 每个通道使用 Layer 31，并在世界 Z 方向使用互不相交的固定深度切片，杜绝跨通道串拍。
5. `Underlay` RawImage 固定在作用域首位，`Overlay` 固定在末位，统一使用 `Shenxiao/UI/UIEffectAdditive` 合成。
6. 通道 RenderTexture 跟随对应作用域尺寸重建，并受全局渲染倍率和最大尺寸限制。
7. 通道整体做一次水平翻转，匹配老端相机 `rotationY=180` 的画面方向。

## 坐标与缩放

旧实现以宿主尺寸建立独立 RT；共享实现把宿主中心实时映射到通道坐标，再换算到特效世界坐标。

- `sourceHeight`：调用方 `renderSize.y`，未提供时取宿主矩形高度。
- `channelHeight`：当前 UI 层矩形高度。
- `instanceFactor = sourceHeight / channelHeight`。
- 特效局部位置和缩放先乘 `instanceFactor`，以保持旧实现的屏幕像素占比。
- 宿主的世界位移、旋转和缩放在 `LateUpdate` 同步到 `EffectWrapper`。
- 因通道画面水平翻转，映射到特效世界时 X 坐标和 Z 旋转取反。

宿主销毁时自动释放 Handle；宿主隐藏时只停止该实例渲染，粒子时间线继续推进，行为与旧离屏舞台一致。

## 配置体系

`UIEffectProfileCatalog` 是可在 Inspector 中编辑的 ScriptableObject：

- 全局项：RT 渲染倍率、最小/最大 RT 尺寸、闲置通道回收时间。
- 差异项：按 effectName 选择 UI 层覆盖、渲染带、额外位置、缩放、Y 旋转和镜像修正。
- `UIEffectSlot` 可选 profileId，常规特效保持 `default`，不需要业务代码。

配置只描述差异。公共相机、材质、坐标和生命周期规则由服务统一维护。

## 生命周期和失败处理

- 创建：获取共享通道 -> 创建轻量 Wrapper -> Addressables 实例化资源 -> 应用 Profile -> 播放。
- 隐藏：关闭该实例 Renderer，不销毁通道，也不暂停粒子。
- 释放：释放 Addressables 实例和 Wrapper；通道无实例时立即停 Camera，空闲超时后释放 RT 和通道对象。
- 分辨率变化：仅重建受影响通道的 RT，不重建特效实例。
- 异步加载期间宿主被销毁：释放已加载实例并撤销预留 Handle。
- 域重载/场景重建：服务从场景对象恢复或清理失效通道，通道 ID 不依赖静态自增值。

## 验收门槛

1. 同一作用域、同一渲染带同时播放 20 个特效时，只有 1 Camera、1 RenderTexture、1 RawImage。
2. Main + Popup + Top 同时播放时总 Camera 数不超过 3；关闭后 Camera 不渲染，闲置后 RT 被释放。
3. 战力提升扫光方向、位置、比例和 additive 亮度与当前已确认版本一致。
4. 变强按钮不再出现额外金框；任意顺序重复播放、脚本域重载后也不能跨特效串拍。
5. 720x1280、常见刘海屏和编辑器 GameView 缩放下位置稳定。
6. 连续创建/释放 100 次后 Handle 和 Addressables 实例回到基线；空闲超时后 Camera、RenderTexture 和 RawImage 一并释放。

## 已实现落点

1. `UIEffectStage` 保留兼容 API，内部已替换为共享通道服务。
2. `UIEffectProfileCatalog.asset` 负责公共参数和资源差异，`UIEffectSlot.profileId` 可在 Inspector 选择。
3. `UIEffectScope` 提供可选的界面级共享边界，解决后续复杂窗口排序，不增加业务代码。
4. 运行态诊断按“通道 + 实例”报告，并且每条共享 RT 只导出一次。
5. 编辑器压力测试覆盖 20 并发、100 次生命周期、720x1280→1024x768 重建和空闲释放。
6. 旧的实例私有 Camera/RT/RawImage 后端已经删除。

## 任务完成态特效约束

- MainUI 左侧任务条的 `ui_renwulan` 只允许在 `IsAllStepFinish(taskId)` 为真时播放，未完成任务必须释放实例。
- 任务条会按描述行数改变高度，并被列表循环复用。创建特效时必须把当次任务框尺寸作为固定 `renderSize` 传入共享舞台；尺寸变化后释放并按新尺寸重建，禁止让旧实例缩放继续乘新宿主高度。
- 老端 `ui_renwulan` 的两组粒子初速度均为 0，移动只来自 `Velocity over Lifetime` 的 ±0.3。Laya 的双常量最小值可能因等于默认值 0 而不写入 `.lh`；转换器不得用 Unity 单常量默认值（如 `startSpeed=5`）代替缺失的最小值。
- 当前运行资源允许保留针对 `ui_renwulan` 的 prefab 修正；以后重转时，转换器必须生成同样的 0 初速度，不能把修正覆盖回 0～5。
