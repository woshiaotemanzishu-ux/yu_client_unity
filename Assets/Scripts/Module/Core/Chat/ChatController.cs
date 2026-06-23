using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Chat
{
    public sealed class ChatController : BaseController
    {
        public static readonly ChatController Instance = new ChatController();

        private ChatController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.CHAT_MESSAGE, On11001);
            RegisterProtocal(Proto.CHAT_CACHE, On11010);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            ChatModel.Instance.Reset();
            base.Dispose();
        }

        private void OnGameStart()
        {
            ChatModel.Instance.Reset();
            RequestCache(ChatModel.ChannelGuild);
            RequestCache(ChatModel.ChannelWorld);
            ChatModel.Instance.EnsureWelcomeSystemMessage();
        }

        private void RequestCache(int channel)
        {
            SendFmt(Proto.CHAT_CACHE, "c", channel);
        }

        private void On11001(NetReader r)
        {
            ChatMessage message = ReadMessage(r);
            ChatModel.Instance.AddMessage(message);
            GameLog.Info("Chat", "11001 channel={0} player={1} msg={2}", message.Channel, message.PlayerName, message.Message);
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

        private static ChatMessage ReadMessage(NetReader r)
        {
            var message = new ChatMessage();
            message.Channel = r.ReadU8();
            message.ServerNum = r.ReadU16();
            message.CrossServerText = r.ReadString();
            message.ServerId = r.ReadU16();
            message.ServerName = r.ReadString();
            r.ReadString(); // province
            r.ReadString(); // city
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
