using System.Collections.Generic;
using System.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Chat
{
    public sealed class ChatModel
    {
        // ----- 频道枚举(对标 A1 表 / yu_server include/chat.hrl ?CHAT_CHANNEL_*,数值权威以服务端为准) -----
        /// <summary>世界(原实现误为0,已按 chat.hrl ?CHAT_CHANNEL_WORLD=1 纠正——此前该值会导致 11001/11010
        /// 世界频道消息永远分到错误的桶,收不到任何世界聊天,是本轮修的一个真实 bug)。</summary>
        public const int ChannelWorld = 1;
        /// <summary>喇叭(发送用 channel;下行不直接落此频道桶,走 11029→按 HornType 映射到 World/SmallKuafu/WorldKuafu)。</summary>
        public const int ChannelHorn = 2;
        public const int ChannelGuild = 4;
        public const int ChannelTeam = 5;
        /// <summary>私聊(11002 专用桶,数据结构见 <see cref="_privateChats"/>,不进 <see cref="_messages"/>)。</summary>
        public const int ChannelPrivate = 6;
        public const int ChannelSystem = 10;
        public const int ChannelWorldKuafu = 13;
        /// <summary>同城(A1 表明确指出的客户端缺口:服务端 chat.hrl ?CHAT_CHANNEL_SAME_CITY=15,
        /// 老客户端源码从未为其定义显式枚举——按服务端补齐,当前无 UI Tab 消费,仅供协议层对齐使用。</summary>
        public const int ChannelSameCity = 15;
        public const int ChannelSmallKuafu = 17;
        /// <summary>阵营(原实现误为15——15 实际是 <see cref="ChannelSameCity"/>,已按 chat.hrl
        /// ?CHAT_CHANNEL_CAMP=18 纠正)。</summary>
        public const int ChannelCamp = 18;
        public const int ChannelSea = 19;
        /// <summary>百煞冲霄/百鬼夜行专用发送与缓存频道。老端收到后统一映射进小跨服频道展示。</summary>
        public const int ChannelGhostWalk = 20;

        /// <summary>主界面每个消息区最多保留的渲染条数，对标老端 MainUIChatView.MAX_SHOW_ITEM_NUM。</summary>
        public const int MainHudMessageCap = 30;

        public static readonly ChatModel Instance = new ChatModel();

        public const string WelcomeSystemMessage =
            "欢迎踏入九州大荒。神霄崩灭后，天殒遗骸化作道痕，秘境引劫而生——愿君融痕证道，历尽九天梯劫！";

        private static readonly ChatMessage[] EmptyMessages = new ChatMessage[0];
        private readonly Dictionary<int, List<ChatMessage>> _messages = new Dictionary<int, List<ChatMessage>>();

        private ChatModel() { }

        public void Reset()
        {
            _messages.Clear();
            _privateChats.Clear();
            _privateChatTabOrder.Clear();
            _hornQueue.Clear();
            _notices.Clear();
            IsZoneOpen = false;
            LastPrivatePlayerInfo = null;
        }

        public IReadOnlyList<ChatMessage> GetMessages(int channel)
        {
            return _messages.TryGetValue(channel, out List<ChatMessage> list) ? list : EmptyMessages;
        }

        /// <summary>
        /// 主界面上半区的合并聊天流。对标老端 allChat():排除系统、私聊和仙宗答题频道，
        /// 按服务端时间升序后只取末尾 30 条。返回新列表，调用方不能改写频道原始缓存。
        /// </summary>
        public IReadOnlyList<ChatMessage> GetMainHudMessages(int maxCount = MainHudMessageCap)
        {
            IEnumerable<ChatMessage> query = _messages
                .Where(kv => kv.Key != ChannelSystem && kv.Key != ChannelPrivate && kv.Key != 16)
                .SelectMany(kv => kv.Value)
                .Where(message => message != null)
                .OrderBy(message => message.Time);

            List<ChatMessage> result = query.ToList();
            TrimToLast(result, maxCount);
            return result;
        }

        /// <summary>主界面下半区的系统消息流，对标老端 sysChat()，只取末尾 30 条。</summary>
        public IReadOnlyList<ChatMessage> GetSystemHudMessages(int maxCount = MainHudMessageCap)
        {
            var result = new List<ChatMessage>(GetMessages(ChannelSystem));
            result.Sort((a, b) => (a?.Time ?? 0).CompareTo(b?.Time ?? 0));
            TrimToLast(result, maxCount);
            return result;
        }

        public void SetCache(int channel, List<ChatMessage> messages)
        {
            _messages[channel] = messages ?? new List<ChatMessage>();
            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, channel);
        }

        public void AddMessage(ChatMessage message)
        {
            if (message == null) return;

            List<ChatMessage> list = GetMutable(message.Channel);
            if (list.Count > 100) list.RemoveAt(0);
            list.Add(message);
            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, message.Channel);
        }

        public void EnsureWelcomeSystemMessage()
        {
            List<ChatMessage> list = GetMutable(ChannelSystem);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Message == WelcomeSystemMessage) return;
            }

            AddMessage(new ChatMessage
            {
                Channel = ChannelSystem,
                Message = WelcomeSystemMessage,
                Result = 1
            });
        }

        public static string ChannelLabel(int channel)
        {
            switch (channel)
            {
                case ChannelWorld: return "世界";
                case ChannelHorn: return "喇叭";
                case ChannelGuild: return "仙宗";
                case ChannelTeam: return "队伍";
                case ChannelPrivate: return "私聊";
                case ChannelSystem: return "系统";
                case ChannelWorldKuafu: return "活动";
                case ChannelSameCity: return "同城";
                case ChannelSmallKuafu: return "跨服";
                case ChannelCamp: return "阵营";
                case ChannelSea: return "沧海舆图";
                case ChannelGhostWalk: return "百煞冲霄";
                default: return channel.ToString();
            }
        }

        private static void TrimToLast(List<ChatMessage> messages, int maxCount)
        {
            if (maxCount < 0) maxCount = 0;
            int removeCount = messages.Count - maxCount;
            if (removeCount > 0) messages.RemoveRange(0, removeCount);
        }

        private List<ChatMessage> GetMutable(int channel)
        {
            if (!_messages.TryGetValue(channel, out List<ChatMessage> list))
            {
                list = new List<ChatMessage>();
                _messages[channel] = list;
            }
            return list;
        }

        // =====================================================================================
        // 私聊会话模型(自动循环 轮6;对标老端 ChatModel.ts channelChatDic[PRIVATE_CHAT]/privateChatTabList_)
        // 消息记录(per-role 桶+未读数)与"打开的会话tab顺序"是两套独立状态,不合并存储(r6_oldchat 报告要点5)。
        // =====================================================================================

        /// <summary>单个私聊对方的消息记录桶(对标老端 channelChatDic[PRIVATE_CHAT][role_id] = [消息数组, 未读数])。</summary>
        public sealed class PrivateChatBucket
        {
            public readonly List<ChatMessage> Messages = new List<ChatMessage>();
            public int UnreadCount;
        }

        /// <summary>单会话消息上限(对标老端 this.privateChat=50;老端先判后插——满50时先裁再插,
        /// 稳态在50~51条间摆动,本端逐字对标,不额外优化)。</summary>
        public const int PrivateChatCap = 50;

        private readonly Dictionary<long, PrivateChatBucket> _privateChats = new Dictionary<long, PrivateChatBucket>();
        private readonly List<long> _privateChatTabOrder = new List<long>();

        public IReadOnlyList<ChatMessage> GetPrivateMessages(long targetId) =>
            _privateChats.TryGetValue(targetId, out PrivateChatBucket bucket) ? bucket.Messages : EmptyMessages;

        public int GetPrivateUnread(long targetId) =>
            _privateChats.TryGetValue(targetId, out PrivateChatBucket bucket) ? bucket.UnreadCount : 0;

        /// <summary>主界面通知栏使用的私聊未读总数；只汇总服务端消息驱动的会话桶。</summary>
        public int TotalPrivateUnread
        {
            get
            {
                int total = 0;
                foreach (PrivateChatBucket bucket in _privateChats.Values)
                {
                    total += bucket.UnreadCount;
                }
                return total;
            }
        }

        /// <summary>已开过的私聊会话顺序(对标老端 privateChatTabList_;仅存 role_id,玩家名/头像等由消费方另查
        /// 11028)。当前无 FriendChatView 等 UI 消费方,数据结构先落地,TODO 等私聊窗口移植时接。</summary>
        public IReadOnlyList<long> PrivateChatTabList => _privateChatTabOrder;

        /// <summary>11002 私聊消息落桶(对标老端 setChatData)。senderId/receiverId 来自 PlayerList[0]/[1],
        /// targetId = 非自己的那一方;showTips=false 用于静默加载(如历史缓存回放),不计未读。</summary>
        public void AddPrivateMessage(long selfId, long senderId, long receiverId, ChatMessage message, bool showTips = true)
        {
            if (message == null) return;

            long targetId = senderId == selfId ? receiverId : senderId;
            message.TargetPlayerId = targetId;

            if (!_privateChats.TryGetValue(targetId, out PrivateChatBucket bucket))
            {
                bucket = new PrivateChatBucket();
                _privateChats[targetId] = bucket;
            }

            if (bucket.Messages.Count > PrivateChatCap) bucket.Messages.RemoveAt(0);
            bucket.Messages.Add(message);

            bool isOutgoing = senderId == selfId;
            if (showTips && !isOutgoing) bucket.UnreadCount++;

            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, targetId);
        }

        /// <summary>清空指定会话未读数(对标老端 reSetPrivateNum,由 11027 发送侧同步本地调用)。</summary>
        public void ClearPrivateUnread(long targetId)
        {
            if (_privateChats.TryGetValue(targetId, out PrivateChatBucket bucket) && bucket.UnreadCount != 0)
            {
                bucket.UnreadCount = 0;
                EventDispatcher.Emit(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, targetId);
            }
        }

        /// <summary>对标老端 addPrivateChatTab:newChat=true(新开会话)仅在不存在时追加到尾部;
        /// newChat=false(重新打开已有会话)移到最前。</summary>
        public void AddPrivateChatTab(long targetId, bool newChat = false)
        {
            int idx = _privateChatTabOrder.IndexOf(targetId);
            if (newChat)
            {
                if (idx < 0) _privateChatTabOrder.Add(targetId);
                return;
            }

            if (idx >= 0) _privateChatTabOrder.RemoveAt(idx);
            _privateChatTabOrder.Insert(0, targetId);
        }

        /// <summary>对标老端 delAllPrivateChatTab。</summary>
        public void RemoveAllPrivateChatTabs() => _privateChatTabOrder.Clear();

        /// <summary>对标老端 delNoMsgPlayer:清掉没有消息记录的会话标签。</summary>
        public void RemoveNoMsgPrivateChatTabs()
        {
            for (int i = _privateChatTabOrder.Count - 1; i >= 0; i--)
            {
                long id = _privateChatTabOrder[i];
                if (!_privateChats.TryGetValue(id, out PrivateChatBucket bucket) || bucket.Messages.Count == 0)
                    _privateChatTabOrder.RemoveAt(i);
            }
        }

        /// <summary>11046 黑名单/清理玩家消息(对标老端 ClearRoleChatData):清掉所有公共频道里该玩家的消息 +
        /// 私聊桶整体清空。调用方(ChatController.On11046)负责跳过自己,这里不重复判断。</summary>
        public void ClearRoleChatData(long roleId)
        {
            var touchedChannels = new List<int>();
            foreach (KeyValuePair<int, List<ChatMessage>> kv in _messages)
            {
                List<ChatMessage> list = kv.Value;
                bool touched = false;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null && list[i].PlayerId == roleId)
                    {
                        list.RemoveAt(i);
                        touched = true;
                    }
                }
                if (touched) touchedChannels.Add(kv.Key);
            }
            foreach (int channel in touchedChannels) EventDispatcher.Emit(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, channel);
            if (touchedChannels.Count > 0) EventDispatcher.Emit(GlobalEvent.EVT_CHAT_ROLE_DATA_CLEARED, roleId);

            if (_privateChats.Remove(roleId))
            {
                EventDispatcher.Emit(GlobalEvent.EVT_CHAT_PRIVATE_CLEARED, roleId);
            }
        }

        // =====================================================================================
        // 喇叭队列(11029;对标老端 setTrumpet——同时落对应公共频道桶 + 独立喇叭展示队列)
        // =====================================================================================

        public const int HornQueueCap = 20;
        private readonly List<ChatMessage> _hornQueue = new List<ChatMessage>();
        public IReadOnlyList<ChatMessage> HornQueue => _hornQueue;

        /// <summary>11029 到达:按 HornType 映射进对应公共频道桶(1本服→World/2小跨服→SmallKuafu/3全服→WorldKuafu,
        /// 对标老端 TRUMPET_TYPE→CHAT_TYPE 映射),同时入独立喇叭队列供横幅/跑马灯消费。</summary>
        public void AddHornMessage(ChatMessage message)
        {
            if (message == null) return;
            message.IsHorn = true;

            if (_hornQueue.Count >= HornQueueCap) _hornQueue.RemoveAt(0);
            _hornQueue.Add(message);

            int targetChannel;
            switch (message.HornType)
            {
                case 2: targetChannel = ChannelSmallKuafu; break;
                case 3: targetChannel = ChannelWorldKuafu; break;
                default: targetChannel = ChannelWorld; break;
            }
            // 复用现有单 Channel 字段做频道桶 key(老端用独立局部变量 channel 索引、不改 data.channel 本身;
            // 本端简化为直接改写 message.Channel,原始喇叭 wire 值(=HORN/2)已在 HornType 字段单独保留)。
            message.Channel = targetChannel;
            AddMessage(message);

            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_HORN_RECEIVED, message);
        }

        // ----- 小跨服聊天开关(11023) -----
        public bool IsZoneOpen { get; private set; }

        public void SetZoneOpen(bool open)
        {
            IsZoneOpen = open;
            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_ZONE_OPEN_CHANGED, open);
        }

        // ----- 查看私聊玩家信息(11028) -----
        public sealed class PrivatePlayerInfo
        {
            public long RoleId;
            public Shenxiao.Common.Proto.FigureProto Figure;
            public long CombatPower;
            public bool Online;
            public int Intimacy;
        }

        public PrivatePlayerInfo LastPrivatePlayerInfo { get; private set; }

        public void SetPrivatePlayerInfo(PrivatePlayerInfo info)
        {
            LastPrivatePlayerInfo = info;
            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_PRIVATE_PLAYER_INFO, info);
        }

        // =====================================================================================
        // 系统公告/跑马灯(11050;对标老端 StartGongGaoList/UpdatetGongGaoTimer)
        // 定时判定用绝对服务器时间戳比较(TimeUtil.NowSec()),不依赖调用频率/Update——CliVerify 等无头断言
        // 可以直接反复调用 PumpNotice() 驱动,不受编辑期 batchmode 不 tick MonoBehaviour.Update 影响
        // (轮1 教训:Update 驱动的行为无头断言必须走非 Update 通道)。生产环境驱动源见 ChatNoticeDriver。
        // =====================================================================================

        public sealed class NoticeEntry
        {
            public string Source = "";
            public int Type;
            public string Color = "";
            public string Content = "";
            public string Url = "";
            public int SendCount;
            public int SendGap;
            public long StartTime;
            public long EndTime;
            public int State;
            /// <summary>下次展示的服务器时间戳(秒),对标老端 vo.next_time_stamp,由 SetNoticeList/PumpNotice 维护。</summary>
            public long NextTimestamp;
        }

        private readonly List<NoticeEntry> _notices = new List<NoticeEntry>();
        public IReadOnlyList<NoticeEntry> Notices => _notices;

        /// <summary>11050 全量到达:过滤 state==0/已过期项,计算每条下次展示时间戳,幂等重建
        /// (对标老端 StartGongGaoList;平台名过滤 CheckPlatName 未移植——服务端已按 plat_name 过滤,TODO 若要客户端
        /// 二次过滤需补 ClientConfig.plat_name 等价物)。</summary>
        public void SetNoticeList(List<NoticeEntry> list)
        {
            _notices.Clear();
            long now = TimeUtil.NowSec();
            if (list != null)
            {
                foreach (NoticeEntry n in list)
                {
                    if (n == null) continue;
                    if (n.State == 0 || now > n.EndTime) continue;
                    if (n.SendGap <= 3) n.SendGap = 300; // 对标老端退化保护(send_gap<=3 视为无效,兜底300秒)

                    long delta = n.StartTime - now;
                    if (delta > 0)
                    {
                        n.NextTimestamp = n.StartTime;
                    }
                    else
                    {
                        long mod = (now - n.StartTime) % n.SendGap;
                        n.NextTimestamp = now + (mod > 0 ? n.SendGap - mod : 0);
                    }
                    _notices.Add(n);
                }
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CHAT_NOTICE_LIST_UPDATED);
            EnsureNoticeDriver();
        }

        /// <summary>公告跑马灯每秒判定入口(对标老端 UpdatetGongGaoTimer):过期移除;到点触发展示事件并把
        /// next_time_stamp 前移一个 send_gap(循环重复展示直到 end_time)。纯绝对时间戳比较,可安全反复调用。</summary>
        public void PumpNotice()
        {
            if (_notices.Count == 0) return;
            long now = TimeUtil.NowSec();
            for (int i = _notices.Count - 1; i >= 0; i--)
            {
                NoticeEntry n = _notices[i];
                if (now > n.EndTime)
                {
                    _notices.RemoveAt(i);
                    continue;
                }
                if (now >= n.NextTimestamp)
                {
                    EventDispatcher.Emit(GlobalEvent.EVT_CHAT_NOTICE_TRIGGERED, n);
                    n.NextTimestamp += n.SendGap;
                }
            }
        }

        private static GameObject _noticeDriverGo;

        /// <summary>创建每帧驱动 PumpNotice 的常驻 GameObject(同 Scene.DamageFontRenderer/MonsterRenderer 的
        /// Renderer+Driver 拆分约定:业务判定是纯静态方法,Driver 只是薄薄一层 Update 转发,"工程现有 tick 通道")。</summary>
        private static void EnsureNoticeDriver()
        {
            if (_noticeDriverGo != null) return;
            _noticeDriverGo = new GameObject("__ChatNoticeDriver");
            // DontDestroyOnLoad 仅 Play 模式合法;编辑期(CliVerify 无头经 PumpNotice 手动推进)跳过即可
            if (Application.isPlaying) Object.DontDestroyOnLoad(_noticeDriverGo);
            _noticeDriverGo.AddComponent<ChatNoticeDriver>();
        }
    }

    /// <summary>ChatModel 公告跑马灯每帧驱动(见 <see cref="ChatModel.PumpNotice"/> 注释)。</summary>
    public sealed class ChatNoticeDriver : MonoBehaviour
    {
        private void Update() => ChatModel.Instance.PumpNotice();
    }
}
