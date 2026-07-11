using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.FriendInvite
{
    /// <summary>
    /// 好友邀请(分享)控制器(对标老客户端 FriendInviteController,协议段 34000~34012)。进游戏请求 34001;
    /// 据「分享开关」(FriendInviteModel.CheckIconOpenState)增删主界面图标 340。
    ///
    /// 图标条件对标老端 init_fun:CheckIconOpenState() 为真时 addIcon(340)——其内部再走 open_lv(30)/开服天/
    /// 审核隐藏 的配置门。本端 AddIconAsync 的 FunIsOpenByIconType 即等价的配置门,故用 AddIconAsync(而非
    /// AddOwnerIcon)。注:340 配置 controll_by_own_fun=true,默认图标扫描(RefreshDefaultIconsCoreAsync)会跳过它,
    /// 由本控制器独家管理,不会双管。等级变化(EVT_ROLE_INFO_UPDATE)时复请求 34001(对标老端 CHANGE_LEVEL→
    /// LevelChange→init_fun),让升到 30 级且分享开启后图标及时出现。
    ///
    /// 本期只做图标,红点/助力/红包/福利/微信分享(34002~34012、11301/11302)与面板待用户验收。
    /// </summary>
    public sealed class FriendInviteController : BaseController
    {
        public static readonly FriendInviteController Instance = new FriendInviteController();
        private FriendInviteController() { }

        public const string ICON_TYPE = FriendInviteModel.ICON_TYPE;

        // 复请求 34001 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.FRIENDINVITE_INFO, On34001);
            // 对标老端 CHANGE_LEVEL→LevelChange→init_fun:等级变化时复请求(30级 + 分享开启后显示图标)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            FriendInviteModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 init_fun 发 34001)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.FRIENDINVITE_INFO);
        }

        // 34001: get_status:c, recover_time:i, daily_count:c, total_count:i, reward_list[u16×{reward_id:c, status:c}]
        private void On34001(NetReader r)
        {
            int getStatus = r.ReadU8();
            int recoverTime = (int)r.ReadU32();
            int dailyCount = r.ReadU8();
            int totalCount = (int)r.ReadU32();
            int rewardCount = r.ReadU16();
            for (int i = 0; i < rewardCount; i++)
            {
                r.ReadU8(); // reward_id
                r.ReadU8(); // status(奖励领取态,面板/红点用,本期不存)
            }

            FriendInviteModel m = FriendInviteModel.Instance;
            m.SetInfo(getStatus, recoverTime, dailyCount, totalCount);

            // 图标显隐取分享开关(CheckIconOpenState),等级/开服天/审核门由 AddIconAsync 配置门叠加把控。
            if (m.CheckIconOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("FriendInvite", "34001 好友邀请: get_status={0} daily={1} total={2} shareOpen={3}",
                getStatus, dailyCount, totalCount, m.CheckIconOpenState());
        }

        // 对标老端:主角等级变化复请求 34001(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
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
