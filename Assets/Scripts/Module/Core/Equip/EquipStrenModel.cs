using System.Collections.Generic;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备强化数据层(对标老端 EquipController.ts on15204/on15205;协议段 pt_152)。
    /// 槽位(equip_type)→ 强化等级(stren)的纯字典,供主线 100720(ctype31 StrenSum:全身强化等级总和≥8)判定用。
    /// 消耗铜币/物品等展示细节留后,本轮只落强化等级数据。
    /// </summary>
    public sealed class EquipStrenModel
    {
        public static readonly EquipStrenModel Instance = new EquipStrenModel();
        private EquipStrenModel() { }

        private readonly Dictionary<int, int> _strenMap = new Dictionary<int, int>();

        public bool HasData => _strenMap.Count > 0;

        /// <summary>15204 查询回包套值(res==1 时由控制器调用)。</summary>
        public void Apply15204(int equipType, int stren)
        {
            _strenMap[equipType] = stren;
        }

        /// <summary>15205 强化回包套值(res==1 时由控制器逐项调用覆盖 stren_info 列表)。</summary>
        public void Apply15205(List<(int equipType, int stren)> strenInfo)
        {
            if (strenInfo == null) return;
            foreach ((int equipType, int stren) in strenInfo)
            {
                _strenMap[equipType] = stren;
            }
        }

        /// <summary>指定槽位当前强化等级(未查询过返回 0)。</summary>
        public int GetStren(int equipType)
        {
            return _strenMap.TryGetValue(equipType, out int v) ? v : 0;
        }

        /// <summary>全身已穿戴装备强化等级总和(主线 100720 ctype31 StrenSum 判定依据)。</summary>
        public int TotalStren()
        {
            int sum = 0;
            foreach (int v in _strenMap.Values) sum += v;
            return sum;
        }

        public void Clear()
        {
            _strenMap.Clear();
        }
    }
}
