$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$main = Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\Views\RedPacketMainView.cs'
$controller = Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\RedPacketController.cs'
$proto = Join-Path $repo 'Assets\Scripts\Framework\Net\Proto.cs'
$prefab = Join-Path $repo 'Assets\Prefabs\UI\RedPacket\RedPacketModule.prefab'

$mainText = [System.IO.File]::ReadAllText($main)
$controllerText = [System.IO.File]::ReadAllText($controller)
$protoText = [System.IO.File]::ReadAllText($proto)
$prefabText = [System.IO.File]::ReadAllText($prefab)
$auditText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'static-audit.md'))

if ($auditText.Contains('所有其他叶均为 `needs-runtime-verify`')) { throw 'static audit contains obsolete absolute needs-runtime claim' }

foreach ($needle in @('SwitchTab(0)', 'RequestList()', 'InstructionFlow.Show(339)', 'SetTabPagesVisible(showRecord, !showRecord)')) {
  if (-not $mainText.Contains($needle)) { throw "RedPacketMainView missing: $needle" }
}
$onShow = [regex]::Match($mainText, 'protected override void OnShow\(object args\)\s*\{(?<body>[\s\S]*?)\n\s*\}')
if (-not $onShow.Success) { throw 'RedPacketMainView.OnShow not found' }
$onShowBody = $onShow.Groups['body'].Value
$requestIndex = $onShowBody.IndexOf('RequestList()', [System.StringComparison]::Ordinal)
$tabIndex = $onShowBody.IndexOf('SwitchTab(0)', [System.StringComparison]::Ordinal)
if ($requestIndex -lt 0 -or $tabIndex -lt 0 -or $requestIndex -ge $tabIndex) {
  throw 'RedPacketMainView.OnShow must request 33901 before fixed SwitchTab(0)'
}
foreach ($forbidden in @('RequestOpen(', 'RequestSend(', 'RequestSendVip(')) {
  if ($mainText.Contains($forbidden)) { throw "transaction call leaked into RedPacketMainView: $forbidden" }
}
foreach ($active in @('Proto.REDPACKET_ERROR', 'Proto.REDPACKET_LIST', 'Proto.REDPACKET_OPEN', 'Proto.REDPACKET_SEND', 'Proto.REDPACKET_SEND_VIP', 'Proto.REDPACKET_NEW_PUSH', 'Proto.REDPACKET_TAKEN_PUSH')) {
  if (-not $controllerText.Contains("RegisterProtocal($active")) { throw "active protocol registration missing: $active" }
}
if ($controllerText -match 'RegisterProtocal\([^\r\n]*(33903|33905)') { throw '33903/33905 must remain absent' }
if ($protoText -match 'const\s+int[^\r\n]*=\s*(33903|33905)\s*;') { throw '33903/33905 constants must remain absent' }
if (-not $prefabText.Contains('Shenxiao.Module.Core::Shenxiao.Module.Core.RedPacket.RedPacketMainView')) { throw 'Prefab runtime RedPacketMainView binding missing' }

dotnet build (Join-Path $PSScriptRoot 'RedPacket.StaticCompile.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'output-only RedPacket compile failed' }
python (Join-Path $repo '.agents\skills\audit-game-ui-route\scripts\route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'RedPacket schema6 ledger validation failed' }

$ledger = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'route-ledger.json')) | ConvertFrom-Json
$nodes = @($ledger.nodes)
if ($nodes.Count -ne 62) { throw "RedPacket ledger expected 62 nodes, got $($nodes.Count)" }
$blockedCount = @($nodes | Where-Object status -eq 'blocked').Count
$runtimeCount = @($nodes | Where-Object status -eq 'needs-runtime-verify').Count
if ($blockedCount -ne 53 -or $runtimeCount -ne 9) { throw "RedPacket ledger status mismatch: blocked=$blockedCount runtime=$runtimeCount" }

$parentIds = @{}
foreach ($node in $nodes) { if ($null -ne $node.parent) { $parentIds[[string]$node.parent] = $true } }
$leaves = @($nodes | Where-Object { -not $parentIds.ContainsKey([string]$_.id) })
$leafBlocked = @($leaves | Where-Object status -eq 'blocked').Count
$leafRuntime = @($leaves | Where-Object status -eq 'needs-runtime-verify').Count
if ($leaves.Count -ne 53 -or $leafBlocked -ne 44 -or $leafRuntime -ne 9) {
  throw "RedPacket leaf status mismatch: total=$($leaves.Count) blocked=$leafBlocked runtime=$leafRuntime"
}

$expectedRuntime = @(
  'mainui.red-packet.route-state.cold-warm',
  'mainui.red-packet.route-state.error',
  'mainui.red-packet.route-state.startup-read',
  'mainui.red-packet.route-state.viewports',
  'mainui.red-packet.shell.identity',
  'mainui.red-packet.shell.instruction',
  'mainui.red-packet.tabs.default-rule',
  'mainui.red-packet.tabs.function',
  'mainui.red-packet.tabs.record'
) | Sort-Object
$actualRuntime = @($leaves | Where-Object status -eq 'needs-runtime-verify' | ForEach-Object { [string]$_.id } | Sort-Object)
$runtimeDiff = @(Compare-Object $expectedRuntime $actualRuntime)
if ($runtimeDiff.Count -ne 0) { throw "RedPacket runtime leaf set mismatch: $($runtimeDiff | Out-String)" }

$byId = @{}
foreach ($node in $nodes) { $byId[[string]$node.id] = $node }
if ($byId['mainui.red-packet.route-state.kill-absent'].status -ne 'blocked') { throw 'kill-absent must be blocked' }

$blockerPath = Join-Path $PSScriptRoot 'results-static-qa-blockers.json'
$correctionPath = Join-Path $PSScriptRoot 'results-static-qa-correction.json'
if (-not (Test-Path $blockerPath) -or -not (Test-Path $correctionPath)) { throw 'QA blocker/correction batch missing' }
$blockers = @(([IO.File]::ReadAllText($blockerPath) | ConvertFrom-Json).nodes)
$corrections = @(([IO.File]::ReadAllText($correctionPath) | ConvertFrom-Json).nodes)
if ($blockers.Count -ne 40 -or $corrections.Count -ne 40) { throw "QA batch count mismatch: blockers=$($blockers.Count) corrections=$($corrections.Count)" }
$blockerIds = @($blockers | ForEach-Object { [string]$_.id } | Sort-Object)
$correctionIds = @($corrections | ForEach-Object { [string]$_.id } | Sort-Object)
if (@(Compare-Object $blockerIds $correctionIds).Count -ne 0) { throw 'QA correction ids differ from blocker ids' }
foreach ($correction in $corrections) {
  if ($correction.status -ne 'blocked') { throw "correction status is not blocked: $($correction.id)" }
  if (-not ($correction.PSObject.Properties.Name -contains 'runtime_gap') -or $null -ne $correction.runtime_gap) { throw "correction runtime_gap must be explicit null: $($correction.id)" }
  $ledgerNode = $byId[[string]$correction.id]
  if ($null -eq $ledgerNode -or $ledgerNode.status -ne 'blocked' -or $null -ne $ledgerNode.runtime_gap) { throw "correction not reflected in ledger: $($correction.id)" }
}
foreach ($blocked in @($nodes | Where-Object status -eq 'blocked')) {
  if ($null -ne $blocked.runtime_gap -and -not [string]::IsNullOrWhiteSpace([string]$blocked.runtime_gap)) { throw "blocked node retains runtime_gap: $($blocked.id)" }
}
'REDPACKET_STATIC_VERIFY_PASS'
