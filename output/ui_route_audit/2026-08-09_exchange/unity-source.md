# ExchangeGift Unity 静态事实与差异

- 已有可编辑 Prefab：`Assets/Prefabs/UI/Exchange/ExchangeModule.prefab`，因此使用 `fix-view` 增量修复，未重转模块。
- Prefab 已绑定真实 `TMP_InputField`、领取按钮根、错误文案与渠道说明；`_bg1/_ti_input` 继续保持老端一致的隐藏态。
- `BagController.SendGiftCard`、15087 handler 和 `EVT_GIFT_CARD_RESULT` 虽已存在于其他文件岛，但本轮明确不在 Exchange View 接入。
- 本轮只实现空输入提示和非空输入的阻塞提示；View 不发送 15087、不订阅结果事件，也不伪造失败/成功表现。
- 渠道说明清除了转换器 `htmlText` 泄漏，Prefab 固化老端运行样式的 `691×100`、22 号棕色文字；View 支持 `Presentation` 覆盖渠道名/微信号，缺省使用当前老端配置值。
- Unity 仍无通用 `CongratulationObtainView`；完整奖励弹窗属于公共弹层，超出本文件岛，明确阻塞。
- 失败事件 `EVT_GIFT_CARD_RESULT` 只传 `success/rewards`、没有 `res`；精确错误码文案也保持阻塞。
- Unity 当前也没有 `WelfareView` 外壳/“兑换礼包”页签接入；入口条件、返回链与 cold/warm 因此外部阻塞。
- 老端该页外层背景来自 `WelfareView.bg_list[3]=uigzhl_001_720x1222.jpg`；Exchange Prefab 内部节点不能替代这个外壳视觉，整页背景同样外部阻塞。
- 本轮未启动 Unity/浏览器、未输入或提交兑换码；所有玩家运行态、真实 Web、视觉 diff、协议回包、奖励、错误码、关闭重开与性能均未验收。
