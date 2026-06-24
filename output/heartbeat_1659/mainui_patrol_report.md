# Shenxiao UI Heartbeat 16:59

## Scope

- Baseline: old Laya runtime at `http://127.0.0.1:8090/index.html`, forced 720x1280 portrait viewport.
- Account path: random register account `zxc34010097 / zxczxc`; old client returned generated account `21055 / 25012`.
- Current focus: MainUI first-screen usability and route coverage, not static `.scene` parity.

## Old Runtime Evidence

- `old_laya_register_form_720x1280.png`
- `old_laya_after_register_720x1280.png`
- `old_laya_after_confirm_register_720x1280.png`
- `old_laya_after_agree_720x1280.png`
- `old_laya_after_enter_role_button_720x1280.png`
- `old_laya_new_account_mainui_720x1280.png`
- `old_laya_new_account_mainui_probe.json`

The create-role enter button was verified from old source and runtime click. Source evidence:

- `D:\git_res\yu_client\h5\laya\pages\resource\game\login\LoginCreateRoleView.scene`: `_img_enter`, `width=378`, `height=140`, `centerX=0`, `bottom=120`.
- On 720x1280 this maps to about `x=171..549`, `y=1020..1160`; click at `360,1090` entered MainUI.

Visible first-screen old MainUI entries captured from the new account:

- Top/HUD: role head, level/combat power, VIP, recharge, customer service.
- Left rail: special gift, first recharge, task tab, team tab.
- Right rail: map/time (`云来镇`), activity icon cluster, mount/function unlock hints.
- Bottom/right: auto hangup entry, locked skill slots, partially visible bottom function bar.

Stage evidence is limited to DOM/canvas metrics because old runtime does not expose stable `window.Laya` / stage globals in browser context. `old_laya_new_account_mainui_probe.json` confirms one visible `layaCanvas` at 720x1280 plus an offscreen render canvas.

## Unity Route Coverage

Generated: `mainui_route_coverage.json`

- Registered routes: 27
- Placeholder routes: 14

Most blocking placeholders for "usable MainUI":

- `autobrush`
- `team_create`
- `team_search`
- `team_invite`
- `onhook`

Lower-risk placeholders that can stay as unified empty panels for now:

- `customerservice`
- `activity_rank`
- `marriage_gift_tips`
- `partnerawake`
- `pushgift`
- `redpacket_rain`
- `tt_record`

## Differences / Risks

- Old `zxczxc` account is guide-heavy and blocks clean MainUI inspection with reward/function/rebate/forge popups. New account reaches MainUI more reliably, but still has newbie overlays and locked buttons.
- Unity route coverage is currently static-code evidence only. It does not prove runtime click handling, because Unity MCP is still blocked.
- MainUI must be validated as portrait mobile first. Wide browser screenshots are not acceptable as baseline.

## Common Root Cause

- MainUI route structure is mostly centralized through `MainUIRouter`, so the next common fix is to register real Bootstrap targets for high-value placeholders and keep low-value entries on one unified placeholder panel.
- Static visual defects such as transparent backgrounds, missing window skins, default button skins, list templates, Bind backfill, and Addressables grouping must continue through the LayaUI conversion pipeline / editor menu regeneration path, not manual prefab edits.
- Business View/Flow should only cover runtime behavior: click events, dynamic lists, data refresh, runtime images, visibility state, model display, and protocol flow.

## Commands / Verification

- `claude --version` -> `2.1.185 (Claude Code)`
- Claude read-only command timed out after 94 seconds with no output:
  `claude -p --permission-mode acceptEdits --allowedTools Read,Grep --output-format text`
- Sub-agent read-only review completed; it agreed that AutoBrush, Team, and OnHook placeholders are the highest priority.
- `Unity_RunCommand` ping failed with `Transport closed`.
- MCP failure spawned two `C:\Users\tr\.unity\relay\relay_win.exe` processes; both were cleaned with `Stop-Process -Force`.
- `git diff --check` passed.
- `dotnet build .\yu_client_unity.slnx -v:minimal --nologo` passed: 0 warnings, 0 errors.

## Next Priority

1. In old Laya runtime, click-capture MainUI first-screen entries one by one from the new account: auto hangup, team tab, task tab, customer service, VIP/recharge, activity icon cluster, map.
2. In Unity runtime, once MCP/editor capture works, run `神霄/调试/UI运行态/截图+节点Dump` after each MainUI click and compare against old screenshots.
3. Convert the five highest-impact placeholders into usable modules or real interim panels: `autobrush`, `team_create`, `team_search`, `team_invite`, `onhook`.
4. Keep background/window-skin/list-template defects routed through the shared conversion pipeline and regeneration, especially for Bag and any transparent-window pages.
