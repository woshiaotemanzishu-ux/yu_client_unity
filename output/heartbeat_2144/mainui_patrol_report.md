# Shenxiao UI Patrol - 21:44 MainUI Idle Precondition

## Scope

- Automation: `shenxiao-ui`
- Baseline: old Laya runtime at `http://127.0.0.1:8090/index.html`
- Required viewport: 720x1280 portrait mobile
- Focus: define and verify the precondition needed before MainUI entry-by-entry comparison

## Covered Entries

- Current old runtime state captured:
  - `old_laya_current_start.png`
- Fixed viewport and re-captured the same page in proper 720x1280 portrait:
  - `old_laya_current_after_viewport_set.png`
- Tried to leave `大比拼` via Escape and browser history:
  - `old_laya_after_escape_from_activity.png`
  - `old_laya_after_browser_back_from_activity.png`
- Recovered old client after browser back caused a black page:
  - `old_laya_after_recover_goto_index.png`

## Findings / Differences

- `browser.back()` must not be used as an old-client UI return action. It navigated the browser to a black/blank client state instead of returning in-game.
- `大比拼` is not MainUI. Its bottom icons are visible, but screenshots taken there are not valid MainUI baseline evidence.
- Viewport state can drift back to landscape/wide screenshots. Each patrol round must explicitly set 720x1280 and verify the screenshot dimensions/visual orientation before baseline capture.
- No valid new MainUI entry coverage was claimed in this round.

## Source Evidence / Common Root Cause

- Old runtime must be stabilized before entry clicks:
  - Reward layer: `D:\git_res\yu_client\h5\src\common\CongratulationObtainView.ts:201-220` supports click-to-stop animation and second click-to-close; `:325-330` listens to `EventName.CLOSE_CONGRATULATION_VIWE`.
  - Auto brush: `D:\git_res\yu_client\h5\src\commonModel\AutoBrushModel.ts:355-360` implements `StopAutoBrushState()`, sending protocol `13307` with `"c", 1` only when auto state is active.
  - Auto brush enter/continue: `D:\git_res\yu_client\h5\src\autoBrush\AutoBrushMainView.ts:226-233` sends `13307` with `"c", 0` when starting auto brush from the auto brush view.
  - Auto task: `D:\git_res\yu_client\h5\src\commonModel\TaskModel.ts:3244-3250` reads `SettingModel` key `auto_task`; `D:\git_res\yu_client\h5\src\setting\SettingView.ts:270-285` toggles it; `:853-858` sends packed protocol `10203`.
- Common root cause remains old-client runtime state: reward, guide, activity, and auto-task systems intercept clicks. Unity comparison should not proceed until this is deterministic.

## Proposed Patrol Precondition

1. Set viewport to 720x1280 and confirm portrait screenshot.
2. Never use browser history/back for game navigation.
3. Login and select a patrol role only after ensuring it is not inside first-time guide/reward chain.
4. Clear `CongratulationObtainView` with click-to-stop then click-to-close, or use its `CLOSE_CONGRATULATION_VIWE` event if a runtime hook becomes available.
5. Open Settings and set `auto_task` to off through the Settings UI/protocol.
6. Stop active auto brush using the MainUI auto button or old protocol semantics `13307`.
7. Only then start MainUI entry clicks and screenshots.

## Generation / Code Tasks

- No code edits in this heartbeat.
- No prefab edits.
- No Unity conversion/regeneration executed.

## Verification

- Screenshots saved under `output/heartbeat_2144/`.
- `claude --version` returned `2.1.185 (Claude Code)`.
- `dotnet build yu_client_unity.slnx -v:minimal` passed with 0 warnings and 0 errors.
- Unity MCP:
  - `Unity_RunCommand` result: `Transport closed`
  - No `relay_win.exe` residue was present before the MCP attempt.

## Claude / MCP Availability

- Claude CLI is installed and can answer version checks.
- No Claude Code implementation task was run because the current work is still old-client runtime stabilization, not a code-change target.
- Unity MCP remains unavailable, so Unity Editor menu generation cannot be used this round.

## Next Priority

1. Implement or script a repeatable old-client idle precondition using the source findings above.
2. Use that idle state to capture clean MainUI baseline.
3. Then validate MainUI entries: VIP, recharge, customer service, settings, role, bag, chat, map, task/team, and auto brush.
4. Only after valid baseline evidence should Unity router/converter fixes be applied.
