using System.Collections.Generic;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 全身奖励(淬炉宗师/骸珀镶嵌大师共用基建,15260/15261;自动循环 轮4 队列#4)数据层:
    /// type(1强化,本轮只用/3宝石,4b 另单) → whole_lv。对标老端 EquipModel.SetMasterData/UpdateMasterData。
    /// 挂 <see cref="EquipStrenController"/>(规格 §0 指定),供 EquipStrenMasterView(type=1)消费;
    /// 4b 的骸珀镶嵌大师(type=3)复用同一 Model,不必另起。
    /// </summary>
    public sealed class EquipWholeAwardModel
    {
        public static readonly EquipWholeAwardModel Instance = new EquipWholeAwardModel();
        private EquipWholeAwardModel() { }

        private readonly Dictionary<int, int> _map = new Dictionary<int, int>();

        public bool HasData => _map.Count > 0;

        /// <summary>15261 全量套值(对标老端 SetMasterData,整表覆盖)。</summary>
        public void SetList(List<(int type, int wholeLv)> list)
        {
            _map.Clear();
            if (list == null) return;
            foreach ((int type, int wholeLv) it in list) _map[it.type] = it.wholeLv;
        }

        /// <summary>15260 激活回包套单条(对标老端 UpdateMasterData)。</summary>
        public void Update(int type, int wholeLv) => _map[type] = wholeLv;

        public int GetWholeLv(int type) => _map.TryGetValue(type, out int v) ? v : 0;

        public void Clear() => _map.Clear();
    }
}
