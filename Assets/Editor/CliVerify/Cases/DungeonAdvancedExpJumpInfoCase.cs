using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61061 高级经验跳关专项：纯 S2C、三字段替换、61059 双向隔离与 ambient 恢复。</summary>
    public static class DungeonAdvancedExpJumpInfoCase
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
                Debug.LogError("CLIVERIFY dungeon-advanced-exp-jump-info EXCEPTION " + e);
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
            bool oldHasJumpInfo = model.HasAdvancedExpJumpInfo;
            DungeonModel.AdvancedExpJumpInfoSnapshot oldJumpInfo = model.LastAdvancedExpJumpInfo;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_ADVANCED_EXP_JUMP_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_ADVANCED_EXP_JUMP_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasAdvancedExpJumpInfo", false);
                RestoreModelProperty(model, "LastAdvancedExpJumpInfo", null);

                MethodInfo on61061 = typeof(DungeonController).GetMethod("On61061", IF);
                MethodInfo on61059 = typeof(DungeonController).GetMethod("On61059", IF);
                MethodInfo request = typeof(DungeonController).GetMethod("RequestAdvancedExpJumpInfo", ALL);
                FieldInfo intercept = typeof(DungeonController).GetField("s_advancedExpJumpInfoOutboundIntercept", ALL);
                pass = Proto.DUNGEON_ADVANCED_EXP_JUMP_INFO == 61061 && on61061 != null && on61059 != null
                    && request == null && intercept == null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "s2c-only seams/registration/no-request", pass);
                if (on61061 == null || on61059 == null)
                    throw new MissingMethodException("On61061/On61059 seam missing");

                model.ApplyAdvancedExpInfo(11, 12, 13, 14, 15);
                DungeonModel.AdvancedExpInfoSnapshot panelSeed = model.LastAdvancedExpInfo;
                Check(ref pass, "no response keeps clear and 61059 untouched",
                    !model.HasAdvancedExpJumpInfo && model.LastAdvancedExpJumpInfo == null
                    && Panel(model, 11, 12, 13, 14, 15)
                    && ReferenceEquals(model.LastAdvancedExpInfo, panelSeed));

                model.ApplyAdvancedExpJumpInfo(1, 2, 3);
                DungeonModel.AdvancedExpJumpInfoSnapshot jumpSeed = model.LastAdvancedExpJumpInfo;
                Check(ref pass, "no response keeps existing snapshot",
                    Jump(model, 1, 2, 3) && ReferenceEquals(model.LastAdvancedExpJumpInfo, jumpSeed));

                Check(ref pass, "61059 does not pollute 61061/read-to-end",
                    Feed61059(on61059, controller, 21, 22, 23, 24, 25)
                    && ReferenceEquals(model.LastAdvancedExpJumpInfo, jumpSeed)
                    && Jump(model, 1, 2, 3)
                    && Panel(model, 21, 22, 23, 24, 25));
                DungeonModel.AdvancedExpInfoSnapshot panelFrom61059 = model.LastAdvancedExpInfo;

                Check(ref pass, "61061 max does not pollute 61059/read-to-end",
                    Feed61061(on61061, controller, uint.MaxValue, uint.MaxValue, ulong.MaxValue)
                    && Jump(model, uint.MaxValue, uint.MaxValue, ulong.MaxValue)
                    && !ReferenceEquals(model.LastAdvancedExpJumpInfo, jumpSeed)
                    && ReferenceEquals(model.LastAdvancedExpInfo, panelFrom61059)
                    && Panel(model, 21, 22, 23, 24, 25));
                DungeonModel.AdvancedExpJumpInfoSnapshot maxJump = model.LastAdvancedExpJumpInfo;

                Check(ref pass, "all zero whole replace/read-to-end",
                    Feed61061(on61061, controller, 0, 0, 0)
                    && Jump(model, 0, 0, 0)
                    && !ReferenceEquals(model.LastAdvancedExpJumpInfo, maxJump)
                    && ReferenceEquals(model.LastAdvancedExpInfo, panelFrom61059));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-advanced-exp-jump-info VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasAdvancedExpInfo", oldHasInfo);
                RestoreModelProperty(model, "LastAdvancedExpInfo", oldInfo);
                RestoreModelProperty(model, "HasAdvancedExpJumpInfo", oldHasJumpInfo);
                RestoreModelProperty(model, "LastAdvancedExpJumpInfo", oldJumpInfo);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasAdvancedExpInfo == oldHasInfo
                    && ReferenceEquals(model.LastAdvancedExpInfo, oldInfo)
                    && model.HasAdvancedExpJumpInfo == oldHasJumpInfo
                    && ReferenceEquals(model.LastAdvancedExpJumpInfo, oldJumpInfo)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-advanced-exp-jump-info restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed61061(MethodInfo handler, DungeonController controller,
            uint wave, uint historyWave, ulong exp)
        {
            byte[] bytes = new CliVerify.Pkt().I(wave).I(historyWave).L(unchecked((long)exp)).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Feed61059(MethodInfo handler, DungeonController controller,
            uint wave, uint waveStartTime, uint waveEndTime, uint historyWave, ulong exp)
        {
            byte[] bytes = new CliVerify.Pkt()
                .I(wave).I(waveStartTime).I(waveEndTime).I(historyWave).L(unchecked((long)exp)).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Jump(DungeonModel model, uint wave, uint historyWave, ulong exp)
        {
            DungeonModel.AdvancedExpJumpInfoSnapshot actual = model.LastAdvancedExpJumpInfo;
            return model.HasAdvancedExpJumpInfo && actual != null && actual.Wave == wave
                && actual.HistoryWave == historyWave && actual.Exp == exp;
        }

        private static bool Panel(DungeonModel model, uint wave, uint waveStartTime,
            uint waveEndTime, uint historyWave, ulong exp)
        {
            DungeonModel.AdvancedExpInfoSnapshot actual = model.LastAdvancedExpInfo;
            return model.HasAdvancedExpInfo && actual != null && actual.Wave == wave
                && actual.WaveStartTime == waveStartTime && actual.WaveEndTime == waveEndTime
                && actual.HistoryWave == historyWave && actual.Exp == exp;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_ADVANCED_EXP_JUMP_INFO) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_ADVANCED_EXP_JUMP_INFO], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-advanced-exp-jump-info " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
