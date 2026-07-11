using System.Collections.Generic;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Mail;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 邮件界面(对标老客户端 friend/EmailView.ts):邮件列表(itemScroller/Content 克隆 EmailItem)+ 空态(nullGroup)+
    /// 一键删除(btnDelet,19003,只删无附件/已领取邮件)+ 一键领取(btnGet,19005,背包容量前置校验)。
    /// 由二级 HUD 邮件按钮打开(FriendModule 内顶层窗,经 FriendFlow.ToggleEmail)。
    /// </summary>
    public sealed class EmailView : EmailViewBind
    {
        private readonly List<EmailItem> _pool = new List<EmailItem>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_EmailItem != null) _tpl_EmailItem.SetActive(false);
            UIUtil.AddClick(btnDelet, () => MailController.Instance.RequestDeleteAll());
            UIUtil.AddClick(btnGet, () => MailController.Instance.RequestReceiveAll());
            MailController.Instance.RequestMailList();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            RefreshList();
        }

        protected override void OnHide() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_MAIL_LIST_UPDATE, RefreshList);
            EventDispatcher.On<long>(GlobalEvent.EVT_MAIL_DETAIL_READY, OnDetailReady);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_MAIL_LIST_UPDATE, RefreshList);
            EventDispatcher.Off<long>(GlobalEvent.EVT_MAIL_DETAIL_READY, OnDetailReady);
        }

        /// <summary>19002 详情就绪(缓存命中或回包落地都会走这里)→ 打开详情弹窗(对标老端 OPEN_EMAIL_VIEW)。</summary>
        private void OnDetailReady(long mailId) => FriendFlow.OpenSub("EmailPopView", mailId);

        private void RefreshList()
        {
            List<MailVo> list = new List<MailVo>(MailModel.Instance.Mails);
            list.Sort(CompareMail);
            EnsurePool(list.Count);
            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < list.Count;
                _pool[i].gameObject.SetActive(active);
                if (active) _pool[i].SetData(list[i]);
            }
            if (nullGroup != null) nullGroup.gameObject.SetActive(list.Count == 0);
        }

        private void EnsurePool(int count)
        {
            if (_tpl_EmailItem == null || Content == null) return;
            while (_pool.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_EmailItem, Content);
                go.SetActive(true);
                _pool.Add(go.GetComponent<EmailItem>());
            }
        }

        /// <summary>对标老端 FriendModel.sortEmail(未读优先、新的优先、未领附件优先、已领取靠后)。</summary>
        private static int CompareMail(MailVo a, MailVo b)
        {
            int aIndex = 500, bIndex = 500;
            if (a.State == 2) aIndex += 100;
            if (b.State == 2) bIndex += 100;
            if (a.Time != b.Time)
            {
                if (a.Time > b.Time) aIndex += 100; else bIndex += 100;
            }
            if (a.State == 1 && a.Time > b.Time) aIndex -= 100;
            if (b.State == 1 && b.Time > a.Time) bIndex -= 100;
            if (a.State == 1 && a.IsAttach == 1) aIndex += 50;
            if (b.State == 1 && b.IsAttach == 1) bIndex += 50;
            if (a.State == 3) aIndex -= 100;
            if (b.State == 3) bIndex -= 100;
            return bIndex.CompareTo(aIndex); // 降序:aIndex 越大越靠前
        }
    }
}
