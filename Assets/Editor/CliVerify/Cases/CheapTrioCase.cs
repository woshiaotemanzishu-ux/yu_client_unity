using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Halo(514xx)+FairyWish(513xx)+RedPacket(339xx)三小合包实证(自动循环 轮18 PK2)。反射喂包驱动三个
    /// Controller 的私有 On 处理体,断言 Model 落地字段/事件 + 注册线(含 33903 空消费、33905 老端不可达事务
    /// 严禁注册的负向核实)
    /// + config 计数。纯逻辑用例(无壳渲染/截图),复用 CliVerify.Stage/Pkt(均已 public),不改 CliVerify.cs
    /// 本体(主控统一接 RenderAll)。独立文件避免多代理改 CliVerify.cs 冲突。日志前缀统一 "CLIVERIFY cheaptrio"。
    /// </summary>
    public static class CheapTrioCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);

                bool haloOk = await RunHaloAsync();
                bool fairyOk = await RunFairyWishAsync();
                bool redOk = await RunRedPacketAsync();

                bool pass = haloOk && fairyOk && redOk;
                Debug.Log("CLIVERIFY cheaptrio VERDICT halo=" + haloOk + " fairywish=" + fairyOk
                    + " redpacket=" + redOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        // ============================================================================================
        // Halo(514xx)
        // ============================================================================================
        private static async Task<bool> RunHaloAsync()
        {
            await Shenxiao.Module.Core.Halo.HaloConfigs.EnsureLoaded();
            bool configOk = Shenxiao.Module.Core.Halo.HaloConfigs.IsLoaded && Shenxiao.Module.Core.Halo.HaloConfigs.Count == 9;
            Debug.Log("CLIVERIFY cheaptrio halo config count=" + Shenxiao.Module.Core.Halo.HaloConfigs.Count + " ok=" + configOk);

            object ctrl = Shenxiao.Module.Core.Halo.HaloController.Instance;
            var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
            if (!baseCtrl.IsInitialized) baseCtrl.Init();

            bool regOk = CheckRegistered("halo", new[]
            {
                Shenxiao.Framework.Net.Proto.HALO_INFO, Shenxiao.Framework.Net.Proto.HALO_REWARD_RECEIVE,
                Shenxiao.Framework.Net.Proto.HALO_SETTING_UPDATE,
            }, null);

            Shenxiao.Module.Core.Halo.HaloModel model = Shenxiao.Module.Core.Halo.HaloModel.Instance;
            model.Reset();

            bool anyThrew = false;
            void Feed(string method, byte[] pkt)
            {
                MethodInfo m = ctrl.GetType().GetMethod(method, F);
                if (m == null) { Debug.LogError("CLIVERIFY cheaptrio halo handler missing: " + method); anyThrew = true; return; }
                try { m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) }); }
                catch (System.Exception e) { anyThrew = true; Debug.LogError("CLIVERIFY cheaptrio halo " + method + " threw: " + e); }
            }

            int updateCount = 0;
            System.Action<int> onUpdate = (protoId) => updateCount++;
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_HALO_UPDATE, onUpdate);

            // 51400 全量:EndTime=2000000000(u32 范围内), Rewards=2条, SettingList=2条(嵌套探针:HaloId/Type/State 三元组顺序)。
            byte[] p51400 = new CliVerify.Pkt().I(2000000000)
                .H(2)
                    .I(1).C(1)     // reward id=1 state=1
                    .I(3).C(0)     // reward id=3 state=0
                .H(2)
                    .H(3).H(0).C(1)   // haloId=3(ArenaSweep) type=0 state=1
                    .H(5).H(2).C(0)   // haloId=5(DungeonSweep) type=2 state=0
                .Bytes();
            Feed("On51400", p51400);
            bool b51400 = model.HasData && model.Rewards.Count == 2 && model.GetSetting(3, 0) == 1 && model.GetSetting(5, 2) == 0;
            Debug.Log("CLIVERIFY cheaptrio halo 51400 hasData=" + model.HasData + " rewards=" + model.Rewards.Count
                + " setting(3,0)=" + model.GetSetting(3, 0) + " setting(5,2)=" + model.GetSetting(5, 2) + " ok=" + b51400);

            // 51400 边界:空数组(0,0),不应抛异常。
            byte[] p51400Empty = new CliVerify.Pkt().I(0).H(0).H(0).Bytes();
            Feed("On51400", p51400Empty);
            bool b51400Empty = model.HasData && model.Rewards.Count == 0 && !anyThrew;
            Debug.Log("CLIVERIFY cheaptrio halo 51400 empty rewards=" + model.Rewards.Count + " noThrow=" + !anyThrew + " ok=" + b51400Empty);

            // 重灌一份全量供后续 51401/51402 断言基线。
            Feed("On51400", p51400);

            // 51401 成功:Id=99(新增), State=1, Errcode=1(末尾)。
            byte[] p51401Ok = new CliVerify.Pkt().I(99).C(1).I(1).Bytes();
            Feed("On51401", p51401Ok);
            bool b51401Ok = model.Rewards.Any(e => e.Id == 99 && e.State == 1);
            Debug.Log("CLIVERIFY cheaptrio halo 51401 ok found99=" + b51401Ok);

            // 51401"失败"(Errcode=5,非1):m1修复后老端镜像=不判 errcode 无条件套值(HaloController.ts:90-93
            // 直接 ShowHaloReward,不判 errcode),失败包同样落地 State=0;仅额外弹失败toast是本端单向加强。
            byte[] p51401Fail = new CliVerify.Pkt().I(99).C(0).I(5).Bytes();
            bool b51401FailNoThrow = true;
            try { Feed("On51401", p51401Fail); } catch (System.Exception e) { b51401FailNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio halo 51401 fail threw: " + e); }
            bool b51401Applied = model.Rewards.Any(e => e.Id == 99 && e.State == 0);
            Debug.Log("CLIVERIFY cheaptrio halo 51401 fail noThrow=" + b51401FailNoThrow + " applied(state=0)=" + b51401Applied);

            // 51402 成功:haloId=6(GodBeastComposite), type=0, state=1, Errcode=1(末尾)。
            byte[] p51402Ok = new CliVerify.Pkt().H(6).H(0).C(1).I(1).Bytes();
            Feed("On51402", p51402Ok);
            bool b51402Ok = model.GetSetting(6, 0) == 1;
            Debug.Log("CLIVERIFY cheaptrio halo 51402 ok setting(6,0)=" + model.GetSetting(6, 0));

            // 51402"失败"(Errcode=7,非1):m1修复后同样不判 errcode 无条件套值(HaloController.ts:96-99),
            // 失败包同样落地 state=0,覆盖此前的 1。
            byte[] p51402Fail = new CliVerify.Pkt().H(6).H(0).C(0).I(7).Bytes();
            bool b51402FailNoThrow = true;
            try { Feed("On51402", p51402Fail); } catch (System.Exception e) { b51402FailNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio halo 51402 fail threw: " + e); }
            bool b51402Applied = model.GetSetting(6, 0) == 0;
            Debug.Log("CLIVERIFY cheaptrio halo 51402 fail noThrow=" + b51402FailNoThrow + " applied(state=0)=" + b51402Applied);

            Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_HALO_UPDATE, onUpdate);
            model.Reset();

            bool pass = configOk && regOk && b51400 && b51400Empty && !anyThrew && b51401Ok && b51401FailNoThrow && b51401Applied
                && b51402Ok && b51402FailNoThrow && b51402Applied && updateCount > 0;
            Debug.Log("CLIVERIFY cheaptrio halo VERDICT config=" + configOk + " reg=" + regOk + " p51400=" + b51400
                + " p51401=" + b51401Ok + " p51402=" + b51402Ok + " updateEvents=" + updateCount + " pass=" + pass);
            return pass;
        }

        // ============================================================================================
        // FairyWish(513xx)
        // ============================================================================================
        private static async Task<bool> RunFairyWishAsync()
        {
            await Shenxiao.Module.Core.FairyWish.FairyWishConfigs.EnsureLoaded();
            bool configOk = Shenxiao.Module.Core.FairyWish.FairyWishConfigs.IsLoaded
                && Shenxiao.Module.Core.FairyWish.FairyWishConfigs.FairyCount == 5
                && Shenxiao.Module.Core.FairyWish.FairyWishConfigs.NodeCount == 250;
            Debug.Log("CLIVERIFY cheaptrio fairywish config fairy=" + Shenxiao.Module.Core.FairyWish.FairyWishConfigs.FairyCount
                + " node=" + Shenxiao.Module.Core.FairyWish.FairyWishConfigs.NodeCount + " ok=" + configOk);

            object ctrl = Shenxiao.Module.Core.FairyWish.FairyWishController.Instance;
            var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
            if (!baseCtrl.IsInitialized) baseCtrl.Init();

            bool regOk = CheckRegistered("fairywish", new[]
            {
                Shenxiao.Framework.Net.Proto.FAIRYWISH_INFO, Shenxiao.Framework.Net.Proto.FAIRYWISH_NODE_ACTIVATE,
                Shenxiao.Framework.Net.Proto.FAIRYWISH_CLICK_PUSH,
            }, null);

            // 51302 是入口红点确认，不是购买/激活。仅 Bubble 态第一次触碰发送，之后状态机不再发送。
            MethodInfo touchMethod = ctrl.GetType().GetMethod("ConfirmEntryTouch", BindingFlags.Public | BindingFlags.Instance);
            bool touchMethodOk = touchMethod != null;
            Shenxiao.Module.Core.FairyWish.FairyWishModel.Instance.SetEntryRedStateForAuthority(1001,
                Shenxiao.Module.Core.FairyWish.FairyWishModel.EntryRedState.Bubble);
            bool touchNoThrow = true;
            try { Shenxiao.Module.Core.FairyWish.FairyWishController.Instance.ConfirmEntryTouch(1001); }
            catch (System.Exception e) { touchNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio fairywish ConfirmEntryTouch threw: " + e); }
            Debug.Log("CLIVERIFY cheaptrio fairywish 51302 touchMethodOk=" + touchMethodOk + " noThrow=" + touchNoThrow);

            Shenxiao.Module.Core.FairyWish.FairyWishModel model = Shenxiao.Module.Core.FairyWish.FairyWishModel.Instance;
            model.Reset();

            bool anyThrew = false;
            void Feed(string method, byte[] pkt)
            {
                MethodInfo m = ctrl.GetType().GetMethod(method, F);
                if (m == null) { Debug.LogError("CLIVERIFY cheaptrio fairywish handler missing: " + method); anyThrew = true; return; }
                try { m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) }); }
                catch (System.Exception e) { anyThrew = true; Debug.LogError("CLIVERIFY cheaptrio fairywish " + method + " threw: " + e); }
            }

            int updateCount = 0;
            System.Action<int> onUpdate = (fairyId) => updateCount++;
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_FAIRYWISH_UPDATE, onUpdate);

            // 51300:FairyId=1001, IsBuy=1, NodeList=2条(嵌套探针:NodeId/IsActivate/Combat 三元组顺序)。
            byte[] p51300 = new CliVerify.Pkt().I(1001).C(1)
                .H(2)
                    .I(1).C(1).I(100)   // node1 activated combat=100
                    .I(2).C(0).I(50)    // node2 not activated combat=50
                .Bytes();
            Feed("On51300", p51300);
            var fairy1001 = model.GetFairy(1001);
            bool b51300 = fairy1001 != null && fairy1001.IsBuy == 1 && fairy1001.NodeList.Count == 2
                && fairy1001.NodeList[0].Combat == 100 && fairy1001.NodeList[1].IsActivate == 0;
            Debug.Log("CLIVERIFY cheaptrio fairywish 51300 fairyId=1001 isBuy=" + fairy1001?.IsBuy
                + " nodes=" + fairy1001?.NodeList.Count + " ok=" + b51300);

            // 51300 边界:FairyId=1002, IsBuy=0, NodeList 空数组——不应抛异常。
            byte[] p51300Empty = new CliVerify.Pkt().I(1002).C(0).H(0).Bytes();
            Feed("On51300", p51300Empty);
            var fairy1002 = model.GetFairy(1002);
            bool b51300Empty = fairy1002 != null && fairy1002.NodeList.Count == 0 && !anyThrew;
            Debug.Log("CLIVERIFY cheaptrio fairywish 51300 empty fairyId=1002 nodes=" + fairy1002?.NodeList.Count + " noThrow=" + !anyThrew);

            // 51301 成功:FairyId=1001, NodeId=2(当前未激活), Code=1(末尾)——应翻转 is_activate。
            byte[] p51301Ok = new CliVerify.Pkt().I(1001).I(2).I(1).Bytes();
            Feed("On51301", p51301Ok);
            bool b51301Ok = model.GetFairy(1001).NodeList[1].IsActivate == 1;
            Debug.Log("CLIVERIFY cheaptrio fairywish 51301 ok node2 isActivate=" + model.GetFairy(1001).NodeList[1].IsActivate);

            // 51301 失败:同节点再次强化但 Code=5(非1)——应保持已激活状态不变,不抛异常。
            byte[] p51301Fail = new CliVerify.Pkt().I(1001).I(2).I(5).Bytes();
            bool b51301FailNoThrow = true;
            try { Feed("On51301", p51301Fail); } catch (System.Exception e) { b51301FailNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio fairywish 51301 fail threw: " + e); }
            bool b51301Unchanged = model.GetFairy(1001).NodeList[1].IsActivate == 1;
            Debug.Log("CLIVERIFY cheaptrio fairywish 51301 fail noThrow=" + b51301FailNoThrow + " unchanged=" + b51301Unchanged);

            // 51303 recv-only 点击推送(嵌套探针):FairyId=1001,Times=3;FairyId=1002,Times=0。
            byte[] p51303 = new CliVerify.Pkt().H(2)
                    .I(1001).C(3)
                    .I(1002).C(0)
                .Bytes();
            Feed("On51303", p51303);
            bool b51303 = model.GetClickTimes(1001) == 3 && model.GetClickTimes(1002) == 0;
            Debug.Log("CLIVERIFY cheaptrio fairywish 51303 times1001=" + model.GetClickTimes(1001)
                + " times1002=" + model.GetClickTimes(1002) + " ok=" + b51303);

            Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_FAIRYWISH_UPDATE, onUpdate);
            model.Reset();

            bool pass = configOk && regOk && touchMethodOk && touchNoThrow && !anyThrew
                && b51300 && b51300Empty && b51301Ok && b51301FailNoThrow && b51301Unchanged && b51303 && updateCount > 0;
            Debug.Log("CLIVERIFY cheaptrio fairywish VERDICT config=" + configOk + " reg=" + regOk + " touch=" + touchMethodOk
                + " p51300=" + b51300 + " p51301=" + b51301Ok + " p51303=" + b51303 + " updateEvents=" + updateCount + " pass=" + pass);
            return pass;
        }

        // ============================================================================================
        // RedPacket(339xx)
        // ============================================================================================
        private static async Task<bool> RunRedPacketAsync()
        {
            await Shenxiao.Module.Core.RedPacket.RedPacketConfigs.EnsureLoaded();
            bool configOk = Shenxiao.Module.Core.RedPacket.RedPacketConfigs.IsLoaded
                && Shenxiao.Module.Core.RedPacket.RedPacketConfigs.Count == 16
                && Shenxiao.Module.Core.RedPacket.RedPacketConfigs.GoodsCount == 3;
            Debug.Log("CLIVERIFY cheaptrio redpacket config count=" + Shenxiao.Module.Core.RedPacket.RedPacketConfigs.Count
                + " goods=" + Shenxiao.Module.Core.RedPacket.RedPacketConfigs.GoodsCount + " ok=" + configOk);

            object ctrl = Shenxiao.Module.Core.RedPacket.RedPacketController.Instance;
            var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
            if (!baseCtrl.IsInitialized) baseCtrl.Init();

            // 正向注册线 + 负向核实(33903 空消费、33905 老端不可达写事务均严禁注册)。
            bool regOk = CheckRegistered("redpacket", new[]
            {
                Shenxiao.Framework.Net.Proto.REDPACKET_ERROR, Shenxiao.Framework.Net.Proto.REDPACKET_LIST,
                Shenxiao.Framework.Net.Proto.REDPACKET_OPEN, Shenxiao.Framework.Net.Proto.REDPACKET_SEND,
                Shenxiao.Framework.Net.Proto.REDPACKET_SEND_VIP, Shenxiao.Framework.Net.Proto.REDPACKET_NEW_PUSH,
                Shenxiao.Framework.Net.Proto.REDPACKET_TAKEN_PUSH,
            }, new[] { 33903, 33905 });

            Shenxiao.Module.Core.RedPacket.RedPacketModel model = Shenxiao.Module.Core.RedPacket.RedPacketModel.Instance;
            model.Reset();

            bool anyThrew = false;
            void Feed(string method, byte[] pkt)
            {
                MethodInfo m = ctrl.GetType().GetMethod(method, F);
                if (m == null) { Debug.LogError("CLIVERIFY cheaptrio redpacket handler missing: " + method); anyThrew = true; return; }
                try { m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) }); }
                catch (System.Exception e) { anyThrew = true; Debug.LogError("CLIVERIFY cheaptrio redpacket " + method + " threw: " + e); }
            }

            int updateCount = 0;
            System.Action<long> onUpdate = (id) => updateCount++;
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_REDPACKET_UPDATE, onUpdate);
            int resultCount = 0; int lastResultProto = 0; int lastResultCode = 0;
            System.Action<int, int> onResult = (protoId, code) => { resultCount++; lastResultProto = protoId; lastResultCode = code; };
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_REDPACKET_RESULT, onResult);

            // 33901:RedEnvelopesList 2条 + RecordList 2条(并行数组探针;字符串往返 ReadString/S() 核实)。
            byte[] p33901 = new CliVerify.Pkt()
                .H(2)
                    .L(1001).L(9001).S("张三").C(1).C(0).C(1).S("pic1").I(1).C(0).I(0).C(1).C(0).H(5).H(0).S("恭喜发财").I(1700000000)
                    .L(1002).L(9002).S("李四").C(2).C(1).C(0).S("pic2").I(2).C(1).I(0).C(0).C(0).H(3).H(3).S("").I(1700000100)
                .H(2)
                    .I(1).S("王五").I(42060002).I(1700000200)
                    .I(2).S("赵六").I(4203004).I(1700000300)
                .Bytes();
            Feed("On33901", p33901);
            bool b33901 = model.HasData && model.List.Count == 2 && model.Records.Count == 2
                && model.List[0].RoleName == "张三" && model.List[0].Id == 1001
                && model.Records[1].RoleName == "赵六" && model.Records[1].CfgId == 4203004;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33901 list=" + model.List.Count + " records=" + model.Records.Count
                + " name0=" + model.List[0].RoleName + " ok=" + b33901);

            // 33902 打开详情(嵌套探针,15标量字段+RecipientList 2条,字段顺序回 pt_339.erl:65-112 原文核):
            // RedEnvelopesId=1001(命中 33901 已灌入的列表项,验证列表联动 ReceiveStatus/RecipientsNum 刷新)。
            byte[] p33902 = new CliVerify.Pkt()
                .L(1001).L(9001).S("张三").C(1).C(0).C(1).S("pic1").I(1).C(1).I(500).H(5).H(2).I(2000).C(1).I(0)
                .H(2)
                    .L(8001).S("甲").C(1).C(0).C(0).S("picA").I(1).I(300).I(1700000400)
                    .L(8002).S("乙").C(2).C(1).C(1).S("picB").I(2).I(200).I(1700000500)
                .Bytes();
            Feed("On33902", p33902);
            var detail = model.LastOpenDetail;
            bool b33902 = detail != null && detail.RedEnvelopesId == 1001 && detail.RecipientList.Count == 2
                && detail.RecipientList[0].RoleName == "甲" && detail.RecipientList[1].ReceiveMoney == 200
                && model.List[0].ReceiveStatus == 1 && model.List[0].RecipientsNum == 2;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33902 id=" + detail?.RedEnvelopesId + " recipients=" + detail?.RecipientList.Count
                + " listReceiveStatus=" + model.List[0].ReceiveStatus + " ok=" + b33902);

            // 33904 成功:Errcode=1 → 应触发 RequestList 回补(no-throw,headless SendFmt 安全 no-op)。
            byte[] p33904Ok = new CliVerify.Pkt().I(1).Bytes();
            Feed("On33904", p33904Ok);
            bool b33904Ok = resultCount >= 1 && lastResultProto == Shenxiao.Framework.Net.Proto.REDPACKET_SEND && lastResultCode == 1 && !anyThrew;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33904 ok resultProto=" + lastResultProto + " code=" + lastResultCode);

            // 33904 失败:Errcode=5——显码降级,不抛异常。
            byte[] p33904Fail = new CliVerify.Pkt().I(5).Bytes();
            bool b33904FailNoThrow = true;
            try { Feed("On33904", p33904Fail); } catch (System.Exception e) { b33904FailNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio redpacket 33904 fail threw: " + e); }
            bool b33904Fail = lastResultCode == 5 && b33904FailNoThrow;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33904 fail code=" + lastResultCode + " noThrow=" + b33904FailNoThrow);

            // 33906 三分支:errcode==1(理论不可达但代码路径需 no-throw)。
            byte[] p33906Code1 = new CliVerify.Pkt().I(1).S("").Bytes();
            bool b33906Code1NoThrow = true;
            try { Feed("On33906", p33906Code1); } catch (System.Exception e) { b33906Code1NoThrow = false; Debug.LogError("CLIVERIFY cheaptrio redpacket 33906 code1 threw: " + e); }

            // 33906 真成功:errcode=3390012, args="5"(剩余次数)。
            byte[] p33906Real = new CliVerify.Pkt().I(3390012).S("5").Bytes();
            Feed("On33906", p33906Real);
            bool b33906Real = resultCount >= 1 && lastResultProto == Shenxiao.Framework.Net.Proto.REDPACKET_SEND_VIP && lastResultCode == 3390012;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33906 real success code=" + lastResultCode);

            // 33906 失败:errcode=999, args="err339_xxx"。
            byte[] p33906Fail = new CliVerify.Pkt().I(999).S("err339_split_max_num_err").Bytes();
            bool b33906FailNoThrow = true;
            try { Feed("On33906", p33906Fail); } catch (System.Exception e) { b33906FailNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio redpacket 33906 fail threw: " + e); }
            bool b33906Fail = lastResultCode == 999 && b33906FailNoThrow;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33906 fail code=" + lastResultCode + " noThrow=" + b33906FailNoThrow);

            // 33907 新增推送:1条新记录(Id=1003,与既有列表不重复)——列表应增至3条。
            byte[] p33907 = new CliVerify.Pkt().H(1)
                    .L(1003).L(9003).S("孙七").C(1).C(0).C(0).S("pic3").I(1).C(0).I(0).C(0).C(0).H(10).H(0).S("新春快乐").I(1700000600)
                .Bytes();
            Feed("On33907", p33907);
            bool b33907 = model.List.Count == 3 && model.List[2].Id == 1003;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33907 count=" + model.List.Count + " ok=" + b33907);

            // 33908 领完推送:Id=1002(既有列表第2条,Status 应置2)。
            byte[] p33908 = new CliVerify.Pkt().L(1002).Bytes();
            Feed("On33908", p33908);
            var taken = model.List.FirstOrDefault(e => e.Id == 1002);
            bool b33908 = taken != null && taken.Status == 2 && taken.RecipientsNum == taken.TotalNum;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33908 id=1002 status=" + taken?.Status + " ok=" + b33908);

            // 33900 通用错误码推送:no-throw + 事件触发。
            byte[] p33900 = new CliVerify.Pkt().I(400).Bytes();
            int updateBefore = updateCount;
            bool b33900NoThrow = true;
            try { Feed("On33900", p33900); } catch (System.Exception e) { b33900NoThrow = false; Debug.LogError("CLIVERIFY cheaptrio redpacket 33900 threw: " + e); }
            bool b33900 = b33900NoThrow && updateCount > updateBefore;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33900 noThrow=" + b33900NoThrow + " eventFired=" + (updateCount > updateBefore));

            // 边界:33901 空表(0,0)不应抛异常,列表清空。
            byte[] p33901Empty = new CliVerify.Pkt().H(0).H(0).Bytes();
            bool b33901EmptyNoThrow = true;
            try { Feed("On33901", p33901Empty); } catch (System.Exception e) { b33901EmptyNoThrow = false; Debug.LogError("CLIVERIFY cheaptrio redpacket 33901 empty threw: " + e); }
            bool b33901Empty = b33901EmptyNoThrow && model.List.Count == 0 && model.Records.Count == 0;
            Debug.Log("CLIVERIFY cheaptrio redpacket 33901 empty list=" + model.List.Count + " ok=" + b33901Empty);

            Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_REDPACKET_UPDATE, onUpdate);
            Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_REDPACKET_RESULT, onResult);
            model.Reset();

            bool pass = configOk && regOk && !anyThrew
                && b33901 && b33902 && b33904Ok && b33904Fail
                && b33906Code1NoThrow && b33906Real && b33906Fail
                && b33907 && b33908 && b33900 && b33901Empty;
            Debug.Log("CLIVERIFY cheaptrio redpacket VERDICT config=" + configOk + " reg=" + regOk + " p33901=" + b33901
                + " p33902=" + b33902 + " p33904=" + (b33904Ok && b33904Fail) + " p33906=" + (b33906Real && b33906Fail)
                + " p33907=" + b33907 + " p33908=" + b33908 + " p33900=" + b33900 + " pass=" + pass);
            return pass;
        }

        // ============================================================================================
        // 注册线核实(NetManager._handlers):mustReg 全部命中 + mustNotReg 全部缺席(死号严禁注册的负向断言)。
        // ============================================================================================
        private static bool CheckRegistered(string tag, int[] mustReg, int[] mustNotReg)
        {
            FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
            var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
            bool ok = handlers != null;
            var missing = new List<int>();
            var leaked = new List<int>();
            if (handlers != null)
            {
                if (mustReg != null)
                    foreach (int id in mustReg) if (!handlers.Contains(id)) { ok = false; missing.Add(id); }
                if (mustNotReg != null)
                    foreach (int id in mustNotReg) if (handlers.Contains(id)) { ok = false; leaked.Add(id); }
            }
            Debug.Log("CLIVERIFY cheaptrio " + tag + " 注册线核实(NetManager._handlers) missing=["
                + string.Join(",", missing) + "] deadRegistered=[" + string.Join(",", leaked) + "] ok=" + ok);
            return ok;
        }
    }
}
