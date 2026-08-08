# 共鸣人工复查回卷（2026-08-07 15:50）

## 复查事实

- 本轮人工复查认定上一轮整体效果可接受，但指出三项遗漏：部位装备格缺共鸣特效框、材料组没有按整体宽度居中、共享物品详情的类型文字换行且单按钮偏左。
- 原图已保存为 `user_feedback_full.png` 与 `user_feedback_detail.png`。这两张人工运行态截图推翻对应叶子的旧 `done` 结论；旧 `2026-08-07_1452_instruction_final` 证据只保留为修前基线。
- 三项都属于共享组件或共享容器问题，不按 22 个部位分别写页面补丁。

## 修复落点

| 缺陷 | 共享落点 | 页面输入 | 状态矩阵 |
|---|---|---|---|
| 部位装备格缺特效框 | `Assets/Scripts/Module/Core/Common/Views/EquipmentItem.cs` 的 `SetSuitEffect` | 特效资源名、阶级 | 关闭；一阶 `effBox`；二阶 `effBox1` 横向 1.3；三阶 `effBox2` 整体 1.3；刷新/销毁清理 |
| 材料从左向右顶格排列 | `Assets/Prefabs/UI/Suit/SuitModule.prefab` 的 `costList.content` | 1/2/3 个 `EquipSuitCostItem` 数据 | Content 按 preferred width 收缩，中心锚点/中心 pivot，1/2/3 项整体居中；物品继续复用 `_tpl_EquipmentItem` |
| 类型文字换行、确定按钮偏左 | `Assets/Prefabs/UI/Common/CommonModule.prefab` 的 `GoodsTooltips` | 类型、数量、可见按钮集合 | 类型/数量栏 240px；单按钮、双按钮均由 `btn_group/HorizontalLayoutGroup` 整体居中 |

部位特效是否出现由 `ResonanceConfigs.GetPositionEffectTier` 对标老端最高可达阶规则计算，`ResonancePresenter` 只把资源名和阶级传给共享 `EquipmentItem`。页面没有复制特效宿主、缩放或生命周期逻辑。

## 当前验证边界

- `route_ledger.py` schema 5 自测通过，并保留 schema 4 历史台账兼容。
- `Shenxiao.Module.Core.csproj`：restore 后串行 build，0 error（仓库既有 warning 99）。
- `Shenxiao.Editor.csproj`：restore 后串行 build，0 error（仓库既有 warning 2）。
- 已扩展 `ResonanceCase` 静态断言和 `ResonanceRouteCase` 运行态断言：共享模板身份、1/2/3 材料整体居中、类型文字单行、单按钮居中、部位特效 handle/宿主/真实 RT 像素及关闭清理。
- 当前项目已有 Unity Editor 与导入 Worker 在运行，本轮未另启 batchmode、未操作用户前台。因此新增断言尚未执行，相关叶子统一回卷为 `needs-runtime-verify`，不得沿用修前截图宣称通过。

下一次复验必须写入新的不可变目录，并至少产出部位特效框、1/2/3 材料组、物品详情单按钮和双按钮状态的局部证据，再沿完整共鸣路线复走一次。
