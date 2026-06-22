// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHotPoint/kfHotPointItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHotPoint
{
    public partial class KfHotPointItemBind : BaseView
    {
        public Image _img_1;
        public Image _img_bg;
        public TextMeshProUGUI _desc;
        public TextMeshProUGUI _cond;
        public RectTransform _btn_go;
        public TextMeshProUGUI labelDisplay;
        public Image _img_icon;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _point;
        public Image _draw;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_1), _img_1);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_desc), _desc);
            EnsureBound(nameof(_cond), _cond);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_point), _point);
            EnsureBound(nameof(_draw), _draw);
        }
    }
}
