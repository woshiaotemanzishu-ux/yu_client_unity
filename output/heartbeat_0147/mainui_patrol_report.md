# MainUI Patrol 01:47

## Scope

- Baseline remains the old Laya runtime page at `http://127.0.0.1:8090/index.html`, captured as a 720x1280 portrait runtime screen.
- Runtime baseline evidence was copied into this round:
  - `output/heartbeat_0147/old_runtime_current_720x1280_recapture.png`
  - `output/heartbeat_0147/old_runtime_current_pageinfo_recapture.json`
- This round focused on MainUI route clickability and Unity import state, not manual prefab polishing.

## Covered Entries

- Bottom main function keys from `MainUIModel.MainFuncIcons`: `role`, `bag`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`.
- Top/chat entries routed through MainUI views: `map`, `setting`, `buff`, `fightmode`, `vip`, `recharge`, `halo`, `customerservice`, `chat`, `friend`, `shop`.
- Secondary/task/team/skill entries found in code: `email`, `redpacket`, `levelreward`, `firstblood`, `dailyfind`, `guildhelp`, `brightsea`, `team_invite`, `pushgift`, `onhook`, `onhook_addition`, `marriage_gift_tips`, `redpacket_rain`, `tt_record`, `team_create`, `team_search`, `templeawaken`, `partnerawake`, `autobrush_toggle`.

## Differences Found

- Unity Editor log advanced to `2026/6/24 09:46:31`, but the new background asset still has no `.meta`: `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg.meta` is missing.
- The queued import marker is still present: `Temp/ShenxiaoRunMainUIEntryModules.request`.
- Editor log search did not show `[LayaUI] Auto-running queued MainUI entry module rebuild`, `ui_bg_1`, or marker consumption in the latest activity window.
- MCP command execution still fails with `Transport closed`, so this round could not drive the Unity Editor menu import through MCP.

## Common Root Cause

- The current blocker is not a prefab-level visual tweak. Unity is not executing the queued LayaUI/MainUI import path, so generated UI assets and copied resources are not being imported.
- Runtime clickability needed a common fallback guard: registered MainUI openers that throw synchronously should not leave the user with a dead click.

## Generation / Code Tasks

- Added central opener failure protection in `Assets/Scripts/Module/Core/MainUI/MainUIRouter.cs`: registered route openers now fall back to `MainUIRoutePlaceholder.Show(viewKey)` if the opener throws synchronously.
- Previous active pipeline changes remain in `Assets/Editor/LayaUI/LayaUIPipeline.cs`: `RunMainUIEntryModules`, no-confirm rebuild, and queued marker polling.
- Previous active placeholder changes remain in `Assets/Scripts/Module/Core/MainUI/MainUIRoutePlaceholder.cs`: Chinese unified empty panel and route-key display.
- No generated prefab was hand-edited as the final solution in this round.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed; existing warning observed earlier in `MainRoleAgent.cs(206) CS0162`.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed on sequential rerun with 0 warnings and 0 errors.
- `git diff --check`: passed.
- `Unity_RunCommand` health check title `Shenxiao MCP Health 0147`: failed with `Transport closed`.
- Claude Code attempt:
  - Command: `claude -p "Read-only review only, do not modify files. In D:\git_res\yu_client_unity, review MainUIRouter.Open try/catch fallback to MainUIRoutePlaceholder.Show(viewKey), plus prior MainUIRoutePlaceholder Chinese empty panel and LayaUIPipeline marker polling. Check for runtime/duplicate-execution risks. Reply concise."`
  - Result: timed out after 20 seconds with no useful output.
  - Residual `claude.exe` and `relay_win.exe` processes were cleaned.

## Next Priority

1. Restore a working Unity Editor command path: either fix MCP transport or use approved interactive Unity control to run the LayaUI menu import.
2. Confirm import success by checking marker deletion, `ui_bg_1.jpg.meta` creation, and MainUI entry prefab generation.
3. Re-run Unity runtime MainUI click patrol: bottom `role`/`bag`, top `setting`/`map`, chat/shop/customer-service, task/team, auto, and partner lock.
4. After MainUI clickability is usable, return to real page parity for `bag`, `role`, `chat`, and `setting`.
