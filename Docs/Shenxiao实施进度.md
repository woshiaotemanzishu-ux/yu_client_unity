# Shenxiao 实施进度

> 实时更新。每完成一项 / 调整范围 / 新增需求都在这里登记。
> 关联文档：
> - [整体方案](Shenxiao重构实施方案.md)
> - [编码规范](Shenxiao编码规范.md)
> - [Copilot 红线](../.github/copilot-instructions.md)

**最近更新**：2026-07-28

**状态图例**：
- ✅ 已完成
- 🟡 进行中
- 🔵 已规划，未开始
- 🟠 需求变更/范围调整
- ⛔ 已废弃/暂缓

## 当前协议迁移口径（2026-07-28）

- 最新裁决轮次：R219，40507 的铭文卸下 C2S 已迁移，但专属 S2C writer 无调用点，成功/失败分别由40502/40500承载，故保持C2S-only现状；最新有效实现仍为R217的 GuildActivity 40230。
- Unity ProtocolCoverage：`registered=1022`，`liveDefined=1468`，`liveGap=568`，未注册族错误出口15个；真实活协议覆盖 `900/1468=61.3%`，A～E 治理断言全通过。
- 逐轮证据、边界和下一候选以[自动循环协议与逻辑接入工单](工单-自动循环-协议与逻辑接入-20260711.md)为准；覆盖 baseline 不按单轮追写。

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
| 1.7 | 文档总入口与持续沉淀规则 | ✅ | [README.md](README.md) | 2026-07-27：技术文档、经验文档、进度与 AI 规则同轮维护 |

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
| 2026-07-27 | 规范 | 建立 `Docs/README.md` 文档总入口；AGENTS 与编码规范增加强制沉淀规则，要求架构、公共组件、工具/资源流水线、关键链路和复用型疑难修复在同一提交更新技术/经验文档。|

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
② 新增全量清扫 `RemoveDuplicateBinds`(CLI `LayaBindFiller.RemoveDuplicateBindsCli`;存量清完后已撤掉一次性菜单入口)——
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

## 主线竖切 第 16~17 轮(2026-07-02):大脑+Sonnet子代理模式落地——同修验收+三系统并行移植(套装/礼包/OutWard)

**模式切换(用户指示)**:主模型只做 工单/审查/共享接线/串行验证/提交;确定性工作派 Sonnet 子代理并行
(侦察=只读 Explore,实现=general-purpose+worktree 隔离);见记忆 orchestrator-loop-mode。

