# UI 分辨率适配手册

> 目标:像老端一样,同一套 UI 同时适应 web(宽屏)与手机(长屏)。
> 本文是这条工作线的唯一事实源,动 UI 锚定前先读这里。

## Web 画布与弹窗遮罩（2026-08-04）

- Web 模板的 `html/body/#unity-container/#unity-canvas` 必须跟随 `100vw × 100dvh`，窗口变化时 Canvas 继续铺满浏览器可视区；竖版 720×1280 页面仍按移动端设计居中显示，左右由场景或外层背景补齐，不改全局 `CanvasScaler`。
- “页面主体保持移动端版式”不等于“遮罩也只有 720 宽”。需要拦截整页点击的普通半透明遮罩使用通用 `RootCanvasRectFitter`，运行时把自身四个世界角精确贴合 `rootCanvas`；不得按 WebGL/移动平台分支写死尺寸，也不得在页面代码里重建视觉树。
- `SettingModule.prefab` 的 `SettingView/ModalDim`、`SettingChangeHeadView/ModalDim`、`SettingChangeNameView/ModalDim` 均直接保存该通用组件。1280×720 浏览器实包已验证主设置与改名弹窗左右扩展区均完整压暗；CLI 同时校验三个遮罩的运行态世界角与根 Canvas 一致。

---

## 一、老端是怎么做到的

老端(Laya)的适配是三层结构,不是"整体缩放一张设计稿":

| 层 | 老端做法 | 证据 |
|---|---|---|
| ① 画布缩放 | `scaleMode = fixedauto`:短边贴合、长边溢出,不裁切不变形 | `h5/laya/.laya`、`h5/src/GameConfig.ts`;引擎实现 `cdn/libs/laya.core.js:17430-17439` |
| ② 层容器 | `LayerManager.UpdateUILayer()` 把 UIRoot 及以上所有层拉成满 stage | `h5/src/common/LayerManager.ts:216-229` |
| ③ **view 根锚定** | **基类运行时代码**设 `centerX/centerY/left/right/bottom` | `BaseView1.ts:263-265`、`BaseWindowComponent.ts:139-143` |
| ④ view 内部子元素 | 绝对 `x/y` | `.scene` 典型模式:容器 `centerX:0` + 内部元素绝对坐标 |

**关键认知:老端也不是每个元素都自适应的。** 只有"区域根"贴边,区域内部保持绝对坐标。
所以 Unity 侧那些左上锚的子节点**是对的,不用动** —— 问题只出在 view 根这一层。

### Unity 侧的对应关系

- ① **已经对齐**:`Launch.unity` 的 CanvasScaler = `ScaleWithScreenSize` + `720x1280` + **Expand**,
  与 `fixedauto` 语义等价。上游事实源是 `Assets/Scripts/Framework/Config/AppConfig.cs`
  的 `designResolution` / `canvasMatch`,`LaunchSceneCreator.cs` 据此烤出 scaler。
  **改 AppConfig 必须同步改 CliVerify 里的常量**,否则验收舞台与线上发散。
- ③ **这层丢了** —— 因为它在老端写在 TS 运行时,而我们的转换器只读 `.scene` 数据。这是全部根因。

### 登录舞台：宽屏补左右，长竖屏扩高度（2026-07-28）

登录模块的外壳是一个特例：页面以 720 设计宽为基准，但不能把承载页固定成 720×1280。
旧实现把 `LoginStage/Viewport720x1280` 固定为 720×1280 并居中；在 1224×2700 真机上，
`Expand` 的缩放倍数由宽度决定为 1.7，固定视口只能覆盖 2176px 高，因而上下各露出约 262px 外层背景。

现行约束如下：

- `LoginStage` 继续铺满 Window 层；`Viewport720x1280` 固定宽 720，纵向锚点为 0..1、`sizeDelta.y=0`。
- 长竖屏下 Viewport 随父级增高，页面不再出现上下补边；横屏/PC 宽屏下父级逻辑高度仍为 1280，
  Viewport 保持 720×1280，只在左右露出 `WebBackground`。
- `LoginPanel/Bg` 使用 `AspectRatioFitter.EnvelopeParent` 按原图比例 cover：允许裁掉超出的左右画面，
  不允许拉伸人物，也不允许露出上下空隙。
