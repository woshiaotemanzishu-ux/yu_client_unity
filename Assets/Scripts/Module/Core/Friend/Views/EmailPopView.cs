using System.Collections.Generic;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Mail;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 邮件详情弹窗(对标老客户端 friend/EmailPopView.ts):发件人/标题/正文 + 附件横向列表(EmailPopViewItem)+
    /// 领取按钮(btnGet,19005 单封领取,已领取则变"下一封/关闭")+ 无附件邮件走 btnClose2("下一封/关闭")+
    /// 右上角 close(直接关闭,不联动下一封)。由 <see cref="EmailItem"/> 点击(经 19002 缓存优先)携 mailId 打开。
    ///
    /// 降级:正文富文本(彩色词条/物品超链接,老端 formatEmailContent + HTMLDivElement LINK 事件)未接——
    /// 本工程 content 字段是纯 TMP 文本,只显示原始 Content 字符串,不解析 color@/a@ 标记,TODO。
    /// </summary>
    public sealed class EmailPopView : EmailPopViewBind
    {
        private long _mailId;
        private MailDetail _detail;
        private readonly List<EmailPopViewItem> _pool = new List<EmailPopViewItem>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_EmailPopViewItem != null) _tpl_EmailPopViewItem.SetActive(false);
            UIUtil.AddClick(close, Hide);
            UIUtil.AddClick(btnClose2, OnClickCloseOrNext);
            UIUtil.AddClick(btnGet, OnClickGet);
        }

        protected override void OnShow(object args)
        {
            if (args is long id) _mailId = id;
            Subscribe();
            RefreshInfo();
        }

        protected override void OnHide() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<List<(int style, int typeId, int count)>>(GlobalEvent.EVT_MAIL_RECEIVE_REWARD, OnRewardUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<List<(int style, int typeId, int count)>>(GlobalEvent.EVT_MAIL_RECEIVE_REWARD, OnRewardUpdate);
        }

        private void OnRewardUpdate(List<(int style, int typeId, int count)> rewards)
        {
            if (IsShown) RefreshInfo();
        }

        private void RefreshInfo()
        {
            _detail = MailModel.Instance.GetDetail(_mailId);
            if (_detail == null)
            {
                GameLog.Warn("Friend", "EmailPopView 打开但详情缓存为空 mailId={0}", _mailId);
                Hide();
                return;
            }

            if (sendName != null) sendName.text = _detail.Sender;
            if (title != null) title.text = _detail.Title;
            if (content != null) content.text = _detail.Content;

            bool hasAttach = _detail.Attachment.Count > 0;
            if (btnGet != null) btnGet.gameObject.SetActive(hasAttach);
            if (Image111 != null) Image111.gameObject.SetActive(hasAttach);
            if (btnClose2 != null) btnClose2.gameObject.SetActive(!hasAttach);

            long nextId = MailModel.Instance.NextMailId();
            bool hasNext = nextId > -1 && nextId != _mailId;
            if (hasAttach && _detail.IsReceive == 0)
            {
                if (_Label_get != null) _Label_get.text = "领取";
            }
            else
            {
                string label = hasNext ? "下一封" : "关闭";
                if (_Label_get != null) _Label_get.text = label;
                if (_Label_close != null) _Label_close.text = label;
            }

            EnsurePool(_detail.Attachment.Count);
            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < _detail.Attachment.Count;
                _pool[i].gameObject.SetActive(active);
                if (active) _pool[i].SetData(_detail.Attachment[i]);
            }
        }

        private void EnsurePool(int count)
        {
            if (_tpl_EmailPopViewItem == null || Content_list == null) return;
            // ScrollRect.content 根兜底(轮3 三坑之一):content 引用未序列化时退用 ScrollRect 自身 transform,
            // 避免克隆目标为 null 直接跳过铺格。
            Transform parent = Content_list.content != null ? Content_list.content : Content_list.transform;
            while (_pool.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_EmailPopViewItem, parent);
                go.SetActive(true);
                _pool.Add(go.GetComponent<EmailPopViewItem>());
            }
        }

        private void OnClickGet()
        {
            if (_detail == null) return;
            if (_detail.IsReceive == 1)
            {
                long nextId = MailModel.Instance.NextMailId();
                Hide();
                if (nextId > -1 && nextId != _mailId) MailController.Instance.RequestMailDetail(nextId);
                return;
            }
            MailController.Instance.RequestReceiveOne(_mailId);
        }

        private void OnClickCloseOrNext()
        {
            long nextId = MailModel.Instance.NextMailId();
            Hide();
            if (nextId > -1 && nextId != _mailId) MailController.Instance.RequestMailDetail(nextId);
        }
    }
}
