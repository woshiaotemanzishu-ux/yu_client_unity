# AGENTS.md

本仓库的 AI 编码约束统一维护在:

- [.github/copilot-instructions.md](.github/copilot-instructions.md) — 精简红线(GitHub Copilot 自动加载)
- [Docs/Shenxiao编码规范.md](Docs/Shenxiao编码规范.md) — 完整编码规范
- [Docs/Shenxiao重构实施方案.md](Docs/Shenxiao重构实施方案.md) — 整体方案与架构
- [Docs/LayaUI转换流水线.md](Docs/LayaUI转换流水线.md) — UI 主路线:粒度/烘焙/Bind/验收规矩
- [Docs/Shenxiao登录链路.md](Docs/Shenxiao登录链路.md) — yu_client→yu_gm→yu_server 链路与协议出处
- [Docs/Shenxiao进游戏链路.md](Docs/Shenxiao进游戏链路.md) — 选角/创角后 MainUI、地图、主角、NPC/怪物、弹层的阶段接管规矩

## Unity MCP 连接记忆

- 连接 Unity MCP 服务前，先检查是否存在残留的 Unity MCP bridge/relay 进程，重点看 `relay_win.exe`；残留桥接会占满槽位导致新连接失败。确认是僵尸桥后，直接结束该残留进程，再重新连接 Unity MCP。

任何 AI 工具(Claude Code / Cursor / Codex / Copilot 等)写代码前必须读前三份;
动 UI/转换器读流水线文档,动登录/网络读登录链路文档,动进游戏/主界面/场景接管读进游戏链路文档。
冲突时以 `Docs/Shenxiao重构实施方案.md` 为权威;实施进度与变更日志见
[Docs/Shenxiao实施进度.md](Docs/Shenxiao实施进度.md)。
