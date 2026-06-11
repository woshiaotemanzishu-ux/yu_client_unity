# LayaUI 转换流水线(yu_client .scene → UGUI prefab)

> 2026-06-10 建立。解决「Laya 界面 → Unity 预制体」的批量转换:
> 一个逻辑界面 = 一个 prefab,列表项内联,不再是 1981 个散件。

## 粒度规则(已由分析器自动决策)

这个项目的 .scene 引用关系不在 scene 文件里,而在 TS 代码里
(全项目 ~1900 个类统一用 `base_file` + `layout_file` 声明自己的 scene,
列表项由宿主代码 `new XxxItem(parent)` 运行时拼装)。所以粒度由
**代码引用拓扑**决定,基类不可靠(achvView 这种主界面也继承 BaseItem1):

| 判定 | 决策 | 去处 |
|------|------|------|
| 窗口(BaseView1 链 / 设过 `layer_value`) | `view-prefab` | `Assets/Prefabs/UI/{Module}/{Name}.prefab` |
| 组件,恰好 1 个 UI 类引用 | `inline` | 宿主 prefab 内 `__Templates/{Name}`(禁用节点) |
| 组件,≥2 个 UI 类引用 | `shared-prefab` | 独立 prefab + 嵌套进各宿主 `__Templates` |
| 组件,只被控制器引用 | `standalone-prefab` | 独立 prefab |
| 代码无引用 / `*Skin` `*Exml` 换皮变体 / 孤儿 | 不转换 | manifest 标记,报告列出 |

2026-06-10 对 yu_client 全量分析结果:2056 个 scene →
**756 view + 161 standalone + 160 shared = 1077 个 prefab**,715 个 item 内联,264 个不转。

### 合并模式(2026-06-10 加,默认推荐)

上面的「窗口=prefab」试点后反馈仍太碎(login 一个模块 16 个文件)。合并模式把
**一个模块(或自定义大 Panel)收成一个 prefab**:

```
Assets/Prefabs/UI/Login/LoginModule.prefab
 ├─ LoginBgView          ← 各窗口为子节点,默认只激活第一个
 ├─ LoginLoadingView     (每个窗口子根挂自己的 {Name}Bind 组件)
 ├─ LoginView
 │   └─ __Templates/...  (该窗口的列表项模板,禁用)
 ├─ RegisterView
 └─ ...
```

- 默认整模块一组,名字 `{Module}Module`;想拆几个大 Panel,建 `Schemas/LayaUI/ui_groups.json`:

```json
{
  "login": [
    { "name": "LoginEntry",  "scenes": ["login/LoginBgView", "login/LoginLoadingView", "login/LoginView", "login/RegisterView"] },
    { "name": "LoginSelect", "scenes": ["login/LoginSelectServerView", "login/LoginSelectRoleView"] }
  ]
}
```

  没列进任何组的窗口仍按单窗口 prefab 转。
- 合并转换会**删除**该组窗口旧的单窗口 prefab(报告里有记录),避免两套并存。
- 改了某个窗口想重转:转换器窗口填 scene key → 「在合并 prefab 内重转该窗口」,
  只替换该子树,**其他窗口(含手调过的)不动**。
- 跨模块共享组件(`shared-prefab`)仍是独立 prefab,嵌套进各窗口 `__Templates`。

## 流水线

```
┌ yu_client ─────────────────────────────────────────────┐
│ h5/src/**/*.ts            cdn/resource/game/**/*.json   │
│ (类↔scene、引用拓扑)       (运行时净化版 scene,转换以它为准) │
│ h5/laya/assets/**         cdn/resource/UIConfig.json    │
│ (散图)                    + game/{module}/texture.png   │
└─────────────────────────────────────────────────────────┘
        │ ① python3 Tools/LayaUI/analyze_layaui.py [yu_client路径]
        ▼
Schemas/LayaUI/ui_manifest.json      ← 粒度决策,进 git
        │ ② Unity 菜单 神霄/LayaUI/转换器
        ▼
Assets/Prefabs/UI/{Module}/*.prefab  + Assets/GameRes/resource/game/...(图)
Assets/Scripts/Generated/UI/{Module}/{Name}Bind.cs
Reports/LayaUI/{module}_report.md    ← 缺图/近似/运行时赋值清单(不进 git)
```

## Unity 端操作步骤(本地,2026-06-11 改版后)

1. yu_client 仓库先 `git lfs pull`(图集与散图在 LFS)。
2. 打开 `神霄/LayaUI/转换器`:设置里配一次 yu_client 目录 + 中文 TMP 字体;
   之后**点模块按钮即一键流水线**(散图导入→模板→转换→编译后自动回填→
   Addressable 分组→报告)。Tab 按域分类,按钮中英对照
   (`Schemas/LayaUI/module_names_cn.json` 维护),右侧 ⚠N=缺图数、
   「验收」勾上后重转会弹确认。单窗口重转/预览/报告在「高级」折叠里。
