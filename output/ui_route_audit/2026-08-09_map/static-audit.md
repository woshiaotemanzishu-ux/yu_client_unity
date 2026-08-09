# MainUI → Map 静态审计（2026-08-09）

## 本轮边界

- 路线：`mainui.map`；schema 6 拓扑共 49 节点、34 叶子。
- 只修改 Map 专属代码、`MapModule.prefab` 与本审计目录。
- 未启动 Unity，未操作浏览器，未登录账号，未发送 `12001` / `12005`，未做任何写事务。
- 老客户端同账号、同状态、同 viewport 的真实运行表现仍是最终权威；本文只记录静态实现与阻塞，不把编译或 Prefab 检查冒充真实 Web 通过。

## 老端事实基线

| 分支 | 老端事实 | 当前 Unity 静态结论 |
| --- | --- | --- |
| `MapEnterView` | BaseWindow 外壳；区域地图 index 0 默认；世界地图 index 1；可见标题、两页签、关闭 | `MapFlow` 已改为默认区域图、互斥切页、关闭/热重开；`MapModule.prefab` 仍只有 Area/World 两个直接子页，缺老端可见外壳、页签和关闭控件 |
| 区域地图 | `ClientMapConfig` 当前场景 `point_list`；1292×1138 动态图；双轴滚动；角色点/名字；怪物/NPC 点；横向目的地列表；路径/终点 | 配置解析、地图加载、坐标缩放、角色点、点/列表克隆和生命周期已实现；滚动轴已修正。资源未进入 Unity 闭包；怪物图标、选中态、路径结果和写事务仍未闭合 |
| 世界地图 | `ClientWorldMapConfig.world_map`；1870×1005 `big_bg.jpg`；9 个城市；横向滚动；开放态、等级范围、当前地点/头像/上下运动 | 配置解析、共享条目克隆、开放态/等级、当前位置、横向初始位置、标记动画和生命周期已实现；配置、地图图像、城市图像及角色头像资源未进入 Unity 闭包 |
| 协议 | 地图数据本身为客户端配置 + 当前场景状态；地图/怪物点击发 `12001`；城市切换发 `12005` | 本轮只读。未注册、未调用、未伪造成功。三个写叶保持 blocked |

老端源码：

- `E:/GitProject/yu_client/h5/src/map/MapEnterView.ts`
- `E:/GitProject/yu_client/h5/src/map/AreaMapView.ts`
- `E:/GitProject/yu_client/h5/src/map/AreaMapMonItem.ts`
- `E:/GitProject/yu_client/h5/src/map/AreaMapPonitItem.ts`
- `E:/GitProject/yu_client/h5/src/map/AreaMapWayPonitItem.ts`
- `E:/GitProject/yu_client/h5/src/map/WorldMapView.ts`
- `E:/GitProject/yu_client/h5/src/map/WorldMapItem.ts`
- `E:/GitProject/yu_client/h5/src/commonModel/MapModel.ts`

## 静态落地

1. `MapFlow`
   - 冷开默认 `AreaMapView`，与老端 index 0 一致。
   - `OpenArea` / `OpenWorld` / `OpenSub` 复用同一模块根，切页先隐藏兄弟页。
   - 关闭、异步加载期间的待开页、Reset 均清理当前引用。
   - 新增只读 `MapConfigs`，按老端字段解析 `ClientWorldMapConfig` 与 `ClientMapConfig`；资源缺失时保持真实空态，不造假数据。
2. 区域地图
   - 使用当前 `SceneMapLoader.Current` 尺寸与 `RoleModel` 坐标换算角色点。
   - 按配置克隆已烘焙 `AreaMapPonitItem` / `AreaMapMonItem`，通过 `Show/Hide` 触发 BaseView 生命周期。
   - 点位保持老端 `-42/-63` 偏移；列表步长 142；关闭时停止惯性并隐藏克隆项。
   - `AreaMapWayPonitItem.SetData` 可静态切换普通路径点与终点，但当前尚无寻路结果消费者。
