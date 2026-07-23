using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Setting;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class SettingWxSubscriptionCase
    {
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY settingwx EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            SettingController ctrl = SettingController.Instance;
            FieldInfo dataField = typeof(SettingModel).GetField("_data", SF);
            FieldInfo pendingField = typeof(SettingController).GetField("_pending", IF);
            FieldInfo interceptField = typeof(SettingController).GetField("s_outboundIntercept", SF);
            MethodInfo on = typeof(SettingController).GetMethod("On11307", IF);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool wasInit = ctrl.IsInitialized;
            object oldIntercept = interceptField?.GetValue(null);
            var data = dataField?.GetValue(null) as IDictionary;
            var pending = pendingField?.GetValue(ctrl) as IEnumerable;
            var savedData = CopyData(data);
            var savedPending = CopyPending(pending);
            bool savedHas = SettingModel.HasWxSubscriptionSwitch;
            byte savedRaw = SettingModel.WxSubscriptionSwitchRaw;
            var oldHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in new[] { 10203, 10210, 11307 }) oldHandlers[id] = HandlerState.Capture(handlers, id);
            bool pass = data != null && pendingField != null && interceptField != null && on != null && handlers != null;
            void Check(string name, bool ok) { Debug.Log("CLIVERIFY settingwx " + name + " ok=" + ok); if (!ok) pass = false; }

            try
            {
                ctrl.Init();
                Check("registration", HandlerState.Matches(handlers, 10203, oldHandlers[10203], wasInit) && HandlerState.Matches(handlers, 10210, oldHandlers[10210], wasInit)
                    && handlers.Contains(11307));
                ctrl.Init(); // BaseController must remain idempotent.
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                ctrl.RequestWxSubscriptionSwitch();
                Check("explicit exact frame", Frames(frames, 1));
                SettingModel.ApplyWxSubscriptionSwitch(1);
                frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Check("game-start clears slice and sends one", Frames(frames, 1) && !SettingModel.HasWxSubscriptionSwitch && SettingModel.WxSubscriptionSwitchRaw == 0);
                SettingModel.ApplyWxSubscriptionSwitch(255);
                frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Check("repeat-init no duplicate subscription", Frames(frames, 1) && !SettingModel.HasWxSubscriptionSwitch && SettingModel.WxSubscriptionSwitchRaw == 0);

                foreach (byte raw in new byte[] { 0, 1, 255 })
                {
                    var r = new NetReader(new[] { raw }, 0, 1);
                    on.Invoke(ctrl, new object[] { r });
                    Check("raw " + raw, r.Remaining == 0 && SettingModel.HasWxSubscriptionSwitch
                        && SettingModel.WxSubscriptionSwitchRaw == raw && SettingModel.WxSubscriptionSwitchEnabled == (raw == 1));
                }
                byte before = SettingModel.WxSubscriptionSwitchRaw;
                frames.Clear(); ctrl.RequestWxSubscriptionSwitch();
                Check("no response retains", Frames(frames, 1) && before == 255 && SettingModel.HasWxSubscriptionSwitch && SettingModel.WxSubscriptionSwitchRaw == before);

                ctrl.Dispose();
                Check("dispose unregisters and clears", !handlers.Contains(11307) && !SettingModel.HasWxSubscriptionSwitch);
                frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Check("dispose unsubscribes event", frames.Count == 0);
            }
            finally
            {
                // Remove only our active registration, then restore the original controller state once.
                if (ctrl.IsInitialized) ctrl.Dispose();
                RestoreData(data, savedData);
                RestorePending(pendingField.GetValue(ctrl), savedPending);
                if (savedHas) SettingModel.ApplyWxSubscriptionSwitch(savedRaw); else SettingModel.ClearWxSubscriptionSwitch();
                interceptField.SetValue(null, oldIntercept);
                if (wasInit) ctrl.Init();
                RestoreHandler(handlers, 10203, oldHandlers[10203]);
                RestoreHandler(handlers, 10210, oldHandlers[10210]);
                RestoreHandler(handlers, 11307, oldHandlers[11307]);
                bool restored = ctrl.IsInitialized == wasInit && DataEquals(data, savedData)
                    && PendingEquals(pendingField.GetValue(ctrl) as IEnumerable, savedPending)
                    && SettingModel.HasWxSubscriptionSwitch == savedHas && SettingModel.WxSubscriptionSwitchRaw == savedRaw
                    && ReferenceEquals(interceptField.GetValue(null), oldIntercept)
                    && HandlerEquals(handlers, 10203, oldHandlers[10203]) && HandlerEquals(handlers, 10210, oldHandlers[10210]) && HandlerEquals(handlers, 11307, oldHandlers[11307]);
                pass &= restored;
                Debug.Log("CLIVERIFY settingwx restore ok=" + restored + " initialized=" + ctrl.IsInitialized + " data=" + savedData.Count + " pending=" + savedPending.Count);
            }
            Debug.Log("CLIVERIFY settingwx VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private sealed class HandlerState
        {
            public bool Exists; public object Value;
            public static HandlerState Capture(IDictionary h, int id) => new HandlerState { Exists = h != null && h.Contains(id), Value = h != null && h.Contains(id) ? h[id] : null };
            public static bool Matches(IDictionary h, int id, HandlerState old, bool wasInit) => h.Contains(id) && (!wasInit || (old.Exists && ReferenceEquals(h[id], old.Value)));
        }
        private static void RestoreHandler(IDictionary h, int id, HandlerState s) { if (s.Exists) h[id] = s.Value; else if (h.Contains(id)) h.Remove(id); }
        private static bool HandlerEquals(IDictionary h, int id, HandlerState s) => h.Contains(id) == s.Exists && (!s.Exists || ReferenceEquals(h[id], s.Value));
        private static bool Frames(List<byte[]> f, int count) => f.Count == count && f[0].Length == 6 && f[0][0] == 0 && f[0][1] == 6 && f[0][2] == 3 && f[0][3] == 232 && f[0][4] == 0x2c && f[0][5] == 0x2b;
        private static Dictionary<int, Dictionary<int, int>> CopyData(IDictionary d) { var r = new Dictionary<int, Dictionary<int, int>>(); foreach (DictionaryEntry e in d) r[(int)e.Key] = new Dictionary<int, int>((Dictionary<int, int>)e.Value); return r; }
        private static void RestoreData(IDictionary d, Dictionary<int, Dictionary<int, int>> s) { d.Clear(); foreach (var e in s) d[e.Key] = new Dictionary<int, int>(e.Value); }
        private static bool DataEquals(IDictionary d, Dictionary<int, Dictionary<int, int>> s)
        {
            if (d.Count != s.Count) return false;
            foreach (var e in s)
            {
                if (!d.Contains(e.Key) || !((Dictionary<int, int>)d[e.Key]).Count.Equals(e.Value.Count)) return false;
                foreach (var pair in e.Value) if (!((Dictionary<int, int>)d[e.Key]).TryGetValue(pair.Key, out int v) || v != pair.Value) return false;
            }
            return true;
        }
        private static List<KeyValuePair<int, List<KeyValuePair<int, int>>>> CopyPending(IEnumerable q) { var r = new List<KeyValuePair<int, List<KeyValuePair<int, int>>>>(); foreach (object x in q) { var e = (KeyValuePair<int, List<KeyValuePair<int, int>>>)x; r.Add(new KeyValuePair<int, List<KeyValuePair<int, int>>>(e.Key, new List<KeyValuePair<int, int>>(e.Value))); } return r; }
        private static void RestorePending(object queue, List<KeyValuePair<int, List<KeyValuePair<int, int>>>> s)
        {
            var q = (Queue<KeyValuePair<int, List<KeyValuePair<int, int>>>>)queue;
            q.Clear(); foreach (var e in s) q.Enqueue(new KeyValuePair<int, List<KeyValuePair<int, int>>>(e.Key, new List<KeyValuePair<int, int>>(e.Value)));
        }
        private static bool PendingEquals(IEnumerable queue, List<KeyValuePair<int, List<KeyValuePair<int, int>>>> s)
        {
            var now = CopyPending(queue); if (now.Count != s.Count) return false;
            for (int i = 0; i < now.Count; i++)
            {
                if (now[i].Key != s[i].Key || now[i].Value.Count != s[i].Value.Count) return false;
                for (int j = 0; j < now[i].Value.Count; j++) if (!now[i].Value[j].Equals(s[i].Value[j])) return false;
            }
            return true;
        }
    }
}
