using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Kaifu;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class KaifuCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY kaifu EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            KaifuController controller = KaifuController.Instance;
            KaifuModel model = KaifuModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var oldOpenList = new List<KaifuModel.InvestItem>();
            foreach (KaifuModel.InvestItem item in model.OpenList)
            {
                oldOpenList.Add(new KaifuModel.InvestItem
                {
                    Type = item.Type,
                    ShowId = item.ShowId,
                    State = item.State,
                    RefreshTime = item.RefreshTime,
                });
            }

            bool oldShowValueIcon = model.ShowValueIcon;
            bool oldShowTopIcon = model.ShowTopIcon;
            bool oldBookActive = model.BookActive;
            bool oldBookAllClaimed = model.BookAllClaimed;
            var oldInvestInfos = new Dictionary<byte, KaifuModel.InvestInfoSnapshot>(model.InvestInfos);
            FieldInfo interceptField = typeof(KaifuController).GetField("s_investInfoOutboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on42001 = typeof(KaifuController).GetMethod("On42001", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on42001 != null && handlers != null
                    && handlers.Contains(Proto.KAIFU_INVEST_ERROR)
                    && handlers.Contains(Proto.KAIFU_INVEST_INFO)
                    && handlers.Contains(Proto.KAIFU_INVEST_OPEN)
                    && handlers.Contains(Proto.KAIFU_BOOK_INFO)
                    && !handlers.Contains(42002) && !handlers.Contains(42003)
                    && typeof(KaifuController).GetMethod("OnGameStart", InstanceNonPublic) == null
                    && model.InvestInfos.Count == 0;
                if (!pass)
                {
                    Debug.LogError("CLIVERIFY kaifu VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestInvestInfo(0);
                controller.RequestInvestInfo(1);
                controller.RequestInvestInfo(byte.MaxValue);
                pass &= frames.Count == 3
                    && IsExactInvestRequest(frames[0], 0)
                    && IsExactInvestRequest(frames[1], 1)
                    && IsExactInvestRequest(frames[2], byte.MaxValue)
                    && model.InvestInfos.Count == 0;
                frames.Clear();

                var zeroReader = new NetReader(new CliVerify.Pkt().C(0).H(0).I(0).I(0).H(0).H(0).Bytes(), 0, 15);
                on42001.Invoke(controller, new object[] { zeroReader });
                pass &= zeroReader.Remaining == 0 && model.TryGetInvestInfo(0, out KaifuModel.InvestInfoSnapshot zero)
                    && zero.CurLv == 0 && zero.BuyTime == 0 && zero.GetTime == 0 && zero.LoginDays == 0 && zero.Rewards.Count == 0
                    && model.InvestInfos.Count == 1 && frames.Count == 0;

                byte[] fullBytes = new CliVerify.Pkt().C(1).H(ushort.MaxValue).I(uint.MaxValue).I(4000000000L).H(ushort.MaxValue).H(3)
                    .C(0).H(0).C(byte.MaxValue).H(ushort.MaxValue).C(byte.MaxValue).H(ushort.MaxValue).Bytes();
                var fullReader = new NetReader(fullBytes, 0, fullBytes.Length);
                on42001.Invoke(controller, new object[] { fullReader });
                pass &= fullReader.Remaining == 0 && model.TryGetInvestInfo(1, out KaifuModel.InvestInfoSnapshot full)
                    && full.CurLv == ushort.MaxValue && full.BuyTime == uint.MaxValue && full.GetTime == 4000000000U && full.LoginDays == ushort.MaxValue
                    && full.Rewards.Count == 3 && full.Rewards[0].Id == 0 && full.Rewards[0].GotLv == 0
                    && full.Rewards[1].Id == byte.MaxValue && full.Rewards[1].GotLv == ushort.MaxValue
                    && full.Rewards[2].Id == byte.MaxValue && full.Rewards[2].GotLv == ushort.MaxValue
                    && model.TryGetInvestInfo(0, out zero) && zero.Rewards.Count == 0 && frames.Count == 0;

                var typeMaxReader = new NetReader(new CliVerify.Pkt().C(byte.MaxValue).H(1).I(2).I(3).H(4).H(1).C(5).H(6).Bytes(), 0, 18);
                on42001.Invoke(controller, new object[] { typeMaxReader });
                pass &= typeMaxReader.Remaining == 0 && model.TryGetInvestInfo(byte.MaxValue, out KaifuModel.InvestInfoSnapshot typeMax)
                    && typeMax.CurLv == 1 && typeMax.BuyTime == 2 && typeMax.GetTime == 3 && typeMax.LoginDays == 4
                    && typeMax.Rewards.Count == 1 && typeMax.Rewards[0].Id == 5 && typeMax.Rewards[0].GotLv == 6
                    && model.InvestInfos.Count == 3 && frames.Count == 0;

                controller.RequestInvestInfo(1);
                pass &= frames.Count == 1 && IsExactInvestRequest(frames[0], 1)
                    && model.TryGetInvestInfo(1, out full) && full.Rewards.Count == 3;
                frames.Clear();

                var replacementReader = new NetReader(new CliVerify.Pkt().C(1).H(7).I(8).I(9).H(10).H(1).C(11).H(12).Bytes(), 0, 18);
                on42001.Invoke(controller, new object[] { replacementReader });
                pass &= replacementReader.Remaining == 0 && model.TryGetInvestInfo(1, out KaifuModel.InvestInfoSnapshot replacement)
                    && replacement.CurLv == 7 && replacement.BuyTime == 8 && replacement.GetTime == 9 && replacement.LoginDays == 10
                    && replacement.Rewards.Count == 1 && replacement.Rewards[0].Id == 11 && replacement.Rewards[0].GotLv == 12
                    && model.TryGetInvestInfo(0, out zero) && model.TryGetInvestInfo(byte.MaxValue, out typeMax) && frames.Count == 0;

                var emptyReader = new NetReader(new CliVerify.Pkt().C(1).H(0).I(0).I(0).H(0).H(0).Bytes(), 0, 15);
                on42001.Invoke(controller, new object[] { emptyReader });
                pass &= emptyReader.Remaining == 0 && model.TryGetInvestInfo(1, out KaifuModel.InvestInfoSnapshot empty)
                    && empty.Rewards.Count == 0 && model.TryGetInvestInfo(0, out zero) && model.TryGetInvestInfo(byte.MaxValue, out typeMax)
                    && model.InvestInfos.Count == 3 && frames.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized
                    && !handlers.Contains(Proto.KAIFU_INVEST_ERROR)
                    && !handlers.Contains(Proto.KAIFU_INVEST_INFO)
                    && !handlers.Contains(Proto.KAIFU_INVEST_OPEN)
                    && !handlers.Contains(Proto.KAIFU_BOOK_INFO)
                    && model.InvestInfos.Count == 0 && model.OpenList.Count == 0 && !model.BookActive && !model.BookAllClaimed;

                Debug.Log("CLIVERIFY kaifu VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                model.Reset();
                model.SetInvestOpen(oldOpenList);
                model.ShowValueIcon = oldShowValueIcon;
                model.ShowTopIcon = oldShowTopIcon;
                model.SetBookInfo(oldBookActive, oldBookAllClaimed);
                foreach (KeyValuePair<byte, KaifuModel.InvestInfoSnapshot> pair in oldInvestInfos)
                {
                    KaifuModel.InvestInfoSnapshot snapshot = pair.Value;
                    model.ReplaceInvestInfo(snapshot.Type, snapshot.CurLv, snapshot.BuyTime, snapshot.GetTime, snapshot.LoginDays, new List<KaifuModel.InvestRewardEntry>(snapshot.Rewards));
                }

                if (wasInitialized)
                {
                    controller.Init();
                }

                if (interceptField != null)
                {
                    interceptField.SetValue(null, oldIntercept);
                }
            }
        }

        private static bool IsExactInvestRequest(byte[] frame, byte type)
        {
            return frame != null
                && frame.Length == 7
                && frame[0] == 0 && frame[1] == 7
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.KAIFU_INVEST_INFO >> 8)
                && frame[5] == (byte)(Proto.KAIFU_INVEST_INFO & 0xFF)
                && frame[6] == type;
        }
    }
}
