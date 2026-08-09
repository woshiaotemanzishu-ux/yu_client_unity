# Friend → Email 首轮静态核查

## 本轮可静态确定并已修复

- `FriendChatTabItem` 现在按老端 `index==0` 语义使用 `uilt_018` 选中底图，其他会话使用 `uilt_019`；修复前 `isActive` 参数完全未消费。
- `FriendMenuView` 现在消费条目点击的屏幕坐标，并通过 `RectTransformUtility.ScreenPointToLocalPointInRectangle` 换算到 Window 父容器局部坐标。
- 菜单背景和根高度按老端 `buttonCount * 60 + 16` 伸缩。
- 菜单按钮不再以纯色块模拟，改为加载老端同名 `common/texture/com_rect_btn1` 与 `com_rect_btn3`。
- 上传头像满足 `picture == role_id` 时恢复老端“举报头像”条件按钮。
- 邮件详情恢复有/无附件时三层底板的 384/245、675/555、603/483 高度切换，以及领取/下一封按钮底图切换。
- 私聊气泡时间从简化的 `HH:mm` 恢复为老端 `MM - dd HH:mm:ss`。

## 仍需专属 Prefab/运行态收口

- `FriendMenuView` 的按钮节点仍由业务 View 运行时 `new GameObject` 创建。生成的 `FriendViewButtonSkinBind` / `MenuRedButtonSkinBind` 在当前 `FriendModule.prefab` 中没有实例，无法在不启动 Unity 且不猜 YAML 身份的前提下安全落模板；这是人工 Prefab 视觉所有权缺口。
- 菜单位置已接线，但仍需在 1280×720、720×1280 两档真实 Web 中验证边缘点击、按钮数 2/4/5 的越界与锚点。
- 好友列表/申请/黑名单头像依赖共享 `CustomHeadItem`；邮件附件依赖共享 `EquipmentItem`，需要按共享组件消费者矩阵做真实运行抽样。

## 跨岛缺口（本轮禁止修改）

- 私聊：表情面板、聊天物品面板、位置分享、赠花、正文富链接、防诈骗配置仍未接；`FriendChatViewBind` 位于 `Generated/UI/Chat`。
- 好友菜单：赠礼、举报头像仍只有降级日志；查看资料依赖 `LookOverFlow`。
- 邮件详情：正文 `color@`/`a@...open_fun` 富文本与跳转未接；非堆叠附件拆格、已领取灰罩未接。
- 邮件领取成功仍走 Mail 岛的 toast 降级，未接老端统一奖励展示/飞行动画。

## 运行闸

- 未启动 Unity，未操作浏览器，未做账号写事务。
- schema 6 台账全部保持 `not-run`；静态编译/断言不得替代真实 Unity Web、同账号协议回包、即时刷新、关闭重开与双 viewport 像素证据。
