# Shenxiao 实施进度

> 实时更新。每完成一项 / 调整范围 / 新增需求都在这里登记。
> 关联文档：
> - [整体方案](Shenxiao重构实施方案.md)
> - [编码规范](Shenxiao编码规范.md)
> - [Copilot 红线](../.github/copilot-instructions.md)

**最近更新**：2026-06-11

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
| L.4 | Phase 1 登录界面重建 | 🟡 | 2026-06-10 改走 LayaUI 直转流水线(见 [LayaUI转换流水线.md](LayaUI转换流水线.md)):login 模块转换工具已就绪,待本地 Unity 实跑「加载页 LoginLoadingView + 登录页 LoginView」验收。 |
| L.5 | **登录链路端到端试点(量产前置门槛)** | ✅ | 2026-06-11 用户拍板:量产前先用转换产物接真实 API 跑通整条登录链路,验证"UI 不只是长得对,还得用得对"。链路与里程碑见下表。 |

#### L.5 登录链路端到端试点

yu_client 真实链路(已核对源码):`LoginStateManager` 状态机
`Register / Login → (SimplifyServer | ServerList → ServerWin) → ConnectGame`;
账号登录/注册 = HTTP GET `ClientConfig.login_php`;服务器列表 = HTTP(md5+cookie,ServerModel);
连游戏服 = WebSocket(UserMsgAdapter)+ Erlang 协议。Unity 侧已有对应骨架:
HttpUtil / GmApi(HTTP 参考)/ NetManager / ErlangParser / UserMsgAdapter / ViewManager。

| # | 里程碑 | 验收点 |
|---|--------|--------|
| M1 | 资源加载 | LoginModule.prefab 经 Addressables 加载;LoginLoadingView 显示真实加载进度 |
| M2 | 登录/注册 | LoginView/RegisterView 接 `login_php` 真实 API,成功拿到账号凭据 |
| M3 | 选服 | ServerModel 移植,服务器列表渲染进 LoginSelectServerView(item 用 `_tpl_*` 模板 Instantiate) |
| M4 | 连游戏服 | WebSocket 握手 + 首个游戏协议(角色列表)收到,推进到选角/创角界面显示 |
| M5 | 复盘量产 | 试点暴露的转换器问题全部修掉 → 全模块批量转换 |

**UI 验收红线(衡量"UI 真的处理好了")**:业务代码零 `transform.Find`、零样式参数;
节点全走 Bind 字段;动态图全走 ResManager(Addressables);窗口切换全走 ViewManager
操作 LoginModule 子窗口;转换报告里 login 模块「运行时赋值」清单全部被业务代码覆盖。

### 项目级路线记忆（2026-06-09）

- `yu_client` 是老项目和重构来源，**不放弃**；继续作为业务逻辑、协议、运行时 UI 行为、资源路径和配置流水线参考。
- Shenxiao 是 Unity 客户端重构，不重构服务端；协议号、格式串、字段顺序、字段含义和收发时机原则上照抄 `yu_client`，客户端适配既有 Erlang 服务端，确需调整时单独报告。
- 直接复制 LayaAir / yu_client 运行时 prefab 不再作为 UI 主路线。已确认大量 UI 内容由 Laya TS 运行时生成，照搬最终 prefab 会持续漏动态图、列表状态和运行时皮肤。
- UI 后续主线准备改为：蓝湖设计稿/资源 → Unity Prefab → Bind → 缺图报告 → 接入 yu_client 对应逻辑。
- 蓝湖工具必须遵守现有资源策略：Editor 可挂本地 Sprite 预览，运行时统一走 Addressables Remote / CDN；功能存在但图片不存在时输出缺图报告，推动 UI/美术补资源。
- 2026-06-09 已清理旧 Laya Login 试点产物：Prefab、Bind、旧 UICreator 工具、旧 login 资源和旧 Remote Addressables 组；后续 UI 任务从蓝湖路线继续。
- 对接蓝湖前先收口工程骨架：启动场景、Addressable key、包体资源、编译与最小运行链路。