- `LoginStage` 已进入人工精修阶段，背景的 `Source Image`、`Image.Type`、颜色及
  `AspectRatioFitter` 全部直接保存在 `LoginStage.prefab/WebBackground`；页面专用
  `LoginStageCreator` 已删除。原有宽高比适配保持不变：`LoginStage` 只按 Prefab 当前图片的原始尺寸
  同步 `AspectRatioFitter.aspectRatio`，不加载或替换图片。不得改全局
  `CanvasScaler`，也不得在 `LoginFlow` 里按平台或屏幕尺寸计算布局。

| 屏幕 | Expand 后逻辑画布 | 登录视口结果 |
|---|---:|---|
| 720×1280 | 720×1280 | 720×1280，基准档不变 |
| 1080×2400 | 720×1600 | 720×1600，上下铺满 |
| 1224×2700 | 720×1588.24 | 720×1588.24，上下原 262px 补边归零 |
| 1920×1080 | 2275.56×1280 | 720×1280 居中，只补左右 |

---

## 二、根因分四层

1. **源头**:老端 2058 个 `.scene` 里只有 79 个(3.8%)根节点带相对布局属性,其余 86% 的锚定
   来自三处代码:`is_center`(523 个 view 类)、`BaseWindowComponent` 的 `bottom=0+centerX=0`
   (80 个业务大窗共用一个 `BaseWindowSkin`)、45 个子类自己覆写。
2. **转换器**:`LayaSceneConverter.BuildRoot` 只读 scene props,压成二值 —— 有显式锚就翻译,
   否则**无条件居中**。注释自称"沿用 is_center 居中默认",但它从没读过 `is_center` 的值。
   756 个 view-prefab 里 692 个(91.5%)落进这条兜底:511 个碰巧对,**181 个反向错**。
3. **MainUI 特有**:走 UiCreator,几何源是 720×1280 的**单点运行时快照**。在这一个采样点上
   `centerX=-1` 与 `left=209` 完全等价、快照分辨不出,于是一律烤成左锚绝对坐标。
   父节点水平拉伸 → 宽屏下整簇左漂 `(实际宽-720)/2`。
4. **验收面结构性失明**:所有验收手段恰好只跑 720×1280 一档,且 CliVerify 漏设
   `screenMatchMode`(默认 MatchWidthOrHeight ≠ 线上 Expand)。**这批锚定错误从未被任何一次验收看见过。**

> 注:曾统计出"68% 节点是 `{0,1}` 左上锚"—— 那是**子节点**统计,是 `LayaRectMath` 的正常兜底
> (Laya 子节点本就是父左上坐标系),**不要去动**。view 根节点才是 91.5% 被错误居中。

### 页面坐标与局部锚点不是一回事（2026-08-04）

同一张截图中的最终位置必须用页面根坐标比较。`anchoredPosition` 只在“父 Rect、锚点、pivot、scale 全部相同”时可直接互比；跨父容器时局部数字没有视觉意义。

标准取证方法：调用目标 `GetWorldCorners`，再用页面根 `InverseTransformPoint`，输出相对页面左上角的 `(x,y,width,height)`。本轮套装竖牌的父容器已在 x=599，子图继续右锚 x=-45，最终页面 x=664；只看子图局部值不会暴露 65px 偏移。修复应落在当前 Prefab 的锚点/位置，运行时代码不写坐标。

列表区域还要把几何和结构分开验：Viewport 的页面矩形对齐，只能证明窗口位置；还必须存在 `RectMask2D`、正确 Content 和自适应尺寸，并用真实拖动证明内容可达。

---

## 三、铁律

1. **基准档零位移**:每一处改动都是"把老端相对布局属性换算成等价 Unity 锚",
   720×1280 下最终屏幕矩形必须不变。任何在基准档产生位移的改动都要单独解释并截图确认。
   这是分清"哪些是修好的、哪些是改坏的"的唯一手段。
2. **只改 anchor,不改 pivot**:pivot 一起改会连带影响子节点,且调用点数值全要重算。
   保持 pivot 不动,换算只落在 anchor + anchoredPosition 上。
3. **数值取快照实测值,不直接抄老端字面量**:快照精度更高。例如 `_box_help` 实测中心 195.5,
   老端 `centerX=-165` 折合 195,直抄会引入 0.5px 位移。
