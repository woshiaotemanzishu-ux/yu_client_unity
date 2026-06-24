# MainUI Hotzone Patrol 2026-06-23 15:59

## Scope

- Continue from MainUI.
- Keep old Laya runtime as the baseline.
- Do not hand-edit generated prefabs.
- Make visible MainUI entries respond through runtime routing or placeholder.

## Claude Code

- Command attempted:
  `claude -p --permission-mode acceptEdits --allowedTools Read,Edit,MultiEdit,Grep --output-format text`
- Result:
  timed out after 304 seconds with no final response.
- Follow-up:
  Codex completed the bounded patch and verified it locally.

## Code Changes

- `Assets/Scripts/Framework/UI/UIUtil.cs`
  - Added `AddClick(Component, Action)` and `AddClick(GameObject, Action)`.
  - Box/RectTransform targets without `Graphic` now get a transparent `Image` hotzone.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUIChatView.cs`
  - Chat, system, setting, friend, shop use Box/RectTransform hotzones.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUITopView.cs`
  - Top HUD entries use whole node hotzones, including customer service.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUISecondaryView.cs`
  - More secondary HUD entries route to real modules or placeholder.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUIAutoBrushView.cs`
  - `click_gp`, `_box_auto_level`, and `_img_auto_level` route through the same click utility.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUITaskTeamView.cs`
  - Task/team tabs and temple awaken use Box hotzones.
- `Assets/Scripts/Module/Core/MainUI/Views/MainUISkillView.cs`
  - Partner skill container routes to placeholder.

## Verification

- `git diff --check`: passed.
- `dotnet build .\Shenxiao.Module.Core.csproj --nologo`: succeeded, 0 errors.
- Existing warnings are unrelated Framework/Generated warnings.
- Unity MCP: stale `C:\Users\tr\.unity\relay\relay_win.exe` processes were cleaned; `Unity_RunCommand` still failed with `Transport closed`.

## Remaining

- Unity runtime click validation is blocked by MCP.
- AutoBrush still needs the old-client split between opening the auto-brush page and toggling auto state.
- MainUI visual parity still needs runtime screenshots after MCP or a persistent local screenshot harness is available.
