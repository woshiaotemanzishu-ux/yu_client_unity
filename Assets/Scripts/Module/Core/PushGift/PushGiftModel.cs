using System.Collections.Generic;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.PushGift
{
    /// <summary>
    /// 礼包推送数据(对标老客户端 PushGiftModel,模块 191)。只承载 19101 下发的「激活礼包列表」,
    /// 供主界面图标(191)显隐/倒计时判定用:本地存在任一未过期礼包(gift_id@sub_id → end_time)
    /// 即视为有可购礼包,图标开启(GetEntranceOpenState)。过期礼包按服务器时间(TimeUtil.NowSec)剔除。
    /// 面板/购买/红点/激活弹窗(19102/19103/PushGiftTips 等)待用户验收,本期只做图标。
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

        // 本地激活礼包表(对标老端 gift_list[gift_id][sub_id]):key=gift_id@sub_id → end_time。
        private readonly Dictionary<string, long> _giftEndTime = new Dictionary<string, long>();

        private static string Key(int giftId, int subId) => giftId + "@" + subId;

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
        }
    }
}
