using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61088 周本特殊信息专项：精确空请求、原始整包替换、异键隔离与 ambient 深恢复。</summary>
    public static class DungeonPolarSpecialInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-polar-special-info EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            PolarModel model = PolarModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            var oldSpecialInfo = new Dictionary<uint, PolarModel.SpecialInfoSnapshot>(
                model.SpecialInfoByDunId);
            FieldInfo interceptField = typeof(DungeonController).GetField(
                "s_polarSpecialInfoOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null
                && handlers.Contains(Proto.DUNGEON_POLAR_SPECIAL_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_POLAR_SPECIAL_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                model.SpecialInfoByDunId.Clear();
                MethodInfo on61088 = typeof(DungeonController).GetMethod("On61088", IF);
                pass = Proto.DUNGEON_POLAR_SPECIAL_INFO == 61088 && on61088 != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);
                if (on61088 == null) throw new MissingMethodException("On61088 seam missing");

                model.ApplySpecialInfo(77, 36, 1, "seed");
                model.TryGetSpecialInfo(77, out PolarModel.SpecialInfoSnapshot seed);

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestPolarSpecialInfo();
                Check(ref pass, "exact 6B empty request", frames.Count == 1 && RequestFrame(frames[0]));
                Check(ref pass, "request no response keeps snapshot",
                    model.SpecialInfoByDunId.Count == 1
                    && model.TryGetSpecialInfo(77, out PolarModel.SpecialInfoSnapshot seedAfter)
                    && ReferenceEquals(seedAfter, seed) && Snapshot(seedAfter, 77, 36, 1, "seed"));

                const string fullTerm = "[{left_count,4294967295},{boss_info,[{1,0}]},{dead_time,0}]";
                PolarModel.SpecialInfoSnapshot full = null;
                Check(ref pass, "push1 full term/u32-u8 max/read-to-end",
                    Feed(on61088, controller, uint.MaxValue, byte.MaxValue, 1, fullTerm)
                    && model.TryGetSpecialInfo(uint.MaxValue, out full)
                    && Snapshot(full, uint.MaxValue, byte.MaxValue, 1, fullTerm));

                const string localTerm = "[{left_count,0}]";
                PolarModel.SpecialInfoSnapshot local = null;
                Check(ref pass, "same-key push2 local term whole replace/read-to-end",
                    Feed(on61088, controller, uint.MaxValue, 36, 2, localTerm)
                    && model.TryGetSpecialInfo(uint.MaxValue, out local)
                    && !ReferenceEquals(local, full)
                    && Snapshot(local, uint.MaxValue, 36, 2, localTerm)
                    && model.TryGetSpecialInfo(77, out seedAfter) && ReferenceEquals(seedAfter, seed));

                const string unicode = "极境·复活✓—原文";
                PolarModel.SpecialInfoSnapshot other = null;
                Check(ref pass, "different key unicode/u8 max isolated/read-to-end",
                    Feed(on61088, controller, 0, 0, byte.MaxValue, unicode)
                    && model.TryGetSpecialInfo(0, out other)
                    && Snapshot(other, 0, 0, byte.MaxValue, unicode)
                    && model.TryGetSpecialInfo(uint.MaxValue, out PolarModel.SpecialInfoSnapshot localAfter)
                    && ReferenceEquals(localAfter, local));

                Check(ref pass, "same other key empty string whole replace/read-to-end",
                    Feed(on61088, controller, 0, byte.MaxValue, 0, string.Empty)
                    && model.TryGetSpecialInfo(0, out PolarModel.SpecialInfoSnapshot empty)
                    && !ReferenceEquals(empty, other)
                    && Snapshot(empty, 0, byte.MaxValue, 0, string.Empty)
                    && model.TryGetSpecialInfo(uint.MaxValue, out localAfter)
                    && ReferenceEquals(localAfter, local));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-polar-special-info VERDICT pass=" + pass);
            }
            finally
            {
                Restore(model.SpecialInfoByDunId, oldSpecialInfo);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                restored = controller.IsInitialized == oldInitialized
                    && AmbientEquals(model.SpecialInfoByDunId, oldSpecialInfo)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null
                        || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-polar-special-info restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller,
            uint dunId, byte dunType, byte pushType, string content)
        {
            byte[] bytes = new CliVerify.Pkt().I(dunId).C(dunType).C(pushType).S(content).Bytes();
            if (bytes.Length != 8 + Encoding.UTF8.GetByteCount(content ?? string.Empty)) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool RequestFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 160;
        }

        private static bool Snapshot(PolarModel.SpecialInfoSnapshot snapshot,
            uint dunId, byte dunType, byte pushType, string content)
        {
            return snapshot != null && snapshot.DunId == dunId && snapshot.DunType == dunType
                && snapshot.PushType == pushType && snapshot.Content == content;
        }

        private static void Restore(Dictionary<uint, PolarModel.SpecialInfoSnapshot> target,
            Dictionary<uint, PolarModel.SpecialInfoSnapshot> captured)
        {
            target.Clear();
            foreach (KeyValuePair<uint, PolarModel.SpecialInfoSnapshot> pair in captured)
                target[pair.Key] = pair.Value;
        }

        private static bool AmbientEquals(Dictionary<uint, PolarModel.SpecialInfoSnapshot> actual,
            Dictionary<uint, PolarModel.SpecialInfoSnapshot> captured)
        {
            if (actual.Count != captured.Count) return false;
            foreach (KeyValuePair<uint, PolarModel.SpecialInfoSnapshot> pair in captured)
                if (!actual.TryGetValue(pair.Key, out PolarModel.SpecialInfoSnapshot snapshot)
                    || !ReferenceEquals(snapshot, pair.Value)) return false;
            return true;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_POLAR_SPECIAL_INFO) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_POLAR_SPECIAL_INFO], value));
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-polar-special-info " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
