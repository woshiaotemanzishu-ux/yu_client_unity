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
    /// <summary>61062 副本开关设置专项：精确请求、同键替换、异键隔离、原序/重复/零值与 ambient 深恢复。</summary>
    public static class DungeonSettingInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class WireEntry
        {
            public byte Type;
            public byte SelectType;
            public byte IsOpen;
            public byte Count;
        }

        private sealed class AmbientValue
        {
            public DungeonModel.DungeonSettingInfoEntry Entry;
            public byte Type;
            public byte SelectType;
            public byte IsOpen;
            public byte Count;
        }

        private sealed class AmbientSnapshot
        {
            public DungeonModel.DungeonSettingInfoSnapshot Snapshot;
            public List<DungeonModel.DungeonSettingInfoEntry> SettingList;
            public AmbientValue[] Values;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-setting-info EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<uint, AmbientSnapshot> oldSettings = Capture(model.DungeonSettingInfoByDunId);
            FieldInfo interceptField = typeof(DungeonController).GetField(
                "s_dungeonSettingInfoOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_SETTING_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_SETTING_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                model.DungeonSettingInfoByDunId.Clear();
                MethodInfo on61062 = typeof(DungeonController).GetMethod("On61062", IF);
                pass = Proto.DUNGEON_SETTING_INFO == 61062 && on61062 != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                model.ApplyDungeonSettingInfo(77, new List<DungeonModel.DungeonSettingInfoEntry>
                {
                    Entry(1, 2, 3, 4),
                });
                model.TryGetDungeonSettingInfo(77, out DungeonModel.DungeonSettingInfoSnapshot seed);

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestDungeonSettingInfo(uint.MaxValue);
                controller.RequestDungeonSettingInfo(0);
                Check(ref pass, "exact 10B max/zero requests", frames.Count == 2
                    && Frame(frames[0], uint.MaxValue) && Frame(frames[1], 0));
                Check(ref pass, "request no response keeps snapshot",
                    model.TryGetDungeonSettingInfo(77, out DungeonModel.DungeonSettingInfoSnapshot seededAfter)
                    && ReferenceEquals(seededAfter, seed)
                    && Values(seededAfter, E(1, 2, 3, 4)));

                DungeonModel.DungeonSettingInfoSnapshot multi = null;
                Check(ref pass, "u8 max/zero/duplicate order/read-to-end",
                    Feed(on61062, controller, uint.MaxValue,
                        E(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
                        E(0, 0, 0, 0),
                        E(byte.MaxValue, 1, 2, 3))
                    && model.TryGetDungeonSettingInfo(uint.MaxValue, out multi)
                    && Values(multi,
                        E(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
                        E(0, 0, 0, 0),
                        E(byte.MaxValue, 1, 2, 3)));

                DungeonModel.DungeonSettingInfoSnapshot other = null;
                Check(ref pass, "different key isolated/read-to-end",
                    Feed(on61062, controller, 0, E(4, 5, 6, 7), E(4, 8, 9, 10))
                    && model.TryGetDungeonSettingInfo(0, out other)
                    && Values(other, E(4, 5, 6, 7), E(4, 8, 9, 10))
                    && model.TryGetDungeonSettingInfo(
                        uint.MaxValue, out DungeonModel.DungeonSettingInfoSnapshot multiAfter)
                    && ReferenceEquals(multiAfter, multi));

                DungeonModel.DungeonSettingInfoSnapshot single = null;
                Check(ref pass, "multi-to-single same key replace/read-to-end",
                    Feed(on61062, controller, uint.MaxValue, E(11, 12, 13, 14))
                    && model.TryGetDungeonSettingInfo(uint.MaxValue, out single)
                    && !ReferenceEquals(single, multi) && Values(single, E(11, 12, 13, 14))
                    && model.TryGetDungeonSettingInfo(0, out DungeonModel.DungeonSettingInfoSnapshot otherAfter)
                    && ReferenceEquals(otherAfter, other));

                Check(ref pass, "single-to-empty same key replace/read-to-end",
                    Feed(on61062, controller, uint.MaxValue)
                    && model.TryGetDungeonSettingInfo(
                        uint.MaxValue, out DungeonModel.DungeonSettingInfoSnapshot empty)
                    && !ReferenceEquals(empty, single) && Values(empty)
                    && model.TryGetDungeonSettingInfo(0, out DungeonModel.DungeonSettingInfoSnapshot otherAfterEmpty)
                    && ReferenceEquals(otherAfterEmpty, other));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-setting-info VERDICT pass=" + pass);
            }
            finally
            {
                Restore(model.DungeonSettingInfoByDunId, oldSettings);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && AmbientEquals(model.DungeonSettingInfoByDunId, oldSettings)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-setting-info restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static DungeonModel.DungeonSettingInfoEntry Entry(
            byte type, byte selectType, byte isOpen, byte count)
        {
            return new DungeonModel.DungeonSettingInfoEntry
            {
                Type = type,
                SelectType = selectType,
                IsOpen = isOpen,
                Count = count,
            };
        }

        private static WireEntry E(byte type, byte selectType, byte isOpen, byte count)
        {
            return new WireEntry
            {
                Type = type,
                SelectType = selectType,
                IsOpen = isOpen,
                Count = count,
            };
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint dunId,
            params WireEntry[] entries)
        {
            var p = new CliVerify.Pkt().I(dunId).H(entries.Length);
            foreach (WireEntry entry in entries)
                p.C(entry.Type).C(entry.SelectType).C(entry.IsOpen).C(entry.Count);
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Frame(byte[] frame, uint dunId)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 134
                && frame[6] == (byte)(dunId >> 24) && frame[7] == (byte)(dunId >> 16)
                && frame[8] == (byte)(dunId >> 8) && frame[9] == (byte)dunId;
        }

        private static bool Values(DungeonModel.DungeonSettingInfoSnapshot snapshot, params WireEntry[] expected)
        {
            if (snapshot?.SettingList == null || snapshot.SettingList.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                DungeonModel.DungeonSettingInfoEntry actual = snapshot.SettingList[i];
                WireEntry value = expected[i];
                if (actual == null || actual.Type != value.Type || actual.SelectType != value.SelectType
                    || actual.IsOpen != value.IsOpen || actual.Count != value.Count) return false;
            }
            return true;
        }

        private static Dictionary<uint, AmbientSnapshot> Capture(
            Dictionary<uint, DungeonModel.DungeonSettingInfoSnapshot> source)
        {
            var captured = new Dictionary<uint, AmbientSnapshot>(source.Count);
            foreach (KeyValuePair<uint, DungeonModel.DungeonSettingInfoSnapshot> pair in source)
            {
                DungeonModel.DungeonSettingInfoSnapshot snapshot = pair.Value;
                List<DungeonModel.DungeonSettingInfoEntry> list = snapshot?.SettingList;
                AmbientValue[] values = null;
                if (list != null)
                {
                    values = new AmbientValue[list.Count];
                    for (int i = 0; i < list.Count; i++)
                    {
                        DungeonModel.DungeonSettingInfoEntry entry = list[i];
                        values[i] = new AmbientValue
                        {
                            Entry = entry,
                            Type = entry != null ? entry.Type : (byte)0,
                            SelectType = entry != null ? entry.SelectType : (byte)0,
                            IsOpen = entry != null ? entry.IsOpen : (byte)0,
                            Count = entry != null ? entry.Count : (byte)0,
                        };
                    }
                }
                captured[pair.Key] = new AmbientSnapshot
                {
                    Snapshot = snapshot,
                    SettingList = list,
                    Values = values,
                };
            }
            return captured;
        }

        private static void Restore(Dictionary<uint, DungeonModel.DungeonSettingInfoSnapshot> target,
            Dictionary<uint, AmbientSnapshot> captured)
        {
            target.Clear();
            foreach (KeyValuePair<uint, AmbientSnapshot> pair in captured)
            {
                AmbientSnapshot old = pair.Value;
                if (old.Snapshot != null)
                {
                    old.Snapshot.SettingList = old.SettingList;
                    if (old.SettingList != null)
                    {
                        old.SettingList.Clear();
                        if (old.Values != null)
                        {
                            foreach (AmbientValue value in old.Values)
                            {
                                if (value.Entry != null)
                                {
                                    value.Entry.Type = value.Type;
                                    value.Entry.SelectType = value.SelectType;
                                    value.Entry.IsOpen = value.IsOpen;
                                    value.Entry.Count = value.Count;
                                }
                                old.SettingList.Add(value.Entry);
                            }
                        }
                    }
                }
                target[pair.Key] = old.Snapshot;
            }
        }

        private static bool AmbientEquals(
            Dictionary<uint, DungeonModel.DungeonSettingInfoSnapshot> actual,
            Dictionary<uint, AmbientSnapshot> captured)
        {
            if (actual.Count != captured.Count) return false;
            foreach (KeyValuePair<uint, AmbientSnapshot> pair in captured)
            {
                if (!actual.TryGetValue(pair.Key, out DungeonModel.DungeonSettingInfoSnapshot snapshot)
                    || !ReferenceEquals(snapshot, pair.Value.Snapshot)) return false;
                if (snapshot == null) continue;
                if (!ReferenceEquals(snapshot.SettingList, pair.Value.SettingList)) return false;
                AmbientValue[] values = pair.Value.Values;
                if ((snapshot.SettingList?.Count ?? -1) != (values?.Length ?? -1)) return false;
                if (values == null) continue;
                for (int i = 0; i < values.Length; i++)
                {
                    AmbientValue expected = values[i];
                    DungeonModel.DungeonSettingInfoEntry entry = snapshot.SettingList[i];
                    if (!ReferenceEquals(entry, expected.Entry)) return false;
                    if (entry != null && (entry.Type != expected.Type || entry.SelectType != expected.SelectType
                        || entry.IsOpen != expected.IsOpen || entry.Count != expected.Count)) return false;
                }
            }
            return true;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_SETTING_INFO) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_SETTING_INFO], value));
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-setting-info " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
