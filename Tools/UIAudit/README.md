# UIAudit 公共 Headless 采集与探针

`Tools/UIAudit` 是 UI 对接/精修路线唯一可复用的老 H5 浏览器执行层。它复用 `Tools/headless/node_modules/puppeteer` 驱动系统 Edge 的真实 Headless 页面；不生成合成截图，不占用前台，也不把可复用代码留在 `output/`。

## 命令

```powershell
node Tools/UIAudit/cli.cjs version
node Tools/UIAudit/cli.cjs server status --profile legacy-h5-local
node Tools/UIAudit/cli.cjs server start --profile legacy-h5-local
node Tools/UIAudit/cli.cjs preflight --route <route.json> --output <全新 output/run>
node Tools/UIAudit/cli.cjs run --route <route.json> --output <全新 output/run>
node Tools/UIAudit/cli.cjs run --ensure-server --route <route.json> --output <全新 output/run>
node Tools/UIAudit/cli.cjs server stop --profile legacy-h5-local
node --test Tools/UIAudit/tests/*.test.cjs
```

`preflight` 只用有界 HTTP 检查 `route.url`、HTML 标记和必需 bundle，不启动 Edge、不创建 run 目录。固定检查 ID 是 `route-url-readiness`；本地端口未监听返回 `SERVER_NOT_RUNNING`，端口被错误服务占用返回内容/状态类错误。只有所有检查通过，`run` 才创建不可变证据目录并启动 Headless Edge。

`legacy-h5-local` 是 UIAudit 的标准老 H5 profile：固定 `E:/GitProject/yu_client/h5` 为编译 cwd、`E:/GitProject/yu_client/cdn` 为静态根、`127.0.0.1:8091` 为路线端口。它绕过旧 `npm start` 的 8070 默认值以及 `open:true/openUrl`，在后台内存编译，不打开用户浏览器。`stop` 只终止同时匹配 PID、worker 路径和私有 owner token 的本工具进程，绝不接管或杀死非本工具服务。

Node 调用方可 `const uiAudit = require('./Tools/UIAudit')`，也可直接 require `lib/*.cjs`。`runPreflight` 是异步 API，必须 `await`。

## 页面 route 合同

正式 route 只保存 route map、selector、控件名和断言数据，不复制登录、运行树、JSON、弹窗、协议或 server 启动逻辑。

### 启动弹窗

`policies/startup-popups.json` 只允许 source-backed 的精确身份。配置队列弹窗按 `ClientConfigPopupLevel.sort` 处理；不在该配置且由回包直接打开的弹窗禁止伪造 sort，只能使用真实 visible stack 的 top-first 顺序。`CycleimpActlistYesterday` 是后一类：由 `22703` 回包在推送或当日首次登录、榜单非空且角色等级至少 150 时直接打开；唯一安全面是 `_btn_close`。关闭后必须连续两个已推进的 Laya 帧都确认“被点击的具体实例不再打开、也不在 stage 可见”，重复 frame token 或中途重现均不算关闭完成。未知弹窗仍一律 `unknown-hard-stop`。

稳定关闭不是固定 sleep 后检查 View 名称。公共层以点击时的 `ViewManager`/runtime registry 实例键和根 `stagePath` 锚定具体实例，持续调和 loaded view 的 `HasOpen/isPop`、managed view 根状态和 `Laya.stage` 的可见/`displayedInStage`。`open=false` 但仍保留在 registry、等待延迟销毁的根属于 `closed-cached`，不会被误报为仍打开；关闭请求后 stage 仍可见属于 `closing`，继续等真实推进帧；消失后同名新实例或同一缓存实例重新打开属于 `requeued` 并硬停。稳定闸内不会二次点击。

启动弹窗的 `safeClose.node` 按真实 View 实例字段解释。公共 session 使用 `ownerView + boundField` 精确定位 `_btn_close` 指向的显示对象，不再要求字段名与 Laya display node 的 `name` 相同，也不会退回页面坐标。绑定缺失或出现多个实例时仍由 `expectedCount=1` 与运行时 hit-test 硬停。

