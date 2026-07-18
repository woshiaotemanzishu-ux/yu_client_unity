using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.SevenDay
{
    /// <summary>
    /// 七天/合服七天登录控制器(对标老客户端 SevenDayController)。进游戏请求 17500(七天登录)与
    /// 17502(合服七天);回包据每个 act_type 的可领档增删主界面三个图标 175 / 175@8 / 175_1
    /// (对标老端 frist_open_view:GetOpenType(act)==icon 对应 key 则加,否则删)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)时复请求 17500/17502(对标老端 CHANGE_LEVEL→LevelChange,
    /// 让等级到 open_lv 后图标及时出现),用 _lastLevel 去抖(该事件亦随经验/货币触发)。
    /// 本期只做图标,面板/领奖(17501/17503)待用户验收。
    /// </summary>
    public sealed class SevenDayController : BaseController
    {
        public static readonly SevenDayController Instance = new SevenDayController();
        private SevenDayController() { }

        // 复请求的等级去抖(EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发)。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.SEVENDAY_OPEN_INFO, On17500);
            RegisterProtocal(Proto.SEVENDAY_MERGE_INFO, On17502);
            // 对标老端 CHANGE_LEVEL→LevelChange:等级到 open_lv 时复请求 17500/17502。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 SevenDayController.ts:44:game_start(=RequestStartup 同款,发17500/17502)同时绑
            // GAME_START 与 DAY_CHANGE 两个事件——跨天后复请求(七天/合服七天面板按 current_day 换页)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
            ActivityIconManager.Instance.DeleteIcon(SevenDayModel.ICON_OPEN);
            ActivityIconManager.Instance.DeleteIcon(SevenDayModel.ICON_EIGHT);
            ActivityIconManager.Instance.DeleteIcon(SevenDayModel.ICON_MERGE);
            SevenDayModel.Instance.Reset();
            _lastLevel = -1;
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
            if (m.IsIconOpen(iconType)) _ = ActivityIconManager.Instance.AddIconAsync(iconType, 0, m.GetIconText(iconType));
            else ActivityIconManager.Instance.DeleteIcon(iconType);
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
    }
}
