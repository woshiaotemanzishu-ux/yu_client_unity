$ErrorActionPreference = 'Stop'
$nodes = New-Object System.Collections.ArrayList

function Control([string]$id, [string]$kind) {
  [ordered]@{ id = $id; kind = $kind }
}

function AddPage([string]$id, $parent, [object[]]$controls) {
  $inventory = @()
  foreach ($control in $controls) {
    $inventory += [ordered]@{ id = $control.id; kind = $control.kind; child = "$id.$($control.id)" }
  }
  $node = [ordered]@{ id = $id; type = 'page'; risk = 'read-only' }
  if ($null -ne $parent) { $node.parent = $parent }
  $node.control_inventory = $inventory
  [void]$nodes.Add($node)
}

function AddLeaf([string]$id, [string]$parent, [string]$type, [string]$risk, [string]$note) {
  [void]$nodes.Add([ordered]@{ id = $id; type = $type; risk = $risk; parent = $parent; note = $note })
}

AddPage 'mainui.friend-invite' $null @(
  (Control shell window-shell), (Control tabs conditional-tabs), (Control shared-action state-action),
  (Control recourse hidden-tab-page), (Control help hidden-tab-page), (Control level hidden-tab-page),
  (Control boost hidden-tab-page), (Control welfare hidden-popup), (Control shop hidden-popup),
  (Control sdk-share hidden-popup), (Control shared-components shared-components), (Control route-state lifecycle)
)

AddPage 'mainui.friend-invite.shell' 'mainui.friend-invite' @(
  (Control identity visual), (Control count state), (Control preview popup), (Control instruction popup),
  (Control close return), (Control background-close return)
)
AddLeaf 'mainui.friend-invite.shell.identity' 'mainui.friend-invite.shell' read read-only 'FriendInviteView 现有 Prefab 身份、背景/标题/遮罩与根尺寸。'
AddLeaf 'mainui.friend-invite.shell.count' 'mainui.friend-invite.shell' read read-only '消费现有 FriendInviteModel 快照显示 daily_count/total_count/get_status；开窗不重复 RequestStartup。'
AddLeaf 'mainui.friend-invite.shell.preview' 'mainui.friend-invite.shell' navigation read-only '奖励预览当前只有降级日志，真实 RewardPreView 未接。'
AddLeaf 'mainui.friend-invite.shell.instruction' 'mainui.friend-invite.shell' navigation read-only '说明入口当前只有降级日志，公共 InstructionView 未接。'
AddLeaf 'mainui.friend-invite.shell.close' 'mainui.friend-invite.shell' return read-only '_img_close 隐藏当前 View 并解绑更新事件。'
AddLeaf 'mainui.friend-invite.shell.background-close' 'mainui.friend-invite.shell' return read-only '老端 click_bg_toClose 背景关闭未落到现有 Prefab 可绑定遮罩。'

AddPage 'mainui.friend-invite.tabs' 'mainui.friend-invite' @(
  (Control recourse tab), (Control help tab), (Control level tab), (Control boost tab), (Control red-dot state)
)
AddLeaf 'mainui.friend-invite.tabs.recourse' 'mainui.friend-invite.tabs' tab read-only '求助页签当前模板隐藏、无运行时页面。'
AddLeaf 'mainui.friend-invite.tabs.help' 'mainui.friend-invite.tabs' tab read-only '帮助页签受 IsShowAll 条件控制，当前模板隐藏。'
AddLeaf 'mainui.friend-invite.tabs.level' 'mainui.friend-invite.tabs' tab read-only '升级页签受 IsShowAll 条件控制，当前模板隐藏。'
AddLeaf 'mainui.friend-invite.tabs.boost' 'mainui.friend-invite.tabs' tab read-only '助力页签受 IsShowAll 条件控制，当前模板隐藏。'
AddLeaf 'mainui.friend-invite.tabs.red-dot' 'mainui.friend-invite.tabs' read read-only '四页签、底部宝箱及主界面 340 红点矩阵未实现。'

