# MainUI 活动图标 · 加载逻辑移植清单

> 由 workflow 研究老端 41 个活动系统自动生成(icon_type/标签/落排/协议/Unity移植状态)。
> 展示逻辑=读配置(等级/开服天数/任务条件)+ 收服务端协议(活动开没开)决定加载哪些图标;UI 容器已就绪。

## 结论
- 活动网格系统 29 个:✅1 已移植 / 🟡17 部分 / ❌11 未移植。
- Secondary(loc4/5/6/7,不在活动网格)系统 12 个,单列在下方。
- 配置里 291/312 图标是 `controll_by_own_fun`(靠协议),纯配置图标仅 ~8–11 落网格;图1 现在显示的就是配置图标 + 已移植系统。
- **确定 bug**:`241`(百鬼集)/`241@1@0`(活动日历) 老端强制搬进网格第4排、无视 loc5/6;Unity 的 ShouldOwnActivityIcon/GroupFor 没这条 → 进不了网格。


## 已移植(图标优先)进度
- ✅ **Festival 祭典(223)** — 范式模板,逐字节验证。
- ✅ **批次1(8个,已编译)**:GrowthBenefits 成长福利(41720)、FriendInvite 分享(340,⚠️受 ShareOpen 客户端渠道开关控制,默认关→需置真才显)、SurpriseGift 惊喜礼包(490)、TopVip 至尊Vip(451)、DragonBall 龙玉(143)、SevenDay 七天登录(175)、PushGift 礼包推送(191)、Adventure 天天冒险(42701/42702)。
- 均从「图标优先」起步：活动开着才显示，点击弹「功能待接入」；个别条件近似(GrowthBenefits 开启等级135硬编码/战力福利分支TODO、TopVip vip>=4&lv>=160)。TopVip 后续已补45101/02/04全量和45109-12读侧通知/状态，但面板、红点及付费领奖操作仍未迁移。


## 批次2/3/动态族(已编译)
- ✅ **批次2(8)**:Kaifu(巅峰投资4205/超值投资1112/契约之书424)、AddVipService(关注有礼114,⚠️渠道白名单默认关)、DiamondFight(灵玉大战137)、Kf1vn(跨服1vn 621)、SeaHegemony(四海争霸/海域18601)、KfHolyArea(神陨禁区284)、Lung(神纹熔炉181)、BaseDungeon(限时爬塔331@97)。
- ✅ **批次3(6)**:Market(市场151/跨服市场151@1)、LimitLevelShop(限时抢购61201)、Eyou(Facebook666/注销777,⚠️平台开关默认关)、Boss(节日大妖51)、ActivityForeshow(活动预告135@0@1等)、Banquet(婚礼172@1/宾客172@2)。
- ✅ **动态族(3)**:**CustomActivity 331@\* 主列表修复**(缓存33101+等级/任务变化重评→解决一次性下发导致整族不显;+补 节日投资331@62@1/红包返利331@117@0 直接分支)、**Compete 338@\* 竞榜**(33800驱动100+变体)、**Attention 关注**(113/113113,⚠️平台开关默认关)。
- 合计本轮约 **26 个系统**移植,全部编译通过(exit 0)。⚠️ 渠道/平台开关驱动的图标(分享340/Eyou/AddVipService/Attention)默认关→原生包不显,需置真才现;是忠实移植。

## 一、活动网格系统(loc1/2/3/10 + 241强制)—— 要填满图2网格的主体

