# Shenxiao UI Patrol - MainUI 2026-06-24 03:29

## Scope
- Automation: `shenxiao-ui`
- Focus: MainUI usable entries from old Laya runtime, 720x1280 portrait.
- No code, prefab, converter, or Unity menu generation edits in this round.

## Covered Entries
- Old runtime baseline/current HUD:
  - evidence: `old_laya_current_before_task_team.png`
  - DOM evidence: `old_laya_dom_probe_before_task_team.json`
  - result: page uses visible `canvas#layaCanvas` at 720x1280; `window.Laya.stage` is not exposed, so no real stage tree dump was available from browser JS.
- Task/team area:
  - click `(28,815)` on the visible team tab area.
  - result: no visible switch to team content in this runtime state.
  - old-source evidence: `MainUITaskTeamView.ts` binds `_box_team_tab`, but it returns early when `team_model.IsOpenTeam()` is false and also depends on `CanShowTeam()`.
  - evidence: `old_laya_click_team_tab_28_815.png`, `old_laya_click_team_tab_again_28_815.png`
- Task content pitfall:
  - click `(85,678)` was intended as team-create after the team-tab attempt, but because the team tab did not switch, it clicked the task list instead.
  - result: old client opened `觉醒之路`; this is a misclick and is not counted as `team_create` coverage.
  - evidence: `old_laya_click_team_create_85_678.png`, `old_laya_after_team_return_664_1113.png`
- Activity icon:
  - first click `(436,280)` happened after the runtime had shifted into combat and is not counted as activity coverage.
  - retry click `(438,281)` on visible `开服活动` HUD icon opened the old real `开服活动` page.
  - result: old page has full title frame, large banner, reward list, item icons, and claim buttons; return `(664,1113)` closes it.
  - evidence: `old_laya_retry_click_openserver_438_281.png`, `old_laya_retry_openserver_return_664_1113.png`
- Skill/partner area:
  - click `(310,920)` was consumed by combat skill UI while auto battle was active.
  - result: no MainUI panel opened; not counted as partner/lock coverage.
  - evidence: `old_laya_click_strengthen_or_partner_310_920.png`

## Differences Found
- Activity routes:
  - Old `开服活动` is a real, full-screen activity page.
  - Unity `ActivityIcon` opens by `iconType`; any unregistered numeric route must at least show `MainUIRoutePlaceholder`, and migrated activities should open real flows.
- Task/team:
  - Old team tab is condition-gated by runtime model state, not just a static button.
  - Unity currently has `team_create`, `team_search`, and `templeawaken` click paths, but those are placeholder targets unless real Team/TempleAwaken flows are registered.
- Runtime-state risk:
  - Old client auto task/battle can move the character, change visible HUD, and make previously calibrated coordinates invalid.
  - Next patrol must first stop or stabilize auto battle/task before expanding pixel-level MainUI clicking.

## Common Root Causes
- MainUI parity cannot be judged from static `.scene` or generated prefab alone:
  - old task/team and activity visibility depend on runtime models and current scene state.
  - old activity pages load dynamic images/lists at runtime.
- Remaining Unity gaps are common-path issues:
  - static UI defects such as missing backgrounds/frames/skins belong in the LayaUI conversion/default-skin/common-window pipeline.
  - route gaps belong in `MainUIRouter` registration plus unified placeholder fallback.
  - dynamic pages/lists belong in View/Flow runtime logic, not prefab hand edits.

## Unity Route Audit
- Confirmed current code paths:
  - `MainUIRouter.Open` falls back to `MainUIRoutePlaceholder.Show(viewKey)` for unregistered routes.
  - `ActivityIcon` calls `MainUIRouter.Open(iconType)`.
  - `MainUITaskTeamView` binds team/task tabs, `team_create`, `team_search`, and `templeawaken`.
- Real/registered targets from existing module bootstrap coverage include:
  - `role`, `bag`, `chat`, `setting`, `shop`, `map`, `friend`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`, `autobrush_toggle`.
- Placeholder/fallback targets still expected:
  - `customerservice`, `team_create`, `team_search`, `templeawaken`, `autobrush`, numeric activity keys such as `158` and `338@...`.

## Commands And Tool Status
- `dotnet build yu_client_unity.slnx -v:minimal`
  - success, 0 warnings, 0 errors.
- `claude --version`
  - `2.1.185 (Claude Code)`.
  - No Claude code task was launched this round because the needed action was evidence/reporting; a new explorer spawn failed because the sub-agent thread limit was already reached.
  - Existing completed sub-agent outputs were used only as read-only audit evidence.
- Unity MCP:
  - initial `Unity_RunCommand` failed: `Transport closed`.
  - found one `relay_win.exe`: PID `39916`, parent `codex.exe`, command `C:\Users\tr\.unity/relay\relay_win.exe --mcp`.
  - stopped PID `39916` as stale/failed relay.
  - retry `Unity_RunCommand` still failed: `Transport closed`.
  - after cleanup no `relay_win.exe` remained.
- Git/worktree:
  - existing modified files are present in MainUI/Bag/AutoBrush/Common UI code from earlier work.
  - this round added only `output/heartbeat_1914/` evidence and this report.

## Executed Code/Generation Tasks
- None.
- No prefab edits.
- No converter changes.
- No Unity Editor menu regeneration because MCP remained blocked and no static pipeline patch was made.

## Evidence Files
- Folder: `output/heartbeat_1914/`
- Key screenshots:
  - `old_laya_current_before_task_team.png`
  - `old_laya_click_team_tab_28_815.png`
  - `old_laya_click_team_create_85_678.png`
  - `old_laya_retry_click_openserver_438_281.png`
  - `old_laya_click_strengthen_or_partner_310_920.png`
- Key JSON:
  - `old_laya_dom_probe_before_task_team.json`
  - `old_laya_task_team_actions.json`
  - `old_laya_hud_activity_retry.json`

## Next Priority
1. Stabilize old MainUI before clicking: cancel auto battle/task or reload to a clean MainUI state, then re-cover `team_create`, `team_search`, `activity_rank`, `158`, and customer service.
2. Restore Unity runtime capture path:
   - fix/restart Unity MCP, or add a durable `-executeMethod`/Editor-menu screenshot and node-dump harness if MCP keeps failing.
3. Use Claude Code for the next bounded code task, not broad exploration:
   - candidate task: ensure MainUI visible hit areas route through `MainUIRouter` and placeholder opens for every unregistered visible entry.
4. Continue avoiding prefab hand edits:
   - static differences go through converter/default skin/common window/Bind/Addressables, then regenerate.
