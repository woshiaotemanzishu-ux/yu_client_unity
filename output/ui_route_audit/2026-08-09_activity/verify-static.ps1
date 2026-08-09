$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$prefab = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Prefabs/UI/Activity/ActivityModule.prefab')
$accumPrefab = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Prefabs/UI/Activity/AccumRechargeItem.prefab')
$dailyPrefab = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Prefabs/UI/Activity/DailySupplyItem.prefab')
$flow = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Activity/ActivityFlow.cs')
$bootstrap = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Activity/ActivityBootstrap.cs')

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
  if (-not $text.Contains($needle)) { throw $message }
}
Assert-Contains $bootstrap 'RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)' 'Activity bootstrap is not installed'
Assert-Contains $bootstrap 'MainUIRouter.Register("331@109"' 'Safe continuous-recharge route is missing'
Assert-Contains $flow 'else view.gameObject.SetActive(false);' 'Initially active sibling pages are not disabled'
Assert-Contains $flow 'StringComparison.OrdinalIgnoreCase' 'Legacy lowercase rechargeReturnView key is not normalized'
Assert-Contains $flow 'target.Show(info);' 'Target page does not use BaseView.Show'
Assert-Contains $prefab 'Shenxiao.Module.Core.Activity.AccumRechargeView' 'AccumRechargeView runtime identity missing'
Assert-Contains $prefab 'Shenxiao.Module.Core.Activity.ConRechargeView' 'ConRechargeView runtime identity missing'
Assert-Contains $prefab 'Shenxiao.Module.Core.Activity.DailySupplyView' 'DailySupplyView runtime identity missing'
Assert-Contains $prefab 'Shenxiao.Module.Core.Activity.CreatRoleGiftView' 'CreatRoleGiftView runtime identity missing'
Assert-Contains $prefab 'Shenxiao.Module.Core.Activity.RechargeReturnView' 'RechargeReturnView runtime identity missing'
Assert-Contains $accumPrefab 'Shenxiao.Module.Core.Activity.AccumRechargeItem' 'AccumRechargeItem runtime identity missing'
Assert-Contains $dailyPrefab 'Shenxiao.Module.Core.Activity.DailySupplyItem' 'DailySupplyItem runtime identity missing'
foreach ($id in 101..106) {
  $needle = '900000000000000' + $id
  if (([regex]::Matches($prefab, [regex]::Escape($needle))).Count -ne 2) { throw "Prefab layout component $needle must have one reference and one definition" }
}
if (([regex]::Matches($prefab, 'UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup')).Count -lt 3) { throw 'Three Activity page vertical layouts are required' }
if (([regex]::Matches($prefab, 'UnityEngine.UI::UnityEngine.UI.ContentSizeFitter')).Count -lt 3) { throw 'Three Activity page content fitters are required' }
if (([regex]::Matches($prefab, 'm_Father: \{fileID: 2845023754897150411\}')).Count -lt 5) { throw 'Five direct Activity page roots must remain under ActivityModule' }
if (git diff --name-only -- Assets/Scripts/Generated/UI/Activity) { throw 'Generated Activity bindings were modified' }
$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'route-manifest.json') | ConvertFrom-Json
$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'results-static.json') | ConvertFrom-Json
& python (Join-Path $repo '.agents/skills/audit-game-ui-route/scripts/route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'route-ledger validate failed' }
Write-Output 'ACTIVITY_STATIC_VERIFY_PASS'
