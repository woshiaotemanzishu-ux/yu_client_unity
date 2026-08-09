$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$manifest = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'route-manifest.json')) | ConvertFrom-Json
$oldLedger = [IO.File]::ReadAllText((Join-Path $repo 'output\ui_route_audit\2026-08-09_redpacket_static_v3\route-ledger.json')) | ConvertFrom-Json
$oldById = @{}
foreach ($node in @($oldLedger.nodes)) { $oldById[[string]$node.id] = $node }
$parents = @{}
foreach ($node in @($manifest.nodes)) { if ($null -ne $node.parent) { $parents[[string]$node.parent] = $true } }
$newRuntime = @(
  'mainui.red-packet.route-state.disconnect-reset',
  'mainui.red-packet.route-state.late-arrival',
  'mainui.red-packet.route-state.subscription-unbind'
)
$nodes = @()
foreach ($node in @($manifest.nodes)) {
  $id = [string]$node.id
  if ($parents.ContainsKey($id)) { continue }
  $old = $oldById[$id]
  $status = if ($newRuntime -contains $id) { 'needs-runtime-verify' } elseif ($null -ne $old) { [string]$old.status } else { throw "new leaf missing policy: $id" }
  if ($status -eq 'blocked') {
    $reason = if ($null -ne $old -and -not [string]::IsNullOrWhiteSpace([string]$old.blocked_reason)) { [string]$old.blocked_reason } else { 'Implementation or authorization is missing.' }
    $gate = if ($node.risk -eq 'destructive-write' -or $node.type -eq 'transaction') { 'authorization' } else { 'runtime_state' }
    $nodes += [ordered]@{
      id = $id
      status = 'blocked'
      runtime_gap = $null
      blocked_reason = $reason
      note = 'Inherited v3 blocked boundary; no runtime completion claim.'
      applicable_gates = @($gate)
      gates = [ordered]@{ $gate = $false }
      evidence = @('output/ui_route_audit/2026-08-09_redpacket_static_v4/static-audit.md')
    }
  } elseif ($status -eq 'needs-runtime-verify') {
    $nodes += [ordered]@{
      id = $id
      status = 'needs-runtime-verify'
      runtime_gap = 'Static implementation exists, but Unity and real Web lifecycle behavior were not run; player-visible state and cleanup remain unverified.'
      note = 'Static evidence only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_redpacket_static_v4/static-audit.md')
    }
  } else {
    throw "unsupported inherited status for ${id}: $status"
  }
}
$json = [ordered]@{ nodes = $nodes } | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'results-static.json'), $json + [Environment]::NewLine, $utf8NoBom)
