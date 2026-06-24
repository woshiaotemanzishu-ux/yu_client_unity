# Shenxiao UI heartbeat 23:00

## Scope

- Focus: MainUI first-entry modules, with priority on usable click targets and pipeline-level recovery.
- Baseline: valid old runtime portrait evidence reused from 22:44 because the old client is still blocked by the `挂机收益` modal.
- Rule followed: no generated prefab hand-edit; route recovery must be via LayaUI pipeline / Bind / Addressables.

## Covered Entries

- MainUI registered routes and target modules reviewed:
  - `vip` / `recharge` -> `vip`, `VipModule`
  - `pet` -> `pet`, `PetModule`
  - `redpacket` -> `redPacket`, `RedPacketModule`
  - `treasure` -> `rune`, `RuneModule`
  - `love` -> `marriage`, `MarriageModule`
  - `232` -> `godBefall`, `GodBefallModule` plus `common/BaseWindowSkin`
  - `shop` -> `shop`, `ShopModule`
- Old runtime evidence for this cycle:
  - `output/heartbeat_2300/old_runtime_current_720x1280.png`
  - `output/heartbeat_2300/old_runtime_current_pageinfo.json`

## Differences Found

- Existing generated Bind folders prove the modules have been analyzed/generated at code level:
  - `Generated/UI/Vip`, `Pet`, `RedPacket`, `Rune`, `Marriage`, `GodBefall`, `Shop`.
- Missing or incomplete prefab output:
  - Missing dirs: `Assets/Prefabs/UI/Vip`, `Pet`, `RedPacket`, `Rune`, `Marriage`, `Shop`.
  - `Assets/Prefabs/UI/GodBefall` exists but only has single-window prefabs, not `GodBefallModule.prefab`.
  - `Assets/Prefabs/UI/Common/BaseWindowSkin.prefab` exists, so the common frame is present.
- Manifest contains all target modules, so the problem is not missing manifest registration:
  - `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`, `shop`, `common` all exist in `ui_manifest.json`.

## Common Root Cause

- This is a conversion output gap: modules exist in manifest/Bind, but merged module prefabs were not generated or were not retained.
- `ui_groups.json` only has a custom `mainUI` group; for other modules, `LayaUIGroups.ForModule` already defaults to `{ModuleDir}Module`.
- Therefore the next correction is to rerun the normal module pipeline for the MainUI entry modules, not to rewrite route mappings or hand-create prefabs.

## Code / Generation Tasks

- Added a dedicated reusable pipeline entry in `Assets/Editor/LayaUI/LayaUIPipeline.cs`:
  - Menu: `Shenxiao/LayaUI/Rebuild MainUI Entry Modules`
  - Command-line method: `Shenxiao.Editor.LayaUI.LayaUIPipeline.RunMainUIEntryModulesNoConfirm`
- Target modules:
  - `vip`, `pet`, `redPacket`, `rune`, `marriage`, `godBefall`, `shop`, `common`
- This runs the existing pipeline:
  - sprite import
  - template build
  - `LayaSceneConverter.ConvertModuleCombined`
  - pending Bind refill
  - optional Addressable auto-grouping
- No prefab, Generated, Addressables, or schema output was manually edited.

## Verification

- Static checks:
  - `rg -n "MainUIEntryModules|RunMainUIEntryModules" Assets/Editor/LayaUI/LayaUIPipeline.cs`
  - `git diff --check -- <changed files>`
- Build:
  - `dotnet build yu_client_unity.slnx -v:minimal`
  - Result: success, 0 warnings, 0 errors.
- Expected manual/MCP command once Unity Editor command execution is available:
  - Menu: `Shenxiao/LayaUI/Rebuild MainUI Entry Modules`
  - Or execute method:
    `Shenxiao.Editor.LayaUI.LayaUIPipeline.RunMainUIEntryModulesNoConfirm`

## Claude / MCP

- Claude Code CLI:
  - Command used:
    `claude -p "在 D:\git_res\yu_client_unity 仅读取文件，不修改。请检查 VipFlow/PetFlow/RedPacketFlow/RuneFlow/MarriageFlow/GodBefallFlow 的 GameResPath.GetUIPrefab(module, view) 与 Assets/Prefabs/UI 现有目录/文件是否命名不一致。只输出每个模块: 期望key、现有最可能prefab、是否应改Flow映射还是转换配置。不要跑构建。"`
  - Result: timed out after about 74 seconds with no output.
  - Follow-up: the timed-out `claude.exe` process remained alive and spawned `relay_win.exe`; both residual processes were stopped.
- Unity MCP:
  - `Unity_RunCommand` still fails with `Transport closed`, including after cleaning the residual Claude/relay process.
  - No Editor conversion was run this cycle.

## Next Priority

1. Restore Unity MCP or run the new Editor menu manually.
2. Rerun `RunMainUIEntryModulesNoConfirm` and verify the expected module prefabs appear:
   - `VipModule.prefab`, `PetModule.prefab`, `RedPacketModule.prefab`, `RuneModule.prefab`, `MarriageModule.prefab`, `GodBefallModule.prefab`, `ShopModule.prefab`.
3. Rerun Addressables grouping if auto-group is off.
4. Start Unity runtime and click MainUI entries again; real pages should replace placeholders where module prefabs now load.
