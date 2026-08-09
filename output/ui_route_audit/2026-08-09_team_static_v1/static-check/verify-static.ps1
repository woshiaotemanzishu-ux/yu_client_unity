$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$viewPath = Join-Path $repoRoot 'Assets/Scripts/Module/Core/Team/Views/TeamHallItem.cs'
$metaPath = "$viewPath.meta"
$prefabPath = Join-Path $repoRoot 'Assets/Prefabs/UI/Team/TeamHallItem.prefab'
$routeDir = Split-Path $PSScriptRoot -Parent

$source = Get-Content -Raw -Encoding UTF8 $viewPath
$forbidden = @('RequestJoinTeam', 'RequestInvite', 'UIUtil.AddClick', 'OnClickApply')
foreach ($pattern in $forbidden) {
    if ($source.Contains($pattern)) {
        throw "Forbidden TeamHallItem write binding token: $pattern"
    }
}

foreach ($required in @(
    'HideHead();',
    '_renderVersion != renderVersion',
    'item.gameObject.SetActive(false);',
    'item.gameObject.SetActive(true);',
    'item.Show();'
)) {
    if (-not $source.Contains($required)) {
        throw "Missing TeamHallItem reuse guard: $required"
    }
}

$metaGuidMatch = Select-String -Path $metaPath -Pattern '^guid: ([0-9a-f]{32})$'
if (-not $metaGuidMatch) { throw 'TeamHallItem.cs.meta has no valid guid' }
$metaGuid = $metaGuidMatch.Matches[0].Groups[1].Value
$prefab = Get-Content -Raw -Encoding UTF8 $prefabPath
if (-not $prefab.Contains("guid: $metaGuid")) {
    throw 'TeamHallItem prefab script guid mismatch'
}
if (-not $prefab.Contains('m_EditorClassIdentifier: Shenxiao.Module.Core::Shenxiao.Module.Core.Team.TeamHallItem')) {
    throw 'TeamHallItem prefab class identifier mismatch'
}

$results = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-results.json') | ConvertFrom-Json
if ($results.nodes.Count -ne 68) { throw "Unexpected leaf result count: $($results.nodes.Count)" }
if (@($results.nodes | Where-Object status -eq 'needs-runtime-verify').Count -ne 1) {
    throw 'Expected exactly one needs-runtime-verify leaf result'
}
if (@($results.nodes | Where-Object status -eq 'blocked').Count -ne 67) {
    throw 'Expected exactly 67 blocked leaf results'
}

$ledger = Get-Content -Raw -Encoding UTF8 (Join-Path $routeDir 'route-ledger.json') | ConvertFrom-Json
if ($ledger.schema -ne 6 -or $ledger.nodes.Count -ne 84) { throw 'Unexpected Team schema/topology' }
if (@($ledger.nodes | Where-Object status -eq 'needs-runtime-verify').Count -ne 1) {
    throw 'Expected exactly one needs-runtime-verify ledger node'
}
if (@($ledger.nodes | Where-Object status -eq 'blocked').Count -ne 83) {
    throw 'Expected exactly 83 blocked ledger nodes after parent rollup'
}
if (@($ledger.nodes | Where-Object status -eq 'done').Count -ne 0) {
    throw 'Team ledger must not contain done nodes in a static-only run'
}

Write-Output "TEAM_STATIC_OK nodes=84 leaves=68 blocked_leaves=67 runtime_leaves=1 guid=$metaGuid"
