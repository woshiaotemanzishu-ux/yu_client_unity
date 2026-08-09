# VIP 主页面静态增量实现

## 已实现（仍需运行态验证）

- `VipModel` 在只读快照整体替换后发出 `Changed`，VIP 主页面与充值页在 `OnShow/OnHide/OnDestroy` 成对订阅、退订。
- VIP 主页面复用 `VipModule.prefab` 内现有 `VipTabButton`、`VipPrivilegeCardView`、`VipPrivilegeShowView`、`VipTopCardItem`、`VipCardItem` 模板；克隆的 `BaseView` 均经 `Show/Hide` 生命周期切换。
- 两个根页签可本地切换；1/2/4 三种卡选择器只改变本地展示选择，不发送协议。
- VIP 等级、经验、隐藏态和卡类型每日经验提示读取现有只读快照刷新。每日经验文案严格沿用老端已证实分支：type=4 激活显示“每日登录+5点经验”，无激活卡显示老端购买提示，其他已激活卡为空。
- VIP 主页面充值入口打开现有 `RechargeView`，充值页返回入口打开现有 `VipBaseView`；关闭与下滚只执行本地 UI 行为。
- `VipModule.prefab` 的根页签 Content 增加页面专属 `HorizontalLayoutGroup`，保持现有模板作为视觉事实源，不在运行时代码硬编码页签坐标。

## 显式阻塞

- `45001/45002/45003/45007/45008`、`15902`、商品支付、平台支付、领取、购买、免费卡激活均无点击回调或被关闭射线；本轮未发送任何写协议。
- 当前 Unity 内容闭包缺 `config_vip_card.json`、`config_vip_config.json`、`ClientVipPrivilege.json`，因此卡详情/价格/倒计时、特权说明、等级奖励、周礼包和充值商品列表不得猜造。对应玩家可见技术占位已全部移除。
- 顶部 1/2/4 卡固定时长严格沿用老端 `30天/90天/180天`，默认选中 type=4；折扣图和活动倒计时仍缺配置依据，保持运行态待验，不编造动态状态。
- 声音、弹窗身份、列表拖动/末项、cold/warm、两档 viewport、2D diff、状态即时刷新及关闭重开均需要真实 Unity Web 与老 H5 同账号验证；本轮禁止启动运行环境，因此保持 `needs-runtime-verify`。

## 静态验证

- `dotnet restore output/ui_route_audit/2026-08-09_vip_main/VipRoute.Isolated.csproj --nologo`
- `dotnet build output/ui_route_audit/2026-08-09_vip_main/VipRoute.Isolated.csproj --nologo --no-restore`
- Prefab 文本检查：YAML header 有效、1002 个对象定义无重复、无未解析本地 `fileID`、新增 LayoutGroup 定义/组件引用均恰好 1 个。
- schema 6 台账：45 节点，`blocked=16`、`needs-runtime-verify=29`、`not-run=0`。
