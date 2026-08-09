$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$manifestPath = Join-Path $PSScriptRoot 'route-manifest.json'
$outputPath = Join-Path $PSScriptRoot 'results-static.json'
$manifest = Get-Content -Raw -Encoding UTF8 $manifestPath | ConvertFrom-Json

$parents = @{}
foreach ($node in $manifest.nodes) {
    if ($null -ne $node.parent) { $parents[[string]$node.parent] = $true }
}

$blocked = @{
    'scene.dialogue.npc-task.entry.empty' = 'Empty-talk routing may send 30003/30004; no task write transaction was authorized for this run.'
    'scene.dialogue.npc-task.background.wedding' = 'Wedding-scene background and click suppression depend on Banquet runtime state that is not implemented in the Unity dialogue flow.'
    'scene.dialogue.npc-task.speaker.role' = 'ROLE nodes still require the current-role model, outfit parts and authoritative player name branch.'
    'scene.dialogue.npc-task.speaker.override' = 'Per-content speaker id is not preserved by TalkConfigs.TalkContentBlock yet.'
    'scene.dialogue.npc-task.speaker.system' = 'System narrator id 99999 and blank-name/no-model behavior are not implemented.'
    'scene.dialogue.npc-task.speaker.wedding' = 'Wedding bride/groom/officiant speaker branches and dress mapping are not implemented.'
    'scene.dialogue.npc-task.speaker.animation' = 'Old 120ms speaker entry animation is not implemented.'
    'scene.dialogue.npc-task.speaker.idle' = 'Old casual/idle/replay sequencing and scene-NPC action handoff are not implemented.'
    'scene.dialogue.npc-task.content.action' = 'This leaf can send 30003/30004/30007; no task write transaction was authorized.'
    'scene.dialogue.npc-task.content.animation' = 'Old 80ms bottom-panel entry animation is not implemented.'
    'scene.dialogue.npc-task.reward.submit' = 'This leaf can submit task writes; no task write transaction was authorized.'
    'scene.dialogue.npc-task.auto.finish' = 'Finish countdown ends in a task write and cannot be runtime-tested without transaction authorization.'
    'scene.dialogue.npc-task.auto.submit' = 'Automatic action can send a task write; no task write transaction was authorized.'
    'scene.dialogue.npc-task.protocol.30003' = 'Accept-task transaction 30003 was not authorized.'
    'scene.dialogue.npc-task.protocol.30004' = 'Finish-task transaction 30004 was not authorized.'
    'scene.dialogue.npc-task.protocol.30007' = 'Talk-event transaction 30007 was not authorized.'
    'scene.dialogue.npc-task.protocol.success' = 'Immediate success state requires an authorized real UI transaction and authoritative response.'
    'scene.dialogue.npc-task.protocol.failure' = 'Failure-state preservation requires an authorized real UI transaction and authoritative error response.'
    'scene.dialogue.npc-task.protocol.reopen' = 'Reopen consistency depends on an authorized task write followed by authoritative refresh.'
    'scene.dialogue.npc-task.lifecycle.stop' = 'The old STOP_AUTO_DO_TASK equivalent is absent from the current shared GlobalEvent contract.'
}

$results = New-Object System.Collections.Generic.List[object]
foreach ($node in $manifest.nodes) {
    $id = [string]$node.id
    if ($parents.ContainsKey($id)) { continue }
    if ($blocked.ContainsKey($id)) {
        $results.Add([ordered]@{
            id = $id
            status = 'blocked'
            blocked_reason = $blocked[$id]
            note = 'Static audit recorded the exact implementation or authorization blocker.'
            evidence = @('output/ui_route_audit/2026-08-09_dialogue/static-audit.md')
        })
    } else {
        $results.Add([ordered]@{
            id = $id
            status = 'needs-runtime-verify'
            runtime_gap = 'The control/data/lifecycle path is statically present or guarded, but no same-account Unity/Web player-visible run was executed.'
            note = 'Static implementation is not runtime completion evidence.'
            applicable_gates = @('runtime_state')
            gates = [ordered]@{ runtime_state = $false }
            evidence = @('output/ui_route_audit/2026-08-09_dialogue/static-audit.md')
        })
    }
}

$payload = [ordered]@{ nodes = $results }
$json = $payload | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($outputPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
Write-Output ('Dialogue static results: leaves={0} blocked={1} needs-runtime-verify={2}' -f $results.Count, $blocked.Count, ($results.Count - $blocked.Count))
