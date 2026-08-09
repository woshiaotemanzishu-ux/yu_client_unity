# MainUI → 市场静态审计（2026-08-09）

## 边界与结论

- 路线：`mainui.market`，schema 6；老 H5 同账号、同状态、同 viewport 的真实运行表现仍是最终权威。
- 本轮只读核对老端 `MarketBaseView/Buy/Plz/Sell`、`MarketController/MarketModel`、配置、Unity Market 数据层及现有 Market Prefab；没有启动 Unity、浏览器或任何前台程序，也没有登录账号。
- 没有发送任何 151xx 请求；购买、上架、下架、发起/撤销求购、出售给求购单、市场喊话全部未执行。
- Unity 当前只有完整的 Market 数据层与一个 `MarketPlzShowItem.prefab` 模板；没有 `MarketBaseView/MarketBuyView/MarketPlzView/MarketSellView` 页面 Prefab、业务 View 或页面点击路由。因此本轮没有可按 `fix-view` 安全增量修复的完整页面，也不允许越界调用 `convert-module`。
- schema 6 清单共 `376` 个节点、`335` 个叶子；叶子为 `blocked=320`、`needs-runtime-verify=15`。父状态回卷后正式台账总计 `blocked=361`、`needs-runtime-verify=15`、`done=0`。静态枚举和 validator 通过不代表 Unity/真实 Web 完成。

## 老端完整控件树

正式逐控件拓扑在 `route-manifest-v2.json`；以下为可人工阅读的同构摘要。动态列表格、全部配置选项和条件按钮都已在 manifest 中拆为独立叶子，不用一个“列表已看过”代替。

### 入口与外窗

1. 主界面入口
   - 本服图标 `151`；跨服开放后切换为 `151@1`。
   - `15121.open_time`、服务器时间、跨天及等级变化共同决定图标刷新。
   - 服务端市场统一 `open_lv=90`；低于 90 级请求会静默无回包。
   - 点击应进入 `MarketBaseView`。
2. `MarketBaseView`
   - 标题：本服 `market.uisc_001`，跨服 `market.uisc_001_kf`。
   - 背景：三个现行页均为 `ui_bg_1.jpg`；货币栏 `money_list=[2,1,0]`。
   - 一级页签仅有：购买、求购、出售。
   - 拍卖与记录的 import、`viewClassList`、`tabStrList` 已被注释，不是当前可见页签。
   - 关闭按钮返回主界面。

### 购买页

1. 大类横栏：8 项，顺序为男性装备(1)、女性装备(5)、饰品装备(6)、圣印装备(4)、共鸣材料(2)、其他材料(3)、神契装备(7)、启示圣铠(8)。
2. 二级格：当前配置共 69 格，购买页包含各类“全部”。

| 大类 | 二级格 |
| --- | --- |
| 男性装备 | 全部、武器、铠甲、头盔、裤子、护腕、鞋子 |
| 女性装备 | 全部、武器、衣服、发饰、裤裙、护手、鞋子 |
| 饰品装备 | 全部、项链、护符、手镯、戒指 |
| 圣印装备 | 全部、足部、头盔、护腿、护腕、铠甲、武器、晶核、脸具、项链、戒指、圣器 |
| 共鸣材料 | 全部、共鸣石(男)、共鸣石(女)、战魂共鸣石、饰品共鸣石、万物共鸣石、神之源核、灵之源核 |
| 其他材料 | 全部、垂神翼影幻化、神兵幻化、古法符相幻化、经验符、神巫幻化、地境晋升、图谱、剑魄同修碎片、兑换材料、道具碎片、妖灵升级、妖灵碎片、妖灵技能书 |
| 神契装备 | 全部、武器、头盔、铠甲、饰品 |
| 启示圣铠 | 全部、武器、头盔、项链、上衣、护符、下装、手镯、护手、戒指、鞋子 |

3. 商品列表控制
   - 15101 一级挂单数量；点击二级格发 15102。
   - 品质下拉 6 项：品质/紫色/橙色/红色/暗金/粉色。
   - 阶数下拉 17 项：阶数/1阶…16阶。
   - 星级下拉 6 项：星级/0星…4星。
   - 单价标题升/降序切换；15114 显示购买次数；跨服未开时显示“N天后市场开启跨服交易”。
   - 列表空/有数据态、真实滚动、裁剪和末项可达都属于独立叶。
