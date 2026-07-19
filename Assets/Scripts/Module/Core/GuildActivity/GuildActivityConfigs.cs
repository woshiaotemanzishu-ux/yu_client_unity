using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.GuildActivity
{
    /// <summary>
    /// 公会晚宴(自动循环 轮22 PK1)配置读取器——2 张表,均从 yu_client cdn/resource/config/{client|server}/
    /// 原样拷入(与 MarriageConfigs 同规格):
    ///   · ConfigGuildAct(client)5 组:fire_pos_cfg(22条,数组,无id,火苗点位xy)/fire_cfg(3条,数字键=火苗颜色id)/
    ///     evening_intro_cfg(3条,数组,阶段介绍文案)/evening_stage_cfg(6条,数字键=阶段id)/
    ///     evening_main_cfg(3条,数组,主界面介绍文案)。
    ///   · config_guild_activity_gift(server)5条,数字键=id——公会活跃度礼包表(activity=阈值门槛,
    ///     reward=奖励JSON字符串,icon=图标名)。
    /// **跳过 config_gfeast**(服务端 data_guild_feast.erl 35+ key 的 get_cfg 常量对客户端导出版,数字键=key,
    /// 老端 EveningAnswerView/EveningFoodItem/EveningFoodView 等用它取答题次数/龙魂消耗/菜肴消耗等 UI 层参数)——
    /// 本轮 26 号 wire 全部自描述定长/对象列表,解码不依赖该表,故未拉取;UI/答题题库尾包接线时需要补(GuildActivityController
    /// 类注释亦有记录)。答题题库/龙魂表/积分排行奖励表(GuildActCfgType.EVENING_QUIZ/EVENING_DRAGON/EVENING_QUIZ_REWARD
    /// 对应 "Guildreplytitle"/"Guildfeastdragon"/"Guildpointrank")本轮未定位到具体存储位置,同 r22 侦察稿存疑项,留尾包。
    /// </summary>
    public static class GuildActivityConfigs
    {
        private static JObject _fireCfg;
        private static JObject _eveningStageCfg;
        private static JArray _firePosCfg;
        private static JArray _eveningIntroCfg;
        private static JArray _eveningMainCfg;
        private static JObject _gift;

        public static bool IsLoaded => _fireCfg != null;

        public static async Task EnsureLoaded()
        {
            if (_fireCfg != null) return;
            JObject act = await LoadClient("ConfigGuildAct");
            _firePosCfg = act["fire_pos_cfg"] as JArray ?? new JArray();
            _fireCfg = act["fire_cfg"] as JObject ?? new JObject();
            _eveningIntroCfg = act["evening_intro_cfg"] as JArray ?? new JArray();
            _eveningStageCfg = act["evening_stage_cfg"] as JObject ?? new JObject();
            _eveningMainCfg = act["evening_main_cfg"] as JArray ?? new JArray();
            _gift = await LoadServer("config_guild_activity_gift");
            GameLog.Info("GuildActivity", "GuildActivityConfigs 加载: firePos={0} fireCfg={1} intro={2} stage={3} main={4} gift={5}",
                _firePosCfg.Count, _fireCfg.Count, _eveningIntroCfg.Count, _eveningStageCfg.Count, _eveningMainCfg.Count, _gift.Count);
        }

        private static async Task<JObject> LoadClient(string cfg)
        {
            string key = GameResPath.GetClientConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("GuildActivity", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("GuildActivity", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>fire_pos_cfg 单条(数组,0-based index,无 id 字段)。</summary>
        public sealed class FirePosRow { public int X; public int Y; }

        public static FirePosRow GetFirePos(int index)
        {
            if (_firePosCfg == null || index < 0 || index >= _firePosCfg.Count) return null;
            if (!(_firePosCfg[index] is JObject row)) return null;
            return new FirePosRow { X = ReadInt(row, "x"), Y = ReadInt(row, "y") };
        }

        /// <summary>fire_cfg 单行(数字键=火苗颜色id)。</summary>
        public sealed class FireCfgRow { public int Id; public string Color = ""; public string Effect = ""; }

        public static FireCfgRow GetFireCfg(int id)
        {
            if (!(_fireCfg?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new FireCfgRow { Id = id, Color = ReadString(row, "color"), Effect = ReadString(row, "effect") };
        }

        /// <summary>evening_intro_cfg 单条(数组,阶段介绍文案;答题/消消乐双语言字段 name2/icon2/content2 可能缺省)。</summary>
        public sealed class EveningIntroRow
        {
            public int Id;
            public string Icon = "";
            public string Name = "";
            public string Content = "";
            public string Name2 = "";
            public string Icon2 = "";
            public string Content2 = "";
        }

        public static EveningIntroRow GetEveningIntro(int index)
        {
            if (_eveningIntroCfg == null || index < 0 || index >= _eveningIntroCfg.Count) return null;
            if (!(_eveningIntroCfg[index] is JObject row)) return null;
            return new EveningIntroRow
            {
                Id = ReadInt(row, "id"), Icon = ReadString(row, "icon"), Name = ReadString(row, "name"),
                Content = ReadString(row, "content"), Name2 = ReadString(row, "name2"),
                Icon2 = ReadString(row, "icon2"), Content2 = ReadString(row, "content2"),
            };
        }

        /// <summary>evening_stage_cfg 单行(数字键=阶段id)。</summary>
        public sealed class EveningStageRow
        {
            public int Id;
            public string Icon = "";
            public string Name = "";
            public bool ShowBg;
            public int Alpha;
            public bool ShowIcon;
        }

        public static EveningStageRow GetEveningStage(int id)
        {
            if (!(_eveningStageCfg?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new EveningStageRow
            {
                Id = id, Icon = ReadString(row, "icon"), Name = ReadString(row, "name"),
                ShowBg = ReadBool(row, "showbg"), Alpha = ReadInt(row, "alpha"), ShowIcon = ReadBool(row, "show_icon"),
            };
        }

        /// <summary>evening_main_cfg 单条(数组,主界面介绍文案,content 为多行字符串数组)。</summary>
        public sealed class EveningMainRow
        {
            public int Id;
            public string Icon = "";
            public string Name = "";
            public readonly List<string> Content = new List<string>();
        }

        public static EveningMainRow GetEveningMain(int index)
        {
            if (_eveningMainCfg == null || index < 0 || index >= _eveningMainCfg.Count) return null;
            if (!(_eveningMainCfg[index] is JObject row)) return null;
            var r = new EveningMainRow { Id = ReadInt(row, "id"), Icon = ReadString(row, "icon"), Name = ReadString(row, "name") };
            if (row["content"] is JArray arr)
            {
                foreach (JToken t in arr) r.Content.Add(t?.ToString() ?? "");
            }
            return r;
        }

        /// <summary>config_guild_activity_gift 单行(数字键=id;reward 为原样 JSON 字符串,调用方按需再解析)。</summary>
        public sealed class GiftRow
        {
            public int Id;
            public long Activity;
            public string RewardRaw = "[]";
            public string Icon = "";
        }

        public static GiftRow GetGift(int id)
        {
            if (!(_gift?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new GiftRow
            {
                Id = id, Activity = ReadInt(row, "activity"), RewardRaw = ReadRaw(row, "reward"), Icon = ReadString(row, "icon"),
            };
        }

        public static int FirePosCount => _firePosCfg?.Count ?? 0;
        public static int FireCfgCount => _fireCfg?.Count ?? 0;
        public static int EveningIntroCount => _eveningIntroCfg?.Count ?? 0;
        public static int EveningStageCount => _eveningStageCfg?.Count ?? 0;
        public static int EveningMainCount => _eveningMainCfg?.Count ?? 0;
        public static int GiftCount => _gift?.Count ?? 0;

        // ---------- JSON 读取小工具(同 MarriageConfigs/BossConfigs 套路,自成一份不跨模块耦合) ----------

        private static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static bool ReadBool(JObject obj, string key)
        {
            if (obj == null) return false;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return false;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            return ReadInt(obj, key) != 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        private static string ReadRaw(JObject obj, string key) => ReadString(obj, key);
    }
}
