# MainUI Patrol 01:17

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0117/old_runtime_current_720x1280_recapture.png`.
- Runtime node evidence: `output/heartbeat_0117/old_runtime_current_pageinfo_recapture.json` confirms a visible 720x1280 canvas.
- Focus: MainUI entry usability while Unity refresh/import remains blocked.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab MainUI modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked missing-entry modules that must still use placeholder or be regenerated: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
- Rechecked shared bag/window background import state for `resource/game/bigBg/ui_bg_1.jpg`.
- Rechecked central MainUI route placeholder behavior for unregistered or failed routes.

## Differences Found

- Unity Editor has not imported recent filesystem changes:
  - `Editor.log` last write time is still `2026-06-24 09:01:33`.
  - `ui_bg_1.jpg.meta` is still missing.
  - `Temp/ShenxiaoRunMainUIEntryModules.request` still exists.
- `ProjectSettings/EditorSettings.asset` contains `m_RefreshImportMode: 0`; combined with the stale Editor log, current Editor is not auto-importing these external changes. I did not change this global setting because the enum value was not verified locally.
- MainUI route fallback existed, but its placeholder copy was still debug-like English text. For user-visible MainUI clicks, the unmigrated route should open a simple unified empty panel instead.

## Common Root Cause

- Static UI generation remains blocked by Unity import/refresh not running, not by prefab hand-edit work.
- MainUI click usability should be protected at runtime through `MainUIRouter` + `MainUIRoutePlaceholder`, while generated UI assets continue to be fixed through the LayaUI pipeline.
- The placeholder is runtime fallback behavior, not a replacement for proper conversion.

## Generation / Code Tasks

- Patched `Assets/Scripts/Module/Core/MainUI/MainUIRoutePlaceholder.cs`.
- Updated fallback panel copy from debug English to Chinese user-facing text:
  - title: `功能待接入`
  - body: includes the route key and `该功能正在移植中`
- Enlarged and recolored the panel slightly so unmigrated entries open a readable unified empty panel.
- Preserved click-to-close shade and close button behavior.
- No generated prefab was hand-edited in this round.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 errors; first parallel run hit a temporary output-file lock from Huorong, then sequential Core build passed.
- `git diff --check`: passed.
- Old runtime evidence:
  - `output/heartbeat_0117/old_runtime_current_720x1280_recapture.png`
  - `output/heartbeat_0117/old_runtime_current_pageinfo_recapture.json`
- Claude Code command:
  - `claude -p "Read-only review only, do not modify files. In D:\git_res\yu_client_unity, review recent changes: LayaUIPipeline queued marker polling and MainUIRoutePlaceholder Chinese placeholder styling. Check for compile/runtime risks or duplicate execution. Reply concise."`
  - result: timed out after 20 seconds with no output.
  - residual `claude.exe` and `relay_win.exe` processes were stopped.
- Unity MCP health check:
  - `Unity_RunCommand`
  - result: `Transport closed`.
- Current file/process state:
  - `ui_bg_1.jpg.meta`: missing
  - `Temp/ShenxiaoRunMainUIEntryModules.request`: still present
  - no residual `claude.exe` or `relay_win.exe` after cleanup

## Next Priority

1. Get one successful Unity refresh/import action. Current evidence points to the open Editor not auto-importing filesystem changes.
2. After refresh, confirm the LayaUI queue poller is imported, marker is consumed, and `ui_bg_1.jpg.meta` is generated.
3. Verify MainUI placeholder by clicking an unmigrated route such as `shop`/`vip`/`pet`.
4. Verify Bag first after import: background visible, content appears when prefab is valid, placeholder appears on failed load, and `_loading` does not stick.
5. Continue real-page checks: `role`, `chat`, `setting`, `map`, then `shop`, then `vip/pet/redPacket/rune/marriage/godBefall`.
