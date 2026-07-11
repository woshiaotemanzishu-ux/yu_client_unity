using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.ActivityForeshow
{
    /// <summary>
    /// 活动预告/日历控制器(对标老客户端 commonModel/ActivityForeshowManager + commonController/SnatchTreasureController)。
    /// 本期只做"预告图标"(loc6 通知位)的增删,不做完整日历时间窗/倒计时/提示弹窗。
    ///
    /// 图标与驱动条件:
    /// - 领地夺宝(652@31@0):唯一带服务端信号的一路。进游戏裸发 65208 请求;回包 [dun_id, end_time]
    ///   存入 Model,GetSnatchOpenState()(会话未结束)为真则 AddIconAsync(带 end_time 倒计时),否则 DeleteIcon。
    ///   对标老端 SnatchTreasureController.On65208→timeMsgData→REFRESH_FORESHOWICON→checkOpen→addIcon。
    /// - 九魂圣殿(135@0@1 / 135@0@2):老端开启态=功能开放(BattleFieldEnterView)+日历时间窗,纯客户端配置、
    ///   无服务端协议信号。本期未移植该配置,按 Model.NineSkyForeshowEnabled(默认 false)兜底:默认不显示。
    ///
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 65208 并复评九魂圣殿图标,对标老端 CHANGE_LEVEL→SetTimer→重扫。
    /// 完整日历时间窗(预告 60 分钟显示、20 分钟倒计时、进行中、结束)与提示弹窗待 XianShiActivity 配置移植后再补。
    /// </summary>
    public sealed class ActivityForeshowController : BaseController
    {
        public static readonly ActivityForeshowController Instance = new ActivityForeshowController();
        private ActivityForeshowController() { }

        public const string ICON_NINE_SKY_ONE = ActivityForeshowModel.ICON_NINE_SKY_ONE;
        public const string ICON_NINE_SKY_TWO = ActivityForeshowModel.ICON_NINE_SKY_TWO;
        public const string ICON_SNATCH_TREASURE = ActivityForeshowModel.ICON_SNATCH_TREASURE;

        // 复请求 65208 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.ACTIVITYFORESHOW_SNATCH_TIME, On65208);
            // 对标老端 CHANGE_LEVEL→SetTimer 重扫:等级变化时复请求并复评图标。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_SNATCH_TREASURE);
            ActivityIconManager.Instance.DeleteIcon(ICON_NINE_SKY_ONE);
            ActivityIconManager.Instance.DeleteIcon(ICON_NINE_SKY_TWO);
            ActivityForeshowModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用)。</summary>
        public void RequestStartup()
        {
            // read(65208,_)->{ok,[]}:领地夺宝时间信息请求无字段,裸发。
            SendFmt(Proto.ACTIVITYFORESHOW_SNATCH_TIME);
            RefreshNineSkyIcons();
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
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_SNATCH_TREASURE, (int)m.SnatchEndTime);
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_SNATCH_TREASURE);
        }

        // 九魂圣殿两图标(135@0@1 / 135@0@2):无服务端信号,由 NineSkyForeshowEnabled(默认 false)兜底。
        // 放行时走 AddIconAsync(仍会过图标配置门 open_lv/open_day);否则删除。等价老端 GetOpenState()+日历门。
        private void RefreshNineSkyIcons()
        {
            if (ActivityForeshowModel.NineSkyForeshowEnabled)
            {
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_NINE_SKY_ONE);
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_NINE_SKY_TWO);
            }
            else
            {
                ActivityIconManager.Instance.DeleteIcon(ICON_NINE_SKY_ONE);
                ActivityIconManager.Instance.DeleteIcon(ICON_NINE_SKY_TWO);
            }
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
