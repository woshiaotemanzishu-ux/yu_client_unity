# 天穹战歌 UI 路由巡检运行手册

## 当前入口

- 老客户端工具目录：`E:\GitProject\yu_client\tools\yu-resource-tool`
- 启动命令：`npm run dev`
- 启动预览：向 `http://127.0.0.1:7074/api/preview/start` 发送空 JSON 对象
- 老端页面：`http://127.0.0.1:8091/index.html`
- Unity WebGL：先从 `Docs/打包发布手册.md` 确认；2026-08-03 试跑地址为 `http://223.109.142.26:89/web/`
- 测试账号：`111111` / `111111`

启动命令要放在隐藏后台进程中。结束本轮时只停止本轮启动且已核实 PID 的老端工具进程，不扩大到其他 Node/Unity 进程。

## 验收事实源

- 当前老客户端在同账号、同状态、同 viewport 下的真实运行最终表现，是功能、状态、几何、资源、动画时序、合成和生命周期的验收目标。
- 老端源码、配置与完整运行树用于解释该结果；Prefab 是 Unity 侧实现源；转换器、公共架构、静态断言和 Editor 预览都不能反向定义验收标准。
- 任一实现手段与老端最终表现冲突时修改 Unity。老端存在版本或状态分支时，先记录实际入口、版本、账号状态与时间点，不得挑方便复刻的变体。
- 用户运行证据可推翻历史绿灯；通过时也只关闭明确观察到的范围，不得顺带把另一状态、WebGL、清理生命周期或整页标绿。

## GM 测试态准备

- 缺等级、功能开放、任务、物品、货币或成就进度时，先按 [gm-test-state.md](gm-test-state.md) 保存并执行最小状态配方，不等待用户手工养号，也不把可由现有 GM 通道解决的问题登记成页面 blocker。
- 默认同账号对照仍用 `111111`。需要不可恢复写入、互斥状态或反复重置时创建专用测试号；`123123` 只作为有记录的满解锁辅助号，不直接替代 `111111` 做 diff。
- GM 只准备前置态。被测的领取、升级、穿戴、购买等事务必须继续从真实 UI 发出，并验证回包、即时刷新和重开；禁止直接用 GM 把目标结果改完后宣称功能通过。
- 状态配方、执行日志和权威探针进入本轮不可变证据目录，并作为 `runtime_state/state_evidence[]` 绑定台账。

## 日常无头对比

- 常规对比使用 `Tools/UIAudit/` 复用 `Tools/headless/` 的 Puppeteer/系统 Edge 依赖，不使用 Computer Use。老 H5 与 Unity WebGL 均在后台浏览器中按真实 Canvas 指针事件操作；Browser MCP 只用于首次探索或排障。页面路线必须是 JSON 数据，不得重复实现登录、运行树、序列化、启动弹窗、`ItemUseView` 或协议 trace。
- 执行前先跑 `node Tools/UIAudit/cli.cjs preflight --route <route.json> --output <new-run>`；Node/Edge/Puppeteer、策略、authority、凭证、route schema 或不可变输出任一失败都硬停。未知启动弹窗、危险关闭面、未知/格式错误协议同样硬停，不猜右上角、中心点或 Controller。
- 同一种基础设施故障第二次出现时停止页面工作并晋升 `Tools/UIAudit`，补确定性 fixture 后再恢复页面。`output/` 只存不可变截图、snapshot、trace 和报告；公共代码、策略、schema、fixture 不得放入其中。
- 同账号单会话必须先完成老端路线并确认断线，再登录 Unity。固定保存 `720×1280` 移动端和一个宽屏 viewport 的 old/unity/overlay/diff。
- Unity 页面采证前先核对当前源码指纹、Player 哈希和服务器 catalog 哈希。首次或内容状态不可信时运行 `BuildAllWebCli`；只改 C# 且持续工作区保留成套内容时才运行 `BuildWebShellOnlyCli`；纯 Addressable Prefab/资源改动只构建内容。缺少精确同批证明时禁止继续对比。
- 当前壳构建预算按 12～25 分钟。Headless 只消除手工点击和截图整理，不消除构建时间；整包按页面/逻辑批次执行，页面 `done` 前至少有一份当前真实包报告。

## 组件级快速回归

