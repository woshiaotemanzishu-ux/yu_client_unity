# Unity 动态资源开发期可见化规范

> 2026-06-24 建立。目标:保留运行时远程动态加载,同时让 Unity 开发期能看见、能定位、能修改同一份运行时资源。

## 问题定义

Laya 老客户端大量图片、特效、子界面和模型在运行时通过 TS 代码动态加载。Unity 重构后继续使用 Addressables/Remote 动态加载是正确方向,但如果只保留运行时加载,没有开发期可见入口,就会出现:

- 打开 UI prefab 看不到它运行时会加载哪些特效、动态图或子 prefab。
- 资源出错时只能追代码和转换器,不能像正常 Unity 项目一样直接打开 prefab 检查。
- 手工改了某个临时对象,运行时 Addressables 仍加载另一份资源。
- 转换器重跑覆盖人工修正,导致开发成本回到 Laya 运行时黑盒。

因此,动态加载不是问题;**动态资源没有形成 Unity 可编辑资产边界**才是问题。

## 核心目标

1. 运行时仍走 `ResManager + GameResPath/ResourcePath + Addressables Remote` 异步加载。
2. 开发期 UI prefab 中必须能在 Hierarchy 里看到动态资源 slot 节点,并能从 Inspector 跳转到实际运行时资源 prefab。
3. 开发期修改的资源必须就是 Addressables 运行时加载的同一份资源。
4. 转换器可以重复运行,但不能覆盖人工可编辑层。
5. 资源名、路径、特效名优先来自配置、分析器产物或 slot/profile,禁止业务代码随手硬编码。

## 资产分层

动态资源统一分为四层:

```text
Laya Source
  -> Generated Base Asset
  -> Runtime Editable Asset
  -> UI Dynamic Resource Slot / Profile
```

### 1. Laya Source

来源包括:

- `D:/git_res/yu_client/cdn/resource/...`
- `.scene/.json`
- `.lh/.lmat/.lm/.lani`
- 图片、图集、配置 JSON

这是导入来源,不直接作为 Unity 运行时资源。

### 2. Generated Base Asset

转换器产物基底,只由工具覆盖,不允许人工修改。

建议路径:

```text
Assets/GameRes/_Generated/{domain}/...
```

示例:

```text
Assets/GameRes/_Generated/effect/objs/ui_effect/ui_dianjizhiyin/ui_dianjizhiyin_base.prefab
```

规则:

- 可重复覆盖。
- 不作为运行时 Addressable 入口。
- 只表达从 Laya 源资源自动转换出的基准结构。
- 需要记录 source path、tool version、import report。

### 3. Runtime Editable Asset

项目开发者真正编辑、验收、运行时加载的资源。

建议路径保持现有运行时 key:

```text
Assets/GameRes/effect/objs/ui_effect/ui_dianjizhiyin/ui_dianjizhiyin.prefab
```

规则:

- 这是 Addressables 分组和运行时加载的对象。
- 可以是 Generated Base 的 prefab variant,也可以是带 source link 的独立 runtime prefab。
- 人工修材质、缩放、挂点、额外组件、Shader 参数时只改这一层。
- 转换器重跑只能刷新 Generated Base;除非用户明确选择覆盖 runtime 层,否则不得覆盖人工修改。
- Runtime 资源必须能追溯到 Generated Base 和 Laya Source。

### 4. UI Dynamic Resource Slot / Profile

UI prefab 中的开发期可见绑定层。

示例:

```text
MainUITaskItem
  _box_finger_con
    UIDynamicResourceSlot(select_effect = effect/objs/ui_effect/ui_yindaoxiaoguo/ui_yindaoxiaoguo)
    UIDynamicResourceSlot(finger_effect = effect/objs/ui_effect/ui_dianjizhiyin/ui_dianjizhiyin)
```

规则:

- slot/profile 存的是运行时 Addressable key 或配置引用,不是临时场景对象。
- slot 必须是宿主节点下的显式子节点,例如 `__DynamicResources/main_ui_guide_select`;禁止只把多个 slot 组件堆在宿主节点 Inspector 上。
- Inspector 必须能显示资源状态:存在、缺失、过期、未分组、依赖缺失。
- Inspector 必须能打开 Runtime Editable Asset。
- Editor 预览可以实例化本地资源,但运行时仍走 `ResManager`。
- UI 代码优先读取 slot/profile 或配置,不直接散落资源名常量。

## Addressables 规则

