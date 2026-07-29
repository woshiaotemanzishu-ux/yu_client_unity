using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Auction
{
    /// <summary>拍卖154家族读侧控制器；竞价15403及旧端空消费/废弃协议不接。</summary>
    public sealed class AuctionController : BaseController
    {
        public const uint WorldAuctionType = 2;
        public static readonly AuctionController Instance = new AuctionController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private AuctionController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.AUCTION_GOODS, On15401);
            RegisterProtocal(Proto.AUCTION_GOODS_UPDATE, On15402);
            RegisterProtocal(Proto.AUCTION_ESTIMATE, On15407);
            RegisterProtocal(Proto.AUCTION_LIFECYCLE, On15408);
            RegisterProtocal(Proto.AUCTION_PERSONAL_RECORDS, On15409);
            RegisterProtocal(Proto.AUCTION_BONUS_RECORDS, On15410);
            RegisterProtocal(Proto.AUCTION_ALL_CLOSE, On15411);
        }

        public void RequestStartup()
        {
            AuctionModel.Instance.Reset();
            RequestGoods(WorldAuctionType, 0, 0);
        }

        public void RequestGoods(uint auctionType, uint type, uint moduleId) =>
            SendRequest(Proto.AUCTION_GOODS, "iii", auctionType, type, moduleId);
        public void RequestEstimate(uint auctionType, uint moduleId) =>
            SendRequest(Proto.AUCTION_ESTIMATE, "ii", auctionType, moduleId);
        public void RequestPersonalRecords() => SendRequest(Proto.AUCTION_PERSONAL_RECORDS);
        public void RequestBonusRecords() => SendRequest(Proto.AUCTION_BONUS_RECORDS);

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On15401(NetReader r)
        {
            uint auctionType = r.ReadU32();
            List<AuctionModel.Goods> goods = r.ReadArray(rr => new AuctionModel.Goods(
                unchecked((ulong)rr.ReadU64()), rr.ReadU32(), rr.ReadU32(), rr.ReadU16(),
                rr.ReadU32(), rr.ReadU32(), rr.ReadU32(), rr.ReadU32(), rr.ReadU32(),
                unchecked((ulong)rr.ReadU64()), rr.ReadU8(), rr.ReadU8()));
            AuctionModel.Instance.ReplaceGoods(auctionType, goods);
        }

        private void On15402(NetReader r)
        {
            AuctionModel.Instance.ReplaceUpdate(new AuctionModel.GoodsUpdate(
                unchecked((ulong)r.ReadU64()), r.ReadU32(), r.ReadU32(), r.ReadU32(), r.ReadU32(),
                r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU8(), r.ReadU8()));
        }

        private void On15407(NetReader r)
        {
            AuctionModel.Instance.ReplaceEstimate(new AuctionModel.EstimateSnapshot(
                r.ReadU32(), r.ReadU32(), r.ReadU32(), r.ReadU32()));
        }

        private void On15408(NetReader r)
        {
            AuctionModel.Instance.ReplaceLifecycle(new AuctionModel.LifecycleSnapshot(
                r.ReadU32(), r.ReadU32(), r.ReadU8()));
        }

        private void On15409(NetReader r)
        {
            AuctionModel.Instance.ReplacePersonalRecords(r.ReadArray(rr => new AuctionModel.PersonalRecord(
                rr.ReadU8(), rr.ReadU32(), rr.ReadU8(), rr.ReadU16(), rr.ReadU16(),
                rr.ReadU32(), rr.ReadU16(), rr.ReadU32())));
        }

        private void On15410(NetReader r)
        {
            List<AuctionModel.BonusRecord> records = r.ReadArray(rr => new AuctionModel.BonusRecord(
                rr.ReadU32(), rr.ReadU16(), rr.ReadU16(), rr.ReadU32()));
            List<AuctionModel.BonusInfo> infos = r.ReadArray(rr => new AuctionModel.BonusInfo(
                rr.ReadU32(), rr.ReadU16(), rr.ReadU16()));
            AuctionModel.Instance.ReplaceBonusRecords(records, infos);
        }

        private void On15411(NetReader r)
        {
            AuctionModel.Instance.ReplaceAllClose(r.ReadU8());
        }

        public override void Dispose()
        {
            AuctionModel.Instance.Reset();
            base.Dispose();
        }
    }
}
