using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Auction;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>拍卖154读侧七协议的wire边界、键控隔离、精确请求帧与生命周期专项。</summary>
    public static class AuctionCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds = { 15401, 15402, 15407, 15408, 15409, 15410, 15411 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY auction EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            AuctionController controller = AuctionController.Instance;
            AuctionModel model = AuctionModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            Dictionary<uint, AuctionModel.GoodsSnapshot> oldGoods = Copy(model.GoodsByAuctionType);
            Dictionary<AuctionModel.GoodsKey, AuctionModel.GoodsUpdate> oldUpdates = Copy(model.Updates);
            Dictionary<AuctionModel.ModuleKey, AuctionModel.EstimateSnapshot> oldEstimates = Copy(model.Estimates);
            Dictionary<AuctionModel.ModuleKey, AuctionModel.LifecycleSnapshot> oldLifecycles = Copy(model.Lifecycles);
            AuctionModel.PersonalRecordsSnapshot oldPersonal = model.PersonalRecords;
            AuctionModel.BonusSnapshot oldBonus = model.BonusRecords;
            AuctionModel.AllCloseSnapshot oldClose = model.AllClose;
            FieldInfo interceptor = typeof(AuctionController).GetField("s_outboundIntercept", SF);
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
                MethodInfo on15401 = typeof(AuctionController).GetMethod("On15401", F);
                MethodInfo on15402 = typeof(AuctionController).GetMethod("On15402", F);
                MethodInfo on15407 = typeof(AuctionController).GetMethod("On15407", F);
                MethodInfo on15408 = typeof(AuctionController).GetMethod("On15408", F);
                MethodInfo on15409 = typeof(AuctionController).GetMethod("On15409", F);
                MethodInfo on15410 = typeof(AuctionController).GetMethod("On15410", F);
                MethodInfo on15411 = typeof(AuctionController).GetMethod("On15411", F);

                bool a = handlers != null && interceptor != null && on15401 != null && on15402 != null
                    && on15407 != null && on15408 != null && on15409 != null && on15410 != null && on15411 != null;
                foreach (int id in RegisteredIds) a &= handlers != null && handlers.Contains(id);
                foreach (int id in new[] { 15400, 15403, 15404, 15405, 15406 })
                    a &= handlers != null && !handlers.Contains(id);

                bool b = false;
                bool c = false;
                bool d = false;
                bool e = false;
                bool f = false;
                bool g = false;
                bool h = false;
                var frames = new List<byte[]>();
                if (a)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    SeedAll(model);
                    controller.RequestStartup();
                    b = frames.Count == 1
                        && BytesEqual(frames[0], new CliVerify.Pkt().H(18).H(1000).H(15401).I(2).I(0).I(0).Bytes())
                        && IsEmpty(model);
                    frames.Clear();

                    byte[] goodsPacket = new CliVerify.Pkt().I(uint.MaxValue).H(2)
                        .L(-1).I(uint.MaxValue).I(4000000000L).H(ushort.MaxValue)
                        .I(0).I(uint.MaxValue).I(1).I(2).I(3).L(-2).C(byte.MaxValue).C(0)
                        .L(-1).I(7).I(8).H(9).I(10).I(11).I(12).I(13).I(14).L(15).C(1).C(2)
                        .Bytes();
                    AuctionModel.GoodsSnapshot goods = null;
                    c = Invoke(on15401, controller, goodsPacket)
                        && model.TryGetGoods(uint.MaxValue, out goods) && goods.GoodsList.Count == 2
                        && goods.GoodsList[0].GoodsId == ulong.MaxValue
                        && goods.GoodsList[0].ModuleId == uint.MaxValue
                        && goods.GoodsList[0].TypeId == 4000000000U
                        && goods.GoodsList[0].WorldLevel == ushort.MaxValue
                        && goods.GoodsList[0].NextPrice == uint.MaxValue
                        && goods.GoodsList[0].TopPlayerId == unchecked((ulong)-2L)
                        && goods.GoodsList[0].IsDelay == byte.MaxValue && goods.GoodsList[0].HadBonus == 0
                        && goods.GoodsList[1].GoodsId == ulong.MaxValue && goods.GoodsList[1].HadBonus == 2;
                    AuctionModel.GoodsSnapshot firstGoodsRef = goods;
                    c &= Invoke(on15401, controller, new CliVerify.Pkt().I(2).H(0).Bytes())
                        && model.GoodsByAuctionType.Count == 2 && model.TryGetGoods(2, out AuctionModel.GoodsSnapshot emptyGoods)
                        && emptyGoods.GoodsList.Count == 0 && ReferenceEquals(model.GoodsByAuctionType[uint.MaxValue], firstGoodsRef);
                    c &= Invoke(on15401, controller, new CliVerify.Pkt().I(uint.MaxValue).H(0).Bytes())
                        && model.GoodsByAuctionType.Count == 2 && model.GoodsByAuctionType[uint.MaxValue].GoodsList.Count == 0
                        && firstGoodsRef.GoodsList.Count == 2 && firstGoodsRef.GoodsList[0].TypeId == 4000000000U;
                    AuctionModel.GoodsSnapshot currentGoodsRef = model.GoodsByAuctionType[uint.MaxValue];

                    AuctionModel.GoodsUpdate update = null;
                    d = Invoke(on15402, controller, new CliVerify.Pkt().L(-1).I(uint.MaxValue).I(2)
                            .I(3).I(4).I(5).L(-2).C(byte.MaxValue).C(0).Bytes())
                        && model.TryGetUpdate(2, ulong.MaxValue, out update)
                        && update.ModuleId == uint.MaxValue && update.CurrentPrice == 3 && update.TopPlayerId == unchecked((ulong)-2L)
                        && update.IsDelay == byte.MaxValue && update.GoodsStatus == 0
                        && ReferenceEquals(model.GoodsByAuctionType[uint.MaxValue], currentGoodsRef);
                    AuctionModel.GoodsUpdate firstUpdateRef = update;
                    d &= Invoke(on15402, controller, new CliVerify.Pkt().L(-1).I(6).I(3)
                            .I(7).I(8).I(9).L(10).C(0).C(byte.MaxValue).Bytes())
                        && model.Updates.Count == 2 && model.TryGetUpdate(3, ulong.MaxValue, out AuctionModel.GoodsUpdate otherUpdate)
                        && otherUpdate.GoodsStatus == byte.MaxValue && ReferenceEquals(model.Updates[new AuctionModel.GoodsKey(2, ulong.MaxValue)], firstUpdateRef);
                    AuctionModel.GoodsUpdate replacedUpdate = null;
                    d &= Invoke(on15402, controller, new CliVerify.Pkt().L(-1).I(0).I(2)
                            .I(0).I(0).I(0).L(0).C(0).C(0).Bytes())
                        && model.Updates.Count == 2 && model.TryGetUpdate(2, ulong.MaxValue, out replacedUpdate)
                        && !ReferenceEquals(replacedUpdate, firstUpdateRef) && replacedUpdate.ModuleId == 0
                        && firstUpdateRef.ModuleId == uint.MaxValue;
                    AuctionModel.GoodsUpdate updateRef = replacedUpdate;

                    AuctionModel.EstimateSnapshot estimate = null;
                    e = Invoke(on15407, controller, new CliVerify.Pkt().I(uint.MaxValue).I(0).I(uint.MaxValue).I(0).Bytes())
                        && model.TryGetEstimate(uint.MaxValue, 0, out estimate)
                        && estimate.EstimatedGold == uint.MaxValue && estimate.EstimatedBoundGold == 0;
                    AuctionModel.EstimateSnapshot estimateRef = estimate;
                    e &= Invoke(on15407, controller, new CliVerify.Pkt().I(uint.MaxValue).I(1).I(2).I(3).Bytes())
                        && model.Estimates.Count == 2 && ReferenceEquals(model.Estimates[new AuctionModel.ModuleKey(uint.MaxValue, 0)], estimateRef);
                    AuctionModel.LifecycleSnapshot lifecycle = null;
                    e &= Invoke(on15408, controller, new CliVerify.Pkt().I(uint.MaxValue).I(0).C(byte.MaxValue).Bytes())
                        && model.TryGetLifecycle(uint.MaxValue, 0, out lifecycle)
                        && lifecycle.Type == byte.MaxValue && ReferenceEquals(model.Estimates[new AuctionModel.ModuleKey(uint.MaxValue, 0)], estimateRef);
                    AuctionModel.LifecycleSnapshot lifecycleRef = lifecycle;
                    e &= Invoke(on15408, controller, new CliVerify.Pkt().I(2).I(0).C(0).Bytes())
                        && model.Lifecycles.Count == 2 && ReferenceEquals(model.Lifecycles[new AuctionModel.ModuleKey(uint.MaxValue, 0)], lifecycleRef)
                        && ReferenceEquals(model.Updates[new AuctionModel.GoodsKey(2, ulong.MaxValue)], updateRef);

                    f = Invoke(on15409, controller, new CliVerify.Pkt().H(2)
                            .C(0).I(uint.MaxValue).C(byte.MaxValue).H(0).H(ushort.MaxValue).I(4000000000L).H(ushort.MaxValue).I(uint.MaxValue)
                            .C(0).I(uint.MaxValue).C(byte.MaxValue).H(0).H(ushort.MaxValue).I(4000000000L).H(ushort.MaxValue).I(uint.MaxValue)
                            .Bytes())
                        && model.HasPersonalRecords && model.PersonalRecords.Records.Count == 2
                        && model.PersonalRecords.Records[0].ModuleId == uint.MaxValue
                        && model.PersonalRecords.Records[0].PriceType == byte.MaxValue
                        && model.PersonalRecords.Records[0].BoundGold == ushort.MaxValue
                        && model.PersonalRecords.Records[0].TypeId == 4000000000U
                        && model.PersonalRecords.Records[1].ModuleId == uint.MaxValue;
                    AuctionModel.PersonalRecordsSnapshot personalRef = model.PersonalRecords;
                    f &= Invoke(on15409, controller, new CliVerify.Pkt().H(0).Bytes())
                        && model.HasPersonalRecords && model.PersonalRecords.Records.Count == 0
                        && personalRef.Records.Count == 2 && ReferenceEquals(model.GoodsByAuctionType[uint.MaxValue], currentGoodsRef);
                    AuctionModel.PersonalRecordsSnapshot emptyPersonalRef = model.PersonalRecords;

                    g = Invoke(on15410, controller, new CliVerify.Pkt().H(2)
                            .I(uint.MaxValue).H(ushort.MaxValue).H(0).I(4000000000L)
                            .I(uint.MaxValue).H(ushort.MaxValue).H(0).I(4000000000L)
                            .H(2).I(0).H(0).H(ushort.MaxValue).I(0).H(0).H(ushort.MaxValue).Bytes())
                        && model.HasBonusRecords && model.BonusRecords.Records.Count == 2 && model.BonusRecords.Infos.Count == 2
                        && model.BonusRecords.Records[0].ModuleId == uint.MaxValue
                        && model.BonusRecords.Records[0].Gold == ushort.MaxValue
                        && model.BonusRecords.Records[0].Time == 4000000000U
                        && model.BonusRecords.Infos[0].BoundGoldReceived == ushort.MaxValue;
                    AuctionModel.BonusSnapshot bonusRef = model.BonusRecords;
                    g &= Invoke(on15411, controller, new CliVerify.Pkt().C(0).Bytes())
                        && model.HasAllClose && model.AllClose.RawValue == 0 && ReferenceEquals(model.BonusRecords, bonusRef)
                        && ReferenceEquals(model.PersonalRecords, emptyPersonalRef);
                    AuctionModel.AllCloseSnapshot closeZeroRef = model.AllClose;
                    g &= Invoke(on15411, controller, new CliVerify.Pkt().C(byte.MaxValue).Bytes())
                        && model.AllClose.RawValue == byte.MaxValue && !ReferenceEquals(model.AllClose, closeZeroRef)
                        && ReferenceEquals(model.BonusRecords, bonusRef) && ReferenceEquals(model.GoodsByAuctionType[uint.MaxValue], currentGoodsRef);
                    AuctionModel.AllCloseSnapshot closeRef = model.AllClose;
                    g &= Invoke(on15410, controller, new CliVerify.Pkt().H(0).H(0).Bytes())
                        && model.HasBonusRecords && model.BonusRecords.Records.Count == 0 && model.BonusRecords.Infos.Count == 0
                        && bonusRef.Records.Count == 2 && ReferenceEquals(model.AllClose, closeRef);
                    AuctionModel.BonusSnapshot emptyBonusRef = model.BonusRecords;

                    controller.RequestGoods(uint.MaxValue, 4000000000U, 0);
                    controller.RequestEstimate(uint.MaxValue, 4000000000U);
                    controller.RequestPersonalRecords();
                    controller.RequestBonusRecords();
                    h = ExplicitFramesAre(frames)
                        && ReferenceEquals(model.GoodsByAuctionType[uint.MaxValue], currentGoodsRef)
                        && ReferenceEquals(model.Updates[new AuctionModel.GoodsKey(2, ulong.MaxValue)], updateRef)
                        && ReferenceEquals(model.Estimates[new AuctionModel.ModuleKey(uint.MaxValue, 0)], estimateRef)
                        && ReferenceEquals(model.Lifecycles[new AuctionModel.ModuleKey(uint.MaxValue, 0)], lifecycleRef)
                        && ReferenceEquals(model.PersonalRecords, emptyPersonalRef)
                        && ReferenceEquals(model.BonusRecords, emptyBonusRef) && ReferenceEquals(model.AllClose, closeRef);

                    controller.Dispose();
                    h &= !controller.IsInitialized && IsEmpty(model);
                    foreach (int id in RegisteredIds) h &= !handlers.Contains(id);
                }

                pass = a && b && c && d && e && f && g && h;
                Debug.Log($"CLIVERIFY auction VERDICT A={a} B={b} C={c} D={d} E={e} F={f} G={g} H={h} pass={pass}");
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                RestoreDictionary(model, "_goodsByAuctionType", oldGoods);
                RestoreDictionary(model, "_updates", oldUpdates);
                RestoreDictionary(model, "_estimates", oldEstimates);
                RestoreDictionary(model, "_lifecycles", oldLifecycles);
                RestoreProperty(model, "PersonalRecords", oldPersonal);
                RestoreProperty(model, "BonusRecords", oldBonus);
                RestoreProperty(model, "AllClose", oldClose);
                if (wasInitialized) controller.Init();
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && DictionaryMatches(model.GoodsByAuctionType, oldGoods)
                    && DictionaryMatches(model.Updates, oldUpdates)
                    && DictionaryMatches(model.Estimates, oldEstimates)
                    && DictionaryMatches(model.Lifecycles, oldLifecycles)
                    && ReferenceEquals(model.PersonalRecords, oldPersonal)
                    && ReferenceEquals(model.BonusRecords, oldBonus)
                    && ReferenceEquals(model.AllClose, oldClose)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY auction restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static void SeedAll(AuctionModel model)
        {
            model.ReplaceGoods(1, Array.Empty<AuctionModel.Goods>());
            model.ReplaceUpdate(new AuctionModel.GoodsUpdate(1, 1, 1, 1, 1, 1, 1, 1, 1));
            model.ReplaceEstimate(new AuctionModel.EstimateSnapshot(1, 1, 1, 1));
            model.ReplaceLifecycle(new AuctionModel.LifecycleSnapshot(1, 1, 1));
            model.ReplacePersonalRecords(Array.Empty<AuctionModel.PersonalRecord>());
            model.ReplaceBonusRecords(Array.Empty<AuctionModel.BonusRecord>(), Array.Empty<AuctionModel.BonusInfo>());
            model.ReplaceAllClose(1);
        }

        private static bool IsEmpty(AuctionModel model) => model.GoodsByAuctionType.Count == 0
            && model.Updates.Count == 0 && model.Estimates.Count == 0 && model.Lifecycles.Count == 0
            && !model.HasPersonalRecords && !model.HasBonusRecords && !model.HasAllClose;

        private static bool Invoke(MethodInfo handler, AuctionController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExplicitFramesAre(IReadOnlyList<byte[]> frames)
        {
            return frames != null && frames.Count == 4
                && BytesEqual(frames[0], new CliVerify.Pkt().H(18).H(1000).H(15401)
                    .I(uint.MaxValue).I(4000000000L).I(0).Bytes())
                && BytesEqual(frames[1], new CliVerify.Pkt().H(14).H(1000).H(15407)
                    .I(uint.MaxValue).I(4000000000L).Bytes())
                && BytesEqual(frames[2], new CliVerify.Pkt().H(6).H(1000).H(15409).Bytes())
                && BytesEqual(frames[3], new CliVerify.Pkt().H(6).H(1000).H(15410).Bytes());
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static Dictionary<TKey, TValue> Copy<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source)
        {
            var copy = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in source) copy[pair.Key] = pair.Value;
            return copy;
        }

        private static void RestoreProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);

        private static void RestoreDictionary<TKey, TValue>(object target, string name, IDictionary<TKey, TValue> values)
        {
            var dictionary = target.GetType().GetField(name, F)?.GetValue(target) as IDictionary<TKey, TValue>;
            if (dictionary == null) return;
            dictionary.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in values) dictionary[pair.Key] = pair.Value;
        }

        private static bool DictionaryMatches<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> actual,
            IReadOnlyDictionary<TKey, TValue> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (KeyValuePair<TKey, TValue> pair in expected)
                if (!actual.TryGetValue(pair.Key, out TValue value) || !ReferenceEquals(value, pair.Value)) return false;
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

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id) =>
            handlers != null && handlers.Contains(id) == saved.Exists
            && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
    }
}
