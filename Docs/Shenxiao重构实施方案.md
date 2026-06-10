# Shenxiao 重构实施方案

> 主线：先搭框架、公共模块、工具链，业务模块在此之上流水线推进。
> 目标平台：WebGL（主）+ Android/iOS（备）。
> **资源全部 Remote 加载，不打进包**（包体控制核心约束）。
>
> 参考项目：
> - `D:/GitProject/yu_client`（LayaAir 客户端，参考用）
> - `D:/GitProject/yu_gm`（GM 后台，参考用）
> - `D:/GitProject/yu_server`（Erlang 服务端，必要时新建分支修改）

---

## 一、Unity 工程现状

| 项 | 值 |
|----|----|
| Unity 版本 | Unity 6 (6000.x) |
| 渲染管线 | URP 17.3 |
| UI | UGUI 2.0 |
| 输入系统 | Input System 1.18 |
| 路径 | `D:/UnityProject/Shenxiao` |

需要补的包：
```
com.unity.addressables          # 资源远程加载（核心）
com.unity.nuget.newtonsoft-json # JSON 解析
com.unity.textmeshpro           # UI 文本（已隐式，确认）
```

> 多语言：仅中国大陆上线，**不引入 Localization Package**。
> 全部走配表读取（`Lang.Get(key)`），后续若需出海再扩展。

---

## 二、总体架构

```
┌───────────────────────────────────────────────────────────┐
│                    Business Modules                       │  ← Phase 2/3
│  Login | MainUI | Role | Equip | Bag | Skill | Pet ...    │
├───────────────────────────────────────────────────────────┤
│                  Common Modules (公共模块)                 │  ← Phase 1
│  RedDot | Audio | Guide | Tips | Loading | Effect ...     │
├───────────────────────────────────────────────────────────┤
│                  Framework (框架层)                        │  ← Phase 0
│  Net | UI | Res | Scene3D | Event | Config | StateM       │
├───────────────────────────────────────────────────────────┤
│         Tools (Editor 工具)                                │  ← Phase 0
│  AssetConverter | LanhuCreator | ConfigGen | AddrSetup    │
└───────────────────────────────────────────────────────────┘
```

资源走向：

```
LayaAir 源资源              Unity 工程               运行时
─────────────              ──────────              ──────
.png/.jpg ────────→  Assets/GameRes/...png  ──→  Remote AB
.lm + .lani  ──┐
.lmat  ────────┼──→  Assets/GameRes/...prefab  ──→  Remote AB
.lh  ──────────┘
蓝湖设计数据 ────→  Assets/Prefabs/UI/*.prefab  ──→  Remote AB
.json (config)  ──→  Assets/GameRes/.../config/  ──→  Remote AB
```

包体只含：代码 + 启动场景 + 极简 Loading + AppConfig。
Login UI、业务 UI、模型、特效、音频、配置表全部走 Remote/API 加载，不进入包体。

### 2.1 系统级工程记忆（配置驱动 + 工具链先行）

这是本项目推进任何大功能前必须默认采用的工程方式：

- Shenxiao 是 MMORPG 重构项目，大部分业务不应靠逐个手写功能堆出来，而应先定标准、配置结构、生成/校验工具和运行时骨架。
- 业务数据归策划和配置负责，客户端负责提供稳定骨架、配置读取、资源加载、UI 渲染、协议接入和编辑器工具。
- 例如 Boss 活动这类系统，Boss 列表、开放时间、头像、战力、掉落、入口表现等都应优先通过配置定义；代码只实现通用活动框架、配置驱动渲染、协议交互和必要的扩展点。
- 做任何大功能前，AI 必须先判断是否需要：
  1. Schema / JSON 配置结构
  2. ConfigGenerator 输出
  3. Editor 校验或导入工具
  4. Addressable 资源命名与分组规则
  5. 通用运行时骨架与少量业务扩展点
- 禁止在没有标准和配置方案时，直接把某个活动、界面、系统写死成一次性逻辑；除非明确是临时验证切片，并且必须标注后续配置化入口。
- UI 样式同样遵循工具链和 Prefab 优先：业务代码不写颜色、尺寸、位置、字体、描边、过渡等样式变化；除非明确要求临时验证，否则样式从蓝湖、模板 Prefab、Prefab 或 UICreator 输入中调整。
- 正式项目默认不写 mock/fake/stub 数据或假接口；登录、资源、协议、配置和业务数据都优先接真实链路，缺数据时暴露问题并补真实数据处理，只有用户明确要求临时验证时才允许 mock。

