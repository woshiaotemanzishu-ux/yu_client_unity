---
name: audit-game-ui-route
description: 对已经完成开发的游戏 UI 做最终运行验收：枚举全部控件与状态，使用同账号真实老端和 Unity 验证点击、协议、即时刷新、重开、视觉、模型/特效与性能。仅当用户明确要求“巡检”“验收”“最终收口”“逐叶测试”“真实 Web 对比”或检查是否完成时使用；不得因用户说“先跑通”“补功能”“写逻辑/协议”而触发，也不负责开发修复。
---

# UI 最终验收

本 Skill 只发现并证明问题，不承担实现。验收失败时输出精确缺陷；除非用户随后明确要求修复，否则停止。

## 开始

1. 固定页面、账号、状态、viewport、源码和构建指纹。
2. 读取 [references/yu-client-unity-runbook.md](references/yu-client-unity-runbook.md)。新建正式路线时再读 [references/route-ledger-schema.md](references/route-ledger-schema.md)；确需 GM 前置态时再读 [references/gm-test-state.md](references/gm-test-state.md)。不要预加载无关参考。
3. 检查完成范围保护和工作树，不修改已保护页面。

## 一批验收

1. 一次性枚举页面的页签、按钮、列表、输入、弹窗、条件控件和返回链。
2. 同一页面一次会话跑完适用叶子；验证真实点击、正式协议、成功/失败、父页即时刷新、关闭重开和返回。
3. 顺序采集同账号老端与 Unity，核对状态、2D 视觉、3D/特效、资源和 cold/warm。单会话服务器不得两端同时登录。
4. 新路线用现有 `route_ledger.py` 原子记录；不要为单页另写登录、弹窗、协议或证据工具。
5. 只在全部适用闸通过时标记完成；静态、编译、Editor、Web 和用户局部确认不得互相冒充。

## 止损

- 不改业务代码、Prefab 或公共工具；发现缺陷后报告并结束。
- 同一环境问题重复两次即停止，标记环境 blocker，不在页面任务内修基础设施。
- 不为每个按钮重复登录、编译或构建；一次枚举、一次会话、一次结果批次。
- 用户要求修复时，功能问题转 `ui-function-fast-pass`，视觉问题转 `fix-view`，不得在本 Skill 内继续。
