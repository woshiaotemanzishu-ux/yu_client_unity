# Shenxiao UI Patrol - MainUI 2026-06-24 02:57

## Scope
- Automation: `shenxiao-ui`
- Focus: MainUI first, old Laya runtime 720x1280 portrait as baseline.
- No code or prefab edits in this round.

## Covered Entries
- Old Laya runtime recovery path:
  - `reload`
  - login `(467,722)`
  - server enter `(360,935)`
  - role enter `(360,1090)`
  - close `十倍返利` `(625,124)`
  - close `众生之门` `(656,176)`
  - close extra `送强力红武` `(285,168)` when it appeared
- MainUI clean baseline:
  - `output/heartbeat_1844/old_laya_after_close_zhongsheng_720x1280.png`
  - `output/heartbeat_1844/old_laya_after_close_redweapon_720x1280.png`
- Bottom role:
  - click `(102,1214)`
  - result: old runtime `人物` page opened
  - evidence: `output/heartbeat_1844/old_laya_after_click_role_estimate_720x1280.png`
- Bottom bag:
  - click `(207,1214)`
  - result: old runtime `背包` page opened
  - evidence: `output/heartbeat_1844/old_laya_after_click_bag_estimate_720x1280.png`

## Findings
- Old Laya `背包` runtime is not transparent:
  - has full light panel/background
  - shows role/equipment slots
  - shows real item grid with item icons
  - right-side buttons are visible: `装备吞噬`, `共鸣打造`, `容量扩充`, `一键使用`
- Existing Unity evidence still differs:
  - `output/runtime_unity/current_screen_capture_after_with_mainui.png`
  - `output/runtime_unity/play_bag_after_common_tab_fix.png`
  - Unity bag overlays on top of MainUI/scene; the old-client full background/panel is missing or not equivalent.
  - Unity bag runtime data evidence says `bagGoodsCount=0`, so item grid is still placeholder/empty while old runtime account has visible items.
- Old `人物` page opens correctly from bottom role. Close/back coordinate is not yet calibrated:
  - `(705,1115)` did not close it.
  - `Escape` + `Backspace` did not close it.
  - evidence: `old_laya_after_close_role_attempt_720x1280.png`, `old_laya_after_role_escape_backspace_720x1280.png`
- Browser runtime cannot read `window.Laya`/stage:
  - `window.Laya`, `window.laya`, `ViewManager`, `LayerManager`, `MainUIModel` all returned `undefined`.
  - Evidence for old runtime this round is screenshot + canvas dimensions + click result, not exported stage tree.

## Common Root Causes
- Bag visual mismatch should not be fixed by hand-editing the prefab as final work.
- Likely shared areas:
  - `BaseWindowSkin` / `BaseWindowSkinView` background and masking behavior.
  - LayaUI conversion default image/background handling.
  - Bag module content reparenting into shared frame.
  - Runtime bag data/protocol chain: Unity shows `bagGoodsCount=0`, while old runtime account shows items.
- MainUI clickability is partly in place:
  - `MainUIRouter` has placeholder fallback for unregistered entries.
  - Real registered entries include `role`, `bag`, `chat`, `setting`, `shop`, `map`, `friend`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`.
  - Known missing/unregistered targets from read-only scan: `customerservice`, `team_create`, `team_search`, `templeawaken`, `autobrush`, activity icon routes such as `158` / `338@...`.

## Commands And Tool Status
- Build:
  - `dotnet build .\yu_client_unity.slnx -v:minimal --nologo`
  - result: success, 0 warnings, 0 errors.
- Diff check:
  - `git diff --check`
  - result: clean.
- Claude Code:
  - `claude --version`
  - result: `2.1.185 (Claude Code)`
  - read-only prompt command:
    - `claude -p "只读分析，不要修改文件..."`
    - result: timed out after about 64s, no usable analysis returned.
- Unity MCP:
  - `Unity_RunCommand` ping failed with `Transport closed`.
  - `relay_win.exe` check found one process:
    - `C:\Users\tr\.unity\relay\relay_win.exe`
    - treated as current Unity relay, not killed.

## Executed Code/Generation Tasks
- None in this round.
- No prefab edits.
- No converter/editor-menu regeneration in this round because current work was evidence gathering and MCP was unavailable.

## Evidence Files
- Old Laya current/start:
  - `output/heartbeat_1844/old_laya_current_start_720x1280.png`
- Recovery/login sequence:
  - `old_laya_after_reload_720x1280.png`
  - `old_laya_after_login_click_720x1280.png`
  - `old_laya_after_server_enter_720x1280.png`
  - `old_laya_after_role_enter_720x1280.png`
  - `old_laya_after_close_gift_720x1280.png`
  - `old_laya_after_close_zhongsheng_720x1280.png`
  - `old_laya_recovered_mainui_for_bag_720x1280.png`
  - `old_laya_after_close_redweapon_720x1280.png`
- Entry results:
  - `old_laya_after_click_role_estimate_720x1280.png`
  - `old_laya_after_click_bag_estimate_720x1280.png`
- Unity comparison evidence reused:
  - `output/runtime_unity/current_screen_capture_after_with_mainui.png`
  - `output/runtime_unity/play_bag_after_common_tab_fix.png`
  - `output/runtime_unity/play_bag_after_common_tab_fix_with_mainui_nodes.txt`

## Next Priority
1. Fix Unity MCP connection or restart current Unity relay cleanly from the Editor side, then capture fresh Unity runtime screenshots.
2. MainUI clickability batch:
   - `role`, `bag`, `chat`, `setting`, `shop`, `map`, `autobrush_toggle`.
3. Bag parity:
   - shared frame/background/masking through converter/common frame pipeline.
   - bag item data chain, because current Unity evidence has `bagGoodsCount=0`.
4. Register or intentionally route placeholders for:
   - `customerservice`
   - `team_create`
   - `team_search`
   - `templeawaken`
   - `autobrush`
   - activity icon routes `158`, `338@...`.
