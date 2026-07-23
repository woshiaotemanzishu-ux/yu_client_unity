using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.OnHook;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>轮98：13211/12/14快照与13216服务端后续13212推送（客户端不重拉）回归。</summary>
    public static class OnHookCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static int Run()
        {
            OnHookController ctrl = OnHookController.Instance;
            OnHookModel model = OnHookModel.Instance;
            bool wasInitialized = ctrl.IsInitialized;
            byte oldLoginType = model.LoginType; ushort oldOffLevel = model.OffLevel;
            int oldCost = model.CostAfkTime, oldRemaining = model.RemainingAfkTime, oldNext = model.NextTime, oldTotal = model.TotalAfkTime, oldBackCount = model.BackCount;
            long oldBackExp = model.BackExp, oldExpEffect = model.ExpEffect, oldAutoSmelt = model.AutoSmeltExp;
            var oldRewards = new List<OnHookModel.Reward>(model.Rewards);
            bool oldHasExpAdditions = model.HasExpAdditions;
            var oldExpAdditions = new List<OnHookModel.ExpAddition>(model.ExpAdditions);
            var frames = new List<byte[]>();
            FieldInfo intercept = typeof(OnHookController).GetField("s_outboundIntercept", F);
            object oldIntercept = intercept?.GetValue(null);
            CliVerify.Stage stage = null;
            try
            {
                if (ctrl.IsInitialized) ctrl.Dispose();
                model.Reset();
                ctrl.Init();
                if (intercept == null) return 3;
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                FieldInfo hf = typeof(NetManager).GetField("_handlers", F);
                var handlers = hf?.GetValue(null) as IDictionary;
                bool registered = handlers != null && handlers.Contains(13211) && handlers.Contains(13212) && handlers.Contains(13214) && handlers.Contains(13215) && handlers.Contains(13216) && handlers.Contains(13217) && handlers.Contains(13218) && !handlers.Contains(13213);

                NetReader Feed(string method, byte[] bytes)
                {
                    var reader = new NetReader(bytes, 0, bytes.Length);
                    MethodInfo m = typeof(OnHookController).GetMethod(method, F);
                    m.Invoke(m.IsStatic ? null : ctrl, new object[] { reader });
                    return reader;
                }
                bool empty(byte[] frame, int proto) => frame != null && frame.Length == 6
                    && ((frame[4] << 8) | frame[5]) == proto;

                stage = CliVerify.Stage.Create();
                OnHookShellView.Show();
                bool showSentInfo = frames.Count == 1 && empty(frames[0], Proto.ONHOOK_INFO);
                ctrl.RequestExpAdditions();
                bool expAdditionsRequest = frames.Count == 2 && empty(frames[1], Proto.ONHOOK_EXP_ADDITIONS) && !model.HasExpAdditions;
                frames.Clear();
                var r12 = Feed("On13212", new CliVerify.Pkt().C(2).H(88).I(111).H(2).C(3).I(21).L(1000).C(5).I(22).L(2000)
                    .I(3).L(4000).I(5000).I(6000).L(7000).I(8000).Bytes());
                bool info = r12.Remaining == 0 && model.CostAfkTime == 111 && model.RemainingAfkTime == 5000 && model.Rewards.Count == 2
                    && OnHookShellView.DisplayText.Contains("累计挂机") && OnHookShellView.DisplayText.Contains("剩余挂机") && OnHookShellView.DisplayText.Contains("奖励：2项");
                int costBefore13215 = model.CostAfkTime;
                int remainingBefore13215 = model.RemainingAfkTime;
                int totalBefore13215 = model.TotalAfkTime;
                int rewardIdBefore13215 = model.Rewards[0].GoodsId;
                long rewardNumBefore13215 = model.Rewards[1].Num;
                const long expEffect = 5000000000L;
                var r15 = Feed("On13215", new CliVerify.Pkt().L(expEffect).Bytes());
                bool expEffectPush = r15.Remaining == 0 && model.ExpEffect == expEffect
                    && model.CostAfkTime == costBefore13215 && model.RemainingAfkTime == remainingBefore13215
                    && model.TotalAfkTime == totalBefore13215 && model.Rewards.Count == 2
                    && model.Rewards[0].GoodsId == rewardIdBefore13215 && model.Rewards[1].Num == rewardNumBefore13215
                    && OnHookShellView.DisplayText.Contains("经验效率：" + expEffect + "/分");
                bool onlyShowInfoOutbound = frames.Count == 0;
                var r18 = Feed("On13218", new CliVerify.Pkt().H(2).H(60000).C(255).H(7).C(1).Bytes());
                bool autoSmeltPush = r18.Remaining == 0 && model.AutoSmeltExp == 60007
                    && model.CostAfkTime == costBefore13215 && model.RemainingAfkTime == remainingBefore13215
                    && model.TotalAfkTime == totalBefore13215 && model.ExpEffect == expEffect && model.Rewards.Count == 2
                    && model.Rewards[0].GoodsId == rewardIdBefore13215 && model.Rewards[1].Num == rewardNumBefore13215
                    && frames.Count == 0;
                var r18empty = Feed("On13218", new CliVerify.Pkt().H(0).Bytes());
                bool autoSmeltEmpty = r18empty.Remaining == 0 && model.AutoSmeltExp == 0;
                var r11 = Feed("On13211", new CliVerify.Pkt().I(1).I(9000).I(10000).Bytes());
                bool tick = r11.Remaining == 0 && model.NextTime == 9000 && model.TotalAfkTime == 10000 && model.Rewards.Count == 2;
                var r11fail = Feed("On13211", new CliVerify.Pkt().I(2).I(9001).I(99999).Bytes());
                bool tickFail = r11fail.Remaining == 0 && model.NextTime == 9001 && model.TotalAfkTime == 10000 && model.Rewards.Count == 2;
                var r14 = Feed("On13214", new CliVerify.Pkt().I(11000).I(12000).Bytes());
                bool time = r14.Remaining == 0 && model.RemainingAfkTime == 11000 && model.NextTime == 12000 && model.Rewards.Count == 2;

                var r17empty = Feed("On13217", new CliVerify.Pkt().H(0).Bytes());
                bool additionsEmpty = r17empty.Remaining == 0 && model.HasExpAdditions && model.ExpAdditions.Count == 0 && frames.Count == 0;
                var r17one = Feed("On13217", new CliVerify.Pkt().H(1).I(1).L(5000000000L).I(2).Bytes());
                bool additionsOne = r17one.Remaining == 0 && model.ExpAdditions.Count == 1 && model.ExpAdditions[0].Type == 1 && model.ExpAdditions[0].Ratio == 5000000000L && model.ExpAdditions[0].EndTime == 2 && frames.Count == 0;
                var r17many = Feed("On13217", new CliVerify.Pkt().H(2).I(0xFFFFFFFFL).L(-1).I(0xFFFFFFFFL).I(0xFFFFFFFFL).L(5000000001L).I(8).Bytes());
                bool additionsMany = r17many.Remaining == 0 && model.ExpAdditions.Count == 2
                    && model.ExpAdditions[0].Type == uint.MaxValue && model.ExpAdditions[0].Ratio == -1 && model.ExpAdditions[0].EndTime == uint.MaxValue
                    && model.ExpAdditions[1].Type == uint.MaxValue && model.ExpAdditions[1].Ratio == 5000000001L && model.ExpAdditions[1].EndTime == 8 && frames.Count == 0;
                ctrl.RequestExpAdditions();
                bool additionsNoResponse = frames.Count == 1 && empty(frames[0], Proto.ONHOOK_EXP_ADDITIONS) && model.ExpAdditions.Count == 2 && model.ExpAdditions[0].Type == uint.MaxValue;
                frames.Clear();
                var r17less = Feed("On13217", new CliVerify.Pkt().H(1).I(7).L(9).I(10).Bytes());
                bool additionsReplace = r17less.Remaining == 0 && model.ExpAdditions.Count == 1 && model.ExpAdditions[0].Type == 7 && model.ExpAdditions[0].Ratio == 9 && model.ExpAdditions[0].EndTime == 10;
                bool additionsIsolation = model.LoginType == 2 && model.OffLevel == 88 && model.CostAfkTime == costBefore13215
                    && model.RemainingAfkTime == 11000 && model.NextTime == 12000 && model.TotalAfkTime == 10000
                    && model.ExpEffect == expEffect && model.AutoSmeltExp == 0 && model.Rewards.Count == 2;
                var r11Keeps17 = Feed("On13211", new CliVerify.Pkt().I(1).I(1).I(2).Bytes());
                var r12Keeps17 = Feed("On13212", new CliVerify.Pkt().C(1).H(2).I(3).H(1).C(4).I(5).L(6).I(7).L(8).I(9).I(10).L(11).I(12).Bytes());
                var r14Keeps17 = Feed("On13214", new CliVerify.Pkt().I(13).I(14).Bytes());
                var r15Keeps17 = Feed("On13215", new CliVerify.Pkt().L(15).Bytes());
                var r18Keeps17 = Feed("On13218", new CliVerify.Pkt().H(1).H(16).C(17).Bytes());
                var r16Keeps17 = Feed("On13216", new CliVerify.Pkt().I(1).H(0).H(0).H(0).Bytes());
                bool existingKeepAdditions = r11Keeps17.Remaining == 0 && r12Keeps17.Remaining == 0 && r14Keeps17.Remaining == 0
                    && r15Keeps17.Remaining == 0 && r18Keeps17.Remaining == 0 && r16Keeps17.Remaining == 0
                    && model.ExpAdditions.Count == 1 && model.ExpAdditions[0].Type == 7 && frames.Count == 0;
                var r17clear = Feed("On13217", new CliVerify.Pkt().H(0).Bytes());
                bool additionsClear = r17clear.Remaining == 0 && model.HasExpAdditions && model.ExpAdditions.Count == 0 && frames.Count == 0;

                frames.Clear(); var r16ok = Feed("On13216", new CliVerify.Pkt().I(1).H(1).H(2).H(1).C(3).I(4).L(5).Bytes());
                bool successNoRequest = r16ok.Remaining == 0 && frames.Count == 0;
                var r16fail = Feed("On13216", new CliVerify.Pkt().I(2).H(1).H(2).H(0).Bytes());
                bool failNoRequest = r16fail.Remaining == 0 && frames.Count == 0;
                ctrl.Dispose();
                bool disposed = !ctrl.IsInitialized && !handlers.Contains(13211) && !handlers.Contains(13212) && !handlers.Contains(13214) && !handlers.Contains(13215) && !handlers.Contains(13216) && !handlers.Contains(13217) && !handlers.Contains(13218)
                    && !model.HasExpAdditions && model.ExpAdditions.Count == 0 && model.Rewards.Count == 0 && model.ExpEffect == 0 && model.AutoSmeltExp == 0;
                bool pass = registered && showSentInfo && expAdditionsRequest && info && expEffectPush && onlyShowInfoOutbound && autoSmeltPush && autoSmeltEmpty && tick && tickFail && time
                    && additionsEmpty && additionsOne && additionsMany && additionsNoResponse && additionsReplace && existingKeepAdditions && additionsClear && additionsIsolation && successNoRequest && failNoRequest && disposed;
                Debug.Log("CLIVERIFY onhook pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                OnHookShellView.Close(); stage?.Dispose();
                if (ctrl.IsInitialized) ctrl.Dispose();
                model.Reset();
                model.ApplyInfo(oldLoginType, oldOffLevel, oldCost, oldRewards, oldBackCount, oldBackExp, oldRemaining, oldNext, oldExpEffect, oldTotal);
                model.ApplyAutoSmeltExp(oldAutoSmelt);
                if (oldHasExpAdditions) model.ReplaceExpAdditions(oldExpAdditions);
                if (wasInitialized) ctrl.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }
    }
}
