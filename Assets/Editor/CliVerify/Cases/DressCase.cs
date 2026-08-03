using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dress;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>装扮设置页：11200/05读侧与11201/02/03事务的精确帧、单飞和权威回包落地。</summary>
    public static class DressCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY dress EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            DressController controller = DressController.Instance;
            DressModel model = DressModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var old = new Dictionary<byte, DressModel.Snapshot>(model.Snapshots);
            var oldPower = new List<DressModel.InactivePowerSnapshot>(model.InactivePowerSnapshots.Values);
            FieldInfo interceptField = typeof(DressController).GetField("s_outboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            bool pass = true;
            void Check(string tag, bool ok) { Debug.Log("CLIVERIFY dress " + tag + " ok=" + ok); if (!ok) pass = false; }

            try
            {
                controller.Init();
                model.Reset();
                IDictionary handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                MethodInfo on11200 = typeof(DressController).GetMethod("On11200", F);
                MethodInfo on11201 = typeof(DressController).GetMethod("On11201", F);
                MethodInfo on11202 = typeof(DressController).GetMethod("On11202", F);
                MethodInfo on11203 = typeof(DressController).GetMethod("On11203", F);
                MethodInfo on11205 = typeof(DressController).GetMethod("On11205", F);
                Check("register", handlers != null && on11200 != null && on11201 != null && on11202 != null && on11203 != null && on11205 != null
                    && handlers.Contains(11200) && handlers.Contains(11201) && handlers.Contains(11202)
                    && handlers.Contains(11203) && !handlers.Contains(11204) && handlers.Contains(11205));

                var frames = new List<byte[]>();
                interceptField?.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                Check("startup-only-11200", frames.Count == 4 && Command(frames[0]) == 11200 && frames[0][6] == 1
                    && Command(frames[1]) == 11200 && frames[1][6] == 2
                    && Command(frames[2]) == 11200 && frames[2][6] == 3
                    && Command(frames[3]) == 11200 && frames[3][6] == 5);
                frames.Clear();

                Feed(on11200, new CliVerify.Pkt().C(5).I(0).H(0).Bytes());
                const byte type = 5;
                const uint id = 5901001;
                Check("activate-send", controller.ActivateOrUpgrade(type, id) && controller.IsTransactionPending
                    && frames.Count == 1 && IsTypeIdFrame(frames[0], 11201, type, id)
                    && !model.TryGetEntry(type, id, out _));
                Check("single-flight", !controller.Use(type, id) && frames.Count == 1);
                frames.Clear();

                Feed(on11201, new CliVerify.Pkt().I(1).C(type).I(id).H(1).L(5000000000L).L(6000000000L).Bytes());
                model.TryGetEntry(type, id, out DressModel.Entry activated);
                Check("11201-success-immediate", !controller.IsTransactionPending && activated != null
                    && activated.DressLevel == 1 && activated.CurrentPower == 5000000000UL
                    && activated.NextPower == 6000000000UL && frames.Count == 0);

                Check("use-send", controller.Use(type, id) && frames.Count == 1 && IsTypeIdFrame(frames[0], 11202, type, id));
                frames.Clear();
                Feed(on11202, new CliVerify.Pkt().I(1).C(type).I(id).Bytes());
                model.TryGet(type, out DressModel.Snapshot used);
                Check("11202-success-immediate", !controller.IsTransactionPending && used != null && used.UsedDressId == id);

                Check("takeoff-send", controller.TakeOff(type, id) && frames.Count == 1 && IsTypeIdFrame(frames[0], 11203, type, id));
                frames.Clear();
                Feed(on11203, new CliVerify.Pkt().I(1).C(type).I(id).Bytes());
                model.TryGet(type, out DressModel.Snapshot takenOff);
                Check("11203-success-immediate", !controller.IsTransactionPending && takenOff != null && takenOff.UsedDressId == 0);

                Check("failure-no-optimistic", controller.ActivateOrUpgrade(type, id));
                frames.Clear();
                Feed(on11201, new CliVerify.Pkt().I(9).C(type).I(id).H(0).L(0).L(0).Bytes());
                model.TryGetEntry(type, id, out DressModel.Entry afterFailure);
                Check("failure-releases-and-preserves", !controller.IsTransactionPending && afterFailure != null
                    && afterFailure.DressLevel == 1 && afterFailure.CurrentPower == 5000000000UL);

                controller.RequestInactivePower(type, id + 1);
                Check("11205-explicit", frames.Count == 1 && IsTypeIdFrame(frames[0], 11205, type, id + 1)
                    && !model.TryGetInactivePower(type, id + 1, out _));
                frames.Clear();
                Feed(on11205, new CliVerify.Pkt().C(type).I(id + 1).L(7000000000L).Bytes());
                model.TryGetInactivePower(type, id + 1, out DressModel.InactivePowerSnapshot power);
                Check("11205-authoritative", power != null && power.ActivePower == 7000000000UL);

                controller.Dispose();
                Check("dispose", !controller.IsInitialized && !model.HasData && !model.HasInactivePowerData && !controller.IsTransactionPending);
                Debug.Log("CLIVERIFY dress VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                foreach (DressModel.Snapshot snapshot in old.Values)
                    model.Replace(snapshot.Type, snapshot.UsedDressId, new List<DressModel.Entry>(snapshot.Entries));
                foreach (DressModel.InactivePowerSnapshot snapshot in oldPower)
                    model.ReplaceInactivePower(snapshot.Type, snapshot.DressId, snapshot.ActivePower);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static void Feed(MethodInfo method, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(DressController.Instance, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidOperationException(method.Name + " remaining=" + reader.Remaining);
        }

        private static int Command(byte[] frame) => frame != null && frame.Length >= 6 ? (frame[4] << 8) | frame[5] : -1;

        private static bool IsTypeIdFrame(byte[] frame, int command, byte type, uint id)
        {
            return frame != null && frame.Length == 11 && Command(frame) == command && frame[6] == type
                && frame[7] == (byte)(id >> 24) && frame[8] == (byte)(id >> 16)
                && frame[9] == (byte)(id >> 8) && frame[10] == (byte)id;
        }
    }
}
