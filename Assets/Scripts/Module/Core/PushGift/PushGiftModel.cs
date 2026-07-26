using System.Collections.Generic;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.PushGift
{
    /// <summary>
    /// 礼包推送数据(对标老客户端 PushGiftModel,模块 191)。承载 19101 下发的「激活礼包列表」,
    /// 供主界面图标(191)显隐/倒计时判定用:本地存在任一未过期礼包(gift_id@sub_id → end_time)
    /// 即视为有可购礼包,图标开启(GetEntranceOpenState)。过期礼包按服务器时间(TimeUtil.NowSec)剔除。
    /// 19102 仅保存按(gift_id,sub_id)索引的详情快照；不接购买、红点、弹窗或图标派生。
    /// </summary>
    public sealed class PushGiftModel
    {
        public static readonly PushGiftModel Instance = new PushGiftModel();
        private PushGiftModel() { }

        /// <summary>主界面图标类型(对标老端 PushGiftModel.ICON_TYPE=191)。</summary>
        public const string ICON_TYPE = "191";

        /// <summary>19101 单条礼包(item_to_bin_0):图标只关心 gift_id/sub_id/end_time,名称/条件面板才用。</summary>
        public readonly struct GiftEntry
        {
            public readonly int GiftId;
            public readonly int SubId;
            public readonly long EndTime; // 结束时间戳(秒)
            public GiftEntry(int giftId, int subId, long endTime)
            {
                GiftId = giftId;
                SubId = subId;
                EndTime = endTime;
            }
        }

        /// <summary>19102 奖励档位，保持服务端 wire 顺序与重复 grade_id。</summary>
        public sealed class RewardEntry
        {
            public readonly int GradeId;
            public readonly string GradeName;
            public readonly byte BuyCount;
            public readonly uint BuyTime;
            public readonly string RewardsConditions;
            public readonly string Rewards;
            public RewardEntry(int gradeId, string gradeName, byte buyCount, uint buyTime, string rewardsConditions, string rewards)
            {
                GradeId = gradeId; GradeName = gradeName; BuyCount = buyCount; BuyTime = buyTime;
                RewardsConditions = rewardsConditions; Rewards = rewards;
            }
        }

        /// <summary>19102 单礼包完整详情。每个 key 的每次回包整体替换。</summary>
        public sealed class GiftDetail
        {
            public readonly int GiftId;
            public readonly int SubId;
            public readonly string GiftName;
            public readonly uint EndTime;
            public readonly string Conditions;
            public readonly List<RewardEntry> RewardList;
            public GiftDetail(int giftId, int subId, string giftName, uint endTime, string conditions, List<RewardEntry> rewardList)
            {
                GiftId = giftId; SubId = subId; GiftName = giftName; EndTime = endTime; Conditions = conditions;
                RewardList = rewardList ?? new List<RewardEntry>();
            }
        }

        // 本地激活礼包表(对标老端 gift_list[gift_id][sub_id]):key=gift_id@sub_id → end_time。
        private readonly Dictionary<string, long> _giftEndTime = new Dictionary<string, long>();
        private readonly Dictionary<string, GiftDetail> _giftDetails = new Dictionary<string, GiftDetail>();

        private static string Key(int giftId, int subId) => giftId + "@" + subId;

        public int GiftDetailCount => _giftDetails.Count;

        public GiftDetail GetGiftDetail(int giftId, int subId)
        {
            _giftDetails.TryGetValue(Key(giftId, subId), out GiftDetail detail);
            return detail;
        }

        public bool HasGiftDetail(int giftId, int subId) => _giftDetails.ContainsKey(Key(giftId, subId));

        /// <summary>19102 同键绝对替换（空奖励表也为已加载快照）；不同礼包详情互不影响。</summary>
        public void ReplaceGiftDetail(int giftId, int subId, string giftName, uint endTime, string conditions, List<RewardEntry> rewardList)
        {
            _giftDetails[Key(giftId, subId)] = new GiftDetail(giftId, subId, giftName, endTime, conditions,
                rewardList == null ? new List<RewardEntry>() : new List<RewardEntry>(rewardList));
        }

        /// <summary>
        /// 累加礼包(对标老端 SetGiftList):只收未过期礼包(end_time>服务器当前秒),过期的丢弃。
        /// 登录全量(type1)/新增(type2)/离线过期(type3,均已过期→自然被剔)都走这里。
        /// </summary>
        public void SetGiftList(List<GiftEntry> list)
        {
            if (list == null) return;
            long now = TimeUtil.NowSec();
            foreach (GiftEntry g in list)
            {
                if (g.EndTime > now) _giftEndTime[Key(g.GiftId, g.SubId)] = g.EndTime;
            }
        }

        /// <summary>立即清理指定礼包(对标老端 type4 → DeleteGiftList)。</summary>
        public void RemoveGifts(List<GiftEntry> list)
        {
            if (list == null) return;
            foreach (GiftEntry g in list) _giftEndTime.Remove(Key(g.GiftId, g.SubId));
        }

        /// <summary>本地礼包是否为空(对标老端 IsGiftListEmpty):过期礼包不计。</summary>
        public bool IsGiftListEmpty()
        {
            long now = TimeUtil.NowSec();
            foreach (long end in _giftEndTime.Values)
            {
                if (end > now) return false;
            }
            return true;
        }

        /// <summary>
        /// 入口开启状态(对标老端:gift_list 非空 → addIcon(191))。
        /// 有任一未过期礼包即开启图标;服务端只把满足条件的礼包推给本人,门槛由服务端把控。
        /// </summary>
        public bool GetEntranceOpenState() => !IsGiftListEmpty();

        /// <summary>
        /// 最近结束时间戳(对标老端 FindGiftTime.next_end_time):作图标倒计时。无未过期礼包返回 0。
        /// </summary>
        public long NextEndTime()
        {
            long now = TimeUtil.NowSec();
            long min = 0;
            foreach (long end in _giftEndTime.Values)
            {
                if (end <= now) continue;
                if (min == 0 || end < min) min = end;
            }
            return min;
        }

        public void Reset()
        {
            _giftEndTime.Clear();
            _giftDetails.Clear();
        }
    }
}
