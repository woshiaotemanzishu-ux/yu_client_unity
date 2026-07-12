using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 公会核心一期配置读取器(自动循环 轮13a):
    ///   · config_guild_lv.json(server,数字键 id)      —— 结社等级表(member_capacity/growth_val_limit)
    ///   · config_guild_pos.json(server,数字键 position) —— 职位表(name/permission_list/num,5档)
    ///   · config_guild_donate.json(server,数字键 donate_type) —— 捐献档位(UI 不建,数据层留存)
    ///   · config_guild_skill.json(server,数字键 skill_id)     —— 技能基础表
    ///   · config_guild_skill_research.json(server,具名键 "skillId@lv") —— 技能研究表
    ///   · config_guild_constant.json(server,数字键 id) —— 结社通用常量 KV(GetKv(index) 按 id 取 val)
    ///   · config_guild_welcome.json(server,数字键 id)  —— 入会欢迎语模板
    ///   · ConfigGuild.json(client)                     —— 主界面按钮(main_func,本轮未接线,登记供以后消费)
    /// 均从 yu_client cdn/resource/config/{server,client}/ 原样拷入 Assets/GameRes/resource/config/{server,client}/
    /// (与既有 ShopConfigs/DailyConfigs 同规格——具名/数字键 JSON,不需 ErlangParser 解外层)。
    /// </summary>
    public static class GuildConfigs
    {
        private static JObject _lv;
        private static JObject _pos;
        private static JObject _donate;
        private static JObject _skill;
        private static JObject _skillResearch;
        private static JObject _constant;
        private static JObject _welcome;
        private static JObject _clientGuild;

        public static bool IsLoaded => _lv != null;

        public static async Task EnsureLoaded()
        {
            if (_lv != null) return;
            _lv = await LoadServer("config_guild_lv");
            _pos = await LoadServer("config_guild_pos");
            _donate = await LoadServer("config_guild_donate");
            _skill = await LoadServer("config_guild_skill");
            _skillResearch = await LoadServer("config_guild_skill_research");
            _constant = await LoadServer("config_guild_constant");
            _welcome = await LoadServer("config_guild_welcome");
            _clientGuild = await LoadClient("ConfigGuild");
            GameLog.Info("Guild", "GuildConfigs 加载: lv={0} pos={1} donate={2} skill={3} skillResearch={4} constant={5} welcome={6} mainFunc={7}",
                _lv.Count, _pos.Count, _donate.Count, _skill.Count, _skillResearch.Count, _constant.Count, _welcome.Count,
                (_clientGuild["main_func"] as JArray)?.Count ?? 0);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Guild", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        private static async Task<JObject> LoadClient(string cfg)
        {
            string key = GameResPath.GetClientConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Guild", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>结社等级表按 lv 取行(member_capacity/growth_val_limit/upgrade_desc)。</summary>
        public static JObject GetLv(int lv) => _lv?[lv.ToString(CultureInfo.InvariantCulture)] as JObject;

        /// <summary>职位表按 position(1会长/2副会长/3会员/4宝贝/5精英)取行。</summary>
        public static JObject GetPosition(int position) => _pos?[position.ToString(CultureInfo.InvariantCulture)] as JObject;

        /// <summary>捐献档位(UI 未建,数据层留存供以后接线)。</summary>
        public static JObject GetDonate(int donateType) => _donate?[donateType.ToString(CultureInfo.InvariantCulture)] as JObject;

        public static JObject GetSkill(int skillId) => _skill?[skillId.ToString(CultureInfo.InvariantCulture)] as JObject;

        public static JObject GetSkillResearch(int skillId, int lv) => _skillResearch?[skillId + "@" + lv] as JObject;

        /// <summary>结社通用常量 KV(对标老端 GetGuildKVCfg(index)):按 id 取 val 字符串。
        /// 已知下标:3=公告字数上限/8=结社名字数上限/12/13=建社消耗档位(绑玉/金币)/16/20=开探索等级门槛。</summary>
        public static string GetKv(int index)
        {
            JObject row = _constant?[index.ToString(CultureInfo.InvariantCulture)] as JObject;
            return row?["val"]?.ToString() ?? "";
        }

        public static JObject GetWelcome(int id) => _welcome?[id.ToString(CultureInfo.InvariantCulture)] as JObject;

        /// <summary>客户端 ConfigGuild.json 的 main_func 按钮定义(本轮未接线,登记供以后消费)。</summary>
        public static JArray MainFunc => _clientGuild?["main_func"] as JArray;
    }
}
