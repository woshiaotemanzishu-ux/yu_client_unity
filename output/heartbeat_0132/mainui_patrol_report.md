# MainUI Patrol 01:32

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0132/old_runtime_current_720x1280_recapture.png`.
- Runtime node evidence: `output/heartbeat_0132/old_runtime_current_pageinfo_recapture.json` confirms a visible 720x1280 canvas.
- Focus: MainUI entry clickability and route fallback while Unity refresh/import remains blocked.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab MainUI modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked converted/registered entry routes from MainUI:
  - top HUD: `map`, `setting`, `buff`, `fightmode`, `vip`, `recharge`, `halo`, `customerservice`
  - chat strip: `chat`, `setting`, `friend`, `shop`
  - secondary HUD: `email`, `chat`, `redpacket`, `levelreward`, `firstblood`, `dailyfind`, `guildhelp`, `brightsea`, `team_invite`, `pushgift`, `onhook`, `onhook_addition`, `marriage_gift_tips`, `232`, `redpacket_rain`, `tt_record`
  - bottom icons: `role`, `bag`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`
  - task/team/skill: `team_create`, `team_search`, `templeawaken`, `partnerawake`, `autobrush_toggle`
- Rechecked missing-entry modules that still require conversion or placeholder behavior: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.

## Differences Found

- Unity Editor has still not imported recent filesystem changes:
  - `Editor.log` last write time remains `2026-06-24 09:01:33`.
  - `ui_bg_1.jpg.meta` is still missing.
  - `Temp/ShenxiaoRunMainUIEntryModules.request` still exists.
- Static route coverage shows most visible MainUI buttons now call `MainUIRouter`, but registered route openers previously had no synchronous exception guard. A thrown opener could prevent the user from seeing the unified empty panel.

## Common Root Cause

- Static UI generation remains blocked by Unity import/refresh not running.
- MainUI runtime usability should be protected centrally in the router: unregistered route, missing prefab, or failed opener should all degrade to the same placeholder instead of breaking the click.
- This is runtime behavior/failure recovery, not generated prefab editing.

## Generation / Code Tasks

- Patched `Assets/Scripts/Module/Core/MainUI/MainUIRouter.cs`.
- `MainUIRouter.Open(viewKey)` now wraps registered openers in `try/catch`.
- If a registered opener throws synchronously, the router logs the failure and shows `MainUIRoutePlaceholder.Show(viewKey)`.
- No generated prefab was hand-edited in this round.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 errors; first run showed the existing `MainRoleAgent.cs(206) CS0162` warning.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors after rerun. A parallel attempt hit the known temporary Huorong output-file lock.
- `git diff --check`: passed.
- Old runtime evidence:
  - `output/heartbeat_0132/old_runtime_current_720x1280_recapture.png`
  - `output/heartbeat_0132/old_runtime_current_pageinfo_recapture.json`
- Claude Code command:
  - `claude -p "Read-only review only, do not modify files. In D:\git_res\yu_client_unity, review MainUIRouter.Open try/catch fallback to MainUIRoutePlaceholder.Show(viewKey), plus prior MainUIRoutePlaceholder Chinese empty panel and LayaUIPipeline marker polling. Check for runtime/duplicate-execution risks. Reply concise."`
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

1. Get one successful Unity refresh/import action. This remains the main blocker for generated prefabs and `ui_bg_1.jpg.meta`.
2. After Unity imports scripts, confirm the LayaUI marker poller is active, marker is consumed, and MainUI entry modules regenerate.
3. Runtime-click verify unmigrated routes show the Chinese unified empty panel.
4. Verify Bag after import: background visible, content appears when valid, placeholder on failed load, and `_loading` does not stick.
5. Continue real-page checks: `role`, `chat`, `setting`, `map`, then `shop`, then `vip/pet/redPacket/rune/marriage/godBefall`.
