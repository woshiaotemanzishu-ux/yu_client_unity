# Laya 3D 资产转换调研(2026-06-11,决策讨论中,未动工)

## yu_client Electron 工具(tools/yu-resource-tool)的 3D 家底 —— 别忘了它

| 资产 | 说明 | 状态 |
|---|---|---|
| `python/lm_parser.py`(380 行) | `.lm` 网格二进制解析(顶点/索引/骨骼/绑定姿势) | ✅ 经 3D 预览验证 |
| `python/lani_parser.py`(517 行) | `.lani` 动画解析(LAYAANIMATION:03/04 格式) | ✅ |
| `python/laya_to_glb.py`(754 行) | `.lh+.lm+.lmat+贴图+.lani → 单文件 GLB`(含骨骼蒙皮动画) | ✅ Model3DPreview 渲染即用它 |
| `python/fbx_bridge.py` + glb_to_* | FBX 导出(走 Blender 桥)/ GLB→Laya 导入 | 导入角色/头饰已测通;导出能出带动作 FBX |
| `Model3DPreview.vue` | 浏览器 3D 预览(本地连服务端可运行) | ✅ |

关键事实:`.lh` 是 JSON;只有 `.lm/.lani` 是二进制,解析逻辑已在上述 python 里完整存在。
**这些解析器是 Laya 3D 格式的"事实规格书"——无论最终方案选哪条,都以它为参考实现/校验基准。**

## 待决策:老资产批量转换的实现位置与格式

- 用户倾向:Unity 侧做工具(不把旧工具的运行依赖带进新项目);格式倾向 FBX(行业惯例)。
- 候选方案:
  A. **Unity Editor 直产原生资产**:.lh/.lm/.lani → Unity Mesh/AnimationClip/Prefab,
     无中间格式、零外部依赖;python 解析器仅作规格书+自动比对基准(不进运行链)。
  B. python 批量导出(GLB 或 FBX)→ Unity 自动装配:复用快、但带 python(+Blender)运行依赖。
- 共识(无争议部分):**美术新资产统一 FBX**(DCC 工作流);批量转换必须无人值守+增量,
  清单来自配置表(config_fashion_model.json 等),禁止逐个手工导出。
- 第一验收点(定案后执行):model_clothe_1201(已知能渲染参考)在 Unity 渲染 + 待机动画。
- yu_client 仓库 CLAUDE.md 留有上次 3D 工具踩坑记录(Biped 骨骼结构/单位/端到端先行),动工前必读。

## MVP 验收记录(2026-06-11)

**第一验收点通过**:model_clothe_1201(武姬)在 Unity 渲染正确——几何/贴图/蒙皮/
左右朝向(mirrorX=false 即正确,坐标系无需翻转)/待机动画全部 OK。

定案与教训:
- 方案 A 成立:.lh/.lm/.lani → C# 解析 → Unity 原生资产,零外部依赖;
  python 解析器作为规格书的移植一次成功(老教训"先走通一个真实样本"执行到位)。
- 布料必须双面渲染(Cull Off),否则单层面片"看穿"——已固化进材质生成。
- **弄巧成拙记录**:尝试读 .lmat 的 type/albedoColor 自动决策材质 → 模型不可见,
  已回退到固定模板(MaterialMode)。待拿到真实 .lmat 样本核对字段格式后再
  启用参数化(LFS 占位拿不到,需用户提供)。
- **材质定案:角色 UI 模型用 Unlit(默认)**。证据:UIModelClass3D.ts:456 把角色
  材质按 Laya.UnlitMaterial 处理;electron 工具 glb_to_role_pack.py 生成 .lmat
  写的也是 Laya.UnlitMaterial。即贴图直出、不吃光照——SimpleLit + 无灯场景
  正是「偏暗/朦胧/发黑」的根因。Lit 模式保留给后续真需要光照的资产。

## UI 内 3D 展示(SetRoleModel 对等物)

`Common/UI3D/UIModelStage`:隔离区摆模型 → 专用相机 → RenderTexture → RawImage
贴进 UI 容器、透明底。取景逐行复刻老客户端 UIModelClass3D.ts:正交相机
(orthographicVerticalSize=12.8、z=-20),层级 root(×1.1)→yaw(180°转身)→
body(×5×scale),RT 尺寸跟随容器;scale/position 由调用方传入(登录链路
scale=0.5,position=ConfigLogin 的 ModelPos+PosOffset,TODO 配表线)。
创角页与选角页已接入;职业→默认装映射 剑士1111/武姬1213/枪使1300/弓手1400。

## 下一步(顺序)

1. 四职业默认装各转一次(1111/1213/1300/1400 + 各自 action 目录待机),游戏内创角页验收;
2. 批量转换(config_fashion_model.json 全清单,增量,Addressable 分组);
3. 可视化资产管理(替换/增加/删除)——用户已点名需求;
4. 材质模板统一调优(.lmat 样本核对后参数化)。
