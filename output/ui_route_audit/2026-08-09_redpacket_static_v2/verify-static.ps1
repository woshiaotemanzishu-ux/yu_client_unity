$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$main = Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\Views\RedPacketMainView.cs'
$controller = Join-Path $repo 'Assets\Scripts\Module\Core\RedPacket\RedPacketController.cs'
$prefab = Join-Path $repo 'Assets\Prefabs\UI\RedPacket\RedPacketModule.prefab'

$mainText = [System.IO.File]::ReadAllText($main)
$controllerText = [System.IO.File]::ReadAllText($controller)
$prefabText = [System.IO.File]::ReadAllText($prefab)

foreach ($needle in @('RequestList()', 'Records.Count > 0 ? 0 : 1', 'InstructionFlow.Show(339)', 'SetTabPagesVisible(false, false)')) {
  if (-not $mainText.Contains($needle)) { throw "RedPacketMainView missing: $needle" }
}
foreach ($forbidden in @('RequestOpen(', 'RequestSend(', 'RequestSendVip(')) {
  if ($mainText.Contains($forbidden)) { throw "transaction call leaked into RedPacketMainView: $forbidden" }
}
foreach ($active in @('Proto.REDPACKET_ERROR', 'Proto.REDPACKET_LIST', 'Proto.REDPACKET_OPEN', 'Proto.REDPACKET_SEND', 'Proto.REDPACKET_SEND_VIP', 'Proto.REDPACKET_NEW_PUSH', 'Proto.REDPACKET_TAKEN_PUSH')) {
  if (-not $controllerText.Contains("RegisterProtocal($active")) { throw "active protocol registration missing: $active" }
}
if ($controllerText -match 'RegisterProtocal\([^\r\n]*(33903|33905)') { throw '33903/33905 must remain absent' }
if (-not $prefabText.Contains('Shenxiao.Module.Core::Shenxiao.Module.Core.RedPacket.RedPacketMainView')) { throw 'Prefab runtime RedPacketMainView binding missing' }

dotnet build (Join-Path $PSScriptRoot 'RedPacket.StaticCompile.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'output-only RedPacket compile failed' }
python (Join-Path $repo '.agents\skills\audit-game-ui-route\scripts\route_ledger.py') validate (Join-Path $PSScriptRoot 'route-ledger.json')
if ($LASTEXITCODE -ne 0) { throw 'RedPacket schema6 ledger validation failed' }
'REDPACKET_STATIC_VERIFY_PASS'

