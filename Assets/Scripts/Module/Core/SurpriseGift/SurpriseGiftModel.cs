using System.Collections.Generic;

namespace Shenxiao.Module.Core.SurpriseGift
{
    /// <summary>
    /// 惊喜礼包数据（对标老客户端 SurpriseGiftModel / yu_server pt_490）。承载 49000 下发的惊喜礼包信息，
    /// 并据此判定主界面图标(490)显隐：服务端 pp_surprise_gift.handle 仅在「开服天数≥SURP_KV_OPEN_DAY 且
    /// 等级≥SURP_KV_OPEN_LEV」时才回 49000（否则 skip 不回包），故收到 49000 且 code==成功即视为活动开启
    /// (<see cref="GetEntranceOpenState"/>，对标老端 getOpenStatus 的等级/开服门槛——服务端已把控)。
    /// 红点/翻牌/面板 UI 待用户验收，本期只做图标 + 数据解析。
    /// </summary>
    public sealed class SurpriseGiftModel
    {
        public static readonly SurpriseGiftModel Instance = new SurpriseGiftModel();
        private SurpriseGiftModel() { }

        /// <summary>主界面图标类型(对标老端 ActivityIconManager.addIcon(490)/deleteIcon(490))。</summary>
        public const string ICON_TYPE = "490";

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

        // 入口开启标记:49000 回包 code==成功 即为开启(等级/开服门槛由服务端 handle/3 把控,见类注释)。
        public bool IsOpen;

        // 49000 信息
        public int FreeDrawTimes;
        public int AddDrawTimes;
        public int UseFreeTimes;
        public int DrawTimes;
        public int TurnId;
        public readonly List<int> DrawList = new List<int>();          // 已抽到的 DrawId
        public readonly List<GiftItem> GiftList = new List<GiftItem>(); // 礼包购买记录
        public readonly List<TaskItem> DayTaskList = new List<TaskItem>();

        public void SetInfo(bool isOpen, int free, int add, int useFree, int drawTimes, int turnId,
            List<int> drawList, List<GiftItem> giftList, List<TaskItem> dayTasks)
        {
            IsOpen = isOpen;
            FreeDrawTimes = free; AddDrawTimes = add; UseFreeTimes = useFree; DrawTimes = drawTimes; TurnId = turnId;
            DrawList.Clear(); if (drawList != null) DrawList.AddRange(drawList);
            GiftList.Clear(); if (giftList != null) GiftList.AddRange(giftList);
            DayTaskList.Clear(); if (dayTasks != null) DayTaskList.AddRange(dayTasks);
        }

        // 49004 刷新推送:只更新次数/日任务,不改开启态(对标老端 On49004 只刷红点,不动图标增删)。
        public void SetRefresh(int free, int add, int useFree, List<TaskItem> dayTasks)
        {
            FreeDrawTimes = free; AddDrawTimes = add; UseFreeTimes = useFree;
            DayTaskList.Clear(); if (dayTasks != null) DayTaskList.AddRange(dayTasks);
        }

        /// <summary>
        /// 入口开启状态(对标老端 getOpenStatus 的等级/开服门槛)。服务端 pp_surprise_gift.handle 已按
        /// 开服天数/等级把控:未开则 49000 直接 skip 不回包,客户端只看是否收到 code==成功 的 49000。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return IsOpen;
        }

        public void Reset()
        {
            IsOpen = false;
            FreeDrawTimes = AddDrawTimes = UseFreeTimes = DrawTimes = TurnId = 0;
            DrawList.Clear(); GiftList.Clear(); DayTaskList.Clear();
        }
    }
}
