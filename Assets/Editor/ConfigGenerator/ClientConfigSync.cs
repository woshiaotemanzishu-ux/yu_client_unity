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
