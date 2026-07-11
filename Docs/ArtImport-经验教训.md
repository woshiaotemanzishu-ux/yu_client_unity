# 创角整模管线——经验教训(写给后续 AI/程序,2026-07-08)

> 这条管线(美术工程成品 prefab → 游戏创角页)从零到全通踩了二十多轮坑,每条教训都对应真实事故。
> 动 `ArtPrefabImporter / RoleCreateView.TryShowWholeModel / UIModelStage / Pandavfx shader` 之前先读完。
> 配套:《ArtImport-使用说明.md》(用法/排查表)、`ArtImportLedger.json`(导入台账)、
> 菜单[神霄/美术/诊断创角整模]→工程根 `ArtImportDiagnose.log`。

## 核心设计决策(为什么是现在这个样子)

1. **落点/体量/透明分流等一切参数,都在【导入时】精确计算并烤进 prefab 的 `ArtModelRenderProfile`,运行时只读**。
   运行时猜过三版(静态包围盒/末帧 Evaluate/达标阈值)全部翻车。导入期用 `clip.SampleAnimation` 拨到
   末帧 + BakeMesh + **骨骼锚点**(脚骨最低点/骨盆XZ;包围盒会被武器披风污染),clip 来源=解析 prefab
   自己的 Timeline 引用(**不许按文件名猜**:1300 的出场动画在 1300.fbx 总轨里,没有 create2.fbx)。
2. **老模型零改动是硬约束**:所有新行为按 `ArtModelRenderProfile` 有无分流。翻转(FLIP_HORIZONTAL)是给
   Laya 镜像几何的补偿,原生模型翻了=左右反;环境光/合成材质/透视相机都只在整模上台时生效。
3. **整模用透视相机(FOV60,距离 6.4/tan30°)**:出场位移主要沿 Z 轴,正交投影下深度移动几何不可见
   (=看着原地做动作,曾连续多轮误判为播放 bug)。美术工程就是透视 FOV60 预览的。
4. **每个 prefab 用自己动作的末帧采样自己的落点/体量**:同一角色两个 FBX 单位错配 2.54×(英寸/厘米,
   美术惯犯,已发生两次)时也能各自归一、切换无缝,不阻塞在美术侧。
5. **跨工程的手工 shader 补丁必须做成导入时的幂等变换**:Pandavfx 的 `_ScrA/_DstA`(alpha 通道混合,
   没它特效把 alpha 写进展示台 RT,预乘合成被污染=发白/压黑)曾靠"两边工程都改过"维持——美术调贴图时
   shader 被换回原版,导入忠实 Replace,主工程补丁被覆盖,特效全线异常。现在 `EnsurePandaAlphaChannels`
   在拷贝时无条件补,回滚传染不进游戏。

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

## 症状→根因速查

| 症状 | 根因 |
|---|---|
| 白模 | 内嵌材质贴图按名引用没搬进来 / FBX 导入时贴图未就位(重导全部 FBX) |
| 整体偏黑 | Lit 材质+登录场景无灯(整模上台切 Flat 白环境光,已内建) |
| 特效洗白/压黑 | shader 丢 _ScrA/_DstA 补丁(导入自愈已内建)或材质 alpha 参数没跑 NormalizePandaAlpha |
| 镜像/换手 | uvRect 翻转错用在原生模型上(按档案分流,已内建) |
| 原地做动作 | applyRootMotion 没开 / 位移沿 Z 被正交吃掉(均已内建) |
| 巨大/悬空/怼镜头 | 落点采样错:静态盒≠动画停放、猜错 clip、单位错配 2.54×(逐 prefab 末帧采样,已内建) |
| 三职业落点不一致 | 包围盒锚点被武器披风污染(骨骼锚点,已内建) |
| 改完没变化 | 没点[替换新模型]重烤档案 / 没重编译 |

## 遗留事项

- 1213 的 create2.fbx 单位是英寸(与 create3 差 2.54×):已被逐 prefab 归一自动中和,美术统一单位重导出更好(诊断的[落点一致性]会点名)。
- 纱体半透:数据缺失,待美术在 服饰.tga alpha 通道刷中间灰(见 `Docs/服饰alpha标注_给美术.png`);管线检测到渐变 alpha 会自动走 Transparent。
- 贴图体积:导入侧压缩已按要求全部移除,原样入库,后续统一处理。
