$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$manifestPath = Join-Path $PSScriptRoot 'route-manifest.json'
$ledgerPath = Join-Path $PSScriptRoot 'route-ledger.json'
$componentPath = Join-Path $PSScriptRoot 'component-dependency-manifest.json'
$taskFlowPath = Join-Path $repo 'Assets\Scripts\Module\Core\Task\TaskFlow.cs'
$taskModelPath = Join-Path $repo 'Assets\Scripts\Module\Core\Task\TaskModel.cs'
$finishPath = Join-Path $repo 'Assets\Scripts\Module\Core\Task\TaskFinishView.cs'
$controllerPath = Join-Path $repo 'Assets\Scripts\Module\Core\Task\TaskController.cs'
$prefabPath = Join-Path $repo 'Assets\Prefabs\UI\Task\TaskModule.prefab'
$oldTaskViewPath = 'E:\GitProject\yu_client\h5\src\task\TaskView.ts'
$oldTaskContentPath = 'E:\GitProject\yu_client\h5\src\task\TaskContentSubView.ts'
$oldControllerPath = 'E:\GitProject\yu_client\h5\src\commonController\TaskController.ts'

$manifest = Get-Content -Raw -Encoding utf8 -LiteralPath $manifestPath | ConvertFrom-Json
$ledger = Get-Content -Raw -Encoding utf8 -LiteralPath $ledgerPath | ConvertFrom-Json
$components = Get-Content -Raw -Encoding utf8 -LiteralPath $componentPath | ConvertFrom-Json
$taskFlow = Get-Content -Raw -Encoding utf8 -LiteralPath $taskFlowPath
$taskModel = Get-Content -Raw -Encoding utf8 -LiteralPath $taskModelPath
$finish = Get-Content -Raw -Encoding utf8 -LiteralPath $finishPath
$controller = Get-Content -Raw -Encoding utf8 -LiteralPath $controllerPath
$prefab = Get-Content -Raw -Encoding utf8 -LiteralPath $prefabPath
$oldTaskView = Get-Content -Raw -Encoding utf8 -LiteralPath $oldTaskViewPath
$oldTaskContent = Get-Content -Raw -Encoding utf8 -LiteralPath $oldTaskContentPath
$oldController = Get-Content -Raw -Encoding utf8 -LiteralPath $oldControllerPath

$notRunCount = @($ledger.nodes | Where-Object { $_.status -eq 'not-run' }).Count
$checks = [ordered]@{
    'route id' = $manifest.route -eq 'mainui.task.route'
    'schema 6' = $ledger.schema -eq 6
    'manifest/ledger node parity' = $manifest.nodes.Count -eq $ledger.nodes.Count
    'full route has 120+ nodes' = $manifest.nodes.Count -ge 120
    'no static result promoted to runtime' = $notRunCount -eq $ledger.nodes.Count
    'component inventory covers 11 owners' = $components.components.Count -eq 11
    'task prefab editable' = $components.prefab_editability.'Assets/Prefabs/UI/Task/TaskModule.prefab'.editable -eq $true
    'prefab contains TaskView' = $prefab.Contains('m_Name: TaskView')
    'prefab contains TaskFinishView' = $prefab.Contains('m_Name: TaskFinishView')
    'prefab contains TaskCircleFinishView' = $prefab.Contains('m_Name: TaskCircleFinishView')
    'prefab contains TaskUpAlertView' = $prefab.Contains('m_Name: TaskUpAlertView')
    'prefab contains TaskAutoSettingView' = $prefab.Contains('m_Name: TaskAutoSettingView')
    'old task page uses received task data' = $oldTaskView.Contains('model.GetTaskData()')
    'old task content has finish and go clicks' = $oldTaskContent.Contains('AddClickEvent(this.finishBtn') -and $oldTaskContent.Contains('AddClickEvent(this.goBtn')
    'old controller routes dynamic task views' = $oldController.Contains('model.Bind(TaskModel.TASK_OPEN_VIEW')
    'task overview has dedicated received-list data source' = $taskModel.Contains('GetTaskListForTaskView') -and $taskModel.Contains('foreach (KeyValuePair<int, List<TaskVo>> kv in _hasReceiveTaskList)')
    'task overview keeps old turn-four reincarnation gate' = $taskModel.Contains('task.TaskType == REINCARNATION') -and $taskModel.Contains('RoleModel.Instance.Figure?.turn ?? 0) >= 4')
    'task flow consumes converted binds' = $taskFlow.Contains('TaskViewBind') -and $taskFlow.Contains('TaskBarItemBind') -and $taskFlow.Contains('TaskContentSubViewBind')
    'task flow clones prefab templates' = $taskFlow.Contains('Instantiate(_bind._tpl_TaskBarItem') -and $taskFlow.Contains('Instantiate(_bind._tpl_TaskContentSubView')
    'task flow uses BaseView lifecycle for clones' = $taskFlow.Contains('row.Show()') -and $taskFlow.Contains('content.Show()') -and $taskFlow.Contains('cell.Show()')
    'task flow subscribes live task updates' = $taskFlow.Contains('EVT_TASK_LIST_UPDATED') -and $taskFlow.Contains('EVT_TASK_ONE_UPDATED')
    'task content routes go through DoTask' = $taskFlow.Contains('TaskModel.Instance.DoTask(task)')
    'task content finish uses formal 30004 entry' = $taskFlow.Contains('TaskController.Instance.SubmitFinish(task.TaskId)')
    'task finish rechecks authoritative completion' = $finish.Contains('if (!TaskModel.Instance.IsAllStepFinish(_task.TaskId)) return;')
    'registered task protocols remain 30000-30005 core' = $controller.Contains('RegisterProtocal(Proto.TASK_LIST') -and $controller.Contains('RegisterProtocal(Proto.CC_TASK_FINISH')
    'circle protocol gap remains explicit' = -not $controller.Contains('RegisterProtocal(Proto.TASK_CIRCLE')
}

$failed = @()
foreach ($entry in $checks.GetEnumerator()) {
    $status = if ($entry.Value) { 'PASS' } else { 'FAIL' }
    Write-Output ("{0} {1}" -f $status, $entry.Key)
    if (-not $entry.Value) { $failed += $entry.Key }
}
if ($failed.Count -gt 0) {
    throw ('Task static checks failed: ' + ($failed -join ', '))
}
Write-Output ("PASS all static checks ({0})" -f $checks.Count)
