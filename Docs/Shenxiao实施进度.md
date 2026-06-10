# Shenxiao 实施进度

> 实时更新。每完成一项 / 调整范围 / 新增需求都在这里登记。
> 关联文档：
> - [整体方案](Shenxiao重构实施方案.md)
> - [编码规范](Shenxiao编码规范.md)
> - [Copilot 红线](../.github/copilot-instructions.md)

**最近更新**：2026-06-09

**状态图例**：
- ✅ 已完成
- 🟡 进行中
- 🔵 已规划，未开始
- 🟠 需求变更/范围调整
- ⛔ 已废弃/暂缓

---

## 一、Phase 0：框架搭建

### 1. 文档与规范

| # | 任务 | 状态 | 产出 | 备注 |
|---|------|:----:|------|------|
| 1.1 | 整体重构方案 | ✅ | [Shenxiao重构实施方案.md](Shenxiao重构实施方案.md) | 2026-04-28 定稿 |
| 1.2 | 编码规范（完整版） | ✅ | [Shenxiao编码规范.md](Shenxiao编码规范.md) | 2026-04-28 定稿 |
| 1.3 | Copilot 红线 | ✅ | [.github/copilot-instructions.md](../.github/copilot-instructions.md) | VS Code 自动加载 |
| 1.4 | AGENTS.md 入口 | ✅ | [AGENTS.md](../AGENTS.md) | 多 AI 工具兼容 |
| 1.5 | 实施进度表（本表） | ✅ | 本文件 | 持续更新 |
| 1.6 | 地图加载重构方案 | ✅ | [Shenxiao地图加载重构方案.md](Shenxiao地图加载重构方案.md) | 2026-06-09 新增，Phase 1 场景地图前置设计 |

### 2. Unity 工程基础

| # | 任务 | 状态 | 产出 | 备注 |
|---|------|:----:|------|------|
| 2.1 | 安装 Addressables 包 | ✅ | Packages/manifest.json | 当前 `com.unity.addressables` 2.9.1 |
| 2.2 | 安装 Newtonsoft.Json 包 | ✅ | Packages/manifest.json | 当前 `com.unity.nuget.newtonsoft-json` 3.2.2 |
| 2.3 | 确认 TextMeshPro | ✅ | 已合入 com.unity.ugui 2.0 | 不需单独安装 |
| 2.4 | 创建目录结构 | ✅ | _App / GameRes / Prefabs / Scripts / Editor | 见方案 §3.1 |
| 2.5 | 创建 Asmdef | ✅ | Framework / Generated / Common / Module.×4 / Editor | **8 个**（新增 Generated） |
| 2.6 | 启动场景 + Loading UI | ✅ | _App/Scenes/Launch.unity（菜单 Shenxiao/Bootstrap/Create Launch Scene 一键生成）| Camera + EventSystem + UIRoot Canvas + AppLauncher；Loading UI 随 Phase 1 补 |
| 2.7 | AppConfig ScriptableObject | ✅ | _App/Configs/AppConfig.asset（一键创建）| 含 designResolution=720x1280 + canvasMatch |
| 2.8 | Addressables Profile（Local + Remote） | 🟡 | AddressableSetup 可自动初始化 settings + 建 Group | RemoteLoadPath 运行时注入 |

### 3. 框架层骨架（Scripts/Framework）

> Phase 0 仅出接口和空实现，跑通最小链路。

| # | 子系统 | 状态 | 文件 | 备注 |
|---|--------|:----:|------|------|
| 3.1 | Net | � | ErlangParser ✅ / UserMsgAdapter ✅ / NetManager ✅ / Proto ✅ / BaseController ✅ | 骨架可运行，单测待补 |
| 3.2 | Res | ✅ | ResManager / GameResPath / ResVersionManager / ResourcePath | 异步 API，运行时注入 CDN |
| 3.3 | UI | 🟡 | BaseView ✅ / ViewManager ✅ / LayerManager ✅ / UILayer ✅ / UIBinder ⏬ | UIBinder 随蓝湖 UI 生成工具一起做 |
| 3.4 | Event | ✅ | EventDispatcher / GlobalEvent | |
| 3.5 | Config | ✅ | ConfigManager / BaseVo / Lang / AppConfig | |
| 3.6 | StateM | ✅ | StateMachine | |
| 3.7 | Scene3D | ✅ | SceneObj / Character / Role / Monster / Npc | 骨架占位 |
| 3.8 | Util | ✅ | Util / TimeUtil / HttpUtil / GameLog | |

