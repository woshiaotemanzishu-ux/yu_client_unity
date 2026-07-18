using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.KfHolyArea
{
    /// <summary>
    /// 神陨禁区(跨服圣域)控制器(对标老客户端 KfHolyAreaController,模块 284)。进游戏请求 28410;
    /// 回包据 GetEntranceOpenState(act_end>0,服务端本期已配置活动窗)增删主界面图标 284。
    /// 服务端在活动状态变化(开启/关闭)时亦会广播 28410,回包一律走同一路径刷新图标。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)时复请求 28410(对标老端 CHANGE_LEVEL→level==open_lv→请求 28410),
    /// 让升到开启等级后图标及时出现(等级门槛由 ActivityIconManager 的图标配置 284 把控)。
    /// 本期只做图标;场景/建筑/积分/排行/拍卖/死亡疲劳等玩法协议(28400-28423)与面板均待用户验收。
    /// 注:老端 KfHolyAreaMainView.ts:140 另绑 DAY_CHANGE→UpdateModeTips()(面板内文案刷新),该面板的
    /// Unity 数据视图脚本尚未转换(仅有 KfHolyAreaMainViewBind 脚手架),本轮不新建,留后续轮次。
    /// </summary>
    public sealed class KfHolyAreaController : BaseController
    {
        public static readonly KfHolyAreaController Instance = new KfHolyAreaController();
        private KfHolyAreaController() { }

        public const string ICON_TYPE = KfHolyAreaModel.ICON_TYPE;

        // 复请求 28410 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.KFHOLYAREA_ACT_STATE, On28410);
            // 对标老端 CHANGE_LEVEL→(level==open_lv)请求 28410:等级变化时复请求。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 KfHolyAreaController.ts:81-86:DAY_CHANGE→无条件复请求28410+RefreshIcon()
            // (见 OnServerDayChange 注释,GetOpenState() 恒真属老端存量 bug,本端如实镜像为无条件发送)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            KfHolyAreaModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 28410)。</summary>
        public void RequestStartup()
        {
            // read(28410,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.KFHOLYAREA_ACT_STATE);
        }

        // 28410: act_start:i, act_end:i(活动状态变化通知/请求回包,对标老端 time_data)
        private void On28410(NetReader r)
        {
            int actStart = (int)r.ReadU32();
            int actEnd = (int)r.ReadU32();

            KfHolyAreaModel m = KfHolyAreaModel.Instance;
            m.SetActTime(actStart, actEnd);
            RefreshIcon();

            GameLog.Info("KfHolyArea", "28410 神陨禁区: act_start={0} act_end={1} open={2}",
                actStart, actEnd, m.GetEntranceOpenState());
        }

        // 对标老端 RefreshIcon():按当前缓存数据复判图标显隐(独立于本次是否重发协议)。
        private void RefreshIcon()
        {
            KfHolyAreaModel m = KfHolyAreaModel.Instance;
            if (m.GetEntranceOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        // 对标老端:主角等级变化复请求 28410(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        // 对标老端 KfHolyAreaController.ts:81-86:
        //   GlobalEventSystem.Bind(ServerTimeModel.DAY_CHANGE, () => {
        //     if (this.kf_holy_area_model.GetOpenState()) { Fire(REQUEST_CCMD_EVENT, 28410) }
        //     this.kf_holy_area_model.RefreshIcon()
        //   })
        // GetOpenState()(KfHolyAreaModel.ts:470-521)所有返回路径都是数组字面量([false,""]/[false,"xx级"]/
        // [bool,""]),JS 里数组恒 truthy——同文件 :601 RefreshIcon() 自己写 `let [bool, tips] = this.GetOpenState()`
        // 解构取值,坐实这里返回的是数组而非布尔。故 ts:82 的 if 恒真 = 老端存量死门,跨天实际是无条件发 28410。
        // 本端如实镜像为无条件发送:少发包会导致面板永久空,比多发包(服务端忽略)更严重的失败模式,此为主控裁决理由。
        private void OnServerDayChange()
        {
            RequestStartup();
            RefreshIcon();
        }
    }
}
