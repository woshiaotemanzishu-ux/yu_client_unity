# Shenxiao UI heartbeat 22:44

## Scope

- Focus: MainUI entry usability before deep page fidelity.
- Old baseline: Laya runtime at `http://127.0.0.1:8090/index.html`, 720x1280 portrait runtime, account/password `zxczxc/zxczxc`.
- Unity target: current `D:\git_res\yu_client_unity` runtime/imported UI.
- Rule: no generated prefab hand-edit as final fix; static UI defects must go through LayaUI conversion / Bind / Addressables / Editor menu regeneration.

## Covered Entries

- Old runtime evidence:
  - Valid recapture: `output/heartbeat_2244/old_runtime_current_720x1280_recapture.png`.
  - Node/runtime evidence: `output/heartbeat_2244/old_runtime_current_pageinfo_recapture.json`.
  - Primary canvas confirmed 720x1280, viewport 720x1280, DPR 1.
  - Current old runtime state is still blocked by the `挂机收益` modal over MainUI.
- Unity route coverage this round:
  - Added common registered-route fallback for missing prefab/module loads.
  - Covered route keys: `vip`, `recharge`, `pet`, `redpacket`, `treasure`, `love`, `232`.
  - `shop` already had a similar missing-prefab fallback from the prior round and was not changed here.

## Differences Found

- Old runtime MainUI is visible behind a modal; this is the correct runtime source, not `.scene`.
- Unity issue addressed here: registered MainUI routes with missing target prefabs could fail with logs but no visible UI response.
- After this change, those registered-but-missing module entries should show the unified `MainUIRoutePlaceholder` instead of feeling dead.
- This does not make the missing modules visually complete. It only preserves click usability while the conversion pipeline is fixed.

## Common Root Cause

- Missing generated module prefabs / missing registered Addressables keys remain the underlying issue.
- Affected module examples from the static route scan: `vip`, `recharge`, `pet`, `redpacket`, `treasure`, `love`, `232`.
- Final fix should still be in the shared generation chain:
  - LayaUI converter default skin/resource mapping.
  - Bind refill.
  - Addressables grouping.
  - Unity Editor conversion/regeneration menu.

## Code / Generation Tasks

- Added `MainUIRouteFallback` helper inside existing compiled file:
  - `Assets/Scripts/Module/Core/MainUI/MainUIRoutePlaceholder.cs`
  - It wraps `ResManager.InstantiateAsync(...)`; if prefab load returns null or throws, it opens `MainUIRoutePlaceholder`.
- Patched flows:
  - `Assets/Scripts/Module/Core/Vip/VipFlow.cs`
  - `Assets/Scripts/Module/Core/Pet/PetFlow.cs`
  - `Assets/Scripts/Module/Core/RedPacket/RedPacketFlow.cs`
  - `Assets/Scripts/Module/Core/Rune/RuneFlow.cs`
  - `Assets/Scripts/Module/Core/Marriage/MarriageFlow.cs`
  - `Assets/Scripts/Module/Core/GodBefall/GodBefallFlow.cs`
- No prefab, Generated, Addressables, or csproj final edit was made.
- A first attempt to put the helper in a new `.cs` file failed because `Shenxiao.Module.Core.csproj` explicitly includes source files. The helper was moved into the existing compiled placeholder file instead.

## Verification

- Screenshot correction:
  - Invalid file: `output/heartbeat_2244/old_runtime_current_720x1280.png` was actually 1280x720 and should not be used as portrait evidence.
  - Valid file: `output/heartbeat_2244/old_runtime_current_720x1280_recapture.png` is 720x1280.
- Static scan:
  - `rg -n "MainUIRouteFallback|InstantiateOrShowAsync|ShowUnavailable" ...`
  - Confirmed all target flows call the common fallback helper.
- Formatting:
  - `git diff --check -- <changed route fallback files>` passed.
- Build:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - Result: success, 0 warnings, 0 errors.

## Claude / MCP

- Claude Code CLI:
  - `claude --version` had previously reported `2.1.185`.
  - This round implementation attempt used `claude -p "<route fallback task>"`.
  - It timed out after 154 seconds with no output, so Codex implemented the small common runtime fallback directly.
- Multi-agent:
  - Read-only subagent `Newton` completed route analysis and confirmed the same missing-prefab registered route pattern.
- Unity MCP:
  - `Unity_RunCommand` failed with `Transport closed`.
  - Found `relay_win.exe` PID 22672, parent process `codex.exe`, not Unity Editor.
  - Stopped the stale relay and retried `Unity_RunCommand`; it still failed with `Transport closed`.
  - Current verification therefore uses browser/runtime evidence, static checks, and `dotnet build`.

## Next Priority

1. Fix generation pipeline for registered missing-prefab modules, starting with `vip`, `recharge`, `pet`, `redpacket`, `treasure`, `love`, `232`.
2. Restore Unity MCP or run Unity Editor menu manually to regenerate/refill/group UI assets; then verify placeholder routes turn into real pages.
3. Clear or handle old runtime `挂机收益` popup chain to capture clean MainUI baseline and click bottom `角色/背包/设置/聊天/地图`.
4. Continue MainUI click sweep: every visible button must either open a migrated page or a unified placeholder.
