using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Notice
{
    /// <summary>
    /// 公告/广播协议接收（对标老客户端 ChatController 的系统消息分支 + SysInfo）：
    ///   11020 系统公告/提示（纯文本，跑马灯/系统提示）→ 见 yu_server pt_110 write(11020) "s"；
    ///   11015 传闻广播（moduleId, id, content）→ pt_110 write(11015) "hhs"；
    ///   11018 传闻广播带发送者形象（serId, playerId, figure, moduleId, id, content）→ pt_110 write(11018)。
    /// 解析落 <see cref="NoticeModel"/> 并发事件；显示 UI（跑马灯/公告面板）待用户验收时接。
    /// </summary>
    public sealed class NoticeController : BaseController
    {
        public static readonly NoticeController Instance = new NoticeController();
        private NoticeController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.SC_CHUANWEN, On11015);
            RegisterProtocal(Proto.SC_CHUANWEN_FIGURE, On11018);
            RegisterProtocal(Proto.SC_SYS_NOTICE, On11020);
        }

        /// <summary>11020 系统公告：仅一个字符串。</summary>
        private void On11020(NetReader r)
        {
            string content = r.ReadString();
            NoticeModel.Instance.SetSysNotice(content);
            GameLog.Info("Notice", "11020 系统公告: {0}", content);
            EventDispatcher.Emit(GlobalEvent.EVT_SYS_NOTICE);
        }

        /// <summary>11015 传闻广播 "hhs"。</summary>
        private void On11015(NetReader r)
        {
            int moduleId = r.ReadU16();
            int id = r.ReadU16();
            string content = r.ReadString();
            NoticeModel.Instance.PushChuanwen(moduleId, id, content);
            GameLog.Info("Notice", "11015 传闻: module={0} id={1} {2}", moduleId, id, content);
            EventDispatcher.Emit(GlobalEvent.EVT_CHUANWEN);
        }

        /// <summary>11018 传闻广播（带发送者形象）：serId(h) playerId(l) figure h h s。</summary>
        private void On11018(NetReader r)
        {
            r.ReadU16();                 // serId
            long playerId = r.ReadU64();
            FigureProto.Read(r);         // 发送者形象（暂不展示，仅消费字节保持对齐）
            int moduleId = r.ReadU16();
            int id = r.ReadU16();
            string content = r.ReadString();
            NoticeModel.Instance.PushChuanwen(moduleId, id, content, playerId);
            GameLog.Info("Notice", "11018 传闻(带形象): player={0} module={1} id={2} {3}", playerId, moduleId, id, content);
            EventDispatcher.Emit(GlobalEvent.EVT_CHUANWEN);
        }
    }
}
