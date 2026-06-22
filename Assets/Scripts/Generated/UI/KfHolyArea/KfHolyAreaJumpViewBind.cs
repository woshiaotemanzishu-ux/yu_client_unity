// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaJumpView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaJumpViewBind : BaseView
    {
        public Image img_bg;
        public TextMeshProUGUI desc_lb;
        public Image _img_close;
        public RectTransform _gp_enter;
        public RectTransform _gp_cenel;
        public RectTransform desc_gp;
        public Image _img_scene;
        public TextMeshProUGUI _lb_scene_name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(desc_lb), desc_lb);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_gp_enter), _gp_enter);
            EnsureBound(nameof(_gp_cenel), _gp_cenel);
            EnsureBound(nameof(desc_gp), desc_gp);
            EnsureBound(nameof(_img_scene), _img_scene);
            EnsureBound(nameof(_lb_scene_name), _lb_scene_name);
        }
    }
}
