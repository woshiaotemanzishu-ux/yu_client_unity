# AGENTS.md

本仓库的 AI 编码约束统一维护在:

- [.github/copilot-instructions.md](.github/copilot-instructions.md) — 精简红线(GitHub Copilot 自动加载)
- [Docs/Shenxiao编码规范.md](Docs/Shenxiao编码规范.md) — 完整编码规范
- [Docs/Shenxiao重构实施方案.md](Docs/Shenxiao重构实施方案.md) — 整体方案与架构
- [Docs/LayaUI转换流水线.md](Docs/LayaUI转换流水线.md) — UI 主路线:粒度/烘焙/Bind/验收规矩
- [Docs/Shenxiao登录链路.md](Docs/Shenxiao登录链路.md) — yu_client→yu_gm→yu_server 链路与协议出处
- [Docs/Shenxiao进游戏链路.md](Docs/Shenxiao进游戏链路.md) — 选角/创角后 MainUI、地图、主角、NPC/怪物、弹层的阶段接管规矩

## 本机项目全局记忆

- `D:\git_res\yu_client` 是老客户端；这台电脑的主要工作是把这个老客户端重构到新客户端。老客户端用于查协议、资源、旧端行为和对照，不要默认把旧端技术债务搬到新客户端。
- `D:\git_res\yu_client_unity` 是新 Unity 客户端，也是当前准备重构和持续接管的客户端。重构时按全新客户端思路做，只保留必须兼容的资源、协议和运行时行为。
- `D:\git_res\yu_client\tools\yu-resource-tool` 是老客户端里的 Electron 资源管理项目，大部分资源管理、导出、检查、修复工作优先在这里找入口或补工具链。
- `D:\git_res\yu_server` 是服务端，主要是 Erlang 代码；服务端改动通常需要上传到服务器后编译并重启。部署前先检查 `%USERPROFILE%\.ssh\config` 的服务器 Host 信息，并检查是否有 SFTP 配置；当前已知 SSH Host 有 `aliyun`、`jzy`、`sg`，当前已知 SFTP 配置在 `D:\git_res\yu_gm\.vscode\sftp.json`。
- 读取配置表的功能不得在业务代码里补硬编码兜底；宁可让配置缺失导致功能残缺并暴露缺表，也不要把任务、引导、活动、奖励、入口、资源名等写死在代码里。需要补表现时先补真实配置/同步工具/读取器。

## Unity MCP 连接记忆

- 连接 Unity MCP 服务前，先检查是否存在残留的 Unity MCP bridge/relay 进程，重点看 `relay_win.exe`；残留桥接会占满槽位导致新连接失败。确认是僵尸桥后，直接结束该残留进程，再重新连接 Unity MCP。

## Codex 独立工作树与 Unity 性能约束（2026-07-21）

