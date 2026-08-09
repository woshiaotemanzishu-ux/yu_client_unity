# ExchangeGift 老端静态事实

- `WelfareView.ts` 的第 4 个福利页签为“兑换礼包”，`open_lv=50`；显示条件为 `!PlatformManager.is_alpha`，内容类为 `ExchangeGiftView`。
- `ExchangeGiftView.scene` 根尺寸为 `720×992`，无内部分页和列表。页面控件是背景/标题图、条件微信说明、条件错误文案、领取按钮、输入框，以及两个设计态隐藏占位节点 `_bg1/_ti_input`。
- `_input_text` 提示为“点击输入激活码”。空串点击领取只提示“请输入兑换码”；非空串通过 15087 提交。
- 15087 成功条件是 `reward_list.length > 0`，老端打开 `CongratulationObtainView`；失败按错误码设置 `_lb_error`，显示 2 秒后隐藏。
- 当前老客户端 `ClientConfig.json` 的 `gift_wx_name/gift_wx_nmark` 为“永夜2.5d / yyhx25d”；有名称时显示微信说明，无名称时隐藏。
- 页面没有独立关闭按钮，返回/关闭由外层 `WelfareView/BaseWindowComponent` 负责。
- 本轮没有启动老端或浏览器；这些是源码、scene 与当前配置交叉后的静态事实，不是运行时画面证据。

来源：

- `E:/GitProject/yu_client/h5/src/welfare/WelfareView.ts`
- `E:/GitProject/yu_client/h5/src/exChange/ExchangeGiftView.ts`
- `E:/GitProject/yu_client/h5/laya/pages/resource/game/exchange/ExchangeGiftView.scene`
- `E:/GitProject/yu_client/cdn/resource/config/client/ClientConfig.json`
