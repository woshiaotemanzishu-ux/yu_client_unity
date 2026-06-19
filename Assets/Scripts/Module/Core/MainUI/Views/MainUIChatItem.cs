using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面聊天/系统消息项(对标老客户端 MainUIChatItem.ts dataChanged)。
    ///
    /// 老端按频道(系统/世界/喇叭/结社…)做富文本拼接:频道图标 titleBg + 颜色标签 + 表情(disposeChtaFace)
    /// + 超链接 + FormatColorTag,全依赖 ChatModel(未移植)。降级:用最小 DTO 显示「频道图标(可选)+
    /// 纯消息文本」;频道颜色/表情/超链接/喇叭特殊态先不处理。ChatModel 移植后再补频道格式化与点击打开
    /// 聊天面板(OPEN_CHAT_VIEW)。
    ///
    /// 注:本项是 MainUIChatView 内 _tpl_MainUIChatItem 模板的业务脚本;它继承自 Bind,故 ChatView 里
    /// 既有的 GetComponent&lt;MainUIChatItemBind&gt; 用法不受影响(子类 is-a Bind)。
    /// </summary>
    public sealed class MainUIChatItem : MainUIChatItemBind
    {
        /// <summary>填一条聊天/系统消息(降级:纯文本 + 可选频道图标/名)。</summary>
        public void SetData(ChatItemData data)
        {
            if (data == null) return;

            if (contentLabel != null)
            {
                contentLabel.richText = true;
                contentLabel.text = data.Message ?? "";
            }

            // 频道名标签:有就显示,无则隐藏(老端多数频道不显 title 文本,靠 titleBg 图标)。
            if (title != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(data.ChannelName);
                title.gameObject.SetActive(hasTitle);
                if (hasTitle) title.text = data.ChannelName;
            }

            // 频道图标 titleBg(对标 channelImage 的 src,如系统 "mainUI_chat_1")。
            if (titleBg != null)
            {
                bool hasIcon = !string.IsNullOrEmpty(data.TitleIcon);
                titleBg.gameObject.SetActive(hasIcon);
                if (hasIcon)
                    _ = ResManager.SetImageAsync(titleBg, GameResPath.GetIcon("mainUI", data.TitleIcon), nativeSize: false);
            }

            // 喇叭特殊态依赖 ChatModel,未移植 → 隐藏。
            if (_img_trumpet != null) _img_trumpet.gameObject.SetActive(false);
        }
    }

    /// <summary>聊天项最小数据(待 ChatModel 移植后由真实消息/频道填充)。</summary>
    public sealed class ChatItemData
    {
        public string Message;
        public string ChannelName;  // 可选:频道名(系统/世界/结社…)
        public string TitleIcon;    // 可选:频道图标(mainUI atlas,如 mainUI_chat_1)
    }
}
