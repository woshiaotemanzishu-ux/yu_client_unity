// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/DragonBallTooltips.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class DragonBallTooltipsBind : BaseView
    {
        public RectTransform root_wnd;
        public Image img_bg1;
        public Image img_title;
        public TextMeshProUGUI lb_name;
        public Image img_des2;
        public TextMeshProUGUI lb_type;
        public TextMeshProUGUI lb_fightnum;
        public RectTransform gp_item;
        public Image img_title2;
        public TextMeshProUGUI lb_tips;
        public ScrollRect detail_scroller;
        public RectTransform Content;
        public TextMeshProUGUI lb_detail;
        public RectTransform btn_use;
        public Image img_bg;
        public TextMeshProUGUI lb_text;
        public Image img_red;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(root_wnd), root_wnd);
            EnsureBound(nameof(img_bg1), img_bg1);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(img_des2), img_des2);
            EnsureBound(nameof(lb_type), lb_type);
            EnsureBound(nameof(lb_fightnum), lb_fightnum);
            EnsureBound(nameof(gp_item), gp_item);
            EnsureBound(nameof(img_title2), img_title2);
            EnsureBound(nameof(lb_tips), lb_tips);
            EnsureBound(nameof(detail_scroller), detail_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(lb_detail), lb_detail);
            EnsureBound(nameof(btn_use), btn_use);
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(lb_text), lb_text);
            EnsureBound(nameof(img_red), img_red);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
