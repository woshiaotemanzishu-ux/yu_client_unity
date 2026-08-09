using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.SevenDay
{
    /// <summary>
    /// 七天/合服七天登录控制器(对标老客户端 SevenDayController)。进游戏请求 17500(七天登录)与
    /// 17502(合服七天);回包据每个 act_type 的可领档增删主界面三个图标 175 / 175@8 / 175_1
    /// (对标老端 frist_open_view:GetOpenType(act)==icon 对应 key 则加,否则删)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)时复请求 17500/17502(对标老端 CHANGE_LEVEL→LevelChange,
    /// 让等级到 open_lv 后图标及时出现),用 _lastLevel 去抖(该事件亦随经验/货币触发)。
    /// 当前只做图标；17501/17503 会真实发奖并持久化领取态，精确日档/配置/页面/奖励链未完整迁移前保持无常量、无注册、无发送。
    /// </summary>
    public sealed class SevenDayController : BaseController
    {
        public static readonly SevenDayController Instance = new SevenDayController();
        private SevenDayController() { }

        // 复请求的等级去抖(EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发)。
        private int _lastLevel = -1;
        private int _lastObservedFinishTaskId = -1;
        private bool _taskGateRefreshInFlight;
        private bool _taskGatePending;
        private int _taskGatePendingFrom;
        private int _taskGatePendingTo;
        private int _lifecycleVersion;
#if UNITY_EDITOR
        private static System.Func<byte[], bool> s_taskGateOutboundIntercept;
        private static System.Func<Task> s_taskGateEnsureLoadedOverride;
#endif

        protected override void Register()
        {
            RegisterProtocal(Proto.SEVENDAY_OPEN_INFO, On17500);
            RegisterProtocal(Proto.SEVENDAY_MERGE_INFO, On17502);
            // 对标老端 CHANGE_LEVEL→LevelChange:等级到 open_lv 时复请求 17500/17502。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
            _lastObservedFinishTaskId = TaskModel.Instance.NewestFinishTaskId;
            // 对标老端 SevenDayController.ts:44:game_start(=RequestStartup 同款,发17500/17502)同时绑
            // GAME_START 与 DAY_CHANGE 两个事件——跨天后复请求(七天/合服七天面板按 current_day 换页)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
            _lifecycleVersion++;
            _taskGateRefreshInFlight = false;
            _taskGatePending = false;
            _taskGatePendingFrom = 0;
            _taskGatePendingTo = 0;
            ClearIcon(SevenDayModel.ICON_OPEN);
            ClearIcon(SevenDayModel.ICON_EIGHT);
            ClearIcon(SevenDayModel.ICON_MERGE);
            SevenDayModel.Instance.Reset();
            _lastLevel = -1;
            _lastObservedFinishTaskId = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 17500/17502)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.SEVENDAY_OPEN_INFO);   // 17500 七天登录(read 无参)
            SendFmt(Proto.SEVENDAY_MERGE_INFO);  // 17502 合服七天(read 无参)
        }

        // 17500: errcode:i, current_day:c, reward_status[u16×{day_id:h, status:h}]
        private void On17500(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int currentDay = r.ReadU8();
            int count = r.ReadU16();
            var dayIds = new List<int>(count);
            var statuses = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                dayIds.Add(r.ReadU16());
                statuses.Add(r.ReadU16());
            }
            if (errcode != 1) return; // 对标老端:errcode!=1 不更新(活动未开/异常)

            SevenDayModel.Instance.SetInfo(SevenDayModel.ACT_OPEN, currentDay, dayIds, statuses);
            RefreshIcons();
            GameLog.Info("SevenDay", "17500 七天登录: current_day={0} count={1}", currentDay, count);
        }

        // 17502: errcode:i, current_day:c, merge_wlv:h, reward_status[u16×{day_id:h, status:h}]
        private void On17502(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int currentDay = r.ReadU8();
            int mergeWlv = r.ReadU16(); // 合服世界等级(面板用,本期不存)
            int count = r.ReadU16();
            var dayIds = new List<int>(count);
            var statuses = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                dayIds.Add(r.ReadU16());
                statuses.Add(r.ReadU16());
            }
            if (errcode != 1) return;

            SevenDayModel.Instance.SetInfo(SevenDayModel.ACT_MERGE, currentDay, dayIds, statuses);
            RefreshIcons();
            GameLog.Info("SevenDay", "17502 合服七天: current_day={0} merge_wlv={1} count={2}", currentDay, mergeWlv, count);
        }

        // 对标老端 frist_open_view:三个图标各据自身 openType 增删(幂等,任一回包后全量刷新)。
        private void RefreshIcons()
        {
            RefreshIcon(SevenDayModel.ICON_OPEN);
            RefreshIcon(SevenDayModel.ICON_EIGHT);
            RefreshIcon(SevenDayModel.ICON_MERGE);
        }

        private void RefreshIcon(string iconType)
        {
            SevenDayModel m = SevenDayModel.Instance;
            // 老端用普通 addIcon(经 FunIsOpenByIconType 配置闸)→ 这里用 AddIconAsync(同样过闸)。
            if (m.IsIconOpen(iconType))
            {
                // SetIconRedDot 会先缓存红点，即使 AddIconAsync 尚在等待配置，图标创建时也能带上正确状态。
                ActivityIconManager.Instance.SetIconRedDot(iconType, m.IsIconRed(iconType));
                _ = ActivityIconManager.Instance.AddIconAsync(iconType, 0, m.GetIconText(iconType));
            }
            else ClearIcon(iconType);
        }

        private static void ClearIcon(string iconType)
        {
            // DeleteIcon 不会清 ActivityIconManager 的红点缓存，必须先归零，避免下次重建沿用旧状态。
            ActivityIconManager.Instance.SetIconRedDot(iconType, false);
            ActivityIconManager.Instance.DeleteIcon(iconType);
        }

        // 对标老端:主角等级变化复请求 17500/17502(只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        private void OnTaskListUpdated()
        {
            int newestFinishTaskId = TaskModel.Instance.NewestFinishTaskId;
            int previousTaskId = _lastObservedFinishTaskId;
            _lastObservedFinishTaskId = newestFinishTaskId;
            if (newestFinishTaskId <= previousTaskId) return;

            if (!_taskGatePending)
            {
                _taskGatePending = true;
                _taskGatePendingFrom = previousTaskId;
                _taskGatePendingTo = newestFinishTaskId;
            }
            else if (newestFinishTaskId > _taskGatePendingTo)
            {
                _taskGatePendingTo = newestFinishTaskId;
            }

            if (!_taskGateRefreshInFlight)
            {
                _taskGateRefreshInFlight = true;
                _ = RefreshAfterTaskGateAsync(_lifecycleVersion);
            }
        }

        private async Task RefreshAfterTaskGateAsync(int lifecycleVersion)
        {
            try
            {
                while (_taskGatePending && lifecycleVersion == _lifecycleVersion && IsInitialized)
                {
                    int pendingFrom = _taskGatePendingFrom;
                    int pendingTo = _taskGatePendingTo;
#if UNITY_EDITOR
                    await (s_taskGateEnsureLoadedOverride == null
                        ? MainUIConfigs.EnsureLoaded()
                        : s_taskGateEnsureLoadedOverride());
#else
                    await MainUIConfigs.EnsureLoaded();
#endif
                    if (lifecycleVersion != _lifecycleVersion || !IsInitialized) return;

                    MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(SevenDayModel.ICON_OPEN);
                    if (cfg != null && cfg.OpenTaskId > 0 && pendingFrom < cfg.OpenTaskId && pendingTo >= cfg.OpenTaskId)
                        SendTaskGateRefresh();

                    if (_taskGatePendingFrom == pendingFrom && _taskGatePendingTo == pendingTo)
                        _taskGatePending = false;
                    else
                    {
                        // 本轮已消费到 capture 的 to；后续推进只能从该游标继续，不能再次从旧 from 重算跨门槛。
                        _taskGatePendingFrom = pendingTo;
                    }
                }
            }
            finally
            {
                if (lifecycleVersion == _lifecycleVersion)
                    _taskGateRefreshInFlight = false;
            }
        }

        private void SendTaskGateRefresh()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.SEVENDAY_OPEN_INFO, null, null);
            if (s_taskGateOutboundIntercept != null && s_taskGateOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.SEVENDAY_OPEN_INFO);
        }
    }
}
