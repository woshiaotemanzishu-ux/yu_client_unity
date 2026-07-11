using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.DiamondFight
{
    /// <summary>
    /// 灵玉(勾玉)大战控制器(对标老客户端 DiamondFightController,模块 137)。进游戏请求 13700 拿活动状态;
    /// 回包据 war_state 增删主界面图标 137(loc6→Secondary 副活动位):war_state 1..4 且未报名显示,否则删。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 13700(对标老端 CHANGE_LEVEL==OPEN_LEVEL→request_data),
    /// 让升到开启等级后图标及时出现。
    /// 本期只做图标:老端 request_data 里的 13703/13716/13721 及面板/报名/竞猜/战斗协议(13701-13724)
    /// 一律不注册、不移植,均待用户验收。
    /// </summary>
    public sealed class DiamondFightController : BaseController
    {
        public static readonly DiamondFightController Instance = new DiamondFightController();
        private DiamondFightController() { }

        public const string ICON_TYPE = DiamondFightModel.ICON_TYPE;

        // 复请求 13700 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.DIAMONDFIGHT_INFO, On13700);
            // 对标老端 mainRoleInfo.CHANGE_LEVEL→request_data:等级变化时复请求(达到开启等级后拿 war_state)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            DiamondFightModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 13700)。</summary>
        public void RequestStartup()
        {
            // read(13700,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.DIAMONDFIGHT_INFO);
        }

        // 13700: war_state:c, end_time:i(活动阶段状态,唯一驱动图标 137 的协议)。
        private void On13700(NetReader r)
        {
            int warState = r.ReadU8();
            int endTime = (int)r.ReadU32();

            DiamondFightModel m = DiamondFightModel.Instance;
            m.SetStageInfo(warState, endTime);

            // war_state 1..4 且未报名显示,否则删(对标老端 13700 里 addIcon/deleteIcon 分支)。
            if (m.GetIconOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, m.GetIconText());
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("DiamondFight", "13700 灵玉大战: war_state={0} end_time={1} open={2}",
                warState, endTime, m.GetIconOpenState());
        }

        // 对标老端:主角等级变化复请求 13700(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
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
