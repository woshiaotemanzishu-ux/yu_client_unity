---
name: convert-module
description: 作为项目统一“UI 对接 / UI 精修”流程中的首次落地环节，把尚无可编辑 Prefab 的 Laya UI 模块端到端转成 Unity prefab + data-only View 代码，并用运行时快照和 diff 验收。仅当用户要转某个模块/某些 view、批量推进 Laya→Unity，或 UI 对接目标确认尚无可编辑 Prefab 时使用；已有 Prefab 的页面精修不得重转，应改用 fix-view。
---

# convert-module — Laya→Unity UI 模块转换流水线

把一个模块(如 login)的所有 view 从老客户端转成 **节点全在、可视化可编辑的 Unity prefab** + **只绑数据的 View 代码**,用 **运行时快照** 当转换源(不是静态 .scene),用 **diff oracle** 验收。

## 核心原则
- **结构进 prefab,数据进代码**:运行时解析完的整棵视图树烤进 prefab(背景/特效/列表项/位置全是真实节点);代码只把名字/图标/点击/数据绑到已存在的节点。
- **粒度 = 运行时受管视图**(老端 BaseView 的开关单元),由 `Tools/ModuleManifest/<module>.manifest.json` 定(一个 prefab = 一个 view)。manifest 由分组分析产出(见 [[conversion-architecture-and-plan]] 记忆)。
- **并发现实**:Unity 单编辑器,**烤/回填/编译必须串行**(做成一次 MCP 调用);**采快照 + 移植绑定可并行**(多 agent / Playwright)。
- **分级移植(重要,2026-06-26 教训)**:**简单屏**(纯展示 + 按钮:登录/注册/弹窗/加载)可批量草稿移植走③。**数据驱动重屏**(列表/选服/选角:服列表、角色列表、当前服展示)**必须单独、用 diff 对老端逐节点验着做**——批量草稿易把它们搞坏(EnterView 选错服、SelectServer 面板不渲染、SelectRole 烤制数据漏出都是这么栽的)。栽了就 `git checkout HEAD -- <view>.cs <monolith>` 还原成 working,重新嵌回好的(`ReplaceModuleSubviewWithBaked` 幂等),再单独精修。

## 前置
- 老客户端跑在 `http://127.0.0.1:8090/index.html`(Laya 游戏)。
- Unity 编辑器开着、Unity MCP 连着。
- 模块的 manifest 存在:`Tools/ModuleManifest/<module>.manifest.json`。没有就先做分组分析产一份(读快照视图表 + 老端 ViewManager/BaseView 代码 → 按 kind 归类成 prefab 清单)。

## 流水线(每个模块)

### ① 采快照(可并行;Playwright 无头驱动老客户端)
```
node Tools/Conversion/capture_snapshots.cjs --out Tools/ModuleManifest/snapshots/<module> --views A,B,C --wait 8000
```
- 注入 `pageSnapshot.js`,把指定/已加载的视图导成 `page_snapshot_<view>_*.json`。
- 冷启动 stage 可能空(游戏没开 view):调大 `--wait`、或驱动到目标屏、或 `--cdp` attach 你已开着的实例、或用 electron 工具的快照导出兜底(数据驱动屏如选角/选服尤其需要真状态)。
- **抓 dump 的 Unity 那侧**(给 diff 用)要在 GameView 设 **720×1280** 下 Play 抓(见 [[runtime-ui-diff-oracle]])。