| 状态 | 系统 | 图标(icon_type=标签,loc) | 关键协议 | Unity 现状 |
|---|---|---|---|---|
| ❌未移植 | AdventureModel | `42701`=天天冒险(loc1)<br>`42702`=天天冒险(loc1) | 42700, 42701, 42702, 42703, 42704, 42705, 42706 | Only auto-generated LayaUI prefab bind stubs exist under e:/GitProject/yu_client_unity/Assets/Scripts/Generated/UI/Adventure/ (AdventureMain |
| ❌未移植 | BaseDungeonModel | `331@97`=限时爬塔(loc2) | 61117, 61118 | Only auto-generated Laya→Unity prefab-bind stubs exist for the tower VIEW: Assets/Scripts/Generated/UI/DungeonTower/DungeonTowerViewBind.cs  |
| ❌未移植 | EyouManager | `666`=Facebook(loc2)<br>`777`=注销账号(loc1) | — | No EyouManager/controller logic ported (no addIcon for 666/777, no IsShowFB/SetFbState/InitCancelAccount, no EyouDataPlacement analytics). O |
| ❌未移植 | LimitLevelShopModel | `61201`=限时等级抢购1(loc2)<br>`61202`=限时等级抢购2(loc2)<br>`61203`=限时等级抢购3(loc2)<br>`61204`=限时等级抢购4(loc2)<br>`61205`=超值推荐(loc3)<br>`61206`=龙语抢购(loc2)<br>`61207`=圣衣抢购(loc2)<br>`61208`=圣衣抢购(loc2)<br>`61209`=圣衣抢购(loc2) | 61200, 61201, 61203 | Only auto-generated prefab bind stubs exist: Assets/Scripts/Generated/UI/LimitLevelShop/LimitLevelShopViewBind.cs, LimitLevelShopTabItemBind |
| ❌未移植 | LungModel (神纹熔炉/Lung stove-crucible icon) | `181`=神纹熔炉(loc2) | 18100 stove/lung data SetLungData, 18101 upgrade, 18102 wear, 18103 take-off, 18104 decompose, 18105 stove data SetStoveData, 18106 stove summon, 18107 stage reward | none — no logic port. Only auto-generated LayaUI prefab bind shells exist: Assets/Scripts/Generated/UI/Common/LungToolTipsBind.cs and LungSt |
| ❌未移植 | MarketModel | `151`=市场(loc1)<br>`151@1`=跨服市场(loc1) | 15100, 15101, 15102, 15104, 15105, 15106, 15108, 15109 | No MarketModel/MarketController/showIcon logic in Assets/Scripts; icon_type 151 and 151@1 are not referenced anywhere (grep found none). Onl |
| ❌未移植 | PushGiftModel | `191`=礼包推送(loc2) | 19101, 19102, 19103, 19104 | Not ported. Only an auto-generated prefab bind exists: Assets/Scripts/Generated/UI/MainUI/GiftPushIconBind.cs (source cdn/resource/game/main |
| ❌未移植 | QuestionNaireModel | `90@331`=问卷调查(loc2) | 33104, 33190, 33191, base_type 90 | none — A CustomActivity framework exists in Unity (Assets/Scripts/Module/Core/CustomActivity/CustomActivityController.cs, CustomActivityConf |
| ❌未移植 | SevenDayController | `175`=七天登录(loc2)<br>`175@8`=14天登录(loc2)<br>`175_1`=合服七天登录(loc2) | 17500, 17501, 17502, 17503 | none — no SevenDay/SevenDayModel/SevenDayView, no icon 175/175@8/175_1 handler, and no 17500-17503 protocol handler under Assets/Scripts (th |
| 🟡部分 | TopVipController | `451`=至尊Vip(loc2) | 45101, 45102, 45103, 45104, 45105, 45106, 45107, 45108, 45109, 45110, 45111, 45112 | Icon门槛与45101/02/04全量、45109-12读侧通知/状态已接；面板/红点及45103/05领奖、45106/07购买、45108权益领奖未接。TopVipShopItem仅有生成Bind，商城type10商品仍保存在ShopModel专槽。 |
| ❌未移植 | VipModel | `450@1`=直升V4(loc2) | 45000, 45003, 45004, 45001, 45002, 45005, 45006, 45007 | Assets/Scripts/Module/Core/Vip/VipController.cs + VipModel.cs exist but are an explicit "minimal slice" that only registers Proto.RECHARGE_P |
| 🟡部分 | AddVipServiceModel | `114`=关注有礼(loc2) | 15908 | Only an auto-generated data-only view stub exists: Assets/Scripts/Generated/UI/AddVipService/AddVipServiceViewBind.cs (BaseView node binding |
| 🟡部分 | AttentionModel (关注 / 收藏小程序) | `113`=收藏小程序(loc3)<br>`113113`=关注(loc2) | none — AttentionController.RegisterAllProtocals | Only auto-generated scaffolding exists, no runtime logic. Config: Assets/Scripts/Generated/Config/ConfigClientAttention.cs + AttentionCfg.cs |
| 🟡部分 | CompeteListModel | `338@{type}@{subtype} (DYNAMIC: icon = Tr`=竞榜/竞赛榜 race-activity ico(loc2) | 33800 request open race-activities, 33801 request activity view/interface info, 33802 request rank list, 33803 lottery/抽奖, 33804 claim stage, 33807 mystery-key count | Only auto-generated LayaUI->Unity prefab bind stubs exist: Assets/Scripts/Generated/UI/Competelist/{CompetelistViewBind, CompetelistIntegral |
| 🟡部分 | CustomActivityController | `331@62@1`=节日投资(loc2)<br>`331@117@0`=红包返利(loc1)<br>`331@121`=累充有礼(loc1)<br>`331@10@0`=头号玩家(loc1) | 33101, 33211, 33255, 33104, 33136, 22501/22502, 22503/22504/22505, 33100-33267 handler block | Assets/Scripts/Module/Core/CustomActivity/CustomActivityController.cs (113 lines) ports ONLY the 33101 master loop: On33101 -> ApplyActivity |
| 🟡部分 | CustomActivityModel | `331@<base_type>@<show_id>`=自定义活动图标族 (ICON_KEY=331; (loc2)<br>`331@10@0`=头号玩家(loc1)<br>`331@117@0`=红包返利(loc1)<br>`331@115@0`=每日直购(loc2)<br>`331@7@1`=每日累充(loc2)<br>`331@6@0`=每日首充·首充图标(loc2)<br>`331@129@0`=仙灵直购(loc2)<br>`331@62@1`=节日投资(loc2)<br>`331@36@0`=0元礼包(loc2)<br>`331@3`=开服活动(loc2)<br>`331@4`=周末狂欢(loc2)<br>`331@113`=每日登陆(loc2) | 33101, 33102, 33103, 33100, 33104/33105/33106/33108, 33112-33116, 33118, 33120-33123, 33136-33138, 33168/33169, 33185-33196, 22500-22505, 15955-15960 | Partial port present. Core icon-list path ported: Assets/Scripts/Module/Core/CustomActivity/CustomActivityController.cs (registers only Prot |
| 🟡部分 | DragonBallModel | `143`=龙玉(loc3) | 14300, 14301, 14302, 14303, 14304, 14305, 14306, 14310 | Only auto-generated prefab Bind shells exist, no logic/model/controller/icon wiring. Present: Assets/Scripts/Generated/UI/DragonBall/{Dragon |
| ✅已移植(图标) | FestivalController (宝录/祭典 Festival Pass) | `223`=祭典(loc2) | 194, 19400, 19401, 19402, 19403, 19404, 19405 | Only generated UI prefab-bind scaffolding exists: Assets/Scripts/Generated/UI/Festival/ (12 *Bind.cs — FestivalTaskView/FestivalLevelAwardVi |
| 🟡部分 | FriendInviteController | `340`=好友邀请(loc2) | 34000, 34001, 34002, 34003, 34004, 34005, 34006, 34007 | Only auto-generated prefab binder stubs exist under Assets/Scripts/Generated/UI/FriendInvite/ (12 *Bind.cs: FriendInviteView/ShopView/Welfar |
| 🟡部分 | GrowthBenefitsModel | `41720`=成长福利(loc2) | 41720, 41721, 41722, 41723, 41724 | Only auto-generated LayaUI->Unity prefab bind scaffolding exists: Assets/Scripts/Generated/UI/GrowthBenefits/{GrowthBenefitsViewBind,GrowthB |
| 🟡部分 | KaifuActivityModel | `424`=契约之书(loc3)<br>`424@1`=契约之书(loc3)<br>`424@2`=(loc-1)<br>`4205`=巅峰投资(loc2)<br>`1112`=超值投资·投资计划(loc2) | 42000, 42001, 42002, 42003, 42004, 42400, 42401, 42402 | No KaifuActivityModel/Controller ported. Only auto-generated prefab-binding stubs exist under Assets/Scripts/Generated/UI/: Invest/{LVinvest |
| 🟡部分 | PrayController | `415`=祈愿(loc2) | 41500, 41501, 41502 | Only UI view shells exist: Assets/Scripts/Module/Core/Pray/Views/PrayMainView.cs and PrayItemView.cs (data-only stubs, OnShow logs a TODO; c |
| 🟡部分 | RuneTreasureController | `416`=夺宝(loc1) | 41600, 41601, 41603, 41604, 41605, 41606, 41607, 41608 | Only auto-generated LayaUI->Unity prefab bind shells exist: Assets/Scripts/Generated/UI/RuneTreasure/RuneTreasureMainViewBind.cs, RuneTreasu |
| 🟡部分 | SurpriseGiftController | `490`=惊喜礼包(loc2) | 49000, 49001, 49002, 49003, 49004 | Protocols + data model ported: Assets/Scripts/Module/Core/SurpriseGift/SurpriseGiftController.cs (RegisterProtocal 49000-49004 via Proto.SUR |
| 🟡部分 | SurpriseGiftModel | `490`=惊喜礼包(loc2) | 49000 info, 49001 draw/抽奖, 49002 turn/翻牌, 49003 buy/购买, 49004 refresh push | Assets/Scripts/Module/Core/SurpriseGift/SurpriseGiftController.cs + SurpriseGiftModel.cs exist (registered in ControllerHub.cs line 65). Pro |
| 🟡部分 | VipController | `450@1`=直升V4(loc2)<br>`158@0`=充值(loc7)<br>`158@3`=充值X3倍(loc7)<br>`160`=爱微游_svip(loc4) | register 45000, 45001, 45002, 45003, 45004, 45005, 45006, 45007 | Assets\Scripts\Module\Core\Vip\VipController.cs + VipModel.cs port only the recharge-icon slice: RegisterProtocal 15800/15801, VipModel.Have |
| 🟡部分 | WeekCardController | `452`=周卡(loc2) | 45201, 45202, 45203, server pt_452 | e:/GitProject/yu_client_unity/Assets/Scripts/Module/Core/WeekCard/WeekCardController.cs + WeekCardModel.cs. Protocol handlers 45201/45202/45 |
| 🟡部分 | WelfareController | `331@3`=开服活动(loc2)<br>`331@3_1`=开服活动（和331@3相同，仅icon_type(loc2) | 41700, 41701, 41703, 41704, 41705, 41707, 41708, 41715 | 等级礼包切片(协议 41700/41701)已移植到 Assets/Scripts/Module/Core/RushGift/(RushGiftController.cs / RushGiftModel.cs / RushGiftShellView.cs,注释明写"对标老端 co |
| ✅已移植 | SvipMainController | `45120`=SVIP(loc2) | 45120 | Fully ported under Assets/Scripts/Module/Core/Svip/: SvipController.cs (ICON_TYPE="45120"; Register->RegisterProtocal(Proto.SVIP_INFO=45120, |

## 二、Secondary 非网格系统(loc4/5/6/7 → MainUISecondaryView)

| 状态 | 系统 | 图标 | 关键协议 | Unity 现状 |
|---|---|---|---|---|
| ❌未移植 | ActivityForeshowManager | `135@0@1`=活动日历*九魂圣殿(loc6)<br>`135@0@2`=活动日历*九魂圣殿(loc6)<br>`652@31@0`=活动日历*领地夺宝(loc6) | 65208 | Manager NOT ported — no ActivityForeshowManager / foreshow-icon logic in Assets/Scripts. Only downstream UI bind stubs exist: Assets/Scripts |
| ❌未移植 | BanquetController | `172@2`=宾客管理(loc6) | 17249, 17250, 17251, 17252, 17253, 17256, 17257, 17258 | No BanquetController and no Banquet dir under Assets/Scripts/Module/Core. No 17249-17298 protocol registration and no ActivityIconManager.Ad |
| ❌未移植 | BanquetModel | `172@1`=婚礼(loc6)<br>`172@2`=宾客管理(loc6) | 17249, 17250, 17251, 17252, 17253, 17256, 17257, 17258 | Only a data-only stub exists: Assets/Scripts/Module/Core/Marriage/Views/MarriageMainView.cs (plus MarriageBootstrap.cs / MarriageFlow.cs). T |
| ❌未移植 | BossModel | `51`=节日大妖·怪物攻城图标(loc6) | 46003, 33104, 46000-46046, 47000-47117, 20025, 20026, 20201-20205, 61900-61902 | No BossModel/BossController and no FeastBossActivity/AddIcon("51") logic ported. Only generated prefab scaffolding exists: Assets/Scripts/Ge |
| ❌未移植 | GuildActivityModel | — | 40200, 40201, 40203, 40204, 40208, 40209, 40211, 40212 | Only auto-generated UI skeleton binds exist: Assets/Scripts/Generated/UI/Evening/*Bind.cs (EveningMainView/EveningBossView/EveningAnswer*/Ev |
| ❌未移植 | OnHookAdditionTips | — | 13211, 13212, 13213, 13214, 13215, 13216, 13217, 13218 | Assets/Scripts/Module/Core/OnHook/ exists but only base system: OnHookController.cs + OnHookShellView.cs (partial onHook port). No OnHookAdd |
| ❌未移植 | SeaHegemonyController | `18601`=四海争霸(loc6) | 18600-18626, 18650-18656, 18700-18715 | Only an auto-generated LayaUI->Unity view bind stub exists: Assets/Scripts/Generated/UI/SeaHegemony/SeaFightViewBind.cs (data-only BaseView  |
| 🟡部分 | DiamondFightController | `137`=灵玉大战(loc6) | 13700, 13701, 13702, 13703, 13704, 13705, 13706, 13707 | Only auto-generated prefab view-binder scaffolds exist under Assets\Scripts\Generated\UI\DiamondFight\ (14 files: DiamondFightWaitingViewBin |
| 🟡部分 | FirstRechargeController | `159`=首充(loc4) | 15905, 15906, 15907, 15908 | Assets/Scripts/Module/Core/FirstRecharge/FirstRechargeController.cs + FirstRechargeModel.cs exist. Ported: protocol handlers 15905/15906/159 |
| 🟡部分 | Kf1vnController | `621`=跨服1vn(loc6) | 62100 stage/page_info, 62101 stage push -> ShowIcon, 62102, 62103, 62104, 62105, 62107, 62108 | Only auto-generated prefab bind stubs exist: 26 files under Assets/Scripts/Generated/UI/Kf1vn/ (Kf1vnEnterViewBind, Kf1vnShopViewBind, Kf1vn |
| 🟡部分 | KfHolyAreaModel | `284`=神陨禁区(loc5) | 28400, 28401, 28403, 28404, 28405, 28406, 28407, 28408 | Only auto-generated UI binding stubs exist: Assets/Scripts/Generated/UI/KfHolyArea/*Bind.cs (KfHolyAreaMainViewBind, SceneViewBind, BuildMsg |
| 🟡部分 | MainStrongerController | `158`=变强(loc5) | 30000 | Views ported as data-only downgraded stubs: Assets/Scripts/Module/Core/MainStronger/Views/MainUIStrongerView.cs, MainUIStrongerTalkBoard.cs, |
