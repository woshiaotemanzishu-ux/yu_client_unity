# AddVipService 静态复核修订 v2

本版本保留上级目录 v1 台账，按 schema 6 拓扑不可变规则另建。

## v1 遗漏与修正

- 图标逻辑类型始终为 114，但 `ActivityIconManager` 会按当前渠道行改写展示 `icon_name/open_lv/open_day`。
- 入口完整条件拆为：非审核态、15908 `is_buy==1` 已锁存、`plat_name` 命中 14 个顶层渠道之一、渠道行动态等级/开服天门槛。
- 渠道行 1：13 个渠道，展示图标 114，`open_lv=90`、`open_day=0`，`ui_gz_title/image001`，奖励为空。
- 渠道行 2：`yy_suyou`，展示图标 117，`open_lv=150`、`open_day=0`，`ui_gz_title2/image002`，奖励 3 格。
- 无匹配配置行时，页面返回而不填充动态内容；必须验证不会残留上次渠道状态。
- 三个奖励格分别为 `[0,35,200]`、`[0,31,200000]`、`[0,37020002,1]`，均保留独立 EquipmentItem 详情/返回叶；空奖励分支单列。

## 事务与实现边界

- 页面自身没有充值、领取、购买或领奖 transaction 叶，唯一显式页面点击为 close。
- Unity 现有 Prefab/Bind 保留，但缺业务 View 和 `ClientAddVipService` 消费链；相关接入会越出禁止修改的 ClientConfigSync/MainUI 文件岛，因此仍为 `blocked`。
