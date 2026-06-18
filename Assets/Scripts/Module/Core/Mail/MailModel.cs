using System.Collections.Generic;

namespace Shenxiao.Module.Core.Mail
{
    /// <summary>
    /// 邮件数据（对标老客户端邮件 Model）。只存数据；列表/红点 UI 待用户验收时接（订阅事件读这里）。
    /// </summary>
    public sealed class MailModel
    {
        public static readonly MailModel Instance = new MailModel();
        private MailModel() { }

        /// <summary>邮件摘要列表（越新越靠前）。</summary>
        public readonly List<MailVo> Mails = new List<MailVo>();

        /// <summary>是否有未读邮件（19008，红点用）。</summary>
        public bool HasUnread;

        /// <summary>可发邮件剩余次数（19009）。</summary>
        public int LeftNum;

        public void SetMailList(List<MailVo> list)
        {
            Mails.Clear();
            if (list != null) Mails.AddRange(list);
        }

        public void AddOrUpdate(MailVo vo)
        {
            if (vo == null) return;
            int i = Mails.FindIndex(m => m.MailId == vo.MailId);
            if (i >= 0) Mails[i] = vo;
            else Mails.Insert(0, vo);
        }

        public void Clear()
        {
            Mails.Clear();
            HasUnread = false;
            LeftNum = 0;
        }
    }
}
