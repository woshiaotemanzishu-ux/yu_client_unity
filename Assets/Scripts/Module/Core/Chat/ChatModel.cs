using System.Collections.Generic;
using Shenxiao.Framework.Event;

namespace Shenxiao.Module.Core.Chat
{
    public sealed class ChatModel
    {
        public const int ChannelWorld = 0;
        public const int ChannelGuild = 4;
        public const int ChannelTeam = 5;
        public const int ChannelSystem = 10;
        public const int ChannelWorldKuafu = 13;
        public const int ChannelCamp = 15;
        public const int ChannelSmallKuafu = 17;
        public const int ChannelSea = 19;

        public static readonly ChatModel Instance = new ChatModel();

        public const string WelcomeSystemMessage =
            "\u6b22\u8fce\u8e0f\u5165\u4e5d\u5dde\u5927\u8352\u3002\u795e\u9704\u5d16\u706d\u540e\uff0c\u5929\u6b8b\u9057\u9ab8\u5316\u4f5c\u9053\u75d5\uff0c\u79d8\u5883\u5f15\u52ab\u800c\u751f\u2014\u2014\u613f\u541b\u878d\u75d5\u8bc1\u9053\uff0c\u5386\u5c3d\u4e5d\u5929\u68af\u52ab\u3002";

        private static readonly ChatMessage[] EmptyMessages = new ChatMessage[0];
        private readonly Dictionary<int, List<ChatMessage>> _messages = new Dictionary<int, List<ChatMessage>>();

        private ChatModel() { }

        public void Reset()
        {
            _messages.Clear();
        }

        public IReadOnlyList<ChatMessage> GetMessages(int channel)
        {
            return _messages.TryGetValue(channel, out List<ChatMessage> list) ? list : EmptyMessages;
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
                case ChannelWorld: return "\u4e16\u754c";
                case ChannelGuild: return "\u4ed9\u5b97";
                case ChannelTeam: return "\u961f\u4f0d";
                case ChannelSystem: return "\u7cfb\u7edf";
                case ChannelWorldKuafu: return "\u6d3b\u52a8";
                case ChannelCamp: return "\u9635\u8425";
                case ChannelSmallKuafu: return "\u8de8\u670d";
                case ChannelSea: return "\u6ca7\u6d77\u8206\u56fe";
                default: return channel.ToString();
            }
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
    }
}
