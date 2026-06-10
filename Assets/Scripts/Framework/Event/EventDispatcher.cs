using System;
using System.Collections.Generic;

namespace Shenxiao.Framework.Event
{
    /// <summary>
    /// Global event dispatcher. Use string constants from GlobalEvent as keys.
    /// On/Off must be paired. Lambda registration is forbidden (cannot be unregistered).
    /// </summary>
    public static class EventDispatcher
    {
        private static readonly Dictionary<string, List<Delegate>> _handlers
            = new Dictionary<string, List<Delegate>>();

        public static void On(string evt, Action handler) => AddHandler(evt, handler);
        public static void On<T>(string evt, Action<T> handler) => AddHandler(evt, handler);
        public static void On<T1, T2>(string evt, Action<T1, T2> handler) => AddHandler(evt, handler);
        public static void On<T1, T2, T3>(string evt, Action<T1, T2, T3> handler) => AddHandler(evt, handler);

        public static void Off(string evt, Action handler) => RemoveHandler(evt, handler);
        public static void Off<T>(string evt, Action<T> handler) => RemoveHandler(evt, handler);
        public static void Off<T1, T2>(string evt, Action<T1, T2> handler) => RemoveHandler(evt, handler);
        public static void Off<T1, T2, T3>(string evt, Action<T1, T2, T3> handler) => RemoveHandler(evt, handler);

        public static void Emit(string evt)
        {
            if (!_handlers.TryGetValue(evt, out var list)) return;
            foreach (var d in list.ToArray()) (d as Action)?.Invoke();
        }

        public static void Emit<T>(string evt, T arg)
        {
            if (!_handlers.TryGetValue(evt, out var list)) return;
            foreach (var d in list.ToArray()) (d as Action<T>)?.Invoke(arg);
        }

        public static void Emit<T1, T2>(string evt, T1 a, T2 b)
        {
            if (!_handlers.TryGetValue(evt, out var list)) return;
            foreach (var d in list.ToArray()) (d as Action<T1, T2>)?.Invoke(a, b);
        }

        public static void Emit<T1, T2, T3>(string evt, T1 a, T2 b, T3 c)
        {
            if (!_handlers.TryGetValue(evt, out var list)) return;
            foreach (var d in list.ToArray()) (d as Action<T1, T2, T3>)?.Invoke(a, b, c);
        }

        public static void Clear() => _handlers.Clear();

        private static void AddHandler(string evt, Delegate handler)
        {
            if (!_handlers.TryGetValue(evt, out var list))
            {
                list = new List<Delegate>();
                _handlers[evt] = list;
            }
            list.Add(handler);
        }

        private static void RemoveHandler(string evt, Delegate handler)
        {
            if (_handlers.TryGetValue(evt, out var list)) list.Remove(handler);
        }
    }
}