> **2026-06-10 路线更新**:用户拍板 UI 主线回到 **Laya .scene 直转**,蓝湖路线保留备用。
> 与 06-09 判定的区别:旧 UICreator 失败的根因是「逐界面手写 + 无粒度决策 + 尺寸/图源规则缺失」,
> 不是直转路线本身不可行。新流水线(归属分析 manifest + 通用转换器 + 报告)见
> [LayaUI转换流水线.md](LayaUI转换流水线.md);「运行时动态生成的 UI 内容会漏」的问题依旧存在,
> 由转换报告的「运行时赋值」清单显式列出,在接业务逻辑时补。

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
| 2026-06-10 | 决策 | 用户拍板:UI 路线回到 **Laya .scene 直转 Prefab**(蓝湖路线保留备用)。与旧 UICreator 的区别:不再逐界面手写 Creator,改为「Python 归属分析器 + manifest 粒度决策 + 通用转换器」。粒度规则按代码引用拓扑:窗口独立 prefab、单归属 item 内联 `__Templates`、多归属出共享 prefab,全量 2056 scene → 1077 prefab。详见 [LayaUI转换流水线.md](LayaUI转换流水线.md)。|
| 2026-06-10 | 新增 | `Tools/LayaUI/analyze_layaui.py`(已对 yu_client 跑出 `Schemas/LayaUI/ui_manifest.json`)+ `Assets/Editor/LayaUI/` 通用转换器(模板/坐标数学/三级图源/Bind 生成回填/报告);BaseView 补 `EnsureBound`;`Shenxiao.Generated`、`Shenxiao.Editor` asmdef 补 `UnityEngine.UI` 引用。试点 = login 模块(加载页 LoginLoadingView + 登录页 LoginView),待本地 Unity 实跑验收。|
| 2026-06-11 | 里程碑 | **登录链路端到端全通**(真实 UI + 真实 API):加载页(真实进度)→ 账号密码登录/注册 → 协议弹层(按账号,同意即进)→ 踏入仙界 → 大区 tab 选服 → WebSocket 10000 → 角色列表。全程与 yu_client 源码逐行为对齐。|
| 2026-06-11 | 新增 | 转换器改版:Tab 分类 + 中英模块按钮(module_names_cn.json,211 模块)+ 一键流水线(散图→转换→编译后自动回填→分组)+ 验收闸门(ui_acceptance.json,验收模块重转弹确认)。|
| 2026-06-11 | 清理 | 编辑器菜单收纳为 神霄/{LayaUI 转换器,资源,配置,配表,工具,调试};删除旧 UICreator 残留(英文根菜单清零)。|
| 2026-06-11 | 规范 | 编码规范 §3.2 更新为 LayaUI 路线(Bind/UIUtil/alpha 红线/三层样式修改原则/查源码原则);补建 .github/copilot-instructions.md;AGENTS 必读清单加流水线与登录链路文档。|
| 2026-06-11 | 里程碑 | **登录链路端到端全通**(真实 UI + 真实 API):加载页(真实进度)→ 账号密码登录/注册 → 协议弹层(按账号,同意即进)→ 踏入仙界 → 大区 tab 选服 → WebSocket 10000 → 角色列表。全程与 yu_client 源码逐行为对齐。|
| 2026-06-11 | 新增 | 转换器改版:Tab 分类 + 中英模块按钮(module_names_cn.json,211 模块)+ 一键流水线(散图→转换→编译后自动回填→分组)+ 验收闸门(ui_acceptance.json,验收模块重转弹确认)。|
| 2026-06-11 | 清理 | 编辑器菜单收纳为 神霄/{LayaUI 转换器,资源,配置,配表,工具,调试};删除旧 UICreator 残留(英文根菜单清零)。|
| 2026-06-11 | 规范 | 编码规范 §3.2 更新为 LayaUI 路线(Bind/UIUtil/alpha 红线/三层样式修改原则/查源码原则);补建 .github/copilot-instructions.md;AGENTS 必读清单加流水线与登录链路文档。|

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


## 2026-06-13(夜间批次)

- 特效导入线 v1:LayaEffectImporter(引擎源对标)+ 资产管理「特效」域 + 预览粒子模拟 +
  运行时 EffectBinder(常驻/动作/创角特效挂接,已接装配器与创角页)。待本机真实样本验收。
- 协议架构定稿:Docs/Shenxiao协议架构.md(帧/分段/路由/进游戏推送链/心跳重连约束);
  10007 勘误为角色名验证;未注册协议降 Info。进游戏首个落地目标=RoleController+场景段。
- GM 秘籍工具:服务端 11100 下发全量清单(350+ 命令零硬编码),神霄/GM 秘籍 窗口
  分类/搜索/参数默认值/直发;运行时 GmCheatController。
- 编码规范 §十二 增补(3D/特效/协议/GM 硬规则)。


## 2026-06-15(协议线 Phase A)

- 特效线闭环:色彩空间切 Gamma + ×2 移进 shader(无条件,对标 Laya 原生)+ 自定义 shader 进本地
  Addressable 组(修紫块)+ Play Mode 默认 Use Asset Database(杜绝重转 vs 旧 bundle 错位)。
- 进游戏协议框架 Phase A:ControllerHub(注册中心)+ GameEntryFlow(进游戏编排,自装)+
  RoleController/RoleModel(13001/13002/13003/13006,对标 pt_130)+ BaseController.Dispose +
  NetReader.ReadArray + BattleAttrProto。"进游戏看到主角数据"骨架就位,待真机验证。
- 方案定:协议接入"先手搓一个真实模块(RoleController)→ 再建协议代码生成器(Phase B)→ 按模块批量(Phase C)"。


## 2026-06-16(摇杆移动闭环)

计划:完善摇杆 UI + 主角移动,目标"角色在地图中正常跑动"(进游戏链路 2.3/2.5)。
原因:对齐老客户端 UIJoyStick.ts + Scene.ts 输入 + MainRole.UpdateStateMove 移动。
影响范围:
- Framework/Net/Proto.cs:新增 `SC_MOVE = 12001`(移动上报)。
- Framework/Scene3D/Map/SceneMapData.cs:新增 `IsBlockPixel`(像素→逻辑格 /60,/30,阻挡位 &1,越界阻挡;
  动态区域阻挡留待场景对象线)。
- Framework/Scene3D/Map/SceneMapView.cs:抽出公开 `SetFocus(x,y)` 做相机跟随(滚动场景层,焦点移过半 tile 才补刷瓦片)。
- Module/Core/Scene/SceneInput.cs(新):场景输入状态,对标 SceneManager.curr_click_start_pos/move_dist/joystick_dir
  (舞台坐标 x 右 y 下,已归一化,死区 12px)。
- Module/Core/Scene/SceneInputDriver.cs(新):场景级指针捕获(对标 Scene.ts 的 stage MOUSE_DOWN/MOVE/UP),
  落在 UI 按钮上的按压不进摇杆;鼠标走 Editor/PC,触摸走真机。
