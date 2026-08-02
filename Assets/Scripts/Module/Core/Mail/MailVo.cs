using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Mail
{
    /// <summary>
    /// 邮件摘要（对标 yu_server pt_190 item_to_bin_0 / 19001、19004）：
    /// MailId(l) Type(c) State(c) Title(s) IsAttach(c) Time(i) EffectEt(i)。
    /// State:1=已读 2=未读 3=已领取附件(is_attach 独立标记是否带附件,对标 r7_oldfriend 状态机勘误)。
    /// </summary>
    public sealed class MailVo
    {
        public long MailId;
        public int Type;        // 邮件类型(1系统/2私人/3公会)
        public int State;       // 1已读/2未读/3已领取附件
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

    /// <summary>附件极品词条(对标 yu_server pt_190 item_to_bin_2):Color(c) TypeId(c) AttrId(h) AttrVal(i)
    /// PlusInterval(c) PlusUnit(i)。</summary>
    public struct MailExtraAttr
    {
        public int Color;
        public int TypeId;
        public int AttrId;
        public long AttrVal;
        public int PlusInterval;
        public long PlusUnit;

        public static MailExtraAttr Read(NetReader r) => new MailExtraAttr
        {
            Color = r.ReadU8(),
            TypeId = r.ReadU8(),
            AttrId = r.ReadU16(),
            AttrVal = r.ReadU32(),
            PlusInterval = r.ReadU8(),
            PlusUnit = r.ReadU32(),
        };
    }

    /// <summary>邮件附件(对标 yu_server pt_190 item_to_bin_1):ObjectType(c) TypeId(i) Num(i) ExtraAttr[h+item]。
    /// object_type/type_id 走 GoodsModel.GetMappingTypeId 换算真实 type_id(与老端 FriendModel.emailReward 一致,
    /// 本轮暂不接 GoodsModel 换算,只透传原始字段——领取展示走 CongratulationObtain/toast 降级,见 MailController 注释)。</summary>
    public sealed class MailAttachment
    {
        public int ObjectType;
        public int TypeId;
        public long Num;
        public List<MailExtraAttr> ExtraAttr = new List<MailExtraAttr>();

        public static MailAttachment Read(NetReader r)
        {
            var a = new MailAttachment
            {
                ObjectType = r.ReadU8(),
                TypeId = (int)r.ReadU32(),
                Num = r.ReadU32(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++) a.ExtraAttr.Add(MailExtraAttr.Read(r));
            return a;
        }
    }

    /// <summary>邮件详情(19002 回包,对标老端 emailInfoDic 缓存项):MailId(l) Sender(s) Title(s) Content(s)
    /// Attachment[h+item] Time(i) IsReceive(c)。</summary>
    public sealed class MailDetail
    {
        public long MailId;
        public string Sender = "";
        public string Title = "";
        public string Content = "";
        public List<MailAttachment> Attachment = new List<MailAttachment>();
        public int Time;
        public int IsReceive;

        public static MailDetail Read(NetReader r)
        {
            var d = new MailDetail
            {
                MailId = (long)r.ReadU64(),
                Sender = r.ReadString(),
                Title = r.ReadString(),
                Content = r.ReadString(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++) d.Attachment.Add(MailAttachment.Read(r));
            d.Time = (int)r.ReadU32();
            d.IsReceive = r.ReadU8();
            return d;
        }
    }
}
