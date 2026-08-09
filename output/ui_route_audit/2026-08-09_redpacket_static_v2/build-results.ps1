$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'route-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) {
  if ($null -ne $node.parent) { $parents[$node.parent] = $true }
}
$blocked = @(
  'mainui.red-packet.packet-list.open-claim',
  'mainui.red-packet.packet-list.look-fetch',
  'mainui.red-packet.control-popup.send-system',
  'mainui.red-packet.control-popup.send-vip'
)
$nodes = @()
foreach ($node in $manifest.nodes) {
  if ($parents.ContainsKey($node.id)) { continue }
  if ($blocked -contains $node.id) {
    $nodes += [ordered]@{
      id = $node.id
      status = 'blocked'
      blocked_reason = 'No authorization for receiving or sending real red packets; enumerated only and no transaction packet was sent.'
      note = 'Transaction leaf only; 33902/33904/33906 was not sent.'
      applicable_gates = @('authorization')
      gates = [ordered]@{ authorization = $false }
      evidence = @('output/ui_route_audit/2026-08-09_redpacket_static_v2/static-audit.md')
    }
  } else {
    $nodes += [ordered]@{
      id = $node.id
      status = 'needs-runtime-verify'
      runtime_gap = 'Static topology and minimal implementation were checked, but Unity and real Web were not run; player-visible state, raycast clicks, scrolling, popup identity, resources, lifecycle, sounds, and old-client comparison remain unverified.'
      note = 'Static evidence only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_redpacket_static_v2/static-audit.md')
    }
  }
}
$json = [ordered]@{ nodes = $nodes } | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'results-static.json'), $json + [Environment]::NewLine, $utf8NoBom)
