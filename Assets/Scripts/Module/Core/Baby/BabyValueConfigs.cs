using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyValueConfigs
    {
        public sealed class StageMaterial { public int ItemId; public int ExpPerItem; }

        private static Task _loading;
        public static bool IsLoaded { get; private set; }
        public static int StageRaiseLevel { get; private set; }
        public static readonly List<StageMaterial> StageMaterials = new List<StageMaterial>();

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        private static async Task LoadAsync()
        {
            StageRaiseLevel = 0;
            StageMaterials.Clear();
            string key = GameResPath.GetServerConfigPath("config_baby_value");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby value config: {0}", key);
                IsLoaded = true;
                return;
            }
            try
            {
                JObject root = JObject.Parse(asset.text);
                StageRaiseLevel = ReadInt(root["3"] as JObject, "value");
                string raw = root["4"]?["value"]?.ToString();
                if (!string.IsNullOrEmpty(raw))
                    foreach (JToken token in JArray.Parse(raw))
                        if (token is JObject item) StageMaterials.Add(new StageMaterial
                        { ItemId = ReadInt(item, "0"), ExpPerItem = ReadInt(item, "1") });
            }
            catch (System.Exception e) { GameLog.Warn("Baby", "parse baby value config failed: {0}", e.Message); }
            finally { ResManager.Release(asset); }
            IsLoaded = true;
        }

        private static int ReadInt(JObject row, string key)
            => int.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }
}
