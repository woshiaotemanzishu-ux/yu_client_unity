using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>场景态临时挂点；所有节点默认隐藏，只允许对应系统事件按需点亮。</summary>
    public sealed class MainUISceneAssistView : MainUISceneAssistViewBind
    {
        protected override void OnInit()
        {
            SetActive(_box_auto_effect, false);
            SetActive(_box_please, false);
            SetActive(_gp_t_map, false);
            SetActive(_gp_pro, false);
            SetActive(_img_tt_record, false);

            RouteClick(_box_please, "marriage_gift_tips");
            RouteClick(_img_rpr, "redpacket_rain");
            RouteClick(_img_tt_record, "tt_record");
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target != null) UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null) target.gameObject.SetActive(active);
        }
    }
}
