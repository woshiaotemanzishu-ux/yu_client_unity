using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Halo
{
    /// <summary>
    /// 光环(Halo)数据层(自动循环 轮18 PK2;对标老端 commonModel/HaloModel.ts,服务端 pt_514,3 号全活)。
    /// 51400 落 EndTime + Rewards(领奖态)+ SettingList(特权设置);51401 领奖单条套值;51402 特权设置单条套值。
    /// </summary>
    public sealed class HaloModel
    {
        public static readonly HaloModel Instance = new HaloModel();
        private HaloModel() { }

        public uint EndTime { get; private set; }
        public bool HasData { get; private set; }

        private readonly List<(int Id, int State)> _rewards = new List<(int Id, int State)>();
        public IReadOnlyList<(int Id, int State)> Rewards => _rewards;

        public int GetRewardState(int id)
        {
            int index = _rewards.FindIndex(entry => entry.Id == id);
            return index >= 0 ? _rewards[index].State : 0;
        }

        /// <summary>51400 SettingList[u16×item_to_bin_1{HaloId:16,Type:16,State:8}] 命名错位存档
        /// (pt_514.erl:84-94):老端 HaloController.ts:80-85 用 v.halo_id 当业务"特权类型"(HaloPrivilegeType 枚举,
        /// 如 ArenaSweep=3/DungeonSweep=5),v.type 是子域(如 DUN_TYPE.Equip/Dragon);wire 字段名 "Type" 反而
        /// 不是业务语义上的"类型"。镜像老端 setting_data_dic 双层字典:haloId → type → state。</summary>
        private readonly Dictionary<int, Dictionary<int, int>> _settingData = new Dictionary<int, Dictionary<int, int>>();

        /// <summary>51400 全量落地(对标 SetHaloData + On51400 对 SettingList 的过滤/赋值逻辑,本端不做
        /// ArenaSweep/DungeonSweep 过滤——数据层全量保留,过滤留 UI 消费方按需筛选)。</summary>
        public void ApplyInfo(uint endTime, List<(int Id, int State)> rewards, List<(int HaloId, int Type, int State)> settingList)
        {
            EndTime = endTime;
            _rewards.Clear();
            if (rewards != null) _rewards.AddRange(rewards);
            _settingData.Clear();
            if (settingList != null)
            {
                foreach ((int haloId, int type, int state) in settingList) SetSetting(haloId, type, state);
            }
            HasData = true;
        }

        /// <summary>51401 领奖后单条套值(对标 ShowHaloReward:同 id 更新 state,否则新增)。m2存档:
        /// 老端 ShowHaloReward(HaloModel.ts:290-318)有 quirk——for 循环里首个元素 id 一旦不匹配就立刻
        /// insert+break 整个退出,只有目标恰好是首个元素时才能真正命中原地更新,否则会在还没扫到真正
        /// 匹配项前就误插一条重复记录。本端改用 FindIndex 全表扫描定位,不复刻该 bug,按语义修正版实现
        /// (命中则原地更新,未命中才新增)。</summary>
        public void ApplyReward(int id, int state)
        {
            int idx = _rewards.FindIndex(e => e.Id == id);
            if (idx >= 0) _rewards[idx] = (id, state);
            else _rewards.Add((id, state));
        }

        /// <summary>51402 特权设置套值(对标 SetSettingData);haloId 即业务"特权类型"(该枚举实际落在 wire 的
        /// HaloId 槽位,见类头注释)。</summary>
        public void SetSetting(int haloId, int type, int state)
        {
            if (!_settingData.TryGetValue(haloId, out Dictionary<int, int> inner))
            {
                inner = new Dictionary<int, int>();
                _settingData[haloId] = inner;
            }
            inner[type] = state;
        }

        /// <summary>缺数据/缺项一律返回 0(不臆造),对标老端 GetSettingData 未命中时 undefined 的降级读法。</summary>
        public int GetSetting(int haloId, int type)
        {
            return _settingData.TryGetValue(haloId, out Dictionary<int, int> inner) && inner.TryGetValue(type, out int v) ? v : 0;
        }

        /// <summary>对标老端 GetHaloOpenState(仅时间窗判定,GetOpenState 的功能开关/白名单条件留 UI 层)。</summary>
        public bool IsOpen(long serverTimeSec) => HasData && EndTime > serverTimeSec;

        public void Reset()
        {
            EndTime = 0;
            HasData = false;
            _rewards.Clear();
            _settingData.Clear();
        }
    }

    /// <summary>
    /// config_hero_halo(具名键 id/picture/desc/reward/condition/weight/value,主键=id 字符串,9 条)读取器。
    /// 表经 ClientConfigSync 从 yu_client cdn 同步(P0 已搬运)。
    /// </summary>
    public static class HaloConfigs
    {
        public sealed class Reward
        {
            public int Type;
            public int TypeId;
            public int Count;
        }

        public sealed class Entry
        {
            public int Id;
            public string Picture = "";
            public string Description = "";
            public string SupplementDescription = "";
            public int Weight;
            public string ConditionType = "";
            public int ConditionValue;
            public readonly List<Reward> Rewards = new List<Reward>();
        }

        private static JObject _cfg;
        private static readonly List<Entry> _entries = new List<Entry>();

        public static bool IsLoaded => _cfg != null;
        public static int Count => _cfg?.Count ?? 0;
        public static IReadOnlyList<Entry> Entries => _entries;

        public static async Task EnsureLoaded()
        {
            if (_cfg != null) return;
            string key = GameResPath.GetServerConfigPath("config_hero_halo");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Halo", "missing config_hero_halo: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _cfg = new JObject();
                return;
            }
            _cfg = JObject.Parse(asset.text);
            ResManager.Release(asset);
            ParseEntries();
            GameLog.Info("Halo", "config_hero_halo={0}", _cfg.Count);
        }

        public static JObject Get(int id) => _cfg?[id.ToString()] as JObject;

        private static void ParseEntries()
        {
            _entries.Clear();
            if (_cfg == null) return;
            foreach (JProperty property in _cfg.Properties())
            {
                if (!(property.Value is JObject row)) continue;
                var entry = new Entry
                {
                    Id = row.Value<int?>("id") ?? 0,
                    Picture = row.Value<string>("picture") ?? "",
                    Description = row.Value<string>("desc") ?? "",
                    SupplementDescription = row.Value<string>("supplement_desc") ?? "",
                    Weight = row.Value<int?>("weight") ?? 0
                };
                ParseCondition(row.Value<string>("condition"), entry);
                ParseRewards(row.Value<string>("reward"), entry.Rewards);
                if (entry.Id > 0) _entries.Add(entry);
            }
            _entries.Sort((left, right) => right.Weight.CompareTo(left.Weight));
        }

        private static void ParseCondition(string raw, Entry entry)
        {
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            IReadOnlyList<ErlangTerm> list = root?.Items;
            ErlangTerm tuple = list != null && list.Count > 0 ? list[0] : null;
            if (tuple?.Items == null || tuple.Items.Count < 2) return;
            entry.ConditionType = tuple.Get<string>(0) ?? "";
            entry.ConditionValue = tuple.Get<int>(1);
        }

        private static void ParseRewards(string raw, List<Reward> target)
        {
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            IReadOnlyList<ErlangTerm> list = root?.Items;
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                ErlangTerm tuple = list[i];
                if (tuple?.Items == null || tuple.Items.Count < 3) continue;
                target.Add(new Reward
                {
                    Type = tuple.Get<int>(0),
                    TypeId = tuple.Get<int>(1),
                    Count = tuple.Get<int>(2)
                });
            }
        }
    }
}
