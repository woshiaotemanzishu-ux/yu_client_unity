# SevenDay UI 精修路线静态收口

- 路线：`mainui.activity.sevenday`
- schema：6
- 节点：79
- 状态：`blocked=22 / defect=51 / baseline-only=5 / needs-runtime-verify=1`
- 父节点均由 `route_ledger.py apply` 派生；根路线为 `blocked`，没有任何叶子伪标 `done`。

## 起始 dirty 与结束差异

- 开始前 `Assets/Scripts/Module/Core/SevenDay`、预期 SevenDay Prefab 岛、目标 output 目录均无 tracked/untracked dirty。
- 结束时仅有两个 SevenDay 私有 C# 修改和本目录审计产物；没有写入 Activity、MainUI、Welfare、Vip、TopVip、GrowthBenefits、Shop、Daily、Common、Generated、Proto、ClientConfigSync、Addressables、Docs、AGENTS 或项目文件。
- C# 差异：`SevenDayController.cs +17/-5`，`SevenDayModel.cs +28/-0`。

## 静态确定的最小修复

1. `SevenDayModel` 从 17500/17502 的逐日 `status==1` 数据按 `act_type/day_type` 派生三入口红点。
2. `SevenDayController` 在 `AddIconAsync` 前写入红点缓存，保证异步建图标时带上当前状态。
3. 删除图标前先清 `ActivityIconManager` 红点缓存，避免 175、175@8、175_1 后续重建沿用旧状态。
4. 未新增 17501/17503 常量、注册、sender、handler、乐观领取态、本地发奖或重试。

## 三方调和结论

- 老端源码/配置：三入口分别打开 `SevenDayView`、`SevenEightDayView`、`SevenMergeView`；每页 7 个日签、最多 4 个奖励格、不可领/可领/已领取三态、页签/入口红点、关闭链；17501/17503 成功后更新领取态、即时刷新并打开 `CongratulationObtainView`。
- 老端运行：本轮禁止浏览器，且没有与当前源码、账号、viewport 绑定的既有 SevenDay 真实证据，全部如实保持未运行。
- Unity：仅有 17500/17502 只读图标地基；无三套可编辑 Prefab/View/Bind、无两张登录奖励配置、无 SevenDay 专属资源闭包消费者，`MainUIRouter` 也无 175/175@8/175_1 路由。

## Blocker / NVR

- `convert-module` 前置阻断：目标没有 Prefab，但本轮既缺老端真实运行快照，又禁止 Unity 烘焙/回填，不能安全首转。
- 17501/17503 是真实背包/邮件发奖并持久化领取态的 hard-negative，本轮没有写事务授权；领取协议、权威即时刷新、成功弹窗和关闭重开均为 `blocked`。
- 双 viewport、old/unity/overlay/diff、像素、列表拖动/末项、模型/特效双帧、cold/warm、返回链运行证据均未采集。
- `ActivityIconManager`、`MainUIRouter`、`EquipmentItem`、`CongratulationObtainView` 属于共享/跨模块依赖，只登记影响面，未抢写共享文件。

## 验证

- `route_ledger.py init/apply/validate`：通过，schema 6 拓扑 79 节点，最终状态分布与上文一致。
- JSON 全量解析：通过。
- SevenDay 定向静态合同：红点派生、异步建图标前缓存、删除前清缓存均存在；未出现 17501/17503 实现入口。
- `git diff --check`（两份 SevenDay C#）：通过。
- 未执行 build、Unity、浏览器、Computer Use、GM 或真实账号事务；整套合并编译由主控串行协调。

## 产物

- `route-manifest.json`：不可变拓扑清单。
- `route-ledger.json`：正式 schema 6 台账。
- `route-results.json`：本批紧凑结果。
- `static-reconciliation.json`：老端源码/配置与 Unity 静态三方调和、SHA-256、起始 dirty。
- `component-dependencies.json`：共享组件/消费者/状态矩阵与跨岛 blocker。
- `starting-dirty.json`：起始目标岛 dirty 快照。
- `generate_sevenday_audit.py`：只生成 manifest/results/证据文件，不直接写正式 ledger。

本轮不触发文档更新：改动仅限 SevenDay 私有红点消费，且用户明确禁止写 Docs/AGENTS；架构、公共组件、协议/构建流水线均未改变。
