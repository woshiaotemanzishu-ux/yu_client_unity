---
name: fix-view
description: 作为项目统一“UI 对接 / UI 精修”流程中的增量修复环节，修复已转换且已由 Prefab 接管的 Unity view，包括位置/尺寸/层级/裁剪/容器/缺节点/错图/绑定、功能、运行态状态、3D模型姿态与特效。用户说“UI对接”“UI精修”且目标已有可编辑 Prefab，或指出与老端截图不一致、模型缺失/累积、像素偏移、功能没接、状态错误，或拿着 diff 报告逐项收尾时使用；修完必须返回 audit-game-ui-route 沿原路径复验。
---

# fix-view — 收尾修正一个转换后的 view

转换流水线(见 `/convert-module`)产出的 prefab + View 代码,抽查发现问题时用这个逐项修。**当前可编辑 Prefab 是视觉唯一事实源；优先增量改 Prefab，其次改运行态状态/绑定代码，跨页面共性才动公共链路。已接管页面禁止重转、重烤或整页覆盖。**

## 输入
- view 名(如 `LoginCreateRoleView`)。
- 问题来源二选一:
  - **diff 报告**:`python Tools/UiDiff/ui_runtime_diff.py --laya <快照> --unity <ui_dump> --view <View>` 的输出(MISSING/OFFSET/SIZE/RESOURCE 清单)。
  - **用户描述**:"顶部职业项偏左""返回键太高"之类。

## 判断改哪一层