- 当前“定时迁移”是持续任务：除非用户明确喊停，或工单中的可迁移内容已全部完成，否则每完成并提交一包就直接进入下一包，不要停下来征询“是否继续”。技术问题由 Codex 自行定位、实现和验证；只有产品取舍、权限、不可逆操作或多个正确方向需要用户拍板时才提问。
- 用户日常打开和精修的 Unity 项目是 `E:\GitProject\yu_client_unity`，当前工作分支是 `feat/ui-adaptive-anchors`；Codex 自动迁移固定使用 Git worktree `E:\GitProject\yu_client_unity_codex`，分支是 `codex/automation-workspace`。独立目录由提交 `40e68b1dcb836ff59d2e8dc00d5392ad622aaadd` 建立，不是普通文件夹副本。
- 每轮开工、锁定 Unity 自动刷新和下发实现任务前，都必须再次核对 `E:\GitProject\yu_client_unity_codex` 的 `git branch --show-current` 为 `codex/automation-workspace` 且工作树状态符合预期；外部窗口可能把同一 worktree 切回 `main`。若分支漂移，先中止实现代理，确认工作树干净后切回正确分支，再重新下发，禁止让代理在错误分支落文件。常驻 Editor 遇到这种整树切换会触发域重载，重载期间 CLI 短暂 `unreachable`/`401` 时先用 `unity status` 等待恢复，不要重复启动 Editor。
- 两个工作树必须使用不同分支。用户在原目录提交后，Codex 开工前先检查两边状态，再把 `feat/ui-adaptive-anchors` 的新提交合入 Codex 分支；Codex 阶段成果在独立分支提交，验收后再合回用户分支。不要假设另一目录里的未提交修改会自动同步；同改 scene、prefab、`.meta` 或大资源前要先协调，避免二进制/序列化冲突。
- `Library/`、`Temp/`、`obj/` 等 Unity 缓存不在工作树间共享。Codex 工作树初建时没有 `Library/`；第一次打开必然进行完整导入，属于高负载操作，只能安排在用户明确空闲的时间窗口，不能为了普通代码检查擅自启动。
- 所有 Codex 任务全局最多只能有一个任务调用 Unity。用户的原目录正在运行 Unity 时，Codex 默认只做代码迁移、协议/旧端对照和静态检查，不再启动第二套 Unity；Unity 验证集中到阶段收尾，不要每个小任务启动一次。
- 确需自动运行 Unity 时，先确认其他 Codex 任务没有 Unity 进程，使用低优先级和较小的 job/background worker 数，验证完成立即退出。不得让两个 Codex 任务并行启动 Editor、AssetImportWorker 或 ShaderCompiler。
- 2026-07-21 用户已明确授权：定时迁移允许在原项目 Unity 运行时启动第二个 `yu_client_unity_codex` Unity 做编译验证，不必逐次询问。实现包至少经过隔离工作树的 Unity `-batchmode -nographics -quit` 全项目脚本编译，才能标记“编译通过”；Roslyn/`dotnet` 仅作前置快检。第二个 Unity 仍必须单实例、整个进程树设为 `BelowNormal`，不得同时跑实现子代理的重负载任务。
- 2026-07-21 晚间实证：用户同时开着 `yu_client_unity` 与 `ArtsProject` 两个交互 Editor 时，Codex 再用“全核心 + BelowNormal”启动第三个 Editor，仍可能因并发脚本编译/ILPP 把机器拖到卡死重启；`BelowNormal` 不是资源上限。此机后续批处理统一使用 `Idle`、仅绑定 16～19 四个 E 核（affinity `0xF0000`）、`-job-worker-count 2`，且代码全部定稿后再集中启动，禁止边编译边改脚本。启动前必须按命令行里的 `-projectPath` 区分主 Editor 与 AssetImportWorker，不得误杀用户两个项目的子进程。
- `CliVerify` 的入口约定与纯编译/生成器不同：必须保留图形设备，且由用例自己的 `EditorApplication.Exit` 收尾，因此运行 `Shenxiao.EditorTools.CliVerify.*` 时**不要加** `-nographics` 或 `-quit`；否则可能只完成导入就以 0 退出，实际一行 `CLIVERIFY` 都没执行。验收必须在日志里同时看到具体 `VERDICT pass=True` 与 `CLIVERIFY EXIT 0`，不能只看进程返回 0。
- Unity 启动时会清理项目自身的 `Temp/`，所以两份 TMP 字体的运行前备份**不能**放在 `Temp/CodexVerify`；应放 `%LOCALAPPDATA%\Temp` 等项目外临时目录，进程退出后恢复并用 `git hash-object` 对照运行前/HEAD。`ClientConfigSync.SyncIfStale(true)` 还可能只因行尾把 `Assets/GameRes/resource/config/client/configfunctionicon.json` 标脏；若运行前该文件干净且 `git diff` 无业务内容，按运行前版本精确恢复，不要混入提交。
- 隔离工作树自己的 `Library/` 在首次全量导入后保留且不提交，后续只做增量编译；不得与原项目复制、共享或软链接。首次 batchmode 退出会由 TMP `InitializeFontAssetResourceChangeCallBacks` 清空动态字体缓存，已观察到 `Assets/_App/Fonts/DFPYuanW7 SDF.asset` 与 `FZYHJW SDF.asset` 被改写；每次 Unity 验证后必须核对 `git status`，只还原本次进程产生的这类明确副作用，不得把字体清空结果提交。
- 2026-07-21 的只读诊断显示：单个用户 Editor 会派生 2 个 AssetImportWorker 和 3 个 ShaderCompiler，Unity 进程合计约 7.9 GB、333 个线程；全系统约 357 个进程、8241 个线程，出现过 120～180 的处理器队列和约 8.8 万次/秒上下文切换。机器是 i7-12700KF、48 GB 内存、NVMe，检查时内存、页面文件和磁盘均未耗尽；卡顿主因应先排查 Unity 并发导入/编译、Defender 扫描和后台 Chromium/WebView 进程，而不是直接归因于硬件性能不足。
- 本机网络默认路由和 DNS 经过 `TAG Wintun`、`mihomo-tag`/`tagtunnel`。物理 Realtek 网卡到路由器检查时零丢包且没有断线记录；重负载时若仅本机“断网”，优先同时记录 TUN 进程响应、网关连通和公网连通，判断代理进程是否被调度饿死。不要未经用户同意修改 Defender 排除项、网卡节能或代理优先级。
- 本仓库约有 13.5 万个受控文件并使用 Git LFS。首次 `git worktree add` 可能超过命令包装器的超时，但底层 `git`/`git-lfs` 仍会继续检出；遇到超时先检查相关进程、文件数和目录体积是否持续增长，正常增长就等待完成，不要立即重建、删除目录或重复 checkout。最终必须用 `git status`、HEAD、分支、受控文件数和 `Assets/Packages/ProjectSettings` 完整性验收。
- 这台电脑的原仓库 `E:\GitProject\yu_client_unity` 保存共享的本地 LFS 对象库；Git 历史中的 LFS pointer 是正常存储格式，但工作目录里的资源必须是展开后的真实二进制文件。2026-07-21 已验证原工作树和 Codex 工作树各有 56,036 个 LFS 路径，全部为 materialized、指针内容匹配数为 0，共享对象库约 48,463 个去重对象/5.03 GB，`git lfs fsck --objects --pointers HEAD` 通过。新建 worktree 时出现 `git-lfs` 进程只是从共享本地对象库展开内容，不代表重新初始化或从远端拿到占位引用。以后怀疑资源为指针时先看 `git lfs ls-files -l` 的状态标记并执行 `git lfs fsck`，不要直接重拉或覆盖资源。
- 诊断还发现近 18 天存在 7 次无蓝屏代码的意外关机记录，以及三条不同型号的 16 GB 内存混插和 2023 年 BIOS。若意外关机不是用户在卡死后手动重启造成，需要单独排查内存、BIOS、电源和硬件稳定性；当前 WHEA 只有信息型厂商 CPER 记录，不能据此断言某个硬件已经损坏。
- 定时迁移的代理分工固定为“主代理总控、低成本子代理执行”：确定、重复、机械性的侦察和实现优先交给低推理强度代理；主代理只做范围裁定、协议/架构决策、diff 审核、Unity 验收和提交。子任务必须限定目录、产物和字数，禁止多个代理重复通读全仓库；实现代理不得启动 Unity，所有 Unity 操作只由主代理串行执行。
- 2026-07-21 已实测并采用官方 Unity CLI：本机二进制为 `C:\Users\FXL\AppData\Local\Unity\bin\unity.exe`（`1.0.0-beta.2`），隔离项目使用 `com.unity.pipeline@0.3.1-exp.1`，不得把 Pipeline 试装到用户日常工作的原项目。Codex 应让一个受限的隔离 Editor 常驻并通过 `unity status/list/command` 复用，避免每包重复启动 batchmode；首次安装 Pipeline 仍会触发一次完整脚本编译/ILPP，不属于轻量操作。
- Unity CLI 的内建命令优先于 Roslyn `eval`：实测 `editor_status` 约 0.6 秒，热 `eval` 约 1～2 秒，冷 `eval` 可能约 9 秒。`eval` 代码必须是完整方法体片段（例如 `return UnityEditor.EditorApplication.isCompiling;`），并同时检查外层 `success` 与 `data.result.success`，因为 Roslyn 编译失败时 CLI 进程仍可能返回 0、外层仍为成功。CLI 暴露了约 140 个工具，静态查询、重编译、测试、截图和构建优先使用已有工具，不要重复造 Editor 脚本。
- 常驻 Pipeline Editor 与低成本实现代理并行时，主代理应先通过 `eval` 调用 `AssetDatabase.DisallowAutoRefresh()` 和 `EditorApplication.LockReloadAssemblies()`；实现定稿并完成 diff 审核后，再调用 `UnlockReloadAssemblies()`、`AllowAutoRefresh()` 与一次 `AssetDatabase.Refresh()`，然后等待 `editor_status` 恢复 `ready/compiling=false`。这样可避免代理边写脚本、Unity 边反复编译。Pipeline 的 Roslyn `eval` 不能直接 `await Case.Run()`；用 `_ = Case.Run(); return true;` 启动并检查日志中的 `VERDICT ... pass=True`。若完整用例接近 30 秒导致外层 CLI 超时，但日志已出现 `pass=True`，等 Editor 恢复 `ready` 后，用反射调用该 Case 唯一的 private/static/零参数/bool 验证器，并要求 CLI 内层 `data.result.success=true/result=true`；调用前临时设 `ResManager.EditorPreferFallback=true`，调用后恢复，否则立即断言异步图片时会出现假失败。传统独立 batch 才要求日志中的 `CLIVERIFY EXIT 0`。
- Pipeline `eval` 在 Unity 主线程执行，**禁止**在其中对 `ResManager.LoadAsync` / Addressables 等需要主线程继续泵帧的任务调用 `.GetAwaiter().GetResult()`、`.Result` 或 `.Wait()`，否则会形成主线程互等，外层 CLI 超时也无法中止。正确做法是第一条 `eval` 用 `_ = Xxx.EnsureLoaded(); return true;` 启动异步工作，随后用独立的轻量 `eval` 轮询 `IsLoaded` / 结果字段。若误锁死，只结束隔离项目的 Editor PID，保留 `Library`，再按 `Idle` + `0xF0000` + `-job-worker-count 2` 原参数重启；不得碰用户日常 Unity 进程。

