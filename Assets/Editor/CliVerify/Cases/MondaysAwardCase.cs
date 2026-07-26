using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MondaysAward;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class MondaysAwardCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY mondaysaward EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            MondaysAwardController controller = MondaysAwardController.Instance;
            MondaysAwardModel model = MondaysAwardModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            var oldTaskStates = new List<MondaysAwardModel.TaskStateEntry>(model.TaskStates);
            bool oldHasRecords = model.HasRecords;
            var oldRecords = new List<MondaysAwardModel.RecordEntry>(model.Records);
            bool oldHasPools = model.HasPools;
            var oldPools = new List<MondaysAwardModel.PoolEntry>(model.Pools);
            bool oldHasDrawState = model.HasDrawState;
            byte oldDrawStateCode = model.DrawStateCode;
            ushort oldDrawTimes = model.DrawTimes;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            FieldInfo interceptField = typeof(MondaysAwardController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            var oldHandlerExists = new Dictionary<int, bool>();
            int[] ownedProtocols = { 17900, 17904, 17905, 17907, 17908 };
            foreach (int protocol in ownedProtocols)
            {
                oldHandlerExists[protocol] = handlers != null && handlers.Contains(protocol);
                if (oldHandlerExists[protocol]) oldHandlers[protocol] = handlers[protocol];
            }

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on17900 = typeof(MondaysAwardController).GetMethod("On17900", InstanceNonPublic);
                MethodInfo on17904 = typeof(MondaysAwardController).GetMethod("On17904", InstanceNonPublic);
                MethodInfo on17905 = typeof(MondaysAwardController).GetMethod("On17905", InstanceNonPublic);
                MethodInfo on17908 = typeof(MondaysAwardController).GetMethod("On17908", InstanceNonPublic);
                MethodInfo on17907 = typeof(MondaysAwardController).GetMethod("On17907", InstanceNonPublic);
                bool pass = interceptField != null && on17900 != null && on17904 != null && on17905 != null && on17908 != null && on17907 != null && handlers != null;
                for (int proto = 17900; proto <= 17908; proto++)
                {
                    pass &= handlers.Contains(proto) == (proto == 17900 || proto == 17904 || proto == 17905 || proto == 17907 || proto == 17908);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY mondaysaward VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                var errorZeroReader = new NetReader(new CliVerify.Pkt().I(0).Bytes(), 0, 4);
                on17900.Invoke(controller, new object[] { errorZeroReader });
                pass &= errorZeroReader.Remaining == 0 && model.HasError && model.LastErrorCode == 0
                    && !model.HasData && !model.HasRecords && !model.HasPools && !model.HasDrawState && frames.Count == 0;
                uint[] errorCodes = { 1, uint.MaxValue, 7 };
                foreach (uint code in errorCodes)
                {
                    byte[] errorBytes = new CliVerify.Pkt().I((long)code).Bytes();
                    var errorReader = new NetReader(errorBytes, 0, errorBytes.Length);
                    on17900.Invoke(controller, new object[] { errorReader });
                    pass &= errorReader.Remaining == 0 && model.HasError && model.LastErrorCode == code
                        && !model.HasData && !model.HasRecords && !model.HasPools && !model.HasDrawState && frames.Count == 0;
                }
                controller.RequestTaskState();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null);
                pass &= frames.Count == 1 && model.HasError && model.LastErrorCode == 7;
                frames.Clear();

                controller.RequestRecords();
                pass &= IsExactRecordsRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasRecords && model.Records.Count == 0 && model.HasError && model.LastErrorCode == 7;
                frames.Clear();

                controller.RequestPools();
                pass &= IsExactPoolsRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasPools && model.Pools.Count == 0 && model.HasError && model.LastErrorCode == 7;
                frames.Clear();

                controller.RequestDrawState();
                pass &= IsExactDrawStateRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasDrawState && model.DrawStateCode == 0 && model.DrawTimes == 0 && model.HasError && model.LastErrorCode == 7;
                frames.Clear();

                var drawZeroReader = new NetReader(new CliVerify.Pkt().C(0).H(0).Bytes(), 0, 3);
                on17907.Invoke(controller, new object[] { drawZeroReader });
                pass &= drawZeroReader.Remaining == 0 && model.HasDrawState && model.DrawStateCode == 0 && !model.IsDrawOpen && model.DrawTimes == 0;
                var drawOpenReader = new NetReader(new CliVerify.Pkt().C(1).H(ushort.MaxValue).Bytes(), 0, 3);
                on17907.Invoke(controller, new object[] { drawOpenReader });
                pass &= drawOpenReader.Remaining == 0 && model.HasDrawState && model.DrawStateCode == 1 && model.IsDrawOpen && model.DrawTimes == ushort.MaxValue;
                var drawRawReader = new NetReader(new CliVerify.Pkt().C(byte.MaxValue).H(7).Bytes(), 0, 3);
                on17907.Invoke(controller, new object[] { drawRawReader });
                pass &= drawRawReader.Remaining == 0 && model.HasDrawState && model.DrawStateCode == byte.MaxValue
                    && !model.IsDrawOpen && model.DrawTimes == 7 && model.HasError && model.LastErrorCode == 7 && frames.Count == 0;

                controller.RequestDrawState();
                pass &= IsExactDrawStateRequest(frames.Count == 1 ? frames[0] : null)
                    && model.DrawStateCode == byte.MaxValue && model.DrawTimes == 7;
                frames.Clear();

                byte[] firstPoolBytes = new CliVerify.Pkt().H(3)
                    .H(0).H(0)
                    .H(ushort.MaxValue).H(3).H(0).H(ushort.MaxValue).H(ushort.MaxValue)
                    .H(7).H(2).H(5).H(5)
                    .Bytes();
                var firstPoolReader = new NetReader(firstPoolBytes, 0, firstPoolBytes.Length);
                on17908.Invoke(controller, new object[] { firstPoolReader });
                pass &= firstPoolReader.Remaining == 0 && model.HasPools && model.Pools.Count == 3
                    && model.Pools[0].Id == 0 && model.Pools[0].Rids.Count == 0
                    && model.Pools[1].Id == ushort.MaxValue && model.Pools[1].Rids.Count == 3
                    && model.Pools[1].Rids[0] == 0 && model.Pools[1].Rids[1] == ushort.MaxValue && model.Pools[1].Rids[2] == ushort.MaxValue
                    && model.Pools[2].Id == 7 && model.Pools[2].Rids.Count == 2 && model.Pools[2].Rids[0] == 5 && model.Pools[2].Rids[1] == 5
                    && !model.HasData && !model.HasRecords
                    && model.HasDrawState && model.DrawStateCode == byte.MaxValue && model.DrawTimes == 7 && model.HasError && model.LastErrorCode == 7 && frames.Count == 0;

                controller.RequestPools();
                pass &= IsExactPoolsRequest(frames.Count == 1 ? frames[0] : null)
                    && model.HasPools && model.Pools.Count == 3;
                frames.Clear();

                const string roleName = "\u4e2d\u6587\u89d2\u8272";
                const string picture = "\u4e2d\u6587\u5934\u50cf";
                byte[] firstRecordBytes = new CliVerify.Pkt().H(2)
                    .I(0).H(0).L(unchecked((long)ulong.MaxValue)).S(roleName).C(0).H(0).I(0).S(picture).I(0).H(0).H(0)
                    .I(uint.MaxValue).H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S(roleName).C(byte.MaxValue).H(ushort.MaxValue).I(uint.MaxValue).S(picture).I(uint.MaxValue).H(ushort.MaxValue).H(ushort.MaxValue)
                    .Bytes();
                var firstRecordReader = new NetReader(firstRecordBytes, 0, firstRecordBytes.Length);
                on17905.Invoke(controller, new object[] { firstRecordReader });
                pass &= firstRecordReader.Remaining == 0 && model.HasRecords && model.Records.Count == 2
                    && model.Records[0].ServerId == 0 && model.Records[0].ServerNum == 0 && model.Records[0].RoleId == ulong.MaxValue
                    && model.Records[0].RoleName == roleName && model.Records[0].Type == 0 && model.Records[0].PoolId == 0
                    && model.Records[0].Utime == 0 && model.Records[0].Picture == picture && model.Records[0].PictureVer == 0
                    && model.Records[0].Career == 0 && model.Records[0].Turn == 0
                    && model.Records[1].ServerId == uint.MaxValue && model.Records[1].ServerNum == ushort.MaxValue
                    && model.Records[1].RoleId == ulong.MaxValue && model.Records[1].RoleName == roleName
                    && model.Records[1].Type == byte.MaxValue && model.Records[1].PoolId == ushort.MaxValue
                    && model.Records[1].Utime == uint.MaxValue && model.Records[1].Picture == picture && model.Records[1].PictureVer == uint.MaxValue
                    && model.Records[1].Career == ushort.MaxValue && model.Records[1].Turn == ushort.MaxValue
                    && !model.HasData && model.HasPools && model.Pools.Count == 3
                    && model.HasDrawState && model.DrawStateCode == byte.MaxValue && model.DrawTimes == 7 && model.HasError && model.LastErrorCode == 7 && frames.Count == 0;

                byte[] firstBytes = new CliVerify.Pkt().H(2).H(0).C(0).H(65535).C(255).Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on17904.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0 && model.HasData && model.TaskStates.Count == 2
                    && model.TaskStates[0].TaskId == 0 && model.TaskStates[0].State == 0
                    && model.TaskStates[1].TaskId == ushort.MaxValue && model.TaskStates[1].State == byte.MaxValue
                    && model.HasRecords && model.Records.Count == 2
                    && model.HasPools && model.Pools.Count == 3
                    && model.HasDrawState && model.DrawStateCode == byte.MaxValue && model.DrawTimes == 7
                    && model.HasError && model.LastErrorCode == 7 && frames.Count == 0;

                var isolatedErrorReader = new NetReader(new CliVerify.Pkt().I(9).Bytes(), 0, 4);
                on17900.Invoke(controller, new object[] { isolatedErrorReader });
                pass &= isolatedErrorReader.Remaining == 0 && model.HasError && model.LastErrorCode == 9
                    && model.HasData && model.TaskStates.Count == 2 && model.HasRecords && model.Records.Count == 2
                    && model.HasPools && model.Pools.Count == 3 && model.HasDrawState && model.DrawStateCode == byte.MaxValue && model.DrawTimes == 7
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt().H(1).H(7).C(8).Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on17904.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0 && model.HasData && model.TaskStates.Count == 1
                    && model.TaskStates[0].TaskId == 7 && model.TaskStates[0].State == 8
                    && model.HasRecords && model.Records.Count == 2 && model.HasError && model.LastErrorCode == 9
                    && frames.Count == 0;

                byte[] thirdBytes = new CliVerify.Pkt().H(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on17904.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0 && model.HasData && model.TaskStates.Count == 0
                    && model.HasRecords && model.Records.Count == 2
                    && frames.Count == 0;

                var isolationTaskReader = new NetReader(new CliVerify.Pkt().H(1).H(9).C(10).Bytes(), 0, 5);
                on17904.Invoke(controller, new object[] { isolationTaskReader });
                pass &= isolationTaskReader.Remaining == 0 && model.TaskStates.Count == 1
                    && model.TaskStates[0].TaskId == 9 && model.TaskStates[0].State == 10
                    && model.Records.Count == 2 && model.Pools.Count == 3 && frames.Count == 0;

                byte[] replacementRecordBytes = new CliVerify.Pkt().H(1)
                    .I(7).H(8).L(9).S("one").C(1).H(2).I(3).S("pic").I(4).H(5).H(6).Bytes();
                var replacementRecordReader = new NetReader(replacementRecordBytes, 0, replacementRecordBytes.Length);
                on17905.Invoke(controller, new object[] { replacementRecordReader });
                pass &= replacementRecordReader.Remaining == 0 && model.HasRecords && model.Records.Count == 1
                    && model.Records[0].RoleId == 9 && model.Records[0].RoleName == "one"
                    && model.HasData && model.TaskStates.Count == 1 && model.Pools.Count == 3 && frames.Count == 0;

                byte[] replacementPoolBytes = new CliVerify.Pkt().H(1).H(7).H(1).H(9).Bytes();
                var replacementPoolReader = new NetReader(replacementPoolBytes, 0, replacementPoolBytes.Length);
                on17908.Invoke(controller, new object[] { replacementPoolReader });
                pass &= replacementPoolReader.Remaining == 0 && model.HasPools && model.Pools.Count == 1
                    && model.Pools[0].Id == 7 && model.Pools[0].Rids.Count == 1 && model.Pools[0].Rids[0] == 9
                    && model.HasData && model.TaskStates.Count == 1 && model.HasRecords && model.Records.Count == 1
                    && model.Records[0].RoleId == 9
                    && model.HasDrawState && model.DrawStateCode == byte.MaxValue && model.DrawTimes == 7 && frames.Count == 0;

                var replacementDrawReader = new NetReader(new CliVerify.Pkt().C(1).H(123).Bytes(), 0, 3);
                on17907.Invoke(controller, new object[] { replacementDrawReader });
                pass &= replacementDrawReader.Remaining == 0 && model.HasDrawState && model.DrawStateCode == 1
                    && model.IsDrawOpen && model.DrawTimes == 123
                    && model.HasData && model.TaskStates.Count == 1 && model.HasRecords && model.Records.Count == 1
                    && model.HasPools && model.Pools.Count == 1 && frames.Count == 0;

                var emptyPoolReader = new NetReader(new CliVerify.Pkt().H(0).Bytes(), 0, 2);
                on17908.Invoke(controller, new object[] { emptyPoolReader });
                pass &= emptyPoolReader.Remaining == 0 && model.HasPools && model.Pools.Count == 0
                    && model.HasData && model.TaskStates.Count == 1 && model.HasRecords && model.Records.Count == 1
                    && model.Records[0].RoleId == 9
                    && model.HasDrawState && model.DrawStateCode == 1 && model.DrawTimes == 123 && frames.Count == 0;

                var emptyRecordReader = new NetReader(new CliVerify.Pkt().H(0).Bytes(), 0, 2);
                on17905.Invoke(controller, new object[] { emptyRecordReader });
                pass &= emptyRecordReader.Remaining == 0 && model.HasRecords && model.Records.Count == 0
                    && model.HasData && model.TaskStates.Count == 1 && model.HasPools && model.Pools.Count == 0
                    && model.HasDrawState && model.DrawStateCode == 1 && model.DrawTimes == 123 && model.HasError && model.LastErrorCode == 9 && frames.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(17900) && !handlers.Contains(17904) && !handlers.Contains(17905) && !handlers.Contains(17907) && !handlers.Contains(17908)
                    && !model.HasData && model.TaskStates.Count == 0 && !model.HasRecords && model.Records.Count == 0
                    && !model.HasPools && model.Pools.Count == 0 && !model.HasDrawState && model.DrawStateCode == 0 && model.DrawTimes == 0
                    && !model.HasError && model.LastErrorCode == 0;

                Debug.Log("CLIVERIFY mondaysaward VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                model.Reset();
                if (oldHasData)
                {
                    model.Replace(oldTaskStates);
                }

                if (oldHasRecords)
                {
                    model.ReplaceRecords(oldRecords);
                }

                if (oldHasPools)
                {
                    model.ReplacePools(oldPools);
                }

                if (oldHasDrawState)
                {
                    model.ReplaceDrawState(oldDrawStateCode, oldDrawTimes);
                }

                if (oldHasError)
                {
                    model.SetError(oldErrorCode);
                }

                if (wasInitialized)
                {
                    controller.Init();
                }

                if (interceptField != null)
                {
                    interceptField.SetValue(null, oldIntercept);
                }

                if (handlers != null)
                {
                    foreach (int protocol in ownedProtocols)
                    {
                        if (handlers.Contains(protocol)) handlers.Remove(protocol);
                        if (oldHandlerExists[protocol]) handlers[protocol] = oldHandlers[protocol];
                    }
                }
            }
        }

        private static bool IsExactRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MONDAYS_AWARD_TASK_STATE >> 8)
                && frame[5] == (byte)(Proto.MONDAYS_AWARD_TASK_STATE & 0xFF);
        }

        private static bool IsExactRecordsRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MONDAYS_AWARD_RECORDS >> 8)
                && frame[5] == (byte)(Proto.MONDAYS_AWARD_RECORDS & 0xFF);
        }

        private static bool IsExactPoolsRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MONDAYS_AWARD_POOLS >> 8)
                && frame[5] == (byte)(Proto.MONDAYS_AWARD_POOLS & 0xFF);
        }

        private static bool IsExactDrawStateRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MONDAYS_AWARD_DRAW_STATE >> 8)
                && frame[5] == (byte)(Proto.MONDAYS_AWARD_DRAW_STATE & 0xFF);
        }
    }
}
