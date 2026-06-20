using System.Text.RegularExpressions;
using Shenxiao.Generated.UI.Svip;

namespace Shenxiao.Module.Core.Svip
{
    /// <summary>
    /// 至尊VIP特权项(对标老客户端 svip/SvipMainItem.ts):特权图标(image_privilege)+ 特权说明(html_content)。
    ///
    /// SetData(content)。降级:特权图标(配置)待接、HTML 富文本未移植 → html_content 直显纯文本(去 0xRRGGBB 色码)。
    /// 由 SvipMainView 克隆。
    /// </summary>
    public sealed class SvipMainItem : SvipMainItemBind
    {
        public void SetData(string content)
        {
            if (html_content == null) return;
            string plain = (content ?? "").Replace("\n@@", "\n");
            plain = Regex.Replace(plain, "0x[0-9a-fA-F]{6}", "");
            html_content.text = plain;
        }
    }
}
