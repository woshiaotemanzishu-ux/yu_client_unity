$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$prefab = Join-Path $repo 'Assets\Prefabs\UI\Arena\ArenaModule.prefab'
$controller = Join-Path $repo 'Assets\Scripts\Module\Core\Jjc\JjcController.cs'
$model = Join-Path $repo 'Assets\Scripts\Module\Core\Jjc\JjcModel.cs'
$shell = Join-Path $repo 'Assets\Scripts\Module\Core\Jjc\JjcShellView.cs'

function Require-Text([string]$path, [string]$pattern, [string]$label) {
    $text = Get-Content -LiteralPath $path -Raw
    if ($text -notmatch $pattern) { throw "missing $label in $path" }
}

$requiredModuleViews = @(
    'ArenaEnterView', 'ArenaEnterRoleItem', 'ArenaBattleRecordView', 'ArenaBattleRecordItem',
    'ArenaBuyTimesView', 'ArenaRankRewardMainView', 'ArenaRankTabItem',
    'ArenaRankRewardView', 'ArenaRankBreachRewardView',
    'ArenaFightSceneView', 'ArenaResultView'
)
$prefabText = Get-Content -LiteralPath $prefab -Raw
foreach ($view in $requiredModuleViews) {
    if ($prefabText -notmatch "m_Name: $([regex]::Escape($view))(\r?\n)") {
        throw "ArenaModule missing $view"
    }
    if ($prefabText -notmatch "Shenxiao\.Generated\.UI\.Arena\.$([regex]::Escape($view))Bind") {
        throw "ArenaModule missing $($view)Bind component"
    }
}
$rewardItemPrefab = Join-Path $repo 'Assets\Prefabs\UI\Arena\ArenaRankRewardItem.prefab'
Require-Text $rewardItemPrefab 'm_Name: ArenaRankRewardItem' 'standalone ArenaRankRewardItem'
Require-Text $rewardItemPrefab 'Shenxiao\.Generated\.UI\.Arena\.ArenaRankRewardItemBind' 'standalone ArenaRankRewardItemBind component'

Require-Text $prefab 'm_Name: _scroll_con' 'opponent ScrollRect'
Require-Text $prefab 'm_Name: _Scroller1' 'secondary ScrollRect'
Require-Text $prefab 'm_Name: Viewport' 'Viewport'
Require-Text $prefab 'm_Name: Content' 'Content'
Require-Text $controller 'RegisterProtocal\(Proto\.JJC_ERROR, On28000\)' '28000 receiver'
Require-Text $controller 'RegisterProtocal\(Proto\.JJC_BATTLE_STAGE, On28014\)' '28014 receiver'
Require-Text $controller 'RequestTimesInfo\(\);\s*RequestInfo\(\);' 'GAME_START 28004 to 28001 order'
Require-Text $controller 'RequestTimesInfo\(\);\s*RequestRivals\(\);' '28003 refresh 28004 to 28002 order'
Require-Text $model 'Apply28004\(' 'independent 28004 snapshot'
Require-Text $model 'Apply28009\(' '28009 snapshot'
Require-Text $shell 'JjcShellView\(TempShell\)' 'known TEMP shell defect'
Require-Text $shell 'private static void EnsureBuilt\(' 'runtime code-built shell defect'

$expectedPrefabHash = 'f17546bfe88346d37182559b46b4b4a0bee44e665e707c8f6e47a0f336414f30'
$actualPrefabHash = (Get-FileHash -LiteralPath $prefab -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualPrefabHash -ne $expectedPrefabHash) {
    throw "ArenaModule fingerprint drift: $actualPrefabHash"
}

[pscustomobject]@{
    result = 'pass'
    requiredViews = $requiredModuleViews.Count + 1
    generatedBindFiles = (Get-ChildItem -LiteralPath (Join-Path $repo 'Assets\Scripts\Generated\UI\Arena') -Filter '*Bind.cs' -File).Count
    arenaResourceFiles = (Get-ChildItem -LiteralPath (Join-Path $repo 'Assets\GameRes\resource\game\arena') -Recurse -File).Count
    prefabSha256 = $actualPrefabHash
    tempShellDetected = $true
    buildExecuted = $false
    unityExecuted = $false
    webExecuted = $false
} | ConvertTo-Json -Depth 3