4. `MarketShowItem`
   - `EquipmentItem`、名称/数量/阶数/单价、up/down/ban、指定交易玩家名。
   - 物品图标按类型打开装备、圣印、神装、启示、龙狼、时装或普通详情。
   - 条件按钮：普通非本人为“购买”；指定交易非本人为“出售”；本人挂单为“撤销”。
   - 15111 购买、15117 出售、15108 撤销均是未授权写事务，全部 blocked。

### 求购页

1. 二级页签：求购列表、我要求购。
2. 求购列表
   - 范围单选：全部（15118，page=1/size=999）、我的（15119）。
   - 单价升/降序、空/有数据、纵向滚动与末项。
   - `MarketPlzShowItem` 显示物品、名称、阶数、玩家、单价。
   - 非本人且库存足够时“出售”打开 `MarketSellTipView`；库存不足只提示；本人显示“撤销”并发送 15116。
   - 玩家名点击链在当前老端源码中已注释为空操作，不能猜成聊天菜单。
3. 我要求购
   - 同样的 8 个大类。
   - `GetTypeCfgAll(start_index=2)` 排除“全部”，当前 61 个可求购二级格；每格已在 manifest 独立登记。
   - 选择二级格后显示候选 `MarketPlzGoodsItem` 列表；无候选提示“没有物品可供求购”。
   - 品质 6 项、阶数 17 项、星级 6 项筛选。
   - 候选选择态、空态、滚动/裁剪、末项独立验收。
   - 固定价分支：单价、相对基准百分比、±10% 按钮与上下限。
   - 自定义价分支：总价输入、CalculatorView、min/max 钳制。
   - 数量：-1/+1、CalculatorView、1..配置上限；上限<=1 时按钮与文本输入禁用/置灰。
   - 总价、VIP 税率/税额、货币充足/不足即时刷新。
   - 15115 发起求购会消费货币，blocked。
4. `MarketSellTipView`
   - UI 层居中弹窗、背景遮罩/点击背景关闭、关闭按钮。
   - `EquipmentItem`、名称×数量、价格、VIP 税率、扣税后收益。
   - 确认发送 15117 并消耗背包物品，blocked。

### 出售页

1. 背包筛选：神装/装备、普通物品两个页签。
2. 可售背包列表
   - 只纳入可交易、未绑定且具备自定义价或交易价的实例。
   - 五列 `MarketSellItem`，空态、滚动、裁剪、末项。
   - 物品格带锁定/数量、up/down/ban；点击按类型打开共享详情，并由详情里的 upShelf 操作进入 `MarketSellCtrlView`。
3. 我的上架
   - 15109 列表；标题“已上架物品 N/上限”，当前 `market_max_sell_num=1`。
   - 两列 `MarketSellGoodsItem`、空态、滚动/裁剪/末项。
   - 物品详情带 outShelf 操作；喊话按钮发送 15122；下架发送 15108。二者都是服务器写入，blocked。
4. `MarketSellCtrlView`
   - Activity 层居中弹窗、遮罩/点击背景关闭、关闭按钮。
   - 物品、拥有量、默认开启的喊话复选框。
   - 固定价/自定义价两分支、价格 ±10%、价格 CalculatorView、百分比/总价/税额。
   - 数量 -1/+1、数量 CalculatorView、拥有量与配置上限；上限<=1 禁用。
   - VIP/税率；15106 上架确认。上架会消耗或锁定背包实例，blocked。

### 当前隐藏的历史分支

- 拍卖：只残留 scene 资源，业务 import 和页签均注释。
- 记录：`MarketRecordDealView/MarketRecordItem` 仍能静态解释 15112、时间排序、买入/卖出行和空态，但当前 `MarketBaseView` 没有入口；Unity 也没有页面 Prefab/View。

## Unity 静态现状

### 已有数据层

- `MarketController.cs` 已注册 15100/15101/15102/15106/15108/15109/15111/15112/15114/15115/15116/15117/15118/15119/15120/15121/15122，并提供对应请求或被动处理。
- `MarketModel.cs` 已保存跨服状态、一级/二级列表、我的上架、记录、购买次数、全部/我的求购及增删改语义。
- 15103/15104/15105/15107/15110/15113 被明确排除：协议缺失、老端空消费或服务端死链，不能为了“号段齐全”注册或发送。
- 以上只是静态数据链；本轮禁止运行，所以 15 个只读/被动数据叶保持 `needs-runtime-verify`。

### 缺失页面与唯一现有 Prefab

