using System.Text.RegularExpressions;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面聊天/系统消息项。频道徽标仍是独立 Image，但正文用 TMP 的首行 space 标签给徽标留位：
    /// 第一行从徽标右侧开始，换行后回到整行左侧，对标老端 HTML 内联频道图的排版。
    /// </summary>
    public sealed class MainUIChatItem : MainUIChatItemBind
    {
        public const float SingleLineHeight = 29f;
        private const float ContentBottomPadding = 4f;
        private const string BadgeIndent = "<space=46px>";

        private static readonly Regex FontColorTag = new Regex(
            "<font\\s+color\\s*=\\s*['\"]?(#[0-9a-fA-F]{6,8})['\"]?\\s*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LegacyColorTag = new Regex(
            "<color@(\\d+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LegacyLinkOpenTag = new Regex(
            "<a@[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LegacyFaceTag = new Regex(
            "<f_\\d+>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void SetData(ChatMessage message)
        {
            if (message == null) return;

            int channel = message.Channel;
            string icon = GetChannelIcon(channel);
            bool hasBadge = !string.IsNullOrEmpty(icon);

            if (titleBg != null)
            {
                titleBg.gameObject.SetActive(hasBadge);
                if (hasBadge)
                    _ = ResManager.SetImageAsync(titleBg, GameResPath.GetIcon("mainUI", icon), nativeSize: false);
            }

            // 当前已迁移频道都使用带文字的频道徽标图。未知频道才用纯文字兜底，不能图文叠两层。
            if (title != null)
            {
                bool showFallbackTitle = !hasBadge;
                title.gameObject.SetActive(showFallbackTitle);
                if (showFallbackTitle) title.text = ChatModel.ChannelLabel(channel);
            }

            if (_img_trumpet != null) _img_trumpet.gameObject.SetActive(false);

            if (contentLabel != null)
            {
                contentLabel.richText = true;
                contentLabel.textWrappingMode = TextWrappingModes.Normal;
                contentLabel.text = (hasBadge ? BadgeIndent : string.Empty) + BuildMessageText(message);
                RefreshPreferredHeight();
            }
        }

        private void RefreshPreferredHeight()
        {
            RectTransform itemRect = transform as RectTransform;
            if (itemRect == null || contentLabel == null) return;

            RectTransform textRect = contentLabel.rectTransform;
            float width = textRect.rect.width;
            if (width <= 0f) width = textRect.sizeDelta.x;
            if (width <= 0f) width = 381f;

            float preferred = contentLabel.GetPreferredValues(contentLabel.text, width, 0f).y;
            float height = Mathf.Max(SingleLineHeight, Mathf.Ceil(preferred + ContentBottomPadding));
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout != null) layout.preferredHeight = height;
        }

        private static string BuildMessageText(ChatMessage message)
        {
            string content = !string.IsNullOrEmpty(message.Message) ? message.Message : message.Args;
            content = FormatLegacyRichText(content);

            if (message.Channel == ChatModel.ChannelSystem || string.IsNullOrEmpty(message.PlayerName))
                return content;

            string playerName = EscapeRichText(message.PlayerName.Trim());
            string color = GetChannelTextColor(message.Channel);
            return "<color=" + color + ">" + playerName + "：</color>" + content;
        }

        /// <summary>把老端聊天常见 HTML/自定义标签转换成 TMP 可显示的最小富文本子集。</summary>
        public static string FormatLegacyRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string result = text.Trim();
            result = FontColorTag.Replace(result, match => "<color=" + match.Groups[1].Value + ">");
            result = Regex.Replace(result, "</font>", "</color>", RegexOptions.IgnoreCase);
            result = LegacyColorTag.Replace(result, match => "<color=" + GetLegacyColor(match.Groups[1].Value) + ">");
            result = Regex.Replace(result, "</a>", string.Empty, RegexOptions.IgnoreCase);
            result = LegacyLinkOpenTag.Replace(result, string.Empty);
            result = LegacyFaceTag.Replace(result, string.Empty);
            return result.Replace("&nbsp;", "\u00A0");
        }

        private static string EscapeRichText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("<", "＜").Replace(">", "＞");
        }

        private static string GetChannelIcon(int channel)
        {
            switch (channel)
            {
                case ChatModel.ChannelSystem: return "mainUI_chat_1";
                case ChatModel.ChannelWorld: return "mainUI_chat_2";
                case ChatModel.ChannelGuild: return "mainUI_chat_3";
                case ChatModel.ChannelSmallKuafu: return "mainUI_chat_4";
                case ChatModel.ChannelWorldKuafu: return "mainUI_chat_5";
                case ChatModel.ChannelCamp: return "mainUI_chat_6";
                case ChatModel.ChannelSea: return "mainUI_chat_7";
                case ChatModel.ChannelTeam: return "mainUI_chat_9";
                default: return string.Empty;
            }
        }

        private static string GetChannelTextColor(int channel)
        {
            switch (channel)
            {
                case ChatModel.ChannelWorld: return "#5E73F3";
                case ChatModel.ChannelSmallKuafu: return "#B647E1";
                case ChatModel.ChannelWorldKuafu: return "#706ECA";
                case ChatModel.ChannelTeam: return "#B3FF48";
                case ChatModel.ChannelGuild: return "#D85051";
                case ChatModel.ChannelCamp: return "#27B24C";
                case ChatModel.ChannelSea: return "#B8741A";
                default: return "#FFFFFF";
            }
        }

        private static string GetLegacyColor(string rawType)
        {
            switch (rawType)
            {
                case "1": return "#4EC279";
                case "2": return "#5AAFF0";
                case "3": return "#C36FF2";
                case "4": return "#F88452";
                case "5": return "#FA4D4D";
                case "6": return "#FFBC3D";
                case "7": return "#FF72C2";
                case "8": return "#9C9C9C";
                default: return "#FEFAF0";
            }
        }
    }
}
