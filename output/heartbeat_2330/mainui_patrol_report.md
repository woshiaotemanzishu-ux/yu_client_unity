# MainUI Patrol 23:30

## Scope

- Baseline: latest valid old Laya runtime portrait evidence copied from `output/heartbeat_2244/old_runtime_current_720x1280_recapture.png`.
- Unity target: current open Unity Editor project `D:\git_res\yu_client_unity`.
- Focus: unblock MainUI entry module generation through the shared LayaUI pipeline, not hand-edited prefabs.

## Covered Entries

- MainUI route fallback scope from previous pass: VIP, pet, red packet, treasure/rune, marriage/love, god befall.
- Entry module generation scope: `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`, `shop`, `common`.

## Differences Found

- Generated Bind/source evidence exists for these MainUI entry modules, but prefab module outputs are still missing under `Assets/Prefabs/UI`.
- Current module prefab check:
  - `Assets/Prefabs/UI/Vip/VipModule.prefab`: missing
  - `Assets/Prefabs/UI/Pet/PetModule.prefab`: missing
  - `Assets/Prefabs/UI/RedPacket/RedPacketModule.prefab`: missing
  - `Assets/Prefabs/UI/Rune/RuneModule.prefab`: missing
  - `Assets/Prefabs/UI/Marriage/MarriageModule.prefab`: missing
  - `Assets/Prefabs/UI/Shop/ShopModule.prefab`: missing
  - `Assets/Prefabs/UI/GodBefall/GodBefallModule.prefab`: missing
  - `Assets/Prefabs/UI/Common/CommonModule.prefab`: missing

## Common Root Cause

- This is still a shared LayaUI conversion/generation gap, not a per-prefab hand-fix problem.
- Existing Unity Editor is open on the project, so batchmode `-executeMethod` cannot open the same project.
- Unity MCP remains unavailable with `Transport closed`.
- The current Unity Editor did not auto-refresh external C# file changes during this pass, so the marker runner did not execute.

## Generation / Code Tasks

- Updated `Assets/Editor/LayaUI/LayaUIPipeline.cs`.
- Added `Shenxiao/LayaUI/Rebuild MainUI Entry Modules` menu entry in the previous pass.
- Added one-shot Editor reload runner:
  - request file: `Temp/ShenxiaoRunMainUIEntryModules.request`
  - runner: `[InitializeOnLoadMethod]`
  - action: `RunMainUIEntryModulesNoConfirm()`
  - behavior: deletes request before running to avoid repeated rebuilds.
- Created request file `Temp/ShenxiaoRunMainUIEntryModules.request`.
- No generated prefab was hand-edited.

## Verification

- `dotnet build Shenxiao.Editor.csproj -v:minimal`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- Unity MCP health check via `Unity_RunCommand`: failed with `Transport closed`.
- Claude Code command:
  - `claude -p "在 D:\git_res\yu_client_unity 中请只读分析，不修改文件。目标：已有 Unity Editor 打开同项目，batchmode 因项目锁不能执行，Unity MCP Transport closed。请给出最小方案：如何用 Editor 脚本重载和一次性 marker 触发 LayaUIPipeline.RunMainUIEntryModulesNoConfirm，并指出风险。只输出要点。"`
  - result: timed out after 45 seconds with no output; residual `claude.exe` and `relay_win.exe` were stopped.
- Computer Use attempt to send Unity refresh shortcut: app approval timed out after 5 minutes; no Unity UI action was performed.
- Marker status after wait: `Temp/ShenxiaoRunMainUIEntryModules.request` still exists.
- Entry module prefab status after wait: no target `*Module.prefab` generated.

## Next Priority

1. Trigger Unity Editor refresh/reload explicitly, or allow Computer Use approval, so the marker runner can execute without closing the open project.
2. If approval is not available, ask before closing/restarting Unity, then run batchmode `-executeMethod Shenxiao.Editor.LayaUI.LayaUIPipeline.RunMainUIEntryModulesNoConfirm`.
3. After generation, verify prefab existence, Bind refill, Addressables groups, and MainUI click behavior for VIP/pet/red packet/rune/marriage/god befall/shop/common.
