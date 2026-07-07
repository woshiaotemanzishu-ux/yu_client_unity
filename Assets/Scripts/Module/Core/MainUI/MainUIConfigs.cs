using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// MainUI client config access for irregular preload_client_config tables.
    /// Mirrors the LoginConfigs JObject facade instead of hand-writing generated config.
    /// </summary>
    public static class MainUIConfigs
    {
        public sealed class FunctionIconCfg
        {
            public string IconType;
            public int LocationType;
            public int PosIndex;
            public string IconName;
            public string Desc;
            public int OpenLv;
            public int OpenDay;
            public int MaxOpenDay;
            public int OpenTaskId;
            public bool NotIndent;
            public string EffectName;
            public float EffectScale;
            public bool EffectNeedDelete;
            public bool ControllByOwnFun;
            public bool NotDelete;
            public bool CountDownTime;
            public bool IsBox;
            public bool NeedHideInAlpha;
        }

        public sealed class SceneCfg
        {
            public int Id;
            public string Name;
            public int Subtype;   // 1=安全场景(整场安全、禁释放技能;对标老端 SceneManager.IsSafeScene)

            /// <summary>本场景可切换的 PK 模式列表(requirement 里 type=="pkstate_list" 的 attr;
            /// 空数组=不可切换)。对标老端 MainUIFightModeView.LoadSuccess 的取法。</summary>
            public int[] PkStateList = System.Array.Empty<int>();
        }

        private static JObject _functionIcon;
        private static JObject _scene;
        private static readonly Dictionary<int, SceneCfg> _sceneCache = new Dictionary<int, SceneCfg>();

        public static bool IsLoaded => _functionIcon != null;
        public static bool IsSceneLoaded => _scene != null;

        public static async Task EnsureLoaded()
        {
            if (_functionIcon == null)
            {
                _functionIcon = await LoadJson("resource/config/client/configfunctionicon");
            }
        }

        public static async Task EnsureSceneLoaded()
        {
            if (_scene != null) return;

            _scene = await LoadJson(GameResPath.GetServerConfigPath("config_scene")) ?? new JObject();
            _sceneCache.Clear();
        }

        public static FunctionIconCfg GetFunctionIconCfg(string iconType)
        {
            if (string.IsNullOrEmpty(iconType) || _functionIcon == null) return null;
            foreach (KeyValuePair<string, JToken> area in _functionIcon)
            {
                if (!IsDataArea(area.Key)) continue;
                if (!(area.Value is JObject icons)) continue;
                if (icons[iconType] is JObject obj) return ReadFunctionIcon(obj);
            }
            return null;
        }

        public static IEnumerable<FunctionIconCfg> AllFunctionIconCfg()
        {
            if (_functionIcon == null) yield break;
            foreach (KeyValuePair<string, JToken> area in _functionIcon)
            {
                if (!IsDataArea(area.Key)) continue;
                if (!(area.Value is JObject icons)) continue;
                foreach (KeyValuePair<string, JToken> kv in icons)
                {
                    if (!(kv.Value is JObject obj)) continue;
                    FunctionIconCfg cfg = ReadFunctionIcon(obj);
                    if (cfg != null) yield return cfg;
                }
            }
        }

        public static SceneCfg GetSceneCfg(int sceneId)
        {
            if (sceneId <= 0 || _scene == null) return null;
            if (_sceneCache.TryGetValue(sceneId, out SceneCfg cached)) return cached;
            if (!(_scene[sceneId.ToString(CultureInfo.InvariantCulture)] is JObject obj)) return null;

            SceneCfg cfg = new SceneCfg
            {
                Id = ReadInt(obj, "id", sceneId),
                Name = ReadString(obj, "name").Trim(),
                Subtype = ReadInt(obj, "subtype"),
                PkStateList = ReadPkStateList(obj, sceneId),
            };
            _sceneCache[sceneId] = cfg;
            return cfg;
        }

        /// <summary>requirement 是"转义 JSON 字符串"(数组,元素 {attr,type}),二次解析取 pkstate_list 的 attr。
        /// 解析失败按"不可切换"处理(空数组)并告警,不让脏配置炸场景加载。</summary>
        private static int[] ReadPkStateList(JObject obj, int sceneId)
        {
            string requirement = ReadString(obj, "requirement");
            if (string.IsNullOrWhiteSpace(requirement)) return System.Array.Empty<int>();
            try
            {
                if (!(JToken.Parse(requirement) is JArray conds)) return System.Array.Empty<int>();
                foreach (JToken cond in conds)
                {
                    if (!(cond is JObject o)) continue;
                    if (ReadString(o, "type") != "pkstate_list") continue;
                    if (!(o["attr"] is JArray attr)) return System.Array.Empty<int>();

                    var list = new List<int>(attr.Count);
                    foreach (JToken t in attr)
                    {
                        if (t.Type == JTokenType.Integer) list.Add(t.Value<int>());
                    }
                    return list.ToArray();
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Config", "config_scene[{0}].requirement 解析失败: {1}", sceneId, e.Message);
            }
            return System.Array.Empty<int>();
        }

        private static async Task<JObject> LoadJson(string key)
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Config", "MainUI config missing: {0}", key);
                return null;
            }

            var jo = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return jo;
        }

        private static FunctionIconCfg ReadFunctionIcon(JObject obj)
        {
            string iconType = ReadString(obj, "icon_type");
            if (string.IsNullOrEmpty(iconType)) return null;

            return new FunctionIconCfg
            {
                IconType = iconType,
                LocationType = ReadInt(obj, "location_type"),
                PosIndex = ReadInt(obj, "pos_index"),
                IconName = ReadString(obj, "icon_name"),
                Desc = ReadString(obj, "desc"),
                OpenLv = ReadInt(obj, "open_lv"),
                OpenDay = ReadInt(obj, "open_day"),
                MaxOpenDay = ReadInt(obj, "max_open_day"),
                OpenTaskId = ReadInt(obj, "open_task_id"),
                NotIndent = ReadBool(obj, "not_indent"),
                EffectName = ReadString(obj, "effect_name"),
                EffectScale = ReadFloat(obj, "effect_scale", 1f),
                EffectNeedDelete = ReadBool(obj, "effect_need_delete", true),
                ControllByOwnFun = ReadBool(obj, "controll_by_own_fun"),
                NotDelete = ReadBool(obj, "not_delete"),
                CountDownTime = ReadBool(obj, "count_down_time"),
                IsBox = ReadBool(obj, "is_box"),
                NeedHideInAlpha = ReadBool(obj, "need_hide_in_alpha"),
            };
        }

        private static bool IsDataArea(string areaKey)
        {
            return int.TryParse(areaKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        private static string ReadString(JObject obj, string key, string fallback = "")
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            string value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static int ReadInt(JObject obj, string key, int fallback = 0)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<float>();
            if (token.Type == JTokenType.Boolean) return token.Value<bool>() ? 1 : 0;

            string value = token.Value<string>();
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            value = value.Trim();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return intValue;
            }
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return (int)floatValue;
            }
            return fallback;
        }

        private static float ReadFloat(JObject obj, string key, float fallback = 0f)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) return token.Value<float>();
            string value = token.Value<string>();
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : fallback;
        }

        private static bool ReadBool(JObject obj, string key, bool fallback = false)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            if (token.Type == JTokenType.Integer) return token.Value<int>() != 0;

            string value = token.Value<string>();
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            value = value.Trim();
            if (bool.TryParse(value, out bool boolValue)) return boolValue;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return intValue != 0;
            }
            return fallback;
        }
    }
}