---

## 三、Phase 0：框架搭建（最先做）

### 3.1 工程目录规划

```
Assets/
├── _App/                          # 启动期资源（打进包体）
│   ├── Scenes/Launch.unity        # 启动场景
│   ├── UI/Loading.prefab          # 加载界面
│   └── Configs/AppConfig.asset    # 包内启动配置
├── GameRes/                       # 运行时资源（全部 Remote 加载）
│   └── resource/
│       ├── game/{module}/texture/ # UI 图片
│       ├── object/{type}/         # 3D 模型（转换后）
│       ├── effect/                # 特效（转换后）
│       ├── config/                # 配置 JSON
│       └── sound/                 # 音频
├── Prefabs/
│   ├── UI/{Module}/               # UI 生成工具生成
│   └── 3D/                        # 3D 预制体
├── Scripts/
│   ├── Framework/                 # 框架（详见 3.3）
│   ├── Common/                    # 公共模块（详见 3.4）
│   ├── Module/                    # 业务模块（Phase 2/3）
│   └── Generated/                 # AI 翻译产出（Model/Vo/Config）
└── Editor/
    ├── AssetConverter/            # .lm/.lani/.lmat/.lh → Unity
    ├── LanhuCreator/              # 蓝湖 → Prefab + Bind + 缺图报告
    ├── ConfigGenerator/           # 配表统一生成器（C# Vo + 校验）
    ├── AddressableSetup/          # 自动设置 Addressable
    └── BatchTools/                # 批量导入/重命名等
```

### 3.2 Assembly Definition 拆分

```
Shenxiao.Framework.asmdef       ← 不依赖业务
Shenxiao.Generated.asmdef       ← 工具产出（Vo / ConfigXxx / *Bind.cs），依赖 Framework
Shenxiao.Common.asmdef          ← 依赖 Framework + Generated
Shenxiao.Module.Core.asmdef     ← 登录/主界面/角色/背包等核心模块
Shenxiao.Module.Combat.asmdef   ← 场景/战斗/技能/特效表现
Shenxiao.Module.Social.asmdef   ← 公会/好友/聊天/婚姻等社交模块
Shenxiao.Module.Activity.asmdef ← 活动/运营/商城/充值等模块
Shenxiao.Editor.asmdef          ← Editor Only
```

先按大域拆 asmdef，避免 211 个业务模块都建 asmdef 导致依赖管理过重。
后续某些模块稳定且独立后，再按需细拆。

**Generated 单独建 asmdef** 的原因：UI 生成工具输出的 `*Bind.cs` 是 partial class，必须与同名 partial 在同一 assembly；ConfigGenerator 输出的 Vo 类被多个 Module 共用。集中放在 `Assets/Scripts/Generated/` 下统一管理，业务侧通过继承（View）或直接引用（Vo）使用。

### 3.3 框架层（Scripts/Framework）

| 子系统 | 文件 | 职责 | 对应 LayaAir |
|-------|------|------|--------------|
| **Net** | ErlangParser.cs | Erlang term 二进制解析 | ErlangParser.ts |
| | UserMsgAdapter.cs | 协议封包/解包 | UserMsgAdapter.ts |
| | NetManager.cs | WebSocket 连接管理 | — |
| | Proto.cs | 协议号 + 消息结构定义 | Proto.ts |
| | BaseController.cs | RegisterProtocal/SendFmt | BaseController.ts |
| **Res** | ResManager.cs | Addressables 封装 | ResManager.ts |
| | GameResPath.cs | 路径工厂（保持原 API） | GameResPath.ts |
| | ResVersionManager.cs | Catalog 版本/增量更新 | ResVersionManager.ts |
| **UI** | BaseView.cs | 面板生命周期、层级 | BaseView1.ts |
| | ViewManager.cs | 注册/打开/关闭/栈 | ViewManager.ts |
| | LayerManager.cs | UI 层级管理 | LayerManager.ts |
| | UIBinder.cs | 节点名→字段自动绑定 | (新增) |
| **Event** | EventDispatcher.cs | 全局事件 | EventDispatcher.ts |
| | GlobalEventSystem.cs | 事件常量 | GlobalEventSystem.ts |
| **Config** | ConfigManager.cs | 配表加载/缓存 | — |
| | BaseVo.cs | 数据对象基类 | BaseVo.ts |
| | Lang.cs | 语言文本读取（单一汉化表） | (新增) |
| **StateM** | StateMachine.cs | 通用状态机 | StateMachineManager.ts |
| **Scene3D** | SceneObj.cs | 模型加载、特效挂接 | SceneObj.ts |
| | Character.cs | 状态机+动画+移动 | Character.ts |
| | Role/Monster/Npc | 角色子类 | 同名 ts |
| **Util** | Util.cs | 工具方法集 | Util.ts |
| | TimeUtil.cs | 时间处理 | TimeUtil.ts |
| | HttpUtil.cs | HTTP 请求 | HttpUtil.ts |

