# Adventure component dependencies

| 组件 | 消费者 | 本岛结论 |
|---|---|---|
| EquipmentItem/Common | 31 棋盘格中的特殊/终极格、12 奖励预览、6 商店格 | Common 禁写；身份、详情、品质和状态矩阵 blocked |
| BaseWindow/Common | AdventureWindowView 标题、返回、货币、说明 | Common 禁写；外壳运行态 blocked |
| UI 模型/特效 | 主角、UI_2001、UI_yjtb_01、UI_yjtb_02、ui_anniu_5 | 需双时间点真实出帧；blocked |
| RewardFlyService/统一奖励弹窗 | 逐格奖励与最终奖励 | 未触发 42702，账号写禁止；blocked |
| MainUI/ActivityIcon | 42701/42702 活动入口与红点 | 本岛只注册路由/红点数据；实际布局命中 blocked |
| Halo/GodEquip/Advertising | 免费重置、神装跳转、广告增次 | 跨岛只记 blocker |