- Module/Core/Scene/MainRoleAgent.cs(新):每帧按摇杆方向推进 real_pos(速度 250、单帧 dt≤0.04、撞墙 X/Y 分轴滑动),
  写回 RoleModel + 相机跟随 + 播 run/idle + 转向 + 约 0.5s 上报 12001(松手补发一次)。
- Module/Core/Scene/SceneController.cs:新增 `SendMoveRequest`(12001 "ihhchhhh",对标 SceneController.ts:1042;
  发包留在 Module 层)。
- Module/Core/MainUI/Views/UIJoyStick.cs:补齐摇杆图——按下场景空白处才显示并跟手定位底盘,摇杆头沿方向偏移
  (截断到半径 53.5)并旋转(1:1 端口 UIJoyStick.ts:48-67)。
- Module/Core/Scene/MainRoleFlow.cs:装配后补 run 动作、挂 MainRoleAgent、装 SceneInputDriver;改为跟随相机模型——
  主角恒居屏幕中心、移动靠相机滚动地图体现(不再按 X/Y 平移主角节点)。
- Shenxiao.Module.Core.csproj:补 3 个新文件 Compile 项(Unity 重导会自动覆盖)。
验收:摇杆按真实贴图显隐/跟手/转向;主角按摇杆方向跑动、撞墙沿墙滑、地图在脚下滚动、约 0.5s 上报 12001。
3D 主角模型在 UGUI 地图上的精确合成(屏幕对位/遮挡)仍属"待真机验证"(承 2026-06-15),本批只闭环数据/动作/朝向/相机跟随这条可验证逻辑线。
回滚:删除 SceneInput/SceneInputDriver/MainRoleAgent 三个新文件 + 回退 Proto/SceneMapData/SceneMapView/SceneController/UIJoyStick/MainRoleFlow 的本批改动。

### 2026-06-16 真机首跑两处修复(读 Editor.log 定位)

1. 输入后端:本工程 Active Input Handling = Input System Package,SceneInputDriver 原用旧 `UnityEngine.Input`
   每帧抛 InvalidOperationException 刷屏。改为 UGUI EventSystem 捕获——铺满屏幕的透明 raycast 接收板
   挂 canvas 最底层,实现 IPointerDown/Drag/Up 写 SceneInput;与输入后端无关,且"UI 在上层先吃点击"自然成立。

2. **协议解析(关键,主角进不了场景的真因)**:`BattleAttrProto` 早期误按服务端 write_attr_list 的
   "count+id16+val32 列表"分支解析,但 13001 主角实际走 `#attr{}` 固定记录分支
   = Hp:64 + HpLim:64 + Speed:16 + **51×int32 固定属性**(无计数、无 id,对标老客户端 BattleProtoVo+BaseAttrProtoVo)。
   少读约 202 字节 → 同包后续 场景id/副本id/坐标 全部错位(日志 场景=2031616、副本=1048576 = 真值<<16),
   12005 进场被服务端拒(errorCode 1200005),地图/主角自然出不来。已重写 BattleAttrProto 为固定 51 字段,
   字节消耗与服务端 pt.erl write_attr_list/1 的 #attr{} 分支、老客户端 BaseAttrProtoVo.pro_list 完全对齐。
   (注:role_id=4294967349 是服务端 ServerId<<32|本地号 的合成 id,正常,非解析错。)

待验证:Unity 重跑后 12005 应返回有效 instanceId,地图加载 → 主角出现 → 摇杆驱动跑动。
旁路噪声(非本项):UnityConnect/Curl cert 404、UIElements 渲染异常,与进游戏链路无关。

### 2026-06-16 移动卡顿优化(对标老客户端 CameraManager/Laya 滚屏)

症状:摇杆移动相机时非常卡。两处根因:
1. **每帧根 Canvas 重建(常驻卡顿)**:SceneMapView.ApplyCamera 原来每帧移动 `UILayer.Scene`(根 Canvas 的子节点)→
   每帧重建整个根 Canvas(含 HUD)。老客户端是只挪一个容器/相机(CameraManager.ts:676 `_scene_layer.x=h_w-_camera_pos.x`、
   :229 移 3D 相机 transform),Laya 保留模式下几乎免费。
   **修复**:`__SceneMap` 改为自带 `Canvas`(overrideSorting + sortingOrder=-100,压在 HUD 下),滚屏改为只移动这个
   独立 Canvas 的 anchoredPosition → 不触碰根 Canvas、不重建 HUD;sceneLayer 保持满屏静止。
   并加 early-out:焦点(主角格)未变整帧跳过(对标 UpdateCamera:648 的 _camera_pos 未变即 return)。
   文件:SceneMapView.cs(EnsureRoot 加 Canvas、ApplyCamera 改移 _root、SetFocus 加焦点 early-out)。
2. **进新区域时每瓦片同步导入(尖刺卡顿,仅编辑器)**:瓦片未进 Addressables 时,ResManager 编辑器兜底走
   AssetDatabase.FindAssets + 从 yu_client 拷 .jxr→.jpg + ImportAsset/SaveAndReimport(主线程同步),
   每块新瓦片一次大顿。老客户端瓦片是预存 .jxr/.ktx 异步加载,无此问题。
   **缓解**:瓦片仅在相机移过半个 tile 时补刷、已加载不重复请求(本就有);彻底解决需把地图瓦片
   预导入并跑「神霄/资源/Addressable 自动分组」,运行时即走 Addressables 异步,不再触发编辑器重导。
   (首次探索把整图瓦片导入 Assets/GameRes 后,后续会话不再 SaveAndReimport,只剩较轻的 FindAssets。)

