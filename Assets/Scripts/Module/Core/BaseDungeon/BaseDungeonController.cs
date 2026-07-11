using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.BaseDungeon
{
    /// <summary>
    /// 限时爬塔控制器(对标老客户端 BaseDungeonController 的限时爬塔部分)。进游戏请求 61117 拿爬塔状态;
    /// 回包据 round/over_time/reward_mode 增删主界面限时塔图标(331@97,带倒计时 over_time)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 61117(对标老端 CHANGE_LEVEL→RequestLimitTowerData),
    /// 让达到开放条件后图标及时出现。本期只做图标:副本/爬塔玩法(挑战/扫荡/领取大奖 61118)一律不移植,
    /// 面板(DungeonTowerBaseView)待用户验收。
    /// </summary>
    public sealed class BaseDungeonController : BaseController
    {
        public static readonly BaseDungeonController Instance = new BaseDungeonController();
        private BaseDungeonController() { }

        public const string TOWER_ICON_TYPE = BaseDungeonModel.TOWER_ICON_TYPE;

        // 复请求 61117 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.BASEDUNGEON_TOWER_INFO, On61117);
            // 对标老端 CHANGE_LEVEL→RequestLimitTowerData:等级变化时复请求。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(TOWER_ICON_TYPE);
            BaseDungeonModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START→RequestLimitTowerData)。</summary>
        public void RequestStartup()
        {
            // read(61117,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.BASEDUNGEON_TOWER_INFO);
        }

        // 61117: round:c, over_time:i, reward_mode:c, pass_list[u16 count × { dun_id:i }]
        private void On61117(NetReader r)
        {
            int round = r.ReadU8();
            int overTime = (int)r.ReadU32();
            int rewardMode = r.ReadU8();
            int passCount = r.ReadU16();
            for (int i = 0; i < passCount; i++)
            {
                r.ReadU32(); // pass_list dun_id(已通关关卡,爬塔玩法用,本期图标不需)
            }

            BaseDungeonModel m = BaseDungeonModel.Instance;
            m.SetTowerInfo(round, overTime, rewardMode);

            // 对标老端 AddTowerIcon:活动进行中(over_time>0 且非「已领大奖且本次未持续显示」)则挂图标(带倒计时+轮次图),否则删。
            if (m.GetTowerIconOpenState())
            {
                m.IsContinueShow = true; // 对标老端显示分支置 is_continue_show=true(领奖后本次登录仍保持显示)
                _ = ActivityIconManager.Instance.AddIconAsync(TOWER_ICON_TYPE, overTime, "", m.GetLimitTowerIcon(round));
            }
            else
            {
                ActivityIconManager.Instance.DeleteIcon(TOWER_ICON_TYPE);
            }

            GameLog.Info("BaseDungeon", "61117 限时爬塔: round={0} over_time={1} reward_mode={2} open={3}",
                round, overTime, rewardMode, m.GetTowerIconOpenState());
        }

        // 对标老端:主角等级变化复请求 61117(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
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
