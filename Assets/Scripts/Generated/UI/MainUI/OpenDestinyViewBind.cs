// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/OpenDestinyView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class OpenDestinyViewBind : BaseView
    {
        public Image bg_img;
        public Image circule_img;
        public Image icon_img;
        public Image icon_img1;
        public RectTransform effect_gp;
        public Image title_img;
        public RectTransform detail_gp;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public TextMeshProUGUI _Label4;
        public TextMeshProUGUI bottom_label;
        public Image _img_click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(circule_img), circule_img);
            EnsureBound(nameof(icon_img), icon_img);
            EnsureBound(nameof(icon_img1), icon_img1);
            EnsureBound(nameof(effect_gp), effect_gp);
            EnsureBound(nameof(title_img), title_img);
            EnsureBound(nameof(detail_gp), detail_gp);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(_Label4), _Label4);
            EnsureBound(nameof(bottom_label), bottom_label);
            EnsureBound(nameof(_img_click), _img_click);
        }
    }
}
