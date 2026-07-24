using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Daily;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.ActivityForeshow
{
    /// <summary>
    /// 活动预告/日历控制器(对标老客户端 commonModel/ActivityForeshowManager + commonController/SnatchTreasureController)。
    /// 只做预告图标的增删、时间窗和倒计时，不做完整日历面板/提示弹窗。
    ///
    /// 图标与驱动条件:
    /// - 领地夺宝(652@31@0):唯一带服务端信号的一路。进游戏裸发 65208 请求;回包 [dun_id, end_time]
    ///   存入 Model,GetSnatchOpenState()(会话未结束)为真则 AddIconAsync(带 end_time 倒计时),否则 DeleteIcon。
    ///   对标老端 SnatchTreasureController.On65208→timeMsgData→REFRESH_FORESHOWICON→checkOpen→addIcon。
    /// - 配置型限时活动：扫描 HudActivity 第四组(location_type=10)中拥有 config_ac 时段的入口，按等级、
    ///   开服/合服日、星期、月份、指定日期和 time_region 每 15 秒统一复评；开始前 60 分钟显示开启时间，
    ///   活动中显示结束倒计时，不含逐按钮特判。
    ///
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 65208 并复评配置型图标,对标老端 CHANGE_LEVEL→SetTimer→重扫。
    /// 跨天/整点(EVT_SERVER_DAY_CHANGE / EVT_SERVER_TIME_REFRESH)只做本地复评,不发 65208:老端
    /// ActivityForeshowManager.ts:115-127 两处都只调 SetTimer()(:259-272,重算 limit_act_list + 建/撤 15s
    /// 定时器 + OnTime()),全程零发包;65208 的唯一发送点是 :505 ActivityRequestData(),被
    /// request_dic[icon_type] 一次性门守着,与跨天/整点无关。见 OnServerDayChange 注释。
    /// </summary>
    public sealed class ActivityForeshowController : BaseController
    {
        public static readonly ActivityForeshowController Instance = new ActivityForeshowController();
        private ActivityForeshowController() { }

        public const string ICON_SNATCH_TREASURE = ActivityForeshowModel.ICON_SNATCH_TREASURE;

        // 复请求 65208 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;
        private CancellationTokenSource _scheduleCts;
        private readonly HashSet<string> _scheduledIconTypes = new HashSet<string>();

        protected override void Register()
        {
            RegisterProtocal(Proto.ACTIVITYFORESHOW_SNATCH_TIME, On65208);
            // 对标老端 CHANGE_LEVEL→SetTimer 重扫:等级变化时复请求并复评图标。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 ActivityForeshowManager.ts:115-122(DAY_CHANGE)与:124-127(REFRESH_SERVER_TIME):
            // 两处都只调 SetTimer(),本端归并为同一个本地复评函数,详见 OnServerDayChange 注释。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerDayChange);
            _scheduleCts?.Cancel();
            ActivityIconManager.Instance.DeleteIcon(ICON_SNATCH_TREASURE);
            foreach (string iconType in _scheduledIconTypes)
                ActivityIconManager.Instance.DeleteIcon(iconType);
            _scheduledIconTypes.Clear();
            ActivityForeshowModel.Instance.Reset();
            _scheduleCts?.Dispose();
            _scheduleCts = null;
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用)。</summary>
        public void RequestStartup()
        {
            // read(65208,_)->{ok,[]}:领地夺宝时间信息请求无字段,裸发。
            SendFmt(Proto.ACTIVITYFORESHOW_SNATCH_TIME);
            EnsureScheduleLoop();
        }

        // 65208: dun_id:i, end_time:i(write(65208,[Dunid,EndTime]) → <<Dunid:32, EndTime:32>>)
        private void On65208(NetReader r)
        {
            int dunId = (int)r.ReadU32();
            long endTime = (long)r.ReadU32();

            ActivityForeshowModel.Instance.SetSnatchTimeMsg(dunId, endTime);
            RefreshSnatchIcon();

            GameLog.Info("ActivityForeshow", "65208 领地夺宝: dun_id={0} end_time={1} open={2}",
                dunId, endTime, ActivityForeshowModel.Instance.GetSnatchOpenState());
        }

        // 领地夺宝图标(652@31@0):会话未结束则显示(带 end_time 倒计时),否则删除。
        // 对标老端 ShowActivityIconForeshow→addIcon(icon_type, time, ...) / CloseForeshowicon→deleteIcon。
        private void RefreshSnatchIcon()
        {
            ActivityForeshowModel m = ActivityForeshowModel.Instance;
            if (m.GetSnatchOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_SNATCH_TREASURE, m.SnatchEndTime);
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_SNATCH_TREASURE);
        }

        private void EnsureScheduleLoop()
        {
            if (_scheduleCts != null) return;
            _scheduleCts = new CancellationTokenSource();
            _ = ScheduleLoopAsync(_scheduleCts.Token);
        }

        private async Task ScheduleLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await RefreshScheduledIconsAsync(token);
                    await TimeUtil.Delay(15000, token);
                }
            }
            catch (System.OperationCanceledException) { }
        }

        private async Task RefreshScheduledIconsAsync(CancellationToken token = default)
        {
            await Task.WhenAll(DailyConfigs.EnsureLoaded(), MainUIConfigs.EnsureLoaded());
            if (token.IsCancellationRequested) return;

            var configured = new HashSet<string>();
            foreach (MainUIConfigs.FunctionIconCfg cfg in MainUIConfigs.AllFunctionIconCfg())
            {
                if (token.IsCancellationRequested) return;
                if (cfg == null
                    || cfg.LocationType != ActivityIconManager.LocationType.ActivityFourth
                    || cfg.IconType == ICON_SNATCH_TREASURE
                    || !ActivityForeshowModel.Instance.HasSchedule(cfg.IconType)) continue;

                configured.Add(cfg.IconType);
                RefreshScheduledIcon(cfg.IconType);
            }

            foreach (string oldIconType in _scheduledIconTypes)
            {
                if (!configured.Contains(oldIconType)) ActivityIconManager.Instance.DeleteIcon(oldIconType);
            }
            _scheduledIconTypes.Clear();
            foreach (string iconType in configured) _scheduledIconTypes.Add(iconType);
        }

        private void RefreshScheduledIcon(string iconType)
        {
            try
            {
                ActivityForeshowModel.ScheduleDisplay display = ActivityForeshowModel.Instance.EvaluateSchedule(iconType);
                if (display.Visible)
                {
                    _ = ActivityIconManager.Instance.AddIconAsync(iconType, display.EndTime, display.Text);
                }
                else ActivityIconManager.Instance.DeleteIcon(iconType);
            }
            catch (System.Exception e)
            {
                // 单条脏配置或时间计算异常不能终止整个 15 秒统一扫描；撤下本条并继续处理其余活动。
                ActivityIconManager.Instance.DeleteIcon(iconType);
                GameLog.Error("ActivityForeshow", "活动预告计算失败 icon={0}: {1}", iconType, e.Message);
            }
        }

        // 对标老端 ActivityForeshowManager.ts:115-127:
        //   DAY_CHANGE   → 清 tip 弹窗缓存列表(本端未移植提示弹窗故无对应状态) + SetTimer()
        //   REFRESH_SERVER_TIME → SetTimer()
        // SetTimer()(:259-272)只做 limit_act_list = GetLimitActivityList() + 建/撤 15s 定时器 + OnTime(),
        // 全程零发包;OnTime()→CheckLimitActivityState→ShowActivityIconForeshow→ActivityRequestData(:500-511)
        // 才可能发 65208,但后者被 request_dic[icon_type] 一次性门守着(发过一次后 request_dic[icon_type]=true,
        // 同会话内不会再发),与跨天/整点本身无关——故跨天/整点钩子对 65208 而言是零发包的本地复评。
        // 本端复评全部已接入预告项：服务端状态按缓存值，配置型活动按 config_ac 时间窗。
        private void OnServerDayChange()
        {
            RefreshSnatchIcon();
            _ = RefreshScheduledIconsAsync(_scheduleCts?.Token ?? default);
        }

        // 对标老端:主角等级变化复请求 65208(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }
    }
}
