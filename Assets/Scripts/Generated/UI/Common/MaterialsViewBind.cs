// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/MaterialsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class MaterialsViewBind : BaseView
    {
        public Image _bg1;
        public Image _bg2;
        public Image _Image11;
        public RectTransform icon;
        public Image _Image2;
        public TextMeshProUGUI _Image3;
        public TextMeshProUGUI goodName;
        public TextMeshProUGUI goodNamehtml;
        public TextMeshProUGUI goodType;
        public ScrollRect itemScroller;
        public RectTransform item_con;
        public Image btnClose;
        public ScrollRect spcialScroller;
        public RectTransform special_con;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_MaterialsItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg1), _bg1);
            EnsureBound(nameof(_bg2), _bg2);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(goodName), goodName);
            EnsureBound(nameof(goodNamehtml), goodNamehtml);
            EnsureBound(nameof(goodType), goodType);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(item_con), item_con);
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(spcialScroller), spcialScroller);
            EnsureBound(nameof(special_con), special_con);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_MaterialsItem), _tpl_MaterialsItem);
        }
    }
}
