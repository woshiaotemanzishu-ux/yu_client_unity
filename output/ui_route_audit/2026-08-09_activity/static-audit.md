# Activity direct pages static audit — 2026-08-09

## Scope

- Route: `mainui.activity.direct-pages`.
- Unity closure: `Assets/Scripts/Module/Core/Activity/**`, `Assets/Prefabs/UI/Activity/**`, and this audit directory.
- Old H5 authorities: `AccumRecharge*`, `ConRecharge*`, `DailySupply*`, `CreatRoleGiftView`, `rechargeReturnView`.
- No Unity or browser was started; no claim, recharge, purchase, refresh, or other account write was executed.

## Implemented statically

- Added a runtime bootstrap and shared `ActivityFlow` for the five direct `ActivityModule` pages. Only safe single-page key `331@109` is registered; multi-page windowscomponent keys are not hijacked.
- Existing Prefab roots now bind Activity runtime subclasses without editing Generated files.
- Added `33104` detail refresh to cumulative recharge, continuous recharge, daily supply and create-role gift; daily supply also reads `33209`, and continuous recharge requests `15960` history.
- Dynamic list cells use `BaseView.Show/Hide`, status-first ordering, filtered events, clone cleanup, scroll reset, `VerticalLayoutGroup` and `ContentSizeFitter` saved in the Prefab.
- Added status 0/1/2 visibility, red dots, countdown, tier filtering, featured-grade state, claim click wiring and safe unresolved-navigation logging.
- Claim handlers retain the formal `33105` route but no handler was clicked or invoked during this pass.

## Static verification

- Latest `dotnet build Shenxiao.Module.Core.csproj --no-restore -p:CustomAfterMicrosoftCommonTargets=.../activity-extra-compile.targets`: PASS, 0 errors, 84 existing warnings (the first clean dependency pass reported 99).
- Prefab script GUID/type identity, YAML component reference cardinality, direct-page root parentage, layout count, Generated-file immutability and route-ledger schema are checked by `verify-static.ps1`.

## Remaining blockers

- Shared reward cells/details and success popup belong to Common/Equipment and were not copied or edited.
- Recharge, DailyTaskView, seven-day group and outer windowscomponent close ownership are cross-module dependencies.
- `33105` claim paths need explicit write authorization plus success/failure, immediate refresh and close/reopen evidence.
- Continuous-recharge history-derived day matrix and tier resource variants need a prepared real account.
- All pages still require current old-H5 versus matching Unity Web comparison at both required viewports, cold/warm lifecycle, list dragging/clipping, resource-ready and performance evidence.
