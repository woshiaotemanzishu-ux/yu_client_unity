# 创角整模管线——经验教训(写给后续 AI/程序,2026-07-08)

> 这条管线(美术工程成品 prefab → 游戏创角页)从零到全通踩了二十多轮坑,每条教训都对应真实事故。
> 动 `ArtPrefabImporter / RoleCreateView.TryShowWholeModel / UIModelStage / Pandavfx shader` 之前先读完。
> 配套:《ArtImport-使用说明.md》(用法/排查表)、`ArtImportLedger.json`(导入台账)、
> 菜单[神霄/美术/诊断创角整模]→工程根 `ArtImportDiagnose.log`。

## 核心设计决策(为什么是现在这个样子)

1. **落点/透明分流等参数在【导入时】精确计算并烤进 prefab 的 `ArtModelRenderProfile`,运行时只读**。
   运行时猜过三版(静态包围盒/末帧 Evaluate/达标阈值)全部翻车。导入期用 `clip.SampleAnimation` 拨到
   末帧 + BakeMesh + **骨骼锚点**(脚骨最低点/骨盆XZ;包围盒会被武器披风污染),clip 来源=解析 prefab
   自己的 Timeline 引用(**不许按文件名猜**:1300 的出场动画在 1300.fbx 总轨里,没有 create2.fbx)。
2. **模型统一无光照，老模型零改动**:美术贴图直接提供最终颜色；新模型上台时仅在实例上把
   Standard/URP Lit 表面改为 URP Unlit，关闭投射/接收阴影，不写 `RenderSettings`、不创建灯。Panda/粒子特效材质保留。
   翻转(FLIP_HORIZONTAL)仍只给 Laya 镜像几何补偿；原生模型翻了=左右反。合成材质/透视相机继续按档案分流。
3. **整模用透视相机(FOV60,距离 6.4/tan30°)**:出场位移主要沿 Z 轴,正交投影下深度移动几何不可见
   (=看着原地做动作,曾连续多轮误判为播放 bug)。美术工程就是透视 FOV60 预览的。
4. **角色体量以美术源模型为准，1400 作为大致参照，程序不再按包围盒自动缩放**：每个动作仍独立采样脚底落点，
   但 `landingScale` 与 `attachmentSpaceScale` 均固定为 `1`。death 卧倒、jump 腾空和披风展开会污染姿势包围盒，
   只能用于诊断，不能据此改变同一角色或不同角色的体量。旧字段保留仅为序列化兼容，不得继续写入非 1 补偿。
5. **跨工程的手工 shader 补丁必须做成导入时的幂等变换**:Pandavfx 的 `_ScrA/_DstA`(alpha 通道混合,
   没它特效把 alpha 写进展示台 RT,预乘合成被污染=发白/压黑)曾靠"两边工程都改过"维持——美术调贴图时
   shader 被换回原版,导入忠实 Replace,主工程补丁被覆盖,特效全线异常。现在 `EnsurePandaAlphaChannels`
   在拷贝时无条件补,回滚传染不进游戏。
6. **整模经验不能无差别套到组装部件**:`applyRootMotion=true` 只属于角色主体 Animator。头饰、武器、
   翅膀等独立 prefab 必须保留美术设置。1005 翅膀三个 Animator 在源 prefab 都明确为 false；公共上台
   逻辑曾把它们全部改成 true。运行时应以最近的 `ArtModelRenderProfile` 划分主体与部件，只有根档案所属
   Animator 才启用 Root Motion；这是保持源 prefab 运动设置的正确性修复，不应再把它当作渲染异常根因。
7. **HDR 数值必须在真实中间 RT 上做 A/B，不能凭材质峰值猜**：2026-07-29 对正式装配后的同一
   `wing_1005`、同一相机做 `ARGB32` / `ARGBHalf` 同帧对照；9 个非粒子 Renderer 均启用、材质和包围盒
   正常，但 `ARGB32` 会把红绿上翼截成发灰发暗的 LDR 中间色，`ARGBHalf` 则保留与 prefab 预览一致的
   HDR/半透明层次。选角和场景恰好共用该错误 RT 精度，因此现象一致。正确修复是两条模型台统一
   `ARGBHalf + Camera.allowHDR`，`StageComposite` 只做 `One/OneMinusSrcAlpha` 预乘贴回；禁止用 RGB
   最大亮度补 Alpha，那会把 `guanghuan` 等加法粒子错误变成实心遮罩。
8. **“加法材质不写 Alpha”不能一刀切到蒙皮结构层**：1005 `wing-2` 的两个
   `SkinnedMeshRenderer` 共用 `10.mat`，RGB 明确为 `One/One`，但美术同时把 `_MainColor.a` 设为
   `0.937`。旧导入器仅按 `_Dst=One` 把 `_ScrA/_DstA` 统一成 `Zero/One`，导致该层在透明 RT 中
   `maxAlpha=0`，视觉上像整个节点没挂。导入器现把“加法 + SkinnedMesh + MainColor.a<1”识别为
   结构层，保留 `One/OneMinusSrcAlpha` 覆盖；ParticleSystem 加法仍不写 Alpha，避免光环成为实心遮罩。
