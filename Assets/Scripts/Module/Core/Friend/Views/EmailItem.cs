using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Mail;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 邮件列表条目(对标老客户端 friend/EmailItem.ts):标题 + 时间 + 已读/未读图标 + 附件领取态图标(rewardImg=
    /// 待领取,getImg=已领取,均无附件时两者都不显)+ 整行点击(请求 19002 详情,缓存命中不重发协议)。
    /// 由 <see cref="EmailView"/> 克隆 <see cref="EmailViewBind._tpl_EmailItem"/> 铺列表。
    ///
    /// 降级:标题/正文老端会走 EmailTextRemapHelper 做"旧邮件库快照文案→仙侠化"替换,该表未移植 → 原样显示,TODO。
    /// </summary>
    public sealed class EmailItem : EmailItemBind
    {
        private MailVo _vo;
        private bool _inited;

        protected override void OnInit() => EnsureInit();

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            UIUtil.AddClick(click, OnClick);
        }

        public void SetData(MailVo vo)
        {
            EnsureInit();
            _vo = vo;
            if (vo == null) return;

            if (title != null) title.text = vo.Title;
            if (time != null) time.text = FriendUiUtil.FormatDateTime(vo.Time);

            bool isRead = vo.State == 1 || vo.State == 3;
            if (read != null) read.gameObject.SetActive(isRead);
            if (unread != null) unread.gameObject.SetActive(!isRead);

            bool hasUnclaimed = vo.IsAttach == 1 && vo.State != 3;
            if (rewardImg != null) rewardImg.gameObject.SetActive(hasUnclaimed);
            bool showClaimedIcon = !hasUnclaimed && vo.IsAttach == 1; // 无附件邮件两个图标都不显示
            if (getImg != null) getImg.gameObject.SetActive(showClaimedIcon);
        }

        private void OnClick()
        {
            if (_vo == null) return;
            MailController.Instance.RequestMailDetail(_vo.MailId);
        }
    }
}
