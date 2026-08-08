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

`UIEffectStage.Handle.LongestLegacyAnimationSeconds` 提供实例内最长 Legacy Animation 片段的只读时长，供组合演出把外层生命周期收口到真实主体动画。它不把循环粒子视为无限生命周期，也不替代业务加载失败兜底；例如“大妖来袭”必须在 `UI_2103=1.083s` 结束时连同循环流体底纹一起释放，而 3 秒仍只负责加载异常退场。

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

### 老端参数保真与实例足迹门禁

迁移调用点时必须逐项记录老端实际消费的 `effectName/parent/position/scale/rotation/loop/renderSize`，但这些值是源引擎语义，不自动等于 Unity 最终 Transform 数值。接入前先确定效果所有者是页面、共享槽位还是模型骨骼；再结合转换后 Prefab 的基准尺度、宿主 RectTransform/世界缩放、profile 差异项和共享通道 `instanceFactor` 建立该类宿主的映射。禁止因为 Addressables 可解析、Handle 已创建或 Renderer 已启用就忽略源参数，也禁止把旧端的数值机械复制到所有 Unity 宿主。

共鸣是当前回归样例。老端共享 `EquipmentItem.SetSuitEffAni` 与页面 `EquipSuitMianView.RednerEffAni` 虽然都消费 `ui_shenzhuang01/02/03`，但前者属于装备槽的按需流光状态，后者属于页面展示链，两者不是同一个消费者。此前把老端 `scale=10` 同时直接写入共享装备格和 Unity 共鸣 Presenter，结果页面中央上下图标出现了本不应有的贴身流光框，而真正需要流光的背包装备槽又因共享 Prefab 根组件缺失完全不工作。正确处理顺序是先恢复共享组件身份和 opt-in 边界，再分别校准槽位与页面宿主；同一资源名不能作为共用最终倍率的依据。

运行态门禁必须定位到目标 Handle，临时隔离同通道其他 Wrapper，驱动该实例动画并在专用 Camera `Render()` 后读取 RenderTexture。证据至少包含目标实例 PNG、非透明像素数和 alpha 包围盒宽高；通过阈值按资源与宿主基线设置，要求形成预期的二维足迹。少量亮点、单条窄线、尺寸正确但出现在错误宿主，或其他同通道特效贡献的像素都不能证明目标特效正确。归属、位置和足迹必须同时通过；任意像素计数门禁已经失效。

## 配置体系

`UIEffectProfileCatalog` 是可在 Inspector 中编辑的 ScriptableObject：

- 全局项：RT 渲染倍率、最小/最大 RT 尺寸、闲置通道回收时间。
- 差异项：按 effectName 选择 UI 层覆盖、渲染带、额外位置、缩放、Y 旋转和镜像修正。
- `clipToRenderRect`：仅给依赖旧端实例私有 RT 边界的资源开启。共享通道通过实例级 shader 裁剪复刻旧视锥，不增加 Camera 或 RenderTexture。
- `UIEffectSlot` 可选 profileId，常规特效保持 `default`，不需要业务代码。

配置只描述差异。公共相机、材质、坐标和生命周期规则由服务统一维护。

### 物品槽品质流光的实例裁切

`BaseAwardItem.SetItemEffect` 的老端事实是 `scale=14`，资源为 `ui_goods_orange/ui_goods_red/ui_goods_gold/ui_goods_pink/UI_1309/UI_1310`。这个倍率依赖老端为每一个物品格创建独立 RenderTexture，并以 `parent.width/height` 自动裁掉格子外的动画网格；它不是一张可以直接叠到页面上的静态边框。迁入共享全屏通道后，如果只复制 `scale=14` 而漏掉实例裁切，资源中的大尺寸折线会越过物品格并散落在整页。

这六个资源必须分别命中 `UIEffectProfileCatalog` 中 `clipToRenderRect=1` 的同名 profile，继续使用原始动画 Prefab 和 `Shenxiao/Effect/LayaParticleUnlit`，不得用静态框替代。`BaseAwardItem.effect_con` 与 `EquipmentItem.effect_con` 必须是以物品格为中心、尺寸非零且略包围底板的真实 RectTransform；调用方不再硬编码一个脱离宿主的 `renderSize`，裁切尺寸直接取当前共享 Prefab 宿主。静态门禁同时检查宿主几何、六个精确 profile、动画组件和支持实例裁切的材质；运行态仍须隔离一个真实 Handle，至少在两个动画时刻保存局部 PNG 和 alpha 包围盒，确认流光紧贴四周且格外无残留。

同日第二张运行截图进一步证明“框回到格子”仍不等于修复完成：六套 Legacy Animation 共 80 条 UV 曲线只驱动 `material._BaseMap_ST`，而这 27 个品质特效材质原先没有启用 shader 的 `_UseBaseMapST` 分支，实际画面仍固定读取 `_MainTex_ST`，所以动画处于播放态也只显示静止的一帧。当前只给这六个资源闭包内的 27 个材质启用 `_UseBaseMapST=1`，不全局改写其他特效材质；静态门禁同时要求曲线有真实数值变化、材质 shader 正确且动画所写属性被当前分支消费。

