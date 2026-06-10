# Shenxiao 地图加载重构方案

> 本方案用于把 `yu_client` 的地图加载机制重构到 Shenxiao Unity 客户端。
> 目标是先对齐资源语义、加载链路和工程边界，再开始写运行时代码。

**更新时间**：2026-06-09

**参考项目**：
- `D:/git_res/yu_client`：本机实际存在的 LayaAir 客户端，仅作只读参考。
- 文档历史路径 `D:/GitProject/yu_client` 当前本机不存在，后续排查以实际路径为准。

---

## 一、目标与边界

### 1.1 本阶段目标

第一阶段只做地图加载最小闭环：

1. 根据场景 `sceneId` 异步加载地图 `.bytes` 数据。
2. 解析地图宽高、瓦片尺寸、瓦片列表、寻路格、动态区域和地图资源 `resId`。
3. 区分 `sceneId` 与 `mapResId`，支持多个场景复用同一份地图视觉资源。
4. 加载小图 / 占位底图，地图数据完成后允许场景进入。
5. 按相机视野异步加载可见瓦片，并通过固定池复用瓦片对象。
6. 给后续 NPC、门、特效、掉落等场景对象提供地图坐标、寻路格和视野生命周期基础。

### 1.2 协议边界

地图加载相关协议不重新设计，按 `yu_client` 现有场景协议照抄接入：

- 协议号、格式串、字段顺序、字段含义、请求时机和回包处理顺序以 `yu_client` 为准。
- Shenxiao 侧只做 Unity 客户端适配，把旧协议结果转换成地图加载、坐标、场景对象生命周期需要的数据。
- 如果后续确认必须调整 `yu_server` 或协议定义，先按项目决策点报告，不在地图加载实现里顺手改。

### 1.3 暂不纳入

- 不实现完整寻路算法，只先解析和提供格子查询接口。
- 不做世界地图 / 小地图 UI。
- 不重写地图编辑器。
- 不在 Runtime 做 `.jxr/.ktx` 格式转换。
- 不新增 UPM / NuGet 包、asmdef、Addressable Group 或 Profile；如果实现时确实需要，先按规范报告。

---

## 二、yu_client 地图加载事实

### 2.1 加载入口

`yu_client` 的低层地图加载不在 `commonController/MapController.ts`，而在：

| 文件 | 职责 |
|------|------|
| `h5/src/scene/SceneController.ts` | 处理切场景协议，收到 `12005` 后调用 `scene.ChangeScene(instance_id)` |
| `h5/src/scene/Scene.ts` | 场景切换编排，调用 `mapView.Load(sceneId, ...)` |
| `h5/src/scene/MapManager.ts` | 地图数据、底图、瓦片池、相机视野刷新 |
| `h5/src/scene/MapTile.ts` | 单个瓦片资源加载和释放 |
| `h5/src/scene/AstarManager.ts` | 寻路格数据解析和查询 |
| `h5/src/scene/SapManager.ts` | 视野内对象创建 / 视野外对象销毁 |

核心链路：

```text
SceneController.On12005
  -> SceneManager.SetCurrentSceneId(instance_id)
  -> Scene.ChangeScene(instance_id)
  -> MapManager.Load(sceneId)
  -> 加载 sceneId.bytes
  -> 解析 mapResId / 宽高 / 瓦片 / 寻路 / 动态区域
  -> 加载 mapResId 小图
  -> SceneDataLoadCompleted
  -> REQUEST_SCENE_INFO
  -> 12100 / 12002 拉取 NPC、门、怪物、掉落等场景对象
```

注意：`yu_client` 中地图数据完成后场景即可进入，高精瓦片是随后按相机位置继续补齐的。

### 2.2 资源路径

旧客户端资源路径如下：

```text
地图数据：
resource/game/scene/map/{sceneId}/{sceneId}.bytes

小图 / 占位底图：
resource/game/scene/map/{mapResId}/tile/{mapResId}.jpg
resource/game/scene/map/{mapResId}/tile/{mapResId}.ktx

瓦片：
resource/game/scene/map/{mapResId}/tile/{row}{col}.jxr
resource/game/scene/map/{mapResId}/tile/{row}{col}.ktx
```

瓦片名是两位行号 + 两位列号，且从 1 开始，例如 `0101`、`0102`。

