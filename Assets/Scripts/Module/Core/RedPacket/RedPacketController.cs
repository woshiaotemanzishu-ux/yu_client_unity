using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.RedPacket
{
    /// <summary>
    /// 公会红包(RedPacket)协议控制器(自动循环 轮18 PK2;对标老端 commonController/RedPacketController.ts,
    /// 服务端 pt_339)。7 号活:33900(错误码推送)/33901(列表)/33902(打开)/33904(发系统道具红包)/
    /// 33906(发VIP红包)/33907(新增推送)/33908(领完推送)。
    ///
    /// ⚠33903/33905 死号封存,**严禁 RegisterProtocal / 严禁提供发送方法**(r18_oldclient_cheapwins.md §3
    /// 实证:老端全仓零调用点——on33903 读完即弃、on33905 虽有完整成败分支但从无 Fire(REQUEST_PROTO,33905,...)
    /// 调用点,故服务端永远收不到 33905 请求、其回包分支永不可达)。wire 仍在(pt_339.erl:13-16,114-126 与
    /// :21-25,136-142),存档不当活号追:33903 C2S Type:8,Extra:64→S2C TimesLimit:8,RemainTimes:8,
    /// TotalTimes:8,SplitNum:16;33905 C2S GoodsId:64,GtypeId:32,SplitNum:16→S2C Errcode:32。
    ///
    /// ⚠33902 全端仅成功路径会发本号(失败改走 33900,pp_red_envelopes.erl:33-41/
    /// lib_red_envelopes_mod.erl:259-260,275 已核对),故 On33902 恒当成功处理,不含内嵌错误码字段。
    /// </summary>
    public sealed class RedPacketController : BaseController
    {
        public static readonly RedPacketController Instance = new RedPacketController();
        private RedPacketController() { }

        /// <summary>老端 VIP 红包发送成功的真实 errcode(err339_vip_send_succ,非通用 ?SUCCESS=1;
        /// pp_red_envelopes.erl:156:ErrCode={?ERRCODE(err339_vip_send_succ),[TotalTimes-Count-1]}),
        /// Args 携带剩余可发次数。老端 on33906(RedPacketController.ts:102-114)三分支镜像:
        /// errcode==1(通用成功,该分支在此协议实际不可达,仅防御性保留)| errcode==3390012(真成功,
        /// 关窗+回补33901+带 Args 提示)| 其余(失败,显码+Args)。</summary>
        private const int VIP_SEND_SUCCESS_CODE = 3390012;

        protected override void Register()
        {
            RegisterProtocal(Proto.REDPACKET_ERROR, On33900);
            RegisterProtocal(Proto.REDPACKET_LIST, On33901);
            RegisterProtocal(Proto.REDPACKET_OPEN, On33902);
            // 33903/33905 死号封存,严禁注册(见类头注释),此处刻意不写 RegisterProtocal 行。
            RegisterProtocal(Proto.REDPACKET_SEND, On33904);
            RegisterProtocal(Proto.REDPACKET_SEND_VIP, On33906);
            RegisterProtocal(Proto.REDPACKET_NEW_PUSH, On33907);
            RegisterProtocal(Proto.REDPACKET_TAKEN_PUSH, On33908);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            RedPacketModel.Instance.Reset();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await RedPacketConfigs.EnsureLoaded();
            // 对标老端 GAME_START→model.Reset()(清红点态;列表本就靠 RedPacketMainView.ts:94 开窗时拉取,
            // 不在此处主动请求 33901)。
            RedPacketModel.Instance.Reset();
        }

        /// <summary>请求红包列表(对标 RedPacketMainView.ts:94)。发空包。</summary>
        public void RequestList()
        {
            SendFmt(Proto.REDPACKET_LIST);
            GameLog.Info("RedPacket", "request 33901 list");
        }

        /// <summary>打开红包(对标 MainItem.ts:59,64)。发 "l"(RedEnvelopesId)。</summary>
        public void RequestOpen(long redEnvelopesId)
        {
            if (redEnvelopesId <= 0) return;
            SendFmt(Proto.REDPACKET_OPEN, "l", redEnvelopesId);
            GameLog.Info("RedPacket", "request 33902 open id={0}", redEnvelopesId);
        }

        /// <summary>发系统/物品红包(对标 CtrlView.ts:121 物品红包分支)。发 "lh"(Id, SplitNum)。</summary>
        public void RequestSend(long id, int splitNum)
        {
            if (id <= 0 || splitNum <= 0) return;
            SendFmt(Proto.REDPACKET_SEND, "lh", id, splitNum);
            GameLog.Info("RedPacket", "request 33904 send id={0} splitNum={1}", id, splitNum);
        }

        /// <summary>发 VIP 红包(对标 CtrlView.ts:119,type==100 分支)。发 "ihs"(Money, SplitNum, Msg)。</summary>
        public void RequestSendVip(int money, int splitNum, string msg)
        {
            if (money <= 0 || splitNum <= 0) return;
            SendFmt(Proto.REDPACKET_SEND_VIP, "ihs", money, splitNum, msg ?? "");
            GameLog.Info("RedPacket", "request 33906 send vip money={0} splitNum={1}", money, splitNum);
        }

        /// <summary>33900:Errcode:32(纯推送,通用错误提示)。</summary>
        private void On33900(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            TipsManager.Toast("错误(" + errcode + ")"); // 错误码表未移植,显码降级
            GameLog.Info("RedPacket", "33900 error errcode={0}", errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_UPDATE, 0L);
        }

        /// <summary>33901:RedEnvelopesList[u16×item_to_bin_0 16字段] + RecordList[u16×item_to_bin_1 4字段]。</summary>
        private void On33901(NetReader r)
        {
            List<RedPacketModel.RedEnvelopeEntry> list = r.ReadArray(ReadEntry);
            List<RedPacketModel.RecordEntry> records = r.ReadArray(ReadRecord);
            RedPacketModel.Instance.ApplyList(list, records);
            GameLog.Info("RedPacket", "33901 list count={0} records={1}", list.Count, records.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_UPDATE, 0L);
        }

        /// <summary>33902:15 标量字段(RedEnvelopesId 起)+ RecipientList[u16×item_to_bin_2 9字段]
        /// (字段顺序已逐位回 pt_339.erl:65-112 原文核对)。</summary>
        private void On33902(NetReader r)
        {
            var detail = new RedPacketModel.OpenDetail
            {
                RedEnvelopesId = r.ReadU64(),
                RoleId = r.ReadU64(),
                RoleName = r.ReadString(),
                Career = r.ReadU8(),
                Sex = r.ReadU8(),
                Turn = r.ReadU8(),
                Picture = r.ReadString(),
                PictureVer = (int)r.ReadU32(),
                Status = r.ReadU8(),
                ReceiveMoney = (int)r.ReadU32(),
                TotalNum = r.ReadU16(),
                RecipientsNum = r.ReadU16(),
                Money = (int)r.ReadU32(),
                Type = r.ReadU8(),
                Extra = (int)r.ReadU32(),
            };
            detail.RecipientList.AddRange(r.ReadArray(ReadRecipient));
            RedPacketModel.Instance.ApplyOpenDetail(detail);
            GameLog.Info("RedPacket", "33902 open id={0} recipients={1}", detail.RedEnvelopesId, detail.RecipientList.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_RESULT, Proto.REDPACKET_OPEN, 1);
        }

        /// <summary>33904:Errcode:32(对标 on33904:errcode==1 成功→关窗+回补 33901,否则显码)。</summary>
        private void On33904(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            if (errcode == 1) RequestList(); // 对标老端成功后 SendFmtToGame(33901) 回补
            else TipsManager.Toast("发送失败(" + errcode + ")");
            GameLog.Info("RedPacket", "33904 send result errcode={0}", errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_RESULT, Proto.REDPACKET_SEND, errcode);
        }

        /// <summary>33906:Errcode:32, Args:string。三分支镜像老端(见类头 VIP_SEND_SUCCESS_CODE 注释)。</summary>
        private void On33906(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            string args = r.ReadString();
            if (errcode == 1)
            {
                RequestList();
            }
            else if (errcode == VIP_SEND_SUCCESS_CODE)
            {
                RequestList();
                TipsManager.Toast(string.IsNullOrEmpty(args) ? "发送成功" : "发送成功(剩余" + args + "次)");
            }
            else
            {
                TipsManager.Toast(string.IsNullOrEmpty(args) ? "发送失败(" + errcode + ")" : args);
            }
            GameLog.Info("RedPacket", "33906 send vip result errcode={0} args={1}", errcode, args);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_RESULT, Proto.REDPACKET_SEND_VIP, errcode);
        }

        /// <summary>33907:RedEnvelopesList[u16×item_to_bin_3 16字段](与 33901 同构,对标 on33907 合并进现有列表)。</summary>
        private void On33907(NetReader r)
        {
            List<RedPacketModel.RedEnvelopeEntry> pushed = r.ReadArray(ReadEntry);
            foreach (RedPacketModel.RedEnvelopeEntry e in pushed) RedPacketModel.Instance.ApplyNewPush(e);
            GameLog.Info("RedPacket", "33907 new push count={0}", pushed.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_UPDATE, 0L);
        }

        /// <summary>33908:Id:64(公会广播,对标 on33908:同 id 置 status=2 领完)。</summary>
        private void On33908(NetReader r)
        {
            long id = r.ReadU64();
            RedPacketModel.Instance.ApplyTakenPush(id);
            GameLog.Info("RedPacket", "33908 taken push id={0}", id);
            EventDispatcher.Emit(GlobalEvent.EVT_REDPACKET_UPDATE, id);
        }

        private static RedPacketModel.RedEnvelopeEntry ReadEntry(NetReader r) => new RedPacketModel.RedEnvelopeEntry
        {
            Id = r.ReadU64(), RoleId = r.ReadU64(), RoleName = r.ReadString(), Career = r.ReadU8(), Sex = r.ReadU8(),
            Turn = r.ReadU8(), Picture = r.ReadString(), PictureVer = (int)r.ReadU32(), Type = r.ReadU8(),
            Extra = (int)r.ReadU32(), Status = r.ReadU8(), ReceiveStatus = r.ReadU8(), TotalNum = r.ReadU16(),
            RecipientsNum = r.ReadU16(), Msg = r.ReadString(), Stime = (int)r.ReadU32(),
        };

        private static RedPacketModel.RecordEntry ReadRecord(NetReader r) => new RedPacketModel.RecordEntry
        {
            Id = (int)r.ReadU32(), RoleName = r.ReadString(), CfgId = (int)r.ReadU32(), Time = (int)r.ReadU32(),
        };

        private static RedPacketModel.RecipientEntry ReadRecipient(NetReader r) => new RedPacketModel.RecipientEntry
        {
            RoleId = r.ReadU64(), RoleName = r.ReadString(), Career = r.ReadU8(), Sex = r.ReadU8(), Turn = r.ReadU8(),
            Picture = r.ReadString(), PictureVer = (int)r.ReadU32(), ReceiveMoney = (int)r.ReadU32(), Time = (int)r.ReadU32(),
        };
    }
}
