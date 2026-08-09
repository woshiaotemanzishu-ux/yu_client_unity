# Kaifu / Invest 静态三方调和

本轮只完成老 H5 源码与配置、服务器协议语义、Unity Prefab/Bind/Kaifu 代码的静态调和。没有启动 Unity、浏览器或 Computer Use，没有构建，没有真实充值、购买、领取、消费、GM 或账号写入。真实 old/unity Web、双 viewport、像素、滚动、特效双帧、cold/warm、即时刷新与关闭重开均未执行。

## 起始工作树与文件岛

- 起始精确 dirty：`Assets/Scripts/Module/Core/Kaifu`、`Assets/Prefabs/UI/Invest`、`Assets/GameRes/resource/game/invest`、本路线 output 均为空。
- 允许写入仅限 Kaifu 岛和本 output；实际未改 Kaifu C#、Invest Prefab 或专属资源。
- 共享外壳、奖励格、详情、Alert、成功弹窗、充值/VIP/MainUI 路由只登记 blocker。

## 三方结论

| 维度 | 老 H5 / 服务端 | Unity 当前 | 结论 |
| --- | --- | --- | --- |
| 页面集合 | 等级、月卡、巅峰、王者、至尊、神尊；type4 配置存在但没有 `ViewClassCFG` | Prefab 仅含 `LVinvestView`、`MonthCardView`、共用 `TopInvestView` | Prefab 结构能覆盖六个可见业务形态，但没有业务 View 接管 |
| 入口与页签 | 42004 驱动 1112/4205、动态页签、默认索引和红点 | Kaifu 仅维护活动图标；没有 Invest opener/容器绑定 | 页面不可证明可打开，shell 全部 blocked |
| 数据 | 42001 每 type 返回档位、购买时间、领取时间、登录天数、领取表 | `KaifuModel.InvestInfos` 能完整替换快照 | 静态编码一致；真实刷新 NVR |
| 购买 | 42002 `type:u8,lv:u16`，服务器真实扣费、持久化、奖励；月卡由 product 108 充值事件触发 | 没有 42002 注册、sender、single-flight、钱包刷新；当前规则明确硬负约束 | blocked，禁止孤立补 sender/ACK |
| 月卡 | 未购买走支付或 36210008>=8 的 15804；已购买按 get_time 每日领取 | MonthCard Prefab/Bind 存在，无 Pay/Vip/状态逻辑 | blocked，跨充值/Vip/Common 依赖 |
| 领取 | 42003 先持久化，再 `send_reward_with_mail`，回 ObjectList 只作展示 | 没有 42003；无即时列表/红点/成功弹窗 | blocked，禁止本地乐观领取或二次发奖 |
| 列表 | 横向档位列表、纵向奖励列表、状态排序、末项可达 | Prefab 有两套 ScrollRect→Viewport(RectMask2D)→Content 和模板 | 结构静态存在；拖动/裁剪/回顶 NVR |
| 组件 | BaseAwardItem、EquipmentItem、Alert、CongratulationObtainView、BaseWindowComponent | 都在 Kaifu 文件岛外 | 只登记 blocker，不修改共享文件 |
| 特效 | 两个 SelectItem 的 AddUIEffect 已被老端源码注释 | `_gp_effect` 宿主仍在 Prefab | 不应凭宿主存在宣称特效；真实两帧仍 NVR |

## Prefab / Bind 静态清单

- `InvestModule` 根下直接保存 `LVinvestView`、`MonthCardView`、`TopInvestView`。
- 八个 Generated Bind 在 Prefab 中各引用一次：三个 Level Bind、三个 Top Bind、两个 MonthCard Bind。
- Level/Top 均保存档位 ScrollRect、奖励 ScrollRect、购买按钮、列表模板、档位模板、红点、已领图和条件按钮。
- MonthCard 保存四卡容器、展示奖励宿主、购买/领取按钮、说明、剩余天数与红点。
- Prefab 根没有业务组件；Kaifu 岛也没有继承这些 Bind 的运行时 View。按 `fix-view`，现有人工可编辑 Prefab 不重转、不重建；没有足够老端真实像素证据时不做猜测性 YAML 视觉修改。

## Blocker

1. Invest 页面业务 View、窗口 opener 和动态页签/克隆/点击/状态绑定缺失；补齐会跨入 MainUI、Common、Generated、充值/Vip 等严格禁区。
2. 42002/42003 是当前硬负约束；恢复必须成套实现页面、配置、支付回调/去重、钱包/背包/邮件/称号、42001 刷新、错误与 single-flight，不能在 Kaifu 单岛孤立接入。
3. 本轮禁止真实 Web/Unity/构建/账号写事务，故不能生成可接受的像素、滚动、双帧、cold/warm、即时刷新/重开证据。

## 验证边界

- 允许：schema 6 `init/apply/validate`、JSON 解析、目标静态扫描、`git diff --check`。
- 未执行：任何 build、Unity、浏览器、Computer Use、充值/购买/领取/消费/GM。
- 结果状态只使用 `blocked` 与 `needs-runtime-verify`；无任何 `done`。
