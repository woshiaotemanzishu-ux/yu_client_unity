# RedPacket 静态路线审计（schema 6）

## 边界

- 既有 `RedPacketModule.prefab`，本轮按 `audit-game-ui-route → fix-view` 增量核对，没有 convert/rebake/Creator。
- 未启动 Unity、浏览器或前台程序；未登录账号、未模拟服务器回包、未执行领取或发送。
- 老 H5 同账号、同状态、同 viewport 的真实运行表现仍是唯一验收目标；本目录只证明静态拓扑、协议边界和独立 C# 编译。

## 完整路线树

- 主窗：Activity 遮罩、标题、入口红点、说明 339、关闭/背景返回。
- 页签：记录/功能、首次 33901 后按 `record_list.Count > 0 ? 0 : 1` 选默认页；用户已选择后不被后续推送覆盖。
- 红包列表：ScrollRect、`RedPacketMainItem` 身份、头像/名称/货币、Look/Open/Send/Other 四态、缓存详情、发送弹窗、33907/33908 刷新。
- 记录列表：空/非空、时间、配置文案、颜色/长文案、纵向滚动与末项。
- 功能列表：108/23/21/72/61/62/50 七项、VIP 版本分支、盛宴提示、OpenFun 返回链、翻译滚动分支。
- 发包弹窗：系统/VIP 两态、份数加减、两种计算器、祝福输入、金额/份数约束、33904/33906、遮罩返回。
- 详情弹窗：具体 View 身份、头部、本人领取态、领取者列表、最佳手气、货币分支、遮罩返回。
- 路由状态：33901、33900、33907/33908、33903/33905 absent、排序/红点、资源、声音、cold/warm、双 viewport。

## 最小确定性实现

- `RedPacketMainView.OnShow` 只订阅模型更新并发送安全只读 33901。
- 首次快照到达时根据记录数量选择页签；之后用户页签选择不被更新事件重置。
- 页签按钮、说明 339、关闭链已绑定；动态列表模板继续隐藏，未以静态占位冒充运行态完成。

## 协议与授权

- 允许的本轮主动协议：仅 33901 列表读取。
- `33902` 同时承载打开详情与真实领取，`33904`/`33906` 真实发送红包：全部只枚举，`blocked`，没有发包。
- `33903`/`33905` 按 R482 保持 KILL/absent：没有 Unity 注册、发送方法或 UI 调用点，绝不复活。
- 所有其他叶均为 `needs-runtime-verify`：未发生真实 GraphicRaycaster 点击、滚动、资源 ready、弹窗身份、冷/热生命周期、声音或 H5/Unity Web 对比。

## 静态证据

- 老端源码：`E:/GitProject/yu_client/h5/src/redPacket/**`、`commonController/RedPacketController.ts`、`commonModel/RedPacketModel.ts`。
- Unity：`Assets/Scripts/Module/Core/RedPacket/**`、`Assets/Prefabs/UI/RedPacket/RedPacketModule.prefab`，Generated 仅只读核对。
- 独立编译：`RedPacket.StaticCompile.csproj`，产物与 obj 均限制在本输出目录。