## UI 生成/修复记忆

- UI 静态结构、背景、窗框、皮肤、尺寸、默认图片、模板、Bind 回填、Addressables 分组等生成问题，必须优先修通用 LayaUI 转换链路、默认表或回填工具，然后通过 Unity Editor 菜单重新转换/回填/分组/验收；不要直接手工改 prefab 当作最终方案。
- prefab 变更应来自通用转换器或 Unity Editor 菜单生成结果。只有用户明确要求手调，或确认是一次性验收调整时，才允许手工改 prefab，并且必须记录原因和风险。
- 业务 View/Flow 只负责旧端运行时行为: 真实数据刷新、按钮事件、动态列表/模板实例化、运行时换图、角色模型、显隐状态和协议链路。不要用业务代码硬补本该由转换器生成的静态 UI。
- 独立 item prefab 被模块 prefab 作为嵌套模板引用时，给 item 新增业务子类不能只升级模块根 prefab；必须把独立源 prefab 也交给同一 Editor upgrader 重绑，再验证模块里的嵌套模板已解析到业务组件。ListDuobao 的 `ListGoodsItem.prefab` 就是这一类。
- Laya 的 `Box`（例如 `effectGp`）转换后可能只有 `RectTransform`，不能因节点名或用途就按 `Image` 绑定；纯显隐节点用 `Transform/GameObject.SetActive`，并在交互用例里断言 `activeSelf`。嵌套模板内外存在同名节点时，查找必须限制在直属父级或模板根作用域，避免误绑到子模板。
- 宝宝铭刻四个 orphan scene（BabyImprintView/AddImprintView/ImprintItem/AddImprintItem）必须先显式 `GenerateImprintStatic` 生成 prefab/Bind，等 Unity 编译后只跑 `UpgradeImprintStatic` 回填并嵌模板；增量回归不得重复 ConvertSingle。旧 JSON 误引用 `common/texture/com_rect_btn12.png`，真实同名字节只在 old alert/H5 镜像，已按 SHA256 `83ABC71B...DDEF558` 复制到新 common 路径并按 PNG LFS 管理；静态验收需确认主 prefab 真正引用该 Sprite GUID。
- BabyImprintView/BabyForgeView 都是 628×744 且没有 close 节点，尺寸与行为表明它们是 720×992 BabyEquipView 内的子面板，不是独立模态 Window；四个铭刻 prefab 已绑定无 UIViewAttribute 的业务子类，本地 item callback 必须保持零出站。后续只能在确认装备页内层级、切换与返回关系后嵌入，不能仅因 prefab 独立就注册 ViewManager 地址。
- 发现页面背景透明、窗框缺失、按钮皮肤/列表模板/九宫格/图片尺寸不对时，先归因为转换器、资源映射、默认皮肤、Bind 或运行时加载链路，优先找共性修复；避免逐页精修。

