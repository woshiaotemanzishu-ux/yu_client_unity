# Attention Unity 源码/Prefab 清单（静态）

## 文件岛

- `Assets/Scripts/Module/Core/Attention/AttentionModel.cs`
- `Assets/Scripts/Module/Core/Attention/AttentionController.cs`
- `Assets/Prefabs/UI/Attention/AttentionModule.prefab`
- 只读 Bind：
  - `Assets/Scripts/Generated/UI/Attention/AttentionViewBind.cs`
  - `Assets/Scripts/Generated/UI/Attention/AttentionViewLayaBind.cs`
  - `Assets/Scripts/Generated/Config/ConfigClientAttention.cs`
  - `Assets/Scripts/Generated/Config/AttentionCfg.cs`

目标模块与 Prefab 在本路线开始时 `git status/diff/untracked` 均为空。

## Prefab 拓扑

- 根 `AttentionModule` 有两个子页面：
  - `AttentionView`：578×453，默认 active；挂载的仅是自动生成 `AttentionViewBind`。
  - `AttentionViewLaya`：682×928，默认 inactive；挂载的仅是自动生成 `AttentionViewLayaBind`。
- 两个 Bind 的序列化字段均非空；奖励模板均位于各自 `__Templates` 下。
- 通用页的 `bg/title/bg1/link/bg3` 等动态图片多数仍是转换占位 `com_empty` Sprite，符合老端“运行时赋图”的设计，但 Unity 没有业务 View 消费这些字段。
- SDK 页背景 Sprite 静态存在；奖励列表、按钮状态、红点与关闭语义仍需要业务 View。

## 静态缺口

1. `Assets/Scripts/Module/Core/Attention/` 只有 Model/Controller，没有任何继承 `AttentionViewBind` 或 `AttentionViewLayaBind` 的业务 View。
2. 全 `Assets/Scripts` 未找到 `AttentionModule`、`AttentionViewLaya` 或 `AttentionView` 的运行时打开消费者；只存在生成 Bind 类型。
3. Controller 当前只管理 113/113113 图标显隐，并明确把面板/二维码/复制/领奖列为待办。
4. `AttentionOpen`、`IsAiFengGamePlatform`、SDK 状态、活动奖励态只有预留写入口，未发现真实共享层调用方。
5. 源码注释“两个图标均无协议”只适用于图标控制器自身；完整 SDK 页面确有经 CustomActivity 发出的 33105 领奖事务，台账已单列，未把它误记为无协议页面。

本轮未启动 Unity，也未读取玩家可见像素；Prefab 的 Sprite/层级存在不等于页面已出帧或可点击。
