using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Mail
{
    /// <summary>
    /// 邮件协议接收（对标 yu_server pt_190 / 老客户端邮件分支）：
    ///   19001 邮件列表（h + {MailId:l,Type:c,State:c,Title:s,IsAttach:c,Time:i,EffectEt:i}×N）；
    ///   19007 新邮件推送（单封，同列表项格式）；19008 是否有未读(c)；19009 可发剩余(c)。
    /// 解析落 <see cref="MailModel"/> 并发事件；详情(19002)/删除(19003)/领取(19005) 待后续。列表/红点 UI 待验收。
    /// </summary>
    public sealed class MailController : BaseController
    {
        public static readonly MailController Instance = new MailController();
        private MailController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.MAIL_LIST, On19001);
            RegisterProtocal(Proto.MAIL_NEW, On19007);
            RegisterProtocal(Proto.MAIL_UNREAD, On19008);
            RegisterProtocal(Proto.MAIL_LEFT_NUM, On19009);
        }

        /// <summary>请求邮件列表（无参，回包 19001）。</summary>
        public void RequestMailList() => SendFmt(Proto.MAIL_LIST);

        private void On19001(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<MailVo>(count);
            for (int i = 0; i < count; i++)
            {
                var vo = new MailVo();
                vo.ReadFromProtocal(r);
                list.Add(vo);
            }
            MailModel.Instance.SetMailList(list);
            GameLog.Info("Mail", "19001 邮件列表: {0} 封", count);
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_LIST_UPDATE);
        }

        private void On19007(NetReader r)
        {
            var vo = new MailVo();
            vo.ReadFromProtocal(r);
            MailModel.Instance.AddOrUpdate(vo);
            GameLog.Info("Mail", "19007 新邮件: id={0} {1}", vo.MailId, vo.Title);
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_LIST_UPDATE);
        }

        private void On19008(NetReader r)
        {
            MailModel.Instance.HasUnread = r.ReadU8() != 0;
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_UNREAD_UPDATE);
        }

        private void On19009(NetReader r)
        {
            MailModel.Instance.LeftNum = r.ReadU8();
        }
    }
}
