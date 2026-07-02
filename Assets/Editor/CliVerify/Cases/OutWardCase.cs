using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 幻化外观(OutWard)实证:config_mount_star 同步 + 16002/16023/16028/16029 合成包驱动 OutWardModel/Controller,
    /// 断言字段套值 + errcode!=1 不抛异常;再拉起 OutWardShellView 渲染断言「1阶2星」文案与升星按钮存在。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep(已 public),不改 CliVerify.cs 本体(主控统一接 RenderAll)。
    /// </summary>
    public static class OutWardCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.OutWard.OutWardConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.OutWard.OutWardConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_mount_star not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.OutWard.OutWardController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m16002 = ctrl.GetType().GetMethod("On16002", F);
                System.Reflection.MethodInfo m16023 = ctrl.GetType().GetMethod("On16023", F);
                System.Reflection.MethodInfo m16028 = ctrl.GetType().GetMethod("On16028", F);
                System.Reflection.MethodInfo m16029 = ctrl.GetType().GetMethod("On16029", F);
                if (m16002 == null || m16023 == null || m16028 == null || m16029 == null)
                {
                    Debug.LogError("CLIVERIFY outward handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.OutWard.OutWardModel model = Shenxiao.Module.Core.OutWard.OutWardModel.Instance;

                // 16002 回包:type_id:c, stage:c, star:h, blessing:i, figure_stage:c, combat:i, etime:l, auto_buy:c,
                // attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×{skill_id:i}]。type_id=1(坐骑) 1阶1星 blessing=5。
                byte[] p16002 = new CliVerify.Pkt()
                    .C(1)      // type_id
                    .C(1)      // stage
                    .H(1)      // star
                    .I(5)      // blessing
                    .C(0)      // figure_stage
                    .I(100)    // combat
                    .L(0)      // etime
                    .C(0)      // auto_buy
                    .H(0)      // attr_list 计数
                    .H(0)      // skill_list 计数
                    .Bytes();
                Feed(m16002, p16002);
                Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo1 = model.Get(1);
                bool infoOk = vo1 != null && vo1.Stage == 1 && vo1.Star == 1 && vo1.Blessing == 5 && vo1.Combat == 100;
                Debug.Log("CLIVERIFY outward 16002 type1 stage=" + (vo1?.Stage ?? -1) + " star=" + (vo1?.Star ?? -1)
                    + " blessing=" + (vo1?.Blessing ?? -1) + " ok=" + infoOk);

                // 16023 升星成功:errcode=1, type_id=1, stage=1, star=2, blessing=10, blessing_plus=0, etime=0, auto_buy=0, ratio_list 空
                // (成功后控制器内部会 SendFmt 16002 联动刷新,未连接只 warn,无害)。
                byte[] p16023Ok = new CliVerify.Pkt().I(1).C(1).C(1).H(2).I(10).I(0).L(0).C(0).H(0).Bytes();
                Feed(m16023, p16023Ok);
                vo1 = model.Get(1);
                bool starUpOk = vo1 != null && vo1.Stage == 1 && vo1.Star == 2 && vo1.Blessing == 10;
                Debug.Log("CLIVERIFY outward 16023 ok stage=" + (vo1?.Stage ?? -1) + " star=" + (vo1?.Star ?? -1)
                    + " blessing=" + (vo1?.Blessing ?? -1) + " ok=" + starUpOk);

                // 16023 升星失败:errcode=5,只要不抛异常(走 toast log 分支)即过。
                byte[] p16023Fail = new CliVerify.Pkt().I(5).C(1).C(1).H(2).I(0).I(0).L(0).C(0).H(0).Bytes();
                bool starUpFailNoThrow = true;
                try { Feed(m16023, p16023Fail); }
                catch (System.Exception e) { starUpFailNoThrow = false; Debug.LogError("CLIVERIFY outward 16023 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16023 fail noThrow=" + starUpFailNoThrow);

                // 16028 面板回包:type_id:c, level:h, cur_exp:i, combat:i, attr_list[], skill_list[u16×{skill_id:i,skill_level:c}]。
                // type_id=2(同修) level=1。
                byte[] p16028 = new CliVerify.Pkt().C(2).H(1).I(0).I(50).H(0).H(0).Bytes();
                Feed(m16028, p16028);
                Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo2 = model.Get(2);
                bool lvPanelOk = vo2 != null && vo2.HasLv && vo2.Level == 1 && vo2.LvCombat == 50;
                Debug.Log("CLIVERIFY outward 16028 type2 level=" + (vo2?.Level ?? -1) + " combat=" + (vo2?.LvCombat ?? -1) + " ok=" + lvPanelOk);

                // 16029 升级成功:errcode=1, type_id=2, level=2, cur_exp=0, add_exp=100, combat=60, skill_list[]=0, ratio_list[]=0。
                byte[] p16029Ok = new CliVerify.Pkt().I(1).C(2).H(2).I(0).I(100).I(60).H(0).H(0).Bytes();
                Feed(m16029, p16029Ok);
                vo2 = model.Get(2);
                bool lvUpOk = vo2 != null && vo2.Level == 2 && vo2.LvCombat == 60;
                Debug.Log("CLIVERIFY outward 16029 ok level=" + (vo2?.Level ?? -1) + " combat=" + (vo2?.LvCombat ?? -1) + " ok=" + lvUpOk);

                // 16029 升级失败:errcode=5,只要不抛异常即过。
                byte[] p16029Fail = new CliVerify.Pkt().I(5).C(2).H(2).I(0).I(0).I(0).H(0).H(0).Bytes();
                bool lvUpFailNoThrow = true;
                try { Feed(m16029, p16029Fail); }
                catch (System.Exception e) { lvUpFailNoThrow = false; Debug.LogError("CLIVERIFY outward 16029 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16029 fail noThrow=" + lvUpFailNoThrow);

                Shenxiao.Module.Core.OutWard.OutWardShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round16_outward_shell.png");

                Transform row0 = CliVerify.FindDeep(stage.CanvasRoot, "Row0");
                TMP_Text row0Label = row0 != null ? row0.GetComponentInChildren<TMP_Text>(true) : null;
                bool rowOk = row0Label != null && !string.IsNullOrEmpty(row0Label.text) && row0Label.text.Contains("1阶2星");
                Transform starBtn = CliVerify.FindDeep(stage.CanvasRoot, "Btn升星");
                bool starBtnOk = starBtn != null && starBtn.gameObject.activeInHierarchy;
                Debug.Log("CLIVERIFY outward shell rowOk=" + rowOk + " starBtn=" + starBtnOk + " shot=" + png);

                bool pass = infoOk && starUpOk && starUpFailNoThrow && lvPanelOk && lvUpOk && lvUpFailNoThrow && rowOk && starBtnOk;
                Debug.Log("CLIVERIFY outward VERDICT infoOk=" + infoOk + " starUpOk=" + starUpOk
                    + " starUpFailNoThrow=" + starUpFailNoThrow + " lvPanelOk=" + lvPanelOk + " lvUpOk=" + lvUpOk
                    + " lvUpFailNoThrow=" + lvUpFailNoThrow + " rowOk=" + rowOk + " starBtnOk=" + starBtnOk + " pass=" + pass);

                Shenxiao.Module.Core.OutWard.OutWardShellView.Close();
                Shenxiao.Module.Core.OutWard.OutWardModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
