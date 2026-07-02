using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 古宝(妖物结界守护 soap)激活实证:config_enchantment_guard_soap(_debris) 同步 + 13320(全量)/13321(激活成功/失败)
    /// 合成包驱动 GuBaoModel/Controller,断言字段套值 + errcode!=1 不抛异常;再拉起 GuBaoShellView 渲染断言含「幽瞳」文本。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep(已 public),不改 CliVerify.cs 本体(主控统一接 RenderAll)。
    /// 日志前缀统一 "CLIVERIFY gubao"。
    /// </summary>
    public static class GuBaoCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.GuBao.GuBaoConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.GuBao.GuBaoConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY gubao FAIL config_enchantment_guard_soap(_debris) not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.GuBao.GuBaoController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m13320 = ctrl.GetType().GetMethod("On13320", F);
                System.Reflection.MethodInfo m13321 = ctrl.GetType().GetMethod("On13321", F);
                if (m13320 == null || m13321 == null)
                {
                    Debug.LogError("CLIVERIFY gubao handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.GuBao.GuBaoModel model = Shenxiao.Module.Core.GuBao.GuBaoModel.Instance;

                // 13320 全量:combat:l + soap_list[u16×{soap_id:h, debris_list[u16×{debris_id:h}]}]。
                // combat=0, 1 个 soap=10001 含已激活碎片 [1](碎片2未激活)。
                byte[] p13320 = new CliVerify.Pkt()
                    .L(0)          // combat
                    .H(1)          // soap_list 计数
                        .H(10001)      // soap_id
                        .H(1)          // debris_list 计数
                            .H(1)          // debris_id=1(已激活)
                    .Bytes();
                Feed(m13320, p13320);
                bool infoOk = model.IsDebrisActive(10001, 1) && !model.IsDebrisActive(10001, 2);
                Debug.Log("CLIVERIFY gubao 13320 debris1=" + model.IsDebrisActive(10001, 1)
                    + " debris2=" + model.IsDebrisActive(10001, 2) + " ok=" + infoOk);

                // 13321 激活成功:errcode=1, soap_id=10001, debris_list=[1,2](碎片2补齐), combat=100。
                byte[] p13321Ok = new CliVerify.Pkt()
                    .I(1)          // errcode
                    .H(10001)      // soap_id
                    .H(2)          // debris_list 计数
                        .H(1)          // debris_id=1
                        .H(2)          // debris_id=2
                    .L(100)        // combat
                    .Bytes();
                Feed(m13321, p13321Ok);
                bool activeOk = model.IsDebrisActive(10001, 2) && model.Combat == 100;
                Debug.Log("CLIVERIFY gubao 13321 ok debris2=" + model.IsDebrisActive(10001, 2) + " combat=" + model.Combat + " ok=" + activeOk);

                // 13321 激活失败:errcode=5(缺碎片材料),只要不抛异常(走 toast log 分支)即过。
                byte[] p13321Fail = new CliVerify.Pkt()
                    .I(5)          // errcode
                    .H(10001)      // soap_id
                    .H(0)          // debris_list 计数
                    .L(0)          // combat
                    .Bytes();
                bool failNoThrow = true;
                try { Feed(m13321, p13321Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY gubao 13321 fail threw: " + e); }
                Debug.Log("CLIVERIFY gubao 13321 fail noThrow=" + failNoThrow);

                Shenxiao.Module.Core.GuBao.GuBaoShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round17_gubao_shell.png");

                bool textOk = false;
                foreach (TMP_Text t in stage.CanvasRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.text != null && t.text.Contains("幽瞳")) { textOk = true; break; }
                }
                Debug.Log("CLIVERIFY gubao shell textOk=" + textOk + " shot=" + png);

                bool pass = infoOk && activeOk && failNoThrow && textOk;
                Debug.Log("CLIVERIFY gubao VERDICT infoOk=" + infoOk + " activeOk=" + activeOk
                    + " failNoThrow=" + failNoThrow + " textOk=" + textOk + " pass=" + pass);

                Shenxiao.Module.Core.GuBao.GuBaoShellView.Close();
                Shenxiao.Module.Core.GuBao.GuBaoModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
