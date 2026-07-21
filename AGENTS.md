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

- 用户日常打开和精修的 Unity 项目是 `E:\GitProject\yu_client_unity`，当前工作分支是 `feat/ui-adaptive-anchors`；Codex 自动迁移固定使用 Git worktree `E:\GitProject\yu_client_unity_codex`，分支是 `codex/automation-workspace`。独立目录由提交 `40e68b1dcb836ff59d2e8dc00d5392ad622aaadd` 建立，不是普通文件夹副本。
- 两个工作树必须使用不同分支。用户在原目录提交后，Codex 开工前先检查两边状态，再把 `feat/ui-adaptive-anchors` 的新提交合入 Codex 分支；Codex 阶段成果在独立分支提交，验收后再合回用户分支。不要假设另一目录里的未提交修改会自动同步；同改 scene、prefab、`.meta` 或大资源前要先协调，避免二进制/序列化冲突。
- `Library/`、`Temp/`、`obj/` 等 Unity 缓存不在工作树间共享。Codex 工作树初建时没有 `Library/`；第一次打开必然进行完整导入，属于高负载操作，只能安排在用户明确空闲的时间窗口，不能为了普通代码检查擅自启动。
- 所有 Codex 任务全局最多只能有一个任务调用 Unity。用户的原目录正在运行 Unity 时，Codex 默认只做代码迁移、协议/旧端对照和静态检查，不再启动第二套 Unity；Unity 验证集中到阶段收尾，不要每个小任务启动一次。
- 确需自动运行 Unity 时，先确认其他 Codex 任务没有 Unity 进程，使用低优先级和较小的 job/background worker 数，验证完成立即退出。不得让两个 Codex 任务并行启动 Editor、AssetImportWorker 或 ShaderCompiler。
- 2026-07-21 的只读诊断显示：单个用户 Editor 会派生 2 个 AssetImportWorker 和 3 个 ShaderCompiler，Unity 进程合计约 7.9 GB、333 个线程；全系统约 357 个进程、8241 个线程，出现过 120～180 的处理器队列和约 8.8 万次/秒上下文切换。机器是 i7-12700KF、48 GB 内存、NVMe，检查时内存、页面文件和磁盘均未耗尽；卡顿主因应先排查 Unity 并发导入/编译、Defender 扫描和后台 Chromium/WebView 进程，而不是直接归因于硬件性能不足。
- 本机网络默认路由和 DNS 经过 `TAG Wintun`、`mihomo-tag`/`tagtunnel`。物理 Realtek 网卡到路由器检查时零丢包且没有断线记录；重负载时若仅本机“断网”，优先同时记录 TUN 进程响应、网关连通和公网连通，判断代理进程是否被调度饿死。不要未经用户同意修改 Defender 排除项、网卡节能或代理优先级。
- 本仓库约有 13.5 万个受控文件并使用 Git LFS。首次 `git worktree add` 可能超过命令包装器的超时，但底层 `git`/`git-lfs` 仍会继续检出；遇到超时先检查相关进程、文件数和目录体积是否持续增长，正常增长就等待完成，不要立即重建、删除目录或重复 checkout。最终必须用 `git status`、HEAD、分支、受控文件数和 `Assets/Packages/ProjectSettings` 完整性验收。
- 诊断还发现近 18 天存在 7 次无蓝屏代码的意外关机记录，以及三条不同型号的 16 GB 内存混插和 2023 年 BIOS。若意外关机不是用户在卡死后手动重启造成，需要单独排查内存、BIOS、电源和硬件稳定性；当前 WHEA 只有信息型厂商 CPER 记录，不能据此断言某个硬件已经损坏。

## UI 生成/修复记忆

- UI 静态结构、背景、窗框、皮肤、尺寸、默认图片、模板、Bind 回填、Addressables 分组等生成问题，必须优先修通用 LayaUI 转换链路、默认表或回填工具，然后通过 Unity Editor 菜单重新转换/回填/分组/验收；不要直接手工改 prefab 当作最终方案。
- prefab 变更应来自通用转换器或 Unity Editor 菜单生成结果。只有用户明确要求手调，或确认是一次性验收调整时，才允许手工改 prefab，并且必须记录原因和风险。
- 业务 View/Flow 只负责旧端运行时行为: 真实数据刷新、按钮事件、动态列表/模板实例化、运行时换图、角色模型、显隐状态和协议链路。不要用业务代码硬补本该由转换器生成的静态 UI。
- 发现页面背景透明、窗框缺失、按钮皮肤/列表模板/九宫格/图片尺寸不对时，先归因为转换器、资源映射、默认皮肤、Bind 或运行时加载链路，优先找共性修复；避免逐页精修。

任何 AI 工具(Claude Code / Cursor / Codex / Copilot 等)写代码前必须读前三份;
动 UI/转换器读流水线文档,动登录/网络读登录链路文档,动进游戏/主界面/场景接管读进游戏链路文档。
冲突时以 `Docs/Shenxiao重构实施方案.md` 为权威;实施进度与变更日志见
[Docs/Shenxiao实施进度.md](Docs/Shenxiao实施进度.md)。
