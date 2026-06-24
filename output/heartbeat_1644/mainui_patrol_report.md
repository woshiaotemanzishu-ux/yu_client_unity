# Heartbeat 16:44 - MainUI Runtime Patrol

## Covered Entrances

- Old Laya runtime was opened at `http://127.0.0.1:8090/index.html` with a 720x1280 portrait viewport.
- Captured runtime states:
  - login form
  - server entry
  - role selection
  - MainUI entry with offline reward popup
  - MainUI popup/guide chain: new function, rebate pack, forge guide
- Unity static route coverage was checked for MainUI entries:
  - real registered entries include `role`, `bag`, `setting`, `chat`, `shop`, `map`, `friend`, `email`, `equip`, `guild`, `pet`, `vip`, `recharge`, `dailyfind`, `redpacket`, `firstblood`, `levelreward`, `brightsea`, `232`, `buff`, `fightmode`
  - placeholder entries include `customerservice`, `autobrush`, `partnerawake`, `team_invite`, `team_create`, `team_search`, `activity_rank`, `onhook`, `onhook_addition`, `marriage_gift_tips`, `pushgift`, `redpacket_rain`, `templeawaken`, `tt_record`

## Differences Found

- Old runtime did not reach a stable clean MainUI during this round because the account entered a forced popup/guide chain:
  - offline reward popup
  - new function overlay
  - rebate pack popup
  - forge guide
- Clicking a broad overlay area propagated into real underlying MainUI entries, opening `转职`/`锻造`; this is old-client runtime behavior and must not be confused with a clean MainUI baseline.
- Browser page globals did not expose `window.Laya` or stage globals in this run, so stage evidence is limited to DOM/canvas metrics and screenshots.
- Unity route mechanism already has a fallback placeholder path for unported routes; the main remaining risk is runtime verification, not static router absence.

## Common Root Cause

- The old-client account state has strong first-login/returning-player popups and guides, blocking clean MainUI screenshot capture.
- Unity verification is still blocked by MCP `Transport closed`, so runtime click validation cannot yet use Unity Editor automation.
- MainUI functional completeness should be measured in two layers:
  - route layer: does each entry click into real flow or placeholder
  - page layer: does each real flow render correctly, such as Bag background/window skin

## Code/Generation Work

- No runtime code, prefab, generated Bind, converter, Addressables group, or Unity asset was changed in this round.
- Generated local evidence only:
  - `output/heartbeat_1644/*.png`
  - `output/heartbeat_1644/*.json`
  - `output/heartbeat_1644/mainui_route_coverage.json`
  - `output/heartbeat_1644/mainui_patrol_report.md`

## Verification Screenshots / Commands

- Old Laya 720x1280 screenshots:
  - `old_laya_initial_720x1280.png`
  - `old_laya_after_login_720x1280.png`
  - `old_laya_after_enter_720x1280.png`
  - `old_laya_after_role_click_720x1280.png`
  - `old_laya_reenter_before_overlay_close_720x1280.png`
  - `old_laya_wait_forge_720x1280.png`
- Route coverage:
  - `output/heartbeat_1644/mainui_route_coverage.json`
- Commands:
  - `git diff --check` passed
  - `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed with 0 warnings and 0 errors

## Claude / MCP / Subagent Availability

- Claude Code CLI read-only review command:
  - `claude -p --permission-mode acceptEdits --allowedTools Read,Grep --output-format text`
  - result: timed out after 124 seconds, no output
- Multi-agent spawn:
  - result: failed with `agent thread limit reached`
- Unity MCP:
  - stale user relay `C:\Users\tr\.unity\relay\relay_win.exe` was killed
  - `Unity_RunCommand` still failed with `Transport closed`
  - no relay remained after the failed MCP attempt

## Next Priority

1. Use a cleaner account or explicitly skip old-client popup/guide chain before taking the clean MainUI baseline.
2. Run Unity Play Mode and execute `Shenxiao.Editor.RuntimeCapture.RuntimeUiCaptureTool.CaptureNow` once Editor automation is available.
3. Click-check MainUI in route order:
   - `role`, `bag`, `setting`, `chat`, `shop`, `map`
   - `customerservice`, `autobrush`, `partnerawake`, `team_*`
4. For Bag transparency, inspect common window/skin/conversion/runtime loading chain before touching page-specific code.
