using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录公告页签项(对标老客户端 login/LoginNoticeBtnItem.ts):页签文字(labelDisplay)+ 选中态底图(_Image1)。
    ///
    /// SetData(title) + SetSelected(bool)。降级:选中底图(common 图集 uian_001a/b)+ 描边色未移植 →
    /// 文字即时;选中用颜色淡化区分(选中=不透明,未选=半透明)。列表项,由 LoginNoticeView 的 LoopScrowViewMgr 克隆。
    /// </summary>
    public sealed class LoginNoticeBtnItem : LoginNoticeBtnItemBind
    {
        /// <summary>填页签标题(对标 dataChanged 的 data.title)。</summary>
        public void SetData(string title)
        {
            if (labelDisplay != null) labelDisplay.text = title ?? "";
        }

        /// <summary>选中态(对标 selected 的底图切换;底图待 common 图集,先用透明度区分)。</summary>
        public void SetSelected(bool selected)
        {
            if (_Image1 != null) _Image1.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }
    }
}
