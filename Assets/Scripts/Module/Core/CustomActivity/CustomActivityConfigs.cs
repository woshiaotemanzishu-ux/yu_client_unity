using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.CustomActivity
{
    internal static class CustomActivityConfigs
    {
        private static JObject _customActivity;

        public static async Task EnsureLoaded()
        {
            if (_customActivity != null) return;

            TextAsset asset = await ResManager.LoadAsync<TextAsset>("resource/config/client/configcustomactivity");
            if (asset == null)
            {
                _customActivity = new JObject();
                GameLog.Error("CustomActivity", "configcustomactivity missing");
                return;
            }

            _customActivity = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        public static async Task<string> ResolveIconTypeAsync(CustomActivityController.ActInfo info)
        {
            await MainUIConfigs.EnsureLoaded();
            await EnsureLoaded();

            string mapped = ResolveFromWindowsComponent(info);
            if (!string.IsNullOrEmpty(mapped)) return mapped;

            string fallback = info.BaseType == 101
                ? "331@100@" + info.ShowId
                : "331@" + info.BaseType + "@" + info.ShowId;
            return MainUIConfigs.GetFunctionIconCfg(fallback) != null ? fallback : null;
        }

        private static string ResolveFromWindowsComponent(CustomActivityController.ActInfo info)
        {
            if (!(_customActivity?["windowscomponent"] is JObject windows)) return null;
            string key = info.BaseType + "@" + info.ShowId;
            foreach (KeyValuePair<string, JToken> kv in windows)
            {
                if (!(kv.Value is JObject views)) continue;
                if (!(views[key] is JObject view)) continue;
                if (MainUIConfigs.GetFunctionIconCfg(kv.Key) == null) continue;
                if (!IsViewOpen(view)) continue;
                return kv.Key;
            }
            return null;
        }

        private static bool IsViewOpen(JObject view)
        {
            int level = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;
            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;

            int openLv = ReadInt(view, "open_lv");
            int viewOpenDay = ReadInt(view, "open_day");
            if (level < openLv) return false;
            if (viewOpenDay > 0 && openDay < viewOpenDay) return false;
            return true;
        }

        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj?[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (int.TryParse(token.ToString(), out int value)) return value;
            return 0;
        }
    }
}
