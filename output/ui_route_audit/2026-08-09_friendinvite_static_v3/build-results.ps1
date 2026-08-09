$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'route-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) {
  if ($null -ne $node.parent) { $parents[$node.parent] = $true }
}

function IsBlocked($node) {
  $id = [string]$node.id
  if ($node.risk -eq 'destructive-write' -or $node.type -eq 'transaction') { return $true }
  foreach ($prefix in @(
    'mainui.friend-invite.tabs.', 'mainui.friend-invite.shared-action.',
    'mainui.friend-invite.recourse.', 'mainui.friend-invite.help.',
    'mainui.friend-invite.level.', 'mainui.friend-invite.boost.',
    'mainui.friend-invite.welfare.', 'mainui.friend-invite.shop.',
    'mainui.friend-invite.sdk-share.', 'mainui.friend-invite.shared-components.'
  )) {
    if ($id.StartsWith($prefix, [System.StringComparison]::Ordinal)) { return $true }
  }
  return $id -in @(
    'mainui.friend-invite.shell.preview',
    'mainui.friend-invite.shell.instruction',
    'mainui.friend-invite.shell.background-close',
    'mainui.friend-invite.route-state.kill-34010',
    'mainui.friend-invite.route-state.kill-34011'
  )
}

$nodes = @()
foreach ($node in $manifest.nodes) {
  if ($parents.ContainsKey($node.id)) { continue }
  if (IsBlocked $node) {
    $transaction = $node.risk -eq 'destructive-write' -or $node.type -eq 'transaction'
    $reason = if ($transaction) {
      'No authorization for sharing, claiming, exchanging, or other account writes; enumerated only and no transaction packet was sent.'
    } elseif ($node.id -match 'kill-3401[01]') {
      'Protocol is intentionally KILL/absent and must not be registered, sent, or exposed by UI.'
    } elseif ($node.id -eq 'mainui.friend-invite.sdk-share.wx-count') {
      '11301 is a read-only semantic leaf but remains hard-negative with no current constant, registration, model field, or runtime consumer.'
    } else {
      'The control/page/component branch is hidden or not implemented; static presence cannot substitute for a real runtime route.'
    }
    $gate = if ($transaction) { 'authorization' } else { 'runtime_state' }
    $nodes += [ordered]@{
      id = $node.id
      status = 'blocked'
      blocked_reason = $reason
      note = 'Explicitly blocked; no runtime completion claim.'
      applicable_gates = @($gate)
      gates = [ordered]@{ $gate = $false }
      evidence = @('output/ui_route_audit/2026-08-09_friendinvite_static_v3/static-audit.md')
    }
  } else {
    $nodes += [ordered]@{
      id = $node.id
      status = 'needs-runtime-verify'
      runtime_gap = 'Static implementation was checked, but Unity and real Web were not run; clicks, lifecycle, late-arrival cleanup, resources, sounds, and old-client comparison remain unverified.'
      note = 'Static evidence only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_friendinvite_static_v3/static-audit.md')
    }
  }
}
$json = [ordered]@{ nodes = $nodes } | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'results-static.json'), $json + [Environment]::NewLine, $utf8NoBom)
