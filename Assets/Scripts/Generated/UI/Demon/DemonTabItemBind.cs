// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonTabItemBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _gp_con;
        public Image _img_icon;
        public Image _img_name_bg;
        public Image _img_level_bg;
        public TextMeshProUGUI _lb_level;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_stars;
        public Image _img_tips;
        public Image _select;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_name_bg), _img_name_bg);
            EnsureBound(nameof(_img_level_bg), _img_level_bg);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_stars), _gp_stars);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_select), _select);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
