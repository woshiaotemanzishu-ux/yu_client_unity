using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝装备锻造的只读配置与材料预览；实际扣料和结果以 18219 服务端回包为准。</summary>
    public static class BabyEquipUpgradeConfigs
    {
        public sealed class Material { public int TypeId; public int ExpPerItem; }
        public sealed class CostItem { public int Type; public int TypeId; public int Num; }
        public sealed class StrenCfg { public int PosId; public int Stage; public int StageLevel; public int PointCon; }
        public sealed class StageCfg { public int Stage; public readonly List<CostItem> Costs = new List<CostItem>(); }
        public sealed class PreviewResult
        {
            public bool IsStageUpgrade;
            public int RequiredExp;
            public bool Enough;
            public readonly List<CostItem> Costs = new List<CostItem>();
        }

        private static readonly List<Material> MaterialsMutable = new List<Material>();
        private static readonly IReadOnlyList<Material> EmptyMaterials = new List<Material>();
        private static Dictionary<string, StrenCfg> _stren;
        private static Dictionary<int, StageCfg> _stages;
        private static Task _loading;

        public static bool IsLoaded => _stren != null && _stages != null;
        public static IReadOnlyList<Material> Materials => IsLoaded ? MaterialsMutable : EmptyMaterials;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static StrenCfg GetStren(int posId, int stage, int stageLevel)
            => _stren != null && _stren.TryGetValue(Key(posId, stage, stageLevel), out StrenCfg value) ? value : null;

        public static StageCfg GetStage(int stage)
            => _stages != null && _stages.TryGetValue(stage, out StageCfg value) ? value : null;

        public static PreviewResult Preview(BabyEquipEntry equip)
        {
            var result = new PreviewResult();
            if (!IsLoaded || equip == null || equip.PositionId < 1 || equip.PositionId > 6) return result;
            StrenCfg next = GetStren(equip.PositionId, equip.Stage, equip.StageLevel + 1);
            if (next != null)
            {
                result.RequiredExp = System.Math.Max(0, next.PointCon - equip.StageExp);
                int remain = result.RequiredExp;
                for (int i = 0; i < MaterialsMutable.Count && remain > 0; i++)
                {
                    Material material = MaterialsMutable[i];
                    if (material.TypeId <= 0 || material.ExpPerItem <= 0) continue;
                    long have = BagModel.Instance.GetTypeGoodsNum(material.TypeId);
                    int take = (int)System.Math.Min(have, (remain + material.ExpPerItem - 1) / material.ExpPerItem);
                    if (take <= 0) continue;
                    result.Costs.Add(new CostItem { Type = 0, TypeId = material.TypeId, Num = take });
                    remain -= take * material.ExpPerItem;
                }
                result.Enough = result.RequiredExp > 0 && remain <= 0;
                return result;
            }

            if (GetStren(equip.PositionId, equip.Stage + 1, 0) == null) return result;
            StageCfg stage = GetStage(equip.Stage + 1);
            if (stage == null || stage.Costs.Count == 0) return result;
            result.IsStageUpgrade = true;
            bool enough = true;
            for (int i = 0; i < stage.Costs.Count; i++)
            {
                CostItem cost = stage.Costs[i];
                result.Costs.Add(new CostItem { Type = cost.Type, TypeId = cost.TypeId, Num = cost.Num });
                if (cost.Type != 0 || cost.TypeId <= 0 || cost.Num <= 0 || BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Num) enough = false;
            }
            result.Enough = enough;
            return result;
        }

        private static async Task LoadAsync()
        {
            var stren = new Dictionary<string, StrenCfg>();
            var stages = new Dictionary<int, StageCfg>();
            var materials = new List<Material>();
            TextAsset valueAsset = null;
            TextAsset strenAsset = null;
            TextAsset stageAsset = null;
            try
            {
                valueAsset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_baby_value"));
                strenAsset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_baby_equip_stren"));
                stageAsset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_baby_equip_stage"));
                if (valueAsset == null || strenAsset == null || stageAsset == null) return;
                foreach (JToken item in JArray.Parse((JObject.Parse(valueAsset.text)["8"] as JObject)?["value"]?.ToString() ?? "[]"))
                    if (item is JObject row) materials.Add(new Material { TypeId = Read(row, "0"), ExpPerItem = Read(row, "1") });
                foreach (KeyValuePair<string, JToken> pair in JObject.Parse(strenAsset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new StrenCfg { PosId = Read(row, "0"), Stage = Read(row, "1"), StageLevel = Read(row, "2"), PointCon = Read(row, "3") };
                    if (cfg.PosId > 0 && cfg.Stage >= 0 && cfg.StageLevel >= 0) stren[Key(cfg.PosId, cfg.Stage, cfg.StageLevel)] = cfg;
                }
                foreach (KeyValuePair<string, JToken> pair in JObject.Parse(stageAsset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new StageCfg { Stage = Read(row, "stage") };
                    foreach (JToken item in JArray.Parse(row["cost"]?.ToString() ?? "[]"))
                        if (item is JObject cost) cfg.Costs.Add(new CostItem { Type = Read(cost, "0"), TypeId = Read(cost, "1"), Num = Read(cost, "2") });
                    if (cfg.Stage > 0) stages[cfg.Stage] = cfg;
                }
                MaterialsMutable.Clear();
                MaterialsMutable.AddRange(materials);
                _stren = stren;
                _stages = stages;
            }
            catch (System.Exception e)
            {
                MaterialsMutable.Clear();
                _stren = new Dictionary<string, StrenCfg>();
                _stages = new Dictionary<int, StageCfg>();
                GameLog.Warn("Baby", "parse baby equip upgrade configs failed: {0}", e.Message);
            }
            finally
            {
                if (valueAsset != null) ResManager.Release(valueAsset);
                if (strenAsset != null) ResManager.Release(strenAsset);
                if (stageAsset != null) ResManager.Release(stageAsset);
            }
        }

        private static string Key(int posId, int stage, int stageLevel) => posId + "@" + stage + "@" + stageLevel;
        private static int Read(JObject row, string key)
            => int.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }
}