AddPage 'mainui.friend-invite.shared-action' 'mainui.friend-invite' @(
  (Control daily-limit condition), (Control cooldown condition), (Control not-fire condition),
  (Control share transaction), (Control daily-claim transaction), (Control boost-claim transaction)
)
AddLeaf 'mainui.friend-invite.shared-action.daily-limit' 'mainui.friend-invite.shared-action' read read-only '每日次数达到上限时主按钮禁用、文案和倒计时分支未实现。'
AddLeaf 'mainui.friend-invite.shared-action.cooldown' 'mainui.friend-invite.shared-action' read read-only 'recover_time 冷却/可开启/到期即时刷新分支未实现。'
AddLeaf 'mainui.friend-invite.shared-action.not-fire' 'mainui.friend-invite.shared-action' read read-only '老端 not_fire 防重与不可触发分支未实现。'
AddLeaf 'mainui.friend-invite.shared-action.share' 'mainui.friend-invite.shared-action' transaction destructive-write '真实 SDK 成功后 34002，当前只记录 blocked，不模拟回调。'
AddLeaf 'mainui.friend-invite.shared-action.daily-claim' 'mainui.friend-invite.shared-action' transaction destructive-write 'get_status=2 后发送 34003 领取每日宝箱，未授权。'
AddLeaf 'mainui.friend-invite.shared-action.boost-claim' 'mainui.friend-invite.shared-action' transaction destructive-write '助力奖励非空时发送 34009(lv=60)，未授权。'

AddPage 'mainui.friend-invite.recourse' 'mainui.friend-invite' @(
  (Control progress state), (Control list scroll-list), (Control item-detail popup), (Control claim transaction), (Control shop navigation)
)
AddLeaf 'mainui.friend-invite.recourse.progress' 'mainui.friend-invite.recourse' read read-only '累计邀请进度页模板隐藏。'
AddLeaf 'mainui.friend-invite.recourse.list' 'mainui.friend-invite.recourse' read read-only '累计奖励列表、裁剪、拖动和末项未实现。'
AddLeaf 'mainui.friend-invite.recourse.item-detail' 'mainui.friend-invite.recourse' navigation read-only '真实奖励格详情未实现。'
AddLeaf 'mainui.friend-invite.recourse.claim' 'mainui.friend-invite.recourse' transaction destructive-write '34004(type=1,reward_id) 领取未授权。'
AddLeaf 'mainui.friend-invite.recourse.shop' 'mainui.friend-invite.recourse' navigation read-only '跳兑换商店未实现。'

AddPage 'mainui.friend-invite.help' 'mainui.friend-invite' @(
  (Control progress state), (Control progress-preview popup), (Control progress-claim transaction), (Control slots scroll-list), (Control shop navigation)
)
AddLeaf 'mainui.friend-invite.help.progress' 'mainui.friend-invite.help' read read-only '34005 count/reward_list 帮助进度页模板隐藏。'
AddLeaf 'mainui.friend-invite.help.progress-preview' 'mainui.friend-invite.help' navigation read-only '未达成宝箱预览未实现。'
AddLeaf 'mainui.friend-invite.help.progress-claim' 'mainui.friend-invite.help' transaction destructive-write '34004(type=2,reward_id) 领取未授权。'
AddLeaf 'mainui.friend-invite.help.slots' 'mainui.friend-invite.help' read read-only '8 个 FriendInviteHelpItem 和横向滚动未实现；具体四态在共享组件分支枚举。'
AddLeaf 'mainui.friend-invite.help.shop' 'mainui.friend-invite.help' navigation read-only '跳兑换商店未实现。'

AddPage 'mainui.friend-invite.level' 'mainui.friend-invite' @((Control slots scroll-list), (Control shop navigation))
AddLeaf 'mainui.friend-invite.level.slots' 'mainui.friend-invite.level' read read-only '升级邀请 8 槽及 lv=180 条件模板隐藏；具体四态在共享组件分支枚举。'
AddLeaf 'mainui.friend-invite.level.shop' 'mainui.friend-invite.level' navigation read-only '跳兑换商店未实现。'

AddPage 'mainui.friend-invite.boost' 'mainui.friend-invite' @((Control currency state), (Control exchange navigation), (Control claim-or-share transaction))
AddLeaf 'mainui.friend-invite.boost.currency' 'mainui.friend-invite.boost' read read-only '34008(lv=60) total_count/奖励/36255002 状态页模板隐藏。'
AddLeaf 'mainui.friend-invite.boost.exchange' 'mainui.friend-invite.boost' navigation read-only '助力兑换页跳转未实现。'
AddLeaf 'mainui.friend-invite.boost.claim-or-share' 'mainui.friend-invite.boost' transaction destructive-write '有奖励走 34009、无奖励走真实 SDK→34002，均 blocked。'

