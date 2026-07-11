using System.Collections.Generic;

namespace Shenxiao.Module.Core.Mail
{
    /// <summary>
    /// 邮件数据（对标老客户端邮件 Model，自动循环 轮7 补详情缓存/删除/领取语义）。
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

        /// <summary>详情缓存（对标老端 emailInfoDic，19002 缓存优先——命中不重发协议）。</summary>
        private readonly Dictionary<long, MailDetail> _detailCache = new Dictionary<long, MailDetail>();

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

        public MailVo GetById(long mailId) => Mails.Find(m => m.MailId == mailId);

        /// <summary>详情缓存命中查询(对标老端 request_email_info 的缓存优先判定)。</summary>
        public MailDetail GetDetail(long mailId) => _detailCache.TryGetValue(mailId, out MailDetail d) ? d : null;

        /// <summary>19002 回包落地(对标老端 setEmailInfo):写详情缓存 + 若列表项 state!=3 则改成 1(已读)。</summary>
        public void SetDetail(MailDetail detail)
        {
            if (detail == null) return;
            _detailCache[detail.MailId] = detail;
            MailVo vo = GetById(detail.MailId);
            if (vo != null && vo.State != 3) vo.State = 1;
        }

        /// <summary>删除单封(对标老端 deletEmail):清列表项 + 详情缓存。</summary>
        public void DeleteMail(long mailId)
        {
            int i = Mails.FindIndex(m => m.MailId == mailId);
            if (i >= 0) Mails.RemoveAt(i);
            _detailCache.Remove(mailId);
        }

        /// <summary>19005 领取成功(对标老端 getEmailReward):对应邮件 state 改3 + 详情缓存(若存在) is_receive=1。</summary>
        public void MarkReceived(IReadOnlyList<long> mailIds)
        {
            if (mailIds == null) return;
            foreach (long id in mailIds)
            {
                MailVo vo = GetById(id);
                if (vo != null) vo.State = 3;
                if (_detailCache.TryGetValue(id, out MailDetail d)) d.IsReceive = 1;
            }
        }

        /// <summary>可删除邮件(对标老端 GetNoGetRewardEmailList):无附件、或有附件但已领取——
        /// 绝不包含"有未领取附件"的邮件,一键删除保护逻辑必须复刻此过滤,否则会误删未领奖励。</summary>
        public List<MailVo> GetNoGetRewardEmailList()
        {
            var list = new List<MailVo>();
            foreach (MailVo vo in Mails)
            {
                if (vo.IsAttach == 0 || (vo.IsAttach == 1 && vo.State == 3)) list.Add(vo);
            }
            return list;
        }

        /// <summary>待领取附件的全部邮件(对标老端 allRewardEmailList,一键领取用)。</summary>
        public List<MailVo> AllRewardEmailList()
        {
            var list = new List<MailVo>();
            foreach (MailVo vo in Mails)
            {
                if (vo.IsAttach == 1 && vo.State != 3) list.Add(vo);
            }
            return list;
        }

        /// <summary>是否还有未领取附件的邮件(对标老端 emailHasNoGetReward,取反语义见方法名)。</summary>
        public bool HasUnclaimedAttachment()
        {
            foreach (MailVo vo in Mails)
            {
                if (vo.IsAttach == 1 && vo.State != 3) return true;
            }
            return false;
        }

        /// <summary>下一封待处理邮件 id(对标老端 nextMailId,领取/已读后自动跳转用;无则 -1)。</summary>
        public long NextMailId()
        {
            long id = -1;
            foreach (MailVo vo in Mails)
            {
                if ((vo.IsAttach == 1 && vo.State != 3) || (vo.IsAttach == 0 && vo.State == 2)) id = vo.MailId;
            }
            return id;
        }

        public void Clear()
        {
            Mails.Clear();
            HasUnread = false;
            LeftNum = 0;
            _detailCache.Clear();
        }
    }
}
