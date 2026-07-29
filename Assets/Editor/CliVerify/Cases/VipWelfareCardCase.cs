using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Vip;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class VipWelfareCardCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState { public bool Exists; public object Value; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY vipWelfareCard EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            VipController controller = VipController.Instance;
            VipModel model = VipModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var oldProducts = new List<VipModel.RechargeProduct>(model.ProductById.Values);
            bool oldHasCards = model.HasWelfareCardList;
            var oldCards = new List<VipModel.WelfareCard>(model.WelfareCards);
            FieldInfo interceptor = typeof(VipController).GetField("s_welfareCardOutboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var saved = new Dictionary<int, HandlerState>();
            int[] ownedProtocolIds = { 15800, 15801, 15901, 15902 };
            foreach (int id in ownedProtocolIds) SaveHandler(handlers, saved, id);
            bool pass = false;
            bool restored = false;

            try
            {
                if (controller.IsInitialized) controller.Dispose();
                foreach (int id in ownedProtocolIds) handlers?.Remove(id);
                controller.Init();
                model.Reset();
                MethodInfo on15800 = typeof(VipController).GetMethod("On15800", F);
                MethodInfo on15801 = typeof(VipController).GetMethod("On15801", F);
                MethodInfo on15901 = typeof(VipController).GetMethod("On15901", F);
                pass = handlers != null && interceptor != null && on15800 != null && on15801 != null && on15901 != null
                    && handlers.Contains(15800) && handlers.Contains(15801) && handlers.Contains(15901) && !handlers.Contains(15902);

                object h15800 = handlers != null && handlers.Contains(15800) ? handlers[15800] : null;
                object h15801 = handlers != null && handlers.Contains(15801) ? handlers[15801] : null;
                object h15901 = handlers != null && handlers.Contains(15901) ? handlers[15901] : null;
                controller.Init();
                pass &= h15800 != null && h15801 != null && h15901 != null
                    && ReferenceEquals(h15800, handlers[15800]) && ReferenceEquals(h15801, handlers[15801]) && ReferenceEquals(h15901, handlers[15901]);

                var frames = new List<byte[]>();
                interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestWelfareCards();
                pass &= StrictEmptyFrame(frames, Proto.WELFARE_CARD_LIST);
                frames.Clear();
                pass &= !model.HasWelfareCardList && model.WelfareCards.Count == 0;

                Feed(on15800, controller, new CliVerify.Pkt().H(1).I(9).C(2).Bytes());
                pass &= model.ProductById.Count == 1 && model.ProductById[9].ReturnType == 2 && !model.HasWelfareCardList;

                var many = new CliVerify.Pkt().H(3)
                    .I(uint.MaxValue).I(0).I(7).C(byte.MaxValue).H(ushort.MaxValue)
                    .I(0).I(1).I(uint.MaxValue).C(0).H(0)
                    .I(5).I(6).I(7).C(1).H(2).Bytes();
                Feed(on15901, controller, many);
                pass &= model.HasWelfareCardList && CardsMatch(model.WelfareCards,
                    new[] { new VipModel.WelfareCard(uint.MaxValue, 0, 7, byte.MaxValue, ushort.MaxValue), new VipModel.WelfareCard(0, 1, uint.MaxValue, 0, 0), new VipModel.WelfareCard(5, 6, 7, 1, 2) })
                    && model.ProductById.Count == 1 && frames.Count == 0;
                IReadOnlyList<VipModel.WelfareCard> manySnapshot = model.WelfareCards;
                controller.RequestWelfareCards();
                pass &= StrictEmptyFrame(frames, Proto.WELFARE_CARD_LIST) && model.HasWelfareCardList && model.WelfareCards.Count == 3;
                frames.Clear();

                Feed(on15801, controller, new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue).Bytes());
                pass &= model.ProductById.Count == 2 && model.ProductById[unchecked((int)uint.MaxValue)].ReturnType == byte.MaxValue
                    && model.WelfareCards.Count == 3;

                Feed(on15901, controller, new CliVerify.Pkt().H(1).I(3).I(4).I(5).C(6).H(7).Bytes());
                pass &= CardsMatch(model.WelfareCards, new[] { new VipModel.WelfareCard(3, 4, 5, 6, 7) }) && model.ProductById.Count == 2
                    && !ReferenceEquals(model.WelfareCards, manySnapshot) && manySnapshot.Count == 3
                    && manySnapshot[0].ProductType == uint.MaxValue && manySnapshot[2].ProductId == 7;
                IReadOnlyList<VipModel.WelfareCard> singleSnapshot = model.WelfareCards;
                Feed(on15901, controller, new CliVerify.Pkt().H(0).Bytes());
                pass &= model.HasWelfareCardList && model.WelfareCards.Count == 0 && model.ProductById.Count == 2
                    && !ReferenceEquals(model.WelfareCards, singleSnapshot) && singleSnapshot.Count == 1 && singleSnapshot[0].ProductType == 3;

                var supplied = new List<VipModel.WelfareCard> { new VipModel.WelfareCard(8, 9, 10, 11, 12) };
                model.ReplaceWelfareCards(supplied);
                supplied[0] = new VipModel.WelfareCard(0, 0, 0, 0, 0);
                pass &= model.WelfareCards[0].ProductType == 8 && model.WelfareCards[0].LeftCount == 12;
                bool addRejected = false;
                try { ((IList<VipModel.WelfareCard>)model.WelfareCards).Add(new VipModel.WelfareCard(1, 1, 1, 1, 1)); }
                catch (NotSupportedException) { addRejected = true; }
                pass &= addRejected;

                controller.Dispose();
                pass &= !controller.IsInitialized && model.ProductById.Count == 0 && !model.HasWelfareCardList && model.WelfareCards.Count == 0
                    && !handlers.Contains(15800) && !handlers.Contains(15801) && !handlers.Contains(15901) && !handlers.Contains(15902);
                Debug.Log("CLIVERIFY vipWelfareCard VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                model.SetRechargeProductList(oldProducts);
                if (oldHasCards) model.ReplaceWelfareCards(oldCards);
                if (wasInitialized) controller.Init();
                foreach (int id in ownedProtocolIds) RestoreHandler(handlers, saved[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);
                restored = controller.IsInitialized == wasInitialized && ProductsMatch(model.ProductById, oldProducts)
                    && model.HasWelfareCardList == oldHasCards && CardsMatch(model.WelfareCards, oldCards)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in ownedProtocolIds) restored &= HandlerMatches(handlers, saved[id], id);
                Debug.Log("CLIVERIFY vipWelfareCard restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static void Feed(MethodInfo handler, VipController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidOperationException("Unread payload bytes: " + reader.Remaining);
        }

        private static bool StrictEmptyFrame(IReadOnlyList<byte[]> frames, int id)
        {
            return frames.Count == 1 && frames[0] != null && frames[0].Length == 6
                && frames[0][0] == 0 && frames[0][1] == 6 && frames[0][2] == 3 && frames[0][3] == 232
                && frames[0][4] == (byte)(id >> 8) && frames[0][5] == (byte)id;
        }

        private static bool ProductsMatch(IReadOnlyDictionary<int, VipModel.RechargeProduct> actual, IReadOnlyList<VipModel.RechargeProduct> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (!actual.TryGetValue(expected[i].ProductId, out VipModel.RechargeProduct value) || value.ReturnType != expected[i].ReturnType) return false;
            return true;
        }

        private static bool CardsMatch(IReadOnlyList<VipModel.WelfareCard> actual, IReadOnlyList<VipModel.WelfareCard> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i].ProductType != expected[i].ProductType || actual[i].ProductSubtype != expected[i].ProductSubtype
                    || actual[i].ProductId != expected[i].ProductId || actual[i].State != expected[i].State || actual[i].LeftCount != expected[i].LeftCount) return false;
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
            return handlers != null && handlers.Contains(id) == saved.Exists && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
        }
    }
}
