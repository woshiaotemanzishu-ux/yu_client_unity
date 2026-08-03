# 天穹战歌 UI 路由巡检运行手册

## 当前入口

- 老客户端工具目录：`E:\GitProject\yu_client\tools\yu-resource-tool`
- 启动命令：`npm run dev`
- 启动预览：向 `http://127.0.0.1:7074/api/preview/start` 发送空 JSON 对象
- 老端页面：`http://127.0.0.1:8091/index.html`
- Unity WebGL：先从 `Docs/打包发布手册.md` 确认；2026-08-03 试跑地址为 `http://223.109.142.26:89/web/`
- 测试账号：`111111` / `111111`

启动命令要放在隐藏后台进程中。结束本轮时只停止本轮启动且已核实 PID 的老端工具进程，不扩大到其他 Node/Unity 进程。

## Canvas 坐标

每次点击前在浏览器中读取 canvas 矩形：

```javascript
const canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
const rect = canvas.getBoundingClientRect();
({ x: rect.x, y: rect.y, width: rect.width, height: rect.height, scrollY: window.scrollY });
```

Unity 设计分辨率是 `720×1280`。设计坐标到浏览器视口坐标：

```text
screenX = rect.x + designX * rect.width / 720
screenY = rect.y + designY * rect.height / 1280
```

老端外层页面也可能缩放并居中竖屏画布，必须读取实际画布矩形，不固定假设 `1280×720` 截图中的偏移。页面自动滚动后重新读取矩形。

## 证据命名

建议目录：`output/ui_route_audit/YYYY-MM-DD_<route>/`。

- `old_00_entry.jpg`、`old_01_target.jpg`
- `unity_before_00_entry.jpg`、`unity_before_01_failure.jpg`
- `unity_after_00_entry.jpg`、`unity_after_01_target.jpg`
- `old_console.json`、`unity_before_console.json`、`unity_after_console.json`
- `route_matrix.md`

截图保持同一浏览器窗口尺寸；视觉比较时另外保存裁剪到游戏画布的图，不覆盖原始全窗口证据。

## 深度优先执行顺序

1. 打开父页，先列出当前所有可见和条件显示的同层控件，不立即判断“基本完善”。
2. 选一个子节点，把它展开到最终业务结果。有子页就在子页再列全量同层清单，然后继续选一条向下。
3. 对事务执行“提交前 → 等待中 → 成功/失败 → 父页即时刷新 → 关闭重开”。
4. 对跳转执行“点击 → 首屏可见 → 可交互就绪 → 目标页版本核对 → 冷/热再打开”。
5. 只有这个叶子的所有阶段通过才标 `done`，然后回到父页选下一个兄弟节点。

默认记录 `click→first-visible` 和 `click→interactive-ready`。首次打开为 cold，返回后立即再打为 warm。项目无明确阈值时，超过老端 2 倍或 2 秒是默认告警线；5 秒以上必须登记缺陷并拆出资源、配置、协议、建树和串行等待耗时。

## 安全点击边界

- 只读：页签、展开、返回、复制、打开弹窗，可直接完整验收。
- 可恢复写入：音量、屏蔽、自动拾取等。先记录原值，改变一次，确认回包/刷新，再改回原值。
- 破坏性：删号、消费、付费、发奖、退出账号、切换角色、最终修复/重载。默认只验收到二次确认框并取消；需要执行时必须有本轮明确授权。

## 设置路线的重开结论

老端设置固定入口应打开 `SettingView`。基础页包括角色信息、复制 ID、更换头像、改名、四个滑条、自动拾取和底部五个操作；屏蔽页有十个选项。更换头像的路由语义是 `OpenFun(203, [DressType.Head])`，但“能跳转”不等于该叶子完成。

Unity 主界面固定入口必须绑定到 `_img_setting/_img_friend/_img_shop` 这些可见 `Graphic`，并以真实射线点击回归；只绑定外层 `_box_*` 会出现“图标可见但 WebGL 点击无响应”的风险。

2026-08-03 用户复查后，设置路线重新打开：

- 改名事务在提交后设置父面板没有立即刷新角色名，需退出后才更新。上一轮只验到改名弹窗，不能标完成。
- 更换头像能跳转，但加载超过 5 秒，且 Unity 目标页仍是旧版布局；当前老端是角色居中、分类库格的新版时装/头像页，Unity 则仍是城楼背景与旧三页签结构。这是性能+整页版本漂移，不是单纯路由成功。
- 因此 `settings.rename` 和 `settings.change-avatar` 均为 `defect`，设置父路线不是 `done`。下次必须先按 [route-ledger-schema.md](route-ledger-schema.md) 继续这两条线，不跳去下一个主界面入口。

## 两级验收与 Web 构建边界

- 快速循环：Browser MCP 取老端运行事实，Unity CLI 加载真实 Prefab，用 `GraphicRaycaster→PointerClick` 验证点击面和路由。当前没有可用 Unity 专用 MCP 时，这是不占用用户电脑的默认通道。
- 批次收口：累计多条路由后再打 WebGL，重新登录并复走。不要让 Web 冷启和打包时间阻塞每一项小修复。
- 新建隔离工作树没有与当前内容匹配的 Addressables 构建状态时，禁止直接用 `BuildWebShellOnlyCli` 发布。2026-08-03 试跑已复现本地 catalog 缺 `prefabs/ui/login/loginstage` 导致新壳无法启动。使用持续构建工作区的已验证内容状态，或完整重打并成套发布内容+壳。
- 禁止把旧壳的 `StreamingAssets/aa` 手工拼到新 player 当成可验收产物；两者没有成套验证时只能作排障，不能发布。