1. Addressables 只注册 Runtime Editable Asset 及其依赖。
2. Generated Base Asset 默认不注册为运行时 key。
3. Addressable key 必须与 `GameResPath`/`ResourcePath.Normalize()` 一致。
4. Editor 的 `Use Asset Database (fastest)` 只是用本地 AssetDatabase 解析同一个 key,不能绕过 key 体系。
5. `Addressable 自动分组` 必须能识别 runtime prefab、动态资源 slot 和依赖资源。

## 转换器规则

1. 转换器负责从 Laya Source 生成 Generated Base。
2. Runtime Editable Asset 不存在时,工具可以由 Generated Base 初始化一份 runtime prefab/variant。
3. Runtime Editable Asset 已存在时,默认保留人工修改,只标记 base 过期或提供对比/合并入口。
4. 转换报告必须列出:
   - source path
   - generated base path
   - runtime asset path
   - addressable key
   - 引用该资源的 UI slot/profile
   - 缺图、缺材质、Shader 错误、依赖缺失
5. 不允许靠业务代码对某个转换失败资源写临时修正。

## UI 开发规则

所有运行时动态资源都必须落到可见 slot/profile。这里的“可见”指 Hierarchy 中有明确子节点,不是只在 Inspector 里挂隐藏组件:

- 动态图片:`UIImageSlot` 或 `UIDynamicResourceSlot(type=image)`
- UI 特效:`UIEffectSlot`
- 3D 模型展示:`UIModelSlot`
- 动态子 prefab:`UIPrefabSlot`
- 声音/动画等后续资源同理

业务代码职责:

- 读取配置、slot/profile 和状态。
- 调用统一运行时加载器。
- 控制显示、隐藏、播放、释放。

业务代码禁止:

- 随手拼 Addressable 路径。
- 把特效名、图片名散落成 private const。
- 为某个资源问题写专用缩放、材质、坐标补丁。
- 直接实例化不是 Addressables key 对应的临时 prefab。

## 当前实施顺序

先解决架构层,再回到具体引导问题。

1. **建立 Runtime Editable Asset 边界**
   - 修改特效/UI 动态资源转换器,增加 Generated Base 与 Runtime Editable Asset 分层。
   - Addressables 指向 runtime 层。

2. **建立动态资源 slot/profile**
   - 新增通用 `UIDynamicResourceSlot` 基类和 `UIEffectSlot`。
   - slot 挂在 UI prefab 的 `__DynamicResources` 子节点下,保存 key、来源、预览设置和备注。

3. **编辑器可见化**
   - 给 slot 做 Inspector:打开资源、预览、校验、重新转换 base、检查 Addressables。
   - AssetHub/资源管理显示反向引用:哪些 UI slot 正在使用这个动态资源。

4. **转换流水线接入**
   - LayaUI 分析器把能静态识别的运行时动态图/特效写入 manifest。
   - UI 转换器根据 manifest 回填 slot。
   - 无法静态识别的动态资源继续进报告,不得静默丢失。

5. **用 MainUI 引导作为首个验收切片**
   - `MainUITaskItem` 上能看到 `ui_yindaoxiaoguo` 和 `ui_dianjizhiyin` slot。
   - 能从 slot 打开对应 runtime prefab。
   - 修改 runtime prefab 后,Play 模式和远程 Addressables 加载同一份资源。
   - 重跑转换器不覆盖人工修改。

## 验收标准

一个动态资源链路只有满足以下条件才算接入完成:

- 打开宿主 UI prefab 能在 Hierarchy 中看到动态资源 slot 子节点。
- slot 能打开实际运行时加载的 prefab/asset。
- 运行时加载和开发期预览使用同一个 Addressable key。
- 自动分组能把 runtime asset 和依赖资源放进正确 group。
- 转换器重跑不覆盖 runtime 层人工修改。
- 资源管理工具能查到正向状态和反向引用。
- 缺资源、缺图、Shader 错误、过期版本必须在报告或 Inspector 明确暴露。

## 对现有问题的归类

主界面任务引导的问题应拆成三类:

- 任务条 UI 结构:`MainUITaskTeamView/MainUITaskItem` prefab。
- 选中框四角/流光:`effect/objs/ui_effect/ui_yindaoxiaoguo/ui_yindaoxiaoguo` runtime prefab。
- 点击手指/光点:`effect/objs/ui_effect/ui_dianjizhiyin/ui_dianjizhiyin` runtime prefab。

这三个资源应通过 UI slot 建立可见关系。修 `ui_dianjizhiyin` 紫块时,应修改它的 runtime prefab/material/shader 或转换规则,而不是在 `MainUIGuideManager` 写资源专用补丁。
