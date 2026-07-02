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
                if (m15256 == null || m15257 == null)
                {
                    Debug.LogError("CLIVERIFY suitclt handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.SuitCollect.SuitCollectModel model = Shenxiao.Module.Core.SuitCollect.SuitCollectModel.Instance;

                // 15256 全量:clt_list[u16×{suit_id:c, cur_stage:c, cur_pos_list[u16×{equip_type:c}]}] + suit_id:c(当前时装)
                byte[] p15256 = new CliVerify.Pkt()
                    .H(1)          // clt_list 计数
                        .C(1)          // suit_id
                        .C(2)          // cur_stage
                        .H(2)          // cur_pos_list 计数
                            .C(1)          // equip_type
                            .C(2)          // equip_type
                    .C(0)          // suit_id(末尾,当前穿戴时装)
                    .Bytes();
                Feed(m15256, p15256);
                bool infoOk = model.HasData && model.GetCurStage(1) == 2;
                Debug.Log("CLIVERIFY suitclt 15256 hasData=" + model.HasData + " curStage=" + model.GetCurStage(1) + " ok=" + infoOk);

                // 15257 激活成功:code=1 + suit_id=1 + cur_stage=3 + cur_pos_list 空
                byte[] p15257Ok = new CliVerify.Pkt().I(1).C(1).C(3).H(0).Bytes();
                Feed(m15257, p15257Ok);
                bool activeOk = model.GetCurStage(1) == 3;
                Debug.Log("CLIVERIFY suitclt 15257 ok curStage=" + model.GetCurStage(1) + " ok=" + activeOk);

                // 15257 激活失败:code=2,只要不抛异常(走 toast log 分支)即过,数据不应回退
                byte[] p15257Fail = new CliVerify.Pkt().I(2).C(1).C(3).H(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m15257, p15257Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY suitclt 15257 fail threw: " + e); }
                Debug.Log("CLIVERIFY suitclt 15257 fail noThrow=" + failNoThrow + " curStageAfter=" + model.GetCurStage(1));

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

                bool pass = infoOk && activeOk && failNoThrow && activateBtnOk && textOk;
                Debug.Log("CLIVERIFY suitclt VERDICT infoOk=" + infoOk + " activeOk=" + activeOk
                    + " failNoThrow=" + failNoThrow + " activateBtnOk=" + activateBtnOk + " textOk=" + textOk + " pass=" + pass);

                Shenxiao.Module.Core.SuitCollect.SuitCollectShellView.Close();
                Shenxiao.Module.Core.SuitCollect.SuitCollectModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
