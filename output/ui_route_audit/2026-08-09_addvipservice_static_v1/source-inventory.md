# AddVipService 静态源清单（2026-08-09）

## 审计边界

- 本轮只读核对老 H5、当前 Unity C#、Generated Bind 与现有 Prefab；未启动 Unity、浏览器或前台程序。
- 未执行账号写事务；该老端页面本身没有充值、领取、购买或领奖按钮。
- 老端最终表现仍需同账号真实 H5/Unity Web 顺序复走；本文件不是运行态证据。

## 老 H5 路由与条件

- 主界面图标 114。
- 图标显示条件：非审核态、首充 15908 的 `is_buy == 1` 已置显示标记、当前 `plat_name` 命中 `ClientAddVipService.tem` 渠道白名单。
- 页面为 Activity 层、居中、带背景、关闭即销毁。
- 页面加载 `ClientAddVipService`，选择 `tem` 包含当前渠道的配置行；没有匹配行时不填充内容。

## 页面控件/状态/列表叶

- `_img_bg`：固定 `addVipService/ui_gz_01`。
- `_img_title`：配置 `title` 指向的渠道标题图片。
- `_img_link`：配置 `code` 指向的二维码/联系图片。
- `_img_bg1`、`bg3`、`_img_bg2`：标题/正文/分隔装饰；`_img_bg2` 使用 `common2/com_line_5`。
- `_lab_title`：配置 `des1` HTML 文案，老端按指定区域居中。
- `des`：配置 `des` HTML 文案，宽 555、字号 26、自动换行。
- `Content`：配置 `reward` 动态 HBox；逐项经 `GoodsModel.GetMappingTypeId` 映射后创建 85×85 `EquipmentItem`。
- 每个 `EquipmentItem` 的详情身份属于共享组件隐式交互，需要逐格运行核对。
- `close`：唯一页面专属显式点击，关闭页面；`Util.AddClickEvent(..., true)` 的通用点击声音仍需运行态核对。
- 无输入、开关、滚动区、弹窗、充值、领取、购买或领奖叶。

## Unity 现状与 Prefab 身份

- `Assets/Prefabs/UI/AddVipService/AddVipServiceModule.prefab` 存在，包含 `AddVipServiceView`、`_img_*`、`_lab_title`、`des`、`Content`、`close` 和隐藏模板区。
- 根绑定 GUID `038c3dec32bd24e44942327325e198d0`，对应只读 `AddVipServiceViewBind.cs`；序列化字段与老端页面节点匹配，并含 `_tpl_EquipmentItem`。
- `Content` 上存在布局组件，Prefab 保存了共享 `EquipmentItem` 模板实例。
- 当前业务目录只有 `AddVipServiceController.cs` 与 `AddVipServiceModel.cs`，没有消费 Bind 的 `AddVipServiceView` 业务脚本。
- `ClientAddVipService` 未迁移；当前 `ChannelWhitelist` 为空，入口按保守策略恒隐藏，页面配置、渠道图片、HTML 文案和奖励数据均无 Unity 消费链。

## 共享依赖与声音

- `EquipmentItem`：奖励格共享 Prefab/View；需要目标页验证空/有数据、品质/特效、数量、点击详情和关闭返回。
- `GoodsModel.GetMappingTypeId`：奖励类型映射，不得按 ID 猜测。
- ActivityIconManager / FirstRecharge / Login / Platform：只读依赖，不在本文件岛修改。
- 页面无专属主动声音调用；关闭按钮仅可能消费通用点击声，需真实运行态证据。

## 静态结论

- 这是“已有 Prefab、但缺业务 View 与必需客户端配置”的增量修复对象，不得重转。
- 由于配置与入口消费链涉及本轮禁止修改的 ClientConfigSync/主界面等文件，不能在文件岛内安全补齐；本轮未改 C# 或 Prefab。
- 全部叶显式 `blocked`；静态节点存在不冒充页面可打开、内容正确或真实 Web/Unity 通过。
