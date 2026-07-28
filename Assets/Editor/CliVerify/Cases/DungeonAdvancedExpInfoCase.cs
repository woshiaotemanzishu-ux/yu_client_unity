using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61059 高级经验面板专项：纯 S2C、五字段位宽/整体替换、读尾与 ambient 恢复。</summary>
    public static class DungeonAdvancedExpInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-advanced-exp-info EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasInfo = model.HasAdvancedExpInfo;
            DungeonModel.AdvancedExpInfoSnapshot oldInfo = model.LastAdvancedExpInfo;
            bool oldHasExpDungeonInfo = model.HasExpDungeonInfo;
            ushort oldKillCount = model.ExpDungeonKillCount;
            ulong oldTotalExp = model.ExpDungeonTotalExp;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_ADVANCED_EXP_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_ADVANCED_EXP_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasAdvancedExpInfo", false);
                RestoreModelProperty(model, "LastAdvancedExpInfo", null);

                MethodInfo on61059 = typeof(DungeonController).GetMethod("On61059", IF);
                MethodInfo on61044 = typeof(DungeonController).GetMethod("On61044", IF);
                MethodInfo request = typeof(DungeonController).GetMethod("RequestAdvancedExpInfo", ALL);
                FieldInfo intercept = typeof(DungeonController).GetField("s_advancedExpInfoOutboundIntercept", ALL);
                pass = Proto.DUNGEON_ADVANCED_EXP_INFO == 61059 && on61059 != null && on61044 != null
                    && request == null && intercept == null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "s2c-only seams/registration/no-request", pass);
                if (on61059 == null || on61044 == null)
                    throw new MissingMethodException("On61059/On61044 seam missing");

                Check(ref pass, "no response keeps clear",
                    !model.HasAdvancedExpInfo && model.LastAdvancedExpInfo == null);

                model.ApplyAdvancedExpInfo(1, 2, 3, 4, 5);
                DungeonModel.AdvancedExpInfoSnapshot seed = model.LastAdvancedExpInfo;
                Check(ref pass, "no response keeps existing snapshot",
                    Snapshot(model, 1, 2, 3, 4, 5) && ReferenceEquals(model.LastAdvancedExpInfo, seed));

                Check(ref pass, "61044 unrelated/read-to-end",
                    Feed61044(on61044, controller, ushort.MaxValue, ulong.MaxValue)
                    && ReferenceEquals(model.LastAdvancedExpInfo, seed)
                    && Snapshot(model, 1, 2, 3, 4, 5));

                Check(ref pass, "all max including ulong/read-to-end",
                    Feed(on61059, controller, uint.MaxValue, uint.MaxValue, uint.MaxValue,
                        uint.MaxValue, ulong.MaxValue)
                    && Snapshot(model, uint.MaxValue, uint.MaxValue, uint.MaxValue,
                        uint.MaxValue, ulong.MaxValue)
                    && !ReferenceEquals(model.LastAdvancedExpInfo, seed));
                DungeonModel.AdvancedExpInfoSnapshot maxInfo = model.LastAdvancedExpInfo;

                Check(ref pass, "all zero whole replace/read-to-end",
                    Feed(on61059, controller, 0, 0, 0, 0, 0)
                    && Snapshot(model, 0, 0, 0, 0, 0)
                    && !ReferenceEquals(model.LastAdvancedExpInfo, maxInfo));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-advanced-exp-info VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasAdvancedExpInfo", oldHasInfo);
                RestoreModelProperty(model, "LastAdvancedExpInfo", oldInfo);
                RestoreModelProperty(model, "HasExpDungeonInfo", oldHasExpDungeonInfo);
                RestoreModelProperty(model, "ExpDungeonKillCount", oldKillCount);
                RestoreModelProperty(model, "ExpDungeonTotalExp", oldTotalExp);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasAdvancedExpInfo == oldHasInfo
                    && ReferenceEquals(model.LastAdvancedExpInfo, oldInfo)
                    && model.HasExpDungeonInfo == oldHasExpDungeonInfo
                    && model.ExpDungeonKillCount == oldKillCount
                    && model.ExpDungeonTotalExp == oldTotalExp
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-advanced-exp-info restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint wave,
            uint waveStartTime, uint waveEndTime, uint historyWave, ulong exp)
        {
            byte[] bytes = new CliVerify.Pkt()
                .I(wave).I(waveStartTime).I(waveEndTime).I(historyWave).L(unchecked((long)exp)).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Feed61044(MethodInfo handler, DungeonController controller, ushort killCount, ulong exp)
        {
            byte[] bytes = new CliVerify.Pkt().H(killCount).L(unchecked((long)exp)).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Snapshot(DungeonModel model, uint wave, uint waveStartTime,
            uint waveEndTime, uint historyWave, ulong exp)
        {
            DungeonModel.AdvancedExpInfoSnapshot actual = model.LastAdvancedExpInfo;
            return model.HasAdvancedExpInfo && actual != null && actual.Wave == wave
                && actual.WaveStartTime == waveStartTime && actual.WaveEndTime == waveEndTime
                && actual.HistoryWave == historyWave && actual.Exp == exp;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_ADVANCED_EXP_INFO) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_ADVANCED_EXP_INFO], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-advanced-exp-info " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