共享通道的 `RawImage` 挂在页面根，不在背包 `ScrollRect/Viewport` 的 UGUI 裁剪层级里，因此物品图标滚出列表时，特效不能只靠 `RectMask2D` 自动消失。`UIEffectStage` 现在按帧把槽位自身矩形与有效祖先 `RectMask2D/Mask` 的可见区求交，再写入该 Handle 的 shader 裁剪；完全离开 viewport 时同时隐藏 wrapper。运行验收必须对同一隔离 Handle 保存两个不同时间点且像素有变化的图，并覆盖“整格可见、底部部分被裁、完全离开后零 alpha”三态，单帧位置正确、`Animation.isPlaying` 或物品图标本身已被裁掉均不能通过。

2026-08-08 最终人工复验已确认：品质框持续动态流动，背包底部滚动时按 viewport 同步裁切，完全离开后不再在背包/仓库页签与底栏留下孤框。本结论只关闭“品质流光动态与祖先裁剪”这两个被用户明确复验的缺陷，不自动替代其他共享消费者、详情页或真实 Web 的独立证据。

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
7. 每个需要视觉确认的特效都保留老端显式调用参数，并对目标 Handle 做隔离足迹检查；Handle/Renderer/纹理存在或任意少量像素不得替代实际尺寸、位置和动画可见性。

## 已实现落点

1. `UIEffectStage` 保留兼容 API，内部已替换为共享通道服务。
2. `UIEffectProfileCatalog.asset` 负责公共参数和资源差异，`UIEffectSlot.profileId` 可在 Inspector 选择。
3. `UIEffectScope` 提供可选的界面级共享边界，解决后续复杂窗口排序，不增加业务代码。
4. 运行态诊断按“通道 + 实例”报告，并且每条共享 RT 只导出一次。
5. 编辑器压力测试覆盖 20 并发、100 次生命周期、720x1280→1024x768 重建和空闲释放。
6. 旧的实例私有 Camera/RT/RawImage 后端已经删除。

## 任务完成态特效约束

- MainUI 左侧任务条的 `ui_renwulan` 只允许在 `IsAllStepFinish(taskId)` 为真时播放，未完成任务必须释放实例。
- 任务条会按描述行数改变高度，并被列表循环复用，但老端 `_box_effect` 始终保持 192×57；任务高度只参与特效 Y 缩放。Unity 必须保留这个固定宿主和固定 `renderSize`，不能把特效宿主跟随文字行数拉高。
- 老端 `ui_renwulan` 的两组粒子初速度均为 0，移动只来自 `Velocity over Lifetime` 的 ±0.3。Laya 的双常量最小值可能因等于默认值 0 而不写入 `.lh`；转换器不得用 Unity 单常量默认值（如 `startSpeed=5`）代替缺失的最小值。
- 当前运行资源允许保留针对 `ui_renwulan` 的 prefab 修正；以后重转时，转换器必须生成同样的 0 初速度，不能把修正覆盖回 0～5。
- `ui_renwulan` 的网格实际宽于 192×57 宿主；老端依靠实例私有 RT 自动裁边。共享通道必须通过 `task_completion_frame` 差异项开启 `clipToRenderRect`，禁止缩放整套资源来掩盖偏差（会连带改变两组粒子）。

## 预览工具分层

- 单个转换特效统一使用 `神霄/资源/特效管理`：直接选择任意特效 Prefab，支持播放、暂停、重播、倍速和逐帧，不为每个资源新增菜单。
- `BossBornIntro` 这类“遮罩 + UI 层显隐 + 动态特效 + 业务完成回调”的组合演出不是单个特效资源，应预览最终业务 Prefab；当前可从“神霄/重构 UI 生成器”选择对应条目，专用菜单只是同一 Preview 回调的快捷入口。
- 后续再出现组合演出，优先注册到统一 UiCreator/Prefab 预览入口；若数量明显增长，再把该入口提升为“选择任意组合 Prefab 的通用预览台”。禁止长期按每个特效复制一套相机、场景或独立预览窗口。

## UI 中的 3D 模型骨骼特效边界

