using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.SurpriseGift
{
    /// <summary>
    /// 惊喜礼包协议（对标老客户端 SurpriseGiftController / yu_server pt_490）：
    ///   49000 信息（Code:i + 次数若干:i + DrawList[h+{DrawId:i}] + GiftList[h+{GiftId:c,BuyTime:i,BackDay:h}] + DayTaskList[h+{TaskId:c,State:c}]）；
    ///   49001 抽奖结果（Code:i, GiftId:i）；49002 翻牌结果（Code:i, TurnId:h, GiftId:h, UseFreeTimes:i）；
    ///   49003 购买结果（Code:i, GiftId:i）；49004 刷新推送（次数:i×3 + DayTaskList）。
    /// 解析落 <see cref="SurpriseGiftModel"/> 并发 EVT_SURPRISE_GIFT_UPDATE；面板 UI 待用户验收。
    /// </summary>
    public sealed class SurpriseGiftController : BaseController
    {
        public static readonly SurpriseGiftController Instance = new SurpriseGiftController();
        private SurpriseGiftController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.SURPRISE_GIFT_INFO, On49000);
            RegisterProtocal(Proto.SURPRISE_GIFT_DRAW, On49001);
            RegisterProtocal(Proto.SURPRISE_GIFT_TURN, On49002);
            RegisterProtocal(Proto.SURPRISE_GIFT_BUY, On49003);
            RegisterProtocal(Proto.SURPRISE_GIFT_REFRESH, On49004);
        }

        public void RequestInfo() => SendFmt(Proto.SURPRISE_GIFT_INFO);
        public void Draw(int giftId) => SendFmt(Proto.SURPRISE_GIFT_DRAW, "i", giftId);
        public void Turn() => SendFmt(Proto.SURPRISE_GIFT_TURN);
        public void Buy(int giftId) => SendFmt(Proto.SURPRISE_GIFT_BUY, "i", giftId);

        private void On49000(NetReader r)
        {
            r.ReadU32(); // Code
            int free = (int)r.ReadU32();
            int add = (int)r.ReadU32();
            int useFree = (int)r.ReadU32();
            int drawTimes = (int)r.ReadU32();
            int turnId = (int)r.ReadU32();

            int n = r.ReadU16();
            var drawList = new List<int>(n);
            for (int i = 0; i < n; i++) drawList.Add((int)r.ReadU32());

            n = r.ReadU16();
            var giftList = new List<SurpriseGiftModel.GiftItem>(n);
            for (int i = 0; i < n; i++) giftList.Add(new SurpriseGiftModel.GiftItem(r.ReadU8(), (int)r.ReadU32(), r.ReadU16()));

            var dayTasks = ReadTasks(r);

            SurpriseGiftModel.Instance.SetInfo(free, add, useFree, drawTimes, turnId, drawList, giftList, dayTasks);
            GameLog.Info("SurpriseGift", "49000 信息: free={0} 抽过={1} 礼包={2} 日任务={3}", free, drawList.Count, giftList.Count, dayTasks.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        private void On49001(NetReader r)
        {
            int code = (int)r.ReadU32();
            int giftId = (int)r.ReadU32();
            GameLog.Info("SurpriseGift", "49001 抽奖: code={0} giftId={1}", code, giftId);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        private void On49002(NetReader r)
        {
            int code = (int)r.ReadU32();
            int turnId = r.ReadU16();
            int giftId = r.ReadU16();
            int useFree = (int)r.ReadU32();
            GameLog.Info("SurpriseGift", "49002 翻牌: code={0} turnId={1} giftId={2} useFree={3}", code, turnId, giftId, useFree);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        private void On49003(NetReader r)
        {
            int code = (int)r.ReadU32();
            int giftId = (int)r.ReadU32();
            GameLog.Info("SurpriseGift", "49003 购买: code={0} giftId={1}", code, giftId);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        private void On49004(NetReader r)
        {
            int free = (int)r.ReadU32();
            int add = (int)r.ReadU32();
            int useFree = (int)r.ReadU32();
            var dayTasks = ReadTasks(r);
            SurpriseGiftModel.Instance.SetRefresh(free, add, useFree, dayTasks);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        private static List<SurpriseGiftModel.TaskItem> ReadTasks(NetReader r)
        {
            int n = r.ReadU16();
            var list = new List<SurpriseGiftModel.TaskItem>(n);
            for (int i = 0; i < n; i++) list.Add(new SurpriseGiftModel.TaskItem(r.ReadU8(), r.ReadU8()));
            return list;
        }
    }
}