- 页面枚举后先输出组件依赖清单：`页面节点 -> 共享 Prefab/View 路径或 GUID -> 数据/状态/回调输入`。同构结构不得靠页面内复制节点或硬编码坐标维持。
- 组件状态矩阵按适用项覆盖：特效开/关，1/2/3 项整体居中，短/长文本不误换行，单/双按钮居中，充足/不足，选中/未选中，空/有数据及目标 viewport。
- 修改共享组件前先列全直接消费者，并按直接/嵌套、展示/交互、空/有数据、特效开/关、宿主缩放/viewport 分组。运行态默认验目标页并对每个实质不同的组抽一个独立代表，通常共 2～4 个宿主页；根组件、Bind 或生命周期变化时必须包含一个高频既有页面。样本失败才扩大同组检查，公共 API/字段删改、整体换引用或持续失败时才全量核对。
- 特效组件还要记录老端真实调用参数与归属，并做目标 Handle 隔离足迹：`effectName/parent/position/scale/rotation/loop/renderSize` 缺一项就不能只凭资源名补猜。旧端数值需经过转换资源、宿主、profile 与通道映射后校准，不直接复制到所有 Unity 宿主；输出两个不同动画时间点的目标 PNG、动画时间/材质属性推进值、两帧像素差、非透明像素数与 alpha 包围盒宽高，单帧、`isPlaying` 或几个亮点都不算动态特效。共享 RT/RawImage 的列表项还要核对曲线属性确实被 shader/material 消费，并保存完整可见、部分裁切、完全滚出祖先 `RectMask2D/Mask` 后目标 alpha 为零的三态证据。
- 快速循环先加载真实共享 Prefab，保存局部截图、组件 GUID/实例链和页面根几何；组件通过后只沿原路线做一次整页点击、弹窗、返回与截图复验。页面最终 `done` 仍要求当前真实 Web 证据。
- 用户新截图推翻旧结论时，把对应组件变体、目标页和代表样本降级，先修组件再回卷；未受影响的组件证据可以复用，不机械重跑所有引用页。若代表样本暴露同类回归，再扩大该使用形态的抽查范围。

## Canvas 坐标

每次点击前在浏览器中读取 canvas 矩形：

```javascript
const canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
const rect = canvas.getBoundingClientRect();
({ x: rect.x, y: rect.y, width: rect.width, height: rect.height, scrollY: window.scrollY });
```

Unity 设计分辨率是 `720×1280`。设计坐标到浏览器视口坐标：

```text
screenX = rect.x + designX * rect.width / 720
screenY = rect.y + designY * rect.height / 1280
```

老端外层页面也可能缩放并居中竖屏画布，必须读取实际画布矩形，不固定假设 `1280×720` 截图中的偏移。页面自动滚动后重新读取矩形。

### 弧形页签与自动尺寸标签的几何预检

这类节点在截图里只表现为“位置不对”，但修复前先生成同一套页面根几何合同，避免逐点试坐标：

- 老端运行态记录 `pageRect={x,y,w,h}`、`referenceCorner`、最终 `runtimeSize`、父级 scale/rotation/skew、`anchorX/Y`、`centerX/Y`、折叠/展开和选中/红点状态。设计 JSON 的 `width/height=0` 只作为线索，不能覆盖运行时 bounds。
- Unity 用 `GetWorldCorners` 后换算到页面根左上角，记录同名字段以及 RectTransform pivot。多个同尺寸节点若统一偏移半宽/半高，先修参考角/pivot，不改单个槽位数字。
- 弧形、放射形等离散位置在当前 Prefab 保存为 `__Slot0..N` 具名槽位；连续文本才进入只含文字的 LayoutGroup。背景、唯一点击面和状态图标不参与文字 preferred-width 布局。
- 一次采集全部相关状态：折叠/展开、选中/未选中、红点有/无、短/长数值。输出 `old_rect/unity_rect/delta/state/source_fields` 表后再动 Prefab；通过后沿原页面只做一次整页复验。

成就页已验证的判别样例：65×70 弧形子按钮统一偏移 32.5/35 是中心 pivot 把老端左上坐标误解释成中心坐标；一级/二级竖排文字应以运行时 22×91、22×46 为准，不能照抄 Laya `height=0`；属性行重叠来自空模板固定宽度，使用纯文字容器的 TMP preferred width 后消失。

## 证据命名

建议目录：`output/ui_route_audit/YYYY-MM-DD_<route>/<run-id>/`。同一路线每次复验创建新 run 子目录，不覆盖旧截图、日志或报告；`Tools/UIAudit` 写入器默认拒绝覆盖。台账中的正式文件引用同时记录 SHA-256。整个 `output/` 已从 Git 跟踪中移除并被忽略，磁盘证据仍保留；需要版本化的内容必须迁入正式工具或文档目录。

