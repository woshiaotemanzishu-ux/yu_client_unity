# UI 路由项目总控汇总

- 扫描根目录：`output/ui_route_audit`
- 台账：发现 24，成功读取 24，错误 0
- 节点：1766（done=394, blocked=357, needs-runtime-verify=450, baseline-only=11, not-run=554）
- 边界：current-schema=11, historical-read-only=13
- 口径：schema 6 是当前证据合同；schema 2～5 仅为历史只读快照。根状态为 done 也不能跨越该边界升级为当前完成。
- 注意：本报告只做清单汇总，不执行正式台账校验，也不会写回任何 route-ledger/manifest。

| 边界 | 路线 | Schema | 根状态 | 节点状态 | 验证环境 | 台账 | Manifest |
|---|---|---:|---|---|---|---|---|
| current-schema | mainui.bag.window | 6 | blocked | blocked=61, needs-runtime-verify=46 | - | `output/ui_route_audit/2026-08-09_bag/route-ledger.json` | `output/ui_route_audit/2026-08-09_bag/route-manifest.json` |
| current-schema | mainui.chat.fullscreen | 6 | not-run | not-run=88 | - | `output/ui_route_audit/2026-08-09_chat/route-ledger.json` | `output/ui_route_audit/2026-08-09_chat/route-manifest.json` |
| current-schema | mainui.friend-email.shared | 6 | not-run | not-run=117 | - | `output/ui_route_audit/2026-08-09_friend_email/route-ledger.json` | `output/ui_route_audit/2026-08-09_friend_email/component-dependency-manifest.json`<br>`output/ui_route_audit/2026-08-09_friend_email/route-manifest.json` |
| current-schema | mainui.guild.route | 6 | not-run | not-run=134 | - | `output/ui_route_audit/2026-08-09_guild/route-ledger.json` | `output/ui_route_audit/2026-08-09_guild/component-dependency-manifest.json`<br>`output/ui_route_audit/2026-08-09_guild/route-manifest.json` |
| current-schema | mainui.map | 6 | blocked | blocked=35, needs-runtime-verify=14 | - | `output/ui_route_audit/2026-08-09_map/route-ledger.json` | `output/ui_route_audit/2026-08-09_map/route-manifest.json` |
| current-schema | mainui.role.outward | 6 | blocked | blocked=143, needs-runtime-verify=91 | - | `output/ui_route_audit/2026-08-09_role_outward/route-ledger.json` | `output/ui_route_audit/2026-08-09_role_outward/route-manifest.json` |
| current-schema | mainui.role.person.achievement.v1 | 6 | blocked | blocked=7, needs-runtime-verify=48 | - | `output/ui_route_audit/2026-08-08_role_achievement/route-ledger-v1.json` | `output/ui_route_audit/2026-08-08_role_achievement/route-manifest-v1.json` |
| current-schema | mainui.role.person.achievement.v2 | 6 | blocked | blocked=10, needs-runtime-verify=53 | - | `output/ui_route_audit/2026-08-08_role_achievement/route-ledger-v2.json` | `output/ui_route_audit/2026-08-08_role_achievement/route-manifest-v2.json` |
| current-schema | mainui.role.person.medal.v1 | 6 | blocked | blocked=12, needs-runtime-verify=8 | - | `output/ui_route_audit/2026-08-09_role_medal/route-ledger.json` | `output/ui_route_audit/2026-08-09_role_medal/route-manifest.json` |
| current-schema | mainui.role.unreal | 6 | blocked | blocked=5, not-run=52 | - | `output/ui_route_audit/2026-08-09_role_unreal/route-ledger.json` | `output/ui_route_audit/2026-08-09_role_unreal/route-manifest.json` |
| current-schema | mainui.task.route | 6 | not-run | not-run=123 | - | `output/ui_route_audit/2026-08-09_task/route-ledger.json` | `output/ui_route_audit/2026-08-09_task/component-dependency-manifest.json`<br>`output/ui_route_audit/2026-08-09_task/route-manifest.json` |
| historical-read-only | global.bitmap-fonts | 4 | done | done=7 | - | `output/ui_route_audit/2026-08-05_bitmap-fonts/route-ledger.json` | `output/ui_route_audit/2026-08-05_bitmap-fonts/manifest.json` |
| historical-read-only | global.bitmap-fonts.remediation-20260806 | 4 | needs-runtime-verify | done=1, needs-runtime-verify=6 | - | `output/ui_route_audit/2026-08-06_bitmap-font-remediation/route-ledger.json` | `output/ui_route_audit/2026-08-06_bitmap-font-remediation/manifest.json` |
| historical-read-only | mainui.onhook.boost-sweep | 4 | needs-runtime-verify | done=2, needs-runtime-verify=3 | - | `output/ui_route_audit/2026-08-04_mainui_onhook_boost_sweep/route-ledger.json` | `output/ui_route_audit/2026-08-04_mainui_onhook_boost_sweep/manifest.json` |
| historical-read-only | mainui.reported-defects | 4 | done | done=6 | - | `output/ui_route_audit/2026-08-04_mainui_notifications/route-ledger.json` | `output/ui_route_audit/2026-08-04_mainui_notifications/manifest.json` |
| historical-read-only | mainui.role | 4 | baseline-only | done=72, blocked=4, baseline-only=11, not-run=30 | - | `output/ui_route_audit/2026-08-04_role/role-route-ledger.json` | `output/ui_route_audit/2026-08-04_role/manifest.json` |
| historical-read-only | mainui.role.person.attribute-potion | 4 | needs-runtime-verify | done=26, needs-runtime-verify=58 | - | `output/ui_route_audit/2026-08-06_role_attribute_potion/route-ledger.json` | `output/ui_route_audit/2026-08-06_role_attribute_potion/manifest.json` |
| historical-read-only | mainui.role.person.instruction | 4 | not-run | not-run=10 | - | `output/ui_route_audit/2026-08-06_role_instruction/route-ledger.json` | `output/ui_route_audit/2026-08-06_role_instruction/manifest.json` |
| historical-read-only | mainui.role.person.resonance | 4 | blocked | done=264, blocked=80, needs-runtime-verify=114 | - | `output/ui_route_audit/2026-08-07_resonance/route-ledger.json` | `output/ui_route_audit/2026-08-07_resonance/route-manifest.json` |
| historical-read-only | mainui.settings.visual-reopen | 2 | done | done=8 | - | `output/ui_route_audit/2026-08-04_settings-fashion-visual-final/route-ledger.json` | `output/ui_route_audit/2026-08-04_settings-fashion-visual-final/manifest.json` |
| historical-read-only | scene.designation.main-follow | 4 | done | done=2 | - | `output/ui_route_audit/2026-08-06_designation_ghost/route-ledger.json` | `output/ui_route_audit/2026-08-06_designation_ghost/route-manifest.json` |
| historical-read-only | shared.base-window-skin | 5 | needs-runtime-verify | needs-runtime-verify=4 | - | `output/ui_route_audit/2026-08-08_base_window_skin_shared/route-ledger.json` | - |
| historical-read-only | shared.item.presentation | 5 | needs-runtime-verify | needs-runtime-verify=5 | - | `output/ui_route_audit/2026-08-07_shared_item_presentation/route-ledger.json` | `output/ui_route_audit/2026-08-07_shared_item_presentation/route-manifest.json` |
| historical-read-only | web.foundation.loading-entry-adaptation | 4 | done | done=6 | - | `output/ui_route_audit/2026-08-04_web-foundation/route-ledger.json` | `output/ui_route_audit/2026-08-04_web-foundation/manifest.json` |
