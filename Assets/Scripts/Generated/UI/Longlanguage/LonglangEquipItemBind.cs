// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/longlanguage/longlangEquipItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Longlanguage
{
    public partial class LonglangEquipItemBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _img_bg;
        public Image _img_icon;
        public RectTransform effectGp;
        public RectTransform _gp_stage;
        public Image _img_stage_bg;
        public TextMeshProUGUI _lb_stage;
        public Image redImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(effectGp), effectGp);
            EnsureBound(nameof(_gp_stage), _gp_stage);
            EnsureBound(nameof(_img_stage_bg), _img_stage_bg);
            EnsureBound(nameof(_lb_stage), _lb_stage);
            EnsureBound(nameof(redImg), redImg);
        }
    }
}