- `old_00_entry.jpg`、`old_01_target.jpg`
- `unity_before_00_entry.jpg`、`unity_before_01_failure.jpg`
- `unity_after_00_entry.jpg`、`unity_after_01_target.jpg`
- `old_console.json`、`unity_before_console.json`、`unity_after_console.json`
- `old_<state>_canvas.png`、`unity_<state>_canvas.png`、`diff_<state>.png`
- `route_matrix.md`

截图保持同一浏览器窗口尺寸；视觉比较时另外保存裁剪到游戏画布的图，不覆盖原始全窗口证据。

2D UI 以画布裁剪图做半透明叠加和差异图；模型区先比较模型是否存在、资源/部件、朝向、镜像、翻转、角度、位置、比例和特效。Unity 与 Laya 的 Shader、抗锯齿和动画采样不要求逐像素值相同，但不得用这一点豁免模型缺失或明显构图错误。

## 动态特效时间基线

动态或重复特效禁止从一张最终截图直接开始调参数。先在老端同一运行会话保存“开始、早期发射、散开/展开、移动、抵达、清理”的不可变时间序列，再按症状分层：位置/越界查宿主与裁切；数量、相位和随机形态查 `use_cache/cache_key/source_simulation_count/display_copy_count/composition_order`；数量、大小、方向已经一致但出现灰片、变色或透明区域实心化时，查离屏 RT 与最终 UI 合成，不得继续把拓扑当根因。

离屏合成证据固定保存以下同时间点数据：源纹理 RGBA 抽样（特别是 `alpha=0` 的 RGB）、粒子材质 RGB/Alpha blend、未经 RawImage 的 RT RGBA、最终 Canvas 像素、RT 格式/读写色彩空间、合成 shader/blend、Editor/WebGL 平台。再各选一个同 `UIEffectStage` 链路下正常显示的 UI 特效和一个场景内正常特效作控制。一个共享 RT 中混有覆盖型、标准 Alpha 与 Additive 子材质时，不允许靠单个全局“亮度当 Alpha”规则解释所有资源；若需不同最终合成，必须按合成模式拆通道或中间表面。

成就奖励飞行的只读老端表现基线可用：

```powershell
node Tools/Conversion/mainui_login_capture.cjs <account> <password> <outdir> http://127.0.0.1:8091/index.html reward-fly-baseline
```

该 route 直接调用老端展示函数并采集 0/80/250/450/750/1150/1500ms，只证明视觉时间基线，不证明 40905 领取事务、账号状态或 Unity 修复通过。

## schema 6 台账运行批次

- 新路线先由 `route_ledger.py init` 从 manifest 生成 schema 6 正式账；已存在目标会被拒绝，不允许用 init 重置。历史 schema 2～5 文件继续保留，不原地抬版本。
- manifest 是该版本台账的不可变拓扑合同；其哈希、路线名、节点集合及 `parent/type/risk/control_inventory` 都会复核。发现新条件控件或树结构变化时另建版本账，不在旧账上改树续绿。
- 每次 static / Unity Editor / real Web / 用户运行观察都分配唯一 verification run。全部 run 记录 Git HEAD、dirty 指纹和带时区时间；真实 Web run 再记录 Player/catalog SHA-256、两档 viewport、老端断线、Unity 有效会话与 Headless 报告。
- 路线脚本只写本批 `results.json`；正式账唯一写入口是 `route_ledger.py apply`。候选校验失败时正式文件不变；同一路径的第二个并发写者由进程锁明确拒绝，读取最新账后再重放本批 results。
- 用户新图推翻旧证据时，在 results 中写 `invalidate_gates/invalidation_reason/observed_at`。旧绑定进入 `evidence_history`，不能继续靠历史 `true` 维持完成。
- 根页回卷为非完成态时清除旧 `route_run_id`。重新收口的真实 Web run 必须不早于全部完成叶引用的 run，并与它们保持相同 Git HEAD/dirty 指纹；否则旧 Web 包不能替新 Editor 修复背书。
- 完整字段与结构化 effect/scroll/component 合同见 [route-ledger-schema.md](route-ledger-schema.md)。

## 深度优先执行顺序