### ItemUseView

每条 route 必须明确二选一：

```json
{ "session": { "itemUse": { "mode": "hard-stop" } } }
```

或声明一次受控关闭所需的 `authorization`、完整 `expected` 身份、`queueSpecs`、`queueAssertions` 与 `protocolAssertions.mode=read-only`。缺省不再代表安全；未声明会在 `item-use-session-policy` 失败。页面只能声明当前实例和队列授权，不能修改公共弹窗策略。

### 协议

页面只允许使用 `policies/protocols.json` 中有源码证据的精确只读签名。主动读请求写为：

```json
{ "cmd": 16002, "fmt": "c", "args": [5], "ruleId": "outward-base-read-16002" }
```

required outbound 断言必须提供精确 `fmt+args`，或绑定公共 `ruleId`；只写 `cmd` 会在 `route-protocol-contract` 失败。当前公共策略覆盖 Bag/Warehouse `15010` 容器读、Pet `16002/16006/16011/16028` 和 Fashion `41312`。页面不能内联扩展 policy；新增命令必须由公共层核对当前源码哈希、命令语义与签名后加入策略。

protocol trace v3 的收发权威都在 transport 层：outbound 包装 `WriteBegin/WriteFMT/SendToGame`，所以 41305 等自定义写包链也能证明“未发送”或精确捕获违规发送；inbound 在 `ReceiveHandler` 读取 frame header 前记录 `cmd/frame length/compression/sequence`，不要求业务 `register_list[cmd]` 已存在。`inboundCommands` 只声明哪些命令需要可选 payload 关联：handler 已存在则立即包裹，尚未注册则通过 `RegisterMsgOperate` 惰性附着，并在真实 handler 调用前读取 payload 后恢复 Byte cursor。handler 缺失、注册状态或 payload 解码失败只作为关联元数据，不能抹掉或替代 transport frame 证据。

### 通用动作

- `assert-nodes`：`exists:true/false` 或精确/区间数量，表达正负存在与条件 Tab 未生成。
- `branch`：按 `nodes` 或 `geometry` 条件选择数据化 `then/else`，表达条件详情分支。
- `assert-geometry`：矩形数值、`inside/partial/outside/intersects` 裁剪。
- `assert-scroll`：对比先前 snapshot，验证 ScrollBar/Content 位移、裁剪和末项可达。
- `snapshot.samplingTargetMs`：以最近一次真实 click 完成为锚，采 350/1000ms 等时点。
- `reset-sound` / `assert-sound`：统计 `PlaySoundEffect/PlaySceneSound` 逻辑调用和重复消费。
- `wait-render-ready`：读取指定 RenderTexture 的真实 alpha 像素，要求连续稳定帧达到阈值。

selector 支持三种可组合的精确身份：`view`（兼容字段）、`ownerView`（三来源调和后的真实宿主）、`runtimeName`/`name`（显示节点名）以及 `boundField`（View 对显示节点的直接字段引用）。例如字段名与节点名不同时使用：

```json
{
  "source": "laya-stage",
  "ownerView": "CycleimpActlistYesterday",
  "boundField": "_btn_close",
  "expectedCount": 1
}
```

`runtime-tree` 先用 loaded/managed 快照中真实 `display_obj` 的 `stagePath` 给整棵 stage 子树归属 owner；再从 `ViewManager`/运行时 registry 的 View 实例枚举“字段直接引用显示节点”的绑定。规范化节点的 `identity.owner` 与 `identity.bindings[]` 会同时保留 owner 来源、根路径、字段名、运行节点名和实例 registry 来源，便于审计别名链；只在两类权威路径都缺失时保留带 `stage-name-heuristic` 标记的旧名称兜底，不能作为绑定证据。

