# UI 路线 GM 测试态手册

## 适用边界

当页面需要特定等级、任务、物品、货币、功能开放、活动或成就状态时，先用 GM 准备可复现前置态，不等待人工养号。GM 只负责前置态；本次要验证的领取、升级、穿戴、购买、确认和失败分支，仍由真实 UI 点击、正式协议回包和权威 Model 刷新完成。

禁止在正式玩家账号上执行。日常同账号对照优先使用 `111111 / 111111`；不可恢复写入、互斥状态矩阵或需要反复重置时，通过现有 `GmApi.PlayerRegister` / 登录注册页创建用途明确的专用测试号。`123123 / 123123` 是历史满解锁号，不得无记录地替代 `111111` 做同账号 diff。

## 已有通道

- 服务端开关与密码以当前 `E:\GitProject\yu_server\config\gsrv.config` 为准，禁止把聊天记录里的旧值硬编码进脚本或证据。
- 协议 `11100` 拉取服务端当前秘籍清单；`11101` 执行 `命令_参数1_参数2`。先发送 `setgmpassword_<当前密码>` 完成本会话授权。
- 老 H5：Z 键或聊天 `@test` 打开 `CheatInputView`；Headless 可调用 `CheatModel.GetInstance().Fire(CheatModel.SEND_CHEAT_TO_SERVER, command)`，但仍须记录真实命令和执行后的权威探针。
- Unity PlayMode：使用「神霄/GM 秘籍」，底层为 `GmCheatController.Instance.RequestList()` / `SendCommand()`。真实 Web 对照可先在老 H5 同账号执行配方、确认断线，再登录 Unity 验证服务器权威状态。

当前服务端常用命令以 `yu_server/src/gm/pp_gm.erl` 下发清单为准，例如：

- `lv_<增加等级数>`：是增加等级，不是设置绝对等级。
- `goods_<物品ID>_<数量>`、`money_<数值>`：准备物品或货币。
- `task_<任务ID>`、`finishtask_<任务ID>`、`finlvtask_<任务ID>`、`fincurrentmaintask`：准备任务/开放条件。
- `completeachv_<category>`：完成指定系列成就；`clearachv` 会重置玩家成就，属于破坏性命令，只能用于专用测试号。

不得凭记忆猜命令含义；先用 `11100` 或当前 `pp_gm.erl` 核对名称、参数与默认值。`pt_*`、`mfa_*`、清背包、重置进度、全局活动和服务器级命令默认禁止用于普通 UI 验证。

## UIAudit 顶级采集号 CLI

先只读，再显式写入；每次使用全新的不可变输出目录：

```powershell
node Tools/UIAudit/cli.cjs gm top-ui --account 111111 --password 111111 --allow-blocked-read-only --output output/ui_route_audit/<日期>_account_111111_top/<read-only-run>
node Tools/UIAudit/cli.cjs gm top-ui --account 111111 --password 111111 --apply --output output/ui_route_audit/<日期>_account_111111_top/<apply-run>
```

登录密码也可由 `UIAUDIT_PASSWORD` 提供。CLI 从当前服务端配置读取 GM 密码，只在会话内发送 `setgmpassword_<runtime>`；进度与最终报告固定写 `passwordRecorded=false/gmPasswordRecorded=false`，不得记录实际 GM 密码。`--apply` 之外不发送 GM 命令；`--apply` 会在同一热会话逐条发送、逐条读取权威状态，随后关闭浏览器并用完全新的老 H5 会话复验。满级、满转、满 VIP 和货币已经满足时，配方为空或只含剩余差量，禁止为了“确认”重复写入。

当前 `top-ui` 是“全局高阶基线 + 角色/垂神翼影路线 profile”，不是所有业务状态的万能全解锁：