9. **新模型翅膀可用 `yincang` 节点声明跑动隐藏层**：`RoleModelAssembler.BuildNewModelAsync` 在每个
   新动作实例装配完翅膀后，递归查找翅膀内精确名 `yincang` 的节点；`run` 动作实例将其隐藏，其他动作
   实例将其显示。规则只作用于新模型逐动作装配链，老模型 `BuildOldModelAsync` 不变；需要跑动隐藏的
   翅膀由美术直接在可编辑 Prefab 中建立该命名节点，不在代码里写具体翅膀 ID 或子网格名。

## 排查心法(按这个顺序,别跳)

1. **先证明数据存在,再怀疑渲染**:两个方向都发生过——"该透的白块"一半是材质 Opaque 没读 alpha(数据在,
   引擎侧修),一半是 alpha 根本没画(纱体=255,FBX TransparencyFactor=0,无 opacity 图→美术补数据,
   引擎变不出来)。取证手段:python 把贴图通道导成 PNG 亲眼看、解析 FBX 二进制(曲线/材质属性全能解)。
2. **编辑器预览≠运行时**:Timeline 预览无视 applyRootMotion(运行时必须开,否则 Generic 根位移不播);
   FBX 内嵌材质的贴图按名搜索发生在导入那一刻(顺序/重导都会造成悬空→白模,所以导入对全部模型
   ForceUpdate 二次导入)。
3. **GUID 闭包看不见"按名引用"**:内嵌材质贴图记的是美术机路径;所以必须整文件夹导入。内嵌材质名是
   Max 默认名("21 - Default"),想按名匹配外置材质此路不通。
4. **"改了代码没效果"先查执行链**:落点等参数是导入时烤的——没重新点[替换新模型]就没效果;台账里
   有没有"落点采样"记录是铁证。连续两轮无效果,换取证手段,不要换假设硬猜。
5. **展示链路绝不手动碰 PlayableDirector**(RebuildGraph/拨帧 Evaluate 禁止),只设 extrapolationMode
   和读 time;采样统一放导入期(SampleAnimation 不走 Director)。
6. **看到“同一头饰换身体后突然巨大/极小”，先查源 FBX 单位和根/内容节点 scale**：用 1400 体量作大致参照，
   确认 Role 与附件均按统一单位导出。程序侧两个历史倍率必须仍为 `1`，不能再用运行时反向补偿掩盖源资源问题。

## 症状→根因速查

| 症状 | 根因 |
|---|---|
| 白模 | 内嵌材质贴图按名引用没搬进来 / FBX 导入时贴图未就位(重导全部 FBX) |
| 整体偏黑 | 运行实例漏转 URP Unlit，或美术有色贴图未进 `_BaseMap`；不得用平行光/环境光补亮 |
| 特效洗白/压黑 | shader 丢 _ScrA/_DstA 补丁(导入自愈已内建)或材质 alpha 参数没跑 NormalizePandaAlpha |
| 部件运动/轨迹与美术 prefab 不一致 | 公共上台逻辑误改部件 Animator；只允许角色主体开启 Root Motion，部件保留原值 |
| 翅膀白/黄硬块、纹理和色相丢失 | 先确认场景台复用 `ArtModelRenderProfile` 的独立 Renderer、Depth/Opaque Texture，再确认模型透明 RT 为 `ARGBHalf`、相机 HDR 已开、StageComposite 原样预乘贴回 |
| 翅膀上翼褪色但光环/长亮条仍清楚 | `ARGB32` 已截断 Panda HDR/半透明中间色；改回两条模型台统一 `ARGBHalf`，不要加灯，也不要用 RGB 亮度伪造 Alpha |
| Prefab/运行层级中有 `wing-2`，画面却像少了该层 | 检查该蒙皮结构材质的 Alpha 混合；1005 `10.mat` 必须为 `_ScrA=One/_DstA=OneMinusSrcAlpha`。若为 `Zero/One`，对象和 Renderer 都正常也会因 RT Alpha 恒为 0 而失去覆盖 |
| 镜像/换手 | uvRect 翻转错用在原生模型上(按档案分流,已内建) |
| 原地做动作 | applyRootMotion 没开 / 位移沿 Z 被正交吃掉(均已内建) |
| 巨大/悬空/怼镜头 | 先分开查落点与源模型尺寸：落点采样错会悬空，FBX 单位/源体量错会整体巨大；程序不再自动缩放 |
| 同一附件换角色后大数倍/小数倍 | Role 或附件源 FBX 单位、Reset XForm、内容节点 scale 不统一；回美术修源文件，禁止写非 1 附件补偿 |
| 同一角色换 death/jump/run 后体型跳变 | 动作 FBX 导出单位或根变换不一致；`landingScale` 必须全部为 1，姿势包围盒只作诊断 |
| 三职业落点不一致 | 包围盒锚点被武器披风污染(骨骼锚点,已内建) |
| 改完没变化 | 没点[替换新模型]重烤档案 / 没重编译 |

## 遗留事项

- 1213 的旧 create2.fbx 单位曾与 create3 相差 2.54×；2026-08-12 起不再由程序归一，美术必须统一单位后重导出。
- 纱体半透:数据缺失,待美术在 服饰.tga alpha 通道刷中间灰(见 `Docs/服饰alpha标注_给美术.png`);管线检测到渐变 alpha 会自动走 Transparent。
- 贴图体积:导入侧压缩已按要求全部移除,原样入库,后续统一处理。
