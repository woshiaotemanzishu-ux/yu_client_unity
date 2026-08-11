# UIAudit 公共 Headless 采集与探针

## 运行栈遮挡归属（runtime overlay v1）

`policies/runtime-overlays.json` 是公共运行遮挡策略，页面 route 不得复制或覆盖它。采集器把 `ViewManager.GetBackGround()` 的共享全屏背景通过 `curr_view` 归属到实际受管 View，并把该 View 按真实 `stagePath` 加入 observed popup stack；因此背景短名或 `ownerView=null` 不再遗漏栈顶。若上层是已授权安全弹窗，当前关闭动作会在零点击状态下让位，由公共 drain 先处理真实栈顶；未知或危险 View 仍由 popup policy 硬停。

`ViewManager.waitfor_openView_loading.display_obj` 被识别为全局输入门。公共 session 只按源码定义的 `curr_loading_view_dic` 清空/隐藏条件有界等待，超时返回 `RUNTIME_INPUT_GATE_TIMEOUT`；没有 source-backed 归属的全屏可交互对象返回 `RUNTIME_OVERLAY_UNKNOWN`，并写入 `runtime-overlay-diagnostic`，禁止穿透、改 `mouseEnabled`、DOM 直调或坐标重试。

runtime node v3 保留 `runtimeClass`、`identity.systemOverlay`、`hitArea` 与事件 listener 计数。Canvas 输入诊断同时保存真实 topmost 的祖先链、最小 normalized subtree、候选受管 View、layer/manager 关联及 SHA-256；可以区分受管背景、全局输入门和未解释拦截。公共 API 为 `runtimeOverlay.loadRuntimeOverlayPolicy/classifyRuntimeOverlay/runtimeOverlayDecisions/runtimeOverlayViews`。

`Tools/UIAudit` 是 UI 对接/精修路线唯一可复用的老 H5 浏览器执行层。它复用 `Tools/headless/node_modules/puppeteer` 驱动系统 Edge 的真实 Headless 页面；不生成合成截图，不占用前台，也不把可复用代码留在 `output/`。

当前版本为 `1.2.0`。本版本新增完成范围保护、route/step/phase 分层计时，以及资源工具 preview 的严格 CAS 恢复对接。

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

`preflight` 只用有界 HTTP 检查 `route.url`、HTML 标记和必需 bundle，不启动 Edge、不创建 run 目录。固定检查 ID 是 `route-url-readiness`。`ui-audit.server-observation.v1` 同时记录 listener、PID、进程名/路径/脱敏命令、创建时间/年龄、UIAudit owner state，以及每次 GET/HEAD 的耗时和结果；schema 位于 `schemas/server-observation.schema.json`。

稳定分类如下：无 listener 为 `SERVER_NOT_RUNNING`；listener 存在但 HTML/状态/资源不匹配为 `EXTERNAL_SERVER_ROUTE_MISMATCH`；同一外部 PID 持续监听但有界 GET 超时、reset 或矛盾地拒绝连接为 `EXTERNAL_SERVER_UNRESPONSIVE`；探针期间 listener PID 变化为 `EXTERNAL_SERVER_STATE_CHANGED`。底层原因保留在 `causeCode`，每次尝试保留在 `attempts[]`。只有 timeout/reset 且探针前后 listener identity 完全一致时，才按 profile 的 `transientRetry` 做一次有界退避；内容不匹配、连接拒绝、PID 变化和未知错误不重试。重试耗尽后 `retryAllowed=false` 并硬停。

外部 listener 一律 `ownership.owned=false`：`recovery.start/stopOwned/runWithEnsure` 均为空，`automatic.startAllowed/stopAllowed=false`，只允许再次 `server status`，或由用户在 UIAudit 之外检查/重启报告中的外部进程。UIAudit 绝不在占用端口上另起服务，也不停止、杀死或覆盖外部 PID。只有所有检查通过，`run` 才创建不可变证据目录并启动 Headless Edge。

`legacy-h5-local` 是 UIAudit 的标准老 H5 profile：固定 `E:/GitProject/yu_client/h5` 为编译 cwd、`E:/GitProject/yu_client/cdn` 为静态根、`127.0.0.1:8091` 为路线端口。它绕过旧 `npm start` 的 8070 默认值以及 `open:true/openUrl`，在后台内存编译，不打开用户浏览器。`stop` 只终止同时匹配 PID、worker 路径和私有 owner token 的本工具进程，绝不接管或杀死非本工具服务。

该 profile 还声明 `previewProvider=yu-resource-tool-preview`。`server status`/`preflight` 会只读检查 7074 listener 的进程命令、8091 listener PID 与 `GET /api/preview/status`；当两个端口确属同一资源工具进程，但 API 状态与真实 HTTP route 冲突时，稳定返回 `RESOURCE_TOOL_PREVIEW_STALE_STATE`。报告保留原始 route 分类于 `causeCode`/`transportCauseCode`。