### 2026-06-16 瓦片固定池滚动复用(移植 MapManager.UpdateTiles)

针对"H5/小程序按需流式加载还要不卡"的目标,把 SceneMapView 的瓦片系统从"按需建 GameObject 的字典"
重写为对标老客户端的**固定瓦片池滚动复用**(MapManager.tile_list + UpdateTiles:581-628):
- 池大小=视野+边距(编辑器 12 列,真机 floor(w/tile)+2 列 × floor(h/tile)+1 行);**+2/+1 边距即屏幕外预取缓冲**。
- 跨 tile 边界才刷新(窗口起点格未变即整体跳过,对标 :598);移出视野的瓦片直接挪到新格复用,
  **不 new/Destroy、不涨内存、不产生 GC 与 Canvas 重建尖刺**;换图前 Release 旧 sprite(顺带修早期"瓦片只增不放"的泄漏)。
- 越界格保持隐藏,缺图也保持隐藏 → 低清底图透出,绝不空白。
- 我们的元素:**单飞加载泵**(_loadQueue + PumpLoads)一次只加载一块,把加载/编辑器现导分摊到多帧,
  避免一帧涌入几十块造成尖刺;每槽 Token + 版本号双重校验,过期/换图的加载结果直接丢弃。
文件:SceneMapView.cs(EnsureTilePool/UpdateTiles/AssignSlot/PumpLoads/ResetPool/ClearPool;删除旧 RefreshVisibleTiles/LoadTile/字典)。

仍未做(单列为资源管线项,改动影响打包,做前报告):⑤瓦片走平台压缩纹理(对标老客户端 .ktx,
让 GPU 上传零解码),这是真机/小程序彻底消除"加载一块顿一下"的物理基础。当前运行时层(对象池+预取边距+
每帧预算+独立 Canvas)已就位。

### 2026-06-17 地图瓦片离线转换工具(根治"进新区域第一次很卡")

定位:运行时卡顿剩余项 = 编辑器**逐块同步现导**瓦片(ResManager 的 #if UNITY_EDITOR 兜底:把 yu_client 的
.jxr 拷进 GameRes + ImportAsset/SaveAndReimport)。日志实测出生图 10000 是 10960x9009、1548 块瓦片,
之前只零散转了 257 块 → 没去过的区域第一次进就现导卡。运行时优化(对象池等)改善不了"同步导入"这一帧硬卡,
唯一解是**提前离线批量转好**;这不违反两项目边界——转换是编辑器期把素材搬进本项目,运行时仍只走本项目 Addressables。

新增 `Assets/Editor/MapResourceTools/`:
- `MapTileConverter.cs`:可复用的离线转换/盘点。ScanScenes(列出有 {id}.bytes 的场景)、Inspect(解析 .bytes 拿
  mapResId/尺寸 + 数源/产物 RRCC 瓦片)、Convert(批量拷 .bytes+底图+全部 .jxr→.jpg 进 GameRes,
  StartAssetEditing 包裹一次性导入,增量跳过已转,JPEG magic 校验)。
- `MapAssetWindow.cs`:菜单 `神霄/资源/地图资源`。左=场景地图清单 + 转换状态(○未转/◐部分/●已转,显示 已转/总数);
  右=选中图详情(sceneId、mapResId 是否复用、尺寸、瓦片进度)+ 转换(补齐缺失)/定位/删产物 + 顺便分组开关。
  风格对标「资产管理」,但地图是瓦片文件夹(无 .lh/prefab),单独成窗不动模型/特效/装配线。
- 删除上一版临时的单图导入窗 MapTileImporter.cs(被列表窗取代)。
用法:打开 `神霄/资源/地图资源` → 选出生场景(日志里 `12005 ok: sceneId=…`,当前是 10000)→「转换并分组」一次转完
→ 之后进图任何位置不再现导、不卡。出真机/小程序包时这些瓦片在 Remote 组,仍是 CDN 异步流式,不打进首包。

### 2026-06-17 地图工具加名字/类型/出生点/缩略图(对标 electron 资源工具)

对标老客户端 Electron 资源工具 `yu_client/tools/yu-resource-tool`(SceneMaps.vue 列表 + MapEditor.vue 标注画布)。
之前地图工具只有编号,不知是哪张图、无标注。本批补:
- 地图**名字/类型/出生点/尺寸**:`MapTileConverter` 读 `config_scene.json`(server,`{id:{name,type,x,y,width,height}}`,
  Newtonsoft 直读,无需 yu_server/python);类型中文对标 MapEditor.formatSceneType。MapStat 带上 Name/SceneType/BirthX/Y。
- `MapAssetWindow`:列表显示「编号 名字」、可按名字搜;详情显示 名字/类型/出生点/尺寸/瓦片进度 +
  **缩略图(底图)+ 出生点标注**(地图像素→缩略图坐标,y 向下)。
- 完整标注点(NPC/怪/采集/传送门/Boss/跳跃点/任务路线)在 electron 里来自 **yu_server** 数据(map_editor.py 解析),
  坐标为世界像素(逻辑格 60×30)。
记忆:已存 `yu-resource-tool-electron`(参考蓝本)、`map-tile-offline-conversion`(卡顿根因+工具)。

