# Compete 静态校验记录

时间：2026-08-09（Asia/Shanghai）

## 结果

- `validate_static.py`：PASS。
  - manifest 节点 49，叶节点 40。
  - ledger 状态：`baseline-only=5`、`blocked=24`、`defect=20`。
  - CompeteController、CompeteModel、CompetelistModule.prefab 的起始 SHA-256 未变化。
  - Prefab 组件计数与审计记录一致。
  - 六份 race-act Unity 配置仍为缺失状态。
  - NVR 矩阵全部保持 `needs-runtime-verify/blocked`，没有伪造运行证据。
- `route_ledger.py validate revision-v3/route-ledger.json`：PASS，schema 6 拓扑、状态与 manifest SHA 绑定有效。
- 五份 JSON 由 PowerShell `ConvertFrom-Json` 全部解析成功。
- `git diff --check -- Assets/Scripts/Module/Core/Compete Assets/Prefabs/UI/Competelist Assets/GameRes/resource/game/competelist`：PASS（无输出）。
- 目标范围最终 `git status --short`：仅 `?? output/ui_route_audit/2026-08-09_compete_static_v1/`；生产源码、Prefab、专属资源仍无改动。

## 明确未运行

- 未运行任何 build 或编译；主控要求合并编译只由主控串行执行。
- 未启动/操作 Unity、Web、浏览器、Computer Use。
- 未执行 33803/33804、充值/兑换/购买、GM 或其它账号写事务。
- 未生成 Player/catalog、双 viewport、old/unity/overlay/diff、滚动实拖、模型/特效双帧、cold/warm、即时刷新/重开证据。
