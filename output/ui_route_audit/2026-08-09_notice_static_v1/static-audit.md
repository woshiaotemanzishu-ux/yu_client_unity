# Notice / 系统叠层静态审计（schema 6 v1）

## 结论

Notice 文件岛当前只有 `NoticeController` 与 `NoticeModel`，协议解包后保存 raw 并发事件；全仓没有 `EVT_SYS_NOTICE` / `EVT_CHUANWEN` 消费者，也没有 Notice/sysInfo 可编辑 Prefab。因此玩家侧的 11020 浮动提示、11015/11018 三类传闻叠层、富文本格式化、聊天副本、自动消失和遮挡生命周期均未闭环。

本轮没有代码或资源修改。原因不是局部可确定缺陷，而是首次落地所需最小闭包跨越共享 `UILayer.Top/Message/UI`、ChatModel、`config_language_extra`、Goods/Scene/导航链接和多模块目标页；用户明确禁止修改这些共享/禁区文件，且禁止 Unity/浏览器，无法取得 convert-module 所需运行快照。

## 老端完整分流

- `11020`：`Message.show(content)` → `SysInfoMiniMgr.MessageType.One` → `MessageItem`；最多 3 条同显、200 条缓存，缩放淡入、上移、2 秒自动回池。
- `11015/11018`：先以 `module_id@id` 查询 `config_language_extra`，按 `#&` 参数替换内容，处理颜色/属性/物品/玩家/场景等标签。缺配置、空结果或专项条件失败时不显示。
- `type=1/5`：标准 `ChuanwenItem`（Top，y=220）；`type=4`：珍稀 `ChuanwenSpecItem`（Top，y=330）；`type=6`：`ChuanwenItem2`（Top，y=160）；`type=2`：只写聊天；`type=3`：净化链接后浮动提示，且 subtype 非 0 时同时写聊天。
- subtype 非 0 会构造聊天频道消息；内联标签可进一步跳物品、玩家、功能、活动、场景等目标。该链归 Chat/共享路由，Notice 本岛不得复制。
- `11050` 定时公告由 Unity Chat 岛接收并发触发事件，但仍无叠层消费者；它与 Notice 的最终显示共享组件，作为跨岛 blocker 记录。
- `11019` 老端未注册，服务端只有 writer 无调用，保持不接。

## Unity 差异

- `NoticeController` 静态注册 11015/11018/11020，11018 通过 `FigureProto.Read` 保持字节对齐；但 Figure 内容被丢弃。
- `NoticeModel` 仅保存最后一条 11020 和最多 50 条原始传闻；没有配置解析、类型/subtype 路由、显示队列或共享层消费者。
- `NoticeModel.Clear()` 全仓无调用，断线/换角残留风险未闭环。
- `Assets/GameRes/resource/game/sysInfo` 仅见 `mainui_ui_45.png`；标准传闻 `bg.png`、珍稀 `uity_087.png` / `ui_icon.png` 等未形成可证明的 Unity 资源闭包。

## 验证边界

- 已做：目标岛起始 clean 核对、老端 TS/scene/config 与 Unity code/resource 三方静态调和、协议 writer/调用点静态核对、schema6 init/apply/validate、定向 `git diff --check`。
- 未做：Unity、浏览器、真实账号、真实收包、两 viewport、像素 diff、动态双帧、cold/warm、遮挡/重开、账号事务；全部按 NVR/blocked 记录。
- 文档未更新：用户明确禁止修改 Docs；本轮也未改变架构/代码，仅新增独立 output 审计证据。
