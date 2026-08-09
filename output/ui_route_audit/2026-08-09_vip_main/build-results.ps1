param(
    [string]$Ledger = "output/ui_route_audit/2026-08-09_vip_main/route-ledger.json",
    [string]$Out = "output/ui_route_audit/2026-08-09_vip_main/results-static.json"
)

$ledgerData = Get-Content -LiteralPath $Ledger -Raw -Encoding UTF8 | ConvertFrom-Json
$parentIds = @{}
foreach ($node in $ledgerData.nodes) {
    if ($node.parent) { $parentIds[$node.parent] = $true }
}

$blockedReasons = @{
    "mainui.vip.card.action" = "45003 purchase and 45007 free-card activation are destructive writes. No purchase/claim was authorized or executed."
    "mainui.vip.card.free-tips.claim" = "45007 free-card activation writes card/VIP/week state and grants benefits. This route only inventories the control."
    "mainui.vip.benefit.exclusive-confirm.confirm" = "45001 may charge configured currency, writes the claimed list and grants rewards. No claim was authorized or executed."
    "mainui.vip.benefit.week-gift.claim" = "45002 writes weekly claim state and grants rewards. No claim was authorized or executed."
    "mainui.vip.recharge.more-pay" = "The More button enters an external payment provider. External payment was not authorized or executed."
    "mainui.vip.recharge.products.pay" = "Each recharge product invokes a platform payment flow. No payment was authorized or executed."
    "mainui.vip.recharge.products.claim" = "15902 is a welfare reward claim. No reward claim was authorized or executed."
    "mainui.vip.hide" = "45008 persists vip_hide, updates the player figure and broadcasts scene appearance. No write was authorized or executed."
}

$results = @()
foreach ($node in $ledgerData.nodes) {
    if ($parentIds.ContainsKey($node.id)) { continue }

    if ($blockedReasons.ContainsKey($node.id)) {
        $results += [ordered]@{
            id = $node.id
            status = "blocked"
            blocked_reason = $blockedReasons[$node.id]
            note = "Inventory-only leaf; no isolated sender, optimistic state, local reward or payment behavior was added."
            evidence = @("output/ui_route_audit/2026-08-09_vip_main/static-audit.md")
        }
        continue
    }

    $gates = switch ($node.type) {
        "tab" { @("click", "result", "runtime_state") }
        "navigation" { @("click", "result", "target_identity", "timing") }
        "return" { @("click", "return_chain") }
        default { @("runtime_state") }
    }
    if ($node.id -match "list|products") {
        $gates += @("layout_structure", "scroll_interaction", "shared_component_identity", "component_state_matrix")
    }
    $gateState = [ordered]@{}
    foreach ($gate in $gates) { $gateState[$gate] = $false }

    $results += [ordered]@{
        id = $node.id
        status = "needs-runtime-verify"
        runtime_gap = "Static source/Prefab inventory is complete for this leaf, but no Unity/old-Web player-visible click, state, visual, timing, scroll or lifecycle evidence was produced in this no-foreground run."
        note = "Static boundary only; not a Unity Editor or real-Web completion claim."
        applicable_gates = $gates
        gates = $gateState
        evidence = @("output/ui_route_audit/2026-08-09_vip_main/static-audit.md")
    }
}

$json = [ordered]@{ nodes = $results } | ConvertTo-Json -Depth 20
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path (Get-Location) $Out), $json + [Environment]::NewLine, $utf8NoBom)

Write-Output ("results={0} blocked={1} runtime={2}" -f $results.Count, $blockedReasons.Count, ($results.Count - $blockedReasons.Count))
