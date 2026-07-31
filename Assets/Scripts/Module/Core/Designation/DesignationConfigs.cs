using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>称号基础配置 config_dsgt。</summary>
    public static class DesignationConfigs
    {
        public sealed class Attr
        {
            public int Id;
            public long Value;
        }

        public sealed class Row
        {
            public uint Id;
            public string Name = "";
            public string Description = "";
            public string ResourceId = "";
            public int Location;
            public int OrderLimit;
            public readonly List<Attr> Attrs = new List<Attr>();
        }

        private static readonly List<Row> Rows = new List<Row>();
        private static JObject _config;
        private static Task _loading;

        public static IReadOnlyList<Row> All => Rows;

        public static Task EnsureLoaded()
        {
            if (_config != null) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAsync());
        }

        public static Row Get(uint id)
        {
            for (int i = 0; i < Rows.Count; i++)
                if (Rows[i].Id == id) return Rows[i];
            return null;
        }

        private static async Task LoadAsync()
        {
            string key = GameResPath.GetServerConfigPath("config_dsgt");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Designation", "称号配置缺失: {0}", key);
                _loading = null;
                return;
            }

            _config = JObject.Parse(asset.text);
            Rows.Clear();
            foreach (JProperty property in _config.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                var parsed = new Row
                {
                    Id = (uint)ReadLong(row, "id"),
                    Name = ReadString(row, "name"),
                    Description = ReadString(row, "description"),
                    ResourceId = ReadString(row, "resource_id"),
                    Location = (int)ReadLong(row, "location"),
                    OrderLimit = (int)ReadLong(row, "order_limit"),
                };
                ParseAttrs(ReadString(row, "attr_list"), parsed.Attrs);
                Rows.Add(parsed);
            }
            Rows.Sort((a, b) => a.Location.CompareTo(b.Location));
            ResManager.Release(asset);
        }

        private static void ParseAttrs(string raw, List<Attr> target)
        {
            if (string.IsNullOrEmpty(raw) || raw == "0" || raw == "[]") return;
            try
            {
                JArray list = JArray.Parse(raw);
                foreach (JToken token in list)
                {
                    if (!(token is JObject row)) continue;
                    target.Add(new Attr
                    {
                        Id = (int)(row["0"]?.Value<long>() ?? 0L),
                        Value = row["1"]?.Value<long>() ?? 0L,
                    });
                }
            }
            catch
            {
                // 单条坏配置不阻断整个称号页。
            }
        }

        private static long ReadLong(JObject row, string key)
        {
            JToken token = row?[key];
            if (token == null || token.Type == JTokenType.Null) return 0L;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            return long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long value)
                ? value
                : 0L;
        }

        private static string ReadString(JObject row, string key)
            => row?[key]?.ToString() ?? string.Empty;
    }
}
