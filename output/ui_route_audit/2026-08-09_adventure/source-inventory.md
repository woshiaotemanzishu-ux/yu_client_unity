# Adventure source inventory

- 老端页面：AdventureWindowView / AdventureMainView / AdventureItem / AdventureShopView / AdventureShopItem
- 老端模型与控制器：AdventureModel / AdventureController
- Unity：AdventureModule.prefab + Adventure 模块 9 个 C# 文件
- 配置：kv=14、rand=2、reward=32、loc=600；Unity 当前均无可达配置资产。
- 协议：42700/42701/42704 只读；42702 投掷、42703 重置、42705 购买、42706 手动刷新属于写事务。
