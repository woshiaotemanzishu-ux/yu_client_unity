using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Achievement
{
    /// <summary>
    /// 成就页面六张权威配置表。数字列严格按 config_table_default.config_achievement 映射；
    /// category/type/subtype 拓扑来自三张具名表，不在 View 里硬编码菜单。
    /// </summary>
    public static class AchievementConfigs
    {
        public readonly struct RewardTriple
        {
            public readonly int Type;
            public readonly int TypeId;
            public readonly long Count;

            public RewardTriple(int type, int typeId, long count)
            {
                Type = type;
                TypeId = typeId;
                Count = count;
            }
        }

        public readonly struct AttributeValue
        {
            public readonly int Id;
            public readonly long Value;

            public AttributeValue(int id, long value)
            {
                Id = id;
                Value = value;
            }
        }

        public sealed class EntryRow
        {
            public uint Id;
            public byte Category;
            public uint Star;
            public string Description;
            public uint NextId;
            public bool Inherit;
            public bool ShowProgress;
            public string ConditionRaw;
            public string RewardRaw;
            public ulong Target;
            public readonly List<RewardTriple> Rewards = new List<RewardTriple>();
        }

        public sealed class CategoryRow
        {
            public byte Category;
            public string Name;
            public int Color;
            public int Level;
            public int Sort;
            public int Type;
            public ushort Subtype;
        }

        public sealed class TypeRow
        {
            public int Id;
            public string Name;
            public readonly List<ushort> Subtypes = new List<ushort>();
        }

        public sealed class SubtypeRow
        {
            public ushort Id;
            public string Name;
        }

        public sealed class StageRow
        {
            public int Stage;
            public string Name;
            public string Picture;
            public uint RequiredStar;
            public readonly List<AttributeValue> Attributes = new List<AttributeValue>();
        }

        private static readonly Dictionary<uint, EntryRow> Entries = new Dictionary<uint, EntryRow>();
        private static readonly Dictionary<byte, CategoryRow> Categories = new Dictionary<byte, CategoryRow>();
        private static readonly Dictionary<ushort, List<CategoryRow>> CategoriesBySubtype =
            new Dictionary<ushort, List<CategoryRow>>();
        private static readonly List<TypeRow> Types = new List<TypeRow>();
        private static readonly Dictionary<ushort, SubtypeRow> Subtypes = new Dictionary<ushort, SubtypeRow>();
        private static readonly Dictionary<int, StageRow> Stages = new Dictionary<int, StageRow>();
        private static readonly Dictionary<uint, string> OverviewTitles = new Dictionary<uint, string>();
        private static Task _loading;

        public static bool IsLoaded { get; private set; }
        public static int EntryCount => Entries.Count;
        public static int CategoryCount => Categories.Count;
        public static int TypeCount => Types.Count;
        public static int StageCount => Stages.Count;

        public static Task EnsureLoaded() => IsLoaded ? Task.CompletedTask : (_loading ?? (_loading = LoadAsync()));

        public static IReadOnlyList<TypeRow> GetTypes() => Types;

        public static bool TryGetEntry(uint id, out EntryRow row) => Entries.TryGetValue(id, out row);
        public static bool TryGetCategory(byte id, out CategoryRow row) => Categories.TryGetValue(id, out row);
        public static bool TryGetSubtype(ushort id, out SubtypeRow row) => Subtypes.TryGetValue(id, out row);
        public static bool TryGetStage(int stage, out StageRow row) => Stages.TryGetValue(stage, out row);

        public static IReadOnlyList<CategoryRow> GetCategories(ushort subtype)
            => CategoriesBySubtype.TryGetValue(subtype, out List<CategoryRow> rows)
                ? rows
                : Array.Empty<CategoryRow>();

        public static string GetOverviewTitle(uint id)
            => OverviewTitles.TryGetValue(id, out string title) ? title : string.Empty;

        private static async Task LoadAsync()
        {
            string[] serverNames =
            {
                "config_achievement",
                "config_achievement_star_reward",
                "config_achievement_category",
                "config_achievement_type_new",
                "config_achievement_stage_reward",
            };
            var tasks = new List<Task<TextAsset>>(serverNames.Length + 1);
            for (int i = 0; i < serverNames.Length; i++)
                tasks.Add(ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath(serverNames[i])));
            tasks.Add(ResManager.LoadAsync<TextAsset>(GameResPath.GetClientConfigPath("ClientAchv")));
            TextAsset[] assets = await Task.WhenAll(tasks);

            Entries.Clear();
            Categories.Clear();
            CategoriesBySubtype.Clear();
            Types.Clear();
            Subtypes.Clear();
            Stages.Clear();
            OverviewTitles.Clear();
            IsLoaded = false;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null) continue;
                GameLog.Error("Achievement", "配置缺失: {0}",
                    i < serverNames.Length ? serverNames[i] : "ClientAchv");
                ReleaseAll(assets);
                _loading = null;
                return;
            }

            try
            {
                ParseEntries(JObject.Parse(assets[0].text));
                ParseStages(JObject.Parse(assets[1].text));
                ParseCategories(JObject.Parse(assets[2].text));
                ParseTypes(JObject.Parse(assets[3].text));
                ParseSubtypes(JObject.Parse(assets[4].text));
                ParseOverviewTitles(JObject.Parse(assets[5].text));
                IsLoaded = Entries.Count > 0 && Categories.Count > 0 && Types.Count == 7
                    && Subtypes.Count > 0 && Stages.Count > 0;
                GameLog.Info("Achievement",
                    "configs entries={0}, categories={1}, types={2}, subtypes={3}, stages={4}, titles={5}, ready={6}",
                    Entries.Count, Categories.Count, Types.Count, Subtypes.Count, Stages.Count,
                    OverviewTitles.Count, IsLoaded);
            }
            catch (Exception e)
            {
                GameLog.Error("Achievement", "配置解析失败: {0}", e);
                Entries.Clear();
                Categories.Clear();
                CategoriesBySubtype.Clear();
                Types.Clear();
                Subtypes.Clear();
                Stages.Clear();
                OverviewTitles.Clear();
            }
            finally
            {
                ReleaseAll(assets);
                if (!IsLoaded) _loading = null;
            }
        }

        private static void ParseEntries(JObject root)
        {
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                long idValue = row.Value<long?>("0") ?? 0L;
                int categoryValue = row.Value<int?>("1") ?? 0;
                if (idValue <= 0 || idValue > uint.MaxValue || categoryValue <= 0 || categoryValue > byte.MaxValue)
                    continue;
                var entry = new EntryRow
                {
                    Id = (uint)idValue,
                    Category = (byte)categoryValue,
                    Star = (uint)Math.Max(0L, row.Value<long?>("2") ?? 0L),
                    Description = row.Value<string>("3") ?? string.Empty,
                    NextId = (uint)Math.Max(0L, row.Value<long?>("4") ?? 0L),
                    Inherit = (row.Value<int?>("5") ?? 0) != 0,
                    ShowProgress = (row.Value<int?>("6") ?? 0) != 0,
                    ConditionRaw = row.Value<string>("7") ?? "[]",
                    RewardRaw = row.Value<string>("8") ?? "[]",
                };
                entry.Target = ParseTarget(entry.ConditionRaw);
                ParseRewards(entry.RewardRaw, entry.Rewards);
                Entries[entry.Id] = entry;
            }
        }

        private static void ParseCategories(JObject root)
        {
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                int id = row.Value<int?>("category") ?? 0;
                int subtype = row.Value<int?>("subtype") ?? 0;
                if (id <= 0 || id > byte.MaxValue || subtype < 0 || subtype > ushort.MaxValue) continue;
                var category = new CategoryRow
                {
                    Category = (byte)id,
                    Name = row.Value<string>("name") ?? string.Empty,
                    Color = row.Value<int?>("color") ?? 0,
                    Level = row.Value<int?>("lv") ?? 0,
                    Sort = row.Value<int?>("sort") ?? 0,
                    Type = row.Value<int?>("type") ?? 0,
                    Subtype = (ushort)subtype,
                };
                Categories[category.Category] = category;
                if (!CategoriesBySubtype.TryGetValue(category.Subtype, out List<CategoryRow> list))
                    CategoriesBySubtype[category.Subtype] = list = new List<CategoryRow>();
                list.Add(category);
            }
            foreach (List<CategoryRow> rows in CategoriesBySubtype.Values)
                rows.Sort((a, b) => a.Sort != b.Sort
                    ? a.Sort.CompareTo(b.Sort)
                    : a.Category.CompareTo(b.Category));
        }

        private static void ParseTypes(JObject root)
        {
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                int id = row.Value<int?>("id") ?? 0;
                if (id <= 0) continue;
                var type = new TypeRow { Id = id, Name = row.Value<string>("desc") ?? string.Empty };
                string raw = row.Value<string>("subtypes") ?? "[]";
                foreach (JToken token in JArray.Parse(raw))
                {
                    int subtype = token.Value<int>();
                    if (subtype > 0 && subtype <= ushort.MaxValue) type.Subtypes.Add((ushort)subtype);
                }
                Types.Add(type);
            }
            Types.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        private static void ParseSubtypes(JObject root)
        {
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                int id = row.Value<int?>("subtype") ?? 0;
                if (id <= 0 || id > ushort.MaxValue) continue;
                Subtypes[(ushort)id] = new SubtypeRow
                {
                    Id = (ushort)id,
                    Name = row.Value<string>("desc") ?? string.Empty,
                };
            }
        }

        private static void ParseStages(JObject root)
        {
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                int stage = row.Value<int?>("stage") ?? 0;
                if (stage <= 0) continue;
                var value = new StageRow
                {
                    Stage = stage,
                    Name = row.Value<string>("name") ?? string.Empty,
                    Picture = row.Value<string>("pic") ?? string.Empty,
                    RequiredStar = (uint)Math.Max(0L, row.Value<long?>("star") ?? 0L),
                };
                string reward = row.Value<string>("reward") ?? "[]";
                foreach (JToken token in JArray.Parse(reward))
                {
                    if (!(token is JObject attr)) continue;
                    int id = attr.Value<int?>("0") ?? 0;
                    long amount = attr.Value<long?>("1") ?? 0L;
                    if (id > 0) value.Attributes.Add(new AttributeValue(id, amount));
                }
                Stages[stage] = value;
            }
        }

        private static void ParseOverviewTitles(JObject root)
        {
            foreach (JProperty property in root.Properties())
                if (uint.TryParse(property.Name, out uint id))
                    OverviewTitles[id] = property.Value?.ToString() ?? string.Empty;
        }

        private static ulong ParseTarget(string raw)
        {
            ErlangTerm root = ErlangParser.Parse(raw);
            if (root?.Items == null || root.Items.Count == 0) return 1UL;
            ErlangTerm last = root.Items[root.Items.Count - 1];
            if (last?.Items == null || last.Items.Count < 2) return 1UL;
            long value = last.Get<long>(1);
            return value > 0 ? (ulong)value : 1UL;
        }

        private static void ParseRewards(string raw, ICollection<RewardTriple> result)
        {
            ErlangTerm root = ErlangParser.Parse(raw);
            if (root?.Items == null) return;
            foreach (ErlangTerm term in root.Items)
            {
                if (term?.Items == null || term.Items.Count < 3) continue;
                result.Add(new RewardTriple(term.Get<int>(0), term.Get<int>(1), term.Get<long>(2)));
            }
        }

        private static void ReleaseAll(IEnumerable<TextAsset> assets)
        {
            foreach (TextAsset asset in assets) if (asset != null) ResManager.Release(asset);
        }
    }
}
