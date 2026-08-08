# 共享物品槽直接消费者静态清单

可重跑命令：

```powershell
rg -l --glob '*.prefab' '_tpl_BaseAwardItem|value: BaseAwardItem' Assets/Prefabs | Sort-Object
rg -l --glob '*.prefab' '_tpl_EquipmentItem|value: EquipmentItem' Assets/Prefabs | Sort-Object
```

本轮结果：`BaseAwardItem=80` 个 Prefab 文件，`EquipmentItem=81` 个 Prefab 文件。共享 API 未删除或改名，运行时按使用形态抽样，不逐页穷举。

## BaseAwardItem（80）

```text
Assets/Prefabs/UI/Achv/AchvModule.prefab
Assets/Prefabs/UI/Activity/ActivityModule.prefab
Assets/Prefabs/UI/Arena/ArenaModule.prefab
Assets/Prefabs/UI/Arena/ArenaRankRewardItem.prefab
Assets/Prefabs/UI/AtListPurchase/AtListPurchaseModule.prefab
Assets/Prefabs/UI/Baby/BabyAddImprintItem.prefab
Assets/Prefabs/UI/Baby/BabyEquipIcon.prefab
Assets/Prefabs/UI/Baby/BabyEquipSubItem.prefab
Assets/Prefabs/UI/Baby/BabyForgeView.prefab
Assets/Prefabs/UI/Baby/BabyImprintItem.prefab
Assets/Prefabs/UI/Baby/BabyLikeReward.prefab
Assets/Prefabs/UI/Baby/BabyLikeView.prefab
Assets/Prefabs/UI/Baby/BabyModule.prefab
Assets/Prefabs/UI/Baby/BabyRenameView.prefab
Assets/Prefabs/UI/Bag/BagEquipmentIcon.prefab
Assets/Prefabs/UI/Bag/BagModule.prefab
Assets/Prefabs/UI/Bossdomain/BossdomainModule.prefab
Assets/Prefabs/UI/BossField/BossFieldModule.prefab
Assets/Prefabs/UI/BossPersonal/BossPersonalModule.prefab
Assets/Prefabs/UI/BrightSea/BrightSeaModule.prefab
Assets/Prefabs/UI/Chat/ChatModule.prefab
Assets/Prefabs/UI/Common/BaseWindowSkin.prefab
Assets/Prefabs/UI/Common/CommonModule.prefab
Assets/Prefabs/UI/Common/CongratulationObtainItem.prefab
Assets/Prefabs/UI/Common/EquipmentItem.prefab
Assets/Prefabs/UI/Common/ItemInfoItem.prefab
Assets/Prefabs/UI/Common/TabButtonTwoSkin.prefab
Assets/Prefabs/UI/Composite/CompositeModule.prefab
Assets/Prefabs/UI/Composite/CompositeRuneView.prefab
Assets/Prefabs/UI/Country/CountryModule.prefab
Assets/Prefabs/UI/Daily/DailyModule.prefab
Assets/Prefabs/UI/Dailylogin/DailyloginModule.prefab
Assets/Prefabs/UI/DailyRecharge/DailyRechargeModule.prefab
Assets/Prefabs/UI/DailySign/DailySignModule.prefab
Assets/Prefabs/UI/DiscountGift/DiscountGiftModule.prefab
Assets/Prefabs/UI/DragonBall/DragonBallModule.prefab
Assets/Prefabs/UI/Dsgt/DsgtModule.prefab
Assets/Prefabs/UI/DungeonMaterial/DungeonMaterialModule.prefab
Assets/Prefabs/UI/DungeonPolar/DungeonPolarRwRenderItem.prefab
Assets/Prefabs/UI/DungeonRune/DungeonRuneModule.prefab
Assets/Prefabs/UI/DungeonTower/DungeonTowerModule.prefab
Assets/Prefabs/UI/Equip/EquipModule.prefab
Assets/Prefabs/UI/EquipRefinement/EquipRefinementModule.prefab
Assets/Prefabs/UI/Evening/EveningModule.prefab
Assets/Prefabs/UI/Fashion/FashionModule.prefab
Assets/Prefabs/UI/FeastBoss/FeastBossModule.prefab
Assets/Prefabs/UI/Festival/FestivalInfoListItem.prefab
Assets/Prefabs/UI/Festival/FestivalRewardItem.prefab
Assets/Prefabs/UI/Foreshow/ForeshowModule.prefab
Assets/Prefabs/UI/FtvActiveness/FtvActivenessModule.prefab
Assets/Prefabs/UI/FtvCollectionExchange/FtvCollectionExchangeModule.prefab
Assets/Prefabs/UI/FtvExchange/FtvExchangeModule.prefab
Assets/Prefabs/UI/FunctionOpen/FunctionOpenModule.prefab
Assets/Prefabs/UI/GodBeast/GodBeastModule.prefab
Assets/Prefabs/UI/GodBefall/GodBefallEquipmentItem.prefab
Assets/Prefabs/UI/GodBefall/GodBefallMainView.prefab
Assets/Prefabs/UI/GodCourt/GodCourtModule.prefab
Assets/Prefabs/UI/Guild/GuildModule.prefab
Assets/Prefabs/UI/GuildFight/GuildFightModule.prefab
Assets/Prefabs/UI/Guildidol/GuildidolModule.prefab
Assets/Prefabs/UI/Jewel/JewelModule.prefab
Assets/Prefabs/UI/Kf1vn/Kf1vnModule.prefab
Assets/Prefabs/UI/KfHotPoint/KfHotPointModule.prefab
Assets/Prefabs/UI/LimitLevelShop/LimitLevelShopModule.prefab
Assets/Prefabs/UI/ListDuobao/ListDuobaoModule.prefab
Assets/Prefabs/UI/ListDuobao/ListGoodsItem.prefab
Assets/Prefabs/UI/LogGift/LogGiftModule.prefab
Assets/Prefabs/UI/Pet/PetModule.prefab
Assets/Prefabs/UI/Rune/RuneConvertItem.prefab
Assets/Prefabs/UI/Rune/RuneIcon.prefab
Assets/Prefabs/UI/Rune/RuneModule.prefab
Assets/Prefabs/UI/RuneTreasure/RuneTreasureMainView.prefab
Assets/Prefabs/UI/Setting/SettingModule.prefab
Assets/Prefabs/UI/Shop/ShopItem.prefab
Assets/Prefabs/UI/Shop/ShopModule.prefab
Assets/Prefabs/UI/Suit/EquipSuitAwardItem.prefab
Assets/Prefabs/UI/Suit/EquipSuitCostItem.prefab
Assets/Prefabs/UI/Suit/SuitModule.prefab
Assets/Prefabs/UI/TopVip/TopVipShopItem.prefab
Assets/Prefabs/UI/Vip/VipModule.prefab
```