1. 打开父页，先列出当前所有可见和条件显示的同层控件，不立即判断“基本完善”。每个控件都要写入页面 `control_inventory[]` 并映射到直接子节点；页签点击通过不能覆盖页签内部按钮。
2. 选一个子节点，把它展开到最终业务结果。有子页就在子页再列全量同层清单，然后继续选一条向下。
3. 对事务执行“提交前 → 等待中 → 成功/失败 → 父页即时刷新 → 关闭重开”。
4. 对跳转执行“点击 → 首屏可见 → 可交互就绪 → 目标页版本核对 → 冷/热再打开”。
5. 只有这个叶子的所有阶段通过才标 `done`，然后回到父页选下一个兄弟节点。
6. 对列表先验容器树，再从可见子项经 `GraphicRaycaster` 做真实拖动；只看横向排列和子项数量不能证明可滚动、可裁剪或末项可达。
7. 对弹窗记录“触发格 → 目标 View 类型 → 主底图 Sprite → 根尺寸 → 遮罩关闭”的身份链。列表中每个可见格都逐个点击，不能由同排一个成功项代验。
8. 关键位置统一输出页面根左上角矩形；局部 `anchoredPosition` 只用于解释锚点，不作为跨容器视觉结论。

当老 Web 与 Unity 使用同一账号，且服务器是单会话或后登录会踢前登录时，禁止两端同时在线做对照。必须在相同 viewport、角色、页面和状态下顺序采证：先在一端截图并完整退出/确认断线，再登录另一端复现。被踢端若只剩场景/HUD 残影而 `RoleModel` 或协议状态已清空，该证据无效；cold/warm 时间也必须各自在有效登录会话内测量。

默认记录 `click→first-visible` 和 `click→interactive-ready`。首次打开为 cold，返回后立即再打为 warm。项目无明确阈值时，超过老端 2 倍或 2 秒是默认告警线；5 秒以上必须登记缺陷并拆出资源、配置、协议、建树和串行等待耗时。

## 安全点击边界

- 只读：页签、展开、返回、复制、打开弹窗，可直接完整验收。
- 可恢复写入：音量、屏蔽、自动拾取等。先记录原值，改变一次，确认回包/刷新，再改回原值。
- 破坏性：删号、消费、付费、发奖、退出账号、切换角色、最终修复/重载。默认只验收到二次确认框并取消；需要执行时必须有本轮明确授权。

## 设置路线的当前结论

老端设置固定入口应打开 `SettingView`。基础页包括角色信息、复制 ID、更换头像、改名、四个滑条、自动拾取和底部五个操作；屏蔽页有十个选项。更换头像的路由语义是 `OpenFun(203, [DressType.Head])`，但“能跳转”不等于该叶子完成。

Unity 主界面固定入口必须绑定到 `_img_setting/_img_friend/_img_shop` 这些可见 `Graphic`，并以真实射线点击回归；只绑定外层 `_box_*` 会出现“图标可见但 WebGL 点击无响应”的风险。

2026-08-03 用户复查后，设置路线曾因改名即时刷新、头像误路由和 5 秒以上冷开重新打开。第 2 轮已完成全部功能叶子；2026-08-04 又因像素视觉、模型和运行态状态证据不足再次重开。当前经验是：

- 功能、协议和即时刷新通过，不代表视觉与运行态通过；用户的新截图可以直接回卷历史 `done`。
- 时装/发饰要逐条点击基础色和非基础色，核对实际材质名与模型；套装要逐一点击四页，不能只看第一页。
- `UIModelStage` 换模型时必须先失活旧实例再延迟销毁，否则非 PlayMode 截图或同帧 `RenderNow` 会把上一页翅膀/武器累积进下一页。
- 时装资源预检必须覆盖 `config_fashion_color.active_cost/star_cost`，并与全局 `SpriteImporter` 的材质例外一致。连续第二次预检必须 `imported=0、configured=0`，玩家点击后 `addedResources=none`。
- 当前第 3 轮实测：预检闭包 712 项，第二次 `imported=0、configured=0`；设置→头像首开 1867ms、热开 71ms；四套装模型、挂载部件、常驻特效和战力增量条已按老端截图复验。
- 冷开头像还要在约350ms/1000ms留证，禁止用粉色占位图或固定延时冒充ready。套装页除四个预览页签外，还必须点“更换→确认”，验证41302及父页即时切到“已更换”。
- 第 4 轮补充：`_list_fashion_item` 曾绑定到无 Viewport/Mask 的重叠横排节点，截图看似有列表但不能拖动；套装 `Image_130` 曾在父容器 x=599 下继续右锚 x=-45，最终页面坐标右偏 65；四个条件格曾继承 `BaseAwardItem` 默认点击，误开通用小物品窗。以后这三类问题分别由 `layout_structure+scroll_interaction`、`page_space_geometry`、`target_identity` 阻断。
- `IllusionTips.scene` 的 `_img_bg` 本来就没有静态 skin，老端按 `goods.color` 运行时加载 `common4/other/ui_tips_pzbg_1..7`。Unity 验收必须等待 Sprite 实际绘制，并把七张背景纳入点击前资源闭包；只断言 426×772 尺寸会让“底图透明、文字浮在父页上”假通过。
- `UIModelStage` 的 RawImage 拿到 RenderTexture、场景中存在 Renderer 都不等于已经出帧；必须在专用相机实际 `Render()` 完成后置 ready，并读取 RT 非透明像素形成 `render_evidence[]`。固定延时不能替代实际出帧探针。
- 详情弹窗需同时记录配置字段语义、详情/来源等动态组矩形、preferred height 与背景包围盒；组间重叠或字段放错容器即回卷。最终截图使用新的不可变目录，不覆盖已被查看器映射的 PNG。

