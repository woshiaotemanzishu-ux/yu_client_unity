using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HolySeal;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class HolySealCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

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
                Debug.LogError("CLIVERIFY holyseal EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            HolySealController controller = HolySealController.Instance;
            HolySealModel model = HolySealModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var old = new Dictionary<string, object>
            {
                ["HasError"] = model.HasError,
                ["LastErrorCode"] = model.LastErrorCode,
                ["LastErrorArgs"] = model.LastErrorArgs,
                ["HasRating"] = model.HasRating,
                ["TotalRating"] = model.TotalRating,
                ["HasEquipSnapshot"] = model.HasEquipSnapshot,
                ["EquipSnapshot"] = model.EquipSnapshot,
                ["HasPillSnapshot"] = model.HasPillSnapshot,
                ["PillSnapshot"] = model.PillSnapshot,
                ["HasSuitPreview"] = model.HasSuitPreview,
                ["SuitPreview"] = model.SuitPreview,
                ["SuitPreviewCode"] = model.SuitPreviewCode,
                ["HasSuitSnapshot"] = model.HasSuitSnapshot,
                ["SuitSnapshot"] = model.SuitSnapshot,
            };
            FieldInfo intercept = typeof(HolySealController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 65400; id <= 65409; id++) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                if (handlers != null)
                    for (int id = 65400; id <= 65409; id++) handlers.Remove(id);

                controller.Init();
                model.Reset();
                MethodInfo on00 = Handler("On65400");
                MethodInfo on01 = Handler("On65401");
                MethodInfo on05 = Handler("On65405");
                MethodInfo on07 = Handler("On65407");
                MethodInfo on08 = Handler("On65408");
                MethodInfo on09 = Handler("On65409");
                int[] registered = { 65400, 65401, 65405, 65407, 65408, 65409 };
                pass = handlers != null && intercept != null
                    && on00 != null && on01 != null && on05 != null && on07 != null && on08 != null && on09 != null
                    && ExactRegistrations(handlers, registered) && ExactRequestSurface();

                var firstHandlers = new Dictionary<int, object>();
                foreach (int id in registered) firstHandlers[id] = handlers[id];
                controller.Init();
                foreach (int id in registered) pass &= ReferenceEquals(handlers[id], firstHandlers[id]);

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));

                pass &= Feed(on00, controller, new CliVerify.Pkt().I(7).S("seed"))
                    && Feed(on07, controller, new CliVerify.Pkt().I(8))
                    && Feed(on01, controller, new CliVerify.Pkt().H(1).C(1).L(2).H(3))
                    && Feed(on05, controller, new CliVerify.Pkt().H(1).I(4).H(5).H(6))
                    && Feed(on08, controller, new CliVerify.Pkt().H(1).I(7).H(8).I(9))
                    && Feed(on09, controller, new CliVerify.Pkt().H(1).I(10).H(11));
                controller.RequestStartup();
                pass &= FramesEqual(frames, EmptyFrame(65401), EmptyFrame(65405)) && IsReset(model);

                frames.Clear();
                controller.RequestPills();
                controller.RequestRating();
                controller.RequestSuitPreview(uint.MaxValue);
                controller.RequestSuitSnapshot();
                pass &= FramesEqual(frames, EmptyFrame(65405), EmptyFrame(65407),
                    Frame(65408, new CliVerify.Pkt().I(uint.MaxValue)), EmptyFrame(65409));

                pass &= Feed(on00, controller, new CliVerify.Pkt().I(uint.MaxValue).S("最大值"))
                    && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "最大值";
                pass &= Feed(on07, controller, new CliVerify.Pkt().I(0))
                    && model.HasRating && model.TotalRating == 0;

                pass &= Feed(on01, controller, new CliVerify.Pkt().H(3)
                    .C(1).L(-1).H(ushort.MaxValue)
                    .C(1).L(0).H(0)
                    .C(byte.MaxValue).L(1).H(2));
                pass &= model.HasEquipSnapshot && model.EquipSnapshot.Count == 3
                    && model.EquipSnapshot[0].Pos == 1 && model.EquipSnapshot[0].GoodsId == ulong.MaxValue
                    && model.EquipSnapshot[0].Strength == ushort.MaxValue
                    && model.EquipSnapshot[1].Pos == 1 && model.EquipSnapshot[1].GoodsId == 0
                    && model.EquipSnapshot[2].Pos == byte.MaxValue;

                pass &= Feed(on05, controller, new CliVerify.Pkt().H(3)
                    .I(uint.MaxValue).H(ushort.MaxValue).H(0)
                    .I(uint.MaxValue).H(0).H(ushort.MaxValue)
                    .I(0).H(1).H(2));
                pass &= model.HasPillSnapshot && model.PillSnapshot.Count == 3
                    && model.PillSnapshot[0].GoodsTypeId == uint.MaxValue
                    && model.PillSnapshot[0].Num == ushort.MaxValue && model.PillSnapshot[0].Limit == 0
                    && model.PillSnapshot[1].GoodsTypeId == uint.MaxValue
                    && model.PillSnapshot[2].GoodsTypeId == 0;

                object equipRef = model.EquipSnapshot;
                object pillRef = model.PillSnapshot;
                pass &= Feed(on08, controller, new CliVerify.Pkt().H(3)
                    .I(uint.MaxValue).H(ushort.MaxValue)
                    .I(uint.MaxValue).H(0)
                    .I(0).H(1).I(uint.MaxValue));
                pass &= model.HasSuitPreview && model.SuitPreview.Count == 3
                    && model.SuitPreview[0].SuitId == uint.MaxValue && model.SuitPreview[0].Num == ushort.MaxValue
                    && model.SuitPreview[1].SuitId == uint.MaxValue && model.SuitPreview[2].SuitId == 0
                    && model.SuitPreviewCode == uint.MaxValue
                    && ReferenceEquals(equipRef, model.EquipSnapshot) && ReferenceEquals(pillRef, model.PillSnapshot);

                object previewRef = model.SuitPreview;
                pass &= Feed(on09, controller, new CliVerify.Pkt().H(2)
                    .I(1).H(2).I(1).H(3));
                pass &= model.HasSuitSnapshot && model.SuitSnapshot.Count == 2
                    && model.SuitSnapshot[0].SuitId == 1 && model.SuitSnapshot[0].Num == 2
                    && model.SuitSnapshot[1].SuitId == 1 && model.SuitSnapshot[1].Num == 3
                    && ReferenceEquals(previewRef, model.SuitPreview) && model.SuitPreviewCode == uint.MaxValue;

                object suitRef = model.SuitSnapshot;
                frames.Clear();
                controller.RequestSuitPreview(123);
                pass &= FramesEqual(frames, Frame(65408, new CliVerify.Pkt().I(123)))
                    && ReferenceEquals(previewRef, model.SuitPreview) && model.SuitPreviewCode == uint.MaxValue
                    && ReferenceEquals(suitRef, model.SuitSnapshot);

                pass &= Feed(on01, controller, new CliVerify.Pkt().H(0))
                    && model.HasEquipSnapshot && model.EquipSnapshot.Count == 0
                    && model.HasPillSnapshot && model.PillSnapshot.Count == 3;
                pass &= Feed(on05, controller, new CliVerify.Pkt().H(0))
                    && model.HasPillSnapshot && model.PillSnapshot.Count == 0
                    && model.HasSuitPreview && model.SuitPreview.Count == 3;
                pass &= Feed(on08, controller, new CliVerify.Pkt().H(0).I(0))
                    && model.HasSuitPreview && model.SuitPreview.Count == 0 && model.SuitPreviewCode == 0
                    && model.HasSuitSnapshot && model.SuitSnapshot.Count == 2;
                pass &= Feed(on09, controller, new CliVerify.Pkt().H(0))
                    && model.HasSuitSnapshot && model.SuitSnapshot.Count == 0
                    && model.HasError && model.HasRating;

                controller.Dispose();
                pass &= !controller.IsInitialized && IsReset(model);
                for (int id = 65400; id <= 65409; id++) pass &= !handlers.Contains(id);
                Debug.Log("CLIVERIFY holyseal VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                foreach (KeyValuePair<string, object> pair in old) RestoreModelProperty(model, pair.Key, pair.Value);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (wasInitialized) controller.Init();
                for (int id = 65400; id <= 65409; id++) RestoreHandler(handlers, savedHandlers[id], id);

                restored = ReferenceEquals(HolySealController.Instance, controller)
                    && ReferenceEquals(HolySealModel.Instance, model)
                    && controller.IsInitialized == wasInitialized;
                foreach (KeyValuePair<string, object> pair in old)
                    restored &= Equals(typeof(HolySealModel).GetProperty(pair.Key)?.GetValue(model), pair.Value);
                for (int id = 65400; id <= 65409; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY holyseal restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static MethodInfo Handler(string name) => typeof(HolySealController).GetMethod(name, F);

        private static bool Feed(MethodInfo handler, HolySealController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExactRegistrations(IDictionary handlers, IReadOnlyList<int> expected)
        {
            var set = new HashSet<int>(expected);
            for (int id = 65400; id <= 65409; id++)
                if (handlers.Contains(id) != set.Contains(id)) return false;
            return true;
        }

        private static bool ExactRequestSurface()
        {
            var expected = new HashSet<string>
            {
                nameof(HolySealController.RequestStartup), nameof(HolySealController.RequestPills),
                nameof(HolySealController.RequestRating), nameof(HolySealController.RequestSuitPreview),
                nameof(HolySealController.RequestSuitSnapshot),
            };
            foreach (MethodInfo method in typeof(HolySealController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0)
                    if (!expected.Remove(method.Name)) return false;
            }
            return expected.Count == 0;
        }

        private static bool IsReset(HolySealModel model) =>
            !model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == null
            && !model.HasRating && model.TotalRating == 0
            && !model.HasEquipSnapshot && model.EquipSnapshot.Count == 0
            && !model.HasPillSnapshot && model.PillSnapshot.Count == 0
            && !model.HasSuitPreview && model.SuitPreview.Count == 0 && model.SuitPreviewCode == 0
            && !model.HasSuitSnapshot && model.SuitSnapshot.Count == 0;

        private static byte[] EmptyFrame(int id) => Frame(id, new CliVerify.Pkt());

        private static byte[] Frame(int id, CliVerify.Pkt payload)
        {
            byte[] body = payload.Bytes();
            var packet = new CliVerify.Pkt().H(6 + body.Length).H(1000).H(id);
            byte[] header = packet.Bytes();
            byte[] result = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(body, 0, result, header.Length, body.Length);
            return result;
        }

        private static bool FramesEqual(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] == null || actual[i].Length != expected[i].Length) return false;
                for (int j = 0; j < expected[i].Length; j++)
                    if (actual[i][j] != expected[i][j]) return false;
            }
            return true;
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null) return;
            if (savedHandler.Exists) handlers[id] = savedHandler.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id) =>
            handlers != null && handlers.Contains(id) == savedHandler.Exists
            && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));

        private static void RestoreModelProperty(HolySealModel model, string propertyName, object value) =>
            typeof(HolySealModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
    }
}
