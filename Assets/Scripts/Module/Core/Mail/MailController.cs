using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Mail
{
    /// <summary>
    /// 邮件协议接收（对标 yu_server pt_190 / 老客户端 commonController/FriendController.ts 邮件段,
    /// 自动循环 轮7 扩容详情/删除/领取/公会邮件/意见反馈）：
    ///   19001 邮件列表；19002 详情(缓存优先,命中不发协议)；19003 批量删除(手写变长包,GetNoGetRewardEmailList
    ///   过滤保护)；19004 新邮件到达增量推送(轮21 PF 补漏批,追加/upsert 语义,别与 19001 全量混)；
    ///   19005 批量领取(手写变长包,背包容量前置校验)；19006 公会邮件发送(功能已被服务端硬编码禁用,
    ///   UI 归公会模块 TODO)；19008 是否有未读；19009 可发剩余(服务端 handle 整段被注释,DEAD,既有 handler 保留)；
    ///   19010 意见反馈(非"联系客服"聊天,是工单提交,30s 服务端硬编码 CD)。
    /// </summary>
    public sealed class MailController : BaseController
    {
        public static readonly MailController Instance = new MailController();
        private MailController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.MAIL_LIST, On19001);
            RegisterProtocal(Proto.MAIL_DETAIL, On19002);
            RegisterProtocal(Proto.MAIL_DELETE, On19003);
            RegisterProtocal(Proto.MAIL_ADD_PUSH, On19004);
            RegisterProtocal(Proto.MAIL_RECEIVE, On19005);
            RegisterProtocal(Proto.MAIL_GUILD_SEND, On19006);
            RegisterProtocal(Proto.MAIL_UNREAD, On19008);
            RegisterProtocal(Proto.MAIL_LEFT_NUM, On19009);
            RegisterProtocal(Proto.MAIL_FEEDBACK, On19010);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, RequestStartup);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, RequestStartup);
            MailModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>
        /// 对标老端 FriendController GAME_START：先请求 19008 未读权威状态，再请求 19001 邮件列表。
        /// 主界面邮件通知只消费 19008，不能等用户首次打开邮件页后才补拉列表。
        /// </summary>
        public void RequestStartup()
        {
            SendFmt(Proto.MAIL_UNREAD);
            RequestMailList();
        }

        /// <summary>请求邮件列表（无参，回包 19001）。</summary>
        public void RequestMailList() => SendFmt(Proto.MAIL_LIST);

        /// <summary>19002 详情：缓存优先（对标老端 request_email_info），命中直接 Fire 打开事件，不发协议。</summary>
        public void RequestMailDetail(long mailId)
        {
            MailDetail cached = MailModel.Instance.GetDetail(mailId);
            if (cached != null)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_MAIL_DETAIL_READY, mailId);
                GameLog.Info("Mail", "19002 详情缓存命中 mailId={0}(不发协议)", mailId);
                return;
            }
            SendFmt(Proto.MAIL_DETAIL, "l", mailId);
            GameLog.Info("Mail", "19002 请求详情(缓存未命中) mailId={0}", mailId);
        }

        /// <summary>19003 一键删除（对标老端 EmailView.deletClick）：邮件列表为空 / 过滤后无可删邮件都提示
        /// "暂无可删除邮件!"；否则只删 <see cref="MailModel.GetNoGetRewardEmailList"/>（无附件或已领取的邮件），
        /// 绝不删有未领取附件的邮件。</summary>
        public void RequestDeleteAll()
        {
            if (MailModel.Instance.Mails.Count == 0)
            {
                TipsManager.Toast("暂无可删除邮件!");
                return;
            }
            List<MailVo> list = MailModel.Instance.GetNoGetRewardEmailList();
            if (list.Count == 0)
            {
                TipsManager.Toast("暂无可删除邮件!");
                return;
            }
            var ids = new List<long>(list.Count);
            foreach (MailVo vo in list) ids.Add(vo.MailId);
            SendVariableLongArray(Proto.MAIL_DELETE, ids);
            GameLog.Info("Mail", "19003 请求删除 count={0}", ids.Count);
        }

        /// <summary>19005 一键领取全部待领附件（对标老端 EmailView.getClick）：背包容量前置校验
        /// （对标老端 GoodsModel.CheckEquipNum——老端该处实参因变量作用域缺陷长期恒为极小值，
        /// 本端不追求逐字节复刻这个疑似缺陷值，改为"至少 1 个空位"的等价前端护栏，服务端仍是权威校验）。</summary>
        public void RequestReceiveAll()
        {
            List<MailVo> list = MailModel.Instance.AllRewardEmailList();
            if (list.Count == 0) return;
            if (!HasFreeBagSlot())
            {
                TipsManager.Toast("背包已满，请先整理背包");
                return;
            }
            var ids = new List<long>(list.Count);
            foreach (MailVo vo in list) ids.Add(vo.MailId);
            SendVariableLongArray(Proto.MAIL_RECEIVE, ids);
            GameLog.Info("Mail", "19005 请求一键领取 count={0}", ids.Count);
        }

        /// <summary>19005 领取单封（对标老端 EmailPopView.getClick）：同样先过背包容量前置校验。</summary>
        public void RequestReceiveOne(long mailId)
        {
            if (!HasFreeBagSlot())
            {
                TipsManager.Toast("背包已满，请先整理背包");
                return;
            }
            SendVariableLongArray(Proto.MAIL_RECEIVE, new List<long> { mailId });
            GameLog.Info("Mail", "19005 请求领取单封 mailId={0}", mailId);
        }

        /// <summary>19006 发公会邮件（当前服务端 check_send_guild_mail_on_server 硬编码恒返回 not_open，
        /// 功能实际不可用；UI 归公会模块 GuildMailView，本轮只补 API，TODO 见汇报）。</summary>
        public void SendGuildMail(string title, string content) => SendFmt(Proto.MAIL_GUILD_SEND, "ss", title, content);

        /// <summary>19010 意见反馈/工单提交（对标老端 CustomerServiceView.SubmitFun，非空校验前置）。</summary>
        public void SendFeedback(string content)
        {
            if (string.IsNullOrEmpty(content?.Trim()))
            {
                GameLog.Info("Mail", "19010 拦截:内容为空");
                return;
            }
            SendFmt(Proto.MAIL_FEEDBACK, "s", content);
        }

        // ---- 手写变长包(h 计数 + l×N):复用 UserMsgAdapter 定长格式串的可变长版本 ----
        private void SendVariableLongArray(int protoId, IReadOnlyList<long> ids)
        {
            var fmt = new StringBuilder("h", ids.Count + 1);
            var args = new object[ids.Count + 1];
            args[0] = ids.Count;
            for (int i = 0; i < ids.Count; i++)
            {
                fmt.Append('l');
                args[i + 1] = ids[i];
            }
            SendFmt(protoId, fmt.ToString(), args);
        }

        /// <summary>奖励摘要文案(降级 toast 用,对标老端 CongratulationObtainView 的物品名列表)。</summary>
        private static string FormatRewardSummary(List<(int style, int typeId, int count)> rewards)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0) sb.Append('、');
                string name = GoodsModel.GetGoodsName(rewards[i].typeId);
                if (string.IsNullOrEmpty(name)) name = "物品" + rewards[i].typeId;
                sb.Append(name).Append('x').Append(rewards[i].count);
            }
            return sb.ToString();
        }

        private static bool HasFreeBagSlot()
        {
            BagModel bag = BagModel.Instance;
            if (!bag.HasData) return true; // 未收到满背包数据时不拦截(fail-open,服务端仍会校验)
            return bag.MaxCell - bag.BagGoodsList.Count >= 1;
        }

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

        // 19002: MailId:l,Sender:s,Title:s,Content:s,Attachment[h+item],Time:i,IsReceive:c
        private void On19002(NetReader r)
        {
            MailDetail detail = MailDetail.Read(r);
            MailModel.Instance.SetDetail(detail);
            GameLog.Info("Mail", "19002 详情到达 mailId={0} attach={1}", detail.MailId, detail.Attachment.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_LIST_UPDATE); // state 可能从 2→1,列表视图需要刷新已读态
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_DETAIL_READY, detail.MailId);
        }

        // 19003: ErrorCode:i, MailIds[h+{MailId:l}]
        private void On19003(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            List<long> ids = r.ReadArray(rr => (long)rr.ReadU64());
            if (errorCode == 1)
            {
                foreach (long id in ids) MailModel.Instance.DeleteMail(id);
                TipsManager.Toast("删除成功");
                EventDispatcher.Emit(GlobalEvent.EVT_MAIL_LIST_UPDATE);
            }
            GameLog.Info("Mail", "19003 删除结果 errorCode={0} count={1}", errorCode, ids.Count);
        }

        // 19004: MailList[h + {MailId:l,Type:c,State:c,Title:s,IsAttach:c,Time:i,EffectEt:i}](字段同19001)
        /// <summary>19004 新邮件到达增量推送(轮21 PF 补漏批,对标老端 FriendController.ts:546-551 On19004
        /// `_model.addEmail(scmd.mail_list)`)。**追加/upsert 语义,不是整表覆盖**——老端 table.insert 直接追加
        /// 到列表末尾(不去重),本端用既有 <see cref="MailModel.AddOrUpdate"/>(按 MailId 存在则原地更新、否则
        /// 插入表头)达到等价效果(显示顺序由 UI 端排序决定,原始插入位置不影响功能)。服务端每次发完本号会
        /// 紧跟着发一次 19008(HasUnread=true,lib_mail.erl:184),On19008 已有独立 handler 处理,这里不重复。</summary>
        private void On19004(NetReader r)
        {
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                var vo = new MailVo();
                vo.ReadFromProtocal(r);
                MailModel.Instance.AddOrUpdate(vo);
            }
            GameLog.Info("Mail", "19004 新邮件到达增量推送 count={0}", count);
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_LIST_UPDATE);
        }

        // 19005: ErrorCode:i, MailIds[h+{MailId:l}], Reward(ObjectList: h+{Style:c,TypeId:i,Count:i})
        private void On19005(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            List<long> ids = r.ReadArray(rr => (long)rr.ReadU64());
            int rewardCount = r.ReadU16();
            var rewards = new List<(int style, int typeId, int count)>(rewardCount);
            for (int i = 0; i < rewardCount; i++)
            {
                int style = r.ReadU8();
                int typeId = (int)r.ReadU32();
                int cnt = (int)r.ReadU32();
                rewards.Add((style, typeId, cnt));
            }

            if (rewardCount > 0)
            {
                MailModel.Instance.MarkReceived(ids);
                EventDispatcher.Emit(GlobalEvent.EVT_MAIL_LIST_UPDATE);
                EventDispatcher.Emit(GlobalEvent.EVT_MAIL_RECEIVE_REWARD, rewards);
                // 降级:CongratulationObtainView 目前无业务子类消费方(见 r7_unity 侦察),
                // 用 toast 摘要代替老端 CongratulationObtainView 展示通道,TODO 待该通道接线后切换。
                TipsManager.Toast("获得 " + FormatRewardSummary(rewards));
            }
            else
            {
                GameLog.Info("Mail", "19005 领取失败 errorCode={0}", errorCode);
            }
            GameLog.Info("Mail", "19005 领取结果 errorCode={0} ids={1} rewards={2}", errorCode, ids.Count, rewardCount);
        }

        // 19006: ErrorCode:i(功能已被服务端硬编码 not_open,UI 归公会模块)
        private void On19006(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            bool success = errorCode == 1;
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_GUILD_SEND_RESULT, success);
            GameLog.Info("Mail", "19006 公会邮件发送结果 errorCode={0}", errorCode);
        }

        private void On19008(NetReader r)
        {
            MailModel.Instance.HasUnread = r.ReadU8() != 0;
            GameLog.Info("Mail", "19008 未读状态: {0}", MailModel.Instance.HasUnread);
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_UNREAD_UPDATE);
        }

        // 服务端 pp_mail.erl:98-104 handle(19009,...) 整段被注释——DEAD:发不出去也收不到回包,
        // 既有 handler 原样保留(若未来服务端恢复该功能,前端已就绪)。
        private void On19009(NetReader r)
        {
            MailModel.Instance.LeftNum = r.ReadU8();
        }

        // 19010: ErrorCode:i(==1 成功清空输入框;老端无论成功失败都先弹 ErrorCodeShow)
        private void On19010(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            bool success = errorCode == 1;
            if (!success) TipsManager.Toast("提交失败(" + errorCode + ")");
            EventDispatcher.Emit(GlobalEvent.EVT_MAIL_FEEDBACK_RESULT, success);
            GameLog.Info("Mail", "19010 意见反馈结果 errorCode={0}", errorCode);
        }
    }
}
