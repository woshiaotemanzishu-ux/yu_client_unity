$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$view = Join-Path $repo 'Assets\Scripts\Module\Core\FriendInvite\Views\FriendInviteMainView.cs'
$flow = Join-Path $repo 'Assets\Scripts\Module\Core\FriendInvite\FriendInviteFlow.cs'
$controller = Join-Path $repo 'Assets\Scripts\Module\Core\FriendInvite\FriendInviteController.cs'
$proto = Join-Path $repo 'Assets\Scripts\Framework\Net\Proto.cs'

$viewText = [IO.File]::ReadAllText($view)
$flowText = [IO.File]::ReadAllText($flow)
$controllerText = [IO.File]::ReadAllText($controller)
$protoText = [IO.File]::ReadAllText($proto)

$onShow = [regex]::Match($viewText, 'protected override void OnShow\(object args\)\s*\{(?<body>[\s\S]*?)\n\s*\}')
if (-not $onShow.Success) { throw 'FriendInviteMainView.OnShow not found' }
if ($onShow.Groups['body'].Value.Contains('RequestStartup')) { throw 'OnShow must not repeat GAME_START RequestStartup' }
foreach ($needle in @('SetListening(true)', 'RefreshSnapshot()')) {
  if (-not $onShow.Groups['body'].Value.Contains($needle)) { throw "OnShow missing $needle" }
}
foreach ($needle in @('protected override void OnHide()', 'protected override void OnDispose()', 'PrepareForRelease()', 'SetListening(false)', 'EventDispatcher.Off(FriendInviteModel.EVENT_UPDATED')) {
  if (-not $viewText.Contains($needle)) { throw "view lifetime guard missing: $needle" }
}
foreach ($needle in @('_generation', '_generation++', 'try', 'finally', 'generation != _generation', 'PrepareForRelease()', 'ReleaseInstance(root)')) {
  if (-not $flowText.Contains($needle)) { throw "flow cancellation guard missing: $needle" }
}
if (-not $controllerText.Contains('public void RequestStartup()')) { throw 'GAME_START startup sequence entry missing' }
if ($protoText -match 'const\s+int[^\r\n]*=\s*(34010|34011|11301|11302)\s*;') { throw '34010/34011/11301/11302 must remain absent' }

dotnet build (Join-Path $PSScriptRoot 'FriendInvite.StaticCompile.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw 'output-only FriendInvite compile failed' }
python (Join-Path $repo '.agents\skills\audit-game-ui-route\scripts\route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'FriendInvite v3 schema6 ledger validation failed' }
$ledger = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'route-ledger.json')) | ConvertFrom-Json
$nodes = @($ledger.nodes)
$blockedCount = @($nodes | Where-Object status -eq 'blocked').Count
$runtimeCount = @($nodes | Where-Object status -eq 'needs-runtime-verify').Count
if ($nodes.Count -ne 83 -or $blockedCount -ne 72 -or $runtimeCount -ne 11) {
  throw "FriendInvite v3 status mismatch: total=$($nodes.Count) blocked=$blockedCount runtime=$runtimeCount"
}
$parentIds = @{}
foreach ($node in $nodes) { if ($null -ne $node.parent) { $parentIds[[string]$node.parent] = $true } }
$leaves = @($nodes | Where-Object { -not $parentIds.ContainsKey([string]$_.id) })
$leafBlocked = @($leaves | Where-Object status -eq 'blocked').Count
$leafRuntime = @($leaves | Where-Object status -eq 'needs-runtime-verify').Count
if ($leaves.Count -ne 68 -or $leafBlocked -ne 57 -or $leafRuntime -ne 11) {
  throw "FriendInvite v3 leaf mismatch: total=$($leaves.Count) blocked=$leafBlocked runtime=$leafRuntime"
}
$sounds = @($nodes | Where-Object id -eq 'mainui.friend-invite.route-state.sounds')
if ($sounds.Count -ne 1 -or $sounds[0].status -ne 'blocked' -or $null -ne $sounds[0].runtime_gap) { throw 'FriendInvite sounds leaf must be blocked with empty runtime_gap' }
if (-not (Test-Path (Join-Path $PSScriptRoot 'results-static-qa-sounds.json'))) { throw 'FriendInvite sounds correction batch missing' }
'FRIENDINVITE_V3_STATIC_VERIFY_PASS'
