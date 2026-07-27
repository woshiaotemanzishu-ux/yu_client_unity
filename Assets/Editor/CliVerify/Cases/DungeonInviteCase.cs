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
    public static class DungeonInviteCase
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
                Debug.LogError("CLIVERIFY dungeon-invite EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasResponse = model.HasInviteResponse;
            string oldMessage = model.InviteResponseMessage;
            FieldInfo interceptField = typeof(DungeonController).GetField("s_inviteOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_INVITE);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_INVITE] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasInviteResponse", false);
                RestoreModelProperty(model, "InviteResponseMessage", null);

                MethodInfo on61046 = typeof(DungeonController).GetMethod("On61046", IF);
                MethodInfo on61048 = typeof(DungeonController).GetMethod("On61048", IF);
                pass = Proto.DUNGEON_INVITE == 61046 && on61046 != null && on61048 == null
                    && interceptField != null;
                Check(ref pass, "seams/no 61048", pass);

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                controller.RequestInvite(0, uint.MaxValue, ulong.MaxValue);
                controller.RequestInvite(3, uint.MaxValue, ulong.MaxValue);
                Check(ref pass, "invalid type emits zero frame", frames.Count == 0);

                controller.RequestInvite(1, uint.MaxValue, ulong.MaxValue);
                Check(ref pass, "type1 u32/u64 max exact frame", frames.Count == 1
                    && Frame(frames[0], 1, uint.MaxValue, ulong.MaxValue));
                controller.RequestInvite(2, 0, 0);
                Check(ref pass, "type2 zero exact frame", frames.Count == 2
                    && Frame(frames[1], 2, 0, 0));
                Check(ref pass, "no response keeps model clear",
                    !model.HasInviteResponse && model.InviteResponseMessage == null);

                Check(ref pass, "empty/read-to-end", Feed(on61046, controller, string.Empty)
                    && model.HasInviteResponse && model.InviteResponseMessage == string.Empty);
                Check(ref pass, "chinese/read-to-end", Feed(on61046, controller, "邀请失败：对方正忙")
                    && model.HasInviteResponse && model.InviteResponseMessage == "邀请失败：对方正忙");
                Check(ref pass, "later packet overwrite/read-to-end", Feed(on61046, controller, "后包覆盖")
                    && model.HasInviteResponse && model.InviteResponseMessage == "后包覆盖");
                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));

                Debug.Log("CLIVERIFY dungeon-invite VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasInviteResponse", oldHasResponse);
                RestoreModelProperty(model, "InviteResponseMessage", oldMessage);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasInviteResponse == oldHasResponse
                    && model.InviteResponseMessage == oldMessage
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-invite restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, string message)
        {
            byte[] bytes = new CliVerify.Pkt().S(message).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Frame(byte[] frame, byte type, uint dunId, ulong otherId)
        {
            byte[] expectedPayload = new CliVerify.Pkt().C(type).I(dunId).L(unchecked((long)otherId)).Bytes();
            if (frame == null || frame.Length != 19 || expectedPayload.Length != 13) return false;
            if (frame[0] != 0 || frame[1] != 19 || frame[2] != 3 || frame[3] != 232
                || frame[4] != 238 || frame[5] != 118) return false;
            for (int i = 0; i < expectedPayload.Length; i++)
                if (frame[i + 6] != expectedPayload[i]) return false;
            return true;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_INVITE) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_INVITE], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-invite " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
