using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyStageConfigs
    {
        public sealed class StageCfg { public int Type; public int Stage; public int Level; public int ExpCon; }
        private static Dictionary<string, StageCfg> _byKey;
        private static Task _loading;
        public static bool IsLoaded => _byKey != null;

        public static Task EnsureLoaded()
        {
            if (_byKey != null) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }
        public static StageCfg Get(int type, int stage, int level)
            => _byKey != null && _byKey.TryGetValue(Key(type, stage, level), out StageCfg cfg) ? cfg : null;
        public static StageCfg GetNext(int stage, int level)
            => Get(2, stage, level + 1) ?? Get(2, stage + 1, 1);

        private static async Task LoadAsync()
        {
            var byKey = new Dictionary<string, StageCfg>();
            string key = GameResPath.GetServerConfigPath("config_baby_stage");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby stage config: {0}", key);
                _byKey = byKey;
                return;
            }
            try
            {
                JObject root = JObject.Parse(asset.text);
                foreach (KeyValuePair<string, JToken> pair in root)
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new StageCfg { Type = ReadInt(row, "type"), Stage = ReadInt(row, "stage"), Level = ReadInt(row, "level"), ExpCon = ReadInt(row, "exp_con") };
                    if (cfg.Type > 0 && cfg.Stage > 0 && cfg.Level > 0) byKey[Key(cfg.Type, cfg.Stage, cfg.Level)] = cfg;
                }
            }
            catch (System.Exception e) { GameLog.Warn("Baby", "parse baby stage config failed: {0}", e.Message); }
            finally { ResManager.Release(asset); }
            _byKey = byKey;
        }
        private static string Key(int type, int stage, int level) => type + "@" + stage + "@" + level;
        private static int ReadInt(JObject row, string key)
            => int.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }
}
