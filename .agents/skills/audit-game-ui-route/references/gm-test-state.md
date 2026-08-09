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