### 2026-06-17 完整标注点(NPC/怪/门/Boss)+ 一键转换全部

- `MapServerData.cs`(新):C# 移植 electron `map_editor.py` 的解析,纯编辑器只读:
  - 位置来自 **yu_server** `src/data/create/data_scene.erl`(`get(ID)->#ets_scene{...};`:mon=[[id,x,y,type,group]]、
    npc=[[id,x,y,"action"]]、elem=[{id,x,y,p1,p2}]、reborn_xys=[{x,y}])+ `data_boss.erl`(`get_boss_cfg`/`get_boss_type_name`);
    正则与 python 一致(方括号平衡取列表、`[^;]+` 取记录体)。
  - 名字来自 **yu_client** `config_mon.json`(字段"1"=名,"2"=类型→采集判定)/`config_npc.json`(name/title)。
  - yu_server 路径存 EditorPrefs(默认 `ClientRoot/../yu_server`),窗口可改。
  - **校验**:scene 10000 解析出 npc=34(与运行时 12100 回包 count=34 完全一致)、mon=79,移植正确。
- `MapAssetWindow` 升级:详情缩略图上叠加 出生点/NPC/怪/采集/传送门/Boss/复活 标注(按图层开关,带计数色块)+
  「标注清单」折叠列表(id+名字+坐标,每类上限 60);新增列表页「一键转换全部」(只转未转/部分,强确认+进度+可取消+分组)。
- 跳跃点/任务路线(electron 还有)暂未接;需 data_task.erl + jump 配置,留待后续。
- electron 记忆已更新为"Unity 侧标注点已落地"。

### 2026-06-17 地图标注点 编辑 + 保存(写回 yu_server data_scene.erl)

之前 Unity 地图工具只读;electron 能拖点位/改属性/写回。本批补编辑+保存:
- `MapSceneWriter.cs`(新):C# 移植 map_editor.py 的 `save_scene_fields` + `_replace_*`/`_format_*` + 备份。
  定位 `(get(ID)->#ets_scene{)(body)(};)`,只替换 body 里 mon/npc/elem/reborn_xys/x/y 字段文本(正则+方括号平衡),
  其余原样;写前自动 `.bak_时间戳` 备份;UTF-8 无 BOM。怪(普通+采集)合并回 mon 列表;Erlang 文本格式与 python 逐字一致。
- `MapServerData`:MapEntity 补回写所需原始字段(怪 Type/Group、NPC Action、门 P1/P2);SceneEntities 补 data_scene 的出生点 x/y。
- `MapAssetWindow`:详情加「编辑模式」——缩略图上点标注点选中、拖动移动;右侧属性面板精调坐标/ID/类型/分组/动画/门目标 +
  删除 + 新增(怪/NPC/传送门/复活);「保存到 data_scene.erl」(强确认 + 备份 + 保存后重载)。可编辑:怪/采集/NPC/门/复活/出生点;
  Boss 在 data_boss.erl 只读不写。
- 缩略图是整图缩放(粗拖),精确坐标用属性面板数字框;**缩放/平移画布**(更顺手的拖拽)留作下一步。
- 仍缺:跳跃点(data_scene 无 jump 数据,源待确认 JumpSceneInfo.json)、任务路线(需 data_task.erl 解析)——下一步补。

### 2026-06-17 主角真正显示在地图上(3D 角色合成台)+ 全向转向 + 取景

进度 2.5「主角加载」此前只闭环了数据/动作/相机跟随逻辑,角色**看不见**——根因不是真机问题:
根 Canvas 是 `ScreenSpaceOverlay`(`Launch.unity` m_RenderMode:0),不透明 UGUI 地图会盖住主相机渲染的
任何世界 3D 物体,而 `MainRoleFlow` 把 3D 主角直接摆进世界空间 `__SceneRoot` → 永远被地图遮死。

- `SceneCharacterStage.cs`(新,Common/UI3D):场景 3D 角色合成台,沿用本工程 `UIModelStage` 已验证的
  「专用正交相机 → RenderTexture → RawImage」套路。角色摆隔离区(3000,-3000,3000),相机渲到满屏 RT,
  贴成 `UILayer.Scene` 层里一张 RawImage(自带 Canvas,overrideSorting sortingOrder=-50:压在地图 -100 之上、
  HUD/Main 之下)。`raycastTarget=false` 不吃点击。主角恒居屏幕中心(相机跟随模型,地图在脚下滚)。
  NPC/怪后续按 (世界坐标-主角焦点) 偏移摆进同一台共用合成(已留 `CharsRoot`)。
- `MainRoleFlow.cs`:模型改走 `SceneCharacterStage.SetMainRole(model)`;`MainRoleAgent` 挂轻量逻辑节点跨层
  驱动模型(转向/动作/相机跟随/上报);删掉原把模型摆世界里的 `ApplyOldClientSceneTransform`。
- `MainRoleAgent.Face` 重写为**全向转向**:`yaw = Atan2(dir.x, -dir.y)*Rad2Deg`(连续朝向移动方向,
  对标老客户端 atan2 全向转身),替换旧的「按 dir.x 正负左右翻面」(那是「只会两个方向跑」的根因);
  加 `TurnSmoothSpeed=720` 最短弧平滑。**实跑(2026-06-17)确认上下+左右皆反 → 整体 +180°(两参同时取反),
  已落定为 `Atan2(dir.x,-dir.y)`**。Face 内不叠加基准 yaw(已删 `_baseModelYaw`)。
