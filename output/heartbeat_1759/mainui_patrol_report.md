# Shenxiao UI Heartbeat 17:59 Report

## Scope

- Baseline: old Laya runtime at `http://127.0.0.1:8090/index.html`, canvas verified as `720x1280`.
- Focus: recover old runtime back to MainUI and capture MainUI HUD evidence before continuing entry-by-entry comparison.
- Unity policy: no prefab hand edits; static UI defects remain converter/Bind/Addressables/router work.

## Covered Entries

- Old runtime recovery path:
  - Reloaded blocked GuildMainView state back to login.
  - Clicked login with the prefilled remembered account.
  - Clicked server enter.
  - Clicked role-select `_img_enter` using source-backed runtime coordinate.
  - Closed the offline reward modal and reached visible MainUI.
- Visible MainUI baseline now includes top HUD, resource bar, VIP/recharge/customer service, activity icons, task panel, auto-pathing, skill cluster, map/instance buttons, auto idle button, and bottom-left system/role/bag-style entry cluster.

## Evidence

- Current MainUI baseline screenshot:
  - `D:\git_res\yu_client_unity\output\heartbeat_1759\old_laya_mainui_after_offline_reward_close_retry_720x1280.png`
- Recovery screenshots:
  - `old_laya_after_reload_recovery_try_720x1280.png`
  - `old_laya_after_login_click_720x1280.png`
  - `old_laya_after_enter_click_720x1280.png`
  - `old_laya_after_select_role_enter_real_coord_720x1280.png`
- Runtime canvas probe:
  - `canvas.width=720`
  - `canvas.height=1280`
  - DOM rect `720x1280` at `(0,0)`
- Source/node evidence used only to locate click targets:
  - `D:\git_res\yu_client\h5\src\login\LoginSelectRoleView.ts:173` binds `_img_enter` click to `TRY_LOGIN_GAME`.
  - `D:\git_res\yu_client\h5\laya\pages\resource\game\login\LoginSelectRoleView.scene:67` defines `_img_enter`.
  - `D:\git_res\yu_client\h5\laya\pages\resource\game\login\LoginSelectRoleView.scene:74` sets `_img_enter.bottom=120`, producing click area around `y=1020..1160`.
  - `D:\git_res\yu_client\h5\src\common\BaseWindowComponent.ts:626` binds common return to `BaseWindowCloseFunc`.
  - `D:\git_res\yu_client\h5\laya\pages\resource\game\common\BaseWindowSkin.scene:205` shows `_img_return` at `x=713,y=1077`, which explains why the first visible-coordinate return click was unreliable.

## Differences / Root Causes

- This round did not complete Unity-vs-Laya pixel comparison for MainUI entries because old runtime started in GuildMainView and keyboard/back did not close it.
- Common root cause for automation instability: old Laya runtime does not expose usable DOM for UI nodes; visual state is canvas-only, and source coordinates must be treated as click-target hints, not as final page evidence.
- The previous GuildMainView return issue is a patrol-flow problem. Standard close is common window `_img_return`, but this state placed it on the right/bottom edge and the coordinate click did not close reliably.
- MainUI can now be reached by a repeatable path: reload -> login -> server enter -> select-role `_img_enter` at roughly `(360,1090)` -> close offline reward modal.

## Executed Commands / Tool Tasks

- Claude Code CLI read-only analysis succeeded earlier in this heartbeat:
  - Checked old Laya BaseWindow/Guild close rules.
  - Confirmed ESC/Backspace are not the standard old-client close path.
  - Confirmed `_img_return` / `BaseWindowCloseFunc` is the intended close chain.
- Browser automation:
  - Set viewport to `720x1280`.
  - Captured old runtime screenshots listed above.
  - Verified MainUI recovery path.
- Unity MCP:
  - `Unity_RunCommand` ping failed with `Transport closed`.
  - `Get-Process relay_win` returned no process, so there was no stale relay to kill in this check.
- Static verification:
  - `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed with `0` warnings and `0` errors.
  - `git diff --check` passed.

## Next Priority

1. Start next heartbeat from the recovered old MainUI screenshot/state.
2. Click visible MainUI entries one by one and pair each click with old runtime screenshot plus Unity runtime screenshot:
   - bottom-left system/settings, role, bag-like entries;
   - customer service, recharge/VIP, activity/festival/welfare/share;
   - task/team panel;
   - map/instance/auto idle;
   - chat/notice area after HUD cleanup if visible.
3. For migrated modules, require real pages. For unmigrated modules, require the Unity MainUI router to open a uniform placeholder panel.
4. If a page shows transparent background, missing frame, wrong default skin, missing template, wrong dynamic image, or lost Bind, fix the shared LayaUI conversion/Bind/resource mapping pipeline and regenerate through Unity Editor menus.
