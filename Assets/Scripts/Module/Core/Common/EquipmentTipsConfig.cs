using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>EquipToolTips 专用的老端权威配表访问器。</summary>
    public static class EquipmentTipsConfig
    {
        public readonly struct AttrValue
        {
            public readonly int AttrId;
            public readonly long Value;
            public AttrValue(int attrId, long value) { AttrId = attrId; Value = value; }
        }

        public readonly struct StoneUnlock
        {
            public readonly int Stage;
            public readonly int Vip;
            public StoneUnlock(int stage, int vip) { Stage = stage; Vip = vip; }
        }

        private static readonly string[] LightColors =
        {
            "#fefaf0", "#4ec279", "#5aaff0", "#c36ff2", "#f88452",
            "#fa4d4d", "#ffbc3d", "#ff72c2", "#9c9c9c"
        };

        private static readonly string[] DarkColors =
        {
            "#663915", "#3cad66", "#5099dd", "#b55eec", "#e17547",
            "#ef4848", "#cd9222", "#f56ebd", "#8a8a8a"
        };

        private static readonly string[] ColorNames =
        {
            "白", "绿", "蓝", "紫", "橙", "红", "暗金", "粉", "无"
        };

        private static JObject _strengthen;
        private static JObject _stoneUnlock;
        private static JObject _stoneLevel;
        private static JObject _reincarnation;
        private static Task _loading;

        public static bool IsLoaded => _strengthen != null && _stoneUnlock != null && _stoneLevel != null && _reincarnation != null;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAll());
        }

        private static async Task LoadAll()
        {
            _strengthen = await Load("config_equip_stren_lv");
            _stoneUnlock = await Load("config_equip_stone_pos_unlock");
            _stoneLevel = await Load("config_equip_stone_lv");
            _reincarnation = await Load("config_reincarnation_cfg");
            _loading = null;
        }

        private static async Task<JObject> Load(string name)
        {
            string key = GameResPath.GetServerConfigPath(name);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Common", "装备详情缺配置 {0}: {1}", name, key);
                return new JObject();
            }

            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        public static IReadOnlyList<AttrValue> GetStrengthenAttrs(int equipType, int strengthenLevel)
        {
            var result = new List<AttrValue>();
            if (strengthenLevel <= 0 || _strengthen == null || !(_strengthen[equipType + "@1"] is JObject row))
                return result;

            ErlangTerm list = ErlangParser.Parse(row["3"]?.ToString());
            if (list?.Items == null) return result;
            foreach (ErlangTerm pair in list.Items)
            {
                if (!pair.IsCollection || pair.Items == null || pair.Items.Count < 2) continue;
                result.Add(new AttrValue(pair.Get<int>(0), pair.Get<long>(1) * strengthenLevel));
            }
            return result;
        }

        public static StoneUnlock GetStoneUnlock(int equipType, int position)
        {
            if (_stoneUnlock == null || !(_stoneUnlock[equipType + "@" + position] is JObject row))
                return default;

            string raw = row["condition"]?.ToString();
            if (string.IsNullOrEmpty(raw) || raw == "[]") return default;
            JArray conditions = JArray.Parse(raw);
            if (conditions.Count == 0 || !(conditions[0] is JObject cond)) return default;
            string kind = cond["0"]?.ToString();
            if (kind == "stage") return new StoneUnlock(cond["1"]?.Value<int>() ?? 0, 0);
            if (kind == "vip") return new StoneUnlock(0, cond["2"]?.Value<int>() ?? 0);
            return default;
        }

        public static IReadOnlyList<AttrValue> GetStoneAttrs(int stoneTypeId)
        {
            var result = new List<AttrValue>();
            if (_stoneLevel == null || !(_stoneLevel[stoneTypeId.ToString()] is JObject row)) return result;
            string raw = row["attr"]?.ToString();
            if (string.IsNullOrEmpty(raw) || raw == "[]") return result;
            JArray attrs = JArray.Parse(raw);
            foreach (JToken token in attrs)
            {
                if (!(token is JObject pair)) continue;
                result.Add(new AttrValue(pair["0"]?.Value<int>() ?? 0, pair["1"]?.Value<long>() ?? 0L));
            }
            return result;
        }

        public static string GetCareerRequirementName(GoodsModel.GoodsBasic basic)
        {
            if (basic == null) return "通用";
            if (_reincarnation != null && _reincarnation[basic.CareerId + "@" + basic.Sex + "@" + basic.Turn] is JObject row)
            {
                string name = row["name"]?.ToString();
                if (!string.IsNullOrEmpty(name)) return name;
            }

            if (basic.Turn == 0)
            {
                if (basic.Sex == 1) return "男性职业";
                if (basic.Sex == 2) return "女性职业";
                return "通用";
            }

            if (basic.Sex == 0) return basic.Turn + "转通用";
            return basic.Turn + "转" + (basic.Sex == 1 ? "男职" : "女职");
        }

        public static string GetLightColor(int color) => ColorAt(LightColors, color);
        public static string GetDarkColor(int color) => ColorAt(DarkColors, color);
        public static string GetColorName(int color) => ColorAt(ColorNames, color);

        private static string ColorAt(string[] values, int index)
        {
            if (index < 0 || index >= values.Length) index = values.Length - 1;
            return values[index];
        }
    }
}
