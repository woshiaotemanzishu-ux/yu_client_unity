# Web foundation verification summary（2026-08-04）

## Unity 门禁

- `CliVerify.SettingPk`：`creator=True pkCfg=True modalFitter=True modalRuntime=True ... pass=True`。三个设置遮罩的 Prefab 组件与运行态根 Canvas 四角全部通过。
- `CliVerify.SceneMixDriver`：`CLIVERIFY mixdriver ALL PASS`。首屏只阻塞 `idle/run` 后，`run/attack` 模型切换、特效宿主与 Unlit 表面仍正确。
- `CliVerify.LoadingView`：`binding=True ... stageEstimate=True estimateText='绘制首屏地图 95.00% · 预计约 5 秒' ... pass=True`。

## 构建与产物

- 完整 `BuildAllWebCli`：内容构建成功，`ServerData/WebGL` 为 2556 个文件、约 1289 MB、2545 个 bundle；Web 壳构建成功，`Builds/WebGL` 约 42 MB。
- 最后只修改 Player ETA 后，在同一持续工作区执行 `BuildWebShellOnlyCli`，结果 `Build Finished, Result: Success`；内容 catalog 哈希保持不变。
- `WebGL.wasm.gz` SHA256：`E5EF5CFE7FADCCB5ABF97CECC0B1A4AF3BA1B6D25797FB35C8AF4404105D21EA`。
- `catalog_live.bin` SHA256：`4A8F36A7C048F755FA7203F55632A56739260E3AD267815C3579B3E4E2D8C824`。
- `catalog_live.hash` SHA256：`BF872267A9F7DD835312D73C5A7A73D46D85BDC144AB48246B0E3028F3556412`。

## 真实浏览器

- 1280×720 Canvas 铺满可视区；移动端 720×1280 主体仍居中。
- 角色选择阻塞资源由旧包 `83项/279.5MB` 降为 `34项/31.7MB`，首次样本约 6.9 秒内到角色页，暖路径 5 秒内到角色页。
- 主场景进入样本约 16.1 秒，暖路径约 13.5 秒；主要剩余时间在地图首帧绘制，不是重复下载。过场连续 6 帧均为完整场景，没有纯黑帧或加载页回跳。
- 最终 95% 阶段真实显示“绘制首屏地图 95.00% · 预计约 5 秒”。
- 主设置与改名弹窗的半透明遮罩覆盖 1280×720 左右扩展区。
- 第二次整页刷新约 0.9 秒进入 HTML 引擎进度，截图调用上界约 6.9 秒到账号页。
- SimCdn 的 `WebGL.wasm.gz` 首次 HEAD 为 200，携带同一 `ETag` 的 `If-None-Match` 条件请求返回 304。