### 4. Editor 工具

| # | 工具 | 状态 | 产出 | 备注 |
|---|------|:----:|------|------|
| 4.1 | AssetConverter（Lh / Lm / Lani / Lmat） | � | Editor/AssetConverter/ 路由骨架完成，四个子转换器为 TODO 占位 | 二进制解析待 Phase 1 逐个补；.lani 格式规范已有 |
| 4.2 | ParticleConverter | 🟠 | — | Phase 2/3 实现 |
| 4.3 | UICreator（基础版） | 已清理 | — | Laya `.scene`→Prefab 复制路线判定不再作为 UI 主路线；实体 `Assets/Editor/UICreator/` 与 `Generated/UI/LayaSourceInfo` 已删除，保留为历史试验记录。 |
| 4.3.1 | UICreator 模板系统 | 已清理 | — | 旧 UICreator 专用模板已随工具删除；后续蓝湖接入如需模板，应重新按蓝湖字段和资源规则设计。 |
| 4.4 | ConfigGenerator | ✅ | Editor/ConfigGenerator/ + Schemas/configs/ + 示例 schema | 菜单 ``Shenxiao/Config/Generate All`` |
| 4.5 | ConfigGenerator Bootstrap | 🔵 | — | 从 yu_server hrl + field_mappings.json 逆向生成 schema 草稿（后续补） |
| 4.6 | AddressableSetup | ✅ | Editor/AddressableSetup/ | 菜单 ``Shenxiao/Addressables/Auto Group All`` |
| 4.7 | SpriteImporter | ✅ | Editor/BatchTools/SpriteImporter | AssetPostprocessor，默认 Sprite 设置应用于 GameRes/resource/ |
| 4.8 | SpriteResolver | 已清理 | — | 旧 UICreator 专用 `LayaSourceInfo.skin` 反填工具已删除；蓝湖路线需要重新实现缺图报告与本地预览挂图。 |
| 4.9 | AutoSpriteAtlas | ✅ | Editor/AddressableSetup/AutoSpriteAtlas | 扫 ``GameRes/resource/**/texture/`` 自动生成同级 ``{module}_texture.spriteatlas``；``Auto Group All`` 链路前置调用 |
| 4.10 | UICreator 坐标/字体保真修复 | 历史记录 | — | 旧 Laya 复制路线试验中的修复记录；工具实体已删除，经验仅用于后续蓝湖坐标/字体规则设计。 |
| 4.11 | UICreator 绑定字段全量 | 历史记录 | — | 旧 Laya 复制路线试验中的绑定规则记录；蓝湖路线仍需要生成 `*Bind.cs`，但字段规则以蓝湖节点命名规范为准。 |
| 4.12 | UICreator Image native size | 历史记录 | — | 旧 Laya 复制路线试验中的图片尺寸规则；蓝湖路线需重新确认图片原始尺寸、九宫格和缺图占位策略。 |
| 4.13 | RuntimeSkinScanner（ts → 占位 sprite） | 已清理 | — | 旧 ts 运行时图扫描工具已删除；后续动态图不再靠扫描旧 ts 补 prefab，而是由蓝湖缺图报告 + 业务逻辑接入处理。 |
| 4.14 | GameResPath 端口对齐 | ✅ | Scripts/Framework/Res/GameResPath.cs | 1:1 复刻 ``yu_client/h5/src/util/GameResPath.ts`` 静态部分，扩展名一致（.png/.jpg/.lh/.json），ts→C# 翻译时业务代码不需要改路径。运行时 Addressable 由 ``ResourcePath.Normalize`` 去扩展名。|
| 4.15 | 一键转换向导 + 菜单中文化 | 已清理 | — | 旧 Laya 模块一键转换入口已删除；蓝湖路线需要新的单一入口，仍保持“少菜单、可重跑、中文化、输出报告”的工具体验原则。 |
| 4.16 | UI 渲染保真小修 | 历史记录 | — | 旧 Laya 复制路线试验中的渲染经验；蓝湖路线应重新定义 Label 默认值、透明占位图和运行时补图规则。 |
| 4.17 | LanhuCreator 基础版 | ✅ | Editor/LanhuCreator/ + Docs/LanhuCreator接入规范.md | 输入 `lanhu_manifest.json + assets/`；生成 Prefab、`*Bind.cs`、缺图报告；区分 `local=true` Loading 包内资源与 Login Remote 资源。 |