### 3.4 公共模块（Scripts/Common）

| 模块 | 职责 | 对应 LayaAir |
|------|------|--------------|
| **RedDotSystem** | 红点位运算 + 80+ 模块 ID | RedDotManager.ts |
| **AudioSystem** | 音乐/音效/语音/分类音量 | SoundManager.ts |
| **TipsSystem** | 飘字、Toast、确认弹窗 | TipsManager / AlertView |
| **LoadingSystem** | 全屏 loading、菊花、进度条 | LoadingView |
| **EffectSystem** | 粒子特效挂接、池化、清理 | ParticleManager |
| **GuideSystem** | 新手引导箭头/手指/遮罩 | ArrowComponent + StoryModel |
| **ChatBubble** | 头顶聊天气泡 | (公共组件) |
| **HudSystem** | 头顶血条/名字/称号 | (公共组件) |
| **Tooltip** | 装备/物品/技能 Tooltip | common 模块下的各 Tips |
| **PopupQueue** | 弹窗排队/优先级 | ViewManager 部分 |
| **PrefsSystem** | 本地数据持久化 | (新增) |

### 3.5 验收标准（Phase 0）

- 框架层代码就位；公共模块完成接口、空实现和最小可运行链路
- 协议层能连 Erlang 服务端，发送一条协议并正确解析返回
- Addressables Remote 配置完成，能从本地 HTTP 服务器加载一个测试 Bundle
- 资产转换器基础版可用：任意一个 LayaAir 模型 + 动画 + 材质能转为 Unity Prefab 并播放
- UI 生成工具可用：任意一个蓝湖导出界面能生成可视的 Unity Prefab + Bind + 缺图报告
- ConfigGenerator 可用：JSON + Schema → C# Vo 类 + Config 读取代码；Erlang `data_*.erl` 由现有 Python 工具链生成
- 包体大小验证：WebGL 压缩首包 <8MB；Android/iOS 空包 <30MB

---

## 四、配表统一生成方案（重点）

### 4.1 现状（参考 yu_client / yu_server）

```
当前流水线（yu_client/tools/yu-resource-tool）：
─────────────────────────────────────────────────
策划编辑 ──> JSON (cdn/resource/config/server/*.json)
              │
              ├── 客户端运行时直接读
              │
              └── config_erlang.py.generate()
                          │
                          └── data_*.erl  ──> 服务端编译 → 热更
```

服务端 `data_*.erl` 三种格式：
- **kv**：`get_name(Id) -> Value;`
- **module_open**：`get_ids(Type) -> [{Id, Lv}, ...];`
- **record**：依赖 hrl 中 `-record(xxx_cfg, {...})`

已有工具：
| 文件 | 作用 |
|------|------|
| `python/config_erlang.py` | 三种格式生成器（已实现） |
| `python/hrl_parser.py` | 解析 hrl record / data_*.erl 头部 |
| `python/field_mappings.json` | JSON 字段 ↔ erl record 字段映射 |
| `python/config_excel.py` | Excel 导入导出 |

**结论：现有 Python 工具链完全可用，不需要重写。**
Shenxiao 的工作是：**消费同一份 JSON**，在 Unity Editor 里生成强类型 C# Vo 和读取代码；
Erlang `data_*.erl` 仍由 `yu-resource-tool` 的 Python 生成器负责，Shenxiao 只做调用、校验和结果检查。

### 4.2 单一数据源原则

