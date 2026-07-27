using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61048 双方邀请状态专项：完整快照、Figure、位宽、覆盖、空表、读尾与 ambient 恢复。</summary>
    public static class DungeonInviteStateCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class WireRole
        {
            public byte Type;
            public long RoleId;
            public string Name;
            public int Level;
            public long MarriageId;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-invite-state EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasState = model.HasInviteState;
            DungeonModel.InviteStateSnapshot oldState = model.LastInviteState;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_INVITE_STATE);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_INVITE_STATE] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasInviteState", false);
                RestoreModelProperty(model, "LastInviteState", null);

                MethodInfo on61048 = typeof(DungeonController).GetMethod("On61048", IF);
                pass = Proto.DUNGEON_INVITE_STATE == 61048 && on61048 != null
                    && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);
                Check(ref pass, "no response keeps clear", !model.HasInviteState && model.LastInviteState == null);

                var inviter = new WireRole
                {
                    Type = 1,
                    RoleId = -1,
                    Name = "邀请方·极值",
                    Level = ushort.MaxValue,
                    MarriageId = -1,
                };
                var invitee = new WireRole
                {
                    Type = 2,
                    RoleId = 0,
                    Name = "被邀请方",
                    Level = 1,
                    MarriageId = 0,
                };
                Check(ref pass, "code1 dual figure/max/read-to-end",
                    Feed(on61048, controller, 1, uint.MaxValue, inviter, invitee)
                    && State(model, 1, uint.MaxValue, 2)
                    && Entry(model.LastInviteState.List[0], 1, ulong.MaxValue, "邀请方·极值", ushort.MaxValue, ulong.MaxValue)
                    && Entry(model.LastInviteState.List[1], 2, 0, "被邀请方", 1, 0));

                DungeonModel.InviteStateSnapshot code1State = model.LastInviteState;
                var cancelRole = new WireRole { Type = 1, RoleId = 7, Name = "取消方", Level = 2, MarriageId = 8 };
                Check(ref pass, "code2 whole replace/read-to-end",
                    Feed(on61048, controller, 2, 13001, cancelRole)
                    && State(model, 2, 13001, 1)
                    && !ReferenceEquals(model.LastInviteState, code1State)
                    && Entry(model.LastInviteState.List[0], 1, 7, "取消方", 2, 8));

                DungeonModel.InviteStateSnapshot code2State = model.LastInviteState;
                var rejectRole = new WireRole { Type = 2, RoleId = 9, Name = "拒绝方", Level = 3, MarriageId = 10 };
                Check(ref pass, "code3 whole replace/read-to-end",
                    Feed(on61048, controller, 3, 13002, rejectRole)
                    && State(model, 3, 13002, 1)
                    && !ReferenceEquals(model.LastInviteState, code2State)
                    && Entry(model.LastInviteState.List[0], 2, 9, "拒绝方", 3, 10));

                DungeonModel.InviteStateSnapshot code3State = model.LastInviteState;
                Check(ref pass, "code4 empty list replaces/read-to-end",
                    Feed(on61048, controller, 4, 0)
                    && State(model, 4, 0, 0)
                    && !ReferenceEquals(model.LastInviteState, code3State));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-invite-state VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasInviteState", oldHasState);
                RestoreModelProperty(model, "LastInviteState", oldState);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasInviteState == oldHasState
                    && ReferenceEquals(model.LastInviteState, oldState)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-invite-state restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint code, uint dunId,
            params WireRole[] roles)
        {
            var p = new CliVerify.Pkt().I(code).H(roles.Length);
            foreach (WireRole role in roles)
            {
                p.C(role.Type).L(role.RoleId);
                AppendFigure(p, role.Name, role.Level, role.MarriageId);
            }
            p.I(dunId);
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool State(DungeonModel model, uint code, uint dunId, int count) =>
            model.HasInviteState && model.LastInviteState != null
            && model.LastInviteState.Code == code && model.LastInviteState.DunId == dunId
            && model.LastInviteState.List != null && model.LastInviteState.List.Count == count;

        private static bool Entry(DungeonModel.InviteStateEntry entry, byte type, ulong roleId,
            string name, ushort level, ulong marriageId)
        {
            return entry != null && entry.Type == type && entry.RoleId == roleId && entry.Figure != null
                && entry.Figure.name == name && entry.Figure.level == level
                && entry.Figure.Raw.TryGetValue("marriage_id", out object rawMarriageId)
                && unchecked((ulong)(long)rawMarriageId) == marriageId;
        }

        /// <summary>按 FigureProto.SCHEMA 精确写入；5 个嵌套数组固定为空。</summary>
        private static CliVerify.Pkt AppendFigure(CliVerify.Pkt p, string name, int level, long marriageId) => p
            .S(name).C(0).C(0).C(0).H(level).C(0).C(0).C(0).C(0)
            .H(0).H(0).S("").I(uint.MaxValue).L(-1).S("").C(0).S("")
            .I(0).I(0).C(0).C(0).C(0).C(1).L(marriageId).S("伴侣")
            .I(0).I(0).I(0).H(0).H(0).H(0).H(0).H(0).I(0).H(0)
            .I(0).I(0).I(0).C(0).I(0).C(0).C(0).C(0).C(0).C(0).C(0);

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_INVITE_STATE) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_INVITE_STATE], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-invite-state " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
