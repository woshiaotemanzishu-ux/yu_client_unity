$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'route-manifest.json') | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) { if ($null -ne $node.parent) { $parents[$node.parent] = $true } }
$leaves = @($manifest.nodes | Where-Object { -not $parents.ContainsKey($_.id) })

$reason = @{
  'mainui.activity.shell.close'='Outer windowscomponent/BaseWindow ownership is shared and outside the Activity-only edit closure.'
  'mainui.activity.accum.rewards'='Reward icon/detail identity belongs to shared CommonRewardItem/EquipmentItem and may not be copied or edited here.'
  'mainui.activity.accum.recharge'='No authoritative recharge opener is registered in the Activity edit island; the button safely stays on the current page.'
  'mainui.activity.accum.claim'='33105 is an account-write claim. No write authorization was granted and no transaction was executed.'
  'mainui.activity.con.visual'='Tier-specific title/background selected sprites require current runtime resource/config evidence; no guessed sprite swap was added.'
  'mainui.activity.con.history'='15960 is requested, but the authoritative old-client achieved-day/history matrix needs real account history and runtime verification.'
  'mainui.activity.con.grade'='Featured reward state is wired, but old today/tomorrow sprite/effect pixels and history-derived progress need runtime/config evidence.'
  'mainui.activity.con.rewards'='BaseAwardItem/reward detail identity is shared outside Activity and may not be privately duplicated.'
  'mainui.activity.con.recharge'='Recharge route ownership is outside Activity and no authoritative opener is currently registered here.'
  'mainui.activity.con.claim'='33105 normal/featured claims are account writes and were not authorized or executed.'
  'mainui.activity.daily.rewards'='EquipmentItem reward identity/detail is shared outside Activity.'
  'mainui.activity.daily.go'='Old OpenFun 82 targets DailyTaskView; its authoritative MainUI route is outside Activity and is not guessed.'
  'mainui.activity.daily.claim'='33105 daily-supply claim is an account write and was not authorized or executed.'
  'mainui.activity.create.rewards'='CommonRewardItem list/detail is shared outside Activity.'
  'mainui.activity.create.claim'='33105 create-role-gift claim is an account write and was not authorized or executed.'
  'mainui.activity.return.images'='Old foreground/background assets are selected by ShowId/config; static Prefab alone cannot prove every current variant.'
  'mainui.activity.return.recharge'='Recharge route ownership is outside Activity and no authoritative opener is currently registered here.'
  'mainui.activity.return.seven'='Seven-day group route ownership is outside Activity; no target key is guessed.'
  'mainui.activity.shared.identity'='Shared reward component implementation is outside the Activity-only edit closure.'
  'mainui.activity.shared.matrix'='Shared reward state matrix requires representative Common/Equipment consumers and real runtime pixels.'
  'mainui.activity.shared.success'='CongratulationObtainView is shared and only appears after an unauthorized claim write.'
}

$items = @()
foreach ($leaf in $leaves) {
  if ($reason.ContainsKey($leaf.id)) {
    $items += [ordered]@{
      id = $leaf.id
      status = 'blocked'
      blocked_reason = $reason[$leaf.id]
      note = 'Blocked without guessing a cross-module dependency or executing an account write.'
      evidence = @('output/ui_route_audit/2026-08-09_activity/static-audit.md')
    }
  } else {
    $items += [ordered]@{
      id = $leaf.id
      status = 'needs-runtime-verify'
      runtime_gap = 'Activity-specific static implementation is present, but player-visible Unity/Web interaction, lifecycle and old-client comparison were not executed.'
      note = 'Static implementation only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_activity/static-audit.md')
    }
  }
}
$json = [ordered]@{ nodes = $items } | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText((Join-Path $root 'results-static.json'), $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Output "results leaves=$($items.Count) needs=$(@($items | Where-Object status -eq 'needs-runtime-verify').Count) blocked=$(@($items | Where-Object status -eq 'blocked').Count)"