### 5. 公共模块（Scripts/Common，接口 + 空实现）

| # | 模块 | 状态 | 备注 |
|---|------|:----:|------|
| 5.1 | RedDotSystem | ✅ | 骨架：SetCount/GetCount/Event。层级传递后续补 |
| 5.2 | AudioSystem | ✅ | 骨架：Music/Sfx/Voice + 分类音量 |
| 5.3 | TipsSystem | ✅ | 骨架：Toast/Float/Confirm log only |
| 5.4 | LoadingSystem | ✅ | 骨架：Show/SetProgress/Hide log only |
| 5.5 | EffectSystem | ✅ | 骨架：Play/Stop 走 ResManager |
| 5.6 | GuideSystem | ✅ | 骨架：Start/Stop log only |
| 5.7 | ChatBubble | ✅ | 骨架 |
| 5.8 | HudSystem | ✅ | 骨架 |
| 5.9 | Tooltip | ✅ | 骨架 |
| 5.10 | PopupQueue | ✅ | 骨架：优先级队列，Phase 1 接入 ViewManager |
| 5.11 | PrefsSystem | ✅ | PlayerPrefs 包装 |

### 6. Phase 0 验收

| # | 验收项 | 状态 | 备注 |
|---|--------|:----:|------|
| 6.1 | 协议层连通 Erlang 服务端，收发一条协议 | 🔵 | 需服务端在跳 |
| 6.2 | Addressables Remote 能从本地 HTTP 加载测试 Bundle | 🔵 | |
| 6.3 | 任一 .lh + .lani + .lmat 转 Unity Prefab，能播放动画 | 🔵 | AssetConverter 内部解析待实现 |
| 6.4 | 任一 .scene 转 Unity Prefab，UI 可见 | 🟡 | 节点树已可生；skin→Sprite 已有 SpriteResolver，待 Unity 实跑验收 |
| 6.5 | 任一配表 JSON + Schema → C# Vo + ConfigXxx，能加载并取值 | 🟡 | 生成代码已验证，运行时加载待验证 |
| 6.6 | WebGL 压缩首包 < 8MB | 🔵 | |
| 6.7 | Android/iOS 空包 < 30MB | 🔵 | |
| 6.x | **最小可运行链路（AppLauncher 干净启动）** | ✅ | Launch 场景 Play 后 Console 只有预期日志，无 Error |

---

## 二、Phase 1+：业务模块（待 Phase 0 完成后填充）

| Phase | 范围 | 状态 |
|-------|------|:----:|
| Phase 1 | Login + MainUI 能登录到主界面；公共模块完整实现 | 🟡 |
| Phase 2 | Role / Skill / Bag / Equip + 战斗表现 | 🔵 |
| Phase 3 | 商城 / 充值 / 活动 / 社交 | 🔵 |
| Phase 4 | 211 个模块全部对齐；上线切换 | 🔵 |

### Phase 1 当前进展（UI 路线重置）

| # | 项 | 状态 | 备注 |
|---|---|:----:|------|
| L.1 | 旧 Laya Login prefab / Bind 试点 | 已清理 | 已删除 `Assets/Prefabs/UI/Login`、`Assets/Scripts/Generated/UI/Login`、`LayaSourceInfo`、旧 login 资源和旧 Remote Addressables 组。 |
| L.2 | LoginBootstrap / LoginFlow / LoginEnterView | 已清理 | 旧 UI 绑定链已删除；`GmApi` 保留，后续可作为登录协议/GM 调用参考。 |
| L.3 | 蓝湖 UI 接入工具 | ✅ | 已定 `lanhu_manifest.json + assets/` 导入包规范；`LanhuCreator` 基础版可生成 Prefab + Bind + 缺图报告。 |
| L.4 | Phase 1 登录界面重建 | 待开始 | 以蓝湖生成的 Prefab 为准接 yu_client 逻辑；不再重跑旧 Laya UICreator。 |

### 项目级路线记忆（2026-06-09）

