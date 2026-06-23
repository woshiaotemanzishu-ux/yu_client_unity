# Shenxiao UI 运行态对比巡检任务说明与踩坑总结

日期: 2026-06-23

## 任务目标

把 Unity 当前导入/运行的 UI 页面，对齐旧 Laya 客户端的真实运行态页面。

基准不是 `.scene` 静态文件，而是旧 Laya 客户端在 720x1280 竖屏移动端画幅下的运行时界面。入口:

- `http://127.0.0.1:8090/index.html`
- 账号可用既有账号或随机新号，密码统一可用 `zxczxc`

首批优先页面:

- 设置
- 聊天
- 角色
- 背包

之后按入口逐页扩展。目标不是“元素大概出现”，而是页面达到可用状态: 背景、窗框、静态图、运行时动态图、真实数据、按钮交互、关闭/切页/子窗行为都尽量和旧端一致。

## 完成标准

一个页面只有同时满足下面条件，才算完成:

1. 有旧端 720x1280 运行态截图和节点证据。
2. 有 Unity 当前运行态截图和节点证据。
3. 页面背景/窗框完整，不允许出现整页透明、缺背景、层级穿透这类基础问题。
4. 静态元素和运行时动态元素都覆盖到位。Laya 运行时加载的图片、列表、按钮、模板，不能只按 `.scene` 结论判断。
5. 使用真实数据链路。没有服务端数据时记录 blocker，不写 fake/mock 数据。
6. 主要按钮可点击。已接入的按钮要打开对应页面/子窗；未接入的按钮要记录缺口，不能算完成。
7. 修复优先落在通用链路: 转换器、Bind 回填、通用窗框、默认皮肤、动态图片加载、列表模板、Addressables 分组。
8. 业务 View 只处理运行时状态、数据、事件和必要显隐。避免逐个 prefab 像素级手工精修，除非确认是页面业务运行时逻辑缺口。
9. `dotnet build .\Shenxiao.Module.Core.csproj` 或 Unity 编译通过。
10. 复验截图和差异清单已更新。

## UI 生成与修复主规则

UI 生成主路线必须是“通用转换器/规则表 -> Unity Editor 菜单生成 -> 运行态验收”，不是直接手工改 prefab。

必须改通用转换链路的情况:

- 背景、窗框、底图、按钮皮肤、九宫格、默认图片缺失或错误。
- 列表模板尺寸/位置/锚点错误。
- `.scene` 静态属性转换不正确。
- TS 运行时静态赋图可以被烘焙但没有烘焙。
- Bind 字段没有回填或回填错。
- Addressables 分组缺资源。
- 多个页面出现同类错位、透明、缺图、缺皮肤问题。

执行方式:

1. 查旧端运行态和旧端 TS 代码，确认差异属于静态生成、运行时烘焙、Bind、资源还是业务逻辑。
2. 属于生成问题时，修改 `Assets/Editor/LayaUI/`、`Schemas/LayaUI/`、默认皮肤/资源映射表或 Bind 回填逻辑。
3. 用 Unity Editor 菜单重新转换/回填/分组/验收对应模块。
4. 保存 Unity 运行态截图和节点证据。
5. 只有确认旧端也是运行时动态行为时，才改业务 View/Flow。

禁止把这些当成完成方案:

- 在 Inspector 里手动拖 prefab 补背景、补图、改尺寸，然后不回到转换规则。
- 在业务 View 里写一堆坐标、尺寸、颜色来补静态生成错误。
- 只修单个页面，不确认是否为共性转换缺陷。
- 没有通过 Unity Editor 菜单重转/回填就声称“导入问题已修复”。

## 当前真实状态

这轮巡检只算跑通了“采样、对比、部分修复”的流程，还没有完成任何一个页面。

已采样的旧端证据:

- `output/manual_round/oldclient_fresh_20_setting.png`
- `output/manual_round/oldclient_chat_20_chat.png`
- `output/manual_round/oldclient_role_20_role.png`
- `output/manual_round/oldclient_bag_20_bag.png`

已做过的 Unity 证据主要在:

- `output/runtime_unity/`

已推进过的修复方向:

- 设置页头像遮挡问题: `CustomHeadItem` 隐藏系统头像覆盖层。
- 通用窗框: 重转/回填 `BaseWindowSkin`，补通用资源。
- 通用标签: `TabButtonTwoSkin` 增加默认文本标签皮肤加载。
- 角色页: 属性列表从单列调整为旧端运行态的两列四行顺序。
- 背包页: 补角色名、战力小组件、角色模型显示链路。

未闭环的问题:

- 背包当前仍存在基础可用性问题，例如背景/窗框透明或缺失时，页面不能算完成。
- 背包按钮、子窗、装备/物品相关运行态行为还没有完整验收。
- 聊天页 UI 壳已有进展，但旧端世界消息等运行时数据链路未闭环，不能伪造消息。
- 角色/背包最新代码只做到了 `dotnet build` 通过，还缺 Unity 最新运行态截图复验。
- 全页面巡检没有形成稳定台账，导致长时间运行后难以量化交付。

## 可持续工作方式

后续每一轮必须先更新工作台账，再修页面。建议每页一行:

