# Reincarnation / 转生路线静态三方调和

## 本批边界

- 目标路线：底部“转生”入口的 `ReincarnationEnterView -> ReincarnationBaseView`。
- 本批禁止启动 Unity、浏览器和真实账号，因此“老端运行”侧没有新采证；它是明确缺口，不能由源码推断替代。
- Unity 目标文件岛在开始时无 dirty：`Assets/Scripts/Module/Core/Reincarnation/`；没有找到归属该模块的 Prefab 或 `resource/game/reincarnation` 资源目录。
- 因无可编辑 Prefab，且首次转换闭包会跨入本批禁止的 Common、Task、Guard、Goods、Shop、Router、Configs、Addressables 等共享域，本批不执行大转换、不接写协议、不制造占位页面。

## 三方事实

| 维度 | 老端真实运行 | 老端源码 / 配置 | 当前 Unity |
|---|---|---|---|
| 主入口 | 本批未采，NVR | `ReincarnationEnterView` 只有“转职”页签，内容类为 `ReincarnationBaseView` | 无 View、无 Prefab、无入口绑定 |
| 主状态 | 本批未采，NVR | 转数、阶段、任务、属性、技能、装备/时装、锁定/完成、礼包、丹药、觉醒格均有条件显隐 | `ReincarnationModel` 只保存 16400 激活 ID 列表和 `HasData` |
| 协议 | 本批未采，NVR | 接收 13040、13041、16400、16401；16401 成功后重拉 16400；任务链使用 30004/30006；丹药使用 15050 | 只注册并请求 16400；定向回归明确要求 16401 未注册 |
| 子页 / 弹窗 | 本批未采，NVR | DanTips、LightUp、Ligthed、SkillTips、Effect、Success；四至七转各自 Item/View | 全部缺失 |
| 资源 | 本批未采，NVR | 23 个模块 TS、20 个 Laya scene、144 个旧资源文件；四至七转格子使用 UI_1127_01/02/03/04，成功页另有动态效果 | 没有转生 Prefab/图集/特效消费链 |

## 控件与状态清单

### 主页

- 页签与返回：转职页签、窗口关闭。
- 概览条件块：转数/阶段、属性、任务进度、技能、装备/时装、锁定、阶段完成、红点、礼包、丹药。
- 主操作：`rebirth_btn`、`stageBtn`、`lock_img`、`_box_guard`、`keyAllBtn`、`goBtn`、`danBox`、`giftIcon`。
- 列表：任务列表、技能列表、属性滚动内容；任务行有 `finishBtn`、`wayBtn`、`submitBtn`、`click_group`。
- 返回链：部分转生副本退出后自动重开转生页；本批不能验证 cold/warm、即时刷新或重开一致性。

### 四至七转觉醒

- 四、五、六、七转是四种宿主布局，包含左右翻页、转数点选、动态格子列表和转数摘要。
- 格子至少有未解锁、可点亮、已点亮三态；点击后分别进入 `ReincarnationLightUpView` 或 `ReincarnationLigthedView`。
- 可点亮弹窗包含属性/材料/经验状态、`lightup_btn`、`shopBtn`、关闭；未解锁态隐藏点亮和商城按钮。
- 已点亮弹窗包含当前属性、累计点亮属性、滚动内容和点击背景关闭。
- 动态效果按宿主出现 `UI_1127_04`、`UI_1127_02`、`UI_1127_03`、`UI_1127_01` 等差异；没有 Unity 资源和双帧证据。

### 丹药、技能与成功页

- `ReincarnationDanTips`：材料/数量/红色不足态、横向条件列表、`upBtn`、背景关闭；`upBtn` 通过 15050 消耗真实背包物品。
- 技能项：升级前/后图标均可打开 `ReincarnationSkillTipsView`，需要核对目标身份和关闭链。
- 成功链：13040 后先进入 `ReincarnationEffectView`，旧代码目前因效果缺失直接回调打开 `ReincarnationSuccessView`；成功页包含属性、技能详情、头模/时装表现、转数图和动态效果，点击背景关闭。

## 组件依赖清单

| 依赖 | 老端用途 | 本批处理 |
|---|---|---|
| BaseWindow / Router | 入口、标题、返回链 | 共享文件岛禁止，blocked |
| TaskModel / 30004 / 30006 | 转生任务提交、补材料、跳转 | 写事务且共享文件岛禁止，blocked |
| GoodsModel / 15050 | 丹药升级 | 写事务且共享文件岛禁止，blocked |
| GuardModel | 守护入口 | Guard 文件岛禁止，blocked |
| OpenFun / Shop | 功能 7/26/60/77 跳转 | Router、Shop 文件岛禁止，blocked |
| SkillTips / item / effect infrastructure | 技能详情、格子、动态效果 | Common/共享组件和资源闭包不清，blocked |
| Dress / role presentation | 成功页头模或时装 | Role/共享展示域禁止，blocked |

## 旧资源闭包清单

- 23 个业务脚本：`ReincarnationBaseView`、Enter/Task/Dan/Skill/Effect/Success，四至七转 View/Item，LightUp/Ligthed，以及三类 Success item。
- 20 个 Laya scene：上述业务页和 item 外，还包含四至七转复用的 `RebirthBottomView`、成功属性/装备/技能 item，以及源码中未发现当前消费者的 `RebirthInstructionItem`。后者仅登记为待老端运行核实，未猜测为可达页面。
- 144 个旧资源文件只证明旧资源目录存在；未核定到 Unity GUID、Addressables 地址或实际加载版本，所以不能形成可转换资源闭包。
- Unity 另有 `Assets/Editor/CliVerify/Cases/ReincarnationCase.cs` 静态用例，它只覆盖 16400 快照、顺序/重复 ID、清理和“16401 未注册”门禁；它不是 UI、视觉或交易闭环证据。

## 协议和授权边界

- 16400 是当前 Unity 唯一已有的只读快照链，可以静态确认，但它不能证明 UI 已存在或能显示。
- 16401 是真实资产/进度写事务；当前定向用例明确保持未注册。缺少可编辑页、成本展示、二次确认、single-flight、错误态、即时刷新和重开链时不得接入。
- 13040/13041、30004/30006、15050 均涉及当前文件岛外的权威状态和交易链。本批没有真实账号写授权，也没有目标 Prefab，所以不做最小协议“补注册”。

## 结论

- 已把源码可见的页面、页签、按钮、列表、弹窗、条件块、协议、返回链和组件依赖冻结为独立 schema 6 manifest。
- 这不是完成态：老端真实运行、Unity Prefab、Unity/真实 Web、两 viewport、像素、滚动、模型/特效双帧、cold/warm、即时刷新、关闭重开均没有证据。
- 全部叶节点登记 `blocked`；父节点由台账工具自动回卷为 `blocked`。没有把静态枚举、16400 回归或文件存在性写成 `done`。
