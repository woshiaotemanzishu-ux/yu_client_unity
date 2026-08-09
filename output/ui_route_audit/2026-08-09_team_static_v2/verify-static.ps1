$ErrorActionPreference = 'Stop'

$routeDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $routeDir '../../..')).Path
$v1Dir = Join-Path (Split-Path $routeDir -Parent) '2026-08-09_team_static_v1'
$viewPath = Join-Path $repoRoot 'Assets/Scripts/Module/Core/Team/Views/TeamHallItem.cs'
$metaPath = "$viewPath.meta"
$prefabPath = Join-Path $repoRoot 'Assets/Prefabs/UI/Team/TeamHallItem.prefab'

# Prove v1 remained immutable relative to its own schema-6 manifest contract.
$v1Ledger = Get-Content -Raw -Encoding UTF8 (Join-Path $v1Dir 'route-ledger.json') | ConvertFrom-Json
$v1ManifestHash = (Get-FileHash -Algorithm SHA256 (Join-Path $v1Dir 'route-manifest.json')).Hash.ToLowerInvariant()
if ($v1Ledger.manifest_source.sha256 -ne $v1ManifestHash) {
    throw 'Preserved v1 manifest no longer matches its ledger contract'
}

$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-manifest.json') | ConvertFrom-Json
$requiredIds = @(
    'mainui.team.view.initial-query-24010',
    'mainui.team.view.button-groups.with-team',
    'mainui.team.view.button-groups.without-team',
    'mainui.team.view.non-leader-prompts.target',
    'mainui.team.view.non-leader-prompts.world-shout',
    'mainui.team.view.non-leader-prompts.apply-list',
    'mainui.team.view.world-shout-state.countdown-text',
    'mainui.team.view.world-shout-state.cooldown-click',
    'mainui.team.view.world-shout-state.expiry-reset',
    'mainui.team.view.apply.open-query-24047',
    'mainui.team.view.apply.join-type-check-state',
    'mainui.team.view.change-target.down-level-click',
    'mainui.team.view.change-target.up-level-click',
    'mainui.team.view.change-target.calculator.open-event',
    'mainui.team.view.change-target.calculator.close-callback',
    'mainui.team.view.change-target.list.row-render',
    'mainui.team.view.change-target.list.selected-state',
    'mainui.team.view.change-target.calculator.validation.min-below-config',
    'mainui.team.view.change-target.calculator.validation.min-above-config-or-current-max',
    'mainui.team.view.change-target.calculator.validation.max-above-config',
    'mainui.team.view.change-target.calculator.validation.max-below-config-or-current-min',
    'mainui.team.match.sentient-alert.open',
    'mainui.team.match.sentient-alert.cancel',
    'mainui.team.match.sentient-alert.confirm.find-way',
    'mainui.team.match.sentient-alert.confirm.request-24108'
)
$manifestIds = @($manifest.nodes | ForEach-Object id)
foreach ($requiredId in $requiredIds) {
    if ($requiredId -notin $manifestIds) { throw "Missing corrected topology node: $requiredId" }
}

$parentIds = @($manifest.nodes | Where-Object { $_.parent } | ForEach-Object parent | Sort-Object -Unique)
$leafIds = @($manifest.nodes | Where-Object { $_.id -notin $parentIds } | ForEach-Object id)
if ($manifest.nodes.Count -ne 127 -or $leafIds.Count -ne 102) {
    throw "Unexpected v2 topology: nodes=$($manifest.nodes.Count) leaves=$($leafIds.Count)"
}

$results = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-results.json') | ConvertFrom-Json
if ($results.nodes.Count -ne 102) { throw "Unexpected result count: $($results.nodes.Count)" }
if (@($results.nodes | Where-Object status -eq 'blocked').Count -ne 101) {
    throw 'Expected exactly 101 blocked leaf results'
}
if (@($results.nodes | Where-Object status -eq 'needs-runtime-verify').Count -ne 1) {
    throw 'Expected exactly one needs-runtime-verify leaf result'
}

$ledger = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-ledger.json') | ConvertFrom-Json
if ($ledger.schema -ne 6 -or $ledger.nodes.Count -ne 127) { throw 'Unexpected v2 ledger schema/topology' }
if (@($ledger.nodes | Where-Object status -eq 'blocked').Count -ne 126) {
    throw 'Expected exactly 126 blocked ledger nodes after parent rollup'
}
if (@($ledger.nodes | Where-Object status -eq 'needs-runtime-verify').Count -ne 1) {
    throw 'Expected exactly one needs-runtime-verify ledger node'
}
if (@($ledger.nodes | Where-Object { $_.status -in @('done', 'not-run', 'baseline-only', 'defect', 'fixing') }).Count -ne 0) {
    throw 'Corrected static ledger contains an impermissible completion or unresolved status'
}

# Code/Prefab are unchanged from the already-validated v1 implementation.
$source = Get-Content -Raw -Encoding UTF8 $viewPath
foreach ($forbidden in @('RequestJoinTeam', 'RequestInvite', 'UIUtil.AddClick', 'OnClickApply')) {
    if ($source.Contains($forbidden)) { throw "Forbidden TeamHallItem write binding token: $forbidden" }
}
foreach ($required in @('HideHead();', '_renderVersion != renderVersion', 'item.gameObject.SetActive(false);', 'item.gameObject.SetActive(true);', 'item.Show();')) {
    if (-not $source.Contains($required)) { throw "Missing TeamHallItem reuse guard: $required" }
}
$metaGuidMatch = Select-String -Path $metaPath -Pattern '^guid: ([0-9a-f]{32})$'
if (-not $metaGuidMatch) { throw 'TeamHallItem.cs.meta has no valid guid' }
$metaGuid = $metaGuidMatch.Matches[0].Groups[1].Value
$prefab = Get-Content -Raw -Encoding UTF8 $prefabPath
if (-not $prefab.Contains("guid: $metaGuid")) { throw 'TeamHallItem prefab script guid mismatch' }
if (-not $prefab.Contains('m_EditorClassIdentifier: Shenxiao.Module.Core::Shenxiao.Module.Core.Team.TeamHallItem')) {
    throw 'TeamHallItem prefab class identifier mismatch'
}

Write-Output "TEAM_V2_STATIC_OK nodes=127 leaves=102 blocked_leaves=101 runtime_leaves=1 blocked_total=126 guid=$metaGuid"
