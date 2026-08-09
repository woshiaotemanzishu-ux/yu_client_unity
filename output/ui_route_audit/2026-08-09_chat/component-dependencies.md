# Chat 组件依赖清单

## 组件身份与消费者

| 组件 | 可编辑资产 / 运行类 | Chat 内直接消费者 | 使用形态 | 本轮处理 |
|---|---|---|---|---|
| 全屏聊天模块 | `Assets/Prefabs/UI/Chat/ChatModule.prefab` / `ChatFlow` | MainUI `chat` 固定入口 | Window 全屏模块根 | 只盘点，不改 Prefab |
| 主聊天页 | `ChatModule.prefab/ChatParentView` / `ChatParentView` | `ChatFlow.Show` | 全屏页、频道/列表/编辑器宿主 | 增量修复滚到底 |
| 频道页签 | `Assets/Prefabs/UI/Chat/ChatParentTab.prefab` / `ChatParentTab` | 主频道列表、聊天背包左侧页签 | 主频道标签与背包标签；同 Prefab 两类宿主 | 增量修复共享选中态；后续运行抽样必须至少覆盖主频道与背包页签 |
| 普通消息项 | `ChatModule.prefab/ChatItem` / `ChatItemBind` | `ChatParentView` 运行克隆 | 自己/他人、普通/喇叭/语音 | 只盘点；语音/富链/头像菜单缺口未修 |
| 系统消息项 | `ChatModule.prefab/SystemItem` / `SystemItemBind` | `ChatParentView` 运行克隆 | 系统频道只读行 | 只盘点 |
| 表情面板 | `ChatModule.prefab/ChatToolPanel` / `ChatToolPanel` | 主编辑器、喇叭编辑器 | 两个输入宿主共用弹层 | 只盘点；网格仍为空 |
| 表情格 | `Assets/Prefabs/UI/Chat/ChatToolGridItem.prefab` / Generated Bind | `ChatToolPanel` | 6 列滚动项、点击写入当前输入 | 只盘点；缺运行 item 行为 |
| 聊天背包 | `ChatModule.prefab/ChatBagPanel` / `ChatBagPanel` | 主编辑器、喇叭编辑器 | 条件页签、主/副两套 5 列网格 | 只盘点；列表仍为空 |
| 喇叭窗口 | `ChatModule.prefab/ChatTrumpetView` / `ChatTrumpetView` | 主聊天 `_trumpet` | 模态编辑器 | 只盘点；仅关闭可用 |
| 喇叭类型菜单 | `ChatModule.prefab/ChatTrumpetMenu` / Generated Bind | `ChatTrumpetView.t_gp_menu` | 世界/小跨服/全服条件菜单 | 只盘点；缺运行 View |
| 玩家菜单 | `ChatModule.prefab/ChatMenuView` / Generated Bind | 他人消息头像 | 条件动作列表 | 只盘点；缺运行 View，当前被资料卡直跳绕过 |
| 录音覆盖层 | `ChatModule.prefab/VoiceChatView` / Generated Bind | `btn_speak` 长按手势 | 录音/取消/倒计时三态 | 只盘点；缺运行 View |

## 共享修改影响面

本轮唯一触及的共享 Chat 组件是 `ChatParentTab`。它只在 Chat 资产闭包内复用，但有两种实质不同宿主：

1. `ChatParentView.Content_tab`：8 类条件频道页签，选中后切换消息列表和编辑区。
2. `ChatBagPanel.Content11`：2 个固定页签加 5 个条件页签，选中后切换物品网格。

运行复验时不能只抽主聊天页签；应至少验证上述两个宿主各一个代表，并覆盖选中/未选中、短/长标签、列表滚动三类状态。任一宿主失败，需回到 `ChatParentTab.prefab/View` 根因，不得在宿主页复制选中态逻辑。

## 明确排除的共享岛

本轮没有修改 `MainUI`、`CommonModule`、共享物品格/装备详情、共享 UI 特效、Addressables 或任何其它路线。聊天背包未来接物品展示时，必须先重新核对已有共享物品 Prefab/View 身份和消费者分组，本轮台账只记录缺口，不以 Chat 专用副本替代。

