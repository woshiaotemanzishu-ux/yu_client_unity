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

        // ----- 聊天补全(自动循环 轮6)扩展字段 -----
        /// <summary>私聊(11002):计算得出的"对方" role_id(不含自己),对标老端 setChatData 的 targetId。
        /// 仅私聊消息有效,由 ChatModel.AddPrivateMessage 回填。</summary>
        public long TargetPlayerId;
        /// <summary>喇叭(11029):范围类型(1本服/2小跨服/3全服,对标 TRUMPET_TYPE),仅喇叭消息有效。</summary>
        public int HornType;
        /// <summary>是否喇叭消息(对标老端 is_trumpet)。</summary>
        public bool IsHorn;
        /// <summary>喇叭(11029)发送者省/市,普通频道消息恒为空串。</summary>
        public string Province = "";
        public string City = "";

        public string PlayerName => Figure != null && !string.IsNullOrEmpty(Figure.name) ? Figure.name : "";
        public int Level => Figure?.level ?? 0;
    }
}
