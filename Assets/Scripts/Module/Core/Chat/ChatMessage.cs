using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Chat
{
    public sealed class ChatMessage
    {
        public int Channel;
        public int ServerNum;
        public string CrossServerText = "";
        public int ServerId;
        public string ServerName = "";
        public long PlayerId;
        public FigureProto Figure;
        public string Message = "";
        public string Args = "";
        public int Result = 1;
        public uint Time;
        public bool IsRead;
        public long VoiceId;
        public int VoiceTime;

        public string PlayerName => Figure != null && !string.IsNullOrEmpty(Figure.name) ? Figure.name : "";
        public int Level => Figure?.level ?? 0;
    }
}
