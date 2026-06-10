# Shenxiao 编码规范（AI 编码必读）

> 本文是 AI 编码的硬约束。所有由 AI 产出的 Unity C# 代码、Editor 工具、Schema、Prefab 命名都必须遵守。
> 与 `Shenxiao重构实施方案.md` 配套使用：方案管"做什么"，本规范管"怎么写"。
>
> **关键原则**：宁可少写不要乱写。模板/约定已有的就照抄，没有的就先停下问，不要发明新风格。

---

## 〇、AI 编码工作流（每次任务必做）

```
1. 读 Shenxiao重构实施方案.md 对应章节，确认任务边界
2. 读本规范的相关小节
3. 找现有同类文件作为模板（90% 情况都有参考）
4. 写代码 / 写工具 / 写 Schema
5. 自检清单（见 §十一）
6. 报告改了哪些文件、是否新增了 asmdef 引用、是否引入新依赖
```

**禁止行为**：

- ❌ 凭印象写"看起来 Unity 风"的代码，绕过框架自己 new GameObject、自己 Resources.Load
- ❌ 大功能一上来就手写固定逻辑，把活动、Boss、UI 数据、奖励、开放时间等写死在代码里
- ❌ 未经明确要求，在业务代码里写 UI 样式变化（颜色、尺寸、位置、字体、描边、过渡、显隐动画参数等）；样式应在蓝湖、Prefab、模板 Prefab 或 UICreator 输入中调整
- ❌ 未经明确要求，用 mock/fake/stub 数据或假接口替代正式链路；正式项目默认接真实配置、真实 HTTP/协议和真实资源
- ❌ 给老代码顺手加注释、加 null 检查、加 try-catch（不是任务的部分一律不动）
- ❌ 引入新的 NuGet/UPM 包（必须先报告）
- ❌ 在 Runtime 代码里写 `#if UNITY_EDITOR` 之外的同步资源加载
- ❌ 用反射/动态代码替代显式接口（除非框架本身的工具层）
- ❌ 复制 LayaAir TS 写法的 `any` / 隐式类型，C# 一律强类型

**系统级记忆**：Shenxiao 的大功能优先走“配置驱动 + 工具链先行”。AI 在写 Boss 活动、运营活动、商城、排行榜、养成系统、主界面入口等大功能前，必须先判断配置结构、Schema、生成工具、资源命名、Addressable key 和通用运行时骨架是否已经存在；没有就先提出方案，不要直接写死业务数据。正式项目默认不写 mock/fake/stub 数据或假接口，除非用户明确要求临时验证。

---

## 一、目录与归属

### 1.1 新文件落点（决定树）

```
是 Editor 工具？            → Assets/Editor/{ToolName}/
是 AI 翻译产出？            → Assets/Scripts/Generated/{Module}/  （不要手改）
是框架层（不依赖业务）？     → Assets/Scripts/Framework/{SubSystem}/
是公共模块（依赖框架）？     → Assets/Scripts/Common/{ModuleName}/
是业务模块？                → Assets/Scripts/Module/{Module}/
是启动期资源？              → Assets/_App/...
是远端资源（图/模型/特效）？ → Assets/GameRes/...
是 UI 预制体？              → Assets/Prefabs/UI/{Module}/
```

### 1.2 Asmdef 归属

