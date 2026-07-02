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

## 主线竖切 第 3 轮(2026-06-21):走到 NPC 再开对话 + 完成弹层 + 奖励解析

commit `656490ba3`(P1+P2 一并;TaskModel.cs 同时含两者改动,无法按文件拆 commit)。

**P1 走到 NPC 再开对话(对标 Scene.ts:1417-1472 MainRoleToNpc → MainRoleMove)**:
- `MainRoleAgent`:抽出 `Advance(map,mx,my)` 分轴撞墙滑行内核(整向→仅X→仅Y,对标 MainRole.ts:794-819),
  手动摇杆 `StepMove` 与新增自动接近 `AutoStep` 共用;`BeginMoveAnim`/`ThrottledSend` 同抽出复用。
- 新增 `MoveToNpc(tx,ty,arriveLogicDist,onArrive)`:直线接近目标像素点,到达半径=2.5 逻辑格
  (像素差 /60、/30 换算逻辑格求欧氏距离,对标老端 dist=2.5 + LogicRealRatio 60/30);**必有兜底**——
  卡死(连续 0.6s 无位移进展)或超时(8s 沿墙滑行未达)也触发 onArrive 把对话开出来,**绝不软锁**;
  玩家推摇杆即 `CancelAutoMove`(丢回调、让位手动)。已在范围内则立即触发(对标 `<=dist+1` 早退)。
- `TaskModel.DoFindNpcTask`:`agent.MoveToNpc(npc.X, npc.Y, 0, () => ShowTask(npcId))`——走到 NPC 身边、
  停下转身后才发 12101,替换第 2 轮「原地转身 + 立刻开对话」。无 MainRoleAgent 时降级直接开对话(不软锁)。
- 无 A* 寻路是与老端的真实差异(已 log):直线 + 滑行逼近,绕不开凹形障碍 → 靠卡死/超时兜底把对话开出来。

**P2 完成弹层 TaskFinishView + 30004 + 奖励解析(对标 task/TaskFinishView.ts:75-79 + DialogueController.ts:45-110)**:
- 新增 `TaskFinishView`(原生 uGUI TEMP 壳,同 DialogueView 约定:ViewManager.GetLayer(Popup) + 复用场景 TMP 字体):
  居中弹层展任务名/目标(完成标)/描述 + 真实奖励列表 + 「领取奖励/提交任务」按钮 → `TaskController.SubmitFinish`
  发 30004 → Close;背景点击 / × 可关。物品图标/名称需 GoodsModel+config_goods(未移植)→ 暂以 type_id×count 真值呈现。
- `TaskController.SubmitFinish(taskId)`:BaseController 发 30004(对标 Fire(REQUEST_CCMD_EVENT,30004,task_id));
  `TaskModel.DoFinishTask` 懒建并打开 TaskFinishView,**替换原 blocker**(对标 Fire(TASK_OPEN_VIEW,'TaskFinishView'))。
- 新增 `TaskReward`(对话弹层共用解析):`config_task` 字段 23 special_goods_list / 24 award_list 经 `ErlangParser` 解析:
  - special_goods 3 元组 `{career,type_id,count}`(实测现网格式):career==当前职业 或 ==0(通用)才计入;type_id==0 记货币/经验。
  - award_list 4 元组 `{a,b,type_id,count}`:取 [2]/[3] 为 type_id/count 全计入(任务奖励总表)。
  - **修正老端 TaskFinishView 的 bug**:老端用 vo[1] 作职业过滤且按 [0,vo[2],vo[3]] 读取,与现网 3 元组不符(早期 4 元组残留);
    此处以真实 config 为准、职业索引取 [0](与 On12102 一致)。
- `TaskConfigs.TaskCfg` / `TaskVo.ApplyConfig` 接入字段 23/24;`On12102` 用 `TaskReward` 装配 `NpcDialogVo.RewardSummary`,
  `DialogueView` 在对话里展奖励摘要(对话/弹层共用同一解析)。

**验证**:
- dotnet build `yu_client_unity.slnx` 0 错(仅 1 个既有 CS0162 无关警告,Face() 的 TurnSmoothSpeed<=0 死支,非本轮引入)。
- Unity 编辑器编译 0 错(MCP ReadConsole Error=0;Unity 自动重导入并把 2 个新脚本写进 .csproj + 生成 .meta)。
- `TaskReward` 以真实 config 数据运行期单测(MCP RunCommand)通过:
  `[{5,0,150000},{3,0,20000},{0,17020001,2}]`+`[{1,1,101011010,1},{2,2,102011010,1}]`
  → career5 = 4 项(奖励×150000 / 物品17020001×2 / 101011010×1 / 102011010×1);career3 = 4 项(货币换 ×20000);
  career1 = 3 项(无本职业货币,仅通用+award);空表 = 0;task100010 career5 = 1 项(奖励×150000)。

**解除的第 2 轮 blocker**:走到 NPC 移动(P1)、TaskFinishView 完成弹层(P2)、30004 提交、award_list/special_goods 奖励解析(P2)。

**本轮 blocker / 未做(诚实声明)**:
- Play 活服往返仍未做:无法驱动登录/服务端 → 走到 NPC 的真实位移、12101/12102 实包、30004 提交后 30001 刷新任务栏
  未在活服跑通;以 dotnet+Unity 双编译 0 错 + TaskReward 真实数据运行期单测替代。要跑活服看的日志:
  `MoveToNpc: 自动直线接近…` → `MoveToNpc 到达/未抵达…` → `send 12101` / `TaskFinishView 打开` → `send 30004 finish`。
