# MainUI Patrol 23:46

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_2346/old_runtime_current_720x1280_recapture.png`.
- Runtime node reference: `Temp/oldclient_mainui_gm_auth_lv100_task101410_dun20.json`.
- Unity target: current project `D:\git_res\yu_client_unity`.
- Priority: MainUI visible entries must be clickable; migrated modules should open real pages, unmigrated or failed modules should open the unified placeholder.

## Covered Entries

- Real prefab currently exists for: `role`, `bag`, `chat`, `setting`, `map`.
- Registered MainUI routes found: `232`, `autobrush_toggle`, `bag`, `brightsea`, `buff`, `chat`, `composite`, `dailyfind`, `email`, `equip`, `fightmode`, `firstblood`, `friend`, `guild`, `guildhelp`, `halo`, `levelreward`, `love`, `map`, `pet`, `recharge`, `red`, `redpacket`, `role`, `setting`, `shop`, `treasure`, `vip`.
- Unregistered MainUI view routes observed in code, therefore expected to hit unified placeholder: `activity_rank`, `customerservice`, `marriage_gift_tips`, `onhook`, `onhook_addition`, `partnerawake`, `pushgift`, `redpacket_rain`, `team_create`, `team_invite`, `team_search`, `templeawaken`, `tt_record`.

## Differences Found

- Old runtime MainUI node evidence has these visible views loaded: `main_ui_top_view`, `main_ui_chat_view`, `main_ui_secondary_view`, `main_ui_task_team_view`, `main_ui_down_view`, `main_ui_auto_brush_view`.
- Unity generated prefabs still missing for entry modules:
  - `Shop`: no prefab directory, no `ShopModule.prefab`
  - `Vip`: no prefab directory, no `VipModule.prefab`
  - `Pet`: no prefab directory, no `PetModule.prefab`
  - `RedPacket`: no prefab directory, no `RedPacketModule.prefab`
  - `Rune`: no prefab directory, no `RuneModule.prefab`
  - `Marriage`: no prefab directory, no `MarriageModule.prefab`
  - `GodBefall`: directory has 3 prefabs, but no `GodBefallModule.prefab`
- `Role`, `Bag`, `Chat`, `Setting`, `Map` have module prefabs and remain the first true-page verification targets.

## Common Root Cause

- MainUI clickability is mostly handled by `MainUIRouter` and `MainUIRoutePlaceholder`.
- The key blocker remains the shared LayaUI generation path for missing entry module prefabs; this should be fixed through conversion/regeneration, not manual prefab editing.
- The existing open Unity Editor has not consumed `Temp/ShenxiaoRunMainUIEntryModules.request`.
- Unity MCP still fails with `Transport closed`, so menu execution through MCP is unavailable.

## Generation / Code Tasks

- Kept the pending marker: `Temp/ShenxiaoRunMainUIEntryModules.request`.
- Patched `Assets/Scripts/Module/Core/Shop/ShopFlow.cs` so the shop route uses `MainUIRouteFallback.InstantiateOrShowAsync` for frame/content loading. This makes the shop button open the unified placeholder when `ShopModule.prefab` is missing.
- No generated prefab was hand-edited.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 errors, 1 pre-existing warning `MainRoleAgent.cs(206) CS0162`.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 errors, same pre-existing warning.
- `git diff --check`: passed.
- Claude Code command:
  - `claude -p "在 D:\git_res\yu_client_unity 中请只读分析，不修改文件。目标：ShopFlow 在缺少 ShopModule.prefab 时应和 Vip/Pet/RedPacket/Rune/Marriage 一样使用 MainUIRouteFallback 显示统一占位。请指出最小修改点和风险，30秒内输出。"`
  - result: timed out after 30 seconds with no output; residual `claude.exe` and `relay_win.exe` were stopped.
- Unity MCP command:
  - `Unity_RunCommand` health check
  - result: `Transport closed`
- Current Unity Editor remains open: PID 12360, project `yu_client_unity - Launch - Web`.

## Next Priority

1. Trigger Unity Editor refresh/reload so the marker runner executes the shared LayaUI conversion for `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`, `shop`, `common`.
2. Verify generated prefabs and Addressables after conversion.
3. Runtime-click verify the real modules first: `role`, `bag`, `chat`, `setting`, `map`, then `shop` placeholder behavior, then regenerated `vip/pet/redpacket/treasure/love/232`.
