# MainUI Patrol 02:18

## Scope

- Continue MainUI-first patrol. This round focused on proving why Unity does not consume `Temp/ShenxiaoRunMainUIEntryModules.request` and does not import `ui_bg_1.jpg`.
- Old Laya runtime evidence retained:
  - `output/heartbeat_0218/old_runtime_current_720x1280_recapture.png`
  - `output/heartbeat_0218/old_runtime_current_pageinfo_recapture.json`

## Covered Entries

- MainUI route coverage was rechecked through a read-only subagent.
- Registered real route keys:
  - `role`, `bag`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`
  - `map`, `setting`, `buff`, `fightmode`, `vip`, `recharge`, `halo`
  - `chat`, `friend`, `shop`, `email`, `redpacket`, `levelreward`, `firstblood`, `dailyfind`, `brightsea`, `guildhelp`, `autobrush_toggle`
- Not registered, expected to use placeholder when clicked:
  - `customerservice`
  - `team_invite`, `pushgift`, `onhook`, `onhook_addition`, `marriage_gift_tips`, `redpacket_rain`, `tt_record`
  - `team_create`, `team_search`, `templeawaken`
  - `partnerawake`, `autobrush`, `activity_rank`
  - dynamic activity ids such as `158`, `158@0`, `158@3`, `159` if they appear.

## Differences Found

- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg` exists, but `ui_bg_1.jpg.meta` is still missing.
- `Temp/ShenxiaoRunMainUIEntryModules.request` still exists.
- `Assets/Editor/LayaUI/LayaUIPipeline.cs` was last written at `2026-06-24 09:03:54`, but Unity's loaded editor assembly `Library/ScriptAssemblies/Shenxiao.Editor.dll` was last written at `2026-06-24 00:45:41`.
- `Library/ScriptAssemblies/Shenxiao.Module.Core.dll` was last written at `2026-06-24 00:37:40`.
- Editor log has no latest `ui_bg_1` import and no `[LayaUI] Auto-running queued MainUI entry module rebuild`.
- Registry check shows `HKCU\Software\Unity Technologies\Unity Editor 5.x\kAutoRefreshMode_h2874646975 = 0x0`.

## Common Root Cause

- The most likely root cause is that the open Unity Editor has not performed a valid Asset refresh / script reload after the current code and asset changes.
- Therefore the currently running Editor probably does not contain the new `LayaUIPipeline` marker polling code, and cannot consume `Temp/ShenxiaoRunMainUIEntryModules.request`.
- This is a Unity Editor refresh/import state issue, not a bad image file and not a prefab-specific UI fix.

## Code / Generation Tasks

- No new code changes were made in this round.
- No generated prefab was hand-edited.
- No Unity generation ran because:
  - Codex MCP still returns `Transport closed`.
  - Batchmode was already proven blocked by the currently open Unity project.
  - Computer Use found the Unity window but app approval timed out before a screenshot/control session was available.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- `Unity_RunCommand` health check title `Shenxiao MCP Health 0218`: failed with `Transport closed`.
- Stale `relay_win.exe` processes created by failed MCP/automation attempts were cleaned. Final relay check returned no `relay_win.exe`.
- Exact evidence commands used:
  - `Get-Item Assets\Editor\LayaUI\LayaUIPipeline.cs,Library\ScriptAssemblies\Shenxiao.Editor.dll`
  - `Test-Path Assets\GameRes\resource\game\bigBg\ui_bg_1.jpg.meta`
  - `Select-String Editor.log -Pattern "[LayaUI] Auto-running queued MainUI entry module rebuild|ui_bg_1|Start importing"`
  - `reg query "HKCU\Software\Unity Technologies\Unity Editor 5.x" /s /f AutoRefresh`

## Claude / MCP / Subagents

- Claude safe-mode is usable for read-only review. It was not used for edits.
- Subagent `Kant` completed read-only import/MCP diagnosis and identified stale Unity script assemblies as the most likely root cause.
- Subagent `Hooke` completed read-only MainUI route coverage analysis and found no broad missing `UIUtil.AddClick` issue in Top/Chat/Secondary/TaskTeam/Skill/Activity/AutoBrush.
- Computer Use bootstrap worked and found Unity, but app approval timed out; no Unity UI action was performed.
- Unity MCP remains blocked with `Transport closed`.

## Next Priority

1. Trigger a real refresh in the already-open Unity Editor:
   - preferred: `Assets > Refresh` / `Ctrl+R`, then wait for script reload.
   - success condition: `Library/ScriptAssemblies/Shenxiao.Editor.dll` timestamp becomes newer than `LayaUIPipeline.cs`.
2. Confirm import success:
   - `ui_bg_1.jpg.meta` exists,
   - `Temp/ShenxiaoRunMainUIEntryModules.request` is deleted,
   - Editor log contains `[LayaUI] Auto-running queued MainUI entry module rebuild`.
3. Then rerun MainUI runtime click patrol: bottom `role`/`bag`, top `setting`/`map`, `chat`/`shop`, customer service placeholder, task/team, auto, and partner lock.
