using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Threading.Tasks;
using Shenxiao.Framework.Net; using Shenxiao.Module.Core.Achievement; using UnityEngine;
namespace Shenxiao.EditorTools
{
    public static class AchievementCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance, SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY achievement EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            AchievementController c = AchievementController.Instance; AchievementModel m = AchievementModel.Instance; bool was = c.IsInitialized, hs = m.HasStageData, he = m.HasEntriesData, hstar = m.HasStarData, ht = m.HasTypesData; byte oldStage = m.CurrentStage; ushort oldNew = m.NewCurrentStage; uint oldStar = m.Star; var oldRewards = new List<AchievementModel.Reward>(m.Rewards); var oldEntries = new List<AchievementModel.Entry>(m.Entries); var oldTypes = new List<AchievementModel.TypeStar>(m.Types); FieldInfo fi = typeof(AchievementController).GetField("s_outboundIntercept", SF); object oldIntercept = fi?.GetValue(null);
            try
            {
                c.Init(); m.Reset(); MethodInfo stage = typeof(AchievementController).GetMethod("On40901", F), entries = typeof(AchievementController).GetMethod("On40903", F), star = typeof(AchievementController).GetMethod("On40906", F), types = typeof(AchievementController).GetMethod("On40908", F); var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool pass = fi != null && stage != null && entries != null && star != null && types != null && handlers != null && handlers.Contains(40901) && handlers.Contains(40903) && handlers.Contains(40906) && handlers.Contains(40908) && !handlers.Contains(40900) && !handlers.Contains(40902) && !handlers.Contains(40904) && !handlers.Contains(40905) && !handlers.Contains(40907) && !handlers.Contains(40909);
                void Check(string tag, bool value) { Debug.Log("CLIVERIFY achievement " + tag + " ok=" + value); if (!value) pass = false; }
                Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); fi.SetValue(null, new Func<byte[], bool>(x => { frames.Add(x); return true; })); c.RequestStartup(); Check("startup exact frames", Frames(frames, 40901, 40903, 40906, 40908)); frames.Clear();
                byte[] stageBytes = new CliVerify.Pkt().C(255).H(2).I(4000000000L).C(255).I(2).C(0).H(65535).Bytes(); var r1 = new NetReader(stageBytes, 0, stageBytes.Length); stage.Invoke(c, new object[] { r1 });
                byte[] entryBytes = new CliVerify.Pkt().H(2).C(255).I(4000000000L).L(5000000000L).C(255).C(0).I(2).L(3).C(0).Bytes(); var r2 = new NetReader(entryBytes, 0, entryBytes.Length); entries.Invoke(c, new object[] { r2 });
                byte[] starBytes = new CliVerify.Pkt().I(4000000000L).Bytes(); var r3 = new NetReader(starBytes, 0, starBytes.Length); star.Invoke(c, new object[] { r3 });
                byte[] typeBytes = new CliVerify.Pkt().H(2).H(65535).I(4000000000L).I(3).H(1).I(4).I(5).Bytes(); var r4 = new NetReader(typeBytes, 0, typeBytes.Length); types.Invoke(c, new object[] { r4 });
                Check("stage fields/order", r1.Remaining == 0 && m.HasStageData && m.CurrentStage == 255 && m.NewCurrentStage == 65535 && m.Rewards.Count == 2 && m.Rewards[0].NeedStar == 4000000000U && m.Rewards[0].Status == 255 && m.Rewards[1].NeedStar == 2 && m.Rewards[1].Status == 0);
                Check("entries fields/order", r2.Remaining == 0 && m.HasEntriesData && m.Entries.Count == 2 && m.Entries[0].Category == 255 && m.Entries[0].Id == 4000000000U && m.Entries[0].Progress == 5000000000UL && m.Entries[0].Status == 255 && m.Entries[1].Category == 0 && m.Entries[1].Id == 2 && m.Entries[1].Progress == 3 && m.Entries[1].Status == 0);
                Check("star fields", r3.Remaining == 0 && m.HasStarData && m.Star == 4000000000U);
                Check("types fields/order", r4.Remaining == 0 && m.HasTypesData && m.Types.Count == 2 && m.Types[0].Type == 65535 && m.Types[0].TotalStar == 4000000000U && m.Types[0].NowStar == 3 && m.Types[1].Type == 1 && m.Types[1].TotalStar == 4 && m.Types[1].NowStar == 5 && m.HasAllStartupData && frames.Count == 0);
                byte[] e1b = new CliVerify.Pkt().C(1).H(0).H(2).Bytes(); var e1 = new NetReader(e1b, 0, e1b.Length); stage.Invoke(c, new object[] { e1 }); byte[] e2b = new CliVerify.Pkt().H(0).Bytes(); var e2 = new NetReader(e2b, 0, e2b.Length); entries.Invoke(c, new object[] { e2 }); byte[] e3b = new CliVerify.Pkt().I(7).Bytes(); var e3 = new NetReader(e3b, 0, e3b.Length); star.Invoke(c, new object[] { e3 }); byte[] e4b = new CliVerify.Pkt().H(0).Bytes(); var e4 = new NetReader(e4b, 0, e4b.Length); types.Invoke(c, new object[] { e4 });
                Check("independent empty replacement", e1.Remaining == 0 && e2.Remaining == 0 && e3.Remaining == 0 && e4.Remaining == 0 && m.HasAllStartupData && m.CurrentStage == 1 && m.NewCurrentStage == 2 && m.Rewards.Count == 0 && m.Entries.Count == 0 && m.Star == 7 && m.Types.Count == 0 && frames.Count == 0);
                c.Dispose(); Check("dispose reset", !c.IsInitialized && !m.HasStageData && !m.HasEntriesData && !m.HasStarData && !m.HasTypesData && m.Rewards.Count == 0 && m.Entries.Count == 0 && m.Types.Count == 0 && m.Star == 0); Debug.Log("CLIVERIFY achievement VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (c.IsInitialized) c.Dispose(); m.Reset(); if (hs) m.ReplaceStage(oldStage, oldRewards, oldNew); if (he) m.ReplaceEntries(oldEntries); if (hstar) m.ReplaceStar(oldStar); if (ht) m.ReplaceTypes(oldTypes); if (was) c.Init(); if (fi != null) fi.SetValue(null, oldIntercept); }
        }
        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids) { if (frames.Count != ids.Length) return false; for (int i = 0; i < ids.Length; i++) { byte[] f = frames[i]; if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232 || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)(ids[i] & 0xFF)) return false; } return true; }
    }
}