- 取景:`SceneCharacterStage.ORTHO_SIZE` 2.6→5.1。依据老客户端**场景相机**(正交全高 12.8@1280px、平视;
  `ResManager.ts:1703/1715`)主角占竖屏典型 ~14%;校准点 ortho2.6→占屏~30% 反比得几何 ~5.57,
  再乘俯角补偿 ~0.914(我方相机俯 24° vs 老版平视+模型倾 38°,`SceneObj.ts:469` extent.x*0.79≈cos38°)≈ **5.1**(区间 4.8~5.6)。
  落地点 `GROUND_FRACTION=0.5`(按半高比例,改 ORTHO_SIZE 时接地点不漂)。模型 localScale 保持 1(对齐老客户端
  场景模型本体 scale=1,大小只由相机管;root 1.1 是容器缩放不算个体大小)。

待 Unity Play 验证(若仍有偏差再微调):①四向转身(理应已对;若再现左右反→第一参符号、上下反→第二参符号、整体差180→+180f);
②占屏比例(目标 ~1/7~1/6;偏大调大 ORTHO_SIZE 5.1→5.6,偏小调小→4.8);③接地点调 GROUND_FRACTION;
④首跑看 Console 有无「衣服模型未转换 / 动作未转换」(资源未转,需先用 神霄/资源 转模型/动作)。
注:相机俯角 24° 与老客户端「相机平视+模型倾 38°」透视不同,接地/仰角像素级 1:1 属后续微调。

### 2026-06-17 断线重连根因(待修)+ 地图重进缓存(已修)

实跑发现"地图隔几秒重新加载"实为**断线重连循环**(读 Editor 日志定位):
- 现象链:进游戏成功 → 几秒后游戏服干净关 WebSocket(remote close)→ 客户端游戏内自动重连
  (`LoginController` `MAX_IN_GAME_AUTO_RECONNECT=2`、2s 间隔,预算耗尽即 3 轮)→ 每轮重发 10000/10004 重进 →
  重新 12005 进场景 → 重载地图。日志:`enter-game ack` ×3、心跳 10006 一来一回正常、无 59004 踢人、无客户端报错。
- 服务端根因(查 yu_server 源码):干净 Close **唯一**来自 `Sid ! {send,close}`(`lib_socket_msg.erl:20-24`);
  框架层无空闲/心跳超时断线。最可能 = **顶号 relogin 循环**:同账号重连命中"已在线"(`mod_login.erl:128`)→
  `mod_server:relogin` 是 5s 超时的重活 `gen_server:call`(`mod_server.erl:63-64,102-333`),超时/抛错 →
  `login_outside`(`mod_login.erl:133-137`)发 59004+logout 关连接 → 客户端又自动重连 → 又顶号失败 → 稳定几秒一断。
  诱因:Unity Stop 不发干净登出,账号在服务端**残留在线**,下次进游戏即触发顶号。
- **待决定性证据**:进 Play 清空 Console,看 `[Net]` 的 Close status/desc + 断开间隔(≈5.5s?)+ 断前是否收到 59004,
  即可在 R1(顶号自关)/R2(客户端误判先重连)间一锤定音。修复方向:登出干净化 + 重连退避(2s→≥8s,等服务端释放旧会话)+
  Close 紧跟自身重连时停重连。**断线本身本批未改(等证据),先解地图缓存以便测试。**

**地图重进缓存(本批已修,`SceneMapView.ShowAsync`)**:
- Bug:断线重连进**同一场景**时,`SceneController.Dispose` 虽 keepMap=true 保留了地图,但重进仍调 `ShowAsync` →
  无条件重载底图 + `EnsureTilePool→ResetPool`(Release 所有瓦片 sprite、整屏重刷)→ 整张图变模糊重下。
- 修复:`ShowAsync` 开头加 `IsSameMapShown(data)` 短路(身份 SceneId/MapResId + 尺寸 全一致且视图/瓦片池已建)→
  不 `++_version`、不 ResetPool、不重载底图,只 `SetFocus`(焦点未变整帧跳过)。重连进同图 = 瓦片零重载。
- 关于"编辑器有没有缓存":瓦片 sprite 本由 Addressables 缓存,但旧逻辑 ResetPool 会 Release 使 refcount 归零卸载、
  且未转 Addressables 的瓦片在编辑器走慢速现导(`.jxr→.jpg`+SaveAndReimport)→ 编辑器尤其明显;短路后这两条都不触发。
- 仍在(同principle,可继续收敛):`MainRoleFlow` 仍在每次 `EVT_SCENE_MAP_READY` 重建主角模型 → 重连时角色会重载/闪一下;
  可同样加"同 roleId+同场景已建则跳过"的守卫,本批未动(用户本次只要地图缓存)。

**瓦片 sprite LRU 缓存(本批已修,`SceneMapView`)**:
- 另一个独立现象:跑出一个屏再回来,原区域瓦片又变模糊重载。根因=固定瓦片池只持有"视野+边距",滚出视野的槽被复用、
  旧 sprite 被 Release,走回来即重新异步加载(编辑器还要现导)→ 变模糊。这是池设计本身的取舍,不是 bug,但体验差。
