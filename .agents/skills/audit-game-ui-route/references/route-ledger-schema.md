# UI 路线台账模板

## 状态

- `not-run`：已列出，老端和 Unity 都未跑。
- `baseline-only`：已有老端事实，Unity 未跑。
- `defect`：Unity 差异已复现。
- `fixing`：根因明确，正在修。
- `needs-runtime-verify`：代码/资源已改，缺同路径运行复验。
- `blocked`：需要不可恢复写入授权、账号条件、服务或资源。
- `done`：叶子的点击、结果、即时状态、重开、耗时和版本视觉中所有适用项均通过。

父节点的状态由子节点推导。只要存在 `defect / fixing / needs-runtime-verify / blocked / not-run / baseline-only`，父节点就不得是 `done`。

## 机器台账

长表只用于最终报告；执行期优先维护 JSON。先准备只含 `route` 与 `nodes[]` 的 manifest，每个节点至少含 `id`，可选 `parent/type/risk`，然后运行：

```powershell
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py init manifest.json route-ledger.json
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py apply route-ledger.json results.json
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py validate route-ledger.json
```

`results.json`只需列本批实际跑过的叶子，格式为`[{"id":"...","status":"done","applicable_gates":[...],"gates":{...},"evidence":[...]}]`。`apply`会合并证据并自底向上推导父节点，避免模型反复重写整张长表；未知ID、缺闸或错误父状态会直接失败。

叶子标 `done` 时，`gates` 中所有适用闸必须为 `true`。默认闸名是 `click/result/protocol/immediate/reopen/return_chain/timing/visual_version/restore`；不适用项应从该节点的 `applicable_gates` 中显式移除，不得留空后宣称完成。父节点标 `done` 时所有直接子节点都必须是 `done`。

## 台账表

| ID | 父节点 | 类型 | 叶子操作/结果 | 老端基线 | Unity 现状 | 即时 UI/Model | 关闭重开 | cold/warm | 版本视觉 | 写入风险 | 状态 | 证据 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|

类型使用 `page / tab / navigation / read / reversible-write / destructive-write / transaction / return`。

## 叶子验收闸

对每个叶子判断下列项是否适用，适用项不得留空：

1. 真实点击面命中，事件只触发一次。
2. 目标页/弹窗/业务结果与当前老端一致。
3. 发包、回包、失败和无回包语义一致。
4. 成功后当前已打开的父页立即刷新，不依赖退出重进。
5. 关闭重开后从权威 Model/服务状态恢复一致结果。
6. 返回链回到正确父页，层级和遮罩没有残留。
7. 首屏可见与可交互就绪的 cold/warm 耗时有数字证据。
8. 目标页的标题、页签、布局、功能清单和主要资源属于当前老端版本。
9. 可恢复写入已还原；破坏性写入有授权和专用测试状态。

## 设置路线树（2026-08-03 重开）

```text
mainui.settings
├─ open-close
├─ base-tab
│  ├─ copy-id
│  ├─ rename
│  │  ├─ query-eligibility
│  │  ├─ submit-result
│  │  ├─ parent-name-immediate-refresh   [defect]
│  │  └─ reopen-persistence
│  ├─ change-avatar
│  │  ├─ navigation
│  │  ├─ cold-warm-ready-time           [defect: >5s]
│  │  ├─ current-page-version           [defect: Unity old page]
│  │  └─ avatar-select-and-refresh
│  ├─ sliders
│  │  ├─ same-screen-count
│  │  ├─ effect-count
│  │  ├─ sound
│  │  └─ music
│  ├─ auto-pick-items
│  ├─ mount-block
│  ├─ sentient-block
│  └─ auto-task-block
├─ shield-tab
│  └─ ten-shield-options
├─ switch-role
├─ switch-account
├─ restore-default
├─ escape-stuck
└─ repair-abnormal
```

上一轮只验到 rename 弹窗和 change-avatar 跳转，所以这两个父节点都不能是 `done`。后续必须先把一条子树跑完，再返回选下一条。
