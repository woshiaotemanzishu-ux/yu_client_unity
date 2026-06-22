// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activityOverView/ActivityOverItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ActivityOverView
{
    public partial class ActivityOverItemBind : BaseView
    {
        public RectTransform _show_box;
        public Image _img_bg;
        public Image _img_title;
        public RectTransform _gp_model;
        public TextMeshProUGUI _lab_name;
        public RectTransform _btn_gain;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_show_box), _show_box);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_gp_model), _gp_model);
            EnsureBound(nameof(_lab_name), _lab_name);
            EnsureBound(nameof(_btn_gain), _btn_gain);
        }
    }
}
