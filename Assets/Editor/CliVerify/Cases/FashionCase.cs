using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 时装(Fashion,pt_413)实证,第21轮 PA:反射喂合成包驱动 FashionController 的 8 个私有 handler +
    /// 仅活下行的 41311,断言 FashionModel 逐号落地正确、失败分支不抛异常;再拉起 FashionFlow 渲染断言
    /// FashionMainView 真实挂上业务子类(而非裸 Bind)且 UI 文本落了真数据(非空)。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep(已 public),不改 CliVerify.cs 本体(RenderAll 接线留给
    /// 后续碰 CliVerify.cs 的人,本轮 PG 也在动这个文件,不抢改)。
    /// </summary>
    public static class FashionCase
    {
        private const int POS = 1;
        private const int FASHION_ID = 12010001; // 真实配置样本(config_fashion "1@12010001@1" 已实读存在)

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Fashion.FashionConfigs.EnsureLoaded();
                await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();
                if (!Shenxiao.Module.Core.Fashion.FashionConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_fashion not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Fashion.FashionController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo m41300 = ctrl.GetType().GetMethod("On41300", F);
                MethodInfo m41301 = ctrl.GetType().GetMethod("On41301", F);
                MethodInfo m41302 = ctrl.GetType().GetMethod("On41302", F);
                MethodInfo m41303 = ctrl.GetType().GetMethod("On41303", F);
                MethodInfo m41304 = ctrl.GetType().GetMethod("On41304", F);
                MethodInfo m41306 = ctrl.GetType().GetMethod("On41306", F);
                MethodInfo m41312 = ctrl.GetType().GetMethod("On41312", F);
                MethodInfo m41316 = ctrl.GetType().GetMethod("On41316", F);
                MethodInfo m41311 = ctrl.GetType().GetMethod("On41311", F);
                if (m41300 == null || m41301 == null || m41302 == null || m41303 == null || m41304 == null
                    || m41306 == null || m41312 == null || m41316 == null || m41311 == null)
                {
                    Debug.LogError("CLIVERIFY fashion handlers missing (reflection)");
                    return 3;
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                var model = Shenxiao.Module.Core.Fashion.FashionModel.Instance;
                model.Clear();

                // 41300 全量:Code=1,pos1 一条,fashion_list 空(对标"该位无任何时装已激活"的冷启动态)
                byte[] p41300 = new CliVerify.Pkt()
                    .I(1)          // Code
                    .H(1)          // PosList 计数
                    .C(POS).I(0).H(0).I(0)  // PosId, WearFashionId, PosLv, PosUpgradeNum
                    .H(0)          // FashionList 计数(空)
                    .Bytes();
                Feed(m41300, p41300);
                Shenxiao.Module.Core.Fashion.FashionModel.PosInfo pos = model.GetPos(POS);
                bool infoOk = pos != null && pos.WearFashionId == 0 && !model.IsActivated(POS, FASHION_ID);
                Debug.Log("CLIVERIFY fashion 41300 pos=" + POS + " wear=" + (pos?.WearFashionId ?? -1) + " ok=" + infoOk);

                // 41304 激活成功:Code=1,PosId,FashionId → 内部会自动 SendFmt(41302)(未连接只是 fire-and-forget,无害)
                byte[] p41304 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).Bytes();
                Feed(m41304, p41304);
                var entry = model.GetActive(POS, FASHION_ID);
                bool activateOk = entry != null && entry.StarLv == 1 && entry.NowColorId == 0;
                Debug.Log("CLIVERIFY fashion 41304 activate star=" + (entry?.StarLv ?? -1) + " ok=" + activateOk);

                // 41302 穿戴成功:Code=1,PosId,FashionId,ColorId=0
                byte[] p41302 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(0).Bytes();
                Feed(m41302, p41302);
                pos = model.GetPos(POS);
                bool wearOk = pos != null && pos.WearFashionId == FASHION_ID;
                Debug.Log("CLIVERIFY fashion 41302 wear=" + (pos?.WearFashionId ?? -1) + " ok=" + wearOk);

                // 41303 卸下成功:Code=1,PosId,FashionId
                byte[] p41303 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).Bytes();
                Feed(m41303, p41303);
                pos = model.GetPos(POS);
                bool takeOffOk = pos != null && pos.WearFashionId == 0;
                Debug.Log("CLIVERIFY fashion 41303 takeoff wear=" + (pos?.WearFashionId ?? -1) + " ok=" + takeOffOk);

                // 41301 解锁颜色1成功:Code=1,PosId,FashionId,ColorId=1,Type=2
                byte[] p41301 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(1).C(2).Bytes();
                Feed(m41301, p41301);
                entry = model.GetActive(POS, FASHION_ID);
                bool unlockOk = entry != null && entry.IsColorUnlocked(1) && entry.GetStarLv(1) == 1;
                Debug.Log("CLIVERIFY fashion 41301 unlock color1 star=" + (entry?.GetStarLv(1) ?? -1) + " ok=" + unlockOk);

                // 41306 基础色进阶成功:Code=1,PosId,FashionId,ColorId=0,FashionStarLv=2(⚠16位)
                byte[] p41306 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(0).H(2).Bytes();
                Feed(m41306, p41306);
                entry = model.GetActive(POS, FASHION_ID);
                bool baseUpOk = entry != null && entry.GetStarLv(0) == 2;
                Debug.Log("CLIVERIFY fashion 41306 base star=" + (entry?.GetStarLv(0) ?? -1) + " ok=" + baseUpOk);

                // 41316 彩色进阶成功:PosId,FashionId,ColorId=1,Lv=2(⚠8位),Code=1(⚠最后)
                byte[] p41316 = new CliVerify.Pkt().C(POS).I(FASHION_ID).C(1).C(2).I(1).Bytes();
                Feed(m41316, p41316);
                entry = model.GetActive(POS, FASHION_ID);
                bool colorUpOk = entry != null && entry.GetStarLv(1) == 2;
                Debug.Log("CLIVERIFY fashion 41316 color1 star=" + (entry?.GetStarLv(1) ?? -1) + " ok=" + colorUpOk);

                // 41312 战力(⚠无 Code):PosId,FashionId,ColorPowerList 1 条{0, 100, 200}
                byte[] p41312 = new CliVerify.Pkt().C(POS).I(FASHION_ID).H(1).C(0).L(100).L(200).Bytes();
                Feed(m41312, p41312);
                var powers = model.GetPower(POS, FASHION_ID);
                bool powerOk = powers != null && powers.Count == 1 && powers[0].Power == 100 && powers[0].NextPower == 200;
                Debug.Log("CLIVERIFY fashion 41312 power=" + (powers != null && powers.Count > 0 ? powers[0].Power : -1) + " ok=" + powerOk);

                // 41301/41302/41306/41316 失败分支(code!=1)只要不抛异常即过(对标老端 Util.ErrorCodeShow 显码)
                bool failNoThrow = true;
                try
                {
                    Feed(m41301, new CliVerify.Pkt().I(5).C(POS).I(FASHION_ID).C(2).C(2).Bytes());
                    Feed(m41302, new CliVerify.Pkt().I(5).C(POS).I(FASHION_ID).C(0).Bytes());
                    Feed(m41306, new CliVerify.Pkt().I(5).C(POS).I(FASHION_ID).C(0).H(3).Bytes());
                    Feed(m41316, new CliVerify.Pkt().C(POS).I(FASHION_ID).C(1).C(3).I(5).Bytes());
                }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY fashion fail-branch threw: " + e); }
                Debug.Log("CLIVERIFY fashion fail branches noThrow=" + failNoThrow);

                // 41311(仅活下行,本端不发,只测收到不抛异常;RoleModel.Instance.RoleId 未登录时默认 0)
                bool figurePushNoThrow = true;
                try
                {
                    byte[] p41311 = new CliVerify.Pkt().L(0).H(1).C(1).I(5001).C(0).Bytes();
                    Feed(m41311, p41311);
                }
                catch (System.Exception e) { figurePushNoThrow = false; Debug.LogError("CLIVERIFY fashion 41311 threw: " + e); }
                Debug.Log("CLIVERIFY fashion 41311 noThrow=" + figurePushNoThrow);

                // 重新穿上(为渲染断言准备一个非空态)
                Feed(m41302, new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(0).Bytes());

                // 渲染断言:打开面板,FashionMainView 是否真挂了业务子类(而不是裸 FashionMainViewBind)
                Shenxiao.Module.Core.Fashion.FashionFlow.Open(0);
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round21_fashion_main.png");

                Transform mainViewT = CliVerify.FindDeep(stage.CanvasRoot, "FashionMainView");
                Shenxiao.Module.Core.Fashion.FashionMainView mainView =
                    mainViewT != null ? mainViewT.GetComponent<Shenxiao.Module.Core.Fashion.FashionMainView>() : null;
                bool viewUpgradedOk = mainView != null;

                Transform lbNameT = mainViewT != null ? CliVerify.FindDeep(mainViewT, "_lb_name") : null;
                TMP_Text lbName = lbNameT != null ? lbNameT.GetComponent<TMP_Text>() : null;
                bool nameOk = lbName != null && !string.IsNullOrEmpty(lbName.text);
                Debug.Log("CLIVERIFY fashion shell viewUpgraded=" + viewUpgradedOk + " lbName='" + (lbName?.text ?? "<null>")
                    + "' nameOk=" + nameOk + " shot=" + png);

                Shenxiao.Module.Core.Fashion.FashionFlow.Close();
                Shenxiao.Module.Core.Fashion.FashionFlow.Reset();
                model.Clear();

                bool pass = infoOk && activateOk && wearOk && takeOffOk && unlockOk && baseUpOk && colorUpOk && powerOk
                    && failNoThrow && figurePushNoThrow && viewUpgradedOk && nameOk;
                Debug.Log("CLIVERIFY fashion VERDICT infoOk=" + infoOk + " activateOk=" + activateOk + " wearOk=" + wearOk
                    + " takeOffOk=" + takeOffOk + " unlockOk=" + unlockOk + " baseUpOk=" + baseUpOk + " colorUpOk=" + colorUpOk
                    + " powerOk=" + powerOk + " failNoThrow=" + failNoThrow + " figurePushNoThrow=" + figurePushNoThrow
                    + " viewUpgradedOk=" + viewUpgradedOk + " nameOk=" + nameOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>
        /// 批处理入口:Unity.exe -batchmode -projectPath . -executeMethod
        ///   Shenxiao.EditorTools.FashionCase.RunBatch -logFile Temp/cliverify_fashion.log
        /// </summary>
        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code;
            try
            {
                code = await Run();
            }
            catch (System.Exception e)
            {
                Debug.LogError("CLIVERIFY fashion 异常: " + e);
                code = 1;
            }
            UnityEditor.EditorApplication.Exit(code);
        }
    }
}
