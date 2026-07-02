using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 薄增量六件套(第20轮工单)实证:16005(OutWard 通用升星)/15208(宝石镶嵌)/13216(挂机收益)/
    /// 15201(装备穿戴)/15024+15025(装备熔炼)五组协议反射喂各私有 handler,断言数据落库/不抛异常。
    /// 纯逻辑用例,无渲染(五件均无强制渲染要求:OutWard 用既有 ShellView,其余四件是无壳/最小壳协议烟测)。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt(均已 public)。
    /// 日志前缀统一 "CLIVERIFY thinslice"。
    /// </summary>
    public static class ThinSliceCase
    {
        private const System.Reflection.BindingFlags F =
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        public static async Task<int> Run()
        {
            bool outward = OutWard16005();
            bool stone = EquipStone15208();
            bool onhook = OnHook13216();
            bool wear = EquipWear15201();
            bool fusion = BagFusion150240250();

            Debug.Log("CLIVERIFY thinslice VERDICT outward16005=" + outward + " stone15208=" + stone
                + " onhook13216=" + onhook + " wear15201=" + wear + " fusion1502425=" + fusion);
            bool pass = outward && stone && onhook && wear && fusion;
            await Task.CompletedTask;
            return pass ? 0 : 3;
        }

        private static void Feed(object ctrl, System.Reflection.MethodInfo m, byte[] pkt) =>
            m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

        /// <summary>16005 OutWard 通用升星:type=3(翼影) stage1→ 成功 stage1 star2;断言 OutWardModel 套值;
        /// errcode!=1 失败包不抛。</summary>
        private static bool OutWard16005()
        {
            object ctrl = Shenxiao.Module.Core.OutWard.OutWardController.Instance;
            System.Reflection.MethodInfo m16005 = ctrl.GetType().GetMethod("On16005", F);
            if (m16005 == null)
            {
                Debug.LogError("CLIVERIFY thinslice outward16005 handler missing (reflection)");
                return false;
            }
            Shenxiao.Module.Core.OutWard.OutWardModel model = Shenxiao.Module.Core.OutWard.OutWardModel.Instance;

            // 成功包:errcode=1, type_id=3(翼影), stage=1, star=2, blessing=10, blessing_plus=0, ratio_list 空。
            byte[] pOk = new CliVerify.Pkt().I(1).C(3).C(1).H(2).I(10).I(0).H(0).Bytes();
            Feed(ctrl, m16005, pOk);
            Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo = model.Get(3);
            bool ok = vo != null && vo.Stage == 1 && vo.Star == 2 && vo.Blessing == 10;
            Debug.Log("CLIVERIFY thinslice outward16005 ok stage=" + (vo?.Stage ?? -1) + " star=" + (vo?.Star ?? -1)
                + " blessing=" + (vo?.Blessing ?? -1) + " pass=" + ok);

            // 失败包:errcode=5,只要不抛异常即过。
            byte[] pFail = new CliVerify.Pkt().I(5).C(3).C(1).H(2).I(0).I(0).H(0).Bytes();
            bool noThrow = true;
            try { Feed(ctrl, m16005, pFail); }
            catch (System.Exception e) { noThrow = false; Debug.LogError("CLIVERIFY thinslice outward16005 fail threw: " + e); }
            Debug.Log("CLIVERIFY thinslice outward16005 fail noThrow=" + noThrow);

            model.Clear();
            return ok && noThrow;
        }

        /// <summary>15208 宝石镶嵌:成功包断言不抛(toast 走 log-only);失败包同样不抛。</summary>
        private static bool EquipStone15208()
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipStoneController.Instance;
            System.Reflection.MethodInfo m15208 = ctrl.GetType().GetMethod("On15208", F);
            if (m15208 == null)
            {
                Debug.LogError("CLIVERIFY thinslice stone15208 handler missing (reflection)");
                return false;
            }

            // 成功包:res=1, equip_type=1(武器), pos=1, type_id=90010001。
            byte[] pOk = new CliVerify.Pkt().I(1).C(1).C(1).I(90010001).Bytes();
            bool noThrowOk = true;
            try { Feed(ctrl, m15208, pOk); }
            catch (System.Exception e) { noThrowOk = false; Debug.LogError("CLIVERIFY thinslice stone15208 ok threw: " + e); }
            Debug.Log("CLIVERIFY thinslice stone15208 ok noThrow=" + noThrowOk + "(toast 镶嵌成功,见上方 log)");

            // 失败包:res=1500,只要不抛异常即过。
            byte[] pFail = new CliVerify.Pkt().I(1500).C(1).C(1).I(0).Bytes();
            bool noThrowFail = true;
            try { Feed(ctrl, m15208, pFail); }
            catch (System.Exception e) { noThrowFail = false; Debug.LogError("CLIVERIFY thinslice stone15208 fail threw: " + e); }
            Debug.Log("CLIVERIFY thinslice stone15208 fail noThrow=" + noThrowFail);

            return noThrowOk && noThrowFail;
        }

        /// <summary>13216 挂机收益领取:成功包(errcode=1,exp_list 空)断言不抛;失败包同样不抛。</summary>
        private static bool OnHook13216()
        {
            object ctrl = Shenxiao.Module.Core.OnHook.OnHookController.Instance;
            System.Reflection.MethodInfo m13216 = ctrl.GetType().GetMethod("On13216", F);
            if (m13216 == null)
            {
                Debug.LogError("CLIVERIFY thinslice onhook13216 handler missing (reflection)");
                return false;
            }

            // 成功包:errcode=1, old_lv=0, old_lv_ratio=0, goods_list 空(对标 ClientProtocol.json "13216" schema)。
            byte[] pOk = new CliVerify.Pkt().I(1).H(0).H(0).H(0).Bytes();
            bool noThrowOk = true;
            try { Feed(ctrl, m13216, pOk); }
            catch (System.Exception e) { noThrowOk = false; Debug.LogError("CLIVERIFY thinslice onhook13216 ok threw: " + e); }
            Debug.Log("CLIVERIFY thinslice onhook13216 ok noThrow=" + noThrowOk + "(toast 挂机收益已领取,见上方 log)");

            // 失败包:errcode=5,只要不抛异常即过。
            byte[] pFail = new CliVerify.Pkt().I(5).H(0).H(0).H(0).Bytes();
            bool noThrowFail = true;
            try { Feed(ctrl, m13216, pFail); }
            catch (System.Exception e) { noThrowFail = false; Debug.LogError("CLIVERIFY thinslice onhook13216 fail threw: " + e); }
            Debug.Log("CLIVERIFY thinslice onhook13216 fail noThrow=" + noThrowFail);

            return noThrowOk && noThrowFail;
        }

        /// <summary>15201 装备穿戴:成功包断言不抛(toast + EVT_BAG_UPDATE 走 log-only);失败包同样不抛。</summary>
        private static bool EquipWear15201()
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipWearController.Instance;
            System.Reflection.MethodInfo m15201 = ctrl.GetType().GetMethod("On15201", F);
            if (m15201 == null)
            {
                Debug.LogError("CLIVERIFY thinslice wear15201 handler missing (reflection)");
                return false;
            }

            // 成功包:res=1, goods_id=1001(l), old_goods_id=0(l), type_id=90010001, cell_pos=3。
            byte[] pOk = new CliVerify.Pkt().I(1).L(1001).L(0).I(90010001).C(3).Bytes();
            bool noThrowOk = true;
            try { Feed(ctrl, m15201, pOk); }
            catch (System.Exception e) { noThrowOk = false; Debug.LogError("CLIVERIFY thinslice wear15201 ok threw: " + e); }
            Debug.Log("CLIVERIFY thinslice wear15201 ok noThrow=" + noThrowOk + "(toast 穿戴成功,见上方 log)");

            // 失败包:res=1500,只要不抛异常即过。
            byte[] pFail = new CliVerify.Pkt().I(1500).L(0).L(0).I(0).C(0).Bytes();
            bool noThrowFail = true;
            try { Feed(ctrl, m15201, pFail); }
            catch (System.Exception e) { noThrowFail = false; Debug.LogError("CLIVERIFY thinslice wear15201 fail threw: " + e); }
            Debug.Log("CLIVERIFY thinslice wear15201 fail noThrow=" + noThrowFail);

            return noThrowOk && noThrowFail;
        }

        /// <summary>15024 查询(level=2,exp=10)断言 BagFusionController.FusionLv==2;15025 熔炼成功包断言不抛;
        /// 15024/15025 失败态(15025 失败码)同样不抛。</summary>
        private static bool BagFusion150240250()
        {
            object ctrl = Shenxiao.Module.Core.Bag.BagFusionController.Instance;
            System.Reflection.MethodInfo m15024 = ctrl.GetType().GetMethod("On15024", F);
            System.Reflection.MethodInfo m15025 = ctrl.GetType().GetMethod("On15025", F);
            if (m15024 == null || m15025 == null)
            {
                Debug.LogError("CLIVERIFY thinslice fusion handlers missing (reflection)");
                return false;
            }

            // 15024 查询回包:level:h=2, exp:i=10。
            byte[] p15024 = new CliVerify.Pkt().H(2).I(10).Bytes();
            Feed(ctrl, m15024, p15024);
            bool lvOk = Shenxiao.Module.Core.Bag.BagFusionController.FusionLv == 2
                && Shenxiao.Module.Core.Bag.BagFusionController.FusionExp == 10;
            Debug.Log("CLIVERIFY thinslice fusion15024 lv=" + Shenxiao.Module.Core.Bag.BagFusionController.FusionLv
                + " exp=" + Shenxiao.Module.Core.Bag.BagFusionController.FusionExp + " ok=" + lvOk);

            // 15025 熔炼成功:code=1, exp_list 1项{add_exp=5,ratio=100}。
            byte[] p15025Ok = new CliVerify.Pkt().I(1).H(1).H(5).C(100).Bytes();
            bool noThrowOk = true;
            try { Feed(ctrl, m15025, p15025Ok); }
            catch (System.Exception e) { noThrowOk = false; Debug.LogError("CLIVERIFY thinslice fusion15025 ok threw: " + e); }
            Debug.Log("CLIVERIFY thinslice fusion15025 ok noThrow=" + noThrowOk + "(toast 熔炼成功,见上方 log)");

            // 15025 失败:code=1500,只要不抛异常即过。
            byte[] p15025Fail = new CliVerify.Pkt().I(1500).H(0).Bytes();
            bool noThrowFail = true;
            try { Feed(ctrl, m15025, p15025Fail); }
            catch (System.Exception e) { noThrowFail = false; Debug.LogError("CLIVERIFY thinslice fusion15025 fail threw: " + e); }
            Debug.Log("CLIVERIFY thinslice fusion15025 fail noThrow=" + noThrowFail);

            return lvOk && noThrowOk && noThrowFail;
        }
    }
}
