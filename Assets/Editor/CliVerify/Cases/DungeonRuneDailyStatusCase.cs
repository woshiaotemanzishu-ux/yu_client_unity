using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61115 每日状态：严格空请求、全值覆盖、与 61113 隔离及精确 ambient 恢复。</summary>
    public static class DungeonRuneDailyStatusCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY dungeon-rune-daily-status EXCEPTION " + e); return Task.FromResult(3); }
        }
        private static int RunSync()
        {
            DungeonController c = DungeonController.Instance; DungeonModel m = DungeonModel.Instance;
            bool oldInit = c.IsInitialized; var oldStatus = m.RuneDailyStatus;
            var old61113 = new System.Collections.Generic.Dictionary<byte, DungeonModel.RuneRewardSnapshot>(m.DungeonRuneRewardInfoByType);
            FieldInfo intercept = typeof(DungeonController).GetField("s_runeDailyStatusOutboundIntercept", SF); object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_RUNE_DAILY_STATUS);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_RUNE_DAILY_STATUS] : null;
            bool pass = false, restored = false;
            try
            {
                m.ClearDungeonRuneDailyStatus();
                MethodInfo on = typeof(DungeonController).GetMethod("On61115", IF);
                pass = Proto.DUNGEON_RUNE_DAILY_STATUS == 61115 && on != null && intercept != null && (!oldInit || oldHandlerExists);
                Check(ref pass, "constant/registration/no-auto-send", pass);
                m.ApplyDungeonRuneDailyStatus(1, 2); var seed = m.RuneDailyStatus;
                var frames = new System.Collections.Generic.List<byte[]>(); intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                c.RequestDungeonRuneDailyStatus();
                Check(ref pass, "exact 6B request/no response preserves", frames.Count == 1 && Frame(frames[0]) && ReferenceEquals(m.RuneDailyStatus, seed));
                m.ApplyDungeonRuneRewardInfo(7, new System.Collections.Generic.List<DungeonModel.RuneRewardEntry> { new DungeonModel.RuneRewardEntry(9, 8, 7) });
                Check(ref pass, "zero push/read-to-end/61113 isolation", Feed(on, c, 0, 0) && Snapshot(m, 0, 0) && m.DungeonRuneRewardInfoByType.ContainsKey(7));
                var zero = m.RuneDailyStatus;
                Check(ref pass, "status 1 and u32 max active push new object/read-to-end", Feed(on, c, 1, uint.MaxValue) && Snapshot(m, 1, uint.MaxValue) && !ReferenceEquals(m.RuneDailyStatus, zero));
                Check(ref pass, "status 2 full replace", Feed(on, c, 2, 7) && Snapshot(m, 2, 7));
                Check(ref pass, "unknown 255 fully overwrites", Feed(on, c, 255, 0) && Snapshot(m, 255, 0));
                m.ClearDungeonRuneDailyStatus();
                Check(ref pass, "clear slice only", m.RuneDailyStatus == null && m.DungeonRuneRewardInfoByType.ContainsKey(7));
                Check(ref pass, "ambient unchanged", c.IsInitialized == oldInit && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-rune-daily-status VERDICT pass=" + pass);
            }
            finally
            {
                m.ClearDungeonRuneDailyStatus(); if (oldStatus != null) Set(m, oldStatus);
                m.ClearDungeonRuneRewardInfo(); foreach (var p in old61113) m.DungeonRuneRewardInfoByType.Add(p.Key, p.Value);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                restored = c.IsInitialized == oldInit && ReferenceEquals(m.RuneDailyStatus, oldStatus) && Same(m.DungeonRuneRewardInfoByType, old61113) && HandlerUnchanged(handlers, oldHandlerExists, oldHandler) && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-rune-daily-status restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }
        private static void Set(DungeonModel m, DungeonModel.RuneDailyStatusSnapshot s) => typeof(DungeonModel).GetProperty("RuneDailyStatus", BindingFlags.Public | BindingFlags.Instance)?.SetValue(m, s);
        private static bool Feed(MethodInfo on, DungeonController c, byte status, uint level) { byte[] b = new CliVerify.Pkt().C(status).I(level).Bytes(); var r = new NetReader(b, 0, b.Length); on.Invoke(c, new object[] { r }); return r.Remaining == 0; }
        private static bool Frame(byte[] f) => f != null && f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == 238 && f[5] == 187;
        private static bool Snapshot(DungeonModel m, byte s, uint l) => m.RuneDailyStatus != null && m.RuneDailyStatus.Loaded && m.RuneDailyStatus.DailyStatus == s && m.RuneDailyStatus.UnlockLevel == l;
        private static bool Same(System.Collections.Generic.Dictionary<byte, DungeonModel.RuneRewardSnapshot> a, System.Collections.Generic.Dictionary<byte, DungeonModel.RuneRewardSnapshot> b) { if (a.Count != b.Count) return false; foreach (var p in b) if (!a.TryGetValue(p.Key, out var x) || !ReferenceEquals(x, p.Value)) return false; return true; }
        private static bool HandlerUnchanged(IDictionary h, bool exists, object value) => h != null && h.Contains(Proto.DUNGEON_RUNE_DAILY_STATUS) == exists && (!exists || ReferenceEquals(h[Proto.DUNGEON_RUNE_DAILY_STATUS], value));
        private static void Check(ref bool pass, string name, bool ok) { Debug.Log("CLIVERIFY dungeon-rune-daily-status " + name + " ok=" + ok); pass &= ok; }
    }
}
