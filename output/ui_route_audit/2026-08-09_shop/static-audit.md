# MainUI → 商城静态审计（2026-08-09）

## 边界

- 路线：`mainui.shop`，schema 6；老 H5 同账号、同状态、同 viewport 的真实表现仍是最终权威。
- 只修改 `Assets/Scripts/Module/Core/Shop/**`、`Assets/Prefabs/UI/Shop/**` 与本审计目录。
- 未启动 Unity，未操作浏览器，未登录账号；没有发送 `15302/15304/15307/64001`，没有执行购买、刷新或领取事务。
- 编译、Prefab YAML 与台账通过只代表静态实现，不代表 Unity/真实 Web、协议事务或玩家可见像素完成。

## 老端事实

### 外窗与一级页签

`ShopView.ts` 声明 11 个一级分支：限购(1)、灵玉(2)、绑玉(3)、抢购(99)、善缘(16)、荣耀(8)、冲霄(18)、神陨禁区(9)、天境(14)、九天神祭(17)、神霄御府(15)。当前老端 `tab_new_cond[3]` 明确恒为 `false`，抢购页签不可见；限购/灵玉、公会、战场、跨服圣域、跨服单排、神庭分支另有外部功能开放条件。九天神祭使用 `LonglangExchangeView` 与 `ui_bg_1.jpg`，其余常规页使用 `ShopCommonView` 与 `ui_Shop_bg1.jpg`。

### 二级页签与商品列表

- 灵玉：热销/稀有/常用/圣仪天衣；善缘：善缘/武魂；仅保留当前数据实际拥有商品的系列。
- 商品列表为三列纵向滚动；`trigger_task_id` 未满足项过滤；终生限购已售罄项沉底；切换一级/二级页签回到顶部。
- `ShopItem` 与 `ShopLimitItem` 是共享商品格。折扣现价使用四舍五入；限购分每日/每周/终生；条件矩阵包括 `lv/vip/rank_dun_level/constellation_equip/god_pool_lv/guild_lv/guild_title`。
- 普通商品与大额限购进入 `ShopBulkPurchaseView`，支持 -10/-1/+1/+10、剩余上限、总价、货币不足态与 15302 确认。

### 专属分支

- 抢购：64000 列表、倒计时、三列商品、旧价/现价/日限购/总限购与 64001 购买；当前入口被老端硬门禁隐藏。
- 神秘商城：15303 数据、倒计时、刷新消耗、15304 刷新、15307 购买、已购态与刷新命中特效。Unity Prefab/Generated Bind 已存在，但运行时 View 仍缺失。
- QuickBuy 不在 Shop 模块专属闭包；本轮仅登记跨岛依赖，未修改 Common/Bag。

## 本轮静态落地

1. `ShopFlow`
   - 按当前老端硬门禁隐藏抢购页签。
   - 为九天神祭传入独立 `ui_bg_1.jpg`，其余页保持 `ui_Shop_bg1.jpg`。
   - 接管烘焙的 `ShopBulkPurchaseView`，打开时放到 Popup 层，关闭商城时先关弹窗，释放模块前安全挂回模块根。
2. 动态列表生命周期与布局
   - `ShopSeriesTab`、`ShopItem`、`ShopLimitItem`、`ShopVieItem` 克隆统一走 `BaseView.Show/Hide`，确保 `BindNodes/OnInit` 和点击绑定实际执行。
   - `ShopModule.prefab` 的常规商品 Content 与抢购 Content 增加三列 `GridLayoutGroup + ContentSizeFitter`；二级页签 Content 增加纯横向 `HorizontalLayoutGroup + ContentSizeFitter`。
   - 一级/二级切换停止惯性并回顶；重建前 Hide 后销毁，避免重复回调。
3. 商品与批量购买
   - 折扣价由整数截断改为 `Mathf.RoundToInt`，对齐老端 `Mathf.Round`。
   - `ShopItem/ShopLimitItem` 按老端规则进入批量弹窗或单件购买，不再把所有商品强制直购 1 件。
   - 新 `ShopBulkPurchaseView` 落地数量步进、限购上限、货币可买上限、总价颜色、关闭/取消、确认与购买成功关闭链。

## 共享组件与状态矩阵

| 组件 | 直接消费者/形态 | 本轮结论 |
| --- | --- | --- |
| `BaseAwardItem` | 普通格、限购格、批量弹窗、神秘格、抢购格；图标/数量/品质/详情点击 | Common 共享身份链，禁止在 Shop 复制；当前是主要跨岛 blocker |
| `ShopItem` | 灵玉、绑玉等普通店铺；无限购/有限购、充足/不足、条件开/关 | 共享 View 保留；运行态矩阵待验 |
| `ShopLimitItem` | 限购、荣耀、勋章、冲霄、天境、神庭、善缘等；折扣/无折扣、售罄/可买 | 共享 View 保留；运行态矩阵待验 |
| `ShopSeriesTab` | 灵玉四系列、善缘两系列；选中/未选中、短/长文案 | 同一模板；横向布局已静态补齐，运行态待验 |
| `BaseWindowSkinView` | 商城外窗 11 个声明页签、每页背景 | Common 只读消费；外部功能开放条件无法在 Shop 内猜测 |

## 真正阻塞

- `BaseAwardItem` 的共享 Prefab/View 身份与资源闭包尚未完成，商品图标、货币图标、品质框、数量和详情弹窗不能在 Shop 内复制实现。
- 一级页签的 Alpha、公会、战场、跨服圣域、跨服单排、神庭开放条件属于外部模块；当前 Unity 除抢购硬门禁外仍未接，不能静态宣称完成。
- 九天神祭仍错误复用 `ShopCommonView`；正确 `LonglangExchangeView` 在 Longlang 岛，Shop 代理只修了确定的背景，不跨岛改实现。
- 神秘商城虽有 Prefab/Bind/协议/配置，但缺 `ShopMysteriousView`/`ShopMysteriouItem` 运行时 View；刷新/购买又是未授权写事务。
- 商品条件只实现 `lv/vip`；排行副本、星座、神池、公会等级/职位依赖外部权威状态。
- 批量购买的绑玉不足→是否以灵玉补足确认框依赖公共 Alert；本轮禁止改 Common，因此保持 blocked。
- QuickBuy 位于共享购买链，不在本轮 Shop 专属闭包。
- 所有购买/刷新成功回包、即时父页刷新、关闭重开，以及两档真实 Web 像素/滚动/裁剪/性能仍需授权运行。

## 验证

- `dotnet build Shenxiao.Module.Core.csproj --no-restore` 通过：0 error，99 个当前工作树既有 warning。构建通过 `shop-extra-compile.targets` 仅补入 Unity 尚未刷新到 csproj 的本轮 `ShopBulkPurchaseView.cs`；同时临时纳入并发 Guild 新文件以避开与 Shop 无关的 csproj 时序错误。
- `verify-static.ps1` 校验 C# 关键语义、Prefab 布局组件/GUID、清单 JSON、schema 6 validator 与写事务未执行声明。
- 正式状态以 `route-ledger.json` 的 validator 输出为准；当前 65 节点为 `blocked=31`、`needs-runtime-verify=34`、`not-run=0`。

## 文档边界

本轮未修改 Docs，不是因为结论无文档价值，而是父任务明确禁止 Shop 代理改 Docs；主控总账应合并本路线的静态实现、共享依赖和真实运行闸。
