# Shenxiao UI Patrol - MainUI 2026-06-24 03:44

## Scope
- Automation: `shenxiao-ui`
- Focus: MainUI usable entries, old Laya runtime 720x1280 portrait baseline.
- No code, prefab, converter, or Unity menu generation edits in this round.

## Covered Entries
- Old runtime baseline:
  - evidence: `old_laya_start_state.png`, `old_laya_dom_probe_start.json`
  - result: `canvas#layaCanvas` is 720x1280; body has no readable text nodes.
- Auto/hangup state stabilization:
  - click `(660,958)` on old `取消挂机`.
  - result: old HUD changed back to `开启挂机`.
  - evidence: `old_laya_after_cancel_hangup_660_958.png`
- Task/team:
  - clicks `(24,830)` and `(18,838)` on visible team tab area.
  - result: old client did not switch to team list and showed `完成特殊试炼任务开启`.
  - source evidence: `TeamModel.IsOpenTeam()` checks mainline task `101260`; if current task is <= target task, it shows `完成` + task name + `任务开启` and returns false.
  - evidence: `old_laya_team_tab_attempt_x24_y830.png`, `old_laya_team_tab_attempt_x18_y838.png`
- Customer service:
  - click `(578,83)`.
  - result: no visible old-client panel and no new browser tab; tab count stayed `1`.
  - evidence: `old_laya_click_customer_service_578_83.png`, `old_laya_customer_activity_actions.json`
- Activity icon:
  - click `(438,281)` on visible `开服活动`.
  - result: old real `开服活动` page opened with title frame, large banner, reward list, item icons, claim buttons.
  - return `(664,1113)` closed the page.
  - source evidence: old `ActivityIcon.ts` key `1112` fires `KaifuActivityModelEvent.OPEN_KAIFUACTIVITYVIEW`.
  - evidence: `old_laya_click_openserver_activity_438_281.png`, `old_laya_return_from_openserver_664_1113.png`

## Differences Found
- Team entry:
  - Old runtime currently locks team behind task `101260`/`特殊试炼`.
  - Unity should not fake-create a team panel for this state; if clicked before real Team flow exists, it should show the same lock/placeholder behavior clearly.
- Open-server activity:
  - Old key `1112` opens a real Kaifu activity page.
  - Unity activity icons currently route by `iconType`; unless `1112` is registered to real KaifuActivity flow, it should fall through to unified placeholder.
- Customer service:
  - Old tested runtime has no visible UI and no new browser tab.
  - Unity `customerservice` can stay placeholder until the real customer-service channel is identified.

## Common Root Causes
- MainUI click parity depends on runtime state:
  - team is task-gated.
  - activity icons are dynamic keys with specific event handlers.
  - auto/hangup state changes both visual layout and click consequences.
- Unity common route path is mostly correct now:
  - visible entries should call `MainUIRouter.Open`.
  - unregistered keys should be handled by `MainUIRoutePlaceholder`.
- Remaining full-page parity is not a prefab hand-edit problem:
  - static frames/backgrounds/skins belong in LayaUI converter/default skin/common window pipeline.
  - dynamic lists and activity pages belong in View/Flow runtime logic.

## Unity Route Audit
- Static route search confirms:
  - `MainUIRouter` falls back to `MainUIRoutePlaceholder.Show(viewKey)`.
  - `ActivityIcon` calls `MainUIRouter.Open(iconType)`.
  - `MainUITopView` binds `customerservice`.
  - `MainUITaskTeamView` binds `team_create`, `team_search`, `templeawaken`.
  - `MainUIActivityView` binds `activity_rank`.
  - `MainUISkillView` binds `partnerawake`.
  - `MainUIAutoBrushView` binds `autobrush` and `autobrush_toggle`.
- Registered/real examples visible in search:
  - `chat`, `setting`, `shop`, `map`, `friend`, bottom modules, `232`, `dailyfind`, `firstblood`, `levelreward`, `autobrush_toggle`.
- Expected placeholders until real modules are migrated:
  - `customerservice`, `team_create`, `team_search`, `templeawaken`, `activity_rank`, `partnerawake`, `autobrush`, activity numeric keys such as `1112`, `158`, `338@...`.

## Commands And Tool Status
- Unity MCP:
  - no `relay_win.exe` existed at start of round.
  - `Unity_RunCommand` still failed with `Transport closed`.
- Build:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - success, 0 warnings, 0 errors.
- Claude Code:
  - command attempted:
    `claude -p "只读审查 ... MainUI 可点击性改动 ..."`
  - result: timed out after ~124s, exit code `124`, no useful output.
- Multi-agent:
  - two explorer subtasks were spawned for old-source and Unity-route checks.
  - both were still running after wait and were closed; no sub-agent result counted.
- Git/worktree:
  - existing modified code files remain from earlier MainUI/Bag/AutoBrush/Common UI work.
  - this round added only `output/heartbeat_1929/` evidence and report.

## Executed Code/Generation Tasks
- None.
- No prefab edits.
- No converter edits.
- No Unity Editor menu regeneration because MCP remained blocked and no converter patch was made.

## Evidence Files
- Folder: `output/heartbeat_1929/`
- Key screenshots:
  - `old_laya_start_state.png`
  - `old_laya_after_cancel_hangup_660_958.png`
  - `old_laya_team_tab_attempt_x24_y830.png`
  - `old_laya_team_tab_attempt_x18_y838.png`
  - `old_laya_click_customer_service_578_83.png`
  - `old_laya_click_openserver_activity_438_281.png`
- Key JSON:
  - `old_laya_dom_probe_start.json`
  - `old_laya_recovery_actions.json`
  - `old_laya_team_tab_actions.json`
  - `old_laya_customer_activity_actions.json`

## Next Priority
1. Add/restore durable Unity runtime screenshot + node dump path independent of MCP, or relaunch Editor MCP so Unity runtime can be verified.
2. Register real Unity flow for old activity key `1112` only when KaifuActivity page is migrated; otherwise keep numeric activity keys in placeholder.
3. Re-test team after old account passes task `101260`, or use a higher-progress account; current `zxczxc` state is locked by old runtime.
4. Continue MainUI clickable coverage with stable old state:
   - `158` strengthen icon.
   - `activity_rank`.
   - partner lock/`partnerawake`.
   - top `buff`, `fightmode`, `vip`, `recharge`, `halo`.
