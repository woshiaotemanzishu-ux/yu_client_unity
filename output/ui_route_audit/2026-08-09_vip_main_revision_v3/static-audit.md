# VIP 主页面 revision-v3 静态审计

本版保留 `2026-08-09_vip_main_revision_v2` 不变，修正其充值候选拓扑。权威 `cdn/assets/resource/config/server/config_recharge_product.json` 的 SHA-256 为 `e521ad0f7d93b6a38c154c3df49ac970e33e60c73e108db0c32054b07c42d421`，共 95 行商品；其中 `product_type in (1,2)` 只有 7 行，均为 `product_type=1, product_id=2..8`，不存在可静态冻结的 type2 候选。

## v3 拓扑修正

- 删除 v2 中无权威配置证据的 7 个 `type2 × product_id 2..8` 候选页及其直接叶。
- 保留 7 个 type1 候选。候选不等于可见：显示仍必须与 15800 有序快照取交集，保留 wire 顺序和重复。
- type2 仍只存在于 15901 动态模板：冻结 state 0/1/2、`left_count`、支付/领取/领完和详情返回，不伪造商品数或 product_id。
- 卡型、每卡 3 条 `show_tips`、左右特权 9/12/15、15 个福利等级、216 条等级特权、60 个专享奖励格、54 个周礼包格和完整逐格详情链均从 v2 原样保留。

## 计数口径

`card_privilege_counts=9/12/15` 只统计 `left_show_tip + right_show_tip`；每种卡另有 3 条 `show_tips`，合计展示文案为 `12/15/18`。`VipRuleView` 的 type 1/2/4 规则行分别为 `10/12/15`，不与前两种口径混用。

## 实现与验证边界

- 代码只实现 VIP 岛内的只读快照、页签/卡型选择、头部状态、充值返回和生命周期；15800 快照保留顺序与重复，15801 只更新所有既有匹配项且不插入缺失项。
- 所有 45001/45002/45003/45007/45008/15902 与平台支付叶均 `blocked`；本轮没有账号写事务授权，不点击、不发送。
- 所有其余叶均 `needs-runtime-verify`。禁止启动 Unity/浏览器，因此没有真实 H5/Unity Web、像素、列表拖动、条件态、声音、cold/warm、即时刷新与重开证据，不能静态标为 `done`。
