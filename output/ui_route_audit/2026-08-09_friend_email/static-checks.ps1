$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$manifestPath = Join-Path $PSScriptRoot 'route-manifest.json'
$ledgerPath = Join-Path $PSScriptRoot 'route-ledger.json'
$componentPath = Join-Path $PSScriptRoot 'component-dependency-manifest.json'
$tabSource = Join-Path $repo 'Assets\Scripts\Module\Core\Friend\Views\FriendChatTabItem.cs'
$menuSource = Join-Path $repo 'Assets\Scripts\Module\Core\Friend\Views\FriendMenuView.cs'
$emailDetailSource = Join-Path $repo 'Assets\Scripts\Module\Core\Friend\Views\EmailPopView.cs'
$friendUtilSource = Join-Path $repo 'Assets\Scripts\Module\Core\Friend\FriendUiUtil.cs'
$prefab = Join-Path $repo 'Assets\Prefabs\UI\Friend\FriendModule.prefab'

$manifest = Get-Content -Raw -Encoding utf8 -LiteralPath $manifestPath | ConvertFrom-Json
$ledger = Get-Content -Raw -Encoding utf8 -LiteralPath $ledgerPath | ConvertFrom-Json
$components = Get-Content -Raw -Encoding utf8 -LiteralPath $componentPath | ConvertFrom-Json
$tabText = Get-Content -Raw -Encoding utf8 -LiteralPath $tabSource
$menuText = Get-Content -Raw -Encoding utf8 -LiteralPath $menuSource
$emailDetailText = Get-Content -Raw -Encoding utf8 -LiteralPath $emailDetailSource
$friendUtilText = Get-Content -Raw -Encoding utf8 -LiteralPath $friendUtilSource
$prefabText = Get-Content -Raw -Encoding utf8 -LiteralPath $prefab

$checks = [ordered]@{
    'route id' = $manifest.route -eq 'mainui.friend-email.shared'
    'schema 6' = $ledger.schema -eq 6
    'manifest/ledger node parity' = $manifest.nodes.Count -eq $ledger.nodes.Count
    'full route has 100+ nodes' = $manifest.nodes.Count -ge 100
    'component inventory covers 17 owners' = $components.components.Count -eq 17
    'friend prefab editable' = $components.prefab_editability.'Assets/Prefabs/UI/Friend/FriendModule.prefab'.editable -eq $true
    'prefab contains FriendView' = $prefabText.Contains('m_Name: FriendView')
    'prefab contains EmailView' = $prefabText.Contains('m_Name: EmailView')
    'prefab contains FriendChatView' = $prefabText.Contains('m_Name: FriendChatView')
    'prefab contains EmailPopView' = $prefabText.Contains('m_Name: EmailPopView')
    'active tab uses uilt_018' = $tabText.Contains('isActive ? "uilt_018" : "uilt_019"')
    'menu consumes screen point' = $menuText.Contains('ScreenPointToLocalPointInRectangle')
    'menu uses legacy height formula' = $menuText.Contains('buttonCount * 60f + 16f')
    'menu preserves report-avatar condition' = $menuText.Contains('pictureRoleId == _vo.RoleId')
    'mail detail preserves attachment geometry' = $emailDetailText.Contains('hasAttach ? 675f : 555f') -and $emailDetailText.Contains('hasAttach ? 603f : 483f')
    'mail detail swaps receive/next skin' = $emailDetailText.Contains('"uian_010b"') -and $emailDetailText.Contains('"uian_0120b"')
    'private chat time matches old client' = $friendUtilText.Contains('ToString("MM - dd HH:mm:ss")')
    'normal menu sprite exists' = Test-Path (Join-Path $repo 'Assets\GameRes\resource\game\common\texture\com_rect_btn1.png')
    'danger menu sprite exists' = Test-Path (Join-Path $repo 'Assets\GameRes\resource\game\common\texture\com_rect_btn3.png')
    'active tab sprite exists' = Test-Path (Join-Path $repo 'Assets\GameRes\resource\game\friend\texture\uilt_018.png')
    'inactive tab sprite exists' = Test-Path (Join-Path $repo 'Assets\GameRes\resource\game\friend\texture\uilt_019.png')
    'mail receive sprite exists' = Test-Path (Join-Path $repo 'Assets\GameRes\resource\game\common\texture\uian_010b.png')
    'mail next sprite exists' = Test-Path (Join-Path $repo 'Assets\GameRes\resource\game\common\texture\uian_0120b.png')
}

$failed = @()
foreach ($entry in $checks.GetEnumerator()) {
    $status = if ($entry.Value) { 'PASS' } else { 'FAIL' }
    Write-Output ("{0} {1}" -f $status, $entry.Key)
    if (-not $entry.Value) { $failed += $entry.Key }
}

if ($failed.Count -gt 0) {
    throw ('Friend/Email static checks failed: ' + ($failed -join ', '))
}

Write-Output ("PASS all static checks ({0})" -f $checks.Count)
