using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 婚姻(征友/戒指/结婚,自动循环 轮16)实证:pt_172 172xx(征友17200-05/戒指17210-13/求婚·结婚·离婚·
    /// 秀恩爱17222-40/副本匹配邀请17245-97)+ 223xx 鲜花(22300-05)共 33 号,合成包驱动 MarriageController
    /// 反射喂包,断言 MarriageModel 落地字段/事件 + config 四表计数(模板 KfBossCase,纯逻辑段)。
    ///
    /// 重点覆盖:17200 bin_0 全字段(**无 CombatPower**)+ tag_list 嵌套数组;17226 双数组(biaobai_list +
    /// biaobai_answer_list,CombatPower u64)必须都读完保游标,断言两数组精确字段值(错位会导致字段值不匹配
    /// 而非崩溃,故用不易碰撞的探针值断言);17222 CombatPower **u32** 独例(勿与17226/17232的u64混淆);
    /// 17205/17224/17226/17229/17238/17296/17297 无 Code 前导帧;17246 与 r16 侦察报告"无Code"结论不同,
    /// 本端核对 ClientProtocol.json + 老端 on17246 实读 scmd.code 后订正为带 Code(见 Proto.cs/Controller 注释);
    /// 17210 双数组(polish_list+attr_list);17232 三成功码(1/1720012/1012)+ FigureProto 全字段;17239
    /// ObjectList 奖励;22302 收礼记录全量。17212 戒指单步是死号,只注册防御 recv、断言无公开发送方法。
    ///
    /// UI 层:14 个 MarriageModule 相关 View 已烤 Bind 但空壳,本轮数据层only不接 View(同 15a/15b Boss 先例,
    /// 纯数据层轮无渲染段)。
    /// </summary>
    public static class MarriageCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PF = BindingFlags.Public | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            bool editorPreferFallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Marriage.MarriageConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Marriage.MarriageConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY marriage FAIL MarriageConfigs not loaded");
                    return 3;
                }
                bool configOk = Shenxiao.Module.Core.Marriage.MarriageConfigs.ConstantCount == 41
                    && Shenxiao.Module.Core.Marriage.MarriageConfigs.RingStarCount == 501 // 实测501条(侦察稿"~101"估算误,stage@star组合键)
                    && Shenxiao.Module.Core.Marriage.MarriageConfigs.FlowerToolsCount == 6
                    && Shenxiao.Module.Core.Marriage.MarriageConfigs.LoveDsgtCount == 10;
                Debug.Log("CLIVERIFY marriage config constant=" + Shenxiao.Module.Core.Marriage.MarriageConfigs.ConstantCount
                    + " ringStar=" + Shenxiao.Module.Core.Marriage.MarriageConfigs.RingStarCount
                    + " flowerTools=" + Shenxiao.Module.Core.Marriage.MarriageConfigs.FlowerToolsCount
                    + " loveDsgt=" + Shenxiao.Module.Core.Marriage.MarriageConfigs.LoveDsgtCount + " ok=" + configOk);

                Shenxiao.Module.Core.Marriage.MarriageModel model = Shenxiao.Module.Core.Marriage.MarriageModel.Instance;
                model.Clear();

                object ctrl = Shenxiao.Module.Core.Marriage.MarriageController.Instance;
                System.Type t = ctrl.GetType();
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY marriage handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }

                // ---- A. 死号 17212 断言:无公开发送方法(老端零发送点,只注册防御 recv) ----
                bool deadSendOk = t.GetMethod("RequestRingUpgradeStep", PF) == null
                    && t.GetMethod("RingUpgradeStep", PF) == null;
                bool deadRecvOk = t.GetMethod("On17212", F) != null;
                Debug.Log("CLIVERIFY marriage 17212死号 noSendMethod=" + deadSendOk + " hasDefensiveRecv=" + deadRecvOk);

                // ---- B. 17200 征友大厅(bin_0 全字段,**无CombatPower**,tag_list嵌套) ----
                byte[] p17200 = new CliVerify.Pkt().I(1).C(1).I(100).I(1700000000).I(1700000100).C(3)
                    .H(1)
                        .L(9001).S("甲").H(170).C(1).I(1001).C(2).C(1).C(0).S("pic1").I(3).C(1).I(500).S("你好").C(2)
                        .I(1700000200).C(1).C(0).I(88)
                        .H(1).C(5).C(6) // tag_list 1条
                        .I(2000).C(1).C(0)
                    .Bytes();
                Feed("On17200", p17200);
                var page1 = model.GetPersonalsPage(1);
                bool b17200 = page1 != null && page1.OwnPopularity == 100 && page1.LessFreeTimes == 3
                    && page1.PlayerList.Count == 1 && page1.PlayerList[0].RoleId == 9001 && page1.PlayerList[0].Name == "甲"
                    && page1.PlayerList[0].Lv == 170 && page1.PlayerList[0].TagList.Count == 1
                    && page1.PlayerList[0].TagList[0].TagId == 5 && page1.PlayerList[0].TagList[0].TagSubId == 6
                    && page1.PlayerList[0].VipExp == 2000 && page1.PlayerList[0].IsSupvip == 0;
                // 失败码边界:code!=1,页数据不应落地
                Feed("On17200", new CliVerify.Pkt().I(1720099).C(9).I(0).I(0).I(0).C(0).H(0).Bytes());
                bool b17200fail = model.GetPersonalsPage(9) == null;
                Debug.Log("CLIVERIFY marriage 17200 大厅 page1N=" + (page1?.PlayerList.Count ?? -1) + " ok=" + b17200 + " failNotStored=" + b17200fail);

                // ---- C. 17201 关注 / 17202 发布(变长发送侧仅需不抛) ----
                Feed("On17201", new CliVerify.Pkt().I(1).L(9002).C(1).Bytes());
                Feed("On17201", new CliVerify.Pkt().I(1720067).L(9003).C(1).Bytes()); // err172_personals_cant_follow_self
                Feed("On17202", new CliVerify.Pkt().I(1).C(0).Bytes());
                Feed("On17202", new CliVerify.Pkt().I(1720001).C(0).Bytes());
                bool b1720102NoThrow = true;
                try
                {
                    var tags = new List<(int, int)> { (1, 2), (3, 4) };
                    ((Shenxiao.Module.Core.Marriage.MarriageController)ctrl).RequestIssue("测试征友", 1, tags);
                    ((Shenxiao.Module.Core.Marriage.MarriageController)ctrl).RequestPersonalsList(1);
                    ((Shenxiao.Module.Core.Marriage.MarriageController)ctrl).RequestFollow(9002, 1);
                }
                catch (System.Exception e) { b1720102NoThrow = false; Debug.LogError("CLIVERIFY marriage 17201/02 send threw: " + e); }
                Debug.Log("CLIVERIFY marriage 17201/17202 关注/发布 sendNoThrow=" + b1720102NoThrow);

                // ---- D. 17205 玩家细节(**无Code前缀独例**) ----
                Feed("On17205", new CliVerify.Pkt().L(9004).L(500001).S("测试公会").Bytes());
                bool b17205 = model.LastRoleDetail != null && model.LastRoleDetail.RoleId == 9004
                    && model.LastRoleDetail.GuildId == 500001 && model.LastRoleDetail.GuildName == "测试公会";
                Debug.Log("CLIVERIFY marriage 17205 玩家细节(无Code) ok=" + b17205);

                // ---- E. 17210 戒指信息(双数组:polish_list+attr_list) ----
                byte[] p17210 = new CliVerify.Pkt().I(1).C(3).C(5).I(120).I(9999)
                    .H(1).I(23020001).H(10)
                    .H(2).I(19).I(100).I(20).I(100)
                    .Bytes();
                Feed("On17210", p17210);
                bool b17210 = model.HasRing && model.Ring.Stage == 3 && model.Ring.Star == 5 && model.Ring.PrayNum == 120
                    && model.Ring.RingCombatPower == 9999 && model.Ring.PolishList.Count == 1
                    && model.Ring.PolishList[0].GoodsTypeId == 23020001 && model.Ring.PolishList[0].UseNum == 10
                    && model.Ring.AttrList.Count == 2 && model.Ring.AttrList[1].AttrType == 20 && model.Ring.AttrList[1].AttrNum == 100;
                // B3 失败码边界:code!=1(1720001 ring_not_polish),Ring 不应被覆盖(stage/star/power 仍是成功值)
                Feed("On17210", new CliVerify.Pkt().I(1720001).C(9).C(9).I(999).I(888).H(0).H(0).Bytes());
                bool b17210fail = model.Ring.Stage == 3 && model.Ring.Star == 5 && model.Ring.RingCombatPower == 9999;
                Debug.Log("CLIVERIFY marriage 17210 戒指信息 stage=" + (model.Ring?.Stage ?? -1) + " polishN="
                    + (model.Ring?.PolishList.Count ?? -1) + " attrN=" + (model.Ring?.AttrList.Count ?? -1) + " ok=" + b17210 + " failNotOverwritten=" + b17210fail);

                // ---- F. 17211解锁/17213一键提升(原地更新stage/star/prayNum) ----
                Feed("On17211", new CliVerify.Pkt().I(1).C(1).C(0).I(0).Bytes());
                bool b17211 = model.Ring.Stage == 1 && model.Ring.Star == 0;
                // B3 失败码边界:code!=1(1720091 ring_unlock),不应覆盖 stage/star
                Feed("On17211", new CliVerify.Pkt().I(1720091).C(9).C(9).I(999).Bytes());
                bool b17211fail = model.Ring.Stage == 1 && model.Ring.Star == 0;
                Feed("On17213", new CliVerify.Pkt().I(1).C(1).C(4).I(30).Bytes());
                bool b17213 = model.Ring.Stage == 1 && model.Ring.Star == 4 && model.Ring.PrayNum == 30;
                // B3 失败码边界:code!=1(1720001 ring_not_polish),不应覆盖 stage/star/prayNum
                Feed("On17213", new CliVerify.Pkt().I(1720001).C(9).C(9).I(999).Bytes());
                bool b17213fail = model.Ring.Stage == 1 && model.Ring.Star == 4 && model.Ring.PrayNum == 30;
                Debug.Log("CLIVERIFY marriage 17211/17213 戒指解锁/一键提升 ok=" + (b17211 && b17213)
                    + " failNotOverwritten=" + (b17211fail && b17213fail));

                // ---- G. 17212 死号防御recv(只解析不消费,失败发 STOP_RING_UPGRADE,不抛) ----
                int stopUpgradeCount = 0;
                System.Action onStop = () => stopUpgradeCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_RING_STOP_UPGRADE, onStop);
                bool b17212NoThrow = true;
                try
                {
                    Feed("On17212", new CliVerify.Pkt().I(1720003).I(23020001).C(1).C(0).I(0).Bytes()); // marriage_not_pray
                }
                catch (System.Exception e) { b17212NoThrow = false; Debug.LogError("CLIVERIFY marriage 17212 threw: " + e); }
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_RING_STOP_UPGRADE, onStop);
                bool b17212 = b17212NoThrow && stopUpgradeCount == 1;
                Debug.Log("CLIVERIFY marriage 17212 死号防御recv noThrow=" + b17212NoThrow + " stopEvents=" + stopUpgradeCount + " ok=" + b17212);

                // ---- H. 17222 推送(无Code,CombatPower **u32**) ----
                byte[] p17222 = new CliVerify.Pkt()
                    .L(9005).S("乙").H(180).I(50000).C(1).I(200).C(3).C(1).S("pic2").I(1).C(2).C(1).S("嫁给我吧").C(0)
                    .H(1).I(2).I(23020001).I(1)
                    .Bytes();
                Feed("On17222", p17222);
                bool b17222 = model.LastPropose != null && model.LastPropose.RoleId == 9005 && model.LastPropose.Name == "乙"
                    && model.LastPropose.CombatPower == 50000 && model.LastPropose.Type == 2 && model.LastPropose.ProposeType == 1
                    && model.LastPropose.CostList.Count == 1 && model.LastPropose.CostList[0].GoodsTypeId == 23020001;
                Debug.Log("CLIVERIFY marriage 17222 求婚推送(无Code,u32) combatPower=" + (model.LastPropose?.CombatPower ?? -1) + " ok=" + b17222);

                // ---- I. 17223 回应求婚(成功后重拉三件套,不抛) ----
                int proposeRespondCount = 0; bool lastProposeRespondOk = false;
                System.Action<bool> onProposeRespond = ok => { proposeRespondCount++; lastProposeRespondOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_PROPOSE_RESPOND_RESULT, onProposeRespond);
                bool b17223NoThrow = true;
                try
                {
                    Feed("On17223", new CliVerify.Pkt().I(1).L(9005).C(1).Bytes());
                    Feed("On17223", new CliVerify.Pkt().I(1720068).L(9005).C(2).Bytes()); // marriage_partner_lv_limit
                }
                catch (System.Exception e) { b17223NoThrow = false; Debug.LogError("CLIVERIFY marriage 17223 threw: " + e); }
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_PROPOSE_RESPOND_RESULT, onProposeRespond);
                bool b17223 = b17223NoThrow && proposeRespondCount == 2 && !lastProposeRespondOk;
                Debug.Log("CLIVERIFY marriage 17223 回应求婚 events=" + proposeRespondCount + " ok=" + b17223);

                // ---- J. 17224 回应结果推送(无Code,双向单播;answer_type==2拒绝无任何反馈——本端镜像不发事件) ----
                int answerPushCount = 0;
                System.Action<long, int, int> onAnswerPush = (rid, type, ans) => answerPushCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_ANSWER_PUSH, onAnswerPush);
                // M-server3 订正:Type=UseType(1=表白/2=求婚/3=离婚,lib_marriage.erl:1224-1227与mod_marriage.erl:1389-1392),非"2=离婚"
                Feed("On17224", new CliVerify.Pkt().L(9005).C(2).C(1).Bytes()); // type=2求婚,answer=1答应 → 应发事件
                bool b17224Accept = answerPushCount == 1;
                Feed("On17224", new CliVerify.Pkt().L(9006).C(2).C(2).Bytes()); // answer=2拒绝 → 老端无反馈,本端不发
                bool b17224Reject = answerPushCount == 1; // 计数不应增加
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_ANSWER_PUSH, onAnswerPush);
                bool b17224 = b17224Accept && b17224Reject;
                Debug.Log("CLIVERIFY marriage 17224 回应结果(无Code) acceptFired=" + b17224Accept + " rejectSilent=" + b17224Reject + " ok=" + b17224);

                // ---- K. 17226 登录表白汇总(无Code,双数组必须都读完保游标,CombatPower u64) ----
                byte[] p17226Request = Shenxiao.Framework.Net.UserMsgAdapter.Encode(
                    Shenxiao.Framework.Net.Proto.MARRIAGE_BIAOBAI_LIST, null, new object[0]);
                bool b17226RequestWire = p17226Request.Length == 6
                    && ((p17226Request[0] << 8) | p17226Request[1]) == 6
                    && ((p17226Request[2] << 8) | p17226Request[3]) == 1000
                    && ((p17226Request[4] << 8) | p17226Request[5]) == 17226;
                logs.Clear();
                ((Shenxiao.Module.Core.Marriage.MarriageController)ctrl).RequestBiaobaiList();
                bool b17226PublicSend = logs.Exists(l => l.Contains("proto=17226"));
                bool b17226Outbound = b17226RequestWire && b17226PublicSend;
                byte[] p17226 = new CliVerify.Pkt()
                    .H(2)
                        .L(9101).S("甲甲").H(101).L(111111).C(1).I(11).C(1).C(1).C(2).C(1).S("求婚msg").C(0)
                            .H(1).I(1).I(23020001).I(1)
                        .L(9102).S("乙乙").H(102).L(222222).C(2).I(22).C(2).C(2).C(4).C(0).S("离婚msg").C(1)
                            .H(0)
                    .H(1)
                        .L(9201).S("丙丙").H(103).L(333333).C(1).I(33).C(3).C(3).C(2).C(1)
                    .Bytes();
                Feed("On17226", p17226);
                bool b17226 = model.HasBiaobai && model.BiaobaiList.Count == 2 && model.BiaobaiAnswerList.Count == 1
                    && model.BiaobaiList[0].RoleId == 9101 && model.BiaobaiList[0].Name == "甲甲" && model.BiaobaiList[0].CombatPower == 111111
                    && model.BiaobaiList[0].CostList.Count == 1 && model.BiaobaiList[0].CostList[0].GoodsTypeId == 23020001
                    && model.BiaobaiList[1].RoleId == 9102 && model.BiaobaiList[1].Name == "乙乙" && model.BiaobaiList[1].CombatPower == 222222
                    && model.BiaobaiList[1].CostList.Count == 0
                    && model.BiaobaiAnswerList[0].RoleId == 9201 && model.BiaobaiAnswerList[0].Name == "丙丙"
                    && model.BiaobaiAnswerList[0].CombatPower == 333333 && model.BiaobaiAnswerList[0].AnswerType == 1;
                Debug.Log("CLIVERIFY marriage 17226 登录表白汇总(双数组游标) biaobaiN=" + model.BiaobaiList.Count
                    + " answerN=" + model.BiaobaiAnswerList.Count + " outboundEmpty=" + b17226Outbound + " ok=" + b17226);

                // ---- L. 17229 键值推送(无Code) ----
                Feed("On17229", new CliVerify.Pkt().H(1).C(1).I(5200).Bytes());
                bool b17229 = model.GetKeyValue(1) == 5200;
                Debug.Log("CLIVERIFY marriage 17229 键值推送(无Code) loveNum=" + model.GetKeyValue(1) + " ok=" + b17229);

                // ---- M. 17231 发送求婚 ----
                int proposeSendCount = 0; bool lastProposeSendOk = false;
                System.Action<bool> onProposeSend = ok => { proposeSendCount++; lastProposeSendOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_PROPOSE_SEND_RESULT, onProposeSend);
                Feed("On17231", new CliVerify.Pkt().I(1).L(9005).Bytes());
                Feed("On17231", new CliVerify.Pkt().I(1720068).L(9005).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_PROPOSE_SEND_RESULT, onProposeSend);
                bool b17231 = proposeSendCount == 2 && !lastProposeSendOk;
                Debug.Log("CLIVERIFY marriage 17231 发送求婚 events=" + proposeSendCount + " ok=" + b17231);

                // ---- N. 17232 我的伴侣(CombatPower u64 + FigureProto + 三成功码) ----
                CliVerify.Pkt p17232a = new CliVerify.Pkt().I(1).L(9005).L(777777);
                AppendFigure(p17232a, "配偶甲", 180, 1, 9005, "配偶甲");
                p17232a.C(2).C(1).I(1700000000).I(999).C(1);
                Feed("On17232", p17232a.Bytes());
                bool b17232code1 = model.HasMate && model.Mate.RoleId == 9005 && model.Mate.CombatPower == 777777
                    && model.Mate.Figure != null && model.Mate.Figure.name == "配偶甲" && model.Mate.Figure.level == 180
                    && model.Mate.LoveNum == 999;
                CliVerify.Pkt p17232b = new CliVerify.Pkt().I(1720012).L(0).L(0);
                AppendFigure(p17232b, "", 0, 0, 0, "");
                p17232b.C(0).C(0).I(0).I(0).C(0);
                Feed("On17232", p17232b.Bytes()); // 1720012=单身,老端仍当成功刷新
                bool b17232single = model.HasMate && model.Mate.RoleId == 0;
                CliVerify.Pkt p17232c = new CliVerify.Pkt().I(1012).L(9006).L(888888);
                AppendFigure(p17232c, "配偶乙", 190, 1, 9006, "配偶乙");
                p17232c.C(2).C(2).I(0).I(500).C(0);
                Feed("On17232", p17232c.Bytes()); // 1012 也当成功
                bool b17232code1012 = model.HasMate && model.Mate.RoleId == 9006 && model.Mate.CombatPower == 888888;
                // B3 边界:非成功码(1720003 marriage_not_pray,非{1,1720012,1012}三码之一)不应刷新伴侣态
                CliVerify.Pkt p17232d = new CliVerify.Pkt().I(1720003).L(9999).L(123456);
                AppendFigure(p17232d, "不应生效", 1, 0, 0, "");
                p17232d.C(0).C(0).I(0).I(0).C(0);
                Feed("On17232", p17232d.Bytes());
                bool b17232notRefreshed = model.Mate.RoleId == 9006 && model.Mate.CombatPower == 888888;
                bool b17232 = b17232code1 && b17232single && b17232code1012 && b17232notRefreshed;
                Debug.Log("CLIVERIFY marriage 17232 我的伴侣(u64+Figure+三成功码) code1=" + b17232code1
                    + " code1720012=" + b17232single + " code1012=" + b17232code1012
                    + " failNotRefreshed=" + b17232notRefreshed + " ok=" + b17232);

                // ---- O. 17234 发送离婚(成功后重拉伴侣)/17235 回应离婚 ----
                int divorceCount = 0; bool lastDivorceOk = false;
                System.Action<bool> onDivorce = ok => { divorceCount++; lastDivorceOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_DIVORCE_RESULT, onDivorce);
                Feed("On17234", new CliVerify.Pkt().I(1).Bytes());
                Feed("On17234", new CliVerify.Pkt().I(1720078).Bytes()); // divorce_lover_not_online
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_DIVORCE_RESULT, onDivorce);
                bool b17234 = divorceCount == 2 && !lastDivorceOk;
                Feed("On17235", new CliVerify.Pkt().I(1).C(1).Bytes());
                Feed("On17235", new CliVerify.Pkt().I(1720001).C(2).Bytes());
                Debug.Log("CLIVERIFY marriage 17234/17235 离婚发送/回应 events=" + divorceCount + " ok=" + b17234);

                // ---- P. 17236 领取恩爱称号 ----
                Feed("On17236", new CliVerify.Pkt().I(1).C(3).Bytes());
                bool b17236 = model.LastDsgtId == 3;
                Feed("On17236", new CliVerify.Pkt().I(1720001).C(0).Bytes());
                Debug.Log("CLIVERIFY marriage 17236 领取恩爱称号 lastId=" + model.LastDsgtId + " ok=" + b17236);

                // ---- Q. 17237买礼包/17238礼包信息(无Code)/17239领取(ObjectList)/17240请对方买 ----
                int giftBuyCount = 0; bool lastGiftBuyOk = false;
                System.Action<bool> onGiftBuy = ok => { giftBuyCount++; lastGiftBuyOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_GIFT_BUY_RESULT, onGiftBuy);
                Feed("On17237", new CliVerify.Pkt().I(1).Bytes());
                Feed("On17237", new CliVerify.Pkt().I(1720080).Bytes()); // B3 失败码边界:love_gift_type_err
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_GIFT_BUY_RESULT, onGiftBuy);
                bool b17237 = giftBuyCount == 2 && !lastGiftBuyOk;
                byte[] p17238 = new CliVerify.Pkt().I(1700010000).I(1700020000)
                    .H(1).C(1).C(0).I(1700030000)
                    .Bytes();
                Feed("On17238", p17238);
                bool b17238 = model.HasGift && model.Gift.LoveGiftTimeS == 1700010000 && model.Gift.GiftState.Count == 1
                    && model.Gift.GiftState[0].CountType == 1;
                byte[] p17239 = new CliVerify.Pkt().I(1).C(1)
                    .H(2).C(0).I(38240101).I(2).C(0).I(38240102).I(1)
                    .Bytes();
                Feed("On17239", p17239);
                bool b17239 = model.LastGiftRewardCountType == 1 && model.LastGiftReward.Count == 2
                    && model.LastGiftReward[0].TypeId == 38240101 && model.LastGiftReward[1].Num == 1;
                Feed("On17239", new CliVerify.Pkt().I(1720001).C(2).H(0).Bytes());
                int giftAskCount = 0; bool lastGiftAskOk = false;
                System.Action<bool> onGiftAsk = ok => { giftAskCount++; lastGiftAskOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_GIFT_ASK_RESULT, onGiftAsk);
                Feed("On17240", new CliVerify.Pkt().I(1).Bytes());
                Feed("On17240", new CliVerify.Pkt().I(1720070).Bytes()); // B3 失败码边界:marriage_ask_lv_limit
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_GIFT_ASK_RESULT, onGiftAsk);
                bool b17240 = giftAskCount == 2 && !lastGiftAskOk;
                bool bGiftGroup = b17237 && b17238 && b17239 && b17240;
                Debug.Log("CLIVERIFY marriage 17237/38/39/40 礼包 giftState=" + (model.Gift?.GiftState.Count ?? -1)
                    + " rewardN=" + model.LastGiftReward.Count + " gift37=" + b17237 + " ask40=" + b17240 + " ok=" + bGiftGroup);

                // ---- R. 17245进退匹配/17246匹配结果(死链UI,数据层照接;17246订正为带Code) ----
                // 裁决1(死号封存):RequestDunMatch 已删除(服务端 handle(17245) 整段注释),仿17212段(L65-68)反射断言无公开发送方法
                bool dead17245SendOk = t.GetMethod("RequestDunMatch", PF) == null;
                bool dead17245RecvOk = t.GetMethod("On17245", F) != null && t.GetMethod("On17246", F) != null;
                Debug.Log("CLIVERIFY marriage 17245死号 noSendMethod=" + dead17245SendOk + " hasDefensiveRecv=" + dead17245RecvOk);
                Feed("On17245", new CliVerify.Pkt().I(1).C(1).I(13001).Bytes());
                bool b17245 = model.IsMatching && model.MatchDunId == 13001;
                Feed("On17245", new CliVerify.Pkt().I(1720001).C(2).I(13001).Bytes());
                CliVerify.Pkt p17246 = new CliVerify.Pkt().I(1)
                    .H(1).C(1).L(9005);
                AppendFigure(p17246, "对手", 180, 0, 0, "");
                p17246.L(999999);
                p17246.C(5); // enter_time
                Feed("On17246", p17246.Bytes());
                bool b17246 = model.LastMatchResult != null && model.LastMatchResult.List.Count == 1
                    && model.LastMatchResult.List[0].RoleId == 9005 && model.LastMatchResult.List[0].Power == 999999
                    && model.LastMatchResult.EnterTime == 5;
                // B3 失败码边界:code!=1(1720087 in_marriaage_dun_match),On17246 无 else 分支,LastMatchResult 不应被覆盖
                CliVerify.Pkt p17246fail = new CliVerify.Pkt().I(1720087)
                    .H(1).C(2).L(9999);
                AppendFigure(p17246fail, "不应生效", 1, 0, 0, "");
                p17246fail.L(1);
                p17246fail.C(9);
                Feed("On17246", p17246fail.Bytes());
                bool b17246fail = model.LastMatchResult != null && model.LastMatchResult.List.Count == 1
                    && model.LastMatchResult.List[0].RoleId == 9005 && model.LastMatchResult.EnterTime == 5;
                Debug.Log("CLIVERIFY marriage 17245/17246 匹配(死链UI,订正带Code) matching=" + b17245 + " result=" + b17246
                    + " failNotOverwritten=" + b17246fail + " ok=" + (b17245 && b17246 && b17246fail));

                // ---- S. 17295邀请买次数/17296收到邀请(无Code)/17297同意拒绝(无Code) ----
                int dunInviteBuyCount = 0; bool lastDunInviteBuyOk = false;
                System.Action<bool> onDunInviteBuy = ok => { dunInviteBuyCount++; lastDunInviteBuyOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_DUN_INVITE_BUY_RESULT, onDunInviteBuy);
                Feed("On17295", new CliVerify.Pkt().I(1).Bytes());
                Feed("On17295", new CliVerify.Pkt().I(1720088).Bytes()); // B3 失败码边界:dun_intimacy_not_enough
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_DUN_INVITE_BUY_RESULT, onDunInviteBuy);
                bool b17295 = dunInviteBuyCount == 2 && !lastDunInviteBuyOk;
                Feed("On17296", new CliVerify.Pkt().L(9005).S("对方").I(13001).Bytes());
                bool b17296 = model.LastDunInvite != null && model.LastDunInvite.RoleId == 9005 && model.LastDunInvite.DunId == 13001;
                var dunRespondList = new List<(int agree, int dunId)>();
                System.Action<int, int> onDunRespond = (agree, dunId) => dunRespondList.Add((agree, dunId));
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_DUN_INVITE_RESPOND_PUSH, onDunRespond);
                Feed("On17297", new CliVerify.Pkt().C(1).I(13001).L(9005).S("对方").Bytes()); // agree=1同意
                Feed("On17297", new CliVerify.Pkt().C(2).I(13002).L(9006).S("对方乙").Bytes()); // B3 边界:agree=2拒绝
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_DUN_INVITE_RESPOND_PUSH, onDunRespond);
                bool b17297 = dunRespondList.Count == 2 && dunRespondList[0].agree == 1 && dunRespondList[0].dunId == 13001
                    && dunRespondList[1].agree == 2 && dunRespondList[1].dunId == 13002;
                Debug.Log("CLIVERIFY marriage 17295/96/97 副本邀请(无Code) invite95=" + b17295 + " invite96=" + b17296
                    + " respond97=" + b17297 + " ok=" + (b17295 && b17296 && b17297));

                // ---- T. 鲜花 22300错误码/22301赠花/22302收礼记录全量/22303信息/22304收花/22305感谢 ----
                Feed("On22300", new CliVerify.Pkt().I(1720001).Bytes());
                // 预置 Fame 基线(供 22301 M4 Fame 联动断言;SetFlowerInfo 整体替换,不影响后续正式 22303 断言)
                Feed("On22303", new CliVerify.Pkt().I(0).I(0).I(1000).Bytes());
                Shenxiao.Module.Core.Marriage.MarriageConfigs.FlowerToolRow fdata38100001 =
                    Shenxiao.Module.Core.Marriage.MarriageConfigs.GetFlowerTool(38100001);
                long expectedFameAfterGive = 1000 + (fdata38100001 != null ? (long)fdata38100001.Fame * 1 : 0);
                Feed("On22301", new CliVerify.Pkt().I(1).L(9007).H(1001).I(38100001).H(1).Bytes());
                bool b22301FameOk = model.Flower != null && model.Flower.Fame == expectedFameAfterGive;
                Feed("On22301", new CliVerify.Pkt().I(1020002).L(9007).H(1001).I(38100001).H(1).Bytes()); // 频繁操作码,不弹
                bool b22301Fame1020002Stable = model.Flower.Fame == expectedFameAfterGive;
                // B3 失败码边界:常规失败码(非1020002,2230001 num_not_enough)→走ShowError路径,Fame不变
                Feed("On22301", new CliVerify.Pkt().I(2230001).L(9007).H(1001).I(38100001).H(1).Bytes());
                bool b22301RegularFailFameStable = model.Flower.Fame == expectedFameAfterGive;
                bool b22301 = b22301FameOk && b22301Fame1020002Stable && b22301RegularFailFameStable;
                byte[] p22302 = new CliVerify.Pkt().H(2)
                    .L(1).L(9007).S("送花者甲").H(1001).H(1).I(38100001).H(1).C(0).C(0).I(1700040000)
                    .L(2).L(9008).S("送花者乙").H(1001).H(1).I(38100002).H(2).C(1).C(1).I(1700040100)
                    .Bytes();
                Feed("On22302", p22302);
                bool b22302 = model.HasFlowerRecords && model.FlowerRecords.Count == 2 && model.FlowerRecords[0].SenderName == "送花者甲"
                    && model.FlowerRecords[1].GoodsNum == 2 && model.FlowerRecords[1].IsThanks == 1;
                Feed("On22303", new CliVerify.Pkt().I(500).I(300).I(200).Bytes()); // 重置为正式断言值(SetFlowerInfo整体替换,冲掉22301的Fame联动)
                bool b22303 = model.HasFlowerInfo && model.Flower.FlowerNum == 500 && model.Flower.Charm == 300 && model.Flower.Fame == 200;
                CliVerify.Pkt p22304 = new CliVerify.Pkt().L(9009);
                AppendFigure(p22304, "送花人", 150, 0, 0, "");
                p22304.H(1001).H(1).I(38100003).H(1);
                Feed("On22304", p22304.Bytes());
                bool b22304 = model.LastFlowerReceived != null && model.LastFlowerReceived.SenderId == 9009
                    && model.LastFlowerReceived.SenderFigure.name == "送花人" && model.LastFlowerReceived.GoodsId == 38100003;
                int flowerThanksCount = 0; bool lastFlowerThanksOk = false;
                System.Action<bool, long> onFlowerThanks = (ok, id) => { flowerThanksCount++; lastFlowerThanksOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On<bool, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_FLOWER_THANKS_RESULT, onFlowerThanks);
                Feed("On22305", new CliVerify.Pkt().I(1).L(9007).Bytes());
                Feed("On22305", new CliVerify.Pkt().I(2230001).L(9008).Bytes()); // B3 失败码边界:num_not_enough
                Shenxiao.Framework.Event.EventDispatcher.Off<bool, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARRIAGE_FLOWER_THANKS_RESULT, onFlowerThanks);
                bool b22305 = flowerThanksCount == 2 && !lastFlowerThanksOk;
                bool bFlowerGroup = b22301 && b22302 && b22303 && b22304 && b22305;
                Debug.Log("CLIVERIFY marriage 鲜花22300-05 give301=" + b22301 + " record=" + b22302 + " info=" + b22303
                    + " received=" + b22304 + " thanks305=" + b22305 + " ok=" + bFlowerGroup);

                bool pass = configOk && deadSendOk && deadRecvOk && b17200 && b17200fail && b1720102NoThrow && b17205
                    && b17210 && b17210fail && b17211 && b17211fail && b17213 && b17213fail && b17212 && b17222 && b17223
                    && b17224 && b17226Outbound && b17226 && b17229 && b17231 && b17232 && b17234 && b17236 && bGiftGroup
                    && dead17245SendOk && dead17245RecvOk && b17245 && b17246 && b17246fail && b17295 && b17296 && b17297
                    && bFlowerGroup;

                Debug.Log("CLIVERIFY marriage VERDICT config=" + configOk + " dead17212=" + (deadSendOk && deadRecvOk)
                    + " l17200=" + b17200 + " l17205=" + b17205 + " l17210=" + (b17210 && b17210fail)
                    + " l1721113=" + (b17211 && b17211fail && b17213 && b17213fail)
                    + " l17212=" + b17212 + " l17222=" + b17222 + " l17223=" + b17223 + " l17224=" + b17224
                    + " l17226=" + (b17226Outbound && b17226) + " l17229=" + b17229 + " l17231=" + b17231 + " l17232=" + b17232
                    + " l1723435=" + b17234 + " l17236=" + b17236 + " gift=" + bGiftGroup
                    + " dead17245=" + (dead17245SendOk && dead17245RecvOk) + " match=" + (b17245 && b17246 && b17246fail)
                    + " dunInvite=" + (b17295 && b17296 && b17297) + " flower=" + bFlowerGroup + " pass=" + pass);

                model.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = editorPreferFallbackBefore;
            }
        }

        /// <summary>按 FigureProto.SCHEMA 逐字段序写入(46 字段,5 个嵌套数组固定写 0 条;name/level/
        /// is_marriage/marriage_id/marriage_name 可覆盖供断言探针)。</summary>
        private static CliVerify.Pkt AppendFigure(CliVerify.Pkt p, string name, int level, int isMarriage, long marriageId, string marriageName) => p
            .S(name)          // name
            .C(0)             // sex
            .C(0)             // realm
            .C(0)             // career
            .H(level)         // level
            .C(0)             // GM
            .C(0)             // vip_flag
            .C(0)             // is_hide_vip
            .C(0)             // touxian
            .H(0)             // level_model_list count
            .H(0)             // fashion_model_list count
            .S("")            // picture
            .I(0)             // prcture_ver
            .L(0)             // guild_id
            .S("")            // guild_name
            .C(0)             // position
            .S("")            // position_name
            .I(0)             // dsgt_id
            .I(0)             // liveness_id
            .C(0)             // turn
            .C(0)             // turn_stage
            .C(0)             // grade_id
            .C(isMarriage)    // is_marriage
            .L(marriageId)    // marriage_id
            .S(marriageName)  // marriage_name
            .I(0)             // escort_state
            .I(0)             // block_id
            .I(0)             // house_id
            .H(0)             // house_lv
            .H(0)             // figure_list count
            .H(0)             // figure_ride_list count
            .H(0)             // achv_lv
            .H(0)             // medal_id
            .I(0)             // fazhen_id
            .H(0)             // dress_list count
            .I(0)             // god_id
            .I(0)             // revelation_suit
            .I(0)             // demon_id
            .C(0)             // supreme_vip
            .I(0)             // title_id
            .C(0)             // mask_id
            .C(0)             // seaCamp
            .C(0)             // brick_id
            .C(0)             // dummy_type
            .C(0)             // suit_fashion_id
            .C(0);            // collect_state
    }
}
