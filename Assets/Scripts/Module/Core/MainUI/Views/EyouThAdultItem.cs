using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 18+ 限制标记项(对标老客户端 EyouThAdultItem.ts,Eyou 泰国平台用):一个可展开/收起的 18+ 图标,
    /// 点图标 → 展开(底图变宽 + 警示文字出现)/ 收起。纯前端交互,无后端依赖。
    ///
    /// 图标照抄 Laya(GetIconOtherPath("mainUI", eyou_th_18 / eyou_th_bg / eyou_th_txt));展开/收起做可见性
    /// 切换(老端的精确缩放/位移补间是 polish + 含预制体相关魔法数,先省,留待用户在预制体调展开态)。
    /// 事件驱动(平台为 Eyou 泰国时显示),默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EyouThAdultItem : EyouThAdultItemBind
    {
        private bool _expanded;
        private bool _clickBound;

        protected override void OnInit()
        {
            if (_img_icon != null)
                _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetIconOtherPath("mainUI", "eyou_th_18"), nativeSize: false);
            if (_box_arrow != null)
                _ = ResManager.SetImageAsync(_box_arrow, GameResPath.GetIconOtherPath("mainUI", "eyou_th_bg"), nativeSize: false);
            if (_img_red != null)
                _ = ResManager.SetImageAsync(_img_red, GameResPath.GetIconOtherPath("mainUI", "eyou_th_txt"), nativeSize: false);

            SetExpanded(false);

            if (!_clickBound && _img_icon != null)
            {
                _img_icon.raycastTarget = true;
                UIUtil.AddClick(_img_icon, OnClickIcon);
                _clickBound = true;
            }
        }

        private void OnClickIcon()
        {
            SetExpanded(!_expanded);
        }

        /// <summary>展开/收起(对标 ScaleBig/ScaleSmall 的最终态:展开才显警示文字;精确补间归预制体)。</summary>
        private void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            if (_img_red != null) _img_red.gameObject.SetActive(expanded);
        }
    }
}