3. 世界地图
   - 按配置克隆同一个 `WorldMapItem` 模板；城市坐标、开放态、名称、等级范围与当前地点由配置驱动。
   - 当前地点按 `locate_pos` 放置并垂直往返；关闭后停止状态并作废异步图像回填。
   - 世界背景使用 `resource/game/map/texture/big_bg.jpg`；城市图使用 `resource/game/map/world_map_img/{img_source}.png`。
4. Prefab 滚动结构
   - 区域地图 `map_scroll`：horizontal=1、vertical=1。
   - 区域目的地 `mon_scroll_group`：horizontal=1、vertical=0。
   - 世界地图 `scroll`：horizontal=1、vertical=0。
   - 外层 `mon_scroll` 保持不滚动，由内层列表承担横向滚动。

## 真正阻塞

### 资源/跨岛依赖

当前工作树未发现以下 Unity 资源，且本代理被禁止修改 Addressables、公共资源管线或 Map 范围外目录：

- `resource/config/client/ClientMapConfig.json`
- `resource/config/client/ClientWorldMapConfig.json`
- `resource/game/map/area_map_img/{sceneId}.jpg`
- `resource/game/map/world_map_img/{img_source}.png`
- `resource/game/map/texture/big_bg.jpg`
- 世界地图当前角色头像、区域怪物/NPC 图标的可达资源/配置映射

资源进入实际加载闭包、保留 `.meta` / GUID、Addressables 地址和当前 catalog 后，配置驱动代码才可真实出图。

### 可见壳与交互

- `MapModule.prefab` 不含老端 `MapEnterView` 的可见外壳、区域/世界页签、关闭按钮。本轮不能修改 Common/BaseWindow，也不能凭空猜图与坐标。
- 区域怪物列表的选中/未选中图状态尚未由业务状态驱动。
- 区域路径点虽然有显示 API，但没有可用的只读寻路结果链；地图点击/怪物点击必须在写授权后走正式 `12001`。
- 世界城市点击必须在写授权后验证等级/开放态并走正式 `12005`；当前回调有意为 null。

### 真实运行闸

仍需在源码、Player、catalog 指纹完全匹配的包上完成：

- 老 H5 与 Unity 同账号顺序复走；720×1280 与 1920×1080 两档 viewport。
- 350ms、1000ms、ready 三时点资源状态，禁止空白/白块冒充 ready。
- 区域双轴拖动、横向怪物列表末项可达与裁剪；世界横向两端可达。
- 当前角色坐标、点位坐标、城市 `root_pos/locate_pos` 的页面根坐标对比。
- 当前地点动画的两个不同时间点像素；关页、切页、热重开无残留、无重复克隆/回调。
- 冷/热耗时、滚动分配、克隆数量和清理。
- 经授权的 `12001` / `12005` 成功回包、即时状态与关闭重开；不得用 GM 直接写最终结果代替 UI 事务。

## 验证

- 主控交叉审查按当前老端 `WorldMapItem.ts` 第 134/136 行纠正高阶等级文案为“神创”；原 manifest 注释中的“神劫”只是描述性笔误，未改变节点集合、父子关系、类型、风险或控件清单，正式 schema 6 拓扑保持不变。
- `dotnet build Shenxiao.Module.Core.csproj --no-restore -m:1`：通过，0 error；84 个仓库既有 warning（2026-08-09 本轮最后一次完整运行）。
- `verify-static.ps1`：检查 MapFlow/配置字段/克隆生命周期、禁止写协议、Prefab 四个 ScrollRect 轴、schema 6 manifest/ledger。
- 正式状态以 `route-ledger.json` 的 validator 输出为准；当前 49 节点为 `blocked=35`、`needs-runtime-verify=14`、`not-run=0`。

## 文档边界

本轮未更新 `Docs/README.md` / `Docs/Shenxiao实施进度.md`，原因不是“不触发文档更新”，而是父任务明确禁止本 Map 代理修改 Docs；总控收口时应把本账结论合入权威进度文档。