- `UIEffectStage/UIEffectSlot` 只负责独立的 UI 特效；随 3D 模型骨骼运动的常驻粒子不走这条通道。
- 老端 `SetRoleModel` 不只加载模型和动作，还会读取 `SceneObjectParticle.{Body/Horse/Wing/FaBao/...}[modelId].always`。Unity 中对应职责是：实例化模型后调用 `EffectBinder.AttachAlways(model, module, modelId)`，再由 `UIModelStage` 展示整棵模型树。
- 禁止因为模型 prefab 已能显示，就假定附属粒子已内嵌。转换后的模型 prefab 与 `effect/objs/{type}` 特效 prefab 是分离资产，漏掉 `EffectBinder` 会稳定表现为“模型和动画正常、光效完全缺失”。
- MainUI 循环冲榜卡 `MainUIRankView` 已按此规则接入。截图中的古法符相 `FaBao[1011]` 会挂 `effect_fabao_1011_Bone_06` 与 `effect_fabao_1011_Bone_14`；同一入口的坐骑、剑魄、翅膀、神兵和背饰也复用同一配置链。
- `RankEffectSlot` 上的 `ui_cb01` 属于头号玩家榜的独立全屏 UI 特效，与循环冲榜 3D 模型的骨骼特效不是同一条链，排查和接入时不得混用。

## 主界面自动战斗 / 自动寻路状态特效

- 权威行为来自老端 `MainUISecondaryView.UpdateAutoStateEffect/TryUpdateAutoStateEffect`：存在未完成寻路时显示 `ui_zidongxunluzhong`；否则自动战斗开启时显示 `ui_zidongzhandouzhong`；两者都不成立时释放特效。寻路优先级高于自动战斗。
- 状态事件到达后立即刷新，不再沿用老端 440ms 合并延迟；Unity 异步 Handle 仍用版本号收编，保证快速切换时旧加载结果不会回挂。
- Unity 由 `MainRoleAgent` 在自动移动/任务跳跃开始、到达、取消、采集、技能、切场景、演出冻结和销毁等边沿写入 `AutoFightModel.AutoFindWayState`，并通过 `EVT_AUTO_FIND_WAY_STATE` 通知 UI。禁止让 View 通过轮询角色坐标猜寻路状态。
- 自动状态特效必须挂在实际进入 `MainUIModule` 的 `HudOnHook/AutoStateEffectSlot`；`HudSecondary` 已退出 `MainUIModuleCreator.Parts` 和 `MainUIFlow.FirstPassViews`，不得再向其接入运行时行为。宿主保持老端 250×200 尺寸，`__DynamicResources` 下两个 `UIEffectSlot` 互斥手动消费；共享 RT 横向镜像后，老端 X 偏移需要反号，固定 `position=(-6.8,-4)`、`scale=6.4`、`autoPlay=false`。这些静态参数同时维护在 `HudAuxiliaryCreator.GenerateOnHook` 与 prefab，业务 View 不写布局魔法数。
- 状态切换、View 隐藏、异步加载过期时必须 `Dispose` 旧 Handle；加载中的旧状态完成后也必须自弃，不能留下双特效或离屏常驻实例。

## 主界面挂机“提升”按钮扫光

- 权威行为来自老端 `MainUISecondaryView.AddOutlineEffect/RefOutlineExp`：挂机加成未点满且不存在 `buff_type == 1` 的经验 Buff 时，在 `add` 宿主以 `scale=35` 循环播放 `UI_tisheng`；已有经验 Buff 时切回静态 `_img_add`，点满时隐藏按钮，两种状态都必须释放特效。
- `UI_tisheng` 是完整按钮视觉加动态扫光，不是单独的透明高亮层。它同时包含 3 秒循环缩放和 1.333 秒循环的材质 `_BaseMap_ST.z` 偏移动画；验收必须看到真实 RT 在两个时刻均有非透明像素且像素发生变化，不能用 Prefab 已加载或 Animator 已存在替代出帧证据。
- 公共 `LayaParticleUnlit` 默认继续读取 `_MainTex_ST`，保障既有旧流光；由当前导入器产出的 `_BaseMap_ST` 动画必须在对应材质显式设置 `_UseBaseMapST=1`。禁止同时改全局默认值来碰运气，否则会让仍驱动 `_MainTex_ST` 的老资源定格。
- `UIEffectStage` 的服务对象仅在 PlayMode 使用 `DontDestroyOnLoad`；Editor/CLI 非 PlayMode 仍走相同 Camera/RT/RawImage 渲染链，但不得调用 Unity 明确禁止的持久化 API。批处理出现空 RT 时必须先检查真实异常和 Renderer 视锥位置，不能降低非透明像素门禁。
- 活动宿主固定为 `HudOnHook/ExpBoostEffectAnchor/mainui_onhook_boost_hint`，canonical key 为 `effect/objs/ui_effect/ui_tisheng/ui_tisheng`，`scale=(35,35,35)`、`autoPlay=false`。`MainUIOnHookView` 互斥消费该槽，版本号负责丢弃过期异步结果，状态切换和 View 隐藏均释放 Handle。
- 退役 `HudSecondary` 中的旧占位槽不属于运行链，不得把行为重新接回退役 Prefab。`HudOnHook.prefab` 是当前视觉唯一事实源并已退出 Creator 自动重建注册表；缺失时从 Git 恢复，不得重跑生成器覆盖槽位。
