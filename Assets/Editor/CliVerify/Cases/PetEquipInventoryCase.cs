using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Bag;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// PetEquip 四装备容器前置实证：启动精确请求 4/22/32/23/33，15010/15017/15018 按容器落库，
    /// 并验证 PetEquip 16017 成功后的已穿戴实例同步 API。独立文件，不改 CliVerify.cs 调度本体。
    /// </summary>
    public static class PetEquipInventoryCase
    {
        private static readonly int[] PetPositions =
        {
            BagModel.POS_HORSE,
            BagModel.POS_HORSE_BAG,
            BagModel.POS_PARTNER,
            BagModel.POS_PARTNER_BAG,
        };

        public static async Task<int> Run()
        {
            bool editorPreferFallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            try
            {
                return await RunCore();
            }
            finally
            {
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = editorPreferFallbackBefore;
            }
        }

        private static async Task<int> RunCore()
        {
            BagController ctrl = BagController.Instance;
            BagModel bag = BagModel.Instance;
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

            MethodInfo mStart = typeof(BagController).GetMethod("OnGameStart", F);
            MethodInfo m15010 = typeof(BagController).GetMethod("On15010", F);
            MethodInfo m15017 = typeof(BagController).GetMethod("On15017", F);
            MethodInfo m15018 = typeof(BagController).GetMethod("On15018", F);
            FieldInfo startupIntercept = typeof(BagController).GetField("s_startupContainerIntercept", SF);
            if (mStart == null || m15010 == null || m15017 == null || m15018 == null || startupIntercept == null)
            {
                Debug.LogError("CLIVERIFY pet-equip inventory handlers/interceptor missing");
                return 3;
            }

            bool wasInitialized = ctrl.IsInitialized;
            if (!wasInitialized) ctrl.Init();
            bag.Clear();
            try
            {
                await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();

                var startupTrace = new List<int>();
                try
                {
                    startupIntercept.SetValue(null, new System.Func<int, bool>(pos =>
                    {
                        startupTrace.Add(pos);
                        return true;
                    }));
                    mStart.Invoke(ctrl, null);
                    await Task.Delay(20); // OnGameStart 是 async void；EnsureLoaded 已预热，仍留一拍接 continuation。
                }
                finally
                {
                    startupIntercept.SetValue(null, null);
                }
                int[] expectedStartup =
                {
                    BagModel.POS_BAG,
                    BagModel.POS_HORSE,
                    BagModel.POS_HORSE_BAG,
                    BagModel.POS_PARTNER,
                    BagModel.POS_PARTNER_BAG,
                    BagModel.POS_BABY_BAG,
                };
                bool startupOk = SequenceEqual(startupTrace, expectedStartup);

                const long mainId = 0x100000004L;
                NetReader mainFullReader = Feed(m15010, ctrl,
                    FullPacket(BagModel.POS_BAG, 1, 40, mainId, 700004, 1, 3, 4004, 4, 5, 0xA10A10A1));
                bool mainFullOk = TailOk(mainFullReader, 0xA10A10A1)
                    && bag.HasData && bag.CellNum == 1 && bag.MaxCell == 40
                    && bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsId == mainId;

                bool fullOk = true;
                var firstIds = new Dictionary<int, long>();
                foreach (int pos in PetPositions)
                {
                    long id = 0x100000000L + pos;
                    firstIds[pos] = id;
                    uint sentinel = (uint)(0xB10A0000 + pos);
                    NetReader rr = Feed(m15010, ctrl,
                        FullPacket(pos, 1, 60 + pos, id, 800000 + pos, 1, 2 + pos, 9000 + pos, 6, 7, sentinel));
                    BagGoods goods = bag.FindContainerGoods(pos, id);
                    fullOk &= TailOk(rr, sentinel)
                        && bag.GetContainer(pos).Count == 1 && bag.GetMaxCell(pos) == 60 + pos
                        && GoodsFieldsOk(goods, id, 800000 + pos, 1, 2 + pos, 9000 + pos, 6, 7, pos);
                }
                bool mainIsolatedAfterFull = bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsId == mainId;

                int babyEvents = 0;
                System.Action onBabyBag = () => babyEvents++;
                EventDispatcher.On(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE, onBabyBag);
                bool babyOk;
                try
                {
                    const long babyFirst = 0x400000025L;
                    const long babySecond = 0x500000025L;
                    NetReader babyFull = Feed(m15010, ctrl, FullPacket(BagModel.POS_BABY_BAG, 2, 77, babyFirst, 900037, 1, 3, 9300, 2, 3, 0xBABA0037));
                    babyOk = TailOk(babyFull, 0xBABA0037) && bag.HasBabyEquipBagData && bag.BabyEquipBagMaxCell == 77
                        && bag.GetContainer(BagModel.POS_BABY_BAG).Count == 1 && GoodsFieldsOk(bag.FindContainerGoods(BagModel.POS_BABY_BAG, babyFirst), babyFirst, 900037, 1, 3, 9300, 2, 3, BagModel.POS_BABY_BAG);
                    NetReader babyDelta = Feed(m15017, ctrl, DeltaFullPacket(BagModel.POS_BABY_BAG, babyFirst, 900038, 2, 8, 9301, 4, 5).I(0xDADA0037).Bytes());
                    NetReader babyAdd = Feed(m15017, ctrl, DeltaFullPacket(BagModel.POS_BABY_BAG, babySecond, 900039, 3, 9, 9302, 6, 7).I(0xDADB0037).Bytes());
                    NetReader babyDelete = Feed(m15017, ctrl, DeltaFullPacket(BagModel.POS_BABY_BAG, babyFirst, 900038, 2, 0, 9301, 4, 5).I(0xDADC0037).Bytes());
                    babyOk &= TailOk(babyDelta, 0xDADA0037) && TailOk(babyAdd, 0xDADB0037) && TailOk(babyDelete, 0xDADC0037)
                        && bag.GetContainer(BagModel.POS_BABY_BAG).Count == 1 && bag.FindContainerGoods(BagModel.POS_BABY_BAG, babySecond) != null;
                    NetReader babyNum = Feed(m15018, ctrl, NumPacket(BagModel.POS_BABY_BAG, babySecond, 99, 900039).I(0xEAEA0037).Bytes());
                    NetReader babyNumAdd = Feed(m15018, ctrl, NumPacket(BagModel.POS_BABY_BAG, 0x600000025L, 5, 900040).I(0xEAEB0037).Bytes());
                    NetReader babyNumDelete = Feed(m15018, ctrl, NumPacket(BagModel.POS_BABY_BAG, babySecond, 0, 900039).I(0xEAEC0037).Bytes());
                    babyOk &= TailOk(babyNum, 0xEAEA0037) && TailOk(babyNumAdd, 0xEAEB0037) && TailOk(babyNumDelete, 0xEAEC0037)
                        && bag.GetContainer(BagModel.POS_BABY_BAG).Count == 1 && bag.GetContainer(BagModel.POS_BABY_BAG)[0].GoodsId == 0x600000025L
                        && bag.GetContainer(BagModel.POS_BABY_BAG)[0].GoodsNum == 5 && babyEvents == 7;
                }
                finally { EventDispatcher.Off(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE, onBabyBag); }

                int wornBabyEvents = 0;
                System.Action onWornBaby = () => wornBabyEvents++;
                EventDispatcher.On(GlobalEvent.EVT_BABY_EQUIP_UPDATE, onWornBaby);
                bool wornBabyOk;
                try
                {
                    const long wornFirst = 0x700000024L;
                    const long wornSecond = 0x800000024L;
                    NetReader wornFull = Feed(m15010, ctrl, FullPacket(BagModel.POS_BABY_EQUIP, 1, 66, wornFirst, 910036, 1, 4, 9400, 3, 4, 0x3ABA0036));
                    wornBabyOk = TailOk(wornFull, 0x3ABA0036) && bag.HasBabyEquipData && bag.BabyEquipMaxCell == 66
                        && bag.GetContainer(BagModel.POS_BABY_EQUIP).Count == 1 && GoodsFieldsOk(bag.FindContainerGoods(BagModel.POS_BABY_EQUIP, wornFirst), wornFirst, 910036, 1, 4, 9400, 3, 4, BagModel.POS_BABY_EQUIP);
                    NetReader wornAdd = Feed(m15017, ctrl, DeltaFullPacket(BagModel.POS_BABY_EQUIP, wornSecond, 910037, 2, 7, 9401, 5, 6).I(0x4ADA0036).Bytes());
                    NetReader wornNum = Feed(m15018, ctrl, NumPacket(BagModel.POS_BABY_EQUIP, wornSecond, 88, 910037).I(0x5AEA0036).Bytes());
                    NetReader wornDelete = Feed(m15018, ctrl, NumPacket(BagModel.POS_BABY_EQUIP, wornFirst, 0, 910036).I(0x5AEB0036).Bytes());
                    wornBabyOk &= TailOk(wornAdd, 0x4ADA0036) && TailOk(wornNum, 0x5AEA0036) && TailOk(wornDelete, 0x5AEB0036)
                        && bag.GetContainer(BagModel.POS_BABY_EQUIP).Count == 1 && bag.FindContainerGoods(BagModel.POS_BABY_EQUIP, wornSecond)?.GoodsNum == 88
                        && bag.GetContainer(BagModel.POS_BABY_BAG).Count == 1 && wornBabyEvents == 4;
                }
                finally { EventDispatcher.Off(GlobalEvent.EVT_BABY_EQUIP_UPDATE, onWornBaby); }

                bool deltaOk = true;
                var secondIds = new Dictionary<int, long>();
                foreach (int pos in PetPositions)
                {
                    long firstId = firstIds[pos];
                    long secondId = 0x200000000L + pos;
                    secondIds[pos] = secondId;

                    Feed(m15017, ctrl, DeltaFullPacket(pos, firstId, 810000 + pos, 1, 11 + pos, 9100 + pos, 8, 9).Bytes());
                    BagGoods updated = bag.FindContainerGoods(pos, firstId);
                    deltaOk &= GoodsFieldsOk(updated, firstId, 810000 + pos, 1, 11 + pos, 9100 + pos, 8, 9, pos);

                    Feed(m15017, ctrl, DeltaFullPacket(pos, secondId, 820000 + pos, 2, 21 + pos, 9200 + pos, 10, 11).Bytes());
                    deltaOk &= bag.GetContainer(pos).Count == 2
                        && GoodsFieldsOk(bag.FindContainerGoods(pos, secondId), secondId, 820000 + pos, 2,
                            21 + pos, 9200 + pos, 10, 11, pos);
                }

                bool numDeleteOk = true;
                foreach (int pos in PetPositions)
                {
                    long firstId = firstIds[pos];
                    long secondId = secondIds[pos];
                    Feed(m15018, ctrl, NumPacket(pos, secondId, 99, 820000 + pos).Bytes());
                    Feed(m15018, ctrl, NumPacket(pos, firstId, 0, 810000 + pos).Bytes());
                    long thirdId = 0x300000000L + pos;
                    Feed(m15018, ctrl, NumPacket(pos, thirdId, 5, 830000 + pos).Bytes());
                    IReadOnlyList<BagGoods> list = bag.GetContainer(pos);
                    BagGoods third = bag.FindContainerGoods(pos, thirdId);
                    numDeleteOk &= list.Count == 2
                        && list[0].GoodsId == secondId && list[1].GoodsId == thirdId
                        && bag.FindContainerGoods(pos, secondId)?.GoodsNum == 99
                        && third != null && third.TypeId == 830000 + pos && third.GoodsNum == 5;
                }

                long horseId = secondIds[BagModel.POS_HORSE];
                long partnerId = secondIds[BagModel.POS_PARTNER];
                bool syncResult = bag.UpdatePetEquipState(BagModel.POS_HORSE, horseId, 12, 13, 120013)
                    && bag.UpdatePetEquipState(BagModel.POS_PARTNER, partnerId, 14, 15, 140015);
                BagGoods horse = bag.FindContainerGoods(BagModel.POS_HORSE, horseId);
                BagGoods partner = bag.FindContainerGoods(BagModel.POS_PARTNER, partnerId);
                bool syncOk = syncResult
                    && horse != null && horse.EquipStage == 12 && horse.EquipStar == 13 && horse.OverallRating == 120013
                    && partner != null && partner.EquipStage == 14 && partner.EquipStar == 15 && partner.OverallRating == 140015
                    && !bag.UpdatePetEquipState(BagModel.POS_HORSE_BAG, secondIds[BagModel.POS_HORSE_BAG], 1, 1, 1)
                    && !bag.UpdatePetEquipState(BagModel.POS_HORSE, 0x7FFFFFFFFL, 1, 1, 1);

                Feed(m15017, ctrl, DeltaFullPacket(BagModel.POS_BAG, mainId, 710004, 3, 8, 4104, 2, 3).Bytes());
                Feed(m15018, ctrl, NumPacket(BagModel.POS_BAG, mainId, 77, 710004).Bytes());
                BagGoods main = bag.FindContainerGoods(BagModel.POS_BAG, mainId);
                bool mainDeltaOk = bag.HasData && bag.BagGoodsList.Count == 1 && main != null
                    && main.TypeId == 710004 && main.Cell == 3 && main.GoodsNum == 77
                    && main.OverallRating == 4104 && main.EquipStage == 2 && main.EquipStar == 3;
                bag.Clear();
                bool babyClearOk = !bag.HasBabyEquipData && bag.BabyEquipMaxCell == 0 && bag.GetContainer(BagModel.POS_BABY_EQUIP).Count == 0
                    && !bag.HasBabyEquipBagData && bag.BabyEquipBagMaxCell == 0 && bag.GetContainer(BagModel.POS_BABY_BAG).Count == 0;

                bool pass = startupOk && mainFullOk && fullOk && mainIsolatedAfterFull
                    && deltaOk && numDeleteOk && syncOk && mainDeltaOk && babyOk && wornBabyOk && babyClearOk;
                Debug.Log("CLIVERIFY pet-equip inventory startup=" + startupOk
                    + " mainFull=" + mainFullOk + " fourFull=" + fullOk + " isolated=" + mainIsolatedAfterFull
                    + " delta=" + deltaOk + " numDelete=" + numDeleteOk + " sync=" + syncOk
                    + " mainRegression=" + mainDeltaOk + " baby=" + babyOk + " wornBaby=" + wornBabyOk + " babyClear=" + babyClearOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                startupIntercept.SetValue(null, null);
                bag.Clear();
                if (!wasInitialized) ctrl.Dispose();
            }
        }

        private static bool SequenceEqual(IReadOnlyList<int> actual, IReadOnlyList<int> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }

        private static NetReader Feed(MethodInfo method, BagController ctrl, byte[] packet)
        {
            var reader = new NetReader(packet, 0, packet.Length);
            method.Invoke(ctrl, new object[] { reader });
            return reader;
        }

        private static bool TailOk(NetReader reader, uint sentinel)
        {
            return reader.Remaining == 4 && reader.ReadU32() == sentinel && reader.Remaining == 0;
        }

        private static byte[] FullPacket(int pos, int cellNum, int maxCell, long goodsId, int typeId,
            int cell, long num, long overallRating, int equipStage, int equipStar, uint sentinel)
        {
            CliVerify.Pkt packet = new CliVerify.Pkt().H(pos).H(cellNum).H(maxCell).C(0).H(1);
            AppendGoods(packet, goodsId, typeId, cell, num, overallRating, equipStage, equipStar, pos);
            return packet.I(sentinel).Bytes();
        }

        private static CliVerify.Pkt DeltaFullPacket(int pos, long goodsId, int typeId, int cell, long num,
            long overallRating, int equipStage, int equipStar)
        {
            CliVerify.Pkt packet = new CliVerify.Pkt().H(pos).H(1);
            AppendGoods(packet, goodsId, typeId, cell, num, overallRating, equipStage, equipStar, pos);
            return packet;
        }

        private static CliVerify.Pkt NumPacket(int pos, long goodsId, long num, int typeId)
        {
            return new CliVerify.Pkt().H(pos).H(1).L(goodsId).I(num).I(typeId);
        }

        /// <summary>逐字段镜像 pt_150 15010/15017 goods 单项，含三个嵌套数组各一项。</summary>
        private static void AppendGoods(CliVerify.Pkt p, long goodsId, int typeId, int cell, long num,
            long overallRating, int equipStage, int equipStar, int seed)
        {
            p.L(goodsId).I(typeId).C(3).H(cell).I(num)
                .C(1).C(2).C(3).C(4).C(5)
                .I(1700000000L + seed).I(5000 + seed).H(10 + seed).H(20 + seed)
                .I(6000 + seed).I(overallRating)
                .H(1).C(6).I(7000 + seed).C(7).I(8000 + seed)
                .H(1).C(8).C(9).H(100 + seed).I(9000 + seed).C(2).I(10 + seed)
                .C(equipStage).C(equipStar).I(59140030L + seed).C(4)
                .H(1).H(1000 + seed).I(30 + seed).I(40 + seed);
        }

        private static bool GoodsFieldsOk(BagGoods goods, long goodsId, int typeId, int cell, long num,
            long overallRating, int equipStage, int equipStar, int seed)
        {
            return goods != null && goods.GoodsId == goodsId && goods.TypeId == typeId
                && goods.Cell == cell && goods.GoodsNum == num && goods.Bind == 1 && goods.Color == 5
                && goods.CombatPower == 5000 + seed && goods.Stren == 10 + seed && goods.Level == 20 + seed
                && goods.Rating == 6000 + seed && goods.OverallRating == overallRating
                && goods.EquipStage == equipStage && goods.EquipStar == equipStar
                && goods.AdditionAttrs != null && goods.AdditionAttrs.Count == 1
                && goods.AdditionAttrs[0].AttrType == 6 && goods.AdditionAttrs[0].AttrValue == 7000 + seed
                && goods.AdditionAttrs[0].Color == 7 && goods.AdditionAttrs[0].CombatPower == 8000 + seed
                && goods.ExtraAttrs != null && goods.ExtraAttrs.Count == 1
                && goods.ExtraAttrs[0].Color == 8 && goods.ExtraAttrs[0].AttrTypeId == 9
                && goods.ExtraAttrs[0].AttrId == 100 + seed && goods.ExtraAttrs[0].AttrVal == 9000 + seed
                && goods.ExtraAttrs[0].PlusInterval == 2 && goods.ExtraAttrs[0].PlusUnit == 10 + seed
                && goods.AwakeList != null && goods.AwakeList.Count == 1
                && goods.AwakeList[0].AttrType == 1000 + seed
                && goods.AwakeList[0].AwakeLv == 30 + seed && goods.AwakeList[0].AwakeExp == 40 + seed;
        }
    }
}
