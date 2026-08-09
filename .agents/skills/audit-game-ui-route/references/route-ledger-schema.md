# UI 路线台账 schema 6

## 1. 状态与完成边界

- `not-run`：已列出，老端和 Unity 都未跑。
- `baseline-only`：已有老端事实，Unity 未跑。
- `defect`：Unity 差异已复现。
- `fixing`：根因明确，正在修。
- `needs-runtime-verify`：代码/资源已改，缺同路径运行复验；schema 6 必须写非空 `runtime_gap`。
- `blocked`：需要不可恢复写入授权、账号条件、服务或资源；schema 6 必须写非空 `blocked_reason`。
- `done`：该叶子的全部适用闸已在明确运行批次中通过，并绑定可校验的证据。

父状态由直接子节点推导。任一子节点为 `defect / fixing / needs-runtime-verify / blocked / not-run / baseline-only`，父节点都不得是 `done`。页面能打开、协议发出、Editor 退出码为 0 或旧截图存在，都不能单独完成父页。

新建台账固定使用 schema 6。schema 2～5 只保留历史读取兼容；禁止只改 `schema` 数字来伪造新证据。历史路线被新截图推翻后，应从原 manifest 新建 schema 6 台账，把仍有效结论按明确 `gate_runs/gate_evidence` 重新绑定，而不是把旧字符串路径机械复制成新绿灯。

## 2. 唯一写入口与原子性

```powershell
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py init manifest.json route-ledger.json
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py apply route-ledger.json results.json
python .agents/skills/audit-game-ui-route/scripts/route_ledger.py validate route-ledger.json
```

- `init` 先在内存校验 manifest，再原子写入 schema 6 台账；坏 manifest 不会留下半成品正式账。
- `apply` 先校验现有台账，在内存合并结果、回卷父状态、复算汇总，再校验候选；候选失败时正式文件字节不变。
- `init` 拒绝覆盖已存在的目标；`init/apply` 对同一绝对台账路径持有系统临时目录中的非阻塞跨进程写锁。第二个写者会明确失败，禁止两个进程各自基于旧账提交后让后写者静默覆盖前一批结果。
- 路线专用脚本只能生成 manifest 或紧凑 results，禁止直接写正式 `route-ledger.json`。2026-08-07 共鸣专用更新器已封存为历史候选重放工具，不再是当前写入口。
- schema 6 的 `done` 结果必须在本批结果中显式提交完整 `applicable_gates/gates/gate_runs/gate_evidence` 和对应专项字段；不能靠节点里残留的旧 `true` 或旧路径重新变绿。

## 3. Manifest 结构

manifest 只描述树、类型、风险和控件清单，不写完成结论：

```json
{
  "route": "mainui.role.example",
  "baseline": {},
  "nodes": [
    {
      "id": "mainui.role.example",
      "type": "page",
      "risk": "read-only",
      "control_inventory": [
        { "id": "open-detail", "kind": "button", "child": "mainui.role.example.open-detail" }
      ]
    },
    {
      "id": "mainui.role.example.open-detail",
      "parent": "mainui.role.example",
      "type": "navigation",
      "risk": "read-only"
    }
  ]
}
```

schema 6 结构门禁：

1. 恰好一个根节点，且根节点 `type=page`。
2. 父链无缺失、无环，ID 唯一。
3. 节点类型只使用 `page / tab / navigation / read / reversible-write / destructive-write / transaction / return`。
4. 风险只使用 `read-only / reversible-write / destructive-write`。
5. 每个有直接子节点的 `page` 都必须有 `control_inventory[]`；控件 ID 唯一，每个控件只映射一个直接子节点，全部直接子节点必须恰好被覆盖一次。用一个“页签组”吞掉页内多个按钮会在 init 阶段失败。

`manifest_source` 是该账的不可变拓扑合同。validator 会读取并核对其 SHA-256、路线名、节点集合，以及每个节点的 `parent/type/risk/control_inventory`。manifest 内容或这些字段变化后，旧账立即失败；应保留旧账并用修正版 manifest 初始化一个新版本台账，再把仍有效证据按新 run/哈希合同显式提交，禁止直接修改旧账或只改 manifest 哈希。

## 4. 运行批次与证据引用

### 4.1 `verification_runs`

每个完成闸都要指向一个运行批次。所有批次均记录带时区的时间、Git HEAD 和 dirty 指纹：

