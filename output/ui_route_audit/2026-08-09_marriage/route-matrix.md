# Marriage UI 静态路由矩阵

> 仅静态盘点；未启动 Unity/浏览器，未登录账号，未发送任何协议，未执行任何账号写事务。

- 节点：192（页面 29，叶 163）
- 叶状态：blocked=142，needs-runtime-verify=21，done=0
- 老端 TS：48；Unity Marriage 业务 C#：19；Generated Bind：40
- 顶层窗口：21；独立共享 Prefab：MarriageFriendItem / MarriageDropBtn

| 叶 ID | 状态 | 原因/运行缺口 |
|---|---|---|
| `marriage.sound-contract` | blocked | 需要真实可达路径、成功回包时点、关闭/切页生命周期与声音播放证据；禁止在未迁移事务按钮上伪播 |
| `marriage.main.close` | needs-runtime-verify | 缺真实 GraphicRaycaster 点击、关闭清理与热重开 |
| `marriage.main.background-close` | blocked | 当前 Prefab/Flow 未提供可静态确认的背景关闭链，需运行态补证并增量修复 |
| `marriage.main.default-tab` | blocked | 婚姻状态驱动的默认页签尚未实现；需接权威角色婚姻态后再做运行验证 |
| `marriage.main.open-level-gates` | blocked | 等级条件、提示文案与状态刷新未迁移 |
| `marriage.main.tab-red-dots` | blocked | 红点事件消费、条件矩阵和切页即时刷新未迁移 |
| `marriage.main.lobby.empty` | blocked | 列表表现层尚未接管 |
| `marriage.main.lobby.scroll` | blocked | 需 FriendItem 业务 View、ScrollRect 真实拖动与裁剪/末项证据 |
| `marriage.main.lobby.row-self` | blocked | 列表项业务 View 未接管 |
| `marriage.main.lobby.row-other` | blocked | 依赖 Friend/Common 共享头像、称号和菜单，超出当前文件岛 |
| `marriage.main.lobby.page-first` | blocked | 分页 Model 消费和页码刷新未接线 |
| `marriage.main.lobby.page-prev` | blocked | 分页 Model 消费和页码刷新未接线 |
| `marriage.main.lobby.page-next` | blocked | 分页 Model 消费和页码刷新未接线 |
| `marriage.main.lobby.page-last` | blocked | 分页 Model 消费和页码刷新未接线 |
| `marriage.main.lobby.open-flower-record` | blocked | 目标弹窗业务 View 尚未接管 |
| `marriage.main.lobby.open-com-self` | blocked | 目标弹窗业务 View 尚未接管 |
| `marriage.main.lobby.open-com-other` | blocked | 目标弹窗业务 View 尚未接管 |
| `marriage.main.lobby.open-issue` | blocked | 目标弹窗业务 View 尚未接管 |
| `marriage.main.mate.self-model` | blocked | 需要 UIModelStage/RT 真实出帧；公共模型链不在本文件岛 |
| `marriage.main.mate.mate-state` | blocked | 权威状态表现接线缺失 |
| `marriage.main.mate.mate-model` | blocked | 需要真实 RT 像素、换态清理和两 viewport 证据 |
| `marriage.main.mate.open-menu` | blocked | 目标仅 Generated Bind，且依赖 Friend/Chat 禁止岛 |
| `marriage.main.mate.find` | needs-runtime-verify | 缺条件显隐、参数、目标身份及关闭重开真实运行证据 |
| `marriage.main.mate.ask` | needs-runtime-verify | 缺条件显隐、参数、目标身份及关闭重开真实运行证据 |
| `marriage.main.mate.again` | needs-runtime-verify | 缺条件显隐、参数、目标身份及关闭重开真实运行证据 |
| `marriage.main.mate.break` | needs-runtime-verify | 缺条件显隐、参数、目标身份及关闭重开真实运行证据 |
| `marriage.main.mate.flow` | needs-runtime-verify | 缺条件显隐、参数、目标身份及关闭重开真实运行证据 |
| `marriage.main.mate.dsgt` | needs-runtime-verify | 缺条件显隐、参数、目标身份及关闭重开真实运行证据 |
| `marriage.main.mate.flower` | blocked | 需当前老端真实运行态确认是否为死控件，禁止凭源码猜可见性 |
| `marriage.main.mate.banquet` | blocked | marriage2/Common/MainUI 不在当前文件岛 |
| `marriage.main.mate.gift-time` | blocked | 礼包状态表现接线缺失 |
| `marriage.main.mate.intimacy-day` | blocked | 依赖 Friend 与服务器时间公共链，当前文件岛禁止修改 |
| `marriage.main.ring.presentation` | blocked | 模型与 FightingShowSmallItem 表现链未接 |
| `marriage.main.ring.partner-state` | blocked | RoleModel 婚姻字段未落地且不在本文件岛 |
| `marriage.main.ring.attrs` | blocked | 列表/配置表现接线缺失 |
| `marriage.main.ring.progress` | blocked | 需双时间点动画、RT/Canvas 与清理证据 |
| `marriage.main.ring.cost` | blocked | Common/Bag 共享组件不在文件岛 |
| `marriage.main.ring.upgrade` | blocked | 戒指解锁/升级会消费材料，未获账号写事务授权 |
| `marriage.main.ring.stop-visual` | needs-runtime-verify | 需 Prefab 射线与演出显隐确认不再形成可点击叶 |
| `marriage.main.gift.mate-model` | blocked | 需真实模型出帧与清理证据 |
| `marriage.main.gift.state` | blocked | 礼包表现接线缺失 |
| `marriage.main.gift.items` | blocked | 共享物品格/Common 不在文件岛 |
| `marriage.main.gift.ask-partner` | blocked | 会向伴侣发真实请求，未获账号写事务授权 |
| `marriage.main.gift.buy` | blocked | 会真实消费货币/购买礼包，未授权 |
| `marriage.main.gift.take-return` | blocked | 真实领奖事务未授权 |
| `marriage.main.gift.take-daily` | blocked | 真实领奖事务未授权 |
| `marriage.main.dungeon.count` | blocked | BaseDungeon 禁止岛依赖未接 |
| `marriage.main.dungeon.rewards` | blocked | 配置与共享物品格跨禁止岛 |
| `marriage.main.dungeon.teammate` | blocked | 依赖 FriendModel 且缺共享下拉业务 View |
| `marriage.main.dungeon.mate-mark` | blocked | 下拉链未接 |
| `marriage.main.dungeon.match` | blocked | 服务端入口不可达；不得恢复发送 |
| `marriage.main.dungeon.challenge` | blocked | 会创建真实副本事务，未获授权且 BaseDungeon 不在文件岛 |
| `marriage.main.dungeon.add-count` | blocked | 真实购买/邀请写事务未授权 |
| `marriage.main.dungeon.help` | needs-runtime-verify | 需接入现有说明路由后做目标身份与返回链；公共路由不在本文件岛 |
| `marriage.ask-list.close` | needs-runtime-verify | 缺真实点击、遮罩层级和热重开 |
| `marriage.ask-list.empty` | blocked | Friend 权威快照未接，固定空态不能验收 |
| `marriage.ask-list.scroll` | blocked | 依赖 FriendModel/MarriageAskListItem，跨禁止岛 |
| `marriage.ask-list.row-ask` | blocked | 共享项未接管 |
| `marriage.ask-list.go-main` | blocked | 缺目标路由和返回链 |
| `marriage.ask-tips.profile` | blocked | 推送弹窗表现未接管 |
| `marriage.ask-tips.agree` | blocked | 真实结婚事务未获授权 |
| `marriage.ask-tips.refuse` | blocked | 真实拒绝事务未获授权 |
| `marriage.ask-tips.close` | blocked | 关闭具有写语义，未授权时不得伪作普通返回 |
| `marriage.ask.close` | needs-runtime-verify | 缺真实点击/遮罩/重开 |
| `marriage.ask.help` | needs-runtime-verify | 说明路由跨公共层，需运行态目标身份 |
| `marriage.ask.self-head` | blocked | 依赖共享头像与 Friend 候选 |
| `marriage.ask.target-drop` | blocked | MarriageDropBtn/DownDropBtn 业务 View 与 FriendModel 未接 |
| `marriage.ask.ring-list` | blocked | 配置/共享物品格/选择状态未接 |
| `marriage.ask.propose` | blocked | 求婚与消费属于真实写事务，未授权 |
| `marriage.break-tips.profile` | blocked | 推送弹窗业务未接 |
| `marriage.break-tips.agree` | blocked | 真实离婚事务未授权 |
| `marriage.break-tips.refuse` | blocked | 真实回应事务未授权 |
| `marriage.break-tips.close` | blocked | 关闭具有写语义，未授权 |
| `marriage.break.close` | needs-runtime-verify | 缺真实点击/遮罩/重开 |
| `marriage.break.profile` | blocked | 共享头像/Friend 状态未接 |
| `marriage.break.cost` | blocked | 需要 Friend 在线时间与 Goods 映射，均在禁止岛 |
| `marriage.break.peace` | blocked | 真实离婚写事务未授权 |
| `marriage.break.force` | blocked | 破坏性且可能消费，未授权 |
| `marriage.com.close` | blocked | 业务 View 未接管且依赖 Friend/Common |
| `marriage.com.profile` | blocked | 业务 View 未接管且依赖 Friend/Common |
| `marriage.com.hi` | blocked | Chat/Friend 禁止岛 |
| `marriage.com.gift` | blocked | 含真实关注写事务，未授权 |
| `marriage.com.ask` | blocked | 条件态依赖 Friend/Role 婚姻状态 |
| `marriage.com.friend` | blocked | 真实社交写事务未授权 |
| `marriage.dsgt.close` | needs-runtime-verify | 缺真实点击与重开 |
| `marriage.dsgt.progress` | blocked | 表现接线缺失 |
| `marriage.dsgt.list` | blocked | DsgtModel/动态特效与列表业务未接 |
| `marriage.dsgt.locked-go` | blocked | 依赖 Alert/Role 状态与目标弹窗 |
| `marriage.dsgt.auto-take` | blocked | 真实领取资产事务；本轮禁止自动发包 |
| `marriage.dun-luck.answers` | blocked | 依赖 BaseDungeon 问答状态 |
| `marriage.dun-luck.timer` | blocked | BaseDungeon/服务器时钟不在文件岛 |
| `marriage.dun-luck.submit` | blocked | 真实副本答题写事务未授权 |
| `marriage.dun-luck.gray` | blocked | 需真实回包/超时证据 |
| `marriage.dun-luck.close` | blocked | 关闭与倒计时/自动提交生命周期需 BaseDungeon 证据 |
| `marriage.dun-tips.profile` | blocked | BaseDungeon 邀请 View 未接 |
| `marriage.dun-tips.timer` | blocked | 需真实邀请生命周期 |
| `marriage.dun-tips.accept` | blocked | 真实副本邀请写事务未授权且 BaseDungeon 禁止岛 |
| `marriage.dun-tips.refuse` | blocked | 真实副本邀请写事务未授权且 BaseDungeon 禁止岛 |
| `marriage.dun-tips.cancel` | blocked | 真实副本邀请写事务未授权且 BaseDungeon 禁止岛 |
| `marriage.dun-tips.close` | blocked | 真实副本邀请写事务未授权且 BaseDungeon 禁止岛 |
| `marriage.flower-tips.profile` | blocked | 推送弹窗业务未接 |
| `marriage.flower-tips.go` | blocked | 目标参数与返回链缺失 |
| `marriage.flower-tips.thanks` | blocked | 真实感谢写事务未授权 |
| `marriage.flower-tips.friend` | blocked | Friend 写事务未授权且在禁止岛 |
| `marriage.flower-tips.close` | blocked | 业务 View 未接管 |
| `marriage.flower.close` | needs-runtime-verify | 缺真实点击/遮罩/重开 |
| `marriage.flower.help` | needs-runtime-verify | 公共说明路由未在文件岛接线 |
| `marriage.flower.target` | blocked | FriendModel 与共享下拉业务未接 |
| `marriage.flower.profile` | blocked | 依赖 Friend/Common |
| `marriage.flower.list` | blocked | Bag/Goods/Shop 禁止岛依赖未接 |
| `marriage.flower.give` | blocked | 真实消耗鲜花写事务未授权 |
| `marriage.flower.buy` | blocked | Shop/Bag 禁止岛且可能消费 |
| `marriage.flow.close` | needs-runtime-verify | 缺真实点击与重开 |
| `marriage.flow.list` | blocked | 配置与 MarriageFlowItem 业务未接 |
| `marriage.flow.go-main` | blocked | 目标路由/参数/返回链未接 |
| `marriage.flow.go-flower` | blocked | 目标路由/参数/返回链未接 |
| `marriage.flow.go-ask-list` | blocked | 目标路由/参数/返回链未接 |
| `marriage.fore-show.countdown` | blocked | 等级弹窗编排/业务 View 未接管 |
| `marriage.fore-show.open` | blocked | 等级弹窗编排/业务 View 未接管 |
| `marriage.fore-show.close` | blocked | 等级弹窗编排/业务 View 未接管 |
| `marriage.fore-show.background` | blocked | 等级弹窗编排/业务 View 未接管 |
| `marriage.gift-tips.message` | blocked | 业务 View 未接 |
| `marriage.gift-tips.go` | blocked | 缺目标页参数/身份 |
| `marriage.gift-tips.cancel` | blocked | 业务 View 未接管 |
| `marriage.gift-tips.close` | blocked | 业务 View 未接管 |
| `marriage.honour.close` | needs-runtime-verify | 缺真实 Prefab 点击/遮罩/重开 |
| `marriage.honour.fame` | needs-runtime-verify | 缺同账号运行态状态证据 |
| `marriage.honour.list` | needs-runtime-verify | 缺 ScrollRect 结构、真实拖动、末项可达、长文案和视觉证据 |
| `marriage.honour.go` | needs-runtime-verify | 缺目标身份、参数、返回与热重开 |
| `marriage.issue.profile` | blocked | 业务 View、输入与共享头像未接 |
| `marriage.issue.edit` | blocked | 业务 View、输入与共享头像未接 |
| `marriage.issue.random` | blocked | 业务 View、输入与共享头像未接 |
| `marriage.issue.tag` | blocked | 老端标签编辑模块未加载，当前产品死链；不得臆造恢复 |
| `marriage.issue.publish` | blocked | 真实发布写事务未授权 |
| `marriage.issue.cancel` | blocked | 业务 View 未接管 |
| `marriage.issue.close` | blocked | 业务 View 未接管 |
| `marriage.record-com.close` | blocked | 列表/行项目/参数化业务 View 未接，依赖 Friend/Common |
| `marriage.record-com.self` | blocked | 列表/行项目/参数化业务 View 未接，依赖 Friend/Common |
| `marriage.record-com.other` | blocked | 列表/行项目/参数化业务 View 未接，依赖 Friend/Common |
| `marriage.record-com.empty` | blocked | 列表/行项目/参数化业务 View 未接，依赖 Friend/Common |
| `marriage.record-com.flower` | blocked | 列表/行项目/参数化业务 View 未接，依赖 Friend/Common |
| `marriage.record-com.menu` | blocked | 列表/行项目/参数化业务 View 未接，依赖 Friend/Common |
| `marriage.record-flower.close` | blocked | 业务 View 未接 |
| `marriage.record-flower.list` | blocked | 列表业务未接 |
| `marriage.record-flower.empty` | blocked | 业务 View 未接 |
| `marriage.record-flower.thanks` | blocked | 真实感谢写事务未授权 |
| `marriage.record-flower.gift` | blocked | 目标参数/跨服/Friend 条件未接 |
| `marriage.role-menu.outside` | blocked | 依赖 Friend/Chat/资料页，均在禁止岛 |
| `marriage.role-menu.look` | blocked | 依赖 Friend/Chat/资料页，均在禁止岛 |
| `marriage.role-menu.chat` | blocked | 依赖 Friend/Chat/资料页，均在禁止岛 |
| `marriage.role-menu.ask` | blocked | 依赖 Friend/Chat/资料页，均在禁止岛 |
| `marriage.role-menu.friend` | blocked | 真实 Friend 写事务未授权且文件岛禁止 |
| `marriage.success.profile` | blocked | 结果弹窗业务/特效未接 |
| `marriage.success.go` | blocked | 真实回应/婚宴事务未授权 |
| `marriage.success.close` | blocked | 关闭具有写语义，未授权 |
| `marriage.shared.friend-item.identity` | blocked | 缺业务 View/GUID 实例链与消费者运行抽查 |
| `marriage.shared.friend-item.states` | blocked | 组件状态矩阵未实现且依赖 Friend/Common |
| `marriage.shared.friend-item.flirt` | blocked | 共享项点击未接 |
| `marriage.shared.friend-item.touch` | blocked | 需真实老端确认当前是否死点击面，禁止臆造 |
| `marriage.shared.drop-btn.identity` | blocked | 缺业务 View 与消费者实例链 |
| `marriage.shared.drop-btn.toggle` | blocked | 共享下拉业务未实现 |
| `marriage.shared.drop-btn.options` | blocked | 缺真实滚动/裁剪/选中 |
| `marriage.shared.drop-btn.states` | blocked | 状态矩阵与代表宿主抽查缺失 |
