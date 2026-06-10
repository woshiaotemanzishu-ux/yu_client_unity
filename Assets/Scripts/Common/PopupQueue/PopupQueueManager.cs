using System;
using System.Collections.Generic;

namespace Shenxiao.Common.PopupQueue
{
    /// <summary>
    /// Priority popup queue. Higher priority pops first; same priority FIFO.
    /// Phase 0 skeleton; integration with ViewManager added in Phase 1.
    /// </summary>
    public static class PopupQueueManager
    {
        public class Entry
        {
            public int Priority;
            public Action Show;
        }

        private static readonly List<Entry> _queue = new List<Entry>();
        private static bool _busy;

        public static void Enqueue(int priority, Action show)
        {
            _queue.Add(new Entry { Priority = priority, Show = show });
            _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            TryShowNext();
        }

        public static void NotifyClosed()
        {
            _busy = false;
            TryShowNext();
        }

        private static void TryShowNext()
        {
            if (_busy || _queue.Count == 0) return;
            var e = _queue[0];
            _queue.RemoveAt(0);
            _busy = true;
            e.Show?.Invoke();
        }
    }
}
