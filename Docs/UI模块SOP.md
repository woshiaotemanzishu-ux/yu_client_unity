# UI 模块制作 SOP（铁律）+ UICreator 管线

> 适用：yu_client_unity 客户端重构里**一切 UI**。范本 = `Module/Core/Login`（LoginModule）。
> 一句话：**我（AI）管结构，用户管效果。** 我交付能跑的「预制体 + Bind + View 逻辑」，用户在预制体里调位置/大小/颜色/图片。

## 0. 四条铁律（每个界面都必须遵守）
1. **预制体优先，代码不写布局**：位置/大小/锚点/层级全在预制体里，代码里不出现摆 UI 的数值。
2. **运行时才有的元素 → 预制体留 disabled 占位/模板**，代码 `Instantiate` 克隆（范例：`MainUIDownView._tpl_MainFuncIconItem`、`LoginSelectRoleView` 的角色槽）。绝不在代码里 new 出布局。
3. **可调值挂到预制体脚本的 `[SerializeField]` 字段**：颜色、偏移、时长这类，代码读它、不写死魔法数；用户在 Inspector 直接改。
4. **图片引用从 Laya `.scene` 照抄**；引用错/缺的，用户在预制体里换。

边界：**我 = 结构/数据/协议/调用关系/能编译能跑；用户 = 位置/大小/颜色/图片观感。**

## 1. UICreator 生成管线（产 预制体 + Bind）
- 工具：`Assets/Editor/LayaUI/`，菜单 **神霄/LayaUI** → `LayaUIPipeline.RunModule("{module}")`。
- 流程：导散图 + 模板 → `LayaSceneConverter.ConvertModuleCombined` 产 prefab + `Generated/UI/{module}/{X}ViewBind.cs` → 编译 → `LayaBindFiller` 自动回填 Bind 引用 →（可选）Addressable 分组。
- 前置：`LayaUISettings.ValidateClientRoot`（需配 yu_client 目录，`.scene` 源在那）。
- **验收机制（关键）**：`LayaUIAcceptance.IsAccepted(module)`。模块被标记验收 ✅ 后，**重转会覆盖用户手调的预制体**。所以铁律：
  - 我**只在模块未成形时生成/重转**（尽量一次）。
  - 用户调完预制体 → 标记验收。
  - 之后我**只动 View 逻辑（.cs），绝不重转 prefab**（除非用户明确同意、知道手调会丢）。

### 1.1 重构 UI 生成器面板

- 菜单 **神霄/重构UI 生成器** 用于运行已注册的纯代码 Creator。顶部按模块分为 Tab，默认只展示当前模块，避免不同模块的同位置按钮混在同一长列表中。
- 当前模块内每个生成器独占一张纵向卡片，固定分成“界面 / 状态 / 操作”三列；`生成/重建`、`预览`、`定位` 只作用于同一张卡片。
- 搜索会临时跨全部模块显示匹配卡片；清空搜索后回到上次选中的模块 Tab。
- `全部重建` 只位于当前模块标题处，并保留覆盖人工调整的二次确认。重建前仍须关闭相关 Prefab 编辑页签，避免旧页签自动保存覆盖新产物。
- 面板实现：`Assets/Editor/UiCreator/UiRebuildWindow.cs`；条目来源：`UiRebuildRegistry`。新增页面只注册条目，不在窗口内写模块特判。

## 2. 模块结构模板（照抄 LoginModule）
`Assets/Scripts/Module/{域}/{Name}/`：
- `{Name}Bootstrap.cs`（static）：`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` → 订阅入口事件（如 EVT_FRAMEWORK_READY / EVT_GAME_START）→ setup Controller + 启动 Flow。
- `{Name}Flow.cs`（static）：`StartAsync`/`Open` → `ResManager.InstantiateAsync(GameResPath.GetUIPrefab("{module}","{Name}Module"), ViewManager.GetLayer(UILayer.X))` 实例化**一个模块预制体** → `GetComponentsInChildren<BaseView>(true)` 按 `is XxxView` 抓各页、初始 `SetActive(false)` → 编排 Show/Hide/切换；持有 View 引用、订阅事件。
- `{Name}Controller.cs : BaseController`：协议（`Register()` 里 `RegisterProtocal` + `OnXXXX` 处理）。
- `{Name}Model.cs`：数据单例（唯一真相源）。
- `Views/{X}View.cs : {X}ViewBind`：`OnInit`（`UIUtil.AddClick` 接点击）、`OnShow`（刷数据）、`Show()/Hide()`（来自 BaseView）。**只接逻辑**。
- 层级：`BaseView.Show()` = 激活 + 置顶（`SetAsLastSibling`）；背景 `SetAsFirstSibling` 垫底。

## 3. 粒度「收口」（Laya 散节点 → Unity 模块）
Laya 把一个控件拆成 box+bg+icon+label 一堆散节点，生成的 Bind 会暴露这些散字段。收口 = 用 `*Bind.cs` 生成后我再处理一遍：
- View 里把"一个逻辑控件"的散节点当一组用（如 `AddClick` 同时挂 `_img_ok` 和 `_lb_ok`，见 `LoginAlertView`）。
- 一个"页" = 一个 `BaseView` 子树；一个"模块" = 一个模块预制体含多页；`Flow` 统一实例化 + wire。
- 别把散节点布局搬进代码——它们的位置在预制体里、由用户调。

## 4. 动态 / 列表 / 指示器 / 未移植入口
- 列表项、动态标记 → 预制体放 disabled 模板节点，代码克隆。
- 未移植模块的入口 → 按钮可点，点击打日志标注"待对接 XView/协议Z"，**不崩**（优雅降级）。

## 5. 每页/模块产出自查清单
- [ ] `RunModule` 出 prefab + Bind（或确认已存在）。
- [ ] `View : Bind` 写好逻辑（点击/数据/协议），**MCP 探针确认能编译**。
- [ ] 动态元素有预制体占位；可调值是序列化字段。
- [ ] Flow/Bootstrap 接好（能打开/关/切）。
- [ ] `git commit`（只提交能编译的安全点）。
- [ ] 在 `重构进度.md` 标状态 → 交用户实机调效果 → 用户验收。
