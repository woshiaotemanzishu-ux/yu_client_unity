using Shenxiao.Generated.UI.MainUI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 对话框气泡(对标老客户端 mainUI/MainUIStrongerTalkBoard.ts : scene/sceneboard/TalkBoard):
    /// 活动图标旁的文字气泡(模板),显示一段文案。
    ///
    /// SetContent(text) 填文案(&lt;br/&gt; → 换行)。降级:老端按文本上下文尺寸自适应 content_bg 的逻辑,
    /// 以及 base TalkBoard 的 id/pos/计时自动关闭(场景气泡管理)未移植 → 自适应/定位/出现时机归预制体
    /// (建议 content_bg 挂 ContentSizeFitter)/调用方。事件驱动(图标气泡),默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class TalkBoard : TalkBoardBind
    {
        /// <summary>填气泡文案(对标 SetContent;&lt;br/&gt; 转换行)。</summary>
        public void SetContent(string text)
        {
            if (content != null) content.text = (text ?? "").Replace("<br/>", "\n");
        }
    }
}
