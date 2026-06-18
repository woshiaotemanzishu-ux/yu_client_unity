using System.Collections.Generic;

namespace Shenxiao.Module.Core.SurpriseGift
{
    /// <summary>
    /// 惊喜礼包数据（对标老客户端 SurpriseGiftController / yu_server pt_490）。只存数据；面板/红点 UI 待用户验收。
    /// </summary>
    public sealed class SurpriseGiftModel
    {
        public static readonly SurpriseGiftModel Instance = new SurpriseGiftModel();
        private SurpriseGiftModel() { }

        public struct GiftItem
        {
            public int GiftId;
            public int BuyTime;
            public int BackDay;
            public GiftItem(int giftId, int buyTime, int backDay) { GiftId = giftId; BuyTime = buyTime; BackDay = backDay; }
        }

        public struct TaskItem
        {
            public int TaskId;
            public int TaskState;
            public TaskItem(int taskId, int taskState) { TaskId = taskId; TaskState = taskState; }
        }

        // 49000 信息
        public int FreeDrawTimes;
        public int AddDrawTimes;
        public int UseFreeTimes;
        public int DrawTimes;
        public int TurnId;
        public readonly List<int> DrawList = new List<int>();          // 已抽到的 DrawId
        public readonly List<GiftItem> GiftList = new List<GiftItem>(); // 礼包购买记录
        public readonly List<TaskItem> DayTaskList = new List<TaskItem>();

        public void SetInfo(int free, int add, int useFree, int drawTimes, int turnId,
            List<int> drawList, List<GiftItem> giftList, List<TaskItem> dayTasks)
        {
            FreeDrawTimes = free; AddDrawTimes = add; UseFreeTimes = useFree; DrawTimes = drawTimes; TurnId = turnId;
            DrawList.Clear(); if (drawList != null) DrawList.AddRange(drawList);
            GiftList.Clear(); if (giftList != null) GiftList.AddRange(giftList);
            DayTaskList.Clear(); if (dayTasks != null) DayTaskList.AddRange(dayTasks);
        }

        public void SetRefresh(int free, int add, int useFree, List<TaskItem> dayTasks)
        {
            FreeDrawTimes = free; AddDrawTimes = add; UseFreeTimes = useFree;
            DayTaskList.Clear(); if (dayTasks != null) DayTaskList.AddRange(dayTasks);
        }

        public void Clear()
        {
            FreeDrawTimes = AddDrawTimes = UseFreeTimes = DrawTimes = TurnId = 0;
            DrawList.Clear(); GiftList.Clear(); DayTaskList.Clear();
        }
    }
}
