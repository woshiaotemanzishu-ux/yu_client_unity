using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装配表读取器(对标老端 FashionModel.InitConfig/FashionSuitView 的 config_fashion/
    /// config_fashion_color/config_fashion_model/config_fashion_pos/config_fashion_suit/
    /// config_fashion_suit_star)。表由 ClientConfigSync 从 yu_client cdn/resource/config/server/ 同步。
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
        private static JObject _fashionModel;
        private static JObject _fashionPosition;
        private static JObject _fashionSuit;
        private static JObject _fashionSuitStar;
        private static Task _loading;

        // pos -> 排序后的 fashion_id 列表(懒建缓存,来自 config_fashion 的 key 集合去重)
        private static readonly Dictionary<int, List<int>> _idsByPos = new Dictionary<int, List<int>>();

        public static bool IsLoaded => _fashion != null && _fashionColor != null && _fashionModel != null
            && _fashionPosition != null && _fashionSuit != null && _fashionSuitStar != null;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            return _loading ?? (_loading = LoadCoreAsync());
        }

        private static async Task LoadCoreAsync()
        {
            _fashion = await LoadObj("config_fashion");
            _fashionColor = await LoadObj("config_fashion_color");
            _fashionModel = await LoadObj("config_fashion_model");
            _fashionPosition = await LoadObj("config_fashion_pos");
            _fashionSuit = await LoadObj("config_fashion_suit");
            _fashionSuitStar = await LoadObj("config_fashion_suit_star");
            _idsByPos.Clear();
            GameLog.Info("Fashion", "fashion={0} color={1} model={2} pos={3} suit={4} suitStar={5}",
                _fashion.Count, _fashionColor.Count, _fashionModel.Count, _fashionPosition.Count,
                _fashionSuit.Count, _fashionSuitStar.Count);
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
            set.RemoveWhere(fashionId => !HasAnyModelRow(posId, fashionId));
            var list = new List<int>(set);
            _idsByPos[posId] = list;
            return list;
        }

        /// <summary>
        /// 老端列表只展示有 config_fashion_model 展示行的条目。仅存在培养配置、没有任何模型行的历史号
        /// 不应进入 UI；否则会在首开时逐项触发缺图导入，也无法形成有效角色预览。
        /// </summary>
        private static bool HasAnyModelRow(int posId, int fashionId)
        {
            if (_fashionModel == null) return false;
            string prefix = posId + "@" + fashionId + "@";
            foreach (KeyValuePair<string, JToken> pair in _fashionModel)
                if (pair.Key.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
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

        public sealed class AttrValue
        {
            public int AttrId;
            public long Value;
        }

        public sealed class CostValue
        {
            public int Type;
            public int TypeId;
            public long Num;
        }

        public sealed class SkillValue
        {
            public int SkillId;
            public int Level;
        }

        public sealed class PositionRow
        {
            public int PosId;
            public int PosLv;
            public int Cost;
            public IReadOnlyList<AttrValue> AttrAdds = Array.Empty<AttrValue>();
        }

        public sealed class SuitCondition
        {
            public int Slot;
            public int Type;
            public int SubType;
            public int TypeId;
        }

        public sealed class SuitAttrTier
        {
            public int ActiveCount;
            public IReadOnlyList<AttrValue> Attrs = Array.Empty<AttrValue>();
        }

        public sealed class SuitStageCondition
        {
            public int Slot;
            public int RequiredLevel;
        }

        public sealed class SuitRow
        {
            public int Id;
            public string Name = "";
            public float Ratio;
            public IReadOnlyList<SuitCondition> Conditions = Array.Empty<SuitCondition>();
            public IReadOnlyList<SuitAttrTier> AttrTiers = Array.Empty<SuitAttrTier>();
            public IReadOnlyList<SkillValue> Skills = Array.Empty<SkillValue>();
        }

        public sealed class SuitStarRow
        {
            public int SuitId;
            public int StarId;
            public string Desc = "";
            public IReadOnlyList<AttrValue> Attrs = Array.Empty<AttrValue>();
            public IReadOnlyList<SkillValue> Skills = Array.Empty<SkillValue>();
            public IReadOnlyList<SuitStageCondition> Conditions = Array.Empty<SuitStageCondition>();
            public IReadOnlyList<CostValue> Costs = Array.Empty<CostValue>();
        }

        public sealed class ModelRow
        {
            public int PosId;
            public int FashionId;
            public int Career;
            public int Sex;
            public int ColorId;
            public int ShowColor;
            public int ModelId;
            public int Exp;
            public string Name = "";
            public string Desc = "";
            public float Ratio;
        }

        /// <summary>时装部位等级行，主键 "pos_id@pos_lv"。</summary>
        public static PositionRow GetPositionRow(int posId, int posLv)
        {
            if (!(_fashionPosition?[MakeKey(posId, posLv)] is JObject row)) return null;
            return new PositionRow
            {
                PosId = ReadInt(row, "pos_id"),
                PosLv = ReadInt(row, "pos_lv"),
                Cost = ReadInt(row, "cost"),
                AttrAdds = ParseAttrValues(ReadStr(row, "attr_add_list")),
            };
        }

        /// <summary>指定部位的全部等级行，按 pos_lv 升序。</summary>
        public static IReadOnlyList<PositionRow> GetPositionRows(int posId)
        {
            var result = new List<PositionRow>();
            if (_fashionPosition == null) return result;
            foreach (KeyValuePair<string, JToken> pair in _fashionPosition)
            {
                if (!(pair.Value is JObject row) || ReadInt(row, "pos_id") != posId) continue;
                result.Add(new PositionRow
                {
                    PosId = posId,
                    PosLv = ReadInt(row, "pos_lv"),
                    Cost = ReadInt(row, "cost"),
                    AttrAdds = ParseAttrValues(ReadStr(row, "attr_add_list")),
                });
            }
            result.Sort((a, b) => a.PosLv.CompareTo(b.PosLv));
            return result;
        }

        public static int GetMaxPositionLv(int posId)
        {
            IReadOnlyList<PositionRow> rows = GetPositionRows(posId);
            return rows.Count == 0 ? 0 : rows[rows.Count - 1].PosLv;
        }

        /// <summary>时装套装基础行，数字键 suit_id。</summary>
        public static SuitRow GetSuit(int suitId)
        {
            if (!(_fashionSuit?[suitId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return ReadSuit(row);
        }

        /// <summary>全部时装套装，按 id 升序。</summary>
        public static IReadOnlyList<SuitRow> GetSuits()
        {
            var result = new List<SuitRow>();
            if (_fashionSuit == null) return result;
            foreach (KeyValuePair<string, JToken> pair in _fashionSuit)
            {
                if (pair.Value is JObject row) result.Add(ReadSuit(row));
            }
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            return result;
        }

        /// <summary>时装套装升阶行，主键 "suit_id@star_id"。</summary>
        public static SuitStarRow GetSuitStar(int suitId, int starId)
        {
            if (!(_fashionSuitStar?[MakeKey(suitId, starId)] is JObject row)) return null;
            return ReadSuitStar(row);
        }

        /// <summary>指定套装的全部升阶行，按 star_id 升序。</summary>
        public static IReadOnlyList<SuitStarRow> GetSuitStars(int suitId)
        {
            var result = new List<SuitStarRow>();
            if (_fashionSuitStar == null) return result;
            foreach (KeyValuePair<string, JToken> pair in _fashionSuitStar)
            {
                if (!(pair.Value is JObject row) || ReadInt(row, "suit_id") != suitId) continue;
                result.Add(ReadSuitStar(row));
            }
            result.Sort((a, b) => a.StarId.CompareTo(b.StarId));
            return result;
        }

        public static int GetMaxSuitStar(int suitId)
        {
            IReadOnlyList<SuitStarRow> rows = GetSuitStars(suitId);
            return rows.Count == 0 ? 0 : rows[rows.Count - 1].StarId;
        }

        /// <summary>职业/性别/颜色对应的时装模型行。</summary>
        public static ModelRow GetModelRow(int posId, int fashionId, int career, int sex, int colorId)
        {
            string key = MakeKey(posId, fashionId, career, sex, colorId);
            if (!(_fashionModel?[key] is JObject row)) return null;
            return new ModelRow
            {
                PosId = ReadInt(row, "pos_id"),
                FashionId = ReadInt(row, "fashion_id"),
                Career = ReadInt(row, "career"),
                Sex = ReadInt(row, "sex"),
                ColorId = ReadInt(row, "color_id"),
                ShowColor = ReadInt(row, "show_color"),
                ModelId = ReadInt(row, "model_id"),
                Exp = ReadInt(row, "exp"),
                Name = ReadStr(row, "name", ""),
                Desc = ReadStr(row, "desc", ""),
                Ratio = ReadFloat(row, "ratio"),
            };
        }

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

        private static SuitRow ReadSuit(JObject row)
        {
            return new SuitRow
            {
                Id = ReadInt(row, "id"),
                Name = ReadStr(row, "name", ""),
                Ratio = ReadFloat(row, "ratio"),
                Conditions = ParseSuitConditions(ReadStr(row, "condition")),
                AttrTiers = ParseSuitAttrTiers(ReadStr(row, "attr")),
                Skills = ParseSkillValues(ReadStr(row, "skill")),
            };
        }

        private static SuitStarRow ReadSuitStar(JObject row)
        {
            return new SuitStarRow
            {
                SuitId = ReadInt(row, "suit_id"),
                StarId = ReadInt(row, "star_id"),
                Desc = ReadStr(row, "desc", ""),
                Attrs = ParseAttrValues(ReadStr(row, "attr")),
                Skills = ParseSkillValues(ReadStr(row, "skill")),
                Conditions = ParseSuitStageConditions(ReadStr(row, "condition")),
                Costs = ParseCostValues(ReadStr(row, "cost")),
            };
        }

        private static IReadOnlyList<AttrValue> ParseAttrValues(string json)
        {
            JArray array = JArray.Parse(json);
            var result = new List<AttrValue>(array.Count);
            foreach (JToken token in array)
            {
                if (!(token is JObject item)) continue;
                result.Add(new AttrValue { AttrId = ReadInt(item, "0"), Value = ReadLong(item, "1") });
            }
            return result;
        }

        private static IReadOnlyList<CostValue> ParseCostValues(string json)
        {
            JArray array = JArray.Parse(json);
            var result = new List<CostValue>(array.Count);
            foreach (JToken token in array)
            {
                if (!(token is JObject item)) continue;
                result.Add(new CostValue
                {
                    Type = ReadInt(item, "0"),
                    TypeId = ReadInt(item, "1"),
                    Num = ReadLong(item, "2"),
                });
            }
            return result;
        }

        private static IReadOnlyList<SkillValue> ParseSkillValues(string json)
        {
            JArray array = JArray.Parse(json);
            var result = new List<SkillValue>(array.Count);
            foreach (JToken token in array)
            {
                if (!(token is JObject item)) continue;
                result.Add(new SkillValue { SkillId = ReadInt(item, "0"), Level = ReadInt(item, "1") });
            }
            return result;
        }

        private static IReadOnlyList<SuitCondition> ParseSuitConditions(string json)
        {
            JArray array = JArray.Parse(json);
            var result = new List<SuitCondition>(array.Count);
            foreach (JToken token in array)
            {
                if (!(token is JObject item) || !(item["1"] is JObject condition)) continue;
                result.Add(new SuitCondition
                {
                    Slot = ReadInt(item, "0"),
                    Type = ReadInt(condition, "0"),
                    SubType = ReadInt(condition, "1"),
                    TypeId = ReadInt(condition, "2"),
                });
            }
            return result;
        }

        private static IReadOnlyList<SuitAttrTier> ParseSuitAttrTiers(string json)
        {
            JArray array = JArray.Parse(json);
            var result = new List<SuitAttrTier>(array.Count);
            foreach (JToken token in array)
            {
                if (!(token is JObject item)) continue;
                JToken attrs = item["1"];
                result.Add(new SuitAttrTier
                {
                    ActiveCount = ReadInt(item, "0"),
                    Attrs = ParseAttrValues(attrs == null ? "[]" : attrs.ToString()),
                });
            }
            return result;
        }

        private static IReadOnlyList<SuitStageCondition> ParseSuitStageConditions(string json)
        {
            JArray array = JArray.Parse(json);
            var result = new List<SuitStageCondition>(array.Count);
            foreach (JToken token in array)
            {
                if (!(token is JObject item)) continue;
                result.Add(new SuitStageCondition
                {
                    Slot = ReadInt(item, "0"),
                    RequiredLevel = ReadInt(item, "1"),
                });
            }
            return result;
        }

        private static string MakeKey(int a, int b)
        {
            return a.ToString(CultureInfo.InvariantCulture) + "@" + b.ToString(CultureInfo.InvariantCulture);
        }

        private static string MakeKey(int a, int b, int c, int d, int e)
        {
            return a.ToString(CultureInfo.InvariantCulture) + "@" + b.ToString(CultureInfo.InvariantCulture)
                + "@" + c.ToString(CultureInfo.InvariantCulture) + "@" + d.ToString(CultureInfo.InvariantCulture)
                + "@" + e.ToString(CultureInfo.InvariantCulture);
        }

        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj?[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int value) ? value : 0;
        }

        private static long ReadLong(JObject obj, string key)
        {
            JToken token = obj?[key];
            if (token == null || token.Type == JTokenType.Null) return 0L;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            if (token.Type == JTokenType.Float) return (long)token.Value<double>();
            return long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long value) ? value : 0L;
        }

        private static float ReadFloat(JObject obj, string key)
        {
            JToken token = obj?[key];
            if (token == null || token.Type == JTokenType.Null) return 0f;
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) return token.Value<float>();
            return float.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }

        private static string ReadStr(JObject o, string key)
        {
            return ReadStr(o, key, "[]");
        }

        private static string ReadStr(JObject o, string key, string fallback)
        {
            JToken t = o?[key];
            return t == null || t.Type == JTokenType.Null
                ? fallback
                : t.Type == JTokenType.String ? t.Value<string>() : t.ToString();
        }
    }
}
