using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// config_rush_rank(服务端配置)访问器。对标老端 Config.PRELOAD_SERVER_CONFIG.config_rush_rank。
    /// 用于:① 头号玩家该请求哪些 rank_type(22501);② 判定某 rank_type 当前是否处于展示时间窗。
    /// 注:该 json 需作为 Addressable 导入到 Assets/GameRes/resource/config/server/config_rush_rank.json
    /// (与 config_scene 同组)。未导入则 All 为空,头号玩家图标不出(不崩)。
    /// </summary>
    public static class RushRankConfigs
    {
        public sealed class RushRankCfg
        {
            public int Id;
            public string Name = "";
            public int StartDay;
            public int ClearDay;
            public long OpenStartTime;
            public long OpenEndTime;
        }

        private static Dictionary<int, RushRankCfg> _byId;

        public static bool Loaded => _byId != null;

        public static async Task EnsureLoaded()
        {
            if (_byId != null) return;
            _byId = new Dictionary<int, RushRankCfg>();

            TextAsset asset = await ResManager.LoadAsync<TextAsset>(
                GameResPath.GetServerConfigPath("config_rush_rank"));
            if (asset == null)
            {
                GameLog.Info("TopPlayer", "config_rush_rank 未导入,头号玩家图标不显示(非错误,功能待资源齐备)");
                return;
            }

            JObject jo = JObject.Parse(asset.text);
            ResManager.Release(asset);

            foreach (KeyValuePair<string, JToken> kv in jo)
            {
                if (!(kv.Value is JObject o)) continue;
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                _byId[id] = new RushRankCfg
                {
                    Id = id,
                    Name = (string)o["name"] ?? "",
                    StartDay = ReadInt(o, "start_day"),
                    ClearDay = ReadInt(o, "clear_day"),
                    OpenStartTime = ReadLong(o, "open_start_time"),
                    OpenEndTime = ReadLong(o, "open_end_time"),
                };
            }
        }

        public static IReadOnlyDictionary<int, RushRankCfg> All => _byId;

        public static RushRankCfg Get(int rankType)
            => _byId != null && _byId.TryGetValue(rankType, out RushRankCfg v) ? v : null;

        private static int ReadInt(JObject o, string k)
            => o[k] != null && int.TryParse(o[k].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

        private static long ReadLong(JObject o, string k)
            => o[k] != null && long.TryParse(o[k].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;
    }
}