## EquipmentItem（81）

```text
Assets/Prefabs/UI/Achv/AchvModule.prefab
Assets/Prefabs/UI/Activity/AccumRechargeItem.prefab
Assets/Prefabs/UI/Activity/DailySupplyItem.prefab
Assets/Prefabs/UI/ActivityOverView/ActivityOverViewModule.prefab
Assets/Prefabs/UI/AddVipService/AddVipServiceModule.prefab
Assets/Prefabs/UI/Adventure/AdventureModule.prefab
Assets/Prefabs/UI/Attention/AttentionModule.prefab
Assets/Prefabs/UI/AutoBrush/AutoBrushModule.prefab
Assets/Prefabs/UI/Bag/BagEquipmentIcon.prefab
Assets/Prefabs/UI/Boss/BossDropRecordItem.prefab
Assets/Prefabs/UI/Bossdomain/BossdomainModule.prefab
Assets/Prefabs/UI/BossField/BossFieldModule.prefab
Assets/Prefabs/UI/BossField/BossFieldRewardItem.prefab
Assets/Prefabs/UI/BossMystery/BossMysteryModule.prefab
Assets/Prefabs/UI/BossPersonal/BossPersonalModule.prefab
Assets/Prefabs/UI/Chat/ChatModule.prefab
Assets/Prefabs/UI/Chc/ChcModule.prefab
Assets/Prefabs/UI/Common/CommonModule.prefab
Assets/Prefabs/UI/Common/CommonRewardItem.prefab
Assets/Prefabs/UI/Common/CongratulationObtainItem.prefab
Assets/Prefabs/UI/Common/ItemInfoItem.prefab
Assets/Prefabs/UI/Composite/CompositeGoodsMatItem.prefab
Assets/Prefabs/UI/Composite/CompositeHolySealMatItem.prefab
Assets/Prefabs/UI/Composite/CompositeModule.prefab
Assets/Prefabs/UI/Composite/CompositeRuneView.prefab
Assets/Prefabs/UI/Composite/CompositeSelectEquipItem.prefab
Assets/Prefabs/UI/Composite/RingCompositeItem.prefab
Assets/Prefabs/UI/CustomActivity/CustomActivityModule.prefab
Assets/Prefabs/UI/CycleimpActlist/CycleimpActlistModule.prefab
Assets/Prefabs/UI/Daily/DailyModule.prefab
Assets/Prefabs/UI/Demon/DemonModule.prefab
Assets/Prefabs/UI/DestinyTurntable/DestinyTurntableModule.prefab
Assets/Prefabs/UI/Dialogue/DialogueModule.prefab
Assets/Prefabs/UI/DiamondGift/DiamondGiftModule.prefab
Assets/Prefabs/UI/DragonBallGift/DragonBallGiftModule.prefab
Assets/Prefabs/UI/DragonWhisper/DragonWhisperModule.prefab
Assets/Prefabs/UI/DungeonDragon/DungeonDragonModule.prefab
Assets/Prefabs/UI/DungeonDragon/DungeonDragonRewardItem.prefab
Assets/Prefabs/UI/DungeonEquip/DungeonEquipModule.prefab
Assets/Prefabs/UI/DungeonExp/DungeonExpEnterView.prefab
Assets/Prefabs/UI/DungeonExp/DungeonExpModule.prefab
Assets/Prefabs/UI/DungeonPartner/DungeonPartnerModule.prefab
Assets/Prefabs/UI/DungeonPartner/DungeonPartnerVsRewardItem.prefab
Assets/Prefabs/UI/DungeonRune/DungeonRuneModule.prefab
Assets/Prefabs/UI/Equip/EquipModule.prefab
Assets/Prefabs/UI/EquipRefinement/EquipRefinementModule.prefab
Assets/Prefabs/UI/Eternity/EternityModule.prefab
Assets/Prefabs/UI/Eudaemon/EudaemonModule.prefab
Assets/Prefabs/UI/Evening/EveningModule.prefab
Assets/Prefabs/UI/FeastBoss/FeastBossModule.prefab
Assets/Prefabs/UI/FirstBlood/FirstBloodModule.prefab
Assets/Prefabs/UI/FtvAnyRecharge/FtvAnyRechargeModule.prefab
Assets/Prefabs/UI/FtvExchange/FtvExchangeGoodItem.prefab
Assets/Prefabs/UI/FtvExchange/FtvExchangeModule.prefab
Assets/Prefabs/UI/FtvInvest/FtvInvestModule.prefab
Assets/Prefabs/UI/FtvShop/FtvShopModule.prefab
Assets/Prefabs/UI/GhostWalk/GhostWalkModule.prefab
Assets/Prefabs/UI/GodBeast/GodBeastModule.prefab
Assets/Prefabs/UI/GodBefall/GodBefallEquipItem.prefab
Assets/Prefabs/UI/GodBefall/GodBefallEquipmentItem.prefab
Assets/Prefabs/UI/GodBefall/GodBefallModule.prefab
Assets/Prefabs/UI/GodCourt/GodCourtModule.prefab
Assets/Prefabs/UI/Guild/GuildDepotItem.prefab
Assets/Prefabs/UI/Guild/GuildModule.prefab
Assets/Prefabs/UI/HolySeal/HolySealModule.prefab
Assets/Prefabs/UI/HolyTerritory/HolyTerritoryModule.prefab
Assets/Prefabs/UI/Invest/InvestModule.prefab
Assets/Prefabs/UI/Jewel/JewelModule.prefab
Assets/Prefabs/UI/KfGroupBuy/KfGroupBuyModule.prefab
Assets/Prefabs/UI/KfSingleRank/KfSingleRankModule.prefab
Assets/Prefabs/UI/LevelReward/LevelRewardModule.prefab
Assets/Prefabs/UI/Longlanguage/LonglanguageModule.prefab
Assets/Prefabs/UI/Market/MarketPlzShowItem.prefab
Assets/Prefabs/UI/NameVerify/NameVerifyModule.prefab
Assets/Prefabs/UI/PetEquip/PetEquipItem.prefab
Assets/Prefabs/UI/PetEquip/PetEquipOutItem.prefab
Assets/Prefabs/UI/Rune/RuneModule.prefab
Assets/Prefabs/UI/Suit/EquipSuitAwardItem.prefab
Assets/Prefabs/UI/Suit/EquipSuitCostItem.prefab
Assets/Prefabs/UI/Suit/SuitModule.prefab
Assets/Prefabs/UI/Task/TaskModule.prefab
```

## 使用形态分组与代表

- 普通奖品/材料/列表格：`BaseAwardItem`；本轮代表为 `BagModule/BagItemRenderer`。
- 已穿戴/重装备格：`EquipmentItem`；本轮代表为 `BagEquipmentIcon`。
- 两类共享详情：`CommonModule` 内嵌 `BaseAwardItem/EquipmentItem`；本轮两类都验。
- 页面明确 opt-in 的槽位特效：`SuitModule/EquipSuitPosItem`；只验边缘槽与中央所有权隔离。
- 其余同形态消费者不逐页打开；代表失败时再扩同组，公共序列化字段/API 被删改时才全量核对。
