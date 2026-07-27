using System.Text.RegularExpressions;
using UnityEngine;

namespace Shenxiao.Common.UI
{
    /// <summary>
    /// 老客户端 ColorUtil 的颜色语义。迁移期配置文本仍会携带 &lt;color@N&gt; 标签，
    /// 统一在这里转换成 TMP 富文本，避免各弹层自行猜颜色或直接把标签剥掉。
    /// </summary>
    public static class LegacyUiColor
    {
        private static readonly string[] Light =
        {
            "#fefaf0", "#4ec279", "#5aaff0", "#c36ff2", "#f88452",
            "#fa4d4d", "#ffbc3d", "#ff72c2", "#9c9c9c",
        };

        private static readonly string[] Dark =
        {
            "#663915", "#3cad66", "#5099dd", "#b55eec", "#e17547",
            "#ef4848", "#cd9222", "#f56ebd", "#8a8a8a",
        };

        private static readonly Regex ColorTag =
            new Regex("<color@(\\d+)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetHtml(int colorType, bool light)
        {
            string[] palette = light ? Light : Dark;
            return colorType >= 0 && colorType < palette.Length ? palette[colorType] : palette[0];
        }

        public static Color GetColor(int colorType, bool light)
        {
            return ColorUtility.TryParseHtmlString(GetHtml(colorType, light), out Color color)
                ? color
                : Color.white;
        }

        public static string ToTmpRichText(string text, bool light)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return ColorTag.Replace(text, match =>
            {
                return int.TryParse(match.Groups[1].Value, out int colorType)
                    ? "<color=" + GetHtml(colorType, light) + ">"
                    : string.Empty;
            });
        }
    }
}
