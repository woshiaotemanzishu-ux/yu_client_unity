using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 公会核心一期配置读取器(自动循环 轮13a,轮13b 扩展仓库/宝箱/协助/神像四族):
    ///   · config_guild_lv.json(server,数字键 id)      —— 结社等级表(member_capacity/growth_val_limit)
    ///   · config_guild_pos.json(server,数字键 position) —— 职位表(name/permission_list/num,5档)
    ///   · config_guild_donate.json(server,数字键 donate_type) —— 捐献档位(UI 不建,数据层留存)
    ///   · config_guild_skill.json(server,数字键 skill_id)     —— 技能基础表
    ///   · config_guild_skill_research.json(server,具名键 "skillId@lv") —— 技能研究表
    ///   · config_guild_constant.json(server,数字键 id) —— 结社通用常量 KV(GetKv(index) 按 id 取 val)
    ///   · config_guild_welcome.json(server,数字键 id)  —— 入会欢迎语模板
    ///   · ConfigGuild.json(client)                     —— 主界面按钮(main_func,轮13b 已接"仓库"入口)
    ///   · config_guild_depot_score.json(server,具名键 "stage@star@color") —— 仓库积分兑换表(轮13b;
    ///     已与服务端 data_guild_depot.erl 逐条核对:117条=Stage4-16×Star1-3×Color3-5笛卡尔积全覆盖,
    ///     数值/兜底[0,0]完全一致;老端侦察报告"约48条"系约数,实测满格117条,以本核对为准)
    ///   · config_guild_daily.json(server,数字键 task_id) —— 宝箱任务表(轮13b;已与 data_guild_daily.erl
    ///     逐条核对:7任务id[1001-1007]/key/max_num/persist=4 完全一致)
    ///   · config_guild_assist.json(server,具名键 "type@sub_type") —— 协助开放条件表(轮13b;已与
    ///     data_guild_assist.erl 逐条核对:8个type+sub_type组合/condition/rewards 完全一致)
    ///   · config_guild_god*.json ×7(server,轮13b 神像/铭文;均已与 data_guild_god.erl 逐条核对一致):
    ///     god(数字键 god_id,4神)/god_color(具名键"god_id@color")/god_lv(具名键"god_id@lv")/
    ///     god_kv(具名键 key)/god_rune(数字键 goods_id,25条覆盖81010031-81010060段)/
    ///     god_rune_combo(具名键"god_id@combo_id",4神×4组合=16条)/
    ///     god_rune_achievement(具名键"god_id@need_lv",4神×[10,7,5,3]=16条)
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
        private static JObject _depotScore;
        private static JObject _daily;
        private static JObject _assist;
        private static JObject _god;
        private static JObject _godColor;
        private static JObject _godLv;
        private static JObject _godKv;
        private static JObject _godRune;
        private static JObject _godRuneCombo;
        private static JObject _godRuneAchievement;

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
            _depotScore = await LoadServer("config_guild_depot_score");
            _daily = await LoadServer("config_guild_daily");
            _assist = await LoadServer("config_guild_assist");
            _god = await LoadServer("config_guild_god");
            _godColor = await LoadServer("config_guild_god_color");
            _godLv = await LoadServer("config_guild_god_lv");
            _godKv = await LoadServer("config_guild_god_kv");
            _godRune = await LoadServer("config_guild_god_rune");
            _godRuneCombo = await LoadServer("config_guild_god_rune_combo");
            _godRuneAchievement = await LoadServer("config_guild_god_rune_achievement");
            GameLog.Info("Guild", "GuildConfigs 加载: lv={0} pos={1} donate={2} skill={3} skillResearch={4} constant={5} welcome={6} mainFunc={7} "
                + "depotScore={8} daily={9} assist={10} god={11} godColor={12} godLv={13} godKv={14} godRune={15} godRuneCombo={16} godRuneAchievement={17}",
                _lv.Count, _pos.Count, _donate.Count, _skill.Count, _skillResearch.Count, _constant.Count, _welcome.Count,
                (_clientGuild["main_func"] as JArray)?.Count ?? 0,
                _depotScore.Count, _daily.Count, _assist.Count, _god.Count, _godColor.Count, _godLv.Count, _godKv.Count,
                _godRune.Count, _godRuneCombo.Count, _godRuneAchievement.Count);
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

        /// <summary>客户端 ConfigGuild.json 的 main_func 按钮定义(轮13b 已接"仓库"入口,其余维持 TODO)。</summary>
        public static JArray MainFunc => _clientGuild?["main_func"] as JArray;

        // ==================== 轮13b:仓库积分兑换表(具名键 "stage@star@color") ====================

        /// <summary>仓库积分表按 stage@star@color 取行(score_plus/score_cost;未覆盖组合服务端兜底[0,0],
        /// 本地同样返回 null 由调用方按 0 处理)。</summary>
        public static JObject GetDepotScore(int stage, int star, int color) => _depotScore?[stage + "@" + star + "@" + color] as JObject;

        // ==================== 轮13b:宝箱任务表(数字键 task_id) ====================

        public static JObject GetDailyTask(int taskId) => _daily?[taskId.ToString(CultureInfo.InvariantCulture)] as JObject;

        // ==================== 轮13b:协助开放条件表(具名键 "type@sub_type") ====================

        public static JObject GetAssistCfg(int type, int subType) => _assist?[type + "@" + subType] as JObject;

        // ==================== 轮13b:神像/铭文(7张,均按服务端 data_guild_god.erl 逐值核对) ====================

        /// <summary>神像基础表(数字键 god_id,4神:仁/义/智/勇)。</summary>
        public static JObject GetGod(int godId) => _god?[godId.ToString(CultureInfo.InvariantCulture)] as JObject;

        public static JObject GetGodKv(string key) => _godKv?[key] as JObject;

        /// <summary>神像品级表(具名键 "god_id@color")。</summary>
        public static JObject GetGodColor(int godId, int color) => _godColor?[godId + "@" + color] as JObject;

        /// <summary>神像等级表(具名键 "god_id@lv")。</summary>
        public static JObject GetGodLv(int godId, int lv) => _godLv?[godId + "@" + lv] as JObject;

        /// <summary>铭文表(数字键 goods_id,25条覆盖81010031-81010060段)。</summary>
        public static JObject GetGodRune(long goodsId) => _godRune?[goodsId.ToString(CultureInfo.InvariantCulture)] as JObject;

        /// <summary>铭文组合表(具名键 "god_id@combo_id",4神×4组合)。</summary>
        public static JObject GetGodRuneCombo(int godId, int comboId) => _godRuneCombo?[godId + "@" + comboId] as JObject;

        /// <summary>铭文大师成就表(具名键 "god_id@need_lv",need_lv∈[3,5,7,10])。</summary>
        public static JObject GetGodRuneAchievement(int godId, int needLv) => _godRuneAchievement?[godId + "@" + needLv] as JObject;
    }
}