**第 16 轮**:①实现代理收尾同修(PartnerCase 用例+3 配表,5e10f09d2,七用例全绿,round15_partner_shell.png:
「星御·璇玑」1阶2星+培养按钮+失败toast同框);②侦察代理×2 产出 坐骑/套装+礼包 工单级报告
(**重大修正:100521(链序#57)=剑魄同修等级线(OutWard type_id=2)非坐骑;OutWard 协议按 type_id 参数化,
一套解 100330/100521/100901 三卡点**);③共享准备(f8f656486):Proto/GlobalEvent 预置+CliVerify 工具 public
+SYNC_LIST 扩 9 表+工单×2;④第三份侦察(天命觉醒/强化/古宝)→ 工单-天命觉醒与强化与古宝.md
(**修正:ctype81「功能开启」=天命觉醒 pt_429 专属,与 FunctionOpenController(pt_138)无关**)。

**第 17 轮(三实现代理并行,worktree 隔离,git core.longpaths 修复了长路径)**:
- 套装收集(99ba9ad84):15256/15257 + SuitCollectModel/Controller/ShellView + config_suit_clt×2 + 用例。
- 冲级豪礼(7a134bfd4):41700/41701(rewards=ObjectList 经 GetMappingTypeId toast)+ RushGift 三件套 +
  领取成功且主线==100420 自动关壳(对标 LevelRewardItem.ts:106)+ config_rush_giftbag + 用例。
- OutWard(1b86a1a22):16002/16023/16028/16029(阶星+等级双线,type_id 参数化)+ OutWard 三件套 +
  6 张 config_mount_* + 用例(八断言)。
- 主控:cherry-pick 三分支(唯一冲突=Cases.meta 文件夹 GUID,保先入)→ 接线(ControllerHub+3、
  DoTask 新增 TIP_TRAIN_MOUNT/TIP_AWARD_LV_GIFT/TIP_SUIT_CLT/TIP_MOUNT_LEVEL 分支并从降级表移除 23/54/84/90、
  RenderAll 挂 3 用例)→ csproj 补 12 行 → DoTaskCoverage 期望更新(tips23 → OutWardShellView)。
- **验证:十用例全绿 EXIT 0**(protoDelta/dotask/partner/suitclt/rushgift/outward/taskfinish/itemtips/toast);
  三张壳截图:round16_suitclt_shell.png / round16_rushgift_shell.png / round16_outward_shell.png。

**主线卡点路线图进展**:已解 #20 同修(100190)/#34 坐骑(100330)/#42 套装(100391)/#46 礼包(100420)/
#57 同修等级(100521)+100901;剩余最近卡点:#64 天命觉醒(100590)/#72 翼影(100665,OutWard type3 候选)/
#80 全身强化(100720)/#92 古宝(100811)——前三已有工单,第三波实现代理在派。

**blocker(诚实)**:活服整合往返(全部壳的真实往返仍需交互 Unity+MCP);翼影(24)是否 OutWard type3 待侦察确认;
壳均为 TEMP(用户后续重做 UI);错误码表未移植(显码降级)。

## 主线竖切 第 18 轮(2026-07-02):天命觉醒+装备强化+古宝 三系统并行,十二用例全绿

- 三实现代理(worktree):天命觉醒(f984ce3c5,42900/42909;⚠ctype81「功能开启」=TempleAwaken 专属,
  与 FunctionOpenController pt_138 无关)、装备强化(c3ddc1261,15204/15205+EquipStrenView 按钮接真调用+3 配表)、
  古宝(1c70346c0,13320/13321,soap 10001 幽瞳 2 碎片)。
- 侦察代理:副本入口(61001/61013/61020,御魂本 type12=100980/101522;澄清 AutoBrush 133xx 与 61xxx 是两套)、
  灵魄镶嵌(16700/16701,孔位1无条件)、结社加入(40004 建社=空服最短路径)→ 工单-副本灵魄结社.md。
- 主控接线:Hub+3、DoTask ctype8/31→EquipFlow.OpenSub("EquipStrenView")、81→TempleAwakenShellView、
  89→GuBaoShellView(降级表移除 8/31/81/89)、RenderAll 12 用例、csproj+11、第 19 波共享常量预置
  (61001/61013/61020、16700/16701、40001/40003/40004/30008 + 事件 + config_dungeon 入 SYNC_LIST)。
- **验证:12 用例全绿 EXIT 0**(新增 templeawaken/equipstren/gubao;含 round17_templeawaken_shell.png/round17_gubao_shell.png)。
- 路线图已解:#20/#34/#42/#46/#57/#64/#80/#92 + ctype8;第 19 波=结社(#123)/灵魄(#113)/副本(#112)实现中。

## 主线竖切 第 19 轮(2026-07-02):结社+灵魄+御魂本副本壳,十五用例全绿

- 三实现代理(worktree):结社(40001/40003/40004+30008 补触发,空服建社最短路径)、灵魄(16700/16701,
  rune_bag pos=11 经 BagController 8 行例外分支转存 RuneModel)、御魂本副本壳(61001/61003/61020;
  **子侦察实证 61003 才是通用结算,61013 老端未实装仅类型声明;61001 成功=error_code==1**)。
- 侦察代理(薄增量六件套):翼影24/圣器92/神兵41=OutWard 16005 通用升星一条解三闸(回包=16023 少 etime/auto_buy);
  宝石48=15208/09;挂机91=13216 一发(唯一事件计数型);橙装93=15201 穿戴;熔炼18=15024/25(BagSmeltView 壳已有);
  **⚠大妖63 服务端预存缺口:lib_special_boss_mod.erl:516 计数 guard 只认 BossId∈{402,403},任务 101200 需要
  3400001 → 正常击杀永不计数,需人工改服务端**。→ 工单-薄增量六件套.md。
- 主控接线:Hub+3、DoTask ctype9/57→御魂本壳、14→结社壳、33→灵魄壳(降级表移除)、RenderAll 15 用例、
  DoTaskCoverage 期望更新(tips9→DungeonRuneShellView)、csproj+12。
- **验证:15 用例全绿 EXIT 0**。路线图已解:#20/#34/#42/#46/#57/#64/#80/#92/#112/#113/#123 + ctype8。
- blocker:大妖63(服务端);副本真实进出需活服;各壳 TEMP 待用户重做 UI。

## 主线竖切 第 20 轮(2026-07-02):薄增量六件套 + 主线 ctype 全集复核,十六用例全绿

- 实现代理(worktree):OutWard 16005 通用升星(一条解 翼影24/圣器92/神兵41,壳加 type3/4/5 三行)、
  宝石 15208/09、挂机 13216(真实 schema:errcode/old_lv/old_lv_ratio/goods_list,壳+领取按钮)、
  穿戴 15201(ItemTipsView 加[穿戴]按钮三按钮布局)、熔炼 15024/25(BagSmeltView 无选择列表 → useBtn 不接线,诚实标注)。
- 侦察代理(收尾):灵魄强化 16702(判定无缺口)/神装合成=通用合成 15020 type=2 规则/排位赛 28001-28003
  (**⚠服务端断链 #2:?JJC_USE_NUM increment 被注释 mod_jjc_cast.erl:87,任务 101465 无法自然完成,需人工恢复**)/
  LV27 决策=UpAlertView 为独立活动导览不做,维持降级 toast。
- **主线链复核(权威):type=1 共 543 任务、链 100010→104560 无分叉无遗漏;ctype 全集 27 项全部盘点覆盖**。
- 接线:DoTask 24/92/41→OutWard 壳、91→挂机壳(降级表移除;35 标注服务端断链);RenderAll 16 用例;csproj+6。
- **验证:16 用例全绿 EXIT 0**。工单-收尾三件套.md 已备(16702/15020/排位壳=主线 ctype 收官)。
- **服务端工单(需人工)**:①大妖63 计数 guard(lib_special_boss_mod.erl:516,只认 402/403);
  ②排位35 计数 increment(mod_jjc_cast.erl:87 被注释)。

## 主线竖切 第 21 轮(2026-07-02):收尾三件套——主线 543 任务 27 种 ctype 客户端侧收官,十七用例全绿

- 实现代理:灵魄强化 16702(壳加[强化]按钮,成功刷 16700)、神装合成 15020(config_goods_compose 18 列入库,
  type==2 规则壳;诚实缺口:954 条 type2 规则中 666 条走 irregular_mat 孔位池未接匹配,显式标注不假发包)、
  排位赛壳 28001/02/03(figure 嵌套块复用现成 FigureProto.Read 全量读;壳顶固定警示服务端计数断链)。
- 接线:Hub 补 9 控制器(含第 20 轮薄增量 4 个)、DoTask 50→灵魄壳/73→合成壳/35→排位壳(降级表清空至仅剩
  11 FinDungeon 主线未用项)、RenderAll 17 用例、csproj+7。
- **验证:17 用例全绿 EXIT 0。**
- **收官清单(主线链 100010→104560,543 任务,27 种 ctype)**:
  · 已接真实入口/壳 24 种:1,4,9,10(战斗采集副本)5,6,7(对话)8,31(强化)14(结社)18(熔炼备)23,90(坐骑)24,92,41(16005)
    25(同修)27(LV 提示型)33,50(灵魄)35(排位壳)37(欢迎)48(宝石)54(礼包)57(副本层)73(合成)81(觉醒)84(套装)89(古宝)91(挂机)93(穿戴)
  · 主线未用:11(FinDungeon,保降级)
  · **服务端断链 ×2(需人工)**:63 大妖(lib_special_boss_mod.erl:516 guard 只认 402/403)、
    35 排位(mod_jjc_cast.erl:87 ?JJC_USE_NUM increment 被注释)——客户端壳已备,服务端修复即通。
- **下一阶段=活服整合实跑验证**(登录→主线逐任务推进→各壳真实往返),需交互 Unity+MCP 或批处理 Play 实验。

## 活服整合阶段 第 1~2 轮(2026-07-03):PlaySmoke 批处理活服冒烟通道打通 + 主线实跑首证

- **PlaySmoke 通道**(Assets/Editor/CliVerify/PlaySmoke.cs + LoginBootstrap -shenxiaoPlaySmoke 开关/0角色自动创角):
  批处理真正 EnterPlaymode 连活服;首跑实证 domain reload 会吞 playModeStateChanged 静态订阅
  (RuntimeUiCaptureTool 范式不成立)→ 修=SessionState+[InitializeOnLoadMethod] 自愈重挂(b35b80239)。
  复跑:五门闩(登录链全通/进入游戏/GAME_START/12002/30000)全 OK,EXIT 0,6 分钟自动退出。
  跑法:Unity.exe -batchmode -executeMethod Shenxiao.EditorTools.PlaySmoke.Run -shenxiaoPlaySmoke -logFile。
- **★主线活服实跑首证(首跑 14h 日志取证)**:登录→创角→进游戏→主线 100030→100160 共 16 任务
  全自动推进(FindNextAutoFightTask→DoTask 寻路→打怪→30004 提交→下一个,真实服务器闭环)。
- **新卡点=100170 主线副本(tipsType10)**:13305 进副本成功,副本内打不过 → 13306 pass failed nextLevel=2
  → 61002 退出 → 无限重试。下一攻坚=副本内战斗链(伤害/技能/怪物击杀)。

## 活服整合阶段 第 3~6 轮(2026-07-03):100170 主线副本攻坚(五层修复)+ 自动穿戴——战力 1760→8170,副本通关

「实跑→定位→修复→重跑」逐层剥洋葱(每层都有活服日志实证):
1. 进副本前武装 AutoFightModel(老端靠全局挂机常开的隐含状态,Unity 自动链没搬)——d59c0986f
2. 登录冷启动点火(半途任务登录后无 30001 增量,两个续跑入口都不触发→首个 30000 后 kickoff)
3. 点火等 MainRoleAgent 就绪(30000 早于主角渲染,过早点火被 no MainRoleAgent 早退)
4. 攻击节奏对齐老端(僵直门禁 skill_rigidity/100ms tick/400ms 接近节流;代理侦察:老端 20s 理论 20-24 次 vs Unity 8 次)——55a3e18d3
5. **自动穿戴**(根因:主线奖励装备躺背包,裸装战力 1760 vs 副本推荐 15000):侦察实证老端=一键穿戴按钮
   (GetStrongestEquips:职业/部位/等级过滤+唯一 rating 比较,空槽直接穿/严格大于才换;15201 只发 goods_id);
   Unity=EquipAutoWear 自动任务模式代行 + 装备通道(15010 pos=1)转存比较 + EVT_BAG_UPDATE 防抖触发。
**playsmoke7 实证:auto-wear 逐件穿 6 件,战力 1760→8170;maxFinishedTask=100170(副本通关!),lastDoTask=100180 继续推进。**
教训沉淀:playsmoke1.log(14h 取证)被 Unity 重启清掉——取证日志放 Logs/ 不放 Temp/(Unity 管辖)。

## 通用飘字提示 TipToast(2026-07-06):prefab 化 + 登录链接入

- 对标老端 sysInfo 链(Message.show → SysInfoMiniMgr → MessageItem Type One):底图 mainui_ui_45.png
  (222×26 九宫格 10,10,10,10)从 yu_client 拷入 GameRes/resource/game/sysInfo/texture/;
  动画=缩放0.3→1+淡入0.3s → 停2s(新条来时旧条上顶30px) → 直接消失;同刻仅一条 Born,排队缓存200。
- 新增:TipToastCreator(面板 Common/TipToast,生成 Prefabs/UI/Common/TipToastView.prefab,样式手调)、
  TipToastView/TipToastItem(动画参数在组件上可调);TipsManager.Toast 改为首选 prefab 版,
  prefab 缺失降级代码建树、UI 层未起 log-only(无头断言不受影响)。
- 登录链补接入:LoginFlow(TipsToLoginPage TODO 落地 + 恭喜登录/注册成功)、RoleCreateView(创角错误码
  文案对标老端)、RoleSelectView(未选角色)、LoginController(10007 名字验证/10004 失败/59004 踢线)、
  LoginPanelView(请输入账号密码 + 预览兜底)。dotnet 编译 0 错误;prefab 待「重构UI 生成器」生成后手调样式。
- **二轮修正(同日,用户实测反馈)**:①「弹出后不动、不消失」根因=调样式时模板节点激活态存盘→静态常驻,
  修=OnInit 强制隐藏模板;动画改为 Born 弹出后持续上浮(riseSpeed 45px/s)+到寿淡出(fadeOutDuration 0.35s),
  参数字段更名使已手调 prefab 无需重生成即吃新默认值。②进游戏窗口缺「走出/进入安全区」:老端=客户端自查
  (MainRole.CheckIsCrossSafeArea:场景表 subtype==1 整场安全不飘 + 地图格 SafeType=4 位),Unity 数据现成
  (WalkGrid 即同一区域字节)→ SceneMapData.IsSafePixel + MainUIConfigs.SceneCfg.Subtype +
  MainRoleAgent 每帧 CheckCrossSafeArea(静态 state 跨场景不复位,翻转才飘)。老端该窗口无其它无条件飘字。

## 自动循环 轮1(2026-07-11 夜间无人值守):Goods 协议扩容 18 号,二十用例全绿

- 无人值守自动循环启动(总控 Docs/工单-自动循环-协议与逻辑接入-20260711.md;全量差距分析 Docs/差距分析-协议接入GapMap-20260711.md,四路侦察交叉:老端 2371 cmd vs Unity Proto.cs 169 常量≈7% 覆盖,12 个带验证配方的工作包排队)。
- **Goods 18 号落地**(04b95d848):15000/01 详情(GoodsDynamicModel 独立缓存+3s 节流+ItemTips 详情段 epoch 防竞态)、15002 扩容、15003 移位、15019 分解、15022/15026 通用兑换通道、15027 过期物品、15053 拾取三态、15055 buff、15083/84 礼包动态(84 补齐老端已断链路)、15086 自选(slot=1字节c)、15087 兑换码(5s 中央CD+结果异步推)、15089 预览战力(4字节typeId)+纯推送 15030(重拉15010)/15088(掉落序)/15090(自动分解toast)。跳过:15004-06 服务端死号、15085 老端零引用、15023 服务端已阉割。BagModel 补 35 个 pos 枚举+per-pos 容量。
- **存量修复 ×2**:①OutWard 视图订阅泄漏自愈守卫(f2be5e2db)——OnInit 订阅但未激活的实例整树销毁不走 OnDestroy,残留订阅炸 MissingReference;②toast 用例适配 TipToast prefab 化——prefab 版 MonoBehaviour.Update 驱动在编辑期 batchmode 不 tick(卡 Born),用例强制走代码兜底路径。教训:Update 驱动的行为无头断言必须走非 Update 通道。
- **验证:RenderAll 二十用例全绿 EXIT 0**(新增 goodsproto 七断言,15000 全 schema 尾哨兵验字节游标)。
- 遗留 TODO:场景掉落实体绑定(15053/15088 只发事件)、自选礼包/兑换码/幻化 tooltip 的 UI 消费方、15027 倒计时确认后弹窗视觉残留。

## 自动循环 轮2(2026-07-11 夜间无人值守):战斗补全+死亡复活闭环,二十一用例全绿

- **死亡→复活链首次贯通**(4b75933c2):20013 死亡广播(击杀者名三级 fallback:MonsterVo.type_id→config_mon,老端根因修复完整保留)→ EVT_ROLE_DEAD → 停挂机+主角 death 动作 → MainUIReliveView(服控走 20009 时间戳/非服控本地 5s 倒计时到点自动请求)→ 20004 复活(15 种 flag 逐字文案;服务端白名单 19 值守门,BOSS/ASHES 成功回 12 也按成功走)→ EVT_RELIVE_SUCCESS 恢复挂机+idle。20017 复活疲劳、20022 模拟死亡、20005(log-only)、20007/14/15/18/27/28 推送、20010/20/21/23 一并接入。
- **UiCreator 新范式**:MainUIReliveCreator 从 HudOverlayCombat 捆包抽取既有 BuildRelive 子树生成独立 prefab(几何经 .scene 公式独立复核一致),建立批处理入口 GenerateBatch(-executeMethod 可无头生成);Addressable 自动分组待用户回来跑(设置资产与既有暂存合流,未提交)。
- **协议纠偏**:老端 20005 处理体是死代码且其 ReadFmt 与服务端 pt_200 write 字节序不符——按服务端实序实现;20015=l,h(侦察稿笔误 l,i 已纠)。20003-20028 整段不在 ClientProtocol.json/proto*.d.ts,TS+pt_200.erl 是唯一 schema 源。
- **验证:ReliveCase 七断言绿 + RenderAll 二十一用例全绿 EXIT 0**。
- **并行会话卫生披露**:①轮1 提交 Proto.cs 时无意混入前会话在途的活动图标波次常量(~25 条,内容正确、编译+回归全绿,无害但需知情);②ControllerHub/MainUIFlow 在基线即脏(25 控制器注册/FirstPass 绑定清单),轮2 采用「回 HEAD→只重放本轮增量→提交→放回工作区」的 hunk 级拆分,基线残留保持未提交归还原会话。

## 自动循环 轮3(2026-07-11 夜间无人值守):技能成长线收官(3a+3b),二十三用例全绿

- **裁决先行**:GapMap #3 包原清单大半是死功能——21003-05 技能强化双端死(老端读弃+服务端 handle 注释)、21101-04 远古奥术(老端视图缺失/模型注释+服务端 @deprecated),全部砍掉且启动不再空发 21101。
- **3a(84eb195ce)**:21001 被动技能升级(ErlangParser 正解 condition,不复刻老端 .goods 直取死代码)+天赋 21010/11/12(SkillTalentModel)+13008/10 快捷栏+12093/18401 推送+20006 辅助技能(AssistVo 双端交叉核对;两段式表现语义)+20018/27 CD 广播接进 SkillManager(补 HUD 轮询脱节)+角色面板主动/被动技能两页签。
- **3b(232fe7fec)天赋页真 UI**:发现烤入管线早把天赋六件套烤进 RoleModule.prefab 当死重模板——Creator 走外科装配:提升+m_Script 替换法(Bind→业务子类,保序列化引用/fileID)+修复式幂等+贴图纠偏;RoleFlow 第 8 页签「天赋」带 4 转门控(BaseWindowSkinView 新 OpenCheck/LockedToast)。
- **三个可复用的坑**:①Bind 子组件是 BaseView,父视图必须先 Show() 触发 EnsureBound/OnInit,否则模板捕获静默为空;②config_skill 的 condition 空时是 JSON 数组、非空才是 Erlang 串,Value<string> 会对 JArray 抛 InvalidCast;③烤入管线不保证 ScrollRect.content 接线,视图查找要留根兜底。
- **验证:InnateViewCase(装配 prefab 实例化渲染,技能树 10/10 条目+截图)+RenderAll 二十三用例全绿 EXIT 0**。

## 自动循环 轮4a(2026-07-11 夜间无人值守):装备补全一段(精炼/洗魄/神炼/宗师),二十四用例全绿

- **b284495d9**:精炼 15250/51、洗魄 15212/13/14/52(15213 手写锁定槽序列 c+h+c[]+c,index+1)、神屠九炼 15255(refinement_lv 复用 15000 详情)、淬炉宗师 15260/61;GoodsDynamicModel 补 Invalidate/Patch 双口子;EquipConfigs 最小配置层(缺表降级)。跳过 15202/06/07(老端无入口)与 15242/43/15253(服务端 DEAD);宝石全套=4b;神装/共鸣套装/铸灵护灵觉醒唤魔归后续包。
- 侦察修正 GapMap:该包原清单混入大量独立系统(神装在合成窗、共鸣套装独立窗、觉醒唤魔另有归属),已按老端窗口归属重新切包。
- ControllerHub 并行会话残留仍未提交,继续用「回 HEAD 重放增量」拆分法注册 3 个新控制器。
- **验证:EquipGrowthCase 绿 + RenderAll 二十四用例全绿 EXIT 0**。

## 自动循环 轮4b(2026-07-11 夜间无人值守):宝石(骸珀镶嵌)全套,二十五用例全绿

- **cde0c7d52**:15210/11/15/16 协议层(GAME_START 预拉 10 位、雕刻成功自动刷新、合成 oneKey 自循环)+Jewel tab2 七视图接入+镶嵌拆除接既有 EquipStoneController。
- **管线发现**:EquipJewelView 整树被烤进 CommonModule.prefab/__Templates 当死模板(与轮3 天赋同款行为)——JewelBindUpgrader 嫁接进 JewelModule 顶层后复用现成 LayaBindFiller.FillPrefab 回填管线升级 Bind→业务子类(比轮3 手写 m_Script 替换更该用的正道,已沉淀:**烤图缺组件先找 LayaBindFiller,别重写升级器**)。
- 服务端实证纠偏:15210 refine_lv=1 字节(侦察稿 h 有误)。配置缺口(config_equip_stone_inlay/lv/refine/refine_goods)如实降级留 TODO。
- **验证:JewelCase(协议+渲染双段)+RenderAll 二十五用例全绿 EXIT 0**。

## 自动循环 轮5(2026-07-11 夜间无人值守):角色面板+改名+转职,二十六用例全绿

- **8d483fd76(46文件+3985行)**:世界等级/他人Figure/托管/被动技能推送/经验飘字分支全表/转职冷却(绝对时间戳)/头像三件套/13088终身计数真存储;改名全链(服务端错误码实测纠老端硬编码假设);转职全链(道具分支解锁+等级预检+二次确认+TransferJobCreator 从老端 json 设计值从零建 prefab——比嫁接更早期的第三种 UI 供给形态)。
- **真 bug 修复**:12086 改名推送无自分流,自己改名后 RoleModel.Figure.Name 不更新——仿 On12074 补分流。
- 逐号裁决:13082(老端上传头像半成品,SettingUploadHeadView 整类被注释)/13084(GPS 无触发源)/13085(双端不存在)/13087(App 生命周期)跳过;13081 字段序按服务端 Res:32+Id:64(老端假设从未被真回包验证过)。
- **验证:RoleGrowthCase 八组断言+渲染段 + RenderAll 二十六用例全绿 EXIT 0**。

## 自动循环 轮6(2026-07-11 夜间无人值守):聊天补全,二十七用例全绿

- **d3eb0a7ec(+1225行)**:发言 11001 发送侧全频道+喇叭(range=receive_id 复用语义;消耗物 1102015065 预检)、私聊真通道 11002、11029 喇叭下行、小跨服开关/物品链接/消红点/私聊资料/黑名单清理/系统公告跑马灯(Renderer+Driver 拆分,绝对时间戳可无头断言)、被禁言 toast(改良)。
- **真 bug ×2**:①ChannelWorld=0/ChannelCamp=15 错值(chat.hrl 实为 1/18)→世界频道消息永远分桶失败(无异常纯静默);②私聊回包走 11002,此前常量都没有。
- **GapMap 勘误**:此包清单大半错位——"发言11005"实为死语音号,真发言=11001;语音族老端发送三层注释不可达全砍。
- **风险#7 正式拆除**:服务端 pt.erl 压缩分支整段注释、flag 硬编码 0、唯一显式 Zip=1 调用点也被上游吃掉——压缩帧在当前服务端不可能出现;后续 Rank/公会列表等大列表包无需 inflate,只需保留"若有人恢复该注释先补客户端"的前置检查项。
- **验证:ChatCase 十一组断言 + RenderAll 二十七用例全绿 EXIT 0**。

## 自动循环 轮7(2026-07-11 夜间无人值守):好友+邮件全量,二十八用例全绿

- **6cdcf8dbd(50文件+18140行)**:Friend 模块全量(三桶/推荐假人/搜索/申请/一键与单条处理/关系操作/右键菜单 800ms 节流缓存+五路推送+14099 兜底)、邮件真链(详情缓存优先/批删手写序只删无未领附件/批领背包护栏——老端 CheckEquipNum 实参作用域缺陷不复刻,等价改「至少1空位」)、19501 完整角色卡、私聊窗跨 prefab 活树嫁接(FriendBindUpgrader,比死树嫁接更简的第四形态)+消费轮6 ChatModel 私聊桶。
- 邮件状态机勘误:真链=19001→19002(读)→19005(领)→19003(删);GapMap 所写 19006 实为公会邮件发送。
- **验证:FriendMailCase(好友/邮件/资料卡/渲染四段)+RenderAll 二十八用例全绿 EXIT 0**。

## 自动循环 轮8(2026-07-11 夜间无人值守):组队 Team 协议全量,二十九用例全绿

- **189ab3a2c(+2341行)**:TeamController 38 收+TeamModel 全状态机;桶1核心 25 号(24004/06/08 手写自定义序;24005 退队连锁重拉;跨服邀请 24057 分流)+桶2推送 14 号;被邀最小闭环走 Confirm 队列(大厅/邀请列表大 UI 记队列尾)。
- **纠侦察稿**:24008 每项编码=l(team_id)+c(agree)(报告写 h+l 有误),经 UserMsgAdapter Encode 往返实证。
- 砍:24011 委任队长(老端 UI 四层链路全断的僵尸:点头像注释/菜单空函数/PlayerMenuView 源文件不存在)/24042/桶4 十六未用/服务端 DEAD 五号。
- **卫生**:HUD 队伍三件与基线残留交织,本轮不入库(工作区全量验证渲染段通过);TeamCase 渲染段对未收编态优雅降级,保证已提交树门禁自洽。
- **验证:TeamCase(逻辑+渲染)+RenderAll 二十九用例全绿 EXIT 0**。

## 自动循环 轮9(2026-07-11 夜间无人值守):副本家族一期,三十用例全绿

- **0e066c634(+1716行)**:61004 双路(loading 白名单/补发/进场三连)、坐标事件状态机(61007/61019 流转与对账)、购买 61021(共享 vip_count 组+婚姻本专文案)、扫荡 61022(count 位宽 32≠61003 的 64)、鼓舞 61025/26、新资源本 61120/121、61020 触发时机补全(500ms 防抖 21 类型)、周本 50801/02 独立 PolarModel、DungeonBuyTimeView 壳接真。
- **归类大纠正**:GapMap"塔 61112-21"实为灵魄本奖励系统(61112-16)+真塔(61117/18);61031-41 是守卫公会本(归公会包);61010 真实序=iic(BaseDungeonController 的 ilc 是死分支)。
- 过程小坑:渲染断言全场景 FindObjectsByType 撞早前用例残留的烤制占位实例——改反射定位业务视图自有 _bind(沉淀:**渲染断言定位到被测实例,别全场景搜类型**)。
- 本轮实现代理曾因网络证书瞬断中止,SendMessage 断点续跑成功(实现未落盘无半成品)。
- **验证:DungeonFamilyCase 十组断言 + RenderAll 三十用例全绿 EXIT 0**。

## 2026-07-15：1213 / 1200 Art 挂点模板与一键导入闭环

- **Role 模板**：1213 的 `rhand` 从错误的腕骨原点修正为手掌权重网格实测
  `(-0.14,0.03,0.02)`；Art 新增每骨架一份 `role_mount_profile.json`，所有动作统一烘焙。
  未知新 `role_*` 缺 profile 直接失败，不再自动猜 0/0/1。1111/1300/1400 明确列为 1213 前旧资源兼容跳过。
- **Head / Weapon 模板**：Head 使用 `head_mount ↔ head_attach`；Weapon 使用
  `rhand ↔ weapon_attach`。1200 `weapon_attach` 对齐 FBX `Bone_wq_r` 的位置与 `Z=89.71°` 轴向；
  `AttachmentSocketAligner` 改为同时解算 locator 位置和旋转。
- **Art 总闸门**：新增「交付/检查全部模板与基准」。批处理实测 Role 7/0、Head 3/0、Weapon 2/0，
  `[DeliveryCheck] ... pass=True`，进程退出码 0。
- **Art 视觉预览**：新增 Role/Head/Weapon 拖入式装配预览和正背左右四方向截图；1213+Head1213+Weapon1200
  使用图形设备实测生成 4 张有效截图，locator 位置误差约 `0.0000014`、旋转误差 `0°`。
- **主工程导入闸门**：资产管理继续保留一次设置 Art 项目根目录、每模型手选并显示具体目录、点击即整夹替换；
  `ImportPart` 导入后新增最终 prefab 结构硬检查，失败时不自动写入 `model_replacement.json`。

## 2026-07-27：大妖入场演出 Missing Script 与第二只大妖折返修复

- **演出不播放根因已修**：`BossBornEffectPlayer` 从 `BossBornEffectFlow.cs` 拆为同名独立脚本，修复 Unity 按文件 GUID 解析到静态主类型后把 Prefab 组件保存成 Missing Script 的问题；`BossBornIntro.prefab` 已绑定播放器独立 GUID。
- **完整预览入口**：新增 `神霄/特效/播放完整 BossBornIntro`，Play Mode 主界面可一次播放遮罩与全部粒子，不再逐粒子预览；错误条件改为明确弹窗。
- **进场折返修复**：`12005` 立即同步 `MainRoleAgent` 的权威坐标并清理旧自动接近；大妖实体绑定本轮 Boss 实例，演出结束前先锁 Boss 再解冻；副本内主线大妖任务禁止回退选择普通怪，野外刷怪逻辑不变；怪物异步生成增加场景 epoch/实体引用闸门。
- **验证**：`dotnet build Shenxiao.Module.Core.csproj --no-restore` 与 `dotnet build Shenxiao.Editor.csproj --no-restore` 均 0 error；Prefab 的 `m_Script` 为 `BossBornEffectPlayer.cs.meta` 非零 GUID。活服完整演出与第二只大妖无折返待用户在 Unity 实跑复验。
- **整链路验证**：从 `E:/Project/ArtsProject` 重新导入 role_1213/head_1213/weapon_1200，
  三类模板结构、7/2/1 动作、Addressables、运行时补偿 0/0/1、头饰/武器正式 locator 拼装全部通过；
  武器定位误差约 `0.0000014`、旋转误差 `0°`。
- **经验文档**：[Art模板验收与挂点排查经验.md](Art模板验收与挂点排查经验.md)。核心教训：
  “错误变换稳定”不等于视觉挂点正确，自动矩阵验收必须和四方向、多动作视觉验收同时存在。

## 2026-07-27：大妖特效退场时序与服务端自动入场冻结补全

- **横底残留修复**：重新核对老端 `DungeonFightSceneMaskView + UIEffect.AddUIEffect`，确认正常时序为 `0.15s` 滑入、资源加载完成后播放 `1.5s`、先销毁特效、`0.15s` 滑出；`3s` 仅为加载失败兜底。`BossBornIntro.prefab` 已拆为四个可编辑时长，循环的 `liutizuo/liutiyou` 不再跟随兜底时长残留。
- **第二只大妖折返根因收口**：活服日志确认 `12005` 后副本快照只有 Boss，问题是服务端自动进场跳过客户端 `BeginEntering`，导致切入前最后一次野外小怪动作跨进同图副本。`AutoBrushBattleFlow` 现从权威场景清理事件补入 `Entering`，在 Toast/Boss 快照前统一冻结和收脚。
- **预览约束**：单特效继续走“神霄/资源/特效管理”通用预览；组合演出注册到 UiCreator/Prefab 预览入口，专用菜单仅作快捷入口，不按特效复制预览相机和窗口。
- **离线验证**：`Shenxiao.Module.Core.csproj`、`Shenxiao.Editor.csproj` 串行编译均 0 error；活服视觉结果待 Unity 连续推进到第二只大妖复验。

## 2026-07-27：第二只大妖进场顺序纠正

- **复验推翻上一版的不完整判断**：最新日志明确为 `13307 开启 → Unity 恢复野外自动战斗并追击小怪 → 12005 进入副本 → Intro`。`12005` 场景清理处补冻结只能收掉跨场动作，不能阻止入场前重新选怪；问题本质是等待服务端入场期间的顺序错误。
- **冻结前移**：`13300` 进度达到 `need_times` 时立即进入 `Entering`；`13307` 开启后严格对齐老端，只有进度快照存在且尚未满足时才恢复野外打怪，进度已满或快照未知均保持停止并等待权威入场。
- **双闸保留**：进度满足处负责覆盖服务端 `send_after(1000, do_info_enter)` 的等待窗口，`12005` 场景清理处继续作为权威切场安全闸；Boss 演出结束后才锁 Boss、解冻开打。
- **验证状态**：`Shenxiao.Module.Core.csproj` 离线编译 0 error；活服验收需要再次推进到第二只大妖，确认 `13307/13300 ready` 与 `12005` 之间不再出现野外小怪锁定、追击或攻击日志。

## 2026-07-27：战力提升数字与布局复原

- **位图数字**：老端 `fight_up/fight_up2` 的 BMFont XML 与彩色 PNG 已纳入 Unity；新增 `BitmapFontAssetBuilder` 生成 TMP 静态彩色位图字体，替换普通 TMP 渐变描边近似方案。
- **布局与定位**：Creator 按老端左上坐标恢复主数值 `119,33`、增量 `231,18` 与 50px 字号；根节点使用底部中心锚固定 `bottom=400`，不再固定在父层中心；绿色增量方向修正为 Unity `Y+30` 向上。
- **验证状态**：Core/Editor 离线编译均 0 error；需退出 Play Mode 后在重构 UI 生成器重新生成 `MainUI / FightingUpView(战力飘字)`，再用②预览完成图片字形和实际屏幕位置验收。

## 2026-07-27：首次技能与刀光冷加载前移

- **根因**：GameStart 原来只预热主角 `idle/run`；首次技能仍在战斗帧加载动作和 `skills_effect`。对 `role/1111` 这类逐动作替换角色，`attack/skill*` 未配置新 Prefab 时还会在首刀临时拼装整套旧模型兼容分支，之后因缓存命中才恢复正常。
- **配置驱动预热**：`SkillMovieConfigs` 现从 `ConfigSkillUI + ConfigCareerSkillMovies` 生成当前职业动作/粒子最小集合；既有 GameStart 预加载服务负责下载、加载并保留这些资源，同时纳入动作替换身体和部件 Prefab，不恢复老端数千条全量启动预热。
- **实例预建**：`ReplaceableRoleModel.PrepareActionsAsync` 在角色正式显示前静默预建新动作实例和未替换动作的旧模型分支；`MainRoleFlow` 对新建与同形象复用都执行首战动作准备，预热期间不改变当前 `idle`。
- **验证状态**：`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 离线编译均 0 error；`SceneMixDriverCase` 已增加“run/attack 已预建且 idle 未被抢台”的回归断言。活服需要停止并重新进入 Play Mode，以第一次普攻和第一次主动技能做冷启动复验。
- **经验文档**：[战斗表现首次施法卡顿-经验与排障.md](战斗表现首次施法卡顿-经验与排障.md)。

## 2026-07-27：战力提示偶发不关闭

- **根因**：`FightingUpView` 位于 `Window` 层，任务对话会临时禁用该父层；Unity 自动关闭 Coroutine 随父层失活被终止，但旧 `_autoClose` 句柄仍非空，恢复后既显示旧提示又无法重启关闭计时。
- **修复**：自动关闭改为 `Time.unscaledTime` 绝对截止时间，不再依赖会被父层生命周期中断的 Coroutine；父层恢复后若已超时，首帧立即关闭。连续战力更新仍会重新延后 1.8 秒。
- **验证状态**：`Shenxiao.Module.Core.csproj` 离线编译 0 error；待 Play Mode 复验“战力提示期间进入任务对话再返回”的交叉路径。

## 2026-07-27：普攻刀光错误挂到待机模型

- **根因**：主角使用逐动作新旧混合模型，`idle/run` 和 `attack/skill` 并非同一实例；技能粒子原先从混合容器递归找 `root`，总是先挂到隐藏的 idle 子树，导致攻击时不可见、收招切回 idle 后才补闪。
- **修复**：技能动作改为可等待切换；切换完成后统一从 `ReplaceableRoleModel.ActiveModel` 取得本次动作特效宿主，动作绑定和 `pos_type=0/2` 技能粒子均挂该实例；延时粒子捕获同一宿主。公共装配器也补齐混合模型的动作准备与切换等待。
- **验证状态**：`Shenxiao.Common.csproj`、`Shenxiao.Module.Core.csproj`、`Shenxiao.Editor.csproj` 串行离线编译均 0 error（仅既有 warning）；`SceneMixDriverCase` 已增加攻击特效宿主断言。需退出当前 Play Mode、等待 Unity 编译后重新进入，活服确认刀光与挥刀同步且收招不再补闪。

## 2026-07-27：物品/高阶装备与获得技能基础弹层

- **物品弹层补齐**：既有 `ItemUseFlow` 继续只消费 15010/15017/15018 已落地的真实背包实例，并按 `clientitemuse` 与穿戴评分筛选；补齐老端 0.3 秒下方淡入、质量名称色、`ClientConfigDefaultVo` 缺字段默认值（默认 10 秒）和 Popup 层生命周期。更高评分装备确认走 15201，普通物品确认走 15050。
- **获得技能弹层**：新增 `FunctionOpenAutoFlow + FunctionOpenAutoView`，严格以首份 21002 建基线、后续快捷栏技能等级上升入 FIFO 队列；复用 `FunctionOpenModule.prefab`，暗幕、布局、字号、图标尺寸、开场和关闭时长均可在 Prefab 调整。新增 Play Mode 菜单 `神霄/调试/UI弹层/预览获得技能`。
- **颜色公共语义**：新增 `LegacyUiColor`，集中承接老端 ColorUtil 深/浅色板与 `<color@N>`→TMP 转换，物品质量色和技能描述不再各自猜色。
- **声音审计**：Unity 当前音频文件为 0，`AudioManager` 未初始化且无播放调用；老端 676 个物理音频文件合并为 310 个逻辑声音、293 个逻辑名有多格式重复。批量去重导入和运行时服务接线已形成方案，但本轮未擅自新增 Addressables 资源或启动接线。
- **验证状态**：临时纳入 Unity 生成 csproj 后，`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 均离线编译 0 error；临时 csproj 条目已移除，等待 Unity 自动刷新正式工程。物品真实触发与技能获得视觉仍需 Play Mode 活服验收。
- **专题文档**：[基础弹层与声音迁移.md](基础弹层与声音迁移.md)。

## 2026-07-28：创角随机名服务器预校验与失败提示

- **根因**：Unity 直接把 `ConfigRandomName` 的“姓+名”组合提交给 `10003`，遗漏老客户端生成后先发
  `10007`、失败自动换名的流程；当候选名命中服务端动态敏感词库时返回 7，Unity 又因错误码映射缺失
  只打印“未知错误(7)”，玩家没有任何可见反馈。
- **修复**：随机名恢复最多 10 次的串行 `10007` 权威校验，3/4/5/7 静默换候选；断线、登录状态等
  非名字错误直接提示。`10003` 的所有失败结果统一 Toast，同时纠正 `10007` 的 3~7 错误码映射。
- **架构约束**：`10007` 没有请求序号，客户端同一时刻只保留一个在途验证，避免回包与候选名错配；
  服务端词库含运行时 `config_word`，不得以客户端静态过滤替代服务器校验。
- **验证状态**：`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 离线编译均 0 error；活服需用
  新账号确认随机名先出现 `10007` 日志，再发送 `10003`，并用手动敏感名确认玩家可见 Toast。

## 2026-07-28：登录连接等待动画与网络失败反馈

- **缺失根因**：老端 `START_GAME_CONNECT/GAME_CONNECT` 会驱动全局 `WaitforOpenViewLoading`，Unity
  虽已有旋转、延迟显形和 15 秒 source 过期代码，但没有 Prefab、没有资源地址，`LoginFlow` 也未接入；
  原流程只弹短 Toast，socket 连不上或 `10000` 不回时玩家看不到持续状态和可操作提示。
- **UI 架构修正**：新增 `WaitforOpenViewLoadingCreator` 到“重构UI 生成器 → Login”，只拥有新路径
  `Assets/Prefabs/UI/Login/WaitforOpenViewLoading.prefab`，不会重建任何现有登录 Prefab；View 改为直接
  `BaseView + public prefab 引用`，切断旧 `Generated WaitforOpenViewLoadingBind` 依赖。
- **连接状态机**：入口解析、WebSocket 建连、等待角色列表三阶段持续刷新同一转圈 source；在发
  `10000` 前建立 waiter，消除本机服极速回包竞态。各阶段 15 秒超时，连接任务超时会主动断开；
  等待 10000 期间收到断线事件会立即结束等待，不再傻等到超时。
- **玩家反馈**：入口失败、连服失败、10000 超时和中途断线均撤下转圈并弹出“是否重新连接”确认框，
  详细技术异常仅写日志。Prefab 未生成时保留 Toast 降级，登录流程本身不被新资源硬依赖阻断。
- **验证状态**：`Shenxiao.Module.Core.csproj` 串行离线编译 0 error；Editor 新生成器需等待 Unity 刷新
  工程后编译，并由用户在“重构UI 生成器 → Login”仅生成新增等待层，再跑 Addressable 自动分组和
  Play Mode 断服/停服实测。

## 2026-07-28：获得技能样式权威与物品弹层不可见修复

- **获得技能可编辑性**：确认弹层是 `FunctionOpenModule.prefab/FunctionOpenAutoView`，不是独立文件；
  移除运行时对 `tips` 字号、颜色、对齐、尺寸/位置及 `icon` 尺寸的强制覆盖，并禁止标题图片加载后
  `SetNativeSize`。`title`、`skillLab`、`tips`、`close_tip`、`icon` 现均以 Prefab 为视觉权威。
- **物品弹层根因**：背包协议、`clientitemuse` 筛选和候选队列均已正常执行，日志可见
  `[ItemUse] show ...`；真正不可见原因是 `CommonModule.prefab/ItemUseView/_gp_con` 被转换为 inactive，
  而 `BaseView.Show()` 只激活 `ItemUseView` 根节点。
- **修复**：恢复 `_gp_con` 的 Prefab 激活状态，并在加载/刷新时防御性激活；静止位置按老端
  `centerX=150, centerY=150` 对应到 Unity 的 `(150,-150)`，入场动画继续相对该人工位置从下方滑入，
  不再把布局写死在代码中。
- **验证状态**：`Shenxiao.Module.Core.csproj --no-restore` 编译 0 warning / 0 error；需要停止并重新进入
  Play Mode，实测下一件符合 `clientitemuse` 的物品和下一件高评分装备均能出现弹层。

## 2026-07-28：物品使用/穿戴弹层悬浮表现

- **老端取证**：`ItemUseView.ts` 的原始演出只有 `_gp_con` 从下方 244 单位、透明度 0 在 0.3 秒内
  `EASE_OUT` 入场；`ItemUseView.scene` 的 `ani1` 节点表为空，没有持续循环动画。
- **补充表现**：按本轮视觉要求，在入场完成后增加 1.6 秒一轮的轻微悬浮，面板从静止位向上 8 单位
  平滑往返；关闭、换候选和重置继续使用 `presentationVersion` 终止旧动画。
- **布局边界**：动画基准始终读取 `CommonModule.prefab/ItemUseView/_gp_con` 的 RectTransform，代码不保存
  屏幕绝对位置，后续人工拖动 Prefab 不需要同步修改动画代码。
- **活服复验修正**：`101011010/101021010` 的配置均明确为 `auto_use_sec=5`，但 `async Task + Task.Yield`
  逐帧演出未可靠持续，表现为悬浮和倒计时同时缺失。现已改为 `ItemUseViewBind.StartCoroutine` 统一驱动
  入场、12 单位悬浮和倒计时，并恢复装备卡底部“合成可获更强装备！”提示；日志追加实际 autoUse 秒数。
- **二次复验根因**：Editor.log 捕获到 `_gp_con` 缺少 `CanvasGroup` 后，C# `??` 未按 Unity 假空语义补挂，
  协程首帧抛 `MissingComponentException`。现改为 `GetComponent` 后用 Unity `== null` 显式判断并补挂；
  该异常正是“底部文案正常、悬浮和倒计时均不运行”的共同原因。

## 2026-07-28：主界面循环冲榜 3D 模型常驻特效

- **根因**：老端 `SetRoleModel` 会在模型和动作之外继续读取 `SceneObjectParticle` 并挂载骨骼常驻特效；
  Unity `MainUIRankView` 的轻量模型路径只执行 prefab 实例化、`UIModelStage` 上台和动作播放，漏掉了
  `EffectBinder`，所以稳定表现为“模型正常、附属光效消失”。
- **修复**：循环冲榜模型上台后按 `module + showId` 调用 `EffectBinder.AttachAlways`。古法符相
  `FaBao[1011]` 的 `Bone_06/Bone_14` 两套特效以及坐骑、剑魄、翅膀、神兵、背饰均复用同一配置链；
  活动关闭或模型切换时继续随模型宿主统一销毁。
- **边界**：该链属于 3D 模型骨骼特效，不启用头号玩家榜独立的 `RankEffectSlot/ui_cb01`，也不走
  `UIEffectStage` 共享 UI 特效通道。
- **验证状态**：模型、目标骨骼、对应特效 prefab 与 Addressables 地址静态核对通过；离线编译通过后，
  仍需 Play Mode 打开古法符相竞榜卡，确认书卷周围的两层常驻光效随模型旋转且关闭活动后无残留。

## 2026-07-28：主界面自动战斗 / 自动寻路状态特效

- **老端取证**：`MainUISecondaryView` 在 `_box_auto_effect` 播放 `ui_zidongzhandouzhong` / `ui_zidongxunluzhong`，寻路中的 `SourceOperateMove` 优先于自动战斗；状态事件按 440ms 合并刷新。对应宿主在野外固定于屏幕中下部、离线经验文字上方。
- **缺失根因与返修**：Unity 已导入两套特效 prefab、动画、材质和 Addressables key，但首轮误把消费逻辑接到已退役的 `HudSecondary/MainUISecondaryView`；该 View 不在 `MainUIModuleCreator.Parts` 或 `MainUIFlow.FirstPassViews` 中，运行时根本不会实例化，因此即使编译和静态资源检查通过也不会显示。现已迁到实际承载绿色挂机经验文字的 `HudOnHook/MainUIOnHookView`，并撤销退役 View 上的运行时代码与槽位。
- **修复**：补 `EVT_AUTO_FIND_WAY_STATE`，由主角自动移动/任务跳跃的开始与各类结束边沿维护寻路状态；`MainUIOnHookView` 按“寻路 > 自动战斗 > 隐藏”选择互斥特效并用版本号收编异步 Handle。根据 Play 画面返修，共享 RT 横向镜像后的 X 偏移由 `+6.8` 反号为 `-6.8`，让文字回到屏幕中轴；同时移除 440ms 人为合并延迟，状态事件到达即切换。Creator 与现网 prefab 同步落 `offset=(-6.8,-4), scale=6.4`，布局继续归 prefab 管理。
- **验证状态**：`Shenxiao.Module.Core.csproj`、`Shenxiao.Editor.csproj` 编译通过；用户 Play 画面已确认 `MainUIOnHookView` 实际显示特效。返修后用真实 `HudOnHook.prefab` 和 `UIEffectStage` 分别实例化两套特效，渲染器包围盒经共享 Camera/横向镜像回算到 720 基准宽的中心分别为 `x=363.34`（自动战斗）和 `x=363.12`（自动寻路），与屏幕中轴 `x=360` 的误差均小于 4px；运行槽序列化偏移均为 `(-6.8,-4)`。状态事件处理已改为直接调用刷新，不再经过 440ms 延迟。

## 2026-07-28：真机长竖屏登录页上下补边修复

- **根因**：`Launch` 的 `CanvasScaler=Expand` 配置正确；异常来自 `LoginStage` 把内部视口固定为
  720×1280。1224×2700 真机按宽度缩放 1.7 倍后，视口物理高度只有 2176px，剩余 524px 被居中
  分到上下，表现为各约 262px 的深色外层背景，并非横竖屏识别或安全区错误。
- **修复**：`LoginStage/Viewport720x1280` 改为固定设计宽 720、纵向铺满父级；长竖屏随逻辑画布增高，
  横屏画布高度仍保持 1280，因此继续只在左右补边。`LoginPanel/Bg` 使用
  `AspectRatioFitter.EnvelopeParent` 以当前 872×1560 竖图等比 cover，超宽部分裁切而不拉伸人物。
  Creator 源与 `LoginStage.prefab`、`LoginPanel.prefab` 已同步，运行时不计算屏幕布局。
- **验证状态**：静态矩阵覆盖 720×1280、1080×2400、1224×2700、750×1334、1280×720、
  1920×1080；目标真机档的上下补边由约 262px/边降为 0，横屏两档仍只产生左右补边。
  `Shenxiao.Module.Core.csproj`、`Shenxiao.Editor.csproj` 均为 0 error，prefab/Creator 一致性检查通过；
  需重新出 Android 包在原真机做最终画面确认。

## 2026-07-28：Android 大妖入场永久冻结与模拟器视频花屏定位

- **大妖卡死根因**：整包日志完整走到 Boss 生成和 `Entering -> Intro`，但没有 `-> Fighting`；
  `BossBornIntro.prefab` 激活后的 `Start()` 抢先以空回调自动预览，异步加载续体再绑定战斗回调时被
  `_started` 拒绝，最终 `CombatFreeze` 永不释放。
- **修复**：生产 Prefab/Creator 关闭自动预览，播放器允许旧 Prefab 在已开始后补绑唯一完成回调，计时改用
  非缩放时间；外层增加 epoch 隔离的真实时间看门狗，播放器或回调异常也会释放演出并进入 Fighting。
- **视频结论**：8 段创角 MP4 均为 H.264/yuv420p；模拟器实录是 `OMX.qcom.video.decoder.avc` 向
  `goldfish_vulkan` 输出时 `DynamicANWBuffer failed`，属于模拟器硬解码/Vulkan 表面桥接问题。先切模拟器
  OpenGL/兼容模式并冷启动，最终以真机为准；未为不受 Unity 支持的模拟器全局改动 Android 图形 API。
- **验证状态**：已完成旧包 logcat 与状态机日志取证；`Shenxiao.Module.Core.csproj`、
  `Shenxiao.Editor.csproj` 串行编译均为 0 error，Prefab/Creator 的自动预览值一致为关闭。新整包仍需在
  真机复验大妖正常进入 Fighting，并在模拟器切 OpenGL 后复验创角视频。

## 2026-07-28：主界面技能手点与范围规则对齐

- **点击根因（二次核验）**：真实 `HudSkillBar.prefab` 的 `bg/icon/lock` 三层 Image 均开启
  Raycast，但旧实现只把 Button 挂在底层 `bg`，实际点按先被上层 `icon` 或同级 `lock` 命中，
  `OnClickSkill` 根本不可达；此前直接反射点击函数的用例漏掉了 UGUI 射线路径。现关闭全部装饰层
  Raycast，由 45×47 的 `con` 生成唯一透明点击面和 Button。业务层同时保留前次修复：只在服务端
  `13017` 的 `RoleModel.DepositState` 为真时拦截，普通自动战斗允许玩家插入手动技能。
- **手动/自动分流**：手动无锁定目标时按当前朝向原地释放，只在技能 `area` 内局部预选，不再跨全场
  抢最近怪后自动贴近；自动战斗仍保留全场寻敌与接近。`obj==1` 自身技能补齐原地真实释放。
- **范围根因**：旧实现只用 `distance*0.8` 作接敌距离，漏掉老端圆形模式的 `area`。现 `range==1`
  使用 `max(100,(distance+area)*0.8)`，其他模式使用 `max(100,distance*0.8)`；御剑一式从错误的
  100px 恢复为 360px。
- **命中几何**：补齐 `range=1/2/3` 的圆形、前向矩形和扇形选怪，保留直线/扇形 100px 近身豁免、
  距离稳定排序、主目标置首和 `num[1]` 怪物上限；无目标圆形的前方圆心同时恢复老端横向 1.0 /
  纵向 0.5 的椭圆距离修正。配置 `desc` 可能与 `range` 冲突，运行时严格以
  `range/distance/area/num` 为权威；PvP 玩家列表仍未迁移。
- **验证状态**：Unity 6000.3.17f1 强编译 `completed/failed=false`；`SkillTargetingCase` 会克隆真实
  `HudSkillBar.prefab`，用 `GraphicRaycaster` 验证顶层命中 `con`，再走 `PointerClick` 验证释放事件；
  同时覆盖普通 AutoFight/托管门闩、360/440/480 三个接敌距离、圆/直线/扇形边界及手动无目标局部
  预选，返回 `0`、`failures=0`。
- **专题文档**：[技能点击与范围规则-经验与排障.md](技能点击与范围规则-经验与排障.md)。

## 2026-07-28：主界面聊天消息内容与显示链路补齐

- **消息少的真实根因**：`MainUIChatView` 原来只生成硬编码欢迎语，既不读取 `ChatModel` 也不监听
  `EVT_CHAT_MESSAGES_UPDATED`；同时 `GAME_START` 只请求仙宗、世界两类 11010 缓存，漏掉私聊以及
  非开服首日的小跨服/百煞冲霄缓存。
- **协议修复**：启动请求恢复为老端顺序；11010 公共缓存由 wire 的新→旧翻为旧→新，私聊缓存保留
  发收双方和未读状态；11001/11010 的频道 20 均映射到频道 17。普通聊天协议既有回归保持通过。
- **显示修复**：现有上下两块内容区保持不变，均改为真实模型驱动并实时刷新；频道徽标使用正文首行
  `<space=46px>` 留位，避免“系统/世界”图片覆盖文字，换行仍回到左边缘。单条基础高度从 50 对齐
  老端 29，多行按 TMP 首选高度扩展，每块显示末尾 30 条并自动滚到底部；欢迎语同步纠正为老端原文。
- **验证状态**：`Shenxiao.Module.Core.csproj` 与 `Assembly-CSharp-Editor.csproj` 均 0 error；Unity
  `ChatCase` 返回 `pass=True`，真实 `HudChatBar.prefab` 的 `MainUIChatHudCase` 覆盖分流、30 条裁剪、
  徽标首行位置、29 高度、富文本、实时事件和滚动位置，返回 `pass=True`。
- **边界**：本轮不调整上下双栏、合并聊天栏或 Tab 方案，待策划确认后单独处理。
- **专题文档**：[主界面聊天消息链路-经验与排障.md](主界面聊天消息链路-经验与排障.md)。

## 2026-07-28：主角升级、任务跑动与直接自身特效补齐

- **升级根因**：13003 已真实到达、`effect_xemlvup` prefab/材质/Addressables 地址也可独立渲染；
  旧实现却手工实例化到带 `-38°` 倾斜的 `MainRoleTilt`，不符合老端 `attach_type=15=SceneObj`
  的“同位置但不受模型旋转”语义，并会在主角尚未装配时直接丢触发。
- **挂载修复**：`SceneCharacterStage` 增加与 `MainRoleTilt` 同级、同落点、单位旋转的
  `MainRoleDetachedEffects`；升级与采集完成统一通过 `EffectBinder` 挂载。升级协议支持最多两次待播，
  主角装配后补播，不再业务层手工 `LoadAsync + Instantiate`。
- **任务跑动拖尾**：新增任务专用 `MoveToTaskTarget`，严格按老端仅在场景 `type=0/1/4`、目标距离
  `>7` 逻辑格时延时 150ms 播 `char_acceleratebuff01`；X/Y 分别按 `LogicRatioX/Y` 换算。拖尾挂当前
  `ReplaceableRoleModel.ActiveModel`，动作实例切换时重挂；12001 活动态使用 `MoveType.Accelerate`，
  到达、手动接管、技能、采集、跳跃、权威位置修正和销毁均完整清理。
- **跳跃与采集**：任务多段跳在每段起点留下世界坐标特效，中间段用 `char_jumpfx_01`、最终段用
  `effect_jump_qitiaoyan`，不误播任务 `show_effect=false` 时不应出现的 `char_jumpfx_02`；采集完成请求
  `20008 flag=2` 前补 `other_effect_caiji_02`。
- **模型常驻边界**：场景主角关闭老端本就禁用的 `Body.always`，UI 模型默认行为不变；武器、翅膀、
  背饰常驻特效继续加载。其他玩家 `protect_time` 离线保护待其他玩家渲染实体链，通用战斗 Buff 仍归
  独立 `BuffListVo` 生命周期迁移，均未伪挂主角。
- **淡化/不可见续查**：运行日志已证明 `effect_xemlvup` 和 `char_acceleratebuff01` 均真实进入播放态；
  根因是 `SceneCharacterStage` 在切入无 `ArtModelRenderProfile` 的旧跑动模型时，把透明 RT 的 RawImage
  切回默认 UI `SrcAlpha`，使已预乘进 RT 的粒子再次乘 Alpha。现统一固定为
  `Shenxiao/UI/StageComposite`，不再由新旧模型/profile 决定；老端明确的跑动自身特效仍只有
  `char_acceleratebuff01`，没有凭截图增加第二套橙色寻路特效。
- **任务完成演出**：补齐老端 30004 `code=1` 成功分支的 `ui_renwuwancheng`：挂 `UILayer.Top`、位置
  `(0,4)`、缩放 1、严格显示 1.5 秒；它独立于 `TaskFinishView` 和角色身上粒子，连续播放及会话退出均
  会释放旧句柄。
- **验证状态**：Unity 强制编译 `completed/failed=false`；专项 `RolePresentationEffectsCase` 覆盖 8 个
  已转换资源、attach_type=15 直立宿主、真实 EffectBinder 挂载、预乘 RT shader、场景/7 格距离门禁，
  并真实创建 Top 层任务完成实例，返回 `0 / ALL PASS`。运行时 RT 探针验证升级画面峰值 255，跑动资源
  峰值 164/约 2.1 万个亮像素；30004 合成探针验证任务完成资源含 5 个粒子系统、6 个 Renderer，画面峰值 255。
- **专题文档**：[主角场景自身特效-经验与排障.md](主角场景自身特效-经验与排障.md)。

## 2026-07-29：NPC 对话真机位置、语义点击与场景选中反馈

- **位置根因与修复**：`DialogueModule` 外层虽已全屏，内部 `DialogueView/_img_bg/_box_model` 仍固定
  720×1280 顶对齐，长竖屏多出的逻辑高度全部留在下方，导致底栏被抬高。三者现改为全伸展，
  `_box_bottom` 继续锚底；未在运行时代码加入分辨率坐标特判。
- **交互修复**：对话恢复老端背景、底栏、继续、领取和跳过统一进入当前语义动作；Module 根为唯一
  Raycast/Button 点击面，且显式创建根透明 Image，避免布局首帧零尺寸时 `UIUtil` 退用局部子 Graphic；
  子 Graphic 不截射线，手点和倒计时共用入口并防重复执行。任务完成弹层的
  屏外遮罩与面板点击全部提交 30004，关闭图标不再只隐藏，`_submitSent` 防止连点多发；未改用户正在
  调整的 `TaskModule.prefab` 布局。
- **选中反馈**：补 `SceneTargetSelection`，NPC/怪物共用老端真实 `function_selection`，按老端缩放 0.7，
  挂目标稳定 `Tilt` 并以 +38° 抵消场景倾斜。点选、异步模型就绪、切换、移除/死亡、清目标、断线和
  切场景生命周期已闭合；不按不同新模型复制或手画选中圈。
- **验证状态**：`DialogueInteractionCase` 已加入真实 prefab 的 720×1600 锚定与
  `GraphicRaycaster → PointerClick` 三点验收；`TaskFinishInteractionCase` 增加真实任务 prefab 的面板内外
  语义点击验收；`RolePresentationEffectsCase` 增加选中资源可播放、0.7 缩放和倾斜抵消断言。
  当前 Unity Editor 强编译通过；对话专项为 `3/3` 真 PointerClick、任务完成专项为 `2/2` 真 PointerClick，
  `function_selection` 另经 `EffectBinder` 实际加载/播放探针验证 Renderer、Animation、0.7 缩放与 +38° 均通过。
  最终长竖屏位置与选中观感仍需真机确认。
- **专题文档**：[NPC对话与场景选中-经验与排障.md](NPC对话与场景选中-经验与排障.md)。

## 2026-07-28：升级文字/整套特效亮度与任务完成位置二次校正

- **分类确认**：`ui_renwuwancheng` 是 30004 成功后挂 `UILayer.Top` 的全屏 UI 特效；“升级”图片虽是文字，实际是 `effect_xemlvup` 内的粒子 Billboard，整套特效按老端 `attach_type=15` 挂角色同位的直立场景宿主，二者不能合并为同一条 UI/角色链。
- **升级发淡根因**：项目切到 Linear 后，旧 Laya 粒子普遍使用的 `tint=0.5 × shader 2` 中性约定被 Unity 颜色属性转换为约 `0.214×2`，只有原强度约 43%。`LayaParticleUnlit` 现仅在 Linear 下把材质 tint 恢复为 Laya 数值，纹理和项目整体仍保持 Linear；实测中性样本由约 `RGB 0.428`（Linear）恢复为 `RGB 1.000`。
- **升级文字消失根因**：旧转换器用 Unity 自动平滑切线承接 Laya 的线性尺寸关键点，`other_xemshenji01` 在 0.55 秒处曲线从应有约 `0.503` 下冲到 `-0.083`，粒子尺寸被钳为 0。转换器升到 v26，所有 Laya 数值渐变明确使用 Linear 切线；当前升级 prefab 定点恢复三处源 `0.1s` 延迟并线性化文字尺寸曲线，保留此前人工修正的图片引用。修后文字粒子在 0.55 秒处尺寸为 `2.015`，1.1/0.8 秒源寿命未被擅自延长。
- **任务完成位置**：老端 UIEffect 的 RenderTexture 最终贴图纵向与 Unity 通道相反；老端输入 `(0,+4)` 在屏幕上方，Unity 原样传入却落到下方。现仅在任务成功业务边界映射为 `(0,-4)`，探针中心由 `y≈1040/1280` 校正到 `y≈240/1280`，颜色和 1.5 秒时长不改。
- **验证状态**：Unity shader 编译 `errors=false/supported=true`，中性色真实 RT 采样 `RGBA(1,1,1,1)`；升级完整 RT 在 0.55 秒已同时显示“升级”、法阵、光柱和粒子，任务完成完整 RT 位于屏幕上方。`RolePresentationEffectsCase` 新增 RGB 三通道、shader error、升级文字延迟/寿命/中段尺寸及任务坐标回归。

## 2026-07-29：新模型任务跑动拖尾空间校正

- **形态根因**：新动作 prefab 的空 `root` 留在美术原始原点；`ArtModelStager` 按 `landingOffset/landingScale` 把人物落点归零，并叠加主角场景缩放 0.85 后，运行时实测 1111 run 的旧挂点偏离人物 3.476、世界缩放 0.315，1213 run 偏离 1.284、世界缩放 0.333，导致老端覆盖人物周身的御风流光在新模型上缩成脱离人物的小尾巴。
- **挂载修复**：`SceneCharacterStage` 新增 `MainRoleAttachedEffects`，作为模型容器下的稳定随身宿主；它继承主角 yaw 和 2.5D 倾斜，但以逆缩放抵消 0.85 场景体量，保持世界缩放 1。任务拖尾改挂该宿主，不再递归命中新动作内部 `root`，idle/run/skill 切换也不再销毁重挂。
- **边界**：骨骼技能/动作特效仍挂 `ActiveModel`，升级和采集完成仍挂直立的 `MainRoleDetachedEffects`，世界位置型跳跃特效仍挂 `SceneEffectAnchor`；未修改公共 `char_acceleratebuff01` 资源，老模型表现不受单独缩放补偿污染。
- **回归**：`RolePresentationEffectsCase` 增加 0.85 模型缩放下的宿主落点、世界缩放 1 和真实任务拖尾挂载断言，返回 `0 / ALL PASS`。实际 1111/1213 新 run 与 1111 老模型探针均确认新宿主 `distance=0 / worldScale=1`；Unity 强编译 `completed/failed=false`。

## 2026-07-29：任务跑动流光动画恢复

- **纠正方向**：撤销对 `char_acceleratebuff01` 增加固定 Z 后移的尝试，恢复资源原始局部位置；新旧模型继续共用稳定、单位缩放的 `MainRoleAttachedEffects`，不再按舞姬或其他单个模型做位置特判。
- **表现根因**：老端网格材质用 1 秒循环动画把 `_MainTex_ST.z` 从 0 线性滚到 3；`lyz_trail_117` 自身包含透明—亮—透明分布，因此运行中自然形成流光和周期隐现。Unity 旧转换资源只留下 `_BaseMap_ST` 曲线，而 `LayaParticleUnlit` 实际采样 `_MainTex_ST`，导致 UV 停在首帧，三角光带从起跑起持续常亮。
- **资源修复**：将当前 `char_acceleratebuff01.anim` 的 UV 四分量绑定恢复到 `material._MainTex_ST`，保留原始 `0→3/1s`、线性切线和 `WrapMode.Loop`，不修改贴图、颜色、网格、粒子或空间位置。
- **回归**：Unity 强制编译 `completed/failed=false`；`RolePresentationEffectsCase` 返回 `0 / ALL PASS`，同时要求 `_MainTex_ST.z` 原始曲线在 `0s/0.5s/1s` 分别为 `0/1.5/3` 且循环，并通过真实 `Animation.Sample + MaterialPropertyBlock` 验证运行时从 `0` 滚到 `1.5`，防止资源再次退化成只写 `_BaseMap_ST` 或曲线存在但未进入渲染器的常亮尾片。

## 2026-07-29：任务跑动流光顶点渐隐恢复

- **根因复核**：直接以老 Laya 引擎加载 `char_acceleratebuff01.lh` 并逐相位对照，确认空间朝向和 `_MainTex_ST.z:0→3/1s` 都没有放反。硬直线来自 Unity 现有 `eff_cys_sz01_mesh.asset` 丢失老 `.lm` 的 `COLOR` 通道：源网格 32 个顶点均有颜色，Alpha 包含 `0 / 0.133 / 0.467 / 0.733 / 0.8 / 1`，Unity 产物此前为 `colors=0`。
- **资源修复**：只给 `eff_cys_sz01_mesh.asset` 恢复源网格的 32 个顶点色，不改人物挂点、特效 prefab 位置/旋转、贴图、材质和 UV 动画。这样流光亮区扫到网格端点时仍受逐顶点 Alpha 收束，不再形成方形硬截断。
- **回归加固**：`RolePresentationEffectsCase` 在原有真实 `_MainTex_ST` 运行时采样之外，新增 `colors == vertexCount`、Alpha 最小值约 `0`、最大值约 `1` 且存在软渐变值的断言，防止历史资源重生成后再次丢失顶点渐隐。