## 两级验收与 Web 构建边界

- 快速循环：Browser MCP 取老端运行事实，Unity CLI 加载真实 Prefab，用 `GraphicRaycaster→PointerClick` 验证点击面和路由。当前没有可用 Unity 专用 MCP 时，这是不占用用户电脑的默认通道。
- 批次收口：累计多条路由后再打 WebGL，重新登录并复走。不要让 Web 冷启和打包时间阻塞每一项小修复。
- 新建隔离工作树没有与当前内容匹配的 Addressables 构建状态时，禁止直接用 `BuildWebShellOnlyCli` 发布。2026-08-03 试跑已复现本地 catalog 缺 `prefabs/ui/login/loginstage` 导致新壳无法启动。使用持续构建工作区的已验证内容状态，或完整重打并成套发布内容+壳。
- 禁止把旧壳的 `StreamingAssets/aa` 手工拼到新 player 当成可验收产物；两者没有成套验证时只能作排障，不能发布。
- `AddressableAssetSettings` 的 Player Build Option 序列化值 `0` 是 `PreferencesValue`，并不等于禁止随 Player 构建内容。shell-only 必须在 Player 构建前强制 `DoNotBuildWithPlayer`，在 `finally` 恢复，并验证构建前后 catalog 未被意外改写。
- 本地 Web 若烧包默认仍是 `{streaming}/cdn`，必须通过 `?cdn=http://127.0.0.1:8090/res` 或壳同目录 `boot_config.json` 覆盖。卡在 90% 时读取浏览器日志；出现 `/StreamingAssets/cdn/WebGL/*.bundle` 404 先修 CDN 基址，不重打内容。
- 运行时克隆 `BaseView` 页签/格子时调用 `Show/Hide`；只 `SetActive` 不会触发 `OnInit`。动态列表把高度写到真正的 `ScrollRect.content`，规范顶部锚点并强制布局；拖动从可命中子项开始，必须验证隐藏末行可达。

## 转换页的三项快速核对

- **先算有效变换，不先目测调数值**：老端节点的最终尺寸必须包含父节点 `scale`、运行时 `SetScale` 和 `anchor/skew`。例如 `anchorX=1 + skewY=180` 在 Unity 中应保留为围绕右轴的 `localScale.x=-1`；漏掉翻转会让相邻页签底图压住文字，漏掉第二级 `SetScale(0.8)` 会把 80px 物品格放大到约 100px。
- **重名 `Content` 只认序列化引用和实际组件树**：`Panel → List` 转换后可能出现外层包装、内层 `ScrollRect`、布局 Content 三个同名节点。先从 Bind 已序列化引用向下定位唯一的 `ScrollRect → Viewport(RectMask2D) → Content(LayoutGroup/ContentSizeFitter)`，禁用重复外层滚动，再把克隆项挂到该 `ScrollRect.content`；不得对同名节点直接 `GetComponent` 后凭空值回退到错误容器。
- **CLI 首次点击前先真实渲染一帧**：`ScreenSpaceCamera + RenderTexture` 在 batchmode 首次 `Camera.Render/Capture` 前，页面虽然已实例化，`GraphicRaycaster` 仍可能返回空命中。入口页以及首次激活的缓存弹窗都先留一张不可变首帧证据，再执行 `GraphicRaycaster → PointerClick`；这能区分业务入口失败与验收脚本尚未建立射线几何。
- **原始日志直接写入不可变证据目录**：Unity 重开项目时可能清理项目 `Temp/`，因此正式复验的 `-logFile` 不得继续指向 `Temp/cliverify_*.log`。应与本轮截图一起写入新的 `output/ui_route_audit/<route>/cli/<timestamp>/`，结果台账也只引用该持久路径；`Temp/` 仅允许承载可丢弃的调试跑法。
