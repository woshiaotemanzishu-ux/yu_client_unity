using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// Boss 家族一期·本服核心(自动循环 轮15a)配置读取器——6 张地基表,均从
    /// yu_client cdn/resource/config/server/ 原样拷入 Assets/GameRes/resource/config/server/
    /// (与既有 RankConfigs/ShopConfigs 同规格,数字键扁平 JSON,不需 ErlangParser 解外层):
    ///   · config_boss_type(17条,缺11/feast·15·17/domainserver三行是源数据事实,不补造)——boss_type 级配置
    ///     (日限次数/最大怒气/疲劳上限/condition 等级门槛)。
    ///   · config_boss_cfg(206条)——单个 boss 实例(场景/坐标/复活消耗/hurt_limit 归属门槛/首杀奖励等)。
    ///   · config_boss_type_key_value(92条,复合键 "boss_type@key")——KV 补充表。
    ///   · config_boss_show_hp(68条,主键 scene)——哪些场景显示 boss 血条。
    ///   · config_domain_kill_reward(3条)——秘境领域阶段奖励档位。
    ///   · config_decoration_boss(25条)——幻域/特殊 boss 装饰配置(复活提示等)。
    /// </summary>
    public static class BossConfigs
    {
        private static JObject _bossType;
        private static JObject _bossCfg;
        private static JObject _bossTypeKv;
        private static JObject _showHp;
        private static JObject _domainKillReward;
        private static JObject _decorationBoss;

        public static bool IsLoaded => _bossType != null;

        public static async Task EnsureLoaded()
        {
            if (_bossType != null) return;
            _bossType = await LoadServer("config_boss_type");
            _bossCfg = await LoadServer("config_boss_cfg");
            _bossTypeKv = await LoadServer("config_boss_type_key_value");
            _showHp = await LoadServer("config_boss_show_hp");
            _domainKillReward = await LoadServer("config_domain_kill_reward");
            _decorationBoss = await LoadServer("config_decoration_boss");
            GameLog.Info("Boss", "BossConfigs 加载: type={0} cfg={1} kv={2} showHp={3} domainReward={4} decoration={5}",
                _bossType.Count, _bossCfg.Count, _bossTypeKv.Count, _showHp.Count, _domainKillReward.Count, _decorationBoss.Count);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Boss", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>config_boss_type 单行(对标服务端 #boss_type{} / 老端 Config.PRELOAD_SERVER_CONFIG.config_boss_type)。</summary>
        public sealed class BossTypeCfg
        {
            public int BossType;
            public string BossName = "";
            public int Count;       // 每日次数上限(0=不限)
            public int DailyId;
            public int MaxAnger;
            public int Tired;       // 疲劳上限
            /// <summary>condition 原始 Erlang 式 JSON 串([{"0":"lv","1":N}]),等级门槛等——留原串,
            /// 调用方按需用 <see cref="Shenxiao.Framework.Net.ErlangParser"/> 或直接 JArray 解析。</summary>
            public string Condition = "[]";
        }

        public static BossTypeCfg GetBossType(int bossType)
        {
            if (!(_bossType?[bossType.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new BossTypeCfg
            {
                BossType = bossType,
                BossName = ReadString(row, "bossname"),
                Count = ReadInt(row, "count"),
                DailyId = ReadInt(row, "daily_id"),
                MaxAnger = ReadInt(row, "max_anger"),
                Tired = ReadInt(row, "tired"),
                Condition = ReadRaw(row, "condition"),
            };
        }

        /// <summary>config_boss_cfg 单行(单个 boss 实例核心字段;完整 206 条各含更多字段,本轮仅登记
        /// 消费点用得到的场景/坐标/名称/归属门槛,其余字段调用方按需从原始 JObject 直读)。</summary>
        public sealed class BossCfgRow
        {
            public int BossId;
            public int Type;      // boss_type
            public int Scene;
            public int X;
            public int Y;
            public int Layers;
            public int HurtLimit; // 伤害份额归属门槛(百分比,达标才算参与结算——非"点死归属")
            public int TiredAdd;
        }

        public static BossCfgRow GetBossCfg(int bossId)
        {
            if (!(_bossCfg?[bossId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new BossCfgRow
            {
                BossId = bossId,
                Type = ReadInt(row, "type"),
                Scene = ReadInt(row, "scene"),
                X = ReadInt(row, "x"),
                Y = ReadInt(row, "y"),
                Layers = ReadInt(row, "layers"),
                HurtLimit = ReadInt(row, "hurt_limit"),
                TiredAdd = ReadInt(row, "tired_add"),
            };
        }

        /// <summary>该 boss_type 下所有 boss_cfg 行(遍历全表按 type 过滤;206 条量级,不建索引也够用)。</summary>
        public static List<BossCfgRow> GetBossCfgsByType(int bossType)
        {
            var result = new List<BossCfgRow>();
            if (_bossCfg == null) return result;
            foreach (JProperty prop in _bossCfg.Properties())
            {
                if (!(prop.Value is JObject row)) continue;
                if (ReadInt(row, "type") != bossType) continue;
                if (!int.TryParse(prop.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int bossId)) continue;
                BossCfgRow r = GetBossCfg(bossId);
                if (r != null) result.Add(r);
            }
            return result;
        }

        /// <summary>config_boss_type_key_value 单值(复合键 "boss_type@key",value 原样返回字符串——
        /// 部分值本身是 JSON 数组串,调用方按需再解一层)。</summary>
        public static string GetTypeKv(int bossType, string key)
        {
            if (_bossTypeKv == null) return null;
            string compound = bossType.ToString(CultureInfo.InvariantCulture) + "@" + key;
            return _bossTypeKv[compound] is JObject row ? ReadRaw(row, "value") : null;
        }

        /// <summary>config_boss_show_hp:场景是否在血条展示白名单内(主键=scene)。</summary>
        public static bool ShowHpInScene(int scene) => _showHp?[scene.ToString(CultureInfo.InvariantCulture)] != null;

        /// <summary>config_domain_kill_reward 单行(3档;reward_list 留原串)。</summary>
        public static JObject GetDomainKillReward(int rewardId) => _domainKillReward?[rewardId.ToString(CultureInfo.InvariantCulture)] as JObject;

        /// <summary>config_decoration_boss 单行(25条;幻域/特殊boss装饰配置,复活提示用)。</summary>
        public static JObject GetDecorationBoss(int bossId) => _decorationBoss?[bossId.ToString(CultureInfo.InvariantCulture)] as JObject;

        public static int BossTypeCount => _bossType?.Count ?? 0;
        public static int BossCfgCount => _bossCfg?.Count ?? 0;
        public static int BossTypeKvCount => _bossTypeKv?.Count ?? 0;
        public static int ShowHpCount => _showHp?.Count ?? 0;
        public static int DomainKillRewardCount => _domainKillReward?.Count ?? 0;
        public static int DecorationBossCount => _decorationBoss?.Count ?? 0;

        // ---------- JSON 读取小工具(同 RankConfigs/ShopConfigs 套路) ----------

        public static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        public static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        /// <summary>原样返回字符串(condition/value 等可能自身是 JSON 数组串,不在这里二次解析)。</summary>
        public static string ReadRaw(JObject obj, string key) => ReadString(obj, key);
    }
}
