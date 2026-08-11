# UIAudit 公共 Headless 采集与探针

`Tools/UIAudit` 是 UI 对接/精修路线唯一可复用的浏览器采集执行层。它用 `Tools/headless/node_modules/puppeteer` 驱动系统 Edge 的真实 Headless 页面，不生成合成画面，也不把路线专项实现写进 `output/`。

## 命令

```powershell
node Tools/UIAudit/cli.cjs version
node Tools/UIAudit/cli.cjs preflight --route Tools/UIAudit/examples/route.example.json --output output/ui_route_audit/2026-08-11_example/run-001
$env:UIAUDIT_ACCOUNT='111111'
$env:UIAUDIT_PASSWORD='...'
node Tools/UIAudit/cli.cjs run --route <route.json> --output <全新不可变证据目录>
node --test Tools/UIAudit/tests/*.test.cjs
```

`preflight` 不启动浏览器；`run` 才会启动真实 Headless Edge、登录并执行路线。正式路线文件必须是 JSON，只保存 route map、控件 selector、状态与协议断言；禁止在页面目录复制登录、运行树、JSON、弹窗、`ItemUseView` 或协议实现。

Node 调用方可从根入口加载命名空间：`const uiAudit = require('./Tools/UIAudit')`；也可按需直接 require `lib/*.cjs`。

## 公共 API

| 模块 | 职责 |
|---|---|
| `lib/session.cjs` | Edge 启动、登录、选角、进城、启动弹窗处理和热会话身份 |
| `lib/safe-json.cjs` | 只丢祖先环、不误删共享引用的 JSON 与原子不可变写入 |
| `lib/runtime-tree.cjs` | loaded view、managed page snapshot、`Laya.stage` 统一节点 schema |
| `lib/canvas-input.cjs` | 精确实例、viewport、`hitTestPoint` 通过后的真实 click/drag |
| `lib/popup-policy.cjs` | `allow / forbid / unknown-hard-stop`、队列排序和安全关闭面 |
| `lib/item-use.cjs` | `ItemUseView` 精确身份、双稳定帧、队列前后、一次受控关闭 |
| `lib/protocol-probe.cjs` | `UserMsgAdapter` 传输层收发 trace、读写分类、required/forbidden |
| `lib/preflight.cjs` | Node/Edge/Puppeteer、route、策略、凭证、authority 与输出目录闸 |
| `lib/route-runner.cjs` | 数据化步骤执行和真实截图/运行树证据 |
| `lib/report.cjs` | 版本化结构报告、产物大小与 SHA-256 |

策略位于 `policies/`，schema 位于 `schemas/`。页面不得猜某个 Controller 作为主探针；协议事实来自 `UserMsgAdapter` 传输收发记录，页面只声明 required/forbidden 数据。

## 不可变证据

`output/` 整体由 Git 忽略，只保存每次运行的新目录。公共代码、策略、fixture、schema 和说明必须进入 `Tools/UIAudit` 或 `Docs`。写入器默认拒绝覆盖已存在文件；同一路线复验必须创建新 run-id。

详细设计、迁移来源和未迁移专项边界见 [UIAudit 公共采集与探针基础设施](../../Docs/RuntimeCompare/UIAudit公共采集与探针基础设施-20260811.md)。
