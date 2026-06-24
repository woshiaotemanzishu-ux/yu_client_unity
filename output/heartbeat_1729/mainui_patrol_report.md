# Shenxiao UI Heartbeat 17:29 MainUI Patrol

## Scope

- Baseline: old Laya runtime, 720x1280 portrait, `http://127.0.0.1:8090/index.html`.
- Account used: cached old-client account `21055`, password already remembered; user-provided `zxczxc` remains available for later clean-role tests.
- Focus: MainUI first, prove clickability/runtime behavior before expanding to other pages.

## Old Runtime Evidence

- `old_laya_after_select_role_enter_720x1280.png`: select-role enter button click enters MainUI.
- `old_laya_relogin_after_select_role_enter_720x1280.png`: relogin reaches MainUI, but first-recharge/newbie popup stack appears immediately.
- `old_laya_weapon_popup_close_probe_points_720x1280.png`: red-weapon floating popup cleared only after probing its actual close hit area near `(270..300, 150..170)`.
- `old_laya_after_unlock_confirm_720x1280.png`: runtime unlock/tutorial overlay appears after MainUI clicks; this blocks reliable route testing until confirmed/cleared.
- `old_laya_click_role_bottom_720x1280.png`: bottom role button opens the real old-client `人物` page.
- `old_laya_click_customer_service_720x1280.png`: customer-service click produced no clear modal/page before runtime upgrade提示 appeared; mark as hit-but-not-confirmed.
- `old_laya_click_map_720x1280.png`: intended map click was intercepted by runtime unlock overlay; not counted as map coverage.

## Source-Confirmed MainUI Entrances

- `D:\git_res\yu_client\h5\src\mainUI\MainUIChatView.ts`
  - `_box_setting` opens `SettingView`.
  - `_box_friend` opens friend view.
  - `_box_shop` opens shop main view.
  - `_panel_chat` and `_panel_sys` open chat view.
- `D:\git_res\yu_client\h5\laya\pages\resource\game\mainUI\MainUIChatView.scene`
  - `_box_setting`: `x=4 y=25 w=64 h=64`.
  - `_box_friend`: `x=72 y=22 w=64 h=64`.
  - `_box_shop`: `x=550 y=33 w=64 h=64`.
  - `_panel_chat`: `x=148 y=3 w=396 h=65`.
  - `_panel_sys`: `x=148 y=83 w=397 h=65`.
- `D:\git_res\yu_client\h5\src\mainUI\MainUIModel.ts`
  - Bottom function row starts with `Role`, `Bag`, `Pet`, `Equip`, `Treasure`.
  - `Role` and `Bag` are always open by `GetMainFuncOpenCond`.
  - `Role` routes to `OPEN_CHOSE_ROLE_VIEW`; `Bag` routes through `OpenFunHandler(105)`.
- `D:\git_res\yu_client\h5\laya\pages\resource\game\login\LoginSelectRoleView.scene`
  - `_img_enter` is `width=378 height=140 centerX=0 bottom=120`; in 720x1280 this maps to about `x=171..549 y=1020..1160`.

## Differences / Risks Found

- Old client MainUI is not a stable static screen after login. It has first-recharge floating tips, tutorial dialogue, unlock/reward overlays, and route-guiding effects that take click priority.
- Earlier blind clicks on map/customer-service/close buttons can be false negatives because runtime overlays steal input.
- The visible red-weapon popup close hit area did not match the first visual estimate; source/runtime hitbox evidence is required before judging an entry broken.
- MainUI screenshot at 720x1280 currently shows the old runtime's duplicated lower viewport band; keep this as the baseline unless a cleaner old runtime mode is confirmed.

## Common Root Cause Direction

- This round did not identify a Unity prefab-specific defect. The main actionable finding is patrol-methodology: MainUI entry testing must first normalize old runtime state by clearing popup/tutorial stacks.
- For Unity fixes, keep the existing rule: static UI/background/window/skin/list/template/Bind/Addressables defects go through the LayaUI converter/editor menu pipeline, not direct prefab edits.
- Runtime behavior such as MainUI button routing, placeholder fallback, dynamic page opening, and unlock/tutorial state belongs in View/Flow/router code.

## Claude / MCP / Commands

- Claude Code CLI: available. Command used: `claude -p "...只读协作分析..."`; it returned a focused MainUI patrol closure and pipeline order.
- Unity MCP: blocked. `Unity_RunCommand` returned `Transport closed`.
- Relay cleanup check: `Get-Process relay_win -ErrorAction SilentlyContinue` found no stale `relay_win.exe`, so no process was killed.
- `git diff --check`: passed with no output.
- `dotnet build .\yu_client_unity.slnx -v:minimal --nologo`: passed, `0` warnings and `0` errors.

## Next Priority

1. Use an old role with tutorial/first-recharge stack already cleared, or automate a deterministic popup-clear step before every MainUI click.
2. Re-test MainUI entries in this order: role, bag, settings, chat, shop, map, customer service.
3. Compare the same entries against Unity runtime; registered modules must open real pages, missing modules must open the unified placeholder panel.
4. If Unity static UI differs, fix through converter/default skins/Bind/Addressables and regenerate via Unity Editor menu.
