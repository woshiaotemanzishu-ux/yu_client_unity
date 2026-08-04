# UI 路线台账模板

## 状态

- `not-run`：已列出，老端和 Unity 都未跑。
- `baseline-only`：已有老端事实，Unity 未跑。
- `defect`：Unity 差异已复现。
- `fixing`：根因明确，正在修。
- `needs-runtime-verify`：代码/资源已改，缺同路径运行复验。
- `blocked`：需要不可恢复写入授权、账号条件、服务或资源。
- `done`：叶子的功能、真实运行态、2D视觉、3D模型/特效、重开和耗时中所有适用项均通过，并具备机器要求的证据字段。

父节点的状态由子节点推导。只要存在 `defect / fixing / needs-runtime-verify / blocked / not-run / baseline-only`，父节点就不得是 `done`。

`type=page` 的父节点还必须有 `control_inventory[]`。每项至少包含稳定 `id`、控件类型 `kind` 和对应直接子节点 `child`；这是“当前页全部控件已列清单”的机器证据。页签节点不能代替页签内部按钮，新增可见控件必须先入清单再验收。

## 机器台账

长表只用于最终报告；执行期优先维护 JSON。先准备只含 `route` 与 `nodes[]` 的 manifest，每个节点至少含 `id`，可选 `parent/type/risk`，然后运行：

```powershell
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py init manifest.json route-ledger.json
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py apply route-ledger.json results.json
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py validate route-ledger.json
```

`results.json`只需列本批实际跑过的叶子，格式为`[{"id":"...","status":"done","applicable_gates":[...],"gates":{...},"timing":{"cold_ms":123,"warm_ms":45},"visual_evidence":{"old":"...","unity":"...","diff":"..."},"state_evidence":["..."],"model_evidence":{"old":"...","unity":"..."},"effect_evidence":["..."],"resource_evidence":{"preflight_first":"...","preflight_second":"...","runtime_delta":"..."},"evidence":["..."]}]`。`apply`会合并证据并自底向上推导父节点，避免模型反复重写整张长表；未知ID、缺闸或错误父状态会直接失败。

叶子标 `done` 时，`gates` 中所有适用闸必须为 `true`。默认闸名是 `click/result/protocol/immediate/reopen/return_chain/timing/visual_version/visual_match/target_identity/layout_structure/scroll_interaction/page_space_geometry/runtime_state/model_presentation/render_completion/effect_match/resource_stable/restore`；不适用项应从该节点的 `applicable_gates` 中显式移除，不得留空后宣称完成。`timing`要求非负`cold_ms/warm_ms`，`visual_match`要求老端/Unity/diff三份路径；`target_identity/layout_structure/scroll_interaction/page_space_geometry/render_completion`分别要求非空的`identity_evidence[]/layout_evidence[]/interaction_evidence[]/geometry_evidence[]/render_evidence[]`。`render_completion`不能只记录 RawImage 已绑定 RenderTexture；证据至少应包含本轮渲染完成标记和 RenderTexture 非透明像素探针。`runtime_state`要求非空`state_evidence[]`，`model_presentation`要求老端/Unity模型截图，`effect_match`要求非空特效证据。`resource_stable`要求首次预检、第二次幂等预检和玩家点击目录差异证据，第二次必须证明`imported=0、configured=0`。父节点标 `done` 时所有直接子节点都必须是 `done`；`type=page` 的完成父节点还要求 `control_inventory[]` 非空、控件 ID 唯一，且每个 `child` 都存在并确实是直接子节点。

历史台账在新增证据或用户复查发现模型缺失、状态错误、明显视觉偏差时必须降级为`defect`。旧版台账只通过点击/协议/回包，不自动继承新的视觉完成资格。

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
10. 同账号、同角色和可对齐状态下，选中、激活、穿戴、锁定、红点、属性、文案与条件显隐一致。
11. 2D 页面有同分辨率老端/Unity/diff证据，位置、尺寸、层级、裁剪、图片、文字和间距没有未登记差异。
12. 页面有3D展示位时，模型存在且职业/部件正确，不得镜像、翻转或角度明显错误，位置和比例须大致正常；不要求跨引擎逐像素重合，但明显构图差异必须修复。
13. 模型骨骼常驻特效和独立UI特效分别核对；不存在特效的页面才可显式移除该闸。
14. 详情/弹窗的具体 View 类型、主底图、根尺寸和遮罩层与老端一致；“打开了别的通用小窗”或底图 Sprite 为空均失败。
15. 列表/滚动区域具备正确容器树、裁剪与自适应 Content，并以真实拖动证明 Content 位移及末项可达。
16. 所有跨父容器的关键矩形都换算为页面根左上角坐标再比较，不用局部锚点数值冒充页面位置。

## 设置路线树（2026-08-04 第 4 轮结构与叶子身份重开后）

```text
mainui.settings
├─ open-close
├─ base-tab
│  ├─ copy-id
│  ├─ rename
│  │  ├─ query-eligibility
│  │  ├─ submit-result
│  │  ├─ parent-name-immediate-refresh
│  │  └─ reopen-persistence
│  ├─ change-avatar
│  │  ├─ navigation
│  │  ├─ cold-warm-ready-time
│  │  ├─ current-page-version
│  │  ├─ avatar-select-and-refresh
│  │  ├─ fashion-hair-suit-visual-states
│  │  │  ├─ fashion-list-structure-and-real-drag
│  │  │  ├─ suit-banner-page-space-geometry
│  │  │  └─ four-suit-goods-illusion-tip-leaves
│  │  └─ suit-change-confirm-and-immediate-state
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

第 2 轮的功能、协议、即时刷新和重开结论仍有效；第 3 轮又被用户截图重开：列表只是横排、没有滚动容器，套装竖牌因跨父容器锚点错位，条件格误开通用小窗，且测试没有逐格点击。第 4 轮起历史绿灯只有补齐目标身份、结构、真实拖动和页面坐标四个新闸后才可重新标 `done`。