4. **安全区有意不镜像老端**:统一用 Unity 的 `SafeAreaRoot`(真实 `Screen.safeArea`)。
   老端 `Util.GetLiuhaiHeight()` 是硬编码 60 + 静态缓存永不更新 + 只在 h/w≥2 移动端生效,
   是老端 bug。遇到 `top=GetLiuhaiHeight()` 一律折算成 0,**绝不烤成 60px 偏移**
   (会与 SafeAreaRoot 叠成双倍内缩)。**后续轮次不要把这条当 diff 改回去。**
5. **快照优先**:`.scene` 设计值会跑偏,以运行时快照为准。
6. **区分首转与精修**：尚无可编辑 Prefab 的界面才通过转换器或 Creator 首次成形；已落袋、
   已人工调整的界面直接修改 Prefab，页面专用 Creator 必须退出重建注册表。跨页面公共问题
   修改共享 Prefab 或幂等公共升级器；布局和静态图片归 Prefab，运行时 C# 不写视觉参数。
7. **不要扩 `LayerManager.SafeAreaLayers` 把 Window 整层内缩** —— 该文件注释已记录整层内缩
   会让满铺遮罩盖不住刘海角。安全区只挂到需要的 view 根上。

### 铁律 6 与老端做法的冲突怎么裁决

表面上"布局归 prefab、运行时不写位置"和"老端锚定恰恰是运行时基类设的"互斥,实际不互斥:
老端那批赋值是**声明式常量**而非动态计算,执行一次后完全交给 Laya 的 Widget 引擎随父矩形重算。
Laya 的 `centerX/left/right/top/bottom` 与 Unity 的 `anchorMin/anchorMax/offset` 是同一套
"相对父矩形"代数。**所以把它折叠进 prefab 锚点是等价变换,两条铁律都不破。**

裁决:凡是构造期的字面量赋值,一律折叠进 prefab;凡是依赖运行时输入(点击点、屏幕实测、变量)的,
才允许写运行时代码。不可折叠的动态项白名单只有 4 类:点选式 tooltip/菜单(8 个)、
`FightingUpView` 的 `bottom=运行时变量`、`TopPlayerTipItem` 的贴屏幕边缘翻转、
`FunctionOpenIcon` 的进出场动画。

---

## 四、进度

### 已完成

| 批次 | 内容 | 提交 |
|---|---|---|
| — | 存档主界面手改(重跑 Creator 前保命) | `65393a5ea` |
| A-防护 | 转换器黑名单 + CLI 入口 + CliVerify 多分辨率标尺 | `5ce872fa1` |
| A/B | MainUI 贴边锚定 + 手改回写 Creator + BaseWindowSkin | `9e28a18a0` |
| A/B 产物 | MainUI 7 区域重烤落盘(实物验证通过) | `29945a3c7` |
| **C** | **转换器根锚定推导链(代码已就绪,待灰度重转)** | `9c8a6bbdd` |
| Login | 登录视口固定宽、纵向铺满；登录背景等比 cover | 本轮 |

**MainUI 逐区域最终状态**(✅=已按老端语义贴边,➖=本来就对):

| 区域 | 老端语义 | 状态 |
|---|---|---|
| HudSecondary 11 子簇 | 8×centerX + 2×right + 1×真左锚 | ✅ |
| RightIconSlot 右侧功能列 | `right=0 + centerY=250` | ✅ 锚屏幕中线 |
| MarriageItem | `right=-10` | ✅ |
| HudNotice | 随底边(挂锚底的 SecondaryView 下) | ✅ 垂直锚翻回底边 |
| HudChatBar | `centerX=0` + 固定 720 | ✅ 去掉横向拉伸 |
| HudOverlayCombat 大血条 | 右上贴边 | ✅ |
| HudOverlayCombat 特效层 | 四边铺满 | ✅ 改真 Stretch |
| HudOverlayCombat 进度条 | `centerX=0 + bottom=450` | ✅ ⚠ 基准档移位 276px(见下) |
| HudTop / HudRank / HudFuncOpen / HudTaskTeam / HudSkillBar / HudAutoBrush / HudNavBar | — | ➖ 语义已正确 |
| HudJoystick | root 当坐标系用 | ➖ 功能等价;**警告:加 Mask 或布局组件会立刻裁掉转盘** |

**已知的有意位移**:`DropProgress` 中心距底 190→466。原值是 Creator 自己标注的占位猜测
("无运行态佐证,占位默认屏幕位置"),老端 `bottom=450` 才是真值。验收时单独截图确认。

### 待办

