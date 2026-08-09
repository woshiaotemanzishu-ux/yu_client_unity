$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$flow = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/FriendInvite/FriendInviteFlow.cs')
$view = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/FriendInvite/Views/FriendInviteMainView.cs')
$controller = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Scripts/Module/Core/FriendInvite/FriendInviteController.cs')
$prefab = Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'Assets/Prefabs/UI/FriendInvite/FriendInviteModule.prefab')
function Need([string]$text, [string]$needle, [string]$message) { if (-not $text.Contains($needle)) { throw $message } }
Need $flow 'GetUIPrefab(Module, Prefab)' 'FriendInvite prefab route missing'
Need $view 'FriendInviteController.Instance.RequestStartup();' 'FriendInvite read refresh missing'
Need $view 'blocked-no-transaction-send' 'FriendInvite transaction block missing'
Need $controller 'SendEmpty(Proto.FRIENDINVITE_INFO);' '34001 startup missing'
Need $controller 'RequestWelfareInfo(FriendInviteModel.WelfareType);' '34012 startup missing'
Need $controller 'RequestHelpInfo();' '34005 startup missing'
Need $controller 'RequestLevelInfo();' '34006 startup missing'
Need $controller 'RequestBoostInfo(FriendInviteModel.BoostLevelKey);' '34008 startup missing'
Need $prefab 'guid: 692b01f580674c40b0d8e56387452cf6' 'FriendInviteMainView script not bound in prefab'
Need $prefab 'Shenxiao.Module.Core.FriendInvite.FriendInviteMainView' 'FriendInvite runtime type identity missing'
foreach ($forbidden in @('FRIENDINVITE_SHARE','FRIENDINVITE_DAILY_CLAIM','FRIENDINVITE_CLAIM','FRIENDINVITE_LEVEL_CLAIM','FRIENDINVITE_BOOST_CLAIM')) {
  if ($controller.Contains($forbidden)) { throw "Forbidden transaction constant present: $forbidden" }
}
$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'route-manifest.json') | ConvertFrom-Json
$null = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'results-static.json') | ConvertFrom-Json
& dotnet build (Join-Path $PSScriptRoot 'FriendInvite.StaticCompile.csproj') --nologo --no-restore
if ($LASTEXITCODE -ne 0) { throw 'output-only FriendInvite compile failed' }
& python (Join-Path $repo '.agents/skills/audit-game-ui-route/scripts/route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'route ledger validation failed' }
Write-Output 'FRIENDINVITE_STATIC_VERIFY_PASS'