- `yu_client` 是老项目和重构来源，**不放弃**；继续作为业务逻辑、协议、运行时 UI 行为、资源路径和配置流水线参考。
- Shenxiao 是 Unity 客户端重构，不重构服务端；协议号、格式串、字段顺序、字段含义和收发时机原则上照抄 `yu_client`，客户端适配既有 Erlang 服务端，确需调整时单独报告。
- 直接复制 LayaAir / yu_client 运行时 prefab 不再作为 UI 主路线。已确认大量 UI 内容由 Laya TS 运行时生成，照搬最终 prefab 会持续漏动态图、列表状态和运行时皮肤。
- UI 后续主线准备改为：蓝湖设计稿/资源 → Unity Prefab → Bind → 缺图报告 → 接入 yu_client 对应逻辑。
- 蓝湖工具必须遵守现有资源策略：Editor 可挂本地 Sprite 预览，运行时统一走 Addressables Remote / CDN；功能存在但图片不存在时输出缺图报告，推动 UI/美术补资源。
- 2026-06-09 已清理旧 Laya Login 试点产物：Prefab、Bind、旧 UICreator 工具、旧 login 资源和旧 Remote Addressables 组；后续 UI 任务从蓝湖路线继续。
- 对接蓝湖前先收口工程骨架：启动场景、Addressable key、包体资源、编译与最小运行链路。

---

## 三、变更日志

