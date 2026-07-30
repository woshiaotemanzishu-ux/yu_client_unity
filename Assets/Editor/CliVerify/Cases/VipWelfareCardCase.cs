using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
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
            bool oldHasWelfareCards = model.HasWelfareCardList;
            var oldWelfareCards = new List<VipModel.WelfareCard>(model.WelfareCards);
            bool oldHasVipInfo = model.HasVipInfo;
            VipModel.VipInfoSnapshot oldVipInfo = model.VipInfo;
            bool oldHasPrivilegeCards = model.HasPrivilegeCards;
            var oldPrivilegeCards = new List<VipModel.PrivilegeCard>(model.PrivilegeCards);
            bool oldHasActivation = model.HasActivationNotice;
            VipModel.CardNotice oldActivation = model.LastActivationNotice;
            bool oldHasTimeout = model.HasTimeoutNotice;
            VipModel.CardNotice oldTimeout = model.LastTimeoutNotice;
            bool oldHasRechargeSuccess = model.HasRechargeSuccessNotice;
            bool oldHasTotalRecharge = model.HasTotalRechargeGold;
            uint oldTotalRecharge = model.TotalRechargeGold;
            FieldInfo vipInterceptor = typeof(VipController).GetField("s_vipOutboundIntercept", SF);
            FieldInfo welfareInterceptor = typeof(VipController).GetField("s_welfareCardOutboundIntercept", SF);
            object oldVipInterceptor = vipInterceptor?.GetValue(null);
            object oldWelfareInterceptor = welfareInterceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var saved = new Dictionary<int, HandlerState>();
            int[] ownedProtocolIds = { 45000, 45001, 45002, 45003, 45004, 45005, 45006, 45007, 45008,
                15800, 15801, 15802, 15803, 15804, 15901, 15902 };
            foreach (int id in ownedProtocolIds) SaveHandler(handlers, saved, id);
            bool pass = false;
            bool restored = false;
            int rechargeSuccessEvents = 0;
            int rechargeTotalEvents = 0;
            uint lastRechargeTotalEvent = 0;
            Action rechargeSuccessListener = () => rechargeSuccessEvents++;
            Action<uint> rechargeTotalListener = value => { rechargeTotalEvents++; lastRechargeTotalEvent = value; };
            bool listenersAttached = false;

            try
            {
                if (controller.IsInitialized) controller.Dispose();
                foreach (int id in ownedProtocolIds) handlers?.Remove(id);
                controller.Init();
                model.Reset();

                MethodInfo on45000 = typeof(VipController).GetMethod("On45000", F);
                MethodInfo on45004 = typeof(VipController).GetMethod("On45004", F);
                MethodInfo on45005 = typeof(VipController).GetMethod("On45005", F);
                MethodInfo on45006 = typeof(VipController).GetMethod("On45006", F);
                MethodInfo on15800 = typeof(VipController).GetMethod("On15800", F);
                MethodInfo on15801 = typeof(VipController).GetMethod("On15801", F);
                MethodInfo on15802 = typeof(VipController).GetMethod("On15802", F);
                MethodInfo on15803 = typeof(VipController).GetMethod("On15803", F);
                MethodInfo on15901 = typeof(VipController).GetMethod("On15901", F);
                MethodInfo onDayChange = typeof(VipController).GetMethod("OnServerDayChange", F);
                pass = handlers != null && vipInterceptor != null && welfareInterceptor != null
                    && on45000 != null && on45004 != null && on45005 != null && on45006 != null
                    && on15800 != null && on15801 != null && on15802 != null && on15803 != null
                    && on15901 != null && onDayChange != null
                    && handlers.Contains(45000) && handlers.Contains(45004) && handlers.Contains(45005) && handlers.Contains(45006)
                    && !handlers.Contains(45001) && !handlers.Contains(45002) && !handlers.Contains(45003)
                    && !handlers.Contains(45007) && !handlers.Contains(45008)
                    && handlers.Contains(15800) && handlers.Contains(15801) && handlers.Contains(15802)
                    && handlers.Contains(15803) && !handlers.Contains(15804)
                    && handlers.Contains(15901) && !handlers.Contains(15902);

                int[] registeredIds = { 45000, 45004, 45005, 45006, 15800, 15801, 15802, 15803, 15901 };
                var registeredHandlers = new Dictionary<int, object>();
                foreach (int id in registeredIds) registeredHandlers[id] = handlers[id];
                controller.Init();
                foreach (int id in registeredIds)
                    pass &= registeredHandlers[id] != null && ReferenceEquals(registeredHandlers[id], handlers[id]);

                var vipFrames = new List<byte[]>();
                var welfareFrames = new List<byte[]>();
                vipInterceptor.SetValue(null, new Func<byte[], bool>(frame => { vipFrames.Add(frame); return true; }));
                welfareInterceptor.SetValue(null, new Func<byte[], bool>(frame => { welfareFrames.Add(frame); return true; }));

                model.ReplaceVipInfo(new VipModel.VipInfoSnapshot(1, 2, 3, 4,
                    new ushort[] { 5 }, new ushort[] { 6 }, new[] { new VipModel.UseCard(7, 8) }));
                model.ReplacePrivilegeCards(new[] { new VipModel.PrivilegeCard(1, 2, 3, 4, 5) });
                model.ReplaceActivationNotice(new VipModel.CardNotice(6, 7));
                model.ReplaceTimeoutNotice(new VipModel.CardNotice(8, 9));
                model.SetRechargeOneProduct(10, 11);
                model.ReplaceWelfareCards(new[] { new VipModel.WelfareCard(12, 13, 14, 15, 16) });
                model.MarkRechargeSuccessNotice();
                model.ReplaceTotalRechargeGold(17);
                controller.RequestStartup();
                pass &= StrictEmptyFrames(vipFrames, 45000, 45004, 15800)
                    && !model.HasVipInfo && !model.HasPrivilegeCards && !model.HasActivationNotice && !model.HasTimeoutNotice
                    && model.PrivilegeCards.Count == 0 && model.ProductById.Count == 0
                    && !model.HasWelfareCardList && model.WelfareCards.Count == 0
                    && !model.HasRechargeSuccessNotice && !model.HasTotalRechargeGold && model.TotalRechargeGold == 0;
                vipFrames.Clear();

                controller.RequestTotalRechargeGold();
                pass &= StrictEmptyFrames(vipFrames, 15803) && !model.HasTotalRechargeGold;
                vipFrames.Clear();

                controller.RequestWelfareCards();
                pass &= StrictEmptyFrames(welfareFrames, 15901) && !model.HasWelfareCardList;
                welfareFrames.Clear();

                var fullInfo = new CliVerify.Pkt().H(ushort.MaxValue).I(uint.MaxValue).I(0).C(byte.MaxValue)
                    .H(3).H(ushort.MaxValue).H(0).H(ushort.MaxValue)
                    .H(3).H(1).H(1).H(0)
                    .H(2).C(byte.MaxValue).I(uint.MaxValue).C(0).I(0).Bytes();
                Feed(on45000, controller, fullInfo);
                pass &= VipInfoMatches(model.VipInfo, ushort.MaxValue, uint.MaxValue, 0, byte.MaxValue,
                    new ushort[] { ushort.MaxValue, 0, ushort.MaxValue }, new ushort[] { 1, 1, 0 },
                    new[] { new VipModel.UseCard(byte.MaxValue, uint.MaxValue), new VipModel.UseCard(0, 0) })
                    && !model.HasPrivilegeCards && !model.HasActivationNotice && !model.HasTimeoutNotice;
                VipModel.VipInfoSnapshot firstInfo = model.VipInfo;
                controller.RequestVipInfo();
                pass &= StrictEmptyFrames(vipFrames, 45000) && ReferenceEquals(firstInfo, model.VipInfo);
                vipFrames.Clear();

                var privilegePayload = new CliVerify.Pkt().H(3)
                    .C(byte.MaxValue).C(0).C(byte.MaxValue).C(1).I(uint.MaxValue)
                    .C(0).C(byte.MaxValue).C(0).C(byte.MaxValue).I(0)
                    .C(byte.MaxValue).C(1).C(2).C(3).I(4).Bytes();
                Feed(on45004, controller, privilegePayload);
                pass &= PrivilegeCardsMatch(model.PrivilegeCards, new[]
                    {
                        new VipModel.PrivilegeCard(byte.MaxValue, 0, byte.MaxValue, 1, uint.MaxValue),
                        new VipModel.PrivilegeCard(0, byte.MaxValue, 0, byte.MaxValue, 0),
                        new VipModel.PrivilegeCard(byte.MaxValue, 1, 2, 3, 4)
                    }) && ReferenceEquals(firstInfo, model.VipInfo);
                IReadOnlyList<VipModel.PrivilegeCard> firstPrivilegeCards = model.PrivilegeCards;
                controller.RequestPrivilegeCards();
                pass &= StrictEmptyFrames(vipFrames, 45004) && ReferenceEquals(firstPrivilegeCards, model.PrivilegeCards);
                vipFrames.Clear();

                Feed(on45005, controller, new CliVerify.Pkt().C(byte.MaxValue).C(0).Bytes());
                pass &= NoticeMatches(model.LastActivationNotice, byte.MaxValue, 0)
                    && ReferenceEquals(firstInfo, model.VipInfo) && ReferenceEquals(firstPrivilegeCards, model.PrivilegeCards)
                    && !model.HasTimeoutNotice && vipFrames.Count == 0;
                VipModel.CardNotice firstActivation = model.LastActivationNotice;

                Feed(on45006, controller, new CliVerify.Pkt().C(0).C(byte.MaxValue).Bytes());
                pass &= NoticeMatches(model.LastTimeoutNotice, 0, byte.MaxValue)
                    && ReferenceEquals(firstActivation, model.LastActivationNotice)
                    && ReferenceEquals(firstInfo, model.VipInfo) && ReferenceEquals(firstPrivilegeCards, model.PrivilegeCards)
                    && StrictEmptyFrames(vipFrames, 45004);
                vipFrames.Clear();

                onDayChange.Invoke(controller, null);
                pass &= StrictEmptyFrames(vipFrames, 45000, 45004, 15800)
                    && ReferenceEquals(firstInfo, model.VipInfo) && ReferenceEquals(firstPrivilegeCards, model.PrivilegeCards)
                    && ReferenceEquals(firstActivation, model.LastActivationNotice) && NoticeMatches(model.LastTimeoutNotice, 0, byte.MaxValue);
                vipFrames.Clear();

                Feed(on45000, controller, new CliVerify.Pkt().H(0).I(0).I(uint.MaxValue).C(0).H(0).H(0).H(0).Bytes());
                pass &= VipInfoMatches(model.VipInfo, 0, 0, uint.MaxValue, 0,
                    Array.Empty<ushort>(), Array.Empty<ushort>(), Array.Empty<VipModel.UseCard>())
                    && !ReferenceEquals(firstInfo, model.VipInfo) && firstInfo.GotRewards.Count == 3;
                Feed(on45004, controller, new CliVerify.Pkt().H(0).Bytes());
                pass &= model.HasPrivilegeCards && model.PrivilegeCards.Count == 0
                    && !ReferenceEquals(firstPrivilegeCards, model.PrivilegeCards) && firstPrivilegeCards.Count == 3;
                Feed(on45005, controller, new CliVerify.Pkt().C(0).C(0).Bytes());
                pass &= NoticeMatches(model.LastActivationNotice, 0, 0) && !ReferenceEquals(firstActivation, model.LastActivationNotice);
                vipFrames.Clear();
                Feed(on45006, controller, new CliVerify.Pkt().C(byte.MaxValue).C(byte.MaxValue).Bytes());
                pass &= NoticeMatches(model.LastTimeoutNotice, byte.MaxValue, byte.MaxValue)
                    && StrictEmptyFrames(vipFrames, 45004) && model.HasPrivilegeCards && model.PrivilegeCards.Count == 0;
                vipFrames.Clear();

                var suppliedGot = new List<ushort> { 21 };
                var suppliedUse = new List<VipModel.UseCard> { new VipModel.UseCard(22, 23) };
                model.ReplaceVipInfo(new VipModel.VipInfoSnapshot(17, 18, 19, 20, suppliedGot, Array.Empty<ushort>(), suppliedUse));
                suppliedGot[0] = 0;
                suppliedUse[0] = new VipModel.UseCard(0, 0);
                bool vipInfoAddRejected = false;
                try { ((IList<ushort>)model.VipInfo.GotRewards).Add(1); }
                catch (NotSupportedException) { vipInfoAddRejected = true; }
                var suppliedPrivilege = new List<VipModel.PrivilegeCard> { new VipModel.PrivilegeCard(24, 25, 26, 27, 28) };
                model.ReplacePrivilegeCards(suppliedPrivilege);
                suppliedPrivilege[0] = new VipModel.PrivilegeCard(0, 0, 0, 0, 0);
                bool privilegeAddRejected = false;
                try { ((IList<VipModel.PrivilegeCard>)model.PrivilegeCards).Add(new VipModel.PrivilegeCard(1, 1, 1, 1, 1)); }
                catch (NotSupportedException) { privilegeAddRejected = true; }
                pass &= model.VipInfo.GotRewards[0] == 21 && model.VipInfo.UseCards[0].CardType == 22
                    && model.PrivilegeCards[0].CardType == 24 && model.PrivilegeCards[0].Time == 28
                    && vipInfoAddRejected && privilegeAddRejected;

                Feed(on15800, controller, new CliVerify.Pkt().H(1).I(9).C(2).Bytes());
                pass &= model.ProductById.Count == 1 && model.ProductById[9].ReturnType == 2 && model.HasVipInfo;
                EventDispatcher.On(GlobalEvent.EVT_RECHARGE_SUCCESS, rechargeSuccessListener);
                EventDispatcher.On(GlobalEvent.EVT_RECHARGE_TOTAL_UPDATED, rechargeTotalListener);
                listenersAttached = true;
                Feed(on15802, controller, Array.Empty<byte>());
                Feed(on15802, controller, Array.Empty<byte>());
                pass &= model.HasRechargeSuccessNotice && rechargeSuccessEvents == 2
                    && !model.HasTotalRechargeGold && rechargeTotalEvents == 0 && model.ProductById.Count == 1;
                Feed(on15803, controller, new CliVerify.Pkt().I(uint.MaxValue).Bytes());
                pass &= model.HasTotalRechargeGold && model.TotalRechargeGold == uint.MaxValue
                    && rechargeTotalEvents == 1 && lastRechargeTotalEvent == uint.MaxValue
                    && model.HasRechargeSuccessNotice && model.ProductById.Count == 1;
                controller.RequestTotalRechargeGold();
                pass &= StrictEmptyFrames(vipFrames, 15803) && model.TotalRechargeGold == uint.MaxValue
                    && rechargeTotalEvents == 1;
                vipFrames.Clear();
                Feed(on15803, controller, new CliVerify.Pkt().I(0).Bytes());
                pass &= model.HasTotalRechargeGold && model.TotalRechargeGold == 0
                    && rechargeTotalEvents == 2 && lastRechargeTotalEvent == 0 && model.HasRechargeSuccessNotice;
                var manyWelfare = new CliVerify.Pkt().H(3)
                    .I(uint.MaxValue).I(0).I(7).C(byte.MaxValue).H(ushort.MaxValue)
                    .I(0).I(1).I(uint.MaxValue).C(0).H(0)
                    .I(5).I(6).I(7).C(1).H(2).Bytes();
                Feed(on15901, controller, manyWelfare);
                pass &= model.HasWelfareCardList && WelfareCardsMatch(model.WelfareCards,
                    new[] { new VipModel.WelfareCard(uint.MaxValue, 0, 7, byte.MaxValue, ushort.MaxValue), new VipModel.WelfareCard(0, 1, uint.MaxValue, 0, 0), new VipModel.WelfareCard(5, 6, 7, 1, 2) })
                    && model.ProductById.Count == 1;
                IReadOnlyList<VipModel.WelfareCard> manyWelfareSnapshot = model.WelfareCards;
                controller.RequestWelfareCards();
                pass &= StrictEmptyFrames(welfareFrames, 15901) && ReferenceEquals(manyWelfareSnapshot, model.WelfareCards);
                welfareFrames.Clear();

                Feed(on15801, controller, new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue).Bytes());
                pass &= model.ProductById.Count == 2 && model.ProductById[unchecked((int)uint.MaxValue)].ReturnType == byte.MaxValue
                    && ReferenceEquals(manyWelfareSnapshot, model.WelfareCards);
                Feed(on15901, controller, new CliVerify.Pkt().H(0).Bytes());
                pass &= model.HasWelfareCardList && model.WelfareCards.Count == 0 && model.ProductById.Count == 2
                    && !ReferenceEquals(manyWelfareSnapshot, model.WelfareCards) && manyWelfareSnapshot.Count == 3;

                controller.Dispose();
                pass &= !controller.IsInitialized && model.ProductById.Count == 0
                    && !model.HasVipInfo && !model.HasPrivilegeCards && !model.HasActivationNotice && !model.HasTimeoutNotice
                    && !model.HasRechargeSuccessNotice && !model.HasTotalRechargeGold && model.TotalRechargeGold == 0
                    && !model.HasWelfareCardList && model.WelfareCards.Count == 0
                    && !handlers.Contains(45000) && !handlers.Contains(45001) && !handlers.Contains(45002)
                    && !handlers.Contains(45003) && !handlers.Contains(45004) && !handlers.Contains(45005)
                    && !handlers.Contains(45006) && !handlers.Contains(45007) && !handlers.Contains(45008)
                    && !handlers.Contains(15800) && !handlers.Contains(15801) && !handlers.Contains(15802)
                    && !handlers.Contains(15803) && !handlers.Contains(15804)
                    && !handlers.Contains(15901) && !handlers.Contains(15902);
                Debug.Log("CLIVERIFY vipWelfareCard VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                model.SetRechargeProductList(oldProducts);
                if (oldHasWelfareCards) model.ReplaceWelfareCards(oldWelfareCards);
                if (oldHasVipInfo) model.ReplaceVipInfo(oldVipInfo);
                if (oldHasPrivilegeCards) model.ReplacePrivilegeCards(oldPrivilegeCards);
                if (oldHasActivation) model.ReplaceActivationNotice(oldActivation);
                if (oldHasTimeout) model.ReplaceTimeoutNotice(oldTimeout);
                if (oldHasRechargeSuccess) model.MarkRechargeSuccessNotice();
                if (oldHasTotalRecharge) model.ReplaceTotalRechargeGold(oldTotalRecharge);
                if (wasInitialized) controller.Init();
                foreach (int id in ownedProtocolIds) RestoreHandler(handlers, saved[id], id);
                if (vipInterceptor != null) vipInterceptor.SetValue(null, oldVipInterceptor);
                if (welfareInterceptor != null) welfareInterceptor.SetValue(null, oldWelfareInterceptor);
                if (listenersAttached)
                {
                    EventDispatcher.Off(GlobalEvent.EVT_RECHARGE_SUCCESS, rechargeSuccessListener);
                    EventDispatcher.Off(GlobalEvent.EVT_RECHARGE_TOTAL_UPDATED, rechargeTotalListener);
                }
                restored = controller.IsInitialized == wasInitialized && ProductsMatch(model.ProductById, oldProducts)
                    && model.HasWelfareCardList == oldHasWelfareCards && WelfareCardsMatch(model.WelfareCards, oldWelfareCards)
                    && model.HasVipInfo == oldHasVipInfo && (!oldHasVipInfo || VipInfoEquals(model.VipInfo, oldVipInfo))
                    && model.HasPrivilegeCards == oldHasPrivilegeCards && PrivilegeCardsMatch(model.PrivilegeCards, oldPrivilegeCards)
                    && model.HasActivationNotice == oldHasActivation && (!oldHasActivation || NoticeEquals(model.LastActivationNotice, oldActivation))
                    && model.HasTimeoutNotice == oldHasTimeout && (!oldHasTimeout || NoticeEquals(model.LastTimeoutNotice, oldTimeout))
                    && model.HasRechargeSuccessNotice == oldHasRechargeSuccess
                    && model.HasTotalRechargeGold == oldHasTotalRecharge
                    && (!oldHasTotalRecharge || model.TotalRechargeGold == oldTotalRecharge)
                    && (vipInterceptor == null || ReferenceEquals(vipInterceptor.GetValue(null), oldVipInterceptor))
                    && (welfareInterceptor == null || ReferenceEquals(welfareInterceptor.GetValue(null), oldWelfareInterceptor));
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

        private static bool StrictEmptyFrames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6 || frame[0] != 0 || frame[1] != 6 || frame[2] != 3 || frame[3] != 232
                    || frame[4] != (byte)(ids[i] >> 8) || frame[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private static bool ProductsMatch(IReadOnlyDictionary<int, VipModel.RechargeProduct> actual, IReadOnlyList<VipModel.RechargeProduct> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (!actual.TryGetValue(expected[i].ProductId, out VipModel.RechargeProduct value) || value.ReturnType != expected[i].ReturnType) return false;
            return true;
        }

        private static bool WelfareCardsMatch(IReadOnlyList<VipModel.WelfareCard> actual, IReadOnlyList<VipModel.WelfareCard> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i].ProductType != expected[i].ProductType || actual[i].ProductSubtype != expected[i].ProductSubtype
                    || actual[i].ProductId != expected[i].ProductId || actual[i].State != expected[i].State || actual[i].LeftCount != expected[i].LeftCount) return false;
            return true;
        }

        private static bool PrivilegeCardsMatch(IReadOnlyList<VipModel.PrivilegeCard> actual, IReadOnlyList<VipModel.PrivilegeCard> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i].CardType != expected[i].CardType || actual[i].IsTempCard != expected[i].IsTempCard
                    || actual[i].IsActive != expected[i].IsActive || actual[i].IsForever != expected[i].IsForever
                    || actual[i].Time != expected[i].Time) return false;
            return true;
        }

        private static bool VipInfoMatches(VipModel.VipInfoSnapshot actual, ushort vipLevel, uint vipExp, uint needExp, byte vipHide,
            IReadOnlyList<ushort> gotRewards, IReadOnlyList<ushort> canRewards, IReadOnlyList<VipModel.UseCard> useCards)
        {
            return actual != null && actual.VipLevel == vipLevel && actual.VipExp == vipExp && actual.NeedExp == needExp
                && actual.VipHide == vipHide && U16ListEquals(actual.GotRewards, gotRewards)
                && U16ListEquals(actual.CanRewards, canRewards) && UseCardsMatch(actual.UseCards, useCards);
        }

        private static bool VipInfoEquals(VipModel.VipInfoSnapshot actual, VipModel.VipInfoSnapshot expected)
        {
            return expected != null && VipInfoMatches(actual, expected.VipLevel, expected.VipExp, expected.NeedExp, expected.VipHide,
                expected.GotRewards, expected.CanRewards, expected.UseCards);
        }

        private static bool U16ListEquals(IReadOnlyList<ushort> actual, IReadOnlyList<ushort> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static bool UseCardsMatch(IReadOnlyList<VipModel.UseCard> actual, IReadOnlyList<VipModel.UseCard> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i].CardType != expected[i].CardType || actual[i].Time != expected[i].Time) return false;
            return true;
        }

        private static bool NoticeMatches(VipModel.CardNotice actual, byte cardType, byte isTempCard)
        {
            return actual != null && actual.CardType == cardType && actual.IsTempCard == isTempCard;
        }

        private static bool NoticeEquals(VipModel.CardNotice actual, VipModel.CardNotice expected)
        {
            return expected != null && NoticeMatches(actual, expected.CardType, expected.IsTempCard);
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
