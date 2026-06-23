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

## Unity MCP 连接记忆

- 连接 Unity MCP 服务前，先检查是否存在残留的 Unity MCP bridge/relay 进程，重点看 `relay_win.exe`；残留桥接会占满槽位导致新连接失败。确认是僵尸桥后，直接结束该残留进程，再重新连接 Unity MCP。

## UI 生成/修复记忆

- UI 静态结构、背景、窗框、皮肤、尺寸、默认图片、模板、Bind 回填、Addressables 分组等生成问题，必须优先修通用 LayaUI 转换链路、默认表或回填工具，然后通过 Unity Editor 菜单重新转换/回填/分组/验收；不要直接手工改 prefab 当作最终方案。
- prefab 变更应来自通用转换器或 Unity Editor 菜单生成结果。只有用户明确要求手调，或确认是一次性验收调整时，才允许手工改 prefab，并且必须记录原因和风险。
- 业务 View/Flow 只负责旧端运行时行为: 真实数据刷新、按钮事件、动态列表/模板实例化、运行时换图、角色模型、显隐状态和协议链路。不要用业务代码硬补本该由转换器生成的静态 UI。
- 发现页面背景透明、窗框缺失、按钮皮肤/列表模板/九宫格/图片尺寸不对时，先归因为转换器、资源映射、默认皮肤、Bind 或运行时加载链路，优先找共性修复；避免逐页精修。

任何 AI 工具(Claude Code / Cursor / Codex / Copilot 等)写代码前必须读前三份;
动 UI/转换器读流水线文档,动登录/网络读登录链路文档,动进游戏/主界面/场景接管读进游戏链路文档。
冲突时以 `Docs/Shenxiao重构实施方案.md` 为权威;实施进度与变更日志见
[Docs/Shenxiao实施进度.md](Docs/Shenxiao实施进度.md)。
