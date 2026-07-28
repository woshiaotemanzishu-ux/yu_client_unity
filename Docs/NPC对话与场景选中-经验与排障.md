# NPC 对话与场景选中：经验与排障

> 状态：生效
>
> 最后核对：2026-07-29
>
> 适用范围：`DialogueView`、`TaskFinishView`、场景 NPC/怪物点击与选中表现

## 1. 用户可见症状

- 720×1280 编辑器预览正常，长竖屏真机的 NPC 对话底栏却被抬到屏幕中部。
- NPC、怪物能被点击和锁定，但没有老端脚下的蓝色选中反馈。
- 对话只能点右下角继续/领取；任务完成弹层点关闭只隐藏，任务未提交后又被自动任务重新弹出。

## 2. 老端证据

- `yu_client/h5/src/dialogue/DialogueView.ts`
  - `display_obj.height`、`_box_model.height` 都跟随 `stageHeight`。
  - `_img_bg`、`_box_bottom`、`_box_get_click`、`_box_skip`、`_box_go_on` 都进入同一个 `OnTaskSelect`。
- `yu_client/h5/src/task/TaskFinishView.ts`
  - 领取动作发送 `30004`；纯关闭不会消费任务。Unity 若把关闭保留为纯 `Close()`，自动任务就会再次打开弹层。
- `yu_client/h5/src/scene/Scene.ts`
  - 当前目标共用资源 `function_selection`。
  - 选中特效挂目标根，局部旋转 `-SceneObj.StartRotate.x`、缩放 `0.7`；切换目标、目标死亡/移除和切场景必须清理。

## 3. 根因

### 3.1 长屏位置漂移

`DialogueModule` 外层已经全屏伸展，但内部 `DialogueView` 仍固定为 720×1280并顶对齐。真机逻辑画布高于 1280 时，内部根的“底部”并不是屏幕底部，所以 `_box_bottom` 即使使用 bottom anchor 也会整体上移。

正确修复是让 `DialogueView`、`_img_bg`、`_box_model` 纵横铺满父级；`_box_bottom` 继续锚定内部根底边。不要按某台手机高度在运行时代码里减一个像素常量。

### 3.2 点击语义丢失

转换后的装饰 Graphic 仍参与 Raycast，而 Unity 业务只给右下角图片挂了 Button。结果是可见区域与语义点击区域分离。

本项目的确定规则是：

- 对话层只保留 Module 根作为唯一全屏点击面；所有子 Graphic 关闭 `raycastTarget`。
- 根级语义点击面必须显式挂透明 `Image` 后再交给 `UIUtil.AddClick(Graphic, ...)`；不能把零尺寸布局根直接交给容器重载，否则布局首帧可能退用第一个子 Graphic，真机点击范围会退化成局部。
- 每一页把当前“继续/领取/接取/完成/关闭”动作写入同一个 `_currentClickAction`，背景、底栏和图标不再各自维护回调。
- 点击和自动倒计时都走同一入口，并在执行前置 `_actionConsumed`，防止一次手势或连点重复发协议。
- 任务完成弹层的 Module 根和 View 根都是领取/提交面，关闭图标不再执行纯隐藏；`_submitSent` 保证 30004 单发。

### 3.3 选中数据有了但表现未接

`SceneCombat.CurrentTargetId` 已维护怪物锁定，但此前没有消费这条状态的表现层；NPC 点击甚至会先把怪物目标清零后直接寻路，因此也没有可复用的视觉状态。

`SceneTargetSelection` 现统一管理 NPC/怪物选中：只加载真实 `other_effect/function_selection`，挂 `SceneCharacterStage` 的目标 `Tilt`，用 `+38°` 抵消舞台 `-38°`，缩放保持老端 `0.7`。异步加载通过 epoch 防止旧目标回包反挂，新目标未完成建模时由 Renderer 的 `On*Ready` 补挂。

## 4. 生命周期硬边界

- 点 NPC：先清战斗目标，再 `SelectNpc`，随后寻路和开对话。
- 点怪/自动选怪：统一经 `SceneCombat.SetClickTarget` 调 `SelectMonster`。
- 同目标重复选择不重复实例化。
- NPC/怪物移除、死亡删除、目标失效、主动清目标、切场景和断线都必须清旧实例。
- 特效不能挂模型内部骨骼，否则会继承不同新模型的体量、Yaw 或动作 prefab 切换；目标 `Tilt` 才是稳定场景根。

## 5. 验收

- `DialogueInteractionCase`：在 720×1600 画布实例化真实 `DialogueModule.prefab`，断言根/背景/模型全屏、底栏贴底，并用 `GraphicRaycaster → PointerClick` 从中部、底栏和左上角三点进入生产代码的统一点击面。
- `TaskFinishInteractionCase`：实例化真实 `TaskModule.prefab`，检查 Module/View 双语义点击面与装饰层射线归属，并从面板内外两点真实点击进入生产 `OnSubmit`；测试使用空任务保护分支，不会发送协议。
- `RolePresentationEffectsCase`：检查 `function_selection` 真实 prefab 可播放，并验证 `0.7` 缩放和 `+38°` 倾斜抵消。
- 真机必须至少复验 720×1280 基准档和一档长竖屏；点击 NPC、普通怪、采集怪，确认切换/死亡/切场景无残留。

## 6. 禁止做法

- 禁止在 `DialogueView.cs` 按 `Screen.height` 动态硬改底栏坐标。
- 禁止只给右下角图标加大 Button 冒充“任意位置”。
- 禁止让关闭图标只 `Close()` 而不执行任务领取语义。
- 禁止画临时 Sprite 圈替代 `function_selection`，或把选中特效复制进每种 NPC/怪物模型。
