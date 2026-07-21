using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public static class ListDuobaoConfigs
    {
        public struct RewardEntry { public int Type; public int GoodsId; public int Num; }
        public struct CostEntry { public int Type; public int GoodsId; public int Num; }

        public sealed class StageRow
        {
            public int Type, SubType, RewardId, NeedValue, Format;
            public string Desc = "";
            public readonly List<RewardEntry> Reward = new List<RewardEntry>();
        }

        public sealed class RankRow
        {
            public int Type, SubType, RankType, RewardId, RankMin, RankMax, LimitValue, Format;
            public string Desc = "";
            public readonly List<RewardEntry> Reward = new List<RewardEntry>();
        }

        private static readonly List<StageRow> StageRows = new List<StageRow>();
        private static readonly List<RankRow> RankRows = new List<RankRow>();
        private static bool _loaded;
        private static Task _loadTask;

        public static async Task EnsureLoaded()
        {
            if (_loaded) return;
            if (_loadTask == null) _loadTask = LoadAll();
            try
            {
                await _loadTask;
            }
            finally
            {
                if (!_loaded) _loadTask = null; // 加载失败不缓存空表，下一次打开可重试。
            }
        }

        private static async Task LoadAll()
        {
            StageRows.Clear();
            RankRows.Clear();
            await LoadStage();
            await LoadRank();
            _loaded = StageRows.Count > 0 && RankRows.Count > 0;
            if (!_loaded)
                GameLog.Error("ListDuobao", "rush treasure configs incomplete: stage={0} rank={1}", StageRows.Count, RankRows.Count);
        }

        public static List<StageRow> GetStages(int type, int subType)
        {
            var result = StageRows.FindAll(v => v.Type == type && v.SubType == subType);
            result.Sort((a, b) => a.RewardId.CompareTo(b.RewardId));
            return result;
        }

        public static StageRow GetStage(int type, int subType, int rewardId) =>
            StageRows.Find(v => v.Type == type && v.SubType == subType && v.RewardId == rewardId);

        public static List<RankRow> GetRanks(int type, int subType, int rankType)
        {
            var result = RankRows.FindAll(v => v.Type == type && v.SubType == subType && v.RankType == rankType);
            result.Sort((a, b) => a.RankMin.CompareTo(b.RankMin));
            return result;
        }

        public static bool TryReadCondition(string raw, string key, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(raw)) return false;
            ErlangTerm root = ErlangParser.Parse(raw);
            if (root?.Items == null) return false;
            foreach (ErlangTerm pair in root.Items)
            {
                if (pair?.Items == null || pair.Items.Count < 2 || pair.Items[0].As<string>() != key) continue;
                number = pair.Items[1].As<int>();
                return true;
            }
            return false;
        }

        public static bool TryReadCost(string raw, string key, out CostEntry cost)
        {
            cost = default;
            if (string.IsNullOrEmpty(raw)) return false;
            ErlangTerm root = ErlangParser.Parse(raw);
            if (root?.Items == null) return false;
            foreach (ErlangTerm pair in root.Items)
            {
                if (pair?.Items == null || pair.Items.Count < 2 || pair.Items[0].As<string>() != key) continue;
                IReadOnlyList<ErlangTerm> list = pair.Items[1]?.Items;
                if (list == null || list.Count == 0) return false;
                IReadOnlyList<ErlangTerm> tuple = list[0]?.Items;
                if (tuple == null || tuple.Count < 3) return false;
                cost = new CostEntry { Type = tuple[0].As<int>(), GoodsId = tuple[1].As<int>(), Num = tuple[2].As<int>() };
                return true;
            }
            return false;
        }

        private static async Task LoadStage()
        {
            JObject table = await Load("config_rush_treasure_stage_reward");
            if (table == null) return;
            foreach (KeyValuePair<string, JToken> pair in table)
            {
                if (!(pair.Value is JObject o)) continue;
                var row = new StageRow
                {
                    Type = ReadInt(o, "type"), SubType = ReadInt(o, "sub_type"), RewardId = ReadInt(o, "reward_id"),
                    NeedValue = ReadInt(o, "need_val"), Format = ReadInt(o, "format"), Desc = (string)o["desc"] ?? "",
                };
                row.Reward.AddRange(ParseReward((string)o["reward"]));
                StageRows.Add(row);
            }
        }

        private static async Task LoadRank()
        {
            JObject table = await Load("config_rush_treasure_rank_reward");
            if (table == null) return;
            foreach (KeyValuePair<string, JToken> pair in table)
            {
                if (!(pair.Value is JObject o)) continue;
                var row = new RankRow
                {
                    Type = ReadInt(o, "type"), SubType = ReadInt(o, "sub_type"), RankType = ReadInt(o, "rank_type"),
                    RewardId = ReadInt(o, "reward_id"), RankMin = ReadInt(o, "rank_min"), RankMax = ReadInt(o, "rank_max"),
                    LimitValue = ReadInt(o, "limit_val"), Format = ReadInt(o, "format"), Desc = (string)o["desc"] ?? "",
                };
                row.Reward.AddRange(ParseReward((string)o["reward"]));
                RankRows.Add(row);
            }
        }

        private static async Task<JObject> Load(string name)
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath(name));
            if (asset == null) { GameLog.Error("ListDuobao", "missing config: {0}", name); return null; }
            JObject result = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return result;
        }

        private static List<RewardEntry> ParseReward(string raw)
        {
            var result = new List<RewardEntry>();
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            if (root?.Items == null) return result;
            foreach (ErlangTerm item in root.Items)
            {
                if (item?.Items == null || item.Items.Count < 3) continue;
                result.Add(new RewardEntry { Type = item.Items[0].As<int>(), GoodsId = item.Items[1].As<int>(), Num = item.Items[2].As<int>() });
            }
            return result;
        }

        private static int ReadInt(JObject o, string key) => int.TryParse(o?[key]?.ToString(), out int value) ? value : 0;
    }
}
