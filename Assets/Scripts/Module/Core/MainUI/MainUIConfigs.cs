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

        /// <summary>
        /// 收纳盒成员配置(对标老端 ConfigBoxIcon.json / CommonManager.GetBoxIconCfg)。
        /// 每条把一个「成员图标」(IconType)绑到一个「盒入口图标」(AttachIconType,如 100/101)上,
        /// 带自己的 attach_* 放入门槛。活动栏上的盒入口点开后按 SortOrder 铺这些成员。
        /// </summary>
        public sealed class BoxIconCfg
        {
            public string IconType;
            public int SortOrder;
            public string AttachIconType;   // 收纳在哪个盒入口 icon_type 里(可能是 "100"/"172" 这类)
            public int AttachOpenLv;
            public int AttachOpenDay;
            public int AttachMaxOpenDay;
            public int AttachOpenTaskId;
            public int AttachVipLv;         // 仅 450@1 用(=4);反极性:VIP≥此值时该成员反而移出盒(照抄老端,勿"修正")
            public bool ControllByOwnFun;
            public string Desc;
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
        private static JObject _boxIcon;
        private static JObject _scene;
        private static readonly Dictionary<int, SceneCfg> _sceneCache = new Dictionary<int, SceneCfg>();

        public static bool IsLoaded => _functionIcon != null;
        public static bool IsBoxLoaded => _boxIcon != null;
        public static bool IsSceneLoaded => _scene != null;

        private static Task _functionIconLoading;
        private static Task _boxIconLoading;

        public static async Task EnsureLoaded()
        {
            // 图标系统同时依赖 configfunctionicon(归属/门槛)与 configboxicon(收纳盒聚合):
            // 任何 await EnsureLoaded 的调用方(控制器 AddIconAsync / RefreshDefaultIcons)都要两份齐了才能
            // 正确地把成员图标折进盒子。两份并发重试到就绪。
            await System.Threading.Tasks.Task.WhenAll(EnsureFunctionIconLoaded(), EnsureBoxIconLoaded());
        }

        private static Task EnsureFunctionIconLoaded()
        {
            if (_functionIcon != null) return System.Threading.Tasks.Task.CompletedTask;
            // configfunctionicon 走 Addressables,启动初期目录/远程未就绪时 LoadAsync 会返回 null。
            // 旧实现失败即返回(_functionIcon 仍 null),调用方(各活动控制器 AddIconAsync)此刻拿到 null cfg
            // → 图标永久漏加且不再重试(CustomActivity/配置扫描靠后续事件反复重评才侥幸赶上就绪)。
            // 改为:单飞 + 重试到配置真就绪,让 await EnsureLoaded 的调用方等到配置好再落图标。
            if (_functionIconLoading == null || _functionIconLoading.IsCompleted)
                _functionIconLoading = LoadFunctionIconWithRetryAsync();
            return _functionIconLoading;
        }

        /// <summary>只保证收纳盒配置就绪(box 状态机的同步 GetBoxIconCfg 需要它先加载好)。</summary>
        public static Task EnsureBoxIconLoaded()
        {
            if (_boxIcon != null) return System.Threading.Tasks.Task.CompletedTask;
            if (_boxIconLoading == null || _boxIconLoading.IsCompleted)
                _boxIconLoading = LoadBoxIconWithRetryAsync();
            return _boxIconLoading;
        }

        private static async Task LoadFunctionIconWithRetryAsync()
        {
            for (int attempt = 0; attempt < 600 && _functionIcon == null; attempt++)
            {
                TextAsset asset = await ResManager.LoadOptionalAsync<TextAsset>("resource/config/client/configfunctionicon");
                if (asset != null)
                {
                    _functionIcon = JObject.Parse(asset.text);
                    ResManager.Release(asset);
                    return;
                }
                await System.Threading.Tasks.Task.Yield();
            }
            if (_functionIcon == null)
                GameLog.Error("Config", "configfunctionicon 重试 600 帧仍加载失败(Addressables 目录/远程一直不就绪)");
        }

        private static async Task LoadBoxIconWithRetryAsync()
        {
            for (int attempt = 0; attempt < 600 && _boxIcon == null; attempt++)
            {
                TextAsset asset = await ResManager.LoadOptionalAsync<TextAsset>("resource/config/client/configboxicon");
                if (asset != null)
                {
                    _boxIcon = JObject.Parse(asset.text);
                    ResManager.Release(asset);
                    return;
                }
                await System.Threading.Tasks.Task.Yield();
            }
            if (_boxIcon == null)
                GameLog.Error("Config", "configboxicon 重试 600 帧仍加载失败(Addressables 目录/远程一直不就绪)");
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

        /// <summary>取某成员图标的收纳盒配置(对标老端 CommonManager.GetBoxIconCfg);无则 null。</summary>
        public static BoxIconCfg GetBoxIconCfg(string iconType)
        {
            if (string.IsNullOrEmpty(iconType) || _boxIcon == null) return null;
            if (iconType == "icon_type") return null; // 表头 legend,不是真条目
            if (_boxIcon[iconType] is JObject obj) return ReadBoxIcon(iconType, obj);
            return null;
        }

        /// <summary>某盒入口(attachIconType,如 "100"/"101")收纳的全部成员配置(供盒展开/聚合)。</summary>
        public static IEnumerable<BoxIconCfg> BoxIconsForHolder(string attachIconType)
        {
            if (_boxIcon == null || string.IsNullOrEmpty(attachIconType)) yield break;
            foreach (KeyValuePair<string, JToken> kv in _boxIcon)
            {
                if (kv.Key == "icon_type") continue;
                if (!(kv.Value is JObject obj)) continue;
                BoxIconCfg cfg = ReadBoxIcon(kv.Key, obj);
                if (cfg != null && cfg.AttachIconType == attachIconType) yield return cfg;
            }
        }

        private static BoxIconCfg ReadBoxIcon(string key, JObject obj)
        {
            string iconType = ReadString(obj, "icon_type");
            if (string.IsNullOrEmpty(iconType)) iconType = key;
            return new BoxIconCfg
            {
                IconType = iconType,
                SortOrder = ReadInt(obj, "sort_order"),
                AttachIconType = ReadString(obj, "attach_icon_type"),
                AttachOpenLv = ReadInt(obj, "attach_open_lv"),
                AttachOpenDay = ReadInt(obj, "attach_open_day"),
                AttachMaxOpenDay = ReadInt(obj, "attach_max_open_day"),
                AttachOpenTaskId = ReadInt(obj, "attach_open_task_id"),
                AttachVipLv = ReadInt(obj, "attach_vip_lv"),
                ControllByOwnFun = ReadBool(obj, "controll_by_own_fun"),
                Desc = ReadString(obj, "desc"),
            };
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