- `Assets/Prefabs/UI/Market` 只有 `MarketPlzShowItem.prefab`；没有 Market 外窗、购买、求购、出售、上架弹窗、出售确认或记录页 Prefab。
- `MarketPlzShowItem.prefab` 根节点只挂 `Shenxiao.Generated.UI.Market.MarketPlzShowItemBind`，没有 Market 业务子类负责数据、按钮显隐、15116/15117 或 `Show/Hide` 生命周期。
- 该 Prefab 嵌套 Common `EquipmentItem`，同时被 `Assets/Prefabs/UI/Chat/ChatModule.prefab` 作为 `_tpl_MarketPlzShowItem` 直接消费。它已经是跨页面共享模板；没有 Chat 代表抽查与运行证据时不能随意改其视觉/根身份。
- 因完整页面 Prefab 不存在，`fix-view` 没有可安全增量修的页面；代理的文件岛又明确禁止转换缺页和修改 Common/MainUI/Chat，所以 320 个 UI/事务/生命周期叶均 blocked，而不是用静态通过冒充完成。

## 协议与写事务边界

| 类别 | 协议 | 本轮状态 |
| --- | --- | --- |
| 只读/被动 | 15100、15101、15102、15109、15112、15114、15118、15119、15120、15121 | 静态实现存在；`needs-runtime-verify` |
| 写事务 | 15106 上架、15108 下架、15111 购买、15115 发起求购、15116 撤销求购、15117 出售、15122 喊话 | 全部 `blocked`，零发送 |
| 明确排除 | 15103、15104、15105、15107、15110、15113 | 保持不注册/不发送；静态负约束待运行环境复核 |

写事务若后续获授权，不能只验“发包成功”：必须覆盖等待中、成功/失败回包、父页即时刷新、关闭重开、背包/货币/次数变化与可恢复状态。15122 也会写入公共和单物品喊话冷却，不能当纯只读按钮。

## 共享组件依赖

| 组件 | 消费形态 | 当前边界 |
| --- | --- | --- |
| `MarketPlzShowItem.prefab` | 老端求购行；Unity ChatModule 模板 | Market 路径下的跨路线共享 Prefab；无代表抽查不改 |
| `EquipmentItem` | 购买行、求购行、候选、背包格、上架格、两个弹窗 | Common 共享组件，文件岛只读 |
| `DownDropBtn` | 购买/求购的品质、阶数、星级 | Common 动态下拉，文件岛只读 |
| `CalculatorView` | 求购/上架的价格与数量输入 | Common 弹窗，文件岛只读 |
| 多类 ToolTip | 购买详情、出售背包详情、上架详情 | Common/多模块弹窗身份链，文件岛只读 |
| `BaseWindowComponent` | MarketBaseView 外窗、页签、货币、关闭 | Unity Market 页面实现缺失 |

## Blocked 与 needs-runtime-verify

- `blocked`：所有购买/上架/下架/求购/出售/喊话事务；所有缺完整页面 Prefab/View 的 UI 叶；跨岛 Common/Chat 共享组件；当前老端隐藏的拍卖/记录；真实滚动、像素、关闭重开、cold/warm、资源幂等。
- `needs-runtime-verify`：Unity 已有的本服/跨服图标判定、等级/时间门禁、15100/15101/15102/15109/15112/15114/15118/15119/15120/15121 数据处理，以及排除号负约束。它们只表示“静态实现存在但未运行”，不表示 UI 或 Web 已通过。

## 验证与文档边界

- `build_inputs.py` 仅生成当前 `route-manifest-v2.json` 与 `results-static-v2.json`；正式 `route-ledger-v2.json` 只由通用 `route_ledger.py init/apply` 原子写入。v1 保留首次初始化快照；配置计数标签修正后按拓扑不可变规则另建 v2，没有原地改旧账。
- 已执行 Python 语法检查、manifest 初始化、results 原子 apply、schema 6 validator、文件岛静态断言与 `git diff --check`；未创建共享 csproj、未触发 Unity 编译或资源导入。
- 本轮没有修改 C# 或 Prefab：当前缺陷是“完整页面尚未落地且本代理无转换授权”，不存在有证据支持的岛内最小修补。强改唯一共享 item Prefab 会扩大到 Chat 消费者。
- 父任务明确禁止本代理修改 Docs/AGENTS；因此本轮只在授权的审计目录保存事实和台账，由主控决定是否在总任务收口时更新仓库权威文档。
