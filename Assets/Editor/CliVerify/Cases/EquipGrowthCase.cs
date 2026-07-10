using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 装备成长四件套实证(自动循环 轮4 队列#4):15250/15251(神兵淬炼,EquipSmeltController)、
    /// 15212/15213/15214/15252(吞天洗魄,EquipWashController)、15255(神屠九炼,EquipRefinementController)、
    /// 15260/15261(淬炉宗师全身奖励,挂 EquipStrenController)合成包反射喂对应控制器私有 handler,断言:
    /// 模型套值 + 尾哨兵字节不被多吃/少吃(GameLog "remaining=NB" 行) + 失败码包不炸(res!=1/res1!=0 对标
    /// ALL_SMELT_FAIL) + GlobalEvent 广播 + GoodsDynamicModel.Invalidate/Patch 生效(经 Peek 断言)。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt(均已 public)。
    /// 日志前缀统一 "CLIVERIFY equipgrowth"。
    /// </summary>
    public static class EquipGrowthCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                bool smeltOk = RunSmelt(logs);
                bool washOk = RunWash(logs);
                bool refinementOk = RunRefinement(logs);
                bool wholeOk = RunWhole(logs);

                bool pass = smeltOk && washOk && refinementOk && wholeOk;
                Debug.Log("CLIVERIFY equipgrowth VERDICT smelt=" + smeltOk + " wash=" + washOk
                    + " refinement=" + refinementOk + " whole=" + wholeOk + " pass=" + pass);

                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                stage.Dispose();
            }
        }

        private static void Feed(object ctrl, MethodInfo m, byte[] pkt) =>
            m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

        // ---- 神兵淬炼(15250/15251) ----

        private static bool RunSmelt(List<string> logs)
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipSmeltController.Instance;
            MethodInfo m50 = ctrl.GetType().GetMethod("On15250", F);
            MethodInfo m51 = ctrl.GetType().GetMethod("On15251", F);
            if (m50 == null || m51 == null)
            {
                Debug.LogError("CLIVERIFY equipgrowth smelt handlers missing (reflection)");
                return false;
            }

            Shenxiao.Module.Core.Equip.EquipSmeltModel model = Shenxiao.Module.Core.Equip.EquipSmeltModel.Instance;
            model.Clear();

            // 15250 查询(尾哨兵 2 字节 0xEE 0xEE):res=1 equip_type=1 refine=5 refine_high=9。
            logs.Clear();
            byte[] p50 = new CliVerify.Pkt().I(1).C(1).H(5).H(9).C(0xEE).C(0xEE).Bytes();
            Feed(ctrl, m50, p50);
            (int refine, int refineHigh) v1 = model.GetSmelt(1);
            bool tail50 = logs.Exists(l => l.Contains("remaining=2B"));
            bool queryOk = v1.refine == 5 && v1.refineHigh == 9 && tail50;
            Debug.Log("CLIVERIFY equipgrowth 15250 refine=" + v1.refine + " refineHigh=" + v1.refineHigh
                + " tail=" + tail50 + " ok=" + queryOk);

            // 15251 一键成功(res=1,res1=0,type=2):2 项 refine_info + 尾哨兵 2 字节。
            logs.Clear();
            byte[] p51Ok = new CliVerify.Pkt()
                .I(1).C(0).C(2)
                .H(2)
                    .C(1).H(9)
                    .C(2).H(3)
                .C(0xEE).C(0xEE)
                .Bytes();
            Feed(ctrl, m51, p51Ok);
            (int refine, int refineHigh) a1 = model.GetSmelt(1);
            (int refine, int refineHigh) a2 = model.GetSmelt(2);
            bool okApplied = a1.refine == 9 && a1.refineHigh == 9 && a2.refine == 3 && a2.refineHigh == 3;
            bool okToast = logs.Exists(l => l.Contains("toast: 精炼成功"));
            bool okTail = logs.Exists(l => l.Contains("remaining=2B"));
            Debug.Log("CLIVERIFY equipgrowth 15251 ok applied=" + okApplied + " toast=" + okToast + " tail=" + okTail);

            // 15251 一键部分失败:res=1500(!=1) res1=7(!=0) type=2 → 对标老端 ALL_SMELT_FAIL,toast「精炼失败」,不抛异常。
            logs.Clear();
            byte[] p51Fail = new CliVerify.Pkt().I(1500).C(7).C(2).H(0).Bytes();
            bool failNoThrow = true;
            try { Feed(ctrl, m51, p51Fail); }
            catch (Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY equipgrowth 15251 fail threw: " + e); }
            bool failToast = logs.Exists(l => l.Contains("toast: 精炼失败"));
            (int refine, int refineHigh) a1After = model.GetSmelt(1);
            bool dataUnchanged = a1After.refine == 9 && a1After.refineHigh == 9;
            Debug.Log("CLIVERIFY equipgrowth 15251 fail noThrow=" + failNoThrow + " toast=" + failToast
                + " dataUnchanged=" + dataUnchanged);

            model.Clear();
            bool pass = queryOk && okApplied && okToast && okTail && failNoThrow && failToast && dataUnchanged;
            Debug.Log("CLIVERIFY equipgrowth smelt VERDICT pass=" + pass);
            return pass;
        }

        // ---- 吞天洗魄(15212/15213/15214/15252) ----

        private static bool RunWash(List<string> logs)
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipWashController.Instance;
            MethodInfo m12 = ctrl.GetType().GetMethod("On15212", F);
            MethodInfo m13 = ctrl.GetType().GetMethod("On15213", F);
            MethodInfo m14 = ctrl.GetType().GetMethod("On15214", F);
            MethodInfo m52 = ctrl.GetType().GetMethod("On15252", F);
            if (m12 == null || m13 == null || m14 == null || m52 == null)
            {
                Debug.LogError("CLIVERIFY equipgrowth wash handlers missing (reflection)");
                return false;
            }

            Shenxiao.Module.Core.Equip.EquipWashModel.Instance.Clear();
            Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Clear();

            int washUpdateCount = 0;
            Action onWashUpdate = () => washUpdateCount++;
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_EQUIP_WASH_UPDATE, onWashUpdate);
            try
            {
                // 15212 开槽成功:先发起 OpenSlot(equip_type=1,index=0) 记 pending equip_type,再喂回包
                // res=1 goods_id=100 index=0(0-based,回包字段本身就是 0-based index,不是 15212 发送时的 +1)。
                Shenxiao.Module.Core.Equip.EquipWashController.Instance.OpenSlot(1, 0);
                byte[] p12 = new CliVerify.Pkt().I(1).L(100).C(0).Bytes();
                Feed(ctrl, m12, p12);
                bool slotOpened = Shenxiao.Module.Core.Equip.EquipWashModel.Instance.IsSlotOpened(1, 0);
                Debug.Log("CLIVERIFY equipgrowth 15212 slotOpened=" + slotOpened);

                // 预热缓存:goods_id=555 装入一个假 vo,验证 15213/15252 成功后经 Invalidate 被清空(Peek==null)。
                var vo555 = new Shenxiao.Module.Core.Bag.GoodsDetailVo { GoodsId = 555, RefinementLv = 1 };
                Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Store(vo555);
                bool cachedBefore13 = Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Peek(555) != null;

                // 15213 洗魄执行成功:res=1 goods_id=555 attr_list 2 项([{index:0},{index:2}]) + 尾哨兵 2 字节。
                logs.Clear();
                byte[] p13 = new CliVerify.Pkt().I(1).L(555).H(2).C(0).C(2).C(0xEE).C(0xEE).Bytes();
                bool wash13NoThrow = true;
                try { Feed(ctrl, m13, p13); }
                catch (Exception e) { wash13NoThrow = false; Debug.LogError("CLIVERIFY equipgrowth 15213 threw: " + e); }
                bool tail13 = logs.Exists(l => l.Contains("remaining=2B"));
                bool invalidated13 = Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Peek(555) == null;
                Debug.Log("CLIVERIFY equipgrowth 15213 noThrow=" + wash13NoThrow + " tail=" + tail13
                    + " cachedBefore=" + cachedBefore13 + " invalidatedAfter=" + invalidated13);

                // 15214 免费次数:free_times=3(无 res 字段)。
                byte[] p14 = new CliVerify.Pkt().C(3).Bytes();
                Feed(ctrl, m14, p14);
                bool freeTimesOk = Shenxiao.Module.Core.Equip.EquipWashModel.Instance.FreeTimes == 3;
                Debug.Log("CLIVERIFY equipgrowth 15214 freeTimes=" + Shenxiao.Module.Core.Equip.EquipWashModel.Instance.FreeTimes
                    + " ok=" + freeTimesOk);

                // 15252 升段成功:res=1 goods_id=555(复用同一 goods_id,复验 Invalidate 再次生效)。
                var vo555b = new Shenxiao.Module.Core.Bag.GoodsDetailVo { GoodsId = 555, RefinementLv = 1 };
                Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Store(vo555b);
                logs.Clear();
                byte[] p52Ok = new CliVerify.Pkt().I(1).L(555).Bytes();
                Feed(ctrl, m52, p52Ok);
                bool division52Toast = logs.Exists(l => l.Contains("toast: 升段成功"));
                bool invalidated52 = Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Peek(555) == null;
                Debug.Log("CLIVERIFY equipgrowth 15252 toast=" + division52Toast + " invalidatedAfter=" + invalidated52);

                // 15252 失败码包:res=1500,只要不抛异常即过。
                bool division52FailNoThrow = true;
                try { Feed(ctrl, m52, new CliVerify.Pkt().I(1500).L(555).Bytes()); }
                catch (Exception e) { division52FailNoThrow = false; Debug.LogError("CLIVERIFY equipgrowth 15252 fail threw: " + e); }

                bool eventFired = washUpdateCount >= 3;   // 15212/15213/15252 均 Emit(至少 3 次)
                Debug.Log("CLIVERIFY equipgrowth wash eventFired count=" + washUpdateCount);

                bool pass = slotOpened && wash13NoThrow && tail13 && cachedBefore13 && invalidated13
                    && freeTimesOk && division52Toast && invalidated52 && division52FailNoThrow && eventFired;
                Debug.Log("CLIVERIFY equipgrowth wash VERDICT pass=" + pass);
                return pass;
            }
            finally
            {
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_EQUIP_WASH_UPDATE, onWashUpdate);
                Shenxiao.Module.Core.Equip.EquipWashModel.Instance.Clear();
                Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Clear();
            }
        }

        // ---- 神屠九炼(15255) ----

        private static bool RunRefinement(List<string> logs)
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipRefinementController.Instance;
            MethodInfo m55 = ctrl.GetType().GetMethod("On15255", F);
            if (m55 == null)
            {
                Debug.LogError("CLIVERIFY equipgrowth refinement handler missing (reflection)");
                return false;
            }

            Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Clear();
            int refiUpdateCount = 0;
            Action onRefiUpdate = () => refiUpdateCount++;
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_EQUIP_REFINEMENT_UPDATE, onRefiUpdate);
            try
            {
                // 预热缓存:goods_id=777,RefinementLv 初值 3。15255 成功回包自带新值 → 走 Patch 就地改(不是 Invalidate/
                // 清空),故断言用 Peek 读到更新后的值,而非 Peek==null(与洗魄 15213/15252 走 Invalidate 的断言方式不同,
                // 因为 15255 回包本身就带 refine_lv 新值,不必强制重拉,对标老端 on15255 直接 vo.refinement_lv=scmd.refine_lv)。
                var vo777 = new Shenxiao.Module.Core.Bag.GoodsDetailVo { GoodsId = 777, RefinementLv = 3 };
                Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Store(vo777);

                logs.Clear();
                byte[] p55Ok = new CliVerify.Pkt().I(1).L(777).I(4).Bytes();
                bool noThrow = true;
                try { Feed(ctrl, m55, p55Ok); }
                catch (Exception e) { noThrow = false; Debug.LogError("CLIVERIFY equipgrowth 15255 threw: " + e); }
                Shenxiao.Module.Core.Bag.GoodsDetailVo patched = Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Peek(777);
                bool patchOk = patched != null && patched.RefinementLv == 4;
                bool toastOk = logs.Exists(l => l.Contains("toast: 神炼成功"));
                Debug.Log("CLIVERIFY equipgrowth 15255 noThrow=" + noThrow + " patchOk=" + patchOk
                    + " refinementLv=" + (patched?.RefinementLv ?? -1) + " toast=" + toastOk);

                // 失败码包:code=1500,只要不抛异常即过,且缓存不应被误改。
                bool failNoThrow = true;
                try { Feed(ctrl, m55, new CliVerify.Pkt().I(1500).L(777).I(0).Bytes()); }
                catch (Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY equipgrowth 15255 fail threw: " + e); }
                Shenxiao.Module.Core.Bag.GoodsDetailVo afterFail = Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Peek(777);
                bool dataUnchangedOnFail = afterFail != null && afterFail.RefinementLv == 4;

                bool eventFired = refiUpdateCount >= 1;
                bool pass = noThrow && patchOk && toastOk && failNoThrow && dataUnchangedOnFail && eventFired;
                Debug.Log("CLIVERIFY equipgrowth refinement VERDICT eventFired=" + eventFired + " pass=" + pass);
                return pass;
            }
            finally
            {
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_EQUIP_REFINEMENT_UPDATE, onRefiUpdate);
                Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Clear();
            }
        }

        // ---- 淬炉宗师全身奖励(15260/15261,挂 EquipStrenController) ----

        private static bool RunWhole(List<string> logs)
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipStrenController.Instance;
            MethodInfo m60 = ctrl.GetType().GetMethod("On15260", F);
            MethodInfo m61 = ctrl.GetType().GetMethod("On15261", F);
            if (m60 == null || m61 == null)
            {
                Debug.LogError("CLIVERIFY equipgrowth whole handlers missing (reflection)");
                return false;
            }

            Shenxiao.Module.Core.Equip.EquipWholeAwardModel model = Shenxiao.Module.Core.Equip.EquipWholeAwardModel.Instance;
            model.Clear();

            // 15261 列表:2 项 {type=1,whole_lv=3} {type=3,whole_lv=1}。
            byte[] p61 = new CliVerify.Pkt().H(2).C(1).H(3).C(3).H(1).Bytes();
            Feed(ctrl, m61, p61);
            bool listOk = model.GetWholeLv(1) == 3 && model.GetWholeLv(3) == 1;
            Debug.Log("CLIVERIFY equipgrowth 15261 type1=" + model.GetWholeLv(1) + " type3=" + model.GetWholeLv(3) + " ok=" + listOk);

            // 15260 激活成功:errcode=1 type=1 whole_lv=5。
            logs.Clear();
            byte[] p60Ok = new CliVerify.Pkt().I(1).C(1).H(5).Bytes();
            Feed(ctrl, m60, p60Ok);
            bool activateOk = model.GetWholeLv(1) == 5;
            bool activateToast = logs.Exists(l => l.Contains("toast: 激活成功"));
            Debug.Log("CLIVERIFY equipgrowth 15260 ok whole_lv=" + model.GetWholeLv(1) + " toast=" + activateToast + " ok=" + activateOk);

            // 15260 失败码包:errcode=1500,只要不抛异常即过,数据不应回退。
            bool failNoThrow = true;
            try { Feed(ctrl, m60, new CliVerify.Pkt().I(1500).C(1).H(0).Bytes()); }
            catch (Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY equipgrowth 15260 fail threw: " + e); }
            bool dataUnchanged = model.GetWholeLv(1) == 5;

            model.Clear();
            bool pass = listOk && activateOk && activateToast && failNoThrow && dataUnchanged;
            Debug.Log("CLIVERIFY equipgrowth whole VERDICT pass=" + pass);
            return pass;
        }
    }
}
