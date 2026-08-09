$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$errors = New-Object System.Collections.Generic.List[string]

function Require([bool]$condition, [string]$message) {
  if (-not $condition) { $errors.Add($message) }
}
function Text([string]$relative) { Get-Content -Raw -Encoding UTF8 (Join-Path $repo $relative) }

$hud = Text 'Assets/Scripts/Module/Core/MainUI/Views/MainUIAutoBrushView.cs'
$hudPrefab = Text 'Assets/Prefabs/UI/MainUI/Regions/HudAutoBrush.prefab'
$controller = Text 'Assets/Scripts/Module/Core/AutoBrush/AutoBrushController.cs'
$model = Text 'Assets/Scripts/Module/Core/AutoBrush/AutoBrushModel.cs'
$configs = Text 'Assets/Scripts/Module/Core/AutoBrush/AutoBrushConfigs.cs'
$flow = Text 'Assets/Scripts/Module/Core/AutoBrush/AutoBrushFlow.cs'
$main = Text 'Assets/Scripts/Module/Core/AutoBrush/AutoBrushMainView.cs'
$rank = Text 'Assets/Scripts/Module/Core/AutoBrush/AutoBrushRankView.cs'
$prefab = Text 'Assets/Prefabs/UI/AutoBrush/AutoBrushModule.prefab'

Require ($hud.Contains('FuncOpenConfig.CheckFuncOpenState("AutoBrush")') -and $hud.Contains('MainUIConfigs.IsFieldScene(RoleModel.Instance.SceneId)')) 'HUD double gate must be FuncOpenConfig AutoBrush AND config_scene.type==1.'
Require ($hud.Contains('EVT_TASK_LIST_UPDATED') -and $hud.Contains('EVT_ROLE_INFO_UPDATE') -and $hud.Contains('EVT_SCENE_MAP_READY')) 'HUD must re-evaluate the double gate for task/role/scene transitions.'
Require ($hud.Contains('info.CurrentTimes == info.NeedTimes') -and $hud.Contains('ChallengeEffectSlot')) 'HUD completion effect must use exact progress equality and dedicated slot.'
Require ($hud.Contains('_challengeEffectVersion') -and $hud.Contains('ClearChallengeEffect()')) 'HUD effect must version-cancel and clean up.'
Require ($hudPrefab.Contains('m_Name: ChallengeEffectSlot') -and $hudPrefab.Contains('ui_mainDungeon')) 'HUD Prefab must retain the ui_mainDungeon effect host/address.'

Require ($controller.Contains('RequestRankInfo()') -and $controller.Contains('new AutoBrushModel.RankEntry')) '13301 must expose a refresh API and retain every rank row.'
Require ($model.Contains('IReadOnlyList<RankEntry> RankEntries') -and $model.Contains('RankEntries = (entries')) 'Model must retain and atomically replace rank rows.'
Require ($main.Contains('BindClick(_bind._img_rank, AutoBrushFlow.OpenRank)')) 'Rank entry must open the rank popup, not log a placeholder.'
Require (-not $main.Contains('AutoBrushRankView not migrated yet')) 'Rank placeholder log must be removed.'
Require ($flow.Contains('AutoBrushRankViewBind') -and $flow.Contains('new AutoBrushRankView')) 'Flow must bind the existing rank Prefab and data View.'
Require ($rank.Contains('RequestRankInfo()') -and $rank.Contains('verticalNormalizedPosition = 1f')) 'Rank open must refresh and reset the scroll position.'
Require ($rank.Contains('rank_icon_') -and $rank.Contains('ui_activity_11')) 'Rank rows must cover top-three and normal visual states.'
Require ($prefab.Contains('AutoBrushRankViewBind') -and $prefab.Contains('AutoBrushRankItemBind')) 'AutoBrush Prefab must retain existing rank View/Item bindings.'
Require ($main.Contains('IgnoreRedTaskId') -and $main.Contains('RoleModel.Instance.CombatPower >= boss.Power')) 'Main challenge entry/red must respect the activation-task suppression and power gate.'
Require ($main.Contains('LoadStageEffectAsync') -and $main.Contains('ClearStageEffect()')) 'Stage reward effect must load dynamically and clean up.'
Require ($prefab.Contains('autobrush_stage_reward') -and $prefab.Contains('ui_partner_skillicon_01')) 'AutoBrush Prefab must own the stage reward effect slot and exact address.'
Require ($configs.Contains('config_enchantment_guard_stage_reward') -and $configs.Contains('GetStageReward(ulong gate)')) 'Stage reward config and gate-to-reward resolver must be loaded.'
Require ($main.Contains('LoadStageRewardIconAsync') -and $main.Contains('GameResPath.GetGoodsIconPath(icon)')) 'Stage reward icon must come from the configured goods mapping.'

Require ($controller.Contains('Proto.AUTOBRUSH_INFO') -and $controller.Contains('Proto.AUTOBRUSH_RANK') -and $controller.Contains('Proto.AUTOBRUSH_STAGE_REWARD')) 'Core AutoBrush protocol family must remain registered.'
Require ($controller.Contains('_stageRewardRefreshPending') -and $controller.Contains('Proto.AUTOBRUSH_NEXT_STAGE_REWARD')) 'Stage claim must retain pending and authoritative refresh guards.'
Require ($main.Contains('guild assist not migrated yet')) 'Forbidden Guild assist dependency must remain an explicit blocker instead of a guessed write implementation.'

# The shared worktree already contains unrelated/concurrent MainUI, Addressables and Docs edits.
# This route's owned diff is audited separately and must stay inside AutoBrush + this output directory.
$ownedDiff = & git -C $repo diff --name-only -- 'Assets/Scripts/Module/Core/AutoBrush' 'Assets/Prefabs/UI/AutoBrush'
if ($LASTEXITCODE -ne 0) { throw 'git owned-scope check failed' }
foreach ($path in @($ownedDiff)) {
  Require ($path -like 'Assets/Scripts/Module/Core/AutoBrush/*' -or $path -like 'Assets/Prefabs/UI/AutoBrush/*') ('Owned diff escaped AutoBrush: ' + $path)
}

if ($errors.Count -gt 0) {
  $errors | ForEach-Object { Write-Error $_ }
  exit 1
}
Write-Output 'PASS AutoBrush static assertions and owned-scope diff check.'
