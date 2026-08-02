using System.Collections.Generic;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// 功能开放/功能预告数据(对标老客户端 FunctionOpenModel,服务端 pt_138)。
    /// FinishState：服务器 13800 的「已开放功能」id → state(0未领/1可领/2已领之类)。
    /// 预告逻辑(对标老端 ShowFuncOpenIcons/NeedShowFunctionOpenIcon/HaveRewardCanGet):从 config_module_open(is_hide==0)
    /// 里挑「未开放(不在 FinishState) + 非 id99 敬请期待 + 等级未满足」的功能,按 sort_no→task>lv>open_day→id 排序,给入口框轮播/列表。
    /// 调用这些方法前需先 await <see cref="FunctionOpenConfigs"/>.EnsureLoaded()。
    /// </summary>
    public sealed class FunctionOpenModel
    {
        public static readonly FunctionOpenModel Instance = new FunctionOpenModel();
        private FunctionOpenModel() { }

        public readonly Dictionary<int, int> FinishState = new Dictionary<int, int>(); // id -> state(服务器13800)

        private List<FunctionOpenConfigs.Cfg> _showList; // is_hide==0,按 SortFuncOpen 排(对标 real_show_open_list)

        public void SetAll(Dictionary<int, int> map)
        {
            FinishState.Clear();
            if (map == null) return;
            foreach (KeyValuePair<int, int> kv in map) FinishState[kv.Key] = kv.Value;
        }

        public void SetState(int id, int state) => FinishState[id] = state;

        public void Clear()
        {
            FinishState.Clear();
            _showList = null;
        }

        /// <summary>整个展示列表(is_hide==0,SortFuncOpen 排);config 须先 EnsureLoaded。</summary>
        private void EnsureShowList()
        {
            if (_showList != null) return;
            IReadOnlyDictionary<int, FunctionOpenConfigs.Cfg> all = FunctionOpenConfigs.All;
            if (all == null || all.Count == 0) return; // config 还没加载好:别缓存空列表(否则加载好后 _showList!=null 不再重建→图标永远空)
            var list = new List<FunctionOpenConfigs.Cfg>();
            foreach (KeyValuePair<int, FunctionOpenConfigs.Cfg> kv in all)
                if (kv.Value.IsHide == 0) list.Add(kv.Value);
            list.Sort(SortFuncOpen);
            _showList = list;
        }

        /// <summary>即将开放的功能(最多 2 个,入口框轮播)。对标老端 ShowFuncOpenIcons:一个低配(最靠前)+一个高配(末尾)。</summary>
        public List<FunctionOpenConfigs.Cfg> ShowFuncOpenIcons()
        {
            EnsureShowList();
            var icons = new List<FunctionOpenConfigs.Cfg>();
            if (_showList == null || _showList.Count == 0) return icons;
            int roleLv = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;

            // 高配:从末尾往前,遇到已开放停;取该未开放段最靠前的一个(跳 id99)。
            FunctionOpenConfigs.Cfg maxData = null;
            for (int i = _showList.Count - 1; i >= 0; i--)
            {
                FunctionOpenConfigs.Cfg c = _showList[i];
                if (FinishState.ContainsKey(c.Id)) break;
                if (c.Id == 99) continue;
                maxData = c;
            }
            // 低配:从头找第一批「未开放 + 非99 + 等级未满足」。
            for (int i = 0; i < _showList.Count; i++)
            {
                FunctionOpenConfigs.Cfg c = _showList[i];
                if (FinishState.ContainsKey(c.Id)) continue;
                if (c.Id == 99) continue;
                if (LvSatisfied(c, roleLv)) continue;
                icons.Add(c);
                if (maxData != null && maxData.Id != c.Id) icons.Add(maxData);
                if (icons.Count >= 2) break;
            }
            return icons;
        }

        /// <summary>第一个即将开放的功能(入口框显隐判定);无则 null。</summary>
        public FunctionOpenConfigs.Cfg NeedShowFunctionOpenIcon()
        {
            EnsureShowList();
            if (_showList == null) return null;
            int roleLv = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;
            for (int i = 0; i < _showList.Count; i++)
            {
                FunctionOpenConfigs.Cfg c = _showList[i];
                if (FinishState.ContainsKey(c.Id)) continue;
                if (c.Id == 99) continue;
                if (LvSatisfied(c, roleLv)) continue;
                return c;
            }
            return null;
        }

        /// <summary>是否有功能开放奖励可领(FinishState state 0/1 且该功能配了奖励)。</summary>
        public bool HaveRewardCanGet()
        {
            foreach (KeyValuePair<int, int> kv in FinishState)
            {
                if (kv.Value != 0 && kv.Value != 1) continue;
                FunctionOpenConfigs.Cfg cfg = FunctionOpenConfigs.Get(kv.Key);
                if (cfg != null && cfg.Rewards.Count > 0) return true;
            }
            return false;
        }

        /// <summary>condition 里有 lv 且 lv<=roleLv → 等级已满足(不再算「即将开放」)。</summary>
        private static bool LvSatisfied(FunctionOpenConfigs.Cfg c, int roleLv)
        {
            for (int i = 0; i < c.Conditions.Count; i++)
                if (c.Conditions[i].Type == "lv" && c.Conditions[i].Val <= roleLv) return true;
            return false;
        }

        // 排序(对标老端 SortFuncOpen):sort_no → task>lv>open_day → id。
        private static int SortFuncOpen(FunctionOpenConfigs.Cfg a, FunctionOpenConfigs.Cfg b)
        {
            if (a.SortNo != b.SortNo) return a.SortNo.CompareTo(b.SortNo);
            long at = CondVal(a, "task", 100000000), bt = CondVal(b, "task", 100000000);
            if (at != bt) return at.CompareTo(bt);
            long al = CondVal(a, "lv", 0), bl = CondVal(b, "lv", 0);
            if (al != bl) return al.CompareTo(bl);
            long ad = CondVal(a, "open_day", 0), bd = CondVal(b, "open_day", 0);
            if (ad != bd) return ad.CompareTo(bd);
            return a.Id.CompareTo(b.Id);
        }

        private static long CondVal(FunctionOpenConfigs.Cfg c, string type, long def)
        {
            for (int i = 0; i < c.Conditions.Count; i++)
                if (c.Conditions[i].Type == type) return c.Conditions[i].Val;
            return def;
        }
    }
}
