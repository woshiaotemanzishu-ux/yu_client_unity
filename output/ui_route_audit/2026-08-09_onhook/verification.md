# Verification

- OnHook code/output target was clean before work.
- Full `audit-game-ui-route`, runbook, schema and `fix-view` were reread; missing editable Prefab triggered the required `convert-module` decision.
- Old TS/controller/model plus five CDN scene/json assets were inventoried without starting the browser.
- Unity OnHook controller/model/shell plus read-only MainUI/Task consumers were reconciled.
- No Unity, browser, protocol send, account write, Generated edit, Addressables edit or Prefab creation was performed.
- No production code was changed because the known UI defects cannot be repaired safely without the first-conversion gates.

## Schema 6

- `route_ledger.py init` succeeded: `route=mainui.onhook.complete`, `schema=6`, `nodes=109`.
- `route_ledger.py apply` succeeded atomically.
- Final strict validation succeeded: `blocked=9`, `defect=8`, `not-run=92`, `done=0`.
- All three JSON inputs parse under PowerShell `ConvertFrom-Json`.

## Static assertions

- `Assets/Prefabs/UI/OnHook` does not exist.
- `OnHookBootstrap` registers `onhook` exactly once and does not register `onhook_addition`.
- Read-only `MainUIOnHookView` opens `onhook_addition` once, proving the route gap without runtime inference.
- `OnHookController` contains 13216 handling and no 13213 registration/send path.

## Compile boundary

- Current full `Shenxiao.Module.Core.csproj` compile is blocked outside this route by concurrent
  `AutoBrushController.cs:207,211` `CS0266 long -> ulong`; OnHook files are not in the error set.
- A verification-only compile selected only `OnHookController.cs` and `OnHookShellView.cs`, with the
  last successful Core assembly supplying read-only cross-island symbols: **0 errors, 23 warnings**.
  The warnings are expected duplicate-type warnings from that isolation technique plus the existing
  editor intercept warning. Temporary compile output was removed and the touched `Temp/bin` Core
  artifact was restored from `Library/ScriptAssemblies`; no source, Prefab or project file was changed.

## Remaining runtime gates

- First-conversion runtime snapshots, editable Prefab bake, Bind generation and Addressables registration.
- Real old/Unity Web same-account traversal for HUD visibility, main page controls, reward list, addition page,
  redemption, receive result, immediate server-pushed refresh, reopen and return chains.
- 13213 remains hard-negative until cost/confirmation/asset/EXP/result/reopen coverage is complete.
- 13216 result popup and `RewardFlyService` remain deterministic implementation defects.
- Task tips=91 direct `Receive()` is a cross-island write blocker and was not modified.