| 页面 | 旧端截图/节点 | Unity截图/节点 | 背景窗框 | 静态元素 | 动态数据 | 按钮交互 | 根因 | 已修复 | 验证 | 状态 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 背包 | oldclient_bag... | 待复验 | 未通过 | 部分 | 部分 | 未完成 | BaseWindow/运行时链路待查 | 部分 | build通过 | 未完成 |

状态建议只用这些值:

- `blocked`: 工具、服务、账号、资源或协议阻塞。
- `investigating`: 已有证据，根因未定。
- `fixing`: 根因明确，正在修。
- `needs-runtime-verify`: 代码/资源已改，缺 Unity 运行态截图。
- `done`: 完成标准全部满足。

每轮输出必须包含:

- 已覆盖入口
- 旧端证据路径
- Unity 证据路径
- 发现差异
- 共性根因
- 已执行代码/生成/导入任务
- 验证命令和截图
- 未完成项
- 下一批页面优先级

## Claude Code 协作要求

用户明确希望 Claude Code 参与代码修改，但不能假装已经协作。

使用规则:

1. 需要代码时先尝试 Claude Code CLI。
2. 如果 CLI 不可用，必须记录具体命令和完整现象。
3. 不要在 Claude 不可用时继续声称“已协同”。
4. Claude 可用后，应该拆成明确任务包，例如“修通用窗框背景透明”“修背包运行态按钮链路”“补页面台账生成脚本”，而不是让多个 agent 无目标乱跑。

当前已知问题:

```powershell
claude -p --bare --no-session-persistence --permission-mode bypassPermissions --tools "Read,Edit,MultiEdit"
```

现象:

```text
Not logged in · Please run /login
```

因此这轮实际没有形成 Claude Code 协同。

## Unity MCP / 工具踩坑

### 1. Unity MCP relay 残留

连接 Unity MCP 前要检查 `relay_win.exe`。残留 `--mcp` 子进程会占用或干扰连接。

注意: 不要误杀 Unity 插件自己的 HTTP relay。通常 9002/9001 端口由插件 relay 持有。

可先看端口:

```powershell
Get-NetTCPConnection -State Listen | Where-Object { $_.LocalPort -in 9001,9002 }
```

再看进程:

```powershell
Get-CimInstance Win32_Process -Filter "name='relay_win.exe'" |
  Select-Object ProcessId,ParentProcessId,CreationDate,CommandLine
```

### 2. MCP 可能出现 Transport closed

本轮 Unity MCP 工具连续返回:

```text
Transport closed
```

出现这个状态时，不要继续消耗大量时间硬跑。应记录为工具阻塞，并改用:

- `dotnet build .\Shenxiao.Module.Core.csproj`
- Unity `Editor.log`
- 已有截图/节点证据

等 MCP 恢复后再做运行态截图复验。

### 3. RunCommand 禁用 System.Reflection

Unity RunCommand 动态脚本不能使用 `System.Reflection`。已知报错:

```text
UNEXPECTED_ERROR: Script uses one or more unauthorized namespaces:
Namespace System.Reflection is imported...
```

需要改用公开 API、Addressables 实例化、已有 Flow/Bootstrap 入口，不要用反射访问私有方法。

### 4. ReadPixels 截图可能灰屏

Unity UI 运行态截图用 `Texture2D.ReadPixels` 有时会得到灰屏。更可靠方式是:

```csharp
ScreenCapture.CaptureScreenshot(path);
```

本轮有效截图例子:

- `output/runtime_unity/current_screen_capture_after_with_mainui.png`

### 5. Laya 必须看运行态

Laya 的 `.scene` 不是最终页面。很多图片、按钮、列表项、背景、战力组件、角色模型和消息都是运行时 TS 代码加载或重挂的。

错误方式:

- 只看 `.scene`
- 只看转换后的 prefab
- 只看静态 Bind 字段

正确方式:

- 打开旧端网页
- 进入真实游戏
- 点击页面入口
- 保存截图
- 导出 `Laya.stage` 节点树
- 再对照 Unity 运行态

### 6. 横屏网页不是设计基准

旧端可以在网页里打开，但游戏本体是竖屏移动端。巡检必须用 720x1280 竖屏移动端画幅作为基准。

宽屏网页只能作为调试入口，不能作为视觉还原标准。

### 7. “六七成”不能当完成

页面上主要图片出现不等于完成。以下情况都属于未完成:

- 背景透明或缺失。
- 旧端运行时动态加载的控件缺失。
- 按钮不可点或只打日志但未记录为缺口。
- 数据是空的但未说明是服务端/协议 blocker。
- 没有 Unity 运行态复验截图。
- 只修了单页，没有确认是否是通用转换问题。

## 下一轮建议

不要马上恢复心跳巡检。先做两件事:

1. 补一个页面台账，先把设置、聊天、角色、背包按完成标准重新打分。
2. 优先修背包“背景/窗框透明”这类基础共性问题，再谈按钮和子功能。

确认 Claude Code 登录可用后，再恢复多 agent/Claude 协同。否则应明确按 Codex 单线程推进，不要把进度预估建立在不存在的协作上。
