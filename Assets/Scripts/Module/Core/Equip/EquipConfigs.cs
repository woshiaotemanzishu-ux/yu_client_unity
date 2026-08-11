using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备成长四件套(神兵淬炼/吞天洗魄/淬炉宗师;自动循环 轮4 队列#4)最小配置访问层,照 Skill.SkillConfigs 模式起。
    /// 对标老端 Config.PRELOAD_SERVER_CONFIG.config_equip_wash_unlock_lv / config_equip_refine_max /
    /// config_equip_whole_reward / config_equip_stone_lv(老端 EquipWashView.ts/EquipSmeltView.ts/
    /// EquipStrenMasterView.ts/EquipJewelView.ts 引用同名表)。
    ///
    /// 当前 config_equip_stone_lv 已落地；wash_unlock_lv/refine_max/whole_reward 仍可能缺失。
    /// EnsureLoaded 按 SkillConfigs 同款"缺表降级"处理：逐表找不到只记 Info 并置空，不阻断已落地表的读取。
    /// 缴费/消耗类预校验缺表时仍交服务端兜底；一键宝石升级若缺 stone_lv 则禁止猜测材料链并停止序列。
    /// </summary>
    public static class EquipConfigs
    {
        private static JObject _washUnlockLv;   // config_equip_wash_unlock_lv:pos(字符串键)→{unlock_lv,...}
        private static JObject _refineMax;      // config_equip_refine_max:equip_type→{...}(神兵淬炼消耗/上限判定,本轮暂无 getter 消费)
        private static JObject _wholeReward;    // config_equip_whole_reward:type_lv→{...}(15260/61 全身奖励阶位,本轮暂无 getter 消费)
        private static JObject _stoneLevel;     // config_equip_stone_lv:type_id→{lv,pre_lv_stone,next_lv_stone,need_num,...}
        private static JObject _strengthenMax;  // config_equip_strengthen_max:stage@color@pos -> stren_max
        private static JObject _strengthenLevel;// config_equip_stren_lv:pos@lv -> object_list/attr_list
        private static JObject _refineLevel;    // config_equip_refine_lv:pos@lv -> object_list/attr_list/stren_ratio
        private static JObject _refinement;     // config_equip_refinement:refinement_lv@suit_type -> promote/cost_list
        private static JObject _positionToSuit; // config_equip_pos2suittype:equip_type -> suit type
        private static bool _loaded;
        private static bool _stoneLoaded;
        private static Task _stoneLoading;
        private static Task _loading;

        public readonly struct StrengthAttribute
        {
            public readonly int AttrId;
            public readonly long PerLevelValue;

            public StrengthAttribute(int attrId, long perLevelValue)
            {
                AttrId = attrId;
                PerLevelValue = perLevelValue;
            }
        }

        public readonly struct StrengthLevel
        {
            public readonly int EquipType;
            public readonly int Level;
            public readonly long CoinCost;
            public readonly IReadOnlyList<StrengthAttribute> Attributes;

            public StrengthLevel(int equipType, int level, long coinCost, IReadOnlyList<StrengthAttribute> attributes)
            {
                EquipType = equipType;
                Level = level;
                CoinCost = coinCost;
                Attributes = attributes;
            }
        }

        public readonly struct WholeReward
        {
            public readonly int Id;
            public readonly int Type;
            public readonly int NeedLevel;
            public readonly int NextLevel;
            public readonly IReadOnlyList<StrengthAttribute> Attributes;
            public readonly int StrengthRatio;

            public WholeReward(int id, int type, int needLevel, int nextLevel,
                IReadOnlyList<StrengthAttribute> attributes, int strengthRatio)
            {
                Id = id;
                Type = type;
                NeedLevel = needLevel;
                NextLevel = nextLevel;
                Attributes = attributes;
                StrengthRatio = strengthRatio;
            }
        }

        public readonly struct SmeltLevel
        {
            public readonly int EquipType;
            public readonly int Level;
            public readonly int MaterialTypeId;
            public readonly long NeedNum;
            public readonly int StrengthRatio;

            public SmeltLevel(int equipType, int level, int materialTypeId, long needNum, int strengthRatio)
            {
                EquipType = equipType;
                Level = level;
                MaterialTypeId = materialTypeId;
                NeedNum = needNum;
                StrengthRatio = strengthRatio;
            }
        }

        public readonly struct StoneLevel
        {
            public readonly int Level;
            public readonly int PreviousTypeId;
            public readonly int NextTypeId;
            public readonly int NeedNum;

            public StoneLevel(int level, int previousTypeId, int nextTypeId, int needNum)
            {
                Level = level;
                PreviousTypeId = previousTypeId;
                NextTypeId = nextTypeId;
                NeedNum = needNum;
            }
        }

        public readonly struct RefinementLevel
        {
            public readonly int Level;
            public readonly int SuitType;
            public readonly int Promote;
            public readonly int MaterialTypeId;
            public readonly long NeedNum;

            public RefinementLevel(int level, int suitType, int promote, int materialTypeId, long needNum)
            {
                Level = level;
                SuitType = suitType;
                Promote = promote;
                MaterialTypeId = materialTypeId;
                NeedNum = needNum;
            }
        }

        public static bool IsLoaded => _loaded;

        public static Task EnsureLoaded()
        {
            if (_loaded) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAll());
        }

        private static async Task LoadAll()
        {
            Task stoneTask = EnsureStoneLevelLoaded();
            Task<JObject> washTask = LoadOptional("config_equip_wash_unlock_lv");
            Task<JObject> refineTask = LoadOptional("config_equip_refine_max");
            Task<JObject> wholeTask = LoadOptional("config_equip_whole_reward");
            Task<JObject> maxTask = LoadOptional("config_equip_strengthen_max");
            Task<JObject> levelTask = LoadOptional("config_equip_stren_lv");
            Task<JObject> refineLevelTask = LoadOptional("config_equip_refine_lv");
            Task<JObject> refinementTask = LoadOptional("config_equip_refinement");
            Task<JObject> positionToSuitTask = LoadOptional("config_equip_pos2suittype");
            await Task.WhenAll(stoneTask, washTask, refineTask, wholeTask, maxTask, levelTask, refineLevelTask,
                refinementTask, positionToSuitTask);

            _washUnlockLv = washTask.Result;
            _refineMax = refineTask.Result;
            _wholeReward = wholeTask.Result;
            _strengthenMax = maxTask.Result;
            _strengthenLevel = levelTask.Result;
            _refineLevel = refineLevelTask.Result;
            _refinement = refinementTask.Result;
            _positionToSuit = positionToSuitTask.Result;
            _loaded = true;
            _loading = null;
        }

        public static Task EnsureStoneLevelLoaded()
        {
            if (_stoneLoaded) return Task.CompletedTask;
            return _stoneLoading ?? (_stoneLoading = LoadStoneLevel());
        }

        private static async Task LoadStoneLevel()
        {
            _stoneLevel = await LoadOptional("config_equip_stone_lv");
            _stoneLoaded = true;
            _stoneLoading = null;
        }

        private static async Task<JObject> LoadOptional(string name)
        {
            string key = GameResPath.GetServerConfigPath(name);
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Info("Equip", "缺表 {0}(本轮未同步落地),对应客户端预校验不拦截、直接发协议(服务端兜底)", name);
                return null;
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>洗魄槽解锁等级门槛(config_equip_wash_unlock_lv[pos].unlock_lv);缺表/缺项 → false(不拦截)。</summary>
        public static bool TryGetWashUnlockLv(int pos, out int unlockLv)
        {
            unlockLv = 0;
            if (_washUnlockLv?[pos.ToString()] is JObject o)
            {
                unlockLv = o.Value<int?>("unlock_lv") ?? 0;
                return unlockLv > 0;
            }
            return false;
        }

        /// <summary>宝石等级/前后级/升级材料数；供一键升级按老端低等级、低槽位顺序串行选择。</summary>
        public static bool TryGetStoneLevel(int typeId, out StoneLevel value)
        {
            value = default;
            if (!(_stoneLevel?[typeId.ToString()] is JObject row)) return false;

            value = new StoneLevel(
                row.Value<int?>("lv") ?? 0,
                row.Value<int?>("pre_lv_stone") ?? 0,
                row.Value<int?>("next_lv_stone") ?? 0,
                row.Value<int?>("need_num") ?? 0);
            return value.Level > 0 && value.NeedNum > 0;
        }

        /// <summary>读取指定装备阶数、品质和部位的权威强化上限。</summary>
        public static bool TryGetStrengthenMax(int stage, int color, int equipType, out int maxLevel)
        {
            maxLevel = 0;
            if (_strengthenMax?[$"{stage}@{color}@{equipType}"] is JObject row)
            {
                maxLevel = row.Value<int?>("stren_max") ?? 0;
                return maxLevel > 0;
            }
            return false;
        }

        public static bool TryGetRefineMax(int stage, int color, int equipType, out int maxLevel)
        {
            maxLevel = 0;
            if (_refineMax?[$"{stage}@{color}@{equipType}"] is JObject row)
            {
                maxLevel = row.Value<int?>("refine_max") ?? 0;
                return maxLevel > 0;
            }
            return false;
        }

        public static bool TryGetSmeltLevel(int equipType, int level, out SmeltLevel value)
        {
            value = default;
            if (!(_refineLevel?[$"{equipType}@{Math.Max(0, level)}"] is JObject row)) return false;
            int materialTypeId = 0;
            long needNum = 0;
            string raw = row.Value<string>("object_list") ?? "[]";
            JArray costs = JArray.Parse(raw);
            if (costs.Count > 0 && costs[0] is JObject cost)
            {
                materialTypeId = cost.Value<int?>("type_id") ?? cost.Value<int?>("1") ?? 0;
                needNum = cost.Value<long?>("num") ?? cost.Value<long?>("2") ?? 0;
            }
            value = new SmeltLevel(
                row.Value<int?>("part") ?? equipType,
                row.Value<int?>("refine") ?? level,
                materialTypeId,
                needNum,
                row.Value<int?>("stren_ratio") ?? 0);
            return materialTypeId > 0 && needNum > 0;
        }

        public static int GetCumulativeSmeltRatio(int equipType, int level)
        {
            // 老端累计区间是 [0, level)：零淬炼必须显示 0%，第 0 行只作为下一次 +ratio 预览。
            if (level <= 0) return 0;
            int total = 0;
            for (int i = 0; i < level; i++)
                if (TryGetSmeltLevel(equipType, i, out SmeltLevel row)) total += row.StrengthRatio;
            return total;
        }

        public static bool TryGetRefinementLevel(int equipType, int level, out RefinementLevel value)
        {
            value = default;
            if (equipType <= 0 || level <= 0) return false;
            if (!(_positionToSuit?[equipType.ToString()] is JObject posRow)) return false;
            int suitType = posRow.Value<int?>("type") ?? 0;
            if (suitType <= 0 || !(_refinement?[$"{level}@{suitType}"] is JObject row)) return false;

            int materialTypeId = 0;
            long needNum = 0;
            string raw = row.Value<string>("cost_list") ?? "[]";
            JArray costs = JArray.Parse(raw);
            if (costs.Count > 0 && costs[0] is JObject cost)
            {
                (materialTypeId, _) = GoodsModel.GetMappingTypeId(
                    cost.Value<int?>("0") ?? 0,
                    cost.Value<int?>("1") ?? 0);
                needNum = cost.Value<long?>("2") ?? 0;
            }
            value = new RefinementLevel(
                row.Value<int?>("refine_lv") ?? level,
                row.Value<int?>("refine_pos") ?? suitType,
                row.Value<int?>("promote") ?? 0,
                materialTypeId,
                needNum);
            return value.Promote > 0 && value.MaterialTypeId > 0 && value.NeedNum > 0;
        }

        public static long CalculateRefinementBonus(long attrValue, int promote)
        {
            if (attrValue <= 0 || promote <= 0) return 0;
            return (long)Math.Floor(attrValue * promote / 10000d + 0.5d);
        }

        /// <summary>
        /// 当前等级强化展示行。老端以 config_equip_stren_lv[pos@lv].attr_list 的单级值乘当前等级，
        /// 并以 object_list 首项作为升到下一级的铜币消耗。
        /// </summary>
        public static bool TryGetStrengthLevel(int equipType, int level, out StrengthLevel value)
        {
            value = default;
            if (!(_strengthenLevel?[$"{equipType}@{Math.Max(0, level)}"] is JObject row)) return false;

            long coinCost = 0;
            ErlangTerm costs = ErlangParser.Parse(ReadRowString(row, "object_list", "2") ?? "[]");
            if (costs?.Items != null && costs.Items.Count > 0 && costs.Items[0]?.Items?.Count >= 3)
                coinCost = costs.Items[0].Get<long>(2);

            var attrs = new List<StrengthAttribute>();
            ErlangTerm attrList = ErlangParser.Parse(ReadRowString(row, "attr_list", "3") ?? "[]");
            if (attrList?.Items != null)
            {
                foreach (ErlangTerm tuple in attrList.Items)
                {
                    if (tuple?.Items == null || tuple.Items.Count < 2) continue;
                    attrs.Add(new StrengthAttribute(tuple.Get<int>(0), tuple.Get<long>(1)));
                }
            }

            value = new StrengthLevel(equipType, level, coinCost, attrs);
            return attrs.Count > 0;
        }

        /// <summary>按老端 Util.coe_arr 计算强化属性战力；特殊百分比属性不在强化表时保持 0。</summary>
        public static long CalculateStrengthPower(IReadOnlyList<StrengthAttribute> attributes, int level)
        {
            if (attributes == null || level <= 0) return 0;
            double power = 0;
            for (int i = 0; i < attributes.Count; i++)
                power += GetPowerCoefficient(attributes[i].AttrId) * attributes[i].PerLevelValue * level;
            return (long)Math.Round(power, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 对标老端 GetWholeAwardCfg：progress 落在 need_lv..next_nlv 时返回当前档；
        /// 未达到首档时 current=false、next=首档，超过末档时 next=false。
        /// </summary>
        public static void GetWholeRewardPair(int type, int progress, out bool hasCurrent,
            out WholeReward current, out bool hasNext, out WholeReward next)
        {
            current = default;
            next = default;
            hasCurrent = false;
            hasNext = false;
            if (_wholeReward == null || type <= 0) return;

            int id = type * 1000;
            while (_wholeReward[id.ToString()] is JObject row)
            {
                WholeReward parsed = ParseWholeReward(row);
                if (progress >= parsed.NeedLevel && progress <= parsed.NextLevel)
                {
                    current = parsed;
                    hasCurrent = true;
                    if (_wholeReward[(id + 1).ToString()] is JObject nextRow)
                    {
                        next = ParseWholeReward(nextRow);
                        hasNext = true;
                    }
                    return;
                }
                if (!hasNext && progress < parsed.NeedLevel)
                {
                    next = parsed;
                    hasNext = true;
                    return;
                }
                id++;
            }
        }

        public static bool TryGetNextWholeReward(int type, int activatedLevel, out WholeReward next)
        {
            next = default;
            if (_wholeReward == null || type <= 0) return false;
            int id = type * 1000;
            while (_wholeReward[id.ToString()] is JObject row)
            {
                WholeReward parsed = ParseWholeReward(row);
                if (parsed.NeedLevel > activatedLevel)
                {
                    next = parsed;
                    return true;
                }
                id++;
            }
            return false;
        }

        private static WholeReward ParseWholeReward(JObject row)
        {
            var attrs = new List<StrengthAttribute>();
            string raw = row.Value<string>("attr_list") ?? "[]";
            JArray list = JArray.Parse(raw);
            foreach (JToken token in list)
            {
                if (!(token is JObject attr)) continue;
                int attrId = attr.Value<int?>("attr_id") ?? attr.Value<int?>("0") ?? 0;
                long value = attr.Value<long?>("attr_val") ?? attr.Value<long?>("1") ?? 0;
                attrs.Add(new StrengthAttribute(attrId, value));
            }
            return new WholeReward(
                row.Value<int?>("id") ?? 0,
                row.Value<int?>("type") ?? 0,
                row.Value<int?>("need_lv") ?? 0,
                row.Value<int?>("next_nlv") ?? 0,
                attrs,
                row.Value<int?>("stren_ratio") ?? 0);
        }

        private static double GetPowerCoefficient(int attrId)
        {
            switch (attrId)
            {
                case 1: case 3: case 4: case 5: case 6: case 15: case 16: return 10d;
                case 2: return 0.5d;
                case 7: case 8: return 5d;
                case 46: case 47: return 7.5d;
                case 300: case 301: case 302: case 303: case 304:
                case 305: case 306: case 307: case 308: return 1d;
                default: return 0d;
            }
        }

        private static string ReadRowString(JObject row, string name, string compactKey)
        {
            return row?.Value<string>(name) ?? row?.Value<string>(compactKey);
        }

        public static void Clear()
        {
            _washUnlockLv = null;
            _refineMax = null;
            _wholeReward = null;
            _stoneLevel = null;
            _strengthenMax = null;
            _strengthenLevel = null;
            _refineLevel = null;
            _refinement = null;
            _positionToSuit = null;
            _loaded = false;
            _stoneLoaded = false;
            _stoneLoading = null;
            _loading = null;
        }
    }
}
