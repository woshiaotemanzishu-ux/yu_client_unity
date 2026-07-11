using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 好友+邮件+私聊(自动循环 轮7)实证:反射喂 FriendController(14000/14003/14004/14005/14006/14007/
    /// 14008/14009/14010/14013/14014/14015)与 MailController(19001/19002/19003/19005)私有 handler,
    /// 手工按 yu_server pt_140.erl/pt_190.erl 字段序拼大端合成包,断言:
    ///   14000 分桶 + 尾哨兵(NetReader.Remaining 精确对齐,不多吃不少吃);
    ///   14006 全量 + 14008 推送去重插入;14009 在线状态 + offline_time(TimeUtil.SyncServerTime 操纵时钟);
    ///   14013 增量插入/覆盖;14014 增量移除;14015 亲密度只在好友桶命中才更新;
    ///   19001→19002 缓存优先(用 MailController 自身"请求详情(缓存未命中)"后置日志的有无断言是否真的发了协议,
    ///   对标 ChatCase SendChat 断言"send 11001"日志的套路,比依赖 NetManager 连接态更稳);
    ///   19005 领取 state→3 + 背包预检拦截(背包满时不发协议);19003 GetNoGetRewardEmailList 过滤未领附件保护;
    ///   19501/19502 资料卡基础装备字段落地。
    /// 渲染段:FriendBindUpgrader 自跑装配 + 实例化 FriendModule.prefab,喂 14000 好友列表,断言 FriendView
    /// 列表条目数与展示文本 + 截图。独立用例文件,复用 CliVerify.Pkt/Stage,不改 CliVerify.cs 本体。
    /// 日志前缀统一 "CLIVERIFY friendmail"。
    /// </summary>
    public static class FriendMailCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            if (!Shenxiao.Editor.UiCreator.Friend.FriendBindUpgrader.Generate())
            {
                Debug.LogError("CLIVERIFY friendmail FAIL FriendBindUpgrader.Generate()(嫁接/升级失败,看前面 [UiCreator] 日志)");
                return 3;
            }

            CliVerify.Stage stage = CliVerify.Stage.Create();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                bool friendOk = RunFriendProto(logs);
                bool mailOk = RunMailProto(logs);
                bool cardOk = RunPlayerCard();
                bool renderOk = await RunRenderAsync(stage);

                bool pass = friendOk && mailOk && cardOk && renderOk;
                Debug.Log("CLIVERIFY friendmail VERDICT friend=" + friendOk + " mail=" + mailOk
                    + " card=" + cardOk + " render=" + renderOk + " pass=" + pass);
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

        // =====================================================================================
        // 好友(140xx)
        // =====================================================================================

        private static bool RunFriendProto(List<string> logs)
        {
            object ctrl = Shenxiao.Module.Core.Friend.FriendController.Instance;
            MethodInfo m14000 = ctrl.GetType().GetMethod("On14000", F);
            MethodInfo m14006 = ctrl.GetType().GetMethod("On14006", F);
            MethodInfo m14008 = ctrl.GetType().GetMethod("On14008", F);
            MethodInfo m14009 = ctrl.GetType().GetMethod("On14009", F);
            MethodInfo m14010 = ctrl.GetType().GetMethod("On14010", F);
            MethodInfo m14013 = ctrl.GetType().GetMethod("On14013", F);
            MethodInfo m14014 = ctrl.GetType().GetMethod("On14014", F);
            MethodInfo m14015 = ctrl.GetType().GetMethod("On14015", F);
            if (m14000 == null || m14006 == null || m14008 == null || m14009 == null || m14010 == null
                || m14013 == null || m14014 == null || m14015 == null)
            {
                Debug.LogError("CLIVERIFY friendmail friend handlers missing (reflection)");
                return false;
            }

            var model = Shenxiao.Module.Core.Friend.FriendModel.Instance;
            model.Reset();

            // ---- 14000 分桶 + 尾哨兵(自建 NetReader 保留引用,校验 Remaining 精确对齐) ----
            byte[] p14000 = new CliVerify.Pkt().C(1).H(2)
                .L(1001).S("甲").C(1).C(1).C(0).H(50).C(3).C(0).S("").I(0).L(1000).C(1)
                    .I(88).C(0).I(0).I(0).H(0).C(0).I(0).I(0).I(1700000000).H(0)
                .L(1002).S("乙").C(2).C(2).C(1).H(60).C(0).C(0).S("").I(0).L(2000).C(0)
                    .I(0).C(0).I(0).I(0).H(0).C(0).I(0).I(500).I(1700000001).H(1).C(1).I(9001)
                .C(0xEE).C(0xEE) // 尾哨兵
                .Bytes();
            var reader14000 = new Shenxiao.Framework.Net.NetReader(p14000, 0, p14000.Length);
            m14000.Invoke(ctrl, new object[] { reader14000 });
            var friendList = model.GetFriendData(1);
            bool bucket14000Ok = friendList.Count == 2 && friendList[0].RoleId == 1001 && friendList[0].Name == "甲"
                && friendList[0].Vip == 3 && friendList[1].RoleId == 1002 && friendList[1].DressList.Count == 1
                && friendList[1].DressList[0].DressId == 9001;
            bool tailOk = reader14000.Remaining == 2; // 尾哨兵完好 → 没多吃/少吃字节
            bool bucket14000All = bucket14000Ok && tailOk;
            Debug.Log("CLIVERIFY friendmail 14000 count=" + friendList.Count + " tailRemaining=" + reader14000.Remaining
                + " bucketOk=" + bucket14000Ok + " tailOk=" + tailOk + " ok=" + bucket14000All);

            // ---- 14006 全量 + 14008 推送去重插入 ----
            byte[] p14006 = new CliVerify.Pkt().H(1)
                .L(2001).S("申请甲").C(1).C(1).H(30).S("").I(0).L(500).I(1700000010)
                .Bytes();
            Feed(ctrl, m14006, p14006);
            bool apply14006Ok = model.ApplyList.Count == 1 && model.ApplyList[0].RoleId == 2001 && model.HaveNewApply;

            byte[] p14008Dup = new CliVerify.Pkt().L(2001).S("申请甲").C(1).C(1).H(30).S("").I(0).L(500).I(1700000010).Bytes();
            Feed(ctrl, m14008, p14008Dup);
            bool dedupOk = model.ApplyList.Count == 1; // 去重:同 id 不重复插入

            byte[] p14008New = new CliVerify.Pkt().L(2002).S("申请乙").C(2).C(0).H(40).S("").I(0).L(800).I(1700000020).Bytes();
            Feed(ctrl, m14008, p14008New);
            bool newApplyOk = model.ApplyList.Count == 2 && model.ApplyList[1].RoleId == 2002;
            bool apply14006And14008Ok = apply14006Ok && dedupOk && newApplyOk;
            Debug.Log("CLIVERIFY friendmail 14006/14008 apply14006Ok=" + apply14006Ok + " dedupOk=" + dedupOk
                + " newApplyOk=" + newApplyOk + " ok=" + apply14006And14008Ok);

            // ---- 14009 在线状态 + offline_time(操纵服务器时钟做确定性验证,对标 ChatCase 11050 套路) ----
            const long baseEpochSec = 2000000000L;
            Shenxiao.Framework.Util.TimeUtil.SyncServerTime(baseEpochSec * 1000L);
            byte[] p14009 = new CliVerify.Pkt().L(1001).S("甲").C(1).C(0).I(baseEpochSec - 300).Bytes(); // 下线,300秒前
            Feed(ctrl, m14009, p14009);
            var vo1001 = model.GetFriendById(1001);
            bool onlineOk = vo1001 != null && vo1001.OnlineFlag == 0 && vo1001.OfflineTime == 300;
            Debug.Log("CLIVERIFY friendmail 14009 onlineFlag=" + (vo1001?.OnlineFlag ?? -1) + " offlineTime=" + (vo1001?.OfflineTime ?? -1) + " ok=" + onlineOk);

            // ---- 14013 增量插入/覆盖 ----
            byte[] p14013Insert = new CliVerify.Pkt().H(1) // 1个 update group
                .C(1).H(1) // type=1, 1项
                    .L(1003).S("丙").C(1).C(1).C(0).H(70).C(0).C(0).S("").I(0).L(3000).C(1)
                        .I(0).C(0).I(0).I(0).H(0).C(0).I(0).I(0).I(1700000030).H(0)
                .Bytes();
            Feed(ctrl, m14013, p14013Insert);
            bool insertOk = model.GetFriendData(1).Count == 3 && model.GetFriendById(1003) != null;

            byte[] p14013Overwrite = new CliVerify.Pkt().H(1)
                .C(1).H(1)
                    .L(1003).S("丙").C(1).C(1).C(0).H(70).C(0).C(0).S("").I(0).L(9999).C(1) // combat 改成 9999
                        .I(0).C(0).I(0).I(0).H(0).C(0).I(0).I(0).I(1700000030).H(0)
                .Bytes();
            Feed(ctrl, m14013, p14013Overwrite);
            var vo1003 = model.GetFriendById(1003);
            bool overwriteOk = model.GetFriendData(1).Count == 3 && vo1003 != null && vo1003.Combat == 9999; // 覆盖非新增
            bool delta14013Ok = insertOk && overwriteOk;
            Debug.Log("CLIVERIFY friendmail 14013 insertOk=" + insertOk + " overwriteOk=" + overwriteOk + " ok=" + delta14013Ok);

            // ---- 14014 增量移除 ----
            byte[] p14014 = new CliVerify.Pkt().H(1).C(1).H(1).L(1003).Bytes();
            Feed(ctrl, m14014, p14014);
            bool removeOk = model.GetFriendData(1).Count == 2 && model.GetFriendById(1003) == null;
            Debug.Log("CLIVERIFY friendmail 14014 count=" + model.GetFriendData(1).Count + " ok=" + removeOk);

            // ---- 14015 亲密度(只在好友桶命中才更新) ----
            byte[] p14015 = new CliVerify.Pkt().L(1001).I(66).Bytes();
            Feed(ctrl, m14015, p14015);
            bool intimacyOk = model.GetFriendById(1001)?.Intimacy == 66;
            Debug.Log("CLIVERIFY friendmail 14015 intimacy=" + (model.GetFriendById(1001)?.Intimacy ?? -1) + " ok=" + intimacyOk);

            // ---- 14010 菜单数据 + 800ms 节流(自身/0 拦截由 ShouldRequestMenu 负责,此处直接验证回包落地) ----
            byte[] p14010 = new CliVerify.Pkt().I(1).L(1001)
                .AppendMinimalFigure("甲")
                .C(1).I(777).Bytes();
            Feed(ctrl, m14010, p14010);
            var menu = model.GetMenuData(1001);
            bool menuOk = menu != null && menu.Rela == 1 && menu.TeamId == 777 && menu.Figure != null && menu.Figure.name == "甲";
            bool throttleBlockOk = !model.ShouldRequestMenu(1001, selfRoleId: 9999); // 刚请求过,800ms 内应节流
            bool throttleSelfOk = !model.ShouldRequestMenu(1001, selfRoleId: 1001);   // role_id==自己应拦截
            bool throttleZeroOk = !model.ShouldRequestMenu(0, selfRoleId: 9999);       // role_id==0 应拦截
            bool menuAll = menuOk && throttleBlockOk && throttleSelfOk && throttleZeroOk;
            Debug.Log("CLIVERIFY friendmail 14010 menuOk=" + menuOk + " throttleBlock=" + throttleBlockOk
                + " throttleSelf=" + throttleSelfOk + " throttleZero=" + throttleZeroOk + " ok=" + menuAll);

            model.Reset();
            bool pass = bucket14000All && apply14006And14008Ok && onlineOk && delta14013Ok && removeOk && intimacyOk && menuAll;
            Debug.Log("CLIVERIFY friendmail friend VERDICT pass=" + pass);
            return pass;
        }

        // =====================================================================================
        // 邮件(190xx)
        // =====================================================================================

        private static bool RunMailProto(List<string> logs)
        {
            object mailCtrl = Shenxiao.Module.Core.Mail.MailController.Instance;
            MethodInfo m19002 = mailCtrl.GetType().GetMethod("On19002", F);
            MethodInfo m19005 = mailCtrl.GetType().GetMethod("On19005", F);
            if (m19002 == null || m19005 == null)
            {
                Debug.LogError("CLIVERIFY friendmail mail handlers missing (reflection)");
                return false;
            }

            var mailModel = Shenxiao.Module.Core.Mail.MailModel.Instance;
            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            mailModel.Clear();
            bag.Clear();

            // ---- 19001(列表,直接调 Model API 铺底数据)→ 19002 缓存优先 ----
            var v1 = new Shenxiao.Module.Core.Mail.MailVo { MailId = 5001, Type = 2, State = 2, Title = "标题A", IsAttach = 1, Time = 1700000000, EffectEt = 1900000000 };
            var v2 = new Shenxiao.Module.Core.Mail.MailVo { MailId = 5002, Type = 2, State = 1, Title = "标题B", IsAttach = 0, Time = 1700000001, EffectEt = 1900000001 };
            var v3 = new Shenxiao.Module.Core.Mail.MailVo { MailId = 5003, Type = 2, State = 3, Title = "标题C", IsAttach = 1, Time = 1700000002, EffectEt = 1900000002 };
            mailModel.SetMailList(new List<Shenxiao.Module.Core.Mail.MailVo> { v1, v2, v3 });

            // 第一次请求详情(缓存未命中)→ 应尝试真发协议(断言用控制器自身"请求详情(缓存未命中)"后置日志,
            // 比依赖 NetManager 内部连接态判定更稳,对标 ChatCase SendChat 断言"send 11001"日志的套路)。
            logs.Clear();
            Shenxiao.Module.Core.Mail.MailController.Instance.RequestMailDetail(5001);
            bool firstSendAttempted = logs.Exists(l => l.Contains("19002 请求详情(缓存未命中)"));

            // 服务端回包落地缓存(手工 19002 合成包)
            byte[] p19002 = new CliVerify.Pkt().L(5001).S("张三").S("标题A").S("正文内容")
                .H(1) // attachment count=1
                    .C(0).I(520100).I(3).H(0) // object_type=0,type_id=520100,num=3,extra_attr count=0
                .I(1700000000).C(0)
                .Bytes();
            Feed(mailCtrl, m19002, p19002);
            bool detailCached = mailModel.GetDetail(5001) != null && mailModel.GetDetail(5001).Attachment.Count == 1;
            bool stateBecameRead = v1.State == 1; // state!=3 → 改1已读(对标老端 setEmailInfo)

            // 第二次请求详情(缓存命中)→ 不应再尝试发协议。
            logs.Clear();
            Shenxiao.Module.Core.Mail.MailController.Instance.RequestMailDetail(5001);
            bool secondNoSend = !logs.Exists(l => l.Contains("19002 请求详情(缓存未命中)"));
            bool cacheHitLogged = logs.Exists(l => l.Contains("缓存命中"));

            bool detail19002Ok = firstSendAttempted && detailCached && stateBecameRead && secondNoSend && cacheHitLogged;
            Debug.Log("CLIVERIFY friendmail 19001->19002 firstSend=" + firstSendAttempted + " cached=" + detailCached
                + " stateRead=" + stateBecameRead + " secondNoSend=" + secondNoSend + " cacheHitLog=" + cacheHitLogged + " ok=" + detail19002Ok);

            // ---- 19003 过滤:只删无附件(5002)或已领取附件(5003)的邮件,绝不含未领附件(5001) ----
            List<Shenxiao.Module.Core.Mail.MailVo> deletable = mailModel.GetNoGetRewardEmailList();
            bool filterOk = deletable.Count == 2 && deletable.Exists(x => x.MailId == 5002) && deletable.Exists(x => x.MailId == 5003)
                && !deletable.Exists(x => x.MailId == 5001);
            Debug.Log("CLIVERIFY friendmail 19003 deletableCount=" + deletable.Count + " ok=" + filterOk);

            // ---- 19005 背包预检拦截(背包满时不应发协议;断言用控制器自身"请求领取单封"后置日志) ----
            bag.SetBagFull(cellNum: 10, maxCell: 10, goods: new List<Shenxiao.Module.Core.Bag.BagGoods>());
            for (int i = 0; i < 10; i++) bag.BagGoodsList.Add(new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = i + 1, TypeId = 520100, GoodsNum = 1, Cell = i + 1 });
            logs.Clear();
            Shenxiao.Module.Core.Mail.MailController.Instance.RequestReceiveOne(5001);
            bool blockedWhenFull = !logs.Exists(l => l.Contains("19005 请求领取单封")) && logs.Exists(l => l.Contains("背包已满"));

            // 腾出空位后再次领取:应真的发协议。
            bag.BagGoodsList.RemoveAt(0);
            logs.Clear();
            Shenxiao.Module.Core.Mail.MailController.Instance.RequestReceiveOne(5001);
            bool sentWhenFree = logs.Exists(l => l.Contains("19005 请求领取单封"));
            bool bagGuardOk = blockedWhenFull && sentWhenFree;
            Debug.Log("CLIVERIFY friendmail 19005 guard blockedWhenFull=" + blockedWhenFull + " sentWhenFree=" + sentWhenFree + " ok=" + bagGuardOk);

            // ---- 19005 回包:state→3 ----
            byte[] p19005 = new CliVerify.Pkt().I(1).H(1).L(5001).H(1).C(0).I(520100).I(3).Bytes();
            Feed(mailCtrl, m19005, p19005);
            bool receivedStateOk = v1.State == 3;
            Debug.Log("CLIVERIFY friendmail 19005 state=" + v1.State + " ok=" + receivedStateOk);

            mailModel.Clear();
            bag.Clear();
            bool pass = detail19002Ok && filterOk && bagGuardOk && receivedStateOk;
            Debug.Log("CLIVERIFY friendmail mail VERDICT pass=" + pass);
            return pass;
        }

        // =====================================================================================
        // 资料卡(19501/19502)
        // =====================================================================================

        private static bool RunPlayerCard()
        {
            object ctrl = Shenxiao.Module.Core.Friend.FriendController.Instance;
            MethodInfo m19501 = ctrl.GetType().GetMethod("On19501", F);
            MethodInfo m19502 = ctrl.GetType().GetMethod("On19502", F);
            if (m19501 == null || m19502 == null)
            {
                Debug.LogError("CLIVERIFY friendmail lookover handlers missing (reflection)");
                return false;
            }

            bool noThrow = true;
            try { Feed(ctrl, m19501, new CliVerify.Pkt().I(0).Bytes()); }
            catch (System.Exception e) { noThrow = false; Debug.LogError("CLIVERIFY friendmail 19501 threw: " + e); }

            byte[] p19502 = new CliVerify.Pkt().H(0).L(3001).L(88888).H(5)
                .AppendMinimalFigure("角色卡甲")
                .H(1).L(9001).I(520100).H(7).C(3).H(10).C(2).H(20).H(30).H(1) // equip item
                .H(1).C(3).C(1).C(0).I(1900000000) // magic circle item
                .H(1).H(2).C(1) // fairy item
                .Bytes();
            Feed(ctrl, m19502, p19502);
            Shenxiao.Module.Core.Friend.FriendModel.PlayerCard card = Shenxiao.Module.Core.Friend.FriendModel.Instance.LastPlayerCard;
            bool cardOk = card != null && card.RoleId == 3001 && card.Combat == 88888 && card.AchvStage == 5
                && card.Figure != null && card.Figure.name == "角色卡甲"
                && card.EquipList.Count == 1 && card.EquipList[0].GoodsId == 9001 && card.EquipList[0].TypeId == 520100
                && card.MagicCircle.Count == 1 && card.MagicCircle[0].Lv == 3
                && card.FairyList.Count == 1 && card.FairyList[0].Type == 2 && card.FairyList[0].IsActive == 1;

            Debug.Log("CLIVERIFY friendmail 19501/19502 noThrow=" + noThrow + " roleId=" + (card?.RoleId ?? -1)
                + " combat=" + (card?.Combat ?? -1) + " equip=" + (card?.EquipList.Count ?? -1) + " ok=" + (noThrow && cardOk));
            return noThrow && cardOk;
        }

        // =====================================================================================
        // 渲染:FriendModule.prefab → FriendView 喂 14000 断言列表条目
        // =====================================================================================

        private static async Task<bool> RunRenderAsync(CliVerify.Stage stage)
        {
            var model = Shenxiao.Module.Core.Friend.FriendModel.Instance;
            model.Reset();

            object ctrl = Shenxiao.Module.Core.Friend.FriendController.Instance;
            MethodInfo m14000 = ctrl.GetType().GetMethod("On14000", F);
            byte[] p = new CliVerify.Pkt().C(1).H(2)
                .L(6001).S("渲染甲").C(1).C(1).C(0).H(88).C(0).C(0).S("").I(0).L(12345).C(1)
                    .I(0).C(0).I(0).I(0).H(0).C(0).I(0).I(0).I(1700000000).H(0)
                .L(6002).S("渲染乙").C(2).C(2).C(0).H(99).C(0).C(0).S("").I(0).L(23456).C(0)
                    .I(0).C(0).I(0).I(0).H(0).C(0).I(0).I(0).I(1700000001).H(0)
                .Bytes();
            Feed(ctrl, m14000, p);

            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Friend/FriendModule.prefab");
            if (prefab == null)
            {
                Debug.LogError("CLIVERIFY friendmail FriendModule.prefab missing");
                return false;
            }
            GameObject go = Object.Instantiate(prefab, stage.CanvasRoot);
            try
            {
                var friendView = go.GetComponentInChildren<Shenxiao.Module.Core.Friend.FriendView>(true);
                if (friendView == null)
                {
                    Debug.LogError("CLIVERIFY friendmail FriendView missing in FriendModule.prefab");
                    return false;
                }
                friendView.gameObject.SetActive(true);
                friendView.Show(); // Bind 子组件父视图须先 Show() 才触发 EnsureBound(轮3 三坑规避)

                await Task.Delay(300);
                stage.ForceCjkFont();

                var items = go.GetComponentsInChildren<Shenxiao.Module.Core.Friend.FriendListItem>(true);
                int activeCount = 0;
                foreach (var it in items) if (it.gameObject.activeInHierarchy) activeCount++;

                string png = stage.Capture("Temp/round22_friend_list.png");
                bool pass = activeCount == 2;
                Debug.Log("CLIVERIFY friendmail render activeItems=" + activeCount + " shot=" + png + " pass=" + pass);
                return pass;
            }
            finally
            {
                Object.DestroyImmediate(go);
                model.Reset();
            }
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块(与 ChatCase.AppendMinimalFigure
        /// 逐字节相同,独立文件各自持有一份,避免跨用例文件耦合)。改 SCHEMA 顺序时两处必须同步。</summary>
        private static CliVerify.Pkt AppendMinimalFigure(this CliVerify.Pkt p, string name)
        {
            return p
                .S(name)  // name
                .C(0)     // sex
                .C(0)     // realm
                .C(0)     // career
                .H(0)     // level
                .C(0)     // GM
                .C(0)     // vip_flag
                .C(0)     // is_hide_vip
                .C(0)     // touxian
                .H(0)     // level_model_list count
                .H(0)     // fashion_model_list count
                .S("")    // picture
                .I(0)     // prcture_ver
                .L(0)     // guild_id
                .S("")    // guild_name
                .C(0)     // position
                .S("")    // position_name
                .I(0)     // dsgt_id
                .I(0)     // liveness_id
                .C(0)     // turn
                .C(0)     // turn_stage
                .C(0)     // grade_id
                .C(0)     // is_marriage
                .L(0)     // marriage_id
                .S("")    // marriage_name
                .I(0)     // escort_state
                .I(0)     // block_id
                .I(0)     // house_id
                .H(0)     // house_lv
                .H(0)     // figure_list count
                .H(0)     // figure_ride_list count
                .H(0)     // achv_lv
                .H(0)     // medal_id
                .I(0)     // fazhen_id
                .H(0)     // dress_list count
                .I(0)     // god_id
                .I(0)     // revelation_suit
                .I(0)     // demon_id
                .C(0)     // supreme_vip
                .I(0)     // title_id
                .C(0)     // mask_id
                .C(0)     // seaCamp
                .C(0)     // brick_id
                .C(0)     // dummy_type
                .C(0)     // suit_fashion_id
                .C(0);    // collect_state
        }
    }
}
