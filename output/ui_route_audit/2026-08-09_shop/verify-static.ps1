$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$prefab = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Prefabs/UI/Shop/ShopModule.prefab')
$flow = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Shop/ShopFlow.cs')
$common = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Shop/Views/ShopCommonView.cs')
$bulk = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Shop/Views/ShopBulkPurchaseView.cs')
$item = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Shop/Views/ShopItem.cs')
$limit = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Shop/Views/ShopLimitItem.cs')

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
  if (-not $text.Contains($needle)) { throw $message }
}
Assert-Contains $flow 'return index != 3;' 'Rush hard gate is missing'
Assert-Contains $flow 'backgroundImages[9] = GameResPath.GetBigBgPath("ui_bg_1.jpg")' 'Longlang background override is missing'
Assert-Contains $flow 'OpenBulkPurchase(ShopModel.GoodsVo goods)' 'Bulk popup route is missing'
Assert-Contains $common 'tab.Show();' 'Series clone does not use BaseView.Show'
Assert-Contains $common 'if (cell != null) cell.Show();' 'Goods clone does not use BaseView.Show'
Assert-Contains $common 'verticalNormalizedPosition = 1f;' 'Shop list reset-to-top is missing'
Assert-Contains $item 'Mathf.RoundToInt' 'ShopItem still truncates discounted price'
Assert-Contains $limit 'Mathf.RoundToInt' 'ShopLimitItem still truncates discounted price'
Assert-Contains $bulk 'ShopController.Instance.BuyGoods(_vo.KeyId, _count);' 'Bulk confirm protocol route is missing'
Assert-Contains $prefab 'guid: 8f43b26296c44e31b7d65a0f3eae7d91' 'Bulk runtime script is not bound in prefab'
Assert-Contains $prefab 'm_EditorClassIdentifier: Shenxiao.Module.Core::Shenxiao.Module.Core.Shop.ShopBulkPurchaseView' 'Bulk runtime type identity is missing'
foreach ($id in 1..6) {
  $needle = ('91008090000000000' + $id)
  if (([regex]::Matches($prefab, [regex]::Escape($needle))).Count -ne 2) { throw "Prefab layout component $needle must have one reference and one definition" }
}
if (([regex]::Matches($prefab, 'UnityEngine.UI::UnityEngine.UI.GridLayoutGroup')).Count -lt 2) { throw 'Two Shop grid layouts are required' }
if (([regex]::Matches($prefab, 'UnityEngine.UI::UnityEngine.UI.ContentSizeFitter')).Count -lt 3) { throw 'Three Shop content fitters are required' }
if (([regex]::Matches($prefab, 'UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup')).Count -lt 1) { throw 'Series horizontal layout is required' }

$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'route-manifest.json') | ConvertFrom-Json
$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'results-static.json') | ConvertFrom-Json
& python (Join-Path $repo '.agents/skills/audit-game-ui-route/scripts/route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'route-ledger validate failed' }
Write-Output 'SHOP_STATIC_VERIFY_PASS'
