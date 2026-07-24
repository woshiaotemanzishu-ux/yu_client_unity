using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Module.Core.WeekCard
{
    /// <summary>
    /// 周卡协议（对标老客户端 WeekCardController / yu_server pt_452）：
    ///   45201 周卡信息（Lv:h, Exp:i, IsActivity:c, GiftBagNum:h, CanReceiveGift:h, ExpiredTime:i）；
    ///   45202 领取结果（Code:i + 奖励[h+{Style:c,TypeId:i,Count:i}×N]）；
    ///   45203 奖励推送（Type:c + 奖励[…]）。
    /// 解析落 <see cref="WeekCardModel"/> 并发 EVT_WEEK_CARD_UPDATE；入口红点由可领取次数及 45203
    /// 特殊提醒驱动，面板 UI 待用户验收。
    /// </summary>
    public sealed class WeekCardController : BaseController
    {
        public static readonly WeekCardController Instance = new WeekCardController();
        private WeekCardController() { }

        // 周卡主界面图标(对标老端 WeekCardController:452,open_lv=9999 默认扫描不加→自管理 AddOwnerIcon)。
        private const string ICON_TYPE = "452";

        protected override void Register()
        {
            RegisterProtocal(Proto.WEEK_CARD_INFO, On45201);
            RegisterProtocal(Proto.WEEK_CARD_CLAIM, On45202);
            RegisterProtocal(Proto.WEEK_CARD_REWARD, On45203);
            // 对标老端:CHANGE_LEVEL→复检图标;首充图标 159 被删(首充完成)→显示周卡。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshIcon);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnIconDeleted);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshIcon);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnIconDeleted);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            ActivityIconManager.Instance.SetIconRedDot(ICON_TYPE, false);
            WeekCardModel.Instance.Clear();
            base.Dispose();
        }

        public void RequestInfo() => SendFmt(Proto.WEEK_CARD_INFO);
        public void Claim() => SendFmt(Proto.WEEK_CARD_CLAIM);

        /// <summary>进游戏请求(对标老端 GAME_START 发 45201+45203)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.WEEK_CARD_INFO);
            SendFmt(Proto.WEEK_CARD_REWARD);
        }

        // 对标老端 UPDATE_ICON:首充已完成(!IsNoFirstRecharge)→显示周卡图标(452),否则删除。
        private async void RefreshIcon()
        {
            await MainUIConfigs.EnsureLoaded();
            if (!FirstRechargeModel.Instance.IsNoFirstRecharge())
                ActivityIconManager.Instance.AddOwnerIcon(ICON_TYPE);
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        // 对标老端:首充图标 159 被删(首充完成)→显示周卡。
        private async void OnIconDeleted(string iconType)
        {
            if (iconType != "159") return;
            await MainUIConfigs.EnsureLoaded();
            ActivityIconManager.Instance.AddOwnerIcon(ICON_TYPE);
        }

        private void On45201(NetReader r)
        {
            int lv = r.ReadU16();
            int exp = (int)r.ReadU32();
            bool isActivity = r.ReadU8() != 0;
            int giftBagNum = r.ReadU16();
            int canReceiveGift = r.ReadU16();
            int expiredTime = (int)r.ReadU32();
            WeekCardModel.Instance.SetInfo(lv, exp, isActivity, giftBagNum, canReceiveGift, expiredTime);
            RefreshRedDot();
            GameLog.Info("WeekCard", "45201 周卡: lv={0} 可领={1} 激活={2}", lv, canReceiveGift, isActivity);
            EventDispatcher.Emit(GlobalEvent.EVT_WEEK_CARD_UPDATE);
        }

        private void On45202(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<WeekCardModel.RewardItem> rewards = ReadRewards(r);
            WeekCardModel.Instance.SetRewards(rewards);
            if (code == 1) RequestInfo();
            GameLog.Info("WeekCard", "45202 领取结果: code={0} 奖励={1} 项", code, rewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_WEEK_CARD_UPDATE);
        }

        private void On45203(NetReader r)
        {
            int type = r.ReadU8();
            List<WeekCardModel.RewardItem> rewards = ReadRewards(r);
            WeekCardModel.Instance.SetRewards(rewards);
            if (type == 3) WeekCardModel.Instance.SetSpecialRed(true);
            RefreshRedDot();
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

        private static void RefreshRedDot()
        {
            ActivityIconManager.Instance.SetIconRedDot(
                ICON_TYPE, WeekCardModel.Instance.HasEntranceRedDot);
        }
    }
}
