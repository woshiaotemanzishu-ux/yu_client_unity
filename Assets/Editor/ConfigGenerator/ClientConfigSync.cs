using System.IO;
using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools.ConfigGen
{
    /// <summary>
    /// 客户端散表 JSON 同步:yu_client cdn/resource/config/client/{Name}.json
    /// → Assets/GameRes/resource/config/client/{name 小写}.json(地址=小写约定)。
    /// ConfigFunctionIcon 例外:它的 location_type 是老端界面布局语义,以 h5/laya/assets 源表为准；
    /// cdn 成品表会把第四排入口二次改写到 loc5/6,不能用来还原老端 MainUI 容器归属。
    /// 这些表结构不规则(嵌套 dict/数组),不走 ConfigGenerator 的强类型生成,
    /// 运行时由 LoginConfigs 等按 JObject 读取。新链路要用新表 → 往 SYNC_LIST 加一行。
    /// </summary>
    public static class ClientConfigSync
    {
        // 登录链路用到的客户端表(与运行时 LoginConfigs 的 key 保持一致)
        private static readonly string[] SYNC_LIST =
        {
            "ConfigLogin",
            "ConfigModelAni",
            "ConfigRandomName",
            "ConfigPreloadResList",
            "UIModelParameter",
            "SceneObjectParticle",
            "ConfigFunctionIcon",
            "ConfigTaskArrow",
            "ConfigFuncOpenCondition",
            "ConfigNotNormalGoods", // 货币/经验映射(type→goods_id,如 3→31 金币、5→32 经验),GoodsModel.GetMappingTypeId 用
            "ConfigItemAttr",       // 物品属性 id→名(attr_id→name,如 1→攻击),GoodsModel.GetAttrName 用(对标 WordManager.GetProperties)
            "ConfigSkillUI",        // 职业技能快捷栏配置(carrerSkillList[career]→[{skill_id,common,stren}]),SkillUIConfigs/MainUISkillView shortcutList 用
            "ConfigCareerSkillMovies",
            "ConfigMonsterSkillMovies",
            "ConfigOtherFightInfo",
            "ConfigCustomActivity",
            "ConfigCustomActivityShow",
            "ConfigCustomActivityView",
            "ConfigAutoBrush",
            "ClientTransfer",       // 转职卡展示文案(career→{name,desc1,desc2}),轮5 TransferJobModel 用
            "ClientShopConfig",     // 商城二级子页签定义(ShopSeries[shop_type]=[{id,desc}],仅灵玉/善缘两类型有),轮11 ShopConfigs 用
            "ConfigGuild",          // 结社主界面按钮定义(main_func,9行;本轮未接线,GuildConfigs.MainFunc 登记供以后消费),轮13a
            "ClientGodBefall",      // GodBefall(降神/谪仙临凡)客户端展示表,自动循环 轮18 便宜活批 PK1
            "ConfigGodBefall",      // GodBefall 客户端配置(与 ClientGodBefall 配套),轮18 PK1
            "ClientConfigFiestaITaskTab", // Festival 任务页签客户端展示表,轮18 PK3
            "ClientConfigFiestaIntroduce", // Festival 介绍文案表,轮18 PK3
            "ClientFirstBloodShow", // FirstBlood 客户端展示表,轮18 PK3
            "ClientRedBagOpen",     // RedPacket 客户端展示表,轮18 PK2
            "ClientVipWelfare",     // Welfare/VIP 客户端展示表,轮18 PK4
            "ConfigGuildAct",       // 公会晚宴(GuildActivity,pt_402)客户端展示表:fire_pos_cfg/fire_cfg/
                                    // evening_intro_cfg/evening_stage_cfg/evening_main_cfg 共5组,自动循环 轮22 PK1
            "ConfigConstellation",  // 星宿(StarEquip)客户端专属星图坐标表(star_point_cfg,UI 用),轮23 PK1
        };

        // 登录链路用到的服务端表(头像=config_dress_up_cfg type5 的 screen 字段)
        private static readonly string[] SYNC_LIST_SERVER =
        {
            "config_attr_medicament", // 属性药剂物品→等级权威表(pt_217)
            "config_attr_medicament_use_count", // 属性药剂等级段日/总次数权威表(pt_217)
            "config_baby_equip", // 宝宝装备 goods_id→pos/stage/color/skills 穿戴门禁
            "config_enchantment_guard_boss",
            "config_enchantment_guard_stage_reward",
            "config_dress_up_cfg",
            "config_task",
            "config_scene",
            "config_npc",   // NPC 身份/默认对话 id(name/title/talk/icon/icon_scale/brith_rot),对话与场景 NPC 名牌都要
            "config_talk",  // NPC 对话内容表(content 为 JSON 串,按 talk_id 查),DialogueController 12101/12102 用
            "config_goods", // 物品基础表(数字索引键 "1"=名/"14"=图标/"18"=品质;"9"/"10" 是 type/subtype),GoodsModel 解析奖励真实物品名/图标
            "GoodsType",        // 物品大类 type→type_name(如 10→装备),GoodsModel.GetGoodsTypeName 用(对标 WordManager.GetGoodsStyle)
            "config_equip_attr", // 装备配置(type_id→{stage 阶/star 星/base_rating 评分/recommend_attr/other_attr}),装备 tips 基础属性用
            "config_skill",     // 技能总表(skill_id→{name/career/type/is_normal/lv_data});SkillConfigs 取技能名/等级图标(lv_data[lv-1].icon),21002 过滤合法技能
            "config_mon",       // 怪物配置(数字索引键 "1"=名/"10"=monster_res 模型资源/"11"=icon_scale 缩放),MonsterConfigs 取场景怪名牌/缩放(对标老端 Config.config_mon)
            "config_companion",       // 剑魄同修本体(id→名/形象/解锁),PartnerModel 用(主线 100190 培养任务;对标老端 PartnerModel cfg)
            "config_companion_stage", // 同修阶星配置(培养消耗/属性成长;对标 PartnerModel config_companion_stage)
            "config_companion_kv",    // 同修杂项 kv(对标 PartnerModel cfg_kv)
            "config_suit_clt",        // 套装收集(主键 {suit_id}@{career},具名键;主线 100391;⚠不在列序表内,按字段名直读)
            "config_suit_clt_process",// 套装阶段属性(主键 {suit_id}@{suit_stage},具名键)
            "config_rush_giftbag",    // 冲级豪礼(主键 bag_lv,具名键;主线 100420 领 35 级)
            "config_start_nuclear",   // 龙珠礼包 14311 图标门槛(id/open_lv/open_day/times_limit)
            "config_mount_constant",  // OutWard 常量(开放等级等,具名键)
            "config_mount_stage",     // 坐骑/外观阶配置(具名键,含 max_star;系统A)
            "config_mount_star",      // 星配置(⚠数字键,列序 config_table_default: type_id/stage/star/max_blessing/attr/combat/clear_status)
            "config_mount_level",     // 系统B等级经验表(具名键,~12000 行)
            "config_mount_goods",     // 培养道具(具名键)
            "config_mount_prop",      // 道具经验换算(具名键)
            "config_mount_figure",       // 幻化可激活形象列表(主键"type_id@id@career",320条),轮24 PI OutWardConfigs.GetFigureRow 用
            "config_mount_figure_stage", // 幻化升阶配置(主键"type_id@id@stage",5730条),轮24 PI OutWardConfigs.GetFigureStageRow 用
            "config_mount_figure_star",  // 幻化升星配置(主键"type_id@id@star",3232条,老端 upStarCfg),轮24 PI OutWardConfigs.GetFigureStarRow 用
            "config_mount_skill",        // 幻化技能配置(主键"type_id@skill_id",144条),轮24 PI OutWardConfigs.GetSkillRow 用
            "config_pet_equip_pos",      // 坐骑/伙伴装备槽位基础(8条),轮25A PetEquipConfigs
            "config_pet_equip_pos_lv",   // 装备槽位强化等级(2408条)
            "config_pet_equip_stage",    // 装备打磨阶段(1600条)
            "config_pet_equip_star",     // 装备打磨星级(1600条)
            "config_pet_equip_goods",    // 装备物品静态属性(120条)
            "config_temple_awaken_kv",        // 天命觉醒 KV(前置任务/等级门槛;主线 100590)
            "config_equip_stren_lv",          // 强化消耗+属性(⚠数字键,列序 part/stren/object_list/attr_list,主键 "part@stren")
            "config_equip_stren_lv_key",      // 强化等级索引
            "config_equip_strengthen_max",    // 强化上限(主键 "stage@color@pos")
            "config_enchantment_guard_soap",        // 古宝本体(soap_id/soap_name/condition;主线 100811 幽瞳)
            "config_enchantment_guard_soap_debris", // 古宝碎片(主键 "soap@debris":cost/attr)
            "config_dungeon",         // 副本配置(⚠数字键,39 列序在 config_table_default;御魂本 12001~,主线 100980/101522)
            "config_goods_compose",   // 神装合成规则(⚠数字键,列序见 config_table_default;type==2=装备类;主线 101725 ctype73)
            "config_career",          // 转职合法(career,sex)组合("career@sex" 具名键),轮5 TransferJobModel 用
            "config_shop",                // 服务端权威商品表(241条,具名键 key_id),轮11 ShopConfigs.GetShopCfgRow 用(15301 已直接下发过滤后数据,本表暂无直接消费点,仅登记供以后至尊VIP/喇叭跳转复用)
            "config_limit_shop_config",   // 抢购(64000/64001)静态数据(42条,具名键 id),轮11 ShopConfigs.GetVieData 用
            "config_mystery_shop_good",   // 神秘/神纹商店格子静态配置(352条,具名键 id),轮11 ShopConfigs.GetMysteryGoodCfg 用
            "config_mystery_shop_hit",    // 神秘/神纹商店刷新消耗区间表(7条,具名键 id),轮11 ShopConfigs.GetRefreshCfg 用
            "config_quick_buy_price",     // QuickBuyView 速购单价表(35条,具名键 goods_type_id),轮11 ShopConfigs.GetQuickBuyPrice 用(UI 未接壳)
            "config_guild_prestige",      // 结社头衔购买条件文案(11条,具名键 title_id),轮11 ShopConfigs.GetGuildPrestige 用
            "config_ranking",   // 排行榜 rank_type 枚举权威表(15条,数字键 type),轮12 RankConfigs.GetByType/GetVisibleSorted 用
            "config_medal",     // 勋章样式表(约131条,数字键 id),轮12 RankConfigs.GetMedal 用(本轮仅导表+访问器,渲染留UI尾包)
            "config_guild_lv",            // 结社等级表(数字键 id;member_capacity/growth_val_limit),轮13a GuildConfigs.GetLv 用
            "config_guild_pos",           // 结社职位表(数字键 position,5档:会长/副会长/会员/宝贝/精英),轮13a GuildConfigs.GetPosition 用
            "config_guild_donate",        // 结社捐献档位(数字键 donate_type;UI 未建,数据层留存),轮13a GuildConfigs.GetDonate 用
            "config_guild_skill",         // 结社技能基础表(数字键 skill_id),轮13a GuildConfigs.GetSkill 用
            "config_guild_skill_research", // 结社技能研究表(具名键 "skillId@lv"),轮13a GuildConfigs.GetSkillResearch 用
            "config_guild_constant",      // 结社通用常量KV(数字键 id;公告字数/结社名字数/建社消耗档位等),轮13a GuildConfigs.GetKv 用
            "config_guild_welcome",       // 结社入会欢迎语模板(数字键 id),轮13a GuildConfigs.GetWelcome 用
            "config_guild_depot_score",   // 仓库积分兑换表(具名键"stage@star@color",117条),轮13b GuildConfigs.GetDepotScore 用(已与 data_guild_depot.erl 逐值核对一致)
            "config_god",                  // 谪仙临凡(GodBefall)神格基础表,自动循环 轮18 便宜活批 PK1 用
            "config_god_equip",            // GodBefall 神装配置,轮18 PK1
            "config_god_lv",               // GodBefall 升级经验表,轮18 PK1
            "config_god_stage",            // GodBefall 升阶配置,轮18 PK1
            "config_god_star",             // GodBefall 升星配置,轮18 PK1
            "config_god_kv",               // GodBefall 杂项KV,轮18 PK1
            "config_god_star_up_limit",    // GodBefall 升星上限,轮18 PK1
            "config_god_stren",            // GodBefall 神格强化(44017/44018)配置,轮18 PK1
            "config_hero_halo",            // 光环(Halo,pt_514)配置表,轮18 PK2
            "config_red_envelopes",        // 公会红包(RedPacket,pt_339)配置,轮18 PK2
            "config_red_envelopes_goods",  // 公会红包可选物品表,轮18 PK2
            "config_fairy",                // 仙灵祝福(FairyWish,pt_513)基础表(fairy_buy 数据源,非148精灵系统),轮18 PK2
            "config_fairy_node",           // FairyWish 强化节点表,轮18 PK2
            "config_boss_first_blood_plus_boss", // 首杀/首通(FirstBlood,pt_188)Boss配置,轮18 PK3
            "config_fiesta_act",           // 祭典/宝录(Festival,pt_194)活动表,轮18 PK3
            "config_fiesta_act_task",      // Festival 活动任务表,轮18 PK3
            "config_fiesta_kv",            // Festival 杂项KV,轮18 PK3
            "config_fiesta_lv_exp",        // Festival 等级经验表,轮18 PK3
            "config_fiesta_task",          // Festival 任务表,轮18 PK3
            "config_checkin_type",         // 签到(Welfare 41703-05)类型表,轮18 PK4
            "config_checkin_daily_rewards",// 签到每日奖励表,轮18 PK4
            "config_checkin_total_rewards",// 签到累计奖励表,轮18 PK4
            "config_checkin_daily_retroactive", // 签到补签配置,轮18 PK4
            "config_checkin_key_value",    // 签到杂项KV,轮18 PK4
            "config_online_reward",        // 在线福利(41715/41716)奖励表,轮18 PK4
            "config_welfare_cfg",          // Welfare 通用配置,轮18 PK4
            "config_welfare_night_reward", // Welfare 晚间奖励配置,轮18 PK4
            "config_grow_welfare_info",    // 成长福利(41722)配置,轮18 PK4
            "config_combat_welfare_reward",// 战力福利(41723/41724)奖励表,轮18 PK4
            "config_combat_welfare_times", // 战力福利摇奖次数表,轮18 PK4
            "config_xinyue_gift",          // 心悦礼包(41719)配置,轮18 PK4
            "config_real_info_reward",     // 实名认证奖励表,轮18 PK4
            "config_scene_revive_cost",    // 场景复活消耗(12083联动),轮18 PK5
            "config_guild_daily",         // 宝箱任务表(数字键 task_id,7条),轮13b GuildConfigs.GetDailyTask 用(已与 data_guild_daily.erl 逐值核对一致)
            "config_guild_assist",        // 协助开放条件表(具名键"type@sub_type",8条),轮13b GuildConfigs.GetAssistCfg 用(已与 data_guild_assist.erl 逐值核对一致)
            "config_guild_god",           // 神像基础表(数字键 god_id,4条),轮13b GuildConfigs.GetGod 用
            "config_guild_god_color",     // 神像品级表(具名键"god_id@color"),轮13b GuildConfigs.GetGodColor 用
            "config_guild_god_lv",        // 神像等级表(具名键"god_id@lv"),轮13b GuildConfigs.GetGodLv 用
            "config_guild_god_kv",        // 神像杂项KV(具名键 key:open_day/lv_limit/combo_tv_time),轮13b GuildConfigs.GetGodKv 用
            "config_guild_god_rune",      // 铭文表(数字键 goods_id,25条,81010031-81010060段),轮13b GuildConfigs.GetGodRune 用
            "config_guild_god_rune_combo",       // 铭文组合表(具名键"god_id@combo_id",16条),轮13b GuildConfigs.GetGodRuneCombo 用
            "config_guild_god_rune_achievement",  // 铭文大师成就表(具名键"god_id@need_lv",16条),轮13b GuildConfigs.GetGodRuneAchievement 用
            "config_boss_type",            // Boss家族一期(轮15a)boss_type级配置(17条,数字键;缺11/feast·15·17/domainserver三行是源数据事实),BossConfigs.GetBossType 用
            "config_boss_cfg",             // 单个boss实例(206条,数字键;场景/坐标/hurt_limit归属门槛等),BossConfigs.GetBossCfg 用
            "config_boss_type_key_value",  // KV补充表(92条,复合键"boss_type@key"),BossConfigs.GetTypeKv 用
            "config_boss_show_hp",         // 场景血条显示白名单(68条,数字键=scene),BossConfigs.ShowHpInScene 用
            "config_domain_kill_reward",   // 秘境领域阶段奖励档位(3条,数字键),BossConfigs.GetDomainKillReward 用
            "config_decoration_boss",      // 幻域/特殊boss装饰配置(25条,数字键),BossConfigs.GetDecorationBoss 用
            "config_eudemons_boss_cfg",    // Boss家族二期·跨服族(轮15b)千幻蜃楼/圣兽岭逐只配置(48条,数字键),KfBossConfigs.GetEudemonsBossCfg 用
            "config_kf_great_demon",       // 跨服"太古遗凶"专用配置(31条,数字键),KfBossConfigs.GetKfGreatDemonCfg 用
            "config_marriage_constant",    // 婚姻(轮16)通用常量表(41条,数字键 id),MarriageConfigs.GetConstant 用
            "config_ring_star",            // 戒指阶星属性表(501条,stage 1-50,复合键"stage@star";源=cdn\resource\config\server法定同步源,与服务端data_ring.erl 501+1兜底精确吻合;cdn\assets下101条为陈旧副本),MarriageConfigs.GetRingStar 用
            "config_flower_tools",         // 鲜花道具表(6条,数字键=goods_id),MarriageConfigs.GetFlowerTool 用
            "config_love_dsgt_cfg",        // 恩爱称号档位表(10条,数字键=顺位id),MarriageConfigs.GetLoveDsgt 用(config_personal_tag_info 半死跳过,见 MarriageConfigs 注释)
            "config_key_value",            // 全局杂项KV(59条,数字键=key;value 字符串形态不统一——key1 JSON数组/key11&20001 裸整数数组/key15&18 单引号渠道名列表),轮20 P5 KeyValueConfigs.GetRaw 用(41708 下载礼包明细消费 key1)
            "config_fashion",              // 时装(pt_413,41300+)基础表,主键"pos@fashion_id@star_lv"(⚠此源=cdn\resource\config\server 实测6420条/星级到200,与服务端data_fashion.erl一致;cdn\assets下1200条/20星是陈旧副本,同config_ring_star的先例),轮21 PA FashionConfigs.GetBaseRow 用
            "config_fashion_color",        // 时装染色表,主键"pos@fashion_id@color_id@star_lv"(此源15180条,同上口径),轮21 PA FashionConfigs.GetColorRow 用
            "config_fashion_model",        // 时装模型表,主键"pos@fashion_id@career@sex@color_id"(704条,两份源一致),轮21 PA FashionConfigs 预留(本轮未强依赖,留展示/第二刀用)
            "config_fashion_pos",          // 时装部位等级表,主键"pos_id@pos_lv"(3003条,3部位×0..1000级),FashionLevelView 用
            "config_fashion_suit",         // 时装套装基础表,数字键 suit_id(4条),FashionSuitView 页签/激活条件用
            "config_fashion_suit_star",    // 时装套装升阶表,主键"suit_id@star_id"(40条,4套×1..10阶),FashionSuitView 用
            "config_guild_activity_gift",  // 公会晚宴(GuildActivity,pt_402)活跃度礼包表(数字键 id,5条),自动循环 轮22 PK1 GuildActivityConfigs.GetGift 用
            "config_constellation_equip",           // 星宿(StarEquip pt_232)装备基础表(180条,数字键 goods_id),轮23 PK1 StarEquipConfigs.GetEquipInfo 用
            "config_constellation_page",             // 星宿套装页表(5条,数字键 page),轮23 PK1 StarEquipConfigs.GetPageInfo 用
            "config_constellation_compose",          // 星宿合成规则表(20条,数字键 id),轮23 PK1 StarEquipConfigs.GetComposeInfo 用
            "config_constellation_decompose",        // 星宿吞噬等级表(2000条,数字键 lv),轮23 PK1 StarEquipConfigs.GetDecomposeInfo 用
            "config_constellation_pos",               // 星宿装备位表(10条,数字键 pos),轮23 PK1 StarEquipConfigs.GetPos 用
            "config_constellation_kv",                // 星宿核心杂项KV(6条,具名键;open_lv=560/open_day_limit=0 等),轮23 PK1 StarEquipConfigs.GetKv 用
            "config_constellation_strength",          // 星宿锻造-强化表(876条,复合键"type@pos@lv"),轮23 PK2 消费,本类只加载
            "config_constellation_strength_buff",     // 星宿锻造-强化额外Buff表(15条,复合键"type@lv"),轮23 PK2 消费
            "config_constellation_strength_master",   // 星宿锻造-强化大师表(23条,复合键"type@lv"),轮23 PK2 消费
            "config_constellation_enchantment",       // 星宿锻造-附魔(觉醒)表(930条,复合键"type@pos@lv"),轮23 PK2 消费
            "config_constellation_enchantment_master", // 星宿锻造-附魔大师表(55条,复合键"type@lv"),轮23 PK2 消费
            "config_constellation_evolution",         // 星宿锻造-进化表(222条,复合键"type@pos@lv"),轮23 PK2 消费
            "config_constellation_evolution_pool",    // 星宿锻造-进化属性池表(30条,复合键"type@pos"),轮23 PK2 消费
            "config_constellation_evolution_rate",    // ⚠源表实测0条(空表存疑,见 StarEquipConfigs.GetEvolutionRateRaw 注释),轮23 PK2 占位加载
            "config_constellation_spirit",             // 星宿锻造-启灵表(30条,复合键"type@pos"),轮23 PK2 消费
            "config_constellation_forge_kv",           // 星宿锻造杂项KV(10条,数字键 id;四子系统开放等级580+类型码),轮23 PK1/PK2 共用
            "config_wedding_info",              // 婚宴(Banquet pt_172)婚礼类型主表(3条,数字键=wedding_type),轮24 PB BanquetConfigs.GetInfo 用
            "config_wedding_time",               // 婚宴预约时段表(12条,数字键=time_id;begin/end_time 为内嵌JSON字符串{"0":H,"1":M}),轮24 PB BanquetConfigs.GetTime 用
            "config_wedding_time_stage",          // 婚宴预约阶段流程表(3条,数字键=stage_id),轮24 PB BanquetConfigs.GetTimeStage 用
            "config_wedding_candies",             // 婚宴喜糖表(2条,数字键=candies_id 8002003/8002004),轮24 PB BanquetConfigs.GetCandy 用
            "config_wedding_fires",               // 婚宴烟花表(2条,数字键=fires_id 1/2),轮24 PB BanquetConfigs.GetFires 用
            "config_wedding_table",               // 婚宴餐桌菜品表(3条,数字键=table_id 8002001/02/05),轮24 PB BanquetConfigs.GetTable 用
            "config_wedding_aura",                // 婚宴气氛值奖励档位表(1条,数字键=aura_id),轮24 PB BanquetConfigs.GetAura 用
            "config_wedding_guest_position",      // 婚宴宾客坐标点位表(41条,数字键=id),轮24 PB BanquetConfigs.GetGuestPosition 用(无婚礼场景,仅加载)
            "config_wedding_position",            // 婚宴场景坐标点位表(696条,数字键=pos_id),轮24 PB BanquetConfigs.GetPosition 用(无婚礼场景,仅加载)
            "config_wedding_scene_exp_coef",       // 婚宴经验系数表(27条,复合键"wedding_type@num1@num2"),轮24 PB BanquetConfigs.GetSceneExpCoef 用
            "config_wedding_card",                 // ⚠源表实测0条(空表,对应pp_marriage.erl:1662 WeddingCardCon整行注释),轮24 PB 占位加载
            "config_wedding_scene_exp",            // ⚠源表实测0条(空表,真正在用的是config_wedding_scene_exp_coef),轮24 PB 占位加载
            "config_wedding_trouble_maker",        // ⚠源表实测0条(空表,对应killlist 17269/17274捣蛋鬼死链佐证),轮24 PB 占位加载
        };

        [MenuItem("神霄/配表/同步客户端配置(JSON)", priority = 62)]
        public static void Sync()
        {
            int ok = SyncIfStale(force: true);
            Debug.Log($"[ClientConfigSync] 强制同步 {ok} 份(client {SYNC_LIST.Length} + server {SYNC_LIST_SERVER.Length})");
        }

        private const string DST_DIR = "Assets/GameRes/resource/config/client";

        /// <summary>缺失或源更新才拷贝;返回拷贝数。进 Play 模式前自动调用(免去手动菜单步骤)。</summary>
        public static int SyncIfStale(bool force = false)
        {
            int copied = 0;
            copied += SyncDir("client", SYNC_LIST, DST_DIR, force);
            copied += SyncDir("server", SYNC_LIST_SERVER, "Assets/GameRes/resource/config/server", force);
            if (copied > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[ClientConfigSync] 自动同步配置 {copied} 份 → Assets/GameRes/resource/config");
            }
            return copied;
        }

        private static int SyncDir(string sub, string[] names, string dstDir, bool force)
        {
            string srcDir = Path.Combine(LayaUISettings.CdnResourceRoot, "config", sub);
            Directory.CreateDirectory(dstDir);
            int copied = 0;
            foreach (string name in names)
            {
                string src = sub == "client" && name == "ConfigFunctionIcon"
                    ? Path.Combine(LayaUISettings.LayaAssetsRoot, "resource", "config", "client", name + ".json")
                    : Path.Combine(srcDir, name + ".json");
                if (!File.Exists(src))
                {
                    Debug.LogError($"[ClientConfigSync] 缺源文件: {src}");
                    continue;
                }
                string dst = Path.Combine(dstDir, name.ToLowerInvariant() + ".json");
                if (!force && File.Exists(dst)
                    && File.GetLastWriteTimeUtc(dst) >= File.GetLastWriteTimeUtc(src)) continue;
                File.Copy(src, dst, true);
                copied++;
            }
            return copied;
        }
    }

    /// <summary>进 Play 模式前自动同步客户端配置(运行时链路依赖这些 JSON,不靠人记菜单)。</summary>
    [InitializeOnLoad]
    internal static class ClientConfigAutoSync
    {
        static ClientConfigAutoSync()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) ClientConfigSync.SyncIfStale();
            };
        }
    }
}
