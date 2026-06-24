# Shenxiao UI heartbeat 22:29

## Scope

- Baseline: old Laya runtime, 720x1280 portrait, `http://127.0.0.1:8090/index.html`.
- Unity target: current `yu_client_unity` MainUI route layer and first visible entries.
- Rule: do not treat `.scene` static files as final; static UI defects must go through the LayaUI conversion pipeline and regeneration.

## Covered entries

- Old runtime path successfully re-ran from fresh instance:
  - login page with `zxczxc/zxczxc`: `old_runtime_fresh_720x1280.png`
  - server/enter page: `old_runtime_after_login_720x1280.png`
  - role select: `old_runtime_after_enter_720x1280.png`
  - enter game via platform click: `old_runtime_after_platform_click_720x1280.png`
  - MainUI visible behind reward popup: `old_runtime_mainui_or_popup_720x1280.png`
  - reward popup close/claim attempts: `old_runtime_after_reward_close_720x1280.png`, `old_runtime_after_reward_close_retry_720x1280.png`, `old_runtime_after_reward_claim_720x1280.png`
- Unity MainUI static route coverage:
  - registered real route candidates with prefab present: `role`, `bag`, `setting`, `chat`, `map`, `equip`, `dailyfind`, `brightsea`, `composite`, `red`, `friend`, `email`, `firstblood`, `levelreward`, `halo`, `guild`, `guildhelp`.
  - registered route but target prefab missing: `shop`, `vip`, `recharge`, `pet`, `redpacket`, `treasure`, `love`, `232`.
  - visible/direct MainUI placeholder candidates: `customerservice`, `team_create`, `team_search`, `templeawaken`, `partnerawake`.

## Differences found

- Old runtime can reach MainUI, but the first stable state is immediately covered by the offline/reward popup. This blocks reliable pixel-by-pixel clicking of MainUI entries in the old client until the popup chain is handled.
- Old runtime DOM remains empty for game UI; valid evidence is screenshot plus canvas metadata. Current fresh instance exposed two canvases, primary canvas `720x1280`.
- Unity route behavior is uneven:
  - unregistered keys correctly fall back to `MainUIRoutePlaceholder.Show(viewKey)`;
  - `shop` now falls back to placeholder when `ShopModule`/frame load fails;
  - other registered-but-missing-prefab routes mostly log and return without placeholder, so they can still feel like dead clicks.

## Common root cause

- This is not a per-prefab manual adjustment problem. The common issue is the generated/imported UI asset chain and route availability contract:
  - several registered Flow classes request missing module prefabs through `GameResPath.GetUIPrefab(module, prefab)`;
  - `GameResPath.GetUIPrefab` lowercases module and prefab keys for Addressables-style lookup;
  - static missing module/background/frame/list assets should be fixed through LayaUI conversion, Bind refill, Addressables grouping, and Unity Editor regeneration;
  - runtime fallback should be shared so any registered opener whose target prefab is absent still opens the unified placeholder instead of silently failing.

## Commands and verification

- Route register scan:
  - `rg -n "MainUIRouter\\.Register\\(" Assets/Scripts/Module/Core -g "*.cs"`
- MainUI opener scan:
  - `rg -n "MainUIRouter\\.Open\\(" Assets/Scripts/Module/Core/MainUI -g "*.cs"`
- Prefab existence checks:
  - confirmed `role/bag/setting/chat/map/common BaseWindowSkin` exist.
  - confirmed `shop/vip/pet/redPacket/rune/marriage/godBefall` target prefabs are missing.
  - confirmed `guildhelp` uses `guild/GuildModule`, which exists.
- Build:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - result: success, 0 warnings, 0 errors.
- Diff whitespace:
  - `git diff --check -- Assets/Scripts/Module/Core/Shop/ShopFlow.cs`
  - result: pass.

## Claude and MCP status

- `claude --version`: `2.1.185 (Claude Code)`.
- Claude Code read-only route catalog attempt:
  - command: `claude -p "...只读分析 MainUIRouter.Register / MainUIRouter.Open / prefab 是否存在..."`
  - result: timed out after about 94 seconds with no output. CLI exists, but this non-interactive analysis prompt did not complete in time; not counted as successful collaboration.
- Unity MCP:
  - `relay_win.exe`: no running process found.
  - Unity Editor process exists.
  - `Unity_RunCommand`: still failed with `Transport closed`.

## Next priority

1. Implement a common runtime fallback pattern for registered MainUI openers whose prefab load fails, then apply it to `vip/recharge/pet/redpacket/treasure/love/232` without changing generated prefab assets.
2. Use Unity Editor regeneration/MCP once transport works to produce missing module prefabs from the conversion pipeline, especially `shop`, `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`.
3. Continue old-runtime automation by handling the reward popup chain first; only after a clean MainUI screenshot is available should the next round click bottom `role/bag`, settings, chat, shop, map, team/task, and activity entries.
