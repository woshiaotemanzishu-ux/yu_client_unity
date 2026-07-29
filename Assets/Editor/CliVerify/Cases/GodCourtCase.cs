using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GodCourt;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>神庭 23300/23301/23306/23310 二进制边界、切片隔离、启动/等级门与生命周期专项。</summary>
    public static class GodCourtCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds = { 23300, 23301, 23306, 23310 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY godcourt EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            GodCourtController controller = GodCourtController.Instance;
            GodCourtModel model = GodCourtModel.Instance;
            RoleModel role = RoleModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            GodCourtModel.OverviewSnapshot oldOverview = model.Overview;
            GodCourtModel.HouseSnapshot oldHouse = model.House;
            GodCourtModel.ErrorSnapshot oldError = model.LastError;
            var oldUpdates = new List<GodCourtModel.CourtEntry>(model.CourtUpdates.Values);
            int oldLevel = role.Level;
            bool oldHasBaseInfo = role.HasBaseInfo;

            FieldInfo hasBaseField = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", F);
            FieldInfo lastLevelField = typeof(GodCourtController).GetField("_lastLevel", F);
            object oldLastLevel = lastLevelField?.GetValue(controller);
            FieldInfo interceptor = typeof(GodCourtController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in RegisteredIds) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on23300 = typeof(GodCourtController).GetMethod("On23300", F);
                MethodInfo on23301 = typeof(GodCourtController).GetMethod("On23301", F);
                MethodInfo on23306 = typeof(GodCourtController).GetMethod("On23306", F);
                MethodInfo on23310 = typeof(GodCourtController).GetMethod("On23310", F);
                MethodInfo onRole = typeof(GodCourtController).GetMethod("OnRoleInfoUpdate", F);

                bool a = handlers != null && interceptor != null && hasBaseField != null && lastLevelField != null
                    && on23300 != null && on23301 != null && on23306 != null && on23310 != null && onRole != null;
                foreach (int id in RegisteredIds) a &= handlers != null && handlers.Contains(id);
                int[] deferred = { 23302, 23303, 23304, 23305, 23307, 23308, 23309 };
                foreach (int id in deferred) a &= handlers != null && !handlers.Contains(id);

                bool b = false;
                bool c = false;
                bool d = false;
                bool e = false;
                bool f = false;
                bool g = false;
                bool h = false;
                bool i = false;
                var frames = new List<byte[]>();
                if (a)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    role.Level = 321;
                    hasBaseField.SetValue(role, true);
                    model.ReplaceError(9, "seed");
                    model.ReplaceOverview(new[] { SimpleCourt(8) });
                    model.ReplaceHouse(1, 2, 3, 4, 5, 6, Array.Empty<GodCourtModel.GrandStatusEntry>());
                    model.ReplaceCourtUpdate(SimpleCourt(7));
                    controller.RequestStartup();
                    b = FramesAre(frames, 23301, 23306) && !model.HasOverview && !model.HasHouse && !model.HasError
                        && model.CourtUpdateCount == 0 && Equals(lastLevelField.GetValue(controller), 321);
                    frames.Clear();
                    controller.RequestOverview();
                    controller.RequestHouse();
                    b &= FramesAre(frames, 23301, 23306);
                    frames.Clear();

                    byte[] earlyUpdate = CourtBytes(42, 1, -1, 255, true);
                    GodCourtModel.CourtEntry early = null;
                    c = Invoke(on23310, controller, earlyUpdate)
                        && model.CourtUpdateCount == 1 && model.TryGetCourtUpdate(42, out early)
                        && early.CourtLevel == 1 && early.Power == ulong.MaxValue && early.IsActive == 255
                        && early.Attrs.Count == 2 && early.Attrs[0].AttrId == 7 && early.Attrs[1].AttrId == 7
                        && early.Attrs[1].Value == uint.MaxValue
                        && early.Equips.Count == 2 && early.Equips[0].Pos == 2 && early.Equips[1].Pos == 2
                        && early.Equips[1].EquipId == ulong.MaxValue && early.Equips[1].Stage == byte.MaxValue
                        && early.Suits.Count == 2 && early.Suits[0].Stage == 4 && early.Suits[1].Stage == 4
                        && early.Suits[1].Num == ushort.MaxValue && frames.Count == 0;
                    GodCourtModel.CourtEntry earlyRef = early;

                    byte[] overviewPacket = new CliVerify.Pkt().H(3)
                        .I(0).H(65535).L(-1).H(3)
                            .H(7).I(0).H(7).I(uint.MaxValue).H(65535).I(4000000000L)
                            .C(255).H(3)
                            .C(1).L(0).C(0).C(1).L(-1).C(255).C(255).L(5000000000L).C(7)
                            .H(3).C(2).H(0).C(2).H(65535).C(255).H(9)
                        .I(0).H(1).L(2).H(0).C(0).H(0).H(0)
                        .I(uint.MaxValue).H(2).L(3).H(0).C(1).H(0).H(0).Bytes();
                    d = Invoke(on23301, controller, overviewPacket)
                        && model.HasOverview && model.Overview.Courts.Count == 3
                        && model.Overview.Courts[0].CourtId == 0 && model.Overview.Courts[1].CourtId == 0
                        && model.Overview.Courts[2].CourtId == uint.MaxValue
                        && model.Overview.Courts[0].CourtLevel == ushort.MaxValue
                        && model.Overview.Courts[0].Power == ulong.MaxValue
                        && model.Overview.Courts[0].Attrs.Count == 3
                        && model.Overview.Courts[0].Attrs[0].AttrId == 7
                        && model.Overview.Courts[0].Attrs[1].AttrId == 7
                        && model.Overview.Courts[0].Attrs[1].Value == uint.MaxValue
                        && model.Overview.Courts[0].Attrs[2].Value == 4000000000U
                        && model.Overview.Courts[0].Equips.Count == 3
                        && model.Overview.Courts[0].Equips[0].Pos == 1
                        && model.Overview.Courts[0].Equips[1].Pos == 1
                        && model.Overview.Courts[0].Equips[1].EquipId == ulong.MaxValue
                        && model.Overview.Courts[0].Suits.Count == 3
                        && model.Overview.Courts[0].Suits[0].Stage == 2
                        && model.Overview.Courts[0].Suits[1].Stage == 2
                        && model.CourtUpdateCount == 1 && ReferenceEquals(model.CourtUpdates[42], earlyRef);
                    GodCourtModel.OverviewSnapshot overviewRef = model.Overview;

                    byte[] housePacket = new CliVerify.Pkt()
                        .H(65535).I(uint.MaxValue).C(255).I(4000000000L).H(65535).H(65535).H(3)
                        .H(9).C(0).H(9).C(255).H(65535).C(7).Bytes();
                    e = Invoke(on23306, controller, housePacket)
                        && model.HasHouse && model.House.RewardLevel == ushort.MaxValue
                        && model.House.SumNum == uint.MaxValue && model.House.CrystalColor == byte.MaxValue
                        && model.House.DailyNum == 4000000000U && model.House.HouseLevel == ushort.MaxValue
                        && model.House.HouseExp == ushort.MaxValue && model.House.GrandStatuses.Count == 3
                        && model.House.GrandStatuses[0].Times == 9 && model.House.GrandStatuses[1].Times == 9
                        && model.House.GrandStatuses[1].Status == byte.MaxValue
                        && ReferenceEquals(model.Overview, overviewRef) && ReferenceEquals(model.CourtUpdates[42], earlyRef);
                    GodCourtModel.HouseSnapshot houseRef = model.House;

                    GodCourtModel.CourtEntry fortyThree = null;
                    f = Invoke(on23310, controller, CourtBytes(43, 2, 3, 1, false))
                        && model.CourtUpdateCount == 2 && model.TryGetCourtUpdate(43, out fortyThree)
                        && fortyThree.CourtLevel == 2;
                    GodCourtModel.CourtEntry replaced = null;
                    f &= Invoke(on23310, controller, CourtBytes(42, 9, 0, 0, false))
                        && model.CourtUpdateCount == 2 && model.TryGetCourtUpdate(42, out replaced)
                        && !ReferenceEquals(replaced, earlyRef) && replaced.CourtLevel == 9 && replaced.Power == 0
                        && replaced.Attrs.Count == 0 && replaced.Equips.Count == 0 && replaced.Suits.Count == 0
                        && ReferenceEquals(model.CourtUpdates[43], fortyThree)
                        && ReferenceEquals(model.Overview, overviewRef) && ReferenceEquals(model.House, houseRef);

                    g = Invoke(on23300, controller, new CliVerify.Pkt().I(0).S("").Bytes())
                        && model.HasError && model.LastError.ErrorCode == 0 && model.LastError.ErrorArgs == "";
                    g &= Invoke(on23300, controller, new CliVerify.Pkt().I(uint.MaxValue).S("参数原样").Bytes())
                        && model.HasError && model.LastError.ErrorCode == uint.MaxValue && model.LastError.ErrorArgs == "参数原样"
                        && ReferenceEquals(model.Overview, overviewRef) && ReferenceEquals(model.House, houseRef)
                        && ReferenceEquals(model.CourtUpdates[43], fortyThree) && frames.Count == 0;
                    GodCourtModel.ErrorSnapshot errorRef = model.LastError;

                    h = Invoke(on23301, controller, new CliVerify.Pkt().H(0).Bytes())
                        && model.HasOverview && model.Overview.Courts.Count == 0
                        && ReferenceEquals(model.House, houseRef) && ReferenceEquals(model.LastError, errorRef)
                        && model.CourtUpdateCount == 2;
                    GodCourtModel.OverviewSnapshot emptyOverview = model.Overview;
                    h &= Invoke(on23306, controller, new CliVerify.Pkt().H(0).I(0).C(0).I(0).H(0).H(0).H(0).Bytes())
                        && model.HasHouse && model.House.RewardLevel == 0 && model.House.SumNum == 0
                        && model.House.GrandStatuses.Count == 0 && ReferenceEquals(model.Overview, emptyOverview)
                        && ReferenceEquals(model.LastError, errorRef) && model.CourtUpdateCount == 2;
                    GodCourtModel.HouseSnapshot emptyHouse = model.House;
                    controller.RequestOverview();
                    controller.RequestHouse();
                    h &= FramesAre(frames, 23301, 23306) && ReferenceEquals(model.Overview, emptyOverview)
                        && ReferenceEquals(model.House, emptyHouse) && ReferenceEquals(model.LastError, errorRef)
                        && model.CourtUpdateCount == 2;
                    frames.Clear();

                    role.Level = 321;
                    onRole.Invoke(controller, null);
                    bool sameLevel = frames.Count == 0;
                    role.Level = 489;
                    onRole.Invoke(controller, null);
                    bool below = frames.Count == 0 && Equals(lastLevelField.GetValue(controller), 489);
                    role.Level = 491;
                    onRole.Invoke(controller, null);
                    bool jumpPast = frames.Count == 0 && Equals(lastLevelField.GetValue(controller), 491);
                    role.Level = 490;
                    onRole.Invoke(controller, null);
                    bool exact = FramesAre(frames, 23301, 23306) && Equals(lastLevelField.GetValue(controller), 490)
                        && ReferenceEquals(model.Overview, emptyOverview) && ReferenceEquals(model.House, emptyHouse);
                    frames.Clear();
                    onRole.Invoke(controller, null);
                    bool duplicateEvent = frames.Count == 0;
                    role.Level = 489;
                    hasBaseField.SetValue(role, false);
                    onRole.Invoke(controller, null);
                    bool noBase = frames.Count == 0 && Equals(lastLevelField.GetValue(controller), 490);
                    hasBaseField.SetValue(role, true);
                    i = sameLevel && below && jumpPast && exact && duplicateEvent && noBase;

                    controller.Dispose();
                    i &= !controller.IsInitialized && !model.HasOverview && !model.HasHouse && !model.HasError
                        && model.CourtUpdateCount == 0 && Equals(lastLevelField.GetValue(controller), -1);
                    foreach (int id in RegisteredIds) i &= !handlers.Contains(id);
                }

                pass = a && b && c && d && e && f && g && h && i;
                Debug.Log($"CLIVERIFY godcourt VERDICT A={a} B={b} C={c} D={d} E={e} F={f} G={g} H={h} I={i} pass={pass}");
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                RestoreModelProperty(model, "Overview", oldOverview);
                RestoreModelProperty(model, "House", oldHouse);
                RestoreModelProperty(model, "LastError", oldError);
                foreach (GodCourtModel.CourtEntry entry in oldUpdates) model.ReplaceCourtUpdate(entry);
                role.Level = oldLevel;
                if (hasBaseField != null) hasBaseField.SetValue(role, oldHasBaseInfo);
                if (wasInitialized) controller.Init();
                if (lastLevelField != null) lastLevelField.SetValue(controller, oldLastLevel);
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ReferenceEquals(model.Overview, oldOverview) && ReferenceEquals(model.House, oldHouse)
                    && ReferenceEquals(model.LastError, oldError) && UpdatesMatch(model, oldUpdates)
                    && role.Level == oldLevel && role.HasBaseInfo == oldHasBaseInfo
                    && (lastLevelField == null || Equals(lastLevelField.GetValue(controller), oldLastLevel))
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY godcourt restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static GodCourtModel.CourtEntry SimpleCourt(uint id)
        {
            return new GodCourtModel.CourtEntry(id, 0, 0, Array.Empty<GodCourtModel.AttrEntry>(), 0,
                Array.Empty<GodCourtModel.EquipEntry>(), Array.Empty<GodCourtModel.SuitEntry>());
        }

        private static byte[] CourtBytes(uint id, ushort level, long power, byte active, bool nested)
        {
            var packet = new CliVerify.Pkt().I(id).H(level).L(power);
            if (nested)
            {
                packet.H(2).H(7).I(0).H(7).I(uint.MaxValue).C(active)
                    .H(2).C(2).L(0).C(0).C(2).L(-1).C(255)
                    .H(2).C(4).H(0).C(4).H(65535);
            }
            else
            {
                packet.H(0).C(active).H(0).H(0);
            }
            return packet.Bytes();
        }

        private static bool Invoke(MethodInfo handler, GodCourtController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool FramesAre(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames == null || frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
                if (!BytesEqual(frames[i], new CliVerify.Pkt().H(6).H(1000).H(ids[i]).Bytes())) return false;
            return true;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> saved, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            saved[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState saved, int id)
        {
            if (handlers == null) return;
            if (saved.Exists) handlers[id] = saved.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id)
        {
            return handlers != null && handlers.Contains(id) == saved.Exists
                && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
        }

        private static void RestoreModelProperty(GodCourtModel model, string name, object value)
        {
            typeof(GodCourtModel).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static bool UpdatesMatch(GodCourtModel model, IReadOnlyList<GodCourtModel.CourtEntry> expected)
        {
            if (model.CourtUpdateCount != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (!model.TryGetCourtUpdate(expected[i].CourtId, out GodCourtModel.CourtEntry actual)
                    || !ReferenceEquals(actual, expected[i])) return false;
            return true;
        }
    }
}
