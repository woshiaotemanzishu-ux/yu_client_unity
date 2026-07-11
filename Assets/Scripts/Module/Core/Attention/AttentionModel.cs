using Shenxiao.Generated.Config;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Attention
{
    /// <summary>
    /// 关注数据(对标老客户端 commonModel/AttentionModel)。承载两个「平台/渠道 + 奖励态」驱动的主界面
    /// 图标,两者**均无服务端协议**(老端 AttentionController.RegisterAllProtocals 为空):
    ///  - 113(收藏小程序 / 通用关注,configfunctionicon loc3、open_lv=90):对标老端 CheckIconOpen,
    ///    走 ClientAttention 渠道配置 + ClientConfig.attention_open 开关 + 开服天/等级门。
    ///  - 113113(关注 / SDK 关注,configfunctionicon loc2、open_lv=0):对标老端 UpdateSdkAttentionState,
    ///    走爱疯平台判定(IsAiFengGamePlatform)+ native subscribe(enabled)开启态 + CustomActivity
    ///    (base_type=70,sub_type=1)第 1 个奖励未领态。
    ///
    /// 忠实移植铁律:原生客户端无 SDK 桥、无 attention_open 注入、无 CustomActivity 奖励态回填,下面的
    /// 「平台/构建级」开关与 SDK 运行态默认取原生态值(false / null)→ **两图标默认都不显示**(与老端
    /// 非爱疯、无 attention_open 配置包一致)。与 <see cref="PlatformModel.IsAlpha"/>、
    /// FriendInviteModel.ShareOpen、EyouModel 的平台门同属「登录引导按真实渠道信号赋值」的一类。
    /// 面板/二维码/复制公众号/领奖等 UI 待用户验收,本期只做图标。
    /// </summary>
    public sealed class AttentionModel
    {
        public static readonly AttentionModel Instance = new AttentionModel();
        private AttentionModel() { }

        /// <summary>通用关注图标类型(对标老端 AttentionModel.IconType=113,收藏小程序,loc3、open_lv=90)。</summary>
        public const string ICON_TYPE = "113";

        /// <summary>SDK 关注图标类型(对标老端 AttentionModel.SdkIconType=113113,关注,loc2、open_lv=0)。</summary>
        public const string SDK_ICON_TYPE = "113113";

        /// <summary>SDK 关注奖励定位:CustomActivity base_type(对标老端 AttentionModel.ActivityBaseType=70)。</summary>
        public const int ActivityBaseType = 70;

        /// <summary>SDK 关注奖励定位:CustomActivity sub_type(对标老端 AttentionModel.ActivitySubType=1)。</summary>
        public const int ActivitySubType = 1;

        // ============ 通用关注(113)平台/构建级门 ============

        /// <summary>
        /// 通用关注开关(对标老端 ClientConfig.attention_open)。原生包无此配置注入,默认 false → 113 不显。
        /// TODO(attention-source):接入渠道/构建配置后,在登录引导里(主界面建活动图标之前)按真实开关赋值。
        /// </summary>
        public static bool AttentionOpen { get; set; }

        // ============ SDK 关注(113113)平台/构建级门 ============

        /// <summary>
        /// 是否深海爱疯混服平台(对标老端 PlatformManager.IsAiFengGamePlatform():plat_name 含 "jzy_sh921_aifeng")。
        /// 原生包默认 false → 113113 不显。TODO(attention-source):由平台层按 LoginModel.PlatName 判定后赋值。
        /// (老端还叠加 Util.IsDebug();本端无 debug 覆盖入口,只留平台判定,默认 false。)
        /// </summary>
        public static bool IsAiFengGamePlatform { get; set; }

        // ============ SDK 运行态(对标老端 sdk_open_state / activity_reward_state,native/活动回调写入) ============

        /// <summary>
        /// native subscribe(apiType=enabled)回调的开启态(对标老端 sdk_open_state)。
        /// null=未回调(原生无桥,恒 null → 图标不显),true=已开启,false=未开启。
        /// </summary>
        public bool? SdkOpenState;

        /// <summary>
        /// CustomActivity(base_type=70,sub_type=1)第 1 个奖励是否已领(status==2)(对标老端 activity_reward_state)。
        /// null=活动数据未到,true=已领(收起图标),false=未领(满足显示条件之一)。原生无回填,恒 null。
        /// </summary>
        public bool? ActivityRewardState;

        public void SetSdkOpenState(bool open) { SdkOpenState = open; }

        public void SetActivityRewardState(bool claimed) { ActivityRewardState = claimed; }

        /// <summary>爱疯平台条件(对标老端 SdkAttentionPlatNameCond = IsAiFengGamePlatform() || IsDebug())。</summary>
        public bool SdkAttentionPlatNameCond() => IsAiFengGamePlatform;

        /// <summary>
        /// 通用关注图标(113)入口开启态(对标老端 CheckIconOpen):
        /// !IsAlpha && attention_open && ClientAttention[plat_name] 存在 && open_day<=开服天 && open_lv<=主角等级。
        /// (图标自身配置 open_lv=90 / need_hide_in_alpha 再由 AddIconAsync 的 FunIsOpenByIconType 叠加把控,与老端
        /// addIcon 内部一致——两道门同时满足才显。)
        /// </summary>
        public bool GetAttentionIconOpenState()
        {
            if (PlatformModel.IsAlpha) return false;
            if (!AttentionOpen) return false;

            string platName = LoginModel.Instance.PlatName ?? "";
            AttentionCfg cfg = ConfigClientAttention.Get(platName);
            if (cfg == null) return false;

            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            int level = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;
            return cfg.open_day <= openDay && cfg.open_lv <= level;
        }

        /// <summary>
        /// SDK 关注图标(113113)入口开启态(对标老端 UpdateSdkAttentionState):
        /// 爱疯平台 && sdk_open_state==true && activity_reward_state==false(奖励未领)。
        /// 老端在 activity_reward_state / sdk_open_state 任一为 null 时早返回不增删;本端等价:
        /// SdkOpenState 非 true 或 ActivityRewardState 非 false 都不显(原生无桥 → 恒不显)。
        /// </summary>
        public bool GetSdkIconOpenState()
        {
            if (!SdkAttentionPlatNameCond()) return false;
            if (SdkOpenState != true) return false;        // null(未回调)/ false(未开启)都不显
            if (ActivityRewardState != false) return false; // null(未到)/ true(已领)都不显
            return true;
        }

        public void Reset()
        {
            SdkOpenState = null;
            ActivityRewardState = null;
        }
    }
}
