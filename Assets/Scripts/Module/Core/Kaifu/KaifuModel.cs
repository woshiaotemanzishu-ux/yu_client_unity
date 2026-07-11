using System;
using System.Collections.Generic;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Kaifu
{
    /// <summary>
    /// 开服活动(投资 + 契约之书)数据(对标老客户端 KaifuActivityModel)。本期只承载主界面图标显隐要用的
    /// 状态,面板/奖励/购买等 UI 一律不碰。共三个图标各有独立开启判定与驱动协议:
    ///   4205 巅峰投资 / 1112 超值投资  —— 由 42004(投资开启列表)驱动,对标老端 InvestOpen;
    ///   424 / 424@1 契约之书          —— 由 42401(契约之书信息)驱动,对标老端 SetBookInfoData→AddBookIcon。
    /// 投资类型(对标老端 investType):1 等级 2 月卡 3 VIP 5 巅峰(top) 6 王者 7 至尊 8 神眷。
    /// </summary>
    public sealed class KaifuModel
    {
        public static readonly KaifuModel Instance = new KaifuModel();
        private KaifuModel() { }

        // ---- 图标类型(对标老端 addIcon(...) 传入的 icon_type;均为 controll_by_own_fun=true,门判在本模块自管) ----
        /// <summary>巅峰投资图标(对标老端 addIcon("4205"))。</summary>
        public const string ICON_INVEST_TOP = "4205";
        /// <summary>超值投资图标(对标老端 addIcon("1112"))。</summary>
        public const string ICON_INVEST_VALUE = "1112";
        /// <summary>契约之书图标(对标老端 AddBookIcon addIcon("424"))。</summary>
        public const string ICON_BOOK = "424";
        /// <summary>契约之书图标(8天后档,对标老端 addIcon("424@1"))。</summary>
        public const string ICON_BOOK_1 = "424@1";

        /// <summary>巅峰投资类型(对标老端 investType.top=5;state==2 且非 top 才算"进行中"要显超值图标)。</summary>
        private const int TYPE_TOP = 5;

        // 42004 投资开启项(对标服务端 pt_420 item_to_bin_1)
        public sealed class InvestItem
        {
            public int Type;        // 投资类型(1/2/3/5/6/7/8)
            public int ShowId;      // 是否走"限时展示图标"(1=是,对标老端 show_id)
            public int State;       // 状态(0/1/2 进行中,3 已结束)
            public long RefreshTime; // 刷新时间戳(限时展示窗口起点)
        }

        /// <summary>42004 原始开启列表(对标老端 open_data.open_list;面板/图标计算的数据源)。</summary>
        public readonly List<InvestItem> OpenList = new List<InvestItem>();

        /// <summary>是否显示超值投资图标 1112(对标老端 InvestOpen 的 show)。</summary>
        public bool ShowValueIcon;
        /// <summary>是否显示巅峰投资图标 4205(对标老端 InvestOpen 的 showIcon)。</summary>
        public bool ShowTopIcon;

        // 契约之书(42401)图标态:是否有活动数据、是否已全部领取(对标老端 finishAll 的图标相关近似)
        public bool BookActive;
        public bool BookAllClaimed;

        /// <summary>
        /// 解析 42004 开启列表,算出两个投资图标的显隐(对标老端 KaifuActivityModel.InvestOpen)。
        /// 1112 超值投资:任一项处于进行中(state 0/1,或 state2 且非 top),或任一限时展示项(show_id=1)仍在窗口内 → 显示。
        /// 4205 巅峰投资:任一限时展示项(show_id=1)仍在其 showDay 窗口内 → 显示(仅巅峰/王者/至尊/神眷类型有 showDay=3)。
        /// </summary>
        public void SetInvestOpen(List<InvestItem> items)
        {
            OpenList.Clear();
            if (items != null) OpenList.AddRange(items);

            bool show = false;
            bool showIcon = false;
            for (int i = 0; i < OpenList.Count; i++)
            {
                InvestItem it = OpenList[i];

                // 进行中(要显超值投资图标 1112):state 0/1,或 state2 且非 top(对标老端条件)。
                if (it.State == 0 || it.State == 1 || (it.State == 2 && it.Type != TYPE_TOP))
                {
                    show = true;
                }

                // 限时展示图标(对标老端 show_id==1 分支)。
                if (it.ShowId == 1)
                {
                    // 对标老端 cfg = ViewClassCFG[type-1];未知类型(无 cfg)→ break 整个循环。
                    if (!HasViewCfg(it.Type)) break;
                    if (FullShowIconTime(it.RefreshTime, ShowDayForType(it.Type)))
                    {
                        show = true;
                        showIcon = true;
                    }
                }
            }

            ShowValueIcon = show;
            ShowTopIcon = showIcon;
        }

        /// <summary>
        /// 记录契约之书图标态(对标老端 AddBookIcon 的 !finishAll() 判定的图标相关近似:
        /// 有活动数据且尚未全部领取则视为开启)。allClaimed 由 42401 载荷推出(无任何未领章节/阶段)。
        /// 面板(章节/阶段/奖励)未移植,这里只保留图标要用的两个布尔。
        /// </summary>
        public void SetBookInfo(bool hasData, bool allClaimed)
        {
            BookActive = hasData;
            BookAllClaimed = allClaimed;
        }

        /// <summary>契约之书图标是否应开启(对标老端 AddBookIcon 外层 !finishAll())。</summary>
        public bool GetBookIconOpen()
        {
            return BookActive && !BookAllClaimed;
        }

        public void Reset()
        {
            OpenList.Clear();
            ShowValueIcon = false;
            ShowTopIcon = false;
            BookActive = false;
            BookAllClaimed = false;
        }

        // 对标老端 ViewClassCFG:仅 1/2/3/5/6/7/8 有配置(type 4 无 → 老端 !cfg break)。
        private static bool HasViewCfg(int type)
        {
            return type == 1 || type == 2 || type == 3 || type == 5 || type == 6 || type == 7 || type == 8;
        }

        // 对标老端 ViewClassCFG[*].showDay:仅巅峰(5)/王者(6)/至尊(7)/神眷(8)有 showDay=3;等级/月卡/VIP 无(0→窗口恒不成立)。
        private static int ShowDayForType(int type)
        {
            return (type == 5 || type == 6 || type == 7 || type == 8) ? 3 : 0;
        }

        // 对标老端 fullShowIconTime:服务器时间 < (refresh_time + day 天) 当日 0 点 → 仍在展示窗口内。
        private static bool FullShowIconTime(long startSec, int day)
        {
            return TimeUtil.NowSec() < GetTimeFutureZeroStamp(startSec, day);
        }

        // 对标老端 TimeUtil.GetTimeFutureZeroStamp(starTime, future_days):取 (starTime + future_days 天) 当日 0 点时间戳(本地时区)。
        private static long GetTimeFutureZeroStamp(long startSec, int futureDays)
        {
            long temp = startSec + 86400L * futureDays;
            DateTime local = DateTimeOffset.FromUnixTimeSeconds(temp).LocalDateTime;
            DateTime zero = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Local);
            return ((DateTimeOffset)zero).ToUnixTimeSeconds();
        }
    }
}
