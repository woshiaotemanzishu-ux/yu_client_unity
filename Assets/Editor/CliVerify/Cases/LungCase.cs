using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Lung;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>18100 神纹基础快照，以及 18105/18112 熔炉启动与服务器时间刷新闭环回归。</summary>
    public static class LungCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY lung EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            LungController ctrl = LungController.Instance;
            bool wasInitialized = ctrl.IsInitialized;
            try { ctrl.Init(); return RunInitialized(ctrl); }
            finally { if (!wasInitialized && ctrl.IsInitialized) ctrl.Dispose(); }
        }

        private static int RunInitialized(LungController ctrl)
        {
            LungModel model = LungModel.Instance;
            var savedAttributes = new List<LungModel.AttributeEntry>(model.Attributes);
            var savedPositions = new List<LungModel.PositionEntry>(model.Positions);
            bool savedHasLungData = model.HasLungData;
            uint savedCombatPower = model.CombatPower;
            FieldInfo[] state = typeof(LungModel).GetFields(BindingFlags.Public | BindingFlags.Instance);
            object[] saved = new object[state.Length];
            for (int i = 0; i < state.Length; i++) saved[i] = state[i].GetValue(model);
            try { model.Reset(); return RunIsolated(ctrl, model); }
            finally
            {
                model.Reset();
                for (int i = 0; i < state.Length; i++) state[i].SetValue(model, saved[i]);
                if (savedHasLungData) model.ReplaceLungData(savedAttributes, savedPositions, savedCombatPower);
            }
        }

        private static int RunIsolated(LungController ctrl, LungModel model)
        {
            MethodInfo on18100 = ctrl.GetType().GetMethod("On18100", F);
            MethodInfo on18105 = ctrl.GetType().GetMethod("On18105", F);
            MethodInfo on18112 = ctrl.GetType().GetMethod("On18112", F);
            MethodInfo on18113 = ctrl.GetType().GetMethod("On18113", F);
            MethodInfo on15010 = typeof(BagController).GetMethod("On15010", F);
            MethodInfo on15017 = typeof(BagController).GetMethod("On15017", F);
            MethodInfo on15018 = typeof(BagController).GetMethod("On15018", F);
            FieldInfo intercept = ctrl.GetType().GetField("s_outboundIntercept", SF);
            bool pass = on18100 != null && on18105 != null && on18112 != null && on18113 != null
                && on15010 != null && on15017 != null && on15018 != null && intercept != null;
            void Check(string tag, bool ok) { Debug.Log("CLIVERIFY lung " + tag + " ok=" + ok); if (!ok) pass = false; }
            Check("handlers", pass);
            if (!pass) return 3;

            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
            var handlers = handlersField?.GetValue(null) as IDictionary;
            Check("registered 18100/18105/18112/18113", handlers != null && handlers.Contains(Proto.LUNG_INFO)
                && handlers.Contains(Proto.LUNG_STOVE_INFO) && handlers.Contains(Proto.LUNG_STOVE_OPEN_STATE)
                && handlers.Contains(Proto.LUNG_GOODS_DETAIL));

            object oldIntercept = intercept.GetValue(null);
            var trace = new List<byte[]>();
            BagModel bag = BagModel.Instance;
            int bagEvents = 0;
            int equipEvents = 0;
            Action onBag = () => bagEvents++;
            Action onEquip = () => equipEvents++;
            try
            {
                intercept.SetValue(null, new Func<byte[], bool>(frame => { trace.Add(frame); return true; }));
                bag.Clear();
                EventDispatcher.On(GlobalEvent.EVT_LUNG_BAG_UPDATE, onBag);
                EventDispatcher.On(GlobalEvent.EVT_LUNG_EQUIP_UPDATE, onEquip);
                ctrl.RequestStartup();
                Check("startup exact three empty frames", Frames(trace, Proto.LUNG_INFO, Proto.LUNG_STOVE_INFO, Proto.LUNG_STOVE_OPEN_STATE));

                const long bagFirstId = 0x100000023L;
                const long equipFirstId = 0x100000022L;
                NetReader bagFull = Feed(on15010, BagController.Instance,
                    FullGoodsPacket(BagModel.POS_LUNG_BAG, 1, 48, bagFirstId, 810035, 3, 7, 0xA1350035));
                NetReader equipFull = Feed(on15010, BagController.Instance,
                    FullGoodsPacket(BagModel.POS_LUNG_EQUIP, 1, 12, equipFirstId, 810034, 1, 1, 0xA1340034));
                Check("15010 lung full containers", TailOk(bagFull, 0xA1350035) && TailOk(equipFull, 0xA1340034)
                    && bag.HasLungBagData && bag.HasLungEquipData && bag.GetMaxCell(BagModel.POS_LUNG_BAG) == 48
                    && bag.GetMaxCell(BagModel.POS_LUNG_EQUIP) == 12
                    && bag.FindContainerGoods(BagModel.POS_LUNG_BAG, bagFirstId)?.NextPower == 0
                    && bag.FindContainerGoods(BagModel.POS_LUNG_EQUIP, equipFirstId)?.Cell == 1
                    && bagEvents == 1 && equipEvents == 1);

                trace.Clear();
                const long bagSecondId = 0x123456789ABCDEFL;
                NetReader delta = Feed(on15017, BagController.Instance,
                    DeltaGoodsPacket(BagModel.POS_LUNG_BAG, bagSecondId, 820035, 4, 2, 0xB1350035));
                Check("15017 requests exact 18113 and defers mutation", TailOk(delta, 0xB1350035)
                    && trace.Count == 1 && DetailFrame(trace[0], bagSecondId, BagModel.POS_LUNG_BAG)
                    && bag.FindContainerGoods(BagModel.POS_LUNG_BAG, bagSecondId) == null && bagEvents == 1);

                const long nextPower = 7000000000L;
                NetReader detail = Feed(on18113, ctrl,
                    DragonDetailPacket(BagModel.POS_LUNG_BAG, bagSecondId, 820035, 4, 2, nextPower, 0xC1350035));
                BagGoods dragon = bag.FindContainerGoods(BagModel.POS_LUNG_BAG, bagSecondId);
                Check("18113 dragon schema/upsert/read-to-tail", TailOk(detail, 0xC1350035) && dragon != null
                    && dragon.NextPower == (ulong)nextPower && dragon.GoodsNum == 2 && dragon.Cell == 4
                    && dragon.AwakeList != null && dragon.AwakeList.Count == 1
                    && dragon.AwakeList[0].AttrType == 1035 && dragon.AwakeList[0].AwakeLv == 2035
                    && dragon.AwakeList[0].AwakeExp == 0 && bagEvents == 2);

                // 空详情是一次已接收的增量通知，但不得清除旧容器。
                NetReader emptyDetail = Feed(on18113, ctrl,
                    new CliVerify.Pkt().H(BagModel.POS_LUNG_BAG).H(0).I(0xC2350035).Bytes());
                Check("18113 empty preserves prior", TailOk(emptyDetail, 0xC2350035)
                    && bag.FindContainerGoods(BagModel.POS_LUNG_BAG, bagSecondId)?.NextPower == (ulong)nextPower
                    && bagEvents == 3);

                const long equipSecondId = 0x223456789ABCDEFL;
                NetReader equipDetail = Feed(on18113, ctrl,
                    DragonDetailPacket(BagModel.POS_LUNG_EQUIP, equipSecondId, 820034, 1, 1, 9000000000L, 0xC1340034));
                Check("18113 worn cell replaces prior", TailOk(equipDetail, 0xC1340034)
                    && bag.GetContainer(BagModel.POS_LUNG_EQUIP).Count == 1
                    && bag.FindContainerGoods(BagModel.POS_LUNG_EQUIP, equipFirstId) == null
                    && bag.FindContainerGoods(BagModel.POS_LUNG_EQUIP, equipSecondId)?.NextPower == 9000000000UL
                    && equipEvents == 2);

                NetReader number = Feed(on15018, BagController.Instance,
                    NumPacket(BagModel.POS_LUNG_BAG, bagSecondId, 99, 820035, 0xD1350035));
                Check("15018 retains dragon detail", TailOk(number, 0xD1350035)
                    && bag.FindContainerGoods(BagModel.POS_LUNG_BAG, bagSecondId)?.GoodsNum == 99
                    && bag.FindContainerGoods(BagModel.POS_LUNG_BAG, bagSecondId)?.NextPower == (ulong)nextPower
                    && bagEvents == 4);

                int eventsBeforeUnknown = bagEvents + equipEvents;
                NetReader unknown = Feed(on18113, ctrl,
                    DragonDetailPacket(99, 0x333L, 1, 1, 1, 1, 0xC1990099));
                Check("18113 unknown location consumed and isolated", TailOk(unknown, 0xC1990099)
                    && bagEvents + equipEvents == eventsBeforeUnknown
                    && bag.GetContainer(BagModel.POS_LUNG_BAG).Count == 2
                    && bag.GetContainer(BagModel.POS_LUNG_EQUIP).Count == 1);

                byte[] first = new CliVerify.Pkt().H(2).C(1).I(101).C(2).I(202)
                    .H(2).C(3).H(4).L(5000000000L).C(5).H(6).L(7000000000L).I(303).Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on18100.Invoke(ctrl, new object[] { firstReader });
                Check("18100 fields/read-to-end", firstReader.Remaining == 0 && model.HasLungData && model.CombatPower == 303
                    && model.Attributes.Count == 2 && model.Attributes[0].AttributeId == 1 && model.Attributes[1].AttributeValue == 202
                    && model.Positions.Count == 2 && model.Positions[0].Position == 3 && model.Positions[0].Level == 4
                    && model.Positions[0].NextPower == 5000000000UL && model.Positions[1].NextPower == 7000000000UL);

                byte[] replacement = new CliVerify.Pkt().H(0).H(0).I(404).Bytes();
                var replacementReader = new NetReader(replacement, 0, replacement.Length);
                on18100.Invoke(ctrl, new object[] { replacementReader });
                Check("18100 full replace accepts empty", replacementReader.Remaining == 0 && model.HasLungData && model.CombatPower == 404
                    && model.Attributes.Count == 0 && model.Positions.Count == 0);

                trace.Clear();
                var reader = new NetReader(new CliVerify.Pkt().H(7).I(1700000000).Bytes(), 0, 6);
                on18112.Invoke(ctrl, new object[] { reader });
                Check("18112 apply/read-to-end/one-18105", reader.Remaining == 0 && model.HasOpenSchedule && model.NextCrucibleId == 7 && model.NextStartTime == 1700000000L && Frames(trace, Proto.LUNG_STOVE_INFO));

                trace.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_TIME_REFRESH);
                Check("time refresh one 18112", Frames(trace, Proto.LUNG_STOVE_OPEN_STATE));

                ctrl.Dispose();
                trace.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_TIME_REFRESH);
                Check("dispose off/reset", trace.Count == 0 && !model.HasOpenSchedule && model.NextCrucibleId == 0 && model.NextStartTime == 0
                    && !model.HasLungData && model.CombatPower == 0 && model.Attributes.Count == 0 && model.Positions.Count == 0);
                ctrl.Init(); // restore this ControllerHub singleton for later RenderAll cases.
            }
            finally
            {
                EventDispatcher.Off(GlobalEvent.EVT_LUNG_BAG_UPDATE, onBag);
                EventDispatcher.Off(GlobalEvent.EVT_LUNG_EQUIP_UPDATE, onEquip);
                bag.Clear();
                intercept.SetValue(null, oldIntercept);
            }
            Debug.Log("CLIVERIFY lung VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static NetReader Feed(MethodInfo method, object target, byte[] packet)
        {
            var reader = new NetReader(packet, 0, packet.Length);
            method.Invoke(target, new object[] { reader });
            return reader;
        }

        private static bool TailOk(NetReader reader, uint sentinel) =>
            reader.Remaining == 4 && reader.ReadU32() == sentinel && reader.Remaining == 0;

        private static byte[] FullGoodsPacket(int pos, int cellNum, int maxCell, long goodsId, int typeId,
            int cell, long num, uint sentinel)
        {
            var p = new CliVerify.Pkt().H(pos).H(cellNum).H(maxCell).C(0).H(1);
            AppendGoods(p, goodsId, typeId, cell, num, pos, false, 0);
            return p.I(sentinel).Bytes();
        }

        private static byte[] DeltaGoodsPacket(int pos, long goodsId, int typeId, int cell, long num, uint sentinel)
        {
            var p = new CliVerify.Pkt().H(pos).H(1);
            AppendGoods(p, goodsId, typeId, cell, num, pos, false, 0);
            return p.I(sentinel).Bytes();
        }

        private static byte[] DragonDetailPacket(int location, long goodsId, int typeId, int cell, long num,
            long nextPower, uint sentinel)
        {
            var p = new CliVerify.Pkt().H(location).H(1);
            AppendGoods(p, goodsId, typeId, cell, num, location, true, nextPower);
            return p.I(sentinel).Bytes();
        }

        private static byte[] NumPacket(int pos, long goodsId, long num, int typeId, uint sentinel) =>
            new CliVerify.Pkt().H(pos).H(1).L(goodsId).I(num).I(typeId).I(sentinel).Bytes();

        /// <summary>pt_150通用物品主体；dragon=true时镜像pt_181：awake无exp，尾随next_power:u64。</summary>
        private static void AppendGoods(CliVerify.Pkt p, long goodsId, int typeId, int cell, long num,
            int seed, bool dragon, long nextPower)
        {
            p.L(goodsId).I(typeId).C(3).H(cell).I(num)
                .C(1).C(2).C(3).C(4).C(5)
                .I(1700000000L + seed).I(5000 + seed).H(10 + seed).H(20 + seed)
                .I(6000 + seed).I(7000 + seed)
                .H(1).C(6).I(7100 + seed).C(7).I(7200 + seed)
                .H(1).C(8).C(9).H(100 + seed).I(7300 + seed).C(2).I(7400 + seed)
                .C(4).C(5).I(59140030L + seed).C(6)
                .H(1).H(1000 + seed).I(2000 + seed);
            if (dragon) p.L(nextPower);
            else p.I(3000 + seed);
        }

        private static bool DetailFrame(byte[] frame, long goodsId, int location)
        {
            if (frame == null || frame.Length != 16 || frame[0] != 0 || frame[1] != 16
                || frame[2] != 3 || frame[3] != 232
                || frame[4] != (byte)(Proto.LUNG_GOODS_DETAIL >> 8) || frame[5] != (byte)(Proto.LUNG_GOODS_DETAIL & 0xFF))
                return false;
            var reader = new NetReader(frame, 6, 10);
            return reader.ReadU64() == goodsId && reader.ReadU16() == location && reader.Remaining == 0;
        }

        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] f = frames[i];
                if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232 || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)ids[i]) return false;
            }
            return true;
        }
    }
}
