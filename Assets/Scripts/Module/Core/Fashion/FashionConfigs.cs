using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装配表读取器(对标老端 FashionModel.InitConfig 的 config_fashion/config_fashion_color/
    /// config_fashion_model)。表由 ClientConfigSync 从 yu_client cdn/resource/config/server/ 同步。
    ///
    /// ⚠订正(第21轮侦察 r21_fashion.md 的行数记载有误,已用实读文件订正):
    /// 侦察稿引用的是 yu_client/cdn/assets/resource/config/server/(一份较浅的旧副本,config_fashion 只到
    /// 20 星/1200 条);但 ClientConfigSync 实际同步源是 yu_client/cdn/resource/config/server/ ——
    /// 这份是 6420 条、星级到 200(与服务端 data_fashion.erl 的范围一致),config_fashion_color 是 15180 条
    /// (非 2640)。config_fashion_model 两份一致(704 条)。本类按实际同步源(6420/15180/704)实现,
    /// 不假设 20 星上限。
    /// </summary>
    public static class FashionConfigs
    {
        // config_fashion 主键 "pos@fashion_id@star_lv";config_fashion_color 主键 "pos@fashion_id@color_id@star_lv"。
        private static JObject _fashion;
        private static JObject _fashionColor;
        private static JObject _fashionModel; // "pos@fashion_id@career@sex@color_id"(展示用,本轮未强依赖)

        // pos -> 排序后的 fashion_id 列表(懒建缓存,来自 config_fashion 的 key 集合去重)
        private static readonly Dictionary<int, List<int>> _idsByPos = new Dictionary<int, List<int>>();

        public static bool IsLoaded => _fashion != null;

        public static async Task EnsureLoaded()
        {
            if (_fashion != null) return;
            _fashion = await LoadObj("config_fashion");
            _fashionColor = await LoadObj("config_fashion_color");
            _fashionModel = await LoadObj("config_fashion_model");
            _idsByPos.Clear();
            GameLog.Info("Fashion", "config_fashion={0} config_fashion_color={1} config_fashion_model={2}",
                _fashion.Count, _fashionColor.Count, _fashionModel.Count);
        }

        private static async Task<JObject> LoadObj(string name)
        {
            string key = GameResPath.GetServerConfigPath(name);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Fashion", "missing {0}: {1}(未同步?跑 神霄/配表/同步客户端配置)", name, key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>某 pos 全部配置了的 fashion_id(升序;来自 config_fashion 键集合去重,不代表都有模型/都能进游戏,
        /// 对标老端 UpdateCurrentUserModelList 但本轮先用 config_fashion 全集,不裁 config_fashion_model 缺项)。</summary>
        public static IReadOnlyList<int> GetFashionIds(int posId)
        {
            if (_idsByPos.TryGetValue(posId, out List<int> cached)) return cached;
            var set = new SortedSet<int>();
            if (_fashion != null)
            {
                string prefix = posId + "@";
                foreach (KeyValuePair<string, JToken> kv in _fashion)
                {
                    if (!kv.Key.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                    string rest = kv.Key.Substring(prefix.Length);
                    int at = rest.IndexOf('@');
                    if (at <= 0) continue;
                    if (int.TryParse(rest.Substring(0, at), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fashionId))
                    {
                        set.Add(fashionId);
                    }
                }
            }
            var list = new List<int>(set);
            _idsByPos[posId] = list;
            return list;
        }

        /// <summary>某 fashion 在 config_fashion_color 里已配置的颜色 id 集合(升序,不含 0——0 是基础色,
        /// 由 config_fashion 本身覆盖;对标老端"color∈{1,2,3}"22 件有色的说法,但本类按实际配置数据算,
        /// 不同 fashion_id 颜色数可能不同)。</summary>
        public static IReadOnlyList<int> GetColorIds(int posId, int fashionId)
        {
            var set = new SortedSet<int>();
            if (_fashionColor != null)
            {
                string prefix = posId + "@" + fashionId + "@";
                foreach (KeyValuePair<string, JToken> kv in _fashionColor)
                {
                    if (!kv.Key.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                    string rest = kv.Key.Substring(prefix.Length);
                    int at = rest.IndexOf('@');
                    if (at <= 0) continue;
                    if (int.TryParse(rest.Substring(0, at), NumberStyles.Integer, CultureInfo.InvariantCulture, out int colorId))
                    {
                        set.Add(colorId);
                    }
                }
            }
            return new List<int>(set);
        }

        /// <summary>一行配置(active_cost/star_cost/attr_list 都是 JSON 字符串,对标 config_fashion["pos@fashion@star"])。</summary>
        public readonly struct Row
        {
            public readonly string ActiveCostJson;
            public readonly string StarCostJson;
            public readonly string AttrListJson;
            public readonly bool Found;

            public Row(string activeCost, string starCost, string attrList, bool found)
            {
                ActiveCostJson = activeCost; StarCostJson = starCost; AttrListJson = attrList; Found = found;
            }

            public static readonly Row Empty = new Row("[]", "[]", "[]", false);
        }

        /// <summary>基础色(color 0)某星级行:config_fashion["pos@fashion@starLv"]。</summary>
        public static Row GetBaseRow(int posId, int fashionId, int starLv)
        {
            if (_fashion == null) return Row.Empty;
            string key = posId + "@" + fashionId + "@" + starLv;
            if (!(_fashion[key] is JObject o)) return Row.Empty;
            return new Row(ReadStr(o, "active_cost"), ReadStr(o, "star_cost"), ReadStr(o, "attr_list"), true);
        }

        /// <summary>颜色档(color!=0)某星级行:config_fashion_color["pos@fashion@color@starLv"]。</summary>
        public static Row GetColorRow(int posId, int fashionId, int colorId, int starLv)
        {
            if (_fashionColor == null || colorId == 0) return Row.Empty;
            string key = posId + "@" + fashionId + "@" + colorId + "@" + starLv;
            if (!(_fashionColor[key] is JObject o)) return Row.Empty;
            return new Row(ReadStr(o, "active_cost"), ReadStr(o, "star_cost"), ReadStr(o, "attr_list"), true);
        }

        public static Row GetRow(int posId, int fashionId, int colorId, int starLv) =>
            colorId == 0 ? GetBaseRow(posId, fashionId, starLv) : GetColorRow(posId, fashionId, colorId, starLv);

        /// <summary>该档最高配置星级(线性探到第一个不存在的 starLv 为止,上限 999 防死循环;
        /// 对标老端"配置到哪一星就是哪一星满阶",不臆造上限)。</summary>
        public static int GetMaxStarLv(int posId, int fashionId, int colorId)
        {
            int lv = 0;
            for (int s = 1; s <= 999; s++)
            {
                if (!GetRow(posId, fashionId, colorId, s).Found) break;
                lv = s;
            }
            return lv;
        }

        /// <summary>attr_list JSON 串解析为 (attrId,val) 列表(格式 [{"0":attrId,"1":val},...],对标老端
        /// JSON.parse(cfg.attr_list));解析失败/为空返回空表。</summary>
        public static List<(int attrId, long val)> ParseAttrList(string json)
        {
            var result = new List<(int, long)>();
            if (string.IsNullOrEmpty(json) || json == "[]") return result;
            JArray arr;
            try { arr = JArray.Parse(json); } catch { return result; }
            foreach (JToken t in arr)
            {
                if (!(t is JObject o)) continue;
                int attrId = o["0"]?.Value<int>() ?? 0;
                long val = o["1"]?.Value<long>() ?? 0;
                if (attrId > 0) result.Add((attrId, val));
            }
            return result;
        }

        /// <summary>cost JSON 串(active_cost/star_cost)解析为 (type,typeId,num) 列表,格式 [{"0":type,"1":type_id,"2":num},...]。</summary>
        public static List<(int type, int typeId, long num)> ParseCostList(string json)
        {
            var result = new List<(int, int, long)>();
            if (string.IsNullOrEmpty(json) || json == "[]") return result;
            JArray arr;
            try { arr = JArray.Parse(json); } catch { return result; }
            foreach (JToken t in arr)
            {
                if (!(t is JObject o)) continue;
                int type = o["0"]?.Value<int>() ?? 0;
                int typeId = o["1"]?.Value<int>() ?? 0;
                long num = o["2"]?.Value<long>() ?? 0;
                result.Add((type, typeId, num));
            }
            return result;
        }

        private static string ReadStr(JObject o, string key)
        {
            JToken t = o[key];
            return t == null ? "[]" : t.Type == JTokenType.String ? t.Value<string>() : t.ToString();
        }
    }
}
