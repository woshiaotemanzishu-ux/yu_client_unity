$ErrorActionPreference = 'Stop'
$ids = @(
  'mainui.red-packet.shell.entry-red',
  'mainui.red-packet.shell.close',
  'mainui.red-packet.tabs.selected-style',
  'mainui.red-packet.packet-list.structure',
  'mainui.red-packet.packet-list.item-identity',
  'mainui.red-packet.packet-list.content',
  'mainui.red-packet.packet-list.state-matrix',
  'mainui.red-packet.packet-list.look-cached',
  'mainui.red-packet.packet-list.send-popup',
  'mainui.red-packet.packet-list.push-refresh',
  'mainui.red-packet.record-list.empty-nonempty',
  'mainui.red-packet.record-list.row-content',
  'mainui.red-packet.record-list.format',
  'mainui.red-packet.record-list.scroll',
  'mainui.red-packet.function-list.seven-routes',
  'mainui.red-packet.function-list.vip-gate',
  'mainui.red-packet.function-list.feast-message',
  'mainui.red-packet.function-list.navigation',
  'mainui.red-packet.function-list.scroll',
  'mainui.red-packet.control-popup.identity',
  'mainui.red-packet.control-popup.count-buttons',
  'mainui.red-packet.control-popup.count-calculator',
  'mainui.red-packet.control-popup.money-calculator',
  'mainui.red-packet.control-popup.blessing',
  'mainui.red-packet.control-popup.system-state',
  'mainui.red-packet.control-popup.vip-state',
  'mainui.red-packet.control-popup.validation',
  'mainui.red-packet.control-popup.close',
  'mainui.red-packet.detail-popup.identity',
  'mainui.red-packet.detail-popup.header',
  'mainui.red-packet.detail-popup.receive-state',
  'mainui.red-packet.detail-popup.recipient-list',
  'mainui.red-packet.detail-popup.best-marker',
  'mainui.red-packet.detail-popup.currency',
  'mainui.red-packet.detail-popup.close',
  'mainui.red-packet.route-state.kill-absent',
  'mainui.red-packet.route-state.pushes',
  'mainui.red-packet.route-state.sort-red',
  'mainui.red-packet.route-state.resources',
  'mainui.red-packet.route-state.sounds'
)
$nodes = foreach ($id in $ids) {
  $reason = if ($id -eq 'mainui.red-packet.route-state.kill-absent') {
    '33903/33905 are static negative boundaries that must remain absent; they are not runtime verification candidates.'
  } else {
    'The runtime view/list/item/popup/state branch is not implemented; static bindings or model fields cannot substitute for a reachable player-visible route.'
  }
  [ordered]@{
    id = $id
    status = 'blocked'
    blocked_reason = $reason
    note = 'QA status correction: implementation is missing, so runtime verification cannot begin.'
    applicable_gates = @('runtime_state')
    gates = [ordered]@{ runtime_state = $false }
    evidence = @('output/ui_route_audit/2026-08-09_redpacket_static_v3/static-audit.md')
  }
}
$json = [ordered]@{ nodes = @($nodes) } | ConvertTo-Json -Depth 8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'results-static-qa-blockers.json'), $json + [Environment]::NewLine, $utf8NoBom)
