using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Threading.Tasks;
using Shenxiao.Framework.Net; using Shenxiao.Module.Core.Designation; using UnityEngine;
namespace Shenxiao.EditorTools
{
    public static class DesignationCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance; private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY designation EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            DesignationController c = DesignationController.Instance; DesignationModel m = DesignationModel.Instance; bool was = c.IsInitialized; uint oldCurrent = m.CurrentUsedId; var oldEntries = new List<DesignationModel.Entry>(m.Entries); bool oldHas = m.HasData; var oldActivation = m.Activation; var oldSceneNotice = m.SceneNotice; var oldPowerQuery = m.PowerQuery; var oldRemoval = m.Removal; FieldInfo fi = typeof(DesignationController).GetField("s_outboundIntercept", SF); object oldIntercept = fi?.GetValue(null);
            try
            {
                c.Init(); m.Reset(); MethodInfo on = typeof(DesignationController).GetMethod("On41101", F); var h = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary; bool pass = fi != null && on != null && h != null && h.Contains(Proto.DESIGNATION_LIST) && h.Contains(Proto.DESIGNATION_ACTIVATED) && h.Contains(Proto.DESIGNATION_SCENE_NOTICE) && h.Contains(Proto.DESIGNATION_POWER) && h.Contains(Proto.DESIGNATION_REMOVED) && !h.Contains(41100) && !h.Contains(41102) && !h.Contains(41103) && !h.Contains(41106) && !h.Contains(41109) && !h.Contains(41110); void Check(string tag, bool ok) { Debug.Log("CLIVERIFY designation " + tag + " ok=" + ok); if (!ok) pass = false; } Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); fi.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); c.RequestStartup(); Check("startup exact frame", Frame(frames, Proto.DESIGNATION_LIST)); frames.Clear();
                byte[] first = new CliVerify.Pkt().I(100).H(2).I(100).C(1).I(4000000000L).I(101).C(2).I(3).Bytes(); var r = new NetReader(first, 0, first.Length); on.Invoke(c, new object[] { r }); Check("fields/order/read-to-end", r.Remaining == 0 && m.HasData && m.CurrentUsedId == 100 && m.Entries.Count == 2 && m.Entries[0].Id == 100 && m.Entries[0].Order == 1 && m.Entries[0].EndTime == 4000000000U && m.Entries[1].Id == 101 && m.Entries[1].Order == 2 && m.Entries[1].EndTime == 3 && frames.Count == 0);
                byte[] empty = new CliVerify.Pkt().I(0).H(0).Bytes(); var e = new NetReader(empty, 0, empty.Length); on.Invoke(c, new object[] { e }); Check("full replace empty", e.Remaining == 0 && m.HasData && m.CurrentUsedId == 0 && m.Entries.Count == 0 && frames.Count == 0);
                c.Dispose(); Check("dispose reset", !c.IsInitialized && !m.HasData && m.CurrentUsedId == 0 && m.Entries.Count == 0); Debug.Log("CLIVERIFY designation VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (c.IsInitialized) c.Dispose(); m.Reset(); if (oldHas) m.ReplaceData(oldCurrent, oldEntries); typeof(DesignationModel).GetField("<Activation>k__BackingField", F)?.SetValue(m, oldActivation); typeof(DesignationModel).GetField("<SceneNotice>k__BackingField", F)?.SetValue(m, oldSceneNotice); typeof(DesignationModel).GetField("<PowerQuery>k__BackingField", F)?.SetValue(m, oldPowerQuery); typeof(DesignationModel).GetField("<Removal>k__BackingField", F)?.SetValue(m, oldRemoval); if (was) c.Init(); if (fi != null) fi.SetValue(null, oldIntercept); }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id) { if (frames.Count != 1 || frames[0] == null) return false; byte[] f = frames[0]; return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id; }
    }
}
