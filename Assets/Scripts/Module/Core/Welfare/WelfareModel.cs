using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Welfare
{
    /// <summary>
    /// 福利余量(Welfare)数据(对标老客户端 commonModel/WelfareModel.ts)。承载签到(41703)/静默下载(41707)/
    /// 在线福利(41715)/心悦礼包(41719)四段状态。战力福利(41723/41724)老端挂在独立 GrowthForceModel,
    /// Unity 侧数据直接落在 <see cref="Welfare.CombatWelfareController"/> 内部字段,不进本类。
    /// </summary>
    public sealed class WelfareModel
    {
        public static readonly WelfareModel Instance = new WelfareModel();
        private WelfareModel() { }

        // ---- 签到(41703/41704/41705,pt_417.erl:16-24,105-184) ----

        /// <summary>total_state[i] 对标老端 scmd.total_state[i]:{sum:32,receive:8}(阶段性总天数档)。</summary>
        public readonly struct TotalStateEntry
        {
            public readonly long Sum;
            public readonly int Receive;
            public TotalStateEntry(long sum, int receive) { Sum = sum; Receive = receive; }
        }

        /// <summary>acc_state[i] 对标老端 scmd.acc_state[i]:{check_day:8,receive:8}(逐天签到档)。</summary>
        public readonly struct AccStateEntry
        {
            public readonly int CheckDay;
            public readonly int Receive;
            public AccStateEntry(int checkDay, int receive) { CheckDay = checkDay; Receive = receive; }
        }

        public bool HasCheckinInfo { get; private set; }
        public int CheckinTotalDays { get; private set; }
        public int CheckinTotalType { get; private set; }
        public IReadOnlyList<TotalStateEntry> CheckinTotalState => _checkinTotalState;
        public IReadOnlyList<AccStateEntry> CheckinAccState => _checkinAccState;
        public int CheckinCheckType { get; private set; }
        public int CheckinRetroTimes { get; private set; }
        public int CheckinDaysFresh { get; private set; }
        public int CheckinRemainTimes { get; private set; }
        public int CheckinCheckDay { get; private set; }

        private readonly List<TotalStateEntry> _checkinTotalState = new List<TotalStateEntry>();
        private readonly List<AccStateEntry> _checkinAccState = new List<AccStateEntry>();

        /// <summary>今日是否已签到(对标老端 WelfareModel.TodayHadSign:check_day&gt;0)。</summary>
        public bool TodayHadSign => HasCheckinInfo && CheckinCheckDay > 0;

        public void SetCheckinInfo(int totalDays, int totalType, List<TotalStateEntry> totalState,
            List<AccStateEntry> accState, int checkType, int retroTimes, int daysFresh, int remainTimes, int checkDay)
        {
            CheckinTotalDays = totalDays;
            CheckinTotalType = totalType;
            _checkinTotalState.Clear();
            if (totalState != null) _checkinTotalState.AddRange(totalState);
            _checkinAccState.Clear();
            if (accState != null) _checkinAccState.AddRange(accState);
            CheckinCheckType = checkType;
            CheckinRetroTimes = retroTimes;
            CheckinDaysFresh = daysFresh;
            CheckinRemainTimes = remainTimes;
            CheckinCheckDay = checkDay;
            HasCheckinInfo = true;
        }

        // ---- 静默下载(41707/41708,pt_417.erl:28-31,194-212) ----

        public bool HasDownloadInfo { get; private set; }

        /// <summary>对标老端 res_gift_reward_state:41707 回包 Code 原样落地;41708 领取成功后本地置 2
        /// (对标 UpdateResourceGiftRewardState({code:2}))。</summary>
        public int DownloadCode { get; private set; }

        /// <summary>41707 服务端下发的奖励明细(pt_417.erl:194-204 write(41707,[Code,Rewads]),Rewads 走标准
        /// write_object_list)。pp_welfare.erl:291-301 查询分支与 :322-338 领取分支读的是同一份
        /// data_key_value:get(?KEY_DOWNLOAD_GIFT),即这份明细与实际发奖同源、不会漂移;41708 领取回包
        /// 只回裸 Code(pt_417.erl:206-212),明细优先取这里,见 WelfareController.On41708。</summary>
        public IReadOnlyList<(int type, int typeId, int num)> DownloadRewards => _downloadRewards;
        private readonly List<(int type, int typeId, int num)> _downloadRewards = new List<(int, int, int)>();

        public void SetDownloadState(int code)
        {
            DownloadCode = code;
            HasDownloadInfo = true;
        }

        /// <summary>41707 专用:落地 Code + 服务端同源奖励明细(见 <see cref="DownloadRewards"/>)。</summary>
        public void SetDownloadInfo(int code, List<(int type, int typeId, int num)> rewards)
        {
            DownloadCode = code;
            _downloadRewards.Clear();
            if (rewards != null) _downloadRewards.AddRange(rewards);
            HasDownloadInfo = true;
        }

        // ---- 在线福利(41715/41716,pt_417.erl:42-46,266-300) ----

        public readonly struct OnlineEntry
        {
            public readonly int Id;
            public readonly int State;
            public OnlineEntry(int id, int state) { Id = id; State = state; }
        }

        public bool HasOnlineInfo { get; private set; }
        public int OnlineTime { get; private set; }
        public long OnlineLoginTime { get; private set; }
        public long OnlineObservedAt { get; private set; }
        public IReadOnlyList<OnlineEntry> OnlineList => _onlineList;
        private readonly List<OnlineEntry> _onlineList = new List<OnlineEntry>();

        /// <summary>
        /// 41715 的 time 是回包时已经累计的在线秒数；老端在回包后继续用服务器时钟推进显示和红点。
        /// login_time 仅保留协议原值，不能代替本次回包的观测时刻。
        /// </summary>
        public int CurrentOnlineTime
        {
            get
            {
                if (!HasOnlineInfo) return 0;
                long elapsed = OnlineObservedAt > 0 ? System.Math.Max(0L, TimeUtil.NowSec() - OnlineObservedAt) : 0L;
                long current = (long)OnlineTime + elapsed;
                return current >= int.MaxValue ? int.MaxValue : (int)current;
            }
        }

        public void SetOnlineInfo(int time, long loginTime, List<OnlineEntry> list)
        {
            OnlineTime = time;
            OnlineLoginTime = loginTime;
            OnlineObservedAt = TimeUtil.NowSec();
            _onlineList.Clear();
            if (list != null) _onlineList.AddRange(list);
            HasOnlineInfo = true;
        }

        /// <summary>
        /// 返回下一个尚未领取档位变成可领取所需的秒数；0 表示已经可领，-1 表示没有待领取档位。
        /// </summary>
        public int GetNextOnlineRewardDelaySeconds()
        {
            int current = CurrentOnlineTime;
            int nearest = int.MaxValue;
            for (int i = 0; i < _onlineList.Count; i++)
            {
                OnlineEntry item = _onlineList[i];
                if (item.State != 0) continue;
                int target = WelfareConfigs.GetOnlineRewardTime(item.Id);
                if (target == int.MaxValue) continue;
                if (target <= current) return 0;
                if (target < nearest) nearest = target;
            }
            return nearest == int.MaxValue ? -1 : nearest - current;
        }

        // ---- 心悦礼包(41719,pt_417.erl:52-54,326-340) ----

        public bool HasXinyueInfo { get; private set; }
        public int XinyueOpr { get; private set; }
        public int XinyueGiftSt { get; private set; }

        public void SetXinyueState(int opr, int giftSt)
        {
            XinyueOpr = opr;
            XinyueGiftSt = giftSt;
            HasXinyueInfo = true;
        }

        /// <summary>签到或在线福利存在服务端已达成、尚未领取的奖励时点亮福利入口。</summary>
        public bool HasEntranceRedDot()
        {
            for (int i = 0; i < _checkinAccState.Count; i++)
            {
                int receive = _checkinAccState[i].Receive;
                if (receive == 3 || receive == 4) return true;
            }
            for (int i = 0; i < _checkinTotalState.Count; i++)
            {
                if (_checkinTotalState[i].Receive == 3) return true;
            }
            int currentOnlineTime = CurrentOnlineTime;
            for (int i = 0; i < _onlineList.Count; i++)
            {
                OnlineEntry item = _onlineList[i];
                if (item.State == 0 && currentOnlineTime >= WelfareConfigs.GetOnlineRewardTime(item.Id)) return true;
            }
            return false;
        }

        /// <summary>断线/登出清空(对标老端 GAME_START 时 welfareModel.Reset())。</summary>
        public void Reset()
        {
            HasCheckinInfo = false;
            CheckinTotalDays = 0;
            CheckinTotalType = 0;
            _checkinTotalState.Clear();
            _checkinAccState.Clear();
            CheckinCheckType = 0;
            CheckinRetroTimes = 0;
            CheckinDaysFresh = 0;
            CheckinRemainTimes = 0;
            CheckinCheckDay = 0;

            HasDownloadInfo = false;
            DownloadCode = 0;
            _downloadRewards.Clear();

            HasOnlineInfo = false;
            OnlineTime = 0;
            OnlineLoginTime = 0;
            OnlineObservedAt = 0;
            _onlineList.Clear();

            HasXinyueInfo = false;
            XinyueOpr = 0;
            XinyueGiftSt = 0;
        }
    }

    /// <summary>
    /// Welfare 配置读取器(自动循环 轮18 PK4;13 张表,同 DailyConfigs/RushGiftConfigs 套路)。
    /// 表由 ClientConfigSync 从 yu_client cdn/resource/config/server/ 同步进
    /// Assets/GameRes/resource/config/server/(P0 已搬运)。
    /// </summary>
    public static class WelfareConfigs
    {
        private static JObject _welfareCfg;
        private static JObject _onlineReward;
        private static JObject _checkinType;
        private static JObject _checkinDailyRewards;
        private static JObject _checkinTotalRewards;
        private static JObject _checkinDailyRetroactive;
        private static JObject _checkinKeyValue;
        private static JObject _welfareNightReward;
        private static JObject _growWelfareInfo;
        private static JObject _combatWelfareReward;
        private static JObject _combatWelfareTimes;
        private static JObject _xinyueGift;
        private static JObject _realInfoReward;

        public static bool IsLoaded => _welfareCfg != null;

        public static int WelfareCfgCount => _welfareCfg?.Count ?? 0;
        public static int OnlineRewardCount => _onlineReward?.Count ?? 0;
        public static int CheckinTypeCount => _checkinType?.Count ?? 0;
        public static int CheckinDailyRewardsCount => _checkinDailyRewards?.Count ?? 0;
        public static int CheckinTotalRewardsCount => _checkinTotalRewards?.Count ?? 0;
        public static int CheckinDailyRetroactiveCount => _checkinDailyRetroactive?.Count ?? 0;
        public static int CheckinKeyValueCount => _checkinKeyValue?.Count ?? 0;
        public static int WelfareNightRewardCount => _welfareNightReward?.Count ?? 0;
        public static int GrowWelfareInfoCount => _growWelfareInfo?.Count ?? 0;
        public static int CombatWelfareRewardCount => _combatWelfareReward?.Count ?? 0;
        public static int CombatWelfareTimesCount => _combatWelfareTimes?.Count ?? 0;
        public static int XinyueGiftCount => _xinyueGift?.Count ?? 0;
        public static int RealInfoRewardCount => _realInfoReward?.Count ?? 0;

        public static async Task EnsureLoaded()
        {
            if (_welfareCfg != null) return;
            _welfareCfg = await Load("config_welfare_cfg");
            _onlineReward = await Load("config_online_reward");
            _checkinType = await Load("config_checkin_type");
            _checkinDailyRewards = await Load("config_checkin_daily_rewards");
            _checkinTotalRewards = await Load("config_checkin_total_rewards");
            _checkinDailyRetroactive = await Load("config_checkin_daily_retroactive");
            _checkinKeyValue = await Load("config_checkin_key_value");
            _welfareNightReward = await Load("config_welfare_night_reward");
            _growWelfareInfo = await Load("config_grow_welfare_info");
            _combatWelfareReward = await Load("config_combat_welfare_reward");
            _combatWelfareTimes = await Load("config_combat_welfare_times");
            _xinyueGift = await Load("config_xinyue_gift");
            _realInfoReward = await Load("config_real_info_reward");
            GameLog.Info("Welfare",
                "WelfareConfigs 加载: cfg={0} online={1} checkin(type/day/total/retro/kv)={2}/{3}/{4}/{5}/{6} night={7} grow={8} combatReward={9} combatTimes={10} xinyue={11} realInfo={12}",
                _welfareCfg.Count, _onlineReward.Count, _checkinType.Count, _checkinDailyRewards.Count, _checkinTotalRewards.Count,
                _checkinDailyRetroactive.Count, _checkinKeyValue.Count, _welfareNightReward.Count, _growWelfareInfo.Count,
                _combatWelfareReward.Count, _combatWelfareTimes.Count, _xinyueGift.Count, _realInfoReward.Count);
        }

        private static async Task<JObject> Load(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Welfare", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>config_welfare_cfg[id].val(对标老端 WelfareModel.GetKvCfg/GrowthForceModel 直读同表):
        /// 1=夜间福利开启等级/3=在线福利开启等级/4=新手奖励(死配置,GetWelfareWelcomeOpenState 恒 false 不消费)/
        /// 6=战力福利开启等级/8=成长福利开启等级(既有 GrowthBenefitsModel.OPEN_LEVEL=135 已落定同值)/
        /// 9=战力福利开启创角天数。</summary>
        public static string GetKv(int id)
        {
            return _welfareCfg?[id.ToString(CultureInfo.InvariantCulture)] is JObject obj ? obj.Value<string>("val") : null;
        }

        public static int GetKvInt(int id, int fallback)
        {
            string v = GetKv(id);
            return int.TryParse(v, out int n) ? n : fallback;
        }

        public static int GetOnlineRewardTime(int id)
        {
            return _onlineReward?[id.ToString(CultureInfo.InvariantCulture)] is JObject obj
                ? obj.Value<int?>("online_time") ?? int.MaxValue
                : int.MaxValue;
        }
    }
}
