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
- `old_<state>_canvas.png`、`unity_<state>_canvas.png`、`diff_<state>.png`
- `route_matrix.md`

截图保持同一浏览器窗口尺寸；视觉比较时另外保存裁剪到游戏画布的图，不覆盖原始全窗口证据。

2D UI 以画布裁剪图做半透明叠加和差异图；模型区先比较模型是否存在、资源/部件、朝向、镜像、翻转、角度、位置、比例和特效。Unity 与 Laya 的 Shader、抗锯齿和动画采样不要求逐像素值相同，但不得用这一点豁免模型缺失或明显构图错误。

## 深度优先执行顺序

1. 打开父页，先列出当前所有可见和条件显示的同层控件，不立即判断“基本完善”。每个控件都要写入页面 `control_inventory[]` 并映射到直接子节点；页签点击通过不能覆盖页签内部按钮。
2. 选一个子节点，把它展开到最终业务结果。有子页就在子页再列全量同层清单，然后继续选一条向下。
3. 对事务执行“提交前 → 等待中 → 成功/失败 → 父页即时刷新 → 关闭重开”。
4. 对跳转执行“点击 → 首屏可见 → 可交互就绪 → 目标页版本核对 → 冷/热再打开”。
5. 只有这个叶子的所有阶段通过才标 `done`，然后回到父页选下一个兄弟节点。
6. 对列表先验容器树，再从可见子项经 `GraphicRaycaster` 做真实拖动；只看横向排列和子项数量不能证明可滚动、可裁剪或末项可达。
7. 对弹窗记录“触发格 → 目标 View 类型 → 主底图 Sprite → 根尺寸 → 遮罩关闭”的身份链。列表中每个可见格都逐个点击，不能由同排一个成功项代验。
8. 关键位置统一输出页面根左上角矩形；局部 `anchoredPosition` 只用于解释锚点，不作为跨容器视觉结论。

默认记录 `click→first-visible` 和 `click→interactive-ready`。首次打开为 cold，返回后立即再打为 warm。项目无明确阈值时，超过老端 2 倍或 2 秒是默认告警线；5 秒以上必须登记缺陷并拆出资源、配置、协议、建树和串行等待耗时。

## 安全点击边界

- 只读：页签、展开、返回、复制、打开弹窗，可直接完整验收。
- 可恢复写入：音量、屏蔽、自动拾取等。先记录原值，改变一次，确认回包/刷新，再改回原值。
- 破坏性：删号、消费、付费、发奖、退出账号、切换角色、最终修复/重载。默认只验收到二次确认框并取消；需要执行时必须有本轮明确授权。

## 设置路线的当前结论

老端设置固定入口应打开 `SettingView`。基础页包括角色信息、复制 ID、更换头像、改名、四个滑条、自动拾取和底部五个操作；屏蔽页有十个选项。更换头像的路由语义是 `OpenFun(203, [DressType.Head])`，但“能跳转”不等于该叶子完成。

Unity 主界面固定入口必须绑定到 `_img_setting/_img_friend/_img_shop` 这些可见 `Graphic`，并以真实射线点击回归；只绑定外层 `_box_*` 会出现“图标可见但 WebGL 点击无响应”的风险。

2026-08-03 用户复查后，设置路线曾因改名即时刷新、头像误路由和 5 秒以上冷开重新打开。第 2 轮已完成全部功能叶子；2026-08-04 又因像素视觉、模型和运行态状态证据不足再次重开。当前经验是：

- 功能、协议和即时刷新通过，不代表视觉与运行态通过；用户的新截图可以直接回卷历史 `done`。
- 时装/发饰要逐条点击基础色和非基础色，核对实际材质名与模型；套装要逐一点击四页，不能只看第一页。
- `UIModelStage` 换模型时必须先失活旧实例再延迟销毁，否则非 PlayMode 截图或同帧 `RenderNow` 会把上一页翅膀/武器累积进下一页。
- 时装资源预检必须覆盖 `config_fashion_color.active_cost/star_cost`，并与全局 `SpriteImporter` 的材质例外一致。连续第二次预检必须 `imported=0、configured=0`，玩家点击后 `addedResources=none`。
- 当前第 3 轮实测：预检闭包 712 项，第二次 `imported=0、configured=0`；设置→头像首开 1867ms、热开 71ms；四套装模型、挂载部件、常驻特效和战力增量条已按老端截图复验。
- 冷开头像还要在约350ms/1000ms留证，禁止用粉色占位图或固定延时冒充ready。套装页除四个预览页签外，还必须点“更换→确认”，验证41302及父页即时切到“已更换”。
- 第 4 轮补充：`_list_fashion_item` 曾绑定到无 Viewport/Mask 的重叠横排节点，截图看似有列表但不能拖动；套装 `Image_130` 曾在父容器 x=599 下继续右锚 x=-45，最终页面坐标右偏 65；四个条件格曾继承 `BaseAwardItem` 默认点击，误开通用小物品窗。以后这三类问题分别由 `layout_structure+scroll_interaction`、`page_space_geometry`、`target_identity` 阻断。
- `IllusionTips.scene` 的 `_img_bg` 本来就没有静态 skin，老端按 `goods.color` 运行时加载 `common4/other/ui_tips_pzbg_1..7`。Unity 验收必须等待 Sprite 实际绘制，并把七张背景纳入点击前资源闭包；只断言 426×772 尺寸会让“底图透明、文字浮在父页上”假通过。
- `UIModelStage` 的 RawImage 拿到 RenderTexture、场景中存在 Renderer 都不等于已经出帧；必须在专用相机实际 `Render()` 完成后置 ready，并读取 RT 非透明像素形成 `render_evidence[]`。固定延时不能替代实际出帧探针。
- 详情弹窗需同时记录配置字段语义、详情/来源等动态组矩形、preferred height 与背景包围盒；组间重叠或字段放错容器即回卷。最终截图使用新的不可变目录，不覆盖已被查看器映射的 PNG。

## 两级验收与 Web 构建边界

- 快速循环：Browser MCP 取老端运行事实，Unity CLI 加载真实 Prefab，用 `GraphicRaycaster→PointerClick` 验证点击面和路由。当前没有可用 Unity 专用 MCP 时，这是不占用用户电脑的默认通道。
- 批次收口：累计多条路由后再打 WebGL，重新登录并复走。不要让 Web 冷启和打包时间阻塞每一项小修复。
- 新建隔离工作树没有与当前内容匹配的 Addressables 构建状态时，禁止直接用 `BuildWebShellOnlyCli` 发布。2026-08-03 试跑已复现本地 catalog 缺 `prefabs/ui/login/loginstage` 导致新壳无法启动。使用持续构建工作区的已验证内容状态，或完整重打并成套发布内容+壳。
- 禁止把旧壳的 `StreamingAssets/aa` 手工拼到新 player 当成可验收产物；两者没有成套验证时只能作排障，不能发布。
