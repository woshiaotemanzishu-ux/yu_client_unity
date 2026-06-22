// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonBall/DragonBallAttrView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonBall
{
    public partial class DragonBallAttrViewBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _html_type;
        public TextMeshProUGUI _html_fight;
        public RectTransform _box_item;
        public ScrollRect _panel_con;
        public TextMeshProUGUI _html_attr;
        public RectTransform _box_skill;
        public Image _img_skill_icon;
        public TextMeshProUGUI _lb_skill_name;
        public TextMeshProUGUI _html_skill_desc;
        public RectTransform _box_active_title;
        public TextMeshProUGUI _html_active_title;
        public TextMeshProUGUI _html_active_desc;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_html_type), _html_type);
            EnsureBound(nameof(_html_fight), _html_fight);
            EnsureBound(nameof(_box_item), _box_item);
            EnsureBound(nameof(_panel_con), _panel_con);
            EnsureBound(nameof(_html_attr), _html_attr);
            EnsureBound(nameof(_box_skill), _box_skill);
            EnsureBound(nameof(_img_skill_icon), _img_skill_icon);
            EnsureBound(nameof(_lb_skill_name), _lb_skill_name);
            EnsureBound(nameof(_html_skill_desc), _html_skill_desc);
            EnsureBound(nameof(_box_active_title), _box_active_title);
            EnsureBound(nameof(_html_active_title), _html_active_title);
            EnsureBound(nameof(_html_active_desc), _html_active_desc);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
