using Shenxiao.Generated.UI.MainStronger;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>
    /// 变强对话板(对标老客户端 mainStronger/MainUIStrongerTalkBoard):气泡文本(content 纯文本 / htmlContent 富文本)。
    ///
    /// SetContent(text)。降级:富文本(htmlContent)未移植 → 用 content 显纯文本、htmlContent 隐藏。
    /// </summary>
    public sealed class MainUIStrongerTalkBoard : MainUIStrongerTalkBoardBind
    {
        protected override void OnInit()
        {
            if (htmlContent != null) htmlContent.gameObject.SetActive(false); // 富文本待接
        }

        public void SetContent(string text)
        {
            if (content != null) content.text = text ?? "";
        }
    }
}
