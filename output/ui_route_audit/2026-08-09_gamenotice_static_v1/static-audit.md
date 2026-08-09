# GameNotice 静态三方调和（schema 6 v1）

## 范围与事实源

- Unity 专属代码：`Assets/Scripts/Module/Core/GameNotice/`。
- Unity 专属 Prefab：`Assets/Prefabs/UI/GameNotice/GameNoticeModule.prefab`。
- 老端源码：`E:/GitProject/yu_client/h5/src/gameNotice/`；场景：`E:/GitProject/yu_client/h5/bin/gameNotice/`。
- 协议/配置只读依赖：Unity `LoginNoticeModel/LoginNoticeService/LoginFlow/LoginController`、老端 `LoginModel`、`10207`。
- 共享边界：`MainUI/Welfare/Activity/Common/Generated/Proto/Addressables` 只读核对，不修改。

## 三方调和结论

1. GameNotice 是老端 Welfare 的“游戏公告”内嵌页签；Unity 因 Welfare 外壳未落地，同时以登录 Popup 和 417 直达 Popup 承载相同内容页。入口/外壳差异属于共享禁区。
2. `10207` 是 S2C-only，负载为一个 `u8 type`；非零触发串行 CDN refresh。公告按登录/`open_inside`、时间、平台、服务器过滤；解析失败保留旧快照，已读按角色隔离。
3. 老端标题列表 `200x520/repeatY=6`，每项 `200x100`，含唯一点击、选中态和红点；Unity Bind/模板节点齐全。
4. 老端正文是 `520x883` 可滚 Panel，内容 VBox 按真实高度增长；支持 HTML 颜色、动态详情行和 `show_wx_img` 二维码条件块。Unity 当前仅展平为 TMP 纯文本，后三项逐叶 blocked。
5. 五张 `gameNotice/ui_notice_*` Unity 位图与老端源文件 SHA-256 一致，资源本轮无需改动。

## 本轮最小修复

- 标题 `ScrollRect/Viewport/Content` 原 Content 无布局与高度适配，动态克隆会重叠且不能形成可滚高度。为专属 Content 增加 `VerticalLayoutGroup` 与 `ContentSizeFitter(vertical=preferred)`，宽度固定为 200。
- 正文 `ScrollRect.content` 原绑外层空包装 Content，真实动态项位于 `_gp_item`，且 `_gp_item` 不消费运行时 `LayoutElement.preferredHeight`。现将 content 改绑 `_gp_item`，令其布局控制子项宽高，并增加 `ContentSizeFitter(vertical=preferred)`。
- 未修改共享入口、Welfare 外壳、通知队列、协议、Generated Bind、Addressables 或资源。

## 完整控件/状态/依赖清单

- 入口：登录公告、游戏内 417；登录/游戏内模式；进服关闭和会话重置。
- 页：公告主视图、标题列表、标题项、正文面板、正文分节。
- 标题项：根唯一点击面、标题文字、选中图、未读红点。
- 正文：节标题、普通正文、HTML 颜色、详情行、条件二维码。
- 条件：空公告、标题数大于 6 的下箭头、登录/inside 过滤、红点规则 1/2/3/4。
- 滚动：标题列表与正文列表的 Content 位移、裁剪、末项可达、切换回顶、箭头到底隐藏。
- 协议/配置：10207 S2C、CDN cfg/cfg.v、时间/平台/服过滤、解析失败保留、角色隔离已读。
- 返回链：老端 Welfare 外层返回；Unity 登录/游戏内 Popup 关闭、进服关闭、关闭重开。
- 组件依赖：`GameNoticeViewBind`、`GameNoticeListItemBind`、`GameNoticeContentItemBind`、`ScrollRect/RectMask2D/VerticalLayoutGroup/ContentSizeFitter/LayoutElement/TMP/Image`。

## 静态验证与运行缺口

- YAML 头和对象块保留；所有本地 fileID 引用可解析；新增 fileID 唯一。
- `git diff --check` 通过；Prefab/资源 SHA 记录由收口命令生成。
- 未启动 Unity、浏览器或真实账号；未执行 build。因此两个结构修复为 `needs-runtime-verify`，其余运行/视觉/协议叶为 `blocked`，没有任何节点伪标 `done`。
- 真实复验至少需要：老端与 Unity 同账号、720x1280 与宽屏两 viewport；1/6/7+ 标题；短/长/HTML/二维码正文；标题/正文真实拖动；箭头滚到底；红点各规则；login/inside cold/warm；关闭、进服、重开即时一致性。
