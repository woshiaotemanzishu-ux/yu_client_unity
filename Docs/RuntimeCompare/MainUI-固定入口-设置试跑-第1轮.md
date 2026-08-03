# 主界面固定入口巡检：设置试跑第 1 轮

> 日期：2026-08-03
> 路由：主界面固定设置按钮 → 设置全量功能树（当前重开）
> 目的：验证“无人参与地逐项点击、对照、修复、复验”是否能形成可重复闭环。

## 1. 试跑结论（2026-08-03 用户复查后更正）

本轮不是静态审查。老客户端和线上 Unity WebGL 使用同一测试账号真实登录，并由 Browser MCP 沿玩家路径点击。当时只建立了设置页及一层子功能基线，用户复查已证明该粒度不足；Unity 修复前在主界面即可稳定复现“齿轮可见但点击无响应”，同时源码对照发现“更换头像”错误打开设置内占位子窗，而老端实际打开完整时装页头像页签。

本轮只修复了主界面点击面和“更换头像”的路由类型，没有重转或覆盖 `HudChatBar.prefab`、`SettingModule.prefab`。主入口点击已通过真实 Prefab 射线用例；但用户后续完整执行叶子功能后又发现两个实质缺陷，因此“设置路线”整体状态重新改为 `defect`，不是已完成：

1. 改名提交成功后，当前已打开的设置面板没有立即更新角色名，必须退出后才刷新。
2. 更换头像虽能跳转，但至少加载 5 秒，且 Unity 打开的仍是城楼背景、旧三页签的旧版页面；当前老端已是角色居中、分类库格的新版时装/头像页。

上一轮的验收错误是：改名只跑到“弹窗打开”，没有执行完事务并检查父页即时状态；头像只跑到“能跳转”，没有检查目标页版本、子功能和冷/热耗时。这证明“入口 + 一层子功能”不是足够的完成定义。

## 2. 老端点击基线

| 步骤 | 老端真实行为 | 状态处理 |
|---|---|---|
| 主界面设置齿轮 | 打开 `SettingView` 基础设置页 | 只读 |
| 屏蔽列表页签 | 显示两列 10 项 | 只读 |
| 第一项屏蔽开关 | 立即切换；再次点击恢复原值 | 已恢复 |
| 复制 ID | 系统剪贴板得到 `4294967524` | 只读 |
| 更换头像 | 关闭设置，打开完整“时装”页并选中“头像”页签 | 只读，按返回链回主界面 |
| 修改名字 | 发 42602，回包后打开“角色名修改”；当前账号显示首次免费 | 未提交改名 |
| 切换角色/账号、还原默认、脱离卡死、修复异常 | 均进入各自二次确认框 | 只验到确认并取消/关闭 |

老端源码进一步确认 `SettingView.ts` 的更换头像链为 `OpenFun.OpenFunHandler(203, [DressType.Head])`，与运行态一致。

## 3. Unity 修复前复现

线上 WebGL 登录并进入场景后，主界面底部设置齿轮可见。多次在齿轮可见区域点击，设置页均未打开；对应浏览器日志没有 `MainUIRouter` 或 `Setting` 路由记录。用同一输入通道点击其他画布区域能够打开队伍面板，排除 Browser MCP 输入整体失效。

根因位于 `MainUIChatView.WireHudEntries()`：界面注释和实际可见节点是 `_img_setting/_img_friend/_img_shop`，代码却只给外层 `_box_setting/_box_friend/_box_shop` 动态补点击。WebGL 中可见子图先命中时，外层动态点击面不能保证收到回调。

第二个差异由运行态与源码共同确认：Unity `SettingView` 原先把“更换头像”接到 `SettingChangeHeadView` 占位子窗，目标页面语义与老端不同。

## 4. 增量修复

1. 主界面设置、好友、商城三个同结构固定入口改为直接在可见 `_img_*` 上绑定路由。
2. “更换头像”改为先关闭设置，再调用 `FashionFlow.Open(1)` 进入完整时装页的“头像”页签；底层仍使用头饰位 `posId=3`。
3. 扩展 `MainUIChatHudCase`：加载真实 `HudChatBar.prefab`，要求可见图片自身拥有 `Button`，再通过 `GraphicRaycaster→PointerClick` 分别点击设置/好友/商城并验证路由各触发一次。
4. 新增项目 Skill `.agents/skills/audit-game-ui-route`，并在用户复查后升级为“先枚举全量功能树，再选一条分支深度优先跑到叶子”；事务必查即时刷新/重开，跳转必查版本身份和冷/热耗时。

## 5. 证据

取证目录：`output/mainui_fixed_settings_pilot/`

- 老端：`old_setting_base_1280x720.jpg`、`old_setting_shield_1280x720.jpg`、`old_setting_change_head_1280x720.jpg`、`old_setting_rename_1280x720.jpg` 及五个底部确认框截图。
- Unity 修复前：`unity_setting_lower_1280x720.jpg`、`unity_setting_entry_click_no_open_1280x720.jpg`、`unity_setting_entry_logs.json`。
- 线上回滚确认：`unity_web_rollback_confirmed_login_1280x720.jpg`；Unity CLI 日志：隔离工作树 `Temp/mainui_settings_pilot_cli_final.log`。
- 用户复查版本对比：`user_recheck_old_fashion_current.png`、`user_recheck_unity_fashion_legacy.png`，明确显示老端当前新版分类库格与 Unity 城楼背景旧页不是同一版页面。

## 6. 验证记录

| 层级 | 结果 | 证据/备注 |
|---|---|---|
| C# 编译 | 通过 | `dotnet build Assembly-CSharp-Editor.csproj -m:1`：0 error，79 条既有 warning |
| Unity CLI 真实 Prefab 点击 | 通过 | `CliVerify.MainUIChatHud`：设置/好友/商城均由可见 `Image` 经 `GraphicRaycaster→PointerClick` 各路由 1 次，聊天滚动回归同时通过 |
| WebGL 壳构建 | 构建成功，未验收 | 隔离工作树加 `-buildTarget WebGL` 后 Release+gzip 壳成功 |
| 部署后运行态 | 未通过，已回滚 | 新工作树首次只打壳生成了不完整的本地 Addressables catalog，启动报 `prefabs/ui/login/loginstage` 缺 key；尝试拼接旧本地 AA 也未在验收时限内完成启动，不作通过处理 |
| 线上安全恢复 | 通过 | 恢复原稳定 WebGL 壳，远端 wasm/catalog 哈希匹配，Browser MCP 冷启后重新到达登录页 |

因此，本轮只能确认“无需占用用户电脑的老端取证 → Unity 真实 Prefab 点击回归 → 增量修复”工具链可行，不能确认设置功能线已闭环。该修复也尚未获得新壳的 Web 运行态通过标记。

## 7. 下一层清单

下一步不跳到好友或商城。先继续设置功能树：

1. 先闭环 `settings.rename`：提交前/回包/当前设置页立即刷新/关闭重开。
2. 再闭环 `settings.change-avatar`：冷/热耗时、当前老端新版页身份、全部子页签/库格/选择与即时刷新。
3. 再依次跑四滑条、自动拾取、坐骑/降神/自动任务、10 个屏蔽项和底部五个操作。

设置父节点只有在所有应验叶子完成后才能收口，然后才转向好友、商城、聊天、人物/背包等主界面常驻入口。
