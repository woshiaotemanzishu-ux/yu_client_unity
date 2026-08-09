# Static validation

Recorded at: `2026-08-09T16:06:24+08:00`

## Passed

- `python .agents/skills/audit-game-ui-route/scripts/route_ledger.py validate output/ui_route_audit/2026-08-09_parallel_ui_refine/firstrecharge/route-ledger.json`
  - `route=mainui.first-recharge schema=6 nodes=63 status={'blocked': 18, 'defect': 40, 'needs-runtime-verify': 5}`
- Parsed every JSON artifact with PowerShell `ConvertFrom-Json`.
- Asserted manifest and ledger both contain 63 nodes.
- Asserted there are no `done` or `not-run` nodes.
- Asserted every `blocked` node has a non-empty `blocked_reason`.
- Asserted every `needs-runtime-verify` node has a non-empty `runtime_gap`.
- `git diff --name-only -- Assets/Scripts/Module/Core/FirstRecharge` returned empty.
- Recomputed both business-source SHA-256 values; they still match the starting hashes.
- `git diff --check -- Assets/Scripts/Module/Core/FirstRecharge output/ui_route_audit/2026-08-09_parallel_ui_refine/firstrecharge` passed.

## Deliberately not run

- No Unity launch or operation.
- No browser or Computer Use.
- No build or compile; the four-route parent controller owns the one serial build.
- No recharge, claim, consume, GM, or other account write.
- No real-Web, dual-viewport, pixel diff, scroll, model/effect two-frame, cold/warm, immediate-refresh, or reopen test.

Those omissions are represented as `blocked`, `defect`, or `needs-runtime-verify`; none is reported as `done`.
