using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.AttributePotion
{
    /// <summary>仅解析 217 使用裁剪所需字段；源 JSON 由 ClientConfigSync 原样同步。</summary>
    public static class AttributePotionConfigs
    {
        public sealed class Potion { public int GoodsId; public byte Level; }
        public sealed class Limit { public int GoodsId; public int MinRoleLevel; public int MaxRoleLevel; public uint DayTimes; public ulong AllTimes; }
        private static readonly Dictionary<int, Potion> Potions = new Dictionary<int, Potion>();
        private static readonly List<Limit> Limits = new List<Limit>();
        private static Task _loading;
        public static bool IsLoaded { get; private set; }
        public static int PotionCount => Potions.Count;
        public static int LimitCount => Limits.Count;
        public static Task EnsureLoaded() => IsLoaded ? Task.CompletedTask : (_loading ?? (_loading = LoadAsync()));
        private static async Task LoadAsync()
        {
            var a = await ResManager.LoadAsync<UnityEngine.TextAsset>(GameResPath.GetServerConfigPath("config_attr_medicament"));
            var b = await ResManager.LoadAsync<UnityEngine.TextAsset>(GameResPath.GetServerConfigPath("config_attr_medicament_use_count"));
            Potions.Clear(); Limits.Clear();
            if (a != null) foreach (var p in JObject.Parse(a.text).Properties()) if (p.Value is JObject o) { int id=o.Value<int?>("good_id")??0; int lv=o.Value<int?>("lv")??0; if(id>0 && lv>0 && lv<=byte.MaxValue) Potions[id]=new Potion { GoodsId=id, Level=(byte)lv }; }
            if (b != null) foreach (var p in JObject.Parse(b.text).Properties()) if (p.Value is JObject o) { int id=o.Value<int?>("good_id")??0; if(id>0) Limits.Add(new Limit { GoodsId=id, MinRoleLevel=o.Value<int?>("min_role_lv")??0, MaxRoleLevel=o.Value<int?>("max_role_lv")??0, DayTimes=(uint)(o.Value<long?>("day_times")??0), AllTimes=(ulong)(o.Value<long?>("all_times")??0) }); }
            if (a != null) ResManager.Release(a); if (b != null) ResManager.Release(b);
            IsLoaded = true; GameLog.Info("AttributePotion", "configs potion={0}, use_count={1}", Potions.Count, Limits.Count);
        }
        public static bool TryGetPotion(int goodsId, out Potion row) => Potions.TryGetValue(goodsId, out row);
        public static bool HasPotionLevel(byte level)
        {
            foreach (Potion row in Potions.Values) if (row.Level == level) return true;
            return false;
        }
        public static bool TryGetLimit(int goodsId, int roleLevel, out Limit row)
        { for (int i=0;i<Limits.Count;i++) { var x=Limits[i]; if(x.GoodsId==goodsId && roleLevel>=x.MinRoleLevel && roleLevel<=x.MaxRoleLevel) { row=x; return true; } } row=null; return false; }
    }
}
