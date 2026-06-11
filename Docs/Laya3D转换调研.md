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
