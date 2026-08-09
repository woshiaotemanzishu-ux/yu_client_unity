# RedPacket / 红包静态三方调和

## 本批边界

- 仅执行老端源码/配置调用点、Unity Prefab/Bind/业务代码的静态调和。
- 未启动 Unity、浏览器或 Computer Use；未使用账号、GM、消费、领取、发包或购买；未执行 build。
- 因而本批不能产生老端运行树、真实 Unity Web、两 viewport、像素 diff、滚动拖动、cold/warm、即时刷新或关闭重开完成证据。

## 老端页面与控件事实

- 主窗 `RedPacketMainView`：打开即空包请求 `33901`；上方动态红包列表；下方“记录/获取途径”两个页签；说明 `339`；关闭返回。
- 动态红包卡 `RedPacketMainItem`：按 `status/receive_status/self role` 显示 `打开/发送/查看/其他人红包` 四态。`打开/查看` 走 `33902`，本人未开启红包进入 `RedPacketCtrlView`。
- 详情 `RedPacketDetailView`：角色头/name、领取数量/总数/金额、本人是否领取、领取者列表、手气最佳状态、背景关闭。
- 发包 `RedPacketCtrlView`：数量加减、数量计算器、VIP 金额计算器、祝福输入/计数、发送、背景关闭；物品/系统红包走 `33904(id, splitNum)`，VIP 红包走 `33906(money, splitNum, msg)`。
- 记录页：按 `config_red_envelopes.desc`、角色名与 `mm-dd hh:MM` 渲染记录。
- 获取途径固定 7 行：`108/23/21/72/61/62/50`。其中 `61/62` 跳转前关闭红包主窗，`50` 仅提示；老端标题写 VIP4，但 `RedPacketFuncItem` 特判的是 `104` 而列表给的是 `108`，不能由 Unity 猜改。

## Unity 当前闭包

- 可编辑 Prefab：`Assets/Prefabs/UI/RedPacket/RedPacketModule.prefab`，包含主窗、Ctrl/Detail generated Bind 和 Main/Func/Record/Detail 四类模板。
- 业务文件岛：`Assets/Scripts/Module/Core/RedPacket/`。Controller/Model 已具备 `33900/33901/33902/33904/33906/33907/33908` 与配置加载；`33903/33905` 明确封存。
- 只有 `RedPacketMainView` 是业务子类；Ctrl/Detail 与四类 item 仍是 generated-only Bind。主窗会隐藏全部 `_tpl_*`，且未克隆/渲染任何动态列表。
- `RedPacketFlow.OpenSub` 按业务类型名查找，但 Prefab 中子窗类型名仍为 `RedPacketCtrlViewBind/RedPacketDetailViewBind`，所以旧语义名称查找不会命中。

## 本批最小修复

- `RedPacketMainView.OnShow` 补回 `33901` 首屏请求。
- `_btn_record/_btn_func` 从日志占位改为切换 `_Group2/_Group3`。
- `_btn_help` 从日志占位改为复用既有 `InstructionFlow.Show(339)`。
- `EVT_REDPACKET_UPDATE` 订阅与 `OnHide/OnDispose` 解绑成对，避免热重开重复监听。

这些改动仅达到静态确定的业务接线；均保持 `needs-runtime-verify`，没有宣称真实点击或视觉完成。

## 组件依赖与文件岛边界

| 组件 | 归属/身份 | 当前消费者 | 本批结论 |
|---|---|---|---|
| `RedPacketModule.prefab` | RedPacket 私有 Prefab | `RedPacketFlow` | 可编辑，但本批不改 Prefab |
| `RedPacketMainItemBind` | Prefab 内模板 | 主窗红包列表 | generated-only，未渲染 |
| `RedPacketRecordItemBind` | Prefab 内模板 | 记录页 | generated-only，未渲染 |
| `RedPacketFuncItemBind` | Prefab 内模板 | 获取途径页 | generated-only，未渲染 |
| `RedPacketDetailItemBind` | Prefab 内模板 | 详情领取者列表 | generated-only，未渲染 |
| `CustomHeadItem` | Common 共享组件/嵌入模板 | 红包卡与详情 | 只登记依赖；禁止修改 Common，运行态身份未验 |
| `InstructionFlow` | Common 共享服务 | 说明 339 | 只调用现有公开入口；未修改共享文件 |
| `MainUIRouter` / HUD | MainUI 共享入口 | `redpacket` 路由 | 只读确认；禁止修改 MainUI，真实入口点击 blocked |

## 明确 blocker / NVR

- 动态红包、记录、获取途径、详情、发包表单都尚未有业务 renderer，静态确定为 defect。
- `33902/33904/33906` 属领取/消费/发包事务，本轮明确禁止账号写事务，不能执行。
- MainUI 入口、所有射线点击、ScrollRect 结构/真实拖动、目标弹窗身份、即时刷新、关闭重开、资源幂等、two viewport、Web 哈希、像素证据全部未运行。
- Generated、Common、MainUI、Configs、Addressables 与 Docs 均未修改。
