$ErrorActionPreference = 'Stop'

$routeDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $routeDir '../../..')).Path
$auditRoot = Split-Path $routeDir -Parent
$v1Dir = Join-Path $auditRoot '2026-08-09_team_static_v1'
$v3Dir = Join-Path $auditRoot '2026-08-09_team_static_v3'
$routeTool = Join-Path $repoRoot '.agents/skills/audit-game-ui-route/scripts/route_ledger.py'

function Sha([string]$path) {
    return (Get-FileHash -Algorithm SHA256 $path).Hash.ToLowerInvariant()
}

function Require-Node($manifest, [string]$id) {
    $matches = @($manifest.nodes | Where-Object { $_.id -eq $id })
    if ($matches.Count -ne 1) { throw "Expected one topology node: $id" }
    return $matches[0]
}

# Preserve the first schema-6 record and its immutable topology contract.
$expectedV1Manifest = 'c118469eeec360a1a53eed12f160ea1e85eff38a12c01e65406b7175250188c8'
$expectedV1Ledger = 'b5baf384daf6934dc89a6f2b8078380cb581f3b432c3dabc6d299b1eccecbae3'
if ((Sha (Join-Path $v1Dir 'route-manifest.json')) -ne $expectedV1Manifest) { throw 'v1 manifest changed' }
if ((Sha (Join-Path $v1Dir 'route-ledger.json')) -ne $expectedV1Ledger) { throw 'v1 ledger changed' }

# v3 remains an immutable superseded record; v4 is a new ledger, not an edit.
$expectedV3Manifest = '152abbb166dffd14f430b23f196be20c75f7920d6998b02ca575012adc13c287'
$expectedV3Ledger = 'f382ebf951d0ab67470df9172a249698ea3e6b0d16902944d4405d944f8f2e32'
if ((Sha (Join-Path $v3Dir 'route-manifest.json')) -ne $expectedV3Manifest) { throw 'v3 manifest changed' }
if ((Sha (Join-Path $v3Dir 'route-ledger.json')) -ne $expectedV3Ledger) { throw 'v3 ledger changed' }

$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-manifest.json') | ConvertFrom-Json
if ($manifest.route -ne 'mainui.team' -or $manifest.baseline.topology_revision -ne 4) {
    throw 'v4 route identity/revision mismatch'
}

$queryId = 'mainui.team.view.invite.nearby.query'
$fanoutId = "$queryId.field-scene-fanout"
$traversalId = "$fanoutId.traversal"
$requiredIds = @(
    $queryId,
    "$queryId.current-scene-request-24053",
    $fanoutId,
    "$fanoutId.current-scene-is-field",
    "$fanoutId.clear-existing-timer",
    "$fanoutId.period-timer-0.1s",
    $traversalId,
    "$traversalId.get-all-field-scenes",
    "$traversalId.skip-current-scene",
    "$traversalId.request-each-other-scene-24053",
    "$traversalId.complete-clear-timer",
    "$queryId.remove-clear-timer"
)
foreach ($id in $requiredIds) { $null = Require-Node $manifest $id }

$forbiddenIds = @(
    "$queryId.scene-change-timer",
    "$queryId.request-24053",
    "$queryId.destroy-clear-timer"
)
foreach ($id in $forbiddenIds) {
    if (@($manifest.nodes | Where-Object { $_.id -eq $id }).Count -ne 0) {
        throw "Superseded v3 node remains in v4: $id"
    }
}

$query = Require-Node $manifest $queryId
$queryChildren = @($query.control_inventory | ForEach-Object child)
$expectedQueryChildren = @(
    "$queryId.current-scene-request-24053",
    $fanoutId,
    "$queryId.remove-clear-timer"
)
if ($query.type -ne 'page' -or $queryChildren.Count -ne 3 -or
    @($queryChildren | Where-Object { $_ -notin $expectedQueryChildren }).Count -ne 0 -or
    @($expectedQueryChildren | Where-Object { $_ -notin $queryChildren }).Count -ne 0) {
    throw 'Nearby query direct lifecycle/query inventory is wrong'
}

$fanout = Require-Node $manifest $fanoutId
$fanoutChildren = @($fanout.control_inventory | ForEach-Object child)
$expectedFanoutChildren = @(
    "$fanoutId.current-scene-is-field",
    "$fanoutId.clear-existing-timer",
    "$fanoutId.period-timer-0.1s",
    $traversalId
)
if ($fanout.type -ne 'page' -or $fanoutChildren.Count -ne 4 -or
    @($fanoutChildren | Where-Object { $_ -notin $expectedFanoutChildren }).Count -ne 0 -or
    @($expectedFanoutChildren | Where-Object { $_ -notin $fanoutChildren }).Count -ne 0) {
    throw 'Field-scene conditional fanout inventory is wrong'
}

$traversal = Require-Node $manifest $traversalId
$traversalChildren = @($traversal.control_inventory | ForEach-Object child)
$expectedTraversalChildren = @(
    "$traversalId.get-all-field-scenes",
    "$traversalId.skip-current-scene",
    "$traversalId.request-each-other-scene-24053",
    "$traversalId.complete-clear-timer"
)
if ($traversal.type -ne 'page' -or $traversalChildren.Count -ne 4 -or
    @($traversalChildren | Where-Object { $_ -notin $expectedTraversalChildren }).Count -ne 0 -or
    @($expectedTraversalChildren | Where-Object { $_ -notin $traversalChildren }).Count -ne 0) {
    throw 'GetAllFieldScene traversal inventory is wrong'
}

$parentIds = @($manifest.nodes | Where-Object { $_.parent } | ForEach-Object parent | Sort-Object -Unique)
$leaves = @($manifest.nodes | Where-Object { $_.id -notin $parentIds })
if ($manifest.nodes.Count -ne 140 -or $leaves.Count -ne 111) {
    throw "Unexpected v4 topology: nodes=$($manifest.nodes.Count) leaves=$($leaves.Count)"
}

$results = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-results.json') | ConvertFrom-Json
if ($results.nodes.Count -ne 111 -or @($results.nodes | Where-Object { $_.status -ne 'blocked' }).Count -ne 0) {
    throw 'v4 results must contain 111 blocked leaves'
}

$ledger = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-ledger.json') | ConvertFrom-Json
if ($ledger.schema -ne 6 -or $ledger.nodes.Count -ne 140 -or
    @($ledger.nodes | Where-Object { $_.status -eq 'blocked' }).Count -ne 140 -or
    @($ledger.nodes | Where-Object { $_.status -ne 'blocked' }).Count -ne 0) {
    throw 'v4 ledger must contain 140 blocked nodes and no done/runtime status'
}

& python $routeTool validate (Join-Path $routeDir 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw "route_ledger validate failed: $LASTEXITCODE" }

$staticProject = Join-Path $v1Dir 'static-check/Team.StaticCheck.csproj'
& dotnet build $staticProject --nologo --verbosity minimal -warnaserror
if ($LASTEXITCODE -ne 0) { throw "independent Team static build failed: $LASTEXITCODE" }

Write-Output 'TEAM_V4_STATIC_OK nodes=140 leaves=111 blocked_total=140 done=0 runtime=0'
Write-Output "V1_IMMUTABLE manifest=$expectedV1Manifest ledger=$expectedV1Ledger"
Write-Output "V3_SUPERSEDED_IMMUTABLE manifest=$expectedV3Manifest ledger=$expectedV3Ledger"
