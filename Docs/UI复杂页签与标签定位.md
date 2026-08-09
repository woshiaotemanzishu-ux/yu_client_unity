# UI 复杂页签与标签定位

> 状态：生效  
> 形成日期：2026-08-09  
> 适用范围：弧形、放射形、折叠/展开页签，竖排文字，运行时自动尺寸标签，以及由多个短文本组成的属性行

## 结论

这类界面不能从 Laya 设计 JSON 的局部 `x/y/width/height` 直接翻译到 Unity `anchoredPosition/sizeDelta`。可靠做法是先把老端与 Unity 都归一成“页面根左上角矩形”，明确参考角、pivot、运行时尺寸和状态，再修改当前 Prefab。

成就页本轮已经验证三类高频根因：

1. 65×70 弧形子页签的老端 `(x,y)` 表示左上角；Unity 槽位使用中心 pivot 后，每项统一产生半宽 32.5、半高 35 的偏移。改为左上 pivot 后，用户确认弧形页签基本一模一样。
2. Laya 竖排 Label 的设计数据可为 `height=0`，但运行时会自动排版出真实可见高度。本页一级/二级标签实际为 22×91、22×46；照抄零高度会产生穿插和错误中心。
3. 属性行的文字节点来自空模板时，固定烤制宽度不代表真实文案宽度。把连续文字放入纯文字容器，由 TMP preferred width 驱动 `HorizontalLayoutGroup` 后，简单属性行的重叠消失。

## 标准流程

### 1. 一次采集全部相关状态

同一老端运行会话一次打开并采集：折叠/展开、选中/未选中、红点有/无、短/长数值。不要为每个标签反复截图；运行树和页面截图各保留一份即可。

每个高风险节点至少记录：

```text
node_id
state
page_rect = x/y/width/height
reference_corner
runtime_size
anchorX/anchorY/centerX/centerY
parent_scale/rotation/skew
text_bounds
```

高风险触发条件包括：非零 anchor/pivot、`centerX/centerY`、`width/height=0` 文本、父级 scale/rotation/skew、模板克隆，以及状态切换会改变数量或可见性的页签。

### 2. 统一到页面根坐标

- 老端以真实运行态的全局 `gx/gy` 或等价世界矩形为准；设计 `.scene/.json` 只用于解释字段语义。
- Unity 对 RectTransform 调用 `GetWorldCorners`，再换算到页面根坐标系，并转成左上角矩形。
- 比较 `old_rect/unity_rect/delta`。局部 `anchoredPosition` 只用于解释原因，不能作为跨父容器的验收值。

快速判别：若一组同尺寸节点的偏移都接近 `±width/2`、`±height/2`，先修参考角/pivot；不要逐节点补偿坐标。

### 3. 选择正确的 Prefab 表达

- 弧形、放射形和不等距位置：在当前 Prefab 中保存 `__Slot0..N` 具名 RectTransform 槽位，业务只把条目挂入对应槽位。不要为了“通用”强套 Linear/Grid LayoutGroup。
- 连续纯文字：使用独立文字容器和 TMP preferred width，可配合 `HorizontalLayoutGroup/ContentSizeFitter`。背景、唯一点击面、选中图和红点不得成为该 LayoutGroup 的子项。
- 运行时自动尺寸文本：把老端最终 bounds 固化成 Prefab 的初始几何；运行时代码只更新文字和必要显隐，不回写页面专用位置。

### 4. 一批修改，一次复验

同一组件的几何问题累计成一批再修改 Prefab。组件级先验证全部状态矩阵；通过后只沿原页面路线做一次整页 old/unity/overlay/diff、点击和返回复验。不得在“默认态看起来正确”后跳过展开态、红点态或长文本态。

## 验收证据

位置闸至少包含：

- 每个状态的老端和 Unity 页面根矩形；
- `reference_corner/pivot/runtime_size`；
- 差值及容差；
- 修改后的 Prefab 节点或共享组件身份；
- 同分辨率 old/unity/overlay/diff；
- 整页点击、滚动、关闭重开没有因几何修改回归。

以下证据不能单独完成位置闸：只比较局部 `anchoredPosition`、只看一张默认截图、只抄设计 JSON、只证明节点存在，或给每个子项分别写补偿值。

## 工具复用

老端继续使用 `Tools/Conversion/capture_snapshots.cjs`/运行时页面快照采集全局矩形；Unity 使用现有 `UiSnapshot` 与页面根世界角点换算。流程优化的重点不是新增第三套坐标工具，而是让两端输出同一几何合同，并在 `fix-view` 修改前强制完成高风险预检。
