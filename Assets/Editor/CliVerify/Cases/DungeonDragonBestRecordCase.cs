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
    /// <summary>61050 神纹最佳记录专项：精确请求、完整替换、位宽/顺序/空表、读尾与 ambient 恢复。</summary>
    public static class DungeonDragonBestRecordCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class WireRole
        {
            public long RoleId;
            public string Name;
            public uint Power;
            public uint ServerNum;
            public uint ServerId;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-dragon-best-record EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasRecord = model.HasDragonBestRecord;
            DungeonModel.DragonBestRecordSnapshot oldRecord = model.LastDragonBestRecord;
            FieldInfo interceptField = typeof(DungeonController).GetField("s_dragonBestRecordOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_BEST_RECORD);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_DRAGON_BEST_RECORD] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasDragonBestRecord", false);
                RestoreModelProperty(model, "LastDragonBestRecord", null);

                MethodInfo on61050 = typeof(DungeonController).GetMethod("On61050", IF);
                pass = Proto.DUNGEON_DRAGON_BEST_RECORD == 61050 && on61050 != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                var seedRoles = new List<DungeonModel.DragonBestRecordRole>
                {
                    new DungeonModel.DragonBestRecordRole { RoleId = 77, Name = "旧快照", Power = 88, ServerNum = 99, ServerId = 100 },
                };
                model.ApplyDragonBestRecord(101, 2, 3, 4, seedRoles);
                DungeonModel.DragonBestRecordSnapshot seed = model.LastDragonBestRecord;

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestDragonBestRecord(uint.MaxValue, byte.MaxValue);
                controller.RequestDragonBestRecord(0, 0);
                Check(ref pass, "exact 11B max/zero requests", frames.Count == 2
                    && Frame(frames[0], uint.MaxValue, byte.MaxValue)
                    && Frame(frames[1], 0, 0));
                Check(ref pass, "requests keep old snapshot", model.HasDragonBestRecord
                    && ReferenceEquals(model.LastDragonBestRecord, seed)
                    && ReferenceEquals(model.LastDragonBestRecord.RoleList, seedRoles));

                var multi = new[]
                {
                    new WireRole { RoleId = -1, Name = "甲·极值", Power = uint.MaxValue, ServerNum = uint.MaxValue, ServerId = uint.MaxValue },
                    new WireRole { RoleId = 42, Name = "乙", Power = 1, ServerNum = 2, ServerId = 3 },
                    new WireRole { RoleId = -1, Name = "甲·重复", Power = 4, ServerNum = 5, ServerId = 6 },
                };
                Check(ref pass, "multi u32/u64/chinese/duplicate order/read-to-end",
                    Feed(on61050, controller, uint.MaxValue, byte.MaxValue, uint.MaxValue, uint.MaxValue, multi)
                    && Snapshot(model, uint.MaxValue, byte.MaxValue, uint.MaxValue, uint.MaxValue, 3)
                    && Role(model.LastDragonBestRecord.RoleList[0], ulong.MaxValue, "甲·极值", uint.MaxValue, uint.MaxValue, uint.MaxValue)
                    && Role(model.LastDragonBestRecord.RoleList[1], 42, "乙", 1, 2, 3)
                    && Role(model.LastDragonBestRecord.RoleList[2], ulong.MaxValue, "甲·重复", 4, 5, 6));

                DungeonModel.DragonBestRecordSnapshot multiRecord = model.LastDragonBestRecord;
                var single = new[]
                {
                    new WireRole { RoleId = 9, Name = "单人", Power = 10, ServerNum = 11, ServerId = 12 },
                };
                Check(ref pass, "multi-to-single whole replace/read-to-end",
                    Feed(on61050, controller, 13001, 7, 8, 9, single)
                    && Snapshot(model, 13001, 7, 8, 9, 1)
                    && !ReferenceEquals(model.LastDragonBestRecord, multiRecord)
                    && Role(model.LastDragonBestRecord.RoleList[0], 9, "单人", 10, 11, 12));

                DungeonModel.DragonBestRecordSnapshot singleRecord = model.LastDragonBestRecord;
                Check(ref pass, "single-to-empty whole replace/read-to-end",
                    Feed(on61050, controller, 0, 0, 0, 0, Array.Empty<WireRole>())
                    && Snapshot(model, 0, 0, 0, 0, 0)
                    && !ReferenceEquals(model.LastDragonBestRecord, singleRecord));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-dragon-best-record VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasDragonBestRecord", oldHasRecord);
                RestoreModelProperty(model, "LastDragonBestRecord", oldRecord);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasDragonBestRecord == oldHasRecord
                    && ReferenceEquals(model.LastDragonBestRecord, oldRecord)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-dragon-best-record restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint dunId, byte wave,
            uint myTime, uint bestTime, params WireRole[] roles)
        {
            var p = new CliVerify.Pkt().I(dunId).C(wave).I(myTime).I(bestTime).H(roles.Length);
            foreach (WireRole role in roles)
            {
                p.L(role.RoleId).S(role.Name).I(role.Power).I(role.ServerNum).I(role.ServerId);
            }
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Frame(byte[] frame, uint dunId, byte wave)
        {
            return frame != null && frame.Length == 11
                && frame[0] == 0 && frame[1] == 11 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 122
                && frame[6] == (byte)(dunId >> 24) && frame[7] == (byte)(dunId >> 16)
                && frame[8] == (byte)(dunId >> 8) && frame[9] == (byte)dunId && frame[10] == wave;
        }

        private static bool Snapshot(DungeonModel model, uint dunId, byte wave, uint myTime, uint bestTime, int count) =>
            model.HasDragonBestRecord && model.LastDragonBestRecord != null
            && model.LastDragonBestRecord.DunId == dunId && model.LastDragonBestRecord.Wave == wave
            && model.LastDragonBestRecord.MyTime == myTime && model.LastDragonBestRecord.BestTime == bestTime
            && model.LastDragonBestRecord.RoleList != null && model.LastDragonBestRecord.RoleList.Count == count;

        private static bool Role(DungeonModel.DragonBestRecordRole role, ulong roleId, string name,
            uint power, uint serverNum, uint serverId)
        {
            return role != null && role.RoleId == roleId && role.Name == name && role.Power == power
                && role.ServerNum == serverNum && role.ServerId == serverId;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_BEST_RECORD) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_DRAGON_BEST_RECORD], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-dragon-best-record " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
