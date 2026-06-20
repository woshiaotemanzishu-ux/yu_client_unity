// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageFlowItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageFlowItemBind : BaseView
    {
        public Image _img_banner;
        public Image _img_title;
        public RectTransform _box;
        public TextMeshProUGUI no;
        public TextMeshProUGUI _lb_tips;
        public RectTransform _btn_go;
        public Image img1;
        public TextMeshProUGUI _lb_go;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_banner), _img_banner);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(no), no);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(img1), img1);
            EnsureBound(nameof(_lb_go), _lb_go);
        }
    }
}
