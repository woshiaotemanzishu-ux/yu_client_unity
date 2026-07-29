using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.TopPk
{
    /// <summary>巅峰对决281家族安全读侧；领奖、购买、匹配、取消匹配与退出战斗等写操作不接。</summary>
    public sealed class TopPkController : BaseController
    {
        public static readonly TopPkController Instance = new TopPkController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private TopPkController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TOP_PK_ERROR, On28100);
            RegisterProtocal(Proto.TOP_PK_INFO, On28101);
            RegisterProtocal(Proto.TOP_PK_LEVEL_REWARDS, On28105);
            RegisterProtocal(Proto.TOP_PK_ACTIVITY, On28107);
            RegisterProtocal(Proto.TOP_PK_MATCHED, On28111);
            RegisterProtocal(Proto.TOP_PK_STAGE, On28112);
            RegisterProtocal(Proto.TOP_PK_RESULT, On28113);
            RegisterProtocal(Proto.TOP_PK_RANKS, On28115);
            RegisterProtocal(Proto.TOP_PK_PROMOTION, On28117);
        }

        /// <summary>
        /// 镜像旧端GAME_START的读请求顺序。旧端ResetData为空，因此请求前不清旧快照；无回包时保留旧值。
        /// </summary>
        public void RequestStartup()
        {
            RequestInfo();
            RequestLevelRewards();
            RequestActivity();
        }

        public void RequestInfo() => SendRequest(Proto.TOP_PK_INFO);
        public void RequestLevelRewards() => SendRequest(Proto.TOP_PK_LEVEL_REWARDS);
        public void RequestActivity() => SendRequest(Proto.TOP_PK_ACTIVITY);
        public void RequestRanks() => SendRequest(Proto.TOP_PK_RANKS);

        private void SendRequest(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }

        private void On28100(NetReader r) =>
            TopPkModel.Instance.SetError(r.ReadU32(), r.ReadString());

        private void On28101(NetReader r)
        {
            ushort seasonNumber = r.ReadU16();
            uint seasonEndTime = r.ReadU32();
            byte rankLevel = r.ReadU8();
            uint point = r.ReadU32();
            uint seasonCount = r.ReadU32();
            uint seasonWinCount = r.ReadU32();
            uint dailyHonorValue = r.ReadU32();
            byte honorState = r.ReadU8();
            ushort dailyCount = r.ReadU16();
            List<TopPkModel.DailyCountReward> rewards = r.ReadArray(rr =>
                new TopPkModel.DailyCountReward(rr.ReadU8(), rr.ReadU8()));
            ushort dailyBuyCount = r.ReadU16();
            byte yesterdayRankLevel = r.ReadU8();
            TopPkModel.Instance.ReplaceInfo(new TopPkModel.InfoSnapshot(
                seasonNumber, seasonEndTime, rankLevel, point, seasonCount, seasonWinCount,
                dailyHonorValue, honorState, dailyCount, rewards, dailyBuyCount, yesterdayRankLevel));
        }

        private void On28105(NetReader r)
        {
            List<TopPkModel.LevelReward> rewards = r.ReadArray(rr =>
                new TopPkModel.LevelReward(rr.ReadU8(), rr.ReadU8()));
            TopPkModel.Instance.ReplaceLevelRewards(new TopPkModel.LevelRewardsSnapshot(rewards));
        }

        private void On28107(NetReader r) =>
            TopPkModel.Instance.ReplaceActivity(new TopPkModel.ActivitySnapshot(
                r.ReadU8(), r.ReadU32(), r.ReadU32()));

        private void On28111(NetReader r) =>
            TopPkModel.Instance.ReplaceMatch(new TopPkModel.MatchSnapshot(
                r.ReadU8(), r.ReadU8(), r.ReadU8(), unchecked((ulong)r.ReadU64())));

        private void On28112(NetReader r) =>
            TopPkModel.Instance.ReplaceStage(new TopPkModel.StageSnapshot(r.ReadU8(), r.ReadU32()));

        private void On28113(NetReader r) =>
            TopPkModel.Instance.ReplaceResult(new TopPkModel.ResultSnapshot(
                r.ReadU8(), r.ReadU32(), r.ReadU8(), r.ReadU32()));

        private void On28115(NetReader r)
        {
            List<TopPkModel.RankEntry> ranks = r.ReadArray(rr => new TopPkModel.RankEntry(
                unchecked((ulong)rr.ReadU64()), rr.ReadString(), rr.ReadU8(),
                unchecked((ulong)rr.ReadU64()), rr.ReadString(), rr.ReadString(),
                rr.ReadU16(), rr.ReadU8(), rr.ReadU32()));
            TopPkModel.Instance.ReplaceRanks(new TopPkModel.RanksSnapshot(ranks));
        }

        private void On28117(NetReader r) =>
            TopPkModel.Instance.ReplacePromotion(new TopPkModel.PromotionSnapshot(
                r.ReadU8(), r.ReadU32(), r.ReadU8(), r.ReadU32()));

        public override void Dispose()
        {
            TopPkModel.Instance.Reset();
            base.Dispose();
        }
    }
}
