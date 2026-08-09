# Verification

- Scope clean before work: yes (`Assets/Scripts/Module/Core/Setting`, `Assets/Prefabs/UI/Setting`, target output had no dirty overlap).
- Old-client source enumeration: complete for reachable Setting main/base/shield/bottom and rename/navigation dependencies.
- Unity Prefab/Bind/Flow/Model/Controller mapping: complete statically.
- Prefab edit: none; no deterministic visual correction was justified without current runtime pixels.
- Unity/editor/browser: not started.
- Network/account writes: none.
- Runtime state: not-run; destructive execution leaves blocked by authorization.

## Results

- `route_ledger.py init`: pass, schema 6, 90 nodes.
- `route_ledger.py apply blocked-results.json`: pass; 13 nodes are blocked after parent roll-up, 77 remain not-run.
- `route_ledger.py validate`: pass.
- Static manifest assertions: pass (`STATIC_LEDGER_OK schema=6 nodes=90 blocked=13`).
- Required slider/auto-pick/shield subtype binding scan: pass (`STATIC_BINDING_HITS=32`).
- `git diff --check` for Setting/output island: pass.
- Plain `dotnet build Shenxiao.Module.Core.csproj --no-restore`: initially blocked by three concurrent files absent from the generated csproj (`ShopBulkPurchaseView`, `GuildHelpRuntime`, `GuildJoinRuntime`), none in Setting.
- Core build with the existing verification-only include target `output/ui_route_audit/2026-08-09_guild/include-new-guild-scripts.targets`: pass, 0 errors / 84 existing warnings.

## Completion boundary

Static topology, ownership, protocol mapping and compilation are complete. Real old-H5/Unity Web comparison, runtime slider/toggle write-and-restore, scroll drag/clipping, prompt/cancel pixels, cold/warm timings, Player/catalog hashes and immediate/reopen state are still required. No route node is claimed `done` by this static batch.
