using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Longlang;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>龙语622读侧五协议的wire、切片隔离、请求边界与生命周期专项。</summary>
    public static class LonglangCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds = { 62200, 62201, 62207, 62208, 62209 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY longlang EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            LonglangController controller = LonglangController.Instance;
            LonglangModel model = LonglangModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            LonglangModel.ErrorSnapshot oldError = model.LastError;
            LonglangModel.EquipmentSnapshot oldEquipments = model.Equipments;
            LonglangModel.RatingSnapshot oldRating = model.Rating;
            LonglangModel.PreviewSnapshot oldPreview = model.LastPreview;
            LonglangModel.SuitInfoSnapshot oldSuitInfo = model.SuitInfo;
            FieldInfo interceptor = typeof(LonglangController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in RegisteredIds) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on62200 = typeof(LonglangController).GetMethod("On62200", F);
                MethodInfo on62201 = typeof(LonglangController).GetMethod("On62201", F);
                MethodInfo on62207 = typeof(LonglangController).GetMethod("On62207", F);
                MethodInfo on62208 = typeof(LonglangController).GetMethod("On62208", F);
                MethodInfo on62209 = typeof(LonglangController).GetMethod("On62209", F);

                bool a = handlers != null && on62200 != null && on62201 != null && on62207 != null
                    && on62208 != null && on62209 != null;
                foreach (int id in RegisteredIds) a &= handlers.Contains(id);
                for (int id = 62202; id <= 62206; id++) a &= !handlers.Contains(id);

                SeedAll(model);
                LonglangModel.EquipmentSnapshot seededEquipments = model.Equipments;
                LonglangModel.RatingSnapshot seededRating = model.Rating;
                LonglangModel.PreviewSnapshot seededPreview = model.LastPreview;
                LonglangModel.SuitInfoSnapshot seededSuitInfo = model.SuitInfo;
                bool b = Invoke(on62200, controller, new CliVerify.Pkt().I(uint.MaxValue).S("参数甲").Bytes())
                    && model.LastError.Code == uint.MaxValue && model.LastError.Args == "参数甲"
                    && ReferenceEquals(model.Equipments, seededEquipments)
                    && ReferenceEquals(model.Rating, seededRating)
                    && ReferenceEquals(model.LastPreview, seededPreview)
                    && ReferenceEquals(model.SuitInfo, seededSuitInfo);

                LonglangModel.ErrorSnapshot parsedError = model.LastError;
                bool c = Invoke(on62201, controller, new CliVerify.Pkt().H(2)
                    .C(2).L(123).H(4)
                    .C(2).L(unchecked((long)0xFEDCBA9876543210UL)).H(ushort.MaxValue).Bytes())
                    && model.Equipments.Items.Count == 2
                    && model.Equipments.Items[0].Position == 2
                    && model.Equipments.Items[0].GoodsId == 123UL
                    && model.Equipments.Items[0].Strength == 4
                    && model.Equipments.Items[1].GoodsId == 0xFEDCBA9876543210UL
                    && model.Equipments.Items[1].Strength == ushort.MaxValue
                    && model.TryGetEquipment(2, out LonglangModel.Equipment effective)
                    && ReferenceEquals(effective, model.Equipments.Items[1])
                    && ReferenceEquals(model.LastError, parsedError)
                    && ReferenceEquals(model.Rating, seededRating)
                    && ReferenceEquals(model.LastPreview, seededPreview)
                    && ReferenceEquals(model.SuitInfo, seededSuitInfo);

                LonglangModel.EquipmentSnapshot parsedEquipments = model.Equipments;
                bool d = Invoke(on62207, controller, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.Rating.Rating == uint.MaxValue
                    && ReferenceEquals(model.LastError, parsedError)
                    && ReferenceEquals(model.Equipments, parsedEquipments)
                    && ReferenceEquals(model.LastPreview, seededPreview)
                    && ReferenceEquals(model.SuitInfo, seededSuitInfo);

                LonglangModel.RatingSnapshot parsedRating = model.Rating;
                bool e = Invoke(on62208, controller, new CliVerify.Pkt().H(2)
                    .I(4000000000L).H(0)
                    .I(4000000000L).H(ushort.MaxValue).I(1).Bytes())
                    && model.LastPreview.IsValid && model.LastPreview.Code == 1
                    && model.LastPreview.Suits.Count == 2
                    && model.LastPreview.Suits[0].SuitId == 4000000000U
                    && model.LastPreview.Suits[0].Num == 0
                    && model.LastPreview.Suits[1].SuitId == 4000000000U
                    && model.LastPreview.Suits[1].Num == ushort.MaxValue
                    && ReferenceEquals(model.LastError, parsedError)
                    && ReferenceEquals(model.Equipments, parsedEquipments)
                    && ReferenceEquals(model.Rating, parsedRating)
                    && ReferenceEquals(model.SuitInfo, seededSuitInfo);

                LonglangModel.PreviewSnapshot validPreview = model.LastPreview;
                bool f = Invoke(on62209, controller, new CliVerify.Pkt().H(2)
                    .I(9).H(2).I(9).H(3).Bytes())
                    && model.SuitInfo.Suits.Count == 2
                    && model.SuitInfo.Suits[0].SuitId == 9 && model.SuitInfo.Suits[0].Num == 2
                    && model.SuitInfo.Suits[1].SuitId == 9 && model.SuitInfo.Suits[1].Num == 3
                    && ReferenceEquals(model.LastPreview, validPreview)
                    && Invoke(on62209, controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasSuitInfo && model.SuitInfo.Suits.Count == 0
                    && ReferenceEquals(model.LastPreview, validPreview)
                    && Invoke(on62208, controller, new CliVerify.Pkt().H(0).I(uint.MaxValue).Bytes())
                    && model.HasPreview && !model.LastPreview.IsValid
                    && model.LastPreview.Code == uint.MaxValue && model.LastPreview.Suits.Count == 0
                    && model.HasError && model.HasEquipments && model.HasRating && model.HasSuitInfo;

                var source = new List<LonglangModel.Equipment>
                {
                    new LonglangModel.Equipment(1, 1, 1),
                };
                model.ReplaceEquipments(source);
                LonglangModel.EquipmentSnapshot immutable = model.Equipments;
                source.Clear();
                bool g = immutable.Items.Count == 1;

                var frames = new List<byte[]>();
                interceptor?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                SeedAll(model);
                controller.RequestStartup();
                bool h = IsEmpty(model) && frames.Count == 1
                    && BytesEqual(frames[0], new CliVerify.Pkt().H(6).H(1000).H(62201).Bytes());

                SeedAll(model);
                LonglangModel.ErrorSnapshot requestError = model.LastError;
                LonglangModel.EquipmentSnapshot requestEquipments = model.Equipments;
                LonglangModel.RatingSnapshot requestRating = model.Rating;
                LonglangModel.PreviewSnapshot requestPreview = model.LastPreview;
                LonglangModel.SuitInfoSnapshot requestSuitInfo = model.SuitInfo;
                frames.Clear();
                controller.RequestRating();
                controller.RequestSuitPreview(uint.MaxValue);
                controller.RequestSuitInfo();
                bool i = ExplicitFramesAre(frames)
                    && ReferenceEquals(model.LastError, requestError)
                    && ReferenceEquals(model.Equipments, requestEquipments)
                    && ReferenceEquals(model.Rating, requestRating)
                    && ReferenceEquals(model.LastPreview, requestPreview)
                    && ReferenceEquals(model.SuitInfo, requestSuitInfo);

                pass = a && b && c && d && e && f && g && h && i;
                Debug.Log("CLIVERIFY longlang A=" + a + " B=" + b + " C=" + c + " D=" + d
                    + " E=" + e + " F=" + f + " G=" + g + " H=" + h + " I=" + i);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreProperty(model, "LastError", oldError);
                RestoreProperty(model, "Equipments", oldEquipments);
                RestoreProperty(model, "Rating", oldRating);
                RestoreProperty(model, "LastPreview", oldPreview);
                RestoreProperty(model, "SuitInfo", oldSuitInfo);
                if (wasInitialized) controller.Init();
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ReferenceEquals(model.LastError, oldError)
                    && ReferenceEquals(model.Equipments, oldEquipments)
                    && ReferenceEquals(model.Rating, oldRating)
                    && ReferenceEquals(model.LastPreview, oldPreview)
                    && ReferenceEquals(model.SuitInfo, oldSuitInfo)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY longlang restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static void SeedAll(LonglangModel model)
        {
            model.ReplaceError(1, "seed");
            model.ReplaceEquipments(Array.Empty<LonglangModel.Equipment>());
            model.ReplaceRating(1);
            model.ReplacePreview(Array.Empty<LonglangModel.SuitEntry>(), 1);
            model.ReplaceSuitInfo(Array.Empty<LonglangModel.SuitEntry>());
        }

        private static bool IsEmpty(LonglangModel model) => !model.HasError && !model.HasEquipments
            && !model.HasRating && !model.HasPreview && !model.HasSuitInfo;

        private static bool Invoke(MethodInfo handler, LonglangController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExplicitFramesAre(IReadOnlyList<byte[]> frames) => frames != null
            && frames.Count == 3
            && BytesEqual(frames[0], new CliVerify.Pkt().H(6).H(1000).H(62207).Bytes())
            && BytesEqual(frames[1], new CliVerify.Pkt().H(10).H(1000).H(62208).I(uint.MaxValue).Bytes())
            && BytesEqual(frames[2], new CliVerify.Pkt().H(6).H(1000).H(62209).Bytes());

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static void RestoreProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);

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

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id) =>
            handlers != null && handlers.Contains(id) == saved.Exists
            && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
    }
}

