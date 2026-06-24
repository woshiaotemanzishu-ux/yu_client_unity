# MainUI Patrol 00:16

## Scope

- Baseline: old Laya runtime portrait evidence copied to `output/heartbeat_0016/old_runtime_current_720x1280_recapture.png`.
- Focus: keep MainUI modules usable first, with real pages where possible and unified placeholders on failure.
- Unity target: current project `D:\git_res\yu_client_unity`.

## Covered Entries

- Rechecked real-prefab modules: `role`, `bag`, `chat`, `setting`, `map`.
- Rechecked missing-entry modules: `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
- Rechecked shared background import state for `resource/game/bigBg/ui_bg_1.jpg`.

## Differences Found

- `Temp/ShenxiaoRunMainUIEntryModules.request` still exists; Unity has not consumed the pending conversion marker.
- `Assets/GameRes/resource/game/bigBg/ui_bg_1.jpg` exists, but `.meta` is still missing, so Unity has not imported it yet.
- Entry module prefab status is unchanged:
  - real module prefabs exist for `role`, `bag`, `chat`, `setting`, `map`
  - missing module prefabs remain `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`
  - `godBefall` still lacks `GodBefallModule.prefab`

## Common Root Cause

- The main static generation blocker is still Unity Editor not refreshing/reloading, which prevents marker execution, asset import, `.meta` generation, and Addressables grouping.
- Bag usability risk was separate: `BagFlow` directly loaded the shared frame and content prefabs, so a missing frame/content could fail without opening the unified placeholder.

## Generation / Code Tasks

- Patched `Assets/Scripts/Module/Core/Bag/BagFlow.cs`.
- `BagFlow` now uses `MainUIRouteFallback.InstantiateOrShowAsync` for `BaseWindowSkin` and required bag content prefab loading.
- If the frame, primary content, or `BaseWindowSkinView` is missing, `BagFlow` now resets and opens `MainUIRoutePlaceholder.Show("bag")`.
- No generated prefab was hand-edited.

## Verification

- `dotnet build Shenxiao.Module.Core.csproj -v:minimal`: passed, 0 errors. First parallel run showed the existing `MainRoleAgent.cs(206) CS0162` warning, but the later sequential Editor build completed with 0 warnings.
- `dotnet build Shenxiao.Editor.csproj -v:minimal`: first parallel run failed because the shared build output was locked by another process / Huorong scan; rerun sequentially passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- Unity MCP health check:
  - `Unity_RunCommand`
  - result: `Transport closed`
- Claude Code command:
  - `claude -p "在 D:\git_res\yu_client_unity 中请只读分析，不修改文件。目标：BagFlow 在 BaseWindowSkin 或 BagModule 加载失败时，应和 ShopFlow 一样走 MainUIRouteFallback/统一占位，并确保 _loading finally 复位。请给最小修改点和风险，20秒内输出。"`
  - result: timed out after 20 seconds with no output; residual `claude.exe` and `relay_win.exe` were stopped.
- Current process status:
  - Unity Editor remains open on project PID 12360.
  - No `claude.exe` or `relay_win.exe` process remains.

## Next Priority

1. Trigger Unity Editor refresh/reload so `ui_bg_1.jpg` imports and the pending MainUI conversion marker executes.
2. Verify `BagFlow` fallback by intentionally checking bag open behavior after import/reload.
3. Runtime-click verify `role`, `bag`, `chat`, `setting`, `map`, then `shop` placeholder.
4. After conversion succeeds, verify `vip`, `pet`, `redPacket`, `treasure`, `love`, `232`.
