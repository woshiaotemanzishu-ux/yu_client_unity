$ErrorActionPreference = 'Stop'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "ASSERT FAILED: $message" }
    Write-Output "PASS $message"
}

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$manifestPath = Join-Path $PSScriptRoot 'route-manifest.json'
$ledgerPath = Join-Path $PSScriptRoot 'route-ledger.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$ledger = Get-Content -LiteralPath $ledgerPath -Raw | ConvertFrom-Json

Assert-True ($manifest.route -eq 'mainui.role.outward') 'route identity'
Assert-True ($ledger.schema -eq 6) 'schema 6 ledger'
Assert-True ($manifest.nodes.Count -eq 234) 'complete frozen topology has 234 nodes'
Assert-True ($ledger.nodes.Count -eq $manifest.nodes.Count) 'ledger topology matches manifest'
Assert-True ((@($ledger.nodes | Where-Object { $_.status -eq 'not-run' })).Count -eq 0) 'all frozen nodes have an explicit current status'
Assert-True ((@($ledger.nodes | Where-Object { $_.status -eq 'needs-runtime-verify' })).Count -eq 91) '91 nodes are implemented but runtime-gated'
Assert-True ((@($ledger.nodes | Where-Object { $_.status -eq 'blocked' })).Count -eq 143) '143 nodes are explicitly blocked'

$modelSource = Get-Content -LiteralPath (Join-Path $root 'Assets\Scripts\Module\Core\OutWard\OutWardModel.cs') -Raw
$viewSource = Get-Content -LiteralPath (Join-Path $root 'Assets\Scripts\Module\Core\Pet\Views\OutWardBaseView.cs') -Raw
$prefab = Get-Content -LiteralPath (Join-Path $root 'Assets\Prefabs\UI\Pet\PetModule.prefab') -Raw
$prop = Get-Content -LiteralPath (Join-Path $root 'Assets\GameRes\resource\config\server\config_mount_prop.json') -Raw
$goods = Get-Content -LiteralPath (Join-Path $root 'Assets\GameRes\resource\config\server\config_mount_goods.json') -Raw

Assert-True ($modelSource.Contains('config_mount_prop')) 'training material loader uses config_mount_prop'
Assert-True ($modelSource.Contains('GetCrystalGoodsIds')) 'crystal config has an independent accessor'
Assert-True ($modelSource.Contains('GetStageModelRes')) 'stage model accessor is present'

$expected = @(
    @(3, 18020001, 18010001),
    @(4, 19020001, 19010001),
    @(5, 20020001, 20010001),
    @(12, 25020001, 25010001)
)
foreach ($row in $expected) {
    $type = $row[0]; $train = $row[1]; $crystal = $row[2]
    Assert-True ($prop.Contains('"type_id":' + $type + ',"goods_id":' + $train + ',"type":1')) "type $type train mapping"
    Assert-True ($goods.Contains('"type_id":' + $type + ',"goods_id":' + $crystal)) "type $type crystal mapping"
}

foreach ($fragment in @(
    'case 3: module = "wing"; prefix = "w"; fallback = "default_wing"',
    'case 4: module = "fabao"; prefix = "a"; fallback = "default_artifact"',
    'case 5: module = "weapon"; prefix = "d"; fallback = "default_weapon"',
    'case 12: module = "back"; prefix = "b"; fallback = "default_back_ornament"'
)) { Assert-True ($viewSource.Contains($fragment)) "model mapping $fragment" }

Assert-True ($viewSource.Contains('new UIModelStage()')) 'one reusable dedicated model stage implementation'
Assert-True ($viewSource.Contains('EffectBinder.AttachAlways')) 'model always-effect binding is implemented'
Assert-True ($viewSource.Contains('HideNode(star_group)')) 'role outward star strip is hidden'
Assert-True ($viewSource.Contains('HideNode(before_btn)')) 'role outward browse arrows are hidden'
Assert-True ($viewSource.Contains('lv_button_text.text = "一键提升"')) 'role outward primary action label is fixed'
Assert-True ($viewSource.Contains('vo.FigureStage == vo.Stage')) 'base appearance using/unuse state is driven by authoritative data'
Assert-True (([regex]::Matches($prefab, 'value: PetRoundItem_crystal[0-2]')).Count -eq 3) 'prefab has three crystal slots'
Assert-True ($prefab.Contains('m_Name: material_group')) 'prefab has training material group'
Assert-True ($prefab.Contains('m_Name: res')) 'prefab has 3D model host'
Assert-True (-not $prefab.Contains('m_Name: IllusionBaseView')) 'illusion subview is not falsely claimed as baked prefab'
Assert-True (-not $prefab.Contains('m_Name: OutwardLvSystem')) 'level subview is not falsely claimed as baked prefab'
Assert-True (-not $prefab.Contains('m_Name: PetProptityView')) 'property subview is not falsely claimed as baked prefab'

Write-Output 'STATIC_ROLE_OUTWARD_OK'
