using Shenxiao.Framework.Util;

namespace Shenxiao.Common.ChatBubble
{
    /// <summary>Overhead chat bubble manager. Phase 0 skeleton.</summary>
    public static class ChatBubbleManager
    {
        public static void Show(int characterId, string text)
        {
            GameLog.Info("Bubble", "char={0} text={1}", characterId, text);
        }
    }
}
