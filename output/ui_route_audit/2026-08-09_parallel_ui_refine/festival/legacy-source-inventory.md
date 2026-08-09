# Festival 老端源码清单

本轮按用户约束未启动浏览器、未登录账号、未执行 19402/19404/19405 或商城购买；因此本文只是源码/配置调用链基线，不是老端运行证据。

- 入口：主界面活动图标 `223`；`FestivalBaseView.Open` 首次状态可能先开 `FestivalGoToAscendingOrderView`，否则进入三页签窗口。
- 一级页签：任务、奖励、进阶祭典；标题、背景、货币栏随页签切换。
- 任务页：每日/每周/赛季条件页签、倒计时、任务纵向列表、单项前往、单项领取、全部领取、升级祭典、进阶祭典入口、页签/总红点。
- 任务项状态：`status=0` 显示前往，`status=1` 显示领取和红点，`status=2` 显示已领取；任务文案、次数、进度和跳转来自 `config_fiesta_task`。
- 奖励页：等级/经验进度、倒计时、等级奖励纵向列表、单项领取、全部领取、升级祭典、进阶祭典入口、领取态与红点。
- 进阶页：豪华/至尊两档说明、奖励列表、图标详情、购买按钮；支付或勾玉确认属于真实资产事务。
- 条件弹窗：首次查看引导、升级祭典滑条、升级确认及“不再提示”开关、领取结果/未购买提示/购买弹窗、遮罩或关闭返回链。
- 协议：19401 基础快照；19403 任务三类快照；19402 等级奖励领取；19404 任务经验领取；19405 高阶购买。领取与购买必须检查权威后续 19401/19403、即时刷新和重开。
- 配置依赖：`config_fiesta_task`、`config_fiesta_lv_exp`、`config_fiesta_kv`、`config_fiesta_act`、`ClientConfigFiestaITaskTab`、`config_goods_effect`、`config_quick_buy_price`、`config_recharge_product`，以及物品/商城/支付/通用奖励格。
- 主要源码：`h5/src/festival/*.ts`、`h5/src/commonModel/FestivalModel.ts`、`h5/src/commonController/FestivalController.ts`。

声音调用扫描：Festival 专属源码未发现独立 `PlaySoundEffect/PlayFightingVoice/PlaySceneSound` 主动调用；通用按钮声不作为专属声音完成证据。
