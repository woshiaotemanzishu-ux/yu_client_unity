# Shenxiao UI Heartbeat 17:44 MainUI Patrol

## Scope

- Baseline: old Laya runtime at 720x1280 portrait, `http://127.0.0.1:8090/index.html`.
- Current old-client account state: cached role `21055` continues to auto-progress and trigger tutorial/reward overlays.
- Focus: MainUI usable-module closure, with route coverage checked against Unity code and existing Unity runtime captures.

## Old Runtime Evidence

- `old_laya_current_start_720x1280.png`: current old client is on MainUI, but still has tutorial prompt and bottom entry guidance.
- `old_laya_after_guide_auto_wait_720x1280.png`: waiting for the guide auto-continue pushes the client into `斩妖巡行` reward flow.
- `old_laya_after_reward_close_720x1280.png`: clicking the reward overlay reveals another reward/guide layer; MainUI is still blocked.

Conclusion: this old account is not currently suitable for reliable per-entry MainUI click patrol. The old runtime state is valid evidence of the real game, but entry-click results from this state would be polluted by tutorial/reward overlays.

## Unity Route Coverage

Checked with Claude Code CLI and existing route coverage JSON (`output/heartbeat_1659/mainui_route_coverage.json`, same as `heartbeat_1644`):

| Entry | Route key | Status | Evidence |
| --- | --- | --- | --- |
| Role | `role` | registered real module | `RoleBootstrap.cs` |
| Bag | `bag` | registered real module | `BagBootstrap.cs` |
| Setting | `setting` | registered real module | `SettingBootstrap.cs`, opened from `MainUITopView`/`MainUIChatView` |
| Chat | `chat` | registered real module | `ChatBootstrap.cs`, opened from `MainUIChatView` |
| Shop | `shop` | registered real module | `ShopBootstrap.cs` |
| Map | `map` | registered real module | `MapBootstrap.cs`, opened from `MainUITopView` |
| Activity rank | `activity_rank` | placeholder | opened from `MainUIActivityView`, no register refs |
| Task/team create/search | `team_create`, `team_search` | placeholder | opened from `MainUITaskTeamView`, no register refs |
| Auto-brush toggle | `autobrush_toggle` | registered real action | `AutoBrushBootstrap.cs` |
| Auto-brush panel | `autobrush` | placeholder | opened from `MainUIAutoBrushView`, no register refs |
| Customer service | `customerservice` | placeholder | opened from `MainUITopView`, no register refs |
| Partner lock | `partnerawake` | placeholder | opened from `MainUISkillView`, no register refs |

Important distinction: placeholder routes are deliberate degradation through `MainUIRoutePlaceholder`; they are not dead clicks. They still need visual/runtime verification once MCP or Play-mode capture is available.

## Unity Visual Evidence Checked

- `output/runtime_unity/play_role_after_common_tab_fix.png`: role page has window/background/content; not a transparent empty panel.
- `output/runtime_unity/play_bag_after_common_tab_fix.png`: bag page has window/background/content; not a transparent empty panel.
- `output/runtime_unity/current_screen_capture_after_with_mainui.png`: bag with MainUI behind still needs stricter visual comparison, but it is not the earlier fully transparent failure.
- `output/runtime_unity/20260624_004613/status.txt`: latest capture session is `isPlaying=False`, so it only proves editor `UIRoot`, not runtime MainUI.

## Differences / Risks Found

- Old runtime account state is the current blocker. It repeatedly enters tutorial/reward/feature-unlock flows and steals click focus.
- Unity route coverage is better than the old click patrol currently proves: six priority routes are registered real modules, but old runtime click evidence cannot safely cover them until a clean old role or deterministic overlay-clear step is used.
- Existing Unity runtime images show role/bag have backgrounds, but they are prior captures; current MCP is blocked, so this round did not generate fresh Unity Play screenshots.

## Common Root Cause Direction

- Patrol methodology must be fixed before judging module quality: use a clean old role or an automated old-client overlay-clear routine.
- Static visual mismatches still belong to LayaUI conversion/default-skin/resource-map/Bind/Addressables regeneration.
- Runtime route behavior belongs in `MainUIRouter` registrations and module `Bootstrap/Flow`; placeholder routes are valid only for unported modules.

## Claude / MCP / Commands

- Claude Code CLI: available. Used for read-only MainUI route coverage; no file edits.
- Unity MCP: blocked. `Unity_RunCommand` returned `Transport closed`.
- Relay check: `Get-Process relay_win -ErrorAction SilentlyContinue` found no stale relay process.
- `git diff --check`: passed with no output.
- `dotnet build .\yu_client_unity.slnx -v:minimal --nologo`: passed, `0` warnings and `0` errors.

## Next Priority

1. Get a clean old-client baseline: either an old role past tutorial/reward stacks or a scripted overlay-clear checklist.
2. Re-run old runtime entry clicks in this exact order: role, bag, setting, chat, shop, map.
3. For Unity, refresh Play-mode screenshots when MCP recovers; existing route coverage says those six should open real modules.
4. After real-module screenshots, compare role/bag/settings/chat/shop/map pixel/layout gaps and push static defects through the converter pipeline, not prefab hand edits.