### ② 批量烤制(串行,一次 MCP 调用 = 烤 + 回填 + 注册 addressable)
Unity MCP `RunCommand`:
```csharp
result.Log(Shenxiao.Editor.LayaUI.LayaSceneConverter.BakeModuleFromManifest(
    "Tools/ModuleManifest/<module>.manifest.json",
    "Tools/ModuleManifest/snapshots/<module>",
    "<ModuleDir>"));   // 如 "Login"
```
- 对 manifest 里每个有快照的 view:`BakeViewTree`(复用 BuildRoot/LayaRectMath/散图)→ `LayaBindFiller.FillPrefab`(挂业务组件+按节点名回填)→ `RegisterAddressable`(**postEvent:false**,地址 = `prefabs/ui/<module>/<view>`)。无快照的 SKIP。
- ⚠️ **MCP 交互守卫**:Addressables 写操作用 `postEvent:true` 会报 "User interactions are not supported";必须 `CreateOrMoveEntry(..,false,false)` + `SetAddress(..,false)`(baker 里已这么做)。`AutoGroupAll` 因内部 postEvent + 进度条不能在 MCP 里跑。
- ✅ **烤后抽验清单**(漏一项就会"烤制数据漏出",见 `/fix-view`):① Label/输入框无冗余子 TMP_Text(不重叠)② 数据驱动列表的 item 节点有 `{Item}Bind` 且字段非空(`LayaBindFiller` 已加全树递归 `FillNestedItemBinds` 补挂内嵌 item)③ 容器/可见节点绑对。
- ⚠️ **改了烤制器/回填器(Baker.cs / LayaBindFiller.cs)别马上靠 MCP 重烤**:编辑器程序集要重编一轮才生效,刚改完那次 MCP 调用可能还跑旧逻辑;要么确认重编完,要么对现有 prefab 用**新写的命令脚本**(每次都新编)直接做外科修。`BakeModuleFromManifest` 路径参数别用 `Assets/../`(会重复拼成 `Assets/Prefabs/UI/Assets/...`)。

### ③ 移植 View 绑定(可并行,多 agent)
每个 view 都要它的绑定代码,来源是 **port 老客户端 TS**。批量用 Workflow:
```
Workflow({ name: 'port-view-bindings', args: {
  module: '<Module>', reference: '<已手工验证的样板 View,如 LoginCreateRoleView>',
  views: ['LoginView','RegisterView', ...]   // 已烤好、待绑定的
}})
```
- 每个 view 一个 agent(worktree 隔离):读 老 TS + 烤好的 prefab 结构 + Bind + 样板 → 写 data-only 的 `Views/<View>.cs`。纯文件读写,不碰 Unity。
- 产物是**草稿**,要走 ④ 编译 + ⑤ diff 验收。

### ④ 编译(串行,MCP)
```csharp
UnityEditor.AssetDatabase.Refresh();
result.Log("compileFailed={0}", UnityEditor.EditorUtility.scriptCompilationFailed);
```
有错就看 Console / 让对应 view 的 agent 修。

### ⑤ 切加载(每模块一次)
让模块流程加载独立烤制 prefab 替掉 monolith 子视图。参考 `LoginFlow.StartAsync` 里创角的写法:
```csharp
var go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("<module>","<View>"), parent);
if (go != null) _view = go.GetComponent<View>();   // 加载失败优雅回退
```
(后续可把这步泛化成框架级"按 manifest 自动加载烤制 prefab",免得逐 view 写。)

### ⑥ 验收(抽查,不是关卡)
- Play 到目标屏 → `神霄/调试/UI运行态/截图+节点Dump` → `ui_dump.json`。
- `python Tools/UiDiff/ui_runtime_diff.py --laya <快照> --unity <ui_dump> --view <View>` → 每节点偏移清单。
- 偏哪修哪:能直接在 prefab 里**拖**(节点都是真的),或回烤制器/LayaRectMath 修根因,或 `/fix-view`。

## 自动化 / 无人跑
- ②④ 串行(Unity);①③ 并行。整条可挂 `/loop` 自走:有新快照就续烤 + 移植 + 编译 + 出 diff 报告,排成队;用户闲了抽查 + `/fix-view` 收尾。
- 端到端真无人的前提:采快照导航补全(或 attach 运行实例)。采不到的单屏用 electron 工具兜底。

## 验证过能跑的
- `BakeViewFromSnapshot` / `BakeModuleFromManifest`(一次调用烤+回填+注册);登录模块 baked=3。
- Playwright 无头驱动老客户端 + 注入 + 导出(`capture_snapshots.cjs`)。
- 创角已端到端跑通(烤→回填→data-only View→切加载→addressable),当 ③ 的 `reference` 样板。

相关记忆:[[conversion-architecture-and-plan]]、[[runtime-ui-diff-oracle]]。