- 修复:加 `_tileCache`(键=格 (row,col))+ `_tileCacheLru`,sprite **归缓存所有**,瓦片槽只引用不持有;
  `AssignSlot`/`PumpLoads` 命中缓存即**同步贴图**(不异步、不变模糊);`Release` 只在 LRU 淘汰或换图/清图(`ClearTileCache`)时发生。
  上限 `_tileCacheCap = clamp(视野格数×3, 96, 256)`(EnsureTilePool 里按池大小自适应),内存有界;
  淘汰跳过当前可见格防变白。**来回走动邻近区域 = 零重载**;只有走出缓存覆盖范围(>cap)再回来才重载。
- 真机内存:cap 256 上限下若瓦片未压缩纹理,最坏约几十 MB;彻底降内存仍指向那条未做的"瓦片走平台压缩纹理(.ktx)"资源管线项。


## 2026-06-21(主线竖切第 1 轮:资源门 → NPC → 任务点击)

> 方向纠偏(任务包 `Docs/Claude任务包-主线竖切-第1轮.md`):停止扩散 UI shell,打通"进游戏后主竖切"
> 到可复现、可见、可验收。按 P0 → P1 → P2 推进,每项一 commit,全程 dotnet build 0 error。

**P0 资源可复现门(commit `0f66cca9b`)**:
- 矛盾:`.gitignore:47` 忽略 `/Assets/Prefabs/UI/`,但 tracked 的 `Remote_Prefabs.asset` 里
  mainuimodule/mapmodule/settingmodule/basewindowskin 等 Addressables 条目(address→guid)指向其中
  prefab,而 guid→prefab 资产只在本机(ignored)→ clean checkout 全是悬空引用。
- 选"入库"路线(非再生成):再生成需 sibling 仓库 yu_client 的 Laya .scene + live Unity 域重载,且
  SaveAsPrefabAsset 给新 guid 与 HEAD 条目不匹配 → 仅 clone 本仓库不可确定性再生成。
- 用 `git add -f` 入库 4 个根 prefab 的递归引用闭包(BFS 不动点 = 23 prefab + meta + 5 目录 meta),
  `.gitignore` 加注释说明例外;其余 105 个 UI prefab 仍忽略(可再生成)。依赖核查:闭包 64 个脚本 guid
  全可解析(57 tracked 工程脚本 + 7 Unity 包组件如 UGUI RectMask2D);248 个未解析 = 散图/字体,仍在
  被忽略的 GameRes/resource(静态贴图缺失=优雅降级,动态图运行时走 ResManager)。
- 验收:4 key 的 HEAD 条目 guid == HEAD prefab.meta guid(全 MATCH);prefab+meta 均可从 HEAD 取出。

**P1 NPC 可见链路(commit `0dd7b327b`)**:
- 新增 `NpcRenderer`(static flow + `NpcRendererDriver` 每帧驱动,与 MainRoleFlow/Agent 同构):订阅
  SceneManager.NpcAdded/NpcRemoved/NpcChanged(此前零订阅者),把 12100/12103 真实 NPC 加载成 3D 模型
  摆进 SceneCharacterStage 合成台。对标老端 Scene.CreateNpc→Npc.InitNpc(yu_client Npc.ts:92-169)。
- 真实模型:`object/npc/model_clothe_{npcId}/model_clothe_{npcId}` + idle 动作(转换产物,**git tracked**
  在 GameRes/object/npc,35 个 NPC 模型;非被忽略的 resource/ 树 → clean checkout 可复现)。缺模型打精确
  blocker,绝不画假模型。位置 = (npcX-roleX, npcY-roleY),相机夹边整图偏移由主角 RawImage 统一承载。
- SceneCharacterStage 扩展 AddSceneCharacter/SetSceneCharacterPixelOffset/RemoveSceneCharacter(不动主角路径)。
- 精确 blocker:名字/称号/icon_scale/朝向来自 server 配置 config_npc,**该配置未导入 Unity**(GetServerConfigPath
  下无 config_npc.json)→ 暂用 NpcId 占位;头顶任务标 sprite 资源路径待确认(12020 的值已落 vo.TaskIcon
  并经 NpcChanged 到达渲染层,链路通)。

**P2 任务点击链路最小入口(commit `6581c648b`)**:
- `TaskModel.DoTask(TaskVo)`:置 NowSelectTaskId + 广播 EVT_TASK_SELECT_CHANGED,按 task_tips_type 进三分支。
  对标老端 TaskModel.DoTask(yu_client TaskModel.ts:744-784 + 797 switch)。
- 修 bug:`IsFindNpcTask` 错占位 `==0` → Talk(5)/StartTalk(6)/EndTalk(7)(核对老端枚举 + ts:2966-2972)。
- 三分支(真实路由 + 精确 blocker):①找 NPC 对话 = SceneManager.GetNpc 真实定位,对话链
  (Scene.MainRoleToNpc→SHOW_TASK→DialogueController→12101/12102)未移植 → blocker;②完成 = TaskFinishView
  (+30004)未移植 → blocker(不跳弹层直发协议);③带场景坐标 = 寻路(无 A*)/飞鞋(USE_FLY_SHOE)未移植 → blocker。
- `MainUITaskItem` 点击(UIUtil.AddClick `_img_bg`)→ DoTask;`MainUITaskTeamView` 订阅选中变化刷 `_img_select`。
- "对话已开则不重复 DoTask"去重(老端 MainUITaskTeamView.ts:563-573)依赖 DialogueModel,待其移植后补。

