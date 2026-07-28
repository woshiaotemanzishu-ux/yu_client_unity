using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61065 入场自动鼓舞专项：纯 S2C、两字段整体覆盖、加成一致与 ambient 精确恢复。</summary>
    public static class DungeonInspiritEntryCase
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
                Debug.LogError("CLIVERIFY dungeon-inspirit-entry EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            int oldCoinCount = model.InspiritCoinCount;
            int oldGoldCount = model.InspiritGoldCount;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null
                && handlers.Contains(Proto.DUNGEON_INSPIRIT_ENTRY_STATE);
            object oldHandler = oldHandlerExists
                ? handlers[Proto.DUNGEON_INSPIRIT_ENTRY_STATE]
                : null;
            bool pass = false;
            bool restored = false;

            try
            {
                MethodInfo on61065 = typeof(DungeonController).GetMethod("On61065", IF);
                MethodInfo request = typeof(DungeonController).GetMethod(
                    "RequestDungeonInspiritEntryState", ALL);
                FieldInfo intercept = typeof(DungeonController).GetField(
                    "s_dungeonInspiritEntryStateOutboundIntercept", ALL);
                pass = Proto.DUNGEON_INSPIRIT_ENTRY_STATE == 61065 && on61065 != null
                    && request == null && intercept == null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "s2c-only seams/registration/no-sender", pass);
                if (on61065 == null) throw new MissingMethodException("On61065 seam missing");

                model.SetInspiritInfo(7, 8);
                Check(ref pass, "no response keeps model", State(model, 7, 8, 150, 75));

                Check(ref pass, "2B max whole replace/read-to-end",
                    Feed(on61065, controller, byte.MaxValue, byte.MaxValue)
                    && State(model, byte.MaxValue, byte.MaxValue, 5100, 2550));

                Check(ref pass, "2B small whole replace/read-to-end",
                    Feed(on61065, controller, 2, 1)
                    && State(model, 2, 1, 30, 15));

                Check(ref pass, "2B zero whole replace/read-to-end",
                    Feed(on61065, controller, 0, 0)
                    && State(model, 0, 0, 0, 0));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-inspirit-entry VERDICT pass=" + pass);
            }
            finally
            {
                model.SetInspiritInfo(oldCoinCount, oldGoldCount);
                restored = controller.IsInitialized == oldInitialized
                    && model.InspiritCoinCount == oldCoinCount
                    && model.InspiritGoldCount == oldGoldCount
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-inspirit-entry restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller,
            byte coinCount, byte goldCount)
        {
            byte[] bytes = new CliVerify.Pkt().C(coinCount).C(goldCount).Bytes();
            if (bytes.Length != 2) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool State(DungeonModel model, int coinCount, int goldCount,
            int normalBonus, int guildBonus)
        {
            return model.InspiritCoinCount == coinCount
                && model.InspiritGoldCount == goldCount
                && model.GetInspiritBonusPercent(12001) == normalBonus
                && model.GetInspiritBonusPercent(DungeonModel.GUILD_EXP_ID) == guildBonus;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null
                && handlers.Contains(Proto.DUNGEON_INSPIRIT_ENTRY_STATE) == existed
                && (!existed
                    || ReferenceEquals(handlers[Proto.DUNGEON_INSPIRIT_ENTRY_STATE], value));
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-inspirit-entry " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