```
                    ┌─────────────────────────────┐
                    │   JSON (single source)      │
                    │   cdn/resource/config/      │
                    └──────────────┬──────────────┘
                                   │
            ┌──────────────────────┼──────────────────────┐
            ↓                      ↓                      ↓
   ┌────────────────┐    ┌─────────────────┐    ┌────────────────┐
   │  Erlang Server │    │ Unity Client    │    │ GM Backend (PHP)│
   │  data_*.erl    │    │ Vo + JSON       │    │ 直接读 JSON     │
   │ (existing pipe)│    │ (NEW pipeline)  │    │ (existing)      │
   └────────────────┘    └─────────────────┘    └────────────────┘
```

**核心约束**：
1. **JSON 是唯一权威源**，谁都不能绕开 JSON 直接改下游产物
2. **JSON Schema 必须定义清楚**（字段名、类型、是否必填、默认值）
3. **三端的字段定义对齐**（Erlang record 字段 = Unity Vo 字段 = JSON key）

### 4.3 Shenxiao 端实现：ConfigGenerator

Unity Editor 工具，输入 JSON + Schema，输出强类型 C# 类。

#### 输入：JSON Schema（每张配表一个）

`Schemas/configs/config_skill.schema.json`：
```json
{
  "table": "config_skill",
  "key_field": "id",
  "key_type": "int",
  "fields": [
    { "name": "id", "type": "int", "comment": "技能ID" },
    { "name": "name", "type": "string", "comment": "技能名" },
    { "name": "career", "type": "int", "default": 0, "comment": "职业" },
    { "name": "damage", "type": "float", "default": 1.0 },
    { "name": "particles", "type": "ParticleCfg[]", "comment": "特效列表" }
  ],
  "nested_types": {
    "ParticleCfg": [
      { "name": "res", "type": "string" },
      { "name": "pos_type", "type": "int" },
      { "name": "attach_type", "type": "int" }
    ]
  }
}
```

> Schema 来源：从 yu_server hrl 文件 + field_mappings.json **半自动生成**，
> 由 ConfigGenerator 工具反向生成 Schema 草稿，人工审核入库。

#### 输出 1：C# Vo 类

`Assets/Scripts/Generated/Config/SkillCfg.cs`：
```csharp
[Serializable]
public class SkillCfg : BaseVo {
    public int id;
    public string name;
    public int career;
    public float damage = 1.0f;
    public ParticleCfg[] particles;
}

[Serializable]
public class ParticleCfg {
    public string res;
    public int pos_type;
    public int attach_type;
}
```

#### 输出 2：配表读取代码

`Assets/Scripts/Generated/Config/ConfigSkill.cs`：
```csharp
public static class ConfigSkill {
    private static Dictionary<int, SkillCfg> _data;

    public static async Task LoadAsync() {
        var json = await ResManager.LoadAsync<TextAsset>("resource/config/server/config_skill");
        _data = JsonConvert.DeserializeObject<Dictionary<int, SkillCfg>>(json.text);
    }

    public static SkillCfg Get(int id) {
        return _data.TryGetValue(id, out var v) ? v : null;
    }

    public static IEnumerable<SkillCfg> All() => _data.Values;
}
```

#### 输出 3（可选）：Erlang 端 Schema 校验

ConfigGenerator 还可以**反向校验** yu_server 的 hrl 字段是否与 Schema 一致，
不一致时报警。这样三端对齐由工具保证，不依赖人记忆。

### 4.4 工作流

```
┌────────────────────────────────────────────────────────────┐
│ 策划编辑 JSON (在 yu_client 或 GM 后台 工具中)               │
│         ↓                                                  │
│ Python config_erlang.py 生成 data_*.erl  ─→  服务端编译/热更 │
│         ↓                                                  │
│ Unity ConfigGenerator 读取 JSON + Schema                   │
│   - 校验 JSON 字段是否符合 Schema                            │
│   - 生成 SkillCfg.cs / ConfigSkill.cs                      │
│   - 校验 Erlang hrl 是否对齐（可选）                         │
│         ↓                                                  │
│ Unity 客户端运行时通过 ResManager 加载 JSON → 强类型 Vo      │
└────────────────────────────────────────────────────────────┘
```

### 4.5 Schema 库管理

```
位置: Shenxiao/Tools/ConfigSchemas/
内容: 每张配表一个 .schema.json
版本控制: 跟随 Unity 项目一起 git
首次填充: ConfigGenerator 提供 "Bootstrap" 命令，
         扫描 yu_server hrl + field_mappings.json，
         自动生成 Schema 草稿
```

### 4.6 处理纯客户端配表