3. 预览场景与 Laya 运行时截图比对;问题改转换器/默认表后幂等重转。

## 转换映射要点

- **坐标**:全部公式集中在 `LayaRectMath.cs`。统一 Unity pivot=(anchorX, 1-anchorY),
  锚父左上,`pos=(x,-y)`;`centerX/centerY/left/right/top/bottom` 转对应锚点,左右(上下)同给即拉伸。
- **尺寸**:Laya 不写宽高 = 贴图原始尺寸 / 文本自适应。Image 回退 sprite 尺寸,Label 用 TMP 测量。
  (全项目 3833 个 Image、5472 个 Label 不写宽高,这是以前转换"大小不对"的主因。)
- **九宫格**:Laya sizeGrid「上,右,下,左」→ Unity spriteBorder(左,下,右,上),写在 TextureImporter 上。
- **图**:散图直接拷;散图缺但在图集 → 按 UIConfig.json frame 从 texture.png 反裁;都没有 → 报告。
- **描边**:SDF outline 材质预设近似(`Assets/GameRes/Fonts/Materials`),报告标记待人工核。
- **HTMLDivElement**:`<br>`/`<font color>` 转 TMP 富文本,其余标签剥掉,报告标记。
- **List**:转出 ScrollRect 骨架(方向按 repeatX/repeatY),item 模板在 `__Templates`,
  虚拟列表逻辑归运行时框架,转换器不管。

## 正确的观察方式(2026-06-10 试点反馈后补)

1. **必须在 Canvas 下看**:直接打开 prefab 会漂在天空盒里,没有"屏幕"参照,看起来就像
   超屏/偏移。用转换器窗口的「创建 720×1280 预览场景」,Game 视图分辨率切 720x1280。
2. **窗口要叠着看**:Laya 运行时是多窗口叠层的——选服/进入/创角界面的全屏背景其实是
   底下的 `LoginBgView`(或运行时赋图)。预览场景里同时激活 LoginBgView + 当前窗口
   才是运行时的样子。
3. **进度条满格是正常的**:Laya 用 `_mask_box` 这类 Box 在运行时改宽度当进度遮罩,
   静态转换给的是满宽初始态,业务代码接进度后即正确。

## 运行时图静态烘焙(2026-06-10 加)

scene 里大量 `_img_bg` 等节点 skin 为空,图是 TS 运行时赋的。分析器现在静态扫描
`this._img_xxx.skin = "..."` / `SetTexture(this, this._img_xxx, GameResPath.GetIcon[Jpg]("m","n"))`
等字面量模式(首写胜),全项目解析出 ~300 处,写进 manifest 的 `bakedSkins`,
转换时烘焙回 prefab 并在报告标注「真实运行可能换图」。
动态的(模板串 `${id}`、平台 logo、随机背景列表)烘焙不了,仍是透明占位 + 报告。

## 已知边界(转出来 ≈ 静态初始态,预期 85~95% 还原)

- View.ts 运行时赋的图/文本:字面量已烘焙(见上),动态的不在 prefab 里
  (报告里有「运行时赋值」清单,无 skin 的 Image 转成 `enabled=false` 占位)。
- `animations` 时间轴、Laya 内置 `comp/` 皮肤组件交互(CheckBox 等)需手工补。
- Bind 脚本两步走:转换生成 cs → Unity 编译 → 回填引用(新脚本编译前无法 AddComponent)。

## 相关文件

- 分析器:`Tools/LayaUI/analyze_layaui.py`
- 转换器:`Assets/Editor/LayaUI/`(Settings / Manifest / RectMath / Templates / SpriteImporter / SceneConverter / TextStyles / BindGenerator / BindFiller / Window)
- 决策数据:`Schemas/LayaUI/ui_manifest.json`

## 现状总结与改进计划(2026-06-11 登录链路全通后定稿)

### 已沉淀的通用规则(地基,改动需谨慎)

1. 粒度:引用拓扑决定(窗口/内联/共享/不转),manifest 是唯一决策源;
2. 坐标:公式全部集中 `LayaRectMath`,相对布局用显示尺寸(×scale);
3. 尺寸:缺宽高回退贴图原始尺寸/TMP 测量;容器自动宽高逐轴统计;HBox/VBox=排列语义;
4. 列表项模板根一律左上锚定(`NormalizeItemRoot`);
5. 图源三级:散图(镜像→cdn)→ 图集反裁 → 报缺;sizeGrid「上右下左」→border「左下右上」;
6. 运行时图烘焙:GameResPath 模板自动推导 + 5 类赋图入口 + 别名/局部常量/模板串 glob
   + texture→other 重写;纠错走 `ui_default_skins.json`(强制覆盖);
