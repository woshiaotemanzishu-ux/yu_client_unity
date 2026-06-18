using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Mail
{
    /// <summary>
    /// 邮件摘要（对标 yu_server pt_190 item_to_bin_0 / write(19007)）：
    /// MailId(l) Type(c) State(c) Title(s) IsAttach(c) Time(i) EffectEt(i)。
    /// 详情（发件人/正文/附件，19002）与一键领取奖励（19005）较复杂（含嵌套 ExtraAttr），本期未接、待后续。
    /// </summary>
    public sealed class MailVo
    {
        public long MailId;
        public int Type;        // 邮件类型
        public int State;       // 读取/领取状态
        public string Title = "";
        public int IsAttach;    // 是否带附件(0/1)
        public int Time;        // 发送时间戳(秒)
        public int EffectEt;    // 失效时间戳

        public void ReadFromProtocal(NetReader r)
        {
            MailId = r.ReadU64();
            Type = r.ReadU8();
            State = r.ReadU8();
            Title = r.ReadString();
            IsAttach = r.ReadU8();
            Time = (int)r.ReadU32();
            EffectEt = (int)r.ReadU32();
        }
    }
}
