# Setting 完整路线静态核查

## 结论

- Setting 已有可编辑 `SettingModule.prefab`，本轮按 `fix-view` 增量核查，不重转模块。
- 老端、Unity Prefab/Bind、View/Flow/Model/Controller 三方已形成完整控件树；玩家可达主体是基础页与屏蔽页，不把隐藏的微信订阅导入模板误算为可达页签。
- 当前未发现需要在 Setting 专属文件中盲改的确定性缺陷；保留人工 Prefab 作为视觉事实源。
- 本批仅为静态首轮。未启动 Unity/浏览器、未发 10203/10210、未执行断线/切角/回登录，因此不得标记真实运行或页面 done。

## 老端到 Unity 映射

| 区域 | 老端语义 | Unity 实现 | 静态结论 |
|---|---|---|---|
| 固定入口 | HUD 设置按钮 -> `SettingView` | `SettingBootstrap` / `SettingFlow.Open` | 目标身份存在；MainUI 本轮只读 |
| 顶部资料 | 头像、名字、角色 ID、区服、复制 ID | `PopulateRoleInfo` / `RefreshHeadIcon` / `CopyRoleId` | 映射完整 |
| 更换头像 | `OpenFun(203,[DressType.Head])` | 关闭 Setting 后 `FashionFlow.OpenDress(HeadType)` | 目标身份一致，不改 Role |
| 改名 | 42602 免费资格 -> 改名弹窗 | `RoleController.RequestRenameFreeCheck` + `SettingChangeNameView` | 跨 Role 协议，运行写闸未执行 |
| 四滑条 | subtype 6/7/9/12，关窗批量 10203 | 4 个 `WithBtnHSlider` + `SendSliderValues` | 范围与音量副作用一致 |
| 自动拾取 | subtype 17/18/19，150 级门禁 | `SettingShieldItem` 三行 + `OnToggleAutoPick` | 条件链存在 |
| 条件设置 | 坐骑 202、降神 201、自动任务 21 | 功能开放/配置显隐 + 勾选 | 条件链存在 |
| 屏蔽列表 | 1/2/3/10/14/20/22/25/5/26 | `ShieldSubtypes` 十项 | 非微信端集合一致；24 微信推送不展示 |
| 极简模式 | 修改屏蔽/拾取前退出 subtype 8 确认 | `ToggleWithSimpleModeGuard` | 确认/取消链需运行核 |
| 协议/隐私 | 两个独立正文入口 | `LoginFlow.OpenAgreementDocument(1/2)` | 目标身份存在，Login 本轮只读 |
| 底部动作 | 换角/回登录/默认/脱卡/修复均二次确认 | `TipsManager.Confirm` 五条链 | 确认链静态存在；破坏性执行 blocked |

## 协议与即时状态

- 10202 是服务器权威全量；10203 成功后才 `SettingModel.ApplyChanged` 并广播 `EVT_SETTING_UPDATED`。
- 连续 10203 使用 FIFO `_pending` 对应服务端顺序回包，避免单槽错配。
- 滑条关闭时批量发送与老端 `close_callback -> SendSetSliderNum` 一致；真实验证必须先保存四个原值并在结束时恢复。
- 自动拾取/屏蔽/坐骑/降神/任务切换必须验证：成功回包、父页即时刷新、关页重开、还原原值。
- 破坏性叶 `change-role`、`return-login`、`10210 flee`、`disconnect-repair` 未获执行授权，只允许核确认框文案、取消和静态目标。

## 运行闸

1. 同账号老 H5 / Unity WebGL，720x1280 与 1920x1080，同一会话按台账 DFS。
2. 四滑条和 13 个开关逐项记录原值 -> UI 点击 -> 权威回包 -> 即时刷新 -> 关开 -> UI 恢复。
3. 屏蔽列表真实拖动，验证 `RectMask2D` 裁切与 subtype 26 末项可达。
4. 极简模式至少覆盖确认与取消两条链，取消必须零写入。
5. 头像只验证到时装 Head 页身份；改名需 Role 路线授权后单独写回并恢复。
6. 底部破坏性动作只点到确认框并取消；执行叶保持 blocked。
7. 保存源码/dirty、Player、catalog、两档 viewport、cold/warm 和 old/unity/overlay/diff 证据后，才可提交 applicable gates。
