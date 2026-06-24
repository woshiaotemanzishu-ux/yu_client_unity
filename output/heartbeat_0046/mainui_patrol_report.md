# MainUI Patrol 00:46

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0046/old_runtime_current_720x1280_recapture.png`.
- Runtime node evidence: `output/heartbeat_0046/old_runtime_current_pageinfo_recapture.json` confirms a visible 720x1280 canvas.
- Focus: unblock MainUI conversion/import path first, then keep MainUI entries usable.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab MainUI modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked missing-entry modules that still require conversion or placeholder behavior: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
- Rechecked shared bag/window background import state for `resource/game/bigBg/ui_bg_1.jpg`.
- Rechecked LayaUI pipeline entry menu wiring for MainUI entry rebuild and Task rebuild.

## Differences Found

- `Temp/ShenxiaoRunMainUIEntryModules.request` still exists; the open Unity Editor has not consumed the queued conversion marker.
- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg` exists, but `ui_bg_1.jpg.meta` is still missing.
- Entry module prefab status is unchanged:
  - real module prefabs exist for `role`, `bag`, `chat`, `setting`, `map`
  - missing module prefab directories or module prefabs remain `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`
- `Assets/Editor/LayaUI/LayaUIPipeline.cs` had a menu wiring defect: `神霄/LayaUI/重转任务(Task)` was attached to `RunMainUIEntryModules()` instead of `RunTask()`.
- Unity Editor log shows prior `Asset Pipeline Refresh` / domain reload records and repeated Unity Connect certificate/token errors, but no `Auto-running queued MainUI entry module rebuild` or `ui_bg_1` import evidence.

## Common Root Cause

- The active blocker remains Unity Editor command/refresh access, not prefab content: MCP cannot execute commands, desktop automation timed out on app approval, and batchmode cannot safely run while the project is already open.
- Static UI gaps still belong to the LayaUI conversion/import pipeline: generated prefabs, shared backgrounds, Bind, Addressables, and default skins should be regenerated/imported through Unity tooling.
- The menu wiring issue is a conversion-tool entry defect; fixing it reduces the chance of running the wrong pipeline action from Unity menus.

## Generation / Code Tasks

- Patched `Assets/Editor/LayaUI/LayaUIPipeline.cs`.
- Moved `[MenuItem("神霄/LayaUI/重转任务(Task)", priority = 21)]` from `RunMainUIEntryModules()` to `RunTask()`.
- Kept `[MenuItem("Shenxiao/LayaUI/Rebuild MainUI Entry Modules", priority = 22)]` on `RunMainUIEntryModules()`.
- No generated prefab was hand-edited in this round.

## Verification

- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- Menu wiring static check:
  - `RunMainUIEntryModules()` now only has `Shenxiao/LayaUI/Rebuild MainUI Entry Modules`.
  - `RunTask()` now has `神霄/LayaUI/重转任务(Task)`.
- Unity MCP health check:
  - `Unity_RunCommand`
  - result: `Transport closed` before and after cleaning residual processes.
- Computer Use attempt:
  - located Unity window `yu_client_unity - Launch - Web - Unity 6.3 LTS (6000.3.17f1) <DX12>`
  - attempted to activate and send refresh, but Computer Use returned `Computer Use app approval timed out`; this action was not counted as a successful Unity refresh.
- Claude Code:
  - `claude --version`: `2.1.185 (Claude Code)`
  - read-only analysis command timed out after 60 seconds with no output.
  - residual `claude.exe` and `relay_win.exe` from the timeout were stopped.
- Current file/process state:
  - `ui_bg_1.jpg.meta`: missing
  - `Temp/ShenxiaoRunMainUIEntryModules.request`: still present
  - no residual `claude.exe` or `relay_win.exe` after cleanup

## Next Priority

1. Get one successful Unity-side refresh/command path: MCP command, approved Computer Use action, or user/manual Unity `Assets/Refresh`.
2. Confirm `ui_bg_1.jpg.meta` generation and marker consumption.
3. Run `Shenxiao/LayaUI/Rebuild MainUI Entry Modules` from Unity after refresh if the queued marker still does not execute.
4. Runtime-click verify Bag first: background, content, placeholder fallback, and no stuck `_loading`.
5. Continue MainUI real-page checks: `role`, `chat`, `setting`, `map`, then `shop`, then `vip/pet/redPacket/rune/marriage/godBefall`.
