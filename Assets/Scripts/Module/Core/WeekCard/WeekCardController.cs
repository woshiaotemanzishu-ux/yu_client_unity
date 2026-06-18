using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.WeekCard
{
    /// <summary>
    /// 周卡协议（对标老客户端 WeekCardController / yu_server pt_452）：
    ///   45201 周卡信息（Lv:h, Exp:i, IsActivity:c, GiftBagNum:h, CanReceiveGift:h, ExpiredTime:i）；
    ///   45202 领取结果（Code:i + 奖励[h+{Style:c,TypeId:i,Count:i}×N]）；
    ///   45203 奖励推送（Type:c + 奖励[…]）。
    /// 解析落 <see cref="WeekCardModel"/> 并发 EVT_WEEK_CARD_UPDATE；面板/红点 UI 待用户验收。
    /// </summary>
    public sealed class WeekCardController : BaseController
    {
        public static readonly WeekCardController Instance = new WeekCardController();
        private WeekCardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.WEEK_CARD_INFO, On45201);
            RegisterProtocal(Proto.WEEK_CARD_CLAIM, On45202);
            RegisterProtocal(Proto.WEEK_CARD_REWARD, On45203);
        }

        public void RequestInfo() => SendFmt(Proto.WEEK_CARD_INFO);
        public void Claim() => SendFmt(Proto.WEEK_CARD_CLAIM);

        private void On45201(NetReader r)
        {
            int lv = r.ReadU16();
            int exp = (int)r.ReadU32();
            bool isActivity = r.ReadU8() != 0;
            int giftBagNum = r.ReadU16();
            int canReceiveGift = r.ReadU16();
            int expiredTime = (int)r.ReadU32();
            WeekCardModel.Instance.SetInfo(lv, exp, isActivity, giftBagNum, canReceiveGift, expiredTime);
            GameLog.Info("WeekCard", "45201 周卡: lv={0} 可领={1} 激活={2}", lv, canReceiveGift, isActivity);
            EventDispatcher.Emit(GlobalEvent.EVT_WEEK_CARD_UPDATE);
        }

        private void On45202(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<WeekCardModel.RewardItem> rewards = ReadRewards(r);
            WeekCardModel.Instance.SetRewards(rewards);
            GameLog.Info("WeekCard", "45202 领取结果: code={0} 奖励={1} 项", code, rewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_WEEK_CARD_UPDATE);
        }

        private void On45203(NetReader r)
        {
            int type = r.ReadU8();
            List<WeekCardModel.RewardItem> rewards = ReadRewards(r);
            WeekCardModel.Instance.SetRewards(rewards);
            GameLog.Info("WeekCard", "45203 奖励推送: type={0} 奖励={1} 项", type, rewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_WEEK_CARD_UPDATE);
        }

        private static List<WeekCardModel.RewardItem> ReadRewards(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<WeekCardModel.RewardItem>(count);
            for (int i = 0; i < count; i++)
            {
                int style = r.ReadU8();
                int typeId = (int)r.ReadU32();
                int num = (int)r.ReadU32();
                list.Add(new WeekCardModel.RewardItem(style, typeId, num));
            }
            return list;
        }
    }
}
