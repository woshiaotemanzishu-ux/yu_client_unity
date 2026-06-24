# MainUI AutoBrush Patrol 2026-06-23 16:14

## Scope

- Continue from MainUI.
- Keep the old Laya runtime/source as the baseline.
- Do not hand-edit prefabs or generated Bind files.

## Old Client Evidence

- `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:173-175`
  - `click_gp` hides guide finger and opens `AutoBrushBaseView`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:211-215`
  - `_box_auto_level` sends `13307, "c", 1` when auto state is on.
  - `_box_auto_level` sends `13307, "c", 0` when auto state is off.

## Claude Code

- Command attempted:
  `claude -p --permission-mode acceptEdits --allowedTools Read,Edit,MultiEdit,Grep --output-format text`
- Result:
  timed out after 184 seconds with no final response.
- Follow-up:
  Codex completed the bounded patch.

## Code Changes

- `Assets/Scripts/Module/Core/AutoBrush/AutoBrushBootstrap.cs`
  - Registers only `autobrush_toggle`.
  - Leaves `autobrush` unregistered so the main page entry falls back to the shared MainUI placeholder until `AutoBrushBaseView` is ported.
- `Assets/Scripts/Module/Core/AutoBrush/AutoBrushController.cs`
  - Keeps `RequestToggle()` as the old-client `13307 "c"` toggle path.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUIAutoBrushView.cs`
  - `click_gp` routes to `autobrush`.
  - `_box_auto_level` and `_img_auto_level` route to `autobrush_toggle`.

## Verification

- `Select-String` confirmed no `Register("autobrush")` remains.
- `git diff --check`: passed.
- `dotnet build .\Shenxiao.Module.Core.csproj --nologo`: succeeded, 0 errors.
- Unity MCP: stale `C:\Users\tr\.unity\relay\relay_win.exe` was cleaned; only Unity PackageCache relay remains. `Unity_RunCommand` still fails with `Transport closed`.

## Remaining

- Port the real `AutoBrushBaseView` page or keep placeholder until the page is ready.
- Runtime click validation still needs Unity MCP recovery or a persistent screenshot/node-dump harness.