| 批次 | 内容 | 预估 | 前置 |
|---|---|---|---|
| **C-重转** | 按灰度重转 prefab,验收清单见下 | — | **需在编辑器里操作** |
| **D** | 13 个全屏战斗 view 四边贴边 + 安全区落到 view 根;例外表人工裁决 | 1 人日 | C-重转 |
| **E** | 平台面:WebGL 模板跟随窗口、宽屏底图、放行横屏、文档 | 1–2 人日 | **已完成 1280×720 Web 实包复验；更多设备档持续回归** |
| 附 | 吸收 manifest 的 1190 条无关漂移(见下) | 0.5 人日 | 独立于主线 |

#### 批次 C 重转的验收清单

灰度顺序:**外科式单窗口 → 3~5 模块 → 20 模块 → 全量**。每档都跑 720×1280 + 1080×2400。

1. **最危险的失效模式**:转换报告「## 分类统计」里 `根锚定/⚠兜底居中(推导不出)` 应约 **8**、
   `根锚定/⚠共用件冲突(维持现状待裁决)` 应为 **4**。
   若前者接近 0 且「左上绝对定位」数暴涨,说明 `isCenter` 字段丢了 ——
   **那会把 483 个本来就对的 view 全改成左上**。
2. `git diff` 应只出现 **68 个** view-prefab 的根 RectTransform 变化;
   **483 个 fast-center 必须零 diff**(这是分清"修好的"与"改坏的"的标尺)。
3. 53 个基准档真位移需在 1080×2400 / 1920×1080 目视验收,其中 3 个 `is_center=true` 的优先看:
   `friendInvite/FriendInviteView`、`outline/OutLineView`、`seaHegemony/SeaFightSettleView`
   —— 已读 `laya.ui.js:242` 原文确认是 scene 显式锚被 `is_center` 遮蔽的**有意修正**,非回归。
4. `patched` 名单 4 条重烤后**必须手动重跑** `InnateSkillCreator` / `FriendBindUpgrader` /
   `JewelBindUpgrader`,否则天赋页、好友私聊窗、骸珀镶嵌页缺业务组件。

#### 批次 C 的实现要点(已落地,供日后维护参考)

推导链优先级(低→高):`scene props` → 基类默认 → `manifest.rootLayout` → `ui_root_layouts.json`。
基类默认:`tsChain` 含 `BaseWindowComponent` → `{centerX:0,bottom:0}`;`isCenter=true` → 走
**原样不动的快路径**;两者都无 → 保持左上兜底(这条正是修掉反向错的关键)。

几个不能动的约束:

- **`isCenter` 必须单列字段,不能折进 `rootLayout` 的 `{centerX:0,centerY:0}`** ——
  折进去就得走 `LayaRectMath` 才能复现居中,而那会把 pivot 从 `(0.5,0.5)` 打成 `(0,1)`、
  `anchoredPosition` 全变,炸出 495 个 prefab 的 diff 噪声,零位移证明作废。
- **合并结果必须走 clean-props 通道,不与 scene props 混合。**
  `LayaRectMath` 的水平分支顺序是 `left&&right > centerX > right > left`,
  而 Laya 实测(`laya.ui.js:242` `resetLayoutX`)是 `centerX` 绝对优先、`left+right` 仅在
  `centerX` 为 NaN 时拉伸。两者只在 `centerX` 与 `left+right` 共存时分叉,当前全库该组合命中 0
  所以无感,一旦合并就会变成真 bug。走 clean-props 天然规避,`LayaRectMath` 一行未改。
- **刻意不提取 `scaleX`/`scaleY`**:实测 50 余处 scale 赋值绝大多数是开场/补间动画起始值
  (`BaseView1` 的 0.825)。静态折叠会造成全局缩放事故 —— 0.825 一旦沿链下发,全部 view 都缩水。
- **逻辑落在 `ApplyRootLayout` 而非 `BuildRoot` 本体**:`BuildRoot` 另有两个调用方
  (`CollectInlineTemplates` 的 715 个内联 item、`Baker.BakeViewTree` 的快照烤图),
  它们的几何已是运行时绝对值,再叠基类语义会双重偏移。
- **`shared-prefab`(160 个)整体跳过推导链**:`ConvertOne` 里 `BuildWindow` 之后紧跟
  `NormalizeItemRoot` 无条件抹平,推导只是白算再被抹掉。
