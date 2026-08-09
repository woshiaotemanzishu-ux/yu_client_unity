# VIP revision-v4 验证记录

日期：2026-08-09。所有验证均为后台 CLI/静态验证；未启动 Unity、浏览器或任何前台程序，未操作账号，未点击充值/领取/购买。

## Schema 6

```text
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py validate output/ui_route_audit/2026-08-09_vip_main_revision_v4/route-ledger.json
route=mainui.vip.v4 schema=6 nodes=1347 status={'blocked': 83, 'needs-runtime-verify': 1264}
```

- v4 为 334 个 page/parent 节点、1013 个叶，共 1347 节点。
- 叶状态：`needs-runtime-verify=966`、`blocked=47`、`not-run=0`。
- 全部 1013 个叶的 `applicable_gates` 均为数组。
- v3/v4 规范化 route 前缀后 `nodes[]` 完全相等，拓扑差异为 0。

## 独立 C# 验证

```text
dotnet build output/ui_route_audit/2026-08-09_vip_main/VipRoute.Isolated.csproj --no-restore
0 warnings, 0 errors

dotnet run --project output/ui_route_audit/2026-08-09_vip_main_revision_v2/VipModel.OrderedSnapshot.csproj
VipModel ordered snapshot case: PASS
```

行为小测覆盖：15800 深拷贝、有序和重复保留；15801 更新全部匹配项、缺失项不插入；`HaveFirstRecharge` 遍历有序快照；`Reset` 同时清空有序列表和兼容字典。

## 配置加载链与 SHA

- 6 个带实际文件路径的证据均存在且 SHA-256 精确匹配。
- `config.zip` 内 `config.json` SHA-256 为 `aeea254274a99f9b092328d476bcae72b78fdbfe119700144e66ab4d0acca43a`，含 387 个顶层配置对象。
- PRELOAD canonical 对象：`config_recharge_product` 95 项、`config_recharge_return` 16 项、`ClientRechargeShow` 11 项，三者 canonical SHA 均与 v4 baseline 相同。
- `config_recharge_product` 中 `product_type in (1,2)` 仅 7 条：`(2..8, type1)`；无 type2 静态候选。

## Prefab YAML

```text
YAML header: true
definitions: 1002
unique definitions: 1002
unresolved local fileID: 0
stripped RectTransform blocks: 2
git diff --check: pass
```

Prefab 仅在现有 `VipModule.prefab` 的 `tab_content/Viewport/Content` 增量增加居中横向布局与尺寸调整，没有重转或重建 Prefab。

## 禁止写协议扫描

在 `Assets/Scripts/Module/Core/Vip/**/*.cs` 扫描 `45001/45002/45003/45007/45008/15902` 及领取/购买/隐藏写常量，结果为 0 命中。现有出站只读查询为 45000、45004、15800、15803、15901；View 内领取、购买、更多支付和 VIP 隐藏点击面保持禁用或未绑定。

## 未完成门禁

没有 Unity Editor、真实 Unity Web 或老 H5 运行证据；视觉像素、动态模型/特效、声音、条件显隐、列表拖动/裁剪、详情身份、cold/warm、即时刷新和关闭重开全部保持 `needs-runtime-verify`。所有账号写叶保持 `blocked`，本记录不把静态成功写成真实 Web 通过。