`Client*.json` / `Config*.json` 共 206 个，**不进 Erlang 流水线**，
也不需要 Schema 中的 erl 映射，照样用 Unity ConfigGenerator 生成 Vo。

### 4.7 必要时改动 yu_server

如果发现 hrl 字段与 JSON 字段对不上、或要新增字段：
- 在 yu_server 仓库**新建分支** 修改 hrl + record 默认值
- 同步更新 Schema
- Python 生成器跟着升级 field_mappings.json
- 经测试服验证后合并主干

---

## 五、工具链（Editor 工具）

### 5.1 资产转换器（AssetConverter）

```
LhConverter         ← 入口（递归处理）
├── LmConverter     ← .lm  → Unity Mesh (.asset)
├── LaniConverter   ← .lani → AnimationClip (.anim)
├── LmatConverter   ← .lmat → Material (.mat)
└── ParticleConverter ← .lh 中的 ShuriKenParticle3D → ParticleSystem（后续完善）
```

**实现要点**：
- .lm 二进制解析参考 `cdn/libs/laya.d3.js` 的 `MeshReader.read`
- .lani 格式见 `lani-format-analysis.md`（已分析）
- 路径映射：`resource/object/role/objs/xxx.lh` → `Assets/GameRes/resource/object/role/objs/xxx.prefab`
- Phase 0 的 LhConverter 只要求还原层级、网格、材质、骨骼动画；粒子先生成占位或基础映射
- ParticleConverter 完整还原放到 Phase 2/3，根据技能和特效优先级逐步补齐

### 5.2 UI 生成器（蓝湖优先）

```
主输入：蓝湖设计数据 / 导出资源
历史输入：yu_client/h5/laya/pages/resource/game/{module}/*.scene（旧 UICreator 试验，2026-06-09 已清理）
输出：Assets/Prefabs/UI/{Module}/*.prefab
       + 自动生成绑定脚本 {ViewName}Bind.cs
       + 缺图 / 缺字段 / 资源映射报告
```

旧 Laya `UICreator` 的实体工具、模板 prefab、`LayaSourceInfo`、Login 试点 prefab 和旧 login 资源已清理；后续 UI 主路线以蓝湖为视觉和资源标准。下面的组件映射、Bind 生成、模板化构建思想保留为能力模型，具体字段和输入格式以蓝湖接入工具重新定义。

**组件映射表**：

| LayaAir | Unity UGUI |
|---------|------------|
| View | Canvas/Panel + RectTransform |
| Image | Image (skin → Sprite 引用) |
| Label | TextMeshProUGUI |
| Button | Button + Image |
| TextInput | TMP_InputField |
| List | ScrollRect + 虚拟列表 |
| Tab | ToggleGroup |
| Box | empty RectTransform |
| HBox/VBox | Horizontal/VerticalLayoutGroup |
| ProgressBar | Slider |
| CheckBox | Toggle |
| Clip | RectMask2D |
| ComboBox | TMP_Dropdown |

**绑定脚本生成**：

```csharp
public partial class LoginViewBind : BaseView {
    public Button _btn_login;
    public TMP_InputField _input_account;
    public Image _img_logo;

    protected override void BindNodes() {
        EnsureBound(nameof(_btn_login), _btn_login);
        EnsureBound(nameof(_input_account), _input_account);
        EnsureBound(nameof(_img_logo), _img_logo);
    }
}
```

LanhuCreator 在 Editor 阶段把字段引用写入 Prefab；运行时只做空引用校验，不使用 `transform.Find`。业务代码继承 `LoginViewBind`，访问字段更安全也便于跳转。

**渲染骨架：模板驱动（Template-based）**

蓝湖 UI 生成器不应在代码里散落 `new GameObject + AddComponent` 拼组件，而应延续“模板 prefab + 字段 override + 可重跑生成”的方式。旧 `Assets/Editor/UICreator/Templates/` 已删除；蓝湖接入时重新建立模板目录，模板命名以蓝湖节点类型和 Unity 组件映射为准。

```
Assets/Editor/LanhuCreator/Templates/
  View.prefab         空 RectTransform，按蓝湖画布尺寸生成
  Box.prefab          空 RectTransform
  Image.prefab        Image，raycastTarget=false
  Label.prefab        TextMeshProUGUI（字体/对齐/材质在此挂）
  Button.prefab       Image + Button（过渡可改）
  TextInput.prefab    Image + TMP_InputField + Text Area + Placeholder
  ProgressBar.prefab  三层（Background/Fill Area/Fill）+ Slider
  CheckBox.prefab     Image + Toggle + Checkmark
  Clip.prefab         Image + Mask
```

