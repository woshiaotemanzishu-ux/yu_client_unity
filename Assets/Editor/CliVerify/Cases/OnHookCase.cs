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
            var frames = new List<byte[]>();
            FieldInfo intercept = typeof(OnHookController).GetField("s_outboundIntercept", F);
            object oldIntercept = intercept?.GetValue(null);
            CliVerify.Stage stage = null;
            try
            {
                if (!ctrl.IsInitialized) ctrl.Init();
                if (intercept == null) return 3;
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                FieldInfo hf = typeof(NetManager).GetField("_handlers", F);
                var handlers = hf?.GetValue(null) as IDictionary;
                bool registered = handlers != null && handlers.Contains(13211) && handlers.Contains(13212) && handlers.Contains(13214) && handlers.Contains(13215) && handlers.Contains(13216) && handlers.Contains(13218);

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
                var r12 = Feed("On13212", new CliVerify.Pkt().C(2).H(88).I(111).H(2).C(3).I(21).L(1000).C(5).I(22).L(2000)
                    .I(3).L(4000).I(5000).I(6000).L(7000).I(8000).Bytes());
                OnHookModel model = OnHookModel.Instance;
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
                bool onlyShowInfoOutbound = frames.Count == 1 && empty(frames[0], Proto.ONHOOK_INFO);
                var r18 = Feed("On13218", new CliVerify.Pkt().H(2).H(60000).C(255).H(7).C(1).Bytes());
                bool autoSmeltPush = r18.Remaining == 0 && model.AutoSmeltExp == 60007
                    && model.CostAfkTime == costBefore13215 && model.RemainingAfkTime == remainingBefore13215
                    && model.TotalAfkTime == totalBefore13215 && model.ExpEffect == expEffect && model.Rewards.Count == 2
                    && model.Rewards[0].GoodsId == rewardIdBefore13215 && model.Rewards[1].Num == rewardNumBefore13215
                    && frames.Count == 1 && empty(frames[0], Proto.ONHOOK_INFO);
                var r18empty = Feed("On13218", new CliVerify.Pkt().H(0).Bytes());
                bool autoSmeltEmpty = r18empty.Remaining == 0 && model.AutoSmeltExp == 0;
                var r11 = Feed("On13211", new CliVerify.Pkt().I(1).I(9000).I(10000).Bytes());
                bool tick = r11.Remaining == 0 && model.NextTime == 9000 && model.TotalAfkTime == 10000 && model.Rewards.Count == 2;
                var r11fail = Feed("On13211", new CliVerify.Pkt().I(2).I(9001).I(99999).Bytes());
                bool tickFail = r11fail.Remaining == 0 && model.NextTime == 9001 && model.TotalAfkTime == 10000 && model.Rewards.Count == 2;
                var r14 = Feed("On13214", new CliVerify.Pkt().I(11000).I(12000).Bytes());
                bool time = r14.Remaining == 0 && model.RemainingAfkTime == 11000 && model.NextTime == 12000 && model.Rewards.Count == 2;

                frames.Clear(); var r16ok = Feed("On13216", new CliVerify.Pkt().I(1).H(1).H(2).H(1).C(3).I(4).L(5).Bytes());
                bool successNoRequest = r16ok.Remaining == 0 && frames.Count == 0;
                var r16fail = Feed("On13216", new CliVerify.Pkt().I(2).H(1).H(2).H(0).Bytes());
                bool failNoRequest = r16fail.Remaining == 0 && frames.Count == 0;
                bool pass = registered && showSentInfo && info && expEffectPush && onlyShowInfoOutbound && autoSmeltPush && autoSmeltEmpty && tick && tickFail && time && successNoRequest && failNoRequest;
                Debug.Log("CLIVERIFY onhook pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                OnHookShellView.Close(); stage?.Dispose(); OnHookModel.Instance.Reset();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }
    }
}
