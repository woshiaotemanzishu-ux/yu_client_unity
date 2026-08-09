$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

function Read-RepoFile([string]$relativePath) {
    return Get-Content -Raw -Encoding UTF8 (Join-Path $repo $relativePath)
}

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) { throw $message }
}

function Assert-ScrollAxes([string]$prefab, [string]$fileId, [int]$horizontal, [int]$vertical) {
    $pattern = "(?s)--- !u!114 &$fileId\r?\nMonoBehaviour:.*?(?=--- !u!|\z)"
    $match = [regex]::Match($prefab, $pattern)
    if (-not $match.Success) { throw "ScrollRect component not found: $fileId" }
    if ($match.Value -notmatch "m_Horizontal: $horizontal(\r?\n)" -or
        $match.Value -notmatch "m_Vertical: $vertical(\r?\n)") {
        throw "ScrollRect axes mismatch: $fileId expected H=$horizontal V=$vertical"
    }
}

$flow = Read-RepoFile 'Assets/Scripts/Module/Core/Map/MapFlow.cs'
$area = Read-RepoFile 'Assets/Scripts/Module/Core/Map/Views/AreaMapView.cs'
$world = Read-RepoFile 'Assets/Scripts/Module/Core/Map/Views/WorldMapView.cs'
$worldItem = Read-RepoFile 'Assets/Scripts/Module/Core/Map/Views/WorldMapItem.cs'
$waypoint = Read-RepoFile 'Assets/Scripts/Module/Core/Map/Views/AreaMapWayPonitItem.cs'
$prefab = Read-RepoFile 'Assets/Prefabs/UI/Map/MapModule.prefab'

Assert-Contains $flow 'ShowExclusive(ResolvePage(_pendingViewType) ?? _areaView)' 'Cold open no longer defaults to AreaMapView.'
Assert-Contains $flow 'if (_areaView != null && _areaView != target) _areaView.Hide();' 'Area/world exclusivity is missing.'
Assert-Contains $flow 'GameResPath.GetClientConfigPath("ClientWorldMapConfig")' 'World config load is missing.'
Assert-Contains $flow 'GameResPath.GetClientConfigPath("ClientMapConfig")' 'Area config load is missing.'
Assert-Contains $flow 'row["point_list"] is JArray list' 'Area point_list parsing is missing.'
Assert-Contains $flow 'row["root_pos"]' 'World root_pos parsing is missing.'
Assert-Contains $area 'Task.WhenAll(MapConfigs.EnsureLoaded(), MonsterConfigs.EnsureLoaded(), NpcConfigs.EnsureLoaded())' 'Area read dependencies are missing.'
Assert-Contains $area 'for (int i = 0; i < _points.Count; i++) _points[i].Hide();' 'Area clone hide lifecycle is missing.'
Assert-Contains $world 'for (int i = 0; i < _items.Count; i++) _items[i].Hide();' 'World clone hide lifecycle is missing.'
Assert-Contains $worldItem 'internal void SetData(MapConfigs.WorldEntry data' 'World item API accessibility drifted.'
Assert-Contains $worldItem 'GameResPath.GetFilePath("map/world_map_img", image)' 'World city resource path is missing.'
$expectedHighLevel = ([char]0x795E).ToString() + [char]0x521B
$obsoleteHighLevel = ([char]0x795E).ToString() + [char]0x52AB
if (-not $worldItem.Contains($expectedHighLevel)) { throw 'World level wording no longer matches the current old client.' }
if ($worldItem.Contains($obsoleteHighLevel)) { throw 'Obsolete world level wording remains.' }
Assert-Contains $worldItem 'Mathf.PingPong((Time.unscaledTime - _markerStartTime) * 60f, 30f)' 'Current-location motion is missing.'
Assert-Contains $waypoint 'end_pos.gameObject.SetActive(destination)' 'Destination waypoint state is missing.'

$mapCode = (Get-ChildItem (Join-Path $repo 'Assets/Scripts/Module/Core/Map') -Recurse -Filter '*.cs' |
    Get-Content -Raw -Encoding UTF8) -join "`n"
if ($mapCode -match '\b12001\b|\b12005\b|RequestChangeScene|SendMoveRequest') {
    throw 'Unauthorized movement/scene-change write path found in Map code.'
}
if (-not $area.Contains('private async Task RefreshAsync') -or
    -not $world.Contains('private async Task RefreshAsync')) {
    throw 'Legacy TODO-only Map implementation remains.'
}

Assert-ScrollAxes $prefab '3389374986221318149' 1 0
Assert-ScrollAxes $prefab '7110672666693282998' 1 1
Assert-ScrollAxes $prefab '2580218995293503601' 1 0
Assert-ScrollAxes $prefab '336411688516111074' 0 0

$manifest = Read-RepoFile 'output/ui_route_audit/2026-08-09_map/route-manifest.json' | ConvertFrom-Json
$ledger = Read-RepoFile 'output/ui_route_audit/2026-08-09_map/route-ledger.json' | ConvertFrom-Json
if ($manifest.route -ne 'mainui.map' -or $manifest.nodes.Count -ne 49) { throw 'Map manifest topology drifted.' }
if ($ledger.schema -ne 6 -or $ledger.route -ne 'mainui.map' -or $ledger.nodes.Count -ne 49) { throw 'Map schema6 ledger drifted.' }

Write-Output 'PASS Map static verification: flow/config/lifecycle/write-guard/prefab axes/schema6.'
