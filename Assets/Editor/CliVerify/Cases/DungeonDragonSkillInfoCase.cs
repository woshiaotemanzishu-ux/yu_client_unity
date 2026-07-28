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
    /// <summary>61055 临时技能数量专项：严格空请求、有序整表替换、读尾与 ambient 恢复。</summary>
    public static class DungeonDragonSkillInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class WireEntry
        {
            public uint SkillId;
            public ushort Num;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-dragon-skill-info EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasInfo = model.HasDragonSkillInfo;
            List<DungeonModel.DragonSkillInfoEntry> oldSkills = model.DragonSkillInfo;
            FieldInfo interceptField = typeof(DungeonController).GetField("s_dragonSkillInfoOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_SKILL_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_DRAGON_SKILL_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasDragonSkillInfo", false);
                RestoreModelProperty(model, "DragonSkillInfo", new List<DungeonModel.DragonSkillInfoEntry>());

                MethodInfo on61055 = typeof(DungeonController).GetMethod("On61055", IF);
                pass = Proto.DUNGEON_DRAGON_SKILL_INFO == 61055 && on61055 != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                var seed = new List<DungeonModel.DragonSkillInfoEntry>
                {
                    new DungeonModel.DragonSkillInfoEntry { SkillId = 7, Num = 8 },
                };
                model.ApplyDragonSkillInfo(seed);
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestDragonSkillInfo();
                Check(ref pass, "exact 6B empty request", frames.Count == 1 && EmptyFrame(frames[0]));
                Check(ref pass, "request no response keeps snapshot",
                    model.HasDragonSkillInfo && ReferenceEquals(model.DragonSkillInfo, seed)
                    && Entry(model.DragonSkillInfo[0], 7, 8));

                var maxDuplicate = new[]
                {
                    new WireEntry { SkillId = uint.MaxValue, Num = ushort.MaxValue },
                    new WireEntry { SkillId = 0, Num = 0 },
                    new WireEntry { SkillId = uint.MaxValue, Num = 7 },
                };
                Check(ref pass, "max duplicate original order/read-to-end",
                    Feed(on61055, controller, maxDuplicate)
                    && Snapshot(model, maxDuplicate));
                List<DungeonModel.DragonSkillInfoEntry> multi = model.DragonSkillInfo;

                var single = new[] { new WireEntry { SkillId = 3, Num = 4 } };
                Check(ref pass, "multi-to-single whole replace/read-to-end",
                    Feed(on61055, controller, single) && Snapshot(model, single)
                    && !ReferenceEquals(model.DragonSkillInfo, multi));
                List<DungeonModel.DragonSkillInfoEntry> one = model.DragonSkillInfo;

                Check(ref pass, "single-to-empty whole replace/read-to-end",
                    Feed(on61055, controller, Array.Empty<WireEntry>())
                    && Snapshot(model, Array.Empty<WireEntry>())
                    && !ReferenceEquals(model.DragonSkillInfo, one));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-dragon-skill-info VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasDragonSkillInfo", oldHasInfo);
                RestoreModelProperty(model, "DragonSkillInfo", oldSkills);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasDragonSkillInfo == oldHasInfo
                    && ReferenceEquals(model.DragonSkillInfo, oldSkills)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-dragon-skill-info restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, params WireEntry[] entries)
        {
            var p = new CliVerify.Pkt().H(entries.Length);
            foreach (WireEntry entry in entries) p.I(entry.SkillId).H(entry.Num);
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool EmptyFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 127;
        }

        private static bool Snapshot(DungeonModel model, IReadOnlyList<WireEntry> expected)
        {
            if (!model.HasDragonSkillInfo || model.DragonSkillInfo == null
                || model.DragonSkillInfo.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (!Entry(model.DragonSkillInfo[i], expected[i].SkillId, expected[i].Num)) return false;
            return true;
        }

        private static bool Entry(DungeonModel.DragonSkillInfoEntry actual, uint skillId, ushort num)
        {
            return actual != null && actual.SkillId == skillId && actual.Num == num;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_SKILL_INFO) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_DRAGON_SKILL_INFO], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-dragon-skill-info " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
