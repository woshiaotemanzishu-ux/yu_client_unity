using System.Collections.Generic;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Login;

namespace Shenxiao.Module.Core.AddVipService
{
    /// <summary>
    /// 关注有礼数据(对标老客户端 commonModel/AddVipServiceModel)。只承载主界面图标 114 的显隐判定,
    /// 面板/微信收藏/领奖等 UI 待用户验收,本期只做图标。
    ///
    /// 老端显隐链(AddVipServiceModel.CheckIconOpen + FirstRechargeController.On15908):
    ///   1) 收到首充 15908 且 is_buy==1 → is_show_add_vip_service_icon=true;
    ///   2) CheckIconOpen():!is_alpha && is_show_add_vip_service_icon 时,加载渠道配置 ClientAddVipService,
    ///      遍历其 tem 列表,若当前 plat_name 命中 → addIcon(114)。
    /// 即:!IsAlpha && IsShowAddVipServiceIcon && plat_name∈ClientAddVipService.tem 三者同时满足才显示。
    /// 老端无等级门、无独立请求协议(纯靠首充 15908 的 is_buy 驱动),故本模型不存等级/时间字段。
    /// </summary>
    public sealed class AddVipServiceModel
    {
        public static readonly AddVipServiceModel Instance = new AddVipServiceModel();
        private AddVipServiceModel() { }

        /// <summary>主界面图标类型(对标老端 addIcon(114) 关注有礼)。</summary>
        public const string ICON_TYPE = "114";

        /// <summary>
        /// 是否允许显示关注有礼图标(对标老端 is_show_add_vip_service_icon)。
        /// 老端仅在首充 15908 回包 is_buy==1 时置 true,一旦置真不再回落;本端由控制器在 IsBuy 变真时置真,
        /// Reset()(断线/登出)清零。
        /// </summary>
        public bool IsShowAddVipServiceIcon;

        /// <summary>
        /// 渠道白名单门(对标老端 CheckIconOpen 内遍历 ClientAddVipService.tem 判 plat_name)。
        /// ClientAddVipService 配置尚未移植到 Unity → 无白名单可查。老端在"非白名单渠道"根本不显示 114
        /// (且关注有礼面板亦未移植,点了无处可去),故此处默认不放行,避免误显。
        /// TODO(client-add-vip-service-cfg): 移植 ClientAddVipService 配置后,把 tem 的渠道名灌入
        ///   <see cref="ChannelWhitelist"/>(或改为运行时读配置),即可让白名单渠道恢复显示。
        /// </summary>
        private static readonly HashSet<string> ChannelWhitelist = new HashSet<string>();

        private static bool ChannelAllowsIcon()
        {
            if (ChannelWhitelist.Count == 0) return false; // 配置未移植:保守不显示
            string platName = LoginModel.Instance.PlatName ?? "";
            return ChannelWhitelist.Contains(platName);
        }

        /// <summary>
        /// 入口开启状态(对标老端 CheckIconOpen 的三重条件)。
        /// !IsAlpha(审核态不显示) && IsShowAddVipServiceIcon(首充 is_buy==1) && 渠道白名单命中。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return !PlatformModel.IsAlpha
                && IsShowAddVipServiceIcon
                && ChannelAllowsIcon();
        }

        public void Reset()
        {
            IsShowAddVipServiceIcon = false;
        }
    }
}