- **`FIX_ITEM_CHAIN_ROOTS = false`**:链上无 `BaseView1` 的 128 个 view-prefab
  (大窗的子页 item,由父容器 `addChild` 挂入、从不参与 `is_center`)本批次只打标记不改行为。
  核实父容器语义后把该 const 改 `true` 即放行。
- **生成器以 `ui_manifest.json` 的 scenes 为索引反查 TS,不要以 TS 类为索引正推** ——
  view 类比 scene 条目多,正推会为不存在的键写配置。

#### manifest 的 1190 条无关漂移(待单独处理)

`yu_client` 的 TS 源自 06-11 以来变过,直接跑 `python Tools/LayaUI/analyze_layaui.py` 会带来
1190 个 scene 的差异(`otherRefFiles` 1166 / `skinSource` 57 / `missingSkins` 55 / `tsClass` 24)
与 14 个 decision 翻转,summary 从 view-prefab 756 漂到 754、shared-prefab 160 漂到 167。

**这个不一致在批次 C 动手前就已存在。** 批次 C 为了不污染"基准档零位移"的可证明性,
采用了**只增不改的嫁接**(索引口径与脚本完全一致)。所以当前仓库的 manifest ≠ 今天全量跑脚本的产物。
待适配主线收口后单独开一轮吸收漂移,那时直接跑脚本即可得到同样的 4 个新字段。

---

## 五、验收

### 五档标准采样

`CliVerify` 已支持 `-cliVerifyWidth` / `-cliVerifyHeight`,默认仍 720×1280:

| 分辨率 | 用途 |
|---|---|
| 720×1280 | 基准档,**应逐像素零 diff** |
| 1080×2400 | 主流长屏手机 |
| 750×1334 | 9:16 短屏 |
| 1280×720 | 横屏 |
| 1920×1080 | PC 宽屏 |

非基准档截图自动加 `_宽x高` 后缀;非标准档会告警(兜住"只传宽漏传高"的 footgun)。

### 重烤 prefab 的风险与灰度

- 全量重烤会覆盖已入库 prefab。**必须**:git 工作区干净、单独开分支、逐模块看 diff 而非目录级 add。
- 灰度顺序:`ReconvertWindowInGroup` 外科式单窗口 → 3~5 个模块 → 20 个模块 → 全量。
- 黑名单里 `patched` 那 4 条(RoleModule/FriendModule/FriendChatItem/JewelModule)按设计**不拦截**,
  全量重烤后**必须手动重跑** `InnateSkillCreator` / `FriendBindUpgrader` / `JewelBindUpgrader`,
  否则天赋页、好友私聊窗、骸珀镶嵌页缺业务组件。
- prefab 重写会让内部子对象 fileID 变动,可能连坐 Addressables 分组。重烤后跑一次
  内存与首包体积对账,与 `Docs/打包发布手册.md` 的基线(内存 800、首包 24.7MB、冷访 66s)比对。

---

## 六、存疑项(需实测裁决)

1. **`UIEffectStage` 的 `orthographicSize`**:两路调查结论直接对立 —— 一说 `GetStageHeight`
   钉死 1280 导致 20:9 下特效偏小 25%,一说钉死恰恰复现老端净行为(老端 RT 按原生像素居中贴回,
   stage.height 能约掉;Unity 侧 RawImage 拉满 parent,约不掉)。
   **裁决:先不改。** 验证方法:1080×2400 下同时打开老端与 Unity 端同一个 UI 特效,
   对比特效相对周围 UI 元素的比例。
2. **3D 相机与地图瓦片 resize 后不重算**:`SceneCharacterStage.SyncProjection` 只在
   SetMainRole/AddSceneCharacter 时被调;`SceneMapView.SetFocus` 有"焦点未变整帧跳过"的 early-out。
   若批次 E 要放行横屏,需先补一个轻量 `ScreenSizeWatcher` 只广播给这两处 ——
   **不要复刻老端 12 个订阅者的全量广播**,其中 9 个是纯拉满/纯锚点语义,用 stretch/center 锚点即可替代。
3. **`Regions/HudBottomBar.prefab` 是遗留孤儿**:无 Creator、无代码引用、不在
   `MainUIModuleCreator.Parts` 名单,但仍带一套自己的锚。建议连 `.meta` 一起删除。
4. **z 序账**:老端 `MainUIDownView` 在 `Enum_UILayer.Activity` 层(高于 UI 层),
   Unity 整个 MainUIModule 塞在 Main 层,与打开的窗口遮挡关系相反。先记账,确认真有遮挡再动。