Shenxiao 运行时 Addressable key 仍按项目规范走无扩展名路径，业务代码只能通过 `GameResPath` 获取路径，再由 `ResourcePath.Normalize()` 统一去扩展名、转小写和规范斜杠。

### 2.3 sceneId 与 mapResId 必须分离

`.bytes` 中的 `resId` 是地图视觉资源 ID，不一定等于场景 ID。

本机扫描 `D:/git_res/yu_client/cdn/resource/game/scene/map` 结果：

| 项 | 数量 |
|----|----:|
| 地图 bytes 总数 | 175 |
| `sceneId != resId` 的复用地图 | 128 |

示例：

| sceneId | mapResId |
|--------:|---------:|
| 901 | 2003 |
| 2035 | 2034 |
| 7002 | 7001 |
| 8002 | 8001 |
| 10008 | 10000 |
| 10101 | 10001 |

因此 Unity 侧的数据结构必须同时保存 `SceneId` 和 `MapResId`，不能把资源目录、瓦片目录、寻路数据和业务场景 ID 混成一个字段。

### 2.4 bytes 格式

`MapManager.ts` 中的 `MapElement.LoadData` 解析顺序如下：

| 顺序 | 类型 | 含义 |
|------|------|------|
| 1 | int32 | tileSize |
| 2 | int32 | mapHeight |
| 3 | int32 | mapWidth |
| 4 | int32 | tileCount |
| 5 | uint32 | tileDataSize，旧字段 |
| 6 | uint32 | maskDataSize，旧字段 |
| 7 | tileCount * uint32, uint32 | tile 的 x / y 坐标 |
| 8 | byte * gridCol * gridRow | 寻路格，按列优先存储 |
| 9 | uint32 | resId，即 mapResId |
| 10 | dynamicAreaCount + area records | 动态区域数据 |

格子尺寸来自旧客户端全局常量：

```text
LogicRealRatio_X = 60
LogicRealRatio_Y = 30
LogicRealRatio_Z = 76
```

格子数量：

```text
gridCol = floor((mapWidth  + 60 - 1) / 60)
gridRow = floor((mapHeight + 30 - 1) / 30)
```

寻路格标记：

| 标记 | 值 |
|------|---:|
| Block | 1 |
| Way | 2 |
| Safe | 4 |
| Shield | 8 |
| Jump | 16 |
| Water | 32 |
| Swing | 64 |

---

## 三、Shenxiao 侧架构

### 3.1 文件落点

遵守现有分层，不新增 asmdef。

| 层级 | 建议落点 | 内容 |
|------|----------|------|
| Framework | `Assets/Scripts/Framework/Scene3D/Map/` | 地图数据结构、bytes 解析、坐标换算、瓦片层、寻路格查询 |
| Module.Combat | `Assets/Scripts/Module/Combat/Scene/` | 切场景协议编排、地图加载时机、场景对象拉取 |
| Editor | `Assets/Editor/MapResourceTools/` | 地图资源导入、格式转换、校验、缺图报告 |
| Remote 资源 | `Assets/GameRes/resource/game/scene/map/` | 地图 bytes、底图、瓦片、转换后的 Unity 可加载纹理 |

### 3.2 Framework 职责拆分

建议先建立这些类型：

| 类型 | 职责 |
|------|------|
| `SceneMapData` | 一张地图的解析结果：`SceneId`、`MapResId`、宽高、tileSize、tile 列表、寻路格、动态区域 |
| `SceneMapTile` | 单个瓦片运行时对象，负责加载、显示、释放 |
| `SceneMapTileLayer` | 固定瓦片池、根据相机视野计算可见行列、复用瓦片 |
| `SceneMapLoader` | 地图加载编排：加载 bytes、解析、加载小图、初始化 tile layer |
| `MapDataParser` | 只负责 `.bytes` 二进制解析，不依赖 Unity 场景对象 |
| `MapCoordinate` | 像素坐标、逻辑格、Unity 世界坐标之间的唯一换算入口 |
| `MapWalkGrid` | 寻路格查询、动态区域覆盖查询 |
| `SceneVisibilityManager` | 复刻 `SapManager` 思路，后续统一管理视野内对象生命周期 |

### 3.3 Module.Combat 职责

Combat 模块只做业务编排：