| 症状 | 多半的根因 | 怎么修 |
|---|---|---|
| `OFFSET` / `SIZE`(位置/尺寸偏) | prefab 节点的 RectTransform 锚点/位置 | **直接在 prefab 里拖/改 RectTransform**(节点都是真的);系统性偏移则回 `LayaRectMath` 或快照采集状态 |
| 局部数值接近但截图整体偏移 | 父容器位移与子节点锚点坐标系叠加 | 把目标世界角点换算为页面根左上角矩形再对照；修当前 Prefab 的锚点/局部位置，禁止只比较 `anchoredPosition` |
| 横排越界/列表不能滑 | 只有 LayoutGroup，缺 ScrollRect/Viewport/Mask/ContentSizeFitter，或绑定到重叠的假 Content | 保留唯一 `ScrollRect→Viewport(RectMask2D)→Content(Layout+Fitter)`，绑定真实 Content；真实拖动并验末项可达 |
| 点击能开窗但样式/版本不对 | 继承通用默认点击，未核对老端弹窗身份；或运行时品质底图没赋值 | 逐个触发格核对具体 View 类型、根尺寸、主底图 Sprite/启用态和遮罩；运行时赋图进入资源闭包与 ready 条件 |
| `MISSING`(少节点) | 初始转换漏节点/条件节点/该节点运行时才加载 | 先核对老端运行态；在当前 Prefab 增量补可视节点和序列化引用。只有页面从未接管且尚无可编辑 Prefab 时才允许首次转换 |
| `RESOURCE`(换错图/没图) | Prefab Sprite、数据路径/索引、资源闭包或 Addressables 错 | 静态图直接改 Prefab；动态图修绑定与配置闭包。首次点击才生成 PNG/.meta 仍是缺陷 |
| 模型缺失/镜像/部件累积/特效不符 | 模型装配参数、RT 翻转、旧实例延迟销毁、`EffectBinder` 漏接 | 先对照老端同状态截图；核对模型存在、部件、朝向、镜像/翻转、角度、位置比例和骨骼常驻特效。换页先失活旧实例再销毁 |
| Renderer 存在但截图里模型空白 | RawImage/RenderTexture 已绑定就提前置 ready，相机尚未真正出帧 | 只在 `Camera.Render()` 完成后置渲染完成标记；验收读取 RT 像素并要求足量非透明像素，不能只查 Renderer/RT 引用 |
| 特效 Handle 存在但画面只有小光点、宿主错位或出现到不该出现的页面区域 | 没先区分页面/共享槽位/模型归属；或把旧端 scale 机械复制到不同 Unity 宿主；或同通道其他实例贡献像素让低门槛误过 | 先确定特效所有者和 opt-in 消费者；记录老端全参数，再按转换资源、宿主缩放、profile/通道映射校准每类宿主；隔离精确 Handle 后保存 PNG、非透明像素和 alpha 包围盒宽高 |
| 特效位置正确但始终静止，或列表项滚出 Viewport 后只剩特效框 | 动画曲线写入的材质属性没有被 shader 当前分支读取；只凭单帧/`isPlaying` 误判动态；共享 RT/RawImage 绕过了宿主祖先遮罩 | 同一隔离 Handle 在两个时间点真实 Render，保存动画时间/属性推进、两帧 PNG 和像素差；逐项核对曲线属性与 shader/material 开关。实例裁切再与有效祖先 `RectMask2D/Mask` 求交，验完整、部分、完全离开三态，完全离开后目标 alpha 必须为零 |
| 动态详情文字重叠或背景包不住 | 配置字段映射错容器，固定高度没有跟随内容布局 | 先核对老端字段语义与分组；分别计算详情、来源等组的 preferred height，断言相邻组不重叠且背景包围全部内容 |
| 同构物品格/详情/按钮在不同位置各修一遍 | 页面复制了节点树、坐标或状态逻辑，没有复用共享 Prefab/View | 先按视觉结构、交互语义和数据形状确认组件身份；修共享 Prefab/View，宿主只传数据、状态和回调，禁止页面专用副本 |
| 1/2/3 个条目不能整体居中，短长文案换行，单按钮偏左或特效状态遗漏 | 只验了一个页面截图，没有覆盖共享组件状态矩阵 | 用 LayoutGroup/ContentSizeFitter 和 Prefab 文本/按钮容器表达布局；先跑组件状态矩阵，再回原页面路线复验 |
| 绑定字段 null / 点了没反应 | Bind 字段没回填,或 View 没绑事件 | 重跑 `LayaBindFiller.FillPrefab(<prefab>)`;容器字段(RectTransform)按【声明类型】解析(烤后 Box 多挂 Image 会置空);点击 ClearClicks+AddClick |
| 输入框/文字**重叠** | 老端 TextInput/Label 内部文字节点被烤成常显子 Text,盖住输入值/真实文字 | 删该 TMP_Text/输入框下多余的子 TMP_Text(父权威、子冗余);烤制器 `AdaptSnapshotNode` 已跳 text-like 子节点 + TextInput 提示提到 placeholder(治本) |
| 列表**显示了不该有的项 / 数量像写死**(如选角"1 角色却显 3 个") | 烤制快照**冻结了数据驱动列表**(烤时那个账号的真实项被当固定节点) | View 必须按**真实数据**建表(遍历/克隆),别信烤制数量。逐层查漏出:① item 有 `{Item}Bind` 且字段非空(漏挂→View `bind==null` 整列跳过)② 无冗余子 Text ③ 绑的是**可见**节点(头像是 `icon_sys_head` 不是空的 `icon`;路径因 `_box_con` 嵌套失效就递归按名找)④ 空槽用 `节点.SetActive(false)` 整块隐藏(连子节点),比 `组件.enabled=false` 稳 |
| 位图字体仍是普通字、审计显示未命中，或替换后尺寸异常/被截断/串入邻字 | 人工 Prefab 已重命名节点，名称扫描漏掉序列化 Bind；把 FNT `info size`、glyph 槽高与 TMP `pointSize` 当成同一尺寸；旧端本来逐字切图，Unity 却误用 TMP 基线/行高排版；或替换 PNG 的实心笔画已经越过旧 FNT 槽位 | 先按 `source node → View 序列化 Bind 字段 → 当前控件` 定位，名称只作兜底；再查旧端实际绘制器。普通 TMP 消费者按 `BitmapFont.fontSize/autoScaleSize` 换算并用 preferred bounds 扩容；旧端若直接按 FNT 切片，则逐字符复制 `x/y/width/height/xoffset/yoffset/xadvance` 生成原图四边形，并逐槽核对 PNG 不透明内容没有触边或串槽。只修确认的 Prefab/消费者/固定 atlas，禁止因一个字体族异常全局重算或重建全部字体资源 |

