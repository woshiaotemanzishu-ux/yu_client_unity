// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossdomain/BossDomainItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bossdomain
{
    public partial class BossDomainItemBind : BaseView
    {
        public Image _item_ng;
        public Image _head_bg;
        public Image _head_img;
        public RectTransform _gp;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI lv;
        public TextMeshProUGUI _lb_time;
        public RectTransform gp_scene;
        public TextMeshProUGUI text_scene;
        public Image _img_relive1;
        public TextMeshProUGUI _lb_equip;
        public Image _img_equip;
        public Image _img_select;
        public Image _img_icon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_item_ng), _item_ng);
            EnsureBound(nameof(_head_bg), _head_bg);
            EnsureBound(nameof(_head_img), _head_img);
            EnsureBound(nameof(_gp), _gp);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(lv), lv);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(gp_scene), gp_scene);
            EnsureBound(nameof(text_scene), text_scene);
            EnsureBound(nameof(_img_relive1), _img_relive1);
            EnsureBound(nameof(_lb_equip), _lb_equip);
            EnsureBound(nameof(_img_equip), _img_equip);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_icon), _img_icon);
        }
    }
}
