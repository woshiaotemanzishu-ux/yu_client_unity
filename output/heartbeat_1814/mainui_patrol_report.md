# Shenxiao UI Heartbeat 18:14 Report

## Scope

- Baseline target: old Laya runtime at `http://127.0.0.1:8090/index.html`, portrait `720x1280`.
- Focus: start from recovered MainUI and begin clickable-entry evidence collection.
- Unity policy: no prefab hand edits; no converter/UI code changes in this round.

## Covered Entries

- MainUI start state:
  - Captured `old_laya_mainui_start_720x1280.png`.
  - Runtime canvas probe remains `720x1280`.
- MainUI visible activity/gift path:
  - A click intended for settings mis-hit the visible old-client gift activity entry.
  - Captured `old_laya_mainui_click_settings_720x1280.png`; actual result is `超值礼包/十倍返利` runtime popup.
- Secondary mis-hit:
  - Attempting to close the gift popup by visual close coordinate led into `御风云骑/幻化` page.
  - Captured `old_laya_after_close_gift_popup_720x1280.png`.
- Recovery:
  - Re-ran the old-client recovery sequence: reload -> remembered login -> server enter -> role enter.
  - Captured `old_laya_recovered_mainui_after_misclick_720x1280.png`.
  - Screenshot still contains the `超值礼包/十倍返利` popup, so the current browser state is not a clean MainUI HUD.

## Findings

- The old runtime click coordinate mapping is not yet stable enough for entry-by-entry patrol.
- The first intended settings click at `(36,1066)` did not open `SettingView`; it opened an activity/gift popup. This cannot be counted as settings coverage.
- The gift close coordinate `(648,126)` did not produce a clean MainUI return; it led into a mount/illusion page. This is a patrol automation problem, not a Unity UI defect.
- CDP/runtime inspection is blocked, so we cannot currently call old-client runtime close functions directly.

## Source Evidence

- `D:\git_res\yu_client\h5\src\mainUI\MainUIChatView.ts:114` binds `_box_setting` to `SettingView`.
- `D:\git_res\yu_client\h5\laya\pages\resource\game\mainUI\MainUIChatView.scene:125` defines `_box_setting`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUIChatView.ts:131` binds `_box_shop` to `OPEN_SHOP_MAIN_VIEW`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:153` binds `_box_cs` to customer service.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:185` binds `_box_vip` to `OPEN_VIP_VIEW`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:189` binds `_box_recharge` to `RechargeView`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUITopView.ts:193` binds `_box_map` to `MapEnterView`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:173` binds auto-brush click to `AutoBrushBaseView`.

## Executed Commands / Tool Tasks

- Browser:
  - Set viewport to `720x1280`.
  - Saved runtime screenshots under `D:\git_res\yu_client_unity\output\heartbeat_1814`.
  - Verified canvas rect `720x1280`.
- Claude Code CLI:
  - Command: `claude -p "...MainUI 可见入口名到坐标/组件名..."`
  - Result: timed out after 120 seconds with no useful output. This is recorded as failed collaboration and not counted as delivered analysis.
- Unity MCP:
  - `Unity_RunCommand` ping failed: `Transport closed`.
  - `Get-Process relay_win` found no stale `relay_win.exe` process.
- Static verification:
  - `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed with `0` warnings and `0` errors.
  - `git diff --check` passed.
- CDP:
  - Runtime globals probe failed with `Raw CDP is unavailable while Browser Use is resolving a paused document response`.

## Common Root Cause

- Old Laya runtime is canvas-only for UI hit testing. Browser DOM has no usable UI node contracts.
- Static `.scene` coordinates are useful for source-backed click-target hints, but they cannot be treated as final runtime state.
- Current patrol needs a click calibration layer that maps runtime screenshots and source node positions to reliable browser CUA coordinates before broad entry coverage.

## Next Priority

1. First solve clean MainUI state:
   - close or suppress the `超值礼包/十倍返利` popup without triggering underlying entries;
   - verify the browser is on clean MainUI HUD before any entry click.
2. Build a small evidence table for MainUI entries:
   - component/source event;
   - intended runtime coordinate;
   - actual screenshot after click;
   - close/recovery method.
3. Only after old runtime entry clicks are stable, compare Unity runtime:
   - migrated entry opens real page;
   - unmigrated entry opens uniform placeholder.
4. If Unity shows transparent backgrounds, missing frames, bad skins, missing templates, broken Bind, or missing Addressables, fix the shared LayaUI conversion/Bind/resource pipeline and regenerate through Unity Editor menus.
