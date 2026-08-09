$ErrorActionPreference = 'Stop'

$routeDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $routeDir '../../..')).Path
$routeTool = Join-Path $repoRoot '.agents/skills/audit-game-ui-route/scripts/route_ledger.py'
$auditRoot = Split-Path $routeDir -Parent
$v1Dir = Join-Path $auditRoot '2026-08-09_team_static_v1'
$v2Dir = Join-Path $auditRoot '2026-08-09_team_static_v2'
$hallViewPath = Join-Path $repoRoot 'Assets/Scripts/Module/Core/Team/Views/TeamHallItem.cs'
$mainRoleViewPath = Join-Path $repoRoot 'Assets/Scripts/Module/Core/Team/Views/TeamMainRoleItem.cs'
$metaPath = "$hallViewPath.meta"
$prefabPath = Join-Path $repoRoot 'Assets/Prefabs/UI/Team/TeamHallItem.prefab'

function Get-LowerSha256([string]$path) {
    return (Get-FileHash -Algorithm SHA256 $path).Hash.ToLowerInvariant()
}

function Require-Node($manifest, [string]$id) {
    $matches = @($manifest.nodes | Where-Object { $_.id -eq $id })
    if ($matches.Count -ne 1) { throw "Expected exactly one topology node: $id" }
    return $matches[0]
}

# The first schema-6 topology remains a byte-identifiable superseded record.
$expectedV1ManifestSha = 'c118469eeec360a1a53eed12f160ea1e85eff38a12c01e65406b7175250188c8'
$expectedV1LedgerSha = 'b5baf384daf6934dc89a6f2b8078380cb581f3b432c3dabc6d299b1eccecbae3'
$actualV1ManifestSha = Get-LowerSha256 (Join-Path $v1Dir 'route-manifest.json')
$actualV1LedgerSha = Get-LowerSha256 (Join-Path $v1Dir 'route-ledger.json')
if ($actualV1ManifestSha -ne $expectedV1ManifestSha) {
    throw "v1 manifest changed: $actualV1ManifestSha"
}
if ($actualV1LedgerSha -ne $expectedV1LedgerSha) {
    throw "v1 ledger changed: $actualV1LedgerSha"
}

$v1Ledger = Get-Content -Raw -Encoding UTF8 (Join-Path $v1Dir 'route-ledger.json') | ConvertFrom-Json
if ($v1Ledger.manifest_source.sha256 -ne $expectedV1ManifestSha) {
    throw 'v1 manifest_source no longer pins its preserved manifest'
}

