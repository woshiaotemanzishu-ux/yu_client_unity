# GrowthBenefits revision-v2 静态审计

## 冻结拓扑

- schema 6 修正版拓扑展开为 901 节点、728 叶。
- `config_grow_welfare_info` 冻结 7 天、36 任务：第1日6任务，其余每日5任务；每任务 1–3 个奖励格。
- 每日逐页签状态、逐任务描述/进度/status0跳转/status1领取/status2已领、awardList 横向拖动/裁剪/末格和逐奖励格详情四叶全部显式展开。
- 补齐 GrowthForce 动态背景/标题/名称、1/2页签居中、closeBox/背景返回、默认红点页、完成页签移除、7日页签滚动、`jump_id=0` 仍显示 jumpBox 时 `OpenFun(0)+关壳`、战力页身份/返回。

## 静态现状与 blocker

- 现有 GrowthBenefits Prefab 仍直接绑定 Generated Bind；岛内只有协议 Model/Controller，没有业务 View、外层 GrowthForce 路由、配置详情消费者和共享详情链。
- 因此除少量 Prefab/静态生命周期观察点为 `needs-runtime-verify` 外，所有功能叶显式 `blocked`；41722 领取全部是未授权写事务，禁止点击。
- 未修改 GrowthBenefits C# 或 Prefab，未启动 Unity、浏览器或前台程序。
