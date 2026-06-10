using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Common.RedDot
{
    /// <summary>
    /// Hierarchical red-dot system. Modules call SetCount(id, n); UI listens to changes via events.
    /// Phase 0 skeleton: flat dictionary, no parent propagation. Replace with tree-based later.
    /// </summary>
    public static class RedDotManager
    {
        public const string EVT_CHANGED = "EVT_RED_DOT_CHANGED";

        private static readonly Dictionary<int, int> _counts = new Dictionary<int, int>();

        public static int GetCount(int id)
        {
            return _counts.TryGetValue(id, out var n) ? n : 0;
        }

        public static bool IsActive(int id) => GetCount(id) > 0;

        public static void SetCount(int id, int count)
        {
            if (count < 0) count = 0;
            int old = GetCount(id);
            if (old == count) return;
            _counts[id] = count;
            EventDispatcher.Emit(EVT_CHANGED, id, count);
            GameLog.Debug("RedDot", "id={0} {1}->{2}", id, old, count);
        }

        public static void Clear(int id) => SetCount(id, 0);
        public static void ClearAll() { _counts.Clear(); }
    }
}