## 协议迁移补充记忆

- 宝宝装备的通用物品容器必须分开：`pos=36` 是已穿戴装备实例库，供 18205/18218 槽位里的装备实例 id 反查；`pos=37` 是待穿候选背包。二者都接收 15010/15017/15018，但使用独立存储与事件；老端登录批量请求中 36/37 均被注释，Unity 当前只主动请求 37，36 保持被动接收，未经实证不要擅自加入启动请求。182xx 槽位包与 pos36 物品包没有固定先后，UI 必须同时监听两条更新链，实例未到或 id 不匹配时先降级显示槽位包的 `GoodsTypeId`。候选变强红点严格比较 `BagGoods.Rating`（不用 `OverallRating`）：空槽或候选更高即红；红点节点是 `BabyEquipSubItem.redImg`，槽位 `BabyEquipIcon.effectGp` 只表示选中。
- 宝宝装备强化 18219 上行只有 `pos_id:u8`；回包是 `code:i,pos:u8,id:u64,type:i,stage:u16,stage_lv:u16,stage_exp:i,power:i`。服务端会自行挑选强化经验材料或按升阶配置直接扣固定 cost，客户端不发送材料列表；扣包另走 15017/15018。`forgeBtn` 已通过运行时 Button 开放，但只能走“实时预览足够 → 列出材料名×数量 → 二次确认 → 回调再次校验同槽位/实例/消费快照 → pending 防连点 → 发 18219”的链路，不能直接绑定 `RequestEquipUpgrade`。
- `imprintBtn` 是铭刻 18220，上行 `pos:u8,count:u16,N×type_id:i32`，回包 `code:i,pos:u8,id:u64,type:i,skill_id:i,power:i`；协议、模型与 `config_baby_equip_engrave` 只读预览已迁，但独立选择/概率/结果 UI 尚未迁，所以按钮必须继续无 Button。每个提交的 type 都按装备颜色对应配置的 `num` 全扣，重复 type 会重复计费，ratio 累加后封顶 10000；服务端先扣料再掷概率，因此 `code=1,skill_id=0` 是“已消费但铭刻失败”，不能当协议错误，也不能回滚本地背包。材料只来自普通背包 `pos=4`，已有 SkillId 的装备禁止再次铭刻。
- 18219 的只读消耗预览来自三处：`config_baby_value[8]` 定义材料固定顺序 38040031/32/33 与每件 10/50/100 经验，`config_baby_equip_stren` 用 `pos@stage@nextLv.point_con - 当前stage_exp` 算经验缺口，当前阶段满级时改读下一条 `config_baby_equip_stage.cost`。服务端按该材料顺序从普通背包 `pos=4` 取最少件数；升阶预览即使有一项不足也必须返回完整 cost，不能提前截断。
- 不得从 S2C 命令号反推 C2S。连服夺宝是已核实的非对称链：老端专用 `ListDuobaoView.ts` 发 33191，服务端 `pp_custom_act.erl` 在 `type=116` 时转入 rush treasure，成功后回 33803；Unity 必须保留 33191 请求 + 33803 独立接收。另一个通用 `CompetelistView` 直接发 33803，不代表专用夺宝页也应照抄。
- 活动入口路由必须查新客户端实际 `configcustomactivity/configfunctionicon` 键，不要只照旧端拼接规则。当前连服夺宝实际可见父入口是 `331@110`，子活动数据是 `116@0`；在通用父容器尚未接管前，专用路由只能有条件占用 `331@110`，同时保留精确键 `331@116@0`。
- 时装第二刀已核实的 wire：41305 上行是 `PosId:u8 + Count:u16 + N×{GoodsInstanceId:u64,Num:u16}`，这里必须发背包实例 id，严禁用 `type_id`；当前服务端实际只允许衣服位 `pos=1`。41313 下行是无 Code 的套装全量快照，落地前要清旧表；41314/41315 的 Code 都位于 `SuitId/ActiveNum或Lv` 之后的第三字段，不能套用“Code 总在包头”的惯例。41314 只开放 2 件/4 件两档，4 件成功时套装等级置 1。
- 时装第二刀权威配置是老端 CDN `config_fashion_pos`、`config_fashion_suit`、`config_fashion_suit_star`，分别驱动部位经验/属性、四件条件/激活属性、套装 1～10 阶；不得把条件、阶数或消耗硬编码进 View。`FashionModule.prefab` 的 Level/Suit/材料/页签/条件格都是顶层模板节点，业务 Flow 必须在 reparent 前保存模板引用，BindUpgrader 当前成功判据为 9 个业务组件。
- 41305 服务端会删除请求里给出的全部数量；客户端默认候选必须只凑当前等级的经验缺口，最后一个实例按 `ceil(剩余缺口/单件经验)` 裁量并受真实库存/u16 限制，补足后停止，绝不能默认把整堆材料全发。41315 不能只查 cost：必须逐项核对 `SuitStarRow.Conditions`；Slot 先映射 `SuitRow.Conditions`，时装取指定基础色星级，幻化 subtype 1/2 取 Star、其余取 Stage，条件不足时按钮、红点和发包都要拦截。

任何 AI 工具(Claude Code / Cursor / Codex / Copilot 等)写代码前必须读前三份;
动 UI/转换器读流水线文档,动登录/网络读登录链路文档,动进游戏/主界面/场景接管读进游戏链路文档。
冲突时以 `Docs/Shenxiao重构实施方案.md` 为权威;实施进度与变更日志见
[Docs/Shenxiao实施进度.md](Docs/Shenxiao实施进度.md)。
