# Attention 老端源码/配置清单（静态）

## 两条独立路线

1. `IconType=113`（收藏小程序/通用关注）
   - 入口门：非 alpha、`ClientConfig.attention_open`、`ClientAttention[plat_name]` 存在、`open_day <= 开服天`、`open_lv <= 角色等级`。
   - 页面：`AttentionViewLaya`，Activity 层、居中、带背景，关闭即销毁。
   - 控件：动态主背景、标题、二维码/渠道图、奖励横排、分隔线、HTML 描述、复制按钮、关闭按钮。
   - 复制：微信小游戏走 `wx.setClipboardData`，其他 Web 走 DOM input + `execCommand('copy')`，成功提示“复制成功”。
   - 动态数据：`ClientAttention[plat_name].image/wx_name/des/reward/open_day/open_lv`。

2. `SdkIconType=113113`（SDK 关注/领奖）
   - 入口门：爱疯平台或 debug；native `subscribe(enabled)` 开启；CustomActivity `(base_type=70,sub_type=1)` 第 1 档奖励未领。
   - 页面：`AttentionView`，居中、背景遮罩、切场景关闭、点击背景关闭。
   - 控件：主背景、奖励横排、关注/领取按钮、按钮文案、条件红点。
   - 未关注点击：1 秒节流后调用 `subscribe({apiType:'askShow'})`；`cpStatus==0` 提示“调起失败”。
   - 已关注未领奖点击：经 CustomActivity 发送 `33105,70,1,1`，属于真实领奖事务。
   - 已领取点击：提示“已领取”。
   - 红点：`sdk_attention_state && !activity_reward_state`。
   - 回包/活动刷新：刷新奖励、红点、按钮；奖励已领时关闭 `AttentionView` 并删除 113113 图标。

## 老端文件证据

- `E:/GitProject/yu_client/h5/src/commonModel/AttentionModel.ts`
- `E:/GitProject/yu_client/h5/src/commonController/AttentionController.ts`
- `E:/GitProject/yu_client/h5/src/attention/AttentionView.ts`
- `E:/GitProject/yu_client/h5/src/attention/AttentionViewLaya.ts`
- `E:/GitProject/yu_client/cdn/resource/game/attention/AttentionView.json`
- `E:/GitProject/yu_client/cdn/resource/game/attention/AttentionViewLaya.json`
- `E:/GitProject/yu_client/cdn/resource/game/attention/texture.atlas`

本轮禁止浏览器，因此没有老端真实运行树、截图、cold/warm 或账号状态证据；这些不得由源码静态清单替代。
