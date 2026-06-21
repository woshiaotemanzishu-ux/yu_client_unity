using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 服务端 config_mon 访问器(对标老端 Config.GetConfigVo("config_mon", type_id),见 Monster.ts:90-92)。
    /// 表由 ClientConfigSync 从 yu_client cdn/resource/config/server/config_mon.json 同步进
    /// Assets/GameRes/resource/config/server/config_mon.json(地址 resource/config/server/config_mon)。
    ///
    /// 字段为【数字索引键】(同 config_goods/config_task,非 config_npc 的具名键)。本轮取证用到的列:
    ///   "0"=type_id  "1"=name(如 10001001→"转运傀儡")  "10"=monster_res 模型资源(如 10020103)
    ///   "11"=icon_scale 模型缩放(如 "0.9")  "16"=brith_rot(本轮不接朝向,仅留待后续)
    /// (列序已用 10001001 行交叉校验:"10"=10020103 与既有模型资源 model_clothe_10020103 一致,
    ///  "19"=140 与服务器 12007 下发 hp=140/140 一致。)
    ///
    /// 用途:场景怪渲染层(<see cref="MonsterRenderer"/>)名牌名字 + 模型缩放。模型资源以协议
    /// <see cref="Vo.MonsterVo.MonsterRes"/> 为准(服务器实时下发),config 仅作名字/缩放与交叉校验来源。
    /// 表未同步时优雅降级(空表 + 一条 Error),渲染层回退协议 vo.Name + 默认缩放,绝不写假名。
    /// </summary>
    public static class MonsterConfigs
    {
        public sealed class MonCfg
        {
            public int Id;
            public string Name;
            public int Body;        // monster_res 模型资源(config "10"),与协议 vo.MonsterRes 同语义,用于交叉校验
            public float IconScale; // config "11" 模型缩放(对标 Monster.ts SetScale(vo.icon_scale))
        }

        private static JObject _mon;
        private static readonly Dictionary<int, MonCfg> _cache = new Dictionary<int, MonCfg>();

        public static bool IsLoaded => _mon != null;

        public static async Task EnsureLoaded()
        {
            if (_mon != null) return;

            string key = GameResPath.GetServerConfigPath("config_mon");
            TextAssetWrap asset = await LoadText(key);
            if (asset == null)
            {
                GameLog.Error("Scene", "missing config_mon: {0}(跑 神霄/配表/同步客户端配置(JSON) 同步;缺表时怪名/缩放降级)", key);
                _mon = new JObject();
                return;
            }

            _mon = JObject.Parse(asset.Text);
            _cache.Clear();
            asset.Release();
            GameLog.Info("Scene", "config_mon loaded: {0} mon", _mon.Count);
        }

        public static MonCfg Get(int typeId)
        {
            if (typeId <= 0 || _mon == null) return null;
            if (_cache.TryGetValue(typeId, out MonCfg cached)) return cached;
            if (!(_mon[typeId.ToString()] is JObject obj)) return null;

            MonCfg cfg = new MonCfg
            {
                Id = ReadInt(obj, "0"),
                Name = ReadString(obj, "1"),
                Body = ReadInt(obj, "10"),
                IconScale = ReadFloat(obj, "11", 1f),
            };
            _cache[typeId] = cfg;
            return cfg;
        }

        // —— 读取小工具:数字键容错(字符串/数字混排)——
        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static float ReadFloat(JObject obj, string key, float fallback)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) return token.Value<float>();
            return float.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        private static string ReadString(JObject obj, string key)
        {
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        // ResManager.LoadAsync 返回 TextAsset 并要 Release,这里包一层避免业务直接拿 Unity 句柄(同 NpcConfigs)。
        private sealed class TextAssetWrap
        {
            public string Text;
            private UnityEngine.TextAsset _asset;
            public TextAssetWrap(UnityEngine.TextAsset a) { _asset = a; Text = a.text; }
            public void Release() { ResManager.Release(_asset); _asset = null; }
        }

        private static async Task<TextAssetWrap> LoadText(string key)
        {
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            return asset == null ? null : new TextAssetWrap(asset);
        }
    }
}
