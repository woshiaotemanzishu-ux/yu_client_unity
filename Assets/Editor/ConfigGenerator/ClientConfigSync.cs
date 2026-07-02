using System.IO;
using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools.ConfigGen
{
    /// <summary>
    /// 客户端散表 JSON 同步:yu_client cdn/resource/config/client/{Name}.json
    /// → Assets/GameRes/resource/config/client/{name 小写}.json(地址=小写约定)。
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
        };

        // 登录链路用到的服务端表(头像=config_dress_up_cfg type5 的 screen 字段)
        private static readonly string[] SYNC_LIST_SERVER =
        {
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
            "config_mount_constant",  // OutWard 常量(开放等级等,具名键)
            "config_mount_stage",     // 坐骑/外观阶配置(具名键,含 max_star;系统A)
            "config_mount_star",      // 星配置(⚠数字键,列序 config_table_default: type_id/stage/star/max_blessing/attr/combat/clear_status)
            "config_mount_level",     // 系统B等级经验表(具名键,~12000 行)
            "config_mount_goods",     // 培养道具(具名键)
            "config_mount_prop",      // 道具经验换算(具名键)
            "config_temple_awaken_kv",        // 天命觉醒 KV(前置任务/等级门槛;主线 100590)
            "config_equip_stren_lv",          // 强化消耗+属性(⚠数字键,列序 part/stren/object_list/attr_list,主键 "part@stren")
            "config_equip_stren_lv_key",      // 强化等级索引
            "config_equip_strengthen_max",    // 强化上限(主键 "stage@color@pos")
            "config_enchantment_guard_soap",        // 古宝本体(soap_id/soap_name/condition;主线 100811 幽瞳)
            "config_enchantment_guard_soap_debris", // 古宝碎片(主键 "soap@debris":cost/attr)
            "config_dungeon",         // 副本配置(⚠数字键,39 列序在 config_table_default;御魂本 12001~,主线 100980/101522)
            "config_goods_compose",   // 神装合成规则(⚠数字键,列序见 config_table_default;type==2=装备类;主线 101725 ctype73)
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
                string src = Path.Combine(srcDir, name + ".json");
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
