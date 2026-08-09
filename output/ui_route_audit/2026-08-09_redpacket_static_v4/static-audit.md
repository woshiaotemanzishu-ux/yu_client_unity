# RedPacket 静态路线审计 v4（schema 6）

## 版本与边界

- v1/v2 是早期不完整拓扑，v3 保留 62 节点最终 QA 状态但缺三个生命周期叶；三者均 superseded。
- v4 保持逻辑路线 `mainui.red-packet`，以 65 节点全新建账，是唯一权威静态结果。
- 既有 Prefab 使用 `audit-game-ui-route → fix-view`；未启动 Unity、浏览器或前台程序，未登录或执行领取/发送。

## 生命周期修复

- `RedPacketMainView` 保持 OnShow 订阅、OnHide/OnDispose 解绑，并新增 `PrepareForRelease` 供模块释放前幂等解绑。
- `RedPacketFlow` 新增 generation 与 `try/finally`。Reset 递增 generation、先解绑再 Release；await 晚到 root 判旧后立即 Release，不能回填或 Show。
- 新增 `disconnect-reset`、`late-arrival`、`subscription-unbind` 三个独立叶。代码静态接线已完成，但未真实运行，均为 `needs-runtime-verify`。

## 继承状态

- v3 的 44 个 blocked 叶原样继承，全部显式 `runtime_gap=null`；其中含 33902/33904/33906 写事务和 33903/33905 absent/KILL。
- v3 的 9 个已静态接线叶继续 `needs-runtime-verify`，加三个生命周期叶后共 12 个。
- 动态列表、详情/发包弹窗等缺实现分支继续 blocked，不能以 Bind/Prefab/Model 存在冒充可达。
- 0 `done`、0 `not-run`。

## 验证边界

- 独立 output-only C# 项目同时编译 `RedPacketMainView.cs` 与 `RedPacketFlow.cs`。
- 静态验证固定 65 节点、总计/叶计数、12 个 runtime ID 精确集合、33901 顺序、33903/33905 absent、生命周期源码防线和所有 blocked gap 为空。

