# MainUI Patrol 00:01

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0001/old_runtime_current_720x1280_recapture.png`.
- Focus: MainUI usable modules first, especially shared window/background behavior for already connected modules such as bag.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked missing-entry modules: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
- Inspected shared window frame path used by bag and other tabbed modules: `BaseWindowSkinView.ApplyBackground`.

## Differences Found

- `BaseWindowSkinView.ApplyBackground(null)` and bag tab 0/1 both depend on `GameResPath.GetBigBgPath("ui_bg_1.jpg")`.
- Old client has the file at `D:\git_res\yu_client\cdn\assets\resource\game\bigBg\ui_bg_1.jpg`.
- Unity project was missing `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg`.
- This explains the visible transparent/missing background symptom on shared tab windows such as bag when the runtime background swap runs.
- Entry module prefab status is unchanged:
  - real module prefabs exist for `role`, `bag`, `chat`, `setting`, `map`
  - missing module prefabs remain `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`
  - `godBefall` still lacks `GodBefallModule.prefab`

## Common Root Cause

- The transparent window symptom is a shared runtime image/resource dependency issue, not a per-prefab hand edit.
- The broader missing entry modules remain blocked on the shared LayaUI conversion/regeneration path.
- Unity Editor still has not consumed `Temp/ShenxiaoRunMainUIEntryModules.request`.

## Generation / Code Tasks

- Copied old-client default shared window background into Unity resources:
  - source: `D:\git_res\yu_client\cdn\assets\resource\game\bigBg\ui_bg_1.jpg`
  - destination: `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg`
- No generated prefab was hand-edited.
- No new code edit in this pass beyond existing pending changes from prior passes.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg`: exists, size 13,520 bytes.
- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg.meta`: not generated yet because Unity has not refreshed/imported.
- Unity MCP health check:
  - `Unity_RunCommand`
  - result: `Transport closed`
- Claude Code:
  - not invoked for this asset copy; no code-generation request was needed.
- Current process status:
  - Unity Editor remains open on project PID 12360.
  - No `claude.exe` or `relay_win.exe` process remains.

## Next Priority

1. Trigger Unity Editor refresh/reload so `ui_bg_1.jpg` imports, generates `.meta`, and can be Addressables-grouped.
2. Trigger the pending MainUI entry-module conversion marker.
3. Runtime verify bag first: open from MainUI, confirm the shared window background is no longer transparent, then verify role/chat/setting/map.
4. After conversion succeeds, verify `shop/vip/pet/redPacket/rune/marriage/godBefall` as real pages or placeholder fallback.
