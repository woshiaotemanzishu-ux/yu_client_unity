using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 套装收集实证:config_suit_clt 同步 + 15256(全量)/15257(激活成功/失败)合成包驱动 SuitCollectModel,
    /// 断言协议解析语义;再拉起 SuitCollectShellView 渲染断言进度文案 + 激活按钮存在。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt/FindDeep(均已 public)。
    /// 日志前缀统一 "CLIVERIFY suitclt"。
    /// </summary>
    public static class SuitCollectCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.SuitCollect.SuitCollectConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.SuitCollect.SuitCollectConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY suitclt FAIL config_suit_clt not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.SuitCollect.SuitCollectController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m15256 = ctrl.GetType().GetMethod("On15256", F);
                System.Reflection.MethodInfo m15257 = ctrl.GetType().GetMethod("On15257", F);
                System.Reflection.MethodInfo m15258 = ctrl.GetType().GetMethod("On15258", F);
                System.Reflection.MethodInfo m15259 = ctrl.GetType().GetMethod("On15259", F);
                System.Reflection.FieldInfo outboundField = ctrl.GetType().GetField("s_outboundIntercept",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (m15256 == null || m15257 == null || m15258 == null || m15259 == null || outboundField == null)
                {
                    Debug.LogError("CLIVERIFY suitclt handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                int FeedRemaining(System.Reflection.MethodInfo m, byte[] pkt)
                {
                    var reader = new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);
                    m.Invoke(ctrl, new object[] { reader });
                    return reader.Remaining;
                }

                Shenxiao.Module.Core.SuitCollect.SuitCollectModel model = Shenxiao.Module.Core.SuitCollect.SuitCollectModel.Instance;

                // 15256 全量:clt_list[u16×{suit_id:c, cur_stage:c, cur_pos_list[u16×{equip_type:c}]}] + suit_id:c(当前时装)
                byte[] p15256 = new CliVerify.Pkt()
                    .H(2)          // clt_list 计数
                        .C(1)          // suit_id
                        .C(2)          // cur_stage
                        .H(2)          // cur_pos_list 计数
                            .C(1)          // equip_type
                            .C(2)          // equip_type
                        .C(2)          // 另一套装，供 15258 非破坏断言
                        .C(1)
                        .H(1)
                            .C(3)
                    .C(0)          // suit_id(末尾,当前穿戴时装)
                    .Bytes();
                Feed(m15256, p15256);
                bool infoOk = model.HasData && model.GetCurStage(1) == 2;
                Debug.Log("CLIVERIFY suitclt 15256 hasData=" + model.HasData + " curStage=" + model.GetCurStage(1) + " ok=" + infoOk);

                // 15257 激活成功:code=1 + suit_id=1 + cur_stage=3 + cur_pos_list=[1,2]
                byte[] p15257Ok = new CliVerify.Pkt().I(1).C(1).C(3).H(2).C(1).C(2).Bytes();
                Feed(m15257, p15257Ok);
                bool activeOk = model.GetCurStage(1) == 3;
                Debug.Log("CLIVERIFY suitclt 15257 ok curStage=" + model.GetCurStage(1) + " ok=" + activeOk);

                // 15257 激活失败:code=2,只要不抛异常(走 toast log 分支)即过,数据不应回退
                byte[] p15257Fail = new CliVerify.Pkt().I(2).C(1).C(3).H(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m15257, p15257Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY suitclt 15257 fail threw: " + e); }
                Debug.Log("CLIVERIFY suitclt 15257 fail noThrow=" + failNoThrow + " curStageAfter=" + model.GetCurStage(1));

                int updateEvents = 0;
                System.Action onUpdate = () => updateEvents++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_SUIT_CLT_UPDATE, onUpdate);

                byte[] p15258 = new CliVerify.Pkt().H(5)
                    .C(1).C(2) // 已有，验证去重
                    .C(1).C(4)
                    .C(1).C(4) // 包内重复
                    .C(2).C(3) // 另一套装已有部位
                    .C(2).C(5)
                    .Bytes();
                int remain15258 = FeedRemaining(m15258, p15258);
                bool merge58Ok = remain15258 == 0
                    && model.Get(1).CurStage == 3
                    && model.Get(1).PosList.Count == 3
                    && model.Get(1).PosList.Contains(1) && model.Get(1).PosList.Contains(2) && model.Get(1).PosList.Contains(4)
                    && model.Get(2).CurStage == 1
                    && model.Get(2).PosList.Count == 2
                    && model.Get(2).PosList.Contains(3) && model.Get(2).PosList.Contains(5);
                int suit1Count = model.Get(1).PosList.Count;
                int suit2Count = model.Get(2).PosList.Count;
                int remain15258Empty = FeedRemaining(m15258, new CliVerify.Pkt().H(0).Bytes());
                bool empty58Ok = remain15258Empty == 0 && model.Get(1).PosList.Count == suit1Count
                    && model.Get(2).PosList.Count == suit2Count;
                bool event58Ok = updateEvents == 2;
                Debug.Log("CLIVERIFY suitclt 15258 merge=" + merge58Ok + " empty=" + empty58Ok + " event=" + event58Ok);

                var logs = new System.Collections.Generic.List<string>();
                Application.LogCallback logCallback = (msg, stack, type) => logs.Add(msg);
                Application.logMessageReceived += logCallback;
                bool wear59Ok;
                bool unwear59Ok;
                bool externalClear59Ok;
                bool fail59Ok;
                try
                {
                    int before59Events = updateEvents;
                    int wearRemaining = FeedRemaining(m15259, new CliVerify.Pkt().I(1).C(1).C(1).Bytes());
                    wear59Ok = wearRemaining == 0 && model.FashionSuitId == 1
                        && logs.Exists(x => x.Contains("toast: 穿戴成功"));

                    logs.Clear();
                    int unwearRemaining = FeedRemaining(m15259, new CliVerify.Pkt().I(1).C(1).C(0).Bytes());
                    unwear59Ok = unwearRemaining == 0 && model.FashionSuitId == 0
                        && logs.Exists(x => x.Contains("toast: 脱下成功"));

                    FeedRemaining(m15259, new CliVerify.Pkt().I(1).C(1).C(1).Bytes());
                    logs.Clear();
                    int clearRemaining = FeedRemaining(m15259, new CliVerify.Pkt().I(1).C(0).C(0).Bytes());
                    externalClear59Ok = clearRemaining == 0 && model.FashionSuitId == 0
                        && !logs.Exists(x => x.Contains("toast:"));

                    FeedRemaining(m15259, new CliVerify.Pkt().I(1).C(1).C(1).Bytes());
                    logs.Clear();
                    int failRemaining = FeedRemaining(m15259, new CliVerify.Pkt().I(2).C(2).C(1).Bytes());
                    fail59Ok = failRemaining == 0 && model.FashionSuitId == 1
                        && logs.Exists(x => x.Contains("toast: 操作失败(2)"))
                        && updateEvents == before59Events + 5;
                }
                finally { Application.logMessageReceived -= logCallback; }
                Debug.Log("CLIVERIFY suitclt 15259 wear=" + wear59Ok + " unwear=" + unwear59Ok
                    + " externalClear=" + externalClear59Ok + " failUnchanged=" + fail59Ok);

                var outbound = new System.Collections.Generic.List<byte[]>();
                System.Func<byte[], bool> intercept = frame => { outbound.Add(frame); return true; };
                bool outbound59Ok;
                try
                {
                    outboundField.SetValue(null, intercept);
                    Shenxiao.Module.Core.SuitCollect.SuitCollectController.Instance.SetFashionWear(1, true);
                    bool wearWire = outbound.Count == 1 && FrameEquals(outbound[0],
                        Shenxiao.Framework.Net.Proto.SUIT_CLT_FASHION_WEAR, new CliVerify.Pkt().C(1).C(1).Bytes());
                    outbound.Clear();
                    Shenxiao.Module.Core.SuitCollect.SuitCollectController.Instance.SetFashionWear(1, false);
                    bool unwearWire = outbound.Count == 1 && FrameEquals(outbound[0],
                        Shenxiao.Framework.Net.Proto.SUIT_CLT_FASHION_WEAR, new CliVerify.Pkt().C(1).C(0).Bytes());
                    outbound.Clear();
                    Shenxiao.Module.Core.SuitCollect.SuitCollectController.Instance.SetFashionWear(0, true);
                    Shenxiao.Module.Core.SuitCollect.SuitCollectController.Instance.SetFashionWear(255, false);
                    bool invalidSuppressed = outbound.Count == 0;
                    outbound59Ok = wearWire && unwearWire && invalidSuppressed;
                }
                finally { outboundField.SetValue(null, null); }
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_SUIT_CLT_UPDATE, onUpdate);
                Debug.Log("CLIVERIFY suitclt 15259 outbound=" + outbound59Ok);

                Shenxiao.Module.Core.SuitCollect.SuitCollectShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round16_suitclt_shell.png");

                Transform activateBtn = CliVerify.FindDeep(stage.CanvasRoot, "BtnActivate");
                bool activateBtnOk = activateBtn != null && activateBtn.gameObject.activeInHierarchy;
                TMP_Text bodyText = null;
                Transform bodyTextTf = CliVerify.FindDeep(stage.CanvasRoot, "BodyText");
                if (bodyTextTf != null) bodyText = bodyTextTf.GetComponent<TMP_Text>();
                bool textOk = bodyText != null && !string.IsNullOrEmpty(bodyText.text) && bodyText.text.Contains("3");
                Debug.Log("CLIVERIFY suitclt shell textOk=" + textOk + " activateBtn=" + activateBtnOk + " shot=" + png);

                model.Clear();
                bool clearOk = !model.HasData && model.Suits.Count == 0 && model.FashionSuitId == 0;
                bool pass = infoOk && activeOk && failNoThrow && merge58Ok && empty58Ok && event58Ok
                    && wear59Ok && unwear59Ok && externalClear59Ok && fail59Ok && outbound59Ok
                    && activateBtnOk && textOk && clearOk;
                Debug.Log("CLIVERIFY suitclt VERDICT infoOk=" + infoOk + " activeOk=" + activeOk
                    + " failNoThrow=" + failNoThrow + " p15258=" + (merge58Ok && empty58Ok && event58Ok)
                    + " p15259=" + (wear59Ok && unwear59Ok && externalClear59Ok && fail59Ok && outbound59Ok)
                    + " activateBtnOk=" + activateBtnOk + " textOk=" + textOk + " clear=" + clearOk + " pass=" + pass);

                Shenxiao.Module.Core.SuitCollect.SuitCollectShellView.Close();
                Shenxiao.Module.Core.SuitCollect.SuitCollectModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static bool FrameEquals(byte[] actual, int protoId, byte[] payload)
        {
            int total = 6 + payload.Length;
            if (actual == null || actual.Length != total) return false;
            byte[] expected = new byte[total];
            expected[0] = (byte)(total >> 8);
            expected[1] = (byte)total;
            expected[2] = 0x03;
            expected[3] = 0xE8;
            expected[4] = (byte)(protoId >> 8);
            expected[5] = (byte)protoId;
            System.Buffer.BlockCopy(payload, 0, expected, 6, payload.Length);
            for (int i = 0; i < total; i++) if (actual[i] != expected[i]) return false;
            return true;
        }
    }
}
