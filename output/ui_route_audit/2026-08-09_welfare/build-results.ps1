$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'route-manifest.json') | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) { if ($null -ne $node.parent) { $parents[$node.parent] = $true } }
$leaves = @($manifest.nodes | Where-Object { -not $parents.ContainsKey($_.id) })

$blocked = @{
  'mainui.welfare.shell.entry'='MainUI route 417 is still owned by the forbidden GameNoticeBootstrap fallback; Welfare shell integration is not editable in this island.'
  'mainui.welfare.shell.identity'='No editable Welfare shell Prefab, Generated Bind or runtime Flow exists; converter capture/bake and Generated/Addressables edits were forbidden.'
  'mainui.welfare.shell.title'='Title belongs to the missing Welfare shell.'
  'mainui.welfare.shell.money'='Money list belongs to the missing Welfare shell.'
  'mainui.welfare.shell.tabs'='The six-tab host does not exist in Unity.'
  'mainui.welfare.shell.background'='Per-tab background replacement belongs to the missing Welfare shell and current assets were not guessed.'
  'mainui.welfare.shell.conditions'='Tab conditions require the missing shell plus forbidden Activity/platform/login owners.'
  'mainui.welfare.shell.close'='The return chain belongs to the missing Welfare shell and shared BaseWindow owner.'
  'mainui.welfare.red.aggregate'='Entry 417 aggregate is owned by forbidden GameNoticeBootstrap/MainUI integration.'
  'mainui.welfare.red.supply'='DailySupply red state is owned by forbidden Activity/CustomActivity modules.'
  'mainui.welfare.red.notice'='Notice unread state is owned by forbidden Login/GameNotice modules.'
  'mainui.welfare.sign.identity'='DailySign Prefabs contain Generated-only bindings; no Core DailySign runtime View exists and Generated is forbidden.'
  'mainui.welfare.sign.summary'='DailySign runtime rendering is absent.'
  'mainui.welfare.sign.daily-list'='DailySign list runtime rendering, scrolling and item lifecycle are absent.'
  'mainui.welfare.sign.daily-state'='DailySign item runtime status rendering is absent.'
  'mainui.welfare.sign.detail'='Shared reward detail ownership is outside Welfare.'
  'mainui.welfare.sign.claim'='41704 daily claim is an account write and was not authorized or executed.'
  'mainui.welfare.sign.makeup'='41704 retroactive=1 is an account/currency write and was not authorized or executed.'
  'mainui.welfare.sign.makeup-popup'='DailyTips/Alert popup implementation is outside Welfare and no makeup write was authorized.'
  'mainui.welfare.sign.vip'='VIP popup and shared reward cells are outside Welfare.'
  'mainui.welfare.sign.total'='DailySign cumulative-item runtime and UI_2037_1 rendering are absent.'
  'mainui.welfare.sign.total-claim'='41705 cumulative reward claim is an account write and was not authorized or executed.'
  'mainui.welfare.online.identity'='No OnlineView Prefab, Bind, Flow or runtime View exists.'
  'mainui.welfare.online.summary'='OnlineView runtime rendering is absent.'
  'mainui.welfare.online.slots'='The six OnlineItem hosts do not exist in Unity.'
  'mainui.welfare.online.item'='OnlineItem runtime rendering is absent.'
  'mainui.welfare.online.countdown'='OnlineView countdown rendering and threshold re-request lifecycle are absent.'
  'mainui.welfare.online.detail'='RewardPreView ownership is outside Welfare.'
  'mainui.welfare.online.claim'='41716 specific-id claim is an account write and was not authorized or executed.'
  'mainui.welfare.online.all'='41716 one-key claim is an account write and was not authorized or executed.'
  'mainui.welfare.online.month'='Monthly-card state owner is outside Welfare and OnlineView is absent.'
  'mainui.welfare.online.effect'='OnlineView/UI_1601_03 runtime host is absent.'
  'mainui.welfare.supply.identity'='DailySupplyView is owned by forbidden Activity and the Welfare shell host is absent.'
  'mainui.welfare.supply.condition'='Activity/CustomActivity state is outside the permitted edit island.'
  'mainui.welfare.supply.read'='33104/33209 are owned by forbidden Activity/Daily modules.'
  'mainui.welfare.supply.list'='DailySupply list is owned by forbidden Activity.'
  'mainui.welfare.supply.status'='DailySupply status is owned by forbidden Activity.'
  'mainui.welfare.supply.detail'='EquipmentItem detail is shared outside Welfare.'
  'mainui.welfare.supply.go'='DailyTask route is outside Welfare.'
  'mainui.welfare.supply.claim'='33105 is an account-write claim and was not authorized or executed.'
  'mainui.welfare.exchange.identity'='ExchangeModule exists but Welfare tab hosting and shell integration are absent/outside the Welfare island.'
  'mainui.welfare.exchange.submit'='15087 exchange-code redemption is an account write and was not authorized or executed.'
  'mainui.welfare.exchange.success'='CongratulationObtainView is shared outside Welfare and only appears after an unauthorized write.'
  'mainui.welfare.notice.identity'='GameNoticeModule exists but Welfare tab hosting is absent and route ownership is forbidden.'
  'mainui.welfare.follow.identity'='EyouModule has Generated-only bindings and no Core runtime View; Generated is forbidden.'
  'mainui.welfare.follow.background'='Eyou runtime asset loading is absent.'
  'mainui.welfare.follow.list'='Eyou config-list runtime rendering is absent.'
  'mainui.welfare.follow.item'='Eyou item runtime rendering is absent.'
  'mainui.welfare.follow.rewards'='EquipmentItem is shared outside Welfare and the Eyou runtime consumer is absent.'
  'mainui.welfare.follow.detail'='Shared item detail is outside Welfare.'
  'mainui.welfare.shared.identity'='Shared reward components are outside the permitted edit island.'
  'mainui.welfare.shared.matrix'='Representative shared-component consumers require cross-module runtime verification.'
  'mainui.welfare.shared.popups'='DailyTips/Alert/RewardPreView/CongratulationObtain are shared outside Welfare.'
  'mainui.welfare.shared.routing'='MainUI/Activity/Daily/GameNotice/Exchange route owners are outside the permitted edit island.'
  'mainui.welfare.lifecycle.cold'='The Welfare shell and two child runtimes are missing; Unity/browser execution was also forbidden.'
  'mainui.welfare.lifecycle.warm'='The Welfare shell and two child runtimes are missing; Unity/browser execution was also forbidden.'
  'mainui.welfare.lifecycle.switch'='Six-tab switching cannot run without the Welfare shell.'
  'mainui.welfare.lifecycle.viewports'='Real old-H5/Unity Web comparison was explicitly forbidden.'
  'mainui.welfare.lifecycle.performance'='Runtime timing/allocation/effect cleanup requires the missing shell and a real run.'
  'mainui.welfare.lifecycle.return'='Full popup-to-tab-to-shell return chain cannot run without the Welfare shell.'
}

$items = @()
foreach ($leaf in $leaves) {
  if ($blocked.ContainsKey($leaf.id)) {
    $items += [ordered]@{
      id = $leaf.id
      status = 'blocked'
      blocked_reason = $blocked[$leaf.id]
      note = 'Blocked without guessing a cross-module owner or executing an account write.'
      evidence = @('output/ui_route_audit/2026-08-09_welfare/static-audit.md')
    }
  } else {
    $items += [ordered]@{
      id = $leaf.id
      status = 'needs-runtime-verify'
      runtime_gap = 'Static source/config/protocol path is enumerated or implemented, but player-visible Unity/Web interaction and lifecycle were not executed.'
      note = 'Static evidence only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_welfare/static-audit.md')
    }
  }
}
$json = [ordered]@{ nodes = $items } | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText((Join-Path $root 'results-static.json'), $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Output "results leaves=$($items.Count) needs=$(@($items | Where-Object status -eq 'needs-runtime-verify').Count) blocked=$(@($items | Where-Object status -eq 'blocked').Count)"
