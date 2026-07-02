using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 灵魄镶嵌实证(第19轮工单 B):16700(全量)/16701(镶嵌)合成包反射喂 RuneController 私有 handler,
    /// 断言 RuneModel 槽位落库 + 镶嵌成功套值 + 失败包不抛异常;再拉起 RuneWearShellView 渲染断言标题「灵魄镶嵌」。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep(均已 public),不改 CliVerify.cs 本体(主控统一接 RenderAll)。
    /// 日志前缀统一 "CLIVERIFY rune"。
    /// </summary>
    public static class RuneCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                object ctrl = Shenxiao.Module.Core.Rune.RuneController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m16700 = ctrl.GetType().GetMethod("On16700", F);
                System.Reflection.MethodInfo m16701 = ctrl.GetType().GetMethod("On16701", F);
                if (m16700 == null || m16701 == null)
                {
                    Debug.LogError("CLIVERIFY rune handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Rune.RuneModel model = Shenxiao.Module.Core.Rune.RuneModel.Instance;
                model.Clear();

                // 16700 全量:rune_point:i, rune_chip:i, skill_lv:h, rune_list[u16×1项(pos1,if_open=1,空 goods)], rune_sum_power:l。
                byte[] p16700 = new CliVerify.Pkt()
                    .I(0)          // rune_point
                    .I(0)          // rune_chip
                    .H(0)          // skill_lv
                    .H(1)          // rune_list 计数
                        .C(1)          // pos_id=1
                        .C(1)          // if_open=1
                        .L(0)          // goods_id=0(未镶嵌)
                        .I(0)          // goods_type_id
                        .C(0)          // color
                        .H(0)          // lv
                        .H(0)          // attr_list 计数=0
                    .L(0)          // rune_sum_power
                    .Bytes();
                Feed(m16700, p16700);
                Shenxiao.Module.Core.Rune.RuneModel.SlotVo slot1 = model.GetSlot(1);
                bool infoOk = model.HasData && model.Slots.Count == 1
                    && slot1 != null && slot1.IfOpen && !slot1.IsWorn;
                Debug.Log("CLIVERIFY rune 16700 hasData=" + model.HasData + " slots=" + model.Slots.Count
                    + " pos1IfOpen=" + (slot1?.IfOpen ?? false) + " pos1Worn=" + (slot1?.IsWorn ?? false) + " ok=" + infoOk);

                // 16701 镶嵌成功:code=1, pos_id=1, new_goods_id=901, old_goods_id=0, new_goods_type_id=888001。
                byte[] p16701Ok = new CliVerify.Pkt()
                    .I(1)          // code
                    .C(1)          // pos_id
                    .L(901)        // new_goods_id
                    .L(0)          // old_goods_id
                    .I(888001)     // new_goods_type_id
                    .Bytes();
                Feed(m16701, p16701Ok);
                slot1 = model.GetSlot(1);
                bool wearOk = slot1 != null && slot1.GoodsId == 901 && slot1.GoodsTypeId == 888001 && slot1.IsWorn;
                Debug.Log("CLIVERIFY rune 16701 ok goodsId=" + (slot1?.GoodsId ?? -1)
                    + " goodsTypeId=" + (slot1?.GoodsTypeId ?? -1) + " ok=" + wearOk);

                // 16701 镶嵌失败:code=5(常见=材料不对),只要不抛异常即过,槽位数据不应回退。
                byte[] p16701Fail = new CliVerify.Pkt().I(5).C(1).L(0).L(901).I(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m16701, p16701Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY rune 16701 fail threw: " + e); }
                slot1 = model.GetSlot(1);
                bool dataUnchanged = slot1 != null && slot1.GoodsId == 901 && slot1.GoodsTypeId == 888001;
                Debug.Log("CLIVERIFY rune 16701 fail noThrow=" + failNoThrow + " dataUnchanged=" + dataUnchanged);

                Shenxiao.Module.Core.Rune.RuneWearShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round18_rune_shell.png");

                bool titleOk = false;
                foreach (TMP_Text t in stage.CanvasRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.text != null && t.text.Contains("灵魄镶嵌")) { titleOk = true; break; }
                }
                Debug.Log("CLIVERIFY rune shell titleOk=" + titleOk + " shot=" + png);

                bool pass = infoOk && wearOk && failNoThrow && dataUnchanged && titleOk;
                Debug.Log("CLIVERIFY rune VERDICT infoOk=" + infoOk + " wearOk=" + wearOk
                    + " failNoThrow=" + failNoThrow + " dataUnchanged=" + dataUnchanged + " titleOk=" + titleOk + " pass=" + pass);

                Shenxiao.Module.Core.Rune.RuneWearShellView.Close();
                Shenxiao.Module.Core.Rune.RuneModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