- 奖励物品图标/名称未做:GoodsModel + config_goods + GetMappingTypeId 未移植 → 弹层/对话以 type_id×count 真值呈现(非假数据,但非成品外观)。BaseAwardItem 组件已存在可复用(其图标查 GoodsModel 亦是 TODO)。
- NpcRenderer 名牌/缩放/朝向、DialogueView/TaskFinishView 立绘头像(config_npc icon/image)未接(本可作 P3 fallback,因 P1/P2 未被卡住未触发)。
- USE_FLY_SHOE 跨场景、A* 寻路(本轮直线 + 滑行 + 兜底替代)、TaskFinishView 自动提交倒计时(老端 close_time)未做。
- TaskFinishView/DialogueView 仍为原生 TEMP 壳,待 LayaUI 转换产出 Bind/prefab 替换。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第3轮.md`。


## 主线竖切 第 4 轮(2026-06-21):奖励真实物品名 + NPC 名牌/缩放/朝向

commit `3dff3442c`(P1 真实物品名)+ `d170a5f54`(P2 NPC 名牌/缩放/朝向)。文件无交叠,按 P 拆 commit。

**P1 奖励显示真实物品名(对标 commonModel/GoodsModel.ts + task/TaskFinishView.ts:206-213)**:
- `ClientConfigSync.SYNC_LIST_SERVER` 加 `config_goods`(对标 config_task/config_npc 路线);实测现网为
  【数字索引键】(同 config_task,非 config_npc 具名键):"0"=id "1"=goods_name "9"=goods_icon "10"=color/品质。
- 新增 `GoodsModel`(对标 GoodsModel.ts):`EnsureLoaded`/`GetGoodsBasicByTypeId`/`GetGoodsName`/
  `GetGoodsIcon`/`GetColor`/`GetMappingTypeId`(JObject 数字键读取,同 NpcConfigs 套路);
  `TaskController.OnGameStart` 预载(与 config_task 同点)。
- `TaskReward.ToText` 用 `GoodsModel.GetGoodsName` 把裸 type_id 换成真实物品名(淬魂原石/初樱轻剑…);
  **完成弹层 TaskFinishView 与对话奖励摘要 On12102 共用 ToText,一处改两处自动生效**;货币/经验(type_id==0)
  保持"奖励 ×N"(老端货币不进 goods 表)。
- `BaseAwardItem.RefreshIcon`(此前是 GoodsModel TODO)接 `GoodsModel` + `ResManager.SetImageAsync` 真实图标
  (对标 BaseAwardItem.ts SetData);全项目复用的物品格子件即时受益,不只奖励。

**P2 NPC 名牌/缩放/朝向(对标 scene/sceneobj/Npc.ts:92-169)**:
- `NpcRenderer` 接 `NpcConfigs`(第 3 轮已导入 config_npc):`EnsureLoaded` 兜底(场景 NPC 可能先于对话子系统出现)。
- 缩放:`AddSceneCharacter(model, icon_scale)`(对标 Npc.ts:109-110 this.scale = icon_scale)。
- 朝向:`brith_rot+90` → Laya 朝向向量 (cosθ,sinθ) → `Atan2(dx,-dy)` 解出模型 yaw,**与 MainRoleAgent.Face
  完全同一解算**(对标 Npc.ts:178-180 SetRotateY(brith_rot+90));brith_rot==-1 保持待机默认朝向。
- 名牌:Scene 层屏幕跟随 TMP(称号金 #fcf910 / 名字青 #c2fdfa,对标 Npc.ts:99-107 NameBoard.SetName/SetNpcName);
  anchoredPosition=(npc 像素 - 相机像素) 每帧跟随 + 头顶偏移(与合成台/地图同一锚定口径);无名降级不挂(不写假名)。
- **为何名牌用最小原生 TMP(诚实声明)**:转换产物 `NameBoardBind` 只有血条节点、**无名字文本节点**(改它要动
  generated prefab,禁止);且 NPC 经合成台 RT 合成(2D NameBoard 无法直接叠进 RT 里的 3D 体)→ 按
  DialogueView/TaskFinishView 同例做最小原生 TMP,待 NameBoard 补名字节点后替换。

**验证**:
- dotnet build `yu_client_unity.slnx` 0 错(含 `--no-incremental` 强制重建 Module.Core;6 个既有无关警告:
  AppLauncher CS0649 / generated Bind CS0108 / MainRoleAgent CS0162,均非本轮引入)。
- Unity 编辑器编译 0 错(MCP ReadConsole Error=0;Unity 自动重导入 GoodsModel.cs 并写进 .csproj + 生成 .meta)。
- 运行期校验(MCP RunCommand,跑真实同步后读 GameRes 文件):config_goods 同步进 GameRes(9230 条),
  17020001=淬魂原石/101011010=初樱轻剑/31=九洲灵钱 真实名按索引 "1" 就位、UTF-8 无乱码;config_npc 真实名/
  称号在位(云霄月华/觉梦仙子);brith_rot→yaw 与 Face 一致(270→90 右/0→180 下/90→-90 左/180→0 上)。

**解除的第 3 轮 blocker**:奖励物品真实名(GoodsModel/config_goods)、NpcRenderer 名牌/缩放/朝向。

**本轮 blocker / 未做(诚实声明)**:
- **goodsIcon 真实图标未导入**:`Assets/GameRes/resource/game/goodsIcon/` 为空(yu_client 源
  `cdn/resource/game/goodsIcon/*.png` 在,但未进 Unity)→ 奖励/物品**图标**降级隐藏、以**真实名称**呈现
  (BaseAwardItem.RefreshIcon 写精确 blocker:缺哪个 key)。补法:神霄/资源 SpriteImporter 导 goodsIcon。
- 品质底板 `com_goods_plate_{color}`(common 图集)同未导入 → BaseAwardItem 暂保留 prefab 默认底板。
- Play 活服往返仍未做(无登录会话):弹层真实名/NPC 名牌的**屏上可见**未在活服跑通;以双编译 0 错 + 真实数据
  运行期校验替代。活服要看的日志:`config_goods loaded: 9230 goods` / `npc render: … name="…" brithRot=…` /
  完成弹层奖励名 / 名牌随 NPC 跟随。
- 名牌竖直贴合(头顶偏移 150px)、icon_scale 视觉大小、brith_rot 朝向 屏上观感属"待真机微调"(同合成台落地点经验值)。
- NPC 立绘/头像(config_npc.image)对话弹层未接(P3 fallback,本轮 P1/P2 未被卡住未触发)。
- 货币/经验真名(exp/coin)需客户端 `ConfigNotNormalGoods`(未同步)+ TaskReward 保留货币 type → 暂"奖励 ×N"。

## 主线竖切 第 5 轮(2026-06-21):奖励真实图标 + 品质底板 + NPC 对话 3D 立绘

commit `f29f7831e`(P1 真实图标/品质底板)+ `97324b93f`(P2 NPC 立绘)。文件无交叠,按 P 拆 commit;资源走
`/Assets/GameRes/resource`(gitignore)+ `object/`(tracked,已有),故 commit 仅代码。

**P1 奖励真实图标 + 品质底板(对标 commonModel/GoodsModel.ts + common/BaseAwardItem.ts:254-279)**:
- **根因订正(关键)**:第 4 轮把 config_goods 数字键 `"9"`/`"10"` 当 icon/color,实为 `type`/`subtype`;
  权威 schema `cdn/resource/config/server/config_table_default.json` 的 config_goods 字段名列表:
  **`"14"`=goods_icon、`"18"`=color**(`"1"`=goods_name 第 4 轮恰好对,故名字一直正常)。键错 → 拼出
  `10.png` 等不存在 key,图标恒降级隐藏;订正后 101011010→icon `1010101`(yu_client/cdn 真实存在)。
  → `GoodsModel.cs` K_ICON 9→14、K_COLOR 10→18;新增 `GetDisplayColor`(含老端 26270005/26260005→7 特例);
  顺手订正 `ClientConfigSync.cs` 注释。(第 4 轮进度段 line 610 的 "9=icon/10=color" 系此 bug 的历史记录,已订正。)
- `BaseAwardItem.RefreshIcon` 增品质底板:`item_bg = com_goods_plate_{color}`(`GameResPath.GetIcon("common",…)`
  + `ResManager.SetImageAsync`,对标 `AtlasUrl("common",…)`)。全项目复用的物品格子件即时受益。
- `TaskFinishView` 完成弹层增**真实物品图标行**(品质底板+图标+数量+名称),复用 GoodsModel/GameResPath/ResManager
  真实数据与加载路径(货币/经验走底部文本)。
- **资源落地路径**:`ResManager` 既有编辑器兜底(`TryImportLooseImageFromClient`)在缺图时自 `yu_client/cdn`
  自动拷入并导成 Sprite → goodsIcon/com_goods_plate **无需手动批量导入**,首次加载即落地(打真机包前仍需跑分组)。

**P2 NPC 对话弹层真实 3D 立绘(对标 dialogue/DialogueView.ts:552-564 SetRoleModel)**:
- **老端真相**:对话头像 = `SetRoleModel` 渲【真实 3D 模型】(clothe_res_id=config_npc.icon、type=icon_type),
  `config_npc.image` 全 262 条恒 `"0"` 不用于头像 → **不做假 2D 头像**,直接接真实立绘(避免假图)。
- `DialogueView` 增 `_modelBox`(面板上方左侧)+ `ShowNpcModel`:config_npc.icon → `object/npc/model_clothe_{icon}`
  真实模型(SkinnedMesh,git tracked,已有 70 个),经 `UIModelStage`(登录角色预览同款"隔离区相机→RT→RawImage")
  渲入;`PlayNpcIdle`(`object/npc/action/{icon}/idle`,对标 `NpcRenderer.PlayIdle`);缺模型/动作降级 + 精确 blocker。
- 生命周期:Open 异步加载(`_modelEpoch` 竞态闸)、Close 清场(`UIModelStage.Clear` + ++epoch 取消在途)。

**验证**:
- dotnet build `yu_client_unity.slnx` 0 错(仅 1 个既有无关警告 MainRoleAgent CS0162);Unity 编辑器编译 0 错
  (MCP ReadConsole Error=0,两次编译均经域重载后核对)。
- **可见证据(MCP RunCommand 渲染,真实资源)**:
  - P1 `Temp/p1_reward_row.png`:任务 100020 四件奖励【初樱轻剑/仙剑/长枪/短弓】真实图标 + 绿色品质底板
    `com_goods_plate_1`(color=1)+ 真名(资源经 ResManager 兜底自 yu_client/cdn 导入,sprite 86×86 非空)。
  - P2 `Temp/p2_npc_lihui.png`:NPC 100101(云霄月华/觉梦仙子)真实 3D 立绘(全身星纹仙装,按 UIModelStage 同参渲染,清晰可辨)。
- 真实数据校验:config_goods["101011010"] 键 "14"=1010101 / "18"=1;`10.png`(旧错键值)在 yu_client 不存在、
  `1010101.png` 存在 → 印证"非缺资源,是读错配表键"。

**解除的第 4 轮 blocker**:奖励真实图标 + 品质底板(P1);NPC 对话立绘(老端 P3/本轮 P2,确认为 3D 模型非 2D 头像)。

**本轮 blocker / 未做(诚实声明)**:
- **活服整合往返截图未做(无登录会话)**:P1 完成弹层、P2 对话立绘的**屏上整合可见**未在活服跑通;以双编译 0 错
  + 真实资源 RunCommand 渲染截图替代。活服要看:`config_goods loaded` / 完成弹层图标行 / `对话立绘: npcId=… model=…`。
- **`BaseAwardItem.prefab` 缺 Bind 组件**(根仅 RectTransform,对照 `ItemInfoItem.prefab` 有挂)→ 经 prefab 复用
  受阻;故 TaskFinishView 按既有原生 TEMP 壳风格自建图标行(数据/加载路径仍走 GoodsModel/ResManager 真实路径)。
  根治 = 修 UI 转换器补挂 Bind 组件 / 或加一次性回填工具(第 6 轮)。
- **货币/经验真名**:`ConfigNotNormalGoods`(type→goods_id,如 5→32 经验、3→31 金币)在 yu_client 客户端配置存在、
  可经 `SYNC_LIST` 同步;但 special_goods_list 元组 `{5,0,150000}` 首元到底是 currency-type 还是 career **存疑**
  (老端 TaskFinishView.ts/DialogueController.ts 两处解析互相矛盾,且为早期 4 元组残留)→ 贸然实现有误标风险(违"禁假数据"),
  须先以活服 12102 实包定语义,转第 6 轮。
- 立绘构图(scale/position/talk_scale/朝向)、数量>1 角标(测试任务 100020 奖励均 count=1,角标规则在但未展示)属待真机微调。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第6轮.md`。


## 主线竖切 第 6 轮(2026-06-21):BaseAwardItem 可复用(回填 Bind 组件)+ 货币/经验真名

commit `8f700ee4c`(P1 回填 Bind 组件 / 复用 BaseAwardItem)+ `c776bbfca`(P2 货币经验真名)。文件无交叠,按 P 拆 commit。

**P0 保护基线**:worktree 干净;第 5 轮链路 rg 命中(K_ICON/GetDisplayColor/com_goods_plate/ShowNpcModel/UIModelStage);
dotnet build 0 错、Unity 编译 0 错;未重做第 5 轮。

**P1 BaseAwardItem.prefab 可复用(对标 common/BaseAwardItem.ts;修工具不点杀)**:
- 根因:shared-prefab/standalone(如 common/BaseAwardItem)不随某模块流水线重转 → 漏挂 Bind 组件,经
  `ResManager.InstantiateAsync` + `GetComponent<BaseAwardItem>()` 得 null(对照 view 类 ItemInfoItem 根有挂)。
- **修工具**:`LayaBindFiller` 抽出 `EnsureBindOnWindow` 核心;新增可重跑全量回填 `FillAll` + 菜单
  「神霄/UI/回填 Bind 组件 /(预览不写盘)」:扫 `Assets/Prefabs/UI/**` 给缺组件的窗口根/内联模板按 `*Bind` 节点名
  补挂业务子类 + 回填序列化引用,**仅变更才写盘**(避免无关 diff),出报告 `Reports/LayaUI/bind_backfill_report.md`;
  嵌套 prefab 实例从源继承、不重复挂。去掉全局 `AssetDatabase.SaveAssets()` 避免刷会话脏字体。
- 跑工具:扫 128 prefab,**补挂 36 组件 / 29 prefab**(BaseAwardItem/EquipmentItem/CustomHeadItem/各 *Item/Tab…);
  其中 9 个已 git 跟踪的入库,其余在 `.gitignore` 的 `/Assets/Prefabs/UI/` 下(可重生成)。
- `TaskFinishView` 去掉自建图标行,改 `InstantiateAsync(common/BaseAwardItem)+GetComponent+SetData` 复用真实格子
  (`async Task` 防竞态 + `ReleaseInstance`);名称走 `_rewardText`。

**P2 货币/经验真名(先定语义、禁臆造;对标 GoodsModel.ts GetMappingTypeId + ConfigNotNormalGoods)**:
- **元组语义实证**(非凭老端矛盾静态码猜):现网 `config_task` special_goods_list 首元**全量分布**
  `{0:86, 2:32, 3:542, 5:501, 10:1, 255:32}` 全为 ConfigNotNormalGoods 类型键(非职业:0/10/255 不可能是职业,
  货币恒 `{type,0,count}`)→ 元组 = `{type, type_id, count}`。老端 TaskFinishView(`vo[1]`)/DialogueController(`vo[0]`)
  当职业过滤的解析是早期 4 元组残留、对现网 3 元组失配且两处互相矛盾,故不照抄。
- `ClientConfigSync.SYNC_LIST` 加 `ConfigNotNormalGoods`(client 配置)。
- `GoodsModel`:加载 ConfigNotNormalGoods;`GetMappingTypeId` 接表(0→typeId;100→绑定;-1/255→键在 typeId;
  其它→键在 type),补 `GetNotNormalDesc` 兜底名。
- `TaskReward` 按 `{type,type_id,count}` 解析:GetMappingTypeId 还原真实 goods_id、flat 3 元组通用无职业过滤、
  名称 config_goods 优先→desc 兜底;嵌套 `{career,[...]}` 礼包跳过(留后续)。TaskFinishView/DialogueController 共用 Build/ToText 自动生效。

**验证**:
- dotnet build 0 错(6 既有无关警告);Unity 编译 0 错(MCP ReadConsole Error=0,两 P 各经域重载后核对)。
- **P1 可见证据**(MCP RunCommand 渲染真实资源):`GetComponent<BaseAwardItem>` 非 null、refs 回填
  (item_bg/icon/num_text 非空);`SetData(101011010,1)` → `Temp/p1_baseawarditem.png`:初樱轻剑(icon 1010101)
  真实图标 + 绿品质底板(com_goods_plate_1),非降级。
- **P2 运行期单测**(MCP RunCommand,真实同步数据):map(5,0)→32 经验 / map(3,0)→31 九洲灵钱 / map(2,0)→35 绑定灵玉 /
  map(0,17020001)→淬魂原石 / map(255,36255042)→至尊币;
  `TaskReward.Build([{5,0,150000},{3,0,20000},{0,17020001,2}])` → 「经验 ×150000 / 九洲灵钱 ×20000 / 淬魂原石 ×2」
  (此前货币恒"奖励 ×N")。
- **整合可见** `Temp/p2_reward_panel.png`:任务完成弹层(真实 BaseAwardItem 淬魂原石格 + 数量角标 2)+ 奖励真名汇总
  「经验 ×300000 / 九洲灵钱 ×20000 / 淬魂原石 ×2」——P1 复用 + P2 真名同框。

**解除的第 5 轮 blocker**:BaseAwardItem.prefab 缺 Bind 组件(P1,根治=可重跑回填工具);货币/经验真名(P2,元组语义已实证)。

**本轮 blocker / 未做(诚实声明)**:
- 活服整合往返(登录→进场景→点 NPC 弹立绘→接/交任务→完成弹层→30004→30001 刷新)仍未在活服跑通:本会话无登录会话/
  无法驱动服务端,以双编译 0 错 + RunCommand 真实资源渲染/运行期单测替代。
- 嵌套 `{career,[{...}]}` 职业定制礼包(circle/循环任务,config_task 中 18 处 `{N,[`)未处理:TaskReward 按"非 3 元组"
  跳过,留后续轮(需职业过滤 + 子列表解析)。
- 货币图标:exp/coin 经 goods_id(32/31)走 config_goods;本轮货币走 `_rewardText` 文本(不进物品图标格)以规避货币
  无 goods_icon 时的降级——让货币也成图标格留第 7 轮。
- BaseAwardItem 点击 tips(UIToolTipMgr)、effect_con 物品特效未移植(P1 之外)。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第7轮.md`。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第5轮.md`。

## 主线竖切 第 7 轮(2026-06-21):奖励货币图标格 + 嵌套职业礼包解析(P2 完成 / P1 背包页转活服 blocker)

commit `753ff3b39`(P2 货币图标格 + 嵌套 `{career,[...]}` 职业礼包)。P1 背包真实物品页本轮**未写码**:核心数据链(bag 协议 + `BagModel`)缺,按红线「核心没跑通前不写辅助代码」转精确 blocker(协议已定位,见下)。

**P0 保护基线**:worktree 干净(仅本轮 2 文件);第 6 轮链路 rg 命中(GetMappingTypeId/GetNotNormalDesc/FillAll/EnsureBindOnWindow/ConfigNotNormalGoods,7 文件);dotnet build 0 错(1 既有无关警告 MainRoleAgent.cs);未重做第 6 轮。

**P2 货币也成图标格 + 嵌套职业定制礼包(对标 TaskFinishView/GoodsModel;真实 config 驱动,禁臆造)**:
- **嵌套礼包解析**(`TaskReward.AppendSpecialGoods`):special_goods_list 除 flat 3 元组 `{type,type_id,count}` 外,
  另有嵌套 `{career, [{type,type_id,count},...]}`(circle/循环任务)。新增按当前职业过滤 + 解析子列表(抽 `AppendTriple` 复用);
  判别 = 元组第 2 元是 List(`ErlangTerm.Kind.List`)。career 参数(第 6 轮预留)启用。
- **货币图标格**(`TaskFinishView.BuildRewardCells`):由按 `IsCurrency` 强制走文本,改为按真实 `config_goods` 是否有
  `goods_icon` 路由——有图 → `BaseAwardItem` 图标格;无图 → 文本行。config 未加载时优雅降级回文本(无回归)。

**验证**(Unity RunCommand,running domain 已含新码):
- **嵌套解析**:真实样本 `[{1,[{0,39510031,2}]},{2,[{0,39510032,2}]},{3,[{0,39510033,2}]}]` →
  career=1→39510031 / 2→39510032 / 3→39510033(各 ×2);career 无匹配 → 0 项。
- **货币映射/图标**(主源 config 实证):ConfigNotNormalGoods 3→31/5→32/1→34/2→35;config_goods 31→icon31(九洲灵钱)/
  32→icon22(经验)/34→icon34(灵玉)/35→icon35(绑玉)→ 货币均 iconable → 进图标格。映射路径沿用第 6 轮已活服验证的
  GetMappingTypeId/GetGoodsName(第 6 轮 `Temp/p2_reward_panel.png` 已证弹层渲染本体,本轮仅把货币条目移入同一图标格)。
- 诚实声明:本会话编辑器处 Play 态但 config 未加载(ResManager 异步未泵,`GoodsModel.IsLoaded=False`),弹层真机截图未重拍;
  以上为运行期单测 + 主源配置实证 + 第 6 轮渲染本体。

**解除的第 6 轮 P2 遗留**:嵌套 `{career,[...]}` 职业礼包(已职业过滤解析);货币也成图标格(已按真实 goods_icon 路由)。

**本轮 blocker / 未做(诚实声明)**:
- **P1 背包入口真实物品页 = 活服 blocker(协议已精确定位)**:Unity 侧仅有视图壳(`BagFlow`/`BagComponentView`/
  `BagItemRenderer` + `BagModule.prefab`),**无 `BagModel`、无 bag 协议**。主源定位(`ClientProtocol.json`):满背包拉取 = **15010**
  (client 送 `pos`(h,bag=4);server 回 `pos:h, cell_num:h, max_cell:h, cell_gold:c, goods_list:[u16 计数数组]`,
  每项 `goods_id:l, type_id:i, sub_pos:c, cell:h, goods_num:i, bind:c,...,color:c,...` + 3 嵌套属性数组 addition_attrlist/
  equip_extra_attr/awake_list);15017/15018 为增量推送(非满包)。Unity 待镜像模式 = `BaseController.RegisterProtocal/SendFmt`
  + `NetReader.ReadArray`(对照 `TaskController.On30000` / `SceneController.On12100`)。**真实物品页需活服回 15010 实包**——本会话
  无登录会话、ResManager 异步未泵、config 未加载;从零写解析器无真包即无法端到端验证、亦无可见物品页,故不写投机解析器/不造假背包,
  转 round 8(协议已抄齐,待活服联调)。
- `BagItemRenderer` 复用 `BaseAwardItem` View 的接线(`BagItemData.TypeId` + `SetData(typeId,count)`)与 15010 解析同属一条
  数据链,与协议同轮做、同包验证(避免「核心未通先写辅助」)。
- 活服整合往返、`BaseAwardItem` 点击 tips、立绘/格子微调:均需 config 加载/真机渲染,本会话编辑器 Play 态不具备,转 round 8。

## 主线竖切 第 8 轮(2026-06-21):背包真实物品页落地(15010)+ 物品点击弹真实详情(P1+P2 双落地)

commit `51dc670e2`(P1 背包 15010 + P2 物品 tips)。第 7 轮定位的「活服 blocker」本轮**收窄到只剩数据**:协议/Model/解析/渲染全链已写齐并真机渲染验证,活服回 15010 即显真背包。

**P0 保护基线**:worktree 干净;第 7 轮链路 rg 命中(AppendTriple/ErlangTerm.Kind.List/GetGoodsIcon(rewards,3 文件);dotnet build 0 错(1 既有无关警告 MainRoleAgent.cs);未重做第 7 轮。

**P1 背包入口真实物品页(BagModel + 15010 + 复用 BaseAwardItem,对标 GoodsController/BagModel.ts;字段照抄 ClientProtocol.json)**:
- **新增 `BagModel`**(`Module/Core/Bag`):`BagGoodsList`(对标 `GoodsModel.bag_goods_list`)+ `MaxCell`/`CellNum`;
  `SetBagFull` 对标 `CreateBagList`(清空再装入满包全量语义)。`BagGoods` 暂存显示 4 字段 `type_id/goods_num/color/cell` + 主键 `goods_id`。
- **新增 `BagController`**(`Module/Core/Bag`,镜像 `TaskController`):`RegisterProtocal(15010, On15010)` + `EVT_GAME_START`
  发 `SendFmt(15010,"h",4)`(对标 GoodsController GAME_START 批量请求 bag pos);注册进 `ControllerHub.ALL`。
  `On15010` 用 `NetReader.ReadArray(ReadGoods)` 逐项读 goods_list,字段名/顺序/3 嵌套数组(addition_attrlist/equip_extra_attr/
  awake_list)照抄 `ClientProtocol.json "15010"`,按 pos 区分仅 bag 落 `BagModel` → 发 `EVT_BAG_UPDATE`。`Proto.GOODS_CONTAINER_INFO=15010`。
- **`BagItemRenderer` 复用 `BaseAwardItem` View**:`_item` 改 `BaseAwardItem`(非 Bind),`SetData` 调 `_item.SetData(typeId,count)`
  显真实图标 + 品质底板 + 数量;`BagItemData` 加 `TypeId`。**RunCommand 实证**:内联 `_tpl_BaseAwardItem`(`bagItemRenderer/__Templates/BaseAwardItem`)
  = `common/BaseAwardItem.prefab` 的**嵌套实例**(第 6 轮已回填 View 组件)→ 克隆即得可用 View(无需回填工具/不点杀)。
- **`BagComponentView.OnShow` 用 `BagModel` 铺格**:克隆 `bagItemRenderer` 模板进 `bag_con.content`(127px 网格,viewport 宽算列数),
  `EVT_BAG_UPDATE` 重铺;无数据空铺(不造假背包)。渲染模板 `bagItemRenderer` 是 `BagModule` 顶层兄弟(非视图 Bind 字段)→
  由 **`BagFlow.ReparentFrom` 注入**(flow 已负责模块结构导航,避免业务视图反向 `transform.Find` 兄弟)。

**P2 BaseAwardItem 点击弹物品详情(对标老端 `UIToolTipMgr.AppendGoodsTips` → `GoodsTooltips`;真实 config,禁臆造)**:
- **`GoodsModel` 加 `intro`**:`config_goods` key **"2" = intro**(RunCommand 实证:`config_table_default.json` config_goods 下标 2 = intro;
  100150 庆典水晶/100152 福气鞭炮 实测有描述,装备 101011010 intro 空)。`GetGoodsIntro(typeId)`。
- **新增 `ItemTipsView`**(`Module/Core/Common/Views`,同 `TaskFinishView` TEMP 壳):真名(key1)+ 真实图标(key14)+ 品质底板
  (key18→com_goods_plate_{color},复用 BaseAwardItem)+ intro 描述(key2);Laya HTML(`<br/>`/`<font color>`)→ TMP 富文本(`<color>`)。
  `BaseAwardItem.OnClick` 默认分支(`_clickCb==null`)由 log 改为 `ItemTipsView.Show(_typeId)`。
- **`BaseAwardItem`/`BagItemRenderer` 幂等 `EnsureInit`**(框架要点):列表项被克隆后常**不经 `BaseView.Show`**(`OnInit` 不自动跑,
  `BaseView` 无 Awake),故由 `SetData` 兜底初始化(点击绑定 + 模板克隆就位)→ 修复**全部图标格的点击 tips**(完成弹层/背包同源,非点杀)。

**验证**(Unity RunCommand 编辑期渲染 + RenderTexture 截图;本会话 config 在编辑期已加载 `GoodsModel.IsLoaded=True`):
- **P1**(`Temp/shot_p1_bag.png`):克隆 `bagItemRenderer` + `SetData` 真实 type_id(初樱系列 101011010/102011010/103011010/104011010)
  → 2×2 真实物品图标 + 真实品质底板(com_goods_plate_1,color1)+ 末格数量角标「88」。render-path 真机渲染。
- **P2**(`Temp/shot_p2_tips.png`):`ItemTipsView.Show(100152 福气鞭炮)`→ 金色真名 + 真实图标(cdn 兜底导入 100152.png)+
  **紫色**品质底板(color4,兜底导入 com_goods_plate_4)+ intro 富文本(「春节运营活动」橙字 `<color=#ff9015>` + `<br/>` 换行)。
- 双编译 0 错(dotnet build + Unity 域重编);git 仅本轮 10 源文件,无 prefab/scene/config 改动(兜底图入 gitignored GameRes)。

**本轮 blocker(诚实声明,已收窄)**:
- **背包真实物品内容 = 活服回 15010 实包**:本会话无登录会话/无活服,无法收真满包 → 真实「你的背包有哪些物品」拿不到。
  但**协议码/Model/解析/渲染全链已就绪**(BagController 进游戏即发 15010、On15010 解析落 BagModel、BagComponentView 据此铺格);
  活服联调即显真背包。本轮以 render-path 真机渲染(真实 config 驱动单元格)替代,**不造假背包/不臆造字段**(红线)。
- 未喂真实 15010 字节单测:无活服抓包样本,造字节即造假(红线禁),故解析器以「格式串照抄 ClientProtocol.json + 编译 + ReadArray 范式」保证,待真包/活服回归。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第9轮.md`。

## 主线竖切 第 9 轮(2026-06-21):物品 tips 内容补全(数量/类型/来源)+ 装备分支起步(基础属性)+ 背包格→点击→tips 端到端联动

第 8 轮 tips 仅「名/图标/品质/描述」。本轮把 `ItemTipsView` 补到接近老端 `GoodsTooltips`,并按 `type==10` 起**装备基础属性**分支(对标 `EquipToolTips`),
再用编辑期真机渲染演示**真实 config 物品格 → 真实点击 → tips** 的端到端联动(无活服、不造假 BagModel)。改动仅 6 个源文件(无 prefab/scene/config 入库)。

**P0 保护基线**:worktree 干净;第 8 轮链路 rg 命中(`BagController`/`GOODS_CONTAINER_INFO`/`ItemTipsView`/`GetGoodsIntro`/`EnsureInit`);`dotnet build` 0 错(1 既有无关警告 MainRoleAgent.cs);未重做第 8 轮。

**配表同步(可再生成,GameRes 仍 gitignore)**:`ClientConfigSync` 加 3 表 → 跑「神霄/配表/同步客户端配置(JSON)」:
- 服务端 `GoodsType`(→ `goodstype.json`,type→type_name)、`config_equip_attr`(→ 阶/星/评分);客户端 `ConfigItemAttr`(→ `configitemattr.json`,attr_id→name)。
- 现经 ResManager **编辑期 AssetDatabase 兜底**加载(同既有 `config_goods`,均未进 Addressables 组)→ 活服 Play 路径需先跑「神霄/资源/Addressable 自动分组」(与 config_goods 同状态,留作 live 路径前置)。

**P2a 物品 tips 内容补全(对标 `GoodsTooltips` quantity_text/type_text/ways;真实 config 驱动,键以 `config_table_default.json` 实证)**:
- **`GoodsModel` 扩键**(权威序 config_goods 字段表):`"3"`=getway(来源)、`"9"`=type、`"10"`=subtype、`"13"`=equip_type、`"15"`=career_id、`"16"`=level、`"26"`=base_attrlist;
  `GoodsBasic` 一并带出;加 `GetGoodsTypeName`(GoodsType.type_name,对标 `WordManager.GetGoodsStyle`)、`GetAttrName`(ConfigItemAttr.name,对标 `GetProperties`)、
  `GetEquipPosName`/`GetCareerName`(硬编码数组,照抄 `WordManager.Equip_Pos_arr`/`GetCareerLimit`)、`GetGoodsGetway`、`IsEquip`(type==10)。
- **`ItemTipsView.Show(typeId, num)`**:正文加 **类型**(GoodsType 类型名)、**数量**(透传堆叠数)、**获取途径**(getway key"3";空 / 占位 `"[]"` 不显)三段;描述 intro 保留。
- **`BaseAwardItem`** 加 `_num`(SetData/SetCount 同步),`OnClick` 由 `Show(typeId)` 改 `Show(typeId,_num)` → 数量随格子真实透传(对标 `UIToolTipMgr.DefaultAppendTips`)。

**P2b 装备分支起步(对标 `EquipToolTips`:基础属性 + 部位/阶/星/等级/职业;真实 config,实例属性精确 blocker)**:
- **`ItemTipsView` 按 `type==10` 分流**:装备走 `AppendEquip` —— 部位(equip_type→`GetEquipPosName`)、阶/星(`config_equip_attr`)、等级需求(level key"16")、职业(career_id→`GetCareerName`)、
  **基础属性行**(base_attrlist key"26" 经 `ErlangParser` 解 `[{attr_id,val},...]` + `GetAttrName` 取真名,对标 `EquipToolTips.basePro`)+ 评分(base_rating>0 时);非装备走描述 intro。
- **`GoodsModel.GetBaseAttrs`**(Erlang 解析集中于 Model,缺属性名兜底标 `属性{id}` 不臆造)、`GetEquipAttr`(阶/星/评分)。
- **`BagGoods` 保留装备实例态**:`BagController.ReadGoods` 把跳读的 `addition_attrlist`/`equip_extra_attr`/`awake_list` 3 数组 + stren/rating/combat_power 由「读过即弃」改为「暂存」(地基);
  `On15010` 日志加 `equipWithInstAttr=N`。**实例「极品/强化」属性行**需活服实装备 + 实例透传到 tips → 本轮只显 config 基础属性,实例行精确 blocker(tips 内已标注,不画假属性)。

**验证**(Unity RunCommand 编辑期:async `EnsureLoaded` + 真机渲染 RenderTexture 截图;config 编辑期可加载 `IsLoaded=True`):
- **数据实证**(`Temp/tips_verify.txt`):typeName 10=装备/12=外观/14=宝石/42=结社/73=妖灵;attrName 1=攻击/2=生命/3=破甲;
  初樱轻剑 101011010 type=10 部位=武器 1阶 职业=剑士 baseAttr 攻击+50/破甲+20;四季轻剑(残)1010150420 4阶2星 攻击+1318/破甲+527;
  镇岳玄龟碎片 7302001 getway='用于激活妖灵';结社红包 4203004 getway='首杀活动'(`[]` 占位的装备 getway 已被抑制)。
- **P2 装备 tips**(`Temp/shot_p2_tips_equip.png`):初樱轻剑 金名 + 真图标 + 类型/数量 + 部位·阶·等级·职业 + 【基础属性】攻击+50/破甲+20 + 实例属性 blocker 注。
- **P2 普通 tips**(`Temp/shot_p2_tips_item.png`):结社红包 数量:5 + intro 富文本 + 获取途径:首杀活动。
- **P1 端到端联动**(`Temp/shot_p1_link.png` + `Temp/p1_link.txt`):一排真实 config 物品格(101011010/100152/4203004/7302001,渲染路径非 BagModel 假数据)→
  **真实 `Button.onClick.Invoke()`**(经 `BaseAwardItem.OnClick`)→ 福气鞭炮 tips 弹出,正文「数量:88」证明数量经真实点击透传;`tipsOpened=True`。
- 双编译 0 错(dotnet + Unity 域重编,console 0 Error);git 仅本轮 6 源文件,无 prefab/scene/config/meta 入库(配表/兜底图入 gitignored GameRes)。

**本轮 blocker(诚实声明,已收窄)**:
- **背包真实物品内容 + 装备实例属性 = 活服回 15010 实包**:本会话无登录会话/无活服 → 真实「背包有哪些物品」「实装备的极品/强化属性」拿不到。
  协议/Model/解析/渲染/点击→tips 全链已就绪(`BagController` 进游戏发 15010、`On15010` 解析落 `BagModel` 含 3 实例数组、`BagComponentView` 据此铺格、格点击经 `OnClick` 弹 tips);
  活服联调即显真背包 + 真实例属性。本轮以真实 config 物品格 + 真实点击的端到端联动替代,**不造假背包/不臆造字段/不画假属性**(红线)。
- **装备实例属性行未接显示**:`equip_extra_attr`(极品)/`stren`(强化)已在 `BagGoods` 暂存,但 tips 仅收 typeId(未透传 `BagGoods` 实例)→ 实例行待「点格透传 goods 实例 + 活服实装备」(第 10 轮)。
- **新配表 Addressables 未分组**:`GoodsType`/`ConfigItemAttr`/`config_equip_attr` 同既有 `config_goods` 走编辑期兜底,live Play 前需「Addressable 自动分组」(避免污染已入库 AddressableAssetSettings,本轮不顺手跑)。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第10轮.md`。

## 主线竖切 第 10 轮(2026-06-21):装备 tips config 极品预览 + 专有属性 + 实例透传地基 + 端到端联动(无活服)

第 9 轮 tips 装备分支落了「基础属性 + 部位/阶/星/等级/职业」,但极品 `recommend_attr`/专有 `other_attr` 仅在注释标 TODO、`BagGoods` 实例只暂存未透传。
本轮把装备 tips 推到**对标 `EquipToolTips.SetBestPro`(极品预览)+ `SetRedPro`(专有)**,并打通**实例透传地基**(点格带 `BagGoods` → `ItemTipsView.Show(BagGoods)` 重载)。改动 5 个源文件(无 prefab/scene/config 入库)。

**P0 保护基线**:worktree 干净;第 9 轮链路 rg 命中(`GetGoodsTypeName`/`GetBaseAttrs`/`GetEquipAttr`/`AppendEquip`/`EquipExtraAttr`);`dotnet build` 0 错(6 既有无关警告);Unity 域重编 console 0 Error;未重做第 9 轮。

**关键:RunCommand 先实证 key5/key6 真实格式(任务包红线「勿臆造」)**——读真实 `config_equip_attr.json`(2442 条 key5 / 1172 条 key6 非空)+ 老端 `EquipBestProItem.SetData`/`Util.GetAttrStr` 渲染源码:
- **`other_attr`(key6)= `[{attr_id,val},...]`**(同 base_attrlist 对偶)→ 专有属性。
- **`recommend_attr`(key5)= `[{100,{color,attr_id,v2,tmpl,v4}},...]`**(嵌套;外层 100=极品类型标记预览态忽略)→ 极品预览:内层 `inner[1]`=attr_id(名含 `{0}` 被 `inner[3]` 替换)、值=成长型(attr_id 300..307)取 `inner[4]` 否则 `inner[2]`。
- **值显示** 对标 `WordManager.ConvertToPercentValue`:`ConfigItemAttr[id].kind==2`(万分比)→ `val/100+"%"`;base_attrlist 显原值(老端不转)。

**P2 装备 tips config 极品/专有 + 实例透传地基**:
- **`GoodsModel` 新增**:`GetEquipRecommendAttrs`(key5 嵌套解析→预览行)、`GetEquipOtherAttrs`(key6→专有行)、`GetBestProNum`(color 3→1/4→2/5,6→3/7→4)、`FormatAttrValue`(kind 万分比)、`GetAttrKind`、`IsGrowthProType`;均走 `ErlangParser`(递归支持嵌套 tuple)+ `GetAttrName`。
- **`ItemTipsView.AppendEquip` 加两段**:`AppendBestPro`(有实例 `equip_extra_attr` 显真值,否则 config `recommend_attr` 预览「随机生成 N 条」)、`AppendOtherPro`(config `other_attr`「名:值」);仅 config 时标 blocker 注。TEMP 壳面板加高(560→820)+正文下边距(70→88)避正文压关闭按钮。
- **实例透传**:`ItemTipsView.Show(BagGoods)` 重载(typeId/数量取自实例;有 `ExtraAttrs`/`Stren` 显实例极品/强化行,缺则回落 config);`BagItemData` 加 `Goods` 字段、`BagItemRenderer.SetData` 给格点击接 `() => ItemTipsView.Show(data.Goods)`、`BagComponentView` 铺格透传真实 `vo`。

**工具修复(红线「工具有问题先修工具」)**:`ResManager.ReleaseInstance` 对 editor 兜底实例由恒 `Destroy` 改 `Application.isPlaying ? Destroy : DestroyImmediate`——edit mode(无 Play 的渲染/截图 harness)下 `Destroy` 抛 "may not be called from edit mode" 错;惠及所有编辑期 harness。

**验证**(Unity RunCommand 编辑期 async `EnsureLoaded` + RenderTexture 真机渲染;`IsLoaded=True`):
- **数据实证**(RunCommand 返回):晨曦轻剑 101015052 type=10 武器 5阶 lv100 剑士 bestProNum=3 → base 攻击+2550/破甲+1020、recommend [推荐]伤害加深+1.3%/攻击加成+1.7%/破甲加成+3.4%(`{100,{4,9,130}}`→9=伤害加深 130/100=1.3%)、other 攻击:880/破甲:352;沧溟轻剑 101015072 7阶 1.8%/2.4%/4.8%;九霄狂剑 101015092 9阶 2.3%/3.1%/6.1%——逐值对回真实 config。
- **P2 装备 tips**(`Temp/round10_equip_tips.png`):晨曦轻剑 金名 + 真图标 + 品质底板(color5 兜底导入)+ 类型/数量 + 部位·5阶2星·等级·职业 + 【基础属性】+ 【极品属性】(随机生成 3 条)[推荐]×3 + 【专有属性】×2 + blocker 注,关闭按钮在正文下不压字。
- **P1 端到端联动**(`Temp/round10_bag_click_tips.png`):真实 `BaseAwardItem` 格(晨曦轻剑 真图标+品质底板)+ 真实 `Button.onClick.Invoke()` → 经 `BagItemRenderer` 同款接线 `ItemTipsView.Show(BagGoods 实例重载)` → tips 弹出;断言 `cellBtn=True; tipsOpen=True; has极品=True; has专有=True`。
- 双编译 0 错;git 仅本轮 5 源文件,无 prefab/scene/config/meta 入库(配表/兜底图入 gitignored GameRes)。

**本轮 blocker(诚实声明,已收窄)**:
- **装备实例「极品 `equip_extra_attr`/强化 `stren`」真值 = 活服回 15010 实包**:实例透传链路已**全通**(`Show(BagGoods)` 重载 + 格点击带实例 + `AppendBestPro` 有实例走真值分支),只缺活服实装备的实例数组 → 现显 config 预览(真实 config,非假)。活服联调即显实例真值。
- **真背包物品内容**:同第 7~9 轮,无登录会话 → 真满包拿不到;协议/Model/渲染/点击→tips 全链就绪,以真实 config 格 + 真实点击端到端替代。
- **`recommend_attr` 颜色/`config_equip_stren_lv` 强化加值数值**:极品行颜色(老端 `ColorUtil.GetColorDark(inner[0])`)本轮用固定橙色(纯样式,非数据);强化加值需 `config_equip_stren_lv[equip_type@1]`(未载)→ 实例 stren 仅显等级,加值待补表。
- **新配表 Addressables 未分组**(同第 9 轮顺延):live Play 前需「Addressable 自动分组」。
- **P3 未触发**:P1/P2 未被卡 >15min,故货币图标真机截图(第 7~9 轮 P3 顺延)/数值红字/Addressables 分组 留第 11 轮。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第11轮.md`。
## 主线竖切 第 11 轮(2026-07-02):批处理 CLI 验证通道 + 使用物品 15050 全链 + 全项目重复 Bind 治理(1114 件)

本轮起改为**自调度循环**推进(用户授权:主线竖切延续、主线全程可推为终点、每轮验收过本地 commit)。
本会话无交互 Unity/无 MCP → 自建**批处理 CLI 渲染验证通道**,不再依赖 MCP RunCommand;顺藤摸出并治理了全项目性的重复 Bind 存量 bug。

**P0 基线**:基线 commit `0e605f97e`(login 重构清理收尾等 237 文件);第 10 轮锚点 rg 命中;dotnet 0 错(8 既有警告)。

**CLI 验证通道(工具,惠及后续所有轮)**:`Assets/Editor/CliVerify/CliVerify.cs` —— `Unity.exe -batchmode -executeMethod
Shenxiao.EditorTools.CliVerify.RenderAll -logFile Temp/x.log`(勿加 -nographics/-quit);断言行前缀 CLIVERIFY,截图落 Temp/,
进程码 0过/2超时/3断言败。async 由 EditorApplication.update 泵。
**坑与修**:batch 域 Addressables 操作永不完成 → `ResManager.KeyExists` 挂死(首跑 TIMEOUT 根因)→ 加
`ResManager.EditorPreferFallback`(编辑器专用:兜底 AssetDatabase 优先,不触 Addressables;交互编辑器/运行时默认 false 不受影响)。

**P2a 完成弹层货币图标(第 7~10 轮顺延的 render 实证,已验)**:真实任务 100520(采集1个物资,award=[{5,0,2500000},{3,0,10000},
{0,17020001,1},{0,17020004,1}])→ `Temp/round11_taskfinish_currency.png`:经验(icon 22,"250W" 缩写)/九洲灵钱(icon 31,"10000")
以**真实图标格**显示(非文本),TaskModule 真皮渲染。断言:格数==奖励数 && 每格图标已载 && 每格恰 1 个 EquipmentItem(防回归)。

**★全项目重复 Bind 治理(本轮最大意外收获)**:插桩发现每个奖励格 2 个 EquipmentItem 组件(8 comps/4 cells,instanceID 成对)——
根因=嵌套标准 prefab(EquipmentItem.prefab)自带 Bind,**旧版回填(嵌套跳过 guard 加入前)又在实例根上 AddComponent 了 added-override 重复件**
→ OnInit/BindClick 双跑,点击回调触发两次(全项目列表项/奖励格潜在双击 bug)。修(修工具不手改 prefab):
① `LayaBindFiller.EnsureBindOnWindow` 加同类型重复清理(保非 override 件)+ 嵌套实例升级守卫(不再 DestroyImmediate 嵌套件——历史重复正是它炸掉后 AddComponent 产生的);
② 新增全量清扫 `RemoveDuplicateBinds`(菜单 神霄/UI/清理重复 Bind 组件;CLI `LayaBindFiller.RemoveDuplicateBindsCli`)——
**清理 145 个 prefab、移除 1114 个重复组件**(Achv/Activity/Bag/Boss/… 全模块波及)。

**P2b 使用物品 15050 全链(协议+逻辑,对标老端逐行)**:
- 任务包原命题「tips 数量走 ConvertNum 万/亿」被老端源码**推翻**(GoodsTooltips.ts:503 数量行显原始数字)→ 不加,记录更正。真正缺口=使用/出售按钮区。
- `Proto.USE_GOODS=15050`(发 "li";回包 schema 逐字段照 ClientProtocol.json;服务端 pt_150 read/write 已核实存在)。
- `BagController.UseGoods`(_pendingUse 防重对标 goods_use_dic)+ `On15050`:res==1 →「使用成功」toast(type 35 冷却物不弹,对标)
  + `EVT_GOODS_USE_SUCCESS`(GlobalEvent 新增,对标 USE_BAG_GOODS_SUCCESS);礼包 type 32/33/84/35 → show_goods 逐项
  「获得X」toast(GetMappingTypeId 还原;CongratulationView+config_gift_box 未移植 → toast 降级,服务端真值不臆造)。
- `GoodsModel.GoodsBasic.Use`(config_goods key"22",权威列序核对 config_table_default.json)。
- `ItemTipsView` 使用按钮:仅背包实例 + use!=0 显示(对标 useBtn 隐藏条件);点击走 `UseBranchBlocker` 分支表
  (对标 CheckSecondView:type 34 礼包选择/37·2 经验符/38·6·10·36·39·42/75 藏宝图/59 装扮/83·1/14·12/22·1/37090001 直升丹
  → 专属界面未移植,明确提示**不发协议**,老端也不直发);默认分支 num<=1 直发 15050+Close,num>1 Confirm 先用 1 个(BatchUseView 未移植)。
- 渲染实证 `Temp/round11_itemtips_use.png`:V1体验卡(520100,真实 config)金名+真图标+品质底板+类型 VIP+数量 3+真实 intro+
  **使用/关闭双按钮**(使用按 config 显隐,关闭右移让位)。断言 useBtnVisible=True。

**P1 活服整合往返(诚实 blocker,第 12 轮条件许可再推)**:GM API(223.109.142.26:88)可达、服务端在线;但本会话无交互 Unity/无 MCP
(仅 Unity Hub 进程;3 个僵尸 relay_win 已清)。批处理 Play 未验,不冒进。

**验证**:dotnet build 0 错 ×4 次;批处理 CLIVERIFY RenderAll 终验退出码 0(两用例断言过,截图重新生成);git 本轮源文件 + 145 个 prefab(工具清理产物,非手改)。

**本轮 blocker(诚实声明)**:
- 活服往返:同上,缺交互 Unity/MCP。
- TipsManager.Toast 仍是 log-only 壳 → 使用成功/获得物品玩家不可见(第 12 轮 P3 视觉化)。
- 背包增量协议(15000/15008/15009)未接 → 用完物品背包/货币不刷新(第 12 轮 P1)。
- CongratulationView/config_gift_box、BatchUseView 未移植(降级路径已明示)。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第12轮.md`。

## 主线竖切 第 12 轮(2026-07-02):背包增量协议闭环(15017/15018/15008/15009)+ 合成包实证

**P0 基线**:worktree 干净;第 11 轮锚点 rg 命中;dotnet 0 错;无交互 Unity/MCP(活服往返 P2 继续 blocker)。

**★任务包纠错(红线「以老端源码为准」的实证)**:第 12 轮任务包原写「15000 单件物品推送」——读老端源码后推翻:
`On15000 → goodsModel.AddDynamic` 是**装备动态属性缓存**(dynamic_goods_dic_,洗炼评分等,供 GetDynamic/tips),不是背包内容;
真正的背包增量是 **15017(全字段)/15018(数量)**(`On15017/On15018 → UpdateBagGoods`:num<=0 删/已有 CopyGoodsVo 替换/新增 AddGoodsToBag)。
15008/15009 是**特殊积分**(special_score_dic_,currency_id→num;主货币金/铜走 13xxx)。

**P1 背包增量协议(对标老端逐行)**:
- `Proto`:GOODS_LIST_UPDATE=15017(pos:h + goods_list[u16×同 15010 单项 schema] → 复用 `BagController.ReadGoods`)、
  GOODS_NUM_UPDATE=15018({goods_id:l,goods_num:i,type_id:i})、SPECIAL_SCORE_UPDATE=15008、SPECIAL_SCORE_LIST=15009。
- `BagModel`:`Upsert`(对标 UpdateBagGoods:num<=0 删/有则整项替换/新增且 num>0 加)、`UpdateNum`(15018 最小面:仅改数量/删;
  不存在且 num>0 以最小字段兜底新建)、`SpecialScores` 字典 + `GetSpecialScore`(Clear 时一并清)。
- `BagController`:On15017/On15018(仅 pos==bag 落;equip 等其它 pos 按序读完跳过,老端 UpdateEquipGoods 未移植)
  → EVT_BAG_UPDATE;On15008/On15009 → EVT_SPECIAL_SCORE_UPDATE(GlobalEvent 新增)。
  15018 的 TRY_SHOW_ITEM_USE_VIEW(获得物品展示 flow)未移植,注释明示。
- 至此「使用物品」闭环:15050 使用 → 服务端推 15017/15018 → BagModel 增删改 → EVT_BAG_UPDATE → 背包格刷新(待活服实跑验证)。

**验证(CLI 合成包实证,新增 `CliVerify.ProtoDelta` 已入 RenderAll)**:按 ClientProtocol.json 手工组**大端**合成包 →
反射调 BagController 私有 handler → 断言:15017 新增(cell=7/num=5 落位)、15018 改数量(5→2)、15018 num=0 删除、
15008 单积分(1001→777)、15009 全量重建(清旧建 2 条)、15017 非背包 pos 跳过 —— 六项全 True。
两渲染用例回归通过(货币图标格 4/4 + tips 使用按钮),CLIVERIFY EXIT 0。dotnet 0 错。

**本轮 blocker(诚实声明)**:活服往返(无交互 Unity/MCP);Toast 仍 log-only(第 13 轮 P1);
CongratulationView/config_gift_box、BatchUseView、SellView(15021 协议未备)未移植。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第13轮.md`。

## 主线竖切 第 13 轮(2026-07-02):Toast 视觉化(玩家可见反馈闭环)+ 出售 15021 协议备货

**P0 基线**:worktree 干净;第 12 轮锚点在;dotnet 0 错;无交互 Unity/MCP(活服往返继续 blocker)。

**P1 Toast 视觉化(对标老端 sysInfo 链:Message.show → APPEND_MSG → SysInfoType.MINI → SysInfoMiniMgr 滚动条)**:
- `TipsManager`(Common 层,已依赖 Framework)从 log-only 升级:Toast/Float → `UILayer.Tip` 层浮动文字条,
  多条向上顶推(LineGap 46px)、同屏上限 5(超出顶掉最旧)、~2.2s 上浮渐隐(后 40% 淡出);
  UI 层未 Init(headless/启动早期)自动退回 log-only,且始终写 GameLog(供 CLI 断言)。
- 字体复用场景已开文本的 TMP 字体(同 ItemTipsView.ApplyFont 约定);编辑期 deltaTime==0 按 tick 兜底、
  编辑期销毁走 DestroyImmediate(同 ResManager 修法)。样式从简(红线:逻辑代码不精修样式)。
- **Confirm 仍是 Phase 0 壳(log + 直接 onYes)**——老端 Alert.Show 双按钮未移植;tips「批量使用先用 1 个」
  的 Confirm 因此当前=自动确认,已在注释/文档明示(候选下轮)。
- 至此第 11 轮使用物品链的「使用成功」「获得X」toast 玩家可见。

**P3 出售 15021 协议备货(对标 OnSellGoodsHandler/On15021)**:
- `Proto.SELL_GOODS=15021`;`BagController.SellGoods(list)`(动态 fmt "h"+n×"li",对标 WriteBegin+WriteFMT 循环);
  `On15021`(res:i + type_id_list[u16×{type_id:i,num:i}] 按序读完;res==1「出售成功」toast,
  失败显码降级——老端 Util.ErrorCodeShow 错误码表未移植,候选后轮)。
- SellView 未移植 → 无 UI 入口(老端出售按钮开 SellView 选量,不直发),纯协议层备货;数量变化由 15018 刷新。

**验证**:dotnet 0 错;CLI RenderAll 四用例全绿(EXIT 0):protoDelta(新增 15021 回包读序烟测)+
货币图标格 4/4 回归 + tips 使用按钮回归 + **toast 用例**(2 条入场 live=2/textOk → 生命周期后 expiredRemain=0),
截图 `Temp/round13_toast.png`(「使用成功」/「获得V1体验卡x1」纵排,CJK 正常)。

**本轮 blocker(诚实声明)**:活服往返(无交互 Unity/MCP);Confirm 视觉确认框未移植(自动 onYes 语义);
错误码表未移植(失败显码);CongratulationView/config_gift_box、BatchUseView、SellView 未移植。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第14轮.md`(回归主线本体:DoTask 类型覆盖)。

## 主线竖切 第 14 轮(2026-07-02):主线 DoTask 全类型覆盖 + 主线卡点路线图(24 系统按链序)

**P0 基线**:worktree 干净;dotnet 0 错;无交互 Unity/MCP(活服往返继续 blocker)。

**★侦察(本轮核心产出之一):tips_type 权威来源与主线分布**
- `task_tips_type` 不在 config_task(35 列无此字段),由服务端按任务步骤下发:`pt_300.erl write(30001)` →
  `pack_task_tip(#task_content.ctype)`;步骤模板在 **`data_task.erl get_content(TaskId,Stage,Cid)`**
  (data_task.content=[{Stage,Cid}] 只是索引;玩家实例存 MySQL task_bag_content)。
- 主线(type=1)543 任务/606 显式步骤 + 261 纯对话任务的 ctype 全量统计:骨干=Kill(1)×79/Collect(4)×45/
  PassMainDungeon(10)×52(已接)+ **LV(27)×57、FinDunType(9)×9 未接** + 22 个低频未接类型(14/18/23/24/25/31/33/35/41/48/50/54/57/63/73/81/84/89/90/91/92/93)。
- **`Docs/主线卡点路线图.md`(新)**:沿 prev/next 链自 100010 走 543 任务,列出 24 个未移植系统的首个卡点:
  链序 #20 同修(100190)→ #34 坐骑(100330)→ #42 套装收集 → #46 等级礼包 → #64 功能开启 → #72 翼影 → #80 全身强化 → …
  这即后续轮次的攻坚顺序(此类步骤条件依赖系统玩法,系统未移植新号会真实卡住)。

**P1 DoTask 全类型覆盖(TaskModel)**:
- 常量 TIP_LV=27/TIP_WELCOME=37 + `UNPORTED_TIP_SYSTEM` 26 系统映射(每项注老端枚举名/主线频次/老端 case 行为出处)。
- DoTask 结构:对话/完成/主线副本(已有)→ Welcome no-op(对标老端空 case)→ **结构化降级**(先于通用寻路——
  老端这些 case 不走坐标:LV 开升级提醒、FinDunType 开副本入口)→ 坐标寻路 → 未知类型兜底 Warn。
- `DoDegradeTask`:真实任务文案(config tips/name)+ 系统名 toast + 精确 blocker 日志——玩家不落静默死路,
  等对应系统移植后逐个替换真实入口。此类任务多为服务端条件自动完成,降级不阻断数据推进。

**验证**:dotnet 0 错;CLI 新增 `DoTaskCoverage`(真实主线任务 id + 服务端权威 tipsType → 断言分支日志:
LV/FinDunType/TrainMount 降级、Welcome no-op、未知类型兜底,5/5 True);RenderAll 六用例 EXIT 0(其余回归无损)。

**本轮 blocker(诚实声明)**:活服往返(无交互 Unity/MCP);降级映射的 26 个系统本体未移植(按路线图逐轮攻坚,
第 15 轮起:同修 → 坐骑 → 套装收集 → …);LV 类的 UpAlertView、FinDunType 的 DungeonEnterView 未移植。

**下一步价值最高**:见 `Docs/Claude任务包-主线竖切-第15轮.md`(P1=链序第一闸「剑魄同修」最小闭环)。
