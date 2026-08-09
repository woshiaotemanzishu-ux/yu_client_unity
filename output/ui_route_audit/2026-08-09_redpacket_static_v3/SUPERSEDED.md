# Superseded

此目录保留 62 节点 schema 6 台账及最终 QA correction 批次。后续发现 `RedPacketFlow.Reset` 直接 Release 时存在订阅解绑和 await 晚到回填风险，且原拓扑缺少三个独立生命周期叶。

修正版在 `2026-08-09_redpacket_static_v4` 以相同逻辑路线 `mainui.red-packet` 全新初始化；v3 不再作为权威结果。

