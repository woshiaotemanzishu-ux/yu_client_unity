# TopPk 静态 UI 精修台账

结论：TopPk 当前不能做 `fix-view` 增量精修，因为仓库没有可编辑的 TopPk 业务 Prefab、Bind 或 View/Flow。现有 `TopPkController`、`TopPkModel` 与 `TopPkCase` 只覆盖 281xx 的安全读侧/推送快照；本轮没有发现可在唯一文件岛内、无需猜测即可修复的生产缺陷，因此没有修改生产文件。

## 已调和范围

- 老端：15 个 `TopPk*.ts` 页面/Item、15 个 `.scene`、`TopPkController`、`TopPkModel`。
- 配置：参与奖励 3 条、赛季排行奖励 8+8 条、关键参数 23 条、段位/每日奖励各 26 条。
- Unity：`TopPkController.cs`、`TopPkModel.cs`、`TopPkCase.cs`；业务 Prefab/View/Bind/配置消费者数量均为 0。
- 路线：73 个节点，其中 60 个叶节点；最终为 72 `blocked`、1 `needs-runtime-verify`、0 `done`。

完整控件树覆盖入口开放条件、赛季/段位/次数状态、排行榜列表、1/5/10 次参与奖励、购买次数弹窗、手动/自动匹配、取消、匹配成功、战斗 HUD、结果与返回、段位/排行/每日三个奖励页签、列表滚动、奖励详情、领取、红点、协议、即时刷新、重开和跨组件依赖。

## 关键边界

- `28102/28103/28104/28106/28110/28114` 是真实领取、购买、匹配或取消事务，保持 hard-negative；`28116` 是无 S2C ACK 的真实退出战斗场景写入。本轮均未点击、未接入。
- `28100/28101/28105/28107/28111/28112/28113/28115/28117` 的现有 Unity 注册保持不变；没有为缺失 UI 添加孤立发送器、乐观状态或本地战斗替身。
- 老端 `open_lv=180` 与服务端活动日历中观察到的 `start_lv=160` 不一致；必须固定真实版本/状态后再裁决，不能在 TopPk 岛内猜改配置。
- `EquipmentItem`/详情/恭喜弹窗、荣誉商店/充值、MainUI、战斗场景 7001/7002、声音与特效均跨文件岛，只登记 blocker。

## 验证级别

- schema 6：`route_ledger.py init/apply/validate` 通过。
- 真实老 Web、Unity Web、两档 viewport、像素 diff、滚动、动态特效/模型双帧、cold/warm：全部 NVR。
- 领取/购买/挑战/取消/退出场景后的即时刷新和关闭重开：全部 blocked。
- 未启动或操作 Unity、浏览器、Computer Use；未进行 GM 或账号写事务。

此轮只增加路线专属审计输出，不改变架构、公共组件、协议实现、构建发布方式或玩家行为；同时 `Docs`/`AGENTS.md` 被明确列为禁区，所以不触发文档更新。
