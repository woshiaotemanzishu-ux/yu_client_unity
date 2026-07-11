using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.TopVip
{
    /// <summary>
    /// 至尊VIP(TopVip)控制器(对标老客户端 TopVipController,模块 451)。进游戏请求 45101 拉至尊vip信息;
    /// 主界面图标(451)门槛不看回包,而是看主角 vip_flag/level(对标老端 vip_flag 闭包:绑 GAME_START/
    /// CHANGE_LEVEL/vip_flag 变化 → vip_flag>=4 且 level>=160 才 addIcon(451))——故等级/vip_flag 任一
    /// 变化(EVT_ROLE_INFO_UPDATE)时复判图标并复请求 45101。老端 addIcon(451) 走配置门(open_lv 等),
    /// 故用 AddIconAsync(内含 FunIsOpenByIconType 配置门),叠加本控制器的 vip_flag/level 门。
    /// 注意:同模块的 45120(SVIP 活动,图标 "45120")已由 SvipController 单独承载,与本图标 451 互不重叠。
    /// 本期只做图标,45102/45103/45104 等技能/任务/商店协议与面板待用户验收。
    /// </summary>
    public sealed class TopVipController : BaseController
    {
        public static readonly TopVipController Instance = new TopVipController();
        private TopVipController() { }

        public const string ICON_TYPE = TopVipModel.ICON_TYPE;

        // 复判/复请求去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级或 vip_flag 真变时处理。
        private int _lastLevel = -1;
        private int _lastVipFlag = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.TOPVIP_INFO, On45101);
            // 对标老端 vip_flag 闭包绑 CHANGE_LEVEL/vip_flag 变化:等级或 vip_flag 变化时复判图标(160级+vip4 开启)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            TopVipModel.Instance.Reset();
            _lastLevel = -1;
            _lastVipFlag = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 延迟发 45101)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.TOPVIP_INFO);
        }

        // 45101: supvip_type:c, supvip_time:i, right_list[u16×{right_type:c, data_str:s, utime:i}], charge_day:c, today_gold:i, is_free_protect:c
        private void On45101(NetReader r)
        {
            int supvipType = r.ReadU8();
            int supvipTime = (int)r.ReadU32();
            int rightCount = r.ReadU16();
            for (int i = 0; i < rightCount; i++)
            {
                r.ReadU8();     // right_type(权益类型,面板用,本期不存)
                r.ReadString(); // data_str(权益内容 json)
                r.ReadU32();    // utime(权益到期时间)
            }
            int chargeDay = r.ReadU8();
            int todayGold = (int)r.ReadU32();
            int isFreeProtect = r.ReadU8();

            TopVipModel.Instance.SetInfo(supvipType, supvipTime, chargeDay, todayGold, isFreeProtect);
            // 图标门槛看主角 vip_flag/level(与本回包无关),回包到后一并复判保持同步。
            RefreshIcon();

            GameLog.Info("TopVip", "45101 至尊vip: type={0} time={1} chargeDay={2} open={3}",
                supvipType, supvipTime, chargeDay, IsEntranceOpen());
        }

        /// <summary>据主角 vip_flag/level 复判图标(对标老端 vip_flag 闭包:满足即 addIcon(451),配置门交给 AddIconAsync)。</summary>
        private void RefreshIcon()
        {
            if (IsEntranceOpen()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        private static bool IsEntranceOpen()
        {
            RoleModel role = RoleModel.Instance;
            int level = role.HasBaseInfo ? role.Level : 0;
            return TopVipModel.Instance.GetEntranceOpenState(GetVipFlag(), level);
        }

        // 对标老端:主角 vip_flag/等级变化 → 复判图标 + 复请求 45101(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在真变时处理)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            int vipFlag = GetVipFlag();
            if (role.Level == _lastLevel && vipFlag == _lastVipFlag) return;
            _lastLevel = role.Level;
            _lastVipFlag = vipFlag;
            RefreshIcon();
            RequestStartup();
        }

        // 主角 vip_flag(至尊vip门槛之一;来自 13001 figure 块 Raw["vip_flag"],对标老端 mainRoleVo.vip_flag)。
        private static int GetVipFlag()
        {
            var fig = RoleModel.Instance.Figure;
            if (fig == null) return 0;
            return fig.Raw.TryGetValue("vip_flag", out object v) ? System.Convert.ToInt32(v) : 0;
        }
    }
}