UIAudit 永远不会对资源工具调用旧 `/api/preview/stop`、`/api/preview/start`，也不会 taskkill、`Stop-Process`、`os.kill` 或清理端口占用者。sibling provider 的安全实现已按 `contracts/yu-resource-tool-preview-lifecycle.v1.json` 落地并通过 9/9 测试：status 返回 control/preview PID、generation、thread/socket/HTTP-ready，recover 仅关闭本进程记录的 HTTPServer/thread，直接 bind，地址冲突即拒绝。只有 `server start`/`run --ensure-server` 且这些字段完整、PID 与 7074/8091 listener 精确一致、状态为 self-owned stale 时，UIAudit 才发送一次携带原 token 的 `/api/preview/recover`；CAS 变化或任何字段缺失均零写硬停。`preflight` 仍完全只读。已经长期运行、尚未加载新 provider 代码的旧进程会继续返回 `RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_REQUIRED`，需要用户自行重启资源工具后才具备安全恢复能力。

## 完成范围保护与计时

`policies/completed-scopes.json` 保存用户确认完成且要求冻结的 route id/前缀。preflight 固定检查 `completed-scope-guard`；直接命中时返回 `COMPLETED_SCOPE_REOPEN_REQUIRED`。相邻页可以导航过境，只有 `scope.reopen[]` 提供新运行证据、用户证据或共享影响的时间与引用时才允许精确重开。

每份运行报告都保存 `preflight.timing`、`timing.phases[]` 和逐步 timing。汇总分开 action/evidence、sampling wait 与 configured settle，并列出最慢步骤。`fixedWaitSavingsCeilingMs` 是固定 settle 可被 ready 探针替代的理论上限，不表示允许省略真实动画、资源 ready 或稳定帧。跨实现阶段的主动/等待工时由 `route_timing.py` 单独原子落账。

Node 调用方可 `const uiAudit = require('./Tools/UIAudit')`，也可直接 require `lib/*.cjs`。`runPreflight` 是异步 API，必须 `await`。

## 页面 route 合同

正式 route 只保存 route map、selector、控件名和断言数据，不复制登录、运行树、JSON、弹窗、协议或 server 启动逻辑。

### 启动弹窗

`policies/startup-popups.json` 只允许 source-backed 的精确身份。配置队列弹窗按 `ClientConfigPopupLevel.sort` 处理；真正执行关闭时仍以当前 `Laya.stage` display list 为准，按每层实际 child order top-first 排序，禁止用 loaded-view 枚举顺序猜栈顶。不在该配置且由回包直接打开的弹窗禁止伪造 sort，只能使用真实 runtime stack。栈顶身份无法调和时返回 `POPUP_RUNTIME_STACK_UNRESOLVED`，栈在点击前变化时返回 `POPUP_RUNTIME_STACK_CHANGED`；上层是未知/危险弹窗则先硬停，上层是已授权安全弹窗才先处理它。`CycleimpActlistYesterday` 是直接回包打开的一类：由 `22703` 回包在推送或当日首次登录、榜单非空且角色等级至少 150 时直接打开；唯一安全面是 `_btn_close`。关闭后必须连续两个已推进的 Laya 帧都确认“被点击的具体实例不再打开、也不在 stage 可见”，重复 frame token 或中途重现均不算关闭完成。未知弹窗仍一律 `unknown-hard-stop`。

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

selector 仍支持 `dataIdentity` 子集匹配，例如 `{ "dataIdentity": { "fashion_id": 12010008 } }`。runtime node v3 另外保留 `childIndex/zOrder/alpha/effectiveAlpha`、`mouseEnabled/mouseThrough/hitTestPrior/_mouseState`、`runtimeClass/systemOverlay/hitArea/eventListeners` 和 mask/scroll clip 信息，供栈与输入诊断使用。

click/drag 不再把目标自身 `hitTestPoint=true` 当作可达证明。公共输入层优先锁定 `Laya.Render.canvas.source`，通过当前 `stage._canvasTransform` 做“逻辑坐标 → 浏览器 client 坐标 → Laya 逻辑坐标”往返，并验证 `document.elementFromPoint` 的顶层 DOM 元素仍是该 canvas。随后按当前 Laya `MouseManager.check` 的真实逆序 child traversal 取 topmost hit；只有 topmost 是目标或目标子节点才允许输入。诊断同时保存 stage→目标链、stage→实际命中链、共同祖先后的遮挡分支、每层 `childIndex/zOrder`、鼠标策略、可见性、alpha、mask/scroll clip、独占 capture 与 Canvas 映射。稳定分类为：

- `canvas-coordinate-mismatch`：Canvas transform 往返或边界不一致；
- `overlay-intercepted`：同一 owner 内的 sibling/DOM overlay/捕获层拦截；
- `stack-order-wrong`：另一个 View 在当前真实 stage 栈上方；
- `event-not-dispatched`：浏览器 down/up 已到 canvas，但绑定字段未收到 `Laya.Event.CLICK` 或没有业务 listener；
- `target-click-consumed`：绑定字段收到 CLICK 且原业务 listener 被派发。

