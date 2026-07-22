using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.DragonBall
{
    public static class DragonBallConfigs
    {
        public sealed class Row { public int Id; public int OpenLevel; public int OpenDay; public int TimesLimit; }
        private static readonly Dictionary<int, Row> Rows = new Dictionary<int, Row>();
        private static Task _loading;
        public static bool IsLoaded { get; private set; }
        public static int Count => Rows.Count;
        public static int MinimumOpenLevel { get; private set; }
        public static Task EnsureLoaded() => IsLoaded ? Task.CompletedTask : (_loading ?? (_loading = LoadAsync()));
        private static async Task LoadAsync()
        {
            string key = GameResPath.GetServerConfigPath("config_start_nuclear");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            Rows.Clear(); MinimumOpenLevel = 0;
            if (asset == null) { GameLog.Error("DragonBall", "missing config_start_nuclear: {0}", key); IsLoaded = true; return; }
            JObject root = JObject.Parse(asset.text);
            foreach (JProperty p in root.Properties())
            {
                if (!(p.Value is JObject o)) continue;
                int id = o.Value<int?>("id") ?? 0;
                if (id <= 0) continue;
                var row = new Row { Id = id, OpenLevel = o.Value<int?>("open_lv") ?? 0, OpenDay = o.Value<int?>("open_day") ?? 0, TimesLimit = o.Value<int?>("times_limit") ?? 0 };
                Rows[id] = row;
                if (row.OpenLevel > 0 && (MinimumOpenLevel == 0 || row.OpenLevel < MinimumOpenLevel)) MinimumOpenLevel = row.OpenLevel;
            }
            ResManager.Release(asset); IsLoaded = true;
            GameLog.Info("DragonBall", "config_start_nuclear={0}", Rows.Count);
        }
        public static Row Get(int id) => Rows.TryGetValue(id, out Row row) ? row : null;
        public static bool HasOpenLevel(int level)
        {
            foreach (Row row in Rows.Values) if (row.OpenLevel == level) return true;
            return false;
        }
    }
}
