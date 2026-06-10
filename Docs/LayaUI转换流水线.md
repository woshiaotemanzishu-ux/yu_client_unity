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

## Unity 端操作步骤(本地)

1. yu_client 仓库先 `git lfs pull`(图集与散图在 LFS)。
2. 打开 `神霄/LayaUI/转换器`:
   - 确认 yu_client 路径;
   - 指定中文 TMP 字体(ttf 放 `Assets/GameRes/Fonts`,Font Asset Creator 生成。不配中文显示方块);
   - 点「生成 / 补齐 UI 模板」(模板在 `Assets/Editor/LayaUI/Templates`,Label 字体、描边、List 弹性等样式都在模板上调,调完重转自动继承);
   - 模块名填 `login`,点「转换整个模块」;
   - 等编译完,点「回填 Bind 引用」。
3. 打开 prefab 与 Laya 运行时截图半透明叠加比对;问题记到对应 scene,改转换器后可幂等重转。

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

## 已知边界(转出来 ≈ 静态初始态,预期 85~95% 还原)

- View.ts 运行时赋的图/文本不在 prefab 里(报告里有「运行时赋值」清单,无 skin 的 Image 转成 `enabled=false` 占位)。
- `animations` 时间轴、Laya 内置 `comp/` 皮肤组件交互(CheckBox 等)需手工补。
- Bind 脚本两步走:转换生成 cs → Unity 编译 → 回填引用(新脚本编译前无法 AddComponent)。

## 相关文件

- 分析器:`Tools/LayaUI/analyze_layaui.py`
- 转换器:`Assets/Editor/LayaUI/`(Settings / Manifest / RectMath / Templates / SpriteImporter / SceneConverter / TextStyles / BindGenerator / BindFiller / Window)
- 决策数据:`Schemas/LayaUI/ui_manifest.json`
