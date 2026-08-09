# UI 精修项目级执行队列（2026-08-09）

## 完成口径

- 本文件只负责排程，不是正式路线台账，也不把历史 schema 2～5 升级为当前完成。
- 每页先由老端运行树、老端源码/配置、Unity Prefab/Bind 三方生成完整控件树，再用 `route_ledger.py init` 新建 schema 6 台账。
- 已有可编辑 Prefab 的页面只走 `fix-view` 增量修复；确实没有可编辑 Prefab 的页面才走 `convert-module` 首次落地。
- 代码、Prefab、资源和正式 Web 按逻辑批次构建；共享根、Addressables、正式台账 apply 和总文档由主控串行合并。
- 页面最终 `done` 必须绑定同账号老 H5 → Unity Web 顺序复走、两档 viewport、Player/catalog/源码 dirty 指纹和 Headless 报告。

## 当前事实

- 已发现 15 份台账、837 个节点；其中当前 schema 6 仅 2 份，历史只读 13 份。
- `Assets/Prefabs/UI` 有 316 个 Prefab，134 个 `*Module.prefab`；后续主体是现有 Prefab 增量修复，不是全量重新转换。
- 当前角色主线：成就在收尾；勋章、幻化尚未形成完整可达闭环；旧角色总账不能直接代表当前事实。
- 当前工作树的成就、共享窗口、奖励飞行、Addressables、特效与总文档修改均为保护区，其他路线不得并发触碰。

## 依赖岛与唯一写者

| 岛 | 主要范围 | 写入规则 |
|---|---|---|
| 角色主线 | Role/Achievement/Medal/Unreal/Fashion/Designation | 单一角色负责人；模型、RoleModule、RoleFlow 不与 Pet 并发 |
| 主窗壳 | BaseWindowSkin/BaseWindowManager/全屏背景 | 全项目单一负责人 |
| 物品详情 | Bag/Equip/Shop/Rune/Composite/奖励格/详情 | 共享 Prefab 和品质特效单一负责人；页面宿主可只读并行 |
| HUD 入口 | MainUIRouter/ActivityIconManager/configfunctionicon | 单一负责人；页面路线不得顺手补 Router |
| 社交 | Chat/Friend/Email/Guild/Team/Marriage/RedPacket | 页面专属文件可并行；奖励/入口跨岛时只报告依赖 |
| 场景任务 | Map/Task/Dialogue/OnHook/AutoBrush/Daily | 页面专属文件可并行；场景生命周期和玩家状态集中复验 |
| 活动商业化 | Shop/Vip/Festival/CustomActivity/Boss/排行 | 先补入口与账号配方，再做写事务；共享物品根串行 |

## 执行波次

### Wave 0：事实合并与总控（正在执行）

1. 生成项目总控 JSON/Markdown，明确 schema 6 与历史只读边界。
2. 合并角色旧账、专项文档、当前代码和用户运行证据，输出当前剩余叶。
3. 固定角色、共享组件和 Addressables 保护闭包。

### Wave 1：三条低冲突并行线

1. 角色：完成勋章增量修复和九霄冥饰首次落地准备，并把成就剩余项集中到下一真实包。当前老端 `EquipmentView._Group6` 在账号 111111（Lv.260）上真实隐藏，布局也序列化为 `visible=false`，因此 `OpenFun 113` 只能作为代码历史，不能冒充当前可达入口；后续必须由真实开放态或冥饰物品的 `OpenFun 195` 重新采证。目标仍是 `SecretTreasureMainView(Unreal tab) → UnrealBagView`；因为共享外窗还暴露 Rune/MonBook/Lung/GodBeast 四个同级页签，其完成边界属于“秘宝容器依赖岛”，不能用无页签的独立 Unreal 窗代替。
2. Chat：建立 schema 6 全控件树，修 Chat 专属实现，禁止改 MainUI/Common。
3. Friend/Email 或 Map：建立 schema 6 全控件树并做页面专属修复；谁先完成静态闭环就先进入批次。

### Wave 2：玩家主链

1. Task → NPC Dialogue → TaskFinish。
2. GuildList → GuildMain → GuildHelp。
3. Bag → Warehouse → ItemTips；冻结共享物品根后再接 Equip。

### Wave 3：共享物品与养成

1. Equip → 强化/洗炼/宝石/精炼。
2. Shop、Rune/Treasure、Composite/Red、Daily/资源找回。
3. 对共享物品、详情和品质特效按消费者形态抽 2～4 个代表宿主。

### Wave 4：模型、活动与长尾入口

1. Pet、Halo、Fashion/Dress 等角色外观/3D 页面。
2. Vip/Recharge、Festival、SevenDay、CustomActivity、Boss。
3. 统一治理动态图标落 `MainUIRoutePlaceholder` 的 Router 长尾；不得把图标显示当页面可达。

## 构建与真实 Web 批次

- Unity 当前由用户前台占用；未经本轮另行确认，不启动、关闭、切焦点或批处理 Unity。
- 等 Wave 1 的 C# 与 Prefab 逻辑批次冻结后，再申请一次构建窗口；C# 汇总重打壳，Addressable-only 在验证过的内容基线上只打内容。
- 构建等待期间继续做下一页老端基线、manifest、资源闭包和协议静态核查，不让三个子智能体空闲。