```text
收到 SC_CHANGE_SCENE
  -> 保存当前 sceneId / dungeonId / 主角出生点
  -> await SceneMapLoader.LoadAsync(sceneId)
  -> 地图数据 ready
  -> 请求场景对象协议
  -> 创建主角 / NPC / 怪物 / 门 / 掉落
```

Framework 不主动发协议，不直接依赖 `Proto`、`BaseController` 或业务 Model。

---

## 四、运行时加载流程

### 4.1 正常加载

```text
SceneMapLoader.LoadAsync(sceneId)
  1. 清理旧地图，取消旧瓦片加载任务
  2. ResManager.LoadAsync<TextAsset>(GameResPath.GetSceneMapData(sceneId))
  3. MapDataParser.Parse(bytes)
  4. 保存 SceneMapData(sceneId, mapResId, width, height, ...)
  5. 初始化 MapWalkGrid 和 MapCoordinate
  6. 加载小图 / 占位底图
  7. 创建 SceneMapTileLayer 固定瓦片池
  8. 抛出地图数据完成事件 / 回调
  9. 相机移动时刷新可见瓦片
```

第 8 步是场景可以继续拉取 NPC / 门 / 怪物的节点，不等待全部瓦片完成。

### 4.2 卸载

切地图时必须处理：

1. 增加加载版本号或取消令牌，旧异步回调不能污染新地图。
2. 释放小图纹理。
3. 释放全部 tile 已加载纹理。
4. 清空瓦片池对象或回收到池。
5. 清空 `SceneVisibilityManager` 注册对象。
6. 清空动态区域覆盖数据。

### 4.3 资源路径接口

在 `GameResPath` 增加地图路径工厂，业务代码不得手拼：

```csharp
public static string GetSceneMapData(int sceneId)
    => "resource/game/scene/map/" + sceneId + "/" + sceneId + ".bytes";

public static string GetSceneMapPreview(int mapResId)
    => "resource/game/scene/map/" + mapResId + "/tile/" + mapResId + ".jpg";

public static string GetSceneMapTile(int mapResId, int row, int col, string ext)
    => "resource/game/scene/map/" + mapResId + "/tile/" + row.ToString("00") + col.ToString("00") + ext;
```

运行时调用 `ResManager.LoadAsync<T>()` 时会统一走 `ResourcePath.Normalize()`，最终 Addressable key 不带扩展名。

---

## 五、坐标与相机

### 5.1 坐标原则

旧客户端大量协议、配表和地图数据都使用像素坐标，所以 Unity 侧不要把协议坐标直接改成 Unity 单位。

建议保留三层坐标：

| 坐标 | 说明 |
|------|------|
| MapPixel | yu_client 原始地图像素坐标，协议和配置主坐标 |
| MapGrid | 寻路格坐标，`x / 60`、`y / 30` |
| UnityWorld | Unity 场景坐标，只在渲染和物体 Transform 使用 |

所有换算都放进 `MapCoordinate`，业务模块不直接写比例常量。

### 5.2 Unity 世界比例

旧客户端 3D 表现使用 `pixel * 0.01` 的比例。Shenxiao 首版建议也以 `0.01f` 作为默认世界比例，先保证角色、特效、地图相对尺寸接近旧表现。

具体落地时需要统一确认地图平面方向：

| 方案 | 表达 |
|------|------|
| XZ 地面方案 | `Vector3(pixelX * 0.01f, height, pixelY * 0.01f)` |
| XY 贴图方案 | `Vector3(pixelX * 0.01f, pixelY * 0.01f, depth)` |

如果主场景采用 3D 角色和相机，优先使用 XZ 地面方案；但必须通过一张真实地图和一个真实角色 prefab 验证比例、遮挡和深度排序。

---

## 六、资源导入与工具链

### 6.1 地图资源进入 Unity 的规则

旧资源目录：

```text
yu_client/cdn/resource/game/scene/map/{id}/
```

Unity 远端资源目录：

```text
Assets/GameRes/resource/game/scene/map/{id}/
```

保留原始相对路径语义，Addressable key 由 `ResourcePath.Normalize()` 统一处理。

### 6.2 .jxr / .ktx 处理

`.jxr` 和 `.ktx` 是否能被 Unity 当前导入链直接稳定识别，需要先验证。方案原则：

1. Runtime 不做图片格式解码或转换。
2. 如果 Unity 不能直接导入，则由 Editor 工具在导入阶段转换成 Unity 可加载纹理。
3. 转换后仍保持原路径语义，运行时只关心 `GameResPath` 返回的 key。
4. 缺失小图、缺失瓦片、格式不支持都输出报告，不在运行时静默替换。

