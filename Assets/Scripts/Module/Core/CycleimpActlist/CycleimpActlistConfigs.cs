using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.CycleimpActlist
{
    /// <summary>
    /// config_cycle_rank_info(对标老端 Config.PRELOAD_SERVER_CONFIG.config_cycle_rank_info),键 "type@subtype"。
    /// 主面板用到:name(展示名)、show_id(3D 展示对象资源号,type==5 为按职业的 JSON 数组字符串)、show_type(UI_MODEL_TYPE)。
    /// json 已拷入 Assets/GameRes/resource/config/server/config_cycle_rank_info.json(打真机包前需 Addressable 自动分组)。
    /// </summary>
    public static class CycleimpActlistConfigs
    {
        public sealed class Cfg
        {
            public int Type;
            public int SubType;
            public string Name = "";
            /// <summary>展示对象资源号;type==5(神兵)是 "[1107,1107,1307,1407]" 这样的按职业数组字符串。</summary>
            public string ShowId = "";
            public int ShowType;
        }

        private static Dictionary<string, Cfg> _byKey;

        public static bool Loaded => _byKey != null;

        public static async Task EnsureLoaded()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, Cfg>();

            TextAsset asset = await ResManager.LoadAsync<TextAsset>(
                GameResPath.GetServerConfigPath("config_cycle_rank_info"));
            if (asset == null)
            {
                GameLog.Info("CycleRank", "config_cycle_rank_info 未导入,循环冲榜面板的名称/3D模型不可用");
                return;
            }

            JObject jo = JObject.Parse(asset.text);
            ResManager.Release(asset);

            foreach (KeyValuePair<string, JToken> kv in jo)
            {
                if (!(kv.Value is JObject o)) continue;
                _byKey[kv.Key] = new Cfg
                {
                    Type = ReadInt(o, "type"),
                    SubType = ReadInt(o, "sub_type"),
                    Name = (string)o["name"] ?? "",
                    ShowId = o["show_id"]?.ToString() ?? "",
                    ShowType = ReadInt(o, "show_type"),
                };
            }
        }

        public static Cfg Get(int type, int subtype)
            => _byKey != null && _byKey.TryGetValue(type + "@" + subtype, out Cfg v) ? v : null;

        private static int ReadInt(JObject o, string k)
            => o[k] != null && int.TryParse(o[k].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
    }
}
