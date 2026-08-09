using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>装备吞噬配置：config_equip_fusion 与 config_equip_fusion_attr。</summary>
    public static class BagFusionConfigs
    {
        private static JObject _fusion;
        private static JObject _levels;
        private static Task _loading;

        public static Task EnsureLoaded()
        {
            if (_fusion != null && _levels != null) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        private static async Task LoadAsync()
        {
            _fusion = await Load("config_equip_fusion");
            _levels = await Load("config_equip_fusion_attr");
        }

        private static async Task<JObject> Load(string name)
        {
            TextAsset asset = await ResManager.LoadOptionalAsync<TextAsset>(GameResPath.GetServerConfigPath(name));
            if (asset == null)
            {
                GameLog.Error("Bag", "装备吞噬缺配置 {0}，界面不允许发送无法预校验的 15025", name);
                return new JObject();
            }
            JObject value = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return value;
        }

        public static bool TryGetFusionExp(int typeId, out long exp)
        {
            exp = 0;
            if (!(_fusion?[typeId.ToString()] is JObject row)) return false;
            return long.TryParse(row["2"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out exp);
        }

        public static long GetLevelNeed(int level)
        {
            if (!(_levels?[level.ToString()] is JObject row)) return 0;
            return long.TryParse(row["exp_need"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
                ? value : 0;
        }

        public static string GetLevelAttrs(int level) =>
            (_levels?[level.ToString()] as JObject)?["attr_list"]?.ToString() ?? "";

        /// <summary>解析熔炼累计属性，格式为 [{"0":attrId,"1":value},...]。</summary>
        public static IReadOnlyList<(int attrId, long value)> GetLevelAttrValues(int level)
        {
            var result = new List<(int attrId, long value)>();
            string raw = GetLevelAttrs(level);
            if (string.IsNullOrWhiteSpace(raw)) return result;
            try
            {
                foreach (JToken token in JArray.Parse(raw))
                {
                    if (!(token is JObject item)) continue;
                    int attrId = item.Value<int?>("0") ?? 0;
                    long value = item.Value<long?>("1") ?? 0L;
                    if (attrId > 0) result.Add((attrId, value));
                }
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                GameLog.Warn("Bag", "熔炼属性配置解析失败 level={0}: {1}", level, e.Message);
            }
            return result;
        }

        public static void Clear()
        {
            _fusion = null;
            _levels = null;
            _loading = null;
        }
    }
}
