using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// BOSS(节日大妖/节日BOSS)图标数据(对标老客户端 BossModel 中与 addIcon("51") 相关的那一小块)。
    /// 大 BOSS 系统体量庞大(本服/跨服 46xxx/47xxx 玩法协议),本期只承载"节日大妖"主界面图标 51 的显隐,
    /// 玩法(进副本/BOSS信息/掉落/复活等)一律不做。
    ///
    /// 图标 51 = 老端 CustomActivityDefine.AdvanceIcon[51]="51",由 BossModel.FeastBossActivity 驱动:
    /// 读自定义活动(FEASTBOSS,base_type=51)的 condition 时间窗——
    ///   · 处于活动时间窗内 → addIcon("51", 活动结束时间戳)(带倒计时);
    ///   · 当天有下一场未开始 → addIcon("51", 文本 "HH:MM开启")(预告);
    ///   · 都没有 → deleteIcon("51")。
    /// 门槛(是否有节日BOSS活动、时间窗)全在服务端 + 自定义活动条件里把控,本 Model 只存最终判定结果。
    /// </summary>
    public sealed class BossModel
    {
        public static readonly BossModel Instance = new BossModel();
        private BossModel() { }

        /// <summary>主界面图标类型(对标老端 CustomActivityDefine.AdvanceIcon[51]="51")。</summary>
        public const string ICON_TYPE = "51";

        /// <summary>节日大妖活动的自定义活动 base_type(对标老端 ConfigCustomActivity.ACT_ID.FEASTBOSS=51)。</summary>
        public const int FEAST_BOSS_BASE_TYPE = 51;

        /// <summary>
        /// 服务端配置时区(线上=UTC+8,按"落定线上值"做法)。活动 condition 里 time 窗的时分秒是服务端墙钟,
        /// 需把 UTC 服务器时间(TimeUtil.NowUtc)加此偏移换算成墙钟再比窗(对标老端 TimeUtil.GetZoneTime 的 server_zone)。
        /// </summary>
        public const int SERVER_ZONE_HOURS = 8;

        /// <summary>
        /// 计算节日BOSS当前三态(对标老端 BossModel.GetFeastBossTime + FeastBossActivity 的图标分支)。
        /// condition 形如 [{show_time,N},{time,[{{H,M,S},{H,M,S}},...]}];窗按服务端时区墙钟(UTC+8)判定。
        /// 返回:active=是否处于某窗内;endTime=窗结束的 unix 时间戳(倒计时用);foreshadow=当天下一场"HH:MM开启"文本(无则 null)。
        /// nowSec=原始服务器 unix 秒(TimeUtil.NowSec);活动总区间 [actStartTime, actEndTime) 之外一律无。
        /// </summary>
        public static (bool active, int endTime, string foreshadow) ComputeFeastWindow(
            string condition, int actStartTime, int actEndTime, long nowSec)
        {
            // 活动总区间外:无(对标老端 curTime>=stime && curTime<etime 的外层门)
            if (actStartTime > 0 && nowSec < actStartTime) return (false, 0, null);
            if (actEndTime > 0 && nowSec >= actEndTime) return (false, 0, null);

            IReadOnlyList<ErlangTerm> timeArr = ExtractTimeWindows(condition);
            if (timeArr == null || timeArr.Count == 0) return (false, 0, null);

            // 服务端墙钟时分秒(UTC+SERVER_ZONE_HOURS)
            DateTime zoneNow = TimeUtil.NowUtc().AddHours(SERVER_ZONE_HOURS);
            int hour = zoneNow.Hour, minute = zoneNow.Minute, second = zoneNow.Second;
            int nowAllToday = hour * 3600 + minute * 60 + second; // 今日已过秒(墙钟)

            // 1) 是否在某个窗内(对标老端 now_all>0 && now_all<=all_sce_time)
            for (int i = 0; i < timeArr.Count; i++)
            {
                if (!TryReadWindow(timeArr[i], out int sH, out _, out _, out int startSec, out int eH, out int endSec)) continue;
                if (hour >= sH && hour <= eH && nowAllToday > startSec && nowAllToday <= endSec)
                {
                    int left = endSec - nowAllToday;                 // 距窗结束剩余秒
                    return (true, (int)(nowSec + left), null);       // 倒计时到窗结束的 unix 时间戳
                }
            }

            // 2) 当天有没有下一场未开始的窗 → 预告 "HH:MM开启"(对标老端 next_open_time 分支)
            for (int i = 0; i < timeArr.Count; i++)
            {
                if (!TryReadWindow(timeArr[i], out int sH, out int sM, out _, out int startSec, out _, out _)) continue;
                if (nowAllToday < startSec)
                {
                    string mm = sM >= 10 ? sM.ToString() : "0" + sM;
                    return (false, 0, sH + ":" + mm + "开启");
                }
            }

            return (false, 0, null);
        }

        // 从 condition 里取 {time,[...]} 的窗列表(对标老端 GetFeastBossTime 遍历 condition 找 "time")。
        private static IReadOnlyList<ErlangTerm> ExtractTimeWindows(string condition)
        {
            ErlangTerm cond = ErlangParser.Parse(condition);
            if (cond?.Items == null) return null;
            foreach (ErlangTerm tup in cond.Items)
            {
                IReadOnlyList<ErlangTerm> kv = tup?.Items;
                if (kv == null || kv.Count < 2) continue;
                if (kv[0].As<string>() == "time") return kv[1]?.Items;
            }
            return null;
        }

        // 解析单个窗 {{sH,sM,sS},{eH,eM,eS}} → 起止时分秒 + 当日秒偏移。
        private static bool TryReadWindow(ErlangTerm window, out int sH, out int sM, out int sS,
            out int startSec, out int eH, out int endSec)
        {
            sH = sM = sS = startSec = eH = endSec = 0;
            IReadOnlyList<ErlangTerm> win = window?.Items;
            if (win == null || win.Count < 2) return false;
            IReadOnlyList<ErlangTerm> a = win[0]?.Items, b = win[1]?.Items;
            if (a == null || b == null || a.Count < 3 || b.Count < 3) return false;
            sH = a[0].As<int>(); sM = a[1].As<int>(); sS = a[2].As<int>();
            eH = b[0].As<int>(); int eM = b[1].As<int>(), eS = b[2].As<int>();
            startSec = sH * 3600 + sM * 60 + sS;
            endSec = eH * 3600 + eM * 60 + eS;
            return true;
        }

        // 节日BOSS图标最终判定(对标老端 FeastBossActivity 的三态):
        public bool FeastBossActive;       // 是否处于活动时间窗内(带倒计时)
        public int FeastBossEndTime;       // 活动结束时间戳(秒),AddIconAsync 的 time 入参,做倒计时
        public string FeastBossForeshadow; // 预告文本("HH:MM开启");非空表示当天有下一场未开始

        /// <summary>
        /// 写入节日BOSS图标判定(对标老端 FeastBossActivity 计算出的 [is_in_activity_time, left_time, next_open_time])。
        /// active=时间窗内、endTime=结束时间戳(倒计时用)、foreshadow=预告文本("HH:MM开启",无则传 null/空)。
        /// </summary>
        public void SetFeastBossActivity(bool active, int endTime, string foreshadow)
        {
            FeastBossActive = active;
            FeastBossEndTime = endTime;
            FeastBossForeshadow = foreshadow;
        }

        /// <summary>
        /// 入口开启状态(对标老端 FeastBossActivity:处于活动时间窗 或 有下一场预告 时才挂图标 51)。
        /// 两者皆无 → 无节日BOSS活动 → 不显示。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return FeastBossActive || !string.IsNullOrEmpty(FeastBossForeshadow);
        }

        public void Reset()
        {
            FeastBossActive = false;
            FeastBossEndTime = 0;
            FeastBossForeshadow = null;
        }
    }
}
