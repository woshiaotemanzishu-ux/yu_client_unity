# MainUI Patrol 02:03

## Scope

- Continue from MainUI first. This round did not expand to new pages because the active blocker is Unity import / MCP execution, not a single page mismatch.
- Old Laya runtime 720x1280 evidence was retained:
  - `output/heartbeat_0203/old_runtime_current_720x1280_recapture.png`
  - `output/heartbeat_0203/old_runtime_current_pageinfo_recapture.json`

## Covered Entries

- Static MainUI route coverage remains the same as the previous round:
  - Bottom: `role`, `bag`, `pet`, `equip`, `treasure`, `red`, `love`, `guild`, `composite`, `232`.
  - Top/chat: `map`, `setting`, `buff`, `fightmode`, `vip`, `recharge`, `halo`, `customerservice`, `chat`, `friend`, `shop`.
  - Secondary/task/skill: `email`, `redpacket`, `levelreward`, `firstblood`, `dailyfind`, `guildhelp`, `brightsea`, `team_invite`, `pushgift`, `onhook`, `onhook_addition`, `marriage_gift_tips`, `redpacket_rain`, `tt_record`, `team_create`, `team_search`, `templeawaken`, `partnerawake`, `autobrush_toggle`.
- Registered module routes include real flows for `bag`, `role`, `chat`, `setting`, `shop`, `map`, `vip`, `recharge`, `pet`, `equip`, `friend`, `guild`, `guildhelp`, `levelreward`, `firstblood`, `dailyfind`, `brightsea`, `redpacket`, `love`, `treasure`, `232`, `halo`, `buff`, `fightmode`, and `autobrush_toggle`.

## Differences Found

- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg.meta` is still missing.
- `Temp/ShenxiaoRunMainUIEntryModules.request` still exists and was not consumed by Unity.
- Editor log advanced, but latest activity is Unity Connect / licensing noise. No latest evidence of `Auto-running queued MainUI entry module rebuild`, marker consumption, or `ui_bg_1` import.
- `Unity_RunCommand` still fails immediately with `Transport closed`.

## Common Root Cause

- The remaining blocker is the Unity Editor command/import path. The project has the copied resource and queued rebuild marker, but the open Editor is not importing/refreshing that project state into Unity assets.
- This is a shared tooling/import problem. It should be resolved through MCP/Unity Editor command execution or the LayaUI pipeline, not by hand-editing generated prefabs.

## Claude / MCP Findings

- Claude Code default mode previously timed out because it attempted to load the normal environment/MCP chain.
- Claude Code safe-mode succeeded with a read-only review command:
  - `claude --safe-mode -p "...inspect MainUIRouter.cs and MainUIRoutePlaceholder.cs..." --output-format text --permission-mode dontAsk --tools Read,Grep`
- Claude finding:
  - No recursion risk: `MainUIRoutePlaceholder.Show` does not call `MainUIRouter.Open`.
  - No duplicate placeholder object creation: placeholder is a static singleton.
  - Narrow risk: if a registered opener opens a real window and then throws synchronously, router catch may show placeholder on top. Current flow openers are mostly async fire-and-forget, and module-level fallbacks already live in each Flow, so no interface refactor was made this round.
- MCP local state:
  - `Library/AI.MCP/connections-v2.asset` contains approved Codex/Claude connections.
  - Recent MCP logs show Claude could connect to `\\.\pipe\unity-mcp-502ec44d-12360` and discover tools, but Codex `Unity_RunCommand` still returns `Transport closed`.
  - No residual `claude.exe` or `relay_win.exe` process remained after this round.

## Code / Generation Tasks

- No new code was changed in this round.
- Existing active code changes remain:
  - `LayaUIPipeline.cs`: MainUI entry rebuild menu, no-confirm run, marker polling.
  - `MainUIRouter.cs`: registered opener synchronous-exception guard with placeholder fallback.
  - `MainUIRoutePlaceholder.cs`: unified Chinese placeholder panel and route display.
- No generated prefab was hand-edited.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- `Unity_RunCommand` health check title `Shenxiao MCP Health 0203`: failed with `Transport closed`.
- State check:
  - `ui_bg_1.jpg.meta`: missing.
  - `Temp/ShenxiaoRunMainUIEntryModules.request`: still present.
  - `claude.exe` / `relay_win.exe`: no residual process.

## Next Priority

1. Restore one reliable Unity execution path: fix Codex MCP transport, use Claude MCP only if it returns successfully, or run the Unity Editor menu interactively with explicit approval.
2. After import runs, verify:
   - marker deleted,
   - `ui_bg_1.jpg.meta` exists,
   - MainUI entry modules regenerated,
   - Addressables grouping updated.
3. Resume runtime click patrol from MainUI: `bag`, `role`, `setting`, `chat`, `shop`, `map`, `customerservice`, task/team, auto, partner lock.
