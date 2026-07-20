using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 聊天补全(自动循环 轮6)实证:纯逻辑用例,不建 Stage/不渲染(仿 GoodsProtoCase/ReliveCase 套路)。
    /// 手工按 yu_server pt_110.erl 权威字节序拼合成包,反射喂 ChatController 私有 On11xxx handler,断言:
    ///   11001 世界频道分桶(验证 ChatModel.ChannelWorld 由错误值0纠正为1后,消息真的能落对桶——这是本轮
    ///     发现并修复的一个真实 bug,老实现下世界聊天会永远收不到消息);
    ///   11002 私聊桶(发送者/接收者分流、未读数只在"对方发来"时+1、50条裁剪);
    ///   11029 喇叭队列(按 HornType 落对应公共频道桶 + 独立 HornQueue);
    ///   11023 小跨服开关;11027 消红点(发送侧本地立即清 + 失败码 toast);11028 私聊玩家信息;
    ///   11046 黑名单清理(公共频道消息 + 私聊 dict 双清,且跳过自己);
    ///   11050 公告(state==0 过滤、绝对时间戳首触发、PumpNotice 循环推进、到期移除——用 TimeUtil.SyncServerTime
    ///     操纵服务器时钟做确定性验证,不依赖真实等待,也不依赖 MonoBehaviour.Update,对标轮1教训的"非 Update 通道");
    ///   11042 被禁言 toast(老端漏接的补齐);11064 假人聊天(降级:不臆造数据,仅验证不炸+日志);
    ///   发送侧 SendChat 预校验(空文本/等级/CD/喇叭道具不足)拦截 + 离线发送不抛异常。
    /// 日志前缀统一 "CLIVERIFY chat"。独立文件复用 CliVerify.Pkt,不改 CliVerify.cs 本体。
    /// </summary>
    public static class ChatCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            bool editorPreferFallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            try { return await RunCore(); }
            finally { Shenxiao.Framework.Res.ResManager.EditorPreferFallback = editorPreferFallbackBefore; }
        }

        private static async Task<int> RunCore()
        {
            object ctrl = Shenxiao.Module.Core.Chat.ChatController.Instance;

            MethodInfo GetM(string name)
            {
                MethodInfo m = ctrl.GetType().GetMethod(name, F);
                if (m == null) Debug.LogError("CLIVERIFY chat handler missing(reflection): " + name);
                return m;
            }

            MethodInfo m11001 = GetM("On11001");
            MethodInfo m11002 = GetM("On11002");
            MethodInfo m11023 = GetM("On11023");
            MethodInfo m11027 = GetM("On11027");
            MethodInfo m11028 = GetM("On11028");
            MethodInfo m11029 = GetM("On11029");
            MethodInfo m11042 = GetM("On11042");
            MethodInfo m11046 = GetM("On11046");
            MethodInfo m11050 = GetM("On11050");
            MethodInfo m11060 = GetM("On11060");
            MethodInfo m11061 = GetM("On11061");
            MethodInfo m11063 = GetM("On11063");
            MethodInfo m11064 = GetM("On11064");
            if (m11001 == null || m11002 == null || m11023 == null || m11027 == null || m11028 == null
                || m11029 == null || m11042 == null || m11046 == null || m11050 == null
                || m11060 == null || m11061 == null || m11063 == null || m11064 == null)
            {
                return 3;
            }

            void Feed(MethodInfo m, byte[] pkt) =>
                m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            int FeedRemaining(MethodInfo m, byte[] pkt)
            {
                var reader = new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);
                m.Invoke(ctrl, new object[] { reader });
                return reader.Remaining;
            }

            var model = Shenxiao.Module.Core.Chat.ChatModel.Instance;
            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            model.Reset();
            bag.Clear();
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 5001;
            Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 999;
            await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();

            bool worldBucketOk = TestWorldBucket(m11001, Feed, model);
            bool privateOk = TestPrivateChat(m11002, Feed, model);
            bool hornOk = TestHorn(m11029, Feed, model);
            bool zoneOk = TestZoneOpen(m11023, Feed, model);
            bool clickCacheOk = TestClickCache(m11027, Feed, model);
            bool playerInfoOk = TestPrivatePlayerInfo(m11028, Feed, model);
            bool blacklistOk = TestBlacklistClear(m11046, Feed, model);
            bool noticeOk = TestNotice(m11050, Feed, model);
            bool goodsGainOk = TestGoodsGain(m11060, FeedRemaining);
            bool objectGainOk = TestObjectGain(m11061, FeedRemaining);
            bool flowerOk = TestFlowerEffect(m11063, FeedRemaining);
            bool bannedOk = TestBannedNotice(m11042, Feed);
            bool robotOk = TestRobot(m11064, Feed);
            bool sendChatOk = TestSendChat(bag);

            model.Reset();
            bag.Clear();

            bool pass = worldBucketOk && privateOk && hornOk && zoneOk && clickCacheOk && playerInfoOk
                && blacklistOk && noticeOk && goodsGainOk && objectGainOk && flowerOk
                && bannedOk && robotOk && sendChatOk;
            Debug.Log("CLIVERIFY chat VERDICT worldBucket=" + worldBucketOk + " private=" + privateOk
                + " horn=" + hornOk + " zone=" + zoneOk + " clickCache=" + clickCacheOk
                + " playerInfo=" + playerInfoOk + " blacklist=" + blacklistOk + " notice=" + noticeOk
                + " goodsGain=" + goodsGainOk + " objectGain=" + objectGainOk + " flower=" + flowerOk
                + " banned=" + bannedOk + " robot=" + robotOk + " sendChat=" + sendChatOk + " pass=" + pass);
            await Task.CompletedTask;
            return pass ? 0 : 3;
        }

        // ---- 11001:世界频道分桶(验证 ChannelWorld=1 纠偏,老实现=0 时此断言必挂) ----
        private static bool TestWorldBucket(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            byte[] pkt = new CliVerify.Pkt()
                .C(Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld).H(0).S("").H(0).S("").S("").S("")
                .L(6001)
                .AppendMinimalFigure("世界喊话者")
                .S("大家好").S("").C(1).I(1780000000)
                .Bytes();
            feed(m, pkt);
            var list = model.GetMessages(Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld);
            bool ok = list.Count == 1 && list[0].PlayerId == 6001 && list[0].Message == "大家好";
            Debug.Log("CLIVERIFY chat 11001 worldBucketCount=" + list.Count + " ok=" + ok);
            return ok;
        }

        // ---- 11002:私聊分桶(发送者/接收者互换、未读数只在"对方发来"才+1) ----
        private static bool TestPrivateChat(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            const long self = 5001;
            const long other = 7002;

            // 对方发来一条 → 落 other 桶,未读+1
            feed(m, BuildPrivatePkt(other, self, "你好呀"));
            bool incomingOk = model.GetPrivateMessages(other).Count == 1 && model.GetPrivateUnread(other) == 1;

            // 自己发出一条(player_list[0]=self) → 同一 other 桶追加,未读不变(仍是1)
            feed(m, BuildPrivatePkt(self, other, "在的"));
            bool outgoingOk = model.GetPrivateMessages(other).Count == 2 && model.GetPrivateUnread(other) == 1;

            // 50条裁剪:直接调 Model API 追加到 55 条,断言不超过 51 条且最早的已被裁掉
            for (int i = 0; i < 55; i++)
            {
                var msg = new Shenxiao.Module.Core.Chat.ChatMessage { Channel = Shenxiao.Module.Core.Chat.ChatModel.ChannelPrivate, Message = "msg" + i };
                model.AddPrivateMessage(self, other, self, msg, showTips: false);
            }
            var bucket = model.GetPrivateMessages(other);
            bool capOk = bucket.Count <= 51 && bucket[0].Message != "msg0";

            bool ok = incomingOk && outgoingOk && capOk;
            Debug.Log("CLIVERIFY chat 11002 incomingOk=" + incomingOk + " outgoingOk=" + outgoingOk
                + " capCount=" + bucket.Count + " capOk=" + capOk + " ok=" + ok);
            return ok;
        }

        private static byte[] BuildPrivatePkt(long senderId, long receiverId, string msg)
        {
            return new CliVerify.Pkt()
                .C(Shenxiao.Module.Core.Chat.ChatModel.ChannelPrivate).H(0).H(0).S("")
                .H(2)
                    .L(senderId).AppendMinimalFigure("甲")
                    .L(receiverId).AppendMinimalFigure("乙")
                .S(msg).S("").C(1).I(1780000000)
                .Bytes();
        }

        // ---- 11029:喇叭落对应公共频道桶 + 独立队列 ----
        private static bool TestHorn(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            byte[] pkt = new CliVerify.Pkt()
                .C(2).H(0).H(0).S("").S("").S("")
                .C(2) // hornType=2(小跨服)→ 应落 ChannelSmallKuafu
                .L(8003).AppendMinimalFigure("喇叭手")
                .S("全服喊话").S("").C(1).I(1780000000)
                .Bytes();
            feed(m, pkt);
            var list = model.GetMessages(Shenxiao.Module.Core.Chat.ChatModel.ChannelSmallKuafu);
            bool bucketOk = list.Count == 1 && list[0].IsHorn && list[0].HornType == 2;
            bool queueOk = model.HornQueue.Count == 1 && model.HornQueue[0].PlayerId == 8003;
            bool ok = bucketOk && queueOk;
            Debug.Log("CLIVERIFY chat 11029 bucketOk=" + bucketOk + " queueOk=" + queueOk + " ok=" + ok);
            return ok;
        }

        // ---- 11023:小跨服开关 ----
        private static bool TestZoneOpen(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            feed(m, new CliVerify.Pkt().C(1).Bytes());
            bool openOk = model.IsZoneOpen;
            feed(m, new CliVerify.Pkt().C(0).Bytes());
            bool closeOk = !model.IsZoneOpen;
            bool ok = openOk && closeOk;
            Debug.Log("CLIVERIFY chat 11023 openOk=" + openOk + " closeOk=" + closeOk + " ok=" + ok);
            return ok;
        }

        // ---- 11027:发送侧本地立即清红点 + recv 失败码 toast ----
        private static bool TestClickCache(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            const long target = 9004;
            model.AddPrivateMessage(5001, target, 5001, new Shenxiao.Module.Core.Chat.ChatMessage { Message = "x" });
            bool unreadBefore = model.GetPrivateUnread(target) == 1;

            bool noThrow = true;
            try { Shenxiao.Module.Core.Chat.ChatController.Instance.SendClickCache(target); }
            catch (System.Exception e) { noThrow = false; Debug.LogError("CLIVERIFY chat 11027 SendClickCache threw: " + e); }
            bool unreadAfter = model.GetPrivateUnread(target) == 0;

            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try { feed(m, new CliVerify.Pkt().I(2).Bytes()); }
            finally { Application.logMessageReceived -= cb; }
            bool failToastOk = logs.Exists(l => l.Contains("toast: 操作失败(2)"));

            bool ok = unreadBefore && noThrow && unreadAfter && failToastOk;
            Debug.Log("CLIVERIFY chat 11027 unreadBefore=" + unreadBefore + " noThrow=" + noThrow
                + " unreadAfter=" + unreadAfter + " failToastOk=" + failToastOk + " ok=" + ok);
            return ok;
        }

        // ---- 11028:私聊玩家信息 ----
        private static bool TestPrivatePlayerInfo(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            bool fired = false;
            Shenxiao.Module.Core.Chat.ChatModel.PrivatePlayerInfo got = null;
            System.Action<Shenxiao.Module.Core.Chat.ChatModel.PrivatePlayerInfo> onInfo = info => { fired = true; got = info; };
            EventDispatcher.On<Shenxiao.Module.Core.Chat.ChatModel.PrivatePlayerInfo>(GlobalEvent.EVT_CHAT_PRIVATE_PLAYER_INFO, onInfo);
            byte[] pkt = new CliVerify.Pkt().I(1).L(9005).AppendMinimalFigure("小明").L(88888).C(1).I(66).Bytes();
            feed(m, pkt);
            EventDispatcher.Off<Shenxiao.Module.Core.Chat.ChatModel.PrivatePlayerInfo>(GlobalEvent.EVT_CHAT_PRIVATE_PLAYER_INFO, onInfo);

            bool ok = fired && got != null && got.RoleId == 9005 && got.CombatPower == 88888 && got.Online && got.Intimacy == 66
                && model.LastPrivatePlayerInfo == got;
            Debug.Log("CLIVERIFY chat 11028 fired=" + fired + " roleId=" + (got?.RoleId ?? -1) + " ok=" + ok);
            return ok;
        }

        // ---- 11046:黑名单清理(公共频道+私聊双清,跳过自己) ----
        private static bool TestBlacklistClear(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            const long self = 5001;
            const long stranger = 6100;

            model.AddMessage(new Shenxiao.Module.Core.Chat.ChatMessage { Channel = Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld, PlayerId = self, Message = "自己的话" });
            model.AddMessage(new Shenxiao.Module.Core.Chat.ChatMessage { Channel = Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld, PlayerId = stranger, Message = "陌生人的话" });
            model.AddPrivateMessage(self, stranger, self, new Shenxiao.Module.Core.Chat.ChatMessage { Message = "私聊" });

            byte[] pkt = new CliVerify.Pkt().H(2).L(self).L(stranger).Bytes();
            feed(m, pkt);

            var worldList = model.GetMessages(Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld);
            bool selfKept = worldList.Any(msg => msg.PlayerId == self);
            bool strangerRemoved = !worldList.Any(msg => msg.PlayerId == stranger);
            bool privateCleared = model.GetPrivateMessages(stranger).Count == 0;

            bool ok = selfKept && strangerRemoved && privateCleared;
            Debug.Log("CLIVERIFY chat 11046 selfKept=" + selfKept + " strangerRemoved=" + strangerRemoved
                + " privateCleared=" + privateCleared + " ok=" + ok);
            return ok;
        }

        // ---- 11050:state==0过滤 + 绝对时间戳首触发 + PumpNotice 循环推进 + 到期移除 ----
        // 用 TimeUtil.SyncServerTime 操纵服务器时钟做确定性验证,不依赖真实等待、不依赖 MonoBehaviour.Update
        // (轮1 教训:Update 驱动的行为无头断言必须走非 Update 通道;这里直接调用 PumpNotice() 验证判定逻辑)。
        private static bool TestNotice(MethodInfo m, System.Action<MethodInfo, byte[]> feed,
            Shenxiao.Module.Core.Chat.ChatModel model)
        {
            const long baseEpochSec = 2000000000L; // 任意固定基准,纯相对计算,无需对应真实日期
            Shenxiao.Framework.Util.TimeUtil.SyncServerTime(baseEpochSec * 1000L);

            byte[] pkt = new CliVerify.Pkt().H(2)
                // 有效公告:start 在过去,end 在未来,gap=10 → 应立即可触发
                .S("cn").C(1).S("#fff").S("欢迎光临").S("").I(0).H(10).I(baseEpochSec - 100).I(baseEpochSec + 1000).C(1)
                // state=0 → 应被 SetNoticeList 过滤,不进 Notices
                .S("cn").C(1).S("#fff").S("已下线公告").S("").I(0).H(10).I(baseEpochSec - 100).I(baseEpochSec + 1000).C(0)
                .Bytes();
            feed(m, pkt);
            bool filterOk = model.Notices.Count == 1 && model.Notices[0].Content == "欢迎光临";

            bool triggered = false;
            Shenxiao.Module.Core.Chat.ChatModel.NoticeEntry triggeredEntry = null;
            System.Action<Shenxiao.Module.Core.Chat.ChatModel.NoticeEntry> onTrigger = n => { triggered = true; triggeredEntry = n; };
            EventDispatcher.On<Shenxiao.Module.Core.Chat.ChatModel.NoticeEntry>(GlobalEvent.EVT_CHAT_NOTICE_TRIGGERED, onTrigger);
            model.PumpNotice();
            EventDispatcher.Off<Shenxiao.Module.Core.Chat.ChatModel.NoticeEntry>(GlobalEvent.EVT_CHAT_NOTICE_TRIGGERED, onTrigger);
            bool triggerOk = triggered && triggeredEntry != null && triggeredEntry.NextTimestamp == baseEpochSec + 10;

            // 时钟快进到超过 end_time,PumpNotice 应把它移除
            Shenxiao.Framework.Util.TimeUtil.SyncServerTime((baseEpochSec + 2000) * 1000L);
            model.PumpNotice();
            bool expiredOk = model.Notices.Count == 0;

            bool ok = filterOk && triggerOk && expiredOk;
            Debug.Log("CLIVERIFY chat 11050 filterOk=" + filterOk + " triggerOk=" + triggerOk
                + " expiredOk=" + expiredOk + " ok=" + ok);
            return ok;
        }

        // ---- 11042:被禁言 toast(老端漏接的补齐) ----
        private static bool TestGoodsGain(MethodInfo m, System.Func<MethodInfo, byte[], int> feedRemaining)
        {
            const int knownGoods = 1102015065; // config_goods 中的喇叭卷轴；仅用于 Case 构包。
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                int silentRemaining = feedRemaining(m,
                    new CliVerify.Pkt().C(2).H(1).I(knownGoods).I(1).Bytes());
                bool silentOk = silentRemaining == 0 && !logs.Any(x => x.Contains("float:") || x.Contains("toast:"));

                logs.Clear();
                byte[] visible = new CliVerify.Pkt().C(5).H(3)
                    .I(knownGoods).I(2)
                    .I(knownGoods).I(0)
                    .I(2147483000).I(1)
                    .Bytes();
                int visibleRemaining = feedRemaining(m, visible);
                int shown = logs.Count(x => x.Contains("float:"));
                bool visibleOk = visibleRemaining == 0 && shown == 1
                    && logs.Any(x => x.Contains("x2") && x.Contains("双倍战魂卡"));
                bool ok = silentOk && visibleOk;
                Debug.Log("CLIVERIFY chat 11060 silentOk=" + silentOk + " visibleOk=" + visibleOk + " ok=" + ok);
                return ok;
            }
            finally { Application.logMessageReceived -= cb; }
        }

        private static bool TestObjectGain(MethodInfo m, System.Func<MethodInfo, byte[], int> feedRemaining)
        {
            const int knownGoods = 1102015065;
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                byte[] pkt = new CliVerify.Pkt().H(4)
                    .C(0).I(knownGoods).I(1)
                    .C(3).I(0).I(2)
                    .C(0).I(knownGoods).I(0)
                    .C(0).I(2147483000).I(1)
                    .Bytes();
                int remaining = feedRemaining(m, pkt);
                List<string> shown = logs.Where(x => x.Contains("float:")).ToList();
                bool cursorOk = remaining == 0;
                bool filterOk = shown.Count == 2;
                bool singleOk = shown.Count > 0 && !shown[0].Contains("x1");
                bool mappedOk = shown.Count > 1 && shown[1].Contains("x2");
                bool ok = cursorOk && filterOk && singleOk && mappedOk;
                Debug.Log("CLIVERIFY chat 11061 cursorOk=" + cursorOk + " filterOk=" + filterOk
                    + " singleOk=" + singleOk + " mappedOk=" + mappedOk + " ok=" + ok);
                return ok;
            }
            finally { Application.logMessageReceived -= cb; }
        }

        private static bool TestFlowerEffect(MethodInfo m, System.Func<MethodInfo, byte[], int> feedRemaining)
        {
            var marriage = Shenxiao.Module.Core.Marriage.MarriageModel.Instance;
            marriage.ClearFlowerEffects();
            var fired = new List<string>();
            System.Action<string> onEffect = value => fired.Add(value);
            EventDispatcher.On<string>(GlobalEvent.EVT_CHAT_FLOWER_EFFECT, onEffect);
            bool cursorOk = true;
            try
            {
                for (int i = 0; i < 21; i++)
                {
                    string effect = "server_effect_" + i;
                    cursorOk &= feedRemaining(m, new CliVerify.Pkt().S(effect).Bytes()) == 0;
                }
            }
            finally { EventDispatcher.Off<string>(GlobalEvent.EVT_CHAT_FLOWER_EFFECT, onEffect); }

            bool fifoOk = marriage.FlowerEffects.Count == 20
                && marriage.FlowerEffects[0] == "server_effect_1"
                && marriage.FlowerEffects[19] == "server_effect_20";
            bool eventOk = fired.Count == 21 && fired[20] == "server_effect_20";
            bool dequeueOk = marriage.TryDequeueFlowerEffect(out string first) && first == "server_effect_1"
                && marriage.FlowerEffects.Count == 19;
            marriage.ClearFlowerEffects();
            bool clearOk = marriage.FlowerEffects.Count == 0;
            bool ok = cursorOk && fifoOk && eventOk && dequeueOk && clearOk;
            Debug.Log("CLIVERIFY chat 11063 cursorOk=" + cursorOk + " fifoOk=" + fifoOk
                + " eventOk=" + eventOk + " dequeueOk=" + dequeueOk + " clearOk=" + clearOk + " ok=" + ok);
            return ok;
        }

        private static bool TestBannedNotice(MethodInfo m, System.Action<MethodInfo, byte[]> feed)
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try { feed(m, new CliVerify.Pkt().I(120).Bytes()); }
            catch (System.Exception e) { noThrow = false; Debug.LogError("CLIVERIFY chat 11042 threw: " + e); }
            finally { Application.logMessageReceived -= cb; }
            bool toastOk = logs.Exists(l => l.Contains("你已被禁言,剩余120秒"));
            bool ok = noThrow && toastOk;
            Debug.Log("CLIVERIFY chat 11042 toastOk=" + toastOk + " ok=" + ok);
            return ok;
        }

        // ---- 11064:假人聊天(降级,不臆造数据,仅验证不炸 + 日志留痕) ----
        private static bool TestRobot(MethodInfo m, System.Action<MethodInfo, byte[]> feed)
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try { feed(m, new CliVerify.Pkt().C(3).Bytes()); }
            catch (System.Exception e) { noThrow = false; Debug.LogError("CLIVERIFY chat 11064 threw: " + e); }
            finally { Application.logMessageReceived -= cb; }
            bool logOk = logs.Exists(l => l.Contains("11064") && l.Contains("降级"));
            bool ok = noThrow && logOk;
            Debug.Log("CLIVERIFY chat 11064 noThrow=" + noThrow + " logOk=" + logOk + " ok=" + ok);
            return ok;
        }

        // ---- SendChat 预校验:空文本/等级/CD/喇叭道具不足 拦截,且离线发送全程不抛异常 ----
        private static bool TestSendChat(Shenxiao.Module.Core.Bag.BagModel bag)
        {
            var ctrl = Shenxiao.Module.Core.Chat.ChatController.Instance;
            var role = Shenxiao.Module.Core.Role.RoleModel.Instance;
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try
            {
                logs.Clear();
                ctrl.SendChat(Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld, "");
                bool emptyOk = logs.Exists(l => l.Contains("拦截:文本为空"));

                logs.Clear();
                role.Level = 1;
                ctrl.SendChat(Shenxiao.Module.Core.Chat.ChatModel.ChannelWorld, "hi");
                bool levelOk = logs.Exists(l => l.Contains("拦截:等级不足"));

                logs.Clear();
                role.Level = 999;
                ctrl.SendChat(Shenxiao.Module.Core.Chat.ChatModel.ChannelGuild, "first");
                bool firstSendOk = !logs.Exists(l => l.Contains("拦截:")) && logs.Exists(l => l.Contains("send 11001"));
                logs.Clear();
                ctrl.SendChat(Shenxiao.Module.Core.Chat.ChatModel.ChannelGuild, "second");
                bool cdOk = logs.Exists(l => l.Contains("拦截:CD 未到"));

                logs.Clear();
                bag.Clear();
                ctrl.SendChat(Shenxiao.Module.Core.Chat.ChatModel.ChannelHorn, "喊话", receiveId: 2); // 小跨服需3个喇叭道具,背包空
                bool hornBlockedOk = logs.Exists(l => l.Contains("拦截:喇叭道具不足"));

                logs.Clear();
                bag.Upsert(new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 1, TypeId = 1102015065, GoodsNum = 5 });
                ctrl.SendChat(Shenxiao.Module.Core.Chat.ChatModel.ChannelHorn, "喊话", receiveId: 2);
                bool hornPassOk = !logs.Exists(l => l.Contains("拦截:")) && logs.Exists(l => l.Contains("send 11001"));

                bool ok = emptyOk && levelOk && firstSendOk && cdOk && hornBlockedOk && hornPassOk;
                Debug.Log("CLIVERIFY chat sendChat emptyOk=" + emptyOk + " levelOk=" + levelOk
                    + " firstSendOk=" + firstSendOk + " cdOk=" + cdOk + " hornBlockedOk=" + hornBlockedOk
                    + " hornPassOk=" + hornPassOk + " ok=" + ok);
                return ok;
            }
            catch (System.Exception e)
            {
                noThrow = false;
                Debug.LogError("CLIVERIFY chat sendChat threw(离线发送不应炸): " + e);
                return false;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                Debug.Log("CLIVERIFY chat sendChat noThrow=" + noThrow);
            }
        }

        /// <summary>按 FigureProto.SCHEMA 字段序(Common/Proto/FigureProto.cs)逐项写一个全零/空的最小 Figure 块,
        /// 供合成包测试用——name 之外全部字段留空/0(3个列表字段只写 u16 count=0,不写子项)。改 SCHEMA 顺序时
        /// 这里必须同步,否则后续字段(如 On11002/11028/11029 里的 msg/args)会读串位。</summary>
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
