// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonBall/DragonBallTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonBall
{
    public partial class DragonBallTipsViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _html_type;
        public TextMeshProUGUI _html_fight;
        public RectTransform _box_item;
        public Image _img_icon_bg;
        public Image _img_icon;
        public RectTransform _box_icon_effect;
        public ScrollRect _panel_con;
        public TextMeshProUGUI _lb_base_attr_name;
        public TextMeshProUGUI _html_attr;
        public RectTransform _box_skill_desc;
        public TextMeshProUGUI _html_skill_desc;
        public Image _img_close;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_html_type), _html_type);
            EnsureBound(nameof(_html_fight), _html_fight);
            EnsureBound(nameof(_box_item), _box_item);
            EnsureBound(nameof(_img_icon_bg), _img_icon_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_box_icon_effect), _box_icon_effect);
            EnsureBound(nameof(_panel_con), _panel_con);
            EnsureBound(nameof(_lb_base_attr_name), _lb_base_attr_name);
            EnsureBound(nameof(_html_attr), _html_attr);
            EnsureBound(nameof(_box_skill_desc), _box_skill_desc);
            EnsureBound(nameof(_html_skill_desc), _html_skill_desc);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