转换流程：

```
.scene 节点
  ↓
Instantiate(LayaXxx.prefab) → Unpack
  ↓
override 字段（pos/size/scale/text/skin/sizeGrid/visible/...）
  ↓
SpriteResolver 二次反填 sprite + spriteBorder
```

调样式（Label 字体/描边、Button 过渡、ProgressBar 配色...）只改对应模板 prefab，不改代码、不重转。新转换的 prefab 自动继承新模板。

运行时业务代码只切 UI 状态、填充数据和绑定事件；除非明确要求，禁止在业务代码中改颜色、尺寸、位置、字体、描边、过渡等样式参数，避免后续 UI 调整被代码反向覆盖。

模板由菜单 `Shenxiao/UI/Build UI Templates` 一键生成（幂等，不会覆盖已有 tweak）。

### 5.3 SpriteImporter

```
1. 扫描 yu_client/cdn/resource/game/ 下所有 png/jpg
2. 复制到 Assets/GameRes/resource/game/{module}/texture/
3. 自动设置 TextureImporter:
   - Texture Type = Sprite (2D and UI)
   - Pixels Per Unit = 100
   - Filter Mode = Bilinear
4. 按模块创建 SpriteAtlas（与 fileconfig.json 分组对应）
5. 自动加入 Addressable Group: UI_{Module}
```

### 5.4 TS2CS 翻译工作流

不是真翻译器，是**批处理工作流**：

```
扫描 yu_client/h5/src/{module}/{Module}Model.ts 和 {Module}Vo.ts
  → 提取类结构（字段、方法签名、依赖）
  → 用模板 + AI prompt 翻译为 C#
  → 输出到 Assets/Scripts/Generated/{Module}/
  → 人工 review 修改
```

**适合批量翻译**：
- `XxxVo.ts`（纯数据类）
- `XxxModel.ts`（纯逻辑类）
- `ConfigXxx.ts`（→ 用 ConfigGenerator 替代，不再手翻）

**不要批量翻译**：Controller（涉及 UI 引用）、View（引擎依赖深）。

### 5.5 AddressableSetup

```
1. 扫描 Assets/GameRes/ 下所有资源
2. 按规则分配到 Group:
   - UI 图片 → UI_{module}
   - 3D 模型 → Model_{type}
   - 特效 → Effect_{type}
   - 配置 → Config
   - 音频 → Sound_{type}
3. Addressable Key = 资源相对路径（保持与 LayaAir 一致）
   例: resource/game/role/texture/role_001
4. 设置 Group 为 Remote
```

### 5.6 工具优先级

```
Phase 0 必须完成：
  AssetConverter (.lm/.lani/.lmat/.lh)
  SpriteImporter
  AddressableSetup
  LanhuCreator / UI 生成工具 (基础版)
  ConfigGenerator (Schema → C# Vo)

Phase 1 完善：
  LanhuCreator / UI 生成工具 (完整组件 + 绑定脚本 + 缺图报告)
  TS2CS 工作流
  ConfigGenerator (反向校验 hrl)

Phase 2/3 按需：
  ParticleConverter (粒子参数细化)
  各种批处理工具
```

---

## 六、资源远程加载方案

### 6.1 Addressables 配置

```
Groups:
  Local/
    Built-In Data           # 启动场景、Loading UI
  Remote/
    UI_{module}             # 各模块 UI（按需加载）
    Model_{type}            # 3D 模型
    Effect_{type}           # 特效
    Config                  # 全部配置 JSON
    Sound_{type}            # 音频

Profile: Production
  RemoteLoadPath: RuntimeFromResourceApi/[BuildTarget]
  RemoteBuildPath: ServerData/[BuildTarget]
```

`RemoteLoadPath` 不写死 CDN 域名。启动时先请求资源版本 API，由服务端返回当前环境、渠道、平台对应的 CDN 地址和 Catalog 地址。

资源版本 API 示例：
```json
{
  "code": 0,
  "data": {
    "env": "prod",
    "platform": "WebGL",
    "resourceVersion": "20260428_001",
    "cdnBaseUrl": "https://cdn.example.com/shenxiao/aa/WebGL/",
    "catalogUrl": "https://cdn.example.com/shenxiao/aa/WebGL/catalog_20260428_001.json"
  }
}
```

