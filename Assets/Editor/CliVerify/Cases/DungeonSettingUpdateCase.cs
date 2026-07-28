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
    /// <summary>61063 副本开关更新专项：精确请求、成功权威重查、失败静止与双 interceptor ambient 深恢复。</summary>
    public static class DungeonSettingUpdateCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

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
                Debug.LogError("CLIVERIFY dungeon-setting-update EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<uint, AmbientSnapshot> oldSettings = Capture(model.DungeonSettingInfoByDunId);
            FieldInfo updateInterceptField = typeof(DungeonController).GetField(
                "s_dungeonSettingUpdateOutboundIntercept", SF);
            FieldInfo infoInterceptField = typeof(DungeonController).GetField(
                "s_dungeonSettingInfoOutboundIntercept", SF);
            object oldUpdateIntercept = updateInterceptField?.GetValue(null);
            object oldInfoIntercept = infoInterceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_SETTING_UPDATE);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_SETTING_UPDATE] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                model.DungeonSettingInfoByDunId.Clear();
                MethodInfo on61063 = typeof(DungeonController).GetMethod("On61063", IF);
                pass = Proto.DUNGEON_SETTING_UPDATE == 61063 && on61063 != null
                    && updateInterceptField != null && infoInterceptField != null
                    && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                model.ApplyDungeonSettingInfo(77, new List<DungeonModel.DungeonSettingInfoEntry>
                {
                    Entry(9, 8, 1, 7),
                    Entry(9, 0, 0, 0),
                });
                model.TryGetDungeonSettingInfo(77, out DungeonModel.DungeonSettingInfoSnapshot seed);

                var updateFrames = new List<byte[]>();
                var infoFrames = new List<byte[]>();
                updateInterceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    updateFrames.Add(frame);
                    return true;
                }));
                infoInterceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    infoFrames.Add(frame);
                    return true;
                }));

                controller.RequestDungeonSetting(
                    uint.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
                controller.RequestDungeonSetting(0, 0, 0, 0, 0);
                Check(ref pass, "exact 14B max/zero requests and field order", updateFrames.Count == 2
                    && UpdateFrame(updateFrames[0], uint.MaxValue,
                        byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue)
                    && UpdateFrame(updateFrames[1], 0, 0, 0, 0, 0));
                Check(ref pass, "request no response keeps model/no requery",
                    infoFrames.Count == 0 && ModelUnchanged(model, seed));

                Check(ref pass, "success 12B read-to-end and exact one 61062 requery",
                    Feed(on61063, controller, 1, uint.MaxValue, 254, 3, 1, 252)
                    && infoFrames.Count == 1 && InfoFrame(infoFrames[0], uint.MaxValue)
                    && updateFrames.Count == 2 && ModelUnchanged(model, seed));

                Check(ref pass, "failure 12B read-to-end no requery/model change",
                    Feed(on61063, controller, uint.MaxValue, 0x01020304, 4, 5, 0, 6)
                    && infoFrames.Count == 1 && updateFrames.Count == 2
                    && ModelUnchanged(model, seed));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-setting-update VERDICT pass=" + pass);
            }
            finally
            {
                Restore(model.DungeonSettingInfoByDunId, oldSettings);
                if (updateInterceptField != null) updateInterceptField.SetValue(null, oldUpdateIntercept);
                if (infoInterceptField != null) infoInterceptField.SetValue(null, oldInfoIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && AmbientEquals(model.DungeonSettingInfoByDunId, oldSettings)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (updateInterceptField == null
                        || ReferenceEquals(updateInterceptField.GetValue(null), oldUpdateIntercept))
                    && (infoInterceptField == null
                        || ReferenceEquals(infoInterceptField.GetValue(null), oldInfoIntercept));
                Debug.Log("CLIVERIFY dungeon-setting-update restored=" + restored);
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

        private static bool Feed(MethodInfo handler, DungeonController controller, uint errorCode,
            uint dunId, byte type, byte selectType, byte isOpen, byte count)
        {
            byte[] bytes = new CliVerify.Pkt()
                .I(errorCode).I(dunId).C(type).C(selectType).C(isOpen).C(count).Bytes();
            if (bytes.Length != 12) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool UpdateFrame(byte[] frame, uint dunId,
            byte type, byte selectType, byte isOpen, byte count)
        {
            return frame != null && frame.Length == 14
                && frame[0] == 0 && frame[1] == 14 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 135
                && frame[6] == (byte)(dunId >> 24) && frame[7] == (byte)(dunId >> 16)
                && frame[8] == (byte)(dunId >> 8) && frame[9] == (byte)dunId
                && frame[10] == type && frame[11] == selectType
                && frame[12] == isOpen && frame[13] == count;
        }

        private static bool InfoFrame(byte[] frame, uint dunId)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 134
                && frame[6] == (byte)(dunId >> 24) && frame[7] == (byte)(dunId >> 16)
                && frame[8] == (byte)(dunId >> 8) && frame[9] == (byte)dunId;
        }

        private static bool ModelUnchanged(
            DungeonModel model, DungeonModel.DungeonSettingInfoSnapshot expected)
        {
            if (model.DungeonSettingInfoByDunId.Count != 1
                || !model.TryGetDungeonSettingInfo(77, out DungeonModel.DungeonSettingInfoSnapshot snapshot)
                || !ReferenceEquals(snapshot, expected) || snapshot.SettingList == null
                || snapshot.SettingList.Count != 2) return false;
            DungeonModel.DungeonSettingInfoEntry first = snapshot.SettingList[0];
            DungeonModel.DungeonSettingInfoEntry second = snapshot.SettingList[1];
            return first != null && first.Type == 9 && first.SelectType == 8
                && first.IsOpen == 1 && first.Count == 7
                && second != null && second.Type == 9 && second.SelectType == 0
                && second.IsOpen == 0 && second.Count == 0;
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
            return handlers != null && handlers.Contains(Proto.DUNGEON_SETTING_UPDATE) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_SETTING_UPDATE], value));
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-setting-update " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