建议新增 Editor 校验工具：

| 工具 | 职责 |
|------|------|
| `MapResourceScanner` | 扫描 map 目录，统计 sceneId、mapResId、tile 数、缺失文件 |
| `MapBytesValidator` | 解析 `.bytes` 并校验格式、grid 长度、resId、动态区域 |
| `MapTextureImporter` | 验证 / 转换 `.jxr/.ktx/.jpg` 为 Unity 可加载纹理 |
| `MapAddressableReport` | 检查 Addressable key 是否符合无扩展名、小写、路径风格 |

---

## 七、验收标准

### 7.1 第一阶段验收

选择 `10001` 或 `10000` 作为首张验证地图：

1. 能通过 `ResManager.LoadAsync<TextAsset>()` 加载 `.bytes`。
2. 能解析出正确的宽、高、tileSize、tileCount、gridCol、gridRow、mapResId。
3. 能加载小图或可替代的 Unity 可加载预览图。
4. 相机移动时只加载可见瓦片，且瓦片池复用，不无限创建 GameObject。
5. 切换到另一张地图后，旧地图纹理和瓦片实例能释放。
6. `sceneId != mapResId` 的地图能正确加载复用资源，例如 `10008 -> 10000`。
7. 无 `Resources.Load`、`AssetBundle.LoadFromFile`、`File.ReadAllBytes`、同步等待。

### 7.2 第二阶段验收

1. 主角能按服务器坐标落到地图正确位置。
2. 寻路格查询结果与 yu_client 对同一坐标一致。
3. 动态区域变更后，阻挡 / 安全区查询能更新。
4. NPC、门、特效能接入 `SceneVisibilityManager`，只在视野附近创建。
5. 地图加载完成事件能驱动场景对象协议请求，不依赖瓦片全加载。

---

## 八、实施顺序

| 顺序 | 任务 | 产出 |
|------|------|------|
| 1 | 补 `GameResPath` 地图路径接口 | 路径统一，业务不手拼 |
| 2 | 实现 `MapDataParser` 和 `SceneMapData` | 能解析 bytes |
| 3 | 做 Editor `MapBytesValidator` | 能批量验证旧地图数据 |
| 4 | 实现 `SceneMapLoader` 最小加载链 | bytes + 小图加载 |
| 5 | 实现 `SceneMapTileLayer` | 可见瓦片加载和复用 |
| 6 | 实现 `MapCoordinate` / `MapWalkGrid` | 坐标和寻路查询 |
| 7 | 接入 Combat 场景切换骨架 | 协议与地图加载串起来 |
| 8 | 做 `SceneVisibilityManager` | 为 NPC / 门 / 特效接入做准备 |

---

## 九、风险与决策点

| 风险 | 处置 |
|------|------|
| `.jxr/.ktx` Unity 导入不稳定 | 先做 Editor 验证；必要时转成 Unity 可加载纹理，Runtime 不转换 |
| 地图资源量大，Addressable 分组策略影响下载 | 首版不新增 Group；若需要独立 `SceneMap` Group，先报告 |
| `sceneId` 与 `mapResId` 混淆 | 数据结构强制分字段；路径接口明确传参名 |
| 坐标系选错导致角色 / 特效 / 地图比例错 | 首张真实地图 + 真实角色 prefab 做视觉验收 |
| 瓦片加载回调晚于切图 | 加载版本号或取消令牌，旧回调直接丢弃 |
| 动态区域和静态寻路格覆盖关系不清 | 以 yu_client `DynamicBlock` / `GetAreaType` 行为为准写单测 |

---

## 十、项目管理记录

本方案属于 Phase 1 前置设计任务，原因是地图加载会影响：

- `Framework/Scene3D`
- `Module/Combat`
- `Res/GameResPath`
- `Assets/GameRes/resource/game/scene/map`
- Editor 地图资源校验工具
- 后续场景协议、NPC、怪物、门、特效、掉落对象生命周期

实现时每一批代码需要在提交说明中报告：

```text
计划：地图加载第 N 步
原因：对齐 yu_client 地图加载行为
影响范围：列出 Framework / Module / Editor / GameResPath 等文件
回滚方式：删除本批新增类或回退对应路径接口
```
