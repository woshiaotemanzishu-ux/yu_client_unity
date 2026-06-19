using System.Text.RegularExpressions;
using Shenxiao.Generated.UI.GameNotice;

namespace Shenxiao.Module.Core.GameNotice
{
    /// <summary>
    /// 游戏公告内容项(对标老客户端 gameNotice/GameNoticeContentItem.ts):标题(_lb_title)+ 详情(_gp_details 动态行)。
    ///
    /// SetData(content)。降级:HTML 富文本/详情动态行未移植 → _lb_title 直显纯文本(\n@@→换行、去 0xRRGGBB 色码)。
    /// 由 GameNoticeView 克隆。
    /// </summary>
    public sealed class GameNoticeContentItem : GameNoticeContentItemBind
    {
        public void SetData(string content)
        {
            if (_lb_title == null) return;
            if (string.IsNullOrEmpty(content)) { _lb_title.text = ""; return; }
            string plain = content.Replace("\n@@", "\n");
            plain = Regex.Replace(plain, "0x[0-9a-fA-F]{6}", "");
            _lb_title.text = plain;
        }
    }
}
