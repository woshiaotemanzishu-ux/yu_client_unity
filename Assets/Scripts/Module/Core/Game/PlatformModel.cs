namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 平台/渠道审核状态(对标老客户端 PlatformManager 的 is_alpha 等审核标志)。
    /// 目前仅需 alpha(审核服)开关:审核态下 need_hide_in_alpha=true 的活动图标必须隐藏
    /// (见 <see cref="Shenxiao.Module.Core.MainUI.ActivityIconManager"/> 的 FunIsOpenByIconType)。
    ///
    /// 老端真相源(PlatformManager.ts):默认 _is_alpha=false;LoginByHttpValidate 里
    /// is_alpha = (GetOption("galpha")=="1")(URL/登录参数);enterGame/roleUpLevel/createRole 的
    /// SDK 回调再按 ret.data.msg.sp 翻转;getter 还叠加 is_wx_alpha / ClientConfig.ignore_cfg_alpha。
    /// 本端本地/开发态无 SDK、无 galpha 参数,可判定值即为 false(与老端默认一致,dev/正式包从不处于审核)。
    /// </summary>
    public static class PlatformModel
    {
        /// <summary>
        /// true = 审核状态(alpha 包,屏蔽充值/审核相关图标)。默认 false。
        /// TODO(alpha-source):接入 SDK 登录后,在 LoginBootstrap.OnFrameworkReady 里(主界面建活动图标之前)
        /// 按真实渠道/SDK 审核信号赋值(老端等价物:galpha=="1" 或 SDK ret.data.msg.sp):
        ///     PlatformModel.IsAlpha = /* 渠道审核标志 */;
        /// 在此之前该标志不被写入,恒为 false。运行时若需翻转,翻转后调用
        /// ActivityIconManager.Instance.RefreshDefaultIconsAsync() 重扫即可(现无翻转源,暂不接)。
        /// </summary>
        public static bool IsAlpha { get; set; }
    }
}
