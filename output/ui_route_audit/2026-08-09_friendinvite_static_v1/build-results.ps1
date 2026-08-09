$ErrorActionPreference = 'Stop'
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'route-manifest.json') | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) { if ($null -ne $node.parent) { $parents[$node.parent] = $true } }
$blocked = @(
  'mainui.friend-invite.shared-action.share',
  'mainui.friend-invite.shared-action.daily-claim',
  'mainui.friend-invite.shared-action.boost-claim',
  'mainui.friend-invite.recourse.claim',
  'mainui.friend-invite.help.progress-claim',
  'mainui.friend-invite.help.slot-claim',
  'mainui.friend-invite.level.slot-claim',
  'mainui.friend-invite.boost.claim-or-share',
  'mainui.friend-invite.welfare.claim-share',
  'mainui.friend-invite.shop.exchange',
  'mainui.friend-invite.sdk-share.callback',
  'mainui.friend-invite.sdk-share.wx-count',
  'mainui.friend-invite.sdk-share.wx-reward'
)
$nodes = @()
foreach ($node in $manifest.nodes) {
  if ($parents.ContainsKey($node.id)) { continue }
  if ($blocked -contains $node.id) {
    $nodes += [ordered]@{
      id = $node.id
      status = 'blocked'
      blocked_reason = 'No current authorization for SDK sharing, claims, rewards, exchanges, or other account writes; enumerated only and no packet was sent.'
      note = 'Transaction enumerated only; no SDK callback was simulated and no packet was sent.'
      applicable_gates = @('authorization')
      gates = [ordered]@{ authorization = $false }
      evidence = @('output/ui_route_audit/2026-08-09_friendinvite_static_v1/static-audit.md')
    }
  } else {
    $nodes += [ordered]@{
      id = $node.id
      status = 'needs-runtime-verify'
      runtime_gap = 'Static topology and minimal implementation were checked, but Unity and real Web were not run; player-visible state, raycast clicks, scroll and clipping, popup identity, resource readiness, lifecycle, and old-client comparison remain unverified.'
      note = 'Static evidence only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_friendinvite_static_v1/static-audit.md')
    }
  }
}
$json = [ordered]@{ nodes = $nodes } | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'results-static.json'), $json + [Environment]::NewLine, $utf8NoBom)