1. 入口层：等级、主线/功能任务、开服天数等只读开放探针；当前配置最高有效等级为 840，`敬请期待/lv999` 是占位项，不强行满足。
2. 转生层：使用运行时 `config_reincarnation_cfg` 的各转满级值，按 `5转630 → 301600 → 6转720 → 301700 → 7转840` 交错推进；必须核对精确 `(turn,turn_stage)`，不能只看等级。
3. 主体层：只补配置要求且尚未激活的外形主体 `1/2/3/4/5/12`，不重置已有养成。
4. 路线层：从当前配置、激活列表和背包读取激活物 ID/需求/持有数。只在目标页面确实需要“可激活”样本时发放一个精确最小闭包；默认保留已激活、可激活、锁定等可测试状态，不批量灌入全部道具。

新路线不得直接复用 `roleOutwardWing.pass`。应先新增路线 profile，列出入口 View、主体类型、内容目录、所需状态矩阵和精确物品闭包，再把它加入 fresh-session 验证。不可恢复的激活事务优先使用专用账号；GM 只把物品放进背包，真正点击“激活”仍由 UI 和正式协议完成。

## 已证实的危险捷径

- 禁止使用 `finlvtask_9999999`、`finishawakentask`、`finfiguretask` 作为通用“全完成”。2026-08-12 的真实老端实验中，账号先达到 7 转，发送这组三条后持久化回退到 5 转且当前转生任务被清空。
- 高转任务列表为空时不得猜号。当前配置证明 6/7 转任务分别为 `301600/301700`；CLI 先等待自然任务出现，缺失时才以源码存在的 `task_<id>` 恢复精确任务，再执行 `finishtask_<id>`。
- `vipexp` 的参数单位不是客户端配置经验单位。当前服务端 `VIP_CONVERT=100`，VIP15 配置累计经验为 77000，因此从 0 补足的原始差量为 7700000；到达最高 VIP 后运行时经验可显示 0，验收以 `vipLevel=15` 为权威。
- `ItemUseView` 不能按“有关闭按钮”盲点。公共处理器只点击源码证明不消费物品的 `close_btn`，以 `typeId/goodsId` 绑定原物品，验证前后背包数量不减，并以 `isPop + displayedInStage` 区分真实打开与已关闭缓存对象；名称文本仅作备注。
- `FunctionOpenAutoView` 的自然 10 秒关闭会发送 13801，且 close callback 可能按 `jump_id` 打开共享 `BaseWindowSkin`。公共 session 等待权威计时器，再只点共享窗框 `_img_return`；不得点击功能提示或页面内容来省时间。

## 生产时间口径

把账号准备拆成三类时间：`startup/runtime wait`、`GM action+observation`、`fresh-session verification`。2026-08-12 最终补写运行总计 70.1 秒，其中剩余货币命令只用 277ms，命令后稳定闸约 3.1 秒；其余主要是两次真实老端登录和随机启动队列。6/7 转任务的精确恢复加完成分别为 2.75 秒、2.84 秒。该数据说明可省的是重复登录、无效命令和假失败等待，不是资源 ready、功能提示权威计时器或全新会话复验。账号基线已经满足后，后续页面只读取并补路线 profile，不得每个 Tab 重做顶级号。

## 配方与证据

每条路线在不可变证据目录保存一个状态配方，至少包含：

```json
{
  "id": "achievement-claimable-v1",
  "account": "专用测试号或111111",
  "purpose": "成就条目可领取与阶段可升级",
  "commands": ["setgmpassword_<runtime>", "completeachv_1"],
  "expected": ["40903/40909 至少一项 status=1", "40901 阶段星数满足领取条件"],
  "probes": ["老端打开成就页", "Unity 登录后重新请求 40901/40903/40906/40908"],
  "reset": "专用号可用 clearachv；111111 禁止无授权重置",
  "executedAt": "带时区时间"
}
```

把配方文件及执行日志的 SHA-256 绑定到相关叶子的 `runtime_state/state_evidence[]`。命令已发送不等于状态成功；至少一个协议、Model 或玩家可见探针必须证明目标前置态已建立。目标事务完成后再保存即时刷新与重开证据，不能把 GM 后的最终状态冒充 UI 事务结果。
