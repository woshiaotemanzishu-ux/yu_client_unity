using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 婚礼花 / 红包雨 特效容器(对标老客户端 FlowerEffectView.ts):播放婚礼撒花特效 + 红包雨(掉落红包点击领取)。
    /// 含 _group_eff / _gp_rpr + 14 个分组挂点。
    ///
    /// 降级:婚礼数据(MarriageModel.GetFlowerEffectList / STOP_FLOWER_EFFECT)、红包活动(CustomActivityModel /
    /// RPRShowView)、UI 特效系统(AddUIEffect)、掉落补间(SYTweenLite)均未移植 → 容器留空,
    /// ShowFlowerEffect / CreateRPREffect 仅打日志「待对接」。事件驱动弹层(婚礼/红包雨时开),默认关闭、
    /// 不进 FirstPass;婚礼/红包系统 + 特效系统接上后再补撒花与红包雨掉落。
    /// </summary>
    public sealed class FlowerEffectView : FlowerEffectViewBind
    {
        protected override void OnShow(object args)
        {
            ShowFlowerEffect();
        }

        /// <summary>对标 ShowFlowerEffect:从 MarriageModel 取撒花特效逐个播。降级:无数据/特效系统 → 打日志。</summary>
        public void ShowFlowerEffect()
        {
            GameLog.Info("MainUI", "婚礼撒花特效 → 待对接 MarriageModel.GetFlowerEffectList + UI 特效系统 AddUIEffect");
        }

        /// <summary>对标 CreateRPREffect:红包雨掉落 + 点击领取。降级:无红包活动/特效 → 打日志。</summary>
        public void CreateRPREffect()
        {
            GameLog.Info("MainUI", "红包雨特效 → 待对接 CustomActivityModel(红包活动)+ 掉落补间 + RPRShowView");
        }
    }
}
