using System.Collections.Generic;

namespace Shenxiao.Module.Core.AttributePotion
{
    /// <summary>21701 是单等级快照，21703 是跨等级全量增量；以 (lv,goods_id) 合并，次数保留 u64。</summary>
    public sealed class AttributePotionModel
    {
        public sealed class Count { public int GoodsId; public byte Level; public uint CurrentDayCount; public ulong CurrentCount; }
        public static readonly AttributePotionModel Instance = new AttributePotionModel();
        private readonly Dictionary<int, Dictionary<int, Count>> _byLevel = new Dictionary<int, Dictionary<int, Count>>();
        private AttributePotionModel() { }
        public void Clear() => _byLevel.Clear();
        public void ReplaceLevel(byte level, IList<Count> values)
        {
            var rows = new Dictionary<int, Count>();
            for (int i = 0; i < values.Count; ++i) if (values[i].Level == level) rows[values[i].GoodsId] = values[i];
            _byLevel[level] = rows;
        }
        public void MergeAll(IList<Count> values)
        {
            for (int i = 0; i < values.Count; ++i) { Count x = values[i]; if (!_byLevel.TryGetValue(x.Level, out var rows)) _byLevel[x.Level] = rows = new Dictionary<int, Count>(); rows[x.GoodsId] = x; }
        }
        public bool TryGet(byte level, int goodsId, out Count value)
        {
            value = null;
            return _byLevel.TryGetValue(level, out var rows) && rows.TryGetValue(goodsId, out value);
        }
        public int LevelCount => _byLevel.Count;
    }
}
