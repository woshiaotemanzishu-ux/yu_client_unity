using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GodBeast;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GodBeastCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds = { 17300, 17301, 17302, 17308, 17309 };
        private sealed class HandlerState { public bool Exists; public object Value; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY godbeast EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            GodBeastController controller = GodBeastController.Instance;
            GodBeastModel model = GodBeastModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            byte oldFight = model.FightCount;
            var oldBeasts = new List<GodBeastModel.Beast>(model.Beasts);
            bool oldHasData = model.HasData;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorArgs = model.LastErrorArgs;
            bool oldHasPreview = model.HasStrengthPreview;
            ulong oldPreviewGoods = model.PreviewGoodsId;
            ushort oldPreviewStrengthen = model.PreviewStrengthen;
            uint oldPreviewExp = model.PreviewExp;
            FieldInfo powerField = typeof(GodBeastModel).GetField("_attributePowers", F);
            var powerMap = powerField?.GetValue(model) as Dictionary<uint, uint>;
            var oldPowers = powerMap == null ? null : new Dictionary<uint, uint>(powerMap);
            FieldInfo interceptor = typeof(GodBeastController).GetField("s_outboundIntercept", SF);
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
                MethodInfo on17300 = typeof(GodBeastController).GetMethod("On17300", F);
                MethodInfo on17301 = typeof(GodBeastController).GetMethod("On17301", F);
                MethodInfo on17302 = typeof(GodBeastController).GetMethod("On17302", F);
                MethodInfo on17308 = typeof(GodBeastController).GetMethod("On17308", F);
                MethodInfo on17309 = typeof(GodBeastController).GetMethod("On17309", F);

                bool a = interceptor != null && handlers != null && powerMap != null
                    && on17300 != null && on17301 != null && on17302 != null && on17308 != null && on17309 != null;
                foreach (int id in RegisteredIds) a &= handlers != null && handlers.Contains(id);
                for (int id = 17303; id <= 17312; id++)
                    if (id != 17308 && id != 17309) a &= handlers != null && !handlers.Contains(id);

                var frames = new List<byte[]>();
                bool b = false;
                bool c = false;
                bool d = false;
                bool e = false;
                bool f = false;
                bool g = false;
                if (a)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    controller.RequestStartup();
                    controller.RequestStrengthPreview(ulong.MaxValue, byte.MaxValue, new[] { 0UL, ulong.MaxValue });
                    controller.RequestAttributePower(ushort.MaxValue, byte.MaxValue, new[]
                    {
                        new GodBeastModel.Attr(0, 0),
                        new GodBeastModel.Attr(ushort.MaxValue, uint.MaxValue),
                    });
                    b = frames.Count == 3
                        && BytesEqual(frames[0], new CliVerify.Pkt().H(6).H(1000).H(17301).Bytes())
                        && BytesEqual(frames[1], new CliVerify.Pkt().H(33).H(1000).H(17308).L(-1).C(255).H(2).L(0).L(-1).Bytes())
                        && BytesEqual(frames[2], new CliVerify.Pkt().H(23).H(1000).H(17309).H(65535).C(255).H(2).H(0).I(0).H(65535).I(uint.MaxValue).Bytes());
                    frames.Clear();

                    var earlyUpdate = new CliVerify.Pkt().I(7).C(9).I(99).H(0).H(0).Bytes();
                    c = Invoke(on17302, controller, earlyUpdate) && !model.HasData && model.Beasts.Count == 0;

                    byte[] snapshot = new CliVerify.Pkt()
                        .C(2).H(3)
                        .I(7).C(1).I(11).H(2)
                        .C(1).L(0).H(0).I(0)
                        .C(2).L(-1).H(65535).I(uint.MaxValue)
                        .H(2).H(0).I(0).H(65535).I(uint.MaxValue)
                        .I(7).C(2).I(22).H(0).H(0)
                        .I(8).C(3).I(33).H(0).H(0)
                        .Bytes();
                    c &= Invoke(on17301, controller, snapshot)
                        && model.HasData && model.FightCount == 2 && model.Beasts.Count == 3
                        && model.Beasts[0].Id == 7 && model.Beasts[0].Equips.Count == 2 && model.Beasts[0].Attrs.Count == 2
                        && model.Beasts[0].Equips[1].GoodsId == ulong.MaxValue
                        && model.Beasts[0].Equips[1].Strengthen == ushort.MaxValue
                        && model.Beasts[0].Equips[1].Exp == uint.MaxValue
                        && model.Beasts[0].Attrs[1].Type == ushort.MaxValue && model.Beasts[0].Attrs[1].Value == uint.MaxValue;
                    GodBeastModel.Beast second = model.Beasts[1];
                    GodBeastModel.Beast third = model.Beasts[2];
                    byte[] update = new CliVerify.Pkt().I(7).C(9).I(99).H(1)
                        .C(4).L(5000000000L).H(6).I(7).H(1).H(8).I(9).Bytes();
                    c &= Invoke(on17302, controller, update)
                        && model.FightCount == 2 && model.Beasts.Count == 3
                        && model.Beasts[0].Id == 7 && model.Beasts[0].State == 9 && model.Beasts[0].Score == 99
                        && model.Beasts[0].Equips.Count == 1 && model.Beasts[0].Equips[0].GoodsId == 5000000000UL
                        && model.Beasts[0].Attrs.Count == 1 && model.Beasts[0].Attrs[0].Type == 8 && model.Beasts[0].Attrs[0].Value == 9
                        && ReferenceEquals(model.Beasts[1], second) && ReferenceEquals(model.Beasts[2], third);
                    GodBeastModel.Beast first = model.Beasts[0];
                    byte[] unknown = new CliVerify.Pkt().I(9).C(1).I(1).H(0).H(0).Bytes();
                    c &= Invoke(on17302, controller, unknown)
                        && ReferenceEquals(model.Beasts[0], first) && ReferenceEquals(model.Beasts[1], second)
                        && ReferenceEquals(model.Beasts[2], third) && frames.Count == 0;

                    d = Invoke(on17308, controller, new CliVerify.Pkt().L(-1).H(65535).I(uint.MaxValue).Bytes())
                        && model.HasStrengthPreview && model.PreviewGoodsId == ulong.MaxValue
                        && model.PreviewStrengthen == ushort.MaxValue && model.PreviewExp == uint.MaxValue;

                    uint power;
                    e = Invoke(on17309, controller, new CliVerify.Pkt().H(2).C(3).I(uint.MaxValue).Bytes())
                        && model.TryGetAttributePower(2, 3, out power) && power == uint.MaxValue
                        && Invoke(on17309, controller, new CliVerify.Pkt().H(2).C(4).I(0).Bytes())
                        && model.TryGetAttributePower(2, 4, out power) && power == 0 && model.AttributePowerCount == 2
                        && Invoke(on17309, controller, new CliVerify.Pkt().H(2).C(3).I(0).Bytes())
                        && model.TryGetAttributePower(2, 3, out power) && power == 0 && model.AttributePowerCount == 2;
                    controller.RequestStrengthPreview(1, 0, null);
                    controller.RequestAttributePower(1, 2, null);
                    e &= frames.Count == 2
                        && BytesEqual(frames[0], new CliVerify.Pkt().H(17).H(1000).H(17308).L(1).C(0).H(0).Bytes())
                        && BytesEqual(frames[1], new CliVerify.Pkt().H(11).H(1000).H(17309).H(1).C(2).H(0).Bytes())
                        && model.HasStrengthPreview && model.PreviewGoodsId == ulong.MaxValue
                        && model.TryGetAttributePower(2, 3, out power) && power == 0 && model.AttributePowerCount == 2;
                    frames.Clear();

                    f = VerifyError(on17300, controller, uint.MaxValue, "最终")
                        && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "最终"
                        && Invoke(on17301, controller, new CliVerify.Pkt().C(9).H(0).Bytes())
                        && model.HasData && model.FightCount == 9 && model.Beasts.Count == 0
                        && model.HasStrengthPreview && model.PreviewGoodsId == ulong.MaxValue
                        && model.AttributePowerCount == 2 && model.TryGetAttributePower(2, 4, out power) && power == 0
                        && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "最终"
                        && Invoke(on17302, controller, update) && model.Beasts.Count == 0
                        && Invoke(on17308, controller, new CliVerify.Pkt().L(0).H(0).I(0).Bytes())
                        && model.HasStrengthPreview && model.PreviewGoodsId == 0 && model.PreviewStrengthen == 0 && model.PreviewExp == 0
                        && frames.Count == 0;

                    controller.Dispose();
                    g = !controller.IsInitialized && !model.HasData && model.FightCount == 0 && model.Beasts.Count == 0
                        && !model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == null
                        && !model.HasStrengthPreview && model.PreviewGoodsId == 0 && model.PreviewStrengthen == 0 && model.PreviewExp == 0
                        && model.AttributePowerCount == 0;
                    foreach (int id in RegisteredIds) g &= !handlers.Contains(id);
                }
                pass = a && b && c && d && e && f && g;
                Debug.Log($"CLIVERIFY godbeast VERDICT A={a} B={b} C={c} D={d} E={e} F={f} G={g} pass={pass}");
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.ReplaceData(oldFight, oldBeasts);
                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                RestoreModelProperty(model, "LastErrorArgs", oldErrorArgs);
                RestoreModelProperty(model, "HasStrengthPreview", oldHasPreview);
                RestoreModelProperty(model, "PreviewGoodsId", oldPreviewGoods);
                RestoreModelProperty(model, "PreviewStrengthen", oldPreviewStrengthen);
                RestoreModelProperty(model, "PreviewExp", oldPreviewExp);
                powerMap = powerField?.GetValue(model) as Dictionary<uint, uint>;
                powerMap?.Clear();
                if (powerMap != null && oldPowers != null)
                    foreach (KeyValuePair<uint, uint> pair in oldPowers) powerMap[pair.Key] = pair.Value;
                if (wasInitialized) controller.Init();
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && model.FightCount == oldFight && model.HasData == oldHasData && BeastsMatch(model.Beasts, oldBeasts)
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode && model.LastErrorArgs == oldErrorArgs
                    && model.HasStrengthPreview == oldHasPreview && model.PreviewGoodsId == oldPreviewGoods
                    && model.PreviewStrengthen == oldPreviewStrengthen && model.PreviewExp == oldPreviewExp
                    && DictionariesMatch(powerMap, oldPowers)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY godbeast restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool Invoke(MethodInfo handler, GodBeastController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool VerifyError(MethodInfo handler, GodBeastController controller, uint code, string args)
        {
            return Invoke(handler, controller, new CliVerify.Pkt().I(code).S(args).Bytes());
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

        private static void RestoreModelProperty(GodBeastModel model, string name, object value)
        {
            typeof(GodBeastModel).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static bool BeastsMatch(IReadOnlyList<GodBeastModel.Beast> actual, IReadOnlyList<GodBeastModel.Beast> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool DictionariesMatch(IReadOnlyDictionary<uint, uint> actual, IReadOnlyDictionary<uint, uint> expected)
        {
            if (actual == null || expected == null) return actual == null && expected == null;
            if (actual.Count != expected.Count) return false;
            foreach (KeyValuePair<uint, uint> pair in expected)
                if (!actual.TryGetValue(pair.Key, out uint value) || value != pair.Value) return false;
            return true;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }
    }
}
