# Chat 全屏页首轮静态审计

日期：2026-08-09  
路线：`mainui.chat.fullscreen`  
边界：只读老端源码、Unity `Chat` 专属 Prefab/Bind/Flow/View；未启动 Unity、未操作浏览器、未发送聊天或执行任何账号写事务。

## 结论

`ChatModule.prefab` 已是可编辑 Prefab，入口 `MainUIRouter.Register("chat", ChatFlow.Toggle)`、Window 层加载、主窗关闭、基础频道列表、文字/系统消息克隆和普通文字发送链已经存在。因此本路线不需要重新转换，后续应继续走 `fix-view` 增量修复。

本轮把老端 `ChatParentView/ChatItem/ChatToolPanel/ChatBagPanel/ChatTrumpetView/ChatMenuView/VoiceChatView` 与 Unity Prefab、Generated Bind、Flow/View 三方对齐为 88 个 schema 6 节点。台账当前全部是 `not-run`，没有把静态存在或历史 HUD 用例扩大为全屏页完成。

## 三方核查

| 区域 | 老端权威行为 | Unity Prefab / Bind | Unity Flow / View 静态结论 |
|---|---|---|---|
| 入口与生命周期 | `ChatParentView` 打开、两处关闭 | `ChatModule.prefab/ChatParentView`，`_close/_btn_close` 均已绑定 | `ChatFlow` 从 Window 层加载并 Show；关闭可用 |
| 频道页签 | 世界、仙盟、队伍、跨服、活动、阵营、沧海舆图、系统；按真实队伍/跨服活动/阵营/海战状态显隐 | `Content_tab` + `ChatParentTab.prefab` 可编辑且被 `ChatModule` 实例化 | Unity 仅完整判断世界/仙盟/系统；跨服只看等级，队伍/阵营/海域固定隐藏，状态矩阵不等价 |
| 页签选中态 | 选中显示 111 宽 `_Image1`、黑字；未选中隐藏背景、白字 | `_Image1/labelDisplay/_fg_line` 均有 Bind | 本轮已把 `SetSelected` 改为老端背景与字色语义，并停用非权威 `_fg_line` |
| 消息列表 | 自己/他人、普通/喇叭/语音、头像菜单、富文本链接、语音播放与转文字、系统消息、未读定位、自动贴底 | `ChatItem/SystemItem` 模板、`content_Scroller`、`_gp_read/_to_bottom` 等节点均在 Prefab/Bind 中 | 当前仅普通文字、系统文字和他人头像直达资料卡；固定行高；语音/富链/未读被隐藏或未接；头像跳过 `ChatMenuView` |
| 滚到底 | 停止惯性后 `KeepScrollBottom()` | `_to_bottom` 已绑定 | 本轮由“只打日志”改为 `StopMovement()+verticalNormalizedPosition=0`；仍需真实拖动与末项可达复验 |
| 输入与发送 | 输入长度、频道/等级/CD/场景校验，`11001` 成功链；系统频道只读 | `TMP_InputField textDisplay`、`sendBtn`、系统频道显隐节点齐全 | 普通文字会调用 `ChatController.SendChat`；发送是玩家可见写事务，本轮未执行；即时回包与失败态未验 |
| 表情面板 | 6 列表情网格，点击把 `<f_id>` 写回当前频道输入 | `ChatToolPanel`、`ChatToolGridItem` 模板和 Bind 均存在 | 面板能被 Toggle，但运行 View 明确只隐藏模板并记录“待对接”，网格为空 |
| 聊天背包 | 背包/装备及五个条件页签，两套 5 列网格，点击物品写入输入 | `ChatBagPanel` 及全部 Tab/网格/条件模板存在 | 运行 View 隐藏所有模板，页签和物品网格为空 |
| 位置分享 | 场景、屏蔽、队伍、跨服频道校验后走 `11001` | `_position` 节点已绑定 | 仅日志，未实现；属于玩家可见写事务 |
| 语音切换与录音 | 180 级门槛；文字/按住说话切换；0.5 秒长按、10 秒倒计时、上滑取消、松开发送 | `voice/btn_speak` 与 `VoiceChatView` Prefab/Bind 均存在 | 主 View 仅日志；`VoiceChatView` 没有 Unity 运行 View，未实现 |
| 喇叭窗 | 类型菜单、道具数量、输入长度、表情、背包、补勾玉确认、`11001` 发送 | `ChatTrumpetView`、`ChatTrumpetMenu` 节点及 Bind 齐全 | 只关闭按钮可用；类型/表情/背包/发送均仅日志 |
| 玩家头像菜单 | 条件化查看资料、加/删好友、私聊、屏蔽/解除、黑名单、送花、头像投诉 | `ChatMenuView` Prefab/Bind 存在 | 没有 Unity 运行 View；当前头像直接打开资料卡，目标 View 身份与老端不等价 |
| 一键换装 | 跳到角色-时装二级页并关闭聊天 | `_dress_up` 已绑定 | 仅日志；跨路线跳转未接 |
| 滚动锁 | 老端按钮仍在，但点击动作已注释 | `_gp_lock/_lock/_unLock` 均存在 | 静态保留；不应凭按钮存在创造新行为 |

## 本轮增量修复

1. `ChatParentTab.SetSelected` 改为老端选中背景/字色规则，避免转换产物 `_fg_line` 冒充最终视觉。
2. `_to_bottom` 从空日志改为真实停止惯性并滚至底部。

两项都只修改 Chat 专属 View，没有改 MainUI、Common、共享物品/特效或 Addressables。因为未运行 Unity/Web，它们只能记为静态修复，不能标记对应叶子 `done`。

## 未完成闸

- 老端与当前 Unity WebGL 同账号、同状态、两档 viewport 的顺序复走、old/unity/overlay/diff。
- Player/catalog/源码/dirty 指纹绑定，页面 cold/warm 首显与 interactive-ready。
- 频道条件矩阵：仙盟、队伍、小跨服、全服活动、阵营、海域及系统频道。
- 消息状态矩阵：空/有数据、自己/他人、普通/喇叭/语音、短/长文本、富链、头像菜单、未读、自动贴底。
- `ScrollRect -> Viewport(RectMask2D) -> Content` 真实拖动、裁剪、末项可达和本轮“滚到底”修复的运行复验。
- 表情、背包、喇叭、玩家菜单、语音录制的目标 View 身份、完整控件、返回链和状态刷新。
- 所有 `risk=destructive-write` 叶子的本轮明确授权与真实成功/失败回包：普通消息、位置、喇叭、录音、好友/黑名单/投诉等。
- 输入、选择表情/物品/喇叭类型等 `reversible-write` 叶子的还原证据。

## 历史证据边界

现有 `ChatCase`、`MainUIChatHudCase` 和聊天链路文档能证明协议/HUD 的部分历史静态或 CLI 基线，不证明本次 88 节点全屏页面、真实 Web、像素视觉、子弹层身份与写事务已通过。因此本台账未重绑这些历史结果。

