# Task / 任务路线首轮静态结论

本轮只完成老端源码、Unity `TaskModule.prefab`、Generated Bind、Task Flow/Model/Controller/协议的静态调和；没有启动 Unity、浏览器，没有操作账号或执行任务提交。因此 schema 6 台账 123 个节点全部保持 `not-run`，没有把编译或静态断言写成运行态完成。

## 已静态落地

- 新增 `TaskFlow`：消费现有 `TaskViewBind / TaskBarItemBind / TaskContentSubViewBind`，按老端 `GetTaskData` 语义展示已接任务，支持默认展开、展开/收起、30000/30001 刷新、奖励格、前往与完成提交。
- 新增 `TaskModel.GetTaskListForTaskView`：任务总览使用已接任务全集，避免复用 HUD 的引导态过滤后漏掉唯一主线；同时保留老端“主角满 4 转后不再展示转生任务”的门禁。
- `TaskFinishView` 提交前再次检查 `IsAllStepFinish`，对齐老端点击时的二次门禁，避免弹层打开后任务已经推进/替换仍提交旧任务号。
- 奖励仍复用 `Common.EquipmentItem`，没有在 Task 页面复制共享节点树或修改 Common。
- 新增 `TaskFlow.cs` 不在本轮未刷新的 Unity 生成 `.csproj` 清单内；主控另用 `TaskFlow.Isolated.csproj` 连同现役 Core/Generated/Common/UI/TMP 引用独立编译，0 warning / 0 error，避免把旧工程清单的绿灯误当作新增文件已编译。

## 静态确定的未闭合项

1. 老端 `MainUITaskTeamView.SwitchView(Task)` 在任务页签已选中时打开 `TaskView`；Unity `MainUITaskTeamView.ShowTaskTab` 当前不调用 `TaskFlow.Toggle`。入口属于 MainUI 岛，本轮禁止修改，因此新任务页还不是玩家可达闭环。
2. `TaskCircleFinishView` 有 Prefab/Bind，但 Unity 无业务消费者；同时 `TaskController` 未注册老端 30010/30011/30012/30013，`TaskVo/TaskModel` 也没有 `circle_task_progress_data` 与普通/额外奖励数据。只写 View 会伪造数据，本轮保留阻塞。
3. `TaskUpAlertView/TaskUpAlertItem` 有 Prefab/Bind，但升级提醒活动配置、限时状态、`OpenFun` 跳转和 `ui_renwulan` 效果尚未调和；`TIP_LV` 当前仍走精确降级提示。
4. `TaskAutoSettingView` 有 Prefab/Bind，Setting 10203 已存在；但首次新职业触发、`CheckBox`、Story 指引锁都跨岛，未在 Task 岛重复实现。
5. 普通完成弹层当前“任意点击都提交”的行为来自既有实现与既有 CLI 用例，而老端源码区分“关闭”和“提交”。缺少同账号真实运行证据，本轮没有静态推翻既有人工结论，只登记为运行态身份核对项。
6. `DoTask` 仍有装备熔炼、宝石强化、大妖挑战、橙装穿戴等系统降级分支；这些是跨模块目标页缺口，不在 Task 岛复制占位页。

## 运行 / 写事务闸门

- Unity 真实 Prefab `GraphicRaycaster → PointerClick`：未运行（明确禁止启动 Unity）。
- 老 H5 与 Unity WebGL 同账号顺序对比、两档 viewport、old/unity/overlay/diff：未运行（明确禁止浏览器）。
- 30004 接受/失败、即时父页刷新、关闭重开、10 秒自动提交：未执行（不可恢复任务提交，未获账号写授权）。
- 10203 自动/手动任务设置写回与还原：未执行（本轮禁止账号写事务）。
- 循环任务 30010–30013：Unity 当前协议/模型缺失，先实现数据链后才能跑。
- `EquipmentItem` 共享状态矩阵、任务特效双时间点像素差、滚动裁切与完全离屏零残留：未运行。
- Web Player/catalog/Git dirty 指纹同批与 cold/warm 性能：未运行。

## 文档边界

本轮按父任务的独占闭包明确禁止修改 `Docs/**`，所以没有同步权威进度文档；该限制高于本代理的局部落地范围，需主代理汇总时处理。
