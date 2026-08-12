---
name: convert-module
description: 把尚无可编辑 Unity Prefab 的老 Laya 页面首次转换为 Prefab 和基础 Bind/View。仅当用户明确要求“转换模块/页面”，且已确认目标没有可编辑 Prefab 时使用；已有 Prefab 的功能补全或视觉精修不得触发。
---

# Laya UI 首次转换

本 Skill 只负责第一次落地，不负责后续功能快接、视觉精修或最终验收。

## 流程

1. 先证明目标没有可编辑 Prefab；否则停止并改用相应 Skill。
2. 读取 [Docs/LayaUI转换流水线.md](../../../Docs/LayaUI转换流水线.md) 中与目标模块直接相关的步骤。
3. 从老端真实运行时页面采集最小页面闭包，不以静态 `.scene` 冒充运行结果。
4. 一次性生成 Prefab、Bind 和 data-only View，并注册必要资源。
5. 做一次定向编译和结构检查后停止；功能补全转 `ui-function-fast-pass`，视觉问题转 `fix-view`。

## 边界

- 不覆盖人工接管或已验收 Prefab。
- 不为单个页面全量转换模块、刷新全库或重建 Library。
- 不在转换阶段启动 GM、最终 UIDAudit、Web 对比或像素精修。
