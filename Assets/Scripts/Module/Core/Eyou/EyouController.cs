using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Eyou
{
    /// <summary>
    /// Eyou(易游渠道)控制器(对标老客户端 EyouManager)。管理两个平台/渠道驱动的主界面图标:
    /// 666 = Facebook 关注按钮,777 = 注销账号入口。**两者均无服务端协议**——老端全靠平台条件与 native 回调
    /// 决定增删,不发/不收任何 pt 协议,故本控制器不 RegisterProtocal 任何协议(仅挂等级变化事件复评)。
    ///
    /// 进游戏(对标老端 GAME_START→InitEyouInterface + InitCancelAccount)时评估两图标:
    ///  - 666:老端 InitEyouInterface→IsShowFB()→native 异步回调 SetFbState→RefreshFbState(IsEyou && IsConch &&
    ///    fb_state 1/2 才显)。本端无 native 桥,fb_state 恒 0 → 默认不显(预留 <see cref="SetFbState"/> 接桥)。
    ///  - 777:老端 InitCancelAccount(IsEyou && IsIOSConch && is_wx_alpha 才显)。纯平台门,默认不显。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)时复评 FB 图标(对标老端 CHANGE_LEVEL→RefreshFbState,过 open_lv 等级门)。
    ///
    /// 忠实移植:平台门默认 false(见 <see cref="EyouModel"/>),两图标默认都不显示;老端的埋点打点
    /// (EyouDataPlacement/RefreshGoToApp/UpdateLvDataPost 等)与图标无关,不在本期图标移植范围。
    /// </summary>
    public sealed class EyouController : BaseController
    {
        public static readonly EyouController Instance = new EyouController();
        private EyouController() { }

        public const string FB_ICON_TYPE = EyouModel.FB_ICON_TYPE;
        public const string CANCEL_ACCOUNT_ICON_TYPE = EyouModel.CANCEL_ACCOUNT_ICON_TYPE;

        // 复评 FB 图标的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时复评。
        private int _lastLevel = -1;

        protected override void Register()
        {
            // 无服务端协议:Eyou 图标全由平台/渠道门驱动(见类注释),故此处不 RegisterProtocal。
            // 对标老端 CHANGE_LEVEL→RefreshFbState:等级变化时复评 FB 图标(过 open_lv 等级门)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(FB_ICON_TYPE);
            ActivityIconManager.Instance.DeleteIcon(CANCEL_ACCOUNT_ICON_TYPE);
            EyouModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>
        /// 进游戏入口(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START→
        /// InitEyouInterface + InitCancelAccount)。无服务端请求,仅按平台门评估两图标。
        /// </summary>
        public void RequestStartup()
        {
            // 老端 InitEyouInterface:IsEyou 时才走 native IsShowFB→SetFbState 异步回调刷 FB 图标。
            // 本端无 native 桥,fb_state 恒 0 → FB 图标不显(TODO 接桥见 SetFbState)。
            RefreshFbIcon();
            // 老端 InitCancelAccount:纯平台门(IsEyou && IsIOSConch && is_wx_alpha)决定 777 增删。
            RefreshCancelAccountIcon();
        }

        /// <summary>
        /// FB 按钮状态回调入口(对标老端 EyouManager.SetFbState:native IsShowFB 回调写 fb_state 后复评 FB 图标)。
        /// 本端预留:接入 Eyou native 桥后由平台层调用(参数 0=无按钮 / 1=有特效 / 2=无特效)。
        /// TODO(eyou-source):特效区分(老端 fb_state==1 挂 "ui_zhujiemianzhuanquan" 旋转特效、==2 无特效)
        /// 待图标特效系统接入后补,当前两态都只做图标增删。
        /// </summary>
        public void SetFbState(int state)
        {
            EyouModel.Instance.SetFbState(state);
            RefreshFbIcon();
        }

        // FB 图标(666):走 AddIconAsync 过配置门(与老端 addIcon 内部 open_lv/开服天/审核校验一致)。
        private void RefreshFbIcon()
        {
            EyouModel m = EyouModel.Instance;
            bool open = m.GetFbIconOpenState();
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(FB_ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(FB_ICON_TYPE);

            GameLog.Info("Eyou", "FB 图标(666): eyou={0} conch={1} fbState={2} open={3}",
                EyouModel.IsEyouPlatform, EyouModel.IsConch, m.FbState, open);
        }

        // 注销账号图标(777):纯平台门,同样走 AddIconAsync 过配置门(对标老端 addIcon)。
        private void RefreshCancelAccountIcon()
        {
            bool open = EyouModel.Instance.GetCancelAccountOpenState();
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(CANCEL_ACCOUNT_ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(CANCEL_ACCOUNT_ICON_TYPE);

            GameLog.Info("Eyou", "注销账号图标(777): eyou={0} iosConch={1} wxAlpha={2} open={3}",
                EyouModel.IsEyouPlatform, EyouModel.IsIOSConch, EyouModel.IsWxAlpha, open);
        }

        // 对标老端:主角等级变化复评 FB 图标(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时复评)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RefreshFbIcon();
        }
    }
}
