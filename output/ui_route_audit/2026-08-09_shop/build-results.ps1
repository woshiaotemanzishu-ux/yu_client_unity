$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'route-manifest.json') | ConvertFrom-Json
$parents = @{}
foreach ($node in $manifest.nodes) { if ($null -ne $node.parent) { $parents[$node.parent] = $true } }
$leaves = @($manifest.nodes | Where-Object { -not $parents.ContainsKey($_.id) })

$needs = @(
  'mainui.shop.shell.title','mainui.shop.shell.background','mainui.shop.shell.currency','mainui.shop.shell.tabs','mainui.shop.shell.close',
  'mainui.shop.top-tabs.bind','mainui.shop.top-tabs.vie','mainui.shop.top-tabs.ghost',
  'mainui.shop.secondary.diamond','mainui.shop.secondary.guild','mainui.shop.secondary.scroll',
  'mainui.shop.list.structure','mainui.shop.list.layout','mainui.shop.list.empty','mainui.shop.list.sort','mainui.shop.list.scroll',
  'mainui.shop.item.identity','mainui.shop.item.quota','mainui.shop.item.soldout','mainui.shop.item.click',
  'mainui.shop.bulk.identity','mainui.shop.bulk.close','mainui.shop.bulk.quantity','mainui.shop.bulk.limit','mainui.shop.bulk.total',
  'mainui.shop.vie.gate',
  'mainui.shop.route-state.open-close','mainui.shop.route-state.warm','mainui.shop.route-state.viewports','mainui.shop.route-state.performance'
)
$needSet = @{}; foreach ($id in $needs) { $needSet[$id] = $true }

$reason = @{
  'mainui.shop.top-tabs.limit'='Old client gates Limit with NotAlphaState; Shop scope has no authoritative Alpha-state owner.'
  'mainui.shop.top-tabs.diamond'='Old client gates Diamond with NotAlphaState; Shop scope has no authoritative Alpha-state owner.'
  'mainui.shop.top-tabs.guild'='Guild idol open state belongs to Guild and is not wired into ShopFlow.'
  'mainui.shop.top-tabs.honor'='Battlefield function-open state is outside Shop and is not wired into ShopFlow.'
  'mainui.shop.top-tabs.medal'='KfHolyArea open state is outside Shop and is not wired into ShopFlow.'
  'mainui.shop.top-tabs.single'='KfSingleRank open state is outside Shop and is not wired into ShopFlow.'
  'mainui.shop.top-tabs.longlang'='Old client uses LonglangExchangeView; Unity still reuses ShopCommonView. Longlang is outside this edit island.'
  'mainui.shop.top-tabs.godcourt'='GodCourt open state is outside Shop and is not wired into ShopFlow.'
  'mainui.shop.item.name'='Goods name is wired, but quality color and final shared item pixels are incomplete.'
  'mainui.shop.item.price'='Rounded price is fixed, but dynamic strike-through width, discount pixels and resource-ready proof remain incomplete.'
  'mainui.shop.item.currency'='Currency icon/amount state depends on the shared BaseAwardItem/resource identity chain, which this Shop pass may not modify.'
  'mainui.shop.item.condition'='Only lv/vip are authoritative here; rank_dun, constellation, god_pool, guild level/title states are cross-module dependencies.'
  'mainui.shop.item.detail'='Item detail must come from shared BaseAwardItem/Common; copying a Shop-private detail path is forbidden.'
  'mainui.shop.bulk.confirm'='15302 purchase is a write transaction. No purchase was authorized or executed; bind-diamond fallback confirmation also depends on Common Alert.'
  'mainui.shop.vie.list'='Current authoritative old-client gate hides the Rush tab, so its runtime list is unreachable in the selected route.'
  'mainui.shop.vie.countdown'='Rush page is hidden by the current old-client gate; no reachable timer evidence exists.'
  'mainui.shop.vie.item'='Rush page is hidden and its shared item icon/detail closure is incomplete.'
  'mainui.shop.vie.buy'='64001 is a purchase write; the current route hides the page and no transaction authorization was granted.'
  'mainui.shop.mystery.identity'='Prefab and generated Bind exist, but no ShopMysteriousView runtime class owns the page.'
  'mainui.shop.mystery.list'='ShopMysteriouItem has no runtime View and shared BaseAwardItem identity is unresolved.'
  'mainui.shop.mystery.countdown'='No runtime ShopMysteriousView owns refresh-time countdown/lifecycle.'
  'mainui.shop.mystery.currency'='No runtime ShopMysteriousView owns refresh cost state; shared currency item identity is unresolved.'
  'mainui.shop.mystery.refresh'='15304 refresh is a write transaction and the runtime page is missing.'
  'mainui.shop.mystery.buy'='15307 purchase is a write transaction and the runtime item/page is missing.'
  'mainui.shop.quick-buy'='QuickBuy belongs to the shared purchase/Common chain outside the Shop-only edit closure.'
}

$items = @()
foreach ($leaf in $leaves) {
  if ($needSet.ContainsKey($leaf.id)) {
    $items += [ordered]@{
      id = $leaf.id
      status = 'needs-runtime-verify'
      runtime_gap = 'The Shop-specific static implementation is present, but player-visible Unity/Web state, interaction, lifecycle and old-client comparison were not executed.'
      note = 'Static implementation only; no runtime completion claim.'
      applicable_gates = @('runtime_state')
      gates = [ordered]@{ runtime_state = $false }
      evidence = @('output/ui_route_audit/2026-08-09_shop/static-audit.md')
    }
  } elseif ($reason.ContainsKey($leaf.id)) {
    $items += [ordered]@{
      id = $leaf.id
      status = 'blocked'
      blocked_reason = $reason[$leaf.id]
      note = 'Blocked without guessing cross-module state or executing an unauthorized write.'
      evidence = @('output/ui_route_audit/2026-08-09_shop/static-audit.md')
    }
  } else {
    throw "Unclassified leaf: $($leaf.id)"
  }
}
$json = [ordered]@{ nodes = $items } | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText((Join-Path $root 'results-static.json'), $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Output "results leaves=$($items.Count) needs=$(@($items | Where-Object status -eq 'needs-runtime-verify').Count) blocked=$(@($items | Where-Object status -eq 'blocked').Count)"