**本轮统一 blocker(下一步价值最高)**:NPC 对话子系统(DialogueController/DialogueModel/12101/12102 + Scene.MainRoleToNpc
+ SHOW_TASK 事件)——它同时是 P1 任务标语义、P2 找 NPC 分支、journey 新手引导的共同前置。其次:config_npc 导入
Unity 配表流水线(解锁 NPC 名字/称号)、TaskFinishView 完成弹层、自动寻路/飞鞋。

**未做(诚实声明)**:三项的 Unity Play 实跑未做(本环境只编译,不代表运行)。3D 合成台精确对位承
2026-06-17「待真机微调」。Play 验收清单见各 commit。

---

## 主线竖切 第 2 轮(2026-06-21):NPC 对话子系统 + 朝向 NPC

**P1 NPC 对话子系统最小闭环(commit `2a3ae18bc`)**:
- 新增 `Module/Core/Dialogue/`:DialogueController / DialogueModel / NpcDialogVo / DialogueView /
  NpcConfigs / TalkConfigs / DialogueNodeVo / DialogueTypeConst。对标老端 commonController/DialogueController.ts
  + commonModel/DialogueModel.ts + dialogue/NpcDialogVo.ts。
- 协议(byte 级出处 = `cdn/resource/config/client/ClientProtocol.zip → ClientProtocol.json`,与已验证 12100 同源交叉核对):
  - 12101 发 `"i"`(npc_id);收 `npc_id:i + task_list[h×{task_id:i, task_state:c, task_name:s, task_type:c}]`。
  - 12102 发 `"ii"`(npc_id, task_id);收 `npc_id:i, task_id:i, talk_id:i`。
  - 30003/30004/30007 发 `"i"`(接/交/对话事件;仅发,服务端回推 30001/30000 刷新)。
  - Proto 常量:CC_NPC_TASK_LIST / CC_NPC_TASK_TALK / CC_TASK_ACCEPT / CC_TASK_FINISH / CC_TASK_TALK_EVENT。
- 链路:TaskModel.DoFindNpcTask → DialogueController.ShowTask(发 12101)→ On12101 装配 NpcDialogVo(NPC 默认对话)
  → ShowNpcTalk(单内容块+单行+单任务且 state≠2 → 短路 SelectTask 发 12102;否则展默认对话 + 任务菜单)
  → On12102 用 talk_id 查 config_talk 展任务对话 → 点动作节点(TRIGGER/FINISH/FINISH_AND_TRIGGER/TALK_EVENT)
  发 30003/30004/30007。对话已开则不重复请求(DialogueModel.DialogIsOpen 去重)。
- 数据真实:对话文字 100% 来自 config_talk;NPC 名来自 config_npc;任务来自 12101 真实回包。
  DialogueView = 最小原生 uGUI 临时壳(已标注 TEMP,字体复用场景已开 MainUI 的 TMP 含中文),数据/入口均真,无假对白。
- config_npc(265)+ config_talk(959)经 `ClientConfigSync.SYNC_LIST_SERVER` 从 yu_client 同步进 GameRes
  (可再生成路线,与 config_task 一致;`/Assets/GameRes/resource` 被 .gitignore:46 忽略,SYNC_LIST 为可复现源)。
  **→ 解除上一轮「config_npc 未导入」blocker**(上文 P1 NPC 可见链路那条已被本轮取代)。
- 验证:dotnet build 0 错;Unity 编译 0 错(Shenxiao.Module.Core.dll 重建,Console 0 Error,8 个 .cs.meta 生成);
  离线配表数据路径自检(PowerShell + Unity MCP):npc 100101(云霄月华)→ config_npc.talk 100101 → config_talk.content
  →「月见梦萦始莫离,弥生魂绕终需醒。」;talk 101 → NPC 行 + FINISH 节点(「完成任务」,IsActionNode=true)。

**P2 朝向 NPC(commit `26fad79f3`)**:
- `MainRoleAgent.Current` 句柄(Init 置 / OnDestroy 清)+ `FaceTowardPixel(targetX,targetY)`(复用 Face 的
  yaw 解算 `Atan2(dir.x,-dir.y)`,瞬时落位;玩家推摇杆后 Update→Face 自动接管,不冲突)。
- `TaskModel.DoFindNpcTask` 定位 NPC 后先 FaceTowardPixel(npc.X,npc.Y) 再 ShowTask。对标 Scene.MainRoleToNpc 的
  `main_role.SetDirection(npcpos.getDir(rolepos))`(Scene.ts:1437-1438)。玩家可见:点找-NPC 任务 → 主角转身面向 NPC → 弹对话。

**本轮 blocker / 未做(诚实声明)**:
- Play 活服往返未做:本 worker 无法驱动登录/服务端(Unity MCP 桥间歇可用,RunCommand 沙箱禁 System.Reflection
  且 EnsureLoaded 异步无法在编辑器同步阻塞)→ 12101/12102 实包未跑;以 dotnet+Unity 双编译 0 错 + 离线配表路径自检替代。
- 30004 完成奖励展示 / award_list(special_goods_list 经 ErlangParser 按职业过滤)未做 → 对话内暂不展奖励。
- TaskFinishView 完成弹层未移植(非对话的「完成」分支仍 blocker)。
- 走到 NPC 的移动(直线 + 碰撞滑行 / A* 寻路)、USE_FLY_SHOE 跨场景未做(本轮只转身)。
- DialogueView 仍为原生临时壳,待 LayaUI 转换产出 DialogueViewBind/prefab 后替换。
- NpcRenderer 名牌/缩放/朝向:config_npc 现已导入可接,本轮未接(见第 3 轮包)。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第3轮.md`。