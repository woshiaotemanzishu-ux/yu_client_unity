# Role outward shared-root static audit

Date: 2026-08-09

Scope: `type_id=3/4/5/12` (垂神翼影 / 古法符相 / 殒锋天刃 / 玄穹云披). This pass did not launch or operate Unity or a browser and did not execute any account write.

## Frozen truth

- The four old H5 subclasses all extend `pet/OutWardBaseView.ts` with `show_type=1` and hide `star_group`, `shadow_group`, `before_btn` and `after_btn`.
- Unity RoleFlow already routes all four types to the single `PetModule.prefab/OutWardBaseView` implementation.
- Read probes on open are `16002`, `16006`, `16011`, `16028`.
- Write leaves are `16003`, `16005`, `16008`, `16009`, `16010`, `16020`, `16029`, `16030`; none were executed in this pass.
- Training materials are `config_mount_prop` rows with `type=1`. `config_mount_goods` is the three-crystal inventory. The previous Unity accessor mixed these two tables.
- The baked Prefab has the shared root, three material slots, three crystal slots and a `res` model host. It does not contain editable `IllusionBaseView`, `OutwardLvSystem` or `PetProptityView` instances, although generated Bind classes exist.

## Static implementation in this pass

- Corrected the shared material accessor and added a separate crystal accessor.
- Added a `ride_figure` stage model accessor.
- Rendered the three crystal slots from config + `16011` counters without wiring or executing write actions.
- Applied the common role-outward visibility contract and the old `show_type=1` star/level display semantics.
- Rendered the inline base-appearance using/unuse state from authoritative `FigureStage == Stage` without executing `16003`.
- Added one per-view `UIModelStage` implementation for all four types:
  - type 3: `wing`, `w`, `default_wing`
  - type 4: `fabao`, `a`, `default_artifact`
  - type 5: `weapon`, `d`, `default_weapon`
  - type 12: `back`, `b`, `default_back_ornament`
- Type switch/hide/dispose invalidates async loads and clears or disposes the dedicated stage, preventing stale model accumulation.

## Remaining runtime gates and blockers

- Model presence is not completion: real Editor/Web RT pixels, two-timepoint animation, always-effect ownership, rotation and close/reopen cleanup remain required.
- Old-vs-Unity visual comparison at both required viewports remains required.
- Property popup, illusion subpage and level-system subpage are blocked by missing editable subview instances in `PetModule.prefab`; runtime UI tree synthesis was intentionally not used.
- Skill/material/crystal detail popups and base-appearance toggle are not wired in the current shared View.
- FairyWish is a cross-island dependency and its existing page is still a degraded implementation; it was not edited in this closure.
- All write leaves remain blocked until explicit transaction authorization and a reversible account-state recipe are available.
- Because the shared class also serves horse/partner, final completion requires one high-frequency horse/partner runtime regression sample.

## Validation

- `dotnet restore Assembly-CSharp.csproj --ignore-failed-sources`: generated the missing local assets file after initial `NETSDK1004`.
- `dotnet build Shenxiao.Module.Core.csproj --no-restore -v:minimal -m:1`: final shared-root sources passed with 0 errors (84 pre-existing project warnings).
- A full `Assembly-CSharp.csproj` build passed before the final read-only state addition. The final full rerun reached and built `Shenxiao.Module.Core`, then was blocked in out-of-scope concurrent `Assets/Editor/CliVerify/Cases/MedalCase.cs` because `MedalConfigs` was temporarily missing; no Map/outward source error was reported.
- `verify-static.ps1`: `STATIC_ROLE_OUTWARD_OK`; checks the schema-6 topology, config ownership/mappings, four model profiles, shared stage/effect hooks, Prefab slots/host and the three absent subviews.
- `route_ledger.py validate`: passed (`schema=6`, `nodes=234`, `needs-runtime-verify=91`, `blocked=143`, `not-run=0`).

## SHA-256

- `Assets/Scripts/Module/Core/OutWard/OutWardModel.cs`: `3895285CE012EBFF4647481886B72AC1B1349DE76C26E95CCC7861A0594B4600`
- `Assets/Scripts/Module/Core/Pet/Views/OutWardBaseView.cs`: `DBE64666463A292C82CB24B9939E18977232F446B4320A299C307981CEB8BEC8`
- `Assets/Prefabs/UI/Pet/PetModule.prefab`: `6B41D2ABC1B7D7C618C7A7D0F00401F7CEBC0EABC25CCEBFE16D45ADAC984CA5`
- `route-manifest.json`: `2EAA37C850D921B58094B85E01CF803C86DA5DF801902479364D49F7B3DE8DA9`