客户端启动时：
1. 读取包内 `AppConfig`，拿到资源版本 API 地址
2. 请求资源版本 API
3. 设置 Addressables 的远端加载地址（通过 Catalog URL / InternalIdTransform）
4. 初始化并更新 Catalog

### 6.2 ResManager 接口

```csharp
public static class ResManager {
    public static Task<T> LoadAsync<T>(string addrKey) where T : Object;
    public static Task<GameObject> InstantiateAsync(string addrKey, Transform parent = null);
    public static void Release(Object asset);
    public static void ReleaseInstance(GameObject go);
    public static Task PreloadGroup(string label);
    public static Task<long> GetDownloadSize(IEnumerable<string> keys);
    public static Task DownloadAsync(IEnumerable<string> keys, Action<float> onProgress);
}
```

运行时禁止暴露同步加载接口，WebGL 和远程 Addressables 全部走异步。
若 Editor 调试需要同步读取，只能放在 `#if UNITY_EDITOR` 工具代码中。

### 6.3 GameResPath 保持兼容

返回 Addressable Key。外部传入的 LayaAir 路径先统一 Normalize：去 CDN 前缀、统一 `/`、去扩展名。

```csharp
public static class GameResPath {
    public static string GetIcon(string module, string resName)
        => $"resource/game/{module}/texture/{resName}";

    public static string GetGoodsIcon(int id)
        => $"resource/game/goodsIcon/{id}";

    public static string GetEffectPath(string type, string name)
        => $"resource/effect/objs/{type}/{name}";

    public static string GetServerConfigPath(string cfg)
        => $"resource/config/server/{cfg}";

    public static string GetClientConfigPath(string cfg)
        => $"resource/config/client/{cfg}";
}
```

路径规范：
```
resource/game/role/texture/icon.png   → resource/game/role/texture/icon
resource/game/role/texture/icon.jpg   → resource/game/role/texture/icon
https://cdn.xxx.com/resource/.../a.png → resource/.../a
\resource\game\a.png                 → resource/game/a
```

UI 生成工具、ResManager、ConfigManager 都必须调用同一个 `ResourcePath.Normalize()`，
避免设计数据/导出资源中带扩展名、GameResPath 中不带扩展名导致资源引用失败。

### 6.4 启动流程

```
1. 进入 Launch 场景（包内）
2. 显示 Loading UI（包内）
3. 请求资源版本 API，拿到 cdnBaseUrl/catalogUrl/resourceVersion
4. 初始化 Addressables 并加载远端 Catalog
5. 检查 Remote Catalog 版本
6. 必要时下载更新（增量）
7. 加载 Login UI 等启动业务资源（Remote）
8. 跳转 Login 场景
```

### 6.5 WebGL 特殊处理

| 问题 | 应对 |
|------|------|
| 首屏白屏 | Custom WebGL Template，加载页带进度条 |
| 缓存策略 | Catalog 用 Hash，资源 URL 自动带版本 |
| CORS | cdn 服务器配置 `Access-Control-Allow-Origin` |
| 包体压缩 | Brotli 压缩 + 启动期资源精简到 5MB |
| 内存 | WebGL Heap 256MB 起，按需调整 |

### 6.6 包体目标

| 平台 | 包体/首包 |
|------|:---------:|
| WebGL | <8MB (压缩) |
| Android | <30MB (APK) |
| iOS | <30MB (IPA) |

完整资源放 cdn，预计 1~3GB（按需下载）。

---

## 七、实施时间线

```
M0: 框架空壳   ─── 工程目录、asmdef、Addressables 配置
M1: 协议通了   ─── 能连服务器，收发一条协议
M2: 工具齐了   ─── 4 个转换器 + LanhuCreator + ConfigGenerator
M3: 公共模块   ─── 红点/音频/Tips/Loading/Effect 全部就位
M4: 能登录     ─── Login + MainUI，能进入主界面
M5: 能跑能打   ─── Role/Skill/Bag/Equip + 战斗表现
M6: 能运营     ─── 商城/充值/活动/社交
M7: 全模块     ─── 211 个模块全部对齐
M8: 上线       ─── 切换 Unity 客户端
```

---

## 八、风险与规避

