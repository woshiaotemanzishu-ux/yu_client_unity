using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 天命觉醒(觉醒之路开启)实证:config_temple_awaken_kv 同步 + 42909(前置完成态推送)/42900(完成初始任务)
    /// 合成包反射喂 TempleAwakenController 私有 handler,断言 TempleAwakenModel 数据;再拉起
    /// TempleAwakenShellView 渲染断言[开启]按钮存在。同 CliVerify.OutWardCase/RushGiftCase 结构
    /// (主控接线调用,本文件不修改 CliVerify.cs)。
    /// </summary>
    public static class TempleAwakenCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.TempleAwaken.TempleAwakenConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.TempleAwaken.TempleAwakenConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY templeawaken FAIL config_temple_awaken_kv not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.TempleAwaken.TempleAwakenController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo m42900 = ctrl.GetType().GetMethod("On42900", F);
                MethodInfo m42909 = ctrl.GetType().GetMethod("On42909", F);
                if (m42900 == null || m42909 == null)
                {
                    Debug.LogError("CLIVERIFY templeawaken handlers missing (reflection)");
                    return 3;
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.TempleAwaken.TempleAwakenModel model = Shenxiao.Module.Core.TempleAwaken.TempleAwakenModel.Instance;

                // 42909 前置任务完成态推送:is_finish:c=1。
                byte[] p42909 = new CliVerify.Pkt().C(1).Bytes();
                Feed(m42909, p42909);
                bool preTaskOk = model.PreTaskFinished;
                Debug.Log("CLIVERIFY templeawaken 42909 preTaskFinished=" + model.PreTaskFinished + " ok=" + preTaskOk);

                // 42900 失败:error_code=300,只要不抛异常且不置 Opened 即过。
                byte[] p42900Fail = new CliVerify.Pkt().I(300).Bytes();
                bool failNoThrow = true;
                try { Feed(m42900, p42900Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY templeawaken 42900 fail threw: " + e); }
                bool failNotOpened = !model.Opened;
                Debug.Log("CLIVERIFY templeawaken 42900 fail noThrow=" + failNoThrow + " notOpened=" + failNotOpened);

                Shenxiao.Module.Core.TempleAwaken.TempleAwakenShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round17_templeawaken_shell.png");

                Transform openBtn = CliVerify.FindDeep(stage.CanvasRoot, "BtnOpen");
                bool openBtnOk = openBtn != null && openBtn.gameObject.activeInHierarchy;
                Debug.Log("CLIVERIFY templeawaken shell openBtnOk=" + openBtnOk + " shot=" + png);

                // 42900 成功:error_code=1 → Opened=true(壳会自动 Close)。
                byte[] p42900Ok = new CliVerify.Pkt().I(1).Bytes();
                Feed(m42900, p42900Ok);
                bool openedOk = model.Opened;
                Debug.Log("CLIVERIFY templeawaken 42900 ok opened=" + model.Opened + " ok=" + openedOk);

                bool pass = preTaskOk && failNoThrow && failNotOpened && openBtnOk && openedOk;
                Debug.Log("CLIVERIFY templeawaken VERDICT preTaskOk=" + preTaskOk + " failNoThrow=" + failNoThrow
                    + " failNotOpened=" + failNotOpened + " openBtnOk=" + openBtnOk + " openedOk=" + openedOk + " pass=" + pass);

                Shenxiao.Module.Core.TempleAwaken.TempleAwakenShellView.Close();
                Shenxiao.Module.Core.TempleAwaken.TempleAwakenModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
