# MainUI shared-window verification note

Use this note when converting or fixing MainUI entry behavior.

- MainUI is a combined HUD prefab, not a normal one-view-per-prefab module.
  Use `Schemas/LayaUI/ui_groups.json` and
  `Schemas/LayaUI/ui_root_layouts.json` as the authority for `MainUIModule`
  and its first-pass HUD roots.
- If a MainUI bottom/top entry route fails with
  `prefabs/ui/common/basewindowskin`, fix the common conversion output first.
  Do not patch `MainUIDownView`, `MainUIRouter`, or `{Module}Flow` to hide the
  missing shared frame.
- The proven repair for the shared window skin is:
  `LayaSceneConverter.ConvertSingle("common/BaseWindowSkin")`,
  `LayaBindFiller.FillPrefab("Assets/Prefabs/UI/Common/BaseWindowSkin.prefab")`,
  then `AddressableSetup.AutoGroupAll()`. Confirm the resulting address is
  `prefabs/ui/common/basewindowskin` and the prefab has `BaseWindowSkinView`.
- Runtime acceptance for this case: auto-enter MainUI, call
  `MainUIRouter.Open("role")`, and confirm `UIRoot/Window/BaseWindowSkin` plus
  `RoleModule` exist. A runtime dump under `output/runtime_unity/<timestamp>`
  is enough evidence when MCP `ScreenCapture` does not emit a PNG.
- Known unrelated noise during this verification: missing/corrupt
  `object/spirit/model_spirit_1001`, `object/mount/model_mount_1000`, and
  `ConfigTaskArrow` tips config warnings. Do not treat these as MainUI
  conversion regressions.
