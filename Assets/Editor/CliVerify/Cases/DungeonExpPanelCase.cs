using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class DungeonExpPanelCase
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
                Debug.LogError("CLIVERIFY dungeon-exp-panel EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasInfo = model.HasExpDungeonInfo;
            ushort oldKillCount = model.ExpDungeonKillCount;
            ulong oldTotalExp = model.ExpDungeonTotalExp;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_EXP_PANEL);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_EXP_PANEL] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                MethodInfo on61044 = typeof(DungeonController).GetMethod("On61044", IF);
                pass = Proto.DUNGEON_EXP_PANEL == 61044 && on61044 != null && No61044RequestApi();
                Check(ref pass, "seams/no request api", pass);

                Check(ref pass, "zero/read-to-end", Feed(on61044, controller, 0, 0)
                    && model.HasExpDungeonInfo && model.ExpDungeonKillCount == 0 && model.ExpDungeonTotalExp == 0);
                Check(ref pass, "u16/u64 max/read-to-end", Feed(on61044, controller, ushort.MaxValue, -1)
                    && model.HasExpDungeonInfo && model.ExpDungeonKillCount == ushort.MaxValue
                    && model.ExpDungeonTotalExp == ulong.MaxValue);
                Check(ref pass, "later packet overwrite", Feed(on61044, controller, 7, 9)
                    && model.HasExpDungeonInfo && model.ExpDungeonKillCount == 7 && model.ExpDungeonTotalExp == 9);
                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));

                Debug.Log("CLIVERIFY dungeon-exp-panel VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasExpDungeonInfo", oldHasInfo);
                RestoreModelProperty(model, "ExpDungeonKillCount", oldKillCount);
                RestoreModelProperty(model, "ExpDungeonTotalExp", oldTotalExp);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasExpDungeonInfo == oldHasInfo
                    && model.ExpDungeonKillCount == oldKillCount
                    && model.ExpDungeonTotalExp == oldTotalExp
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-exp-panel restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, int killCount, long totalExp)
        {
            byte[] bytes = new CliVerify.Pkt().H(killCount).L(totalExp).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool No61044RequestApi()
        {
            foreach (MethodInfo method in typeof(DungeonController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                string name = method.Name;
                bool outbound = name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0;
                bool targets61044 = name.IndexOf("61044", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("ExpDungeon", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("DungeonExp", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("ExpPanel", StringComparison.OrdinalIgnoreCase) >= 0;
                if (outbound && targets61044) return false;
            }

            return true;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_EXP_PANEL) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_EXP_PANEL], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-exp-panel " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
