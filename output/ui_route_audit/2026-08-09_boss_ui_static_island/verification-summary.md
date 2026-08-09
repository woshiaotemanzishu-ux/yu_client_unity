# Boss UI 文件岛静态交付汇总

日期：2026-08-09

## 完成边界

- 仅修改 `Assets/Scripts/Module/Core/Boss/Views/BossMain|BossPersonal|BossField|BossMystery|BossDomain`、对应现有页面专属 Prefab，以及本输出目录。
- 五条路线均使用现有 Prefab 做 `fix-view` 增量接管；未调用 `convert-module` 重转。
- 未启动或控制 Unity、浏览器、前台程序；未执行 GM、登录或账号写事务；未构建 Web Player/Addressables。
- 本轮只声明静态代码、Prefab 身份链和 schema 6 台账通过；不声明真实 Unity/Web、同账号、像素、模型、特效、滚动、性能或事务完成。

## 路线状态

| 路线 | schema 6 节点 | blocked | needs-runtime-verify | done |
|---|---:|---:|---:|---:|
| Boss 主入口/战斗面板 | 46 | 41 | 5 | 0 |
| BossPersonal | 35 | 23 | 12 | 0 |
| BossField | 82 | 63 | 19 | 0 |
| BossMystery | 17 | 10 | 7 | 0 |
| Bossdomain | 39 | 24 | 15 | 0 |
| 合计 | 219 | 161 | 58 | 0 |

每条路线的 `route-manifest.json`、`results-static.json`、正式 `route-ledger.json` 与 `audit-summary.md` 位于对应子目录。正式台账均经 `route_ledger.py init/apply/validate`；父状态由叶状态回卷。

## 静态实现与校验

- 35 个 Prefab-backed 业务 Bind subclass 已逐个核对 `.cs.meta` GUID、目标 Prefab `m_Script` 和 `Shenxiao.Module.Core` 的 `m_EditorClassIdentifier`；检查结果 0 错误。
- 检查 8 个目标 Prefab：无 `m_Script fileID: 0`，无悬空 `m_Children` 引用。
- 所有新增页面目录具备固定 `.meta`；已移除运行时 `AddComponent` 补业务 View 的未绑定兜底。
- 统一 `BossIsland.Isolated.csproj` 仅编译本轮新 C#，引用现有 `Library/ScriptAssemblies`，输出到本目录 `artifacts`；最终为 0 warning / 0 error。
- 限定文件岛 `git diff --check` 通过。

终验过程中已修复 Mystery 列表循环回调捕获、模板重复显示、领奖即时刷新，Personal 的 `VipLevel -> vip_flag/card` 与 `DailyCount -> first_flag` 未证映射，Domain 主进入 CTA 缺绑定及协助勾选图隐藏后无法点击等问题；最终只读复查为 0 outstanding 静态问题。

## 主要 blocker

- MainUI/BaseWindow/CrossServer/Common/Goods/Guild/VIP/Scene 等跨模块入口、共享弹窗与场景消费链不在本文件岛，只登记依赖。
- 实时战斗实体、寻路/采集、伤害归属、复活矩阵、动态模型/RenderTexture/特效、完整 Boss 配置字段与资源闭包仍需运行时和跨模块工作。
- 进入、关注、领取、购买、复活等写事务虽然只在有正式控制器 API 与旧端直接证据的位置做静态点击接线，但本轮未执行；台账叶仍为 `blocked`。
- 真实 Web 必须在源码、Player、catalog 指纹一致的同一批次，以同账号、两档 viewport 顺序复走后才能关闭相应闸门。

未更新 `Docs`：用户将 `Docs`、`AGENTS.md` 明确列为禁止修改文件，且本轮没有把静态阶段冒充已验证实施进度。
