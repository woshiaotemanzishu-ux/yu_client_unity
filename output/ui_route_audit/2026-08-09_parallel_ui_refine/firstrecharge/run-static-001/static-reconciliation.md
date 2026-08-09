# FirstRecharge 静态三方调和

## 结论

本轮只能形成“完整静态盘点 + 明确缺口”，不能做 `fix-view`：Unity 当前没有 FirstRecharge 可编辑 Prefab、Bind 或业务 View。`FirstRechargeModel.cs` / `FirstRechargeController.cs` 已接入 15905、15906、15907、15908 的基础模型与协议，但主面板、提示弹窗、新提示、气泡、气泡文案、配置消费、奖励展示和运行时绑定均未落地。

首次转换也不能在本轮执行。它需要新增生成产物、完整首充资源/配置，并接入 MainUI、Activity、Common 详情/奖励弹窗以及充值页；这些均超出唯一文件岛或位于明确禁写目录。未修改业务代码，避免在没有可见页面和完整资源闭包时制造孤立协议入口或半成品 UI。

## 指纹与起始状态

- Git HEAD：`92b3f5578a90befb85a6255157f08e482214aa1a`
- 起始目标岛：`Assets/Scripts/Module/Core/FirstRecharge` 与本路线 output 均 clean。
- Unity Model SHA-256：`a9d52a1c1b3d7726d112874dca364939d352c672ac8c4315f752bce36617a236`
- Unity Controller SHA-256：`7315d7cb7a5439feb763a78133e95b765b7063b0a67b1b45b81fb7f58ce33cc1`
- 老端 Model SHA-256：`06a402481f46d4b151b0caad91e233e373d36a989d29b9ff4dd503d44040d2b8`
- 老端 Controller SHA-256：`ab2d360309d13a9701dd639e301039705059032ac690bd322facbc6b93d9c9ee`
- 老端主 View SHA-256：`3dafe1bad599c32c25a708a32cd1a20c74ab0ad334c78d7cebe4f97c73e0ef55`
- 老端客户端配置 SHA-256：`6064e85884f312d076fcbb3d2db3511134bb60cbe4046f309ab4befb05d20b70`
- 老端奖励配置 SHA-256：`53d066a42d23ae48c0d57b9eb359f58db0708ff25284f94f4a5c049b1813b37d`

## 三方对照

| 面向 | 老端运行/源码/配置 | Unity Prefab/Bind/代码 | 判定 |
|---|---|---|---|
| 主入口 | MainUI 图标、30 分钟新号气泡、条件提示均可打开主面板 | `ActivityIconManager` 中有展示调度；无 FirstRecharge Prefab/View/Bind | 跨 MainUI 且缺业务页，blocked/defect |
| 主面板 | Activity 层，背景点击关闭，3 个日签，奖励列表，状态按钮，模型/图片/特效 | 无 Prefab/View/Bind；只有 Model/Controller | defect |
| 配置 | `ConfigFirstRecharge.json` 定义提示、气泡和每日职业展示；`config_recharge_first.json` 定义 product=40、sex/day/career 奖励 | Assets 内未找到两份配置或等价强类型配置 | defect |
| 状态 | 0 未购买、1 可领取、2 时间未到、4 已领、5 未达开服日 | `FirstRechargeModel.Slot.Open` 保留并提供聚合判断 | 静态语义基本对齐，runtime NVR |
| 领取 | 按当前日签发 15906；回包显示错误或奖励弹窗，随后 15905 即时刷新 | `Claim(index)` 与 15906 解析存在；成功仅 `RequestInfo()`，无错误展示、奖励解析/弹窗、成功事件或 UI 单飞 | defect + 真实领取 blocked |
| 充值跳转 | 未购买按钮打开充值页 | 无主面板按钮；目标属于充值/Vip 共享路线 | blocked |
| 气泡 | 倒计时、上下浮动、活动图标联动定位，超时发 15907+15905 | Controller 有 30 分钟调度与 15907；无气泡 Prefab/View/Bind | blocked/defect，只有单帧老端证据 |
| 红点 | 三日签和主按钮按 `Open==1` 更新；全部领取后关闭页面并移除入口 | Model 有 `HasClaimableReward`；页面红点未实现；入口清理由 MainUI 负责 | defect/blocked |
| 生命周期 | 打开面板再查 15905；跨天条件复查；场景切换关闭；重开反映权威态 | GAME_START、dayChange 请求和模型事件存在；无页面级 show/hide/reopen 绑定 | runtime NVR / defect |

## 控件、状态与返回链清单

### FirstRechargeView

- `close`：关闭当前面板。
- `_img_bg`：背景点击关闭，且不应穿透。
- `Btn1/Btn2/Btn3`：三天页签；选中态切换当天模型/图片/标题/奖励/按钮/红点。
- `_gp_reward`：职业、性别、product、day 共同决定的动态奖励列表；列表项打开具体物品详情。
- `_gp_recharge`：状态 0 跳充值页，状态 1 发 15906，状态 2/4/5 不得误发领取。
- `_red_1/_red_2/_red_3/_red4`：日签及主按钮红点。
- `_gp_show`、左右模型/图片、标题与描述：按 `view_show[day@career]` 和 sex 分支显示。
- `_bg_effect`：老端调用 `ui_shouchong_01`，需真实动态双帧与宿主归属证据。
- `_img_1/_img_2/_img_3`：未首充且对应开服日时的条件覆盖图。
- 场景切换关闭、关闭后返回 MainUI；全部领完时页面与主入口都应清理。

