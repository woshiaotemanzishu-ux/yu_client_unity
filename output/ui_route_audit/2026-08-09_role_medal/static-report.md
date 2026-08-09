# 角色人物页 · 勋章（地境）静态实施报告

结论：Role `_Group3` 到 `MedalEnterView` 的外窗路由、地境配置与权威快照呈现、条件矩阵、13402 严格空包及回包即时刷新已实现；没有修改或重转 `RoleModule.prefab`。本轮未启动 Unity/浏览器，也未发送真实写事务，因此整页不能标记 `done`。

## 已实现

- `MedalBootstrap` 注册 `MedalEnterView`，断线时清理实例与订阅。
- `MedalConfigs` 读取现有 `config_medal` 的 131 行，解析属性、战力、九劫塔层数和道具消耗；服务端 `id=0` 按老端映射到展示配置 `id=1`。
- `MedalFlow` 复用 `BaseWindowSkin`、`RoleModule/MedalView`、`MedalCostItem` 和共享 `BaseAwardItem`，呈现当前/下一阶属性、星级、条件行、按钮状态及红点。
- 主按钮按老端顺序：层数不足前往 Rune 副本；否则材料、战力预检；仅真实点击、条件满足且通过 1 秒去抖后发送 13402。
- 13402 S2C 仅以 `id/honour` 更新权威模型，保留 13401 的强化、副本层数与战力字段并即时触发页面刷新。

## 明确边界

- 天境：`TitleMainView` 未实现，第二页签可见但永远不切页，提示“天境称号尚未接入”；不以地境页面冒充。
- 强化：两份强化配置缺失，且老端当前成本表门槛均为 `medal_lv=9999`，高于地境最大 id 131；入口隐藏。
- 礼包：`PushGiftModel/GiftPushIcon(eJingJie)` 未移植，宿主隐藏。
- 资源：16 张地境/外窗图片缺失；另有 3 份强化/天境配置缺失。仅生成候选闭包与只读哈希校验，未改 Addressables、未复制资产。
- 写事务：激活、升级、晋升均为 13402 永久写入。本轮只做代码与静态协议验证，没有使用账号执行。

## 待运行门禁

需要在当前源码与 catalog 精确匹配的 Unity Web 包上，用同账号依次验证：入口 cold/warm、350ms/1000ms/ready、未激活/升星/晋升/满阶、条件充足/不足、九劫塔跳转、返回、即时刷新、关闭重进、两档 viewport，以及经授权后的 13402 成败回包。以上完成前，schema 6 根状态保持 `blocked`。

## 本轮静态验证

- Unity Bee 现有 `Shenxiao.Module.Core.rsp` 加本轮新文件与当前 `Proto.cs` 的隔离 Roslyn 编译：退出码 0；输出仅写系统临时目录，没有启动 Unity。
- Unity Bee 现有 `Shenxiao.Editor.rsp` 加本轮 Medal 源文件的隔离 Roslyn 编译：退出码 0，包含扩展后的 `MedalCase`。
- `validate_resource_candidates.py`：19 个候选旧端源文件全部存在且 SHA-256 匹配，Unity 目标 19 个均缺失，`mutation_performed=false`。
- `route_ledger.py init/apply/validate`：schema 6，共 20 节点；12 `blocked`、8 `needs-runtime-verify`，根状态 `blocked`。