| 风险 | 应对 |
|------|------|
| WebGL 包体超标 | 严格 Addressable 分组，启动期资源审计 + Code Stripping |
| WebGL 性能差 | URP 简化 / 关闭后处理 / 减少 Draw Call |
| Erlang 协议解析错误 | 单元测试覆盖每种 term 类型；与 LayaAir 客户端对比抓包 |
| 配表 Schema 与 Erlang hrl 不一致 | ConfigGenerator 反向校验 + CI 检查 |
| 粒子映射不完美 | ShuriKen → Unity ParticleSystem 差异大，必要时手动微调 |
| 大列表性能 | 用虚拟列表（SuperScrollView 或自写 LoopList） |
| Addressables 增量更新失效 | 每次发版前测试增量场景 |
| .lm 骨骼权重精度 | Half-float 压缩验证蒙皮，必要时回退非压缩 |

---

## 九、参考项目用法

| 项目 | 路径 | 在重构中的角色 |
|------|------|--------------|
| yu_client | D:/GitProject/yu_client | LayaAir 客户端，**仅参考代码逻辑和资源**，不修改 |
| yu_gm | D:/GitProject/yu_gm | GM 后台，**仅参考接口定义**，不修改 |
| yu_server | D:/GitProject/yu_server | Erlang 服务端，**必要时新建分支修改**（hrl/协议/配置 schema 不一致时） |
| yu_client/tools/yu-resource-tool | python 工具链 | **直接复用** JSON ↔ Erlang 生成器 |

### 9.1 项目级记忆（2026-06-09）

- `yu_client` 是老项目和重构来源，**不放弃**；后续仍作为业务逻辑、协议调用、运行时 UI 行为、资源路径和配置流水线的主要参考。
- Shenxiao 的核心目标是**重构 Unity 客户端**，不是重构 `yu_server`。协议号、格式串、字段顺序、字段含义、收发时机原则上照抄 `yu_client`，客户端适配既有 Erlang 服务端；只有确认旧协议无法满足 Unity 客户端运行或存在服务端/配置不一致时，才按决策点单独报告调整。
- 不再把“直接复制 yu_client / LayaAir 运行时 prefab 结果”作为主交付路线。原因是大量 UI 图、列表、状态和皮肤由 Laya 运行时代码生成，照搬最终 prefab 会持续漏动态内容。
- UI 主线调整为：以蓝湖设计稿/导出资源作为视觉与资源标准，在 Unity 侧生成可维护的 `Prefab + Bind + 缺图报告`，再接入 yu_client 对应业务逻辑。
- UI 样式不写进业务代码；样式变化优先改蓝湖、模板 Prefab、Prefab 或 UICreator 输入，业务代码只处理状态、数据、事件和必要显隐。
- 正式项目不写 mock/fake/stub 数据或假接口，除非明确要求；登录、选服、角色和进游戏链路必须接真实 HTTP/协议数据。
- 蓝湖转 Unity 的工具应复用旧 UICreator 试验沉淀出的能力模型：模板 prefab、Bind 生成、Sprite 绑定、Addressable key 规范、缺失资源报告；不复用已删除的旧实体工具，输入源改为蓝湖设计数据/导出资源。
- 图片资源后期统一从 CDN 加载；Editor 阶段可以挂本地 Sprite 用于预览，但运行时必须通过 `ResManager + GameResPath/ResourcePath` 和 Addressables Remote 加载。
- 设计图中有功能区域但缺少图片资源时，不允许静默用错误资源替代；生成器必须输出缺图清单，推动 UI/美术补资源。
- 后续推进原则：先确认 Unity 工程骨架、启动链路、Addressable key、包体约束没有硬阻塞，再对接蓝湖。

---

## 十、近期任务（按顺序执行）

1. Shenxiao 工程：安装 Addressables / Newtonsoft.Json
2. 创建目录结构（_App / GameRes / Prefabs / Scripts / Editor）
3. 创建 Assembly Definition 文件
4. 定义 AppConfig + 资源版本 API 返回结构
5. 配置 Addressables Profile（Local + Remote，远端地址运行时由 API 注入）
6. 启动场景 + 极简 Loading UI
7. 框架层骨架代码（空实现，类/接口齐全）
8. 协议层完整实现 + 单元测试
9. .lm/.lani/.lmat/.lh 基础转换器（粒子先占位）
10. LanhuCreator / UI 生成工具基础版
11. ConfigGenerator + Schema Bootstrap（从 yu_server hrl 半自动生成）
12. 公共模块逐个实现

完成 1~12 即 Phase 0 完结，进入 Phase 1。
