# Attention 静态验证记录

## 文件岛与初始状态

- 路线开始前：
  - `git status --short --untracked-files=all -- Assets/Scripts/Module/Core/Attention Assets/Prefabs/UI/Attention` 无输出。
  - `git diff --name-status -- Assets/Scripts/Module/Core/Attention Assets/Prefabs/UI/Attention` 无输出。
  - 目标代码与 Prefab 起始 clean；本轮没有修改它们。
- 最终目标状态：只有 `output/ui_route_audit/2026-08-09_attention_static_v1/` 新文件；Attention 代码/Prefab 仍无 diff。

## 三方静态断言

- 老端源码存在两套页面：`AttentionViewLaya`（通用渠道页）与 `AttentionView`（SDK/领奖页）。
- Prefab 存在两套对应子树及非空生成 Bind；`_tpl_EquipmentItem` 和列出的 Bind 字段未发现 `fileID: 0`。
- `Assets/Scripts/Module/Core/Attention/` 未找到继承 `AttentionViewBind` 或 `AttentionViewLayaBind` 的业务 View。
- `Assets/Scripts` 对 `AttentionModule/AttentionViewLaya/AttentionView` 的命中仅有自动生成 Bind，没有运行时打开消费者。
- 老端 SDK 页的领奖分支明确发送 `33105,70,1,1`；因此本路线没有沿用“Attention 页面无协议”的错误概括。

## schema 6

- manifest SHA-256：`45ae1e04b96138f2f6f180952e14ab61ed3574283b6e9b070a41e2cf643f4f6a`
- ledger `manifest_source.sha256`：`45ae1e04b96138f2f6f180952e14ab61ed3574283b6e9b070a41e2cf643f4f6a`
- `route_ledger.py init`：通过，32 节点初始 `not-run`。
- `route_ledger.py apply`：通过，正式账由通用工具原子更新。
- `route_ledger.py validate`：通过：
  - `route=mainui.attention`
  - `schema=6`
  - `nodes=32`
  - `blocked=13`
  - `defect=18`
  - `needs-runtime-verify=1`
  - `done=0`

## 未执行门禁

按本轮明确边界，未执行 Unity、浏览器、Computer Use、真实账号、SDK subscribe、33105、GM、Core/Unity/WebGL build。视觉、两 viewport、点击、即时刷新、关闭重开、cold/warm、资源幂等与共享组件状态矩阵均没有被伪造为通过。