| 日期 | 类型 | 说明 |
|------|------|------|
| 2026-04-28 | 新增 | 创建实施方案 / 编码规范 / Copilot 红线 / AGENTS.md / 本进度表 |
| 2026-04-28 | 调整 | ParticleConverter 完整实现移到 Phase 2/3，Phase 0 仅占位 |
| 2026-04-28 | 调整 | asmdef 改为按 6 个大域拆，不按 211 模块逐个建 |
| 2026-04-28 | 调整 | 新增 Shenxiao.Generated asmdef（原计划随模块归属），集中放 Vo / ConfigXxx / *Bind.cs；partial 限制 + 多模块共用 Vo 决定集中管理。最终 8 个 asmdef |
| 2026-04-28 | 确认 | 不引入 Localization Package，Lang.Get 走配表 |
| 2026-04-28 | 确认 | RemoteLoadPath 不写死 CDN 域名，运行时由资源版本 API 注入 |
| 2026-04-28 | 确认 | Shenxiao 不生成 data_*.erl，Erlang 端继续用 yu_client/tools 的 Python 工具链 |
| 2026-04-28 | 进展 | 已完成：安装 Addressables 2.3.16 / Newtonsoft.Json 3.2.1；创建 Assets 下 35 个子目录；写入 8 个 asmdef。**需在 Unity 中打开项目触发包导入与 asmdef 编译** |
| 2026-04-28 | 进展 | Editor 工具链骨架完成：ConfigGenerator + AddressableSetup + UICreator + SpriteImporter 全可运行；AssetConverter 路由骨架就绪，内部二进制解析占位。示例示范 Schema：Schemas/configs/client_attention.schema.json |
| 2026-04-28 | 进展 | 拷入首个模块资源：h5/laya/assets/resource/game/login/ → Assets/GameRes/resource/game/login/（清理掉 .atlas/.ktx/.rec，保留 95 png + 20 jpg 散图） |
| 2026-04-28 | 新增 | SpriteResolver Editor 工具（读 LayaSourceInfo.skin → Image.sprite） |
| 2026-04-28 | 遗漏 | 记录 4.9 AutoSpriteAtlas 需求：Unity 6 不像 Cocos 自动合图集，需手建或工具生成 SpriteAtlas，计划随首个模块验证后补 |
| 2026-04-28 | 新增 | AutoSpriteAtlas Editor 工具 + 集成到 ``Auto Group All`` 链路（4.9 ✅） |
| 2026-04-28 | 调整 | UICreator 改为模板驱动：手写 `new GameObject + AddComponent` → `Instantiate(LayaXxx.prefab) + override 字段`。模板放 `Assets/Editor/UICreator/Templates/`，菜单 `Shenxiao/UI/Build UI Templates` 一键生成；样式调整改模板 prefab，不再改代码。**与原方案的差异**：原方案在 §6.4 "UICreator" 只规定输入输出，未约定渲染骨架；模板系统是实现细节扩展，不影响 .scene→Prefab+Bind.cs 的对外契约。|
| 2026-04-28 | 修复 | TMPFontSetup 创建出来的 SDF .asset 重启 Unity 后 atlas 全空 → 改用 `AddObjectToAsset` 把 atlasTexture / material 写为 sub-asset；旧资产自动重建 |
| 2026-04-28 | 增强 | UICreator 字段保真：补 scaleX/Y、rotation、alpha、visible、align/valign、bold/italic、stroke+strokeColor、leading、sizeGrid（9-slice）；SpriteResolver 自动写入 sprite 的 spriteBorder + 设 Image.type=Sliced |
| 2026-04-29 | 新增范围 | 试做 Login 模块发现三类问题，必须落到工具上才能扩到 211 个模块：(1) 偏差大——centerX/centerY/left+right/top+bottom 被烘死成固定 anchor，分辨率/父尺寸变化就跑位；(2) 漏绑——只识别 ``_btn_/_img_/_box_h_`` 等 13 个前缀，``_box1``/``_lb_*``/``_gp_*`` 全漏；(3) 占位多——很多图是 ts 里 ``SetOutsideImageSprite`` 运行时设置，prefab 上无 sprite。新增任务 4.10 / 4.11 / 4.12 / 4.13 解决，先在 Login 上跑通再扩。|
| 2026-04-29 | 完成 | 4.10：UICreator.ConfigureRect 重写。Horizontal: left+right→stretch / centerX→center+pivot 0.5 / right→right pivot 1 / left→left pivot anchorX / x→pivot anchorX。Vertical 镜像，Y 反向。根 View 改为 (0.5,0.5) 中心锚以适配多分辨率。Label：默认 valign=middle（无 height 时）+ ContentSizeFitter（width/height 任一缺省）+ TopLeft/Center/Right 对齐 9 宫映射 + wordWrap。|
| 2026-04-29 | 完成 | 4.11：UICreator IsBindCandidate 改为"任何 ``_`` 开头节点"。ResolveBindType 增加 ``_lb_/_lab_/_box_/_box{N}/_hbox_/_vbox_/_gp_/_panel_/_view_/_scroller_/_scroll_/_html_/_list_/_tab_/_dd_/_ti_/_input_/_chk_/_bar_`` 全套前缀。LoginEnterView 字段从 11 个变为 ~22 个（与 ts 中 GetChildrenByNames 列表完全对齐）。|
| 2026-04-29 | 完成 | 4.12：LayaSourceInfo.useNativeSize；UICreator 在 Image/Clip 缺 width 或 height 时置 true；SpriteResolver 绑 sprite 后 SetNativeSize()。|
| 2026-04-29 | 完成 | 4.13：Editor/UICreator/RuntimeSkinScanner.cs。call-site 检测 + 手写括号深度参数解析（避开 ``GameResPath.GetIcon("a","b")`` 内逗号问题）；GameResPath 12 个静态形式查表；首写胜，避免条件分支多次覆写；命中后再跑 SpriteResolver。Login 实跑数据将记录在变更日志。|
| 2026-04-29 | 完成 | 4.14：GameResPath 1:1 端口对齐（24 个静态方法）。ts 业务代码翻译时直接 ``GameResPath.GetIconOtherPath("login","ui_Login_18")`` 不需改路径。|
| 2026-04-29 | 增强 | UICreator 给 mouseEnabled=true 但无 Graphic 的容器节点（Box/HBox/VBox）自动加透明 Image (alpha=0, raycastTarget=true) 以接收点击 —— 对应 LayaAir 的 Box.mouseEnabled。|
| 2026-04-29 | 增强 | SpriteResolver.ResolveAssetPath 自动尝试 .png / .jpg 后缀（runtimeSkin 路径若无扩展名）。|
| 2026-04-29 | 修复 | UICreator 现在永远写 ``tmp.text``（场景没 text 就写空），消除运行时 Label（``_lb_version`` 等）显示模板默认 "Label" 占位的 bug。UITemplateBuilder 的 Label 模板默认文字也改空。|
| 2026-04-29 | 修复 | UICreator 给 .scene 里没写 skin 的 Image 把 alpha 压到 0，避免预制体出现大白板（典型 ``_img_logo`` / ``_img_search_server_bg``——这类图运行时才决定）。SpriteResolver 之后解析到 runtimeSkin 时把 alpha 抬回 1。静态 skin 路径下不动 alpha，保留 .scene 里 props.alpha 的显式设置。|
| 2026-04-29 | 体验 | 4.15：211 个模块每个点 7 个英文菜单是不可接受的。改造：(a) 顶级菜单 ``Shenxiao`` 全部换 ``神霄``。(b) 单一主入口 ``神霄/UI/① 一键转换模块...``——EditorWindow + 两个字段（模块名 / yu_client 根），按序串起转 .scene → 挂 *Bind → 静态 sprite → 扫 ts 运行时图，进度条 + 实时日志 + Console 同步。(c) 其余单步入口移到 ``神霄/UI/调试/...`` 子菜单（应急/调试用）。(d) 所有按钮/对话框中文化，弹窗只放精简结果，"动态跳过"等详细列表打 Console 不上弹窗，避免被误读为报错。|

