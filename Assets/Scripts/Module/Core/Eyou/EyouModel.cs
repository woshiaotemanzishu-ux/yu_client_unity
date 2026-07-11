namespace Shenxiao.Module.Core.Eyou
{
    /// <summary>
    /// Eyou(易游渠道)数据(对标老客户端 EyouManager 的图标相关状态)。承载两个主界面图标的显隐门:
    /// 666 = Facebook 关注按钮,777 = 注销账号入口。两者**均无服务端协议**,完全由平台/渠道条件驱动
    /// (老端 EyouManager 里靠 PlatformManager.IsEyouPlatform()/Util.IsConch()/Util.IsIOSConch()/is_wx_alpha
    /// 及 native IsShowFB 回调决定 add/del),故本端把这些门表示为「构建/渠道级」静态开关,默认全 false
    /// —— 与老端非 Eyou 原生包默认一致(两个图标默认都不显示)。
    ///
    /// 铁律:原生客户端无 Eyou SDK 注入,拿不到平台信号,一律默认不显。接入 Eyou 渠道桥后,由平台层在
    /// 登录引导里按真实渠道信号给下面的静态门赋值(TODO 见各字段),再复评图标即可。
    /// </summary>
    public sealed class EyouModel
    {
        public static readonly EyouModel Instance = new EyouModel();
        private EyouModel() { }

        /// <summary>Facebook 按钮图标类型(对标老端 EyouManager.fb_icon_type=666)。</summary>
        public const string FB_ICON_TYPE = "666";

        /// <summary>注销账号图标类型(对标老端 EyouManager.cancel_account_icon_type=777)。</summary>
        public const string CANCEL_ACCOUNT_ICON_TYPE = "777";

        // ============ 平台/渠道门(构建级静态开关,默认原生态 false → 两图标默认不显) ============
        // 老端真相源:PlatformManager.IsEyouPlatform() / Util.IsConch() / Util.IsIOSConch() /
        // PlatformManager.is_wx_alpha。本端原生包无 Eyou SDK,判定值即为 false(与老端非 Eyou 包默认一致)。
        // 与 PlatformModel.IsAlpha、FriendInviteModel.ShareOpen 同属「平台/构建级」开关。

        /// <summary>
        /// 是否 Eyou(易游)渠道包(对标老端 PlatformManager.IsEyouPlatform())。默认 false。
        /// TODO(eyou-source):接入 Eyou 渠道桥后,在登录引导里(主界面建活动图标之前)按真实渠道标志赋值。
        /// </summary>
        public static bool IsEyouPlatform { get; set; }

        /// <summary>
        /// 是否 Conch(海螺原生容器)环境(对标老端 Util.IsConch())。默认 false。
        /// TODO(eyou-source):由平台层按真实容器类型赋值。
        /// </summary>
        public static bool IsConch { get; set; }

        /// <summary>
        /// 是否 iOS Conch 环境(对标老端 Util.IsIOSConch(),注销账号图标 777 专用门)。默认 false。
        /// TODO(eyou-source):由平台层按真实容器类型赋值。
        /// </summary>
        public static bool IsIOSConch { get; set; }

        /// <summary>
        /// 微信审核态(对标老端 PlatformManager.is_wx_alpha,注销账号图标 777 专用门)。默认 false。
        /// TODO(eyou-source):由平台层按真实审核信号赋值。
        /// </summary>
        public static bool IsWxAlpha { get; set; }

        // ============ FB 按钮运行态(对标老端 fb_state:native IsShowFB 回调经 SetFbState 写入) ============

        /// <summary>
        /// FB 按钮状态(对标老端 EyouManager.fb_state):0=无按钮 / 1=有特效 / 2=无特效。默认 0。
        /// 老端由 PlatformManager.IsShowFB() 走 native CallNativeFunction("IsShowFB") 异步回调 SetFbState 写入;
        /// 本端无 native 桥,恒为 0 → FB 图标默认不显(TODO 见 EyouController.SetFbState)。
        /// </summary>
        public int FbState;

        public void SetFbState(int state)
        {
            FbState = state;
        }

        /// <summary>
        /// FB 图标(666)入口开启态(对标老端 RefreshFbState:IsEyouPlatform && IsConch 前提下,fb_state 为 1/2 才显)。
        /// 老端 fb_state==1 带旋转特效、==2 不带特效,两者都显示图标(本端特效系统未接,仅做增删,特效见 SetFbState 注释)。
        /// 等级门(icon_cfg.open_lv)由 ActivityIconManager.AddIconAsync 的 FunIsOpenByIconType 配置门叠加把控。
        /// </summary>
        public bool GetFbIconOpenState()
        {
            return IsEyouPlatform && IsConch && FbState > 0;
        }

        /// <summary>
        /// 注销账号图标(777)入口开启态(对标老端 InitCancelAccount:
        /// IsEyouPlatform() && Util.IsIOSConch() && is_wx_alpha)。纯平台/渠道门,无任何服务端信号。
        /// </summary>
        public bool GetCancelAccountOpenState()
        {
            return IsEyouPlatform && IsIOSConch && IsWxAlpha;
        }

        public void Reset()
        {
            FbState = 0;
        }
    }
}
