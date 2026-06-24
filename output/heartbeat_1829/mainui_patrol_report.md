# Shenxiao UI Heartbeat 18:29 Report

## Scope

- Baseline target: old Laya runtime at `http://127.0.0.1:8090/index.html`, portrait `720x1280`.
- Focus: fix the previous click-calibration blocker, recover clean MainUI, and validate at least one usable MainUI module.
- Unity policy: no prefab hand edits; no converter/UI code changes in this round.

## Covered Entries

- Clean MainUI recovery:
  - ESC/Backspace do not close the `超值礼包/十倍返利` popup.
  - Gift popup close at `(625,124)` is reliable enough to advance to the next queued popup without immediately entering a wrong module.
  - `众生之门` popup close at `(656,176)` returns to clean MainUI.
  - Clean HUD evidence: `old_laya_single_close_2_after_zhongsheng_720x1280.png`.
- Settings / system:
  - Click `(36,1060)` from clean MainUI opens old runtime `系统设置`.
  - Close `(655,207)` returns to clean MainUI.
  - This is a completed old-client module click/close loop.
- Recharge:
  - Click `(402,83)` on top recharge entry opens old runtime `超值礼包/十倍返利` recharge activity popup, not a plain recharge page.
  - Closing gift popup then closing `众生之门` can lead into queued `大比拼` page if over-clicked or if another queued activity is active.
  - Current final browser state after this round is `大比拼`, not clean MainUI.

## Screenshots

- `old_laya_after_escape_on_gift_popup_720x1280.png`
- `old_laya_after_backspace_on_gift_popup_720x1280.png`
- `old_laya_after_gift_close_edge_625_124_720x1280.png`
- `old_laya_single_close_1_after_gift_720x1280.png`
- `old_laya_single_close_2_after_zhongsheng_720x1280.png`
- `old_laya_clean_mainui_click_bottom_system_36_1060_720x1280.png`
- `old_laya_after_close_settings_720x1280.png`
- `old_laya_clean_mainui_click_recharge_402_83_720x1280.png`
- `old_laya_after_basewindow_return_on_dabipin_718_1115_720x1280.png`

## Findings

- Settings entry is now calibrated and usable:
  - open: `(36,1060)`
  - close: `(655,207)`
- Queued old-client activity popups are the main patrol blocker:
  - fixed-coordinate repeated close is unsafe.
  - single-step close with screenshot verification is required.
- `大比拼` does not close through the previously known BaseWindow edge return coordinate `(718,1115)`.
- Recharge runtime behavior is not equivalent to a direct `RechargeView` screenshot yet; the old client currently routes through activity/recharge promotion UI first.

## Source Evidence

- `D:\git_res\yu_client\h5\src\mainUI\MainUIChatView.ts:114` binds `_box_setting` to `SettingView`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:189` binds `_box_recharge` to `RechargeView`; old runtime still shows promotion popups before a plain recharge page in the current account/session.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:153` binds `_box_cs` to customer service.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:193` binds `_box_map` to `MapEnterView`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:173` binds auto-brush click to `AutoBrushBaseView`.

## Executed Commands / Tool Tasks

- Browser:
  - Set viewport to `720x1280`.
  - Saved runtime screenshots under `D:\git_res\yu_client_unity\output\heartbeat_1829`.
  - Verified settings open/close loop.
  - Verified queued-popup close sequence for gift -> `众生之门` -> MainUI.
- Claude Code CLI:
  - Command: `claude -p "只读，30秒内回答..."`
  - Result: timed out after 45 seconds with no useful output. This is recorded as failed collaboration and not counted as delivered analysis.
- Unity MCP:
  - `Unity_RunCommand` ping failed: `Transport closed`.
  - `Get-Process relay_win` found no stale `relay_win.exe` process.
- Static verification:
  - `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed with `0` warnings and `0` errors.
  - `git diff --check` passed.

## Common Root Cause

- Old Laya runtime stacks promotional popups after entering MainUI and after clicking recharge-related entries.
- Canvas-only hit testing means we need visual confirmation after each click; static `.scene` coordinates are only click-target hints, not final runtime proof.
- Unity comparison is blocked while Unity MCP remains `Transport closed`; the old-client side evidence still advances, but no Unity runtime equivalence claim should be made from this round.

## Next Priority

1. Start by recovering from the current `大比拼` page:
   - find its source view and close/back mechanism;
   - if unavailable, reload and close queued popups one by one until clean MainUI.
2. Continue old-client calibrated entries:
   - settings is done;
   - next: role/bag bottom entries, map, auto-brush, customer service.
3. Once Unity MCP is restored, compare each calibrated old-client entry against Unity:
   - migrated module opens real page;
   - unmigrated module opens uniform placeholder.
4. If Unity static UI defects appear, fix the shared LayaUI conversion/Bind/resource pipeline and regenerate through Unity Editor menus.