7. Bind 收集:`_` 前缀 ∪ codeNodes(TS this.xxx 引用∩节点名);回填挂业务子类;
8. 文本:字面 \n 转义;描边 SDF 材质预设(近似,报告标注);
9. 窗口运行时模型:模块流程类管子窗口,`BaseView.Show()` 置顶、背景垫底;
10. 业务交互统一 `UIUtil.AddClick`;隐藏可点击元素用 alpha,不用 enabled(射线会失效);
11. 报告不静默:缺图/近似/运行时赋值/未映射属性全落 Markdown。

### 已知薄弱点(诚实清单,排期处理)

- **双生成器风险**:Bind 类有 python 镜像生成(为提交编译用)与 C# 生成器两套,
  规则改动必须双改 → 应去掉 python 镜像,以 C# 为唯一事实源,产物直接提交;
- 烘焙"首写胜"选错分支只能靠人工发现后进默认表(选服 bg11/bg1 是案例);
- 重转覆盖手调:验收后的手调应优先落模板/默认表/业务代码;prefab 手调需记录,
  重转前确认(组内单窗口重转可保护其他窗口);
- `_tpl` 模板内部取节点仍是 transform.Find(待 ItemBind 生成);
- Label 测量依赖转换时字体,换字体需重转;
- codeNodes 求交可能收进同名非节点成员(字段冗余,无害但留意);
- 静态分析边界:动态 layout_file、跨文件常量、字符串拼接不可烘,以报告为准。

### 转换器 UI 改版方案(已确认方向,待实施)

现窗口操作步骤多(模板/转换/回填/散图/分组五连点)。改为:

```
┌ LayaUI 转换器 ──────────────────────────────┐
│ [设置] yu_client 目录 | 中文字体 |(持久化)   │
│ ┌Tab: 核心 │ 战斗 │ 社交 │ 活动 │ ...─────┐ │
│ │  [login 登录]   [mainUI 主界面] ←中英对照 │ │
│ │  [bag 背包]     [role 角色]   点击=全自动 │ │
│ └────────────────────────────────────────┘ │
│ 一键 = 散图导入→转换→编译后自动回填→分组→报告 │
└─────────────────────────────────────────────┘
```

- 模块中英对照:`Schemas/LayaUI/module_names_cn.json`(手工配,缺省显示英文);
- 「编译后自动回填」用 EditorPrefs 排队 + [DidReloadScripts] 续跑,消除两步操作;
- 验收状态:模块按钮显示 ✅/⚠(报告里"需人工确认"计数),验收过的模块重转弹确认。

### 流程规矩(量产期执行)

转换 → 过报告清单 → 预览场景比对 → 接业务逻辑 → 模块标记验收 → 锁定
(锁定模块重转需确认)。样式改模板、图改默认表/美术、布局改源头,prefab 手调最后手段。

## 模块量产 Playbook(2026-06-11,login 模块全流程验收后定稿)

login 模块(13 个窗口、9 个内联模板、完整业务链)证明了分工模型,量产照此执行:

**工具负责(点按钮自动)**:布局/尺寸/坐标、静态图与烘焙图、Bind/ItemBind 生成回填、
模板根归一、报告。结果不对 → 修转换器/分析器/默认表,**永远不点杀产物**。

**业务代码负责(每模块一次性)**:
1. 模块流程类(仿 LoginFlow):窗口编排、阶段切换、互斥显隐(分流前先 Hide 全部相关窗);
2. 业务 View 类(继承 Bind):数据填充、UIUtil.AddClick、列表 Instantiate(_tpl)+ItemBind;
3. 协议:格式串照抄 yu_client 的 SendFmtToGame/ReadFmt,回包 schema 复杂的做成
   数据表+通用读取器(FigureProto 范式);
4. 运行时动态图:ResManager.SetImageAsync(Laya SetTexture 对等)。

**行为对齐方法论(三次返工换来的,必须执行)**:
- 对齐某个界面前,把该 View 的 TS **全部事件绑定读完**(InitEvent/BindEvent/OPEN_VIEW
  监听/阶段切换方法),列成清单再动手——协议弹层(InitAgreementAgreeState +
  AGREE_LOGIN_ALERT)、背景阶段切换(ChangeRoleStatus/UpdateView)都藏在绑定里,
  只看主流程必漏;
- 状态的持久化口径(内存/cookie/按账号)以源码为准,不猜;
- UI 上"多出来/少掉"的元素,先查老客户端同阶段的显隐控制,再考虑是不是转换问题。

**已知系统性边界(量产时直接归档,不算模块问题)**:
3D 展示位(SetRoleModel/_gp_model 类容器)、Laya 时间轴动画、粒子,分别归
3D 转换线与特效线;报告里见到这类节点直接标注跳过。
