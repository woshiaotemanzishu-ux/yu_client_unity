using System.Collections.Generic;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神兵淬炼(精炼,15250/15251;自动循环 轮4 队列#4)数据层:equip_type → (refine, refine_high)。
    /// 对标老端 EquipModel.SetSmeltInfo/GetSmeltInfo(变量/事件全叫 Smelt,UI 文案"淬炼")。refine_high 是
    /// "历史最高精炼值",refine 是"当前生效值";单件/一键精炼成功后二者被拉平(对标 on15251:
    /// sinfo.refine = sinfo.refine_high = info.refine_high)。与 15255"神炼"(EquipRefinementController)
    /// 是两套完全独立系统,不共用本 Model。
    /// </summary>
    public sealed class EquipSmeltModel
    {
        public static readonly EquipSmeltModel Instance = new EquipSmeltModel();
        private EquipSmeltModel() { }

        private readonly Dictionary<int, (int refine, int refineHigh)> _map = new Dictionary<int, (int refine, int refineHigh)>();

        public bool HasData => _map.Count > 0;

        /// <summary>15250 查询回包套值(res==1 时由控制器调用)。</summary>
        public void Apply15250(int equipType, int refine, int refineHigh)
        {
            _map[equipType] = (refine, refineHigh);
        }

        /// <summary>15251 精炼回包套值(res==1 时由控制器调用):对标老端 on15251 —— 逐项把 refine/refine_high
        /// 都拉平到新 refine_high(不管之前有没有记录,统一以本次结果为准)。</summary>
        public void Apply15251(List<(int equipType, int refineHigh)> refineInfo)
        {
            if (refineInfo == null) return;
            foreach ((int equipType, int refineHigh) it in refineInfo)
            {
                _map[it.equipType] = (it.refineHigh, it.refineHigh);
            }
        }

        /// <summary>指定槽位当前(refine, refine_high);未查询过返回 (0,0)。</summary>
        public (int refine, int refineHigh) GetSmelt(int equipType)
            => _map.TryGetValue(equipType, out (int refine, int refineHigh) v) ? v : (0, 0);

        /// <summary>是否已经收到指定部位的 15250 权威快照。</summary>
        public bool HasSmelt(int equipType) => _map.ContainsKey(equipType);

        public void Clear() => _map.Clear();
    }
}
