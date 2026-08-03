# 主界面固定入口巡检：设置试跑第 1 轮

> 日期：2026-08-03
> 路由：主界面固定设置按钮 → 设置全量功能树（当前重开）
> 目的：验证“无人参与地逐项点击、对照、修复、复验”是否能形成可重复闭环。

## 1. 试跑结论（2026-08-03 第二次深挖后更正）

本轮不是静态审查。老客户端和线上 Unity WebGL 使用同一测试账号真实登录，并由 Browser MCP 沿玩家路径点击。当时只建立了设置页及一层子功能基线，用户复查已证明该粒度不足；Unity 修复前在主界面即可稳定复现“齿轮可见但点击无响应”，同时源码对照发现“更换头像”错误打开设置内占位子窗，而老端实际打开完整时装页头像页签。

第一轮只修复了主界面点击面和“更换头像”的路由类型，没有重转或覆盖 `HudChatBar.prefab`、`SettingModule.prefab`。用户复查后发现两个实质缺陷，因此当时把“设置路线”整体重新改为 `defect`：

1. 改名提交成功后，当前已打开的设置面板没有立即更新角色名，必须退出后才刷新。
2. 更换头像虽能跳转，但至少加载 5 秒；用户截图中 Unity 显示城楼背景，而老端显示角色居中、分类库格。

第二次沿路由和老端源码继续点完后，确认两张截图实际不是同一页：老端截图是外层“装扮”里的内层“头像”，Unity 截图是外层“发饰”；老端“发饰”本身也使用城楼背景。真正缺陷是 `FashionFlow.Open(1)` 把参数当外层索引，设置入口因此稳定落到“发饰”，不是整个 Fashion 界面版本过旧。

现已改为显式语义路由 `FashionFlow.OpenDress(DressType.Head)`，并补齐当前装扮页的气泡/相框/头像列表、预览、属性、材料、2/5/10 级技能圆标与技能详情。设置入口到这些只读叶子已通过真实射线闭环；装扮激活/升级/穿戴仍受 R540 协议硬约束，点击只提示且严格不发送 11201～11203。改名即时刷新也不属于本次分支，因此设置父路线仍不能标记整体完成。

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
2. “更换头像”改为先关闭设置，再调用 `FashionFlow.OpenDress(DressView.HeadType)`；外层固定四页签为时装/发饰/装扮/套装，设置入口落外层装扮、内层头像，避免再依赖易错的数字索引。
3. 在现有 `DressModule.prefab` 上增量补业务 Bind、四列列表布局、战力文字和稳定点击面；运行时 View 只填配置/状态，不重建视觉树。内层气泡/相框/头像均从 `config_dress_up_cfg` 生成，技能按同装扮的非零 `skill` 去重并复用 `CommonModule/SkillTipsView` 显示真实技能名、图标和说明。
4. 增加 `DressAssetPreflight`：从装扮、物品和时装模型配置计算 154 项实际资源闭包，运行前要求文件、`.meta` GUID 和 Addressables 条目齐全。新增 128 条定向 Addressables，既有 33812 条目没有删除或语义变化；禁止再靠首次点击即时导入资源。
5. 新增 `SettingFashionCurrentCase`：从真实设置按钮开始，通过 `GraphicRaycaster→PointerClick` 点击四个外层页签、三个内层页签、头像条目、技能详情及关闭按钮，并核验受限写按钮零发送、冷/热耗时和资源目录前后快照。
6. 扩展 `.agents/skills/audit-game-ui-route`：运行前必须按配置求资源闭包，巡检前后比较文件与 Git 状态；首次点击生成 PNG/.meta 一律算失败，不能当作“加载成功”。

## 5. 证据

取证目录：`output/mainui_fixed_settings_pilot/`

