# TopVip 验证记录

- `route_ledger.py init`：通过，schema 6，82 节点。
- `route_ledger.py apply`：通过，82 节点全部派生为 `blocked`。
- `route_ledger.py validate`：通过，输出 `route=mainui.topvip schema=6 nodes=82 status={'blocked': 82}`。
- 目标文件岛 Git 状态：`Assets/Scripts/Module/Core/TopVip` 与 `Assets/Prefabs/UI/TopVip` 无新增修改；本轮只新增本审计目录。
- Prefab/Bind 静态身份：`TopVipShopItem.prefab` 根脚本 GUID `27a0681d0fba7754bafe27b0e9e9f770` 与 `TopVipShopItemBind.cs.meta` 一致。
- 未启动 Unity/浏览器；未执行 Web/Unity runtime、Player/catalog 或像素验证。
- `convert-module` 本轮不可执行：首次落地必须先取得真实老端运行快照，并经 Unity MCP 串行烤制/回填、注册 Addressables、完成 Unity 编译与运行 diff；本轮明确禁止启动 Unity/浏览器，且 Addressables 不在本文件岛，因此只能保留转换 blocker，不能以静态转换器产物替代。
- 未改 C#，因此没有需要编译的变更；按约束未触碰共享 Temp/Core，也未虚设 output-only csproj 伪造编译门禁。

生成时 SHA-256：

- `route-manifest.json`: `ee67eae577aa68628f373bd8aa283adae7e5d6c454f3f3536bee915c35dc8422`
- `route-ledger.json`: `e4339d75b5d56721650ef16d1cd4bb8203108dc471363123ce23067366ac794a`
- `static-results.json`: `6633d3562213d90966d7760976944b8f401f4f9f434f7bc16e98bef51a03d2fa`
- `source-inventory.md`: `c996851beac7e1baa5d8cfb18812762d63adda96205b8e10d264c15e416af3a1`
