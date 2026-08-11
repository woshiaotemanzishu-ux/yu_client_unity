# 角色 → 外观 → 时装（FashionMain pos=1）精修与验收（2026-08-11）

## 范围与结论

本轮只处理 `FashionMain pos=1`。人物、发饰、装扮、套装、称号、技能、Dress、背包、灵宠、锻造和通灵均未进入；已有 `FashionModule.prefab` 只做 `fix-view` 增量修复，没有重转、Creator 覆盖或新建 Library。

schema 6 路线 `mainui.role.fashion-main-pos1.v1` 共 78 个节点，最终为 73 个叶子 `done`、4 个叶子 `blocked`，根页按子状态自动回卷为 `blocked`。四个未完成叶子不是实现缺口：

- `activate-upgrade`：未获消耗型事务授权，禁发 `41301/41304/41306/41316`。
- `wear-toggle`：未获可恢复写授权，禁发 `41302/41303`。
- `level-submit`：未获消耗型事务授权，禁发 `41305`。
- `sound-result`：结果音只能在上述事务的权威成功回包后播放，不能在只读验收中伪播。

正式路线文件为 `Tools/UIAudit/routes/role/fashion-main-pos1.json`，拓扑 manifest、schema 6 ledger、批次 results 和资源清单使用同名前缀保存。ledger 经通用 `route_ledger.py apply/validate` 原子更新，状态为 `{'blocked': 5, 'done': 73}`。

## 可见修复与运行语义

1. 时装列表项建立逐项 `FashionId` 身份和唯一点击面，真实横向拖动后的释放不会再误触点击、播放点击音或发送 `41312`；真正点击“甜心宝贝”仍精确发送 `41312 ci(1,12010008)`。
2. 22 张 pos=1 物品图标以当前老 H5 实际加载根 `yu_client/cdn/resource/game/goodsIcon` 为权威覆盖 Unity，保留原 `.meta` 和 GUID。旧的 `yu_client/cdn/assets/resource/game/goodsIcon` 与当前 Canvas 全部不符，不得再作为该页同步源。
3. `BaseAwardItem` 共享根未修改；只在 FashionMain 消耗格的 Prefab 嵌套实例保存消费者缩放 `0.6`。真实绘制足迹由 90.46×90.46 修为 84×84，与阶数/未激活标签间距由 1.77px 恢复为 8px；已拥有 `[1阶]` 和未拥有 `[未激活]` 两态均覆盖。
4. 属性名和值使用 Prefab 内纯文字行布局和 8px 间距，运行时代码不再写页面专用坐标。
5. 模型预览使用真实服装贴图、武器、朝向和常驻特效；冷开、颜色切换、关闭重开均清除旧 RT 并幂等重建。Editor 读取 RT 得到 41532 个非透明像素。
6. 时装横向列表满足 `ScrollRect → Viewport(RectMask2D) → Content(HorizontalLayoutGroup + ContentSizeFitter)`；真实 `GraphicRaycaster` 拖动后 Content 位移、裁剪和末项点击通过。等级材料列表满足 `GridLayoutGroup + ContentSizeFitter`。
7. 当前 Tab 的标题、背景和固定资源进入小闭包预热；cold/warm、关闭重开、两条等级弹窗关闭链、Fashion 返回链、点击音与只读事务边界均完成。

## 最终真实运行证据

最终账号为 `111111`，顺序严格为老 H5 后 Unity Web，二者均覆盖 `720×1280` 与 `1920×1080`：

- 老 H5：`output/ui_route_audit/2026-08-11_fashion_main_pos1/old_h5_final_20260811T213700+0800`。真实 Canvas、双档 viewport、cold/warm、列表/属性拖动、甜心宝贝、等级弹窗和返回链均有不可变截图。
- Unity Web：`output/ui_route_audit/2026-08-11_fashion_main_pos1/unity_web_final_after_old_20260811T214000+0800`。`headless-report.json` 为 14/14 assertions、0 failure、无未授权 Fashion 写协议。
- 双档对比：`output/ui_route_audit/2026-08-11_fashion_main_pos1/compare_sequence_final_20260811T214100+0800`，保存两档 old/unity/overlay/diff 和统一报告。3D 模型与常驻特效独立推进帧，整帧指标只作诊断；静态 UI 和页面几何已人工检查 overlay。
- Editor 组合页：`output/ui_route_audit/2026-08-11_fashion_main_pos1/cli_structure_final_20260811T214400+0800`，逐项身份、状态矩阵、真实拖动/末项、41312、模型 RT、颜色、材料结构、弹窗关闭与返回链全部 PASS。

最终同批构建指纹：

- `WebGL.wasm.gz`: `97C9705F67C9E652AF62B41F88FA69535B70CCD2F21A1E2408455CE1CDFC7B27`
- `catalog.bin/catalog_live.bin`: `321644FB7CB8A83652D56A37D759CB373603E280721E4691B231A485F220F335`
- 内容构建：2950 文件、1590 MB；官方 shell-only 构建成功。
- `dotnet build Shenxiao.Editor.csproj`: 0 error，118 个项目既有 warning。

## 时间与偏差

- `page_production_time`：约 5 小时 30 分钟。
- `uiaudit_rnd_time`：约 2 分钟；只用于确认公共 provider 的既知 `RESOURCE_TOOL_PREVIEW_STALE_STATE`，未修改公共 UIDAudit 实现。

超过 4 小时目标的主要原因是最终真实 Web 才揭出两项静态检查无法发现的玩家可见差异：22 张图标使用了过期资源根，以及共享物品格的实际子图足迹覆盖 `[未激活]`。随后补做了内容+壳同批构建、old→Unity 双档顺序重采和 schema 6 哈希绑定；未把工具研发时间计入页面生产。

若要把根页从 `blocked` 收口为 `done`，下一批必须取得明确事务授权或使用可恢复专用测试号，按真实 UI 完成激活/进阶、穿脱、等级提交的成功回包、即时刷新、关闭重开、恢复和结果音验证；不得用 GM 直接写最终态替代。
