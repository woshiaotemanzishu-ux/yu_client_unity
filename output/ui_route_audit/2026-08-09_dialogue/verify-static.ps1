$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

function Read-RepoFile([string]$relativePath) {
    return Get-Content -Raw -Encoding UTF8 (Join-Path $repo $relativePath)
}

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) { throw $message }
}

$view = Read-RepoFile 'Assets/Scripts/Module/Core/Dialogue/DialogueView.cs'
$controller = Read-RepoFile 'Assets/Scripts/Module/Core/Dialogue/DialogueController.cs'
$types = Read-RepoFile 'Assets/Scripts/Module/Core/Dialogue/DialogueTypeConst.cs'
$prefab = Read-RepoFile 'Assets/Prefabs/UI/Dialogue/DialogueModule.prefab'

Assert-Contains $view 'ViewManager.GetLayer(UILayer.Tip)' 'Dialogue no longer maps old Message to Unity Tip.'
Assert-Contains $view 'ViewManager.GetLayer(UILayer.Popup)' 'Dialogue no longer hides/restores old Activity equivalent.'
Assert-Contains $view '_clickSurface = _bind._img_bg;' 'Dialogue is not using the Prefab full-screen click surface.'
if ($view.Contains('root.AddComponent<Image>()')) { throw 'Dialogue still mutates the artificial root click tree at runtime.' }
Assert-Contains $view 'actionNodes[actionNodes.Count - 1]' 'Dialogue no longer follows old last-action-node semantics.'
Assert-Contains $view 'if (!loaded && ReferenceEquals(_loadTask, task)) _loadTask = null;' 'Dialogue load failure cannot retry.'
Assert-Contains $types 'type == TRIGGER_AND_FINISH' 'Dialogue action classification omits TRIGGER_AND_FINISH.'
Assert-Contains $controller 'GlobalEvent.EVT_SCENE_OBJECTS_CLEARED' 'Dialogue scene-change lifecycle hook is missing.'
Assert-Contains $controller 'if (Model.ChangeSceneClose)' 'Dialogue config-wait scene-change cancellation is missing.'
Assert-Contains $prefab 'm_Name: _img_bg' 'Dialogue Prefab no longer contains the bound background hit surface.'

$manifest = Read-RepoFile 'output/ui_route_audit/2026-08-09_dialogue/route-manifest.json' | ConvertFrom-Json
$ledger = Read-RepoFile 'output/ui_route_audit/2026-08-09_dialogue/route-ledger.json' | ConvertFrom-Json
if ($manifest.route -ne 'scene.dialogue.npc-task' -or $manifest.nodes.Count -ne 56) { throw 'Dialogue manifest topology drifted.' }
if ($ledger.schema -ne 6 -or $ledger.route -ne 'scene.dialogue.npc-task' -or $ledger.nodes.Count -ne 56) { throw 'Dialogue schema6 ledger drifted.' }

Write-Output 'PASS Dialogue static verification: layer/click/action/retry/scene-lifecycle/schema6.'