| 2026-06-09 | 决策 | 写入项目级路线记忆：不放弃 `yu_client`；`yu_client` 继续作为老项目/业务参考；UI 主线准备转为蓝湖设计稿/资源生成 Unity Prefab + Bind + 缺图报告，再接业务逻辑。|
| 2026-06-09 | 复查 | 工程骨架复查：asmdef 分层正常，Framework/Common/Generated/Module/Core/Login 已成型；dotnet build 可通过；启动场景配置、Addressable key 大小写、启动包体资源仍需收口后再对接蓝湖。|
| 2026-06-09 | 决策 | 写入系统级工程记忆：大功能优先“配置驱动 + 工具链先行”；AI 做 Boss 活动、运营活动、商城、养成、入口等系统前，必须先判断 Schema/配置/生成工具/资源规则/通用骨架，禁止直接写死业务数据。|
| 2026-06-09 | 清理 | 删除旧 Laya Login 试点产物和工具：`Assets/Prefabs/UI/Login`、`Assets/Scripts/Generated/UI/Login`、`LayaSourceInfo`、`Assets/Editor/UICreator`、旧 `GameRes/resource/game/login`、旧 `LoginBootstrap/LoginFlow/LoginEnterView`，并清理 `Remote_resource` / `Remote_Prefabs` Addressables 组；`GmApi` 保留。|
| 2026-06-09 | 新增 | `LanhuCreator` 基础版：定义蓝湖导入包、生成 Loading/Login 所需 Prefab + Bind + 缺图报告；修正 `ResourcePath.Normalize()` 为小写输出，对齐 Addressable key。|
| 2026-06-09 | 新增 | 地图加载重构方案：确认 `yu_client` 地图加载链路、`.bytes` 格式、`sceneId != mapResId`、瓦片异步补齐和 Shenxiao Framework/Combat/Editor 落点。|
| 2026-06-09 | 决策 | 写入项目级协议规范：Shenxiao 只重构 Unity 客户端，协议按 `yu_client` / 既有 Erlang 服务端照抄接入；确需调整服务端或协议时先报告。|

---

## 四、阻塞与风险登记

| 日期 | 项 | 状态 | 处置 |
|------|----|:----:|------|
| 2026-06-09 | Build Settings 启用不存在的 `Assets/Scenes/Main.unity`，`Assets/_App/Scenes/Launch.unity` 反而禁用 | 待修 | 对接蓝湖前先修启动场景配置；此项属于启动流程配置调整，改动前按规范报告。 |
| 2026-06-09 | Addressable key 大小写不一致风险：AddressableSetup 会转小写，ResourcePath.Normalize 当前未转小写 | 已修 | `ResourcePath.Normalize()` 已改为小写输出；蓝湖导入工具生成/复制资源时统一走同一规范。 |
| 2026-06-09 | `_App` 启动期资源约 9.7MB，主要由两个 TTF 字体占用 | 待审计 | WebGL 首包目标 <8MB，需评估字体是否保留在包内或改远端/裁剪。 |

---

## 五、待决策

| # | 议题 | 优先级 | 备注 |
|---|------|:------:|------|
| 1 | 蓝湖转 Unity Prefab 工具输入格式与字段命名规则 | 已定 | 首版采用 `lanhu_manifest.json + assets/`；后续无论手工整理、蓝湖 API、浏览器抓取或插件导出，都统一落到该 manifest。 |

---

> 更新规则：
> 1. 每完成一项任务，把状态从 🔵 / 🟡 → ✅，并在变更日志写一行
> 2. 范围调整 → 状态 🟠 + 变更日志说明原因
> 3. 新需求 → 在对应章节插入新行，初始 🔵
> 4. 阻塞 → 写到第四节，注明日期、原因、当前处置