- 老端：`old_setting_base_1280x720.jpg`、`old_setting_shield_1280x720.jpg`、`old_setting_change_head_1280x720.jpg`、`old_setting_rename_1280x720.jpg` 及五个底部确认框截图。
- Unity 修复前：`unity_setting_lower_1280x720.jpg`、`unity_setting_entry_click_no_open_1280x720.jpg`、`unity_setting_entry_logs.json`。
- 线上回滚确认：`unity_web_rollback_confirmed_login_1280x720.jpg`；Unity CLI 日志：隔离工作树 `Temp/mainui_settings_pilot_cli_final.log`。
- 用户复查版本对比：`user_recheck_old_fashion_current.png`、`user_recheck_unity_fashion_legacy.png`，明确显示老端当前新版分类库格与 Unity 城楼背景旧页不是同一版页面。
- 第二次深度复验：`output/settings_fashion_current/` 下的 `avatar.png`、`bubble.png`、`photo.png`、`skill_tip.png`、`fashion.png`、`hair.png`、`suit.png`。其中 `avatar.png` 已恢复角色头像库格与三个等级技能，`skill_tip.png` 为真实图标点击后的详情叶子。

## 6. 验证记录

| 层级 | 结果 | 证据/备注 |
|---|---|---|
| C# 编译 | 通过 | `dotnet build Assembly-CSharp-Editor.csproj -m:1`：0 error，79 条既有 warning |
| Unity CLI 真实 Prefab 点击 | 通过 | `CliVerify.MainUIChatHud`：设置/好友/商城均由可见 `Image` 经 `GraphicRaycaster→PointerClick` 各路由 1 次，聊天滚动回归同时通过 |
| 设置→头像完整只读分支 | 通过 | `CliVerify.SettingFashionCurrent`：四外页签、三内页签、头像条目、三个技能圆标和技能详情均为真实射线点击；最终复验首次 1810ms、热开 69ms，`addedResources=none`，11201～11203 零发送 |
| Dress/Fashion 既有回归 | 通过 | `CliVerify.Dress` 与 `CliVerify.Fashion` 均 `VERDICT pass=True`；Fashion 原 413xx 行为、套装与等级页没有因四页签索引调整回归 |
| Addressables 语义差异 | 通过 | Remote_resource 33812→33940，仅新增 128 条，删除 0，既有条目地址/标签变化 0；预检闭包 `required=154 added=0 missing=0` |
| WebGL 壳构建 | 构建成功，未验收 | 隔离工作树加 `-buildTarget WebGL` 后 Release+gzip 壳成功 |
| 部署后运行态 | 未通过，已回滚 | 新工作树首次只打壳生成了不完整的本地 Addressables catalog，启动报 `prefabs/ui/login/loginstage` 缺 key；尝试拼接旧本地 AA 也未在验收时限内完成启动，不作通过处理 |
| 线上安全恢复 | 通过 | 恢复原稳定 WebGL 壳，远端 wasm/catalog 哈希匹配，Browser MCP 冷启后重新到达登录页 |

因此，本轮已证明“老端 Browser MCP 取证 → 配置资源闭包 → Unity 真实 Prefab 深度点击 → 增量修复 → 原路径复验”可以无人参与地跑通一个实际分支。它不等于整个设置树完成：改名即时刷新仍待单独闭环，装扮写事务受项目硬约束保持不接；本轮也没有重新宣称 WebGL 新壳已部署验收。

## 7. 下一层清单

下一步不跳到好友或商城。先继续设置功能树：

1. 闭环 `settings.rename`：提交前/回包/当前设置页立即刷新/关闭重开。
2. `settings.change-avatar` 的只读/UI 子树已完成；若产品要恢复激活/升级/穿戴，必须先明确解除 R540 对 11201～11203 的原负约束，再实现权威事务，不能为页面“可点”而裸发包。
3. 依次跑四滑条、自动拾取、坐骑/降神/自动任务、10 个屏蔽项和底部五个操作。

设置父节点只有在所有应验叶子完成后才能收口，然后才转向好友、商城、聊天、人物/背包等主界面常驻入口。
