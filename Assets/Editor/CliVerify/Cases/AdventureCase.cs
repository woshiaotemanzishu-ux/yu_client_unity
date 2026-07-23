using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Adventure;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class AdventureCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY adventure EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            AdventureController controller = AdventureController.Instance;
            AdventureModel model = AdventureModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            int oldStage = model.Stage;
            long oldStartTime = model.StartTime;
            long oldEndTime = model.EndTime;
            bool oldHasBoardState = model.HasBoardState;
            ushort oldCircle = model.Circle;
            ushort oldLocation = model.Location;
            ushort oldLeftTimes = model.LeftTimes;
            ushort oldThrowTimes = model.ThrowTimes;
            ushort oldFreeResetTimes = model.FreeResetTimes;
            ushort oldFreeThrowTimes = model.FreeThrowTimes;
            bool oldHasShopSnapshot = model.HasShopSnapshot;
            uint oldShopTimes = model.ShopTimes;
            List<AdventureModel.ObjectEntry> oldRefreshCost = new List<AdventureModel.ObjectEntry>(model.RefreshCost);
            List<AdventureModel.ShopGoodsEntry> oldShopGoods = new List<AdventureModel.ShopGoodsEntry>(model.ShopGoods);
            FieldInfo outboundIntercept = typeof(AdventureController).GetField("s_boardStateOutboundIntercept", StaticNonPublic);
            Delegate oldIntercept = outboundIntercept == null ? null : outboundIntercept.GetValue(null) as Delegate;
            MethodInfo on42701 = typeof(AdventureController).GetMethod("On42701", InstanceNonPublic);
            MethodInfo on42704 = typeof(AdventureController).GetMethod("On42704", InstanceNonPublic);
            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", StaticNonPublic);
            IDictionary handlers = handlersField == null ? null : handlersField.GetValue(null) as IDictionary;
            List<byte[]> outbound = new List<byte[]>();
            bool pass = true;

            try
            {
                if (controller.IsInitialized)
                    controller.Dispose();
                model.Reset();
                controller.Init();

                pass &= outboundIntercept != null && on42701 != null && on42704 != null && handlers != null;
                pass &= handlers.Contains(Proto.ADVENTURE_INFO) && handlers.Contains(Proto.ADVENTURE_BOARD_STATE) && handlers.Contains(Proto.ADVENTURE_SHOP_SNAPSHOT);
                foreach (int proto in new[] { 42702, 42703, 42705, 42706 })
                    pass &= !handlers.Contains(proto);

                outboundIntercept.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    outbound.Add(frame);
                    return true;
                }));
                controller.RequestBoardState();
                pass &= outbound.Count == 1 && IsEmptyBoardRequest(outbound[0]) && !model.HasBoardState;
                outbound.Clear();

                controller.RequestShopSnapshot();
                pass &= outbound.Count == 1 && IsEmptyShopRequest(outbound[0]) && !model.HasShopSnapshot;
                outbound.Clear();

                pass &= ReceiveShop(on42704, controller, ShopPacket(0, new ObjectSpec[0], new ShopSpec[0]))
                    && IsShop(model, true, 0, 0, 0) && outbound.Count == 0;

                ShopSpec emptyReward = new ShopSpec(1, 2, new ObjectSpec[0], 3, 4, 5, 6);
                pass &= ReceiveShop(on42704, controller, ShopPacket(7, new ObjectSpec[0], new[] { emptyReward }))
                    && IsShop(model, true, 7, 0, 1) && model.ShopGoods[0].Reward.Count == 0 && outbound.Count == 0;

                ObjectSpec duplicate = new ObjectSpec(255, uint.MaxValue, uint.MaxValue);
                ObjectSpec secondCost = new ObjectSpec(0, 1, 2);
                ShopSpec firstWire = new ShopSpec(3, 0, new[] { duplicate, duplicate }, 4, 5, 6, 7);
                ShopSpec secondWire = new ShopSpec(ushort.MaxValue, byte.MaxValue, new[] { new ObjectSpec(8, 9, 10), new ObjectSpec(8, 9, 10) }, uint.MaxValue, uint.MaxValue, byte.MaxValue, byte.MaxValue);
                pass &= ReceiveShop(on42704, controller, ShopPacket(uint.MaxValue, new[] { duplicate, secondCost }, new[] { firstWire, secondWire }))
                    && IsShop(model, true, uint.MaxValue, 2, 2)
                    && IsObject(model.RefreshCost[0], duplicate) && IsObject(model.RefreshCost[1], secondCost)
                    && model.ShopGoods[0].Id == ushort.MaxValue && model.ShopGoods[1].Id == 3
                    && model.ShopGoods[0].Type == byte.MaxValue && model.ShopGoods[0].ShowPrice == uint.MaxValue
                    && model.ShopGoods[0].Price == uint.MaxValue && model.ShopGoods[0].Over == byte.MaxValue && model.ShopGoods[0].State == byte.MaxValue
                    && model.ShopGoods[0].Reward.Count == 2 && IsObject(model.ShopGoods[0].Reward[0], secondWire.Reward[0]) && IsObject(model.ShopGoods[0].Reward[1], secondWire.Reward[1])
                    && model.ShopGoods[1].Reward.Count == 2 && IsObject(model.ShopGoods[1].Reward[0], duplicate) && IsObject(model.ShopGoods[1].Reward[1], duplicate)
                    && outbound.Count == 0;

                controller.RequestShopSnapshot();
                pass &= outbound.Count == 1 && IsEmptyShopRequest(outbound[0]) && IsShop(model, true, uint.MaxValue, 2, 2);
                outbound.Clear();

                pass &= Receive(on42701, controller, 0, 0, 0, 0, 0, 0)
                    && IsBoard(model, true, 0, 0, 0, 0, 0, 0) && outbound.Count == 0;

                pass &= Receive(on42701, controller, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue)
                    && IsBoard(model, true, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue) && outbound.Count == 0;

                pass &= Receive(on42701, controller, 1, 2, 3, 4, 5, 6)
                    && IsBoard(model, true, 1, 2, 3, 4, 5, 6) && outbound.Count == 0;

                controller.RequestBoardState();
                pass &= outbound.Count == 1 && IsEmptyBoardRequest(outbound[0]);
                pass &= IsBoard(model, true, 1, 2, 3, 4, 5, 6);
                outbound.Clear();

                model.SetTimeInfo(7, 8, 9);
                pass &= model.Stage == 7 && model.StartTime == 8 && model.EndTime == 9;
                pass &= IsBoard(model, true, 1, 2, 3, 4, 5, 6);
                pass &= Receive(on42701, controller, 10, 11, 12, 13, 14, 15)
                    && model.Stage == 7 && model.StartTime == 8 && model.EndTime == 9
                    && IsBoard(model, true, 10, 11, 12, 13, 14, 15) && IsShop(model, true, uint.MaxValue, 2, 2) && outbound.Count == 0;

                ShopSpec single = new ShopSpec(12, 13, new[] { new ObjectSpec(14, 15, 16) }, 17, 18, 19, 20);
                pass &= ReceiveShop(on42704, controller, ShopPacket(21, new[] { new ObjectSpec(22, 23, 24) }, new[] { single }))
                    && model.Stage == 7 && model.StartTime == 8 && model.EndTime == 9
                    && IsBoard(model, true, 10, 11, 12, 13, 14, 15) && IsShop(model, true, 21, 1, 1)
                    && model.ShopGoods[0].Id == 12 && outbound.Count == 0;

                pass &= Receive(on42701, controller, 0, 0, 0, 0, 0, 0)
                    && IsBoard(model, true, 0, 0, 0, 0, 0, 0) && IsShop(model, true, 21, 1, 1) && outbound.Count == 0;

                pass &= ReceiveShop(on42704, controller, ShopPacket(0, new ObjectSpec[0], new ShopSpec[0]))
                    && IsShop(model, true, 0, 0, 0) && outbound.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized;
                pass &= !handlers.Contains(Proto.ADVENTURE_INFO) && !handlers.Contains(Proto.ADVENTURE_BOARD_STATE) && !handlers.Contains(Proto.ADVENTURE_SHOP_SNAPSHOT);
                pass &= model.Stage == 0 && model.StartTime == 0 && model.EndTime == 0;
                pass &= IsBoard(model, false, 0, 0, 0, 0, 0, 0);
                pass &= IsShop(model, false, 0, 0, 0);
            }
            finally
            {
                if (controller.IsInitialized)
                    controller.Dispose();
                model.Reset();
                model.SetTimeInfo(oldStage, oldStartTime, oldEndTime);
                if (oldHasBoardState)
                    model.ReplaceBoardState(oldCircle, oldLocation, oldLeftTimes, oldThrowTimes, oldFreeResetTimes, oldFreeThrowTimes);
                if (oldHasShopSnapshot)
                {
                    var oldWireGoods = new List<AdventureModel.ShopGoodsEntry>(oldShopGoods);
                    oldWireGoods.Reverse();
                    model.ReplaceShopSnapshot(oldShopTimes, new List<AdventureModel.ObjectEntry>(oldRefreshCost), oldWireGoods);
                }
                if (wasInitialized)
                    controller.Init();
                if (outboundIntercept != null)
                    outboundIntercept.SetValue(null, oldIntercept);
            }

            Debug.Log("CLIVERIFY adventure VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static bool Receive(MethodInfo handler, AdventureController controller, ushort circle, ushort location, ushort leftTimes, ushort throwTimes, ushort freeResetTimes, ushort freeThrowTimes)
        {
            byte[] payload = new CliVerify.Pkt()
                .H(circle).H(location).H(leftTimes).H(throwTimes).H(freeResetTimes).H(freeThrowTimes).Bytes();
            var reader = new NetReader(payload, 0, payload.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsEmptyBoardRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ADVENTURE_BOARD_STATE >> 8)
                && frame[5] == (byte)(Proto.ADVENTURE_BOARD_STATE & 0xFF);
        }

        private static bool ReceiveShop(MethodInfo handler, AdventureController controller, byte[] payload)
        {
            var reader = new NetReader(payload, 0, payload.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsEmptyShopRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ADVENTURE_SHOP_SNAPSHOT >> 8)
                && frame[5] == (byte)(Proto.ADVENTURE_SHOP_SNAPSHOT & 0xFF);
        }

        private static bool IsShop(AdventureModel model, bool hasSnapshot, uint times, int costCount, int goodsCount)
        {
            return model.HasShopSnapshot == hasSnapshot && model.ShopTimes == times
                && model.RefreshCost.Count == costCount && model.ShopGoods.Count == goodsCount;
        }

        private static bool IsObject(AdventureModel.ObjectEntry actual, ObjectSpec expected)
        {
            return actual.Style == expected.Style && actual.TypeId == expected.TypeId && actual.Count == expected.Count;
        }

        private static byte[] ShopPacket(uint times, ObjectSpec[] refreshCost, ShopSpec[] goods)
        {
            var packet = new CliVerify.Pkt().I(times);
            AppendObjects(packet, refreshCost);
            packet.H(goods.Length);
            foreach (ShopSpec good in goods)
            {
                packet.H(good.Id).C(good.Type);
                AppendObjects(packet, good.Reward);
                packet.I(good.ShowPrice).I(good.Price).C(good.Over).C(good.State);
            }
            return packet.Bytes();
        }

        private static void AppendObjects(CliVerify.Pkt packet, ObjectSpec[] objects)
        {
            packet.H(objects.Length);
            foreach (ObjectSpec entry in objects)
                packet.C(entry.Style).I(entry.TypeId).I(entry.Count);
        }

        private struct ObjectSpec
        {
            public readonly byte Style;
            public readonly uint TypeId;
            public readonly uint Count;
            public ObjectSpec(byte style, uint typeId, uint count) { Style = style; TypeId = typeId; Count = count; }
        }

        private struct ShopSpec
        {
            public readonly ushort Id;
            public readonly byte Type;
            public readonly ObjectSpec[] Reward;
            public readonly uint ShowPrice;
            public readonly uint Price;
            public readonly byte Over;
            public readonly byte State;
            public ShopSpec(ushort id, byte type, ObjectSpec[] reward, uint showPrice, uint price, byte over, byte state)
            { Id = id; Type = type; Reward = reward; ShowPrice = showPrice; Price = price; Over = over; State = state; }
        }

        private static bool IsBoard(AdventureModel model, bool hasBoardState, ushort circle, ushort location, ushort leftTimes, ushort throwTimes, ushort freeResetTimes, ushort freeThrowTimes)
        {
            return model.HasBoardState == hasBoardState
                && model.Circle == circle && model.Location == location && model.LeftTimes == leftTimes
                && model.ThrowTimes == throwTimes && model.FreeResetTimes == freeResetTimes && model.FreeThrowTimes == freeThrowTimes;
        }
    }
}
