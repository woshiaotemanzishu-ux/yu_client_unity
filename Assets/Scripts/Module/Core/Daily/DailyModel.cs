using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 日常中心数据层(自动循环 轮10;对标老端 commonModel/DailyModel.ts)。
    /// 15701 每日任务/限时活动共用一张读表(按 act_type 分两槽存),复刻老端 SortUnLimitActivity/
    /// SortLimitActivityList 排序算法(能否领活跃度&gt;开启状态&gt;达上限&gt;今日/非今日&gt;开始时间&gt;等级);
    /// 15703 活跃度宝箱、15709/15710 活跃度形象、15714 挂机时间、15715/15716 活跃度找回(功能已下线,只存不建 UI)、
    /// 15718/15719/15720 预约表、15721 活动提醒、41900/41903/41904 资源找回、61801 我要变强 均落在本类。
    /// </summary>
    public sealed class DailyModel
    {
        public static readonly DailyModel Instance = new DailyModel();
        private DailyModel() { }

        public const int ACT_UNLIMIT = 1;
        public const int ACT_LIMIT = 2;

        /// <summary>对标老端 DailyModel.LivenessFindOpenLevel(50 级开活跃度找回,GAME_START 达标才发 15715)。</summary>
        public const int LIVENESS_FIND_OPEN_LEVEL = 50;

        /// <summary>
        /// 服务端配置时区(线上=UTC+8)。老端所有本地时钟判定(限时活动 time_region 开启态/EstimateIsToday
        /// 周几月日/预约红点时分比较)都用 TimeUtil.GetZoneTime() 服务器墙钟,不是裸 UTC——同本仓 BossModel.
        /// SERVER_ZONE_HOURS 先例。<see cref="TimeUtilNowUtc"/> 统一在此加偏移,避免各判定点各自裸用 NowUtc()
        /// 系统性偏移 8 小时(轮10交叉验收 blocker)。
        /// </summary>
        public const int SERVER_ZONE_HOURS = 8;

        // 排序算法内部用的展示态(对标老端 ActivityState 枚举;非协议原始 State 字段)
        private const int AS_OPENING = 1, AS_TODAY = 2, AS_UNOPENED = 3, AS_TIMEOVER = 4, AS_LVLIMIT = 5, AS_CLOSED = 6;
        private const int TODAY_YES = 1, TODAY_NO = 2;
        private const int TT_NONE = 0, TT_WEEK = 1, TT_MONTH = 2, TT_DATE = 3;

        // =====================================================================================
        // 15701:每日任务/限时活动共用读表
        // =====================================================================================

        /// <summary>15701 单条(字段名对照 r10_server 15701 字段序速查表)。</summary>
        public sealed class ActivityVo
        {
            public int Module, ModuleSub, AcSub, Num, MaxNum, Live, MaxLive, CanGetLive, State;
            // 以下为排序算法补的派生字段(对标 GetDataFromConfig)
            public int Rank, StartLv, LeftNum;
        }

        public sealed class DailyDataVo
        {
            public int ActType;
            public long OnHookTime;
            public List<ActivityVo> AcList = new List<ActivityVo>();
        }

        private readonly Dictionary<int, DailyDataVo> _dailyData = new Dictionary<int, DailyDataVo>();

        public DailyDataVo GetDailyData(int actType) => _dailyData.TryGetValue(actType, out DailyDataVo v) ? v : null;

        public bool DailyTaskFirstRed { get; private set; }
        private bool _dailyTaskFirstChecked;

        /// <summary>15701 落地(对标 SetDailyData):按等级摘除超龄/无配置条目 + 复刻老端排序算法。</summary>
        public void SetDailyData(int actType, long onHookTime, List<ActivityVo> rawList)
        {
            int lv = RoleModel.Instance.Level;
            var filtered = new List<ActivityVo>();
            if (rawList != null)
            {
                foreach (ActivityVo vo in rawList)
                {
                    JObject liveCfg = DailyConfigs.GetActivityLiveness(vo.Module, vo.ModuleSub);
                    JObject acCfg = DailyConfigs.GetAc(vo.Module, vo.ModuleSub, vo.AcSub);
                    bool overLevel = (liveCfg != null && lv > DailyConfigs.ReadInt(liveCfg, "end_lv"))
                                     || (acCfg != null && lv > DailyConfigs.ReadInt(acCfg, "end_lv"));
                    if (acCfg == null || overLevel) continue; // 对标老端 delete_index 摘除
                    ApplyRankAndLeftNum(vo, acCfg, liveCfg);
                    filtered.Add(vo);
                }
            }
            if (actType == ACT_UNLIMIT) SortUnLimitActivity(filtered);
            else SortLimitActivity(filtered);
            _dailyData[actType] = new DailyDataVo { ActType = actType, OnHookTime = onHookTime, AcList = filtered };
            if (actType == ACT_UNLIMIT) CheckDailyTaskRed();
        }

        private static void ApplyRankAndLeftNum(ActivityVo vo, JObject acCfg, JObject liveCfg)
        {
            vo.Rank = acCfg != null ? DailyConfigs.ReadInt(acCfg, "rank") : 0;
            vo.StartLv = acCfg != null ? DailyConfigs.ReadInt(acCfg, "start_lv") : 0;
            vo.LeftNum = 0;
            if (liveCfg != null)
            {
                int max = DailyConfigs.ReadInt(liveCfg, "max");
                vo.LeftNum = max - vo.Num;
                // 610@10(专属大妖)老端另叠加 config_ac_kv/VipModel 的 VIP 加成次数——该表未导入,TODO(不影响其余分支)。
            }
        }

        // ---- 每日任务排序(对标 SortUnLimitActivity) ----

        private static void SortUnLimitActivity(List<ActivityVo> list)
        {
            list.Sort((a, b) =>
            {
                int aState = GetUnLimitOpenState(a);
                int bState = GetUnLimitOpenState(b);
                int aVal = (a.CanGetLive > 0 ? 10000 : 0) + UnLimitFuncSub(aState, a);
                int bVal = (b.CanGetLive > 0 ? 10000 : 0) + UnLimitFuncSub(bState, b);
                if (aVal != bVal) return aVal.CompareTo(bVal);
                return aState == AS_LVLIMIT ? a.StartLv.CompareTo(b.StartLv) : a.Rank.CompareTo(b.Rank);
            });
        }

        private static int GetUnLimitOpenState(ActivityVo vo)
        {
            JObject cfg = DailyConfigs.GetActivityLiveness(vo.Module, vo.ModuleSub);
            int max = cfg != null ? DailyConfigs.ReadInt(cfg, "max") : 0;
            if (vo.State == 3) return AS_LVLIMIT;
            if (vo.State == 2) return AS_TIMEOVER;
            if (cfg != null && max == 0) return AS_OPENING;
            return vo.LeftNum > 0 ? AS_OPENING : AS_CLOSED;
        }

        private static int UnLimitFuncSub(int state, ActivityVo vo)
        {
            if (state != AS_CLOSED)
            {
                if (state == AS_OPENING)
                {
                    bool special = (vo.Module == 132 && vo.ModuleSub == 0) || (vo.Module == 157 && vo.ModuleSub == 0);
                    if (special) return 100 * AS_LVLIMIT - 50;
                    return vo.Num >= vo.MaxNum ? 100 * AS_CLOSED : 100 * state;
                }
                return 100 * state;
            }
            return vo.Num >= vo.MaxNum ? 100 * AS_CLOSED : 100 * AS_UNOPENED - 50;
        }

        // ---- 限时活动排序(对标 SortLimitActivityList) ----

        private void SortLimitActivity(List<ActivityVo> list)
        {
            DateTime dt = TimeUtilNowUtc();
            var keys = new Dictionary<ActivityVo, (int startSec, int startLv, int state, int today, int timeType)>();
            foreach (ActivityVo vo in list) keys[vo] = ComputeLimitSortKey(vo, dt);
            list.Sort((a, b) =>
            {
                (int startSec, int startLv, int state, int today, int timeType) ka = keys[a];
                (int startSec, int startLv, int state, int today, int timeType) kb = keys[b];
                if (ka.state != kb.state) return ka.state.CompareTo(kb.state);
                if (ka.state == AS_LVLIMIT) return ka.startLv.CompareTo(kb.startLv);
                if (ka.today != kb.today) return ka.today.CompareTo(kb.today);
                if (ka.startSec != kb.startSec) return ka.startSec.CompareTo(kb.startSec);
                if (ka.startLv != kb.startLv) return ka.startLv.CompareTo(kb.startLv);
                return ka.timeType.CompareTo(kb.timeType);
            });
        }

        private static DateTime TimeUtilNowUtc() => Shenxiao.Framework.Util.TimeUtil.NowUtc().AddHours(SERVER_ZONE_HOURS);

        private (int startSec, int startLv, int state, int today, int timeType) ComputeLimitSortKey(ActivityVo vo, DateTime dt)
        {
            JObject acCfg = DailyConfigs.GetAc(vo.Module, vo.ModuleSub, vo.AcSub);
            int startSec = 0, startLv = 0, state = AS_UNOPENED;
            if (acCfg != null)
            {
                List<(int startH, int startM, int endH, int endM)> region =
                    DailyConfigs.ParseTimeRegion(DailyConfigs.ReadString(acCfg, "time_region"));
                if (region.Count > 0)
                {
                    int nowOfDay = dt.Hour * 3600 + dt.Minute * 60;
                    foreach ((int startH, int startM, int endH, int endM) r in region)
                    {
                        int s = r.startH * 3600 + r.startM * 60;
                        int e = r.endH * 3600 + r.endM * 60;
                        startSec = s;
                        if (nowOfDay < s) { state = AS_UNOPENED; break; }
                        if (nowOfDay >= e) { state = AS_CLOSED; continue; }
                        state = AS_OPENING; break;
                    }
                }
                startLv = DailyConfigs.ReadInt(acCfg, "start_lv");
            }

            int today = EstimateIsToday(acCfg, dt) ? TODAY_YES : TODAY_NO;
            if (today == TODAY_NO) state = AS_UNOPENED;
            else if (state != AS_CLOSED) state = AS_TODAY;

            if (vo.State == 3) state = AS_LVLIMIT;
            else if (vo.State == 1) state = AS_OPENING;
            else if (vo.State == 2 && today == TODAY_YES && state == AS_CLOSED)
            {
                // 已参与+今日+服务器"关闭"(=已结束待预约领奖)+预约未领 → 排最前
                // (对标老端 GetActivityStartSecAndStartLv 里 serverState==2 的特判分支)。
                if (CheckIsJoin(vo.Module, vo.ModuleSub, vo.AcSub)
                    && TryGetReservation(vo.Module, vo.ModuleSub, vo.AcSub, out int res) && res != 2)
                    state = 0;
            }

            int timeType = GetTimeType(acCfg);
            return (startSec, startLv, state, today, timeType);
        }

        private static bool EstimateIsToday(JObject acCfg, DateTime dt)
        {
            if (acCfg == null) return false;
            int weekday = (int)dt.DayOfWeek; // Sunday=0
            if (weekday == 0) weekday = 7;

            List<(int year, int month0, int day)> dateList = DailyConfigs.ParseDateList(DailyConfigs.ReadString(acCfg, "time"));
            List<int> monthList = DailyConfigs.ParseIntArray(DailyConfigs.ReadString(acCfg, "month"));
            List<int> weekList = DailyConfigs.ParseIntArray(DailyConfigs.ReadString(acCfg, "week"));
            List<(int start, int end)> openDayList = DailyConfigs.ParseDayRanges(DailyConfigs.ReadString(acCfg, "open_day"));
            List<(int start, int end)> mergeDayList = DailyConfigs.ParseDayRanges(DailyConfigs.ReadString(acCfg, "merge_day"));
            int openDay = ServerTimeModel.GetOpenServerDay();
            const int mergeDay = 0; // Unity 未接合服天数(ServerTimeModel 无 GetMergeServerDay),TODO——恒 0 即走开服天分支

            bool isToday = false;
            if (mergeDay > 0 && mergeDayList.Count > 0)
            {
                // 恒不可达(mergeDay 恒 0),保留分支结构以便未来接入合服天数(TODO)。
            }
            else if (openDayList.Count > 0)
            {
                (int start, int end)? hit = null;
                foreach ((int start, int end) d in openDayList)
                    if (openDay <= d.end) { hit = d; break; } // 对标老端 GetOpenOrMergeDayByCfg(day<=start || day>start&&day<=end 等价于 day<=end)
                if (hit.HasValue && openDay >= hit.Value.start && openDay <= hit.Value.end)
                    isToday = weekList.Count > 0 ? weekList.Contains(weekday) : true;
            }
            else if (weekList.Count != 0) isToday = weekList.Contains(weekday);
            else if (monthList.Count != 0) isToday = monthList.Contains(dt.Month - 1); // 对标老端 getMonth() 0-based
            else if (dateList.Count != 0)
            {
                foreach ((int year, int month0, int day) d in dateList)
                    if (d.year == dt.Year && d.month0 == dt.Month - 1 && d.day == dt.Day) { isToday = true; break; }
            }
            else if (weekList.Count == 0 && monthList.Count == 0 && dateList.Count == 0) isToday = true;

            return isToday;
        }

        private static int GetTimeType(JObject acCfg)
        {
            if (acCfg == null) return TT_NONE;
            if (DailyConfigs.ParseDateList(DailyConfigs.ReadString(acCfg, "time")).Count != 0) return TT_DATE;
            if (DailyConfigs.ParseIntArray(DailyConfigs.ReadString(acCfg, "month")).Count != 0) return TT_MONTH;
            if (DailyConfigs.ParseIntArray(DailyConfigs.ReadString(acCfg, "week")).Count != 0) return TT_WEEK;
            return TT_NONE;
        }

        /// <summary>写死日常任务红点(对标 CheckDailyTaskRed:300@1@0 仙宗日常任务首次登录是否有未完成)。</summary>
        private void CheckDailyTaskRed()
        {
            if (!_dailyTaskFirstChecked)
            {
                _dailyTaskFirstChecked = true;
                ActivityVo vo = GetDailyTaskVo(300, 1, 0);
                JObject liveCfg = DailyConfigs.GetActivityLiveness(300, 1);
                if (vo != null && liveCfg != null && DailyConfigs.ReadInt(liveCfg, "max") > vo.Num) DailyTaskFirstRed = true;
            }
            if (DailyTaskFirstRed)
            {
                ActivityVo vo = GetDailyTaskVo(300, 1, 0);
                JObject liveCfg = DailyConfigs.GetActivityLiveness(300, 1);
                if (vo != null && liveCfg != null && DailyConfigs.ReadInt(liveCfg, "max") == vo.Num) DailyTaskFirstRed = false;
            }
        }

        public ActivityVo GetDailyTaskVo(int module, int moduleSub, int acSub)
        {
            if (!_dailyData.TryGetValue(ACT_UNLIMIT, out DailyDataVo data)) return null;
            foreach (ActivityVo vo in data.AcList)
                if (vo.Module == module && vo.ModuleSub == moduleSub && vo.AcSub == acSub) return vo;
            return null;
        }

        /// <summary>15706 原地改 state(对标 UpdateDailyData;650@1 CSPVP 联动分支老端已注释,不抄)。</summary>
        public bool UpdateActivityState(int actType, int module, int moduleSub, int status)
        {
            if (!_dailyData.TryGetValue(actType, out DailyDataVo data)) return false;
            bool found = false;
            foreach (ActivityVo vo in data.AcList)
                if (vo.Module == module && vo.ModuleSub == moduleSub) { vo.State = status; found = true; }
            if (found) ResortActivity(actType);
            return found;
        }

        /// <summary>老端 SortUnLimitActivity/SortLimitActivityList 由 View 每次渲染时调用(排序键实时取
        /// state/预约表);本端排序落在数据层,改为在状态会变化的写入点(15706/15718/15719/15720)后主动
        /// 重排受影响槽位,效果等价(避免视图渲染仍按旧序,轮10交叉验收 minor)。</summary>
        private void ResortActivity(int actType)
        {
            if (!_dailyData.TryGetValue(actType, out DailyDataVo data)) return;
            if (actType == ACT_UNLIMIT) SortUnLimitActivity(data.AcList);
            else SortLimitActivity(data.AcList);
        }

        /// <summary>预约表(15718/15719)会影响限时活动排序键(CheckIsJoin/TryGetReservation),两槽都可能受影响,
        /// 统一重排两个已存在的槽位。</summary>
        private void ResortAllActivityData()
        {
            foreach (int actType in new List<int>(_dailyData.Keys)) ResortActivity(actType);
        }

        // =====================================================================================
        // 15703:活跃度宝箱进度
        // =====================================================================================

        public sealed class LivenessRewardVo { public int Id; public int State; }

        public int LivenessLive { get; private set; }
        public int LivenessLiveMax { get; private set; }
        public List<LivenessRewardVo> LivenessRewardList { get; private set; } = new List<LivenessRewardVo>();

        public void SetLivenessReward(int live, int liveMax, List<LivenessRewardVo> list)
        {
            LivenessLive = live;
            LivenessLiveMax = liveMax;
            LivenessRewardList = list ?? new List<LivenessRewardVo>();
            LivenessRewardList.Sort((a, b) => a.Id.CompareTo(b.Id)); // 对标老端 SetLivenessReward 按 id 升序
        }

        /// <summary>对标 GetLivenessDataByIndex(1-based,底栏 4 个宝箱格用)。</summary>
        public LivenessRewardVo GetLivenessRewardByIndex(int index1Based)
            => index1Based >= 1 && index1Based <= LivenessRewardList.Count ? LivenessRewardList[index1Based - 1] : null;

        // =====================================================================================
        // 15709/15710:活跃度形象
        // =====================================================================================

        public int LivenessImgLv { get; private set; }
        public int LivenessImgLiveness { get; private set; }
        public int LivenessImgId { get; private set; }
        public int LivenessImgDisplay { get; private set; }
        public bool HasLivenessImg { get; private set; }

        public void SetLivenessImage(int lv, int liveness, int id, int display)
        {
            LivenessImgLv = lv;
            LivenessImgLiveness = liveness;
            LivenessImgId = id;
            LivenessImgDisplay = display;
            HasLivenessImg = true;
        }

        // =====================================================================================
        // 15714:离线挂机时间
        // =====================================================================================

        public long OutlineTime { get; private set; }
        public void SetOutlineTime(long time) => OutlineTime = time;

        // =====================================================================================
        // 15715/15716:活跃度找回(功能已下线,仅存数据,不建 UI)
        // =====================================================================================

        public sealed class HuoYueDuVo { public int ActId; public int ActSub; public int Lefttimes; public int BackTimes; }

        public List<HuoYueDuVo> HuoYueDuList { get; private set; } = new List<HuoYueDuVo>();

        public void SetHuoYueDuData(List<HuoYueDuVo> list)
        {
            HuoYueDuList = list ?? new List<HuoYueDuVo>();
            // 对标老端 sortFunc `b.lefttimes > a.lefttimes`(Lua less-than 语义,true=a排前)= 次数少的排前(升序)。
            HuoYueDuList.Sort((a, b) => a.Lefttimes == b.Lefttimes ? a.ActId.CompareTo(b.ActId) : a.Lefttimes.CompareTo(b.Lefttimes));
        }

        // =====================================================================================
        // 15718/15719/15720:限时活动预约状态表(对标老端 local_reservation/local_join)
        // =====================================================================================

        private readonly Dictionary<string, int> _reservation = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _join = new Dictionary<string, int>();

        public int DailyResRed { get; private set; }

        private static string Key(int module, int moduleSub, int acSub) => module + "@" + moduleSub + "@" + acSub;

        public bool TryGetReservation(int module, int moduleSub, int acSub, out int status)
            => _reservation.TryGetValue(Key(module, moduleSub, acSub), out status);

        public bool CheckIsJoin(int module, int moduleSub, int acSub)
            => _join.TryGetValue(Key(module, moduleSub, acSub), out int j) && j == 1;

        public void SetReservationState(int module, int moduleSub, int acSub, int status)
        {
            _reservation[Key(module, moduleSub, acSub)] = status;
            ResortAllActivityData(); // 预约状态变化(如 15720 领取置2)会改变限时活动排序键,重排
        }

        /// <summary>15718 整表落地(对标 SetResData(is_table=true)):过滤 module==500,重算 dailyResRed。</summary>
        public void SetResTable(List<(int module, int moduleSub, int acSub, int status, int join)> list)
        {
            _reservation.Clear();
            _join.Clear();
            int red = 0;
            int lv = RoleModel.Instance.Level;
            DateTime dt = TimeUtilNowUtc();
            if (list != null)
            {
                foreach ((int module, int moduleSub, int acSub, int status, int join) vo in list)
                {
                    if (vo.module == 500) continue;
                    bool canRes = false;
                    JObject cfg = DailyConfigs.GetAc(vo.module, vo.moduleSub, vo.acSub);
                    if (vo.status == 0 && cfg != null)
                    {
                        List<(int startH, int startM, int endH, int endM)> region =
                            DailyConfigs.ParseTimeRegion(DailyConfigs.ReadString(cfg, "time_region"));
                        if (region.Count > 0)
                        {
                            (int startH, int startM, int endH, int endM) first = region[0];
                            if (dt.Hour < first.startH || (dt.Hour == first.startH && dt.Minute < first.startM))
                                canRes = lv >= DailyConfigs.ReadInt(cfg, "start_lv");
                        }
                    }
                    _reservation[Key(vo.module, vo.moduleSub, vo.acSub)] = vo.status;
                    _join[Key(vo.module, vo.moduleSub, vo.acSub)] = vo.join;
                    // ⚠订正:老端原句 `if (vo.status && canRes)` ——canRes 只在上面 `vo.status==0` 分支内才可能被
                    // 置真,而此处又要求 vo.status 为真(非0),两者互斥,老端这行按字面复刻永远是死代码
                    // (dailyResRed 经本路径恒为0,纯 JS 转译 bug,与协议/双端交互无关)。按其显然意图订正为
                    // "未预约(status==0)且可预约(canRes)才计入红点"(依 rule10 订正并在此注明)。
                    if (vo.status == 0 && canRes) red++;
                }
            }
            DailyResRed = red;
            ResortAllActivityData(); // 预约表变化会影响限时活动"已结束待领奖"排最前的分支,重排
        }

        /// <summary>15719 成功单条落地(对标 SetResData(is_table=false),每次调用都对标老端 dailyResRed--)。
        /// 服务端领奖成功(15720)会同时广播一条 15719(status=2),红点的那一次 -1 就发生在这里——15720 自身
        /// 不再单独扣红点(轮10交叉验收 blocker 订正:此前 On15720 额外调 DecrementResRed 造成双扣)。</summary>
        public void SetResSingle(int module, int moduleSub, int acSub, int status, int join)
        {
            _reservation[Key(module, moduleSub, acSub)] = status;
            _join[Key(module, moduleSub, acSub)] = join;
            if (DailyResRed > 0) DailyResRed--;
            ResortAllActivityData();
        }

        // =====================================================================================
        // 15721:限时活动开启提醒
        // =====================================================================================

        public sealed class ActRemindVo { public int Module, ModuleSub, AcSub, State, Time, SignState; }

        public bool IsRemind { get; private set; } = true;
        public List<ActRemindVo> ActTipList { get; private set; } = new List<ActRemindVo>();

        public void SetDailyActData(bool isRemind, List<ActRemindVo> list)
        {
            IsRemind = isRemind;
            ActTipList = list ?? new List<ActRemindVo>();
        }

        /// <summary>对标老端 HasNewAct:比对 module 差集,返回(是否有新增, 新增的第一个 module)。
        /// 老端首判定原句 `if ((!data||size(data)&lt;=0) || (new_list&amp;&amp;...&amp;&amp;data&amp;&amp;size(data)&gt;0) &amp;&amp;
        /// size(new_list)&gt;size(data)) return [true,new_module]` 按运算符优先级实际语义 =
        /// "旧表为空 || (旧表非空 &amp;&amp; 新表比旧表大)"——第二支路"新表变长"正是"新活动开启"最常见的命中路径,
        /// 并非可丢弃的转译缺陷(轮10交叉验收 blocker 订正:之前误判"近乎恒真"而整支丢弃,导致列表变长的
        /// 最常见场景 hasNew 恒 false)。本端忠实还原两支路:①旧表为空 ②新表严格更长;都不成立才落回按
        /// module 差集比对(老端第二段循环,新活动加入但表长不变——如同长度替换旧活动——时的兜底判定)。</summary>
        public (bool hasNew, int newModule) HasNewAct(List<ActRemindVo> newList)
        {
            List<ActRemindVo> old = ActTipList;
            if (old == null || old.Count == 0) return (true, 0);
            if (newList == null) return (false, 0);
            if (newList.Count > old.Count)
            {
                int firstNewModule = 0;
                foreach (ActRemindVo n in newList)
                {
                    bool inOld = false;
                    foreach (ActRemindVo o in old) if (o.Module == n.Module) { inOld = true; break; }
                    if (!inOld) { firstNewModule = n.Module; break; }
                }
                return (true, firstNewModule);
            }
            foreach (ActRemindVo o in old)
            {
                bool haveSame = false;
                int newMod = 0;
                foreach (ActRemindVo n in newList)
                {
                    newMod = n.Module;
                    if (o.Module == n.Module) { haveSame = true; break; }
                }
                if (haveSame) continue;
                return (true, newMod);
            }
            return (false, 0);
        }

        // =====================================================================================
        // 41900/41903/41904:资源找回
        // =====================================================================================

        public sealed class ResFindVo { public int ActId; public int ActSub; public int Lefttimes; public int LefttimesVip; public int RewardLv; }

        public List<ResFindVo> ResFindList { get; private set; } = new List<ResFindVo>();

        public void SetResFindData(List<ResFindVo> list) => ResFindList = list ?? new List<ResFindVo>();

        /// <summary>41903 成功后只更新对应条目(对标 UpdateFindData:act_id+act_sub+reward_lv 三键匹配)。</summary>
        public void UpdateResFind(int actId, int actSub, int rewardLv, int lefttimes, int lefttimesVip)
        {
            foreach (ResFindVo vo in ResFindList)
            {
                if (vo.ActId == actId && vo.ActSub == actSub && vo.RewardLv == rewardLv)
                {
                    vo.Lefttimes = lefttimes;
                    vo.LefttimesVip = lefttimesVip;
                    return;
                }
            }
        }

        public ResFindVo GetResFind(int actId, int actSub, int rewardLv)
        {
            foreach (ResFindVo vo in ResFindList)
                if (vo.ActId == actId && vo.ActSub == actSub && vo.RewardLv == rewardLv) return vo;
            return null;
        }

        // =====================================================================================
        // 61801:我要变强
        // =====================================================================================

        public sealed class StrongStateVo { public int Id; public int State; public long Time; }

        public List<StrongStateVo> StrongStateList { get; private set; } = new List<StrongStateVo>();
        public bool HasStrongData { get; private set; }

        public void SetStrongerData(List<StrongStateVo> list)
        {
            StrongStateList = list ?? new List<StrongStateVo>();
            HasStrongData = true;
        }

        public StrongStateVo GetStrongerById(int id)
        {
            foreach (StrongStateVo vo in StrongStateList)
                if (vo.Id == id) return vo;
            return null;
        }

        // =====================================================================================
        // 综合红点(简化版,对标老端 ShowRedDot——托管/无尽之海/离线挂机小时数等跨模块依赖未接线,TODO)
        // =====================================================================================

        public bool ComputeRedDot()
        {
            bool livenessCanGet = false;
            if (_dailyData.TryGetValue(ACT_UNLIMIT, out DailyDataVo data))
                foreach (ActivityVo vo in data.AcList)
                    if (vo.CanGetLive > 0) { livenessCanGet = true; break; }

            bool rewardCanGet = false;
            foreach (LivenessRewardVo vo in LivenessRewardList)
                if (vo.State == 1) { rewardCanGet = true; break; }

            bool resCanFind = false;
            foreach (ResFindVo vo in ResFindList)
                if (vo.Lefttimes + vo.LefttimesVip > 0) { resCanFind = true; break; }

            return livenessCanGet || rewardCanGet || resCanFind || DailyResRed > 0 || DailyTaskFirstRed;
        }

        public void Clear()
        {
            _dailyData.Clear();
            _reservation.Clear();
            _join.Clear();
            DailyResRed = 0;
            LivenessRewardList = new List<LivenessRewardVo>();
            LivenessLive = 0;
            LivenessLiveMax = 0;
            HasLivenessImg = false;
            OutlineTime = 0;
            HuoYueDuList = new List<HuoYueDuVo>();
            IsRemind = true;
            ActTipList = new List<ActRemindVo>();
            ResFindList = new List<ResFindVo>();
            StrongStateList = new List<StrongStateVo>();
            HasStrongData = false;
            DailyTaskFirstRed = false;
            _dailyTaskFirstChecked = false;
        }
    }
}
