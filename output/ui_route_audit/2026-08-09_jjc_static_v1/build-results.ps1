$ErrorActionPreference = 'Stop'

$manifestPath = Join-Path $PSScriptRoot 'route-manifest.json'
$outputPath = Join-Path $PSScriptRoot 'results-static.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json

$defects = @{
    'mainui.jjc.entry.prefab-ownership' = 'The player entry still uses the runtime-built TEMP shell; ArenaModule.prefab is not the player-visible source.'
    'mainui.jjc.result.rewards' = '28003 reward_list and break_reward_list are decoded and discarded instead of being retained for ArenaResultView.'
}

$blocked = @{
    'mainui.jjc.entry.open-condition' = 'Entry and open-condition ownership is in the forbidden Task/MainUI scope.'
    'mainui.jjc.entry.return' = 'The real close and return chain depends on the unclaimed prefab and its source page; the TEMP shell is not substitute evidence.'
    'mainui.jjc.enter.card.challenge' = '28003 is a real challenge transaction, explicitly forbidden in this pass.'
    'mainui.jjc.enter.rank' = 'Rank is an explicit no-write module; this cross-module route is report-only.'
    'mainui.jjc.enter.halo' = 'HaloMainView and reversible 51402 setting are outside the Jjc file island.'
    'mainui.jjc.rewards.breach.items' = 'Reward cells and detail belong to the forbidden Common shared-component scope.'
    'mainui.jjc.rewards.breach.claim' = '28017 is an account-mutating claim transaction, explicitly forbidden in this pass.'
    'mainui.jjc.rewards.rank.items' = 'Reward cells and detail belong to the forbidden Common shared-component scope.'
    'mainui.jjc.buy.confirm' = '28005 spends currency to buy challenge attempts, explicitly forbidden in this pass.'
    'mainui.jjc.battle.scene' = 'Scene 24001, 28018 and MainUI visibility exceed the Jjc island and require a real challenge.'
    'mainui.jjc.battle.skip' = '28015 mutates a live arena battle, explicitly forbidden in this pass.'
    'mainui.jjc.battle.exit' = 'Early exit spans 65306/28012 scene semantics and loss settlement; no real battle transaction is authorized.'
    'mainui.jjc.battle.return' = 'The return chain depends on battle scene, MainUI and BattleFieldEnterView forbidden scopes.'
    'mainui.jjc.shared.reward' = 'Common reward and detail components are in an explicit no-write scope.'
    'mainui.jjc.shared.head' = 'CustomHeadItem is a Common shared component that needs representative runtime evidence.'
    'mainui.jjc.shared.model' = '3D model and battle-scene dependencies exceed the Jjc island and require two-frame runtime evidence.'
    'mainui.jjc.shared.alert' = 'Alert is a Common shared popup and may not be edited here.'
    'mainui.jjc.shared.routes' = 'Rank, Halo, BattleField and MainUI are cross-module or explicit forbidden scopes.'
    'mainui.jjc.lifecycle.state' = 'Immediate refresh and reopen require real challenge, purchase or claim results, all forbidden in this pass.'
}

$runtimeGap = 'Static source, config, Prefab and Bind reconciliation only. No Unity, browser, real Web, dual viewport, pixel, scroll, two-frame model/effect, cold/warm or reopen evidence was produced.'
$nodes = @()
foreach ($node in $manifest.nodes) {
    $hasChildren = @($manifest.nodes | Where-Object { $_.parent -eq $node.id }).Count -gt 0
    if ($hasChildren) { continue }
    if ($defects.ContainsKey($node.id)) {
        $nodes += [ordered]@{
            id = $node.id
            status = 'defect'
            note = $defects[$node.id]
            evidence = @('output/ui_route_audit/2026-08-09_jjc_static_v1/static-audit.md')
        }
        continue
    }
    if ($blocked.ContainsKey($node.id)) {
        $nodes += [ordered]@{
            id = $node.id
            status = 'blocked'
            blocked_reason = $blocked[$node.id]
            note = 'Blocked honestly by the unique file island or forbidden transaction boundary; no guessing or out-of-scope write was performed.'
            evidence = @('output/ui_route_audit/2026-08-09_jjc_static_v1/static-audit.md')
        }
        continue
    }
    $nodes += [ordered]@{
        id = $node.id
        status = 'needs-runtime-verify'
        runtime_gap = $runtimeGap
        note = 'Static structure or read-side code is located, but static existence is not reported as runtime completion.'
        applicable_gates = @('runtime_state')
        gates = [ordered]@{ runtime_state = $false }
        evidence = @('output/ui_route_audit/2026-08-09_jjc_static_v1/static-audit.md')
    }
}

$json = [ordered]@{ nodes = $nodes } | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($outputPath, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Output "generated $($nodes.Count) leaf results -> $outputPath"
