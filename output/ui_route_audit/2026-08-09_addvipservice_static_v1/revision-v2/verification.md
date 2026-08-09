# AddVipService revision-v2 验证记录

- `route_ledger.py init`：通过，schema 6，28 节点。
- `route_ledger.py apply`：通过，28 节点全部派生为 `blocked`。
- `route_ledger.py validate`：通过，输出 `route=mainui.addvipservice.revision-v2 schema=6 nodes=28 status={'blocked': 28}`。
- 拓扑：28 节点、24 叶；页面 `control_inventory` 与直接子节点精确一一映射由 validator 校验。
- 交易审计：0 个 transaction/destructive-write 叶，页面没有充值/领取/购买/领奖控件。
- manifest/ledger 哈希绑定由 schema 6 validator 复核；`route-manifest.json` SHA-256 为 `6c0c21c33e8cf54c486d5c746983c5e35a93f9433f72b34e19f3f8112b64cfe3`。
- `route-ledger.json` SHA-256 为 `ed29cefb871cd5682945e45d08115d808a399672895855f4f4500157e7f7305e`。
- `static-results.json` SHA-256 为 `0ccbfd5a476f158a7df694c647c286284750240aef2dba5bdfd8821342f6f558`。
- 未启动 Unity/浏览器、未执行账号写事务、未改 C#/Prefab；无 C# 变更所以未虚设 output-only csproj。

复验命令：

```powershell
python .agents\skills\audit-game-ui-route\scripts\route_ledger.py validate output\ui_route_audit\2026-08-09_addvipservice_static_v1\revision-v2\route-ledger.json
```
