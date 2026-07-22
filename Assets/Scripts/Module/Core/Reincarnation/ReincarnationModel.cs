using System.Collections.Generic;

namespace Shenxiao.Module.Core.Reincarnation
{
    /// <summary>16400 天命觉醒激活快照；保留服务端顺序与重复项，不推导等级或下一阶段。</summary>
    public sealed class ReincarnationModel
    {
        public static readonly ReincarnationModel Instance = new ReincarnationModel();
        private readonly List<uint> _activeIds = new List<uint>();
        private ReincarnationModel() { }
        public IReadOnlyList<uint> ActiveIds => _activeIds;
        public bool HasData { get; private set; }
        public void ReplaceData(List<uint> activeIds)
        {
            _activeIds.Clear();
            if (activeIds != null) _activeIds.AddRange(activeIds);
            HasData = true;
        }
        public void Reset() { _activeIds.Clear(); HasData = false; }
    }
}