# Assert the v2 QA additions explicitly so v3 cannot hide a regression behind
# a larger total node count.
$v2Manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $v2Dir 'route-manifest.json') | ConvertFrom-Json
$v2RequiredIds = @(
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
foreach ($id in $v2RequiredIds) { $null = Require-Node $v2Manifest $id }

$v2ParentIds = @($v2Manifest.nodes | Where-Object { $_.parent } | ForEach-Object parent | Sort-Object -Unique)
$v2LeafIds = @($v2Manifest.nodes | Where-Object { $_.id -notin $v2ParentIds } | ForEach-Object id)
if ($v2Manifest.nodes.Count -ne 127 -or $v2LeafIds.Count -ne 102) {
    throw "Unexpected preserved v2 topology: nodes=$($v2Manifest.nodes.Count) leaves=$($v2LeafIds.Count)"
}

$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-manifest.json') | ConvertFrom-Json
if ($manifest.route -ne 'mainui.team' -or $manifest.baseline.topology_revision -ne 3) {
    throw 'v3 route identity or topology revision is incorrect'
}

$v3RequiredIds = @(
    'mainui.team.view.invite.nearby.query',
    'mainui.team.view.invite.nearby.query.scene-change-timer',
    'mainui.team.view.invite.nearby.query.request-24053',
    'mainui.team.view.invite.nearby.query.destroy-clear-timer',
    'mainui.team.view.change-target.confirm',
    'mainui.team.view.change-target.confirm.existing-team-24017',
    'mainui.team.view.change-target.confirm.no-team-change-target-success'
)
foreach ($id in ($v2RequiredIds + $v3RequiredIds)) { $null = Require-Node $manifest $id }

$nearbyQuery = Require-Node $manifest 'mainui.team.view.invite.nearby.query'
if ($nearbyQuery.type -ne 'page') { throw 'Nearby refresh lifecycle must be a parent page node' }
$nearbyChildren = @($nearbyQuery.control_inventory | ForEach-Object child)
$expectedNearbyChildren = @(
    'mainui.team.view.invite.nearby.query.scene-change-timer',
    'mainui.team.view.invite.nearby.query.request-24053',
    'mainui.team.view.invite.nearby.query.destroy-clear-timer'
)
if (@($nearbyChildren | Where-Object { $_ -notin $expectedNearbyChildren }).Count -ne 0 -or
    @($expectedNearbyChildren | Where-Object { $_ -notin $nearbyChildren }).Count -ne 0 -or
    $nearbyChildren.Count -ne 3) {
    throw 'Nearby refresh lifecycle inventory is incomplete'
}

$confirm = Require-Node $manifest 'mainui.team.view.change-target.confirm'
if ($confirm.type -ne 'page') { throw 'Change-target confirm must be a branch parent' }
$confirmChildren = @($confirm.control_inventory | ForEach-Object child)
$expectedConfirmChildren = @(
    'mainui.team.view.change-target.confirm.existing-team-24017',
    'mainui.team.view.change-target.confirm.no-team-change-target-success'
)
if (@($confirmChildren | Where-Object { $_ -notin $expectedConfirmChildren }).Count -ne 0 -or
    @($expectedConfirmChildren | Where-Object { $_ -notin $confirmChildren }).Count -ne 0 -or
    $confirmChildren.Count -ne 2) {
    throw 'Change-target mutually exclusive confirm branches are incomplete'
}

$parentIds = @($manifest.nodes | Where-Object { $_.parent } | ForEach-Object parent | Sort-Object -Unique)
$leafIds = @($manifest.nodes | Where-Object { $_.id -notin $parentIds } | ForEach-Object id)
if ($manifest.nodes.Count -ne 132 -or $leafIds.Count -ne 105) {
    throw "Unexpected v3 topology: nodes=$($manifest.nodes.Count) leaves=$($leafIds.Count)"
}

$results = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-results.json') | ConvertFrom-Json
if ($results.nodes.Count -ne 105 -or @($results.nodes | Where-Object { $_.status -ne 'blocked' }).Count -ne 0) {
    throw 'v3 leaf results must contain exactly 105 blocked leaves'
}
$rowResult = @($results.nodes | Where-Object { $_.id -eq 'mainui.team.view.hall.row-render' })
if ($rowResult.Count -ne 1 -or
    -not $rowResult[0].blocked_reason.Contains('CustomHeadItem') -or
    -not $rowResult[0].blocked_reason.Contains('Common')) {
    throw 'Combined hall row-render must remain blocked on the cross-island CustomHeadItem gap'
}

$ledger = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-ledger.json') | ConvertFrom-Json
if ($ledger.schema -ne 6 -or $ledger.nodes.Count -ne 132) { throw 'Unexpected v3 ledger schema/topology' }
if (@($ledger.nodes | Where-Object { $_.status -eq 'blocked' }).Count -ne 132) {
    throw 'Expected all 132 v3 ledger nodes to roll up to blocked'
}
if (@($ledger.nodes | Where-Object { $_.status -ne 'blocked' }).Count -ne 0) {
    throw 'v3 contains a done/runtime/not-run or other impermissible status'
}

# Pure display code may consume the existing read-only scene classifier.  It
# must not bind the hall apply button or revive the 24011 avatar menu.
$hallSource = Get-Content -Raw -Encoding UTF8 $hallViewPath
$mainRoleSource = Get-Content -Raw -Encoding UTF8 $mainRoleViewPath
foreach ($required in @(
    'memberSceneId == selfSceneId',
    'MainUIConfigs.IsFieldScene(memberSceneId)',
    'MainUIConfigs.IsFieldScene(selfSceneId)',
    'HideHead();',
    '_renderVersion != renderVersion',
    'item.gameObject.SetActive(false);',
    'item.gameObject.SetActive(true);',
    'item.Show();'
)) {
    if (-not $hallSource.Contains($required)) { throw "Missing TeamHallItem static semantic: $required" }
}
foreach ($required in @(
    'vo.SceneId == selfSceneId',
    'MainUIConfigs.IsFieldScene(vo.SceneId)',
    'MainUIConfigs.IsFieldScene(selfSceneId)'
)) {
    if (-not $mainRoleSource.Contains($required)) { throw "Missing TeamMainRoleItem nearby semantic: $required" }
}
foreach ($forbidden in @('RequestJoinTeam', 'OnClickApply')) {
    if ($hallSource.Contains($forbidden)) { throw "Forbidden TeamHallItem write binding token: $forbidden" }
}

$metaGuidMatch = Select-String -Path $metaPath -Pattern '^guid: ([0-9a-f]{32})$'
if (-not $metaGuidMatch) { throw 'TeamHallItem.cs.meta has no valid guid' }
$metaGuid = $metaGuidMatch.Matches[0].Groups[1].Value
$prefab = Get-Content -Raw -Encoding UTF8 $prefabPath
if (-not $prefab.Contains("guid: $metaGuid")) { throw 'TeamHallItem prefab script guid mismatch' }
if (-not $prefab.Contains('m_EditorClassIdentifier: Shenxiao.Module.Core::Shenxiao.Module.Core.Team.TeamHallItem')) {
    throw 'TeamHallItem prefab class identifier mismatch'
}

& python $routeTool validate (Join-Path $routeDir 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw "route_ledger validate failed: $LASTEXITCODE" }

$staticProject = Join-Path $v1Dir 'static-check/Team.StaticCheck.csproj'
& dotnet build $staticProject --nologo --verbosity minimal -warnaserror
if ($LASTEXITCODE -ne 0) { throw "independent Team static build failed: $LASTEXITCODE" }

Write-Output "TEAM_V3_STATIC_OK nodes=132 leaves=105 blocked_leaves=105 blocked_total=132 done=0 runtime=0 guid=$metaGuid"
Write-Output "V1_IMMUTABLE manifest=$actualV1ManifestSha ledger=$actualV1LedgerSha"