真实 click 只发送一次。输入前安装短生命周期 probe，输入后立即恢复：记录 canvas DOM down/up/click、目标字段的 Laya 事件派发、点击前 listener 数量，以及 `Close/CloseView/Hide/DeleteMe/Remove` 语义调用；不会补第二次点击或改坐标重试。源码依据是老端 `cdn/libs/laya.core.js` 的 `MouseManager.check/_checkAllBaseUI` 与 `TouchManager.sendEvents/onMouseUp`，以及 `h5/src/util/Util.ts::AddClickEvent`。

若唯一身份数量不符，`CANVAS_TARGET_IDENTITY_MISMATCH` 会携带 `ui-audit.selector-diagnostic.v1`。runner 在失败 run 中写入 `selector-diagnostic-*.json`，内容包含目标 View 的最小规范化 stage 子树、按 owner/运行名/绑定字段评分的候选、stage/source 摘要、子树 SHA-256 及诊断内容 SHA-256；`ui-audit-report.json.failure.diagnostic` 和 `artifacts[]` 同时引用它。即使登录阶段尚未产生常规 runtime snapshot，也不会再留下 `runtime-tree=0` 的盲区。

输入前失败使用 `canvas-input-diagnostic` 或 `popup-runtime-stack-diagnostic`；仍走同一不可变 selector 诊断写入器，故即使 route steps/runtime-tree/trace 尚为 0，也会落盘目标子树、候选、topmost/遮挡链、Canvas mapping 和内容 SHA-256。

`POPUP_CLOSE_NOT_STABLE` 使用 `popup-close-lifecycle-diagnostic`。除 selector、最终子树和候选外，`context` 保存点击前子树/哈希、具体实例锚、完整输入 probe、每次采样的 frame token、loaded/managed/stage 判定来源和最终分类：`business-handled-but-not-closed`、`event-not-dispatched`、`click-not-consumed`（兼容无旧输入证据）、`closing-timeout`、`requeued`、`frame-not-advancing` 或 `still-visible-or-managed`。其中绑定字段已收到 CLICK、业务 listener 已派发但六帧仍保持 open，会明确归类为 `business-handled-but-not-closed`，不会再与遮挡或坐标错误混在一起。

历史 `step.expect` 不会被当成注释跳过，而是在 schema/preflight 硬停。应改成上述可执行动作。

## 公共 API

| 模块 | 职责 |
|---|---|
| `lib/session.cjs` | Edge、登录、选角、进城、启动弹窗和热会话 |
| `lib/safe-json.cjs` | 只丢祖先环、保留共享引用的 JSON 与原子写入 |
| `lib/runtime-tree.cjs` | loaded/managed/`Laya.stage` 统一节点 schema、数据身份、display order 与 mask/输入属性 |
| `lib/runtime-overlay.cjs` | 受管背景、全局输入门和未知全屏拦截的归属、策略判定与 observed stack 视图 |
| `lib/selector-diagnostic.cjs` | selector 失败的最小子树、候选、内容哈希与不可变诊断写入 |
| `lib/canvas-input.cjs` | Canvas transform 往返、Laya topmost/遮挡链、一次真实 click/drag 与事件消费 probe |
| `lib/popup-policy.cjs` | `allow/forbid/unknown-hard-stop`、配置队列/observed runtime stack、安全节点与稳定关闭帧 |
| `lib/popup-lifecycle.cjs` | 被点击实例锚定、loaded/managed/stage 关闭调和与失败分类 |
| `lib/item-use.cjs` | ItemUse 精确身份、稳定帧、队列和一次受控关闭 |
| `lib/protocol-probe.cjs` | transport 级收发 trace、handler 惰性关联、读写分类、required/forbidden 与 policy 闸 |
| `lib/route-assertions.cjs` | 节点、条件分支、几何、裁剪和滚动断言 |
| `lib/runtime-probes.cjs` | 声音调用与 RenderTexture 非透明像素 ready |
| `lib/server-readiness.cjs` | 无浏览器 GET/HEAD readiness、逐请求耗时与底层网络/内容分类 |
| `lib/server-lifecycle.cjs` | listener/PID/进程/owner 调和、有界 transient retry、后台 start/status/owned stop |
| `lib/resource-tool-preview.cjs` | 资源工具 7074/8091 同源身份、status 调和、旧 provider 零写闸与严格 CAS recover |
| `lib/completed-scope.cjs` | 用户确认完成范围、精确重开合同与 preflight 保护 |
| `lib/preflight.cjs` | 版本、依赖、authority、完成范围、route 合同、URL 与输出闸 |
| `lib/route-runner.cjs` | 数据化步骤、真实截图/snapshot/trace、分层计时和结构报告 |

## 不可变证据

`output/` 整体由 Git 忽略，只保存每次运行的新目录。公共代码、策略、fixture、schema 和文档必须进入 `Tools/UIAudit` 或 `Docs`。写入器拒绝覆盖已有文件；同一路线复验必须新建 run-id。

设计、迁移来源和边界见 [UIAudit 公共采集与探针基础设施](../../Docs/RuntimeCompare/UIAudit公共采集与探针基础设施-20260811.md)。
