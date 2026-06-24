# Heartbeat 16:29 - Unity Runtime UI Capture Tool

## Scope

- Automation: shenxiao-ui
- Focus: MainUI-first patrol support, runtime screenshot/node evidence for Unity side
- Baseline rule: old Laya runtime at 720x1280 portrait remains the visual source of truth
- Prefab rule: no generated prefab or scene object was hand-edited in this round

## Covered Entrances

- MainUI verification infrastructure:
  - Added Unity Editor menu `神霄/调试/UI运行态/截图+节点Dump`
  - Added batch-callable method `Shenxiao.Editor.RuntimeCapture.RuntimeUiCaptureTool.CaptureNow`
- This round did not claim a page as finished. It removes a repeated validation blocker before the next MainUI click-pass.

## Differences Found

- Unity runtime validation depended on transient MCP access. When MCP returned `Transport closed`, there was no durable local way to capture:
  - current Game View screenshot
  - active/inactive Canvas node tree
  - RectTransform geometry
  - Graphic/Button hit-test state
- This made MainUI clickability and transparent-window regressions harder to prove repeatedly.

## Common Root Cause

- The patrol workflow lacked a Unity-side runtime evidence harness independent of MCP transport.
- This is a workflow/tooling gap, not a business module completion.

## Code/Generation Work

- Added `Assets/Editor/RuntimeCapture/RuntimeUiCaptureTool.cs`
- Added Unity `.meta` files for the new editor folder and script
- No runtime business code, generated prefab, Addressables group, or asmdef was changed for this tool.

## Tool Behavior

- Output directory: `output/runtime_unity`
- Files per capture:
  - `<timestamp>/screenshot.png` when Unity is in Play Mode
  - `<timestamp>/ui_dump.txt`
  - `<timestamp>/status.txt`
- Node dump records:
  - Canvas path, active state, render mode, sorting order, scale factor
  - UI node path, active state, RectTransform anchored position, size, anchors, pivot
  - Graphic type, raycastTarget, color
  - Button interactable
  - TextMeshProUGUI text, sanitized to one line

## Verification

- `git diff --check` passed
- `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed
  - 1 existing warning: `Assets\Scripts\Module\Core\Scene\MainRoleAgent.cs(206,17) CS0162`
  - 0 errors
- Unity MCP was retried after cleaning stale relay:
  - Stale process removed: `C:\Users\tr\.unity\relay\relay_win.exe`
  - Remaining relay: project PackageCache Unity Editor relay
  - `Unity_RunCommand` still failed with `Transport closed`
- No fresh Unity runtime screenshot was produced in this round because MCP remained unavailable and the tool has not yet been executed inside the Editor.

## Claude/MCP Availability

- Claude Code CLI command attempted:
  - `claude -p --permission-mode acceptEdits --allowedTools Read,Edit,MultiEdit,Write,Grep --output-format text`
  - Prompt: add persistent Unity runtime UI capture tool
  - Result: timed out after 184 seconds, no final output, no file created
- Codex implemented the editor-only tool after confirming Claude produced no artifact.
- Unity MCP:
  - stale relay cleanup performed
  - `Unity_RunCommand` still returned `Transport closed`

## Next Priority

1. Execute `Shenxiao.Editor.RuntimeCapture.RuntimeUiCaptureTool.CaptureNow` from Unity Play Mode or batchmode once Editor access is stable.
2. Use the node dump and screenshot to validate MainUI clickable entries:
   - HUD
   - bottom role/bag
   - setting
   - chat
   - shop
   - map
   - activity icons
   - task/team
   - auto brush
   - customer service
   - partner lock
3. Fix the highest-impact MainUI runtime defects through shared routes:
   - converter/default skin/resource mapping for missing static visuals
   - MainUIRouter placeholder for unported entries
   - business View/Flow only for runtime behavior
4. Re-check Bag transparent background as a shared window/skin/conversion issue, not by hand-editing the Bag prefab.