## 步骤
1. 读问题来源(diff 报告或用户描述),定位到具体节点 + 判断改哪层(上表)。
2. **Prefab 改动**：直接编辑当前 Prefab 的 RectTransform、Sprite、LayoutGroup 和可视组件；若用 Editor 工具，只允许对当前 Prefab 做一次幂等增量补丁并立即保存。
3. **运行态改动**：View/Controller/Model 只负责数据、事件、协议、点击语义、序列化引用和必要显隐；不把位置、间距、字号、颜色写回代码。
4. **重新 diff**：2D 在同逻辑分辨率保存 old/unity/diff；3D 不要求跨引擎逐像素相同，但必须有模型、无镜像翻转和明显错误角度，位置与比例大致正常且模型特效一致。
5. **资源性能复验**：资源型页面连续跑两次预检，第二次 `imported=0、configured=0`；玩家 cold/warm 点击前后资源目录零新增。
6. **结构与身份复验**：列表保存容器树并走真实拖动；弹窗保存触发格到实际 View 的身份证据；关键 Rect 同时保存页面根坐标，避免局部锚点假通过。模型页另存实际出帧标记与 RT 非透明像素数，动态详情存各语义组矩形与背景包围盒。

## 共享组件修复顺序

1. 定位节点后先建立组件依赖清单，判断它是页面私有节点还是共享组件缺陷；已有共享组件时禁止另造页面专用副本。
2. 修改前静态列全直接消费者并按使用形态分组；运行态默认抽目标页和每个实质不同形态的一个独立代表，通常共 2～4 页。根组件、Bind 或生命周期变化时必须含一个高频既有页面；样本失败才扩大同组，全量引用检查只留给公共 API/字段变化、整体换引用或持续失败。
3. 共享组件的视觉结构、LayoutGroup、文本宽度、按钮容器和特效宿主保存在 Prefab；宿主页面只传数据、状态和回调。不同页面确需差异时，使用明确的状态/皮肤/profile 输入，不在宿主页复制组件或直接改共享内部节点。
4. 先用真实共享 Prefab 覆盖适用的特效开/关、1/2/3 项居中、短/长文本、单/双按钮、充足/不足、选中/未选中及空/有数据状态，保存局部截图、Prefab/GUID 身份和几何断言。动态特效额外保存双时间点像素差；位于滚动列表时再保存完整可见、部分裁切、完全离开后零残留三态。
5. 组件级与代表样本通过后返回 `audit-game-ui-route`，沿目标原路径只做一次整页点击、弹窗、返回、截图和真实 Web 复验；页面截图不能替代组件变体，引用数量也不能替代代表抽样。

## 原则
- **"烤制数据漏出"是大类**(界面显示的是烤快照时那个账号的旧数据,不是当前账号的)。本质=有"View 没接管的可见节点"在漏:逐个找①没挂 Bind→跳过 ②同义冗余子节点 ③绑错/路径失效的可见节点 ④空槽没整块隐藏。一层层堵,别只堵一层就以为好了(选角踩过:Bind→子Text→头像三层叠着)。改完**在 monolith 嵌套实例上**验证(嵌套实例继承源 prefab 改动)。
- 一次修一个 view、一组症状;改完给出"改了什么 + 怎么验证"。
- 位置类优先 prefab 直改(可视化、可回退),别动代码绕。
- 系统性、跨多 view 的同类偏移 → 修共享 Prefab、公共 View 或公共升级器；不得用批量重转覆盖已接管页面。
- 位图字体图集已经烤色时，TMP 顶点色保持白色；老端 `Image` 即使残留 `font` 属性，只要现行逻辑通过 `SetImageSprite` 换图，就归图片链而不是字体遗漏。
- 动态战斗字先确认旧端是通用文字排版还是 FNT 逐字直绘；后者不能用 TMP `textBounds` 绿灯替代。逐字核对位置文件、图集 UV、offset/advance，并检查每个数字的实心 Alpha 没有跨出对应 FNT 槽；“Unity 与当前 CDN PNG 哈希一致”只能证明同步一致，不能证明替换图仍与 FNT 配套。覆盖普通长数字、`a/b/c` 前缀、对象池短串→长串复用和 Crit 最大缩放。自定义 `Graphic` 的 `CanvasRenderer` 有 Mesh/顶点仍可能在玩家场景零像素；优先复用内置 `RawImage.uvRect` 逐字切图，验收必须读真实 Canvas 像素或由玩家同场截图确认。
- 打开/关闭面板导致场景表现永久消失时，先查“模块缓存根 active”与“实际 BaseView shown”的混用；复验必须在同一场战斗中走“出现→开面板→关闭→再次出现”，不能只测冷启动或只测开窗前。
- MCP 改 prefab/编译 OK;Addressables 写记得 `postEvent:false`;Play 验收靠用户。

相关:`/convert-module`、[[runtime-ui-diff-oracle]]、[[conversion-architecture-and-plan]]。
