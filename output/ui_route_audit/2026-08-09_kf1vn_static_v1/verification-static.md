# Kf1vn 静态校验记录

- 校验日期：2026-08-09
- 范围：仅 Kf1vn 生产文件岛与本目录台账输出
- 生产文件改动：0
- 构建：未运行。四路线共享工作树仅允许主控最终串行合并编译；本路线没有生产源码改动。

## 台账

- `route_ledger.py validate`：通过
- route：`mainui.kf1vn`
- schema：6
- manifest 节点：98
- manifest 叶节点：84
- ledger 状态：`blocked=19`、`defect=2`、`needs-runtime-verify=77`
- `ui_route_master.py`：通过，汇总 `routes=1 nodes=98`

## 定向静态断言

- 老端 `h5/src/kf1vn/*.ts`：26
- Unity `Generated/UI/Kf1vn/*Bind.cs`：26
- `Kf1vnModule.prefab` 内嵌 Kf1vn Bind：25；独立 `Kf1vnTabItem.prefab` 补足第 26 个 Prefab
- `Assets/Scripts/Module/Core/Kf1vn` 中业务 `View/Flow/Bootstrap`：0，作为静态缺陷登记，未伪造局部接管
- `Kf1vnController.cs` 未注册硬负约束协议 `62102/62118/62121/62134`
- 老端展开资源 PNG：93；Unity Kf1vn 专属展开 PNG：116；老端集合缺失：0
- `git diff --check -- Assets/Scripts/Module/Core/Kf1vn Assets/Prefabs/UI/Kf1vn Assets/GameRes/resource/game/kf1vn`：通过

## 明确未验证

本轮禁止 Unity、浏览器、Computer Use、真实挑战、购买、领取、GM 与账号写事务，因此真实 Web、双 viewport、像素、真实滚动、模型/特效双帧、cold/warm、即时刷新、关闭重开与返回链均未执行；对应叶节点保持 `needs-runtime-verify` 或 `blocked`，没有 `done`。
