# VIP 主页面 revision-v2 静态审计

本版保留旧 `2026-08-09_vip_main` 的 45 节点台账不变，以新 manifest 冻结完整拓扑。权威标准仍是当前老 H5 与 Unity Web 在同账号、同状态、同 viewport 下的真实玩家表现；本轮禁止启动 Unity、浏览器和账号写事务，所以没有任何叶被静态标成 `done`。

## 冻结清单

- 特权卡：type 1/2/4，各 3 条 `show_tips`；左右特权分别为 6+3、8+4、10+5，共 36 条。
- 福利：VIP1–15；逐级特权数 `10,12,12,15,16,16,15,15,15,15,15,15,15,15,15`，共 216 条；专享奖励 15×4=60 格；周礼包 2×3+4×12=54 格。114 个奖励格逐格展开为“格视觉→具体详情 View 身份→主底图→关闭按钮→背景返回”，未用 aggregate detail 冒充。
- 充值：type1/type2 × product_id 2..8 共 14 个候选。候选不等于显示；显示必须与 15800 有序快照取交集，并保留 wire 顺序和重复。
- 15901：只冻结动态商品模板、state 0/1/2、`left_count`、支付/领取/领完、详情/返回；未伪造具体商品数量。奖励格按动态模板展开完整具体详情链。
- `ActivityRechargeShow`：冻结配置/等级/活动列表/fallback 9999 可见链、真实/假行、奖励格、逐格具体详情、跳转、支付、关闭与返回。
- `VipTipsView.get_btn`：老端 45007 调用已注释，实际只 `Close()`；因此免费领取只归 `card.action`，不存在 `free-tips.claim` 写叶。

## 静态实现边界

- `VipBaseView` 只接现有快照、页签、卡型 1/2/4 选择、充值页路由、精确经验文案和显隐；卡型默认 4，有效卡为 `IsActive==1 && (Time==0 || Time>NowSec)` 后取最大 `CardType`。关闭后下一次默认福利页。
- `RechargeView` 只接返回、关闭、只读头部与近似滚到底；满级隐藏左右成本与钻石。老端 `scrollTo(8)` 尚无等价静态保证，保持 `needs-runtime-verify`。
- `VipModel` 的 15800 快照保留顺序和重复；15801 只更新全部既有匹配项，不插入缺失项；字典属性仅保留兼容。
- 所有充值、购买、领取、领奖和 VIP 显示写入叶均 `blocked`；未发送 45001/45002/45003/45007/45008/15902，未调用平台支付。

## 配置 SHA-256

| 配置 | SHA-256 |
|---|---|
| `config_vip_card.json` | `0f17f7a6da10828bedbceac7336c93c39fb96579db374155cff9c8682058f73b` |
| `config_vip_config.json` | `dfdb45285cfe664c05badcb0c250a5861c2d581be5f8939057596401066fbda6` |
| `ClientVipPrivilege.json` | `fd546ce92c9ec19ab99df36d69761481d56082545060631ef5500cf3015770b2` |
| `config_recharge_product.json` | `e521ad0f7d93b6a38c154c3df49ac970e33e60c73e108db0c32054b07c42d421` |
| `config_recharge_return.json` | `4021f4d6d87ed69308b736e8c7bede8a501700387b1e347d7d8b68b4a423d7f4` |
| `ClientRechargeShow.json` | `403a3d2f3fe59f035d5ede0d7ae9bf80a904410558381e50b7bc0b15b5c81c2f` |
| `ClientVipWelfare.json` / Unity `clientvipwelfare.json` | `4f5ccb17d2e3877a2271624ca2836a17bf5b51d85c049abacd6082ef4c8d9182` |

## 未决运行态门禁

所有非写叶均为 `needs-runtime-verify`：同账号真实 H5/Unity Web 的像素、条件显隐、列表拖动裁剪、弹窗身份、声音、生命周期、cold/warm、即时状态与关闭重开均未执行。所有写叶均为 `blocked`，其父路线由叶状态派生，不能收口为完成。

## Addendum：特权卡计数口径

`card_privilege_counts=9/12/15` 只统计 `left_show_tip + right_show_tip`；每种卡另有 3 条 `show_tips`，若把两类展示文案合计则为 `12/15/18`。两种数字描述的是同一份配置，不是两套冲突拓扑；`VipRuleView` 的 type 1/2/4 规则行则分别为 `10/12/15`，需在真实运行态继续逐行核对。
