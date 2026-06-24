# Shenxiao UI Heartbeat 17:14

## Scope

- Baseline: old Laya runtime at `http://127.0.0.1:8090/index.html`.
- Viewport: 720x1280 portrait mobile.
- Account state: continued from the generated old-client account captured in the previous round.
- Focus: old runtime MainUI entry click evidence. No Unity prefab or converter changes were made in this round.

## Old Runtime Screenshots

- Start state was not MainUI; it was the post-create-role story scene:
  - `old_laya_current_start_720x1280.png`
  - `old_laya_current_start_probe.json`
- Story skip returned to MainUI:
  - `old_laya_after_story_skip_click_720x1280.png`
  - `old_laya_after_story_skip_click_probe.json`
- Customer service click sample:
  - `old_laya_click_customer_service_720x1280.png`
  - Result: no page opened in this account/state; keep as an unresolved coordinate/state check.
- Bottom settings click sample:
  - `old_laya_click_bottom_settings_720x1280.png`
  - Result: no page opened in this account/state; likely bottom bar coordinate/state needs source-backed confirmation.
- Task tab click:
  - `old_laya_click_task_tab_720x1280.png`
  - Result: triggered a task-completion reward modal, proving this entry has runtime side effects and is not a static tab only.
- Team tab click:
  - `old_laya_click_team_tab_720x1280.png`
  - Result: switched/kept the left-side task/team rail state; no full team page opened.
- Auto hangup click:
  - `old_laya_click_auto_hangup_720x1280.png`
  - Result: stayed on MainUI and behaved as a runtime toggle. The visible button state changed to cancel/open hangup state depending on current auto state.
- Activity icon click:
  - `old_laya_click_activity_icon_720x1280.png`
  - Result: opened real activity page `剑魄同修`.
  - `old_laya_after_activity_escape_720x1280.png`: Escape did not close; it advanced/changed internal guide state.
  - `old_laya_after_activity_top_left_click_720x1280.png`: top-left click returned to MainUI.
- Map click:
  - `old_laya_click_map_720x1280.png`
  - Result: opened real map page.
  - `old_laya_after_map_top_left_click_720x1280.png`: top-left did not close map.
  - `old_laya_after_map_escape_720x1280.png`: Escape did not close map.

All probe files confirm a visible `layaCanvas` at 720x1280. The old runtime still does not expose stable stage globals in the browser context, so screenshot + canvas metrics are the current runtime evidence.

## Differences / Risks

- New accounts are still not a clean MainUI baseline. They pass through story, newbie task, auto-combat, reward, unlock, and guide states.
- Customer service and bottom settings did not open from the sampled coordinates. Do not mark them as old-runtime verified until source-backed coordinates or a cleaner account state confirms them.
- Activity and map open as full pages, but close behavior is page-specific:
  - Activity returned via top-left click.
  - Map did not close with top-left or Escape; this needs source-backed close behavior, likely through the outer `MapEnterView` / `BaseWindowComponent` layer or a map-specific event.
- Task/team rail is not a simple "open panel" behavior in this early account state; task can trigger reward flow, team can stay as a rail mode switch.

## Common Root Cause

- MainUI entry validation needs to distinguish four runtime behaviors:
  1. opens a real full page,
  2. opens a modal/reward flow,
  3. toggles state on MainUI,
  4. does not trigger because of account state, guide, coordinate drift, or lock condition.
- Unity `MainUIRouter` should preserve that distinction. A unified placeholder is acceptable only for missing pages; toggles and rail state changes should stay in their business View/Flow code.
- Static visual issues remain converter work: window skins, backgrounds, default images, templates, Bind fields, and Addressables grouping should be fixed upstream and regenerated.

## Commands / Tool Status

- Browser old-client evidence captured with 720x1280 viewport.
- `claude -p "只输出 OK" --output-format text` returned `OK`; Claude Code CLI is logged in and minimally usable.
- Prior complex Claude read-only review still remains a timeout risk; use short, bounded prompts.
- Unity MCP `Unity_RunCommand` failed: `Transport closed`.
- `relay_win.exe` cleanup check found no remaining process after this round.
- `git diff --check` passed.
- `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed: 0 warnings, 0 errors.

## Next Priority

1. Source-confirm close behavior and exact click hitboxes for `MapEnterView`, customer service, bottom settings, and bottom role/bag/chat/shop entries.
2. Continue old-runtime click capture for role, bag, chat, shop, VIP, recharge, first recharge, and open-server activity after returning to a controllable MainUI state.
3. Unity side: once MCP works, replay the same entry list through runtime screenshots/node dumps, not just static route coverage.
4. Implementation priority remains AutoBrush, Team, OnHook as real usable modules; low-value entries can keep unified placeholder panels.
