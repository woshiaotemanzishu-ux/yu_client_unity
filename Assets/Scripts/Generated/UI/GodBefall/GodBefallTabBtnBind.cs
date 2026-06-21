// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallTabBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallTabBtnBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public TextMeshProUGUI _lb_name;
        public Image _img_arrow;
        public Image _img_icon;
        public RectTransform _box_sub_con;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_box_sub_con), _box_sub_con);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
