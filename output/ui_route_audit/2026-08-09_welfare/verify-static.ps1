$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$controller = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Welfare/WelfareController.cs')
$model = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/Welfare/WelfareModel.cs')

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
  if (-not $text.Contains($needle)) { throw $message }
}
Assert-Contains $controller 'public void ClaimTotalCheckin(int sum)' '41705 cumulative check-in API is missing'
Assert-Contains $controller 'ClaimCheckin(int day, int retroactive)' '41704 daily/makeup API is missing'
Assert-Contains $controller 'RetroactiveCheckin(int sum) => ClaimTotalCheckin(sum);' 'Legacy 41705 API does not delegate to the cumulative check-in API'
Assert-Contains $controller 'ScheduleOnlineRedDotRefresh(scheduleVersion);' 'Online threshold red-dot schedule is missing'
Assert-Contains $controller 'if (scheduleVersion != _onlineRedScheduleVersion) return;' 'Stale online timer guard is missing'
Assert-Contains $model 'public int CurrentOnlineTime' 'Observed online-time progression is missing'
Assert-Contains $model 'OnlineObservedAt = TimeUtil.NowSec();' '41715 observation timestamp is missing'
Assert-Contains $model 'GetNextOnlineRewardDelaySeconds()' 'Next online reward threshold calculation is missing'
Assert-Contains $model 'currentOnlineTime >= WelfareConfigs.GetOnlineRewardTime(item.Id)' 'Entrance red dot still uses frozen packet time'

$welfareDiff = @(git diff --name-only -- Assets/Scripts/Module/Core/Welfare Assets/Prefabs/UI/Welfare)
$unexpected = @($welfareDiff | Where-Object { $_ -notin @('Assets/Scripts/Module/Core/Welfare/WelfareController.cs','Assets/Scripts/Module/Core/Welfare/WelfareModel.cs') })
if ($unexpected.Count -gt 0) { throw "Unexpected Welfare-island files changed: $($unexpected -join ', ')" }
if (git diff --name-only -- Assets/Prefabs/UI/Welfare) { throw 'No Welfare Prefab was authorized or available for editing' }

$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'route-manifest.json') | ConvertFrom-Json
$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'results-static.json') | ConvertFrom-Json
& python (Join-Path $repo '.agents/skills/audit-game-ui-route/scripts/route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'route-ledger validate failed' }
Write-Output 'WELFARE_STATIC_VERIFY_PASS'
