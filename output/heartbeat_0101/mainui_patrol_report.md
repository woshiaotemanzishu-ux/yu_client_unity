# MainUI Patrol 01:01

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0101/old_runtime_current_720x1280_recapture.png`.
- Runtime node evidence: `output/heartbeat_0101/old_runtime_current_pageinfo_recapture.json` confirms a visible 720x1280 canvas.
- Focus: make the MainUI entry-module conversion queue more reliable, then continue MainUI usability checks.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab MainUI modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked missing-entry modules that still require conversion or placeholder behavior: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
- Rechecked shared bag/window background import state for `resource/game/bigBg/ui_bg_1.jpg`.
- Rechecked LayaUI pipeline queued MainUI entry rebuild path.

## Differences Found

- `Temp/ShenxiaoRunMainUIEntryModules.request` still exists; the open Unity Editor has not consumed the queued conversion marker.
- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg` exists, but `ui_bg_1.jpg.meta` is still missing.
- Entry module prefab status is unchanged:
  - real module prefabs exist for `role`, `bag`, `chat`, `setting`, `map`
  - missing module prefabs remain `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`
  - `godBefall` still lacks `GodBefallModule.prefab`
- The previous marker implementation only had a one-shot `[InitializeOnLoadMethod]` check. If the marker appears while the Editor is already open and no domain reload happens, it can sit forever.

## Common Root Cause

- Static UI gaps remain a pipeline/import issue, not a prefab hand-edit issue.
- The specific tool-chain weakness is queue discovery: the queued rebuild request needs to be discoverable while the Editor is idle, not only immediately after script reload.
- Runtime fallback work remains separate from static generation; Flow/View code should only handle behavior and failure recovery.

## Generation / Code Tasks

- Patched `Assets/Editor/LayaUI/LayaUIPipeline.cs`.
- Added a lightweight `EditorApplication.update` poller registered from `[InitializeOnLoadMethod]`.
- The poller checks every 2 seconds for `Temp/ShenxiaoRunMainUIEntryModules.request` and schedules the existing queued rebuild path.
- Existing safeguards remain:
  - no duplicate schedule while `mainUIEntryAutoRunQueued` is true
  - execution is delayed while `EditorApplication.isCompiling` or `EditorApplication.isUpdating`
  - marker is deleted before running `RunMainUIEntryModulesNoConfirm()`
- No generated prefab was hand-edited in this round.

## Verification

- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- Static route/tool check:
  - `RunMainUIEntryModules()` has `Shenxiao/LayaUI/Rebuild MainUI Entry Modules`.
  - `RunTask()` has `神霄/LayaUI/重转任务(Task)`.
  - `RegisterQueuedMainUIEntryModules()` registers the update poller and performs an immediate schedule check.
- Unity MCP health check:
  - `Unity_RunCommand`
  - result: `Transport closed`.
- Claude Code command:
  - `claude -p "Read-only only. In D:\git_res\yu_client_unity, review this intended change: LayaUIPipeline registers EditorApplication.update polling every 2 seconds to call ScheduleQueuedMainUIEntryModules when Temp/ShenxiaoRunMainUIEntryModules.request exists, while RunQueued delays during compiling/updating. Any compile or duplicate-run risk? Answer in 5 bullets."`
  - result: timed out after 20 seconds with no output.
  - residual `claude.exe` and `relay_win.exe` processes were stopped.
- Multi-agent:
  - spawned one read-only explorer for queued marker analysis.
  - result was not available within this heartbeat and the agent was closed while still running; no output counted as progress.
- Current file/process state:
  - `ui_bg_1.jpg.meta`: missing
  - `Temp/ShenxiaoRunMainUIEntryModules.request`: still present
  - Unity Editor remains open
  - no residual `claude.exe` or `relay_win.exe` after cleanup

## Next Priority

1. Trigger a Unity Editor script reload or refresh once. After this patch, the queue poller should keep watching the marker during idle time.
2. Confirm `Temp/ShenxiaoRunMainUIEntryModules.request` is deleted and `ui_bg_1.jpg.meta` is generated.
3. If marker still does not execute, use the Unity menu `Shenxiao/LayaUI/Rebuild MainUI Entry Modules` directly.
4. Runtime-click verify Bag first: background, content, placeholder fallback, and no stuck `_loading`.
5. Continue MainUI real-page checks: `role`, `chat`, `setting`, `map`, then `shop`, then `vip/pet/redPacket/rune/marriage/godBefall`.
