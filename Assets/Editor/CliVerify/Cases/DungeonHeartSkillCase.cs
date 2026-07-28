using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61101 神殿觉醒副本技能专测：严格空请求、有序整表替换、读尾与 ambient 恢复。</summary>
    public static class DungeonHeartSkillCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class WireEntry { public uint SkillId; public ushort SkillLv; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-heart-skill EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasInfo = model.HasHeartSkillInfo;
            IReadOnlyList<DungeonModel.HeartSkillInfoEntry> oldSkills = model.HeartSkillInfo;
            FieldInfo interceptField = typeof(DungeonController).GetField("s_heartSkillInfoOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_HEART_SKILL_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_HEART_SKILL_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                Restore(model, "HasHeartSkillInfo", false);
                Restore(model, "HeartSkillInfo", new List<DungeonModel.HeartSkillInfoEntry>());
                MethodInfo handler = typeof(DungeonController).GetMethod("On61101", IF);
                MethodInfo request = typeof(DungeonController).GetMethod("RequestHeartSkillInfo", BindingFlags.Public | BindingFlags.Instance);
                pass = Proto.DUNGEON_HEART_SKILL_INFO == 61101 && handler != null && request != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                var seed = new List<DungeonModel.HeartSkillInfoEntry>
                    { new DungeonModel.HeartSkillInfoEntry { SkillId = 7, SkillLv = 8 } };
                model.ApplyHeartSkillInfo(seed);
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestHeartSkillInfo();
                Check(ref pass, "exact 6B empty request", frames.Count == 1 && EmptyFrame(frames[0]));
                Check(ref pass, "request no response keeps snapshot", model.HasHeartSkillInfo
                    && ReferenceEquals(model.HeartSkillInfo, seed) && Entry(model.HeartSkillInfo[0], 7, 8));

                var duplicates = new[]
                {
                    new WireEntry { SkillId = uint.MaxValue, SkillLv = ushort.MaxValue },
                    new WireEntry { SkillId = 0, SkillLv = 0 },
                    new WireEntry { SkillId = uint.MaxValue, SkillLv = 7 },
                };
                Check(ref pass, "u32/u16 boundary duplicate original order last wins/read-to-end",
                    Feed(handler, controller, duplicates) && Snapshot(model, duplicates)
                    && model.GetHeartSkillLevel(uint.MaxValue) == 7 && model.GetHeartSkillLevel(123) == 0);
                IReadOnlyList<DungeonModel.HeartSkillInfoEntry> multi = model.HeartSkillInfo;

                var single = new[] { new WireEntry { SkillId = 3, SkillLv = 4 } };
                Check(ref pass, "multi-to-single whole replace/read-to-end", Feed(handler, controller, single)
                    && Snapshot(model, single) && !ReferenceEquals(model.HeartSkillInfo, multi));
                IReadOnlyList<DungeonModel.HeartSkillInfoEntry> one = model.HeartSkillInfo;
                Check(ref pass, "single-to-empty whole replace/read-to-end", Feed(handler, controller, Array.Empty<WireEntry>())
                    && Snapshot(model, Array.Empty<WireEntry>()) && !ReferenceEquals(model.HeartSkillInfo, one));
                Check(ref pass, "ambient untouched during run", controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-heart-skill VERDICT pass=" + pass);
            }
            finally
            {
                Restore(model, "HasHeartSkillInfo", oldHasInfo);
                Restore(model, "HeartSkillInfo", oldSkills);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                restored = controller.IsInitialized == oldInitialized && model.HasHeartSkillInfo == oldHasInfo
                    && ReferenceEquals(model.HeartSkillInfo, oldSkills)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-heart-skill restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, params WireEntry[] entries)
        {
            var p = new CliVerify.Pkt().H(entries.Length);
            foreach (WireEntry entry in entries) p.I(entry.SkillId).H(entry.SkillLv);
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool EmptyFrame(byte[] frame) => frame != null && frame.Length == 6
            && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
            && frame[4] == 238 && frame[5] == 173;

        private static bool Snapshot(DungeonModel model, IReadOnlyList<WireEntry> expected)
        {
            if (!model.HasHeartSkillInfo || model.HeartSkillInfo == null || model.HeartSkillInfo.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (!Entry(model.HeartSkillInfo[i], expected[i].SkillId, expected[i].SkillLv)) return false;
            return true;
        }

        private static bool Entry(DungeonModel.HeartSkillInfoEntry actual, uint id, ushort lv) =>
            actual != null && actual.SkillId == id && actual.SkillLv == lv;

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value) => handlers != null
            && handlers.Contains(Proto.DUNGEON_HEART_SKILL_INFO) == existed
            && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_HEART_SKILL_INFO], value));

        private static void Restore(DungeonModel model, string property, object value) =>
            typeof(DungeonModel).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-heart-skill " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
