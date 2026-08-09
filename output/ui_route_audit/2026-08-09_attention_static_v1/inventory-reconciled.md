# Attention 三方调和与依赖清单

| 路线/叶子 | 老端事实 | Unity 静态现状 | 结论 |
|---|---|---|---|
| 113 入口 | 渠道配置 + attention_open + 开服天/等级 + 非 alpha | 计算逻辑存在，但 `AttentionOpen` 无真实注入；MainUI 入口不在本岛 | blocked |
| 113 页面视觉 | 动态背景、标题、二维码、描述、奖励横排 | Prefab/Bind 骨架存在；动态图片仍占位且无业务 View | defect |
| 113 复制 | 平台剪贴板，成功提示 | 无点击/剪贴板业务 View | defect |
| 113 关闭 | 明确关闭按钮，立即销毁列表项 | 只有 Bind，无关闭/清理实现 | defect |
| 113113 入口 | 爱疯 + SDK enabled + 活动奖励未领 | 只有 nullable 状态与外部回填入口，无调用方 | blocked |
| 113113 奖励列表 | CustomActivity 奖励字符串解析为 EquipmentItem | 只有模板/容器，无活动/共享物品格业务 | defect / shared blocker |
| 113113 未关注点击 | SDK askShow + 失败提示 | 无平台桥与业务 View | blocked |
| 113113 已关注领奖 | 33105(70,1,1)，成功后即时刷新 | 真实奖励写事务；本轮禁止账号写，协议所有权在 CustomActivity | blocked |
| 113113 红点/文案 | SDK关注态 × 奖励态条件刷新 | Model 缺 `sdk_attention_state/red_state` 完整消费，View 不存在 | defect |
| 113113 关闭/切场景 | 背景关闭、切场景关闭 | 只有生成 Bind | defect |

## 组件依赖与所有权

| 依赖 | 老端用途 | Unity 现状 | 本轮处理 |
|---|---|---|---|
| MainUI `ActivityIconManager` | 113/113113 添加、删除、红点、点击入口 | Attention Controller 只能增删图标，入口消费者属 MainUI | blocker，不修改 |
| ClientAttention/渠道配置 | QR、公众号名、描述、奖励、开启门 | 生成访问器存在；实际资产闭包/Addressables 未证明 | blocker，不修改 Configs/Addressables |
| 平台 SDK `subscribe` | enabled/isSubscribe/askShow | 无真实 Unity 平台桥调用方 | blocker，不伪造 |
| CustomActivity `(70,1)` / 33105 | 奖励状态、领奖、刷新/关闭 | 属 Activity/共享协议域 | blocker，不修改/不发事务 |
| Common `EquipmentItem` / Goods 映射 | 两页奖励格 | Prefab 有模板但无 Attention 业务实例化；共享组件禁止修改 | blocker，不复制组件 |
| Clipboard + Message | 复制公众号、成功/失败提示 | Attention 无消费者 | defect；因平台差异与共享提示不在本岛，未实现 |
| BaseView 背景/层级/切场景关闭 | 页面遮罩和返回链 | 无 Attention 业务 View | defect |

## 运行证据边界

真实 Web、两 viewport、像素/层级、滚动、cold/warm、即时刷新、重开、SDK 回调与 33105 事务均为 NVR/blocked。本轮没有把 Prefab 绑定完整、静态 Sprite 存在或源码行为推断写成 `done`。