AddPage 'mainui.friend-invite.welfare' 'mainui.friend-invite' @(
  (Control identity popup), (Control reward-list list), (Control hook reversible-write),
  (Control claim-share transaction), (Control close return)
)
AddLeaf 'mainui.friend-invite.welfare.identity' 'mainui.friend-invite.welfare' read read-only '34012(type=3) 条件福利弹窗模板隐藏。'
AddLeaf 'mainui.friend-invite.welfare.reward-list' 'mainui.friend-invite.welfare' read read-only '福利奖励列表和详情未实现。'
AddLeaf 'mainui.friend-invite.welfare.hook' 'mainui.friend-invite.welfare' reversible-write reversible-write '弹窗内临时勾选显隐未实现。'
AddLeaf 'mainui.friend-invite.welfare.claim-share' 'mainui.friend-invite.welfare' transaction destructive-write '34004(type=3,reward_id=1)+真实分享写事务未授权。'
AddLeaf 'mainui.friend-invite.welfare.close' 'mainui.friend-invite.welfare' return read-only '福利弹窗关闭链未实现。'

AddPage 'mainui.friend-invite.shop' 'mainui.friend-invite' @(
  (Control identity popup), (Control tabs tabs), (Control currency state), (Control list scroll-list),
  (Control item-detail popup), (Control exchange transaction), (Control close return)
)
AddLeaf 'mainui.friend-invite.shop.identity' 'mainui.friend-invite.shop' read read-only '兑换商店 Activity 弹窗模板隐藏。'
AddLeaf 'mainui.friend-invite.shop.tabs' 'mainui.friend-invite.shop' tab read-only '求助/助力两页签及共享 FriendInviteTabItem 未实现。'
AddLeaf 'mainui.friend-invite.shop.currency' 'mainui.friend-invite.shop' read read-only '36255001/36255002 持有数即时刷新未实现。'
AddLeaf 'mainui.friend-invite.shop.list' 'mainui.friend-invite.shop' read read-only '兑换列表、限购/不足/等级和滚动未实现。'
AddLeaf 'mainui.friend-invite.shop.item-detail' 'mainui.friend-invite.shop' navigation read-only '商品真实详情未实现。'
AddLeaf 'mainui.friend-invite.shop.exchange' 'mainui.friend-invite.shop' transaction destructive-write '真实扣特殊货币兑换物品未授权。'
AddLeaf 'mainui.friend-invite.shop.close' 'mainui.friend-invite.shop' return read-only '关闭商店返回邀请页未实现。'

AddPage 'mainui.friend-invite.sdk-share' 'mainui.friend-invite' @(
  (Control overlay conditional-overlay), (Control callback transaction), (Control wx-count read),
  (Control wx-reward transaction), (Control close return)
)
AddLeaf 'mainui.friend-invite.sdk-share.overlay' 'mainui.friend-invite.sdk-share' read read-only 'FriendInviteShareView 全屏覆盖模板未接。'
AddLeaf 'mainui.friend-invite.sdk-share.callback' 'mainui.friend-invite.sdk-share' transaction destructive-write '真实 SDK success/cancel/timeout 防重未接，禁止模拟成功。'
AddLeaf 'mainui.friend-invite.sdk-share.wx-count' 'mainui.friend-invite.sdk-share' read read-only '11301 当日分享次数查询是 read/read-only；当前 hard-negative、无常量/注册/消费者。'
AddLeaf 'mainui.friend-invite.sdk-share.wx-reward' 'mainui.friend-invite.sdk-share' transaction destructive-write '11302 会写次数并真实发奖，当前 hard-negative。'
AddLeaf 'mainui.friend-invite.sdk-share.close' 'mainui.friend-invite.sdk-share' return read-only '分享遮罩关闭链未接。'

AddPage 'mainui.friend-invite.shared-components' 'mainui.friend-invite' @((Control tab-item shared-component), (Control help-item shared-component))
AddPage 'mainui.friend-invite.shared-components.tab-item' 'mainui.friend-invite.shared-components' @(
  (Control render state), (Control selected state), (Control main-four-pages consumers), (Control shop-consumer consumers)
)
AddLeaf 'mainui.friend-invite.shared-components.tab-item.render' 'mainui.friend-invite.shared-components.tab-item' read read-only 'FriendInviteTabItem 标签/图标/点击面渲染未实现。'
AddLeaf 'mainui.friend-invite.shared-components.tab-item.selected' 'mainui.friend-invite.shared-components.tab-item' read read-only '选中/未选中、红点有无状态未实现。'
AddLeaf 'mainui.friend-invite.shared-components.tab-item.main-four-pages' 'mainui.friend-invite.shared-components.tab-item' read read-only '主窗单页/四页条件与 Recourse/Help/Level/Boost 四消费者未实现。'
AddLeaf 'mainui.friend-invite.shared-components.tab-item.shop-consumer' 'mainui.friend-invite.shared-components.tab-item' read read-only 'Shop 求助/助力共享消费者未实现，需与主窗组件身份一致。'
AddPage 'mainui.friend-invite.shared-components.help-item' 'mainui.friend-invite.shared-components' @(
  (Control state0 condition), (Control state1 condition), (Control state2 transaction), (Control state3 condition)
)
AddLeaf 'mainui.friend-invite.shared-components.help-item.state0' 'mainui.friend-invite.shared-components.help-item' read read-only 'status=0 空槽/未邀请身份与视觉未实现。'
AddLeaf 'mainui.friend-invite.shared-components.help-item.state1' 'mainui.friend-invite.shared-components.help-item' read read-only 'status=1 已邀请但未达成身份与视觉未实现。'
AddLeaf 'mainui.friend-invite.shared-components.help-item.state2' 'mainui.friend-invite.shared-components.help-item' transaction destructive-write 'status=2 可领取，Help lv=10/Level lv=180 按 pos 发送 34007；未授权。'
AddLeaf 'mainui.friend-invite.shared-components.help-item.state3' 'mainui.friend-invite.shared-components.help-item' read read-only 'status=3 已领取身份与视觉未实现。'

