using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>config_dress_up_cfg 的只读访问器，对标老端 DressModel。</summary>
    public static class DressConfigs
    {
        public sealed class AttrValue
        {
            public int Id;
            public long Value;
        }

        public sealed class CostValue
        {
            public int Type;
            public int TypeId;
            public long Num;
        }

        public sealed class Row
        {
            public byte Type;
            public uint Id;
            public int Level;
            public string Name = "";
            public string CostJson = "[]";
            public string AttrJson = "[]";
            public int Skill;
            public int Vector;
            public string ConditionJson = "[]";
            public string ScreenJson = "[]";
        }

        private static JObject _config;
        private static Task _loading;
        private static readonly Dictionary<string, Row> Rows = new Dictionary<string, Row>();
        private static readonly Dictionary<byte, List<Row>> FirstRows = new Dictionary<byte, List<Row>>();

        public static bool IsLoaded => _config != null;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAsync());
        }

        private static async Task LoadAsync()
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_dress_up_cfg"));
            _config = asset != null ? JObject.Parse(asset.text) : new JObject();
            if (asset != null) ResManager.Release(asset);
            Rows.Clear();
            FirstRows.Clear();

            foreach (KeyValuePair<string, JToken> pair in _config)
            {
                if (!(pair.Value is JObject value)) continue;
                Row row = new Row
                {
                    Type = (byte)ReadInt(value, "type"),
                    Id = (uint)ReadInt(value, "id"),
                    Level = ReadInt(value, "level"),
                    Name = ReadString(value, "name"),
                    CostJson = ReadString(value, "cost", "[]"),
                    AttrJson = ReadString(value, "attr", "[]"),
                    Skill = ReadInt(value, "skill"),
                    Vector = ReadInt(value, "vector"),
                    ConditionJson = ReadString(value, "condition", "[]"),
                    ScreenJson = ReadString(value, "screen", "[]"),
                };
                Rows[Key(row.Type, row.Id, row.Level)] = row;
                if (row.Level != 1) continue;
                if (!FirstRows.TryGetValue(row.Type, out List<Row> list))
                {
                    list = new List<Row>();
                    FirstRows[row.Type] = list;
                }
                list.Add(row);
            }

            foreach (List<Row> list in FirstRows.Values) list.Sort((a, b) => a.Id.CompareTo(b.Id));
            GameLog.Info("Dress", "config_dress_up_cfg={0}, level1={1}", Rows.Count, FirstRows.Count);
        }

        public static Row GetRow(byte type, uint id, int level)
        {
            Rows.TryGetValue(Key(type, id, level), out Row row);
            return row;
        }

        public static IReadOnlyList<Row> GetDisplayRows(byte type)
        {
            if (!FirstRows.TryGetValue(type, out List<Row> source)) return Array.Empty<Row>();
            var result = new List<Row>(source);
            if (type != DressView.HeadType) return result;

            int roleTurn = RoleModel.Instance.Figure != null ? RoleModel.Instance.Figure.turn : 0;
            uint nextTurnId = 0;
            int nextTurn = int.MaxValue;
            for (int i = 0; i < result.Count; i++)
            {
                int turn = GetTurnCondition(result[i]);
                if (turn <= roleTurn || turn >= nextTurn) continue;
                nextTurn = turn;
                nextTurnId = result[i].Id;
            }

            result.Sort((a, b) =>
            {
                if (a.Id == nextTurnId) return b.Id == nextTurnId ? 0 : -1;
                if (b.Id == nextTurnId) return 1;
                bool aa = DressModel.Instance.IsActive(type, a.Id);
                bool ba = DressModel.Instance.IsActive(type, b.Id);
                if (aa != ba) return aa ? -1 : 1;
                int at = GetTurnCondition(a);
                int bt = GetTurnCondition(b);
                bool ac = at > 0;
                bool bc = bt > 0;
                if (ac != bc) return ac ? 1 : -1;
                if (ac && at != bt) return at.CompareTo(bt);
                return a.Id.CompareTo(b.Id);
            });
            return result;
        }

        public static IReadOnlyList<AttrValue> GetAttrs(Row row)
        {
            var result = new List<AttrValue>();
            foreach (JToken token in ParseArray(row?.AttrJson))
            {
                if (!(token is JObject obj)) continue;
                result.Add(new AttrValue { Id = ReadTokenInt(obj["0"]), Value = ReadTokenLong(obj["1"]) });
            }
            return result;
        }

        public static CostValue GetFirstCost(Row row)
        {
            JArray array = ParseArray(row?.CostJson);
            if (array.Count == 0 || !(array[0] is JObject obj)) return null;
            return new CostValue
            {
                Type = ReadTokenInt(obj["0"]),
                TypeId = ReadTokenInt(obj["1"]),
                Num = ReadTokenLong(obj["2"]),
            };
        }

        public static IReadOnlyList<Row> GetSkillRows(byte type, uint id)
        {
            var result = new List<Row>();
            var skills = new HashSet<int>();
            foreach (Row row in Rows.Values)
            {
                if (row.Type != type || row.Id != id || row.Skill <= 0 || !skills.Add(row.Skill)) continue;
                result.Add(row);
            }
            result.Sort((a, b) => a.Level.CompareTo(b.Level));
            return result;
        }

        public static int GetTurnCondition(Row row)
        {
            JArray array = ParseArray(row?.ConditionJson);
            if (array.Count == 0 || !(array[0] is JObject obj)) return 0;
            return string.Equals(obj["0"]?.ToString(), "turn", StringComparison.OrdinalIgnoreCase)
                ? ReadTokenInt(obj["1"])
                : 0;
        }

        public static string GetHeadIcon(Row row, int career)
        {
            foreach (JToken token in ParseArray(row?.ScreenJson))
            {
                if (!(token is JObject obj) || ReadTokenInt(obj["0"]) != career) continue;
                return obj["1"]?.ToString() ?? "";
            }
            return "";
        }

        private static string Key(byte type, uint id, int level) => type + "@" + id + "@" + level;

        private static JArray ParseArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new JArray();
            try { return JArray.Parse(raw); }
            catch { return new JArray(); }
        }

        private static int ReadInt(JObject obj, string key) => ReadTokenInt(obj?[key]);
        private static string ReadString(JObject obj, string key, string fallback = "") => obj?[key]?.ToString() ?? fallback;
        private static int ReadTokenInt(JToken token) => int.TryParse(token?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        private static long ReadTokenLong(JToken token) => long.TryParse(token?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : 0L;
    }
}
