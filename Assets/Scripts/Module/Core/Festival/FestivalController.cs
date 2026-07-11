using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Festival
{
    /// <summary>
    /// 祭典(宝录)控制器(对标老客户端 FestivalController,模块 194)。进游戏请求 19401;
    /// 回包据 uid 增删主界面图标 223(GetEntranceOpenState:uid>0 显示)。点击图标经 MainUIRouter
    /// 路由 "223" 打开宝录面板(面板未移植 → 落占位弹层)。等级变化(EVT_ROLE_INFO_UPDATE)时复请求
    /// 19401(对标老端 CHANGE_LEVEL→发 19401),让升到 120 级开启宝录后图标及时出现。
    /// 本期只做图标,任务/奖励/购买等协议(19400/19402-19405)与面板待用户验收。
    /// </summary>
    public sealed class FestivalController : BaseController
    {
        public static readonly FestivalController Instance = new FestivalController();
        private FestivalController() { }

        public const string ICON_TYPE = FestivalModel.ICON_TYPE;

        // 复请求 19401 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.FESTIVAL_INFO, On19401);
            // 对标老端 CHANGE_LEVEL→发 19401:等级变化时复请求(120级开启宝录)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            FestivalModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 19401)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.FESTIVAL_INFO);
        }

        // 19401: uid:h, act_id:c, type:c, lv:h, exp:i, expired_time:i, reward_list[u16×{lv:h, status1:c, status2:c}]
        private void On19401(NetReader r)
        {
            int uid = r.ReadU16();
            int actId = r.ReadU8();
            int type = r.ReadU8();
            int lv = r.ReadU16();
            int exp = (int)r.ReadU32();
            int expiredTime = (int)r.ReadU32();
            int rewardCount = r.ReadU16();
            for (int i = 0; i < rewardCount; i++)
            {
                r.ReadU16(); // reward lv
                r.ReadU8();  // status1(奖励领取态,面板用,本期不存)
                r.ReadU8();  // status2(高阶奖励领取态)
            }

            FestivalModel m = FestivalModel.Instance;
            m.SetBasicInfo(uid, actId, type, lv, exp, expiredTime);

            if (m.GetEntranceOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("Festival", "19401 宝录: uid={0} act_id={1} type={2} lv={3} open={4}",
                uid, actId, type, lv, m.GetEntranceOpenState());
        }

        // 对标老端:主角等级变化复请求 19401(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
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
