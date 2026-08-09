# TopVip revision-v2 验证记录

- `route_ledger.py init`：通过，schema 6，133 节点。
- `route_ledger.py apply`：通过，133 节点全部派生为 `blocked`。
- `route_ledger.py validate`：通过，输出 `route=mainui.topvip.revision-v2 schema=6 nodes=133 status={'blocked': 133}`。
- 拓扑：133 节点、110 叶；页面 `control_inventory` 与直接子节点精确一一映射由 validator 校验。
- 交易审计：11 个 transaction/destructive-write 叶全部 `blocked`，且原因均显式含“本轮未点击”；检查结果 `explicit_not_clicked_bad=[]`。
- manifest/ledger 哈希绑定由 schema 6 validator 复核；`route-manifest.json` SHA-256 为 `57fd6c3fabbd836aae988195e5c4ddffacd21e1a4f080f3d4e2e67a556d31bda`。
- `route-ledger.json` SHA-256 为 `810ae74176d6f4eb44460f6410a079b6c1f279abfdb078874ee92d74e311800d`。
- `static-results.json` SHA-256 为 `feb15eda6e8ddf87c0ae486f7c4a526288c4871249b5e541bdbfe6af254c7db4`。
- 未启动 Unity/浏览器、未执行账号写事务、未改 C#/Prefab；无 C# 变更所以未虚设 output-only csproj。

复验命令：

```powershell
python .agents\skills\audit-game-ui-route\scripts\route_ledger.py validate output\ui_route_audit\2026-08-09_topvip_static_v1\revision-v2\route-ledger.json
```
