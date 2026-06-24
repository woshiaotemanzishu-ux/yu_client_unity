# Shenxiao UI Patrol - 21:29 MainUI Runtime Stabilization

## Scope

- Automation: `shenxiao-ui`
- Baseline: old Laya runtime at `http://127.0.0.1:8090/index.html`
- Viewport: 720x1280 portrait mobile
- Focus: stabilize old-client MainUI HUD before entry-by-entry comparison

## Covered Entries

- Reloaded old Laya client and captured current polluted state:
  - `old_laya_current_before_reload.png`
  - `old_laya_after_reload.png`
- Logged in with `zxczxc / zxczxc`.
- Entered role selection and selected low-level role:
  - `章修帝`, initially shown as `0转5级`
  - `old_laya_after_select_low_level_role.png`
  - `old_laya_after_role_enter_attempt.png`
- Cleared or attempted to clear runtime popups:
  - `挂机收益`
  - skill unlocks
  - story dialog with `跳过`
  - `斩秘巡行` reward / exit attempt
  - `新形象解锁`
  - `御风云骑` explanation popup
- Verified Settings can open as a real old-client page:
  - `old_laya_after_mount_cleanup_candidate.png`
  - `old_laya_after_settings_close_calibration_candidate.png`

## Findings / Differences

- The low-level role is better than the high-level role for baseline work because the HUD initially shows `开启挂机`, not `取消挂机`.
- However, the low-level role is still not immediately usable for entry-by-entry comparison:
  - Offline reward must be handled.
  - Claiming rewards triggers level jumps and skill unlocks.
  - Skill unlocks trigger story/dialog chains.
  - The client re-enters `斩秘巡行` and `大比拼` activity flow.
- Settings opened successfully, but closing it was not stable during reward/guide flow. Multiple close-hotspot attempts eventually moved into activity/story state instead of producing a clean HUD.
- Therefore, MainUI entry screenshots are still not trustworthy unless the old-client guide/auto/reward chain is first controlled.

## Common Root Cause

- The current patrol is blocked by old-client runtime state, not by a Unity prefab/conversion issue.
- The old Laya runtime continues to run guide, reward, activity, and auto-continue flows after login. These flows intercept clicks and can turn a MainUI button click into an unrelated activity/page transition.
- A deterministic idle setup is required before comparing Unity MainUI.

## Generation / Code Tasks

- No code edits in this heartbeat.
- No prefab edits.
- No Unity conversion/regeneration executed.
- No page was marked complete.

## Verification

- Screenshots saved under `output/heartbeat_2129/`.
- `claude --version` returned `2.1.185 (Claude Code)`.
- `dotnet build yu_client_unity.slnx -v:minimal` passed with 0 warnings and 0 errors.
- Unity MCP:
  - Before relay cleanup: `Transport closed`
  - Cleaned residual `relay_win.exe`
  - After cleanup: still `Transport closed`

## Claude / MCP Availability

- Claude CLI is installed and responds to `--version`.
- No Claude Code implementation task was run this round because there was no code change target yet.
- Unity MCP is still unavailable after relay cleanup, so Unity Editor menu generation cannot be executed from MCP in this heartbeat.

## Next Priority

1. Stop using arbitrary old-client account state as the baseline.
2. Establish an old Laya idle setup path:
   - Prefer a dedicated patrol account/role with rewards and guide chain already cleared, or
   - Add a repeatable local runtime control path to stop auto brush / guide / reward auto-continue before clicking entries.
3. Once idle HUD is stable, re-run MainUI entry coverage:
   - Top: VIP, recharge, customer service.
   - Bottom: settings, role, bag, skill cluster, auto brush toggle.
   - Right/left activity entries and task/team/map.
4. Only after old runtime evidence is clean should Unity comparison and converter/router fixes proceed.
