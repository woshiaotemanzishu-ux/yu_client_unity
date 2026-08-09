# RedPacketRain 静态三方调和（2026-08-09）

## 结论

本路线没有可在 RedPacket 文件岛内安全编辑的 Unity UI。老端由三块组成：CustomActivity 内嵌的 `RedPacketRainView`、MainUI 顶层的掉落红包 `FlowerEffectView`、Top 层抢包结果弹窗 `RPRShowView`。Unity 当前只有共享 `CustomActivity` 的 33155/33157/33158 协议与 Model 数据片段，以及 `MainUI/Views/FlowerEffectView.cs` 的日志占位；未找到 RedPacketRain 专属 Prefab、Generated Bind 或独立模块目录。

因此，本轮不调用转换器、不修改 RedPacket/CustomActivity/MainUI/Common/Generated/Addressables，只建立 schema 6 静态台账，并把全部叶节点标为 `blocked`。

## 老端页面与状态

- `RedPacketRainView.ts`：活动内嵌页，打开即按 `sub_type` 请求 33155；最多五个波次槽位，固定位置为 `(208,466)`、`(391,466)`、`(122,643)`、`(303,643)`、`(486,643)`。
- 激活条件来自活动 `condition`：`rain_value` 决定充值/活跃度阈值，`rain_time` 决定波次数和间隔，`wave_envelopes_num` 决定展示奖励。
- 页面覆盖未开始、等待下一波、正在开抢、已结束、已领取/未领取；每秒更新倒计时，归零重拉 33155。`clear_type=2` 使用 4 点清算边界。
- `_btn_go` 在 `condition=recharge` 时跳 `OpenFun(21)`，在 `condition=activity` 时跳 `OpenFun(82)`。
- `RedPacketRainItem.ts`：未领取占位点击打开 `RewardPreView`；已领取使用共享 `EquipmentItem` 86×86 展示奖励；波次等待态独立倒计时并在销毁时清理。
- `FlowerEffectView.ts`：波次出现后在 MainUI 的 14 个挂点生成红包，随机缩放 `0.7..1`，以 `10..12s` 线性下落；任一红包点击会隐藏雨层并打开 `RPRShowView`；清理时停止每个 tween 并隐藏节点。
- `RPRShowView.ts`：Top 层、带背景、立即销毁；初态按钮发送 33157。成功码 1 展示奖励，错误码 3310071 展示抢完态，其他失败关闭；成功/抢完态按钮关闭。

## 协议与即时刷新链

- 33155 C2S 只有 `SubType`；S2C 为 `SubType/ActValue/Wave/StartTime/ClearType/WaveReceive[]`。
- 33157 C2S 只有 `SubType`；S2C 为 `Errcode/SubType/Wave/Rewards[]`。成功后老端与 Unity 共享 Model 都回写当前波次的领取态。
- 33158 为 recv-only 的 `SubType/Wave/StartTime`。老端发 `WAVE_RED_PACKET`，并在 `wave==1` 追发 33155；Unity 共享 Controller 也有同样追发。
- 老端页面还会在波次事件、抢包结果、充值成功及倒计时归零时重拉 33155。

## Unity 资源/组件归属

- 未找到 `RedPacketRain`、`RPRShowView` 或 `RedRain` 对应 Unity Prefab/Generated Bind/模块文件。
- `Assets/Scripts/Module/Core/CustomActivity/CustomActivityController.Festival.cs` 与 `CustomActivityModel.Festival.cs` 是共享活动代码，不属于 RedPacket 文件岛。
- `Assets/Scripts/Module/Core/MainUI/Views/FlowerEffectView.cs` 属于明确禁止修改的 MainUI，且红包雨仅日志占位。
- 老端活动宿主、共享 `EquipmentItem`、`RewardPreView` 和 `OpenFun` 对应 Unity 闭包都跨越本轮禁止区，无法证明最小可编辑资源闭包。

## 未运行门禁

用户本轮明确禁止 Unity、浏览器、构建和账号写事务。因此未执行真实 Web 两 viewport、像素差、14 个红包双帧运动、滚动/点击、33157 写事务、即时刷新、关闭重开、cold/warm 与零残留门禁；这些缺口没有被静态证据冒充完成。
