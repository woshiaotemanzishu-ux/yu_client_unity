# Festival 静态精修结果

## 已修复

- 修复 `FestivalFlow` 在异步 Prefab 加载期间被 Close/Reset 后仍可能晚到并重新弹窗的竞态。
- Toggle 现在同时考虑“期望显示”与“已经显示”，避免加载中重复点击产生相反意图丢失。
- Reset 不再把 `_loading` 强行清零后允许第二个并行实例加载；旧 await 结束后按 generation 释放或重启最新请求。

## 明确 blocker

- Unity 缺失 Festival 的业务 View/配置消费链；仅有可编辑 Prefab 与自动生成 Bind。
- `config_fiesta_*`、任务页签配置、商品/支付/通用物品格不在 Festival 私有可写闭包内；本轮禁写 Generated、ClientConfigSync、Addressables、Common、Shop 等共享岛。
- 用户禁止 Unity、浏览器、账号/GM/充值/领取/消费，因此真实点击、协议结果、即时刷新、关闭重开、双 viewport、像素 diff、滚动、cold/warm、特效双帧全部未执行。
- 19402/19404/19405 为领取/消费类操作，无本轮授权；不得标 done。

## 验证边界

- 允许的静态检查：schema 6 init/apply/validate、`git diff --check`、C# 合并后单次串行离线编译。
- 未执行：Unity Editor/CLI、WebGL、Headless 浏览器、部署与账号写事务。
