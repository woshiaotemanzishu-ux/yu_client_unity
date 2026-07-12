using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Rank
{
    /// <summary>
    /// 排行榜配置读取器(自动循环 轮12 #12):
    ///   · config_ranking.json(server,15条) —— rank_type 枚举权威表(show/排序/上限/门槛),对标老端
    ///     RankModel.ts:129 spiritTabDataList() 消费源。
    ///   · config_medal.json(server,约131条) —— 勋章样式表,本轮只导表+基础访问器(id/title/add_attr/
    ///     medal_start/upgrade_power),勋章渲染(No.1 头像旁 _img_medal/title_gp)留 UI 尾包,不在本轮解析。
    /// 均从 yu_client cdn/resource/config/server/ 原样拷入 Assets/GameRes/resource/config/server/
    /// (与既有 ShopConfigs/DailyConfigs 同规格——具名/数字键 JSON,不需 ErlangParser 解外层)。
    /// </summary>
    public static class RankConfigs
    {
        private static JObject _ranking;
        private static JObject _medal;

        public static bool IsLoaded => _ranking != null;

        public static async Task EnsureLoaded()
        {
            if (_ranking != null) return;
            _ranking = await LoadServer("config_ranking");
            _medal = await LoadServer("config_medal");
            GameLog.Info("Rank", "RankConfigs 加载: ranking={0} medal={1}", _ranking.Count, _medal.Count);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Rank", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>config_ranking 单条(对标老端 RankModel.RANK_TYPE 枚举行)。</summary>
        public sealed class RankTypeCfg
        {
            public int Type;
            public string RankName = "";
            /// <summary>服务端榜单条目上限(截断上限,来自 data_ranking.erl:战力榜(200)=100/结社(100)=20
            /// (22102 已死仅存档)/其余 12 种个人榜=50)。轮12 blocker 修复后本字段是 22101 分页续拉的
            /// 总量上界(RankController.RequestRank/On22101 消费),不再是纯展示字段。</summary>
            public int RankMax;
            /// <summary>入榜门槛值(竞技榜等按"数值越小越好"反向比较,本表只存原始数值,比较方向留 UI 层)。</summary>
            public int RankLimit;
            public int TitleId;
            public int SortId;
            /// <summary>1=显示该 tab,0=隐藏(当前 15 条里 4 条隐藏:结社100/飞骑602/精灵2代605/爬塔609)。</summary>
            public int Show;
        }

        public static RankTypeCfg GetByType(int type)
        {
            if (!(_ranking?[type.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new RankTypeCfg
            {
                Type = type,
                RankName = ReadString(row, "rank_name"),
                RankMax = ReadInt(row, "rank_max"),
                RankLimit = ReadInt(row, "rank_limit"),
                TitleId = ReadInt(row, "title_id"),
                SortId = ReadInt(row, "sortid"),
                Show = ReadInt(row, "show"),
            };
        }

        /// <summary>show==1 且按 sortid 升序(对标老端 spiritTabDataList())。本轮无 UI 消费,仅供
        /// CliVerify 断言与未来 UI 尾包直接读取,不在本类内做任何渲染相关处理。</summary>
        public static List<RankTypeCfg> GetVisibleSorted()
        {
            var result = new List<RankTypeCfg>();
            if (_ranking == null) return result;
            foreach (JProperty prop in _ranking.Properties())
            {
                if (!(prop.Value is JObject row)) continue;
                if (ReadInt(row, "show") != 1) continue;
                if (!int.TryParse(prop.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)) continue;
                RankTypeCfg cfg = GetByType(type);
                if (cfg != null) result.Add(cfg);
            }
            result.Sort((a, b) => a.SortId.CompareTo(b.SortId));
            return result;
        }

        /// <summary>config_medal 原始行(本轮仅访问器,不解析 add_attr/cost 等 Erlang term 字符串字段——
        /// 勋章渲染留 UI 尾包时再按需用 <see cref="Shenxiao.Framework.Net.ErlangParser"/> 解一层)。</summary>
        public static JObject GetMedal(int id) => _medal?[id.ToString(CultureInfo.InvariantCulture)] as JObject;

        public static int MedalCount => _medal?.Count ?? 0;

        // ---------- JSON 读取小工具(数字索引/字符串键混排容错,同 ShopConfigs/DailyConfigs 套路) ----------

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
    }
}
