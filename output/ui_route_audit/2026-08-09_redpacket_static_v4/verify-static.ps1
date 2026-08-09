$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$mainText = [IO.File]::ReadAllText((Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\Views\RedPacketMainView.cs'))
$flowText = [IO.File]::ReadAllText((Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\RedPacketFlow.cs'))
$controllerText = [IO.File]::ReadAllText((Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\RedPacketController.cs'))
$protoText = [IO.File]::ReadAllText((Join-Path $repo 'Assets\Scripts\Framework\Net\Proto.cs'))
$prefabText = [IO.File]::ReadAllText((Join-Path $repo 'Assets\Prefabs\UI\RedPacket\RedPacketModule.prefab'))

$onShow = [regex]::Match($mainText, 'protected override void OnShow\(object args\)\s*\{(?<body>[\s\S]*?)\n\s*\}')
if (-not $onShow.Success) { throw 'RedPacketMainView.OnShow not found' }
$body = $onShow.Groups['body'].Value
$requestIndex = $body.IndexOf('RequestList()', [StringComparison]::Ordinal)
$tabIndex = $body.IndexOf('SwitchTab(0)', [StringComparison]::Ordinal)
if ($requestIndex -lt 0 -or $tabIndex -lt 0 -or $requestIndex -ge $tabIndex) { throw 'OnShow must request 33901 before fixed SwitchTab(0)' }
foreach ($needle in @('protected override void OnHide()', 'protected override void OnDispose()', 'PrepareForRelease()', 'SetListening(false)')) {
  if (-not $mainText.Contains($needle)) { throw "main view lifetime guard missing: $needle" }
}
foreach ($needle in @('_generation', '_generation++', 'try', 'finally', 'generation != _generation', 'PrepareForRelease()', 'ReleaseInstance(root)')) {
  if (-not $flowText.Contains($needle)) { throw "flow cancellation guard missing: $needle" }
}
foreach ($forbidden in @('RequestOpen(', 'RequestSend(', 'RequestSendVip(')) {
  if ($mainText.Contains($forbidden)) { throw "transaction call leaked into main view: $forbidden" }
}
foreach ($active in @('Proto.REDPACKET_ERROR', 'Proto.REDPACKET_LIST', 'Proto.REDPACKET_OPEN', 'Proto.REDPACKET_SEND', 'Proto.REDPACKET_SEND_VIP', 'Proto.REDPACKET_NEW_PUSH', 'Proto.REDPACKET_TAKEN_PUSH')) {
  if (-not $controllerText.Contains("RegisterProtocal($active")) { throw "active protocol registration missing: $active" }
}
if ($controllerText -match 'RegisterProtocal\([^\r\n]*(33903|33905)') { throw '33903/33905 must remain unregistered' }
if ($protoText -match 'const\s+int[^\r\n]*=\s*(33903|33905)\s*;') { throw '33903/33905 constants must remain absent' }
if (-not $prefabText.Contains('Shenxiao.Module.Core::Shenxiao.Module.Core.RedPacket.RedPacketMainView')) { throw 'Prefab runtime main view binding missing' }

dotnet build (Join-Path $PSScriptRoot 'RedPacket.StaticCompile.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw 'output-only RedPacket v4 compile failed' }
python (Join-Path $repo '.agents\skills\audit-game-ui-route\scripts\route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'RedPacket v4 schema6 ledger validation failed' }

$ledger = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'route-ledger.json')) | ConvertFrom-Json
$nodes = @($ledger.nodes)
$blockedCount = @($nodes | Where-Object status -eq 'blocked').Count
$runtimeCount = @($nodes | Where-Object status -eq 'needs-runtime-verify').Count
if ($ledger.route -ne 'mainui.red-packet' -or $nodes.Count -ne 65 -or $blockedCount -ne 53 -or $runtimeCount -ne 12) {
  throw "v4 status mismatch: route=$($ledger.route) total=$($nodes.Count) blocked=$blockedCount runtime=$runtimeCount"
}
$parentIds = @{}
foreach ($node in $nodes) { if ($null -ne $node.parent) { $parentIds[[string]$node.parent] = $true } }
$leaves = @($nodes | Where-Object { -not $parentIds.ContainsKey([string]$_.id) })
$leafBlocked = @($leaves | Where-Object status -eq 'blocked').Count
$leafRuntime = @($leaves | Where-Object status -eq 'needs-runtime-verify').Count
if ($leaves.Count -ne 56 -or $leafBlocked -ne 44 -or $leafRuntime -ne 12) { throw "v4 leaf mismatch: total=$($leaves.Count) blocked=$leafBlocked runtime=$leafRuntime" }
$expectedRuntime = @(
  'mainui.red-packet.route-state.cold-warm',
  'mainui.red-packet.route-state.disconnect-reset',
  'mainui.red-packet.route-state.error',
  'mainui.red-packet.route-state.late-arrival',
  'mainui.red-packet.route-state.startup-read',
  'mainui.red-packet.route-state.subscription-unbind',
  'mainui.red-packet.route-state.viewports',
  'mainui.red-packet.shell.identity',
  'mainui.red-packet.shell.instruction',
  'mainui.red-packet.tabs.default-rule',
  'mainui.red-packet.tabs.function',
  'mainui.red-packet.tabs.record'
) | Sort-Object
$actualRuntime = @($leaves | Where-Object status -eq 'needs-runtime-verify' | ForEach-Object { [string]$_.id } | Sort-Object)
if (@(Compare-Object $expectedRuntime $actualRuntime).Count -ne 0) { throw 'v4 runtime leaf set mismatch' }
$byId = @{}
foreach ($node in $nodes) { $byId[[string]$node.id] = $node }
foreach ($id in @('mainui.red-packet.route-state.disconnect-reset','mainui.red-packet.route-state.late-arrival','mainui.red-packet.route-state.subscription-unbind')) {
  if ($byId[$id].status -ne 'needs-runtime-verify') { throw "lifecycle leaf must need runtime: $id" }
}
if ($byId['mainui.red-packet.route-state.kill-absent'].status -ne 'blocked') { throw 'kill-absent must remain blocked' }
foreach ($blocked in @($nodes | Where-Object status -eq 'blocked')) {
  if ($null -ne $blocked.runtime_gap -and -not [string]::IsNullOrWhiteSpace([string]$blocked.runtime_gap)) { throw "blocked node retains runtime_gap: $($blocked.id)" }
}
'REDPACKET_V4_STATIC_VERIFY_PASS'

