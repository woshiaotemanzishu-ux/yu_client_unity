using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 冲级豪礼实证:config_rush_giftbag 同步 + 41700(全量列表)/41701(领取成功)合成包
    /// 反射喂 RushGiftController 私有 handler,断言 RushGiftModel 数据;再拉起 RushGiftShellView 渲染断言
    /// 领取按钮/状态文案含真实"已领"。同 CliVerify.PartnerCase 结构(主控接线调用,本文件不修改 CliVerify.cs)。
    /// </summary>
    public static class RushGiftCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                await Shenxiao.Module.Core.RushGift.RushGiftConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.RushGift.RushGiftConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY rushgift FAIL config_rush_giftbag not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.RushGift.RushGiftController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo m41700 = ctrl.GetType().GetMethod("On41700", F);
                MethodInfo m41701 = ctrl.GetType().GetMethod("On41701", F);
                if (m41700 == null || m41701 == null)
                {
                    Debug.LogError("CLIVERIFY rushgift handlers missing (reflection)");
                    return 3;
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.RushGift.RushGiftModel model = Shenxiao.Module.Core.RushGift.RushGiftModel.Instance;

                // 41700 全量:giftbag_state[u16×{lv:h, received:c, end_time:l, remain_num:i}] —— 2 条(lv=35 可领 / lv=60 未达)
                byte[] p41700 = new CliVerify.Pkt()
                    .H(2)          // giftbag_state 计数
                        .H(35).C(1).L(0).I(100)   // lv=35 received=1(可领) end_time=0 remain_num=100
                        .H(60).C(0).L(0).I(0)     // lv=60 received=0(条件不足) end_time=0 remain_num=0
                    .Bytes();
                Feed(m41700, p41700);
                bool listOk = model.HasData && model.List.Count == 2
                    && model.Get(35) != null && model.Get(35).Received == 1
                    && model.Get(60) != null && model.Get(60).Received == 0;
                Debug.Log("CLIVERIFY rushgift 41700 hasData=" + model.HasData + " count=" + model.List.Count
                    + " lv35Received=" + (model.Get(35)?.Received ?? -1) + " lv60Received=" + (model.Get(60)?.Received ?? -1)
                    + " ok=" + listOk);

                // 41701 领取成功:lv=35, code=1, rewards 1 项 {type=0, type_id=38067001, num=1}(真实 config_rush_giftbag lv35 首件奖励)
                // → 会触发 GetMappingTypeId(0,38067001)=(38067001,0) toast「获得X」+ ApplyReceived(35) + 再发 41700(未连接只 warn,无害)
                byte[] p41701Ok = new CliVerify.Pkt()
                    .H(35).I(1)
                    .H(1).C(0).I(38067001).I(1)   // rewards[0] = {type:0, type_id:38067001, num:1}
                    .Bytes();
                bool receiveNoThrow = true;
                try { Feed(m41701, p41701Ok); }
                catch (System.Exception e) { receiveNoThrow = false; Debug.LogError("CLIVERIFY rushgift 41701 threw: " + e); }
                bool receivedOk = model.Get(35) != null && model.Get(35).Received == 2;
                Debug.Log("CLIVERIFY rushgift 41701 ok lv35Received=" + (model.Get(35)?.Received ?? -1)
                    + " noThrow=" + receiveNoThrow + " ok=" + receivedOk);

                Shenxiao.Module.Core.RushGift.RushGiftShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round16_rushgift_shell.png");

                Transform row0 = CliVerify.FindDeep(stage.CanvasRoot, "Row0");
                TMP_Text rowLabel = row0 != null ? row0.GetComponentInChildren<TMP_Text>(true) : null;
                bool rowOk = rowLabel != null && !string.IsNullOrEmpty(rowLabel.text) && rowLabel.text.Contains("已领");
                Debug.Log("CLIVERIFY rushgift shell rowText=" + (rowLabel != null ? rowLabel.text : "<none>")
                    + " rowOk=" + rowOk + " shot=" + png);

                bool pass = listOk && receiveNoThrow && receivedOk && rowOk;
                Debug.Log("CLIVERIFY rushgift VERDICT listOk=" + listOk + " receiveNoThrow=" + receiveNoThrow
                    + " receivedOk=" + receivedOk + " rowOk=" + rowOk + " pass=" + pass);

                Shenxiao.Module.Core.RushGift.RushGiftShellView.Close();
                Shenxiao.Module.Core.RushGift.RushGiftModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
