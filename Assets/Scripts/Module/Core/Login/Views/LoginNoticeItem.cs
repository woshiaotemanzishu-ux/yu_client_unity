using System.Text.RegularExpressions;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录公告内容项(对标老客户端 login/LoginNoticeItem.ts):标题(_lb_title)+ 详情(_gp_details 动态 HTML 行)。
    ///
    /// 老端 SetData 按 "\n@@" 切成标题+多行详情,详情用 HTMLDivElement 渲染富文本(含 0x 颜色码)。
    /// 降级:HTML 富文本/颜色码/微信二维码未移植 → _lb_title 直显纯文本(\n@@→换行、去 0xRRGGBB 颜色码),
    /// _gp_details 动态行待接。列表项,由 LoginNoticeView 克隆。
    /// </summary>
    public sealed class LoginNoticeItem : LoginNoticeItemBind
    {
        /// <summary>填公告内容(对标 SetData,降级为纯文本)。</summary>
        public void SetData(string content)
        {
            if (_lb_title == null) return;
            if (string.IsNullOrEmpty(content)) { _lb_title.text = ""; return; }
            string plain = content.Replace("\n@@", "\n");
            plain = Regex.Replace(plain, "0x[0-9a-fA-F]{6}", ""); // 去颜色码
            _lb_title.text = plain;
        }
    }
}