```json
{
  "verification_runs": {
    "web-20260808-01": {
      "recorded_at": "2026-08-08T15:30:00+08:00",
      "environment": "real-web",
      "git_commit": "40或64位Git哈希",
      "dirty_fingerprint": "64位SHA-256",
      "player_sha256": "64位SHA-256",
      "catalog_sha256": "64位SHA-256",
      "viewports": ["720x1280", "1920x1080"],
      "old_session_disconnected": true,
      "unity_session_valid": true,
      "report": { "path": "output/.../headless-report.json", "sha256": "64位SHA-256" }
    },
    "editor-20260808-01": {
      "recorded_at": "2026-08-08T14:00:00+08:00",
      "environment": "unity-editor",
      "git_commit": "40或64位Git哈希",
      "dirty_fingerprint": "64位SHA-256",
      "unity_version": "6000.3.17f1"
    }
  }
}
```

`environment` 只允许 `static / unity-editor / real-web / user-runtime`。真实点击、运行态、滚动、模型/特效和恢复类闸不能绑定 `static`；`visual_match` 必须绑定 `real-web`。根页面 `done` 时，顶层 `route_run_id` 必须指向 `real-web` 批次；该批次不得早于任何完成叶引用的 run，且双方 Git commit/dirty 指纹必须一致。根页一旦回卷为非完成态，`apply` 会清除旧 `route_run_id`，因此新 Editor 证据不能借旧 Web 批次把整页重新变绿。

### 4.2 两类证据引用

不可变文件必须记录路径与 SHA-256；校验时会读取文件并比对哈希：

```json
{ "path": "output/ui_route_audit/2026-08-08_example/run-001/frame.png", "sha256": "...64位..." }
```

人工观察只能写成有来源、时间和范围的断言，不能只写“用户说好了”：

```json
{
  "assertion": "背包格完全滚出后目标流光 alpha 为零",
  "source": "user-runtime",
  "scope": "背包高频代表消费者，仅关闭 viewport 残框缺陷",
  "observed_at": "2026-08-08T01:10:00+08:00"
}
```

人工断言可以关闭它实际覆盖的具体缺陷，但不能代替未执行的双帧文件、其它代表宿主、资源幂等或真实 Web 同批证据。

## 5. done 叶子的闸绑定

每个适用闸必须同时具备：

1. `gates[gate] = true`；
2. `gate_runs[gate] = verification_run_id`；
3. `gate_evidence[gate] = [证据引用...]`；
4. 该闸对应的结构化专项字段。

完成叶的 `gates/gate_runs/gate_evidence` 键集合必须与 `applicable_gates` 完全一致。重新提交 `done` 时三张表整体替换，并清除已不适用闸的专项字段，禁止让旧 run、旧证据或旧 `true` 继续潜伏在当前完成态中。

最小运行态只读叶示例：

```json
{
  "id": "mainui.role.example.state",
  "status": "done",
  "applicable_gates": ["runtime_state"],
  "gates": { "runtime_state": true },
  "gate_runs": { "runtime_state": "web-20260808-01" },
  "gate_evidence": { "runtime_state": [{ "path": "output/.../state.json", "sha256": "..." }] },
  "state_evidence": [{ "path": "output/.../state.json", "sha256": "..." }]
}
```

节点类型/风险的最低闸：

| 类型或风险 | 最低闸 |
|---|---|
| `read` | `runtime_state` |
| `navigation` | `click/result/target_identity/timing` |
| `return` | `click/return_chain` |
| `tab` | `click/result/runtime_state` |
| `reversible-write` | `click/result/immediate/reopen/restore` |
| `transaction` | `click/result/protocol/immediate/reopen` |
| `destructive-write` 类型 | 上述事务闸 + `authorization` |
| 任意 `risk=destructive-write` | 额外要求 `authorization` |
| 任意 `risk=reversible-write` | 额外要求 `restore` |

`applicable_gates=[]` 永远不能完成 schema 5/6 叶子。schema 6 不靠“从默认列表删掉若干项”表达完成，而是由节点类型最低闸 + 本叶实际附加闸共同定义；每个声明适用的闸都必须有本批显式证据。

## 6. 专项证据合同

