$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'route-manifest.json') | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) { if ($null -ne $node.parent) { $parents[$node.parent] = $true } }
$leaves = @($manifest.nodes | Where-Object { -not $parents.ContainsKey($_.id) })

$blocked = @{
  'mainui.autobrush.hud.red'='The forbidden MainUI HUD currently initializes both red images to false and has no red refresh consumer; AutoBrush cannot edit that owner.'
  'mainui.autobrush.hud.toggle'='13307 changes live auto-brush gameplay state and was not authorized or executed.'
  'mainui.autobrush.main.stage-claim'='13310 claims account rewards and was not authorized or executed.'
  'mainui.autobrush.main.challenge'='13305 changes dungeon/gameplay state and was not authorized or executed.'
  'mainui.autobrush.main.go'='13307/AutoFight/Task changes live gameplay state and the cross-module owners are forbidden.'
  'mainui.autobrush.main.assist-state'='Guild open-condition/cooldown/pending-assist rendering is owned by the forbidden Guild module; the AutoBrush view keeps the assist host hidden.'
  'mainui.autobrush.main.assist'='40401/40403 are Guild account/gameplay writes; Guild is outside this edit island and no write was authorized.'
  'mainui.autobrush.main.return'='The converted main view has no close control or BaseWindow shell binding; shared window-shell ownership is outside AutoBrush.'
  'mainui.autobrush.result.exit'='Result click/timeout sends 61002 and changes dungeon state; it was not authorized or executed.'
  'mainui.autobrush.protocol.writes'='13305/13307/13310/40401/40403/61002 are write/gameplay transactions and were not executed.'
  'mainui.autobrush.shared.cross'='MainUI red, shared window shell, Task/AutoFight and Guild owners are forbidden cross-island dependencies.'
  'mainui.autobrush.lifecycle.return'='The full chain includes the missing main-shell close plus unexecuted dungeon/result write transactions.'
}

$items = @()
foreach ($leaf in $leaves) {
  if ($blocked.ContainsKey($leaf.id)) {
    $items += [ordered]@{
      id = $leaf.id
      status = 'blocked'
      blocked_reason = $blocked[$leaf.id]
      note = 'Blocked without editing a forbidden owner or executing an account/gameplay write.'
      evidence = @('output/ui_route_audit/2026-08-09_autobrush/static-audit.md')
    }
  } else {
    $items += [ordered]@{
      id = $leaf.id
      status = 'needs-runtime-verify'
      runtime_gap = 'Static source/config/protocol/Prefab path is enumerated or implemented, but player-visible Unity/Web interaction and lifecycle were not executed.'
      note = 'Static evidence only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_autobrush/static-audit.md')
    }
  }
}

$json = [ordered]@{ nodes = $items } | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText((Join-Path $root 'results-static.json'), $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Output "results leaves=$($items.Count) needs=$(@($items | Where-Object status -eq 'needs-runtime-verify').Count) blocked=$(@($items | Where-Object status -eq 'blocked').Count)"
