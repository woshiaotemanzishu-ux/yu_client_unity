using System.Collections.Generic;

namespace Shenxiao.Module.Core.WeekCard
{
    /// <summary>
    /// 周卡数据（对标老客户端 WeekCardController / yu_server pt_452）。只存数据；面板/红点 UI 待用户验收。
    /// </summary>
    public sealed class WeekCardModel
    {
        public static readonly WeekCardModel Instance = new WeekCardModel();
        private WeekCardModel() { }

        public struct RewardItem
        {
            public int Style;
            public int TypeId;
            public int Count;
            public RewardItem(int style, int typeId, int count) { Style = style; TypeId = typeId; Count = count; }
        }

        // 45201 周卡信息
        public int Lv;
        public int Exp;
        public bool IsActivity;
        public int GiftBagNum;
        public int CanReceiveGift;
        public int ExpiredTime;

        /// <summary>最近一次领取/推送的奖励（45202/45203）。</summary>
        public readonly List<RewardItem> LastRewards = new List<RewardItem>();

        public void SetInfo(int lv, int exp, bool isActivity, int giftBagNum, int canReceiveGift, int expiredTime)
        {
            Lv = lv; Exp = exp; IsActivity = isActivity; GiftBagNum = giftBagNum;
            CanReceiveGift = canReceiveGift; ExpiredTime = expiredTime;
        }

        public void SetRewards(List<RewardItem> rewards)
        {
            LastRewards.Clear();
            if (rewards != null) LastRewards.AddRange(rewards);
        }

        public void Clear()
        {
            Lv = 0; Exp = 0; IsActivity = false; GiftBagNum = 0; CanReceiveGift = 0; ExpiredTime = 0;
            LastRewards.Clear();
        }
    }
}