| 闸 | schema 6 结构要求 |
|---|---|
| `timing` | `timing.cold/warm` 分别记录 `first_visible_ms` 与 `interactive_ready_ms`，后者不得早于前者。 |
| `visual_version` | `version_evidence[]` 必须是带 SHA-256 的构建、Player、catalog 或版本报告；不能只写版本号字符串。 |
| `visual_match` | `visual_evidence.old/unity/overlay/diff` 四份带哈希文件及正数 viewport；运行批次必须是 `real-web`。 |
| `target_identity` | `identity_evidence[]` 每项含带哈希 artifact，`checks.view_type/root_size/background_rendered/close_chain` 全真。 |
| `layout_structure` | `layout_evidence[]` 的 `checks.scroll_rect/viewport_mask/content_layout/content_fitter` 全真。 |
| `scroll_interaction` | `interaction_evidence[]` 记录真实 `raycast_drag=true`、非零 `content_delta`、`last_item_reached=true`。 |
| `page_space_geometry` | `geometry_evidence[]` 记录页面根空间 expected/actual 四元矩形与 tolerance，超差直接失败。 |
| `runtime_state` | `state_evidence[]` 使用带哈希文件或有来源/时间/范围的人工断言。 |
| `model_presentation` | old/unity 两份证据；存在、资源/部件、非镜像、非翻转、角度、位置比例、常驻特效八项 checks 全真。 |
| `render_completion` | `render_evidence[]` 含带哈希 artifact、`render_completed=true` 和正数 `nontransparent_pixels`；RawImage/Renderer 存在不算。 |
| `resource_stable` | 三份预检/运行差异文件；第二次 `imported=0/configured=0`，玩家点击后 `added=0`。 |
| `shared_component_identity` | 记录共享资产/GUID/实例链、全部直接消费者、使用形态分组及每组代表样本；消费者必须恰好落入一个分组，不能漏组、跨组重复或夹带未声明宿主；根/生命周期变化时必须含高频页面。 |
| `component_state_matrix` | 每个适用状态必须有 `result=pass` 和证据；不适用状态写原因，至少有一项适用。 |
| `authorization` | `authorization_evidence[]` 必须明确本轮账号、操作与可消耗范围，历史授权不得复用。 |

### 动态特效合同

`effect_evidence[]` 不再接受单张图片路径。每个效果对象必须包含：

- `owner`：页面 / 共享槽 / 模型骨骼归属；
- `legacy_call`：`effect_name/parent/position/scale/rotation/loop/render_size`；
- `driver`：动画属性、材质属性、shader 分支与 `consumed=true`；
- `render`：同一 Handle 的 frame A/B、递增时间、正数像素差、非透明像素和正数 alpha 包围盒；
- `lifecycle.hide/reopen=true`；
- 若 `scroll_viewport=true`，再提供 full/partial/hidden 三态带哈希图和 alpha 像素，hidden 必须为 0。

因此 `Animation.isPlaying`、Handle 存在、单帧有几个亮点、物品图标已被 Mask 裁掉，都不能让动态特效通过。

## 7. 页面清单与真实 Web 收口

完成的 `page` 除精确 `control_inventory[]` 外，还需要：

```json
{
  "inventory_evidence": {
    "legacy_runtime": { "path": "...", "sha256": "..." },
    "legacy_source": { "path": "...", "sha256": "..." },
    "unity_source": { "path": "...", "sha256": "..." },
    "reconciled": true
  }
}
```

这三份证据分别回答“运行时出现什么”“老端源码/配置还可能出现什么”“Unity Prefab/Bind 实际有哪些”，防止只看一张首屏漏掉条件控件。根页只有在全部直接/递归子节点 `done` 且 `route_run_id` 指向同批真实 Web 运行时才可完成。

## 8. 新证据回卷

用户截图或真实运行结果推翻旧结论时，用 `invalidate_gates` 精确作废受影响闸：

```json
{
  "id": "mainui.role.example.effect",
  "status": "defect",
  "invalidate_gates": ["effect_match", "render_completion"],
  "invalidation_reason": "新截图显示流光静止且滚出后残留",
  "observed_at": "2026-08-08T00:40:00+08:00"
}
```

`apply` 会把旧 run/evidence 移入 `evidence_history`，将这些闸置 false，并回卷父状态。若一个已完成叶直接降级但没有列 `invalidate_gates`，schema 6 默认作废该叶全部适用闸，避免旧绿灯残留。重新完成时必须在结果文件中显式提交全部适用闸的新绑定。

## 9. 历史兼容与当前边界

- schema 2～4：按历史闸口继续可读；不会被自动解释成 schema 6 完成。
- schema 5：保留共享组件两闸，但 `done` 叶不得使用空 `applicable_gates`。
- 2026-08-07 共鸣 458 节点 `route-ledger.json` 是 schema 4 历史快照。它仍可表达当时的 `done/blocked/needs-runtime-verify` 边界，但不得继续由历史更新器写入；下一次真实复验应从 `route-manifest.json` 用 `init` 建 schema 6 新账。
- 2026-08-04 挂机“提升”扫光旧台账曾只有树、没有任何状态，现已补成可校验的 schema 4 历史账；Editor 双帧/生命周期叶保留完成，真实 Web/资源幂等叶明确为 `needs-runtime-verify`，没有把旧 Editor 结果扩大成整页完成。
