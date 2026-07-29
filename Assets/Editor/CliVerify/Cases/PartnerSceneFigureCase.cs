using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Partner;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>R480:14200 S2C-only 原始场景伙伴外观通知与 14203/14206/14207 排除边界。</summary>
    public static class PartnerSceneFigureCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY partner-scene-figure EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            PartnerController controller = PartnerController.Instance;
            PartnerModel model = PartnerModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            int oldFightId = model.FightId;
            var oldCompanions = new List<PartnerModel.CompanionVo>(model.Companions);
            PartnerModel.SceneFigureNotice oldNotice = model.LastSceneFigureNotice;
            FieldInfo noticeField = typeof(PartnerModel).GetField("<LastSceneFigureNotice>k__BackingField", InstancePrivate);
            FieldInfo hasDataField = typeof(PartnerModel).GetField("<HasData>k__BackingField", InstancePrivate);
            var handlers = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 14200; id <= 14207; id++) SaveHandler(handlers, savedHandlers, id);

            bool pass = true;
            bool restored = false;
            Action<PartnerModel.SceneFigureNotice> onNotice = null;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                for (int id = 14200; id <= 14207; id++) handlers?.Remove(id);
                model.Clear();
                controller.Init();

                MethodInfo on14200 = typeof(PartnerController).GetMethod("On14200", InstancePrivate);
                bool seams = handlers != null && on14200 != null
                    && handlers.Contains(14200) && handlers.Contains(14201) && handlers.Contains(14202)
                    && !handlers.Contains(14203) && handlers.Contains(14204) && handlers.Contains(14205)
                    && !handlers.Contains(14206) && !handlers.Contains(14207)
                    && Proto.PARTNER_SCENE_FIGURE == 14200 && NoProtoConstants(14203, 14206, 14207)
                    && NoDeferredPublicSurface();
                Check(ref pass, "seams/14200-only-plus-existing", seams);

                object firstHandler = handlers?[14200];
                controller.Init();
                Check(ref pass, "init-idempotent", firstHandler != null && ReferenceEquals(firstHandler, handlers?[14200]));

                var companion = new PartnerModel.CompanionVo { CompanionId = 17, Stage = 3, Star = 4 };
                model.SetList(99, new List<PartnerModel.CompanionVo> { companion });
                int eventCount = 0;
                PartnerModel.SceneFigureNotice eventNotice = null;
                onNotice = notice => { eventCount++; eventNotice = notice; };
                EventDispatcher.On(GlobalEvent.EVT_PARTNER_SCENE_FIGURE_CHANGE, onNotice);

                NetReader maxReader = Feed(on14200, controller,
                    new CliVerify.Pkt().C(byte.MaxValue).L(-1L).I(uint.MaxValue).C(0xA5).Bytes());
                PartnerModel.SceneFigureNotice first = model.LastSceneFigureNotice;
                bool tail = maxReader.Remaining == 1 && maxReader.ReadU8() == 0xA5 && maxReader.Remaining == 0;
                Check(ref pass, "max-boundaries/tail/event/list-isolated", tail && model.HasSceneFigureNotice
                    && first != null && first.TypeId == byte.MaxValue && first.RoleId == -1L
                    && first.FigureId == uint.MaxValue && eventCount == 1 && ReferenceEquals(eventNotice, first)
                    && model.HasData && model.FightId == 99 && model.Companions.Count == 1
                    && ReferenceEquals(model.Companions[0], companion));

                NetReader zeroReader = Feed(on14200, controller, new CliVerify.Pkt().C(0).L(0).I(0).Bytes());
                PartnerModel.SceneFigureNotice second = model.LastSceneFigureNotice;
                Check(ref pass, "zero-full-replace/history-immutable", zeroReader.Remaining == 0
                    && second != null && !ReferenceEquals(first, second) && second.TypeId == 0
                    && second.RoleId == 0 && second.FigureId == 0 && eventCount == 2
                    && ReferenceEquals(eventNotice, second) && first.TypeId == byte.MaxValue
                    && first.RoleId == -1L && first.FigureId == uint.MaxValue
                    && model.FightId == 99 && ReferenceEquals(model.Companions[0], companion));

                model.SetList(123, new List<PartnerModel.CompanionVo> { companion });
                Check(ref pass, "list-replace-preserves-notice", ReferenceEquals(model.LastSceneFigureNotice, second)
                    && model.HasSceneFigureNotice && model.FightId == 123 && model.Companions.Count == 1);

                EventDispatcher.Off(GlobalEvent.EVT_PARTNER_SCENE_FIGURE_CHANGE, onNotice);
                onNotice = null;
                controller.Dispose();
                Check(ref pass, "dispose-unregisters-and-clears", !controller.IsInitialized
                    && !handlers.Contains(14200) && !handlers.Contains(14201) && !handlers.Contains(14202)
                    && !handlers.Contains(14204) && !handlers.Contains(14205)
                    && !model.HasData && model.Companions.Count == 0 && model.FightId == 0
                    && !model.HasSceneFigureNotice && model.LastSceneFigureNotice == null);
                Debug.Log("CLIVERIFY partner-scene-figure VERDICT pass=" + pass);
            }
            finally
            {
                if (onNotice != null) EventDispatcher.Off(GlobalEvent.EVT_PARTNER_SCENE_FIGURE_CHANGE, onNotice);
                if (controller.IsInitialized) controller.Dispose();
                model.Clear();
                model.Companions.AddRange(oldCompanions);
                model.FightId = oldFightId;
                hasDataField?.SetValue(model, oldHasData);
                noticeField?.SetValue(model, oldNotice);
                if (wasInitialized) controller.Init();
                for (int id = 14200; id <= 14207; id++) RestoreHandler(handlers, savedHandlers[id], id);

                restored = controller.IsInitialized == wasInitialized && model.HasData == oldHasData
                    && model.FightId == oldFightId && SequenceReferenceEqual(model.Companions, oldCompanions)
                    && ReferenceEquals(model.LastSceneFigureNotice, oldNotice);
                for (int id = 14200; id <= 14207; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY partner-scene-figure restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static NetReader Feed(MethodInfo method, PartnerController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader;
        }

        private static bool NoProtoConstants(params int[] forbidden)
        {
            foreach (FieldInfo field in typeof(Proto).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(int)) continue;
                int value = (int)field.GetValue(null);
                for (int i = 0; i < forbidden.Length; i++) if (value == forbidden[i]) return false;
            }
            return true;
        }

        private static bool NoDeferredPublicSurface()
        {
            foreach (MethodInfo method in typeof(PartnerController)
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                string name = method.Name;
                if (name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Follow", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Nucleus", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Biography", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            }
            return true;
        }

        private static void Check(ref bool pass, string tag, bool ok)
        {
            Debug.Log("CLIVERIFY partner-scene-figure " + tag + " ok=" + ok);
            if (!ok) pass = false;
        }

        private static bool SequenceReferenceEqual(IReadOnlyList<PartnerModel.CompanionVo> current,
            IList<PartnerModel.CompanionVo> saved)
        {
            if (current.Count != saved.Count) return false;
            for (int i = 0; i < current.Count; i++)
                if (!ReferenceEquals(current[i], saved[i])) return false;
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
    }
}
