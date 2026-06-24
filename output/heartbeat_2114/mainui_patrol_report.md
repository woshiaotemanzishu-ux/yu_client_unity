# Shenxiao UI Patrol - 21:14 MainUI Runtime Baseline

## Scope

- Automation: `shenxiao-ui`
- Baseline: old Laya runtime at `http://127.0.0.1:8090/index.html`
- Viewport: 720x1280 portrait mobile
- Focus: MainUI usable-entry patrol foundation, not prefab edits

## Covered Entries

- Runtime state cleanup:
  - Closed reward overlay from Settings: `old_laya_after_reward_complete_click_retry.png`
  - Closed Settings: `old_laya_after_settings_close.png`
  - Waited through auto continue: `old_laya_after_auto_continue_wait.png`
  - Closed reward layer back to HUD: `old_laya_after_reward_layer_click_2.png`
- MainUI visible HUD captured:
  - `old_laya_mainui_hud_before_entry_clicks.png`
- Attempted entry clicks from HUD:
  - VIP: `old_laya_click_vip_from_hud.png`
  - Recharge: `old_laya_click_recharge_from_hud.png`
  - Customer service: `old_laya_click_customer_from_hud.png`
  - Bottom settings: `old_laya_click_settings_from_hud.png`

## Differences / Problems Found

- The old Laya account is not in a stable idle MainUI state. Closing Settings immediately entered `斩秘巡行` reward/auto-continue flow.
- A later HUD was visible, but it still contained `5秒后自动继续` / `自动战斗中` state. This polluted entry clicks.
- The VIP click screenshot did not open `VIP福利`; instead the page continued into battle/auto state.
- Recharge / customer / settings screenshots landed on `大比拼` activity page, so these clicks are invalid as baseline evidence.
- Current runtime browser evaluate can see canvases but cannot access `window.Laya`, `AutoBrushModel`, `TaskModel`, `VipModel`, etc. CDP was blocked with `Raw CDP is unavailable while Browser Use is resolving a paused document response.`

## Common Root Cause

- The patrol foundation is unstable because old-client MainUI is still under auto task / auto brush / event auto-continue control.
- Source evidence: `D:\git_res\yu_client\h5\src\mainUI\MainUIAutoBrushView.ts:211-216` shows the MainUI auto button toggles auto brush through `AutoBrushModel.REQUEST_PROTO_EVENT` protocol `13307`, with `"c", 1` when `auto_state` is true and `"c", 0` otherwise.
- Until this state is stopped deterministically, coordinate-based entry clicks can produce false positives or false negatives.

## Generation / Code Tasks

- No code edits in this heartbeat.
- No prefab edits.
- No Unity Editor menu conversion/regeneration executed because Unity MCP remained unavailable.
- No successful Claude Code collaboration result was produced.

## Verification

- Screenshots saved under `output/heartbeat_2114/`.
- Runtime probe saved: `old_laya_runtime_probe.json`.
- `claude --version` returned `2.1.185 (Claude Code)`.
- Claude Code read-only analysis command:
  - `claude --no-session-persistence --permission-mode dontAsk --add-dir D:\git_res\yu_client --tools Read,Grep,Glob -p "..."`
  - Result: timed out after about 94 seconds with no conclusion.
  - Cleanup: stopped residual `claude.exe` and its child `relay_win.exe`; one non-Claude Unity relay remained.
- Unity MCP:
  - `Unity_RunCommand` before cleanup: `Transport closed`
  - `Unity_RunCommand` after Claude relay cleanup: `Transport closed`
- Build:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - Passed with 0 warnings, 0 errors.

## Claude / MCP Availability

- Claude CLI binary is installed and reports version successfully.
- Claude Code non-interactive analysis is currently not reliable for this task: timed out and left relay process residue.
- Unity MCP is still blocked with `Transport closed` even after cleaning the Claude-owned relay.

## Next Priority

1. Establish deterministic old Laya idle MainUI:
   - Prefer a fresh/low-noise account or a runtime hook/console path that can stop auto brush / auto task / event auto-continue.
   - Validate protocol `13307` stop behavior against old runtime before further entry patrol.
2. Re-run MainUI entry coverage only after idle state is stable:
   - Top: VIP, recharge, customer service.
   - Left/activity icons: opening reward/activity placeholders or real pages.
   - Right: map, ranking/activity, 御魂, 75级甜蜜开榜.
   - Bottom: settings, role, bag, skill cluster, auto brush toggle.
3. Unity comparison should then verify:
   - Every visible MainUI entry is clickable.
   - Migrated modules open real pages.
   - Unmigrated modules open a unified placeholder panel.
4. If static UI defects appear, fix via LayaUI converter/default maps/Bind/Addressables/menu regeneration, not manual prefab editing.
