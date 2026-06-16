// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIHiterBigBloodView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIHiterBigBloodViewBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public Image _img_next_blood;
        public Image _img_dark_blood;
        public Image _img_light_blood;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_left_blood;
        public TextMeshProUGUI _lb_hp;
        public TextMeshProUGUI _lb_anger;
        public TextMeshProUGUI _lb_vit;
        public Image drop_gp;
        public TextMeshProUGUI drop_lb;
        public Image _img_head;
        public RectTransform _box_head;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_next_blood), _img_next_blood);
            EnsureBound(nameof(_img_dark_blood), _img_dark_blood);
            EnsureBound(nameof(_img_light_blood), _img_light_blood);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_left_blood), _lb_left_blood);
            EnsureBound(nameof(_lb_hp), _lb_hp);
            EnsureBound(nameof(_lb_anger), _lb_anger);
            EnsureBound(nameof(_lb_vit), _lb_vit);
            EnsureBound(nameof(drop_gp), drop_gp);
            EnsureBound(nameof(drop_lb), drop_lb);
            EnsureBound(nameof(_img_head), _img_head);
            EnsureBound(nameof(_box_head), _box_head);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
