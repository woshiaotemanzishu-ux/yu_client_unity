using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Designation;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>称号读链与 41102/03/06/09 已闭环事务的注册边界、隔离和 ambient 恢复。</summary>
    public static class DesignationReadContinuationCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly int[] AllCommands =
            { 41100, 41101, 41102, 41103, 41104, 41105, 41106, 41107, 41108, 41109, 41110 };
        private static readonly int[] RegisteredCommands =
            { 41101, 41102, 41103, 41104, 41105, 41106, 41107, 41108, 41109 };

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY designation-read-continuation EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DesignationController controller = DesignationController.Instance;
            DesignationModel model = DesignationModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<FieldInfo, object> oldAutoFields = CaptureAutoFields(model);
            var oldEntries = new List<DesignationModel.Entry>(model.Entries);
            FieldInfo intercept = typeof(DesignationController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
                foreach (int command in AllCommands)
                    if (handlers.Contains(command)) oldHandlers[command] = handlers[command];

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                MethodInfo h02 = Handler("On41102");
                MethodInfo h03 = Handler("On41103");
                MethodInfo h04 = Handler("On41104");
                MethodInfo h05 = Handler("On41105");
                MethodInfo h06 = Handler("On41106");
                MethodInfo h07 = Handler("On41107");
                MethodInfo h08 = Handler("On41108");
                MethodInfo h09 = Handler("On41109");
                pass = Proto.DESIGNATION_WEAR == 41102
                    && Proto.DESIGNATION_UNWEAR == 41103
                    && Proto.DESIGNATION_ACTIVATED == 41104
                    && Proto.DESIGNATION_SCENE_NOTICE == 41105
                    && Proto.DESIGNATION_UPGRADE == 41106
                    && Proto.DESIGNATION_POWER == 41107
                    && Proto.DESIGNATION_REMOVED == 41108
                    && Proto.DESIGNATION_ACTIVATE_BY_GOODS == 41109
                    && h02 != null && h03 != null && h04 != null && h05 != null && h06 != null
                    && h07 != null && h08 != null && h09 != null
                    && intercept != null && RegistrationsExact(handlers) && RequestSurfaceExact();
                Check(ref pass, "constants/registration/only-closed-write-registered", pass);

                model.Reset();
                model.ReplaceData(10, new List<DesignationModel.Entry>
                    { new DesignationModel.Entry(11, 12, 13) });
                model.ReplaceActivation(14, 15, 16);
                model.ReplaceSceneNotice(17, 18);
                model.ReplacePowerQuery(19, 20);
                model.ReplaceRemoval(21);
                model.ReplaceGoodsActivationResult(22, 23, 24, 25);
                DesignationModel.ActivationSnapshot sentinelActivation = model.Activation;
                DesignationModel.SceneNoticeSnapshot sentinelScene = model.SceneNotice;
                DesignationModel.PowerQuerySnapshot sentinelPower = model.PowerQuery;
                DesignationModel.RemovalSnapshot sentinelRemoval = model.Removal;
                DesignationModel.GoodsActivationResultSnapshot sentinelGoodsActivation = model.GoodsActivationResult;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                controller.RequestPower(0);
                controller.RequestPower(uint.MaxValue);
                Check(ref pass, "startup/power exact frames/no-response-preserves", frames.Count == 3
                    && EmptyFrame(frames[0], 41101) && U32Frame(frames[1], 41107, 0)
                    && U32Frame(frames[2], 41107, uint.MaxValue)
                    && ReferenceEquals(model.Activation, sentinelActivation)
                    && ReferenceEquals(model.SceneNotice, sentinelScene)
                    && ReferenceEquals(model.PowerQuery, sentinelPower)
                    && ReferenceEquals(model.Removal, sentinelRemoval)
                    && ReferenceEquals(model.GoodsActivationResult, sentinelGoodsActivation));
                frames.Clear();

                Check(ref pass, "41104 full overwrite/zero-max/read-end/no-auto-wear",
                    Feed(h04, controller, new CliVerify.Pkt().I(uint.MaxValue).I(uint.MaxValue).I(uint.MaxValue))
                    && model.Activation.Code == uint.MaxValue && model.Activation.Id == uint.MaxValue
                    && model.Activation.EndTime == uint.MaxValue && frames.Count == 0
                    && Feed(h04, controller, new CliVerify.Pkt().I(0).I(0).I(0))
                    && model.Activation.Code == 0 && model.Activation.Id == 0 && model.Activation.EndTime == 0
                    && frames.Count == 0 && model.HasData && model.CurrentUsedId == 10);
                DesignationModel.ActivationSnapshot activationAfter04 = model.Activation;

                Check(ref pass, "41105 u64 full-width/overwrite/isolation/read-end",
                    Feed(h05, controller, new CliVerify.Pkt().L(-1).I(uint.MaxValue))
                    && model.SceneNotice.PlayerId == ulong.MaxValue && model.SceneNotice.Id == uint.MaxValue
                    && Feed(h05, controller, new CliVerify.Pkt().L(0).I(0))
                    && model.SceneNotice.PlayerId == 0 && model.SceneNotice.Id == 0
                    && ReferenceEquals(model.Activation, activationAfter04) && frames.Count == 0);
                DesignationModel.SceneNoticeSnapshot sceneAfter05 = model.SceneNotice;

                Check(ref pass, "41107 raw zero-max/overwrite/isolation/read-end",
                    Feed(h07, controller, new CliVerify.Pkt().I(uint.MaxValue).I(uint.MaxValue))
                    && model.PowerQuery.Code == uint.MaxValue && model.PowerQuery.Power == uint.MaxValue
                    && Feed(h07, controller, new CliVerify.Pkt().I(0).I(0))
                    && model.PowerQuery.Code == 0 && model.PowerQuery.Power == 0
                    && ReferenceEquals(model.SceneNotice, sceneAfter05) && frames.Count == 0);
                DesignationModel.PowerQuerySnapshot powerAfter07 = model.PowerQuery;

                Check(ref pass, "41108 zero-max/overwrite/isolation/read-end/no-list-patch",
                    Feed(h08, controller, new CliVerify.Pkt().I(uint.MaxValue))
                    && model.Removal.Id == uint.MaxValue
                    && Feed(h08, controller, new CliVerify.Pkt().I(0)) && model.Removal.Id == 0
                    && ReferenceEquals(model.PowerQuery, powerAfter07)
                    && model.HasData && model.CurrentUsedId == 10 && model.Entries.Count == 1
                    && model.Entries[0].Id == 11 && frames.Count == 0);

                model.ClearReadContinuationSnapshots();
                Check(ref pass, "slice clear preserves authoritative list", model.Activation == null
                    && model.SceneNotice == null && model.PowerQuery == null && model.Removal == null
                    && ReferenceEquals(model.GoodsActivationResult, sentinelGoodsActivation)
                    && model.HasData && model.CurrentUsedId == 10 && model.Entries.Count == 1
                    && model.Entries[0].Id == 11);

                model.ReplaceActivation(1, 2, 3); model.ReplaceSceneNotice(4, 5);
                model.ReplacePowerQuery(6, 7); model.ReplaceRemoval(8);
                controller.Dispose();
                Check(ref pass, "dispose owns slices and handlers", !controller.IsInitialized
                    && !model.HasData && model.CurrentUsedId == 0 && model.Entries.Count == 0
                    && model.Activation == null && model.SceneNotice == null
                    && model.PowerQuery == null && model.Removal == null
                    && model.GoodsActivationResult == null && model.UpgradeResult == null
                    && NoRegisteredHandlers(handlers));
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                RestoreEntries(model, oldEntries);
                RestoreAutoFields(model, oldAutoFields);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (oldInitialized) controller.Init();
                RestoreHandlers(handlers, oldHandlers);
                restored = controller.IsInitialized == oldInitialized
                    && SameAutoFields(model, oldAutoFields) && SameRefs(model.Entries, oldEntries)
                    && SameHandlers(handlers, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY designation-read-continuation restored=" + restored);
            Debug.Log("CLIVERIFY designation-read-continuation VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static MethodInfo Handler(string name) => typeof(DesignationController).GetMethod(name, IF);

        private static bool Feed(MethodInfo method, DesignationController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool RequestSurfaceExact()
        {
            int count = 0;
            foreach (MethodInfo method in typeof(DesignationController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!method.Name.StartsWith("Request", StringComparison.Ordinal)) continue;
                count++;
                if (method.Name != "RequestStartup" && method.Name != "RequestPower") return false;
            }
            return count == 2;
        }

        private static bool EmptyFrame(byte[] frame, int command) => frame != null && frame.Length == 6
            && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
            && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command;

        private static bool U32Frame(byte[] frame, int command, uint value) => frame != null && frame.Length == 10
            && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
            && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command
            && frame[6] == (byte)(value >> 24) && frame[7] == (byte)(value >> 16)
            && frame[8] == (byte)(value >> 8) && frame[9] == (byte)value;

        private static bool IsRegistered(int command)
        {
            for (int i = 0; i < RegisteredCommands.Length; i++)
                if (RegisteredCommands[i] == command) return true;
            return false;
        }

        private static bool RegistrationsExact(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int i = 0; i < AllCommands.Length; i++)
                if (handlers.Contains(AllCommands[i]) != IsRegistered(AllCommands[i])) return false;
            return true;
        }

        private static bool NoRegisteredHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int i = 0; i < RegisteredCommands.Length; i++)
                if (handlers.Contains(RegisteredCommands[i])) return false;
            return true;
        }

        private static Dictionary<FieldInfo, object> CaptureAutoFields(DesignationModel model)
        {
            var values = new Dictionary<FieldInfo, object>();
            foreach (FieldInfo field in typeof(DesignationModel).GetFields(IF))
                if (field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                    values[field] = field.GetValue(model);
            return values;
        }

        private static void RestoreAutoFields(DesignationModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values) pair.Key.SetValue(model, pair.Value);
        }

        private static bool SameAutoFields(DesignationModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values)
                if (!Equals(pair.Key.GetValue(model), pair.Value)) return false;
            return true;
        }

        private static void RestoreEntries(DesignationModel model, List<DesignationModel.Entry> entries)
        {
            FieldInfo field = typeof(DesignationModel).GetField("_entries", IF);
            var list = field?.GetValue(model) as List<DesignationModel.Entry>;
            if (list == null) return;
            list.Clear();
            list.AddRange(entries);
        }

        private static void RestoreHandlers(IDictionary handlers, Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return;
            for (int i = 0; i < AllCommands.Length; i++)
            {
                int command = AllCommands[i];
                if (handlers.Contains(command)) handlers.Remove(command);
                if (oldHandlers.TryGetValue(command, out object handler)) handlers[command] = handler;
            }
        }

        private static bool SameHandlers(IDictionary handlers, Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return oldHandlers.Count == 0;
            for (int i = 0; i < AllCommands.Length; i++)
            {
                int command = AllCommands[i];
                bool existed = oldHandlers.TryGetValue(command, out object oldHandler);
                if (handlers.Contains(command) != existed
                    || (existed && !ReferenceEquals(handlers[command], oldHandler))) return false;
            }
            return true;
        }

        private static bool SameRefs<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) where T : class
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY designation-read-continuation " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
