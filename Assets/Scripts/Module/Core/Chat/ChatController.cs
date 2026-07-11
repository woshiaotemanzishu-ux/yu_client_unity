using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Chat
{
    public sealed class ChatController : BaseController
    {
        public static readonly ChatController Instance = new ChatController();

        // ----- 发言等级/CD 门槛(自动循环 轮6;纯客户端预校验,对标 yu_server include/chat.hrl get_lv/get_cd,
        //        权威判定始终在服务端,这里只是提前拦截减少无效包) -----
        private static readonly Dictionary<int, int> ChannelLevelRequirement = new Dictionary<int, int>
        {
            { ChatModel.ChannelWorld, 60 },        // ?CHAT_LV_WORLD
            { ChatModel.ChannelHorn, HornOpenLevel }, // ?CHAT_LV_HORN = HORN_KV(open_lv),见 config_horn_kv.json
            { ChatModel.ChannelGuild, 60 },         // ?CHAT_LV_GUILD
            { ChatModel.ChannelTeam, 70 },          // ?CHAT_LV_TEAM
            { ChatModel.ChannelPrivate, 60 },       // ?CHAT_LV_PRIVATE
            { ChatModel.ChannelSameCity, 150 },     // ?CHAT_LV_SAME_CITY
            { ChatModel.ChannelSmallKuafu, 200 },   // ?CHAT_LV_ZONE
            { ChatModel.ChannelCamp, 260 },         // ?CHAT_LV_CAMP
        };
        private const int DefaultChannelLevel = 60; // ?CHAT_LV_DEFAULT

        private static readonly Dictionary<int, float> ChannelCdSeconds = new Dictionary<int, float>
        {
            { ChatModel.ChannelWorld, 5f },         // ?CHAT_GAP_WORLD
            { ChatModel.ChannelHorn, 0f },          // ?CHAT_GAP_HORN = HORN_KV(send_msg_interval),当前配置值 0
            { ChatModel.ChannelGuild, 3f },         // ?CHAT_GAP_GUILD
            { ChatModel.ChannelTeam, 3f },          // ?CHAT_GAP_TEAM
            { ChatModel.ChannelPrivate, 3f },       // ?CHAT_GAP_PRIVATE
            { ChatModel.ChannelSameCity, 5f },      // ?CHAT_GAP_SAME_CITY
            { ChatModel.ChannelSmallKuafu, 10f },   // ?CHAT_GAP_ZONE
            { ChatModel.ChannelCamp, 5f },          // ?CHAT_GAP_CAMP
        };
        private const float DefaultChannelCd = 3f; // ?CHAT_GAP_DEFAULT

        // ----- 喇叭消耗(config_horn_kv.json 实测值,未接入 ConfigSync 管线的最小硬编码镜像;
        //        路径 yu_client/h5/bin/resource/config/server/config_horn_kv.json,策划改表需同步这里) -----
        private const int HornOpenLevel = 60;
        private const int HornCostGoodsTypeId = 1102015065; // ?CHAT_GOODS_HORN
        private static readonly Dictionary<int, int> HornCostByScope = new Dictionary<int, int>
        {
            { 1, 1 }, // 本服(TRUMPET_TYPE.WORLD/?SELF_SERVER)
            { 2, 3 }, // 小跨服(TRUMPET_TYPE.SMALL_KUAFU/?SELF_ZONE)
            { 3, 5 }, // 全服(TRUMPET_TYPE.KUAFU/?ALL_SERVER)
        };

        private readonly Dictionary<int, long> _lastSendUnixSec = new Dictionary<int, long>();

        private ChatController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.CHAT_MESSAGE, On11001);
            RegisterProtocal(Proto.CHAT_PRIVATE_MESSAGE, On11002);
            RegisterProtocal(Proto.CHAT_CACHE, On11010);
            RegisterProtocal(Proto.CHAT_ZONE_OPEN, On11023);
            RegisterProtocal(Proto.CHAT_UPLOAD_ZONE_GOODS, On11025);
            RegisterProtocal(Proto.CHAT_CHECK_ZONE_GOODS, On11026);
            RegisterProtocal(Proto.CHAT_CLICK_CACHE, On11027);
            RegisterProtocal(Proto.CHAT_PRIVATE_PLAYER_INFO, On11028);
            RegisterProtocal(Proto.CHAT_HORN_PUSH, On11029);
            RegisterProtocal(Proto.CHAT_BANNED_NOTICE, On11042);
            RegisterProtocal(Proto.CHAT_BLACKLIST_CLEAR, On11046);
            RegisterProtocal(Proto.CHAT_NOTICE, On11050);
            RegisterProtocal(Proto.CHAT_ROBOT, On11064);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            _lastSendUnixSec.Clear();
            ChatModel.Instance.Reset();
            base.Dispose();
        }

        private void OnGameStart()
        {
            ChatModel.Instance.Reset();
            RequestCache(ChatModel.ChannelGuild);
            RequestCache(ChatModel.ChannelWorld);
            ChatModel.Instance.EnsureWelcomeSystemMessage();

            // 11023/11050/11064 均对标老端 GAME_START 空参发一次(11023 老端另有 DAY_CHANGE 复触发,
            // 本端未接日切事件,仅 GAME_START 查一次,TODO 见汇报)。
            SendFmt(Proto.CHAT_ZONE_OPEN);
            SendFmt(Proto.CHAT_NOTICE);
            SendFmt(Proto.CHAT_ROBOT);
        }

        private void RequestCache(int channel)
        {
            SendFmt(Proto.CHAT_CACHE, "c", channel);
        }

        // =====================================================================================
        // 发送侧 API
        // =====================================================================================

        /// <summary>各频道发言统一入口(对标老端 ChatController.ts send_msg + 11001 wire "csslssis")。
        /// receiveId 语义:私聊(channel=<see cref="ChatModel.ChannelPrivate"/>)=对方 role_id;
        /// 喇叭(channel=<see cref="ChatModel.ChannelHorn"/>)=范围选择(1本服/2小跨服/3全服);其余频道传0。
        /// 预校验(空文本/等级/CD/喇叭消耗)对标老端+服务端 lib_chat:check_talk_condition/check_send_msg,
        /// 拦截时只在本地 toast/log,不发包;通过校验才真正 SendFmt(离线态 NetManager 内部已处理,不炸)。
        /// 敏感词本地过滤 TODO(服务端 lib_word 已兜底,本端暂不实现)。</summary>
        public void SendChat(int channel, string text, long receiveId = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                GameLog.Info("Chat", "SendChat 拦截:文本为空 channel={0}", channel);
                return;
            }

            int needLevel = ChannelLevelRequirement.TryGetValue(channel, out int lv) ? lv : DefaultChannelLevel;
            if (RoleModel.Instance.Level < needLevel)
            {
                TipsManager.Toast("等级不足,无法在该频道发言(需求Lv." + needLevel + ")");
                GameLog.Info("Chat", "SendChat 拦截:等级不足 channel={0} need={1} cur={2}", channel, needLevel, RoleModel.Instance.Level);
                return;
            }

            float cd = ChannelCdSeconds.TryGetValue(channel, out float c) ? c : DefaultChannelCd;
            long now = TimeUtil.NowSec();
            if (cd > 0f && _lastSendUnixSec.TryGetValue(channel, out long last) && now - last < (long)cd)
            {
                TipsManager.Toast("发言太频繁,请稍后再试");
                GameLog.Info("Chat", "SendChat 拦截:CD 未到 channel={0} remain={1}s", channel, cd - (now - last));
                return;
            }

            if (channel == ChatModel.ChannelHorn)
            {
                int cost = HornCostByScope.TryGetValue((int)receiveId, out int cnt) ? cnt : 0;
                if (cost > 0 && BagModel.Instance.GetTypeGoodsNum(HornCostGoodsTypeId) < cost)
                {
                    TipsManager.Toast("喇叭卷轴不足,无法发送");
                    GameLog.Info("Chat", "SendChat 拦截:喇叭道具不足 need={0} have={1}", cost, BagModel.Instance.GetTypeGoodsNum(HornCostGoodsTypeId));
                    return;
                }
            }

            // TODO 敏感词本地过滤:老端在此处按 GM/普通用户分两路 filter_text,服务端 pp_chat.erl 已有等价兜底
            // (lib_word:word_is_sensitive + filter_text),本端暂不做本地过滤,交服务端处理。
            _lastSendUnixSec[channel] = now;
            SendFmt(Proto.CHAT_MESSAGE, "csslssis", channel, string.Empty, string.Empty, receiveId, text, string.Empty, 0, string.Empty);
            GameLog.Info("Chat", "send 11001 channel={0} receiveId={1} text={2}", channel, receiveId, text);
        }

        /// <summary>11025 上传跨服频道物品数据(跨服频道发言带物品链接时先发)。</summary>
        public void SendSpecialChannelGoods(int channel, IReadOnlyList<long> goodsIds)
        {
            goodsIds ??= System.Array.Empty<long>();
            var fmt = new StringBuilder("ch");
            var args = new List<object>(2 + goodsIds.Count) { channel, goodsIds.Count };
            foreach (long id in goodsIds)
            {
                fmt.Append('l');
                args.Add(id);
            }
            SendFmt(Proto.CHAT_UPLOAD_ZONE_GOODS, fmt.ToString(), args.ToArray());
            GameLog.Info("Chat", "send 11025 channel={0} goods={1}", channel, goodsIds.Count);
        }

        /// <summary>11026 跨服查看物品(点击跨服频道物品链接时发;recv 是空壳,不指望它反馈结果)。</summary>
        public void SendCheckSpecialChannelGoods(int channel, long goodsId)
        {
            SendFmt(Proto.CHAT_CHECK_ZONE_GOODS, "cl", channel, goodsId);
            GameLog.Info("Chat", "send 11026 channel={0} goodsId={1}", channel, goodsId);
        }

        /// <summary>11027 点击私聊缓存(消红点)。对标老端:发送同时本地立即清未读(reSetPrivateNum),不等回包。</summary>
        public void SendClickCache(long targetRoleId)
        {
            SendFmt(Proto.CHAT_CLICK_CACHE, "cl", ChatModel.ChannelPrivate, targetRoleId);
            ChatModel.Instance.ClearPrivateUnread(targetRoleId);
            GameLog.Info("Chat", "send 11027 targetRoleId={0}", targetRoleId);
        }

        /// <summary>11028 查看私聊玩家信息(打开私聊窗口时发一次)。</summary>
        public void SendViewPrivatePlayerInfo(long roleId)
        {
            SendFmt(Proto.CHAT_PRIVATE_PLAYER_INFO, "l", roleId);
            GameLog.Info("Chat", "send 11028 roleId={0}", roleId);
        }

        /// <summary>11065 聊天监控动态包编号(微信小游戏分包场景专用)。Unity 构建无该平台概念,无自动触发源,
        /// 仅留 API 供未来平台层接入时调用。</summary>
        public void SendMonitorPackageCode(string packageCode)
        {
            if (string.IsNullOrEmpty(packageCode)) return;
            SendFmt(Proto.CHAT_MONITOR_PKG, "s", packageCode);
            GameLog.Info("Chat", "send 11065 packageCode={0}", packageCode);
        }

        // =====================================================================================
        // 接收侧
        // =====================================================================================

        private void On11001(NetReader r)
        {
            ChatMessage message = ReadMessage(r);
            ChatModel.Instance.AddMessage(message);
            GameLog.Info("Chat", "11001 channel={0} player={1} msg={2}", message.Channel, message.PlayerName, message.Message);
        }

        /// <summary>11002 私聊消息推送(双方各收一份完全相同的包,PlayerList 固定2项[发送者,接收者])。</summary>
        private void On11002(NetReader r)
        {
            int channel = r.ReadU8();
            int serverNum = r.ReadU16();
            int serId = r.ReadU16();
            string serName = r.ReadString();
            List<(long id, FigureProto figure)> players = r.ReadArray(rr => (rr.ReadU64(), FigureProto.Read(rr)));
            string msg = r.ReadString();
            string args = r.ReadString();
            int result = r.ReadU8();
            uint time = r.ReadU32();

            if (players.Count == 0)
            {
                GameLog.Warn("Chat", "11002 empty player list");
                return;
            }

            long selfId = RoleModel.Instance.RoleId;
            long senderId = players[0].id;
            long receiverId = players.Count > 1 ? players[1].id : senderId;

            var message = new ChatMessage
            {
                Channel = ChatModel.ChannelPrivate,
                ServerNum = serverNum,
                ServerId = serId,
                ServerName = serName,
                PlayerId = senderId,
                Figure = players[0].figure,
                Message = msg,
                Args = args,
                Result = result,
                Time = time,
            };
            ChatModel.Instance.AddPrivateMessage(selfId, senderId, receiverId, message);
            GameLog.Info("Chat", "11002 private sender={0} receiver={1} msg={2}", senderId, receiverId, msg);
        }

        private void On11010(NetReader r)
        {
            int count = r.ReadU16();
            List<ChatMessage> messages = new List<ChatMessage>(count);
            int channel = -1;
            for (int i = 0; i < count; i++)
            {
                ChatMessage message = ReadCacheMessage(r);
                if (channel < 0) channel = message.Channel;
                messages.Add(message);
            }

            if (channel < 0)
            {
                GameLog.Info("Chat", "11010 empty cache");
                return;
            }

            ChatModel.Instance.SetCache(channel, messages);
            GameLog.Info("Chat", "11010 cache channel={0} count={1}", channel, count);
        }

        /// <summary>11023 是否开启小跨服聊天。</summary>
        private void On11023(NetReader r)
        {
            bool open = r.ReadU8() >= 1;
            ChatModel.Instance.SetZoneOpen(open);
            GameLog.Info("Chat", "11023 zoneOpen={0}", open);
        }

        /// <summary>11025 上传跨服频道物品数据结果(对标老端:error_code!=1 → 提示"跨服无法查看物品需先上传")。</summary>
        private void On11025(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode != 1)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_CHAT_SPECIAL_GOODS_BLOCKED);
                GameLog.Info("Chat", "11025 error_code={0} → 跨服无法查看物品需先上传", errorCode);
            }
        }

        /// <summary>11026 跨服查看物品结果——对标老端 On11026:方法体为空,静默丢弃。真正的物品面板由
        /// mod_kf_chat 异步转发的另一条推送负责,11026 本身只是"已发起查询"回执。</summary>
        private void On11026(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            GameLog.Info("Chat", "11026 error_code={0}(静默,对标老端空处理)", errorCode);
        }

        /// <summary>11027 点击聊天缓存结果(仅失败提示,成功无反馈,对标老端 On11027)。</summary>
        private void On11027(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode != 1) TipsManager.Toast("操作失败(" + errorCode + ")");
            GameLog.Info("Chat", "11027 error_code={0}", errorCode);
        }

        /// <summary>11028 查看私聊玩家信息结果(对标老端 On11028:无错误码判断,原样转发)。</summary>
        private void On11028(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            long roleId = r.ReadU64();
            FigureProto figure = FigureProto.Read(r);
            long combatPower = r.ReadU64();
            bool online = r.ReadU8() != 0;
            int intimacy = (int)r.ReadU32();

            var info = new ChatModel.PrivatePlayerInfo
            {
                RoleId = roleId,
                Figure = figure,
                CombatPower = combatPower,
                Online = online,
                Intimacy = intimacy,
            };
            ChatModel.Instance.SetPrivatePlayerInfo(info);
            GameLog.Info("Chat", "11028 errorCode={0} roleId={1} combat={2} online={3} intimacy={4}",
                errorCode, roleId, combatPower, online, intimacy);
        }

        /// <summary>11029 喇叭广播推送(字段序对标 pt_110.erl write(11029,...))。</summary>
        private void On11029(NetReader r)
        {
            int channel = r.ReadU8();
            int serverNum = r.ReadU16();
            int serId = r.ReadU16();
            string serName = r.ReadString();
            string province = r.ReadString();
            string city = r.ReadString();
            int hornType = r.ReadU8();
            long playerId = r.ReadU64();
            FigureProto figure = FigureProto.Read(r);
            string msg = r.ReadString();
            string args = r.ReadString();
            int result = r.ReadU8();
            uint time = r.ReadU32();

            var message = new ChatMessage
            {
                Channel = channel,
                ServerNum = serverNum,
                ServerId = serId,
                ServerName = serName,
                Province = province,
                City = city,
                HornType = hornType,
                PlayerId = playerId,
                Figure = figure,
                Message = msg,
                Args = args,
                Result = result,
                Time = time,
            };
            ChatModel.Instance.AddHornMessage(message);
            GameLog.Info("Chat", "11029 horn hornType={0} player={1} msg={2}", hornType, playerId, msg);
        }

        /// <summary>11042 被禁言通知——老端漏接,本端补齐(见 Proto.CHAT_BANNED_NOTICE 注释:服务端唯一生产者
        /// 当前无调用点,现状收不到,注册此 handler 只为完整性/未来兜底)。</summary>
        private void On11042(NetReader r)
        {
            long remainSeconds = r.ReadU32();
            TipsManager.Toast("你已被禁言,剩余" + remainSeconds + "秒");
            GameLog.Info("Chat", "11042 被禁言通知(对老端的补齐) remain={0}s", remainSeconds);
        }

        /// <summary>11046 黑名单/清理玩家消息——跳过自己,其余逐个清理(对标老端 On11046)。</summary>
        private void On11046(NetReader r)
        {
            List<long> ids = r.ReadArray(rr => (long)rr.ReadU64());
            long selfId = RoleModel.Instance.RoleId;
            foreach (long id in ids)
            {
                if (id == selfId) continue;
                ChatModel.Instance.ClearRoleChatData(id);
            }
            GameLog.Info("Chat", "11046 blacklist clear count={0}", ids.Count);
        }

        /// <summary>11050 系统公告/跑马灯全量(对标老端 On11050 → StartGongGaoList)。</summary>
        private void On11050(NetReader r)
        {
            List<ChatModel.NoticeEntry> list = r.ReadArray(rr => new ChatModel.NoticeEntry
            {
                Source = rr.ReadString(),
                Type = rr.ReadU8(),
                Color = rr.ReadString(),
                Content = rr.ReadString(),
                Url = rr.ReadString(),
                SendCount = (int)rr.ReadU32(),
                SendGap = rr.ReadU16(),
                StartTime = rr.ReadU32(),
                EndTime = rr.ReadU32(),
                State = rr.ReadU8(),
            });
            ChatModel.Instance.SetNoticeList(list);
            GameLog.Info("Chat", "11050 notice_list count={0}", list.Count);
        }

        /// <summary>11064 假人聊天触发——降级:config_jjc_robot/ClientRobotLv 未迁移入 Unity,不生成假人消息,
        /// 只记录收到的 type(见 Proto.CHAT_ROBOT 注释)。</summary>
        private void On11064(NetReader r)
        {
            int type = r.ReadU8();
            GameLog.Info("Chat", "11064 robot chat type={0}(降级:config_jjc_robot/ClientRobotLv 未迁移,不生成假人消息)", type);
        }

        private static ChatMessage ReadMessage(NetReader r)
        {
            var message = new ChatMessage();
            message.Channel = r.ReadU8();
            message.ServerNum = r.ReadU16();
            message.CrossServerText = r.ReadString();
            message.ServerId = r.ReadU16();
            message.ServerName = r.ReadString();
            message.Province = r.ReadString();
            message.City = r.ReadString();
            message.PlayerId = r.ReadU64();
            message.Figure = FigureProto.Read(r);
            message.Message = r.ReadString();
            message.Args = r.ReadString();
            message.Result = r.ReadU8();
            message.Time = r.ReadU32();
            return message;
        }

        private static ChatMessage ReadCacheMessage(NetReader r)
        {
            var message = new ChatMessage();
            message.Channel = r.ReadU8();
            int playerCount = r.ReadU16();
            for (int i = 0; i < playerCount; i++)
            {
                ChatPlayer player = ReadPlayer(r);
                if (i == 0)
                {
                    message.ServerNum = player.ServerNum;
                    message.CrossServerText = player.CrossServerText;
                    message.ServerId = player.ServerId;
                    message.ServerName = player.ServerName;
                    message.PlayerId = player.PlayerId;
                    message.Figure = player.Figure;
                }
            }

            message.Message = r.ReadString();
            message.Args = r.ReadString();
            message.Result = r.ReadU8();
            message.Time = r.ReadU32();
            message.IsRead = r.ReadU8() != 0;
            message.VoiceId = r.ReadU64();
            message.VoiceTime = r.ReadU16();
            return message;
        }

        private static ChatPlayer ReadPlayer(NetReader r)
        {
            var player = new ChatPlayer();
            player.ServerNum = r.ReadU16();
            player.CrossServerText = r.ReadString();
            player.ServerId = r.ReadU16();
            player.ServerName = r.ReadString();
            player.PlayerId = r.ReadU64();
            player.Figure = FigureProto.Read(r);
            return player;
        }

        private sealed class ChatPlayer
        {
            public int ServerNum;
            public string CrossServerText = "";
            public int ServerId;
            public string ServerName = "";
            public long PlayerId;
            public FigureProto Figure;
        }
    }
}
