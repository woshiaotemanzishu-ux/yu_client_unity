# Attention/关注 UI 静态路线 v1

## 结论

- schema 6 冻结 32 个节点，覆盖两套独立页面、入口条件、动态资源、奖励列表、复制、关闭、SDK 三态按钮、红点、33105 领奖和成功后即时关闭链。
- Unity 现状不是“无 Prefab”，而是“有可编辑 Prefab/Bind 骨架、无业务 View/可达打开链”。因此按 `fix-view` 边界没有重转，也没有把占位骨架当完工。
- 本轮未修改 Attention 代码或 Prefab。缺口跨越 MainUI、Activity/CustomActivity、平台 SDK、Common 物品格、Configs/Addressables；按文件岛约束逐叶 blocked/defect。

## 状态摘要（以最终 ledger 为准）

- `blocked`：入口/渠道配置/SDK/CustomActivity/33105/共享组件/资源闭包等跨域叶子。
- `defect`：两页缺业务 View，动态视觉、奖励实例化、按钮、复制、红点、关闭和即时状态均未接。
- `needs-runtime-verify`：SDK 页静态背景 Sprite 已绑定，但没有真实页面打开与像素证据。
- `done`：0；未执行 Unity、Web、账号或平台动作。

## 文件与验证

- 仅新增本目录的 manifest、results、三方清单、报告、静态验证记录和由通用工具生成的正式 ledger。
- 验证：schema 6 `init/apply/validate`、manifest SHA 绑定、目标文件岛 `git diff --check`、目标 dirty 复核。
- 未执行：Unity/浏览器/Computer Use、全 Core/Unity/WebGL build、GM/真实账号、33105、SDK subscribe。

## 文档边界

用户明确禁止修改 `Docs/`，且本轮没有改变架构、公共组件、协议或运行行为；因此只保留专项 output 证据，不更新正式文档。
