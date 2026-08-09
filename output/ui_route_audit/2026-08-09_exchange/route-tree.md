# ExchangeGift 路线控件树

`mainui.welfare.exchange-gift`（老端 Welfare 第 4 页签“兑换礼包”）

- 入口页签：`ExchangeGiftView`；非 alpha 渠道显示，页签 `open_lv=50`。
- 页面视觉：720×992 根；外层背景 `uigzhl_001_720x1222.jpg`；内部标题/装饰、领取按钮、输入底图。
- 条件渠道说明：`gift_wx_name` 非空时显示微信名称与微信号，否则隐藏。
- 输入框：提示“点击输入激活码”；可输入/清空；没有内部列表、滚动、页签或开关。
- 领取按钮状态树：
  - 空输入：提示“请输入兑换码”，不得发协议。
  - 非空输入：发送 15087（真实兑换事务，本轮 blocked）。
  - 失败：`reward_list` 为空，错误文案显示 2 秒后隐藏（依赖真实请求，本轮 blocked）。
  - 成功：`reward_list` 非空，老端打开 `CongratulationObtainView`（真实发奖 + Unity 公共弹窗缺失，本轮 blocked）。
- 隐藏条件节点：`_bg1/_ti_input` 与设计态 `Placeholder/Text` 不得泄漏。
- 生命周期：隐藏/重开清错误、旧定时器不串页、结果事件不重复订阅。
- 返回链：页面本身无关闭按钮，由 Welfare/BaseWindow 外壳关闭；Unity 外壳缺失，blocked。
- 性能：入口 cold/warm `first-visible/interactive-ready`；本轮禁启 Unity/浏览器，blocked。

共享组件依赖：本页没有业务共享物品格/列表项/详情卡。输入框使用 Unity `TMP_InputField`；领取按钮和页面图片是页内节点。成功奖励弹窗应复用未来统一公共奖励弹层，禁止在 Exchange 内复制私有弹窗。
