# MainUI Patrol 00:31

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0031/old_runtime_current_720x1280_recapture.png`.
- Runtime node evidence: `output/heartbeat_0031/old_runtime_current_pageinfo_recapture.json` confirms a visible 720x1280 canvas.
- Focus: MainUI usability first, with Bag open failure protection tightened this round.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab MainUI modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked missing-entry modules that must use placeholder or be regenerated: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
- Rechecked shared bag/window background resource state for `resource/game/bigBg/ui_bg_1.jpg`.
- Reviewed `BagFlow.OpenAsync` loading path for frame/content failures and `_loading` recovery.

## Differences Found

- `Temp/ShenxiaoRunMainUIEntryModules.request` still exists; the open Unity Editor has not consumed the pending conversion marker.
- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg` exists, but `ui_bg_1.jpg.meta` is still missing, so Unity has not imported the copied old-client background asset yet.
- MainUI entry module prefab status is unchanged:
  - real module prefabs exist for `role`, `bag`, `chat`, `setting`, `map`
  - missing module prefabs remain `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`
  - `godBefall` still lacks `GodBefallModule.prefab`
- Before this round, `BagFlow` had fallback-based prefab loads but could leave `_loading=true` if an exception escaped the loading block.

## Common Root Cause

- Static UI gaps remain blocked by Unity Editor not refreshing/reloading: the one-shot conversion marker has not executed, the copied shared background has not imported, and generated prefab/Addressables updates cannot be verified.
- Bag entry stability is a runtime flow issue: it should never leave the MainUI button unusable when frame/content loading fails.
- No generated prefab should be hand-edited as the final fix; static backgrounds, frame, skins, Bind, templates, and Addressables still belong to the LayaUI conversion/import chain.

## Generation / Code Tasks

- Patched `Assets/Scripts/Module/Core/Bag/BagFlow.cs`.
- Wrapped the frame/content async loading block in `try/catch/finally`.
- On load exception, `BagFlow` now logs the frame key, resets local window/content state, and opens `MainUIRoutePlaceholder.Show("bag")`.
- `_loading` is now reset from `finally`, so the Bag button should not stay stuck after failed loading.
- No generated prefab was hand-edited in this round.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 errors; existing warning remains `Assets\Scripts\Module\Core\Scene\MainRoleAgent.cs(206,17) CS0162`.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- Old runtime evidence:
  - `output/heartbeat_0031/old_runtime_current_720x1280_recapture.png`
  - `output/heartbeat_0031/old_runtime_current_pageinfo_recapture.json`
- Claude Code command:
  - `claude -p "在 D:\git_res\yu_client_unity 中请只读分析，不修改文件。目标：BagFlow.OpenAsync 现在已使用 MainUIRouteFallback，但 _loading 不是 finally 复位；请给最小修改方案，保证异常时 ShowPlaceholderAndReset 且 _loading=false。只输出要点，20秒内。"`
  - result: timed out after 20 seconds with no output.
- Unity MCP health check:
  - `Unity_RunCommand`
  - result: `Transport closed`.
- Current process state:
  - Unity Editor remains open on the project.
  - No residual `claude.exe` or `relay_win.exe` process was found after cleanup checks.

## Next Priority

1. Trigger Unity Editor refresh/reload so `ui_bg_1.jpg` imports and `Temp/ShenxiaoRunMainUIEntryModules.request` runs.
2. Verify Bag from MainUI in Unity runtime: background present, content appears when prefabs are valid, unified placeholder appears when loading fails, and the button is not stuck.
3. Verify real pages from MainUI: `role`, `chat`, `setting`, `map`.
4. Verify `shop` placeholder/runtime behavior.
5. After conversion executes, verify `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`, then expand to `treasure`, `love`, `232`.
