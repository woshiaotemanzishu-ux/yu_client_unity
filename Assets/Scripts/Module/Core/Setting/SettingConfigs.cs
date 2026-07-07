using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置模块配置门面(镜像 MainUIConfigs 的 JObject 取法):
    ///  - config_setting.json(服务器配置,key="type@subtype"):各设置项显示名 + 服务器缺省值
    ///    (屏蔽列表/自动拾取项文案取这里,对标老端 preload_config config_setting);
    ///  - ClientBlockConfig.json(客户端配置):滑条上限 SameScreenMaxLimitRoleNum、
    ///    「还原默认设置」的 DefaultMode 批量值(对标老端 SimpleModeFun)。
    /// </summary>
    public static class SettingConfigs
    {
        public sealed class ItemCfg
        {
            public int Type;
            public int Subtype;
            public string Name;
            public int DefaultOpen;
        }

        private static JObject _blockCfg;
        private static Dictionary<string, ItemCfg> _items;

        public static bool IsLoaded => _items != null && _blockCfg != null;

        public static async Task EnsureLoaded()
        {
            if (_items == null)
            {
                JObject jo = await LoadJson(GameResPath.GetServerConfigPath("config_setting"));
                var items = new Dictionary<string, ItemCfg>();
                if (jo != null)
                {
                    foreach (KeyValuePair<string, JToken> kv in jo)
                    {
                        if (!(kv.Value is JObject o)) continue;
                        items[kv.Key] = new ItemCfg
                        {
                            Type = o.Value<int?>("type") ?? 0,
                            Subtype = o.Value<int?>("subtype") ?? 0,
                            Name = o.Value<string>("name") ?? "",
                            DefaultOpen = o.Value<int?>("is_open") ?? 0,
                        };
                    }
                }
                _items = items;
            }

            if (_blockCfg == null)
            {
                _blockCfg = await LoadJson("resource/config/client/clientblockconfig") ?? new JObject();
            }
        }

        /// <summary>某设置项配置(缺配置返回 null → 调用方按老端"查不到不显示"容错)。</summary>
        public static ItemCfg GetItem(int type, int subtype)
        {
            if (_items == null) return null;
            return _items.TryGetValue(type + "@" + subtype, out ItemCfg cfg) ? cfg : null;
        }

        /// <summary>同屏人数/特效数量滑条上限(老端两条滑条共用 SameScreenMaxLimitRoleNum)。</summary>
        public static int MaxRoleNum
        {
            get
            {
                JToken t = _blockCfg?["SameScreenMaxLimitRoleNum"];
                return t != null && t.Type == JTokenType.Integer ? t.Value<int>() : 20;
            }
        }

        /// <summary>「还原默认设置」批量项(DefaultMode 的 key 经 BlockSubType 映射为 subtype)。</summary>
        public static List<KeyValuePair<int, int>> GetDefaultModeList()
        {
            var list = new List<KeyValuePair<int, int>>();
            if (!(_blockCfg?["DefaultMode"] is JObject defaults) || !(_blockCfg["BlockSubType"] is JObject subtypes))
            {
                return list;
            }
            foreach (KeyValuePair<string, JToken> kv in defaults)
            {
                JToken sub = subtypes[kv.Key];
                if (sub == null || sub.Type != JTokenType.Integer) continue;
                if (kv.Value.Type != JTokenType.Integer) continue;
                list.Add(new KeyValuePair<int, int>(sub.Value<int>(), kv.Value.Value<int>()));
            }
            return list;
        }

        private static async Task<JObject> LoadJson(string key)
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Config", "Setting config missing: {0}", key);
                return null;
            }
            var jo = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return jo;
        }
    }
}
