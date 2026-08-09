# VIP 家族 UI 路线静态收口

日期：2026-08-09。

本目录汇总 VIP、TopVip、AddVipService、LevelReward、GrowthBenefits 五条路线的 canonical schema 6 静态结果。全程未启动 Unity、浏览器或前台程序，未操作账号，未点击充值、领取或购买；因此没有任何路线被标记为 `done`，静态实现不能替代真实老 H5 / Unity Web 运行验收。

## Canonical 台账

| 路线 | canonical ledger | 节点 | 叶 | 节点状态 |
|---|---|---:|---:|---|
| VIP | `output/ui_route_audit/2026-08-09_vip_main_revision_v4/route-ledger.json` | 1347 | 1013 | blocked 83 / needs-runtime-verify 1264 |
| TopVip | `output/ui_route_audit/2026-08-09_topvip_static_v1/revision-v2/route-ledger.json` | 133 | 110 | blocked 133 |
| AddVipService | `output/ui_route_audit/2026-08-09_addvipservice_static_v1/revision-v2/route-ledger.json` | 28 | 24 | blocked 28 |
| LevelReward | `output/ui_route_audit/2026-08-09_levelreward_revision_v2/route-ledger.json` | 779 | 618 | blocked 672 / needs-runtime-verify 107 |
| GrowthBenefits | `output/ui_route_audit/2026-08-09_growthbenefits_revision_v2/route-ledger.json` | 901 | 728 | blocked 897 / needs-runtime-verify 4 |

所有 canonical ledger 均经 `route_ledger.py validate` 通过；所有叶均显式为 `blocked` 或 `needs-runtime-verify`，无 `done`、无 `not-run`。VIP 的 47 个交易叶、TopVip 的 11 个交易/破坏性叶、LevelReward 的 41701 与 GrowthBenefits 的 41722 均保持禁止点击和发送。

VIP v1/v2/v3、TopVip/AddVipService v1、LevelReward/GrowthBenefits v1 均按 schema 6 拓扑不可变规则保留，不能替代上表 canonical 版本。

## 已落静态实现

- VIP：`VipModel` 增加只读变更通知及保序/保重复的 15800 快照；15801 只更新已有匹配项且不插入。`VipBaseView` 接入两页签、1/2/4 卡选择、只读 VIP 头部、充值互返和精确关闭生命周期；`RechargeView` 接入只读头部、返回/关闭、满级显隐。`VipModule.prefab` 只增量修复现有页签 Content 布局。
- LevelReward：消费现有 `RushGiftModel` 的 41700 缓存与更新事件，按老端 received 排序/状态刷新；现有 Prefab Content 增加纵向布局。41701 不发送，奖励配置、主动 41700 请求和成功/失败链保持 blocked。
- TopVip：Unity 仅有孤立 `TopVipShopItem.prefab`，完整模块、四页、弹窗和业务 View 缺失。首次转换需要 Unity/老 H5/Addressables 写入与运行验收，受本轮禁令阻塞，未伪造转换结果。
- AddVipService：Prefab/Generated Bind 已存在，但业务 View、配置消费链和可达入口缺失；安全修复需要越出文件岛，未改源码/Prefab。
- GrowthBenefits：Prefab/Generated Bind 已存在，但业务 View、GrowthForce 外壳路由和完整配置消费链缺失；未改源码/Prefab。

## VIP 运行配置证据

VIP canonical v4 按老 H5 实际 `ResUrl/resource` 加载链绑定证据：

- `E:/GitProject/yu_client/cdn/resource/config.zip` SHA-256：`031984617dbcf27128265961b76014b7a4e68c7d047de46e6448ae4fcf2b3ac9`
- ZIP 内 `config.json` SHA-256：`aeea254274a99f9b092328d476bcae72b78fdbfe119700144e66ab4d0acca43a`
- PRELOAD `config_recharge_product`：95 项；静态候选仅 type1 `product_id=2..8` 共 7 个，type2 为 0；15901 保持运行态动态模板。
- 卡型 1/2/4，每卡 3 条 show tips，左右特权分别 9/12/15；15 个 VIP 等级、216 条等级特权、60 个专享奖励格、54 个周礼包奖励格均已逐叶展开。

完整证据、对象 canonical SHA 与加载链文件见 `output/ui_route_audit/2026-08-09_vip_main_revision_v4/route-manifest.json` 和 `verification.md`。

## 静态验证

- `VipRoute.Isolated.csproj`：0 warning / 0 error。
- `VipModel.OrderedSnapshot.csproj`：`VipModel ordered snapshot case: PASS`。
- `LevelReward.Isolated.csproj`：0 warning / 0 error。
- Prefab YAML：VIP 1002、LevelReward 139 个对象定义；均无重复定义、无悬空本地 `fileID`。
- 七个业务源码/Prefab 改动执行 `git diff --check` 通过。
- VIP 禁止写协议 `45001/45002/45003/45007/45008/15902` 在目标源码中 0 命中；LevelReward 目标 View 仅记录 41701 blocked，不调用发送入口。

## 仍需真实运行验证

像素、两档 viewport、列表真实拖动/裁剪/末项、逐格详情身份、弹窗返回链、声音、动态状态、cold/warm、即时刷新、关闭重开及真实 Web 指纹均未运行。后续必须在与源码和 catalog 匹配的真实 Unity Web 包上，与当前老 H5 同账号顺序复走；交易叶仍需单独授权。

本轮明确禁止修改 Docs/AGENTS，故没有更新项目文档索引；审计记录仅落在用户授权的 `output/ui_route_audit` 文件岛。
