# RedPacket 静态路线审计（schema 6）

## 边界

- v1 的 45 节点账在其 manifest 被并发改成 62 节点后产生不可修补的 SHA/拓扑不匹配；v2 又按旧 45 节点拓扑重新冻结。两者均保留为 superseded 现场，v3 是唯一权威的完整 62 节点台账。
- 既有 `RedPacketModule.prefab`，本轮按 `audit-game-ui-route → fix-view` 增量核对，没有 convert/rebake/Creator。
- 未启动 Unity、浏览器或前台程序；未登录账号、未模拟服务器回包、未执行领取或发送。
- 老 H5 同账号、同状态、同 viewport 的真实运行表现仍是唯一验收目标；本目录只证明静态拓扑、协议边界和独立 C# 编译。

## 完整路线树

- 主窗：Activity 遮罩、标题、入口红点、说明 339、关闭/背景返回。
- 页签：记录/功能；老端常规异步顺序为先发 33901、随后同步固定 `SwitchBar(0)`，回包中的记录数量条件分支是否有同步瞬时可达性留待真实运行核对。
- 红包列表：ScrollRect、`RedPacketMainItem` 身份、头像/名称/货币、Look/Open/Send/Other 四态、缓存详情、发送弹窗、33907/33908 刷新。
- 记录列表：空/非空、时间、配置文案、颜色/长文案、纵向滚动与末项。
- 功能列表：108/23/21/72/61/62/50 七项、VIP 版本分支、盛宴提示、OpenFun 返回链、翻译滚动分支。
- 发包弹窗：系统/VIP 两态、份数加减、两种计算器、祝福输入、金额/份数约束、33904/33906、遮罩返回。
- 详情弹窗：具体 View 身份、头部、本人领取态、领取者列表、最佳手气、货币分支、遮罩返回。
- 路由状态：33901、33900、33907/33908、33903/33905 absent、排序/红点、资源、声音、cold/warm、双 viewport。

## 最小确定性实现

- `RedPacketMainView.OnShow` 只订阅模型更新并发送安全只读 33901。
- 打开时固定记录页，33901/33907/33908 更新事件不覆盖之后的用户页签选择。
- 页签按钮、说明 339、关闭链已绑定；动态列表模板继续隐藏，未以静态占位冒充运行态完成。

## 协议与授权

- 允许的本轮主动协议：仅 33901 列表读取。
- `33902` 同时承载打开详情与真实领取，`33904`/`33906` 真实发送红包：全部只枚举，`blocked`，没有发包。
- `33903`/`33905` 按 R482 保持 KILL/absent：没有 Unity 注册、发送方法或 UI 调用点，绝不复活。
- 仅已静态接线但缺少真实运行证据的叶为 `needs-runtime-verify`；其余缺实现、写事务或 absent/KILL 叶均为 `blocked`。

## 静态证据

- 老端源码：`E:/GitProject/yu_client/h5/src/redPacket/**`、`commonController/RedPacketController.ts`、`commonModel/RedPacketModel.ts`。
- Unity：`Assets/Scripts/Module/Core/RedPacket/**`、`Assets/Prefabs/UI/RedPacket/RedPacketModule.prefab`，Generated 仅只读核对。
- 独立编译：`RedPacket.StaticCompile.csproj`，产物与 obj 均限制在本输出目录。

## QA 状态回卷

- 39 个只有生成绑定/模板/模型字段、但没有可达运行时 View/列表项/弹窗实现的叶，由 `needs-runtime-verify` 回卷为 `blocked`。
- 回卷覆盖入口红点与组合关闭、页签选中样式、红包/记录/功能列表、控制弹窗除发送事务外的本地交互、完整详情弹窗，以及 push/排序红点/资源/声音消费。
- 原有 33902/33904/33906 四个写事务叶继续 `blocked`。本轮没有修改 manifest 拓扑或 RedPacket 业务代码。
- 33903/33905 的 `kill-absent` 是必须维持的静态负边界，同样回卷为 `blocked`，不再作为运行验证候选。
- `results-static-qa-correction.json` 是后续原子修正批次：对上述 40 个回卷叶显式提交 `runtime_gap=null`，清除旧 `needs-runtime-verify` 遗留说明；正式账只通过 `route_ledger.py apply` 更新。