| 代码类型 | asmdef |
|----------|--------|
| Framework/* | Shenxiao.Framework |
| Generated/* | Shenxiao.Generated |
| Common/* | Shenxiao.Common |
| Module/Login, MainUI, Role, Equip, Bag, Pet, Skill, Mail, Friend ... | Shenxiao.Module.Core |
| Module/Scene, Combat, SkillEffect, Particle ... | Shenxiao.Module.Combat |
| Module/Guild, Chat, Marriage, Team ... | Shenxiao.Module.Social |
| Module/Activity, Shop, Recharge, Operation ... | Shenxiao.Module.Activity |
| Editor/* | Shenxiao.Editor |

**依赖方向**：`Framework ← Generated ← Common ← Module.* ← Editor`（左侧被右侧引用，不可反向）。

**新增 asmdef 必须先报告**，不要自己拍脑袋拆。

### 1.3 命名空间

```csharp
namespace Shenxiao.Framework.Net { }
namespace Shenxiao.Framework.UI { }
namespace Shenxiao.Common.RedDot { }
namespace Shenxiao.Module.Login { }
namespace Shenxiao.Editor.AssetConverter { }
```

文件路径与命名空间一一对应。

---

## 二、C# 编码风格

### 2.1 命名

| 类型 | 规则 | 示例 |
|------|------|------|
| 类 / 结构 / 枚举 | PascalCase | `LoginView`, `SkillCfg` |
| 接口 | I + PascalCase | `IResLoader` |
| 公有方法 / 属性 | PascalCase | `LoadAsync`, `IsReady` |
| 私有字段 | _camelCase | `_resManager`, `_loaded` |
| 局部变量 / 参数 | camelCase | `addrKey`, `result` |
| 常量 | UPPER_SNAKE | `MAX_RETRY` |
| 事件常量 | UPPER_SNAKE | `EVT_LOGIN_SUCCESS` |
| Addressable Key | 路径风格 | `resource/game/role/texture/icon_001` |
| Prefab 文件 | PascalCase + View 后缀 | `LoginView.prefab` |
| 配表 Vo | PascalCase + Cfg 后缀 | `SkillCfg`, `EquipCfg` |
| 配表读取类 | Config + 表名 | `ConfigSkill`, `ConfigEquip` |

UI 节点名（在蓝湖 manifest / Prefab 中）：`_btn_login`, `_input_account`, `_img_logo`, `_txt_name`, `_list_item`, `_panel_root`。
蓝湖 UI 生成工具按这套前缀生成 Bind 字段。

### 2.2 强类型与可空

- 字段必须显式类型，禁止 `var` 用在公有 API；局部 `var` 仅当右侧类型一目了然
- 引用类型字段默认认为可能为 null，访问前必要时显式判空
- **不要**乱加 `?.` 链式调用掩盖错误；框架内部错误就抛
- 数值/布尔字段必须有合理默认值（`int = 0`, `bool = false` 显式写出来）

### 2.3 异步

- 全局异步使用 `async Task` / `async Task<T>`，禁止 `async void`（事件订阅唯一例外）
- Addressables / 网络 / 配表加载一律 await
- UI 主线程操作不使用 `ConfigureAwait(false)`
- Editor 工具可使用 `Task.Run` 做 IO，但不要在 Runtime 用

### 2.4 异常

- 框架层：参数错误用 `ArgumentException`，状态错误用 `InvalidOperationException`
- 业务层：能本地处理就本地处理，不能就让它抛到框架边界
- **禁止** 空 catch / 吞异常 / `catch (Exception) { Debug.Log(...); }`
- 所有可预期错误（网络断开、资源不存在）通过返回值/事件传递，不靠异常控制流

### 2.5 日志

```csharp
GameLog.Info("Login", "user={0} succeed", uid);
GameLog.Warn("Res", "missing addr key={0}", key);
GameLog.Error("Net", "decode failed proto={0}", protoId);
```

- 统一 `GameLog`（框架层封装），不直接 `Debug.Log`
- 业务模块第一参数是模块标签（`Login` / `Bag` / `Skill`）
- Release 构建会按等级裁剪，禁止字符串拼接日志（用格式串）

---

## 三、框架层使用规范

### 3.1 资源加载（ResManager）

```csharp
// ✅ 正确
var icon = await ResManager.LoadAsync<Sprite>(GameResPath.GetGoodsIcon(1001));
var go = await ResManager.InstantiateAsync(GameResPath.GetEffectPath("skill", "fire"), parent);

// ❌ 禁止
var sprite = Resources.Load<Sprite>("...");
var ab = AssetBundle.LoadFromFile("...");
var go = Object.Instantiate(prefab); // 来源不是 ResManager 的禁止
```

- Addressable Key 永远从 `GameResPath` 取，禁止字符串拼路径
- 路径输入先过 `ResourcePath.Normalize()`（去 CDN 前缀、统一 `/`、去扩展名）
- `Release` / `ReleaseInstance` 必须配对调用，View 在 `OnDispose` 里释放
- Editor 同步加载只能写在 `#if UNITY_EDITOR` 块里

### 3.2 UI（BaseView / ViewManager）

- View 必须继承 `BaseView`（或由蓝湖 UI 生成工具生成的 `XxxBind` 间接继承）
- 打开/关闭只能走 `ViewManager.Open<T>(args)` / `ViewManager.Close<T>()`
- 节点引用必须用蓝湖 UI 生成工具生成的字段（`_btn_xxx`），禁止运行时 `transform.Find`
- 业务 View 只负责状态、数据、事件和必要显隐；除非明确要求，不要在运行时代码里改颜色、尺寸、位置、字体、描边、过渡等 UI 样式
- 事件订阅在 `OnShow`，反订阅在 `OnHide`，资源释放在 `OnDispose`
- 不要把业务数据存在 View 上，View 只渲染；数据放对应 Module 的 Model

### 3.3 协议（Net）

```csharp
// 注册
RegisterProtocal(Proto.SC_LOGIN, OnLogin);

// 发送
SendFmt(Proto.CS_LOGIN, "ss", account, password);

// 处理
private void OnLogin(ErlangTerm term) { ... }
```

- 协议号常量在 `Proto.cs` 集中定义，不准散落
- BaseController 子类承担 Register / Handle，Model 不直接订阅协议
- Erlang term 解析全部走 `ErlangParser`，禁止自己写字节解码
- 协议发送必须走 `NetManager`，禁止直接 `WebSocket.Send`

### 3.4 事件（EventDispatcher）

```csharp
EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE, bagId);
```

- 事件常量集中放 `GlobalEvent.cs`，前缀 `EVT_`
- On/Off 必须配对，禁止 lambda 注册（无法 Off）
- 跨模块通信优先事件，禁止模块间直接互相引用 Controller

### 3.5 配表（ConfigManager）

```csharp
await ConfigSkill.LoadAsync();
var cfg = ConfigSkill.Get(skillId);
foreach (var c in ConfigSkill.All()) { ... }
```

- 配表读取类一律由 ConfigGenerator 生成，**不准手写、不准手改**
- 读不到 key 返回 `null`（业务自处理），不抛异常
- 加载时机统一在启动流程里集中 await，禁止运行时按需加载（除非显式标注 lazy）

### 3.6 状态机 / 场景

- `Character` 子类（`Role` / `Monster` / `Npc`）必须用 `StateMachine`，禁止 if-else 状态判断
- 场景对象生命周期由 `SceneObj` 管理，业务不直接 `Destroy`

---

## 四、UI 开发规范

### 4.1 蓝湖 → Prefab 流程

1. 准备蓝湖导入包：`lanhu_manifest.json + assets/`
2. 运行 LanhuCreator，产出：
   - `Assets/Prefabs/UI/{Module}/{ViewName}.prefab`
   - `Assets/Scripts/Generated/UI/{Module}/{ViewName}Bind.cs`
3. 业务代码继承 Bind 类：

```csharp
public partial class LoginView : LoginViewBind {
    protected override void OnShow() {
        _btn_login.onClick.AddListener(OnClickLogin);
    }
    protected override void OnHide() {
        _btn_login.onClick.RemoveAllListeners();
    }
}
```

4. **禁止改 `*Bind.cs`**（每次重新生成都会覆盖）

### 4.2 节点命名

| 前缀 | 含义 | UGUI 类型 |
|------|------|-----------|
| `_btn_` | 按钮 | Button |
| `_img_` | 图片 | Image |
| `_txt_` | 文本 | TextMeshProUGUI |
| `_input_` | 输入 | TMP_InputField |
| `_list_` | 列表 | ScrollRect |
| `_tab_` | 选项卡 | ToggleGroup |
| `_chk_` | 勾选 | Toggle |
| `_bar_` | 进度条 | Slider |
| `_dd_` | 下拉 | TMP_Dropdown |
| `_panel_` | 容器 | RectTransform |
| `_clip_` | 裁剪容器 | RectMask2D |
| `_box_h_` / `_box_v_` | 布局容器 | Horizontal/VerticalLayoutGroup |

不带前缀的节点不会生成绑定字段。

### 4.3 文本

- 显示文本一律走 `Lang.Get(key)` 或 TMP 默认值
- 写死中文文案只允许在临时占位（必须 `// TODO i18n`）
- 字体走统一字体引用，不要每个 View 配自己的字体

---

## 五、Addressables 规范

### 5.1 Key 规则

```
Key = 资源相对路径（不带扩展名，统一正斜杠）

✅ resource/game/role/texture/icon_001
✅ resource/object/role/objs/role_001
✅ resource/effect/objs/skill/fire_ball
✅ resource/config/server/config_skill

❌ Assets/GameRes/...      （绝对路径）
❌ icon_001.png            （只有文件名）
❌ resource\game\...       （反斜杠）
```

### 5.2 Group 划分

| Group 前缀 | 内容 |
|------------|------|
| `Local_*` | 启动场景、Loading、AppConfig |
| `UI_{module}` | 各模块 UI Prefab + 该模块图集 |
| `Model_{type}` | 3D 模型 prefab + mesh + 动画 |
| `Effect_{type}` | 特效 prefab |
| `Config` | 全部配置 JSON（一个 Group 即可） |
| `Sound_{type}` | 音频 |

- Local 永远不进 Remote Group
- 一个资源只能属于一个 Group
- AddressableSetup 工具自动归组，**手工拖拽归组要在 commit 信息里写明原因**

### 5.3 Profile

- `RemoteLoadPath` 不写死域名，运行时由资源版本 API 注入
- `RemoteBuildPath` 用 `ServerData/[BuildTarget]`
- 平台子目录由 BuildTarget 区分（WebGL / Android / iOS）

---

## 六、配表规范

### 6.1 数据流（不要逆行）

```
JSON（cdn/resource/config/）   ← 单一权威源
   ├─ Python 工具链 → data_*.erl  （服务端）
   ├─ ConfigGenerator → C# Vo + ConfigXxx.cs  （Shenxiao）
   └─ GM 后台直读
```

- **禁止**在 Unity 这边改 JSON 后只更新 C#，必须同步走 Python 流水线生成 erl
- **禁止**Shenxiao 自己生成 `data_*.erl`
- **禁止**手写 `SkillCfg.cs` / `ConfigSkill.cs`，必须由 ConfigGenerator 产出

### 6.2 Schema

`Tools/ConfigSchemas/{table_name}.schema.json`：

```json
{
  "table": "config_skill",
  "key_field": "id",
  "key_type": "int",
  "fields": [
    { "name": "id", "type": "int", "comment": "技能ID" },
    { "name": "name", "type": "string" },
    { "name": "damage", "type": "float", "default": 1.0 }
  ],
  "nested_types": { ... }
}
```

支持类型：`int / long / float / string / bool / int[] / string[] / {Type} / {Type}[]`。
新增类型必须先在 ConfigGenerator 里声明，不要直接写到 Schema。

### 6.3 Vo 字段命名

- 字段名严格沿用 JSON key（蛇形保留蛇形，不要驼峰化）
- 这是为了和 yu_server hrl record 字段对齐，校验工具靠这一致性

---

## 七、协议规范

### 7.1 Proto.cs 结构

```csharp
public static class Proto {
    // 登录
    public const int CS_LOGIN = 11001;
    public const int SC_LOGIN = 11002;

    // 角色
    public const int CS_ROLE_INFO = 12001;
    public const int SC_ROLE_INFO = 12002;
}
```

- `CS_` = 客户端 → 服务端，`SC_` = 服务端 → 客户端
- 协议号、格式串、字段顺序、字段含义和收发时机原则上照抄 `yu_client`，Shenxiao 只做 Unity 客户端适配，不主动重设计协议
- 协议号和 yu_server 的协议号严格对齐，新增前先在 yu_server 找对应号
- 同一组协议（请求 + 响应）相邻定义
- 发现旧协议无法满足 Unity 客户端运行，或需要 `yu_server` / `yu_gm` 配合时，必须先按“变更与新增决策点”报告，不得直接改协议或服务端

### 7.2 BaseController 模板

```csharp
public class LoginController : BaseController {
    protected override void Register() {
        RegisterProtocal(Proto.SC_LOGIN, OnLoginRsp);
    }

    public void RequestLogin(string acc, string pwd) {
        SendFmt(Proto.CS_LOGIN, "ss", acc, pwd);
    }

    private void OnLoginRsp(ErlangTerm term) {
        var code = term.Get<int>(0);
        // ...
    }
}
```

格式串字符（与 LayaAir 客户端一致）：
- `i` int32, `l` int64, `f` float, `d` double
- `s` 字符串, `b` 布尔, `a` 原子
- `[X]` 列表, `{X,Y}` 元组

---

## 八、Editor 工具规范

### 8.1 通用约束

- 工具脚本放 `Assets/Editor/{ToolName}/`
- 入口菜单统一前缀：`Shenxiao/{Tool}/...`
- 工具读写源资源路径用 `EditorPath` 常量集，禁止散落硬编码
- 跨平台路径用 `Path.Combine` + `.Replace('\\', '/')`
- 进度展示用 `EditorUtility.DisplayProgressBar`，结束 `ClearProgressBar`

### 8.2 资产转换器（AssetConverter）

- 入口：`LhConverter.ConvertFile(string lhPath)` / `ConvertDirectory(string dir)`
- 输出路径由输入路径机械映射，**禁止**额外猜测
- 转换中断必须能续跑（已存在则跳过 / overwrite 由参数控制）
- 单文件转换内出错只记录 + 跳过，不要终止整个批次

### 8.3 LanhuCreator

- 输入：蓝湖导入包（`lanhu_manifest.json + assets/`）
- 输出：Prefab + Bind.cs + 缺图报告（成对）
- 二次生成必须**完全覆盖** Bind.cs，不做 diff merge
- Prefab 已存在时：保留业务挂载的 MonoBehaviour（用 PrefabUtility 合并），刷新节点引用

### 8.4 ConfigGenerator

- Bootstrap 命令从 yu_server hrl + field_mappings.json 生成 Schema 草稿
- 正常生成命令读 Schema，输出到 `Assets/Scripts/Generated/Config/`
- 反向校验命令对比 hrl 字段与 Schema，不一致时 Console 报错并阻断 CI

---

## 九、Generated 代码约定

`Assets/Scripts/Generated/` 下所有文件：

- 文件头必须包含：

```csharp
// <auto-generated>
//   This file is generated by {ToolName}.
//   DO NOT EDIT. Re-run the tool to regenerate.
// </auto-generated>
```

- 工具产物必须可重复生成（同输入同输出，diff 友好）
- 业务侧通过 `partial class` 扩展，不要修改 Generated 文件本身

---

## 十、性能与平台约束

### 10.1 WebGL 必守

- 全异步加载，禁止任何 `WaitUntil`/`while(!done) yield`
- 禁止 `System.Threading.Thread`、阻塞型 `Task.Wait()`
- 文件 IO 走 Addressables，不直接 `File.ReadAllBytes`
- JSON 解析允许用 Newtonsoft.Json（包内已加），禁止反射密集型方案

### 10.2 GC

- 高频路径（Update / 网络回调 / 战斗结算）禁止：
  - `string` 拼接（用 StringBuilder 或预分配）
  - `LINQ`（用 for/foreach 显式遍历）
  - `new` 临时数组/列表（用对象池）
- 列表 UI 必须用虚拟列表

### 10.3 Draw Call

- UI 图集按模块切分（与 LayaAir 的 fileconfig.json 对齐）
- 同模块同图集内的 UI 元素优先用同一图集
- Image 不要随意 `Set Native Size` 后改 scale

---

## 十一、AI 自检清单（提交前必过）

```
[ ] 读了 Shenxiao重构实施方案.md 对应章节
[ ] 找了同类现有文件作模板（说出文件名）
[ ] 文件落点正确（§1.1）
[ ] asmdef 归属正确，没有引入新 asmdef 引用（如有，已报告）
[ ] 命名空间与目录一致
[ ] 没引入新的 UPM/NuGet 包（如有，已报告）
[ ] 资源加载走 ResManager + GameResPath
[ ] UI 节点引用用 Bind 字段，没用 transform.Find
[ ] UI 样式没有写进业务代码（除非用户明确要求）
[ ] 没写 mock/fake/stub 数据或假接口（除非用户明确要求）
[ ] 事件 On/Off 配对，没用 lambda
[ ] 没碰 Generated 目录的内容
[ ] 没改 Bind.cs / Vo.cs / ConfigXxx.cs（手写禁止）
[ ] 没有同步 Resources.Load / AssetBundle / File.Read
[ ] 没空 catch、没吞异常
[ ] 文本走 Lang.Get（除已标 TODO i18n）
[ ] 编译通过（脑内编译也算，提交时若环境允许跑一下）
[ ] 报告：动了哪些文件、为什么
```

---

## 十二、文档与注释

- **不要**给 AI 任务范围之外的代码加注释
- **不要**给 public API 之外的方法写 XML 文档
- public 框架 API 必须有简短 XML 文档（一句话说清做什么）
- 复杂算法/非显然的位运算必须有注释说明意图（不是说明语法）
- TODO 格式：`// TODO(模块/责任人?): 说明`

---

## 十三、变更与新增决策点（必须先报告，不要自决）

下面这些事 **AI 一律不许直接做**，必须先写明计划并等确认：

1. 新增 UPM 包 / NuGet 依赖
2. 新增 asmdef
3. 新增 Addressable Group / 修改 Profile
4. 修改 Generated 工具的输出格式
5. 修改 yu_server / yu_gm 的代码或配置
6. 修改协议号 / Proto.cs 中已有项
7. 修改 Schema 字段（特别是已上线表）
8. 修改启动流程
9. 引入新的设计模式 / 框架抽象层

报告格式：

```
计划：XXX
原因：XXX
影响范围：哪些文件 / 哪些模块 / 是否需要服务端配合
回滚方式：XXX
```

---

## 附：与方案文档的对应关系

| 本规范章节 | 方案文档章节 |
|-----------|-------------|
| §一 目录归属 | 三、3.1 / 3.2 |
| §三 框架使用 | 三、3.3 / 六 |
| §四 UI 规范 | 五、5.2 |
| §五 Addressables | 六 |
| §六 配表 | 四 |
| §七 协议 | 三、3.3 Net |
| §八 Editor 工具 | 五 |

冲突时以**方案文档为权威**，本规范同步修订。
