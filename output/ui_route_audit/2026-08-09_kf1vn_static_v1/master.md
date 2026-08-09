# UI 路由项目总控汇总

- 扫描根目录：`output/ui_route_audit/2026-08-09_kf1vn_static_v1`
- 台账：发现 1，成功读取 1，错误 0
- 节点：98（blocked=19, needs-runtime-verify=77, defect=2）
- 边界：current-schema=1
- 口径：schema 6 是当前证据合同；schema 2～5 仅为历史只读快照。根状态为 done 也不能跨越该边界升级为当前完成。
- 注意：本报告只做清单汇总，不执行正式台账校验，也不会写回任何 route-ledger/manifest。

| 边界 | 路线 | Schema | 根状态 | 节点状态 | 验证环境 | 台账 | Manifest |
|---|---|---:|---|---|---|---|---|
| current-schema | mainui.kf1vn | 6 | blocked | blocked=19, needs-runtime-verify=77, defect=2 | - | `output/ui_route_audit/2026-08-09_kf1vn_static_v1/route-ledger.json` | `output/ui_route_audit/2026-08-09_kf1vn_static_v1/route-manifest.json` |
