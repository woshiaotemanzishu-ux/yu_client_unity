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
    /// <summary>11200 只读装扮快照：四类启动请求与按类型全量替换。</summary>
    public static class DressCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY dress EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DressController controller = DressController.Instance;
            DressModel model = DressModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var oldSnapshots = new Dictionary<byte, DressModel.Snapshot>(model.Snapshots);
            FieldInfo interceptField = typeof(DressController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField?.GetValue(null);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DRESS_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DRESS_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on11200 = typeof(DressController).GetMethod("On11200", InstanceNonPublic);
                handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                pass = interceptField != null && on11200 != null && handlers != null
                    && handlers.Contains(Proto.DRESS_INFO)
                    && !handlers.Contains(11201) && !handlers.Contains(11202) && !handlers.Contains(11203)
                    && !handlers.Contains(11204) && !handlers.Contains(11205);
                if (!pass)
                {
                    throw new InvalidOperationException("Dress handler/interceptor precondition failed.");
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestStartup();
                pass &= Frames(frames, 1, 2, 3, 5);
                frames.Clear();

                byte[] first = new CliVerify.Pkt()
                    .C(1).I(100).H(2)
                    .I(101).H(3).L(5000000000L).L(6000000000L)
                    .I(102).H(4).L(7).L(8)
                    .Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on11200.Invoke(controller, new object[] { firstReader });
                model.TryGet(1, out DressModel.Snapshot one);
                pass &= firstReader.Remaining == 0 && one != null && one.UsedDressId == 100 && one.EnableCount == 2
                    && one.Entries[0].DressId == 101 && one.Entries[0].DressLevel == 3
                    && one.Entries[0].CurrentPower == 5000000000UL && one.Entries[0].NextPower == 6000000000UL
                    && one.Entries[1].DressId == 102 && one.Entries[1].DressLevel == 4
                    && one.Entries[1].CurrentPower == 7 && one.Entries[1].NextPower == 8 && frames.Count == 0;

                byte[] second = new CliVerify.Pkt().C(2).I(200).H(1).I(201).H(5).L(9).L(10).Bytes();
                var secondReader = new NetReader(second, 0, second.Length);
                on11200.Invoke(controller, new object[] { secondReader });
                model.TryGet(2, out DressModel.Snapshot two);
                pass &= secondReader.Remaining == 0 && one != null && two != null && two.UsedDressId == 200
                    && two.EnableCount == 1 && two.Entries[0].DressId == 201 && frames.Count == 0;

                byte[] empty = new CliVerify.Pkt().C(1).I(0).H(0).Bytes();
                var emptyReader = new NetReader(empty, 0, empty.Length);
                on11200.Invoke(controller, new object[] { emptyReader });
                model.TryGet(1, out one);
                model.TryGet(2, out two);
                pass &= emptyReader.Remaining == 0 && one != null && one.EnableCount == 0
                    && two != null && two.Entries.Count == 1 && frames.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized && !model.HasData && !handlers.Contains(Proto.DRESS_INFO);
            }
            finally
            {
                try
                {
                    if (controller.IsInitialized) controller.Dispose();
                    model.Reset();
                    foreach (DressModel.Snapshot snapshot in oldSnapshots.Values)
                    {
                        model.Replace(snapshot.Type, snapshot.UsedDressId, new List<DressModel.Entry>(snapshot.Entries));
                    }

                    if (wasInitialized) controller.Init();
                    handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                    if (handlers == null) throw new InvalidOperationException("Dress handlers unavailable during restore.");
                    if (oldHandlerExists) handlers[Proto.DRESS_INFO] = oldHandler;
                    else handlers.Remove(Proto.DRESS_INFO);
                    if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                    restored = controller.IsInitialized == wasInitialized
                        && SnapshotsMatch(model.Snapshots, oldSnapshots)
                        && handlers.Contains(Proto.DRESS_INFO) == oldHandlerExists
                        && (!oldHandlerExists || ReferenceEquals(handlers[Proto.DRESS_INFO], oldHandler))
                        && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                }
                catch (Exception exception)
                {
                    Debug.LogError("CLIVERIFY dress restore " + exception);
                    restored = false;
                }
            }

            Debug.Log("CLIVERIFY dress restored=" + restored + " VERDICT pass=" + pass);
            return pass && restored ? 0 : 3;
        }

        private static bool Frames(IReadOnlyList<byte[]> frames, params byte[] types)
        {
            if (frames.Count != types.Length) return false;
            for (int index = 0; index < types.Length; index++)
            {
                byte[] frame = frames[index];
                if (frame == null || frame.Length != 7 || frame[0] != 0 || frame[1] != 7
                    || frame[2] != 3 || frame[3] != 232 || frame[4] != 43 || frame[5] != 192
                    || frame[6] != types[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SnapshotsMatch(
            IReadOnlyDictionary<byte, DressModel.Snapshot> actual,
            IReadOnlyDictionary<byte, DressModel.Snapshot> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (KeyValuePair<byte, DressModel.Snapshot> pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out DressModel.Snapshot snapshot)) return false;
                DressModel.Snapshot old = pair.Value;
                if (snapshot.Type != old.Type || snapshot.UsedDressId != old.UsedDressId || snapshot.Entries.Count != old.Entries.Count) return false;
                for (int index = 0; index < old.Entries.Count; index++)
                {
                    DressModel.Entry a = snapshot.Entries[index];
                    DressModel.Entry b = old.Entries[index];
                    if (a.DressId != b.DressId || a.DressLevel != b.DressLevel
                        || a.CurrentPower != b.CurrentPower || a.NextPower != b.NextPower)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