### FirstRechargeTipsView

- `_gp_get`：打开主面板并关闭提示。
- 模型/图片/文案：按职业、性别和配置显示。
- 显隐：受等级、任务、场景/副本、首次充值状态等条件约束。

### FirstRechargeNewTipsView

- `box_click`：整块打开主面板。
- `close`：主动关闭。
- 20 秒自动关闭。
- 模型/活动态位置与缩放、任务/等级条件分支。

### FirstRechargeBubble / FirstRechargeQiPaoItem

- `_img_bg`：打开主面板。
- `_lb_time`：`mm:ss.ms` 倒计时。
- 气泡上下浮动、随 Activity 图标占位联动。
- 超时写入 15907，再查 15905；这会改变账号通知计数，本轮未执行。
- `_img_tips/_lb_info`：配置驱动文案与持续时长，超时清理；旧运行快照中该分支隐藏。

## 协议与即时刷新

| 协议 | 服务端权威语义 | Unity 当前静态状态 | 仍需证据/修复 |
|---|---|---|---|
| 15905 | 返回 `{Open,Index}[] + ProductId + IsNotify` | 已注册、解析并发 `EVT_FIRST_RECHARGE_UPDATE` | 真实账号状态矩阵、页面即时刷新、关闭重开 |
| 15906 | 请求 `Index:u8`；回 `Errcode:u32 + Index:u8`；成功真实发奖 | 已注册；成功仅再查 15905 | 错误展示、奖励弹窗、按钮单飞、成功刷新、重开；真实领取无授权 |
| 15907 | 无回包；永久增加首充通知计数 | 30 分钟调度可发送 | 属账号写入，本轮 blocked；还需倒计时/关闭/重开证据 |
| 15908 | 返回是否已购“添加有礼” | 已注册并更新 `IsBuy` | 真实状态与 MainUI 入口联动 |

服务端 `get_award_state` 当前优先判断：未达开服日为 5，未购买为 0，已领取为 4，其余为 1；服务端保留 2 的错误出口，但当前状态函数不再产生 2。老端 UI 仍保留 2 的文案分支，因此 Unity 不应删除该兼容状态，真实环境需确认当前配置/版本是否可达。

## 组件依赖与文件岛 blocker

| 依赖 | 用途 | 本轮处理 |
|---|---|---|
| MainUI / `ActivityIconManager` | 主入口、气泡位置、全部领取后移除入口 | 禁写；只登记 blocker |
| Activity 层 | 老端主面板与提示层级 | 禁写；只登记 blocker |
| Recharge/Vip 路线 | “前往充值”目标 | 禁真实充值且相关模块禁写；blocked |
| Common EquipmentItem / 物品详情 | 奖励格及详情弹窗 | Common 禁写；blocked |
| Common 奖励获得弹窗 | 15906 成功结果展示 | Common 禁写；blocked |
| ConfigFirstRecharge / config_recharge_first | 显隐、文案、模型、奖励数据 | Unity 缺失；资源/配置落地超出当前岛 |
| `ui_shouchong_01` / `ui_shouchong_02` | 主面板/提示视觉特效 | 资源存在，但无宿主 Prefab；不能以资源存在证明出帧 |

共享组件消费者矩阵不能在没有 FirstRecharge Prefab 与绑定关系时建立真实 identity。后续转换应复用现有 Common 物品格、物品详情和奖励弹窗，宿主只传数据与回调；不得在 FirstRecharge 私复制共享节点树。

## 本轮 NVR / blocked 边界

- 未启动 Unity、浏览器或 Computer Use；未 build。
- 未执行真实充值、领取、消费、GM 或任何账号写事务。
- 没有 Unity Web Player/catalog 同批指纹，没有双 viewport old/unity/overlay/diff。
- 没有 350ms/1000ms/ready 资源帧、模型 RT 非透明像素、特效双帧、滚动拖动、cold/warm、即时刷新或关闭重开证据。
- 旧运行证据只覆盖 720×1280 下一帧 FirstRechargeBubble；不能替代 Unity 或完整页面验收。
- 因为不存在可编辑 Prefab，`fix-view` 不适用；`convert-module` 被当前写入边界与共享依赖阻塞。

## 后续最小安全路径

1. 扩展一次性首次转换授权，允许 FirstRecharge 专属 Prefab/Bind/完整专属资源与配置落袋，并明确 MainUI、Activity、Common 的接入边界。
2. 转换后回到本 manifest 的原路线，逐叶完成三日签、奖励列表/详情、按钮四态、提示/气泡/红点、15906 成败及即时刷新。
3. 由主控串行 build 后，在匹配源码与 catalog 的真实 Web 包上完成双 viewport、cold/warm、特效双帧、滚动与重开证据；真实领取/15907 写入需另行授权和可恢复测试账号。
