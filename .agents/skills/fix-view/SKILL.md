---
name: fix-view
description: 修一个已转换的 Unity view(位置偏移/缺节点/换错图/绑定不对)。当用户指出某转换后的界面有问题、或拿着 diff 报告要逐项收尾时用。输入:view 名 + diff 报告或用户的具体描述。
---

# fix-view — 收尾修正一个转换后的 view

转换流水线(见 `/convert-module`)产出的 prefab + View 代码,抽查发现问题时用这个逐项修。**优先改 prefab(可视化),其次改 View 代码,最后才动烤制器根因。**

## 输入
- view 名(如 `LoginCreateRoleView`)。
- 问题来源二选一:
  - **diff 报告**:`python Tools/UiDiff/ui_runtime_diff.py --laya <快照> --unity <ui_dump> --view <View>` 的输出(MISSING/OFFSET/SIZE/RESOURCE 清单)。
  - **用户描述**:"顶部职业项偏左""返回键太高"之类。

## 判断改哪一层

| 症状 | 多半的根因 | 怎么修 |
|---|---|---|
| `OFFSET` / `SIZE`(位置/尺寸偏) | prefab 节点的 RectTransform 锚点/位置 | **直接在 prefab 里拖/改 RectTransform**(节点都是真的);系统性偏移则回 `LayaRectMath` 或快照采集状态 |
| `MISSING`(少节点) | 快照没采到(屏没到目标状态)/ 该节点运行时才加载 | 重采快照(到对的状态)重烤;或确认是真·运行时加载,在 View 里补 |
| `RESOURCE`(换错图/没图) | 数据绑定的图路径/索引,或 sprite 名不对 | 改 View 的绑定逻辑(图路径/职业索引映射);编辑器里 tips 这种运行时加载的图本就空,Play 才有 |
| 绑定字段 null / 点了没反应 | Bind 字段没回填,或 View 没绑事件 | 重跑 `LayaBindFiller.FillPrefab(<prefab>)`;容器字段(RectTransform)按【声明类型】解析(烤后 Box 多挂 Image 会置空);点击 ClearClicks+AddClick |
| 输入框/文字**重叠** | 老端 TextInput/Label 内部文字节点被烤成常显子 Text,盖住输入值/真实文字 | 删该 TMP_Text/输入框下多余的子 TMP_Text(父权威、子冗余);烤制器 `AdaptSnapshotNode` 已跳 text-like 子节点 + TextInput 提示提到 placeholder(治本) |
| 列表**显示了不该有的项 / 数量像写死**(如选角"1 角色却显 3 个") | 烤制快照**冻结了数据驱动列表**(烤时那个账号的真实项被当固定节点) | View 必须按**真实数据**建表(遍历/克隆),别信烤制数量。逐层查漏出:① item 有 `{Item}Bind` 且字段非空(漏挂→View `bind==null` 整列跳过)② 无冗余子 Text ③ 绑的是**可见**节点(头像是 `icon_sys_head` 不是空的 `icon`;路径因 `_box_con` 嵌套失效就递归按名找)④ 空槽用 `节点.SetActive(false)` 整块隐藏(连子节点),比 `组件.enabled=false` 稳 |

## 步骤
1. 读问题来源(diff 报告或用户描述),定位到具体节点 + 判断改哪层(上表)。
2. **prefab 改动**:用 Unity MCP `RunCommand` 改 RectTransform / sprite / 组件(`PrefabUtility.LoadPrefabContents` → 改 → `SaveAsPrefabAsset`);或让用户在编辑器里直接拖(节点都是真的,这是烤制的意义)。
3. **View 代码改动**:改 `Assets/Scripts/Module/Core/<Module>/Views/<View>.cs`,然后 MCP 编译验证(`scriptCompilationFailed`)。
4. **重新 diff** 确认该项清零(或让用户重新 Play+dump 后再 diff)。

## 原则
- **"烤制数据漏出"是大类**(界面显示的是烤快照时那个账号的旧数据,不是当前账号的)。本质=有"View 没接管的可见节点"在漏:逐个找①没挂 Bind→跳过 ②同义冗余子节点 ③绑错/路径失效的可见节点 ④空槽没整块隐藏。一层层堵,别只堵一层就以为好了(选角踩过:Bind→子Text→头像三层叠着)。改完**在 monolith 嵌套实例上**验证(嵌套实例继承源 prefab 改动)。
- 一次修一个 view、一组症状;改完给出"改了什么 + 怎么验证"。
- 位置类优先 prefab 直改(可视化、可回退),别动代码绕。
- 系统性、跨多 view 的同类偏移 → 回根因(`LayaRectMath` / 快照采集),别逐个 prefab 硬调。
- MCP 改 prefab/编译 OK;Addressables 写记得 `postEvent:false`;Play 验收靠用户。

相关:`/convert-module`、[[runtime-ui-diff-oracle]]、[[conversion-architecture-and-plan]]。