AddPage 'mainui.friend-invite.route-state' 'mainui.friend-invite' @(
  (Control startup protocol-order), (Control level-change condition), (Control kill-34010 protocol-boundary),
  (Control kill-34011 protocol-boundary), (Control disconnect-reset lifecycle), (Control late-arrival lifecycle),
  (Control subscription-unbind lifecycle), (Control sounds sound), (Control resources resource),
  (Control cold-warm performance), (Control viewports visual)
)
AddLeaf 'mainui.friend-invite.route-state.startup' 'mainui.friend-invite.route-state' read read-only 'GAME_START 唯一发送 34001→34012(3)→34005→34006→34008(60)；开窗只消费模型，不重复请求。'
AddLeaf 'mainui.friend-invite.route-state.level-change' 'mainui.friend-invite.route-state' read read-only '等级真变、分享开启且普通 340 图标缺失才复走启动请求。'
AddLeaf 'mainui.friend-invite.route-state.kill-34010' 'mainui.friend-invite.route-state' read read-only '34010 KILL：常量/注册/sender/UI 持续 absent。'
AddLeaf 'mainui.friend-invite.route-state.kill-34011' 'mainui.friend-invite.route-state' read read-only '34011 KILL：常量/注册/sender/UI 持续 absent。'
AddLeaf 'mainui.friend-invite.route-state.disconnect-reset' 'mainui.friend-invite.route-state' read read-only '真断线 Reset 应先解绑 View，再释放模块并递增 generation。'
AddLeaf 'mainui.friend-invite.route-state.late-arrival' 'mainui.friend-invite.route-state' read read-only 'Reset 后 await 晚到实例必须按 generation 丢弃并 Release，禁止回填/Show。'
AddLeaf 'mainui.friend-invite.route-state.subscription-unbind' 'mainui.friend-invite.route-state' read read-only 'OnShow 订阅、OnHide/OnDispose/PrepareForRelease 解绑且重复开关无泄漏。'
AddLeaf 'mainui.friend-invite.route-state.sounds' 'mainui.friend-invite.route-state' read read-only '页面专属声音未接，真实通用点击/关闭与事务成功音需运行核对。'
AddLeaf 'mainui.friend-invite.route-state.resources' 'mainui.friend-invite.route-state' read read-only 'invite 配置、特殊货币、Prefab/GUID/Addressables 与 ready 时点。'
AddLeaf 'mainui.friend-invite.route-state.cold-warm' 'mainui.friend-invite.route-state' read read-only 'cold/warm 打开、关闭重开、Reset、计时器/克隆/订阅清理。'
AddLeaf 'mainui.friend-invite.route-state.viewports' 'mainui.friend-invite.route-state' read read-only '720×1280 与宽屏真实 H5/Unity Web 对比。'

$manifest = [ordered]@{
  route = 'mainui.friend-invite-v2'
  baseline = [ordered]@{
    supersedes = 'output/ui_route_audit/2026-08-09_friendinvite_static_v1'
    reason = 'QA found duplicate startup requests, event lifetime and async late-arrival gaps plus omitted component/state/control leaves; schema6 topology is immutable.'
    authority = '当前老 H5 同账号、同状态、同 viewport 的最终运行表现是唯一验收目标；本清单不声明 Unity/Web 运行闸通过。'
    protocol_inventory = [ordered]@{
      reads = @('34000','34001','34005','34006','34008','34012','11301')
      blocked_transactions = @('34002','34003','34004','34007','34009','11302')
      kill_absent = @('34010','34011')
    }
  }
  nodes = $nodes
}
$json = $manifest | ConvertTo-Json -Depth 12
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'route-manifest.json'), $json + [Environment]::NewLine, $utf8NoBom)