selector 仍支持 `dataIdentity` 子集匹配，例如 `{ "dataIdentity": { "fashion_id": 12010008 } }`。click/drag 始终先在 Laya 逻辑坐标做唯一身份与 `hitTestPoint` 验证，再按真实 Canvas DOM rect 映射到浏览器坐标；1920×1080 宽屏不再误用 720×1280 逻辑坐标直接点击。

若唯一身份数量不符，`CANVAS_TARGET_IDENTITY_MISMATCH` 会携带 `ui-audit.selector-diagnostic.v1`。runner 在失败 run 中写入 `selector-diagnostic-*.json`，内容包含目标 View 的最小规范化 stage 子树、按 owner/运行名/绑定字段评分的候选、stage/source 摘要、子树 SHA-256 及诊断内容 SHA-256；`ui-audit-report.json.failure.diagnostic` 和 `artifacts[]` 同时引用它。即使登录阶段尚未产生常规 runtime snapshot，也不会再留下 `runtime-tree=0` 的盲区。

`POPUP_CLOSE_NOT_STABLE` 使用同一不可变诊断通道，artifact 类型为 `popup-close-lifecycle-diagnostic`。除 selector、最终子树和候选外，`context` 还保存点击前子树/哈希、具体实例锚、每次采样的 frame token、loaded/managed/stage 判定来源和最终分类：`click-not-consumed`、`closing-timeout`、`requeued`、`frame-not-advancing` 或 `still-visible-or-managed`。这使失败可以区分点击未消费、关闭过程未完成、缓存实例、帧未推进和队列重新打开，而不靠坐标猜测或页面专用重试。

历史 `step.expect` 不会被当成注释跳过，而是在 schema/preflight 硬停。应改成上述可执行动作。

## 公共 API

| 模块 | 职责 |
|---|---|
| `lib/session.cjs` | Edge、登录、选角、进城、启动弹窗和热会话 |
| `lib/safe-json.cjs` | 只丢祖先环、保留共享引用的 JSON 与原子写入 |
| `lib/runtime-tree.cjs` | loaded/managed/`Laya.stage` 统一节点 schema 与数据身份 |
| `lib/selector-diagnostic.cjs` | selector 失败的最小子树、候选、内容哈希与不可变诊断写入 |
| `lib/canvas-input.cjs` | Canvas rect 坐标换算、唯一命中后的真实 click/drag |
| `lib/popup-policy.cjs` | `allow/forbid/unknown-hard-stop`、配置队列/真实栈顺序、安全节点与稳定关闭帧 |
| `lib/popup-lifecycle.cjs` | 被点击实例锚定、loaded/managed/stage 关闭调和与失败分类 |
| `lib/item-use.cjs` | ItemUse 精确身份、稳定帧、队列和一次受控关闭 |
| `lib/protocol-probe.cjs` | transport 级收发 trace、handler 惰性关联、读写分类、required/forbidden 与 policy 闸 |
| `lib/route-assertions.cjs` | 节点、条件分支、几何、裁剪和滚动断言 |
| `lib/runtime-probes.cjs` | 声音调用与 RenderTexture 非透明像素 ready |
| `lib/server-readiness.cjs` | 无浏览器 HTTP readiness 与稳定错误分类 |
| `lib/server-lifecycle.cjs` | 标准 profile 的后台 start/status/owned stop |
| `lib/preflight.cjs` | 版本、依赖、authority、route 合同、URL 与输出闸 |
| `lib/route-runner.cjs` | 数据化步骤、真实截图/snapshot/trace 和结构报告 |

## 不可变证据

`output/` 整体由 Git 忽略，只保存每次运行的新目录。公共代码、策略、fixture、schema 和文档必须进入 `Tools/UIAudit` 或 `Docs`。写入器拒绝覆盖已有文件；同一路线复验必须新建 run-id。

设计、迁移来源和边界见 [UIAudit 公共采集与探针基础设施](../../Docs/RuntimeCompare/UIAudit公共采集与探针基础设施-20260811.md)。
