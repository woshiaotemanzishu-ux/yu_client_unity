# Welfare / 福利中心静态审计（2026-08-09）

## 结论

- 老端 `WelfareView` 是六页签壳：签到、在线奖励、每日补给、兑换礼包、游戏公告、Follow Rewards。已枚举 88 个 schema 6 路由节点，覆盖外壳、页签、条件显隐、红点、列表、详情、弹窗、领取/补签/兑换事务和返回链。
- 正式账当前汇总：`blocked=71`、`needs-runtime-verify=17`、`not-run=0`；父节点状态由 77 个叶子结果自动回卷。
- Unity 当前没有 Welfare 外壳 Prefab/Bind/Flow；入口 `417` 仍由 `GameNoticeBootstrap` 直接降级打开游戏公告。`OnlineView` 完全不存在；DailySign 与 Eyou 只有 Prefab + Generated Bind、没有 Core runtime View。按本轮边界未改 Generated、MainUI、GameNotice、Activity、Common 或 Addressables，也未猜造 Welfare 外壳。
- Exchange 与 GameNotice 子页已有各自运行时；DailySupply 已在 Activity 路线落地，但三者接入 Welfare 壳均属于跨模块依赖，本路线只记录 blocker。
- 本轮仅修复两个 Welfare 专属、静态确定的问题：
  1. `41705` 从错误的“补签”语义更正为“累计签到天数奖励”，新增 `ClaimTotalCheckin(sum)`；真实补签唯一为 `41704(day, retroactive=1)`，旧方法仅保留 obsolete 兼容转发。
  2. `41715.time` 不再冻结入口红点。模型记录本次回包观测时刻，以服务器时钟推进 `CurrentOnlineTime`；控制器只等待最近未领取档位，跨过阈值刷新入口红点，并用版本号淘汰重进/新回包/销毁前的旧等待。

## 老端六页签事实

| 页签 | 显示条件 | 红点 | 关键读/写 |
|---|---|---|---|
| 签到 | 等级达到 `checkin_open_lv` | 每日可签/可补签与累计可领 | 读 41703；写 41704（日签/补签）、41705（累计） |
| 在线奖励 | 存在在线奖励数据 | 随在线秒数跨阈值动态点亮 | 读 41715；写 41716（单领/一键） |
| 每日补给 | CustomActivity base61/sub1 | 活动红点 | 读 33104/33209；写 33105 |
| 兑换礼包 | 等级 50 且非 alpha | 无独立页签红点 | 写 15087 |
| 游戏公告 | 公告列表非空 | 未读公告 | 只读/本地已读状态 |
| Follow Rewards | attention state 且非 alpha | 无独立页签红点 | 配置只读 |

## 组件依赖与授权点

- `BaseWindowSkinView`、Reward/Equipment/BaseAwardItem、DailyTips/Alert、RewardPreView、CongratulationObtainView 均为共享组件，禁止在 Welfare 私复制；需共享所有者写权限与代表消费者抽查。
- Welfare 外壳接入口 `417` 需要 MainUI/GameNotice 路由所有者写权限；DailySupply 需要 Activity 所有者；公告/兑换需要将既有子页作为 Welfare tab child 的跨模块集成权限。
- 41704、41705、41716、33105、15087 都是账号写事务。需要明确写事务授权、可恢复测试状态和正式 UI 点击后，才能验证成功/失败回包、即时刷新及关闭重开。

## 静态验证

- `dotnet build Shenxiao.Module.Core.csproj --no-restore` 的直接构建被并发新增、尚未进入 Unity 生成 csproj 的 Shop/Guild 类型阻断（3 个与 Welfare 无关的 CS0246）。用本输出目录的 build-only targets 纳入这些并发源码后，Core 构建通过：0 errors / 84 existing warnings。
- `git diff --check`：Welfare 两个改动文件通过。
- `verify-static.ps1`：校验 41705 语义、在线时长推进、下一阈值调度、旧等待失效与改动范围。
- 未启动 Unity，未启动浏览器，未执行领取、补签、兑换、购买或任何账号写入；因此所有玩家可见、交互、动态、生命周期和 Web 对比闸仍未完成。

## 最快后续收口顺序

1. 取得 MainUI/GameNotice + Generated/Addressables 权限后，一次性转换 Welfare 六页签共享壳，并把既有 Exchange/GameNotice/DailySupply 作为 child 接入。
2. 同一批落地缺失的 OnlineView；优先复用六固定槽与 Welfare 已有 41715/41716 模型/控制器。
3. 为 DailySign/Eyou 把 Generated-only Prefab 接成 Core runtime View；奖励格只复用共享组件。
4. 只读真实 Web 批次先收条件显隐、红点、滚动、详情、切页/返回；最后在明确授权账号上串行验证 41704/41705/41716/33105/15087 写事务并关闭重开。
